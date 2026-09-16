using UnityEngine;

public class ScannerZone : MonoBehaviour
{
    [Header("Scanner")]
    public ScannerDisplay scannerDisplay;

    [Header("Runtime")]
    public CargoContents currentCargo;

    void OnTriggerEnter(Collider other)
    {
        CargoContents cargo =
            other.GetComponentInParent<CargoContents>();

        if (cargo == null)
            return;

        // Prevent duplicate triggering from multiple colliders.
        if (cargo == currentCargo)
            return;

        currentCargo = cargo;

        Debug.Log(
            $"Scanner detected {cargo.gameObject.name} | " +
            $"Risk: {cargo.GetRiskLevel()} | " +
            $"Contents: {string.Join(", ", cargo.items)}"
        );

        if (scannerDisplay != null)
        {
            scannerDisplay.ShowCargo(cargo);
        }
    }


    void OnTriggerExit(Collider other)
    {
        CargoContents cargo =
            other.GetComponentInParent<CargoContents>();

        if (
            cargo != null &&
            cargo == currentCargo
        )
        {
            currentCargo = null;
        }
    }
}