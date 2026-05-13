using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

// ============================================================
// MPGameManager.cs
// - Host controls Player 1, Guest controls Player 2
// - Disables opponent MPPlayerMovement
// - Initializes MPPositionSync on both players
// - Syncs HP via Firebase
// - Shows winner panel
// ============================================================

public class MPGameManager : MonoBehaviour
{
    [Header("Players in Scene")]
    public GameObject player1Object;
    public GameObject player2Object;

    [Header("Spawn Points")]
    public Transform spawnP1;
    public Transform spawnP2;

    [Header("Health Bars")]
    public HealthBar healthBarP1;
    public HealthBar healthBarP2;

    [Header("Winner UI")]
    public GameObject winnerPanel;
    public TextMeshProUGUI txtWinner;

    [Header("Settings")]
    public int maxHP = 3;

    private DatabaseReference _roomRef;
    private string _myRole;
    private string _opponentRole;
    private bool _gameOver = false;

    private void Start()
    {
        if (winnerPanel != null)
            winnerPanel.SetActive(false);

        _myRole = PlayerSession.IsHost ? "p1" : "p2";
        _opponentRole = PlayerSession.IsHost ? "p2" : "p1";

        Debug.Log("MPGameManager Start — IsHost: " + PlayerSession.IsHost + " | Role: " + _myRole);

        string code = PlayerSession.RoomCode;
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogError("RoomCode is empty! PlayerSession may have been cleared.");
            return;
        }

        _roomRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance,
            "https://starlandsexam-default-rtdb.asia-southeast1.firebasedatabase.app")
            .RootReference.Child("rooms").Child(code);

        SetupPlayers();

        // Host initializes HP
        if (PlayerSession.IsHost)
        {
            _roomRef.Child("players_hp").Child("p1").SetValueAsync(maxHP);
            _roomRef.Child("players_hp").Child("p2").SetValueAsync(maxHP);
        }

        // Init health bars
        if (healthBarP1 != null) healthBarP1.SetMaxHealth(maxHP);
        if (healthBarP2 != null) healthBarP2.SetMaxHealth(maxHP);

        // Listen for HP changes
        _roomRef.Child("players_hp").ValueChanged += OnHPChanged;
    }

    private void OnDestroy()
    {
        if (_roomRef != null)
            _roomRef.Child("players_hp").ValueChanged -= OnHPChanged;
    }

    // ── Setup players ─────────────────────────────────────────
    private void SetupPlayers()
    {
        // Move to spawn points
        if (spawnP1 != null && player1Object != null)
            player1Object.transform.position = spawnP1.position;
        if (spawnP2 != null && player2Object != null)
            player2Object.transform.position = spawnP2.position;

        if (PlayerSession.IsHost)
        {
            // HOST controls Player 1
            EnablePlayerInput(player1Object, true);
            EnablePlayerInput(player2Object, false);

            player1Object.tag = "Player";
            player2Object.tag = "Opponent";

            SetProjectileOwner(player1Object, "p1");
            SetProjectileOwner(player2Object, "p2");

            SetCameraTarget(player1Object);

            // Position sync — P1 is local, P2 is remote
            InitPositionSync(player1Object, "p1", isLocal: true);
            InitPositionSync(player2Object, "p2", isLocal: false);

            Debug.Log("HOST setup complete — controlling Player 1");
        }
        else
        {
            // GUEST controls Player 2
            EnablePlayerInput(player2Object, true);
            EnablePlayerInput(player1Object, false);

            player2Object.tag = "Player";
            player1Object.tag = "Opponent";

            SetProjectileOwner(player1Object, "p1");
            SetProjectileOwner(player2Object, "p2");

            SetCameraTarget(player2Object);

            // Position sync — P2 is local, P1 is remote
            InitPositionSync(player2Object, "p2", isLocal: true);
            InitPositionSync(player1Object, "p1", isLocal: false);

            Debug.Log("GUEST setup complete — controlling Player 2");
        }
    }

    // ── Helpers ───────────────────────────────────────────────
    private void EnablePlayerInput(GameObject player, bool enable)
    {
        if (player == null) return;
        MPPlayerMovement mv = player.GetComponent<MPPlayerMovement>();
        if (mv != null)
        {
            mv.enabled = enable;
            Debug.Log((enable ? "ENABLED" : "DISABLED") + " input on: " + player.name);
        }
        else
        {
            Debug.LogWarning("MPPlayerMovement not found on: " + player.name);
        }
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
        if (cam != null)
            cam.SetTarget(target.transform);
        else
            Debug.LogWarning("MPCameraController not found on Main Camera!");
    }

    private void InitPositionSync(GameObject player, string role, bool isLocal)
    {
        if (player == null) return;
        MPPositionSync sync = player.GetComponent<MPPositionSync>();
        if (sync != null)
        {
            sync.Initialize(role, isLocal);
            Debug.Log("PositionSync initialized: " + player.name + " role=" + role + " local=" + isLocal);
        }
        else
        {
            Debug.LogWarning("MPPositionSync not found on: " + player.name + " — add it!");
        }
    }

    // ── HP Sync ───────────────────────────────────────────────
    private void OnHPChanged(object sender, ValueChangedEventArgs e)
    {
        if (_gameOver || !e.Snapshot.Exists) return;

        int p1HP = maxHP;
        int p2HP = maxHP;

        if (e.Snapshot.Child("p1").Exists)
            int.TryParse(e.Snapshot.Child("p1").Value?.ToString(), out p1HP);
        if (e.Snapshot.Child("p2").Exists)
            int.TryParse(e.Snapshot.Child("p2").Value?.ToString(), out p2HP);

        if (healthBarP1 != null) healthBarP1.SetHealth(p1HP);
        if (healthBarP2 != null) healthBarP2.SetHealth(p2HP);

        Debug.Log($"HP Update — P1: {p1HP} | P2: {p2HP}");

        if (p1HP <= 0 && p2HP <= 0)
            ShowResult("DRAW!");
        else if (p1HP <= 0)
            ShowResult(PlayerSession.IsHost ? "YOU LOSE" : "YOU WIN! 🏆");
        else if (p2HP <= 0)
            ShowResult(PlayerSession.IsHost ? "YOU WIN! 🏆" : "YOU LOSE");
    }

    // ── Called by MPProjectile when it hits opponent ──────────
    public void DealDamageToRole(string targetRole, int amount)
    {
        if (_roomRef == null) return;

        _roomRef.Child("players_hp").Child(targetRole).GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) return;
                int currentHP = maxHP;
                if (task.Result.Exists)
                    int.TryParse(task.Result.Value?.ToString(), out currentHP);
                int newHP = Mathf.Max(0, currentHP - amount);
                _roomRef.Child("players_hp").Child(targetRole).SetValueAsync(newHP);
                Debug.Log("Dealt " + amount + " damage to " + targetRole + " — new HP: " + newHP);
            });
    }

    // ── Win/Lose ──────────────────────────────────────────────
    private void ShowResult(string message)
    {
        if (_gameOver) return;
        _gameOver = true;

        if (winnerPanel != null) winnerPanel.SetActive(true);
        if (txtWinner != null) txtWinner.text = message;

        EnablePlayerInput(player1Object, false);
        EnablePlayerInput(player2Object, false);

        Invoke(nameof(ReturnToMenu), 4f);
    }

    private void ReturnToMenu()
    {
        if (PlayerSession.IsHost && _roomRef != null)
            _roomRef.Child("players_hp").RemoveValueAsync();
        PlayerSession.Clear();
        SceneManager.LoadScene("Start Menu");
    }
}