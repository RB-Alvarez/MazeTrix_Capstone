using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using Pathfinding;

public class ExplosionDamage : MonoBehaviour
{
    public Tilemap wallMap;      // Attached to "Walls" Layer
    public Tilemap floorMap;  // Attached to "Floor" Layer
    public TileBase floorTile;   // The tile to place after destruction

    void Start()
    {
        if (wallMap == null)
        {
            wallMap = GameObject.Find("Wall").GetComponent<Tilemap>();
        }
        if (floorMap == null)
        {
            floorMap = GameObject.Find("Floor").GetComponent<Tilemap>();
        }
        if (floorTile == null)
        {
            Debug.LogError("Floor tile reference is missing on ExplosionDamage.");
        }

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("Player hit by explosion!");
            other.GetComponent<PlayerCollisions>()?.OnGotHit.Invoke(); // Trigger player hit event
        }

        if (other.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Enemy hit by explosion!");
            other.GetComponent<EnemyCombat>()?.TakeDamage(30); // Apply damage to enemy
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            // Check tiles within an area around explosion
            Vector3Int centerCell = wallMap.WorldToCell(transform.position);

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector3Int targetCell = new Vector3Int(centerCell.x + x, centerCell.y + y, 0);

                    // Only try to destroy if there is actually a tile there
                    if (wallMap.HasTile(targetCell))
                    {
                        TryDestroyTileAtCell(targetCell);
                    }
                }
            }

        }
    }

    public void TryDestroyTileAtCell(Vector3Int cellPos)
    {
        if (wallMap == null || floorMap == null)
        {
            Debug.LogWarning("Tilemaps not assigned on ExplosionDamage.");
            return;
        }

        // Get locations of adjacent tiles 
        Vector3Int[] neighbors = {
        new Vector3Int(cellPos.x + 1, cellPos.y, 0),
        new Vector3Int(cellPos.x - 1, cellPos.y, 0),
        new Vector3Int(cellPos.x, cellPos.y + 1, 0),
        new Vector3Int(cellPos.x, cellPos.y - 1, 0)
        };

        bool isExposed = false;

        foreach (var pos in neighbors)
        {
            // If any side is null (empty space), it's an exposed edge, do not destroy, prevents players from walking into the 2d skybox
            if (!wallMap.HasTile(pos) && !floorMap.HasTile(pos))
            {
                isExposed = true;
                Debug.Log("Tile at " + cellPos + " is exposed, skipping destruction.");
                break;
            }
        }

        // Only destroy if it's NOT exposed (completely surrounded)
        if (!isExposed)
        {
            Debug.Log("Destroying tile at " + cellPos);

            wallMap.SetTile(cellPos, null);

            // Place floor tile after destruction
            floorMap.SetTile(cellPos, floorTile);

        }

    }
}
