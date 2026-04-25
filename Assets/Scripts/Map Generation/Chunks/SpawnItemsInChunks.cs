using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class SpawnItemsInChunks : MonoBehaviour
{
    [Header("Item Spawning")]
    [Tooltip("Array of item prefabs that can spawn in chunks")]
    public GameObject[] itemsToSpawn;

    [Tooltip("Number of items to spawn per chunk")]
    public int itemsPerChunk = 5;

    [Tooltip("Minimum distance between spawned items")]
    public float minDistanceBetweenItems = 2f;

    [Header("Spawn Validation")]
    [Tooltip("Radius to check for collisions when spawning items")]
    public float spawnCheckRadius = 0.5f;

    [Tooltip("Layers to check for collisions (walls, other items, etc.)")]
    public LayerMask obstacleLayers;

    [Header("References")]
    [Tooltip("Reference to ChunkManager (auto-assigned if null)")]
    public ChunkManager chunkManager;

    [Tooltip("Reference to floor tilemap (auto-assigned if null)")]
    public Tilemap floorTilemap;

    // Track which chunks have spawned items currently active
    private HashSet<Vector2Int> chunksWithActiveItems = new HashSet<Vector2Int>();

    // Track spawned items per chunk for cleanup when chunks unload
    private Dictionary<Vector2Int, List<GameObject>> spawnedItemsByChunk = new Dictionary<Vector2Int, List<GameObject>>();

    // Track which items have been collected by the player (persistent across chunk loads/unloads)
    private HashSet<string> collectedItems = new HashSet<string>();
    
    // Track items currently being unloaded to prevent false collection notifications
    private HashSet<string> unloadingItems = new HashSet<string>();

    // Track chunks that need delayed population (tiles not ready yet)
    private Dictionary<Vector2Int, int> pendingChunks = new Dictionary<Vector2Int, int>();
    private const int MAX_PENDING_FRAMES = 10;

    void Start()
    {
        if (chunkManager == null)
        {
            chunkManager = ChunkManager.Instance;
        }

        if (chunkManager == null)
        {
            Debug.LogError("SpawnItemsInChunks: ChunkManager not found!");
            return;
        }

        if (itemsToSpawn == null || itemsToSpawn.Length == 0)
        {
            Debug.LogWarning("SpawnItemsInChunks: No items assigned to spawn!");
        }

        // Set default obstacle layers if not set
        if (obstacleLayers == 0)
        {
            obstacleLayers = LayerMask.GetMask("Walls", "Items");
        }

        // Try to find floor tilemap if not assigned
        if (floorTilemap == null)
        {
            GameObject floorObj = GameObject.Find("Floor");
            if (floorObj != null)
            {
                floorTilemap = floorObj.GetComponent<Tilemap>();
            }

            if (floorTilemap == null)
            {
                Debug.LogError("SpawnItemsInChunks: Floor tilemap not found! Please assign it in the inspector.");
            }
        }

    }

    void Update()
    {
        if (chunkManager == null || chunkManager.player == null) return;

        // Re-acquire floor tilemap if lost (can happen if generators replace it)
        if (floorTilemap == null)
        {
            GameObject floorObj = GameObject.Find("Floor");
            if (floorObj != null)
            {
                floorTilemap = floorObj.GetComponent<Tilemap>();
                if (floorTilemap != null)
                {
                    Debug.Log("[SPAWN] Re-acquired floor tilemap!");
                }
            }
            if (floorTilemap == null) return;
        }

        int chunkSize = chunkManager.chunkSize;
        Vector2Int playerChunk = new Vector2Int(
            Mathf.FloorToInt(chunkManager.player.position.x / chunkSize),
            Mathf.FloorToInt(chunkManager.player.position.y / chunkSize)
        );

        var chunksToKeep = new HashSet<Vector2Int>();

        for (int dx = -chunkManager.renderDistance; dx <= chunkManager.renderDistance; dx++)
        {
            for (int dy = -chunkManager.renderDistance; dy <= chunkManager.renderDistance; dy++)
            {
                var chunkCoord = new Vector2Int(playerChunk.x + dx, playerChunk.y + dy);
                chunksToKeep.Add(chunkCoord);

                if (chunkManager.records.ContainsKey(chunkCoord) && !chunksWithActiveItems.Contains(chunkCoord) && !pendingChunks.ContainsKey(chunkCoord))
                {
                    // Check if floor tiles are ready for this chunk
                    if (ChunkHasFloorTiles(chunkCoord, chunkSize))
                    {
                        PopulateChunk(chunkCoord, chunkManager.records[chunkCoord]);
                    }
                    else
                    {
                        // Tiles not ready yet, queue for retry
                        pendingChunks[chunkCoord] = 0;
                        Debug.Log($"[SPAWN] Chunk {chunkCoord} has no floor tiles yet, queuing for retry...");
                    }
                }
            }
        }

        // Retry pending chunks
        var pendingToRemove = new List<Vector2Int>();
        var pendingToPopulate = new List<Vector2Int>();
        foreach (var kvp in pendingChunks)
        {
            var chunkCoord = kvp.Key;
            int framesWaited = kvp.Value;

            if (!chunksToKeep.Contains(chunkCoord))
            {
                pendingToRemove.Add(chunkCoord);
                continue;
            }

            if (chunkManager.records.ContainsKey(chunkCoord) && ChunkHasFloorTiles(chunkCoord, chunkSize))
            {
                pendingToPopulate.Add(chunkCoord);
            }
            else if (framesWaited >= MAX_PENDING_FRAMES)
            {
                Debug.LogWarning($"[SPAWN] Chunk {chunkCoord} still has no floor tiles after {MAX_PENDING_FRAMES} frames. Giving up.");
                pendingToRemove.Add(chunkCoord);
            }
        }

        foreach (var coord in pendingToPopulate)
        {
            Debug.Log($"[SPAWN] Chunk {coord} floor tiles ready after {pendingChunks[coord]} frames, populating now.");
            pendingChunks.Remove(coord);
            PopulateChunk(coord, chunkManager.records[coord]);
        }

        foreach (var coord in pendingToRemove)
        {
            pendingChunks.Remove(coord);
        }

        // Increment wait counters
        var keys = new List<Vector2Int>(pendingChunks.Keys);
        foreach (var key in keys)
        {
            pendingChunks[key]++;
        }

        // Cleanup items from chunks that are no longer loaded
        var chunksToRemove = new List<Vector2Int>();
        foreach (var chunkCoord in chunksWithActiveItems)
        {
            if (!chunksToKeep.Contains(chunkCoord))
            {
                UnloadChunkItems(chunkCoord);
                chunksToRemove.Add(chunkCoord);
            }
        }

        foreach (var chunkCoord in chunksToRemove)
        {
            chunksWithActiveItems.Remove(chunkCoord);
        }
    }

    private bool ChunkHasFloorTiles(Vector2Int chunkCoord, int chunkSize)
    {
        if (floorTilemap == null) return false;

        // Sample a few positions in the chunk to see if floor tiles exist
        Vector3Int center = new Vector3Int(
            chunkCoord.x * chunkSize + chunkSize / 2,
            chunkCoord.y * chunkSize + chunkSize / 2,
            0
        );

        // Check center and a few other spots
        if (floorTilemap.HasTile(center)) return true;

        // Check corners offset inward
        for (int dx = -chunkSize / 4; dx <= chunkSize / 4; dx += chunkSize / 4)
        {
            for (int dy = -chunkSize / 4; dy <= chunkSize / 4; dy += chunkSize / 4)
            {
                if (floorTilemap.HasTile(new Vector3Int(center.x + dx, center.y + dy, 0)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void PopulateChunk(Vector2Int chunkCoord, ChunkRecord record)
    {
        if (itemsToSpawn == null || itemsToSpawn.Length == 0)
        {
            Debug.LogWarning($"[SPAWN] Cannot populate chunk {chunkCoord}: No items to spawn!");
            return;
        }

        if (floorTilemap == null)
        {
            Debug.LogError($"[SPAWN] Cannot populate chunk {chunkCoord}: Floor tilemap is not assigned!");
            return;
        }

        // Mark chunk as having active items
        chunksWithActiveItems.Add(chunkCoord);

        // Calculate chunk bounds
        int chunkSize = chunkManager.chunkSize;
        Vector2 chunkMin = new Vector2(chunkCoord.x * chunkSize, chunkCoord.y * chunkSize);
        Vector2 chunkMax = new Vector2(chunkMin.x + chunkSize, chunkMin.y + chunkSize);

        // Use chunk seed for consistent random placement
        Random.State oldState = Random.state;
        Random.InitState(record.seed ^ 12345);

        List<Vector3> spawnedPositions = new List<Vector3>();
        List<GameObject> spawnedItems = new List<GameObject>();
        int spawnAttempts = 0;
        int maxAttempts = itemsPerChunk * 20;
        
        // Debug counters for spawn failures
        int failNoFloor = 0;
        int failCollision = 0;
        int failDistance = 0;

        for (int i = 0; i < itemsPerChunk && spawnAttempts < maxAttempts; spawnAttempts++)
        {
            float randomX = Random.Range(chunkMin.x + 1f, chunkMax.x - 1f);
            float randomY = Random.Range(chunkMin.y + 1f, chunkMax.y - 1f);
            Vector3 spawnPosition = new Vector3(randomX, randomY, 0f);

            string itemId = $"Item_{chunkCoord.x}_{chunkCoord.y}_{i}";

            // Inline validation with failure tracking
            Vector3Int cellPosition = floorTilemap.WorldToCell(spawnPosition);
            
            if (!floorTilemap.HasTile(cellPosition))
            {
                failNoFloor++;
                continue;
            }

            Collider2D hitCollider = Physics2D.OverlapCircle(spawnPosition, spawnCheckRadius, obstacleLayers);
            if (hitCollider != null)
            {
                failCollision++;
                continue;
            }

            bool tooClose = false;
            foreach (Vector3 existingPos in spawnedPositions)
            {
                if (Vector3.Distance(spawnPosition, existingPos) < minDistanceBetweenItems)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose)
            {
                failDistance++;
                continue;
            }

            // Valid position found - spawn item
            int randomIndex = Random.Range(0, itemsToSpawn.Length);
            GameObject spawnedItem = Instantiate(itemsToSpawn[randomIndex], spawnPosition, Quaternion.identity);
            spawnedItem.transform.SetParent(transform);
            spawnedItem.name = itemId;

            var tracker = spawnedItem.AddComponent<ItemCollectionTracker>();
            tracker.Initialize(this, itemId);

            spawnedPositions.Add(spawnPosition);
            spawnedItems.Add(spawnedItem);
            
            i++;
        }

        spawnedItemsByChunk[chunkCoord] = spawnedItems;

        Random.state = oldState;

    }

    private void UnloadChunkItems(Vector2Int chunkCoord)
    {
        if (spawnedItemsByChunk.TryGetValue(chunkCoord, out List<GameObject> items))
        {
            int destroyedCount = 0;
            
            foreach (var item in items)
            {
                if (item != null)
                {
                    unloadingItems.Add(item.name);
                    Destroy(item);
                    destroyedCount++;
                }
            }
            
            spawnedItemsByChunk.Remove(chunkCoord);
        }
    }

    public void OnItemCollected(string itemId)
    {
        if (!unloadingItems.Contains(itemId))
        {
            collectedItems.Add(itemId);
            Debug.Log($"[COLLECTED] Item collected: {itemId}. Total collected: {collectedItems.Count}");
        }
        else
        {
            unloadingItems.Remove(itemId);
        }
    }

    public void ClearPopulatedChunks()
    {
        foreach (var kvp in spawnedItemsByChunk)
        {
            foreach (var item in kvp.Value)
            {
                if (item != null)
                {
                    unloadingItems.Add(item.name);
                    Destroy(item);
                }
            }
        }

        spawnedItemsByChunk.Clear();
        chunksWithActiveItems.Clear();
        pendingChunks.Clear();
    }

    public void ResetCollectedItems()
    {
        collectedItems.Clear();
        unloadingItems.Clear();
    }
}

public class ItemCollectionTracker : MonoBehaviour
{
    private SpawnItemsInChunks spawner;
    private string itemId;

    public void Initialize(SpawnItemsInChunks spawner, string itemId)
    {
        this.spawner = spawner;
        this.itemId = itemId;
    }

    private void OnDestroy()
    {
        if (spawner != null && !string.IsNullOrEmpty(itemId))
        {
            spawner.OnItemCollected(itemId);
        }
    }
}
