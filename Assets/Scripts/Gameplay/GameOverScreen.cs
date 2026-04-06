using UnityEngine;

public class GameOverScreen : MonoBehaviour
{
  private bool hasSavedTime = false;

  private void OnEnable()
  {
    // When the game over screen shows up, save the player's latest survival time
    if (hasSavedTime)
    {
      return;
    }

    SurvivalTime survivalTimer = FindObjectOfType<SurvivalTime>();
    if (survivalTimer != null)
    {
      survivalTimer.SaveCurrentSurvivalTime();
      hasSavedTime = true;
    }
    else
    {
      Debug.LogWarning("Could not find SurvivalTime when trying to save lastTimeSurvived.");
    }
  }
}
