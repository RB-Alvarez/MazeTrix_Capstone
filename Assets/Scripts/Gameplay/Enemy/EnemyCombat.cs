using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    public Animator animator; // Reference to the enemy's Animator component

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        //animator.SetTrigger("HitTrigger"); // Play hit animation



        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        // Play death animation
        Debug.Log(name + " has died.");
        //animator.SetBool("IsDead", true)

        // Destroy the enemy after a short delay to allow the death animation to play
        Destroy(gameObject, 0.5f);

    }

}
