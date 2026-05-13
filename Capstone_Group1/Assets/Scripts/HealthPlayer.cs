using UnityEngine;

// HealthPlayer.cs — Multiplayer version
// MPGameManager now owns all HP logic via Firebase.
// This script is kept only so Inspector references don't break,
// but it no longer manages HP itself.
// SetMaxHealth() is called by MPGameManager through the HealthBar reference.

public class HealthPlayer : MonoBehaviour
{
    public Canvas canvas;       // "You Lose" / death canvas — still used by MPGameManager
    public int maxHealth = 5;
    public int currentHealth;
    public HealthBar healthBar; // MPGameManager calls SetMaxHealth on this at Start

    // Canvas1/Canvas2 kept for Inspector compatibility — MPGameManager hides them on death
    public Canvas Canvas1;
    public Canvas Canvas2;

    void Start()
    {
        // Hide death canvas at start
        if (canvas != null)
            canvas.gameObject.SetActive(false);

        currentHealth = maxHealth;

        // Initialize the health bar max so the slider isn't stuck at 0
        if (healthBar != null)
            healthBar.SetMaxHealth(maxHealth);
    }

    // Called by MPGameManager when Firebase HP changes for THIS player's bar
    public void SetCurrentHealth(int hp)
    {
        currentHealth = hp;
        if (healthBar != null)
            healthBar.SetHealth(hp);
    }

    // Kept for legacy compatibility — not called in MP mode
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0)
        {
            if (canvas != null) canvas.gameObject.SetActive(true);
            if (Canvas1 != null) Canvas1.gameObject.SetActive(false);
            if (Canvas2 != null) Canvas2.gameObject.SetActive(false);
        }
    }
}