using UnityEngine;

// Keeps the logged-in player's data around while the game is running
public class PlayerSessionData : MonoBehaviour
{
  public static PlayerSessionData Instance { get; private set; }

  public string uid = "";
  public string email = "";

  public int health = 100;
  public int hunger = 100;
  public int highestLevel = 1;
  public int bestScore = 0;

  public float positionX = 0f;
  public float positionY = 0f;
  public float positionZ = 0f;

  private void Awake()
  {
    // Make sure we only keep one session object
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
    int newHighestLevel,
    int newBestScore,
    float newPositionX,
    float newPositionY,
    float newPositionZ
  )
  {
    uid = newUid;
    email = newEmail;
    health = newHealth;
    hunger = newHunger;
    highestLevel = newHighestLevel;
    bestScore = newBestScore;
    positionX = newPositionX;
    positionY = newPositionY;
    positionZ = newPositionZ;
  }
}
