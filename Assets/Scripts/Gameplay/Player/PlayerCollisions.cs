using UnityEngine;

public class PlayerCollisions : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Collided with something"); // Debug log to check collision
        //if touching food
        if (other.gameObject.CompareTag("Food"))
        {
            //delete the food
            Destroy(other.gameObject);
            //increment hunger bar
            FindAnyObjectByType<HungerBar>().currentHunger += 20f; // Adjust the value as needed
        }
    }
}
