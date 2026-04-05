using UnityEngine;
using UnityEngine.Events;

public class PlayerCollisions : MonoBehaviour
{
    public Animator animator; // Reference to the player's Animator component

    public UnityEvent OnFoodPickup;
    public UnityEvent OnHealPickup;
    public UnityEvent OnBombPickup;
    public UnityEvent OnSpeedBuffPickup;
    public UnityEvent OnGotHit;


    void Start()
    {
        if (animator == null)
        {
            // Look for an Animator component on the same GameObject if not assigned in the Inspector
            animator = GetComponent<Animator>();
             if (animator == null)
             {
                 Debug.LogWarning("Animator component not found on. Hit animations will not play.");
            }
        }
    }

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

        if (other.gameObject.CompareTag("EnemyWeapon"))
        {
            Debug.Log("Player got hurt!");
            animator.SetTrigger("HurtTrigger"); // Play hit animation
            OnGotHit.Invoke();
        }
    }
}
