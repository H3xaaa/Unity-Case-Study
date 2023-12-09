using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyLazer : MonoBehaviour
{
    public GameObject bullet;
    public Transform bulletPos;
    //public AudioClip yourSoundEffect; // Reference to your sound effect clip
    //private AudioSource audioSource;

    private float timer;
    private GameObject player;
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {

        float distance = Vector2.Distance(transform.position, player.transform.position);
        //Debug.Log(distance);

        if (distance < 15)
        {
            timer += Time.deltaTime;

            if (timer > 2)
            {
                timer = 0;
                shoot();
            }
        }
    }

    void shoot()
    {
       // audioSource.clip = yourSoundEffect;
       // audioSource.Play();
        Instantiate(bullet, bulletPos.position, Quaternion.identity);
    }
}
