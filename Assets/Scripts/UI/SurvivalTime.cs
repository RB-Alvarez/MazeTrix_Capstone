using UnityEngine;
using System.Collections;
using TMPro; 

public class SurvivalTime : MonoBehaviour
{
    public float survivalTime = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(DoTimer());
    }

    IEnumerator DoTimer()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            survivalTime += 1f;

            // Convert to hours, minutes, seconds
            int hours = Mathf.FloorToInt(survivalTime / 3600f);
            int minutes = Mathf.FloorToInt((survivalTime % 3600f) / 60f);
            int seconds = Mathf.FloorToInt(survivalTime % 60f);
    
            string timeString = string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);

            // Update the text field
            TextMeshProUGUI textField = GetComponent<TextMeshProUGUI>();
            if (textField != null)
            {
                textField.text = timeString;
            }

        }


    }

    void StopTimer()
    {
        StopCoroutine(DoTimer());
    }
}
