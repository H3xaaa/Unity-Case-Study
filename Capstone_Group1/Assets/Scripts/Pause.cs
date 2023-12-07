using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pause : MonoBehaviour
{
    public void SettingButton()
    {
        ToggleTimePause();
    }
    void ToggleTimePause()
    {
        if (Time.timeScale == 0f)
        {
            // If the time scale is 0, unpause the game by setting the time scale to 1
            Time.timeScale = 1f;
        }
        else
        {
            // If the time scale is not 0, pause the game by setting the time scale to 0
            Time.timeScale = 0f;
        }
    }

    public void BackButton()
    {
        Time.timeScale = 1f;
    }
}
