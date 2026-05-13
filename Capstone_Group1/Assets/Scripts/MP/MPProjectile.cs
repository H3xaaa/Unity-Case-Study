using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

public class MPProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 10f;
    public float lifetime = 5f;
    public int damage = 1;

    [HideInInspector] public string ownerRole = "p1";
    [HideInInspector] public bool isRemote = false;

    private DatabaseReference _roomRef;

    private void Start()
    {
        Destroy(gameObject, lifetime);

        string code = PlayerSession.RoomCode;
        if (string.IsNullOrEmpty(code)) return;

        _roomRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance,
            "https://starlandsexam-default-rtdb.asia-southeast1.firebasedatabase.app")
            .RootReference.Child("rooms").Child(code);

        // Only LOCAL bullet writes to Firebase
        if (!isRemote)
        {
            var data = new System.Collections.Generic.Dictionary<string, object>
            {
                { "x",    (double)transform.position.x },
                { "y",    (double)transform.position.y },
                { "rotZ", (double)transform.rotation.eulerAngles.z },
                { "role", ownerRole },
                { "ts",   ServerValue.Timestamp }
            };
            _roomRef.Child("bullets").Push().SetValueAsync(data);
        }
    }

    private void Update()
    {
        transform.position += transform.right * Time.deltaTime * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // CRITICAL: ignore own player so bullet doesn't deflect up
        if (other.CompareTag("Player")) return;

        // Remote bullets = visual only, no damage
        if (isRemote)
        {
            if (other.CompareTag("Ground") || other.CompareTag("Wall") ||
                other.CompareTag("Platform") || other.CompareTag("Opponent"))
                Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Opponent"))
        {
            string targetRole = ownerRole == "p1" ? "p2" : "p1";
            FindObjectOfType<MPGameManager>()?.DealDamageToRole(targetRole, damage);
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Flag"))
        {
            other.GetComponent<MPFlag>()?.OnHitByPlayer(ownerRole);
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Ground") || other.CompareTag("Wall") ||
            other.CompareTag("Platform"))
            Destroy(gameObject);
    }
}