using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public Animator animator;
    public Transform weapon;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;

    public int attackStat = 20;
    public float attackCooldown = 1f;
    float nextAttackTime = 0f;

    void Update()
    {
        if (Time.time >= nextAttackTime)
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                Attack();
                nextAttackTime = Time.time + attackCooldown;
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(weapon.position, attackRange);
    }
}
