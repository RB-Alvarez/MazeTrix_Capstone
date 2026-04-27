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

  // world seed used to generate same world in conjunction with chunk coords
  public int worldSeed = 0;
  public bool worldSeedInitialized = false; // true after first world generation

  public int currentChunkX = 0;
  public int currentChunkY = 0;

  // XP system
  public int xp = 0;
  public int currentLevel = 1;

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
    float newPositionZ,
    int newWorldSeed,
    bool newWorldSeedInitialized,
    int newCurrentChunkX,
    int newCurrentChunkY,
    int newXp = 0,
    int newCurrentLevel = 1
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
    worldSeed = newWorldSeed;
    worldSeedInitialized = newWorldSeedInitialized;
    currentChunkX = newCurrentChunkX;
    currentChunkY = newCurrentChunkY;
    xp = newXp;
    currentLevel = newCurrentLevel;
  }
}
