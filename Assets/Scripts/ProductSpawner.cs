using UnityEngine;
using UnityEngine.InputSystem;

public class ProductSpawner : MonoBehaviour
{
    public GameObject productPrefab;
    public float connectionTolerance = 0.05f;

    void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            SpawnProduct();
        }
    }

    void SpawnProduct()
    {
        Conveyor firstConveyor = FindFirstConveyor();

        if (firstConveyor == null)
        {
            Debug.LogWarning("Cannot spawn product: no conveyor line found.");
            return;
        }

        GameObject product = Instantiate(productPrefab);

        ProductMover mover = product.GetComponent<ProductMover>();

        if (mover == null)
        {
            Debug.LogError("Product prefab does not contain ProductMover.");
            Destroy(product);
            return;
        }

        mover.Initialize(firstConveyor);
    }

    Conveyor FindFirstConveyor()
    {
        Conveyor[] conveyors = FindObjectsByType<Conveyor>();

        foreach (Conveyor candidate in conveyors)
        {
            bool hasPreviousConveyor = false;

            foreach (Conveyor other in conveyors)
            {
                if (candidate == other)
                    continue;

                float distance = Vector3.Distance(
                    other.snapEnd.position,
                    candidate.snapStart.position
                );

                if (distance <= connectionTolerance)
                {
                    hasPreviousConveyor = true;
                    break;
                }
            }

            if (!hasPreviousConveyor)
                return candidate;
        }

        return null;
    }
}