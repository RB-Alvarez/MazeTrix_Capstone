using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public Animator animator; // Reference to the player's Animator component
    public Transform weapon;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;

    public int attackStat = 20;
    public float attackCooldown = 2f;
    float nextAttackTime = 0f;


    void Update()
    {
        // Check for attack input and cooldown
        if (Time.time >= nextAttackTime)
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                Attack();
                nextAttackTime = Time.time + 1f / attackCooldown;
            }
        }
    }

    void Attack()
    {
        Debug.Log("Player attacks!");
        animator.SetTrigger("AttackTrigger");

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(weapon.position, attackRange, enemyLayers);
        int enemiesHit = 0;
        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.CompareTag("Enemy"))
            {
                Debug.Log("Hit " + enemy.name);
                enemy.GetComponent<Rigidbody2D>()?.AddForce((enemy.transform.position - transform.position).normalized * 5f, ForceMode2D.Impulse);
                enemy.GetComponent<EnemyCombat>()?.TakeDamage(attackStat);
                enemiesHit++;
            }
        }
        if (enemiesHit > 0)
        {
            FirebaseAIManager.Instance?.UpdatePlayerLog($"Executed melee attack - {enemiesHit} hostile unit(s) damaged");
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
