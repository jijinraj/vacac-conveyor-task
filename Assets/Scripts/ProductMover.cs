using UnityEngine;

public class ProductMover : MonoBehaviour
{
    public Conveyor currentConveyor;

    public float speed = 0.3f;
    public float heightOffset = 0.02f;
    public float connectionTolerance = 0.05f;

    // Called by ProductSpawner when a new product is created
    public void Initialize(Conveyor conveyor)
    {
        currentConveyor = conveyor;

        if (currentConveyor != null)
        {
            MoveToStartOfCurrentConveyor();
        }
    }

    void Start()
    {
        // Keeps manual Inspector testing possible
        if (currentConveyor != null)
        {
            MoveToStartOfCurrentConveyor();
        }
    }

    void Update()
    {
        if (currentConveyor == null)
            return;

        Vector3 target =
            currentConveyor.pathEnd.position +
            Vector3.up * heightOffset;

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target) < 0.01f)
        {
            Conveyor nextConveyor = FindNextConveyor();

            if (nextConveyor != null)
            {
                currentConveyor = nextConveyor;
                MoveToStartOfCurrentConveyor();
            }
            else
            {
                // End of conveyor line
                currentConveyor = null;
            }
        }
    }

    void MoveToStartOfCurrentConveyor()
    {
        transform.position =
            currentConveyor.pathStart.position +
            Vector3.up * heightOffset;
    }

    Conveyor FindNextConveyor()
    {
        Conveyor[] conveyors = FindObjectsByType<Conveyor>();

        foreach (Conveyor conveyor in conveyors)
        {
            if (conveyor == currentConveyor)
                continue;

            float distance = Vector3.Distance(
                currentConveyor.snapEnd.position,
                conveyor.snapStart.position
            );

            if (distance <= connectionTolerance)
            {
                return conveyor;
            }
        }

        return null;
    }
}