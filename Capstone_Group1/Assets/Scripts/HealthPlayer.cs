using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HealthPlayer : MonoBehaviour
{
    public Canvas canvas;
    public int maxHealth = 3;
    public int currentHealth;
    public HealthBar healthBar;
    public Canvas Canvas1;
    public Canvas Canvas2;

    // Start is called before the first frame update
    void Start()
    {
        canvas.gameObject.SetActive(false);
        currentHealth = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        healthBar.SetHealth(currentHealth);
        if (currentHealth <= 0)
        {
            canvas.gameObject.SetActive(true);
            Canvas1.gameObject.SetActive(false);
            Canvas2.gameObject.SetActive(false);
        }
    }
}