using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

// ============================================================
// MPGameManager.cs — FINAL WITH NAME + HEALTH FIX
// ============================================================

public class MPGameManager : MonoBehaviour
{
    [Header("Players in Scene")]
    public GameObject player1Object;
    public GameObject player2Object;

    [Header("Spawn Points")]
    public Transform spawnP1;
    public Transform spawnP2;

    [Header("Health UI")]
    public HealthBar healthBarP1;
    public HealthBar healthBarP2;
    public GameObject healthCanvasP1;       // HealthCanvas1 GameObject
    public GameObject healthCanvasP2;       // HealthCanvas2 GameObject

    [Header("Name Display")]
    public TextMeshProUGUI txtNameP1;       // TMP text showing P1 name
    public TextMeshProUGUI txtNameP2;       // TMP text showing P2 name

    [Header("Winner UI")]
    public MPWinnerUI winnerUI;

    [Header("In-Game Leave Button")]
    public GameObject btnLeaveInGame;

    [Header("Settings")]
    public int maxHP = 3;

    private DatabaseReference _roomRef;
    private string _myRole;
    private string _opponentRole;
    private bool _gameOver = false;

    private void Start()
    {
        _myRole = PlayerSession.IsHost ? "p1" : "p2";
        _opponentRole = PlayerSession.IsHost ? "p2" : "p1";

        Debug.Log("MPGameManager — IsHost: " + PlayerSession.IsHost + " | Role: " + _myRole);
        Debug.Log("START — IsHost: " + PlayerSession.IsHost);
        Debug.Log("START — PlayerName: " + PlayerSession.PlayerName);

        string code = PlayerSession.RoomCode;
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogError("RoomCode empty! PlayerSession was cleared.");
            return;
        }

        _roomRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance,
            "https://starlandsexam-default-rtdb.asia-southeast1.firebasedatabase.app")
            .RootReference.Child("rooms").Child(code);

        // Show health canvases
        if (healthCanvasP1 != null) healthCanvasP1.SetActive(true);
        if (healthCanvasP2 != null) healthCanvasP2.SetActive(true);

        // Set local player name immediately
        if (PlayerSession.IsHost)
        {
            if (txtNameP1 != null) txtNameP1.text = PlayerSession.PlayerName;
            if (txtNameP2 != null) txtNameP2.text = "Opponent";
        }
        else
        {
            if (txtNameP2 != null) txtNameP2.text = PlayerSession.PlayerName;
            if (txtNameP1 != null) txtNameP1.text = "Opponent";
        }

        // Fetch opponent name from Firebase
        _roomRef.Child("players").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted) return;
            foreach (var snap in task.Result.Children)
            {
                if (snap.Key != PlayerSession.PlayerUID)
                {
                    string oppName = snap.Child("name").Value?.ToString() ?? "Opponent";
                    if (PlayerSession.IsHost)
                    {
                        if (txtNameP2 != null) txtNameP2.text = oppName;
                    }
                    else
                    {
                        if (txtNameP1 != null) txtNameP1.text = oppName;
                    }
                }
            }
        });

        SetupPlayers();

        if (PlayerSession.IsHost)
        {
            _roomRef.Child("players_hp").Child("p1").SetValueAsync(maxHP);
            _roomRef.Child("players_hp").Child("p2").SetValueAsync(maxHP);
            _roomRef.Child("leaver").RemoveValueAsync();
        }

        if (healthBarP1 != null) healthBarP1.SetMaxHealth(maxHP);
        if (healthBarP2 != null) healthBarP2.SetMaxHealth(maxHP);

        _roomRef.Child("players_hp").ValueChanged += OnHPChanged;
        _roomRef.Child("leaver").ValueChanged += OnLeaverDetected;
    }

    private void OnDestroy()
    {
        if (_roomRef != null)
        {
            _roomRef.Child("players_hp").ValueChanged -= OnHPChanged;
            _roomRef.Child("leaver").ValueChanged -= OnLeaverDetected;
        }
    }

    // ── Setup players ─────────────────────────────────────────
    private void SetupPlayers()
    {
        if (spawnP1 != null && player1Object != null)
            player1Object.transform.position = spawnP1.position;
        if (spawnP2 != null && player2Object != null)
            player2Object.transform.position = spawnP2.position;

        if (PlayerSession.IsHost)
        {
            EnableInput(player1Object, true);
            EnableInput(player2Object, false);

            // IMPORTANT: set tags before anything else
            SetTag(player1Object, "Player");
            SetTag(player2Object, "Opponent");

            SetProjectileOwner(player1Object, "p1");
            SetProjectileOwner(player2Object, "p2");
            SetCameraTarget(player1Object);
            InitPositionSync(player1Object, "p1", true);
            InitPositionSync(player2Object, "p2", false);
        }
        else
        {
            EnableInput(player2Object, true);
            EnableInput(player1Object, false);

            SetTag(player2Object, "Player");
            SetTag(player1Object, "Opponent");

            SetProjectileOwner(player1Object, "p1");
            SetProjectileOwner(player2Object, "p2");
            SetCameraTarget(player2Object);
            InitPositionSync(player2Object, "p2", true);
            InitPositionSync(player1Object, "p1", false);
        }
    }

    // ── HP listener ───────────────────────────────────────────
    private void OnHPChanged(object sender, ValueChangedEventArgs e)
    {
        if (_gameOver || !e.Snapshot.Exists) return;

        int p1HP = maxHP, p2HP = maxHP;
        if (e.Snapshot.Child("p1").Exists)
            int.TryParse(e.Snapshot.Child("p1").Value?.ToString(), out p1HP);
        if (e.Snapshot.Child("p2").Exists)
            int.TryParse(e.Snapshot.Child("p2").Value?.ToString(), out p2HP);

        if (healthBarP1 != null) healthBarP1.SetHealth(p1HP);
        if (healthBarP2 != null) healthBarP2.SetHealth(p2HP);

        if (p1HP <= 0 && p2HP <= 0)
            ShowResult("DRAW!", "");
        else if (p1HP <= 0)
            ShowResult(
                PlayerSession.IsHost ? "YOU LOSE" : "YOU WIN! 🏆",
                PlayerSession.IsHost ? "" : "Enemy eliminated!");
        else if (p2HP <= 0)
            ShowResult(
                PlayerSession.IsHost ? "YOU WIN! 🏆" : "YOU LOSE",
                PlayerSession.IsHost ? "Enemy eliminated!" : "");
    }

    // ── Leaver listener ───────────────────────────────────────
    private void OnLeaverDetected(object sender, ValueChangedEventArgs e)
    {
        if (_gameOver || !e.Snapshot.Exists) return;
        string leaverUID = e.Snapshot.Value?.ToString();
        if (string.IsNullOrEmpty(leaverUID)) return;

        if (leaverUID != PlayerSession.PlayerUID)
            ShowResult("YOU WIN! 🏆", "Opponent left the game.");
    }

    // ── In-game leave button ──────────────────────────────────
    public void OnInGameLeave()
    {
        if (_roomRef == null) return;
        _roomRef.Child("leaver").SetValueAsync(PlayerSession.PlayerUID)
            .ContinueWithOnMainThread(_ =>
            {
                if (PlayerSession.IsHost) _roomRef.RemoveValueAsync();
                PlayerSession.Clear();
                SceneManager.LoadScene("Start Menu");
            });
    }

    // ── Damage ────────────────────────────────────────────────
    public void DealDamageToRole(string targetRole, int amount)
    {
        if (_roomRef == null || _gameOver) return;
        _roomRef.Child("players_hp").Child(targetRole).GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) return;
                int hp = maxHP;
                if (task.Result.Exists)
                    int.TryParse(task.Result.Value?.ToString(), out hp);
                int newHP = Mathf.Max(0, hp - amount);
                _roomRef.Child("players_hp").Child(targetRole).SetValueAsync(newHP);
                Debug.Log("Damage to " + targetRole + ": " + hp + " → " + newHP);
            });
    }

    // ── Show result ───────────────────────────────────────────
    private void ShowResult(string result, string subtitle)
    {
        if (_gameOver) return;
        _gameOver = true;
        EnableInput(player1Object, false);
        EnableInput(player2Object, false);
        if (btnLeaveInGame != null) btnLeaveInGame.SetActive(false);
        if (winnerUI != null) winnerUI.ShowResult(result, subtitle);
    }

    // ── Helpers ───────────────────────────────────────────────
    private void SetTag(GameObject obj, string tag)
    {
        if (obj == null) return;
        try { obj.tag = tag; }
        catch { Debug.LogError("Tag '" + tag + "' not defined! Add it in Project Settings → Tags."); }
    }

    private void EnableInput(GameObject player, bool enable)
    {
        if (player == null) return;
        MPPlayerMovement mv = player.GetComponent<MPPlayerMovement>();
        if (mv != null) mv.enabled = enable;
    }

    private void SetProjectileOwner(GameObject player, string role)
    {
        if (player == null) return;
        MPProjectileSpawner sp = player.GetComponent<MPProjectileSpawner>();
        if (sp != null) sp.ownerRole = role;
    }

    private void SetCameraTarget(GameObject target)
    {
        if (target == null) return;
        MPCameraController cam = Camera.main?.GetComponent<MPCameraController>();
        if (cam != null) cam.SetTarget(target.transform);
    }

    private void InitPositionSync(GameObject player, string role, bool isLocal)
    {
        if (player == null) return;
        MPPositionSync sync = player.GetComponent<MPPositionSync>();
        if (sync != null) sync.Initialize(role, isLocal);
    }
}