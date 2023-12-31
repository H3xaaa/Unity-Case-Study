using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextLevel : MonoBehaviour
{
    public RotateObject scriptReference;
    public Canvas canvas;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        int collectItemCount = scriptReference.collectItem;
        if (collectItemCount >= 1)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
        else {
            canvas.gameObject.SetActive(true);
        }
    }
}
