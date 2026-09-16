using UnityEngine;
using UnityEngine.InputSystem;

public class PlacementManager : MonoBehaviour
{
    public Camera mainCamera;

    public GameObject genericConveyorPrefab;
    public GameObject shortConveyorPrefab;

    public Collider groundCollider;

    public float snapDistance = 0.3f;

    // Distance used to determine whether two conveyor
    // endpoints are already connected.
    public float connectionTolerance = 0.05f;

    private GameObject preview;
    private GameObject selectedConveyorPrefab;

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null)
            return;

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

        // Escape = cancel current placement
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelPlacement();
        }

        if (preview == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(
            Mouse.current.position.ReadValue()
        );

        if (groundCollider.Raycast(ray, out RaycastHit hit, 1000f))
        {
            // Move preview to the mouse position.
            preview.transform.position = hit.point;

            // Try to snap to an available endpoint.
            bool isSnapped = TrySnapPreview();

            // Determine whether a real conveyor already exists.
            bool hasExistingConveyor = HasExistingConveyor();

            // First conveyor may be placed freely.
            // All later conveyors must be snapped.
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

        // Remove previous preview if one already exists.
        if (preview != null)
        {
            Destroy(preview);
        }

        selectedConveyorPrefab = prefab;

        preview = Instantiate(selectedConveyorPrefab);
    }

    void PlaceConveyor()
    {
        if (selectedConveyorPrefab == null || preview == null)
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
            // Ignore the active preview.
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
            // Ignore the active preview.
            if (conveyor.gameObject == preview)
                continue;

            // ==================================================
            // CASE 1
            //
            // Add preview AFTER existing conveyor:
            //
            // [ Existing ][ Preview ]
            //
            // Existing Snap_End
            //        ->
            // Preview Snap_Start
            //
            // Existing Snap_End must be free.
            // ==================================================

            if (!IsEndOccupied(conveyor, conveyors))
            {
                float startToEndDistance =
                    Vector3.Distance(
                        previewConveyor.snapStart.position,
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
            // Preview Snap_End
            //        ->
            // Existing Snap_Start
            //
            // Existing Snap_Start must be free.
            // ==================================================

            if (!IsStartOccupied(conveyor, conveyors))
            {
                float endToStartDistance =
                    Vector3.Distance(
                        previewConveyor.snapEnd.position,
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

        // No valid free endpoint is nearby.
        if (closestConveyor == null)
            return false;

        // Keep the current conveyor line straight.
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

        // Make the selected connection points overlap exactly.
        preview.transform.position += offset;

        return true;
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

            // Preview must not count as a real conveyor connection.
            if (other.gameObject == preview)
                continue;

            float distance = Vector3.Distance(
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

            // Preview must not count as a real conveyor connection.
            if (other.gameObject == preview)
                continue;

            float distance = Vector3.Distance(
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