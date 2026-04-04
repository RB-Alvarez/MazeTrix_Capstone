using UnityEngine;
using System.Collections;
using Pathfinding;

public class BombObject : MonoBehaviour
{
    void OnEnable()
    {
        // Wait for aninmation to play then destroy the bomb object
        StartCoroutine(WaitAndExplode());  

    }

    IEnumerator WaitAndExplode()
    {
        Debug.Log("Waiting...");

        // Wait for 4 seconds for animation to finish
        yield return new WaitForSeconds(4.1f);

        // Wait for the physics/rendering to finish this frame
        yield return new WaitForEndOfFrame();

        Debug.Log("Updating graph");

        // Update graph
        AstarPath.active.Scan(); // using scan since only occurs once per bomb explosion, so performance is not a concern

        // Destroy the bomb object
        Destroy(gameObject);
    }
}
