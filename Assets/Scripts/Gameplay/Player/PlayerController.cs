using UnityEngine;

public class PlayerController : MonoBehaviour
{
  public Vector2 movementSpeed = new Vector2(5f, 5f);

  private Rigidbody2D rigidbody2D;
  private Vector2 inputVector;

  [Header("Backend Save Settings")]
  public float savePositionInterval = 2f;
  private float saveTimer;

  private void Awake()
  {
    rigidbody2D = GetComponent<Rigidbody2D>();
  }

  private void Start()
  {
    // Try loading the saved position when the player first spawns
    LoadSavedPosition();
    saveTimer = savePositionInterval;
  }

  private void Update()
  {
    float horizontalInput = Input.GetAxisRaw("Horizontal");
    float verticalInput = Input.GetAxisRaw("Vertical");
    inputVector = new Vector2(horizontalInput, verticalInput).normalized;

    saveTimer -= Time.deltaTime;

    // Save every few seconds while playing
    if (saveTimer <= 0f)
    {
      saveTimer = savePositionInterval;
      SaveCurrentPosition();
    }
  }

  private void FixedUpdate()
  {
    if (rigidbody2D == null)
    {
      return;
    }

    rigidbody2D.linearVelocity = new Vector2(
      inputVector.x * movementSpeed.x,
      inputVector.y * movementSpeed.y
    );
  }

  private void OnDisable()
  {
    // Save one more time if the scene changes or object gets disabled
    SaveCurrentPosition();
  }

  private void OnApplicationQuit()
  {
    // Also save when the game closes
    SaveCurrentPosition();
  }

  private void LoadSavedPosition()
  {
    if (PlayerSessionData.Instance == null)
    {
      Debug.LogWarning("PlayerSessionData.Instance is null, so no saved position was loaded.");
      return;
    }

    Debug.Log("Found PlayerSessionData. Loading saved position...");

    Vector3 loadedPosition = new Vector3(
      PlayerSessionData.Instance.positionX,
      PlayerSessionData.Instance.positionY,
      PlayerSessionData.Instance.positionZ
    );

    Debug.Log("Loaded position from session: " + loadedPosition);
    transform.position = loadedPosition;
  }

  private void SaveCurrentPosition()
  {
    Debug.Log("Trying to save player position...");

    if (AuthManager.Instance == null)
    {
      Debug.LogError("AuthManager.Instance is null.");
      return;
    }

    if (string.IsNullOrWhiteSpace(AuthManager.Instance.CurrentUserId))
    {
      Debug.LogError("CurrentUserId is empty, so position was not saved.");
      return;
    }

    Debug.Log("Current player position: " + transform.position);
    Debug.Log("Current user id: " + AuthManager.Instance.CurrentUserId);

    AuthManager.Instance.SavePlayerPosition(transform.position);
  }

    public void ResetPosition()
    {
        transform.position = Vector3.zero;
        SaveCurrentPosition();
    }
}
