using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

// ============================================================
// MPGameManager.cs - FIXED VERSION
// Host = Player 1 (left), Guest = Player 2 (right)
// Disables opponent input so only local player is controlled
// Syncs HP via Firebase
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

        string code = PlayerSession.RoomCode;
        _roomRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance,
            "https://starlandsexam-default-rtdb.asia-southeast1.firebasedatabase.app")
            .RootReference.Child("rooms").Child(code);

        SetupPlayers();

        if (PlayerSession.IsHost)
        {
            _roomRef.Child("players_hp").Child("p1").SetValueAsync(maxHP);
            _roomRef.Child("players_hp").Child("p2").SetValueAsync(maxHP);
        }

        if (healthBarP1 != null) healthBarP1.SetMaxHealth(maxHP);
        if (healthBarP2 != null) healthBarP2.SetMaxHealth(maxHP);

        _roomRef.Child("players_hp").ValueChanged += OnHPChanged;
    }

    private void OnDestroy()
    {
        if (_roomRef != null)
            _roomRef.Child("players_hp").ValueChanged -= OnHPChanged;
    }

    private void SetupPlayers()
    {
        // Move players to spawn points
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
            Debug.Log("HOST: Controlling Player 1");
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
            Debug.Log("GUEST: Controlling Player 2");
        }
    }

    private void EnablePlayerInput(GameObject player, bool enable)
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
        _roomRef.Child("players_hp").Child(targetRole).GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) return;
                int currentHP = maxHP;
                if (task.Result.Exists)
                    int.TryParse(task.Result.Value?.ToString(), out currentHP);
                int newHP = Mathf.Max(0, currentHP - amount);
                _roomRef.Child("players_hp").Child(targetRole).SetValueAsync(newHP);
                Debug.Log("Damage dealt to " + targetRole + " new HP: " + newHP);
            });
    }

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