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

    // Distance used to determine whether two endpoints
    // are already connected.
    public float connectionTolerance = 0.05f;

    // If Path_Start and Path_End differ in height by more
    // than this amount, treat the conveyor as inclined.
    public float inclineHeightThreshold = 0.05f;

    private GameObject preview;
    private GameObject selectedConveyorPrefab;

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null)
            return;

        // --------------------------------------------------
        // Conveyor selection
        // --------------------------------------------------

        // 1 = Generic Conveyor
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            BeginPlacement(genericConveyorPrefab);
        }

        // 2 = Short Conveyor
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            BeginPlacement(shortConveyorPrefab);
        }

        // 3 = Incline Conveyor
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            BeginPlacement(inclineConveyorPrefab);
        }

        // Escape = cancel placement
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelPlacement();
        }

        if (preview == null)
            return;

        // --------------------------------------------------
        // Move preview using mouse raycast
        // --------------------------------------------------

        Ray ray = mainCamera.ScreenPointToRay(
            Mouse.current.position.ReadValue()
        );

        if (!groundCollider.Raycast(ray, out RaycastHit hit, 1000f))
            return;

        // Preview initially follows the Ground.
        preview.transform.position = hit.point;

        // Try to locate a valid endpoint.
        bool isSnapped = TrySnapPreview();

        bool hasExistingConveyor = HasExistingConveyor();

        // First conveyor can be placed freely.
        // Every conveyor after that must successfully snap.
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

        preview = Instantiate(selectedConveyorPrefab);
    }

    void PlaceConveyor()
    {
        if (preview == null || selectedConveyorPrefab == null)
            return;

        Instantiate(
            selectedConveyorPrefab,
            preview.transform.position,
            preview.transform.rotation
        );
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
            // Ignore placement preview.
            if (conveyor.gameObject == preview)
                continue;

            return true;
        }

        return false;
    }

    bool TrySnapPreview()
    {
        Conveyor previewConveyor =
            preview.GetComponent<Conveyor>();

        if (previewConveyor == null)
            return false;

        Conveyor[] conveyors =
            FindObjectsByType<Conveyor>();

        Conveyor closestConveyor = null;

        // true:
        // Existing Snap_End -> Preview Snap_Start
        //
        // false:
        // Preview Snap_End -> Existing Snap_Start
        bool snapStartToEnd = true;

        float closestDistance = snapDistance;

        foreach (Conveyor conveyor in conveyors)
        {
            // Ignore active preview.
            if (conveyor.gameObject == preview)
                continue;

            // ==================================================
            // CASE 1
            //
            // Add preview AFTER existing conveyor:
            //
            // [ Existing ][ Preview ]
            //
            // Existing Snap_End -> Preview Snap_Start
            // ==================================================

            if (!IsEndOccupied(conveyor, conveyors))
            {
                float startToEndDistance =
                    GetSnapDetectionDistance(
                        previewConveyor,
                        previewConveyor.snapStart.position,
                        conveyor,
                        conveyor.snapEnd.position
                    );

                if (startToEndDistance < closestDistance)
                {
                    closestDistance = startToEndDistance;
                    closestConveyor = conveyor;
                    snapStartToEnd = true;
                }
            }

            // ==================================================
            // CASE 2
            //
            // Add preview BEFORE existing conveyor:
            //
            // [ Preview ][ Existing ]
            //
            // Preview Snap_End -> Existing Snap_Start
            // ==================================================

            if (!IsStartOccupied(conveyor, conveyors))
            {
                float endToStartDistance =
                    GetSnapDetectionDistance(
                        previewConveyor,
                        previewConveyor.snapEnd.position,
                        conveyor,
                        conveyor.snapStart.position
                    );

                if (endToStartDistance < closestDistance)
                {
                    closestDistance = endToStartDistance;
                    closestConveyor = conveyor;
                    snapStartToEnd = false;
                }
            }
        }

        // No suitable free endpoint nearby.
        if (closestConveyor == null)
            return false;

        // Keep logical conveyor roots aligned.
        //
        // The actual incline is contained inside
        // the Conveyor_Incline prefab.
        preview.transform.rotation =
            closestConveyor.transform.rotation;

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
        // Actual snapping uses the complete XYZ offset.
        //
        // This means a Generic/Short conveyor can jump upward
        // and attach to the elevated end of an Incline.
        preview.transform.position += offset;

        return true;
    }

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

        // Flat-to-flat conveyors keep the original full
        // 3D snapping behaviour.
        if (!involvesIncline)
        {
            return Vector3.Distance(
                firstPoint,
                secondPoint
            );
        }

        // When an incline is involved, ignore Y only while
        // searching for a nearby endpoint.
        //
        // The actual snap still aligns full XYZ afterwards.
        return HorizontalDistance(
            firstPoint,
            secondPoint
        );
    }

    bool IsInclined(Conveyor conveyor)
    {
        if (
            conveyor == null ||
            conveyor.pathStart == null ||
            conveyor.pathEnd == null
        )
        {
            return false;
        }

        float heightDifference = Mathf.Abs(
            conveyor.pathEnd.position.y -
            conveyor.pathStart.position.y
        );

        return heightDifference > inclineHeightThreshold;
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

    bool IsEndOccupied(
        Conveyor conveyor,
        Conveyor[] conveyors
    )
    {
        foreach (Conveyor other in conveyors)
        {
            if (other == conveyor)
                continue;

            // Preview does not count as an existing connection.
            if (other.gameObject == preview)
                continue;

            // IMPORTANT:
            // Occupancy always uses full XYZ distance.
            float distance =
                Vector3.Distance(
                    conveyor.snapEnd.position,
                    other.snapStart.position
                );

            if (distance <= connectionTolerance)
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

            // Preview does not count as an existing connection.
            if (other.gameObject == preview)
                continue;

            // IMPORTANT:
            // Occupancy always uses full XYZ distance.
            float distance =
                Vector3.Distance(
                    conveyor.snapStart.position,
                    other.snapEnd.position
                );

            if (distance <= connectionTolerance)
            {
                return true;
            }
        }

        return false;
    }
}