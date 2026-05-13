using UnityEngine;
using Firebase;
using Firebase.Database;

public class MPProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 10f;
    public float lifetime = 5f;
    public int damage = 1;

    [HideInInspector] public string ownerRole = "p1";
    [HideInInspector] public bool isRemote = false;

    private DatabaseReference _roomRef;

    // REAL movement direction
    private Vector2 moveDirection = Vector2.right;

    // ─────────────────────────────────────────────
    public void SetDirection(Vector2 dir)
    {
        moveDirection = dir.normalized;

        // Visual flip
        if (dir.x < 0)
        {
            Vector3 s = transform.localScale;
            s.x *= -1;
            transform.localScale = s;
        }
    }

    // ─────────────────────────────────────────────
    private void Start()
    {
        Destroy(gameObject, lifetime);

        string code = PlayerSession.RoomCode;

        if (string.IsNullOrEmpty(code))
            return;

        _roomRef = FirebaseDatabase.GetInstance(
            FirebaseApp.DefaultInstance,
            "https://starlandsexam-default-rtdb.asia-southeast1.firebasedatabase.app")
            .RootReference
            .Child("rooms")
            .Child(code);
    }

    // ─────────────────────────────────────────────
    private void Update()
    {
        transform.position +=
            (Vector3)(moveDirection * speed * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        // ─────────────────────────────────────
        // Ignore self-hit (robust version)
        // ─────────────────────────────────────
        if (ownerRole == "p1" && other.gameObject.name.Contains("Player1"))
            return;

        if (ownerRole == "p2" && other.gameObject.name.Contains("Player2"))
            return;

        // ─────────────────────────────────────
        // Remote bullets are visual-only
        // ─────────────────────────────────────
        if (isRemote)
        {
            if (other.CompareTag("Ground") ||
                other.CompareTag("Wall") ||
                other.CompareTag("Platform") ||
                other.CompareTag("Player") ||
                other.CompareTag("Opponent"))
            {
                Destroy(gameObject);
            }

            return;
        }

        // ─────────────────────────────────────
        // DAMAGE LOGIC (FIXED - NO TAG OWNERSHIP DEPENDENCY)
        // ─────────────────────────────────────
        if (other.CompareTag("Player") || other.CompareTag("Opponent"))
        {
            string targetRole =
                ownerRole == "p1" ? "p2" : "p1";

            FindObjectOfType<MPGameManager>()
                ?.DealDamageToRole(targetRole, damage);

            Destroy(gameObject);
            return;
        }

        // ─────────────────────────────────────
        // Flag interaction
        // ─────────────────────────────────────
        if (other.CompareTag("Flag"))
        {
            other.GetComponent<MPFlag>()
                ?.OnHitByPlayer(ownerRole);

            Destroy(gameObject);
            return;
        }

        // ─────────────────────────────────────
        // Environment collision
        // ─────────────────────────────────────
        if (other.CompareTag("Ground") ||
            other.CompareTag("Wall") ||
            other.CompareTag("Platform"))
        {
            Destroy(gameObject);
        }
    }
}