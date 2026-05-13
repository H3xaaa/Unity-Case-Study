using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

// ============================================================
// MPBulletSync.cs
// Attach to mpmanager or any persistent GameObject in scene.
// Listens for bullets fired by the OPPONENT and spawns
// a visual-only copy on this client.
// ============================================================

public class MPBulletSync : MonoBehaviour
{
    [Header("Projectile Prefab")]
    public GameObject projectilePrefab; // same prefab as the real bullet

    private DatabaseReference _bulletsRef;
    private string _opponentRole;

    private void Start()
    {
        _opponentRole = PlayerSession.IsHost ? "p2" : "p1";

        string code = PlayerSession.RoomCode;
        if (string.IsNullOrEmpty(code)) return;

        _bulletsRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance,
            "https://starlandsexam-default-rtdb.asia-southeast1.firebasedatabase.app")
            .RootReference.Child("rooms").Child(code).Child("bullets");

        // Listen for new bullet events
        _bulletsRef.ChildAdded += OnBulletAdded;
    }

    private void OnDestroy()
    {
        if (_bulletsRef != null)
            _bulletsRef.ChildAdded -= OnBulletAdded;
    }

    private void OnBulletAdded(object sender, ChildChangedEventArgs e)
    {
        if (!e.Snapshot.Exists) return;

        string role = e.Snapshot.Child("role").Value?.ToString();

        // Only spawn visual for OPPONENT's bullets
        if (role != _opponentRole) return;

        float x = float.Parse(e.Snapshot.Child("x").Value?.ToString() ?? "0");
        float y = float.Parse(e.Snapshot.Child("y").Value?.ToString() ?? "0");
        float rotZ = float.Parse(e.Snapshot.Child("rotZ").Value?.ToString() ?? "0");

        if (projectilePrefab == null) return;

        GameObject bullet = Instantiate(projectilePrefab,
            new Vector3(x, y, 0),
            Quaternion.Euler(0, 0, rotZ));

        // Mark as remote so it doesn't deal damage or write to Firebase again
        MPProjectile mp = bullet.GetComponent<MPProjectile>();
        if (mp != null)
        {
            mp.isRemote = true;
            mp.ownerRole = role;
        }

        // Clean up old bullet event from Firebase
        e.Snapshot.Reference.RemoveValueAsync();
    }
}