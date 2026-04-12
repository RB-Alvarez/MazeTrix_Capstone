using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
  public Vector2 baseSpeed = new Vector2(5f, 5f);
  public Vector2 buffedSpeed = new Vector2(10f, 10f); //public so it can be set in the inspector for testing
  private Vector2 movementSpeed;

  private Rigidbody2D rigidbody2D;
  private Vector2 inputVector;

  [Header("Backend Save Settings")]
  public float savePositionInterval = 2f;
  private float saveTimer;

  // Internal flag set when a generator placed the player so we don't overwrite that placement with saved data
  private bool _placedByGenerator = false;

  private void Awake()
  {
    rigidbody2D = GetComponent<Rigidbody2D>();
  }

  private void Start()
  {
    // Try loading the saved position when the player first spawns (will be skipped if generator already placed the player)
    LoadSavedPosition();
    saveTimer = savePositionInterval;

    // Initialize movement speed to base speed at the start
    movementSpeed = baseSpeed;
  }

  private void Update()
  {
    // Raw input for movement
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

  // SPEED BUFF LOGIC
  public void ActivateSpeedBuff(float duration)
  {
    StartCoroutine(SpeedBuffRoutine(duration));
  }

  IEnumerator SpeedBuffRoutine(float duration)
  {
    movementSpeed = buffedSpeed;
    Debug.Log("Speed buff activated!");
    yield return new WaitForSeconds(duration);
    movementSpeed = baseSpeed;
    Debug.Log("Speed buff expired.");
  }

  // SAVE SYSTEM LOGIC
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

  // Called by generator when it spawns or repositions the player so playerController can force position and apply session stats
  public void OnPlacedByGenerator(Vector3 worldPosition)
  {
    _placedByGenerator = true;

    // Force transform position
    transform.position = worldPosition;

    // Ensure rigidbody matches position and stop motion
    if (rigidbody2D == null) rigidbody2D = GetComponent<Rigidbody2D>();
    if (rigidbody2D != null)
    {
      rigidbody2D.position = new Vector2(worldPosition.x, worldPosition.y);
      rigidbody2D.linearVelocity = Vector2.zero;
    }

    // Pull stats from session and apply to in-scene components
    var session = PlayerSessionData.Instance;
    if (session != null)
    {
      // HealthBar (if present) - set values and update slider directly
      var healthBar = GetComponentInChildren<HealthBar>();
      if (healthBar != null)
      {
        healthBar.currentHealth = session.health;
        healthBar.currentHunger = session.hunger;
        if (healthBar.healthSlider != null)
        {
          healthBar.healthSlider.value = Mathf.Clamp01((float)healthBar.currentHealth / Mathf.Max(1, healthBar.maxHealth));
        }
      }

      // HungerBar (if present)
      var hungerBar = GetComponentInChildren<HungerBar>();
      if (hungerBar != null)
      {
        hungerBar.currentHunger = session.hunger;
        hungerBar.currentHealth = session.health;
        if (hungerBar.hungerSlider != null)
        {
          hungerBar.hungerSlider.value = Mathf.Clamp01((float)hungerBar.currentHunger / Mathf.Max(1, hungerBar.maxHunger));
        }
      }

      // Update PlayerSessionData stored position so subsequent saves reflect generator placement
      session.positionX = worldPosition.x;
      session.positionY = worldPosition.y;
      session.positionZ = worldPosition.z;
    }
  }

  private void LoadSavedPosition()
  {
    if (PlayerSessionData.Instance == null)
    {
      Debug.LogWarning("PlayerSessionData.Instance is null, so no saved position was loaded.");
      return;
    }

    // If a generator already placed the player, do not override placement
    if (_placedByGenerator)
    {
      Debug.Log("PlayerController: Generator placed the player; skipping saved-position apply.");
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
