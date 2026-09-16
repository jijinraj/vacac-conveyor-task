using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RandomConveyorGenerator : MonoBehaviour
{
    // ======================================================
    // PREFABS
    // ======================================================

    [Header("Conveyor Prefabs")]

    public GameObject genericConveyorPrefab;
    public GameObject shortConveyorPrefab;
    public GameObject inclineConveyorPrefab;


    // ======================================================
    // GENERATION
    // ======================================================

    [Header("Random Structure")]

    [Tooltip("Minimum number of conveyors generated.")]
    public int minimumSegments = 6;

    [Tooltip("Maximum number of conveyors generated.")]
    public int maximumSegments = 12;

    [Tooltip(
        "When enabled, the first conveyor will always be " +
        "Generic or Short so the structure begins flat."
    )]
    public bool firstSegmentMustBeFlat = true;

    [Tooltip(
        "Maximum number of Incline conveyors allowed consecutively."
    )]
    public int maximumConsecutiveInclines = 2;


    // ======================================================
    // RANDOM WEIGHTS
    // ======================================================

    [Header("Random Weights")]

    [Min(0f)]
    public float genericWeight = 0.45f;

    [Min(0f)]
    public float shortWeight = 0.30f;

    [Min(0f)]
    public float inclineWeight = 0.25f;


    // ======================================================
    // GENERATION ORIGIN
    // ======================================================

    [Header("Generation Origin")]

    [Tooltip(
        "Optional starting point for the generated structure. " +
        "If empty, this object's Transform is used."
    )]
    public Transform generationOrigin;


    // ======================================================
    // SNAP CALIBRATION
    // ======================================================

    [Header("Generic Incoming Calibration")]

    public float genericAfterGenericStartZ = -1.68f;
    public float genericAfterShortStartZ = -1.22f;
    public float genericAfterInclineStartZ = -1.50f;


    [Header("Short Incoming Calibration")]

    public float shortAfterGenericStartZ = -1.38f;
    public float shortAfterShortStartZ = -0.91f;
    public float shortAfterInclineStartZ = -1.22f;


    [Header("Incline Incoming Calibration")]

    public float inclineAfterGenericStartZ = -0.05f;
    public float inclineAfterShortStartZ = 0.2955f;
    public float inclineAfterInclineStartZ = -0.03f;


    // ======================================================
    // INTERNAL STATE
    // ======================================================

    private readonly List<GameObject> generatedConveyors =
        new List<GameObject>();

    private Transform generatedRoot;


    // ======================================================
    // UNITY LIFECYCLE
    // ======================================================

    void Update()
    {
        if (Keyboard.current == null)
            return;


        // --------------------------------------------------
        // G = GENERATE RANDOM STRUCTURE
        // --------------------------------------------------

        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            GenerateRandomStructure();
        }


        // --------------------------------------------------
        // C = CLEAR GENERATED STRUCTURE
        // --------------------------------------------------

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            ClearGeneratedStructure();
        }
    }


    // ======================================================
    // RANDOM STRUCTURE GENERATION
    // ======================================================

    public void GenerateRandomStructure()
    {
        if (!ValidatePrefabs())
            return;


        // Every press of G replaces only the previously
        // auto-generated structure.
        //
        // Manually placed conveyors are not touched.
        ClearGeneratedStructure();


        CreateGeneratedRoot();


        int segmentCount =
            Random.Range(
                minimumSegments,
                maximumSegments + 1
            );


        Conveyor previousConveyor = null;

        int consecutiveInclines = 0;


        for (
            int i = 0;
            i < segmentCount;
            i++
        )
        {
            ConveyorType newType;


            // --------------------------------------------------
            // FIRST SEGMENT
            // --------------------------------------------------

            if (
                i == 0 &&
                firstSegmentMustBeFlat
            )
            {
                newType =
                    Random.value < 0.65f
                        ? ConveyorType.Generic
                        : ConveyorType.Short;
            }


            // --------------------------------------------------
            // REMAINING SEGMENTS
            // --------------------------------------------------

            else
            {
                newType =
                    ChooseRandomConveyorType(
                        consecutiveInclines
                    );
            }


            GameObject prefab =
                GetPrefabForType(
                    newType
                );


            if (prefab == null)
            {
                Debug.LogWarning(
                    $"Random generator could not find prefab for {newType}."
                );

                continue;
            }


            // --------------------------------------------------
            // CREATE CONVEYOR
            // --------------------------------------------------

            GameObject conveyorObject =
                Instantiate(
                    prefab,
                    generatedRoot
                );


            conveyorObject.name =
                $"Generated_{i + 1:00}_{newType}";


            Conveyor newConveyor =
                conveyorObject.GetComponent<Conveyor>();


            if (newConveyor == null)
            {
                Debug.LogError(
                    $"Generated prefab {prefab.name} does not contain Conveyor."
                );

                Destroy(conveyorObject);

                continue;
            }


            newConveyor.conveyorType =
                newType;


            // --------------------------------------------------
            // FIRST CONVEYOR
            // --------------------------------------------------

            if (previousConveyor == null)
            {
                PlaceFirstConveyor(
                    newConveyor,
                    newType
                );
            }


            // --------------------------------------------------
            // CONNECT TO PREVIOUS CONVEYOR
            // --------------------------------------------------

            else
            {
                ConnectConveyor(
                    previousConveyor,
                    newConveyor
                );
            }


            generatedConveyors.Add(
                conveyorObject
            );


            previousConveyor =
                newConveyor;


            // --------------------------------------------------
            // INCLINE LIMIT
            // --------------------------------------------------

            if (
                newType ==
                ConveyorType.Incline
            )
            {
                consecutiveInclines++;
            }
            else
            {
                consecutiveInclines = 0;
            }
        }


        Debug.Log(
            $"Generated random conveyor structure with " +
            $"{generatedConveyors.Count} segments."
        );
    }


    // ======================================================
    // FIRST CONVEYOR
    // ======================================================

    void PlaceFirstConveyor(
        Conveyor conveyor,
        ConveyorType type
    )
    {
        Transform origin =
            generationOrigin != null
                ? generationOrigin
                : transform;


        conveyor.transform.position =
            origin.position;


        conveyor.transform.rotation =
            origin.rotation;


        // Reset the first conveyor to a known baseline
        // before generation begins.
        conveyor.SetStartLocalZ(
            GetCanonicalStartZ(type)
        );
    }


    // ======================================================
    // CONNECT CONVEYOR
    // ======================================================

    void ConnectConveyor(
        Conveyor previous,
        Conveyor next
    )
    {
        if (
            previous.snapEnd == null ||
            next.snapStart == null
        )
        {
            Debug.LogError(
                "Generated conveyor is missing Snap_Start or Snap_End."
            );

            return;
        }


        // --------------------------------------------------
        // MATCH DIRECTION
        // --------------------------------------------------
        //
        // Generated structures remain linear.
        //
        // Every conveyor inherits the rotation of the
        // previous conveyor.

        next.transform.rotation =
            previous.transform.rotation;


        // --------------------------------------------------
        // APPLY CALIBRATED START Z
        // --------------------------------------------------

        float calibratedStartZ =
            GetIncomingStartZ(
                next.conveyorType,
                previous.conveyorType
            );


        next.SetStartLocalZ(
            calibratedStartZ
        );


        // --------------------------------------------------
        // ALIGN NEW START TO PREVIOUS END
        // --------------------------------------------------
        //
        // Full XYZ alignment means incline height is
        // automatically carried into subsequent conveyors.

        Vector3 offset =
            previous.snapEnd.position -
            next.snapStart.position;


        next.transform.position +=
            offset;
    }


    // ======================================================
    // RANDOM TYPE SELECTION
    // ======================================================

    ConveyorType ChooseRandomConveyorType(
        int consecutiveInclines
    )
    {
        float usableInclineWeight =
            inclineWeight;


        if (
            consecutiveInclines >=
            maximumConsecutiveInclines
        )
        {
            usableInclineWeight = 0f;
        }


        float totalWeight =
            genericWeight +
            shortWeight +
            usableInclineWeight;


        if (totalWeight <= 0f)
        {
            return ConveyorType.Generic;
        }


        float value =
            Random.Range(
                0f,
                totalWeight
            );


        if (value < genericWeight)
        {
            return ConveyorType.Generic;
        }


        value -=
            genericWeight;


        if (value < shortWeight)
        {
            return ConveyorType.Short;
        }


        return ConveyorType.Incline;
    }


    // ======================================================
    // PREFAB LOOKUP
    // ======================================================

    GameObject GetPrefabForType(
        ConveyorType type
    )
    {
        switch (type)
        {
            case ConveyorType.Generic:
                return genericConveyorPrefab;

            case ConveyorType.Short:
                return shortConveyorPrefab;

            case ConveyorType.Incline:
                return inclineConveyorPrefab;

            default:
                return null;
        }
    }


    // ======================================================
    // CALIBRATION MATRIX
    // ======================================================

    float GetIncomingStartZ(
        ConveyorType newType,
        ConveyorType existingType
    )
    {
        // --------------------------------------------------
        // NEW GENERIC
        // --------------------------------------------------

        if (
            newType ==
            ConveyorType.Generic
        )
        {
            switch (existingType)
            {
                case ConveyorType.Generic:
                    return genericAfterGenericStartZ;

                case ConveyorType.Short:
                    return genericAfterShortStartZ;

                case ConveyorType.Incline:
                    return genericAfterInclineStartZ;
            }
        }


        // --------------------------------------------------
        // NEW SHORT
        // --------------------------------------------------

        if (
            newType ==
            ConveyorType.Short
        )
        {
            switch (existingType)
            {
                case ConveyorType.Generic:
                    return shortAfterGenericStartZ;

                case ConveyorType.Short:
                    return shortAfterShortStartZ;

                case ConveyorType.Incline:
                    return shortAfterInclineStartZ;
            }
        }


        // --------------------------------------------------
        // NEW INCLINE
        // --------------------------------------------------

        if (
            newType ==
            ConveyorType.Incline
        )
        {
            switch (existingType)
            {
                case ConveyorType.Generic:
                    return inclineAfterGenericStartZ;

                case ConveyorType.Short:
                    return inclineAfterShortStartZ;

                case ConveyorType.Incline:
                    return inclineAfterInclineStartZ;
            }
        }


        return 0f;
    }


    // ======================================================
    // CANONICAL START VALUES
    // ======================================================

    float GetCanonicalStartZ(
        ConveyorType type
    )
    {
        switch (type)
        {
            case ConveyorType.Generic:
                return genericAfterGenericStartZ;

            case ConveyorType.Short:
                return shortAfterShortStartZ;

            case ConveyorType.Incline:
                return inclineAfterInclineStartZ;

            default:
                return 0f;
        }
    }


    // ======================================================
    // GENERATED ROOT
    // ======================================================

    void CreateGeneratedRoot()
    {
        GameObject rootObject =
            new GameObject(
                "Generated_Conveyor_Structure"
            );


        generatedRoot =
            rootObject.transform;
    }


    // ======================================================
    // CLEAR GENERATED STRUCTURE
    // ======================================================

    public void ClearGeneratedStructure()
    {
        for (
            int i = generatedConveyors.Count - 1;
            i >= 0;
            i--
        )
        {
            if (
                generatedConveyors[i] != null
            )
            {
                Destroy(
                    generatedConveyors[i]
                );
            }
        }


        generatedConveyors.Clear();


        if (generatedRoot != null)
        {
            Destroy(
                generatedRoot.gameObject
            );

            generatedRoot = null;
        }
    }


    // ======================================================
    // VALIDATION
    // ======================================================

    bool ValidatePrefabs()
    {
        if (
            genericConveyorPrefab == null ||
            shortConveyorPrefab == null ||
            inclineConveyorPrefab == null
        )
        {
            Debug.LogError(
                "RandomConveyorGenerator requires Generic, Short " +
                "and Incline conveyor prefabs."
            );

            return false;
        }


        return true;
    }
}