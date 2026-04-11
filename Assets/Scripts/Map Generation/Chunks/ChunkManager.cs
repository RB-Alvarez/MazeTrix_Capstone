using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Prefab that contains a DungeonGenerator_Seeded_Chunks component and is preconfigured (tilemaps, tiles, etc).")]
    public GameObject mazeGeneratorPrefab;

    [Tooltip("Default chunk definition used to configure newly spawned generators (tiles, sizes, player spawn, etc).")]
    public ChunkDefinition defaultChunkDefinition;

    [Tooltip("Player transform used to determine which chunks to keep loaded.")]
    public Transform player;

    [Header("Chunking")]
    [Tooltip("World size of a chunk in cells / units. Used to compute chunk coordinates from world position.")]
    public int chunkSize = 33;

    [Tooltip("How many chunks in each direction (radius) to keep loaded around the player.")]
    public int renderDistance = 2;

    // Active instantiated chunk gameobjects keyed by chunk coord
    private Dictionary<Vector2Int, GameObject> activeChunks = new Dictionary<Vector2Int, GameObject>();

    // Cache last center coord so we only update when player crosses a chunk boundary
    private Vector2Int _lastCenterCoord = new Vector2Int(int.MinValue, int.MinValue);

    void Start()
    {
        if (mazeGeneratorPrefab == null)
        {
            Debug.LogError("ChunkManager: mazeGeneratorPrefab is not assigned.");
        }

        if (defaultChunkDefinition == null)
        {
            Debug.LogWarning("ChunkManager: defaultChunkDefinition is not assigned. Generators will keep prefab defaults.");
        }

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (player == null || mazeGeneratorPrefab == null) return;

        // World is 2D: use X and Y axes for chunk coord calculation
        Vector2Int currentChunkCoord = new Vector2Int(
            Mathf.FloorToInt(player.position.x / chunkSize),
            Mathf.FloorToInt(player.position.y / chunkSize)
        );

        // Only update chunks when player enters a new chunk (prevents thrash)
        if (currentChunkCoord != _lastCenterCoord)
        {
            _lastCenterCoord = currentChunkCoord;
            UpdateChunks(currentChunkCoord);
        }
    }

    private void UpdateChunks(Vector2Int centerCoord)
    {
        var required = new HashSet<Vector2Int>();

        for (int dx = -renderDistance; dx <= renderDistance; dx++)
        {
            for (int dy = -renderDistance; dy <= renderDistance; dy++)
            {
                var coord = new Vector2Int(centerCoord.x + dx, centerCoord.y + dy);
                required.Add(coord);
                if (!activeChunks.ContainsKey(coord))
                {
                    // Try to load a saved chunk record
                    var filename = GetRecordFilename(coord);
                    var record = ChunkDefinition.LoadRecordFromFile(filename);
                    if (record != null)
                    {
                        SpawnChunkFromRecord(coord, record, filename);
                    }
                    else
                    {
                        // No saved record, create new chunk, generate and persist its record
                        SpawnNewChunk(coord, filename);
                    }
                }
            }
        }

        // Unload chunks that are no longer required
        var toRemove = new List<Vector2Int>();
        foreach (var kv in activeChunks)
        {
            if (!required.Contains(kv.Key))
            {
                toRemove.Add(kv.Key);
            }
        }

        foreach (var coord in toRemove)
        {
            UnloadChunk(coord);
        }
    }

    private string GetRecordFilename(Vector2Int coord)
    {
        return $"chunk_{coord.x}_{coord.y}.json";
    }

    private void SpawnChunkFromRecord(Vector2Int coord, ChunkRecord record, string filename)
    {
        // Instantiate generator prefab inactive so we can configure it before Start runs
        var chunk = Instantiate(mazeGeneratorPrefab, Vector3.zero, Quaternion.identity);
        chunk.name = $"Chunk_{coord.x}_{coord.y}";
        chunk.transform.SetParent(transform, true);

        // Deactivate to ensure we can set all properties before Start/Awake call generates
        chunk.SetActive(false);

        var generator = chunk.GetComponent<DungeonGenerator_Seeded_Chunks>();
        if (generator == null)
        {
            Debug.LogError("Spawned maze generator prefab is missing DungeonGenerator_Seeded_Chunks component.");
            Destroy(chunk);
            return;
        }

        // Apply default chunk definition first (if provided) so tilemaps, tiles, sizes are set
        if (defaultChunkDefinition != null)
        {
            defaultChunkDefinition.ApplyToGenerator(generator, record.originCell);
        }
        else
        {
            // Ensure origin is at least set from record
            generator.originCell = record.originCell;
        }

        // Then apply the record's seed settings
        generator.useRandomSeed = record.useRandomSeed;
        generator.seed = record.seed;
        if (!record.useRandomSeed)
        {
            generator.SetSeed(record.seed);
        }

        // Activate => generator.Start will call Generate() (generator is responsible for using origin/seed set above)
        chunk.SetActive(true);

        activeChunks[coord] = chunk;

        Debug.Log($"Loaded chunk from record: {filename} at coord {coord} origin {record.originCell} seed {record.seed} useRandomSeed {record.useRandomSeed}");
    }

    private void SpawnNewChunk(Vector2Int coord, string filename)
    {
        // Compute origin cell in grid coordinates (z = 0)
        var origin = new Vector3Int(coord.x * chunkSize, coord.y * chunkSize, 0);

        // Instantiate generator prefab inactive so we can configure it before Start runs
        var chunk = Instantiate(mazeGeneratorPrefab, Vector3.zero, Quaternion.identity);
        chunk.name = $"Chunk_{coord.x}_{coord.y}";
        chunk.transform.SetParent(transform, true);

        chunk.SetActive(false);

        var generator = chunk.GetComponent<DungeonGenerator_Seeded_Chunks>();
        if (generator == null)
        {
            Debug.LogError("Spawned maze generator prefab is missing DungeonGenerator_Seeded_Chunks component.");
            Destroy(chunk);
            return;
        }

        // Apply default chunk definition first (if provided) so tilemaps, tiles, sizes are set
        if (defaultChunkDefinition != null)
        {
            defaultChunkDefinition.ApplyToGenerator(generator, origin);
        }
        else
        {
            generator.originCell = origin;
        }

        // Generate a deterministic seed for this chunk and persist it
        // Use a stable hash based on coord and a consistent salt (not Environment.TickCount)
        int seed = (coord.x * 73856093) ^ (coord.y * 19349663);
        // Mix with an arbitrary constant to avoid trivial zero seeds
        seed = seed ^ 5;

        generator.useRandomSeed = false;
        generator.seed = seed;
        generator.SetSeed(seed);

        // Activate => generator.Start will call Generate() using the applied settings
        chunk.SetActive(true);

        // Create and save a ChunkRecord so future loads use the same seed/origin
        var record = new ChunkRecord(coord, origin, seed, false);
        ChunkDefinition.SaveRecordToFile(record, filename);

        activeChunks[coord] = chunk;

        Debug.Log($"Created new chunk {coord} origin {origin} seed {seed}. Saved record: {filename}");
    }

    /// <summary>
    /// Unloads a chunk:
    /// - clears tiles written by the generator in the shared tilemaps (if any)
    /// - refreshes tilemaps
    /// - destroys the chunk GameObject and removes it from the active registry
    /// </summary>
    private void UnloadChunk(Vector2Int coord)
    {
        if (!activeChunks.TryGetValue(coord, out var go) || go == null)
        {
            activeChunks.Remove(coord);
            return;
        }

        var gen = go.GetComponent<DungeonGenerator_Seeded_Chunks>();
        if (gen != null)
        {
            var wall = gen.wallTilemap;
            var floor = gen.floorTilemap;

            // If generator wrote to shared tilemaps, clear its area.
            // This uses the generator's originCell/width/height to determine the cell rectangle.
            if (wall != null || floor != null)
            {
                int w = Mathf.Max(0, gen.width);
                int h = Mathf.Max(0, gen.height);
                var origin = gen.originCell;

                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        var cell = new Vector3Int(origin.x + x, origin.y + y, origin.z);
                        if (wall != null) wall.SetTile(cell, null);
                        if (floor != null) floor.SetTile(cell, null);
                    }
                }

                if (wall != null) wall.RefreshAllTiles();
                if (floor != null) floor.RefreshAllTiles();
            }
        }

        // Remove from registry and destroy
        activeChunks.Remove(coord);
        Destroy(go);
        Debug.Log($"Unloaded chunk {coord}");
    }
}