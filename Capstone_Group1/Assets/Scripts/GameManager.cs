using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameObject("GameManager").AddComponent<GameManager>();
                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }

    private int currentLevel;

    private void Awake()
    {
        // Make sure there is only one instance of GameManager
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    public void SetCurrentLevel(int level)
    {
        currentLevel = level;
    }

    public float CheckProgressPercentage()
    {
        // Placeholder implementation, replace with your actual progress calculation logic
        return 0.0f;
    }

    public void LoadProgress(float progressPercentage)
    {
        // Placeholder implementation, replace with your actual loading logic
        // Set the current level and load progress based on the provided percentage
        // For example:
        // currentLevel = CalculateLevelFromPercentage(progressPercentage);
    }

    // Save player progress
    public void SaveProgress(int currentLevel)
    {
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        PlayerPrefs.Save();
    }

    // Load player progress
    public int LoadProgress()
    {
        return PlayerPrefs.GetInt("CurrentLevel", 1); // Default to level 1 if not found
    }
}
