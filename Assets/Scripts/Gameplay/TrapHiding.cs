using UnityEngine;

public class TrapHiding : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            // Make the trap invisible on spawn
            Color color = spriteRenderer.color;
            color.a = 0f;
            spriteRenderer.color = color;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && spriteRenderer != null)
        {
            // Reveal the trap when the player steps on it
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }
    }
}
