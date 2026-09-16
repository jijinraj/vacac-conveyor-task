using UnityEngine;
using UnityEngine.InputSystem;

public class ProductSpawner : MonoBehaviour
{
    // ======================================================
    // PRODUCT PREFAB
    // ======================================================

    [Header("Product")]

    public GameObject productPrefab;


    // ======================================================
    // SPAWNING
    // ======================================================

    [Header("Spawning")]

    public float connectionTolerance = 0.05f;

    [Tooltip("Extra visible space required between products.")]
    public float productGap = 0.05f;


    // ======================================================
    // INTERNAL STATE
    // ======================================================

    // The most recently SUCCESSFULLY spawned product.
    //
    // Pressing R rotates only this product.
    private ProductMover latestSpawnedProduct;


    // ======================================================
    // UNITY LIFECYCLE
    // ======================================================

    void Update()
    {
        if (Keyboard.current == null)
            return;


        // --------------------------------------------------
        // SPAWN PRODUCT
        // --------------------------------------------------
        //
        // 4 = Spawn cardboard box

        if (
            Keyboard.current.digit4Key.wasPressedThisFrame
        )
        {
            SpawnProduct();
        }


        // --------------------------------------------------
        // ROTATE LATEST PRODUCT
        // --------------------------------------------------
        //
        // R = Rotate most recently spawned product by 90°
        //
        // ProductMover stores the yaw offset, so the product
        // keeps this orientation while travelling across
        // Generic, Short and Incline conveyors.

        if (
            Keyboard.current.rKey.wasPressedThisFrame &&
            latestSpawnedProduct != null
        )
        {
            latestSpawnedProduct.RotateProduct90();
        }
    }


    // ======================================================
    // PRODUCT SPAWNING
    // ======================================================

    void SpawnProduct()
    {
        Conveyor firstConveyor =
            FindFirstConveyor();


        if (firstConveyor == null)
        {
            Debug.LogWarning(
                "Cannot spawn product: no conveyor line found."
            );

            return;
        }


        // --------------------------------------------------
        // CREATE CANDIDATE PRODUCT
        // --------------------------------------------------

        GameObject product =
            Instantiate(productPrefab);


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


        // --------------------------------------------------
        // INITIALIZE PRODUCT
        // --------------------------------------------------

        // Position the candidate at the actual start
        // of the conveyor line.
        mover.Initialize(firstConveyor);


        // --------------------------------------------------
        // OVERLAP CHECK
        // --------------------------------------------------

        if (OverlapsExistingProduct(product))
        {
            Debug.LogWarning(
                "Cannot spawn product: conveyor start is occupied."
            );

            Destroy(product);

            return;
        }


        // --------------------------------------------------
        // SUCCESSFUL SPAWN
        // --------------------------------------------------
        //
        // Only remember the product AFTER the overlap check.
        //
        // This prevents R from trying to rotate a candidate
        // that was rejected and destroyed.

        latestSpawnedProduct =
            mover;
    }


    // ======================================================
    // PRODUCT OVERLAP
    // ======================================================

    bool OverlapsExistingProduct(
        GameObject newProduct
    )
    {
        if (
            !TryGetProductBounds(
                newProduct,
                out Bounds newBounds
            )
        )
        {
            return false;
        }


        // Add a small safety gap around the product.
        newBounds.Expand(
            productGap * 2f
        );


        ProductMover[] products =
            FindObjectsByType<ProductMover>();


        foreach (
            ProductMover product
            in products
        )
        {
            // Ignore the candidate product itself.
            if (
                product.gameObject ==
                newProduct
            )
            {
                continue;
            }


            if (
                TryGetProductBounds(
                    product.gameObject,
                    out Bounds existingBounds
                )
            )
            {
                if (
                    newBounds.Intersects(
                        existingBounds
                    )
                )
                {
                    return true;
                }
            }
        }


        return false;
    }


    // ======================================================
    // PRODUCT VISUAL BOUNDS
    // ======================================================

    bool TryGetProductBounds(
        GameObject productObject,
        out Bounds bounds
    )
    {
        Renderer[] renderers =
            productObject
                .GetComponentsInChildren<Renderer>();


        if (renderers.Length == 0)
        {
            bounds =
                new Bounds();

            return false;
        }


        bounds =
            renderers[0].bounds;


        for (
            int i = 1;
            i < renderers.Length;
            i++
        )
        {
            bounds.Encapsulate(
                renderers[i].bounds
            );
        }


        return true;
    }


    // ======================================================
    // FIND START OF CONVEYOR LINE
    // ======================================================

    Conveyor FindFirstConveyor()
    {
        Conveyor[] conveyors =
            FindObjectsByType<Conveyor>();


        foreach (
            Conveyor candidate
            in conveyors
        )
        {
            bool hasPreviousConveyor =
                false;


            foreach (
                Conveyor other
                in conveyors
            )
            {
                if (
                    candidate ==
                    other
                )
                {
                    continue;
                }


                float distance =
                    Vector3.Distance(
                        other.snapEnd.position,
                        candidate.snapStart.position
                    );


                if (
                    distance <=
                    connectionTolerance
                )
                {
                    hasPreviousConveyor =
                        true;

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