using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

// ============================================================
// MPWinnerUI.cs
// Has separate WIN and LOSE panels
// Attach to a parent GameObject that holds both panels
// ============================================================

public class MPWinnerUI : MonoBehaviour
{
    [Header("Win Panel")]
    public GameObject winPanel;           // active when you win
    public TextMeshProUGUI txtWinTitle;   // "YOU WIN!"
    public TextMeshProUGUI txtWinSub;     // "Enemy eliminated!"

    [Header("Lose Panel")]
    public GameObject losePanel;          // active when you lose
    public TextMeshProUGUI txtLoseTitle;  // "YOU LOSE"
    public TextMeshProUGUI txtLoseSub;    // subtitle

    [Header("Shared Buttons")]
    // These can be inside both panels or shared
    // Wire OnClick in Inspector to these methods

    [Header("Scenes")]
    public string playAgainScene = "MultiplayerGame";
    public string mainMenuScene = "Start Menu";

    private DatabaseReference _roomRef;

    private void Awake()
    {
        // Hide both panels on start
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        gameObject.SetActive(true); // parent stays active
    }

    private void Start()
    {
        string code = PlayerSession.RoomCode;
        if (!string.IsNullOrEmpty(code))
        {
            _roomRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance,
                "https://starlandsexam-default-rtdb.asia-southeast1.firebasedatabase.app")
                .RootReference.Child("rooms").Child(code);
        }
    }

    // Called by MPGameManager
    public void ShowResult(string result, string subtitle = "")
    {
        bool won = result.Contains("WIN");

        if (won)
        {
            if (winPanel != null) winPanel.SetActive(true);
            if (losePanel != null) losePanel.SetActive(false);
            if (txtWinTitle != null) txtWinTitle.text = result;
            if (txtWinSub != null) txtWinSub.text = subtitle;
        }
        else if (result.Contains("DRAW"))
        {
            // Show both or win panel with draw text
            if (winPanel != null) winPanel.SetActive(true);
            if (txtWinTitle != null) txtWinTitle.text = "DRAW!";
            if (txtWinSub != null) txtWinSub.text = "";
        }
        else
        {
            if (losePanel != null) losePanel.SetActive(true);
            if (winPanel != null) winPanel.SetActive(false);
            if (txtLoseTitle != null) txtLoseTitle.text = result;
            if (txtLoseSub != null) txtLoseSub.text = subtitle;
        }
    }

    // ── Buttons ───────────────────────────────────────────────
    public void OnPlayAgain()
    {
        if (_roomRef != null && PlayerSession.IsHost)
        {
            _roomRef.Child("players_hp").Child("p1").SetValueAsync(3);
            _roomRef.Child("players_hp").Child("p2").SetValueAsync(3);
            _roomRef.Child("status").SetValueAsync("playing");
            _roomRef.Child("leaver").RemoveValueAsync();
        }
        SceneManager.LoadScene(playAgainScene);
    }

    public void OnLeave()
    {
        if (_roomRef != null)
        {
            _roomRef.Child("leaver").SetValueAsync(PlayerSession.PlayerUID)
                .ContinueWithOnMainThread(_ =>
                {
                    if (PlayerSession.IsHost) _roomRef.RemoveValueAsync();
                    PlayerSession.Clear();
                    SceneManager.LoadScene(mainMenuScene);
                });
        }
        else
        {
            PlayerSession.Clear();
            SceneManager.LoadScene(mainMenuScene);
        }
    }
}