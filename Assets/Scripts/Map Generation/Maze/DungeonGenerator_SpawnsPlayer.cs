using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// Dungeon generator for rooms+mazes, based on https://journal.stuffwithstuff.com/2014/12/21/rooms-and-mazes/

[ExecuteInEditMode]
public class DungeonGenerator_SpawnsPlayer : MonoBehaviour
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
    public int width = 41;   // must be odd
    public int height = 31;  // must be odd
    public Vector3Int originCell = Vector3Int.zero; // bottom-left cell for the dungeon

    [Header("Dungeon parameters")]
    public int numRoomTries = 50;
    public int roomExtraSize = 0; // allow bigger rooms
    [Range(0, 100)]
    public int windingPercent = 0;
    public int extraConnectorChance = 20; // 1-in-X chance to force extra connector

    [Header("Player Spawn")]
    public GameObject playerPrefab; // assign player prefab to spawn
    public bool spawnPlayerAtFirstRoom = true; // toggle to enable/disable player spawning

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
            SpawnPlayerAtFirstRoom();
        }
    }

    [ContextMenu("Generate Dungeon")]
    public void Generate()
    {
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
        RemoveDeadEnds();
        PaintToTilemaps();
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

        // Clear tilemaps in the area
        var bounds = new BoundsInt(originCell, new Vector3Int(width, height, 1));
        wallTilemap.ClearAllTiles();
        floorTilemap.ClearAllTiles();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var cell = new Vector3Int(originCell.x + x, originCell.y + y, originCell.z);
                int t = _tiles[x, y];
                if (t == 0)
                {
                    wallTilemap.SetTile(cell, wallTile);
                }
                else if (t == 1)
                {
                    floorTilemap.SetTile(cell, floorTile);
                }
                else if (t == 2)
                {
                    floorTilemap.SetTile(cell, floorTile);
                    wallTilemap.SetTile(cell, closedDoorTile ?? wallTile); // closed door displayed on wall layer if provided
                }
                else if (t == 3)
                {
                    floorTilemap.SetTile(cell, floorTile);
                    wallTilemap.SetTile(cell, openDoorTile ?? floorTile);
                }
            }
        }

        wallTilemap.RefreshAllTiles();
        floorTilemap.RefreshAllTiles();
    }

    #endregion

    #region Player Spawning

    private void SpawnPlayerAtFirstRoom()
    {
        if (!spawnPlayerAtFirstRoom || _rooms.Count == 0)
        {
            return;
        }

        // Get the first room
        RectInt firstRoom = _rooms[0];

        // Calculate the center of the first room in world coordinates
        Vector3 roomCenterLocal = new Vector3(
            firstRoom.center.x,
            firstRoom.center.y,
            originCell.z
        );

        // Convert from local grid coordinates to world coordinates
        Vector3 playerPosition = originCell + roomCenterLocal;

        // If a player instance already exists in the scene, reposition it
        GameObject playerInstance = GameObject.FindGameObjectWithTag("Player");
        if (playerInstance != null)
        {
            playerInstance.transform.position = playerPosition;
            Debug.Log($"Player repositioned to first room at: {playerPosition}");
        }
        else if (playerPrefab != null)
        {
            // Spawn a new player if none exists
            playerInstance = Instantiate(playerPrefab, playerPosition, Quaternion.identity);
            Debug.Log($"Player spawned at first room: {playerPosition}");
        }
    }

    #endregion

    #region Helpers for debug & deterministic seed

    /// <summary>Optional: call to set deterministic RNG</summary>
    public void SetSeed(int seed)
    {
        _rng = new System.Random(seed);
    }

    #endregion
}