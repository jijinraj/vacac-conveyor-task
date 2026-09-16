using UnityEngine;
using UnityEngine.InputSystem;

public class GameUIController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject toolbarPanel;
    public GameObject pausePanel;

    private bool isPaused = false;


    void Start()
    {
        SetPaused(false);
    }


    void Update()
    {
        if (
            Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame
        )
        {
            TogglePause();
        }
    }


    // ======================================================
    // PAUSE
    // ======================================================

    public void TogglePause()
    {
        SetPaused(!isPaused);
    }


    public void PauseGame()
    {
        SetPaused(true);
    }


    public void ResumeGame()
    {
        SetPaused(false);
    }


    void SetPaused(bool paused)
    {
        isPaused = paused;

        Time.timeScale =
            paused ? 0f : 1f;


        if (pausePanel != null)
        {
            pausePanel.SetActive(paused);
        }


        if (toolbarPanel != null)
        {
            toolbarPanel.SetActive(!paused);
        }
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