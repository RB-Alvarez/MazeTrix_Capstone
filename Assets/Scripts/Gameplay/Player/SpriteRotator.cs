using UnityEngine;

public class SpriteRotator : MonoBehaviour
{
    // Fetch inputVector to determine movement direction
    private Vector2 inputVector;

    void Update()
    {
        // Compose inputVector from PlayerController's input
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        inputVector = new Vector2(horizontalInput, verticalInput).normalized;

        // Rotate the sprite on Z axis to face the movement direction
        if (inputVector.x > 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, 90); // Facing right
        }

        if (inputVector.x < 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, -90); // Facing left
        }

        if (inputVector.y > 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, 180); // Facing up
        }

        if (inputVector.y < 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0); // Facing down
        }
    }
}