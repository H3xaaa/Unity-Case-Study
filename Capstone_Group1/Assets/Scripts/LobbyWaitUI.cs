using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.IO;
using Firebase.Database;
using Firebase.Extensions;
using Firebase;
using Firebase.Auth;

public class LobbyWaitUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI txtRoomCode;
    [SerializeField] private TextMeshProUGUI txtPlayer1;
    [SerializeField] private TextMeshProUGUI txtPlayer2;
    [SerializeField] private Image imgReady1;
    [SerializeField] private Image imgReady2;
    [SerializeField] private Button btnReady;
    [SerializeField] private Button btnStart;
    [SerializeField] private TextMeshProUGUI txtStatus;

    [Header("Ready Colors")]
    [SerializeField] private Color colorReady = Color.green;
    [SerializeField] private Color colorNotReady = Color.gray;

    [Header("Scene Name")]
    [SerializeField] private string gameSceneName = "MultiplayerGame";

    private DatabaseReference _roomRef;
    private bool _localReady = false;
    private string _hostUID = "";

    private Dictionary<string, (string name, bool ready)> _players
        = new Dictionary<string, (string, bool)>();

    private string _logPath;

    // ── Logger works in both Editor and Build ─────────────────
    private void Log(string msg)
    {
        string line = "[" + System.DateTime.Now.ToString("HH:mm:ss") + "] " + msg;
        Debug.Log(msg);
        try
        {
            File.AppendAllText(_logPath, line + "\n");
        }
        catch { }
    }

    // ── Init ──────────────────────────────────────────────────
    private void Start()
    {
        _logPath = Path.Combine(Application.persistentDataPath, "lobby_debug.txt");
        File.WriteAllText(_logPath, "=== LOBBY DEBUG LOG ===\n");

        string code = PlayerSession.RoomCode;
        Log("START — RoomCode: " + code);
        Log("START — My UID: " + PlayerSession.PlayerUID);
        Log("START — IsHost: " + PlayerSession.IsHost);
        Log("START — PlayerName: " + PlayerSession.PlayerName);
        Log("LOG PATH: " + _logPath);

        txtRoomCode.text = "Room: " + code;
        btnStart.gameObject.SetActive(PlayerSession.IsHost);
        btnStart.interactable = false;
        txtPlayer2.text = "Waiting for player...";
        imgReady2.color = colorNotReady;

        _roomRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance,
            "https://starlandsexam-default-rtdb.asia-southeast1.firebasedatabase.app")
            .RootReference
            .Child("rooms").Child(code);

        _roomRef.ValueChanged += OnRoomChanged;
    }

    private void OnDestroy()
    {
        if (_roomRef != null)
            _roomRef.ValueChanged -= OnRoomChanged;
    }

    // ── Firebase listener ─────────────────────────────────────
    private void OnRoomChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null)
        {
            Log("ERROR: " + e.DatabaseError.Message);
            txtStatus.text = "Connection error.";
            return;
        }

        var snap = e.Snapshot;
        if (!snap.Exists)
        {
            Log("ROOM DELETED");
            txtStatus.text = "Room closed by host.";
            Invoke(nameof(ReturnToMenu), 2f);
            return;
        }

        string status = snap.Child("status").Value?.ToString();
        Log("ROOM STATUS: " + status);

        if (status == "starting")
        {
            Log("LOADING GAME SCENE: " + gameSceneName);
            _roomRef.ValueChanged -= OnRoomChanged;
            SceneManager.LoadScene(gameSceneName);
            return;
        }

        // Read host UID from DB
        _hostUID = snap.Child("host").Value?.ToString() ?? "";
        Log("HOST UID FROM DB: " + _hostUID);
        Log("MY UID: " + PlayerSession.PlayerUID);
        Log("AM I HOST: " + PlayerSession.IsHost);

        // Parse players
        _players.Clear();
        var playersSnap = snap.Child("players");
        Log("PLAYER COUNT IN DB: " + playersSnap.ChildrenCount);

        foreach (var playerSnap in playersSnap.Children)
        {
            string uid = playerSnap.Key;
            string name = playerSnap.Child("name").Value?.ToString() ?? "Unknown";
            bool ready = playerSnap.Child("ready").Value?.ToString() == "True";
            bool isThisHost = uid == _hostUID;
            _players[uid] = (name, ready);
            Log("PLAYER — UID: " + uid + " | Name: " + name + " | Ready: " + ready + " | IsHost: " + isThisHost);
        }

        RefreshUI();
    }

    // ── UI refresh ────────────────────────────────────────────
    private void RefreshUI()
    {
        bool allReady = _players.Count == 2;

        // Reset both slots
        txtPlayer1.text = "Waiting...";
        txtPlayer2.text = "Waiting for player...";
        imgReady1.color = colorNotReady;
        imgReady2.color = colorNotReady;

        foreach (var kvp in _players)
        {
            bool isLocalPlayer = kvp.Key == PlayerSession.PlayerUID;
            string displayName = kvp.Value.name + (isLocalPlayer ? " (You)" : "");
            bool ready = kvp.Value.ready;
            bool isHost = kvp.Key == _hostUID;

            Log("REFRESH — UID: " + kvp.Key + " | Name: " + kvp.Value.name + " | IsHost: " + isHost + " | Slot: " + (isHost ? "1(HOST)" : "2(GUEST)"));

            if (isHost)
            {
                txtPlayer1.text = displayName;
                imgReady1.color = ready ? colorReady : colorNotReady;
            }
            else
            {
                txtPlayer2.text = displayName;
                imgReady2.color = ready ? colorReady : colorNotReady;
            }

            if (!ready) allReady = false;
        }

        if (_players.Count < 2)
        {
            txtPlayer2.text = "Waiting for player...";
            imgReady2.color = colorNotReady;
            allReady = false;
        }

        txtStatus.text = _players.Count < 2
            ? "Share your code: " + PlayerSession.RoomCode
            : allReady ? "All ready!" : "Waiting for both players to ready up...";

        if (PlayerSession.IsHost)
            btnStart.interactable = allReady;

        btnReady.GetComponentInChildren<TextMeshProUGUI>().text =
            _localReady ? "Not Ready" : "Ready";

        Log("FINAL SLOT1: " + txtPlayer1.text);
        Log("FINAL SLOT2: " + txtPlayer2.text);
    }

    // ── Ready toggle ──────────────────────────────────────────
    public void OnToggleReady()
    {
        _localReady = !_localReady;
        Log("READY TOGGLE: " + _localReady);
        _roomRef.Child("players").Child(PlayerSession.PlayerUID)
                .Child("ready").SetValueAsync(_localReady);
    }

    // ── Host starts the game ──────────────────────────────────
    public void OnStartGame()
    {
        if (!PlayerSession.IsHost) return;
        Log("START GAME clicked");
        btnStart.interactable = false;
        txtStatus.text = "Starting...";
        _roomRef.Child("status").SetValueAsync("starting");
    }

    // ── Leave lobby ───────────────────────────────────────────
    public void OnLeave()
    {
        Log("LEAVE clicked");
        _roomRef.Child("players").Child(PlayerSession.PlayerUID)
                .RemoveValueAsync();

        if (PlayerSession.IsHost)
            _roomRef.RemoveValueAsync();

        PlayerSession.Clear();
        ReturnToMenu();
    }

    private void ReturnToMenu()
    {
        SceneManager.LoadScene("Start Menu");
    }
}