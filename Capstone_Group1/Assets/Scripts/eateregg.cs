using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class eateregg : MonoBehaviour
{
    public void OnCollisionEnter2D(Collision2D collision)
    {
        SceneManager.LoadScene("Final");
    }
}
