using UnityEngine;
using TMPro;

public class BombCounter : MonoBehaviour
{
    public GameObject bombText; // Reference to the UI Text element that displays the bomb count
    public int bombCount = 3; // Initial number of bombs the player starts with

    public void Start()
    {
        UpdateText(); // Initialize the bomb count display at the start of the game
    }

    public void SubtractBomb()
    {
        if (bombCount > 0)
        {
            bombCount--;
            Debug.Log("Bomb used! Current bombs: " + bombCount);
            UpdateText();
        }
        else
        {
            Debug.Log("No bombs left to use!");
        }
    }
    

    public void AddBomb(int amount)
    {
        bombCount += amount;
        Debug.Log("Bombs added! Current bombs: " + bombCount);
        UpdateText();
    }

    private void UpdateText()
    {
        // Update the bomb count display in the UI
        if (bombText != null)
        {
            bombText.GetComponent<TextMeshProUGUI>().text = $"{bombCount}";
        }
    }

    
    /*
    private void LoadSavedStats()
    {
        if (PlayerSessionData.Instance == null)
        {
            bombCount = 3; 
            return;
        }

       bombCount = PlayerSessionData.Instance.bombCount;
    }

    private void SaveStats()
    {
        if (AuthManager.Instance != null)
        {
            currentHunger = PlayerSessionData.Instance.hunger;
            AuthManager.Instance.SavePlayerStats(currentHealth, currentHunger);
        }
    }
    */
    
}
