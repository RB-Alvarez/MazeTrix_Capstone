using UnityEngine;
using Pathfinding;

public class EnemyScaling : MonoBehaviour
{
    [Header("Scaling Settings")]
    [Tooltip("How often (in seconds) to recalculate enemy stats")]
    public float updateInterval = 5f;

    [Tooltip("Health multiplier gained per minute survived")]
    public float healthScalePerMinute = 0.15f;

    [Tooltip("Move speed multiplier gained per minute survived")]
    public float speedScalePerMinute = 0.05f;

    [Tooltip("Attack cooldown reduction per minute (lower = faster attacks)")]
    public float attackCooldownReductionPerMinute = 0.05f;

    [Tooltip("Sight range increase per minute survived")]
    public float sightRangeIncreasePerMinute = 0.5f;

    [Tooltip("Maximum multiplier cap to prevent enemies from becoming impossible")]
    public float maxMultiplier = 3f;

    private EnemyCombat enemyCombat;
    private EnemyPatrol enemyPatrol;
    private AIPath aiPath;
    private SurvivalTime survivalTime;

    private float nextUpdateTime = 0f;

    void Start()
    {
        enemyCombat = GetComponent<EnemyCombat>();
        enemyPatrol = GetComponent<EnemyPatrol>();
        aiPath = GetComponent<AIPath>();

        survivalTime = FindObjectOfType<SurvivalTime>();

        if (survivalTime == null)
        {
            Debug.LogWarning($"EnemyScaling on {name}: SurvivalTime not found. Scaling disabled.");
            enabled = false;
            return;
        }

        ApplyScaling();
    }

    void Update()
    {
        if (Time.time >= nextUpdateTime)
        {
            ApplyScaling();
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    private void ApplyScaling()
    {
        float minutes = survivalTime.GetCurrentSurvivalTime() / 60f;
        float multiplier = Mathf.Clamp(1f + minutes * healthScalePerMinute, 1f, maxMultiplier);
        float speedMultiplier = Mathf.Clamp(1f + minutes * speedScalePerMinute, 1f, maxMultiplier);
        float cooldownMultiplier = Mathf.Clamp(1f - minutes * attackCooldownReductionPerMinute, 1f / maxMultiplier, 1f);
        float sightBonus = minutes * sightRangeIncreasePerMinute;

        if (enemyCombat != null)
        {
            enemyCombat.ApplyHealthScaling(multiplier);
        }

        if (enemyPatrol != null)
        {
            enemyPatrol.ApplyScaling(cooldownMultiplier, sightBonus);
        }

        if (aiPath != null)
        {
            aiPath.maxSpeed = enemyPatrol != null
                ? enemyPatrol.baseSpeed * speedMultiplier
                : aiPath.maxSpeed;
        }
    }
}
