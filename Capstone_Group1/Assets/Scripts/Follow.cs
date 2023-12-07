using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follow : MonoBehaviour
{
    [SerializeField] float enemySpeed = 1f;       //HERE are the presets for your enemy! feel free to change any of the values here
    [SerializeField] float startFollowRange = 6f; //only in the inspector though because the inspectors values override these values
    [SerializeField] float damageRange = 0.5f;
    [SerializeField] float damageAgainDelay = 1f;

    float distance = 0f;
    bool damagePlayerCalled = false;
    bool damagedPlayer = false;

    GameObject player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

    }

    void Update()
    {
        CheckForPlayer();
    }

    private void CheckForPlayer()
    {
        distance = Vector2.Distance(transform.position, player.transform.position); //calculate the distance

        if (distance < startFollowRange) //Check if player is in following range
        {
            MoveToPlayer();
        }
    }

    private void MoveToPlayer()
    {
        transform.position = Vector2.MoveTowards(transform.position, player.transform.position, enemySpeed * Time.deltaTime); //simply moves from our position to player position

        if (distance < damageRange && !damagePlayerCalled) //We say "&&" to say "and" in an if statement, and i check for if damagePlayerCalled == true
        {                                                  //so we don't call the coroutine multiple times
            StartCoroutine(DamagePlayer());
        }
    }

    private IEnumerator DamagePlayer()
    {
        damagePlayerCalled = true;

        if (damagedPlayer) { yield return new WaitForSeconds(damageAgainDelay); damagedPlayer = false; } //If first time getting in range damage immediately

        //Debug.Log("Damage player!"); //You can put whatever you want here
        damagedPlayer = true;
        damagePlayerCalled = false;
    }
}


