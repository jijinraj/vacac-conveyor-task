using UnityEngine;

public class ProductMover : MonoBehaviour
{
    public Conveyor currentConveyor;


    // ======================================================
    // MOVEMENT
    // ======================================================

    [Header("Movement")]

    public float speed = 0.3f;

    [Tooltip("Surface clearance used on Generic and Short conveyors.")]
    public float flatSurfaceClearance = 0.08f;

    [Tooltip("Surface clearance used while travelling on Incline conveyors.")]
    public float inclineSurfaceClearance = -0.12f;


    // ======================================================
    // TRANSITIONS
    // ======================================================

    [Header("Transitions")]

    [Tooltip(
        "Time used to smoothly move and rotate the product " +
        "between connected conveyors."
    )]
    public float transitionDuration = 0.20f;


    // ======================================================
    // ROTATION
    // ======================================================

    [Header("Rotation")]

    [Tooltip("How quickly the product rotates while travelling.")]
    public float rotationSpeed = 6f;

    [Tooltip(
        "Visual product tilt while travelling on an Incline conveyor."
    )]
    public float inclineTiltDegrees = 23f;

    [Tooltip(
        "How many degrees the product rotates each time RotateProduct() is called."
    )]
    public float productRotationStep = 90f;


    // ======================================================
    // CONNECTIONS
    // ======================================================

    [Header("Connections")]

    public float connectionTolerance = 0.05f;


    // ======================================================
    // INTERNAL STATE
    // ======================================================

    private float pivotToBottomOffset = 0f;

    private bool visualOffsetCalculated = false;

    private bool isTransitioning = false;

    private Conveyor transitionTargetConveyor;

    private float transitionElapsed = 0f;

    private Vector3 transitionStartPosition;
    private Vector3 transitionEndPosition;

    private Quaternion transitionStartRotation;
    private Quaternion transitionEndRotation;


    // Additional rotation selected by the player.
    //
    // 0   = normal
    // 90  = sideways
    // 180 = backwards
    // 270 = opposite sideways
    private float productYawOffset = 0f;


    // ======================================================
    // UNITY LIFECYCLE
    // ======================================================

    void Awake()
    {
        CalculateVisualBottomOffset();
    }


    public void Initialize(Conveyor conveyor)
    {
        currentConveyor = conveyor;

        EnsureVisualOffsetCalculated();

        if (currentConveyor != null)
        {
            MoveToStartOfCurrentConveyor();

            SetRotationToCurrentConveyorImmediate();
        }
    }


    void Start()
    {
        if (currentConveyor != null)
        {
            EnsureVisualOffsetCalculated();

            MoveToStartOfCurrentConveyor();

            SetRotationToCurrentConveyorImmediate();
        }
    }


    void Update()
    {
        if (currentConveyor == null)
            return;


        // --------------------------------------------------
        // TRANSITION BETWEEN CONVEYORS
        // --------------------------------------------------

        if (isTransitioning)
        {
            UpdateTransition();
            return;
        }


        // --------------------------------------------------
        // NORMAL MOVEMENT
        // --------------------------------------------------

        Vector3 target =
            GetProductPosition(
                currentConveyor.pathEnd.position,
                currentConveyor
            );


        transform.position =
            Vector3.MoveTowards(
                transform.position,
                target,
                speed * Time.deltaTime
            );


        RotateTowardsCurrentConveyor();


        // --------------------------------------------------
        // END OF CURRENT CONVEYOR
        // --------------------------------------------------

        if (
            Vector3.Distance(
                transform.position,
                target
            ) < 0.01f
        )
        {
            Conveyor nextConveyor =
                FindNextConveyor();


            if (nextConveyor != null)
            {
                BeginTransition(nextConveyor);
            }
            else
            {
                CargoExitHandler exitHandler =
    GetComponent<CargoExitHandler>();

if (exitHandler != null)
{
    exitHandler.ReportReachedConveyorEnd();
}

Destroy(gameObject);
            }
        }
    }


    // ======================================================
    // PLAYER PRODUCT ROTATION
    // ======================================================

    public void RotateProduct90()
    {
        RotateProduct(productRotationStep);
    }


    public void RotateProduct(float degrees)
    {
        productYawOffset += degrees;


        productYawOffset =
            Mathf.Repeat(
                productYawOffset,
                360f
            );


        // If R is pressed during a conveyor transition,
        // update the target orientation immediately.
        if (
            isTransitioning &&
            transitionTargetConveyor != null
        )
        {
            transitionEndRotation =
                GetTargetConveyorRotation(
                    transitionTargetConveyor
                );
        }
    }


    // ======================================================
    // TRANSITIONS
    // ======================================================

    void BeginTransition(
        Conveyor nextConveyor
    )
    {
        if (
            nextConveyor == null ||
            nextConveyor.pathStart == null
        )
        {
            return;
        }


        isTransitioning = true;

        transitionTargetConveyor =
            nextConveyor;

        transitionElapsed = 0f;


        transitionStartPosition =
            transform.position;


        // --------------------------------------------------
        // CONNECTION-SEAM CLEARANCE
        // --------------------------------------------------

        float seamClearance =
            GetTransitionSeamClearance(
                currentConveyor,
                nextConveyor
            );


        transitionEndPosition =
            nextConveyor.pathStart.position +
            Vector3.up *
            (
                pivotToBottomOffset +
                seamClearance
            );


        transitionStartRotation =
            transform.rotation;


        transitionEndRotation =
            GetTargetConveyorRotation(
                nextConveyor
            );
    }


    float GetTransitionSeamClearance(
        Conveyor fromConveyor,
        Conveyor toConveyor
    )
    {
        bool fromIncline =
            fromConveyor != null &&
            fromConveyor.conveyorType ==
            ConveyorType.Incline;


        bool toIncline =
            toConveyor != null &&
            toConveyor.conveyorType ==
            ConveyorType.Incline;


        // Only Incline -> Incline uses
        // the incline-specific clearance.
        if (fromIncline && toIncline)
        {
            return inclineSurfaceClearance;
        }


        // Any seam involving Generic or Short
        // uses flat clearance.
        return flatSurfaceClearance;
    }


    void UpdateTransition()
    {
        if (transitionTargetConveyor == null)
        {
            isTransitioning = false;
            return;
        }


        transitionElapsed +=
            Time.deltaTime;


        float duration =
            Mathf.Max(
                transitionDuration,
                0.01f
            );


        float t =
            Mathf.Clamp01(
                transitionElapsed /
                duration
            );


        // SmoothStep.
        float smoothT =
            t * t *
            (
                3f -
                2f * t
            );


        transform.position =
            Vector3.Lerp(
                transitionStartPosition,
                transitionEndPosition,
                smoothT
            );


        // Recalculate because the product may be rotated
        // while travelling between conveyors.
        transitionEndRotation =
            GetTargetConveyorRotation(
                transitionTargetConveyor
            );


        transform.rotation =
            Quaternion.Slerp(
                transitionStartRotation,
                transitionEndRotation,
                smoothT
            );


        if (t >= 1f)
        {
            currentConveyor =
                transitionTargetConveyor;


            transform.position =
                transitionEndPosition;

            transform.rotation =
                transitionEndRotation;


            transitionTargetConveyor =
                null;

            isTransitioning =
                false;
        }
    }


    // ======================================================
    // POSITIONING
    // ======================================================

    void MoveToStartOfCurrentConveyor()
    {
        if (
            currentConveyor == null ||
            currentConveyor.pathStart == null
        )
        {
            return;
        }


        transform.position =
            GetProductPosition(
                currentConveyor.pathStart.position,
                currentConveyor
            );
    }


    Vector3 GetProductPosition(
        Vector3 pathPosition,
        Conveyor conveyor
    )
    {
        float clearance =
            flatSurfaceClearance;


        if (
            conveyor != null &&
            conveyor.conveyorType ==
            ConveyorType.Incline
        )
        {
            clearance =
                inclineSurfaceClearance;
        }


        return pathPosition +
               Vector3.up *
               (
                   pivotToBottomOffset +
                   clearance
               );
    }


    // ======================================================
    // PRODUCT ROTATION
    // ======================================================

    Quaternion GetTargetConveyorRotation(
        Conveyor conveyor
    )
    {
        if (conveyor == null)
        {
            return transform.rotation;
        }


        // ==================================================
        // FLAT CONVEYORS
        // ==================================================

        if (
            conveyor.conveyorType !=
            ConveyorType.Incline
        )
        {
            // First orient the product with the conveyor.
            Quaternion flatConveyorRotation =
                Quaternion.Euler(
                    0f,
                    conveyor.transform.eulerAngles.y,
                    0f
                );


            // Then apply the player's 0/90/180/270
            // rotation relative to that conveyor.
            Quaternion flatProductYawRotation =
                Quaternion.Euler(
                    0f,
                    productYawOffset,
                    0f
                );


            return
                flatConveyorRotation *
                flatProductYawRotation;
        }


        // ==================================================
        // INCLINE
        // ==================================================

        if (
            conveyor.pathStart == null ||
            conveyor.pathEnd == null
        )
        {
            return transform.rotation;
        }


        Vector3 direction =
            conveyor.pathEnd.position -
            conveyor.pathStart.position;


        bool travellingUp =
            direction.y >= 0f;


        float pitch =
            travellingUp
                ? -inclineTiltDegrees
                : inclineTiltDegrees;


        // --------------------------------------------------
        // STEP 1:
        // ALIGN TO THE INCLINE SURFACE
        // --------------------------------------------------

        Quaternion inclineSurfaceRotation =
            Quaternion.Euler(
                pitch,
                conveyor.transform.eulerAngles.y,
                0f
            );


        // --------------------------------------------------
        // STEP 2:
        // ROTATE THE PRODUCT ON THAT SURFACE
        // --------------------------------------------------
        //
        // This rotation is applied AFTER the incline surface
        // orientation.
        //
        // Therefore the box can be:
        //
        // 0°
        // 90°
        // 180°
        // 270°
        //
        // while still following the incline surface.

        Quaternion inclineProductYawRotation =
            Quaternion.Euler(
                0f,
                productYawOffset,
                0f
            );


        return
            inclineSurfaceRotation *
            inclineProductYawRotation;
    }


    Quaternion GetTargetConveyorRotation()
    {
        return GetTargetConveyorRotation(
            currentConveyor
        );
    }


    void RotateTowardsCurrentConveyor()
    {
        Quaternion targetRotation =
            GetTargetConveyorRotation();


        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed *
                Time.deltaTime
            );
    }


    void SetRotationToCurrentConveyorImmediate()
    {
        transform.rotation =
            GetTargetConveyorRotation();
    }


    // ======================================================
    // VISUAL HEIGHT
    // ======================================================

    void CalculateVisualBottomOffset()
    {
        Renderer[] renderers =
            GetComponentsInChildren<Renderer>();


        if (renderers.Length == 0)
        {
            Debug.LogWarning(
                "ProductMover could not find any Renderer " +
                "components on the product."
            );

            pivotToBottomOffset = 0f;

            visualOffsetCalculated = true;

            return;
        }


        Bounds combinedBounds =
            renderers[0].bounds;


        for (
            int i = 1;
            i < renderers.Length;
            i++
        )
        {
            combinedBounds.Encapsulate(
                renderers[i].bounds
            );
        }


        // Root pivot -> bottom of visible mesh.
        pivotToBottomOffset =
            transform.position.y -
            combinedBounds.min.y;


        visualOffsetCalculated = true;
    }


    void EnsureVisualOffsetCalculated()
    {
        if (!visualOffsetCalculated)
        {
            CalculateVisualBottomOffset();
        }
    }


    // ======================================================
    // CONVEYOR CONNECTION
    // ======================================================

    Conveyor FindNextConveyor()
    {
        Conveyor[] conveyors =
            FindObjectsByType<Conveyor>();


        foreach (Conveyor conveyor in conveyors)
        {
            if (conveyor == currentConveyor)
                continue;


            float distance =
                Vector3.Distance(
                    currentConveyor.snapEnd.position,
                    conveyor.snapStart.position
                );


            if (
                distance <=
                connectionTolerance
            )
            {
                return conveyor;
            }
        }


        return null;
    }
}