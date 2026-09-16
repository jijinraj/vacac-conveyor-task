using UnityEngine;
using UnityEngine.InputSystem;

public class PlacementManager : MonoBehaviour
{
    [Header("Scene References")]
    public Camera mainCamera;
    public Collider groundCollider;

    [Header("Conveyor Prefabs")]
    public GameObject genericConveyorPrefab;
    public GameObject shortConveyorPrefab;
    public GameObject inclineConveyorPrefab;

    [Header("Snapping")]
    public float snapDistance = 0.3f;
    public float connectionTolerance = 0.05f;
    public float inclineHeightThreshold = 0.05f;
    public float elevatedHeightThreshold = 0.05f;

    // ======================================================
    // GENERIC INCOMING CALIBRATION
    // Existing conveyor -> New Generic
    // ======================================================

    [Header("Generic Incoming Connection Calibration")]

    [Tooltip("Generic -> Generic")]
    public float genericAfterGenericStartZ = -1.68f;

    [Tooltip("Short -> Generic")]
    public float genericAfterShortStartZ = -1.22f;

    [Tooltip("Incline -> Generic")]
    public float genericAfterInclineStartZ = -1.50f;


    // ======================================================
    // SHORT INCOMING CALIBRATION
    // Existing conveyor -> New Short
    // ======================================================

    [Header("Short Incoming Connection Calibration")]

    [Tooltip("Generic -> Short")]
    public float shortAfterGenericStartZ = -1.38f;

    [Tooltip("Short -> Short")]
    public float shortAfterShortStartZ = -0.91f;

    [Tooltip("Incline -> Short")]
    public float shortAfterInclineStartZ = -1.22f;


    // ======================================================
    // INCLINE INCOMING CALIBRATION
    // Existing conveyor -> New Incline
    // ======================================================

    [Header("Incline Incoming Connection Calibration")]

    [Tooltip("Generic -> Incline")]
    public float inclineAfterGenericStartZ = -0.05f;

    [Tooltip("Short -> Incline")]
    public float inclineAfterShortStartZ = 0.2955f;

    [Tooltip("Incline -> Incline")]
    public float inclineAfterInclineStartZ = -0.03f;


    private GameObject preview;
    private GameObject selectedConveyorPrefab;


    // ======================================================
    // UPDATE
    // ======================================================

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null)
            return;

        // --------------------------------------------------
        // Conveyor selection
        // --------------------------------------------------

        // 1 = Generic
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            BeginPlacement(genericConveyorPrefab);
        }

        // 2 = Short
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            BeginPlacement(shortConveyorPrefab);
        }

        // 3 = Incline
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            BeginPlacement(inclineConveyorPrefab);
        }

        // Escape = cancel
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelPlacement();
        }

        if (preview == null)
            return;

        // --------------------------------------------------
        // Ground-following preview
        // --------------------------------------------------

        Ray ray = mainCamera.ScreenPointToRay(
            Mouse.current.position.ReadValue()
        );

        if (!groundCollider.Raycast(
                ray,
                out RaycastHit hit,
                1000f
            ))
        {
            return;
        }

        // Preview initially follows ground.
        preview.transform.position = hit.point;

        bool isSnapped = TrySnapPreview();

        bool hasExistingConveyor =
            HasExistingConveyor();

        // First conveyor may be placed freely.
        // Every conveyor after the first must snap.
        bool canPlace =
            !hasExistingConveyor ||
            isSnapped;

        if (
            Mouse.current.leftButton.wasPressedThisFrame &&
            canPlace
        )
        {
            PlaceConveyor();
        }
    }


    // ======================================================
    // PLACEMENT
    // ======================================================

    void BeginPlacement(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning(
                "Cannot begin conveyor placement: prefab is not assigned."
            );

            return;
        }

        if (preview != null)
        {
            Destroy(preview);
        }

        selectedConveyorPrefab = prefab;

        preview =
            Instantiate(selectedConveyorPrefab);
    }


    void PlaceConveyor()
    {
        if (
            preview == null ||
            selectedConveyorPrefab == null
        )
        {
            return;
        }

        // IMPORTANT:
        //
        // Clone the preview rather than instantiate the
        // original prefab again.
        //
        // The preview may contain a dynamically calibrated
        // Snap_Start / Path_Start position.
        GameObject placedConveyor =
            Instantiate(
                preview,
                preview.transform.position,
                preview.transform.rotation
            );

        placedConveyor.name =
            selectedConveyorPrefab.name;
    }


    void CancelPlacement()
    {
        if (preview != null)
        {
            Destroy(preview);
            preview = null;
        }

        selectedConveyorPrefab = null;
    }


    bool HasExistingConveyor()
    {
        Conveyor[] conveyors =
            FindObjectsByType<Conveyor>();

        foreach (Conveyor conveyor in conveyors)
        {
            if (conveyor.gameObject == preview)
                continue;

            return true;
        }

        return false;
    }


    // ======================================================
    // SNAP SEARCH
    // ======================================================

    bool TrySnapPreview()
    {
        Conveyor previewConveyor =
            preview.GetComponent<Conveyor>();

        if (previewConveyor == null)
            return false;

        // Reset the preview to its canonical connection
        // calibration before searching all possible endpoints.
        ResetPreviewStartCalibration(
            previewConveyor
        );

        Conveyor[] conveyors =
            FindObjectsByType<Conveyor>();

        Conveyor closestConveyor = null;

        // true:
        //
        // [ Existing ][ Preview ]
        //
        // Existing Snap_End -> Preview Snap_Start
        //
        // false:
        //
        // [ Preview ][ Existing ]
        //
        // Preview Snap_End -> Existing Snap_Start
        bool snapStartToEnd = true;

        float closestDistance =
            snapDistance;


        foreach (Conveyor conveyor in conveyors)
        {
            if (conveyor.gameObject == preview)
                continue;


            // ==================================================
            // CASE 1
            //
            // EXISTING -> NEW
            //
            // [ Existing ][ Preview ]
            //
            // Existing Snap_End
            //        ->
            // Preview Snap_Start
            // ==================================================

            if (!IsEndOccupied(
                    conveyor,
                    conveyors
                ))
            {
                // Calculate where the preview's Snap_Start
                // WOULD be after applying the appropriate
                // connection calibration.
                //
                // This does not permanently modify it yet.
                Vector3 previewStartPoint =
                    GetCandidatePreviewStartPosition(
                        previewConveyor,
                        conveyor
                    );

                float startToEndDistance =
                    GetSnapDetectionDistance(
                        previewConveyor,
                        previewStartPoint,
                        conveyor,
                        conveyor.snapEnd.position
                    );

                if (
                    startToEndDistance <
                    closestDistance
                )
                {
                    closestDistance =
                        startToEndDistance;

                    closestConveyor =
                        conveyor;

                    snapStartToEnd =
                        true;
                }
            }


            // ==================================================
            // CASE 2
            //
            // NEW -> EXISTING
            //
            // [ Preview ][ Existing ]
            //
            // Preview Snap_End
            //        ->
            // Existing Snap_Start
            // ==================================================

            if (!IsStartOccupied(
                    conveyor,
                    conveyors
                ))
            {
                float endToStartDistance =
                    GetSnapDetectionDistance(
                        previewConveyor,
                        previewConveyor.snapEnd.position,
                        conveyor,
                        conveyor.snapStart.position
                    );

                if (
                    endToStartDistance <
                    closestDistance
                )
                {
                    closestDistance =
                        endToStartDistance;

                    closestConveyor =
                        conveyor;

                    snapStartToEnd =
                        false;
                }
            }
        }


        // No free endpoint near the mouse.
        if (closestConveyor == null)
            return false;


        // Keep conveyor roots aligned.
        preview.transform.rotation =
            closestConveyor.transform.rotation;


        // ======================================================
        // Apply the connection-specific calibration
        // ======================================================

        if (snapStartToEnd)
        {
            // Existing -> New
            //
            // The NEW conveyor's start profile depends
            // on the existing conveyor type.
            ApplyIncomingStartCalibration(
                previewConveyor,
                closestConveyor
            );
        }
        else
        {
            // New -> Existing
            //
            // This connection is using Preview Snap_End,
            // therefore Preview Snap_Start is irrelevant.
            ResetPreviewStartCalibration(
                previewConveyor
            );
        }


        Vector3 offset;


        if (snapStartToEnd)
        {
            // Existing -> New
            offset =
                closestConveyor.snapEnd.position -
                previewConveyor.snapStart.position;
        }
        else
        {
            // New -> Existing
            offset =
                closestConveyor.snapStart.position -
                previewConveyor.snapEnd.position;
        }


        // IMPORTANT:
        //
        // Candidate detection may ignore Y for elevated or
        // inclined sections.
        //
        // Final snapping ALWAYS uses full XYZ.
        preview.transform.position += offset;

        return true;
    }


    // ======================================================
    // ADAPTIVE START CALIBRATION
    // ======================================================

    Vector3 GetCandidatePreviewStartPosition(
        Conveyor previewConveyor,
        Conveyor existingConveyor
    )
    {
        if (
            previewConveyor == null ||
            previewConveyor.snapStart == null
        )
        {
            return preview.transform.position;
        }

        float targetStartZ =
            GetIncomingStartZ(
                previewConveyor.conveyorType,
                existingConveyor.conveyorType
            );

        return previewConveyor
            .GetSnapStartWorldPositionWithLocalZ(
                targetStartZ
            );
    }


    void ApplyIncomingStartCalibration(
        Conveyor previewConveyor,
        Conveyor existingConveyor
    )
    {
        if (
            previewConveyor == null ||
            existingConveyor == null
        )
        {
            return;
        }

        float targetStartZ =
            GetIncomingStartZ(
                previewConveyor.conveyorType,
                existingConveyor.conveyorType
            );


        // Snap_Start and Path_Start ALWAYS move together.
        previewConveyor.SetStartLocalZ(
            targetStartZ
        );
    }


    void ResetPreviewStartCalibration(
        Conveyor previewConveyor
    )
    {
        if (previewConveyor == null)
            return;


        // Use each conveyor's same-type connection as its
        // canonical/default runtime profile.

        switch (previewConveyor.conveyorType)
        {
            case ConveyorType.Generic:

                previewConveyor.SetStartLocalZ(
                    genericAfterGenericStartZ
                );

                break;


            case ConveyorType.Short:

                previewConveyor.SetStartLocalZ(
                    shortAfterShortStartZ
                );

                break;


            case ConveyorType.Incline:

                previewConveyor.SetStartLocalZ(
                    inclineAfterInclineStartZ
                );

                break;
        }
    }


    // ======================================================
    // 3 x 3 CONNECTION MATRIX
    // ======================================================

    float GetIncomingStartZ(
        ConveyorType newConveyorType,
        ConveyorType existingConveyorType
    )
    {
        // --------------------------------------------------
        // NEW GENERIC
        // --------------------------------------------------

        if (newConveyorType == ConveyorType.Generic)
        {
            switch (existingConveyorType)
            {
                case ConveyorType.Generic:

                    // Generic -> Generic
                    return genericAfterGenericStartZ;


                case ConveyorType.Short:

                    // Short -> Generic
                    return genericAfterShortStartZ;


                case ConveyorType.Incline:

                    // Incline -> Generic
                    return genericAfterInclineStartZ;
            }
        }


        // --------------------------------------------------
        // NEW SHORT
        // --------------------------------------------------

        if (newConveyorType == ConveyorType.Short)
        {
            switch (existingConveyorType)
            {
                case ConveyorType.Generic:

                    // Generic -> Short
                    return shortAfterGenericStartZ;


                case ConveyorType.Short:

                    // Short -> Short
                    return shortAfterShortStartZ;


                case ConveyorType.Incline:

                    // Incline -> Short
                    return shortAfterInclineStartZ;
            }
        }


        // --------------------------------------------------
        // NEW INCLINE
        // --------------------------------------------------

        if (newConveyorType == ConveyorType.Incline)
        {
            switch (existingConveyorType)
            {
                case ConveyorType.Generic:

                    // Generic -> Incline
                    return inclineAfterGenericStartZ;


                case ConveyorType.Short:

                    // Short -> Incline
                    return inclineAfterShortStartZ;


                case ConveyorType.Incline:

                    // Incline -> Incline
                    return inclineAfterInclineStartZ;
            }
        }


        // Safe fallback.
        return 0f;
    }


    // ======================================================
    // SNAP DISTANCE
    // ======================================================

    float GetSnapDetectionDistance(
        Conveyor firstConveyor,
        Vector3 firstPoint,
        Conveyor secondConveyor,
        Vector3 secondPoint
    )
    {
        bool involvesIncline =
            IsInclined(firstConveyor) ||
            IsInclined(secondConveyor);

        bool involvesElevatedConveyor =
            IsElevated(firstConveyor) ||
            IsElevated(secondConveyor);


        // Normal ground-level flat-to-flat snapping
        // uses full 3D distance.
        if (
            !involvesIncline &&
            !involvesElevatedConveyor
        )
        {
            return Vector3.Distance(
                firstPoint,
                secondPoint
            );
        }


        // Inclined/elevated endpoint discovery ignores Y.
        //
        // Actual snapping afterwards still uses full XYZ.
        return HorizontalDistance(
            firstPoint,
            secondPoint
        );
    }


    bool IsInclined(
        Conveyor conveyor
    )
    {
        if (
            conveyor == null ||
            conveyor.pathStart == null ||
            conveyor.pathEnd == null
        )
        {
            return false;
        }


        float heightDifference =
            Mathf.Abs(
                conveyor.pathEnd.position.y -
                conveyor.pathStart.position.y
            );


        return heightDifference >
               inclineHeightThreshold;
    }


    bool IsElevated(
        Conveyor conveyor
    )
    {
        if (conveyor == null)
            return false;


        // Flat conveyors placed after an incline have a
        // non-zero root Y.
        //
        // XZ-only candidate detection lets a ground-following
        // preview find these elevated endpoints.
        return Mathf.Abs(
                   conveyor.transform.position.y
               ) >
               elevatedHeightThreshold;
    }


    float HorizontalDistance(
        Vector3 firstPoint,
        Vector3 secondPoint
    )
    {
        Vector2 firstHorizontal =
            new Vector2(
                firstPoint.x,
                firstPoint.z
            );

        Vector2 secondHorizontal =
            new Vector2(
                secondPoint.x,
                secondPoint.z
            );


        return Vector2.Distance(
            firstHorizontal,
            secondHorizontal
        );
    }


    // ======================================================
    // ENDPOINT OCCUPANCY
    // ======================================================

    bool IsEndOccupied(
        Conveyor conveyor,
        Conveyor[] conveyors
    )
    {
        foreach (Conveyor other in conveyors)
        {
            if (other == conveyor)
                continue;


            if (other.gameObject == preview)
                continue;


            // Occupancy always uses full XYZ.
            float distance =
                Vector3.Distance(
                    conveyor.snapEnd.position,
                    other.snapStart.position
                );


            if (
                distance <=
                connectionTolerance
            )
            {
                return true;
            }
        }


        return false;
    }


    bool IsStartOccupied(
        Conveyor conveyor,
        Conveyor[] conveyors
    )
    {
        foreach (Conveyor other in conveyors)
        {
            if (other == conveyor)
                continue;


            if (other.gameObject == preview)
                continue;


            // Occupancy always uses full XYZ.
            float distance =
                Vector3.Distance(
                    conveyor.snapStart.position,
                    other.snapEnd.position
                );


            if (
                distance <=
                connectionTolerance
            )
            {
                return true;
            }
        }


        return false;
    }
}