using UnityEngine;

public class OpenPauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI; // Reference to your menu GameObject in the scene
    private bool isPaused = false;

    void Start()
    {
        // Ensure the menu is hidden when the game starts
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        Time.timeScale = 1f; // Ensure the game starts unpaused
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) // Check if the Escape key is pressed once
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false); // Hide the menu
        Time.timeScale = 1f;         // Resume game time
        isPaused = false;

    }

    public void PauseGame()
    {
        pauseMenuUI.SetActive(true); // Show the menu
        PauseGameTime();         // Pause game time
        isPaused = true;

    }

    public void PauseGameTime()
    {
        Time.timeScale = 0f; // Pause game time
    }
}
