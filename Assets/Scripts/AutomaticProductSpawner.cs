using System.Collections;
using UnityEngine;

public class AutomaticProductSpawner : MonoBehaviour
{
    // ======================================================
    // REFERENCES
    // ======================================================

    [Header("References")]
    public ProductSpawner productSpawner;
    public InspectionGameController inspectionGameController;


    // ======================================================
    // SPAWN SETTINGS
    // ======================================================

    [Header("Spawn Timing")]
    public float initialDelay = 2f;

    public float minimumSpawnDelay = 4f;
    public float maximumSpawnDelay = 7f;


    [Header("Product Selection")]

    [Range(0f, 1f)]
    public float boxChance = 0.65f;


    // ======================================================
    // UNITY
    // ======================================================

    void Start()
    {
        StartCoroutine(
            SpawnLoop()
        );
    }


    // ======================================================
    // SPAWN LOOP
    // ======================================================

    IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(
            initialDelay
        );


        while (true)
        {
            // ----------------------------------------------
            // GAME OVER
            // ----------------------------------------------

            if (
                inspectionGameController != null &&
                inspectionGameController.IsGameOver
            )
            {
                yield break;
            }


            // ----------------------------------------------
            // CONVEYOR STOPPED
            // ----------------------------------------------

            if (
                inspectionGameController != null &&
                inspectionGameController.IsConveyorStopped
            )
            {
                yield return null;

                continue;
            }


            // ----------------------------------------------
            // SPAWN
            // ----------------------------------------------

            SpawnRandomProduct();


            // ----------------------------------------------
            // RANDOM DELAY
            // ----------------------------------------------

            float delay =
                Random.Range(
                    minimumSpawnDelay,
                    maximumSpawnDelay
                );


            yield return new WaitForSeconds(
                delay
            );
        }
    }


    // ======================================================
    // PRODUCT SELECTION
    // ======================================================

    void SpawnRandomProduct()
    {
        if (productSpawner == null)
        {
            Debug.LogWarning(
                "AutomaticProductSpawner has no ProductSpawner assigned."
            );

            return;
        }


        if (Random.value < boxChance)
        {
            productSpawner.SpawnBox();
        }
        else
        {
            productSpawner.SpawnCan();
        }
    }
}