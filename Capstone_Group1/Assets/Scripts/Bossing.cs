using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bossing : MonoBehaviour
{
    public GameObject player;
    public bool flip;
    public float speed;
    public float aggroRange = 7f; // Set the aggro range as needed
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        Vector3 scale = transform.localScale;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceToPlayer <= aggroRange)
        {
            if (player.transform.position.x > transform.position.x)
            {
                scale.x = Mathf.Abs(scale.x) * -1 * (flip ? -1 : 1);
                transform.Translate(x: speed * Time.deltaTime, y: 0, z: 0);
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
            else
            {
                scale.x = Mathf.Abs(scale.x) * (flip ? -1 : 1);
                transform.Translate(x: speed * Time.deltaTime * -1, y: 0, z: 0);
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }

            animator.SetBool("IsMoving", true);
        }
        else
        {
            animator.SetBool("IsMoving", false);
        }

        transform.localScale = scale;
    }
}