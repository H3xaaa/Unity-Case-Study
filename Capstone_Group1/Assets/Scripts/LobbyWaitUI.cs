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

    [Header("Extra Buttons")]
    [SerializeField] private Button btnCopyCode;

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

    // ─────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────
    private void Start()
    {
        _logPath = Path.Combine(Application.persistentDataPath, "lobby_debug.txt");
        File.WriteAllText(_logPath, "=== LOBBY DEBUG LOG ===\n");

        string code = PlayerSession.RoomCode;

        Log("START — RoomCode: " + code);
        Log("START — My UID: " + PlayerSession.PlayerUID);
        Log("START — IsHost: " + PlayerSession.IsHost);
        Log("START — PlayerName: " + PlayerSession.PlayerName);

        txtRoomCode.text = "Room: " + code;

        btnStart.gameObject.SetActive(PlayerSession.IsHost);
        btnStart.interactable = false;

        txtPlayer2.text = "Waiting for player...";
        imgReady2.color = colorNotReady;

        // Copy button setup
        if (btnCopyCode != null)
            btnCopyCode.onClick.AddListener(CopyRoomCode);

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

    // ─────────────────────────────────────────────────────
    private void CopyRoomCode()
    {
        string code = PlayerSession.RoomCode;

        GUIUtility.systemCopyBuffer = code;

        Log("ROOM CODE COPIED: " + code);

        txtStatus.text = "Room code copied!";
        Invoke(nameof(ResetStatusText), 2f);
    }

    private void ResetStatusText()
    {
        txtStatus.text = _players.Count < 2
            ? "Share your code: " + PlayerSession.RoomCode
            : "Waiting for both players to ready up...";
    }

    // ─────────────────────────────────────────────────────
    private void OnRoomChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null)
        {
            txtStatus.text = "Connection error.";
            return;
        }

        var snap = e.Snapshot;

        if (!snap.Exists)
        {
            txtStatus.text = "Room closed by host.";
            Invoke(nameof(ReturnToMenu), 2f);
            return;
        }

        string status = snap.Child("status").Value?.ToString();

        if (status == "starting")
        {
            _roomRef.ValueChanged -= OnRoomChanged;
            SceneManager.LoadScene(gameSceneName);
            return;
        }

        _hostUID = snap.Child("host").Value?.ToString() ?? "";

        _players.Clear();

        var playersSnap = snap.Child("players");

        foreach (var playerSnap in playersSnap.Children)
        {
            string uid = playerSnap.Key;
            string name = playerSnap.Child("name").Value?.ToString() ?? "Unknown";
            bool ready = playerSnap.Child("ready").Value?.ToString() == "True";

            _players[uid] = (name, ready);
        }

        RefreshUI();
    }

    // ─────────────────────────────────────────────────────
    private void RefreshUI()
    {
        bool allReady = _players.Count == 2;

        txtPlayer1.text = "Waiting...";
        txtPlayer2.text = "Waiting for player...";
        imgReady1.color = colorNotReady;
        imgReady2.color = colorNotReady;

        foreach (var kvp in _players)
        {
            bool isLocal = kvp.Key == PlayerSession.PlayerUID;
            bool isHost = kvp.Key == _hostUID;

            string name = kvp.Value.name + (isLocal ? " (You)" : "");
            bool ready = kvp.Value.ready;

            if (isHost)
            {
                txtPlayer1.text = name;
                imgReady1.color = ready ? colorReady : colorNotReady;
            }
            else
            {
                txtPlayer2.text = name;
                imgReady2.color = ready ? colorReady : colorNotReady;
            }

            if (!ready) allReady = false;
        }

        if (_players.Count < 2)
        {
            txtPlayer2.text = "Waiting for player...";
            allReady = false;
        }

        txtStatus.text = _players.Count < 2
            ? "Share your code: " + PlayerSession.RoomCode
            : allReady ? "All ready!" : "Waiting for both players...";

        if (PlayerSession.IsHost)
            btnStart.interactable = allReady;

        btnReady.GetComponentInChildren<TextMeshProUGUI>().text =
            _localReady ? "Not Ready" : "Ready";
    }

    // ─────────────────────────────────────────────────────
    public void OnToggleReady()
    {
        _localReady = !_localReady;

        _roomRef.Child("players")
            .Child(PlayerSession.PlayerUID)
            .Child("ready")
            .SetValueAsync(_localReady);
    }

    // ─────────────────────────────────────────────────────
    public void OnStartGame()
    {
        if (!PlayerSession.IsHost) return;

        btnStart.interactable = false;
        txtStatus.text = "Starting...";

        _roomRef.Child("status").SetValueAsync("starting");
    }

    // ─────────────────────────────────────────────────────
    public void OnLeave()
    {
        _roomRef.Child("players")
            .Child(PlayerSession.PlayerUID)
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