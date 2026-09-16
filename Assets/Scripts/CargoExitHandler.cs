using UnityEngine;

public class CargoExitHandler : MonoBehaviour
{
    private InspectionGameController gameController;
    private CargoContents cargoContents;

    private bool exitReported = false;


    void Awake()
    {
        cargoContents =
            GetComponent<CargoContents>();
    }


    void Start()
    {
        gameController =
            FindFirstObjectByType<InspectionGameController>();
    }


    public void ReportReachedConveyorEnd()
    {
        if (exitReported)
            return;


        exitReported =
            true;


        if (cargoContents == null)
        {
            cargoContents =
                GetComponent<CargoContents>();
        }


        if (gameController == null)
        {
            gameController =
                FindFirstObjectByType<InspectionGameController>();
        }


        if (
            gameController != null &&
            cargoContents != null
        )
        {
            gameController.HandleCargoReachedEnd(
                cargoContents
            );
        }
    }
}