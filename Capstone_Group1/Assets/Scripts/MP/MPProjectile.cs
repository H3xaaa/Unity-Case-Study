using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

// ============================================================
// MPProjectile.cs
// Replace ProjectileBehaiour on your projectile prefab
// for the multiplayer scene.
//
// - Hits opponent → writes damage to Firebase
// - Hits flag     → changes flag color
// - Hits wall     → destroys self
// ============================================================

public class MPProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 10f;
    public float lifetime = 5f;
    public int damage = 1;

    // Set by MPGameManager when projectile is spawned
    // "p1" if fired by player 1, "p2" if fired by player 2
    [HideInInspector] public string ownerRole = "p1";

    private DatabaseReference _roomRef;

    private void Start()
    {
        Destroy(gameObject, lifetime);

        string code = PlayerSession.RoomCode;
        if (!string.IsNullOrEmpty(code))
        {
            _roomRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance,
                "https://starlandsexam-default-rtdb.asia-southeast1.firebasedatabase.app")
                .RootReference
                .Child("rooms").Child(code);
        }
    }

    private void Update()
    {
        transform.position += transform.right * Time.deltaTime * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Hit a flag
        if (other.CompareTag("Flag"))
        {
            MPFlag flag = other.GetComponent<MPFlag>();
            if (flag != null)
                flag.OnHitByPlayer(ownerRole);

            Destroy(gameObject);
            return;
        }

        // Hit the opponent player
        if (other.CompareTag("Opponent"))
        {
            DamageOpponent();
            Destroy(gameObject);
            return;
        }

        // Hit ground or wall
        if (other.CompareTag("Ground") || other.CompareTag("Wall") || other.CompareTag("Platform"))
        {
            Destroy(gameObject);
        }
    }

    private void DamageOpponent()
    {
        if (_roomRef == null) return;

        // Determine which player to damage
        // If I am p1, I damage p2 and vice versa
        string opponentRole = ownerRole == "p1" ? "p2" : "p1";

        // Read current HP then subtract
        _roomRef.Child("players_hp").Child(opponentRole).GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) return;

                int currentHP = 3; // default
                if (task.Result.Exists)
                    int.TryParse(task.Result.Value?.ToString(), out currentHP);

                int newHP = Mathf.Max(0, currentHP - damage);
                _roomRef.Child("players_hp").Child(opponentRole).SetValueAsync(newHP);
            });
    }
}