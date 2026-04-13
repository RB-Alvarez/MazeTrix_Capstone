using UnityEngine;

public class QuitGame : MonoBehaviour
{
    public void doExitGame()
    {
        Application.Quit();
        Debug.Log("Game is exiting...");

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false; // Stop play mode in the Unity Editor for testing
#endif
    }

    public void ExitAndResetPlayerData()
    {
        PlayerSessionData.Instance?.ResetUserStats();
        doExitGame();
    }
}
