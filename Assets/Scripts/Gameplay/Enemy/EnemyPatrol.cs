using UnityEngine;
using Pathfinding;

public class EnemyPatrol : MonoBehaviour
{
    [SerializeField] private float returnDelay = 5f;
    [SerializeField] private float sightRange = 10f;
    private float currentSightRange;
    private Vector3 homePoint;
    private Transform homeTarget;
    private AIDestinationSetter destinationSetter;
    private AIPath aiPath;
    private GameObject player;

    private bool hasLineOfSight = false;
    private float lostSightTimer = 0f;

    private float attackRange = 2f; // Distance at which the enemy can attack the player
    public float attackCooldown = 2f;
    private float currentAttackCooldown;
    float nextAttackTime = 0f;

    [HideInInspector] public float baseSpeed;

    public Animator animator; // Reference to the enemy's Animator component to play attack animation

    [Header("Grid Bounds")]
    [Tooltip("Reference to the ProceduralGridMover (auto-assigned if null)")]
    public ProceduralGridMover gridMover;

    [Tooltip("Pause movement if enemy falls outside grid bounds")]
    public bool checkGridBounds = true;

    private bool wasInsideGrid = true;

    void Awake()
    {
        // Initialize runtime-only values here
        destinationSetter = GetComponent<AIDestinationSetter>();
        aiPath = GetComponent<AIPath>();
        homePoint = transform.position;

        // Store base speed from AIPath for scaling
        if (aiPath != null)
        {
            baseSpeed = aiPath.maxSpeed;
        }

        currentSightRange = sightRange;
        currentAttackCooldown = attackCooldown;

        // Create a Transform target for returning home
        homeTarget = new GameObject(name + "_HomeTarget").transform;
        homeTarget.position = homePoint;
        homeTarget.gameObject.hideFlags = HideFlags.HideInHierarchy;
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        // Try to find ProceduralGridMover if not assigned
        if (gridMover == null && checkGridBounds)
        {
            gridMover = FindObjectOfType<ProceduralGridMover>();
            if (gridMover == null)
            {
                Debug.LogWarning($"EnemyPatrol on {name}: checkGridBounds is enabled but ProceduralGridMover not found. Grid bounds checking disabled.");
                checkGridBounds = false;
            }
        }
    }

    void OnDestroy()
    {
        if (homeTarget != null)
            Destroy(homeTarget.gameObject);
    }

    /// <summary>
    /// Called by EnemyScaling to adjust attack speed and sight range based on survival time.
    /// </summary>
    public void ApplyScaling(float cooldownMultiplier, float sightBonus)
    {
        currentAttackCooldown = attackCooldown * cooldownMultiplier;
        currentSightRange = sightRange + sightBonus;
    }

    void Update()
    {
        if (destinationSetter == null) return;
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        // Check if enemy is inside grid bounds
        bool isInsideGrid = IsInsideGridBounds();

        // If enemy fell outside the grid, pause movement
        if (checkGridBounds && !isInsideGrid)
        {
            if (wasInsideGrid)
            {
                Debug.LogWarning($"Enemy {name} fell outside ProceduralGridMover bounds at {transform.position}. Pausing movement.");
            }

            // Pause the AIPath component if available
            if (aiPath != null)
            {
                aiPath.canMove = false;
            }

            // Clear destination to stop pathfinding
            destinationSetter.target = null;

            wasInsideGrid = false;
            return; // Don't process normal behavior while outside grid
        }
        else if (checkGridBounds && !wasInsideGrid && isInsideGrid)
        {
            // Enemy re-entered the grid
            Debug.Log($"Enemy {name} re-entered grid bounds. Resuming movement.");
            if (aiPath != null)
            {
                aiPath.canMove = true;
            }
            wasInsideGrid = true;
        }

        if (hasLineOfSight && player != null)
        {
            // Reset timer and chase player immediately
            lostSightTimer = 0f;
            destinationSetter.target = player.transform;

            // Play attack animation if close enough to the player
            if (Vector3.Distance(transform.position, player.transform.position) <= attackRange)
            {
                if (Time.time >= nextAttackTime)
                {
                    animator.SetTrigger("AttackTrigger");
                    // apply knockback
                    player.GetComponent<Rigidbody2D>()?.AddForce((player.transform.position - transform.position).normalized * 5f, ForceMode2D.Impulse);

                    nextAttackTime = Time.time + 1f / currentAttackCooldown;
                }
            }
        }
        else
        {
            // Increment timer while player is out of sight
            lostSightTimer += Time.deltaTime;

            if (lostSightTimer >= returnDelay)
            {
                // After delay, return to home if not already there
                if (Vector3.Distance(transform.position, homePoint) > 0.1f)
                {
                    destinationSetter.target = homeTarget;
                }
                else
                {
                    destinationSetter.target = null; // At home, stop moving
                }
            }
            else
            {
                // During the delay, do not move toward player or home
                destinationSetter.target = null;
            }
        }
    }

    private void FixedUpdate()
    {
        if (player == null)
        {
            hasLineOfSight = false;
            return;
        }

        Vector2 direction = (player.transform.position - transform.position);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction.normalized, currentSightRange);

        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            hasLineOfSight = true;
            Debug.DrawLine(transform.position, player.transform.position, Color.green);
        }
        else
        {
            hasLineOfSight = false;
            Debug.DrawLine(transform.position, player.transform.position, Color.red);
        }
    }

    private bool IsInsideGridBounds()
    {
        // If grid bounds checking is disabled, always return true
        if (!checkGridBounds || gridMover == null || gridMover.graph == null)
        {
            return true;
        }

        // Get the grid graph
        var grid = gridMover.graph;

        // Transform the enemy position to graph space
        var graphPosition = grid.transform.InverseTransform(transform.position);

        // Check if the position is within the grid bounds
        // In graph space, nodes are laid out in the XZ plane
        // The graph extends from (0,0) to (width, depth)
        bool insideX = graphPosition.x >= 0 && graphPosition.x <= grid.width;
        bool insideZ = graphPosition.z >= 0 && graphPosition.z <= grid.depth;

        return insideX && insideZ;
    }
}
