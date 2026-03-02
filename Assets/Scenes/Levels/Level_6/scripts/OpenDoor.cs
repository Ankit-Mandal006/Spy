using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    [Header("Door Settings")]
    public Vector3 openOffset;
    public float moveSpeed = 2f;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Vector3 targetPosition;

    void Start()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + openOffset;
        targetPosition = closedPosition;
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );
    }

    public void Open()
    {
        targetPosition = openPosition;
    }

    public void Close()
    {
        targetPosition = closedPosition;
    }
}