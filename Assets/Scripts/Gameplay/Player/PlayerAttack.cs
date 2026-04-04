using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public Animator animator; // Reference to the player's Animator component
    public Transform weapon;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;

    public int attackStat = 10;
    public float attackRate = 2f; // Attacks per second
    float nextAttackTime = 0f;


    void Update()
    {
        // Check for attack input and cooldown
        if (Time.time >= nextAttackTime)
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                Attack();
                nextAttackTime = Time.time + 1f / attackRate;
            }
        }
    }

    void Attack()
    {
        // Placeholder for attack logic
        Debug.Log("Player attacks!");

        // Play attack animation
        animator.SetTrigger("AttackTrigger");

        // Detect hits on enemies using CompareTag
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(weapon.position, attackRange, enemyLayers);
            foreach (Collider2D enemy in hitEnemies)
            {
                Debug.Log("Detected " + enemy.name + " in attack range.");

            // Deal damage to enemies
                if (enemy.CompareTag("Enemy"))
                {
                        Debug.Log("Hit " + enemy.name);
                        // apply knockback
                        enemy.GetComponent<Rigidbody2D>()?.AddForce((enemy.transform.position - transform.position).normalized * 5f, ForceMode2D.Impulse);

                // apply damage
                enemy.GetComponent<EnemyCombat>()?.TakeDamage(attackStat);
                }
            }
    }

    void OnDrawGizmosSelected()
    {
        if (weapon == null) return;
        // Visualize the attack range in the editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(weapon.position, attackRange);
    }
}
