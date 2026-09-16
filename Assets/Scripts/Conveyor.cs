using UnityEngine;

public enum ConveyorType
{
    Generic,
    Short,
    Incline
}

public class Conveyor : MonoBehaviour
{
    [Header("Conveyor Type")]
    public ConveyorType conveyorType = ConveyorType.Generic;

    [Header("Snap Points")]
    public Transform snapStart;
    public Transform snapEnd;

    [Header("Product Path")]
    public Transform pathStart;
    public Transform pathEnd;

    /// <summary>
    /// Updates both Snap_Start and Path_Start Z together.
    ///
    /// This is used because different supplied conveyor meshes
    /// require slightly different connection offsets.
    /// </summary>
    public void SetStartLocalZ(float z)
    {
        if (snapStart != null)
        {
            Vector3 position = snapStart.localPosition;
            position.z = z;
            snapStart.localPosition = position;
        }

        if (pathStart != null)
        {
            Vector3 position = pathStart.localPosition;
            position.z = z;
            pathStart.localPosition = position;
        }
    }

    /// <summary>
    /// Calculates where Snap_Start WOULD be in world space
    /// if it used the supplied local Z value.
    ///
    /// This lets PlacementManager test different connector
    /// profiles before actually changing the preview.
    /// </summary>
    public Vector3 GetSnapStartWorldPositionWithLocalZ(float z)
    {
        if (snapStart == null)
            return transform.position;

        Vector3 localPosition = snapStart.localPosition;
        localPosition.z = z;

        if (snapStart.parent != null)
        {
            return snapStart.parent.TransformPoint(localPosition);
        }

        return transform.TransformPoint(localPosition);
    }

    private void OnDrawGizmos()
    {
        if (snapStart != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(
                snapStart.position,
                0.03f
            );
        }

        if (snapEnd != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(
                snapEnd.position,
                0.03f
            );
        }

        if (pathStart != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(
                pathStart.position,
                0.025f
            );
        }

        if (pathEnd != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(
                pathEnd.position,
                0.025f
            );
        }
    }
}