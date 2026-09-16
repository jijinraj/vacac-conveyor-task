using UnityEngine;
using UnityEngine.InputSystem;

public class ProductSpawner : MonoBehaviour
{
    // ======================================================
    // PRODUCT PREFABS
    // ======================================================

    [Header("Product Prefabs")]

    [Tooltip("Cardboard Box prefab. Spawn with key 4.")]
    public GameObject productPrefab;

    [Tooltip("Slim Can prefab. Spawn with key 5.")]
    public GameObject canPrefab;


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

    // Most recently successfully spawned product.
    //
    // R or the Rotate UI button rotates only this product.
    private ProductMover latestSpawnedProduct;


    // ======================================================
    // UNITY LIFECYCLE
    // ======================================================

    void Update()
    {
        if (Keyboard.current == null)
            return;


        // --------------------------------------------------
        // 4 = SPAWN CARDBOARD BOX
        // --------------------------------------------------

        if (
            Keyboard.current.digit4Key.wasPressedThisFrame
        )
        {
            SpawnBox();
        }


        // --------------------------------------------------
        // 5 = SPAWN SLIM CAN
        // --------------------------------------------------

        if (
            Keyboard.current.digit5Key.wasPressedThisFrame
        )
        {
            SpawnCan();
        }


        // --------------------------------------------------
        // R = ROTATE LATEST PRODUCT 90 DEGREES
        // --------------------------------------------------

        if (
            Keyboard.current.rKey.wasPressedThisFrame
        )
        {
            RotateLatestProduct();
        }
    }


    // ======================================================
    // PUBLIC UI ACTIONS
    // ======================================================

    public void SpawnBox()
    {
        SpawnProduct(productPrefab);
    }


    public void SpawnCan()
    {
        SpawnProduct(canPrefab);
    }


    public void RotateLatestProduct()
    {
        if (latestSpawnedProduct == null)
        {
            Debug.LogWarning(
                "Cannot rotate product: no successfully spawned product is available."
            );

            return;
        }


        latestSpawnedProduct.RotateProduct90();
    }


    // ======================================================
    // PRODUCT SPAWNING
    // ======================================================

    void SpawnProduct(
        GameObject prefab
    )
    {
        // --------------------------------------------------
        // VALIDATE PREFAB
        // --------------------------------------------------

        if (prefab == null)
        {
            Debug.LogWarning(
                "Cannot spawn product: product prefab is not assigned."
            );

            return;
        }


        // --------------------------------------------------
        // FIND START OF CONVEYOR LINE
        // --------------------------------------------------

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
            Instantiate(prefab);


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

        mover.Initialize(
            firstConveyor
        );


        // --------------------------------------------------
        // OVERLAP CHECK
        // --------------------------------------------------

        if (
            OverlapsExistingProduct(
                product
            )
        )
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

        // Only remember the product after the overlap
        // check succeeds.
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


        // Add a small safety gap around the candidate.
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
            // Ignore the candidate itself.
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