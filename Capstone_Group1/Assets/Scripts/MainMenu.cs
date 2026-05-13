using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;      // your existing main menu panel
    [SerializeField] private GameObject nameInputPanel;     // new panel for MP name input

    [Header("Scene Names")]
    [SerializeField] private string storySceneName = "";    // leave empty to use buildIndex + 1
    [SerializeField] private string mpSceneName = "MultiplayerLobby";

    // ── Existing buttons (unchanged) ─────────────────────────

    public void QuitGame()
    {
        Debug.Log("QUIT");
        Application.Quit();
    }

    public void StartButton()
    {
        // Your original story mode logic — untouched
        SceneManager.LoadScene(storySceneName);
    }

    public void SettingsButton()
    {
        // Your existing settings logic here
    }

    // ── New: Multiplayer button ───────────────────────────────

    public void MultiplayerButton()
    {
        // Hide main menu, show name input panel
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (nameInputPanel != null) nameInputPanel.SetActive(true);
    }

    public void BackToMainMenu()
    {
        // Back button inside name input panel
        if (nameInputPanel != null) nameInputPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }
}