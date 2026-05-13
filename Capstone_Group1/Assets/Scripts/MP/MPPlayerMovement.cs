using System.Collections;
using UnityEngine;

public class MPPlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private int maxJumps = 2;

    [Header("Shooting")]
    public GameObject projectilePrefab;
    public Transform launchOffset;
    public AudioClip shootSFX;
    private bool canFire = true;

    [Header("References")]
    public GameObject fallDetector;

    private Rigidbody2D rb;
    private Animator anim;
    private AudioSource audioSource;
    private float dirX = 0f;
    private int jumpCount = 0;
    private bool isCrouching = false;
    private Vector3 respawnPoint;
    private bool facingRight = true;

    private enum MovementState { idle, running, jumping, falling, crouching, shooting }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        respawnPoint = transform.position;

        // Detect initial facing from localScale
        facingRight = transform.localScale.x > 0;
    }

    private void Update()
    {
        HandleMovement();
        HandleJump();
        HandleCrouch();
        HandleShoot();
        UpdateAnimationState();

        if (fallDetector != null)
            fallDetector.transform.position = new Vector2(
                transform.position.x,
                fallDetector.transform.position.y);
    }

    private void HandleMovement()
    {
        if (isCrouching) return;

        float h = Input.GetAxisRaw("Horizontal");
        dirX = h * moveSpeed;
        rb.velocity = new Vector2(dirX, rb.velocity.y);

        if (h > 0 && !facingRight) Flip();
        else if (h < 0 && facingRight) Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 s = transform.localScale;
        s.x *= -1;
        transform.localScale = s;
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && jumpCount < maxJumps)
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
            FireBullet();
    }

    public void FireBullet()
    {
        if (!canFire || projectilePrefab == null || launchOffset == null) return;

        if (anim != null) anim.SetTrigger("ShootTrigger");

        // Spawn at launch offset, facing current direction
        Quaternion shootRot = facingRight
            ? launchOffset.rotation
            : Quaternion.Euler(0, 180f, launchOffset.rotation.eulerAngles.z);

        GameObject proj = Instantiate(projectilePrefab, launchOffset.position, shootRot);

        // Set owner role
        MPProjectileSpawner spawner = GetComponent<MPProjectileSpawner>();
        if (spawner != null)
        {
            MPProjectile mpProj = proj.GetComponent<MPProjectile>();
            if (mpProj != null) mpProj.ownerRole = spawner.ownerRole;
        }

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

        MovementState state = Mathf.Abs(dirX) > 0
            ? MovementState.running : MovementState.idle;

        if (rb.velocity.y > .1f) state = MovementState.jumping;
        else if (rb.velocity.y < -.1f) state = MovementState.falling;
        if (isCrouching) state = MovementState.crouching;

        anim.SetInteger("state", (int)state);
    }
}