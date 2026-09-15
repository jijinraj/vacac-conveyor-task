using UnityEngine;
using UnityEngine.InputSystem;

public class PlacementManager : MonoBehaviour
{
    public Camera mainCamera;
    public GameObject conveyorPrefab;
    public Collider groundCollider;

    public float snapDistance = 0.3f;

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
            // Move preview to mouse position first
            preview.transform.position = hit.point;

            // Try to connect the preview to an existing conveyor
            bool isSnapped = TrySnapPreview();

            // Check whether another conveyor already exists
            bool hasExistingConveyor = HasExistingConveyor();

            // First conveyor can be placed freely.
            // Every conveyor after that MUST be snapped.
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
            // Ignore the placement preview itself
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
            // Ignore preview itself
            if (conveyor.gameObject == preview)
                continue;

            // ----------------------------------------
            // CASE 1
            // Add preview AFTER existing conveyor
            //
            // [ Existing ][ Preview ]
            //
            // Existing Snap_End
            //        ->
            // Preview Snap_Start
            // ----------------------------------------

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

            // ----------------------------------------
            // CASE 2
            // Add preview BEFORE existing conveyor
            //
            // [ Preview ][ Existing ]
            //
            // Preview Snap_End
            //        ->
            // Existing Snap_Start
            // ----------------------------------------

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

        // No valid snap point nearby
        if (closestConveyor == null)
            return false;

        // Force the new conveyor to have exactly
        // the same orientation as the conveyor line.
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

        // Move the preview so the two snap points
        // perfectly overlap.
        preview.transform.position += offset;

        return true;
    }
}