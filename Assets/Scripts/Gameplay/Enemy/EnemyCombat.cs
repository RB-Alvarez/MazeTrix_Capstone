using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    private int scaledMaxHealth;
    private bool isDead;

    public Animator animator;

    void Start()
    {
        scaledMaxHealth = maxHealth;
        currentHealth = scaledMaxHealth;
        isDead = false;
    }

    public void ApplyHealthScaling(float multiplier)
    {
        if (isDead) return;

        int newMax = Mathf.RoundToInt(maxHealth * multiplier);

        // Only update the ceiling, never restore current health mid-fight
        scaledMaxHealth = newMax;

        // Clamp in case the max shrank below current
        currentHealth = Mathf.Min(currentHealth, scaledMaxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        FirebaseAIManager.Instance?.UpdatePlayerLog($"Neutralized a hostile unit.");
        PlayerXP.Instance?.AddKillXP();
        Destroy(gameObject, 0.5f);
    }
}
