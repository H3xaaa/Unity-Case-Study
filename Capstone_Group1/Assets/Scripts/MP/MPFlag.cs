using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

// ============================================================
// MPFlag.cs
// Flag object that changes color when hit by a projectile.
// Color is synced to all players via Firebase.
//
// Attach to: Flag GameObject in MultiplayerGame scene
// Requires: SpriteRenderer on same GameObject
// ============================================================

public class MPFlag : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Unique ID for this flag e.g. flag_01, flag_02")]
    public string flagID = "flag_01";

    [Header("Colors")]
    public Color colorNeutral = Color.white;
    public Color colorP1 = Color.red;
    public Color colorP2 = Color.blue;

    private SpriteRenderer _sr;
    private DatabaseReference _flagRef;
    private string _currentOwner = "neutral"; // "neutral", "p1", "p2"

    private void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        _sr.color = colorNeutral;

        string code = PlayerSession.RoomCode;
        if (string.IsNullOrEmpty(code)) return;

        _flagRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance,
            "https://starlandsexam-default-rtdb.asia-southeast1.firebasedatabase.app")
            .RootReference
            .Child("rooms").Child(code)
            .Child("flags").Child(flagID);

        // Initialize flag in DB if host
        if (PlayerSession.IsHost)
            _flagRef.Child("owner").SetValueAsync("neutral");

        // Listen for color changes
        _flagRef.Child("owner").ValueChanged += OnOwnerChanged;
    }

    private void OnDestroy()
    {
        if (_flagRef != null)
            _flagRef.Child("owner").ValueChanged -= OnOwnerChanged;
    }

    private void OnOwnerChanged(object sender, ValueChangedEventArgs e)
    {
        if (!e.Snapshot.Exists) return;
        string owner = e.Snapshot.Value?.ToString() ?? "neutral";
        _currentOwner = owner;
        ApplyColor(owner);
    }

    private void ApplyColor(string owner)
    {
        switch (owner)
        {
            case "p1": _sr.color = colorP1; break;
            case "p2": _sr.color = colorP2; break;
            default: _sr.color = colorNeutral; break;
        }
    }

    // Called by MPProjectile when it hits this flag
    public void OnHitByPlayer(string playerRole) // "p1" or "p2"
    {
        if (_flagRef == null) return;
        _flagRef.Child("owner").SetValueAsync(playerRole);
    }

    public string CurrentOwner => _currentOwner;
}