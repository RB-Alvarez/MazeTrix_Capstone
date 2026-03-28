using UnityEngine;
using UnityEngine.Events;

public class PlayerCollisions : MonoBehaviour
{
    public UnityEvent OnFoodPickup;
    public UnityEvent OnHealPickup;
    public UnityEvent OnBombPickup;
    public UnityEvent OnSpeedBuffPickup;


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Food"))
        {
            Destroy(other.gameObject);
            Debug.Log("Food collected!");
            OnFoodPickup.Invoke();
        }

        if (other.gameObject.CompareTag("Heal"))
        {
            Destroy(other.gameObject);
            Debug.Log("Heal collected!");
            OnHealPickup.Invoke();
        }

        if (other.gameObject.CompareTag("Bomb"))
        {
            Destroy(other.gameObject);
            Debug.Log("Bomb collected!");
            OnBombPickup.Invoke();
        }

        if (other.gameObject.CompareTag("Speed"))
        {
            Destroy(other.gameObject);
            Debug.Log("Speed buff collected!");
            OnSpeedBuffPickup.Invoke();
        }
    }
}
