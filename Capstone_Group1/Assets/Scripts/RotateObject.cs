using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RotateObject : MonoBehaviour
{
    public float rotationSpeed = 75f;
    public ParticleSystem particles;

    public float moveSpeed = 5f; // Adjust the speed as needed
    public float moveDistance = 2f; // Adjust the distance of the loop as needed

    private Vector2 initialPosition;
    private bool movingUp = true;

    void Start()
    {
        initialPosition = transform.position;
    }

    void Update()
    {
        //transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        MoveUpDownLoop();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        particles.Play();
        Destroy(gameObject);
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
    void MoveUpDownLoop()
    {
        Vector2 targetPosition = movingUp ? initialPosition + Vector2.up * moveDistance : initialPosition;

        float step = moveSpeed * Time.deltaTime;
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, step);

        if (Vector2.Distance(transform.position, targetPosition) < 0.01f)
        {
            movingUp = !movingUp;
        }
    }
}