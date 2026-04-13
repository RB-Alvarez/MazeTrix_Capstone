using UnityEngine;
using UnityEngine.Events;

// Manages player xp, level up, and applies stat boosts
public class PlayerXP : MonoBehaviour
{
    public static PlayerXP Instance { get; private set; }

    [Header("XP Settings")]
    [Tooltip("XP awarded for killing an enemy")]
    public int xpPerKill = 50;

    [Tooltip("XP awarded for collecting any item")]
    public int xpPerItem = 10;

    [Tooltip("Base XP required to reach level 2. Each subsequent level requires more XP.")]
    public int baseXPToLevelUp = 100;

    [Tooltip("XP requirement multiplier per level (e.g. 1.5 = 50% more XP needed each level)")]
    public float levelUpMultiplier = 1.5f;

    [Header("Level-Up Stat Bonuses (applied per level)")]
    [Tooltip("Max health increase per level-up")]
    public int healthBonusPerLevel = 10;

    [Tooltip("Attack damage increase per level-up")]
    public int attackBonusPerLevel = 5;

    [Tooltip("Attack cooldown reduction per level-up (subtracted from current cooldown)")]
    public float attackCooldownReductionPerLevel = 0.05f;

    [Tooltip("Minimum attack cooldown after reductions")]
    public float minAttackCooldown = 0.2f;

    [Header("Events")]
    public UnityEvent<int> OnXPGained;  // passes amount gained
    public UnityEvent<int> OnLevelUp;   // passes new level

    public int CurrentXP { get; private set; }
    public int CurrentLevel { get; private set; }
    public int XPToNextLevel => Mathf.RoundToInt(baseXPToLevelUp * Mathf.Pow(levelUpMultiplier, CurrentLevel - 1));

    private HealthBar healthBar;
    private PlayerAttack playerAttack;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // Cache player component references
        healthBar = GetComponent<HealthBar>();
        playerAttack = GetComponent<PlayerAttack>();

        // Load saved XP from session
        if (PlayerSessionData.Instance != null)
        {
            CurrentXP = PlayerSessionData.Instance.xp;
            CurrentLevel = Mathf.Max(1, PlayerSessionData.Instance.currentLevel);
        }
        else
        {
            CurrentXP = 0;
            CurrentLevel = 1;
        }

        // Apply cumulative stat bonuses for all levels already earned above 1
        ApplyCumulativeStatBonuses(CurrentLevel - 1);
    }

    /// <summary>
    /// Adds XP to the player and handles level-ups.
    /// </summary>
    public void AddXP(int amount)
    {
        if (amount <= 0) return;

        CurrentXP += amount;
        OnXPGained?.Invoke(amount);

        Debug.Log($"[XP] +{amount} XP | Total: {CurrentXP} | Level: {CurrentLevel} | Next level at: {XPToNextLevel}");
        FirebaseAIManager.Instance?.UpdatePlayerLog($"Gained {amount} XP. Total: {CurrentXP}.");

        // Check for level-up (may level up multiple times in one gain)
        while (CurrentXP >= XPToNextLevel)
        {
            CurrentXP -= XPToNextLevel;
            CurrentLevel++;

            ApplyLevelUpBonuses();

            Debug.Log($"[XP] Level up! Now level {CurrentLevel}.");
            FirebaseAIManager.Instance?.UpdatePlayerLog($"Level up! Now level {CurrentLevel}.");
            OnLevelUp?.Invoke(CurrentLevel);
        }

        SaveXP();
    }

    /// <summary>
    /// Convenience method called by EnemyCombat on enemy death.
    /// </summary>
    public void AddKillXP()
    {
        AddXP(xpPerKill);
    }

    /// <summary>
    /// Convenience method called by PlayerCollisions on item pickup.
    /// </summary>
    public void AddItemXP()
    {
        AddXP(xpPerItem);
    }

    /// <summary>
    /// Applies a single level-up's worth of stat bonuses.
    /// </summary>
    private void ApplyLevelUpBonuses()
    {
        // Increase max health and fully heal the player as a reward
        if (healthBar != null)
        {
            healthBar.maxHealth += healthBonusPerLevel;
            healthBar.Heal(healthBonusPerLevel);
            Debug.Log($"[XP] Max health increased to {healthBar.maxHealth}.");
        }

        // Increase attack damage and reduce cooldown
        if (playerAttack != null)
        {
            playerAttack.attackStat += attackBonusPerLevel;
            playerAttack.attackCooldown = Mathf.Max(
                minAttackCooldown,
                playerAttack.attackCooldown - attackCooldownReductionPerLevel
            );
            Debug.Log($"[XP] Attack damage: {playerAttack.attackStat}, cooldown: {playerAttack.attackCooldown:F2}s.");
        }
    }

    /// <summary>
    /// Re-applies stat bonuses for all levels earned above 1 (called on Start to
    /// restore bonuses after a scene reload or login with existing progress).
    /// </summary>
    private void ApplyCumulativeStatBonuses(int levelsEarned)
    {
        if (levelsEarned <= 0) return;

        if (healthBar != null)
        {
            healthBar.maxHealth += healthBonusPerLevel * levelsEarned;
            // Clamp current health to the new max in case it's somehow over
            healthBar.currentHealth = Mathf.Min(healthBar.currentHealth, healthBar.maxHealth);
        }

        if (playerAttack != null)
        {
            playerAttack.attackStat += attackBonusPerLevel * levelsEarned;
            playerAttack.attackCooldown = Mathf.Max(
                minAttackCooldown,
                playerAttack.attackCooldown - attackCooldownReductionPerLevel * levelsEarned
            );
        }
    }

    private void SaveXP()
    {
        if (PlayerSessionData.Instance != null)
        {
            PlayerSessionData.Instance.xp = CurrentXP;
            PlayerSessionData.Instance.currentLevel = CurrentLevel;
        }

        AuthManager.Instance?.SaveXP(CurrentXP, CurrentLevel);
    }

    public void ResetXP()
    {
        CurrentXP = 0;
        CurrentLevel = 1;
        SaveXP();
    }
}
