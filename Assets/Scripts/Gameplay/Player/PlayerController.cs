using UnityEngine;
using UnityEngine.InputSystem;

// Ensure the component is present on the gameobject the script is attached to
// Uncomment this if you want to enforce the object to require the RB2D component to be already attached
// [RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public Vector2 MovementSpeed = new Vector2(100.0f, 100.0f); // 2D Movement speed to have independant axis speed
    private new Rigidbody2D rigidbody2D; // Local rigidbody variable to hold a reference to the attached Rigidbody2D component
    private Vector2 inputVector;

    void Awake()
    {
        rigidbody2D = GetComponent<Rigidbody2D>(); // Get the Rigidbody2D component attached to the same GameObject
    }

    void Update()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        inputVector = new Vector2(horizontalInput, verticalInput).normalized;
    }

    void FixedUpdate()
    {
        // Rigidbody2D affects physics so any ops on it should happen in FixedUpdate
        // See why here: https://learn.unity.com/tutorial/update-and-fixedupdate#
        //rigidbody2D.MovePosition(rigidbody2D.position + (inputVector * MovementSpeed * Time.fixedDeltaTime));
        rigidbody2D.linearVelocity = inputVector * MovementSpeed;
    }
}
