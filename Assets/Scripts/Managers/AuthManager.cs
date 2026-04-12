using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine.Events;

public class AuthManager : MonoBehaviour
{
  public static AuthManager Instance { get; private set; }

  [Header("UI References (drag these in the Inspector)")]
  [SerializeField] private TMP_InputField emailInput;
  [SerializeField] private TMP_InputField passwordInput;
  [SerializeField] private TMP_Text statusText;

  private FirebaseAuth auth;
  private FirebaseFirestore db;
  public UnityEvent OnLoginSuccess;

  public string CurrentUserId
  {
    get
    {
      if (auth == null || auth.CurrentUser == null)
      {
        return "";
      }

      return auth.CurrentUser.UserId;
    }
  }

  private void Awake()
  {
    // Basic singleton setup so other scripts can use AuthManager easily
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);

    // These get assigned after Firebase is confirmed ready
    auth = null;
    db = null;
  }

  public void OnSignUpPressed()
  {
    string email = GetText(emailInput);
    string password = GetText(passwordInput);

    if (!ValidateInputs(email, password)) return;

    SetStatus("Creating account...");

    SignUp(email, password, message =>
    {
      SetStatus(message);
    });
  }

  public void OnLoginPressed()
  {
    string email = GetText(emailInput);
    string password = GetText(passwordInput);

    if (!ValidateInputs(email, password)) return;

    SetStatus("Logging in...");

    Login(email, password, message =>
    {
      SetStatus(message);
    });
  }

  public void OnLogoutPressed()
  {
    Logout(message =>
    {
      SetStatus(message);
    });
  }

  public void SignUp(string email, string password, Action<string> onResult)
  {
    if (!EnsureFirebase(onResult)) return;

    auth.CreateUserWithEmailAndPasswordAsync(email, password)
      .ContinueWithOnMainThread(task =>
      {
        if (task.IsCanceled)
        {
          onResult?.Invoke("Sign up canceled.");
          return;
        }

        if (task.IsFaulted)
        {
          onResult?.Invoke(task.Exception.GetBaseException().Message);
          return;
        }

        FirebaseUser newUser = task.Result.User;

        if (newUser == null)
        {
          onResult?.Invoke("Account created, but user info was missing.");
          return;
        }

        // After account creation, also save a starter profile in Firestore
        CreateUserProfile(newUser, profileMessage =>
        {
          onResult?.Invoke(profileMessage);
        });
      });
  }

  public void Login(string email, string password, Action<string> onResult)
  {
    if (!EnsureFirebase(onResult)) return;

    auth.SignInWithEmailAndPasswordAsync(email, password)
      .ContinueWithOnMainThread(task =>
      {
        if (task.IsCanceled)
        {
          onResult?.Invoke("Login canceled.");
          return;
        }

        if (task.IsFaulted)
        {
          onResult?.Invoke(task.Exception.GetBaseException().Message);
          return;
        }

        FirebaseUser loggedInUser = task.Result.User;

        if (loggedInUser == null)
        {
          onResult?.Invoke("Login worked, but user info was missing.");
          return;
        }

        // Pull saved data from Firestore after login
        LoadUserProfile(loggedInUser.UserId, profileMessage =>
        {
          onResult?.Invoke(profileMessage);
        });

        OnLoginSuccess?.Invoke();
      });
  }

  private void CreateUserProfile(FirebaseUser user, Action<string> onResult)
  {
    DocumentReference userDoc = db.Collection("users").Document(user.UserId);

    Dictionary<string, object> playerData = new Dictionary<string, object>()
    {
      { "uid", user.UserId },
      { "email", user.Email },
      { "createdAt", Timestamp.GetCurrentTimestamp() },
      { "lastLoginAt", Timestamp.GetCurrentTimestamp() },
      { "bestScore", 0 },
      { "highestLevel", 1 },
      { "health", 100 },
      { "hunger", 100 },
      { "bombCount", 3 },
      { "lastTimeSurvived", 0f },
      { "positionX", 0f },
      { "positionY", 0f },
      { "positionZ", 0f },
      { "worldSeed", 0 },
      { "worldSeedInitialized", false },
      { "currentChunkX", 0 },
      { "currentChunkY", 0 }
    };

    userDoc.SetAsync(playerData).ContinueWithOnMainThread(task =>
    {
      if (task.IsCanceled)
      {
        onResult?.Invoke("Account created, but profile save was canceled.");
        return;
      }

      if (task.IsFaulted)
      {
        onResult?.Invoke("Account created, but Firestore save failed.");
        return;
      }

      // Also store the same info locally for this play session
      EnsureSessionDataObject();

      PlayerSessionData.Instance.ApplyUserData(
        user.UserId,
        user.Email,
        100,
        100,
        3,
        1,
        0,
        0f,
        0f,
        0f,
        0f,
        0,     // worldSeed fields start here
        false, 
        0,     
        0      
      );

      onResult?.Invoke("Sign up successful. Player data saved.");
    });
  }

  private void LoadUserProfile(string uid, Action<string> onResult)
  {
    DocumentReference userDoc = db.Collection("users").Document(uid);

    userDoc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
    {
      if (task.IsCanceled)
      {
        onResult?.Invoke("Login worked, but profile loading was canceled.");
        return;
      }

      if (task.IsFaulted)
      {
        onResult?.Invoke("Login worked, but profile loading failed.");
        return;
      }

      DocumentSnapshot snapshot = task.Result;

      if (!snapshot.Exists)
      {
        onResult?.Invoke("Login successful, but no player profile was found.");
        return;
      }

      string email = snapshot.ContainsField("email") ? snapshot.GetValue<string>("email") : "N/A";
      int bestScore = snapshot.ContainsField("bestScore") ? snapshot.GetValue<int>("bestScore") : 0;
      int highestLevel = snapshot.ContainsField("highestLevel") ? snapshot.GetValue<int>("highestLevel") : 1;
      int health = snapshot.ContainsField("health") ? snapshot.GetValue<int>("health") : 100;
      int hunger = snapshot.ContainsField("hunger") ? snapshot.GetValue<int>("hunger") : 100;
      int bombCount = snapshot.ContainsField("bombCount") ? snapshot.GetValue<int>("bombCount") : 3;

      float lastTimeSurvived = 0f;
      if (snapshot.ContainsField("lastTimeSurvived"))
      {
        object rawTime = snapshot.GetValue<object>("lastTimeSurvived");

        if (rawTime is double doubleTime)
        {
          lastTimeSurvived = Convert.ToSingle(doubleTime);
        }
        else if (rawTime is long longTime)
        {
          lastTimeSurvived = longTime;
        }
      }

      // Firestore stores numbers a little differently sometimes, so convert carefully
      float positionX = snapshot.ContainsField("positionX") ? Convert.ToSingle(snapshot.GetValue<double>("positionX")) : 0f;
      float positionY = snapshot.ContainsField("positionY") ? Convert.ToSingle(snapshot.GetValue<double>("positionY")) : 0f;
      float positionZ = snapshot.ContainsField("positionZ") ? Convert.ToSingle(snapshot.GetValue<double>("positionZ")) : 0f;

      int worldSeed = snapshot.ContainsField("worldSeed") ? snapshot.GetValue<int>("worldSeed") : 0;
      bool worldSeedInitialized = snapshot.ContainsField("worldSeedInitialized") ? snapshot.GetValue<bool>("worldSeedInitialized") : false;
      int currentChunkX = snapshot.ContainsField("currentChunkX") ? snapshot.GetValue<int>("currentChunkX") : 0;
      int currentChunkY = snapshot.ContainsField("currentChunkY") ? snapshot.GetValue<int>("currentChunkY") : 0;

      EnsureSessionDataObject();

      PlayerSessionData.Instance.ApplyUserData(
        uid,
        email,
        health,
        hunger,
        bombCount,
        highestLevel,
        bestScore,
        lastTimeSurvived,
        positionX,
        positionY,
        positionZ,
        worldSeed,
        worldSeedInitialized,
        currentChunkX,
        currentChunkY
      );

      Debug.Log("Loaded profile:");
      Debug.Log("Email: " + email);
      Debug.Log("Best Score: " + bestScore);
      Debug.Log("Highest Level: " + highestLevel);
      Debug.Log("Health: " + health);
      Debug.Log("Hunger: " + hunger);
      Debug.Log("Bomb Count: " + bombCount);
      Debug.Log("Last Time Survived: " + lastTimeSurvived);
      Debug.Log("Position: " + positionX + ", " + positionY + ", " + positionZ);
      Debug.Log("World Seed: " + worldSeed + ", Initialized: " + worldSeedInitialized);
      Debug.Log("Current Chunk: (" + currentChunkX + ", " + currentChunkY + ")");

      UpdateLastLogin(uid);

      onResult?.Invoke("Login successful. Player profile loaded.");
    });
  }

  private void UpdateLastLogin(string uid)
  {
    DocumentReference userDoc = db.Collection("users").Document(uid);

    Dictionary<string, object> updates = new Dictionary<string, object>()
    {
      { "lastLoginAt", Timestamp.GetCurrentTimestamp() }
    };

    userDoc.UpdateAsync(updates).ContinueWithOnMainThread(task =>
    {
      if (task.IsCompleted)
      {
        Debug.Log("Last login time updated.");
      }
    });
  }

  public void SavePlayerStats(int health, int hunger)
  {
    if (!EnsureFirebaseSilent()) return;

    string uid = CurrentUserId;
    if (string.IsNullOrWhiteSpace(uid)) return;

    Dictionary<string, object> updates = new Dictionary<string, object>()
    {
      { "health", health },
      { "hunger", hunger }
    };

    db.Collection("users").Document(uid).UpdateAsync(updates);

    if (PlayerSessionData.Instance != null)
    {
      PlayerSessionData.Instance.health = health;
      PlayerSessionData.Instance.hunger = hunger;
    }
  }

  public void SaveBombCount(int bombCount)
  {
    if (!EnsureFirebaseSilent()) return;

    string uid = CurrentUserId;
    if (string.IsNullOrWhiteSpace(uid)) return;

    Dictionary<string, object> updates = new Dictionary<string, object>()
    {
      { "bombCount", bombCount }
    };

    db.Collection("users").Document(uid).UpdateAsync(updates)
      .ContinueWithOnMainThread(task =>
      {
        if (task.IsCanceled)
        {
          Debug.LogError("Bomb count save was canceled.");
          return;
        }

        if (task.IsFaulted)
        {
          Debug.LogError("Bomb count save failed: " + task.Exception);
          return;
        }

        Debug.Log("Bomb count saved to Firestore.");
      });

    if (PlayerSessionData.Instance != null)
    {
      PlayerSessionData.Instance.bombCount = bombCount;
    }
  }

  public void SaveLastTimeSurvived(float lastTimeSurvived)
  {
    if (!EnsureFirebaseSilent()) return;

    string uid = CurrentUserId;
    if (string.IsNullOrWhiteSpace(uid)) return;

    Dictionary<string, object> updates = new Dictionary<string, object>()
    {
      { "lastTimeSurvived", lastTimeSurvived }
    };

    db.Collection("users").Document(uid).UpdateAsync(updates)
      .ContinueWithOnMainThread(task =>
      {
        if (task.IsCanceled)
        {
          Debug.LogError("Last survival time save was canceled.");
          return;
        }

        if (task.IsFaulted)
        {
          Debug.LogError("Last survival time save failed: " + task.Exception);
          return;
        }

        Debug.Log("Last survival time saved to Firestore.");
      });

    if (PlayerSessionData.Instance != null)
    {
      PlayerSessionData.Instance.lastTimeSurvived = lastTimeSurvived;
    }
  }

  public void SavePlayerPosition(Vector3 position)
  {
    Debug.Log("SavePlayerPosition called.");

    if (!EnsureFirebaseSilent())
    {
      Debug.LogError("Firebase is not ready, so position was not saved.");
      return;
    }

    string uid = CurrentUserId;

    if (string.IsNullOrWhiteSpace(uid))
    {
      Debug.LogError("CurrentUserId is empty, so position was not saved.");
      return;
    }

    Debug.Log("Saving position for uid: " + uid);
    Debug.Log("Position being saved: " + position);

    Dictionary<string, object> updates = new Dictionary<string, object>()
    {
      { "positionX", position.x },
      { "positionY", position.y },
      { "positionZ", position.z }
    };

    db.Collection("users").Document(uid).UpdateAsync(updates)
      .ContinueWithOnMainThread(task =>
      {
        if (task.IsCanceled)
        {
          Debug.LogError("Position save was canceled.");
          return;
        }

        if (task.IsFaulted)
        {
          Debug.LogError("Position save failed: " + task.Exception);
          return;
        }

        Debug.Log("Position saved to Firestore successfully.");
      });

    if (PlayerSessionData.Instance != null)
    {
      PlayerSessionData.Instance.positionX = position.x;
      PlayerSessionData.Instance.positionY = position.y;
      PlayerSessionData.Instance.positionZ = position.z;
    }
  }

  public void SaveWorldSeed(int worldSeed)
  {
    Debug.Log("SaveWorldSeed called.");

    if (!EnsureFirebaseSilent())
    {
      Debug.LogError("Firebase is not ready, so world seed was not saved.");
      return;
    }

    string uid = CurrentUserId;

    if (string.IsNullOrWhiteSpace(uid))
    {
      Debug.LogError("CurrentUserId is empty, so world seed was not saved.");
      return;
    }

    Debug.Log($"Saving world seed for uid: {uid}, seed: {worldSeed}");

    Dictionary<string, object> updates = new Dictionary<string, object>()
    {
      { "worldSeed", worldSeed },
      { "worldSeedInitialized", true }
    };

    db.Collection("users").Document(uid).UpdateAsync(updates)
      .ContinueWithOnMainThread(task =>
      {
        if (task.IsCanceled)
        {
          Debug.LogError("World seed save was canceled.");
          return;
        }

        if (task.IsFaulted)
        {
          Debug.LogError("World seed save failed: " + task.Exception);
          return;
        }

        Debug.Log("World seed saved to Firestore successfully.");
      });

    if (PlayerSessionData.Instance != null)
    {
      PlayerSessionData.Instance.worldSeed = worldSeed;
      PlayerSessionData.Instance.worldSeedInitialized = true;
    }
  }

  public void SaveCurrentChunkPosition(int chunkX, int chunkY)
  {
    if (!EnsureFirebaseSilent()) return;

    string uid = CurrentUserId;
    if (string.IsNullOrWhiteSpace(uid)) return;

    Dictionary<string, object> updates = new Dictionary<string, object>()
    {
      { "currentChunkX", chunkX },
      { "currentChunkY", chunkY }
    };

    db.Collection("users").Document(uid).UpdateAsync(updates);

    if (PlayerSessionData.Instance != null)
    {
      PlayerSessionData.Instance.currentChunkX = chunkX;
      PlayerSessionData.Instance.currentChunkY = chunkY;
    }
  }

  public void Logout(Action<string> onResult)
  {
    if (auth != null)
    {
      auth.SignOut();
    }

    onResult?.Invoke("Logged out.");
  }

  private bool EnsureFirebase(Action<string> onResult)
  {
    if (!FirebaseBootstrap.Ready)
    {
      onResult?.Invoke("Firebase not ready yet. Try again in a moment.");
      return false;
    }

    if (auth == null)
    {
      auth = FirebaseAuth.DefaultInstance;
    }

    if (db == null)
    {
      db = FirebaseFirestore.DefaultInstance;
    }

    return true;
  }

  private bool EnsureFirebaseSilent()
  {
    if (!FirebaseBootstrap.Ready)
    {
      return false;
    }

    if (auth == null)
    {
      auth = FirebaseAuth.DefaultInstance;
    }

    if (db == null)
    {
      db = FirebaseFirestore.DefaultInstance;
    }

    return true;
  }

  private void EnsureSessionDataObject()
  {
    // Create the session holder if it doesn't already exist
    if (PlayerSessionData.Instance == null)
    {
      GameObject sessionObject = new GameObject("PlayerSessionData");
      sessionObject.AddComponent<PlayerSessionData>();
    }
  }

  private bool ValidateInputs(string email, string password)
  {
    if (string.IsNullOrWhiteSpace(email))
    {
      SetStatus("Please enter an email.");
      return false;
    }

    if (string.IsNullOrWhiteSpace(password))
    {
      SetStatus("Please enter a password.");
      return false;
    }

    if (password.Length < 6)
    {
      SetStatus("Password must be at least 6 characters.");
      return false;
    }

    return true;
  }

  private string GetText(TMP_InputField field)
  {
    return field == null ? "" : field.text.Trim();
  }

  private void SetStatus(string message)
  {
    Debug.Log(message);

    if (statusText != null)
    {
      statusText.text = message;
    }
  }
}
