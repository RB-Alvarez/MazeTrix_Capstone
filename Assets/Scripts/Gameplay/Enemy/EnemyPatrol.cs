using UnityEngine;
using Pathfinding;

public class EnemyPatrol : MonoBehaviour
{
    [SerializeField] private float returnDelay = 5f;
    [SerializeField] private float sightRange = 10f;
    private Vector3 homePoint;
    private Transform homeTarget;
    private AIDestinationSetter destinationSetter;
    private GameObject player;

    private bool hasLineOfSight = false;
    private float lostSightTimer = 0f;

    private float attackRange = 2f; // Distance at which the enemy can attack the player
    public float attackRate = 2f; // Attacks per second
    float nextAttackTime = 0f;

    public Animator animator; // Reference to the enemy's Animator component to play attack animation

    void Awake()
    {
        // Initialize runtime-only values here
        destinationSetter = GetComponent<AIDestinationSetter>();
        homePoint = transform.position;

        // Create a Transform target for returning home
        homeTarget = new GameObject(name + "_HomeTarget").transform;
        homeTarget.position = homePoint;
        homeTarget.gameObject.hideFlags = HideFlags.HideInHierarchy;
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void OnDestroy()
    {
        if (homeTarget != null)
            Destroy(homeTarget.gameObject);
    }

    void Update()
    {
        if (destinationSetter == null) return;
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

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

                    nextAttackTime = Time.time + 1f / attackRate;
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
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction.normalized, sightRange);

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
}
