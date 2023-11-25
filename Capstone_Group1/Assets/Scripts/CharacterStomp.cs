using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterStomp : MonoBehaviour
{
    /*public void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.gameObject.tag == "Player")
        {
            collision.gameObject.GetComponent<HealthPlayer>().TakeDamage(1);
        }
    }*/
    public int damageAmount = 1; // Adjust the damage amount as needed

    void OnTriggerEnter2D(Collider2D other)
    {
        // Assuming the player has a "Player" tag
        if (other.CompareTag("Player"))
        {
            // Access the PlayerHealth component of the player and call TakeDamage
            other.gameObject.GetComponent<HealthPlayer>().TakeDamage(damageAmount);
            
            //Destroy the laser projectile

        }
    }

}
