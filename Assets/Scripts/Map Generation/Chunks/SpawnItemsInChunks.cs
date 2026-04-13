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

        // Calculate which chunks should be loaded based on player position and render distance
        int chunkSize = chunkManager.chunkSize;
        Vector2Int playerChunk = new Vector2Int(
            Mathf.FloorToInt(chunkManager.player.position.x / chunkSize),
            Mathf.FloorToInt(chunkManager.player.position.y / chunkSize)
        );

        var chunksToKeep = new HashSet<Vector2Int>();

        // Determine which chunks are within render distance
        for (int dx = -chunkManager.renderDistance; dx <= chunkManager.renderDistance; dx++)
        {
            for (int dy = -chunkManager.renderDistance; dy <= chunkManager.renderDistance; dy++)
            {
                var chunkCoord = new Vector2Int(playerChunk.x + dx, playerChunk.y + dy);
                chunksToKeep.Add(chunkCoord);

                // Populate chunk if it exists and hasn't been populated yet
                if (chunkManager.records.ContainsKey(chunkCoord) && !chunksWithActiveItems.Contains(chunkCoord))
                {
                    PopulateChunk(chunkCoord, chunkManager.records[chunkCoord]);
                }
            }
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

        // Remove from active chunks set
        foreach (var chunkCoord in chunksToRemove)
        {
            chunksWithActiveItems.Remove(chunkCoord);
        }
    }

    public void PopulateChunk(Vector2Int chunkCoord, ChunkRecord record)
    {
        if (itemsToSpawn == null || itemsToSpawn.Length == 0)
        {
            Debug.LogWarning($"Cannot populate chunk {chunkCoord}: No items to spawn!");
            return;
        }

        if (floorTilemap == null)
        {
            Debug.LogError($"Cannot populate chunk {chunkCoord}: Floor tilemap is not assigned!");
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
        Random.InitState(record.seed ^ 12345); // XOR with constant to differentiate from maze generation

        List<Vector3> spawnedPositions = new List<Vector3>();
        List<GameObject> spawnedItems = new List<GameObject>();
        int spawnAttempts = 0;
        int maxAttempts = itemsPerChunk * 20; // Increased attempts to allow for more retries

        // Try to spawn the desired number of items
        for (int i = 0; i < itemsPerChunk && spawnAttempts < maxAttempts; spawnAttempts++)
        {
            // Generate random position within chunk bounds
            float randomX = Random.Range(chunkMin.x + 1f, chunkMax.x - 1f);
            float randomY = Random.Range(chunkMin.y + 1f, chunkMax.y - 1f);
            Vector3 spawnPosition = new Vector3(randomX, randomY, 0f);

            // Create unique item ID based on chunk coordinate and item index
            string itemId = $"Item_{chunkCoord.x}_{chunkCoord.y}_{i}";

            // Check if position is valid
            if (IsValidSpawnPosition(spawnPosition, spawnedPositions))
            {
                // Only spawn if this item hasn't been collected before
                if (!collectedItems.Contains(itemId))
                {
                    // Spawn random item
                    int randomIndex = Random.Range(0, itemsToSpawn.Length);
                    GameObject spawnedItem = Instantiate(itemsToSpawn[randomIndex], spawnPosition, Quaternion.identity);
                    spawnedItem.transform.SetParent(transform);
                    spawnedItem.name = itemId;

                    // Add a component to track when this item is collected
                    var tracker = spawnedItem.AddComponent<ItemCollectionTracker>();
                    tracker.Initialize(this, itemId);

                    spawnedPositions.Add(spawnPosition);
                    spawnedItems.Add(spawnedItem);
                }
                i++; // Increment even if item was already collected to maintain spawn pattern
            }
        }

        // Store spawned items for this chunk
        spawnedItemsByChunk[chunkCoord] = spawnedItems;

        // Restore random state
        Random.state = oldState;

        Debug.Log($"Populated chunk {chunkCoord} with {spawnedItems.Count} items ({collectedItems.Count} already collected, attempted {spawnAttempts} spawns)");
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
                    Destroy(item);
                    destroyedCount++;
                }
            }
            spawnedItemsByChunk.Remove(chunkCoord);
            Debug.Log($"Unloaded {destroyedCount} items from chunk {chunkCoord}");
        }
    }

    private bool IsValidSpawnPosition(Vector3 position, List<Vector3> existingPositions)
    {
        // Convert world position to tilemap cell position
        Vector3Int cellPosition = floorTilemap.WorldToCell(position);

        // Check if there is a floor tile at this position
        if (!floorTilemap.HasTile(cellPosition))
        {
            return false;
        }

        // Check for collisions with walls and other objects
        Collider2D hitCollider = Physics2D.OverlapCircle(position, spawnCheckRadius, obstacleLayers);
        if (hitCollider != null)
        {
            return false;
        }

        // Check minimum distance from already spawned items
        foreach (Vector3 existingPos in existingPositions)
        {
            if (Vector3.Distance(position, existingPos) < minDistanceBetweenItems)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Called by ItemCollectionTracker when an item is collected
    /// </summary>
    public void OnItemCollected(string itemId)
    {
        collectedItems.Add(itemId);
        Debug.Log($"Item collected: {itemId}. Total collected: {collectedItems.Count}");
    }

    public void ClearPopulatedChunks()
    {
        // Destroy all spawned items
        foreach (var kvp in spawnedItemsByChunk)
        {
            foreach (var item in kvp.Value)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
        }

        spawnedItemsByChunk.Clear();
        chunksWithActiveItems.Clear();
        Debug.Log("SpawnItemsInChunks: Cleared all populated chunks and items");
    }

    /// <summary>
    /// Clear collected items tracking (useful for game restart or new session)
    /// </summary>
    public void ResetCollectedItems()
    {
        collectedItems.Clear();
        Debug.Log("SpawnItemsInChunks: Reset collected items tracking");
    }
}

/// <summary>
/// Helper component to track when an item is collected/destroyed
/// </summary>
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
        // When this item is destroyed (collected by player), notify the spawner
        // Only notify if the spawner still exists and this wasn't destroyed due to chunk unloading
        if (spawner != null && !string.IsNullOrEmpty(itemId))
        {
            spawner.OnItemCollected(itemId);
        }
    }
}
