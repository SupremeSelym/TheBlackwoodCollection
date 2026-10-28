
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;

    public GameObject pauseMenuUI;

    void Start()
    {
        pauseMenuUI.SetActive(false);
        GameIsPaused = false;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        GameIsPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    void Pause()
    {
        pauseMenuUI.SetActive(true);
        GameIsPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LoadMenu()

    {
        GameIsPaused = false;
        Time.timeScale = 1f;
        
        if(NetworkManager.Singleton != null)
        {
            var nm = NetworkManager.Singleton;
            nm.Shutdown();
            Destroy(nm.gameObject);
        }

        SceneManager.LoadScene("Main Menu Scene");
    }

    public void QuitGame()
    {

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        Debug.Log("Quitting game...");
        Application.Quit();
    }
}