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
            return waitingForGoodToGo || gameOver;
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
            goodToGoButton.interactable = false;
        }


        if (dangerButton != null)
        {
            dangerButton.interactable = true;
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
            AddWarning(
                "FALSE IDENTIFICATION"
            );
        }


        if (scannerZone != null)
        {
            scannerZone.ClearCurrentCargo();
        }


        UpdateHUD();
    }


    // ======================================================
    // GOOD TO GO
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

        waitingForGoodToGo =
            false;


        PauseAllProducts();


        SetResult(
            "EXPLOSION\nBOMB MISSED — GAME OVER"
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


        Debug.LogWarning(
            "GAME OVER: Bomb reached the end of the conveyor."
        );


        // Later:
        // explosion VFX
        // explosion SFX
        // screen shake
        // dedicated Game Over panel
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

        waitingForGoodToGo =
            false;


        PauseAllProducts();


        SetResult(
            $"EMPLOYMENT TERMINATED\n{maximumWarnings} SECURITY WARNINGS"
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


        Debug.LogWarning(
            "GAME OVER: Maximum security warnings reached."
        );


        // Later:
        // termination letter
        // final score
        // restart button
        // main menu button
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