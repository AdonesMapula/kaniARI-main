using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Stats")]
    public float moveSpeed = 1.75f;
    public float jumpForce = 3.5f;

    [Header("Custom Keyboard Controls")]
    public KeyCode moveLeftKey = KeyCode.A;
    public KeyCode moveRightKey = KeyCode.D;
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Controller Settings")]
    public string horizontalAxis = "Horizontal";
    public string jumpButton = "Jump";
    public float controllerDeadZone = 0.2f;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.05f;

    [Header("Health")]
    public int maxHealth = 3;
    public float damageCooldown = 1.5f;
    public SpriteRenderer[] heartIcons;

    [Header("Respawn")]
    public Transform respawnPoint;
    public float respawnDelay = 1.5f;
    public float hitAnimationTime = 1.5f;

    [Header("Game Over Overlay")]
    public float gameOverDelay = 1.5f;
    public string gameOverSceneName = "GameOver";

    private Rigidbody2D rb;
    private Animator anim;
    private Collider2D coll;
    private SpriteRenderer spriteRenderer;

    private bool isGrounded;
    private bool isDead;
    private bool canTakeDamage = true;
    private int currentHealth;

    private Vector3 startPosition;
    private RigidbodyType2D originalBodyType;
    private float defaultGravity;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        coll = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        rb.freezeRotation = true;
        originalBodyType = rb.bodyType;
        defaultGravity = rb.gravityScale;
        startPosition = transform.position;

        currentHealth = maxHealth;
        UpdateHeartsUI();

        if (coll != null)
        {
            PhysicsMaterial2D zeroFriction = new PhysicsMaterial2D("ZeroFriction")
            {
                friction = 0f,
                bounciness = 0f
            };
            coll.sharedMaterial = zeroFriction;
        }
    }

    private void Update()
    {
        if (isDead) return;

        HandleMovement();
        HandleJump();
    }

    private void HandleMovement()
    {
        float xInput = 0f;

        // Keyboard input
        if (Input.GetKey(moveLeftKey))
        {
            xInput = -1f;
        }
        else if (Input.GetKey(moveRightKey))
        {
            xInput = 1f;
        }

        // Controller input
        float controllerInput = Input.GetAxisRaw(horizontalAxis);

        // If controller is being used, override keyboard
        if (Mathf.Abs(controllerInput) > controllerDeadZone)
        {
            xInput = controllerInput;
        }

        float xVelocity = xInput * moveSpeed;
        rb.velocity = new Vector2(xVelocity, rb.velocity.y);

        // Flip sprite
        if (xInput < -0.01f)
            transform.localScale = new Vector3(-1f, 1f, 1f);
        else if (xInput > 0.01f)
            transform.localScale = new Vector3(1f, 1f, 1f);

        if (anim != null)
            anim.SetBool("Player_run", Mathf.Abs(xInput) > 0.01f && isGrounded);
    }

    private void HandleJump()
    {
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        else
            isGrounded = false;

        if (anim != null)
        {
            anim.SetBool("isGrounded", isGrounded);
            anim.SetFloat("yVelocity", rb.velocity.y);
        }

        bool keyboardJump = Input.GetKeyDown(jumpKey);
        bool controllerJump = Input.GetButtonDown(jumpButton);

        if ((keyboardJump || controllerJump) && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);

            if (anim != null)
                anim.SetTrigger("Player_jump");
        }
    }

    public void Die()
    {
        TakeDamage(1);
    }

    public void TakeDamage(int amount)
    {
        if (isDead || !canTakeDamage) return;

        currentHealth -= amount;
        if (currentHealth < 0)
            currentHealth = 0;

        UpdateHeartsUI();

        if (currentHealth <= 0)
            StartCoroutine(GameOverRoutine());
        else
            StartCoroutine(RespawnAfterHitRoutine());
    }

    private IEnumerator RespawnAfterHitRoutine()
    {
        canTakeDamage = false;

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Static;

        if (anim != null)
        {
            anim.ResetTrigger("Player_jump");
            anim.SetBool("Player_run", false);
            anim.SetBool("isGrounded", true);
            anim.SetFloat("yVelocity", 0f);
            anim.SetTrigger("Player_death");
        }

        yield return new WaitForSeconds(hitAnimationTime);

        ResetAllTraps();

        if (coll != null)
            coll.enabled = false;

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        Vector3 targetPos = respawnPoint != null ? respawnPoint.position : startPosition;
        transform.position = targetPos;

        rb.bodyType = originalBodyType;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = defaultGravity;

        yield return null;

        if (coll != null)
            coll.enabled = true;

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        ResetAnimationToIdle();

        yield return new WaitForSeconds(damageCooldown);
        canTakeDamage = true;
    }

    private void ResetAnimationToIdle()
    {
        if (anim == null) return;

        anim.ResetTrigger("Player_death");
        anim.ResetTrigger("Player_jump");
        anim.Play("Player_Idle", 0, 0f);
        anim.SetBool("Player_run", false);
        anim.SetBool("isGrounded", true);
        anim.SetFloat("yVelocity", 0f);
    }

    private IEnumerator GameOverRoutine()
    {
        isDead = true;
        canTakeDamage = false;

        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;

        if (anim != null)
            anim.SetTrigger("Player_death");

        yield return new WaitForSeconds(gameOverDelay);

        Scene gameOverScene = SceneManager.GetSceneByName(gameOverSceneName);
        if (!gameOverScene.isLoaded)
            yield return SceneManager.LoadSceneAsync(gameOverSceneName, LoadSceneMode.Additive);

        Time.timeScale = 0f;
    }

    private void UpdateHeartsUI()
    {
        if (heartIcons == null) return;

        for (int i = 0; i < heartIcons.Length; i++)
        {
            if (heartIcons[i] != null)
                heartIcons[i].enabled = i < currentHealth;
        }
    }

    private void ResetAllTraps()
    {
        foreach (var trap in FindObjectsOfType<TrapTrigger>()) trap.ResetTrap();
        foreach (var trap in FindObjectsOfType<dropPlatformer>()) trap.ResetTrap();
        foreach (var trap in FindObjectsOfType<SurpriseTrap>()) trap.ResetTrap();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Trap"))
        {
            TakeDamage(1);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Trap"))
        {
            TakeDamage(1);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}