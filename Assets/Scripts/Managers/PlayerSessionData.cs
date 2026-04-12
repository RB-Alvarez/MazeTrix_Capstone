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

  // fields to store current chunk record
  public int currentChunkX = 0;
  public int currentChunkY = 0;
  public int currentChunkOriginX = 0;
  public int currentChunkOriginY = 0;
  public int currentChunkOriginZ = 0;
  public int currentChunkSeed = 0;
  public bool currentChunkIsRandomSeed = true; // new user = true for random spawn, existing user = false to use saved seed

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

  public void ResetUserStats() // function to be called to reset the player's stats to default values on death
  {
    health = 100;
    hunger = 100;
    bombCount = 3;
    highestLevel = 1;
    bestScore = 0;
    lastTimeSurvived = 0f;

    positionX = 0f;
    positionY = 0f;
    positionZ = 0f;
    currentChunkX = 0;

    currentChunkY = 0;
    currentChunkOriginX = 0;
    currentChunkOriginY = 0;
    currentChunkOriginZ = 0;
    currentChunkSeed = 0;
    currentChunkIsRandomSeed = true;
  }
}
