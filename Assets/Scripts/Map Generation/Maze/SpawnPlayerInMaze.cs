using UnityEngine;
using System.Collections;
using Pathfinding;

[ExecuteInEditMode]
public class SpawnPlayerInMaze : MonoBehaviour
{
    public DungeonGenerator_Seeded_Chunks dungeonGenerator;
    public GameObject playerPrefab;
    public bool spawnPlayerAtFirstRoom = true;
    [Tooltip("Maximum distance to search for a floor tile when validating spawn position")]
    public int maxSearchRadius = 10;

    private static bool _playerSpawnedBySpawner = false;

    private void Awake()
    {
        if (dungeonGenerator == null)
        {
            dungeonGenerator = GetComponent<DungeonGenerator_Seeded_Chunks>();
        }
    }

    public void TrySpawnOrRepositionPlayer()
    {
        Debug.Log($"[SpawnPlayerInMaze] TrySpawnOrRepositionPlayer called. spawnPlayerAtFirstRoom={spawnPlayerAtFirstRoom}, _playerSpawnedBySpawner={_playerSpawnedBySpawner}");
        
        if (!spawnPlayerAtFirstRoom)
        {
            Debug.LogWarning("[SpawnPlayerInMaze] Spawn disabled via spawnPlayerAtFirstRoom flag.");
            return;
        }
        
        if (_playerSpawnedBySpawner)
        {
            Debug.LogWarning("[SpawnPlayerInMaze] Player already spawned by another spawner. Skipping.");
            return;
        }

        // Wait one frame to ensure tilemaps are rendered
        StartCoroutine(SpawnPlayerNextFrame());
    }

    private IEnumerator SpawnPlayerNextFrame()
    {
        yield return null; // Wait one frame
        PerformSpawn();
    }

    private void PerformSpawn()
    {
        if (dungeonGenerator == null)
        {
            dungeonGenerator = GetComponent<DungeonGenerator_Seeded_Chunks>();
        }

        if (dungeonGenerator == null)
        {
            Debug.LogWarning("SpawnPlayerInMaze: dungeonGenerator reference not assigned.");
            return;
        }

        // Determine initial spawn position
        Vector3 initialPosition;
        
        if (PlayerSessionData.Instance != null)
        {
            Vector3 sessionPos = new Vector3(
                PlayerSessionData.Instance.positionX,
                PlayerSessionData.Instance.positionY,
                PlayerSessionData.Instance.positionZ
            );
            
            if (sessionPos != Vector3.zero)
            {
                initialPosition = sessionPos;
                Debug.Log($"[SpawnPlayerInMaze] Found saved position: {sessionPos}");
            }
            else
            {
                Debug.Log("[SpawnPlayerInMaze] Using first room.");
                initialPosition = GetFirstRoomPosition();
            }
        }
        else
        {
            Debug.Log("[SpawnPlayerInMaze] No session data - using first room.");
            initialPosition = GetFirstRoomPosition();
        }

        // Find nearest floor tile using generator's internal data
        Vector3? validSpawnPosition = FindNearestFloorTile(initialPosition);
        
        if (!validSpawnPosition.HasValue)
        {
            Debug.LogError($"[SpawnPlayerInMaze] Could not find floor tile. Aborting spawn.");
            return;
        }

        Vector3 playerPosition = validSpawnPosition.Value;

        // FINAL VALIDATION before spawning
        Vector3Int finalCell = new Vector3Int(
            Mathf.FloorToInt(playerPosition.x),
            Mathf.FloorToInt(playerPosition.y),
            Mathf.FloorToInt(playerPosition.z)
        );

        if (!dungeonGenerator.IsFloorCell(finalCell))
        {
            Debug.LogError($"[SpawnPlayerInMaze] CRITICAL: Final position {finalCell} is NOT a floor cell!");
            return;
        }

        Debug.Log($"[SpawnPlayerInMaze] ✅ Final validated spawn position: {playerPosition}");

        // Spawn or reposition player
        GameObject playerInstance = GameObject.FindGameObjectWithTag("Player");
        if (playerInstance != null)
        {
            PlacePlayer(playerInstance, playerPosition);
            return;
        }

        if (playerPrefab != null)
        {
            playerInstance = Instantiate(playerPrefab, playerPosition, Quaternion.identity);
            PlacePlayer(playerInstance, playerPosition);
        }
        else
        {
            Debug.LogWarning("SpawnPlayerInMaze: No player instance found and no playerPrefab assigned.");
        }
    }

    private void PlacePlayer(GameObject playerInstance, Vector3 position)
    {
        playerInstance.transform.position = position;
        _playerSpawnedBySpawner = true;

        var rb = playerInstance.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = new Vector2(position.x, position.y);
            rb.linearVelocity = Vector2.zero;
        }

        var pc = playerInstance.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.OnPlacedByGenerator(position);
        }

        if (ChunkManager.Instance != null)
        {
            ChunkManager.Instance.RegisterPlayerTransform(playerInstance.transform);
        }

        RescanAStarGrid();
        
        Debug.Log($"[SpawnPlayerInMaze] ✅ Player placed at {position}");
    }

    private Vector3 GetFirstRoomPosition()
    {
        if (!dungeonGenerator.TryGetFirstRoomSpawnCell(out Vector3Int spawnCell))
        {
            Debug.LogWarning("[SpawnPlayerInMaze] No rooms available. Using chunk origin.");
            return new Vector3(
                dungeonGenerator.originCell.x + 2.5f,
                dungeonGenerator.originCell.y + 2.5f,
                dungeonGenerator.originCell.z
            );
        }

        // Verify using internal data
        if (!dungeonGenerator.IsFloorCell(spawnCell))
        {
            Debug.LogWarning($"[SpawnPlayerInMaze] First room cell {spawnCell} is not floor! Searching...");
            Vector3Int? nearestFloor = dungeonGenerator.GetNearestFloorCell(spawnCell, 5);
            if (nearestFloor.HasValue)
            {
                spawnCell = nearestFloor.Value;
            }
        }

        return new Vector3(spawnCell.x + 0.5f, spawnCell.y + 0.5f, spawnCell.z);
    }

    private Vector3? FindNearestFloorTile(Vector3 worldPosition)
    {
        Vector3Int centerCell = new Vector3Int(
            Mathf.FloorToInt(worldPosition.x),
            Mathf.FloorToInt(worldPosition.y),
            Mathf.FloorToInt(worldPosition.z)
        );

        Debug.Log($"[SpawnPlayerInMaze] Searching for floor tile from {centerCell}...");

        Vector3Int? floorCell = dungeonGenerator.GetNearestFloorCell(centerCell, maxSearchRadius);
        
        if (floorCell.HasValue)
        {
            Vector3 foundPosition = new Vector3(
                floorCell.Value.x + 0.5f,
                floorCell.Value.y + 0.5f,
                floorCell.Value.z
            );
            Debug.Log($"[SpawnPlayerInMaze] Found floor cell at {floorCell.Value}");
            return foundPosition;
        }

        Debug.LogError($"[SpawnPlayerInMaze] No floor cell found within {maxSearchRadius} cells");
        return null;
    }

    private void RescanAStarGrid()
    {
        if (AstarPath.active == null) return;
        
        Debug.Log("[SpawnPlayerInMaze] Rescanning A* grid...");
        AstarPath.active.Scan();
    }

    public static void ResetSpawnFlag()
    {
        Debug.Log("[SpawnPlayerInMaze] ResetSpawnFlag called");
        _playerSpawnedBySpawner = false;
    }
}