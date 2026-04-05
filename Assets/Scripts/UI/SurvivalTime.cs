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
    // Save the time both locally and to Firestore
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

  public void StopTimer()
  {
    if (timerRoutine != null)
    {
      StopCoroutine(timerRoutine);
      timerRoutine = null;
    }
  }
}
