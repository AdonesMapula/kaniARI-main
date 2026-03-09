using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Stats")]
    public float moveSpeed = 5f;      // Reduced speed
    public float jumpForce = 8f;      // Reduced jump force
    public float climbSpeed = 4f;

    [Header("Custom Controls")]
    public KeyCode moveLeftKey = KeyCode.A;
    public KeyCode moveRightKey = KeyCode.D;
    public KeyCode moveUpKey = KeyCode.W;
    public KeyCode moveDownKey = KeyCode.S;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode punchKey = KeyCode.E;
    public KeyCode attackKey = KeyCode.Mouse0; // Left Click

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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        defaultGravity = rb.gravityScale;

        // FIX FOR ROLLING: This stops the physics engine from rotating the sprite
        rb.freezeRotation = true; 
    }

    void Update()
    {
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
            transform.localScale = new Vector3(-1, 1, 1); // Flip Left
        }
        else if (Input.GetKey(moveRightKey))
        {
            xVelocity = moveSpeed;
            transform.localScale = new Vector3(1, 1, 1); // Flip Right
        }

        rb.linearVelocity = new Vector2(xVelocity, rb.linearVelocity.y);
        anim.SetBool("Player_run", xVelocity != 0);
    }

    void HandleJump()
{
    // 1. Check if we are touching the ground
    isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    
    // Update animations
    anim.SetBool("isGrounded", isGrounded);
    anim.SetFloat("yVelocity", rb.linearVelocity.y);

    // 2. Reset jump count when grounded
    if (isGrounded && rb.linearVelocity.y <= 0.1f) 
    {
        jumpCount = 0;
    }

    // 3. Jump Trigger
    if (Input.GetKeyDown(jumpKey))
    {
        // Check if grounded OR if we still have jumps left (e.g., jumpCount < 2 for double jump)
        if (isGrounded || jumpCount < 150) 
        {
            // Apply jump force
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            
            // Trigger correct animation
            if (isGrounded)
                anim.SetTrigger("Player_jump");
            else
                anim.SetTrigger("Player_doublejump");

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
        // Primary Attack
        if (Input.GetKeyDown(attackKey))
        {
            int attackNum = Random.Range(1, 4);
            if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
                anim.SetTrigger("Player_run_attack");
            else
                anim.SetTrigger("Player_attack" + attackNum);
        }

        // Punch Action
        if (Input.GetKeyDown(punchKey))
            anim.SetTrigger("Player_punch");
    }
}