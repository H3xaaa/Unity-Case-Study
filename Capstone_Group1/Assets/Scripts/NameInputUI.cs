using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class NameInputUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField inputPlayerName;
    [SerializeField] private TextMeshProUGUI txtError;
    [SerializeField] private Button btnConfirm;

    [Header("Scene to load after name is set")]
    [SerializeField] private string mpLobbyScene = "MultiplayerLobby";

    private void Start()
    {
        // Pre-fill name if they already set one this session
        if (!string.IsNullOrEmpty(PlayerSession.PlayerName))
            inputPlayerName.text = PlayerSession.PlayerName;

        txtError.text = "";
    }

    public void OnConfirm()
    {
        string playerName = inputPlayerName.text.Trim();

        // Validation
        if (string.IsNullOrEmpty(playerName))
        {
            txtError.text = "Please enter a name.";
            return;
        }
        if (playerName.Length > 16)
        {
            txtError.text = "Name must be 16 characters or less.";
            return;
        }

        // Save name globally
        PlayerSession.SetPlayerName(playerName);
        txtError.text = "";

        // Go to multiplayer lobby
        SceneManager.LoadScene(mpLobbyScene);
    }

    // Called when user types — clears error message live
    public void OnNameChanged()
    {
        txtError.text = "";
    }
}