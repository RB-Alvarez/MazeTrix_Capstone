using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class HungerBar : MonoBehaviour
{
  public Slider hungerSlider;
  public UnityEvent OnHungerEmpty;

  [Header("Hunger Settings")]
  public int maxHunger = 100;
  public int currentHunger = 100;

    public int currentHealth;
  public int maxHealth = 100;

    [Header("Timer Settings")]
  public float hungerTickSeconds = 0.5f;

  private float hungerTimer;
  private bool isPaused;

  private void Start()
  {
    // Pull previously saved stats if the player logged in already
    LoadSavedStats();
    hungerTimer = hungerTickSeconds;
    UpdateSlider();
  }

  private void ApplyHungerTick()
  {
    if (currentHunger > 0)
    {
      currentHunger -= 20;

      if (currentHunger < 0)
      {
        currentHunger = 0;
      }

      Debug.Log("Hunger dropped to: " + currentHunger);
    }
    else
    {
      OnHungerEmpty.Invoke();
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
            currentHealth = PlayerSessionData.Instance.health;
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

    private void Update()
    {
        if (isPaused)
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
}
