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
                // Product has reached the end
                // of the complete conveyor line.
                Destroy(gameObject);
            }
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


        // Start exactly where the product currently is.
        transitionStartPosition =
            transform.position;


        // --------------------------------------------------
        // CONNECTION-SEAM CLEARANCE
        // --------------------------------------------------
        //
        // Flat -> Flat:
        //      use flat clearance
        //
        // Flat -> Incline:
        //      use flat clearance at the bottom seam
        //
        // Incline -> Flat:
        //      use flat clearance at the TOP seam
        //
        // Incline -> Incline:
        //      use incline clearance

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


        // Only an Incline -> Incline seam uses
        // the incline-specific clearance.
        if (fromIncline && toIncline)
        {
            return inclineSurfaceClearance;
        }


        // Any seam involving Generic or Short
        // uses the normal flat clearance.
        //
        // This is especially important for:
        //
        // Incline -> Generic
        // Incline -> Short
        //
        // because the product needs to rise to the
        // correct flat belt height before levelling out.
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


        // SmoothStep:
        // smoother start and end than a raw linear transition.
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


        // --------------------------------------------------
        // GENERIC / SHORT
        // --------------------------------------------------

        if (
            conveyor.conveyorType !=
            ConveyorType.Incline
        )
        {
            return Quaternion.Euler(
                0f,
                conveyor.transform.eulerAngles.y,
                0f
            );
        }


        // --------------------------------------------------
        // INCLINE
        // --------------------------------------------------

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


        // Path direction is used only to decide
        // whether the product is travelling uphill
        // or downhill.
        bool travellingUp =
            direction.y >= 0f;


        float pitch =
            travellingUp
                ? -inclineTiltDegrees
                : inclineTiltDegrees;


        return Quaternion.Euler(
            pitch,
            conveyor.transform.eulerAngles.y,
            0f
        );
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