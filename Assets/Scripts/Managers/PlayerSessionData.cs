using UnityEngine;

// This stores the logged-in player's data while the game is running
public class PlayerSessionData : MonoBehaviour
{
  public static PlayerSessionData Instance { get; private set; }

  public string uid = "";
  public string email = "";

  public int health = 100;
  public int hunger = 100;
  public int bombCount = 3;
  public int highestLevel = 1;
  public int bestScore = 0;
  public float lastTimeSurvived = 0f;

  public float positionX = 0f;
  public float positionY = 0f;
  public float positionZ = 0f;

  private void Awake()
  {
    // Only keep one copy of this object across scenes
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);
  }

  public void ApplyUserData(
    string newUid,
    string newEmail,
    int newHealth,
    int newHunger,
    int newBombCount,
    int newHighestLevel,
    int newBestScore,
    float newLastTimeSurvived,
    float newPositionX,
    float newPositionY,
    float newPositionZ
  )
  {
    uid = newUid;
    email = newEmail;
    health = newHealth;
    hunger = newHunger;
    bombCount = newBombCount;
    highestLevel = newHighestLevel;
    bestScore = newBestScore;
    lastTimeSurvived = newLastTimeSurvived;
    positionX = newPositionX;
    positionY = newPositionY;
    positionZ = newPositionZ;
  }
}
