using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;

public class AuthManager : MonoBehaviour
{
  [Header("UI References (drag these in the Inspector)")]
  [SerializeField] private TMP_InputField emailInput;
  [SerializeField] private TMP_InputField passwordInput;
  [SerializeField] private TMP_Text statusText;

  private FirebaseAuth auth;
  private FirebaseFirestore db;

  private void Awake()
  {
    // Wait until FirebaseBootstrap says Firebase is ready
    auth = null;
    db = null;
  }

  // Called by the Sign Up button
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

  // Called by the Login button
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

  // Optional logout button
  public void OnLogoutPressed()
  {
    Logout(message =>
    {
      SetStatus(message);
    });
  }

  // Register account and create Firestore player profile
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

        CreateUserProfile(newUser, profileMessage =>
        {
          onResult?.Invoke(profileMessage);
        });
      });
  }

  // Login and load Firestore player profile
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

        LoadUserProfile(loggedInUser.UserId, profileMessage =>
        {
          onResult?.Invoke(profileMessage);
        });
      });
  }

  // Save starter data for a new player
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
      { "hunger", 100 }
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

      onResult?.Invoke("Sign up successful. Player data saved.");
    });
  }

  // Read player data after login
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

      Debug.Log("Loaded profile:");
      Debug.Log("Email: " + email);
      Debug.Log("Best Score: " + bestScore);
      Debug.Log("Highest Level: " + highestLevel);
      Debug.Log("Health: " + health);
      Debug.Log("Hunger: " + hunger);

      UpdateLastLogin(uid);

      onResult?.Invoke("Login successful. Player profile loaded.");
    });
  }

  // Update last login time each time user signs in
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

  // Logout logic
  public void Logout(Action<string> onResult)
  {
    if (auth != null)
    {
      auth.SignOut();
    }

    onResult?.Invoke("Logged out.");
  }

  // Make sure Firebase, Auth, and Firestore are ready
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

  // Simple input validation
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

  // Safely get text from input fields
  private string GetText(TMP_InputField field)
  {
    return field == null ? "" : field.text.Trim();
  }

  // Update UI and also log to Console
  private void SetStatus(string message)
  {
    Debug.Log(message);

    if (statusText != null)
    {
      statusText.text = message;
    }
  }
}
