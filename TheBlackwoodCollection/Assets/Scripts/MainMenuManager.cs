using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class MainMenuManager : MonoBehaviour
{

    public static bool StartAsHost;

    public void HostGame()
    {
        StartAsHost = true;
        SceneManager.LoadScene("MU-FRAME");
    }

    public void JoinGame()
    {
        StartAsHost = false;
        SceneManager.LoadScene("MU-FRAME");
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}

