using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

// ============================================================
// MPWinnerUI.cs
// Attach to WinnerPanel (inactive by default)
// MPGameManager calls ShowResult() to activate it
// ============================================================

public class MPWinnerUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI txtResult;
    public TextMeshProUGUI txtSubtitle;

    [Header("Scenes")]
    public string playAgainScene = "MultiplayerGame";
    public string mainMenuScene = "Start Menu";

    private DatabaseReference _roomRef;

    private void Start()
    {
        gameObject.SetActive(false);

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
        gameObject.SetActive(true);
        if (txtResult != null) txtResult.text = result;
        if (txtSubtitle != null) txtSubtitle.text = subtitle;
    }

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