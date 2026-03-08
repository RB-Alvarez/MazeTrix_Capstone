using UnityEngine;
using UnityEngine.UI;

public class HungerBar : MonoBehaviour
{
  public Slider hungerSlider;

  [Header("Hunger Settings")]
  public int maxHunger = 100;
  public int currentHunger = 100;

  [Header("Health Settings")]
  public int maxHealth = 100;
  public int currentHealth = 100;

  [Header("Timer Settings")]
  public float hungerTickSeconds = 10f;

  private float hungerTimer;
  private bool isPaused;

  private void Start()
  {
    // Pull previously saved stats if the player logged in already
    LoadSavedStats();
    hungerTimer = hungerTickSeconds;
    UpdateSlider();
  }

  private void Update()
  {
    if (isPaused)
    {
      return;
    }

    if (currentHealth <= 0)
    {
      return;
    }

    hungerTimer -= Time.deltaTime;

    // Hunger only drops once every set time, not every frame
    if (hungerTimer <= 0f)
    {
      hungerTimer = hungerTickSeconds;
      ApplyHungerTick();
    }

    UpdateSlider();
  }

  private void ApplyHungerTick()
  {
    if (currentHunger > 0)
    {
      currentHunger -= 1;

      if (currentHunger < 0)
      {
        currentHunger = 0;
      }

      Debug.Log("Hunger dropped to: " + currentHunger);
    }
    else
    {
      // Once hunger hits 0, start taking health damage instead
      currentHealth -= 1;

      if (currentHealth < 0)
      {
        currentHealth = 0;
      }

      Debug.Log("Player is starving. Health is now: " + currentHealth);
    }

    SaveStats();
  }

  public void AddFood(int amount)
  {
    currentHunger += amount;

    if (currentHunger > maxHunger)
    {
      currentHunger = maxHunger;
    }

    Debug.Log("Food collected. Hunger is now: " + currentHunger);

    SaveStats();
    UpdateSlider();
  }

  public void SetPaused(bool pausedState)
  {
    isPaused = pausedState;
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
      AuthManager.Instance.SavePlayerStats(currentHealth, currentHunger);
    }
  }

  private void UpdateSlider()
  {
    if (hungerSlider != null)
    {
      hungerSlider.value = (float) currentHunger / maxHunger;
    }
  }
}
