using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider healthSlider;
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
        }
        Debug.Log("Health dropped to: " + currentHealth);
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
            AuthManager.Instance.SavePlayerStats(currentHealth,currentHunger);
        }
    }
}
