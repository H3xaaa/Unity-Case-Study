using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneManagement : MonoBehaviour
{
    public Button startButton;
    public Button saveButton;

    private void Start()
    {
        // Assign button click listeners
        //SceneManager.LoadScene("Start Menu");
        startButton.onClick.AddListener(StartGame);
        saveButton.onClick.AddListener(SaveProgress);

        // Load the player's progress when the game starts
        int savedLevel = GameManager.Instance.LoadProgress();
        //SceneManager.LoadScene("Level " + savedLevel);
    }

    private void StartGame()
    {
        // Assume the player progresses to the next level when clicking the "Start" button
        int currentLevel = GameManager.Instance.GetCurrentLevel() + 1;
        GameManager.Instance.SetCurrentLevel(currentLevel);
        SceneManager.LoadScene("Level " + currentLevel);
    }

    private void SaveProgress()
    {
        // Save the player's progress when clicking the "Save" button
        GameManager.Instance.SaveProgress(GameManager.Instance.GetCurrentLevel());
    }
}