using UnityEngine;
using System.Collections;
using TMPro;

public class SurvivalTime : MonoBehaviour
{
  public float survivalTime = 0f;

  private Coroutine timerRoutine;
  private TextMeshProUGUI textField;

  void Start()
  {
    textField = GetComponent<TextMeshProUGUI>();

    LoadSavedSurvivalTime();

        // Show 00:00:00 right away so the timer doesn't look blank
        UpdateDisplay();

    timerRoutine = StartCoroutine(DoTimer());
  }

  IEnumerator DoTimer()
  {
    while (true)
    {
      yield return new WaitForSeconds(1f);
      survivalTime += 1f;
      UpdateDisplay();
    }
  }

  private void UpdateDisplay()
  {
    int hours = Mathf.FloorToInt(survivalTime / 3600f);
    int minutes = Mathf.FloorToInt((survivalTime % 3600f) / 60f);
    int seconds = Mathf.FloorToInt(survivalTime % 60f);

    string timeString = string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);

    if (textField != null)
    {
      textField.text = timeString;
    }
  }

  public float GetCurrentSurvivalTime()
  {
    return survivalTime;
  }

  public void SaveCurrentSurvivalTime()
  {
    if (PlayerSessionData.Instance != null)
    {
      PlayerSessionData.Instance.lastTimeSurvived = survivalTime;
    }

    if (AuthManager.Instance != null)
    {
      AuthManager.Instance.SaveLastTimeSurvived(survivalTime);
    }

    Debug.Log("Saved lastTimeSurvived: " + survivalTime);
  }

    public void LoadSavedSurvivalTime()
    {
        if (PlayerSessionData.Instance != null)
        {
            survivalTime = PlayerSessionData.Instance.lastTimeSurvived;
            UpdateDisplay();
        }
    }

  public void StopTimer()
  {
    if (timerRoutine != null)
    {
      StopCoroutine(timerRoutine);
      timerRoutine = null;
    }
  }
}
