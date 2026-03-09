using UnityEngine;
using System.Collections; 
using UnityEngine.SceneManagement; 

public class PlayerController : MonoBehaviour
{
    [Header("Movement Stats")]
    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public float climbSpeed = 4f;
    public int maxJumps = 2; // NEW: Easily change max jumps in the Inspector!

    [Header("Custom Controls")]
    public KeyCode moveLeftKey = KeyCode.A;
    public KeyCode moveRightKey = KeyCode.D;
    public KeyCode moveUpKey = KeyCode.W;
    public KeyCode moveDownKey = KeyCode.S;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode punchKey = KeyCode.E;
    public KeyCode attackKey = KeyCode.Mouse0; 

    [Header("Detection")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public LayerMask ladderLayer;

    private Rigidbody2D rb;
    private Animator anim;
    private bool isGrounded;
    private bool isClimbing;
    private int jumpCount = 0;
    private float defaultGravity;
    private bool isDead = false; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        defaultGravity = rb.gravityScale;

        rb.freezeRotation = true; 
    }

    void Update()
    {
        if (isDead) return; 

        HandleMovement();
        HandleJump();
        HandleClimbing();
        HandleActions();
    }

    void HandleMovement()
    {
        float xVelocity = 0;

        if (Input.GetKey(moveLeftKey))
        {
            xVelocity = -moveSpeed;
            transform.localScale = new Vector3(-1, 1, 1); 
        }
        else if (Input.GetKey(moveRightKey))
        {
            xVelocity = moveSpeed;
            transform.localScale = new Vector3(1, 1, 1); 
        }

        rb.linearVelocity = new Vector2(xVelocity, rb.linearVelocity.y);
        anim.SetBool("Player_run", xVelocity != 0);
    }

    void HandleJump()
    {
        // 1. Check if we are touching the ground
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
        
        // Update animations
        if (anim != null)
        {
            anim.SetBool("isGrounded", isGrounded);
            anim.SetFloat("yVelocity", rb.linearVelocity.y);
        }

        // 2. Safely reset jump count when we land
        // (We use a slightly higher number like 0.5f to account for physics jitters)
        if (isGrounded && rb.linearVelocity.y <= 0.5f) 
        {
            jumpCount = 0;
        }

        // 3. Jump Trigger
        if (Input.GetKeyDown(jumpKey))
        {
            if (isGrounded)
            {
                // -- GROUND JUMP --
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                if (anim != null) anim.SetTrigger("Player_jump");
                
                // Explicitly set to 1 because we just used our first jump
                jumpCount = 1; 
            }
            else if (jumpCount < maxJumps)
            {
                // -- AIR JUMP (Double Jump) --
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                if (anim != null) anim.SetTrigger("Player_doublejump");
                
                // Increase the counter
                jumpCount++; 
            }
        }
    }

    void HandleClimbing()
    {
        isClimbing = rb.IsTouchingLayers(ladderLayer);
        
        if (isClimbing)
        {
            float vVelocity = 0;
            if (Input.GetKey(moveUpKey)) vVelocity = climbSpeed;
            else if (Input.GetKey(moveDownKey)) vVelocity = -climbSpeed;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, vVelocity);
            rb.gravityScale = 0;
            anim.SetBool("Player_climb", true);
        }
        else
        {
            rb.gravityScale = defaultGravity;
            anim.SetBool("Player_climb", false);
        }
    }

    void HandleActions()
    {
        if (Input.GetKeyDown(attackKey))
        {
            int attackNum = Random.Range(1, 4);
            if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
                anim.SetTrigger("Player_run_attack");
            else
                anim.SetTrigger("Player_attack" + attackNum);
        }

        if (Input.GetKeyDown(punchKey))
            anim.SetTrigger("Player_punch");
    }

    public void Die()
    {
        if (isDead) return; 

        isDead = true;
        rb.linearVelocity = Vector2.zero; 
        
        if (anim != null) anim.SetTrigger("Player_death"); 

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(1.5f); 
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}