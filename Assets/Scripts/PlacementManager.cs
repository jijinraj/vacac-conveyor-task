using UnityEngine;
using UnityEngine.InputSystem;

public class PlacementManager : MonoBehaviour
{
    public Camera mainCamera;
    public GameObject conveyorPrefab;
    public Collider groundCollider;

    public float snapDistance = 0.3f;

    // Distance used to determine whether two conveyor
    // endpoints are already connected.
    public float connectionTolerance = 0.05f;

    private GameObject preview;

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null)
            return;

        // Press 1 to begin placing a conveyor
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            BeginPlacement();
        }

        // Press Escape to cancel placement
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
            // Move preview to current mouse position.
            preview.transform.position = hit.point;

            // Attempt to snap to an AVAILABLE conveyor endpoint.
            bool isSnapped = TrySnapPreview();

            // Check whether a real conveyor already exists.
            bool hasExistingConveyor = HasExistingConveyor();

            // The first conveyor may be placed freely.
            // Every later conveyor must successfully snap.
            bool canPlace =
                !hasExistingConveyor ||
                isSnapped;

            if (
                Mouse.current.leftButton.wasPressedThisFrame &&
                canPlace
            )
            {
                Instantiate(
                    conveyorPrefab,
                    preview.transform.position,
                    preview.transform.rotation
                );
            }
        }
    }

    void BeginPlacement()
    {
        if (preview != null)
        {
            Destroy(preview);
        }

        preview = Instantiate(conveyorPrefab);
    }

    void CancelPlacement()
    {
        if (preview != null)
        {
            Destroy(preview);
            preview = null;
        }
    }

    bool HasExistingConveyor()
    {
        Conveyor[] conveyors = FindObjectsByType<Conveyor>();

        foreach (Conveyor conveyor in conveyors)
        {
            // Ignore the placement preview itself.
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
        // existing Snap_End -> preview Snap_Start
        //
        // false:
        // preview Snap_End -> existing Snap_Start
        bool snapStartToEnd = true;

        float closestDistance = snapDistance;

        foreach (Conveyor conveyor in conveyors)
        {
            // Ignore the preview itself.
            if (conveyor.gameObject == preview)
                continue;

            // ------------------------------------------------
            // CASE 1
            //
            // Add preview AFTER existing conveyor:
            //
            // [ Existing ][ Preview ]
            //
            // Existing Snap_End -> Preview Snap_Start
            //
            // Only valid if Existing Snap_End is NOT
            // already connected to another conveyor.
            // ------------------------------------------------

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

            // ------------------------------------------------
            // CASE 2
            //
            // Add preview BEFORE existing conveyor:
            //
            // [ Preview ][ Existing ]
            //
            // Preview Snap_End -> Existing Snap_Start
            //
            // Only valid if Existing Snap_Start is NOT
            // already connected to another conveyor.
            // ------------------------------------------------

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

        // No free/valid endpoint nearby.
        if (closestConveyor == null)
            return false;

        // Keep the conveyor line straight.
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

        // Make the relevant snap points overlap exactly.
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

            // Preview must not count as a real connection.
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

            // Preview must not count as a real connection.
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