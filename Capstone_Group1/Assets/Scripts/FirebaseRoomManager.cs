// ============================================================
// FirebaseRoomManager.cs
// PHASE 2 — Firebase Realtime DB: create room / join room
//
// Requirements:
//   - Firebase Unity SDK installed (Realtime Database + Auth)
//   - Package: com.google.firebase.database, com.google.firebase.auth
//
// Scene setup (MultiplayerLobby scene):
//   Canvas:
//     [Panel] pnlRoomSelect  (active by default)
//         [Button]      btnCreate       → OnClick: OnCreateRoom()
//         [Button]      btnJoin         → OnClick: OnShowJoin()
//     [Panel] pnlJoinInput   (inactive by default)
//         [InputField]  inputJoinCode
//         [Button]      btnConfirmJoin  → OnClick: OnJoinRoom()
//         [Button]      btnBackToSelect → OnClick: OnBackToSelect()
//         [Text/TMP]    txtJoinError
//     [Text/TMP]        txtStatus       (shared status message)
// ============================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class FirebaseRoomManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pnlRoomSelect;
    [SerializeField] private GameObject pnlJoinInput;

    [Header("Join Panel")]
    [SerializeField] private TMP_InputField inputJoinCode;
    [SerializeField] private TextMeshProUGUI txtJoinError;

    [Header("Shared")]
    [SerializeField] private TextMeshProUGUI txtStatus;
    [SerializeField] private Button btnCreate;
    [SerializeField] private Button btnConfirmJoin;

    [Header("Scene Names")]
    [SerializeField] private string lobbyWaitScene = "LobbyWait";

    private DatabaseReference _db;
    private FirebaseAuth _auth;
    private bool _firebaseReady = false;

    // ── Init ──────────────────────────────────────────────────
    private void Start()
    {
        pnlRoomSelect.SetActive(true);
        pnlJoinInput.SetActive(false);
        txtStatus.text = "Connecting…";
        SetButtonsInteractable(false);

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            Debug.Log("Firebase check result: " + task.Result);
            if (task.Result == DependencyStatus.Available)
            {
                _auth = FirebaseAuth.DefaultInstance;
                _db = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance,
                    "https://starlandsexam-default-rtdb.asia-southeast1.firebasedatabase.app")
                    .RootReference;
                Debug.Log("Firebase connected, signing in...");
                SignInAnonymously();
            }
            else
            {
                txtStatus.text = "Firebase error: " + task.Result;
                Debug.Log("Firebase FAILED: " + task.Result);
            }
        });
    }

    private void SignInAnonymously()
    {
        _auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.Log("Auth failed: " + task.Exception);
                txtStatus.text = "Auth failed. Check internet.";
                return;
            }
            Debug.Log("Auth success! UID: " + task.Result.User.UserId);
            PlayerSession.SetUID(task.Result.User.UserId);
            _firebaseReady = true;
            txtStatus.text = "Ready. Welcome, " + PlayerSession.PlayerName + "!";
            SetButtonsInteractable(true);
        });
    }

    // ── Create Room ───────────────────────────────────────────
    public void OnCreateRoom()
    {
        if (!_firebaseReady) return;

        string code = GenerateRoomCode();
        SetButtonsInteractable(false);
        txtStatus.text = "Creating room…";

        var roomData = new System.Collections.Generic.Dictionary<string, object>
        {
            { "status", "waiting" },
            { "host",   PlayerSession.PlayerUID }
        };

        var playerData = new System.Collections.Generic.Dictionary<string, object>
        {
            { "name",  PlayerSession.PlayerName },
            { "ready", false },
            { "hp",    100 },
            { "wins",  0 }
        };

        // Write room node then player node
        _db.Child("rooms").Child(code).SetValueAsync(roomData)
            .ContinueWithOnMainThread(t1 =>
            {
                if (t1.IsFaulted)
                {
                    txtStatus.text = "Failed to create room. Try again.";
                    SetButtonsInteractable(true);
                    return;
                }

                _db.Child("rooms").Child(code).Child("players")
                   .Child(PlayerSession.PlayerUID).SetValueAsync(playerData)
                   .ContinueWithOnMainThread(t2 =>
                   {
                       if (t2.IsFaulted)
                       {
                           txtStatus.text = "Failed to join room. Try again.";
                           SetButtonsInteractable(true);
                           return;
                       }

                       PlayerSession.SetRoom(code, isHost: true);
                       RegisterOnDisconnect();
                       SceneManager.LoadScene(lobbyWaitScene);
                   });
            });
    }

    // ── Join Room ─────────────────────────────────────────────
    public void OnShowJoin()
    {
        pnlRoomSelect.SetActive(false);
        pnlJoinInput.SetActive(true);
        inputJoinCode.ActivateInputField();
    }

    public void OnBackToSelect()
    {
        pnlJoinInput.SetActive(false);
        pnlRoomSelect.SetActive(true);
        txtJoinError.text = "";
    }

    public void OnJoinRoom()
    {
        if (!_firebaseReady) return;

        string code = inputJoinCode.text.Trim().ToUpper();
        if (code.Length != 6)
        {
            txtJoinError.text = "Code must be 6 characters.";
            return;
        }

        txtJoinError.text = "";
        SetButtonsInteractable(false);
        txtStatus.text = "Joining room…";

        // Read the room first to validate
        _db.Child("rooms").Child(code).GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || !task.Result.Exists)
                {
                    txtJoinError.text = "Room not found.";
                    SetButtonsInteractable(true);
                    return;
                }

                var roomSnap = task.Result;

                // Check status
                string status = roomSnap.Child("status").Value?.ToString();
                if (status != "waiting")
                {
                    txtJoinError.text = "Room already started or full.";
                    SetButtonsInteractable(true);
                    return;
                }

                // Check player count — max 2
                long playerCount = roomSnap.Child("players").ChildrenCount;
                if (playerCount >= 2)
                {
                    txtJoinError.text = "Room is full (max 2 players).";
                    SetButtonsInteractable(true);
                    return;
                }

                // Write self as second player
                var playerData = new System.Collections.Generic.Dictionary<string, object>
            {
                { "name",  PlayerSession.PlayerName },
                { "ready", false },
                { "hp",    100 },
                { "wins",  0 }
            };

                _db.Child("rooms").Child(code).Child("players")
                   .Child(PlayerSession.PlayerUID).SetValueAsync(playerData)
                   .ContinueWithOnMainThread(t2 =>
                   {
                       if (t2.IsFaulted)
                       {
                           txtJoinError.text = "Could not join. Try again.";
                           SetButtonsInteractable(true);
                           return;
                       }

                       PlayerSession.SetRoom(code, isHost: false);
                       RegisterOnDisconnect();
                       SceneManager.LoadScene(lobbyWaitScene);
                   });
            });
    }

    // ── Auto-remove player node on disconnect ─────────────────
    // Firebase onDisconnect — cleans up if player closes the game
    private void RegisterOnDisconnect()
    {
        _db.Child("rooms").Child(PlayerSession.RoomCode)
           .Child("players").Child(PlayerSession.PlayerUID)
           .OnDisconnect().RemoveValue();
    }

    // ── Helpers ───────────────────────────────────────────────
    private string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no O/0 confusion
        char[] code = new char[6];
        for (int i = 0; i < 6; i++)
            code[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
        return new string(code);
    }

    private void SetButtonsInteractable(bool state)
    {
        if (btnCreate) btnCreate.interactable = state;
        if (btnConfirmJoin) btnConfirmJoin.interactable = state;
    }
}