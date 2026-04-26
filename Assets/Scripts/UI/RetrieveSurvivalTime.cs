using UnityEngine;
using TMPro;

public class RetrieveSurvivalTime : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign the SurvivalTime component in the Inspector (preferred).")]
    public SurvivalTime survivalTimeComponent;

    void OnEnable()
    {
        
        if (survivalTimeComponent != null)
        {
            float survivalTime = survivalTimeComponent.survivalTime;
            Debug.Log("Retrieved Survival Time: " + survivalTime);

            // Convert to hours, minutes, seconds
            int hours = Mathf.FloorToInt(survivalTime / 3600f);
            int minutes = Mathf.FloorToInt((survivalTime % 3600f) / 60f);
            int seconds = Mathf.FloorToInt(survivalTime % 60f);

            string timeString = string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);

            // Display it
            TextMeshProUGUI textField = GetComponent<TextMeshProUGUI>();
            if (textField != null)
            {
                textField.text = "After "+timeString+"...";
            }
        }
        else
        {
            Debug.LogWarning("SurvivalTime component not assigned.");
        }
    }
}
