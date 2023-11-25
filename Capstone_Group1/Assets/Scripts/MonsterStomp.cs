using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterStomp : MonoBehaviour
{
    public void OnCollisionEnter2D(Collision2D collision)
    {
       if(collision.collider.gameObject.tag == "Weak Point")
        {
            collision.gameObject.GetComponent<HealthChar>().TakeDamage(1);
        }
    }
}
