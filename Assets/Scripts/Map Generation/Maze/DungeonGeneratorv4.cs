using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Pathfinding; // A* nearest-node lookup

/// Dungeon generator for rooms+mazes, based on https://journal.stuffwithstuff.com/2014/12/21/rooms-and-mazes/

[ExecuteInEditMode]
public class DungeonGenerator_v4 : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap wallTilemap;
    public Tilemap floorTilemap;

    [Header("Tiles")]
    public TileBase wallTile;
    public TileBase floorTile;
    public TileBase closedDoorTile;
    public TileBase openDoorTile;

    [Header("Grid / Origin")]
    public int width = 33;   // must be odd
    public int height = 33;  // must be odd
    public Vector3Int originCell = Vector3Int.zero; // bottom-left cell for the dungeon

    [Header("Dungeon parameters")]
    public int numRoomTries = 50;
    public int roomExtraSize = 0; // allow bigger rooms
    [Range(0, 100)]
    public int windingPercent = 0;
    public int extraConnectorChance = 20; // 1-in-X chance to force extra connector

    [Header("Chunk Connectivity")]
    [Tooltip("Ensure connections at the exact center of each edge for perfect chunk alignment")]
    public bool enableChunkConnections = true;

    [Header("Seed")] // confirmed that seeds do persist across editor and play mode sessions
    public int seed = 0;
    public bool useRandomSeed = true;

    // Internal representation
    // 0 = wall, 1 = floor, 2 = closed door, 3 = open door
    private int[,] _tiles;
    private int[,] _regions;
    private int _currentRegion = -1;
    private List<RectInt> _rooms = new List<RectInt>();
    private System.Random _rng = new System.Random();

    // Directions (cardinal)
    private static readonly Vector2Int[] CardDirs = {
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0)
    };

    public void Start()
    {
        // In editor mode leave generation to manual call.
        if (Application.isPlaying)
        {
            Generate();
            // Player spawning moved to separate component: SpawnPlayerInMaze
        }
    }

    [ContextMenu("Generate Dungeon")]
    public void Generate()
    {
        if (!useRandomSeed)
        {
            SetSeed(seed);
        }

        if (width % 2 == 0 || height % 2 == 0)
        {
            Debug.LogError("Dungeon width and height must be odd.");
            return;
        }

        // Initialize arrays
        _tiles = new int[width, height];
        _regions = new int[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                _tiles[x, y] = 0; // wall
        _rooms.Clear();
        _currentRegion = -1;

        AddRooms();
        // Fill remaining areas with mazes
        for (int y = 1; y < height; y += 2)
        {
            for (int x = 1; x < width; x += 2)
            {
                if (_tiles[x, y] == 0) GrowMaze(new Vector2Int(x, y));
            }
        }

        ConnectRegions();
        
        if (enableChunkConnections)
        {
            CarveChunkBoundaryConnections(); // Ensure chunk connections at exact center of each edge
        }

        RemoveDeadEnds();
        PaintToTilemaps();

        // spawn player after generation
        var spawner = GetComponent<SpawnPlayerInMaze>();
        if (spawner != null)
        {
            spawner.TrySpawnOrRepositionPlayer();
        }
    }

    // passes chunk origin before calling generate()
    public void GenerateAsChunk(Vector3Int chunkOrigin)
    {
        originCell = chunkOrigin;
        Generate();
    }

    #region Rooms and Maze generation

    private void AddRooms()
    {
        for (int i = 0; i < numRoomTries; i++)
        {
            int size = _rng.Next(1, 3 + roomExtraSize) * 2 + 1;
            int rectangularity = _rng.Next(0, 1 + size / 2) * 2;
            int roomW = size;
            int roomH = size;
            if (_rng.Next(2) == 0) roomW += rectangularity;
            else roomH += rectangularity;

            int x = _rng.Next((width - roomW) / 2 + 1) * 2 - 1;
            int y = _rng.Next((height - roomH) / 2 + 1) * 2 - 1;

            // convert to bottom-left origin (x,y) might be negative if rng gives 0 -> ensure in bounds
            x = Mathf.Clamp(x, 1, width - roomW - 0);
            y = Mathf.Clamp(y, 1, height - roomH - 0);

            var room = new RectInt(x, y, roomW, roomH);

            bool overlaps = false;
            foreach (var other in _rooms)
            {
                // distance check with 1 tile padding
                RectInt expanded = other;
                expanded.xMin -= 1; expanded.yMin -= 1;
                expanded.xMax += 1; expanded.yMax += 1;
                if (expanded.Overlaps(room))
                {
                    overlaps = true;
                    break;
                }
            }

            if (overlaps) continue;

            _rooms.Add(room);
            StartRegion();
            for (int rx = room.xMin; rx <= room.xMax; rx++)
            {
                for (int ry = room.yMin; ry <= room.yMax; ry++)
                {
                    Carve(new Vector2Int(rx, ry));
                }
            }
        }
    }

    private void GrowMaze(Vector2Int start)
    {
        var cells = new List<Vector2Int>();
        Vector2Int? lastDir = null;

        StartRegion();
        Carve(start);
        cells.Add(start);

        while (cells.Count > 0)
        {
            var cell = cells[cells.Count - 1];
            var unmade = new List<Vector2Int>();

            foreach (var dir in CardDirs)
            {
                if (CanCarve(cell, dir)) unmade.Add(dir);
            }

            if (unmade.Count > 0)
            {
                Vector2Int dir;
                if (lastDir.HasValue && unmade.Contains(lastDir.Value) && _rng.Next(100) > windingPercent)
                {
                    dir = lastDir.Value;
                }
                else
                {
                    dir = unmade[_rng.Next(unmade.Count)];
                }

                Carve(cell + dir);
                Carve(cell + dir * 2);
                cells.Add(cell + dir * 2);
                lastDir = dir;
            }
            else
            {
                cells.RemoveAt(cells.Count - 1);
                lastDir = null;
            }
        }
    }

    private bool CanCarve(Vector2Int pos, Vector2Int dir)
    {
        Vector2Int end = pos + dir * 3;
        if (!InBounds(end)) return false;
        return GetTile(pos + dir * 2) == 0;
    }

    #endregion

    #region Regions & Connectors

    private void ConnectRegions()
    {
        var connectorRegions = new Dictionary<Vector2Int, HashSet<int>>();

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (_tiles[x, y] != 0) continue;
                var regions = new HashSet<int>();
                foreach (var d in CardDirs)
                {
                    int r = _regions[x + d.x, y + d.y];
                    if (r != 0) regions.Add(r);
                }

                if (regions.Count >= 2) connectorRegions[new Vector2Int(x, y)] = regions;
            }
        }

        var connectors = new List<Vector2Int>(connectorRegions.Keys);

        // merged map: original -> representative
        var merged = new Dictionary<int, int>();
        var openRegions = new HashSet<int>();
        for (int i = 0; i <= _currentRegion; i++)
        {
            merged[i] = i;
            openRegions.Add(i);
        }

        while (openRegions.Count > 1 && connectors.Count > 0)
        {
            var connector = connectors[_rng.Next(connectors.Count)];
            AddJunction(connector);

            var regions = connectorRegions[connector];
            var mapped = new List<int>();
            foreach (var r in regions) mapped.Add(merged[r]);
            int dest = mapped[0];
            var sources = mapped.GetRange(1, mapped.Count - 1);

            // Merge
            for (int i = 0; i <= _currentRegion; i++)
            {
                if (sources.Contains(merged[i])) merged[i] = dest;
            }

            foreach (var s in sources) openRegions.Remove(s);

            // Remove unusable connectors
            connectors.RemoveAll(pos =>
            {
                if (Mathf.Abs(connector.x - pos.x) + Mathf.Abs(connector.y - pos.y) < 2) return true; // no adjacent connectors
                var regSet = new HashSet<int>();
                foreach (var r in connectorRegions[pos]) regSet.Add(merged[r]);
                if (regSet.Count > 1) return false;
                // occasionally add extra openings
                if (_rng.Next(extraConnectorChance) == 0) AddJunction(pos);
                return true;
            });
        }
    }

    private void AddJunction(Vector2Int pos)
    {
        int choice = _rng.Next(4);
        if (choice == 0)
        {
            // open door / floor mixture
            _tiles[pos.x, pos.y] = _rng.Next(3) == 0 ? 3 : 1; // open door or floor
        }
        else
        {
            // closed door
            _tiles[pos.x, pos.y] = 2;
        }
    }

    #endregion

    #region Chunk Boundary Connections

    /// <summary>
    /// Carves guaranteed floor connections at the exact center of each chunk edge.
    /// This ensures perfect alignment between neighboring chunks.
    /// </summary>
    private void CarveChunkBoundaryConnections()
    {
        // Calculate exact center positions (always odd for maze compatibility)
        int centerX = width / 2;  // For 33, this is 16 (middle index)
        int centerY = height / 2; // For 33, this is 16 (middle index)

        // Ensure centers are odd (for maze grid alignment)
        if (centerX % 2 == 0) centerX++;
        if (centerY % 2 == 0) centerY++;

        // Carve connections at exact center of each edge
        CarveEdgeConnection(EdgeDirection.North, centerX);
        CarveEdgeConnection(EdgeDirection.South, centerX);
        CarveEdgeConnection(EdgeDirection.East, centerY);
        CarveEdgeConnection(EdgeDirection.West, centerY);
    }

    private enum EdgeDirection { North, South, East, West }

    /// <summary>
    /// Carves a single connection at the specified position on the given edge
    /// </summary>
    private void CarveEdgeConnection(EdgeDirection edge, int position)
    {
        Vector2Int edgeCell = GetEdgeCellPosition(edge, position);
        
        if (!InBounds(edgeCell))
        {
            Debug.LogWarning($"Edge connection at {edge} position {position} is out of bounds!");
            return;
        }

        // Carve the edge cell (guarantee it's a floor)
        _tiles[edgeCell.x, edgeCell.y] = 1; // floor

        // Carve inward to connect to existing dungeon regions
        CarvePathToNearestRegion(edgeCell, edge);
    }

    /// <summary>
    /// Gets the cell position for a given edge and position along that edge
    /// </summary>
    private Vector2Int GetEdgeCellPosition(EdgeDirection edge, int position)
    {
        switch (edge)
        {
            case EdgeDirection.North:
                return new Vector2Int(position, height - 1);
            case EdgeDirection.South:
                return new Vector2Int(position, 0);
            case EdgeDirection.East:
                return new Vector2Int(width - 1, position);
            case EdgeDirection.West:
                return new Vector2Int(0, position);
            default:
                return Vector2Int.zero;
        }
    }

    /// <summary>
    /// Carves a straight path from the edge cell inward until it connects to an existing region
    /// </summary>
    private void CarvePathToNearestRegion(Vector2Int start, EdgeDirection edge)
    {
        Vector2Int direction = GetInwardDirection(edge);
        Vector2Int current = start;
        int maxSteps = Mathf.Max(width, height);
        int steps = 0;

        while (steps < maxSteps)
        {
            if (!InBounds(current)) break;

            // Check if we've reached an existing floor region
            if (_tiles[current.x, current.y] != 0 && current != start)
            {
                // Connected to existing dungeon!
                return;
            }

            // Carve current cell as floor
            _tiles[current.x, current.y] = 1;

            // Check if any adjacent cell (not in the direction we came from) is floor
            bool hasAdjacentFloor = false;
            foreach (var dir in CardDirs)
            {
                // Skip checking the direction we're moving (already carved)
                if (dir == direction * -1) continue;
                
                Vector2Int neighbor = current + dir;
                if (InBounds(neighbor) && _tiles[neighbor.x, neighbor.y] != 0)
                {
                    hasAdjacentFloor = true;
                    break;
                }
            }

            if (hasAdjacentFloor && current != start)
            {
                // Successfully connected to dungeon
                return;
            }

            // Move inward
            current += direction;
            steps++;
        }

        // If we reached here, we carved all the way through without finding a floor
        // This is OK - the path itself provides connectivity
    }

    /// <summary>
    /// Gets the inward direction vector for carving from an edge
    /// </summary>
    private Vector2Int GetInwardDirection(EdgeDirection edge)
    {
        switch (edge)
        {
            case EdgeDirection.North:
                return new Vector2Int(0, -1);
            case EdgeDirection.South:
                return new Vector2Int(0, 1);
            case EdgeDirection.East:
                return new Vector2Int(-1, 0);
            case EdgeDirection.West:
                return new Vector2Int(1, 0);
            default:
                return Vector2Int.zero;
        }
    }

    #endregion

    #region Utilities: carve, regions, dead ends

    private void StartRegion()
    {
        _currentRegion++;
    }

    private void Carve(Vector2Int pos)
    {
        if (!InBounds(pos)) return;
        _tiles[pos.x, pos.y] = 1; // floor
        _regions[pos.x, pos.y] = _currentRegion;
    }

    private int GetTile(Vector2Int pos)
    {
        if (!InBounds(pos)) return 0;
        return _tiles[pos.x, pos.y];
    }

    private bool InBounds(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < width && p.y < height;

    private void RemoveDeadEnds()
    {
        bool done = false;
        while (!done)
        {
            done = true;
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    if (_tiles[x, y] == 0) continue;
                    
                    // Don't remove edge cells (they may be chunk connections)
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1) continue;
                    
                    int exits = 0;
                    foreach (var d in CardDirs)
                    {
                        if (GetTile(new Vector2Int(x + d.x, y + d.y)) != 0) exits++;
                    }
                    if (exits != 1) continue;
                    done = false;
                    _tiles[x, y] = 0; // fill in
                    _regions[x, y] = 0;
                }
            }
        }
    }

    #endregion

    #region Paint to Tilemaps

    private void PaintToTilemaps()
    {
        if (wallTilemap == null || floorTilemap == null || wallTile == null || floorTile == null)
        {
            Debug.LogWarning("Tilemaps or tiles are not fully assigned.");
            return;
        }

        // Paint only the area for this generator's chunk.
        // Do NOT call ClearAllTiles() on shared tilemaps — that will wipe neighbouring chunks.
        var bounds = new BoundsInt(originCell, new Vector3Int(width, height, 1));

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var cell = new Vector3Int(originCell.x + x, originCell.y + y, originCell.z);
                int t = _tiles[x, y];

                // IMPORTANT: when writing into shared Tilemaps, be explicit about each cell.
                // Set tile to null or appropriate tile depending on generator data.
                if (t == 0)
                {
                    wallTilemap.SetTile(cell, wallTile);
                    floorTilemap.SetTile(cell, null);
                }
                else if (t == 1)
                {
                    floorTilemap.SetTile(cell, floorTile);
                    wallTilemap.SetTile(cell, null);
                }
                else if (t == 2)
                {
                    floorTilemap.SetTile(cell, floorTile);
                    wallTilemap.SetTile(cell, closedDoorTile ?? wallTile);
                }
                else if (t == 3)
                {
                    floorTilemap.SetTile(cell, floorTile);
                    wallTilemap.SetTile(cell, openDoorTile ?? floorTile);
                }
            }
        }

        // Refresh only the modified tilemaps. RefreshAllTiles is OK but may be heavier.
        wallTilemap.RefreshAllTiles();
        floorTilemap.RefreshAllTiles();
    }

    #endregion

    #region Helpers for debug, deterministic seed, and ensuring the player doesn't spawn in a wall

    public void SetSeed(int seed)
    {
        _rng = new System.Random(seed);
    }

  // validation 1: try to spawn in center of first room created by generator
    public bool TryGetFirstRoomSpawnCell(out Vector3Int spawnCell)
    {
        spawnCell = originCell;
        if (_rooms == null || _rooms.Count == 0) return false;

        RectInt firstRoom = _rooms[0];

        // Calculate the center of the first room in local cell coordinates
        int centerX = originCell.x + Mathf.RoundToInt(firstRoom.center.x);
        int centerY = originCell.y + Mathf.RoundToInt(firstRoom.center.y);
        Vector3Int desiredCell = new Vector3Int(centerX, centerY, originCell.z);

        // Use internal A* helper to get nearest walkable cell (may return desiredCell)
        spawnCell = GetNearestWalkableCell(desiredCell);
        return true;
    }

    // validation 2: else local nearest walkable cell via A*
    private Vector3Int GetNearestWalkableCell(Vector3Int desiredCell)
    {
        if (AstarPath.active == null)
        {
            Debug.LogError("GetNearestWalkableCell: AstarPath.active is null. Expected A* to be present.");
            return desiredCell;
        }

        if (floorTilemap == null)
        {
            Debug.LogWarning("GetNearestWalkableCell: floorTilemap is null. Cannot restrict to floor tiles.");
            // Fallback to original behaviour
            Vector3 queryWorldFallback = new Vector3(desiredCell.x + 0.5f, desiredCell.y + 0.5f, 0f);
            var nnFallback = AstarPath.active.GetNearest(queryWorldFallback);
            var nodeFallback = nnFallback.node;
            if (nodeFallback != null && nodeFallback.Walkable)
            {
                Vector3 nodeWorld = (Vector3)nodeFallback.position;
                int cx = Mathf.RoundToInt(nodeWorld.x);
                int cy = Mathf.RoundToInt(nodeWorld.y);

                // Clamp to this chunk bounds
                int minX = originCell.x;
                int maxX = originCell.x + width - 1;
                int minY = originCell.y;
                int maxY = originCell.y + height - 1;

                cx = Mathf.Clamp(cx, minX, maxX);
                cy = Mathf.Clamp(cx, minY, maxY);

                return new Vector3Int(cx, cy, originCell.z);
            }

            Debug.LogWarning("GetNearestWalkableCell: A* did not return a walkable node. Using desired cell.");
            return desiredCell;
        }

        // find nearest walkable A* node
        Vector3 queryWorld = new Vector3(desiredCell.x + 0.5f, desiredCell.y + 0.5f, 0f);
        var nn = AstarPath.active.GetNearest(queryWorld);
        var node = nn.node;

        // Helper local to map world/node position to clamped chunk cell
        Vector3Int ClampToChunkCell(Vector3 worldPos)
        {
            int cx = Mathf.RoundToInt(worldPos.x);
            int cy = Mathf.RoundToInt(worldPos.y);

            int minX = originCell.x;
            int maxX = originCell.x + width - 1;
            int minY = originCell.y;
            int maxY = originCell.y + height - 1;

            cx = Mathf.Clamp(cx, minX, maxX);
            cy = Mathf.Clamp(cy, minY, maxY);

            return new Vector3Int(cx, cy, originCell.z);
        }

        if (node != null && node.Walkable)
        {
            Vector3 nodeWorld = (Vector3)node.position;
            var candidate = ClampToChunkCell(nodeWorld);

            // If the mapped cell has a floor tile, return it.
            if (floorTilemap.GetTile(candidate) != null)
            {
                return candidate;
            }
        }

        Debug.LogWarning("GetNearestWalkableCell: Nearest A* node is not on the floor tilemap. Using desired cell.");
        return desiredCell;
    }

    #endregion

    // validation 3: use internal data to check if world cell is floor
    public bool IsFloorCell(Vector3Int worldCell)
    {
        if (_tiles == null) return false;
        
        int localX = worldCell.x - originCell.x;
        int localY = worldCell.y - originCell.y;
        
        if (localX < 0 || localX >= width || localY < 0 || localY >= height)
            return false;
        
        // 1 = floor, 2 = closed door, 3 = open door (all walkable)
        int tileType = _tiles[localX, localY];
        return tileType == 1 || tileType == 2 || tileType == 3;
    }

    public Vector3Int? GetNearestFloorCell(Vector3Int worldCell, int maxRadius = 10)
    {
        if (_tiles == null) return null;
        
        // Check center first
        if (IsFloorCell(worldCell))
            return worldCell;
        
        // Spiral search outward
        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    // Only check outer edge of current radius
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                        continue;
                    
                    Vector3Int candidate = new Vector3Int(
                        worldCell.x + dx,
                        worldCell.y + dy,
                        worldCell.z
                    );
                    
                    if (IsFloorCell(candidate))
                        return candidate;
                }
            }
        }
        
        return null;
    }
}