using System.Collections;
using UnityEngine;

public class ScannerZone : MonoBehaviour
{
    [Header("Scanner")]
    public ScannerDisplay scannerDisplay;

    [Header("Runtime")]
    public CargoContents currentCargo;

    private Coroutine clearCargoCoroutine;


    void OnTriggerEnter(Collider other)
    {
        CargoContents cargo =
            other.GetComponentInParent<CargoContents>();

        if (cargo == null)
            return;


        // Prevent duplicate events from the same cargo.
        if (cargo == currentCargo)
            return;


        currentCargo = cargo;


        Debug.Log(
            $"Scanner detected [{cargo.gameObject.name}] " +
            $"Risk: {cargo.GetRiskLevel()} | " +
            $"Contents: {string.Join(", ", cargo.items)}"
        );


        if (scannerDisplay != null)
        {
            scannerDisplay.ShowCargo(cargo);
        }


        // Keep this cargo available for the same amount of
        // time that the X-ray is visible.

        if (clearCargoCoroutine != null)
        {
            StopCoroutine(clearCargoCoroutine);
        }


        clearCargoCoroutine =
            StartCoroutine(
                ClearCargoAfterDecisionWindow(cargo)
            );
    }


    IEnumerator ClearCargoAfterDecisionWindow(
        CargoContents cargo
    )
    {
        float duration =
            scannerDisplay != null
                ? scannerDisplay.displayDuration
                : 2.5f;


        yield return new WaitForSeconds(
            duration
        );


        if (currentCargo == cargo)
        {
            currentCargo = null;
        }


        clearCargoCoroutine = null;
    }


    public void ClearCurrentCargo()
    {
        currentCargo = null;


        if (clearCargoCoroutine != null)
        {
            StopCoroutine(
                clearCargoCoroutine
            );

            clearCargoCoroutine = null;
        }
    }
}