using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

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
    public float groundCheckRadius = 0.05f;

    [Header("Health")]
    // maxHealth is automatically set to the number of heart segments found
    // You can still override it here — it will be clamped to bar length
    public int maxHealth = 10;
    public float damageCooldown = 1.5f;

    [Header("Health Bar — assign the parent GameObjects")]
    [Tooltip("Drag the 'heart_0' parent here (ON/filled hearts)")]
    public Transform heartsOnParent;
    [Tooltip("Drag the 'off' parent here (OFF/empty hearts)")]
    public Transform heartsOffParent;

    [Header("Respawn")]
    public Transform respawnPoint;
    public float respawnDelay = 1.5f;
    public float hitAnimationTime = 1.5f;

    [Header("Game Over Overlay")]
    public float gameOverDelay = 1.5f;
    public string gameOverSceneName = "GameOver";

    [Header("Stats")]
    public int deathCount = 0;

    [Header("UI")]
    public Text deathCountText;

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

    private SpriteRenderer[] srOn;
    private SpriteRenderer[] srOff;

    private void Awake()
    {
        // --- Auto-find parents by name if not assigned in Inspector ---
        if (heartsOnParent == null)
        {
            GameObject found = GameObject.Find("heart_0");
            if (found != null) heartsOnParent = found.transform;
        }
        if (heartsOffParent == null)
        {
            GameObject found = GameObject.Find("off");
            if (found != null) heartsOffParent = found.transform;
        }

        // Collect direct children only (not grandchildren) as SpriteRenderers
        srOn = GetDirectChildRenderers(heartsOnParent);
        srOff = GetDirectChildRenderers(heartsOffParent);

        // Lock maxHealth to however many segments exist in the bar
        int barLength = Mathf.Max(srOn.Length, srOff.Length);
        if (barLength > 0)
            maxHealth = barLength;

        Debug.Log($"[HealthBar] ON segments: {srOn.Length} | OFF segments: {srOff.Length} | maxHealth set to: {maxHealth}");
    }

    private SpriteRenderer[] GetDirectChildRenderers(Transform parent)
    {
        if (parent == null) return new SpriteRenderer[0];

        var list = new System.Collections.Generic.List<SpriteRenderer>();
        foreach (Transform child in parent)
        {
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr != null) list.Add(sr);
        }
        return list.ToArray();
    }

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
        UpdateHealthBar();
        UpdateDeathUI();

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
            anim.SetBool("Player_run", xVelocity != 0f && isGrounded);

        if (xVelocity != 0f && isGrounded)
        {
            if (!AudioManager.Instance.sfxSource.isPlaying)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.walk);
        }
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
            anim.SetFloat("yVelocity", rb.linearVelocity.y);
        }

        if (Input.GetKeyDown(jumpKey) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            if (anim != null) anim.SetTrigger("Player_jump");
            AudioManager.Instance.PlaySFX(AudioManager.Instance.jump);
        }
    }

    public void Die() => TakeDamage(1);

    public void TakeDamage(int amount)
    {
        if (isDead || !canTakeDamage) return;

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            deathCount++;
            UpdateDeathUI();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayRandomDeathSound();
            StartCoroutine(GameOverRoutine());
        }
        else
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayRandomDeathSound();
            StartCoroutine(RespawnAfterHitRoutine());
        }
    }

    /// <summary>
    /// Slot i is filled when i < currentHealth.
    /// ON  sprite: alpha 1 = filled, 0 = empty
    /// OFF sprite: alpha 0 = filled, 1 = empty
    /// </summary>
    private void UpdateHealthBar()
    {
        int total = Mathf.Max(srOn.Length, srOff.Length);

        for (int i = 0; i < total; i++)
        {
            bool filled = i < currentHealth;

            if (i < srOn.Length && srOn[i] != null)
            {
                Color c = srOn[i].color;
                c.a = filled ? 1f : 0f;
                srOn[i].color = c;
            }

            if (i < srOff.Length && srOff[i] != null)
            {
                Color c = srOff[i].color;
                c.a = filled ? 0f : 1f;
                srOff[i].color = c;
            }
        }
    }

    private void UpdateDeathUI()
    {
        if (deathCountText != null)
            deathCountText.text = "Deaths: " + deathCount;
    }

    private IEnumerator RespawnAfterHitRoutine()
    {
        canTakeDamage = false;

        rb.linearVelocity = Vector2.zero;
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

        if (coll != null) coll.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        transform.position = respawnPoint != null ? respawnPoint.position : startPosition;

        rb.bodyType = originalBodyType;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = defaultGravity;

        if (coll != null) coll.enabled = true;
        if (spriteRenderer != null) spriteRenderer.enabled = true;

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

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;

        if (anim != null) anim.SetTrigger("Player_death");

        yield return new WaitForSeconds(gameOverDelay);

        UpdateHealthBar();

        Scene gameOverScene = SceneManager.GetSceneByName(gameOverSceneName);
        if (!gameOverScene.isLoaded)
            yield return SceneManager.LoadSceneAsync(gameOverSceneName, LoadSceneMode.Additive);

        Time.timeScale = 0f;
    }

    private void ResetAllTraps()
    {
        foreach (var trap in FindObjectsOfType<TrapTrigger>()) trap.ResetTrap();
        foreach (var trap in FindObjectsOfType<dropPlatformer>()) trap.ResetTrap();
        foreach (var trap in FindObjectsOfType<SurpriseTrap>()) trap.ResetTrap();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Trap")) TakeDamage(1);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Trap")) TakeDamage(1);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}