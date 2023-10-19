using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
     
    private SpriteRenderer sprite;
    private Animator anim;

    private float dirX = 0f; 
    private bool isJumping;
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private int maxJumps = 2;

    private enum MovementState { idle, running, jumping, falling, crouching }
    private MovementState state = MovementState.idle;
    private bool isCrouching = false;
    private int jumpCount;
    private Vector3 respawnPoint;
    public GameObject fallDetector;


   
   // Start is called before the first frame update
    private void Start()
    {
       rb = GetComponent<Rigidbody2D>();   
       sprite =GetComponent<SpriteRenderer>();
       anim = GetComponent<Animator>();
      
       respawnPoint = transform.position;
       Debug.Log("Respawn point set to " + respawnPoint.ToString());
    }

    // Update is called once per frame
    private void Update()
    {
        if (!isCrouching)
        {
            dirX = Input.GetAxisRaw("Horizontal");
            rb.velocity = new Vector2(dirX * moveSpeed, rb.velocity.y);
         
            if (Input.GetButtonDown("Jump") && jumpCount < maxJumps)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                isJumping = true;
                jumpCount++;
            }  
        }

       if (Input.GetKeyDown(KeyCode.S))
        {
            isCrouching = true;
        }
        else if (Input.GetKeyUp(KeyCode.S))
        {
            isCrouching = false;
        }

        UpdateAnimationState();
    }   

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            isJumping = false;
            jumpCount = 0;
       
        }
    }

    

    private void UpdateAnimationState()
    {   
        MovementState state;    

        if (dirX > 0f)
        {
            state = MovementState.running; 
            sprite.flipX = false;
        }
        else if (dirX < 0f)
        {
            state = MovementState.running;
            sprite.flipX = true; 
        }
        else
        {
            state = MovementState.idle;
        }

        if(rb.velocity.y > .1f)
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
        if(collision.tag == "FallDetector")
        {
            Debug.Log("Player has collided with FallDetector.");
            transform.position = respawnPoint;  
        }
    }
}