using UnityEngine;
using UnityEngine.InputSystem;

public class ProductSpawner : MonoBehaviour
{
    public GameObject productPrefab;

    public float connectionTolerance = 0.05f;

    // Extra visible space required between products.
    public float productGap = 0.05f;

    void Update()
    {
        if (Keyboard.current == null)
            return;

        // Press 2 to spawn a cardboard box.
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            SpawnProduct();
        }
    }

    void SpawnProduct()
    {
        Conveyor firstConveyor = FindFirstConveyor();

        if (firstConveyor == null)
        {
            Debug.LogWarning(
                "Cannot spawn product: no conveyor line found."
            );

            return;
        }

        // Create candidate product.
        GameObject product = Instantiate(productPrefab);

        ProductMover mover =
            product.GetComponent<ProductMover>();

        if (mover == null)
        {
            Debug.LogError(
                "Product prefab does not contain ProductMover."
            );

            Destroy(product);
            return;
        }

        // Position candidate at the real spawn point.
        mover.Initialize(firstConveyor);

        // Check its actual visual bounds against existing products.
        if (OverlapsExistingProduct(product))
        {
            Debug.LogWarning(
                "Cannot spawn product: conveyor start is occupied."
            );

            Destroy(product);
            return;
        }
    }

    bool OverlapsExistingProduct(GameObject newProduct)
    {
        if (!TryGetProductBounds(newProduct, out Bounds newBounds))
        {
            return false;
        }

        // Add a small safety gap around the product.
        newBounds.Expand(productGap * 2f);

        ProductMover[] products =
            FindObjectsByType<ProductMover>();

        foreach (ProductMover product in products)
        {
            // Ignore the candidate product itself.
            if (product.gameObject == newProduct)
                continue;

            if (
                TryGetProductBounds(
                    product.gameObject,
                    out Bounds existingBounds
                )
            )
            {
                if (newBounds.Intersects(existingBounds))
                {
                    return true;
                }
            }
        }

        return false;
    }

    bool TryGetProductBounds(
        GameObject productObject,
        out Bounds bounds
    )
    {
        Renderer[] renderers =
            productObject.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            bounds = new Bounds();
            return false;
        }

        bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }

    Conveyor FindFirstConveyor()
    {
        Conveyor[] conveyors =
            FindObjectsByType<Conveyor>();

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
            {
                return candidate;
            }
        }

        return null;
    }
}