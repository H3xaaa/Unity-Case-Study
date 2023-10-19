using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FadeInOut : MonoBehaviour
{
    public CanvasGroup canvasgroup;
    public bool fadein = false;
    public bool fadeout = false;
    public float TimetoFade;

    // Update is called once per frame
    void Update()
    {
        if (fadein == true)  
        {
            if (canvasgroup.alpha < 1)
            {
                canvasgroup.alpha += TimetoFade * Time.deltaTime;
                if (canvasgroup.alpha >= 1)
                {
                    fadein = false;
                }
            }
        }
        if (fadeout == true)  
        {
            if (canvasgroup.alpha >= 0)
            {
                canvasgroup.alpha -= TimetoFade * Time.deltaTime;
                if (canvasgroup.alpha == 0)
                {
                    fadeout = false;
                }
            }
        }
    }

    public void FadeIn()
    {
        fadein = true;
    }

    public void FadeOut()
    {
        fadeout = true; 
    }
}
