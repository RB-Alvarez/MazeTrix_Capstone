using UnityEngine;
using TMPro;

public class UpdateLevelDisplay : MonoBehaviour
{
    public int currentLevel = 1;
    private TextMeshProUGUI levelText;

    void Start()
    {
        // fetch current level from PlayerData
        currentLevel = PlayerSessionData.Instance.currentLevel;

        levelText = GetComponent<TextMeshProUGUI>();
        levelText.text = $"{currentLevel}";
    }

    public void UpdateCurrentLevel()
    {
        currentLevel += 1;
        levelText.text = $"{currentLevel}";
    }
}
