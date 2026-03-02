using UnityEngine;

public class FloatingPlatform : MonoBehaviour
{
    [Header("Movement")]
    public Vector3 pointBOffset;   // Offset from start position
    public float moveSpeed = 2f;
    public bool loop;

    private Vector3 pointA;
    private Vector3 pointB;
    private Vector3 target;

    void Start()
    {
        pointA = transform.position;
        pointB = pointA + pointBOffset;
        target = pointB;
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target) < 0.01f)
        {
            if (loop)
            {
                target = target == pointB ? pointA : pointB;
            }
        }
    }
}