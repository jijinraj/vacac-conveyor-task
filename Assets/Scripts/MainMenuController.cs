using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // ======================================================
    // PLAY ACTUAL GAME
    // ======================================================

    public void PlayGame()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene("GameScene");
    }


    // ======================================================
    // TEST / SANDBOX
    // ======================================================

    public void OpenSandbox()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene("SandboxScene");
    }


    // ======================================================
    // QUIT
    // ======================================================

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}