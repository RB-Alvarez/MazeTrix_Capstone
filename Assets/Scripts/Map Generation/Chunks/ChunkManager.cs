using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public static ChunkManager Instance { get; private set; }

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

    [Tooltip("How many chunks in each direction (radius) to keep loaded around the player).")]
    public int renderDistance = 2;

    // Map of ChunkRecord keyed by chunk coord. This stores the seed/origin for multiple chunks.
    public Dictionary<Vector2Int, ChunkRecord> records = new Dictionary<Vector2Int, ChunkRecord>();

    // Active instantiated chunk gameobjects keyed by chunk coord
    private Dictionary<Vector2Int, GameObject> activeChunks = new Dictionary<Vector2Int, GameObject>();

    // Cache last center coord so we only update when player crosses a chunk boundary
    private Vector2Int _lastCenterCoord = new Vector2Int(int.MinValue, int.MinValue);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate ChunkManager detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (records == null) records = new Dictionary<Vector2Int, ChunkRecord>();
    }

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

        // If the manager chunkSize isn't set but the default ChunkDefinition provides a helpful runtime default, use it.
        if (chunkSize == 0 && defaultChunkDefinition != null && defaultChunkDefinition.chunkSize != 0)
        {
            chunkSize = defaultChunkDefinition.chunkSize;
            Debug.Log($"ChunkManager: Using chunkSize from defaultChunkDefinition: {chunkSize}");
        }

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        Debug.Log($"ChunkManager.Start: player={(player != null ? player.name : "null")}, chunkSize={chunkSize}, renderDistance={renderDistance}, recordsCount={records?.Count ?? 0}");

        // Reset the flag RIGHT BEFORE spawning chunks to guarantee it's cleared
        SpawnPlayerInMaze.ResetSpawnFlag();

        // Spawn initial chunks around players
        if (mazeGeneratorPrefab != null && chunkSize != 0) 
        {
            Vector2Int currentChunkCoord;
            if (player != null)
            {
                currentChunkCoord = new Vector2Int(
                    Mathf.FloorToInt(player.position.x / chunkSize),
                    Mathf.FloorToInt(player.position.y / chunkSize)
                );
            }
            else
            {
                // Try to restore last known chunk from session data if available
                var session = PlayerSessionData.Instance;
                if (session != null)
                {
                    currentChunkCoord = new Vector2Int(session.currentChunkX, session.currentChunkY);
                    Debug.Log($"ChunkManager.Start: No Player found, using PlayerSessionData chunk coord {currentChunkCoord}");
                }
                else
                {
                    currentChunkCoord = Vector2Int.zero;
                    Debug.Log("ChunkManager.Start: No Player found and no session data; using chunk coord (0,0).");
                }
            }

            // Force an initial chunk load around the computed center coord.
            _lastCenterCoord = new Vector2Int(int.MinValue, int.MinValue);
            UpdateChunks(currentChunkCoord);
            _lastCenterCoord = currentChunkCoord;
        }
    }

    void Update()
    {
        if (player == null)
        {
            if (_lastCenterCoord.x == int.MinValue && _lastCenterCoord.y == int.MinValue)
            {
                Debug.LogWarning("ChunkManager.Update: player is null. Make sure a GameObject is tagged 'Player' or assign 'player' on the ChunkManager.");
            }
            return;
        }

        if (mazeGeneratorPrefab == null) return;

        if (chunkSize == 0)
        {
            Debug.LogError("ChunkManager.Update: chunkSize is 0. Chunk coord calculation will be invalid.");
            return;
        }

        Vector2Int currentChunkCoord = new Vector2Int(
            Mathf.FloorToInt(player.position.x / chunkSize),
            Mathf.FloorToInt(player.position.y / chunkSize)
        );

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
                    if (records != null && records.TryGetValue(coord, out var rec))
                    {
                        SpawnChunkFromRecord(coord, rec);
                    }
                    else
                    {
                        SpawnNewChunk(coord);
                    }
                }
            }
        }

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

    private void SpawnChunkFromRecord(Vector2Int coord, ChunkRecord record)
    {
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

        if (defaultChunkDefinition != null)
        {
            defaultChunkDefinition.ApplyToGenerator(generator, record.originCell);
        }
        else
        {
            generator.originCell = record.originCell;
        }

        generator.useRandomSeed = record.useRandomSeed;
        generator.seed = record.seed;
        if (!record.useRandomSeed)
        {
            generator.SetSeed(record.seed);
        }

        chunk.SetActive(true);

        activeChunks[coord] = chunk;

        Debug.Log($"Loaded chunk from manager record at coord {coord} origin {record.originCell} seed {record.seed} useRandomSeed {record.useRandomSeed}");
    }

    private void SpawnNewChunk(Vector2Int coord)
    {
        var origin = new Vector3Int(coord.x * chunkSize, coord.y * chunkSize, 0);

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

        if (defaultChunkDefinition != null)
        {
            defaultChunkDefinition.ApplyToGenerator(generator, origin);
        }
        else
        {
            generator.originCell = origin;
        }

        // Generate chunk seed from world seed + coordinates
        int seed;
        var session = PlayerSessionData.Instance;
        
        if (session != null && !session.worldSeedInitialized)
        {
            // First time user - generate random world seed
            session.worldSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            session.worldSeedInitialized = true;
            
            // Save to Firestore
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.SaveWorldSeed(session.worldSeed);
            }
            
            Debug.Log($"Generated new world seed for user: {session.worldSeed}");
        }
        
        if (session != null && session.worldSeedInitialized)
        {
            // Use world seed + coordinates to generate unique chunk seed
            seed = session.worldSeed ^ (coord.x * 73856093) ^ (coord.y * 19349663);
            Debug.Log($"Chunk {coord} seed: {seed} (from worldSeed: {session.worldSeed})");
        }
        else
        {
            // Fallback: coordinate-based only
            seed = (coord.x * 73856093) ^ (coord.y * 19349663) ^ 5;
            Debug.Log($"Chunk {coord} using fallback seed: {seed}");
        }

        generator.useRandomSeed = false;
        generator.seed = seed;
        generator.SetSeed(seed);

        chunk.SetActive(true);

        var record = new ChunkRecord(coord, origin, seed, false);
        if (records == null) records = new Dictionary<Vector2Int, ChunkRecord>();
        records[coord] = record;

        activeChunks[coord] = chunk;

        // Update current chunk position
        if (session != null && player != null)
        {
            Vector2Int playerChunk = new Vector2Int(
                Mathf.FloorToInt(player.position.x / chunkSize),
                Mathf.FloorToInt(player.position.y / chunkSize)
            );
            
            if (playerChunk == coord)
            {
                session.currentChunkX = coord.x;
                session.currentChunkY = coord.y;
                
                if (AuthManager.Instance != null)
                {
                    AuthManager.Instance.SaveCurrentChunkPosition(coord.x, coord.y);
                }
            }
        }

        Debug.Log($"Created new chunk {coord} origin {origin} seed {seed}.");
    }

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

        activeChunks.Remove(coord);
        Destroy(go);
        Debug.Log($"Unloaded chunk {coord}");
    }

    // Passes player position immediately after they're spawned
    public void RegisterPlayerTransform(Transform playerTransform)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("ChunkManager.RegisterPlayerTransform: provided transform is null.");
            return;
        }

        player = playerTransform;

        if (chunkSize == 0)
        {
            Debug.LogError("ChunkManager.RegisterPlayerTransform: chunkSize is 0. Cannot compute chunk coordinates.");
            return;
        }

        // Compute current chunk and force an immediate chunk update
        var currentChunkCoord = new Vector2Int(
            Mathf.FloorToInt(player.position.x / (float)chunkSize),
            Mathf.FloorToInt(player.position.y / (float)chunkSize)
        );

        // Make sure UpdateChunks runs even if _lastCenterCoord equals current chunk
        _lastCenterCoord = new Vector2Int(int.MinValue, int.MinValue);
        UpdateChunks(currentChunkCoord);
        _lastCenterCoord = currentChunkCoord;

        Debug.Log($"ChunkManager: Registered player transform and updated chunks for coord {currentChunkCoord}");
    }
}