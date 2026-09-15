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

        Conveyor[] conveyors =
            FindObjectsByType<Conveyor>(FindObjectsSortMode.None);

        Conveyor closestConveyor = null;
        float closestDistance = snapDistance;

        foreach (Conveyor conveyor in conveyors)
        {
            // Don't snap the preview to itself
            if (conveyor.gameObject == preview)
                continue;

            float distance = Vector3.Distance(
                previewConveyor.snapStart.position,
                conveyor.snapEnd.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestConveyor = conveyor;
            }
        }

        if (closestConveyor != null)
        {
            // Match the existing conveyor's direction
            preview.transform.rotation =
                closestConveyor.transform.rotation;

            // Recalculate after rotation
            Vector3 offset =
                closestConveyor.snapEnd.position -
                previewConveyor.snapStart.position;

            preview.transform.position += offset;
        }
    }
}