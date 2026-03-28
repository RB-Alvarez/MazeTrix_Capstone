using UnityEngine;
using System.Collections; // Required for IEnumerator

public class ItemSpawner : MonoBehaviour
{
    public GameObject[] itemsToSpawn;
    // The range within which items can spawn on the X and Y axes
    public Vector2 spawnRangeX = new Vector2(-11f, 12f);
    public Vector2 spawnRangeY = new Vector2(-8f, 13f);

    public float timeBetweenSpawns = 5f;
    private bool canSpawn = true;

    void Start()
    {
        // Start the spawning process in the Start method
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (canSpawn) // Infinite loop to keep spawning
        {
            yield return new WaitForSeconds(timeBetweenSpawns);
            SpawnRandomItem();
        }
    }

    void SpawnRandomItem()
    {
        // Generate random X and Y positions within the specified ranges
        float randomX = Random.Range(spawnRangeX.x, spawnRangeX.y);
        float randomY = Random.Range(spawnRangeY.x, spawnRangeY.y);
        Vector3 spawnPosition = new Vector3(randomX, randomY, 0f); // z=0 for 2D

        // Instantiate random item at the random position
        int randomIndex = Random.Range(0, itemsToSpawn.Length);
        Instantiate(itemsToSpawn[randomIndex], spawnPosition, Quaternion.identity);

    }
}
