using UnityEngine;

public class PlayerPlaceBomb : MonoBehaviour
{
    public BombCounter bombCounter;
    public GameObject bombPrefab;
    public Transform bombSpawnPoint;

    void Start()
    {
        if (bombCounter == null)
        {
            Debug.LogError("Bomb Counter reference is missing! Attempting to auto-locate...");
            bombCounter = FindObjectOfType<BombCounter>();
            if (bombCounter == null)
            {
                Debug.LogError("Failed to auto-locate Bomb Counter!");
            }
        }
        if (bombPrefab == null)
        {
            Debug.LogError("Bomb Prefab reference is missing!");
        }
        if (bombSpawnPoint == null)
        {
            Debug.LogError("Bomb Spawn Point reference is missing!");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            PlaceBomb();
        }
    }

    void PlaceBomb()
    {
        if (bombPrefab != null && bombSpawnPoint != null && bombCounter != null)
        {
            if (bombCounter.bombCount > 0)
            {
                // Check if bombSpawnPoint will collide with any obstacles before placing the bomb
                Collider2D hitCollider = Physics2D.OverlapCircle(bombSpawnPoint.position, 0.1f, LayerMask.GetMask("Walls"));
                if (hitCollider != null)
                {
                    Debug.LogWarning("Cannot place bomb here! Obstacle detected.");
                    return;
                }

                Instantiate(bombPrefab, bombSpawnPoint.position, Quaternion.identity);
                Debug.Log("Bomb placed!");
                bombCounter.SubtractBomb();
                FirebaseAIManager.Instance?.UpdatePlayerLog("Deployed explosive device");
            }
            else
            {
                Debug.Log("No bombs left to place!");
                FirebaseAIManager.Instance?.UpdatePlayerLog("Attempted explosive deployment - inventory depleted");
            }
        }
        else
        {
            Debug.LogWarning("Bomb prefab, spawn point, or bomb counter not assigned.");
        }
    }
}
