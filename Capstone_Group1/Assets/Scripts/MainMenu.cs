using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour 
{
	
	FadeInOut fade;

	void Start()
	{
		fade = FindObjectOfType<FadeInOut>();

	}

	public IEnumerator ChangeScene()
	{
		fade.FadeIn();
		yield return new WaitForSeconds(1);
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
	}

	public void PlayGame ()
	{
		StartCoroutine(ChangeScene());
	}

	public void QuitGame()
	{
		Debug.Log("QUIT");
		Application.Quit();
	}

}
