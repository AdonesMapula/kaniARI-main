using UnityEngine;

public class OrcEnemy : MonoBehaviour
{
    [Header("Targeting")]
    public Transform player;
    public float sightRange = 1.2f; // Kept your custom range!
    public float attackRange = 0.3f; // Kept your custom range!

    [Header("Movement Speeds")]
    public float walkSpeed = 0.3f; // Kept your custom speed!
    public float runSpeed = 1.0f; // Kept your custom speed!

    [Header("Combat")]
    public float attackCooldown = 1.5f; // Wait 1.5 seconds between attacks
    private float nextAttackTime = 0f;  // Internal timer

    [Header("Patrol Limits (Invisible Fence)")]
    public Transform leftLimit;  
    public Transform rightLimit; 
    private Transform currentTarget;

    private Animator anim;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        currentTarget = rightLimit;

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }
    }

    void Update()
    {
        if (player == null || leftLimit == null || rightLimit == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        bool playerInSight = distanceToPlayer <= sightRange;
        bool playerInsideTerritory = player.position.x >= leftLimit.position.x && player.position.x <= rightLimit.position.x;

        if (distanceToPlayer <= attackRange)
        {
            // FIX 1: Make the Orc face the player when attacking
            spriteRenderer.flipX = player.position.x < transform.position.x;

            // FIX 2: Add the cooldown so the animation finishes
            if (Time.time >= nextAttackTime)
            {
                AttackPlayer();
                nextAttackTime = Time.time + attackCooldown; // Reset the timer
            }
            else
            {
                // Go back to idle/walking while waiting for the next swing
                anim.SetBool("IsAttacking", false);
            }
        }
        else if (playerInSight && playerInsideTerritory)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    void Patrol()
    {
        anim.SetBool("IsAttacking", false);
        anim.SetInteger("MoveState", 1); 

        Vector2 targetPos = new Vector2(currentTarget.position.x, transform.position.y);
        transform.position = Vector2.MoveTowards(transform.position, targetPos, walkSpeed * Time.deltaTime);

        spriteRenderer.flipX = currentTarget.position.x < transform.position.x;

        if (Mathf.Abs(transform.position.x - currentTarget.position.x) < 0.2f)
        {
            if (currentTarget == leftLimit)
            {
                currentTarget = rightLimit;
            }
            else
            {
                currentTarget = leftLimit;
            }
        }
    }

    void ChasePlayer()
    {
        anim.SetBool("IsAttacking", false);
        anim.SetInteger("MoveState", 2); 

        float clampedX = Mathf.Clamp(player.position.x, leftLimit.position.x, rightLimit.position.x);
        Vector2 targetPos = new Vector2(clampedX, transform.position.y);
        
        transform.position = Vector2.MoveTowards(transform.position, targetPos, runSpeed * Time.deltaTime);

        spriteRenderer.flipX = player.position.x < transform.position.x;
    }

    void AttackPlayer()
    {
        anim.SetInteger("MoveState", 0); 
        anim.SetBool("IsAttacking", true); 
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        if (leftLimit != null && rightLimit != null) 
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(new Vector2(leftLimit.position.x, transform.position.y - 1), new Vector2(leftLimit.position.x, transform.position.y + 1));
            Gizmos.DrawLine(new Vector2(rightLimit.position.x, transform.position.y - 1), new Vector2(rightLimit.position.x, transform.position.y + 1));
        }
    }
}