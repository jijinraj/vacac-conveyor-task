using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameUIController : MonoBehaviour
{
    // ======================================================
    // UI REFERENCES
    // ======================================================

    [Header("UI References")]
    public GameObject toolbarPanel;
    public GameObject pausePanel;

    [Header("Gameplay UI")]
    public GameObject inspectionHUD;
    public GameObject musicPanel;


    // ======================================================
    // SCENE SETTINGS
    // ======================================================

    [Header("Scene Settings")]

    [Tooltip(
        "Enable this in SandboxScene. " +
        "Disable it in GameScene."
    )]
    public bool showToolbarDuringGameplay = true;


    // ======================================================
    // RUNTIME
    // ======================================================

    private bool isPaused = false;


    // ======================================================
    // UNITY
    // ======================================================

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
        SetPaused(
            !isPaused
        );
    }


    public void PauseGame()
    {
        SetPaused(
            true
        );
    }


    public void ResumeGame()
    {
        SetPaused(
            false
        );
    }


    void SetPaused(
        bool paused
    )
    {
        isPaused =
            paused;


        Time.timeScale =
            paused
                ? 0f
                : 1f;


        // --------------------------------------------------
        // PAUSE MENU
        // --------------------------------------------------

        if (pausePanel != null)
        {
            pausePanel.SetActive(
                paused
            );
        }


        // --------------------------------------------------
        // SANDBOX TOOLBAR
        // --------------------------------------------------

        if (toolbarPanel != null)
        {
            toolbarPanel.SetActive(
                !paused &&
                showToolbarDuringGameplay
            );
        }


        // --------------------------------------------------
        // GAMEPLAY HUD
        // --------------------------------------------------

        if (inspectionHUD != null)
        {
            inspectionHUD.SetActive(
                !paused
            );
        }


        // --------------------------------------------------
        // MUSIC CONTROLS
        //
        // Audio itself keeps playing.
        // Only the controls are hidden while paused.
        // --------------------------------------------------

        if (musicPanel != null)
        {
            musicPanel.SetActive(
                !paused
            );
        }
    }


    // ======================================================
    // RESTART SHIFT
    // ======================================================

    public void RestartGame()
    {
        Time.timeScale =
            1f;


        Scene currentScene =
            SceneManager.GetActiveScene();


        SceneManager.LoadScene(
            currentScene.name
        );
    }


    // ======================================================
    // MAIN MENU
    // ======================================================

    public void ReturnToMainMenu()
    {
        Time.timeScale =
            1f;


        SceneManager.LoadScene(
            "MainMenu"
        );
    }


    // ======================================================
    // QUIT
    // ======================================================

    public void QuitGame()
    {
        Time.timeScale =
            1f;


#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying =
            false;
#else
        Application.Quit();
#endif
    }
}