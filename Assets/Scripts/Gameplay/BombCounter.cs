using UnityEngine;
using TMPro;

public class BombCounter : MonoBehaviour
{
  public GameObject bombText;
  public int bombCount = 3;

  public void Start()
  {
    LoadSavedBombCount();
    UpdateText();
  }

  public void SubtractBomb()
  {
    if (bombCount > 0)
    {
      bombCount--;
      Debug.Log("Bomb used! Current bombs: " + bombCount);
      SaveBombCount();
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
    SaveBombCount();
    UpdateText();
  }

  private void LoadSavedBombCount()
  {
    // If there is session data, use the value that was loaded from Firestore
    if (PlayerSessionData.Instance != null)
    {
      bombCount = PlayerSessionData.Instance.bombCount;
    }
    else
    {
      // Fall back to default if no session exists yet
      bombCount = 3;
    }
  }

  private void SaveBombCount()
  {
    if (PlayerSessionData.Instance != null)
    {
      PlayerSessionData.Instance.bombCount = bombCount;
    }

    if (AuthManager.Instance != null)
    {
      AuthManager.Instance.SaveBombCount(bombCount);
    }
  }

  private void UpdateText()
  {
    if (bombText != null)
    {
      bombText.GetComponent<TextMeshProUGUI>().text = $"{bombCount}";
    }
  }
}
