using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Stats")]
    public float moveSpeed = 1.75f;
    public float jumpForce = 3.5f;

    [Header("Custom Controls")]
    public KeyCode moveLeftKey = KeyCode.A;
    public KeyCode moveRightKey = KeyCode.D;
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public LayerMask groundLayer;

    [Header("Health")]
    public int maxHealth = 3;
    public float damageCooldown = 0.4f;
    public SpriteRenderer[] heartIcons;

    [Header("Respawn")]
    public Transform respawnPoint;      // Optional: assign a spawn point object
    public float respawnDelay = 0.1f;   // Delay before teleporting after hit

    [Header("Game Over Overlay")]
    public float gameOverDelay = 1.5f;
    public string gameOverSceneName = "GameOver";

    private Rigidbody2D rb;
    private Animator anim;
    private Collider2D coll;

    private bool isGrounded;
    private bool isDead;
    private bool canTakeDamage = true;
    private int currentHealth;

    private Vector3 startPosition;
    private RigidbodyType2D originalBodyType;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        coll = GetComponent<Collider2D>();

        rb.freezeRotation = true;
        originalBodyType = rb.bodyType;
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
        float xVelocity = 0f;

        if (Input.GetKey(moveLeftKey))
        {
            xVelocity = -moveSpeed;
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }
        else if (Input.GetKey(moveRightKey))
        {
            xVelocity = moveSpeed;
            transform.localScale = new Vector3(1f, 1f, 1f);
        }

        rb.linearVelocity = new Vector2(xVelocity, rb.linearVelocity.y);

        if (anim != null)
            anim.SetBool("Player_run", xVelocity != 0f);
    }

    private void HandleJump()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);

        if (anim != null)
        {
            anim.SetBool("isGrounded", isGrounded);
            anim.SetFloat("yVelocity", rb.linearVelocity.y);
        }

        if (Input.GetKeyDown(jumpKey) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            if (anim != null)
                anim.SetTrigger("Player_jump");
        }
    }

    // Called by Obstacle.cs
    public void Die()
    {
        TakeDamage(1);
    }

    public void TakeDamage(int amount)
    {
        if (isDead || !canTakeDamage) return;

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        UpdateHeartsUI();

        if (currentHealth <= 0)
            StartCoroutine(GameOverRoutine());
        else
            StartCoroutine(RespawnAfterHitRoutine());
    }

    private IEnumerator RespawnAfterHitRoutine()
    {
        canTakeDamage = false;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;

        yield return new WaitForSeconds(respawnDelay);

        Vector3 targetPos = respawnPoint != null ? respawnPoint.position : startPosition;
        transform.position = targetPos;

        rb.bodyType = originalBodyType;
        rb.linearVelocity = Vector2.zero;

        // Brief invulnerability after respawn
        yield return new WaitForSeconds(damageCooldown);
        canTakeDamage = true;
    }

    private IEnumerator GameOverRoutine()
    {
        isDead = true;
        canTakeDamage = false;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;

        if (anim != null)
            anim.SetTrigger("Player_death");

        yield return new WaitForSeconds(gameOverDelay);

        GameOverState.lastGameplayScene = SceneManager.GetActiveScene().name;

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
}