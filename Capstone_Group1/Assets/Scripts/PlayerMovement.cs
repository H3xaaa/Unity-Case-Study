using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    #region
    public Joystick joystick;

    //shoot variables
    public ProjectileBehaiour ProjectilePrefab;
    public Transform LaunchOffset;
    private bool canFire = true;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Animator anim;

    private float dirX = 0f;
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private int maxJumps = 2;

    private enum MovementState { idle, running, jumping, falling, crouching, shooting}
    private bool isCrouching = false;
    private int jumpCount;
    private Vector3 respawnPoint;
    public GameObject fallDetector;
    #endregion


    // Start is called before the first frame update
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        respawnPoint = transform.position;
        Debug.Log("Respawn point set to " + respawnPoint.ToString());
    }

    // Update is called once per frame
    private void Update()
    {
        float verticalMove = joystick.Vertical;
        if (!isCrouching)
        {
            if (joystick.Horizontal >= 0.2f)
            {
                dirX = moveSpeed;
            }
            else if (joystick.Horizontal <= -.2f)
            {
                dirX = -moveSpeed;
            }
            else
            {
                dirX = 0f;
            }

             rb.velocity = new Vector2(dirX * moveSpeed, rb.velocity.y);

            if (verticalMove >= 0.5f && jumpCount < maxJumps)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                jumpCount++;
            }
        }

        if (verticalMove <= -0.5f)
        {
            isCrouching = true;
        }
        else
        {
            isCrouching = false;
        }

        UpdateAnimationState();
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            //isJumping = false;
            jumpCount = 0;
        }
    }


    private void UpdateAnimationState()
    {
        MovementState state;
        if (dirX > 0f)
        {
            state = MovementState.running;
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
        else if (dirX < 0f)
        {
            state = MovementState.running;
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
        else
        {
            state = MovementState.idle;
        }

        if (rb.velocity.y > .1f)
        {
            state = MovementState.jumping;
        }
        else if (rb.velocity.y < -.1f)
        {
            state = MovementState.falling;
        }
        if (isCrouching)
        {
            state = MovementState.crouching;
        }
        anim.SetInteger("state", (int)state);
        fallDetector.transform.position = new Vector2(transform.position.x, fallDetector.transform.position.y);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "FallDetector")
        {
            Debug.Log("Player has collided with FallDetector.");
            transform.position = respawnPoint;
        }
    }

    //Shoot Codes
    public void FireButton()
    {
        if (IsIdle() && !IsJumping() && !isCrouching && canFire)
        {
            // Trigger the shoot animation
            anim.SetTrigger("ShootTrigger");

            // Instantiate the projectile
            Instantiate(ProjectilePrefab, LaunchOffset.position, transform.rotation);

            canFire = false;
            StartCoroutine(ResetFireCooldown());
        }
    }

    private IEnumerator ResetFireCooldown()
    {
        yield return new WaitForSeconds(0.5f);
        canFire = true;
    }
    private bool IsIdle()
    {
        // Check if the player is in the idle state
        return anim.GetCurrentAnimatorStateInfo(0).IsName("Player_Idle");
    }

    private bool IsJumping()
    {
        // Check if the player is in the jumping state
        return anim.GetCurrentAnimatorStateInfo(0).IsName("Player_Jumping");
    }
   
}