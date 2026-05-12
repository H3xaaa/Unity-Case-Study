using System.Collections;
using UnityEngine;

// ============================================================
// MPPlayerMovement.cs
// Multiplayer version of PlayerMovement — uses WASD keyboard
// instead of joystick. Works on PC, each player on their own PC.
// Attach to: Player prefab in MultiplayerGame scene
// ============================================================

public class MPPlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private int maxJumps = 2;

    [Header("Shooting")]
    public ProjectileBehaiour projectilePrefab;
    public Transform launchOffset;
    public AudioClip shootSFX;
    private bool canFire = true;

    [Header("References")]
    public GameObject fallDetector;

    // Private
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Animator anim;
    private AudioSource audioSource;
    private float dirX = 0f;
    private int jumpCount = 0;
    private bool isCrouching = false;
    private Vector3 respawnPoint;

    private enum MovementState { idle, running, jumping, falling, crouching, shooting }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        respawnPoint = transform.position;
    }

    private void Update()
    {
        HandleMovement();
        HandleJump();
        HandleCrouch();
        HandleShoot();
        UpdateAnimationState();

        // Keep fall detector in sync
        if (fallDetector != null)
            fallDetector.transform.position = new Vector2(
                transform.position.x,
                fallDetector.transform.position.y);
    }

    private void HandleMovement()
    {
        if (isCrouching) return;

        float h = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right arrows
        dirX = h * moveSpeed;
        rb.velocity = new Vector2(dirX, rb.velocity.y);

        // Flip sprite
        if (h > 0)
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        else if (h < 0)
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && jumpCount < maxJumps) // Space
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpCount++;
        }
    }

    private void HandleCrouch()
    {
        isCrouching = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
    }

    private void HandleShoot()
    {
        if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
        {
            FireBullet();
        }
    }

    public void FireBullet()
    {
        if (!canFire) return;

        if (anim != null) anim.SetTrigger("ShootTrigger");

        if (projectilePrefab != null && launchOffset != null)
            Instantiate(projectilePrefab, launchOffset.position, transform.rotation);

        if (audioSource != null && shootSFX != null)
        {
            audioSource.clip = shootSFX;
            audioSource.Play();
        }

        canFire = false;
        StartCoroutine(ResetFireCooldown());
    }

    private IEnumerator ResetFireCooldown()
    {
        yield return new WaitForSeconds(0.4f);
        canFire = true;
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Ground"))
            jumpCount = 0;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("FallDetector"))
            transform.position = respawnPoint;
    }

    private void UpdateAnimationState()
    {
        if (anim == null) return;

        MovementState state;

        if (dirX > 0f || dirX < 0f)
            state = MovementState.running;
        else
            state = MovementState.idle;

        if (rb.velocity.y > .1f)
            state = MovementState.jumping;
        else if (rb.velocity.y < -.1f)
            state = MovementState.falling;

        if (isCrouching)
            state = MovementState.crouching;

        anim.SetInteger("state", (int)state);
    }
}