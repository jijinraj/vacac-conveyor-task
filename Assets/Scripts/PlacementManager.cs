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

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            BeginPlacement();
        }

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
            preview.transform.position = hit.point;

            TrySnapPreview();

            if (Mouse.current.leftButton.wasPressedThisFrame)
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
            Destroy(preview);

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

    void TrySnapPreview()
    {
        Conveyor previewConveyor = preview.GetComponent<Conveyor>();

        if (previewConveyor == null)
            return;

        Conveyor[] conveyors = FindObjectsByType<Conveyor>();

        Conveyor closestConveyor = null;

        // true  = preview Snap_Start -> existing Snap_End
        // false = preview Snap_End   -> existing Snap_Start
        bool snapStartToEnd = true;

        float closestDistance = snapDistance;

        foreach (Conveyor conveyor in conveyors)
        {
            if (conveyor.gameObject == preview)
                continue;

            // Case 1:
            // Place preview AFTER existing conveyor.
            float startToEndDistance = Vector3.Distance(
                previewConveyor.snapStart.position,
                conveyor.snapEnd.position
            );

            if (startToEndDistance < closestDistance)
            {
                closestDistance = startToEndDistance;
                closestConveyor = conveyor;
                snapStartToEnd = true;
            }

            // Case 2:
            // Place preview BEFORE existing conveyor.
            float endToStartDistance = Vector3.Distance(
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

        if (closestConveyor == null)
            return;

        // Match the orientation of the connected conveyor.
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

        preview.transform.position += offset;
    }
}