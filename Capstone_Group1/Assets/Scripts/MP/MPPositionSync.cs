using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

// ============================================================
// MPPositionSync.cs
// Attach to BOTH Player 1 and Player 2 in the scene.
//
// How it works:
// - LOCAL player: uploads position + flip to Firebase 10x/sec
// - REMOTE player: reads Firebase, smoothly interpolates to position
//
// Setup:
//   1. Attach to Player 1 and Player 2
//   2. Set isLocalPlayer = true on your player, false on opponent
//      (MPGameManager sets this automatically)
//   3. Set playerRole = "p1" or "p2"
// ============================================================

public class MPPositionSync : MonoBehaviour
{
    [Header("Identity")]
    public string playerRole = "p1";    // "p1" or "p2"
    public bool isLocalPlayer = false;  // set by MPGameManager

    [Header("Sync Settings")]
    public float uploadRate = 0.1f;  // 10 times per second
    public float interpolation = 15f;   // smoothing speed

    // Private
    private DatabaseReference _posRef;
    private float _uploadTimer = 0f;
    private Vector3 _targetPos;
    private bool _targetFlipped = false;
    private SpriteRenderer _sr;
    private Animator _anim;
    private bool _initialized = false;

    private void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        _anim = GetComponent<Animator>();
        _targetPos = transform.position;

        string code = PlayerSession.RoomCode;
        if (string.IsNullOrEmpty(code)) return;

        _posRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance,
            "https://starlandsexam-default-rtdb.asia-southeast1.firebasedatabase.app")
            .RootReference
            .Child("rooms").Child(code)
            .Child("positions").Child(playerRole);

        _initialized = true;

        // Remote player listens for position updates
        if (!isLocalPlayer)
        {
            _posRef.ValueChanged += OnRemotePositionChanged;
        }
    }

    private void OnDestroy()
    {
        if (!isLocalPlayer && _posRef != null)
            _posRef.ValueChanged -= OnRemotePositionChanged;
    }

    private void Update()
    {
        if (!_initialized) return;

        if (isLocalPlayer)
        {
            // Upload position at limited rate
            _uploadTimer += Time.deltaTime;
            if (_uploadTimer >= uploadRate)
            {
                _uploadTimer = 0f;
                UploadPosition();
            }
        }
        else
        {
            // Interpolate to target position smoothly
            transform.position = Vector3.Lerp(
                transform.position,
                _targetPos,
                Time.deltaTime * interpolation
            );

            // Apply flip from remote
            if (_sr != null)
                _sr.flipX = _targetFlipped;
        }
    }

    // ── Upload local position to Firebase ────────────────────
    private void UploadPosition()
    {
        bool flipped = transform.localScale.x < 0;

        var data = new System.Collections.Generic.Dictionary<string, object>
        {
            { "x",       (double)transform.position.x },
            { "y",       (double)transform.position.y },
            { "flipped", flipped },
            { "animState", GetAnimState() }
        };

        _posRef.UpdateChildrenAsync(data);
    }

    // ── Receive remote position from Firebase ─────────────────
    private void OnRemotePositionChanged(object sender, ValueChangedEventArgs e)
    {
        if (!e.Snapshot.Exists) return;

        float x = float.Parse(e.Snapshot.Child("x").Value?.ToString() ?? "0");
        float y = float.Parse(e.Snapshot.Child("y").Value?.ToString() ?? "0");
        _targetPos = new Vector3(x, y, transform.position.z);

        // Flip
        bool flipped = e.Snapshot.Child("flipped").Value?.ToString() == "True";
        _targetFlipped = flipped;

        // Animation state
        if (_anim != null && e.Snapshot.Child("animState").Exists)
        {
            int state = int.Parse(e.Snapshot.Child("animState").Value?.ToString() ?? "0");
            _anim.SetInteger("state", state);
        }
    }

    // ── Get current animation state int ──────────────────────
    private int GetAnimState()
    {
        if (_anim == null) return 0;
        return _anim.GetInteger("state");
    }

    // ── Called by MPGameManager to set role ───────────────────
    public void Initialize(string role, bool isLocal)
    {
        playerRole = role;
        isLocalPlayer = isLocal;
    }
}