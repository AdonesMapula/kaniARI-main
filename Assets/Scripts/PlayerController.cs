using UnityEngine;
using UnityEngine.SceneManagement; // Required for reloading the scene
using System.Collections;
public class PlayerController : MonoBehaviour
{
    [Header("Respawn Settings")]
    public float respawnDelay = 1.5f; // Adjust this to match your animation length

    [Header("Movement Stats")]
    public float moveSpeed = 1.75f;
    public float jumpForce = 3.5f;

    [Header("Custom Controls")]
    public KeyCode moveLeftKey = KeyCode.A;
    public KeyCode moveRightKey = KeyCode.D;
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Detection")]
    public Transform groundCheck;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Animator anim;
    private Collider2D coll;

    private bool isGrounded;
    private bool isDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        coll = GetComponent<Collider2D>();

        rb.freezeRotation = true;

        // Remove wall sticking (zero friction)
        if (coll != null)
        {
            PhysicsMaterial2D zeroFrictionMaterial = new PhysicsMaterial2D("ZeroFriction");
            zeroFrictionMaterial.friction = 0f;
            zeroFrictionMaterial.bounciness = 0f;
            coll.sharedMaterial = zeroFrictionMaterial;
        }
    }

    void Update()
    {
        if (isDead) return;

        HandleMovement();
        HandleJump();
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

        rb.velocity = new Vector2(xVelocity, rb.velocity.y);

        if (anim != null)
            anim.SetBool("Player_run", xVelocity != 0);
    }

    void HandleJump()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);

        if (anim != null)
        {
            anim.SetBool("isGrounded", isGrounded);
            anim.SetFloat("yVelocity", rb.velocity.y);
        }

        if (Input.GetKeyDown(jumpKey) && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);

            if (anim != null)
                anim.SetTrigger("Player_jump");
        }
    }

    // 🔴 Called by Obstacle.cs
    public void Die()
    {
        if (isDead) return;

        isDead = true;
        rb.velocity = Vector2.zero;
        
        // Changing to 'Static' prevents the player from falling through the floor 
        // while the death animation plays
        rb.bodyType = RigidbodyType2D.Static;

        if (anim != null)
        {
            anim.SetTrigger("Player_death");
        }

        // Start the countdown to respawn
        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        // Wait for the specified seconds
        yield return new WaitForSeconds(respawnDelay);

        // Reload the current active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}