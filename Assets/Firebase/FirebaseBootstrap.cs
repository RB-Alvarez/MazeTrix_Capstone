using UnityEngine;
using Firebase;
using Firebase.Extensions;

public class FirebaseBootstrap : MonoBehaviour
{
  // Flag other scripts can check before using Firebase
  public static bool Ready { get; private set; }

  private void Awake()
  {
    // Firebase is not ready when the app first starts
    Ready = false;
  }

  private void Start()
  {
    // Check Firebase dependencies and initialize
    FirebaseApp.CheckAndFixDependenciesAsync()
      .ContinueWithOnMainThread(task =>
      {
        var status = task.Result;

        if (status == DependencyStatus.Available)
        {
          Ready = true;
          Debug.Log("Firebase is ready.");
        }
        else
        {
          Debug.LogError("Firebase dependency issue: " + status);
        }
      });
  }
}
