using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InspectionGameController : MonoBehaviour
{
    // ======================================================
    // REFERENCES
    // ======================================================

    [Header("Game Systems")]
    public ScannerZone scannerZone;


    [Header("HUD")]
    public TMP_Text scoreText;
    public TMP_Text warningText;
    public TMP_Text resultText;

    public Button dangerButton;


    // ======================================================
    // SCORING
    // ======================================================

    [Header("Scoring")]
    public int prohibitedItemPoints = 100;
    public int bombPoints = 250;


    // ======================================================
    // WARNINGS
    // ======================================================

    [Header("Warnings")]
    public int maximumWarnings = 3;


    // ======================================================
    // AUTOMATIC RESUME
    // ======================================================

    [Header("Automatic Resume")]

    [Tooltip(
        "How long the conveyor stays stopped after pressing DANGER."
    )]
    public float automaticResumeDelay = 0.5f;


    // ======================================================
    // SCORE MILESTONES
    // ======================================================

    [Header("Score Milestones")]

    public GameObject milestonePanel;
    public TMP_Text milestoneText;

    [Tooltip(
        "Audio source used only for milestone / victory sounds."
    )]
    public AudioSource milestoneAudioSource;

    [Tooltip(
        "Short victory jingle played when a milestone is reached."
    )]
    public AudioClip milestoneVictoryClip;

    [Tooltip(
        "How long the milestone message stays visible."
    )]
    public float milestoneDisplayDuration = 1.5f;


    // ======================================================
    // RUNTIME
    // ======================================================

    private int score = 0;
    private int warnings = 0;

    private bool decisionPauseActive = false;
    private bool gameOver = false;

    private CargoContents inspectedCargo;

    private readonly List<ProductMover> pausedProducts =
        new List<ProductMover>();

    private Coroutine automaticResumeCoroutine;
    private Coroutine milestoneCoroutine;


    // ======================================================
    // MILESTONE DATA
    // ======================================================

    private readonly int[] scoreMilestones =
    {
        100,
        500,
        1000,
        2000,
        3000
    };


    private readonly string[] milestoneMessages =
    {
        "FIRST CATCH!",
        "SHARP EYES!",
        "SECURITY PRO!",
        "ELITE INSPECTOR!",
        "MASTER INSPECTOR!"
    };


    private readonly HashSet<int> reachedMilestones =
        new HashSet<int>();


    // ======================================================
    // PUBLIC GAME STATE
    // ======================================================

    public bool IsConveyorStopped
    {
        get
        {
            return decisionPauseActive || gameOver;
        }
    }


    public bool IsGameOver
    {
        get
        {
            return gameOver;
        }
    }


    public int Score
    {
        get
        {
            return score;
        }
    }


    public int Warnings
    {
        get
        {
            return warnings;
        }
    }


    // ======================================================
    // UNITY
    // ======================================================

    void Start()
    {
        if (dangerButton != null)
        {
            dangerButton.interactable = true;
        }


        if (milestonePanel != null)
        {
            milestonePanel.SetActive(false);
        }


        UpdateHUD();


        SetResult(
            "SCANNING..."
        );
    }


    // ======================================================
    // DANGER BUTTON
    // ======================================================

    public void PressDanger()
    {
        if (
            gameOver ||
            decisionPauseActive
        )
        {
            return;
        }


        if (
            scannerZone == null ||
            scannerZone.currentCargo == null
        )
        {
            SetResult(
                "NO ACTIVE SCAN"
            );

            return;
        }


        inspectedCargo =
            scannerZone.currentCargo;


        // --------------------------------------------------
        // STOP CONVEYOR
        // --------------------------------------------------

        PauseAllProducts();

        decisionPauseActive =
            true;


        if (dangerButton != null)
        {
            dangerButton.interactable =
                false;
        }


        // ==================================================
        // BOMB
        // ==================================================

        if (
            inspectedCargo.ContainsBomb()
        )
        {
            score +=
                bombPoints;


            CheckScoreMilestones();


            SetResult(
                $"EXPLOSIVE DEVICE CONFIRMED\n+{bombPoints} POINTS"
            );


            ConfiscateCargo(
                inspectedCargo
            );
        }


        // ==================================================
        // GUN / KNIFE
        // ==================================================

        else if (
            inspectedCargo.ContainsProhibitedItem()
        )
        {
            score +=
                prohibitedItemPoints;


            CheckScoreMilestones();


            SetResult(
                $"THREAT CONFIRMED\n+{prohibitedItemPoints} POINTS"
            );


            ConfiscateCargo(
                inspectedCargo
            );
        }


        // ==================================================
        // SAFE CARGO — FALSE IDENTIFICATION
        // ==================================================

        else
        {
            AddWarning(
                "FALSE IDENTIFICATION"
            );
        }


        if (scannerZone != null)
        {
            scannerZone.ClearCurrentCargo();
        }


        UpdateHUD();


        // --------------------------------------------------
        // AUTOMATIC RESUME
        // --------------------------------------------------

        if (!gameOver)
        {
            if (automaticResumeCoroutine != null)
            {
                StopCoroutine(
                    automaticResumeCoroutine
                );
            }


            automaticResumeCoroutine =
                StartCoroutine(
                    AutomaticResumeRoutine()
                );
        }
    }


    // ======================================================
    // AUTOMATIC RESUME
    // ======================================================

    IEnumerator AutomaticResumeRoutine()
    {
        yield return new WaitForSeconds(
            automaticResumeDelay
        );


        automaticResumeCoroutine =
            null;


        if (gameOver)
        {
            yield break;
        }


        ResumeAllProducts();


        inspectedCargo =
            null;


        decisionPauseActive =
            false;


        if (dangerButton != null)
        {
            dangerButton.interactable =
                true;
        }


        SetResult(
            "SCANNING..."
        );
    }


    // ======================================================
    // SCORE MILESTONES
    // ======================================================

    void CheckScoreMilestones()
    {
        for (
            int i = 0;
            i < scoreMilestones.Length;
            i++
        )
        {
            int milestone =
                scoreMilestones[i];


            if (
                score >= milestone &&
                !reachedMilestones.Contains(milestone)
            )
            {
                reachedMilestones.Add(
                    milestone
                );


                ShowMilestone(
                    milestone,
                    milestoneMessages[i]
                );


                break;
            }
        }
    }


    void ShowMilestone(
        int milestone,
        string message
    )
    {
        if (milestoneCoroutine != null)
        {
            StopCoroutine(
                milestoneCoroutine
            );
        }


        milestoneCoroutine =
            StartCoroutine(
                ShowMilestoneRoutine(
                    milestone,
                    message
                )
            );
    }


    IEnumerator ShowMilestoneRoutine(
        int milestone,
        string message
    )
    {
        if (milestonePanel != null)
        {
            milestonePanel.SetActive(
                true
            );
        }


        if (milestoneText != null)
        {
            milestoneText.text =
                $"{milestone} POINTS!\n{message}";
        }


        if (
            milestoneAudioSource != null &&
            milestoneVictoryClip != null
        )
        {
            milestoneAudioSource.PlayOneShot(
                milestoneVictoryClip
            );
        }


        yield return new WaitForSeconds(
            milestoneDisplayDuration
        );


        if (milestonePanel != null)
        {
            milestonePanel.SetActive(
                false
            );
        }


        milestoneCoroutine =
            null;
    }


    // ======================================================
    // CARGO REACHED END
    // ======================================================

    public void HandleCargoReachedEnd(
        CargoContents cargo
    )
    {
        if (
            cargo == null ||
            gameOver
        )
        {
            return;
        }


        Debug.Log(
            $"Cargo reached conveyor end [{cargo.gameObject.name}] | " +
            $"Risk: {cargo.GetRiskLevel()} | " +
            $"Contents: {string.Join(", ", cargo.items)}"
        );


        // ==================================================
        // MISSED BOMB
        // ==================================================

        if (
            cargo.ContainsBomb()
        )
        {
            TriggerExplosion();

            return;
        }


        // ==================================================
        // MISSED GUN / KNIFE
        // ==================================================

        if (
            cargo.ContainsProhibitedItem()
        )
        {
            AddWarning(
                "MISSED SECURITY THREAT"
            );

            return;
        }


        // ==================================================
        // SAFE CARGO
        // ==================================================

        Debug.Log(
            "Safe cargo cleared successfully."
        );
    }


    // ======================================================
    // WARNINGS
    // ======================================================

    void AddWarning(
        string reason
    )
    {
        if (gameOver)
            return;


        warnings++;


        SetResult(
            $"{reason}\nWARNING {warnings} / {maximumWarnings}"
        );


        UpdateHUD();


        if (
            warnings >=
            maximumWarnings
        )
        {
            TriggerTermination();
        }
    }


    // ======================================================
    // STOP PRODUCTS
    // ======================================================

    void PauseAllProducts()
    {
        pausedProducts.Clear();


        ProductMover[] products =
            FindObjectsByType<ProductMover>(
                FindObjectsSortMode.None
            );


        foreach (
            ProductMover product
            in products
        )
        {
            if (
                product != null &&
                product.enabled
            )
            {
                product.enabled =
                    false;


                pausedProducts.Add(
                    product
                );
            }
        }
    }


    // ======================================================
    // RESUME PRODUCTS
    // ======================================================

    void ResumeAllProducts()
    {
        foreach (
            ProductMover product
            in pausedProducts
        )
        {
            if (product != null)
            {
                product.enabled =
                    true;
            }
        }


        pausedProducts.Clear();
    }


    // ======================================================
    // CONFISCATE CARGO
    // ======================================================

    void ConfiscateCargo(
        CargoContents cargo
    )
    {
        if (cargo == null)
            return;


        Destroy(
            cargo.gameObject
        );
    }


    // ======================================================
    // MISSED BOMB
    // ======================================================

    void TriggerExplosion()
    {
        if (gameOver)
            return;


        gameOver =
            true;

        decisionPauseActive =
            false;


        if (automaticResumeCoroutine != null)
        {
            StopCoroutine(
                automaticResumeCoroutine
            );

            automaticResumeCoroutine =
                null;
        }


        PauseAllProducts();


        SetResult(
            "EXPLOSION\nBOMB MISSED — GAME OVER"
        );


        if (dangerButton != null)
        {
            dangerButton.interactable =
                false;
        }


        Debug.LogWarning(
            "GAME OVER: Bomb reached the end of the conveyor."
        );
    }


    // ======================================================
    // TERMINATION
    // ======================================================

    void TriggerTermination()
    {
        if (gameOver)
            return;


        gameOver =
            true;

        decisionPauseActive =
            false;


        if (automaticResumeCoroutine != null)
        {
            StopCoroutine(
                automaticResumeCoroutine
            );

            automaticResumeCoroutine =
                null;
        }


        PauseAllProducts();


        SetResult(
            $"EMPLOYMENT TERMINATED\n{maximumWarnings} SECURITY WARNINGS"
        );


        if (dangerButton != null)
        {
            dangerButton.interactable =
                false;
        }


        Debug.LogWarning(
            "GAME OVER: Maximum security warnings reached."
        );
    }


    // ======================================================
    // HUD
    // ======================================================

    void UpdateHUD()
    {
        if (scoreText != null)
        {
            scoreText.text =
                $"SCORE: {score}";
        }


        if (warningText != null)
        {
            warningText.text =
                $"WARNINGS: {warnings} / {maximumWarnings}";
        }
    }


    void SetResult(
        string message
    )
    {
        if (resultText != null)
        {
            resultText.text =
                message;
        }
    }
}