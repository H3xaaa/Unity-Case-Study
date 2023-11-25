using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bossing : MonoBehaviour
{
    public GameObject player;
    public bool flip;
    public float speed;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        Vector3 scale = transform.localScale;

        if (player.transform.position.x > transform.position.x)
        {
            scale.x = Mathf.Abs(scale.x) * -1* (flip ? -1 : 1);
            transform.Translate(x: speed * Time.deltaTime, y: 0, z: 0);
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
        else
        {
            scale.x = Mathf.Abs(scale.x) * (flip ? - 1 : 1);
            transform.Translate(x:speed * Time.deltaTime * -1, y:0, z:0);
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }

        transform.localScale = scale;
    }
}
