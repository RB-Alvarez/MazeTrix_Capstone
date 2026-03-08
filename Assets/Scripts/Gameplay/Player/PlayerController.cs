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
    // If the player already has saved position data, load it here
    LoadSavedPosition();
    saveTimer = savePositionInterval;
  }

  private void Update()
  {
    float horizontalInput = Input.GetAxisRaw("Horizontal");
    float verticalInput = Input.GetAxisRaw("Vertical");
    inputVector = new Vector2(horizontalInput, verticalInput).normalized;

    saveTimer -= Time.deltaTime;

    // Save position every few seconds so progress isn't lost
    if (saveTimer <= 0f)
    {
      saveTimer = savePositionInterval;
      SaveCurrentPosition();
    }
  }

  private void FixedUpdate()
  {
    rigidbody2D.linearVelocity = new Vector2(
      inputVector.x * movementSpeed.x,
      inputVector.y * movementSpeed.y
    );
  }

  private void LoadSavedPosition()
  {
    if (PlayerSessionData.Instance == null)
    {
      return;
    }

    Vector3 loadedPosition = new Vector3(
      PlayerSessionData.Instance.positionX,
      PlayerSessionData.Instance.positionY,
      PlayerSessionData.Instance.positionZ
    );

    transform.position = loadedPosition;
  }

  private void SaveCurrentPosition()
  {
    if (AuthManager.Instance != null)
    {
      AuthManager.Instance.SavePlayerPosition(transform.position);
    }
  }
}

// ================================= Previous File ===================================================

// using UnityEngine;
// using UnityEngine.InputSystem;

// // Ensure the component is present on the gameobject the script is attached to
// // Uncomment this if you want to enforce the object to require the RB2D component to be already attached
// // [RequireComponent(typeof(Rigidbody2D))]
// public class PlayerController : MonoBehaviour
// {
//     public Vector2 MovementSpeed = new Vector2(100.0f, 100.0f); // 2D Movement speed to have independant axis speed
//     private new Rigidbody2D rigidbody2D; // Local rigidbody variable to hold a reference to the attached Rigidbody2D component
//     private Vector2 inputVector;

//     void Awake()
//     {
//         rigidbody2D = GetComponent<Rigidbody2D>(); // Get the Rigidbody2D component attached to the same GameObject
//     }

//     void Update()
//     {
//         float horizontalInput = Input.GetAxisRaw("Horizontal");
//         float verticalInput = Input.GetAxisRaw("Vertical");
//         inputVector = new Vector2(horizontalInput, verticalInput).normalized;
//     }

//     void FixedUpdate()
//     {
//         // Rigidbody2D affects physics so any ops on it should happen in FixedUpdate
//         // See why here: https://learn.unity.com/tutorial/update-and-fixedupdate#
//         //rigidbody2D.MovePosition(rigidbody2D.position + (inputVector * MovementSpeed * Time.fixedDeltaTime));
//         rigidbody2D.linearVelocity = inputVector * MovementSpeed;
//     }
// }
