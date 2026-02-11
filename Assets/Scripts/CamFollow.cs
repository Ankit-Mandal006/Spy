using UnityEngine;

public class CamFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Offset")]
    public Vector3 offset = new Vector3(0f, 12f, -12f);

    [Header("Follow Settings")]
    public float followSmoothTime = 0.25f;
    public float stopSmoothTime = 0.45f;

    [Header("Look Ahead")]
    public float lookAheadDistance = 2f;
    public float lookAheadSmoothTime = 0.3f;

    [Header("Dead Zone")]
    public float movementThreshold = 0.05f;

    private Vector3 currentVelocity;
    private Vector3 lookAheadVelocity;
    private Vector3 currentLookAhead;
    private Vector3 lastTargetPosition;

    void Start()
    {
        if (target != null)
            lastTargetPosition = target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Player movement delta
        Vector3 movementDelta = target.position - lastTargetPosition;

        // Ignore tiny jitter
        bool isMoving = movementDelta.magnitude > movementThreshold;

        // Desired look ahead
        Vector3 targetLookAhead = isMoving
            ? movementDelta.normalized * lookAheadDistance
            : Vector3.zero;

        // Smooth look ahead separately (VERY IMPORTANT)
        currentLookAhead = Vector3.SmoothDamp(
            currentLookAhead,
            targetLookAhead,
            ref lookAheadVelocity,
            lookAheadSmoothTime
        );

        // Desired camera position
        Vector3 desiredPosition = target.position + offset + currentLookAhead;

        // Different smooth times for moving vs stopping
        float smoothTime = isMoving ? followSmoothTime : stopSmoothTime;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            smoothTime
        );

        lastTargetPosition = target.position;
    }
}
