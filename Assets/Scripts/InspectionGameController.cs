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
    public Button goodToGoButton;


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
    // RUNTIME
    // ======================================================

    private int score = 0;
    private int warnings = 0;

    private bool waitingForGoodToGo = false;
    private bool gameOver = false;

    private CargoContents inspectedCargo;

    private readonly List<ProductMover> pausedProducts =
        new List<ProductMover>();


    // ======================================================
    // PUBLIC GAME STATE
    // ======================================================

    public bool IsConveyorStopped
    {
        get
        {
            return
                waitingForGoodToGo ||
                gameOver;
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
        if (goodToGoButton != null)
        {
            goodToGoButton.interactable =
                false;
        }


        if (dangerButton != null)
        {
            dangerButton.interactable =
                true;
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
            waitingForGoodToGo
        )
        {
            return;
        }


        // There must be a cargo currently available
        // inside the scanner decision window.

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


        // Stop all moving products immediately.

        PauseAllProducts();


        waitingForGoodToGo =
            true;


        if (dangerButton != null)
        {
            dangerButton.interactable =
                false;
        }


        if (goodToGoButton != null)
        {
            goodToGoButton.interactable =
                true;
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
            warnings++;


            SetResult(
                $"FALSE IDENTIFICATION\nWARNING {warnings} / {maximumWarnings}"
            );


            if (
                warnings >=
                maximumWarnings
            )
            {
                TriggerTermination();
            }
        }


        if (scannerZone != null)
        {
            scannerZone.ClearCurrentCargo();
        }


        UpdateHUD();
    }


    // ======================================================
    // GOOD TO GO BUTTON
    // ======================================================

    public void PressGoodToGo()
    {
        if (
            gameOver ||
            !waitingForGoodToGo
        )
        {
            return;
        }


        ResumeAllProducts();


        inspectedCargo =
            null;


        waitingForGoodToGo =
            false;


        if (dangerButton != null)
        {
            dangerButton.interactable =
                true;
        }


        if (goodToGoButton != null)
        {
            goodToGoButton.interactable =
                false;
        }


        SetResult(
            "SCANNING..."
        );
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
    // TERMINATION
    // ======================================================

    void TriggerTermination()
    {
        gameOver =
            true;


        waitingForGoodToGo =
            false;


        SetResult(
            "EMPLOYMENT TERMINATED\n3 SECURITY WARNINGS"
        );


        if (dangerButton != null)
        {
            dangerButton.interactable =
                false;
        }


        if (goodToGoButton != null)
        {
            goodToGoButton.interactable =
                false;
        }


        // The products remain stopped.
        //
        // Later this will be replaced with the proper
        // termination-letter / Game Over screen.
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