using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour {

	public void Play1()
    {
        SceneManager.LoadScene(1);
    }

    public void Play2()
    {
        SceneManager.LoadScene(2);
    }
}
