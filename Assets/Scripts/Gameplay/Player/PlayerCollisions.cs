using UnityEngine;

public class PlayerCollisions : MonoBehaviour
{
  [SerializeField] private HungerBar hungerBar;
  [SerializeField] private int foodRestoreAmount = 20;

  private void Start()
  {
    // Just in case it wasn't assigned in the Inspector
    if (hungerBar == null)
    {
      hungerBar = FindAnyObjectByType<HungerBar>();
    }
  }

  private void OnTriggerEnter2D(Collider2D other)
  {
    Debug.Log("Collided with something");

    if (other.gameObject.CompareTag("Food"))
    {
      Destroy(other.gameObject);

      if (hungerBar != null)
      {
        // Food should restore hunger and then push it to Firestore
        hungerBar.AddFood(foodRestoreAmount);
      }
    }
  }
}
