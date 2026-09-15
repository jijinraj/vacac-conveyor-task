using UnityEngine;

public class Conveyor : MonoBehaviour
{
    public Transform snapStart;
    public Transform snapEnd;

    public Transform pathStart;
    public Transform pathEnd;

    private void OnDrawGizmos()
    {
        if (snapStart != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(snapStart.position, 0.03f);
        }

        if (snapEnd != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(snapEnd.position, 0.03f);
        }

        if (pathStart != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(pathStart.position, 0.025f);
        }

        if (pathEnd != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(pathEnd.position, 0.025f);
        }
    }
}