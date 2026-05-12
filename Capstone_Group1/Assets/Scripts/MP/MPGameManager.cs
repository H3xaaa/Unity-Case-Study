using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

// ============================================================
// MPGameManager.cs
// Handles:
// - Spawning local player at correct spawn point
// - Syncing HP via Firebase
// - Win/lose detection
// - Showing winner panel
//
// Scene setup:
//   - SpawnPoint_P1 (right side) — empty GameObject
//   - SpawnPoint_P2 (left side)  — empty GameObject
//   - PlayerPrefab — your player with MPPlayerMovement
//   - OpponentGhost — simple sprite, no physics, just visual
//   - WinnerPanel (Canvas) — inactive by default
//   - txtWinner (TMP Text)
// ============================================================

public class MPGameManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject opponentGhostPrefab; // simple visual only, no input

    [Header("Spawn Points")]
    public Transform spawnP1; // right side
    public Transform spawnP2; // left side

    [Header("UI")]
    public GameObject winnerPanel;
    public TextMeshProUGUI txtWinner;
    public Slider sliderMyHP;
    public Slider sliderOpponentHP;
    public TextMeshProUGUI txtMyName;
    public TextMeshProUGUI txtOpponentName;

    [Header("Camera")]
    public MPCameraController cameraController;

    [Header("Settings")]
    public int maxHP = 3;

    // Private
    private DatabaseReference _roomRef;
    private DatabaseReference _hpRef;
    private string _myRole;       // "p1" or "p2"
    private string _opponentRole; // "p1" or "p2"
    private GameObject _myPlayer;
    private GameObject _opponentGhost;
    private bool _gameOver = false;

    private void Start()
    {
        winnerPanel.SetActive(false);

        string code = PlayerSession.RoomCode;
        _roomRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance,
            "https://starlandsexam-default-rtdb.asia-southeast1.firebasedatabase.app")
            .RootReference.Child("rooms").Child(code);

        // Determine roles
        _myRole = PlayerSession.IsHost ? "p1" : "p2";
        _opponentRole = PlayerSession.IsHost ? "p2" : "p1";

        // Set name displays
        if (txtMyName != null) txtMyName.text = PlayerSession.PlayerName;
        if (txtOpponentName != null) txtOpponentName.text = "Opponent";

        // Fetch opponent name from Firebase
        _roomRef.Child("players").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted) return;
            foreach (var playerSnap in task.Result.Children)
            {
                if (playerSnap.Key != PlayerSession.PlayerUID)
                {
                    string oppName = playerSnap.Child("name").Value?.ToString() ?? "Opponent";
                    if (txtOpponentName != null) txtOpponentName.text = oppName;
                }
            }
        });

        // Initialize HP in Firebase (host only)
        if (PlayerSession.IsHost)
        {
            _roomRef.Child("players_hp").Child("p1").SetValueAsync(maxHP);
            _roomRef.Child("players_hp").Child("p2").SetValueAsync(maxHP);
        }

        // Spawn local player
        SpawnLocalPlayer();

        // Spawn opponent ghost
        SpawnOpponentGhost();

        // Listen to HP changes
        _roomRef.Child("players_hp").ValueChanged += OnHPChanged;

        // Listen to opponent position
        _roomRef.Child("positions").Child(_opponentRole).ValueChanged += OnOpponentMoved;
    }

    private void OnDestroy()
    {
        if (_roomRef != null)
        {
            _roomRef.Child("players_hp").ValueChanged -= OnHPChanged;
            _roomRef.Child("positions").Child(_opponentRole).ValueChanged -= OnOpponentMoved;
        }
    }

    // ── Spawn ─────────────────────────────────────────────────
    private void SpawnLocalPlayer()
    {
        Transform spawnPoint = PlayerSession.IsHost ? spawnP1 : spawnP2;
        _myPlayer = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);

        // Set projectile owner role
        MPPlayerMovement movement = _myPlayer.GetComponent<MPPlayerMovement>();

        // Tag opponent for collision detection
        // The local player is tagged "Player", opponent ghost is tagged "Opponent"
        _myPlayer.tag = "Player";

        // Point camera at local player
        if (cameraController != null)
            cameraController.SetTarget(_myPlayer.transform);

        // Wire up projectile spawner role
        MPProjectileSpawner spawner = _myPlayer.GetComponent<MPProjectileSpawner>();
        if (spawner != null) spawner.ownerRole = _myRole;
    }

    private void SpawnOpponentGhost()
    {
        Transform spawnPoint = PlayerSession.IsHost ? spawnP2 : spawnP1;
        if (opponentGhostPrefab != null)
        {
            _opponentGhost = Instantiate(opponentGhostPrefab, spawnPoint.position, Quaternion.identity);
            _opponentGhost.tag = "Opponent";
        }
    }

    // ── Position sync ─────────────────────────────────────────
    private void Update()
    {
        if (_myPlayer == null || _gameOver) return;

        // Upload local player position every frame
        var posData = new System.Collections.Generic.Dictionary<string, object>
        {
            { "x", (double)_myPlayer.transform.position.x },
            { "y", (double)_myPlayer.transform.position.y }
        };
        _roomRef.Child("positions").Child(_myRole).UpdateChildrenAsync(posData);
    }

    private void OnOpponentMoved(object sender, ValueChangedEventArgs e)
    {
        if (_opponentGhost == null || !e.Snapshot.Exists) return;

        float x = float.Parse(e.Snapshot.Child("x").Value?.ToString() ?? "0");
        float y = float.Parse(e.Snapshot.Child("y").Value?.ToString() ?? "0");

        _opponentGhost.transform.position = Vector3.Lerp(
            _opponentGhost.transform.position,
            new Vector3(x, y, 0),
            0.3f
        );
    }

    // ── HP sync ───────────────────────────────────────────────
    private void OnHPChanged(object sender, ValueChangedEventArgs e)
    {
        if (_gameOver || !e.Snapshot.Exists) return;

        int myHP = maxHP;
        int oppHP = maxHP;

        if (e.Snapshot.Child(_myRole).Exists)
            int.TryParse(e.Snapshot.Child(_myRole).Value?.ToString(), out myHP);
        if (e.Snapshot.Child(_opponentRole).Exists)
            int.TryParse(e.Snapshot.Child(_opponentRole).Value?.ToString(), out oppHP);

        // Update HP bars
        if (sliderMyHP != null)
        {
            sliderMyHP.maxValue = maxHP;
            sliderMyHP.value = myHP;
        }
        if (sliderOpponentHP != null)
        {
            sliderOpponentHP.maxValue = maxHP;
            sliderOpponentHP.value = oppHP;
        }

        // Check win condition
        if (myHP <= 0)
            ShowResult(false);
        else if (oppHP <= 0)
            ShowResult(true);
    }

    // ── Win/Lose ──────────────────────────────────────────────
    private void ShowResult(bool won)
    {
        if (_gameOver) return;
        _gameOver = true;

        winnerPanel.SetActive(true);
        if (txtWinner != null)
            txtWinner.text = won ? "YOU WIN!" : "YOU LOSE";

        // Clean up room after 5 seconds and return to menu
        Invoke(nameof(ReturnToLobby), 5f);
    }

    private void ReturnToLobby()
    {
        if (PlayerSession.IsHost)
            _roomRef.RemoveValueAsync();

        PlayerSession.Clear();
        SceneManager.LoadScene("Start Menu");
    }
}