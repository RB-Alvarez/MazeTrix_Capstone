using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class HealthBar : MonoBehaviour
{
    public Slider healthSlider;
    public UnityEvent OnDeath;
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    public int currentHunger;
    public int maxHunger = 100;

    private void Start()
    {
        // Pull previously saved stats if the player logged in already
        LoadSavedStats();
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        if (currentHealth < 0)
        {
            currentHealth = 0;
            OnDeath.Invoke();
        }
        Debug.Log("Health dropped to: " + currentHealth);
        UpdateSlider();
        SaveStats();
        FirebaseAIManager.Instance?.UpdatePlayerLog($"Sustained damage, health now at {currentHealth}");
    }

    public void Heal(int healAmount)
    {
        currentHealth += healAmount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        Debug.Log("Health increased to: " + currentHealth);
        UpdateSlider();
        SaveStats();
    }

    private void UpdateSlider()
    {
        if (healthSlider != null)
        {
            healthSlider.value = (float)currentHealth / maxHealth;
        }
    }

    private void LoadSavedStats()
    {
        if (PlayerSessionData.Instance == null)
        {
            currentHunger = maxHunger;
            currentHealth = maxHealth;
            return;
        }

        currentHunger = PlayerSessionData.Instance.hunger;
        currentHealth = PlayerSessionData.Instance.health;
    }

    private void SaveStats()
    {
        if (AuthManager.Instance != null)
        {
            currentHunger = PlayerSessionData.Instance.hunger;
            AuthManager.Instance.SavePlayerStats(currentHealth, currentHunger);
        }
    }
}
