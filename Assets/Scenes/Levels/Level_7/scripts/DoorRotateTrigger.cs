using UnityEngine;

public class DoorRotateTrigger : MonoBehaviour
{
    [Header("Door Settings")]
    public Transform doorToRotate;
    public Vector3 rotationOffset;   // Degrees
    public float rotateSpeed = 120f;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Quaternion targetRotation;

    void Start()
    {
        closedRotation = doorToRotate.localRotation;
        openRotation = closedRotation * Quaternion.Euler(rotationOffset);
        targetRotation = closedRotation;
    }

    void Update()
    {
        doorToRotate.localRotation = Quaternion.RotateTowards(
            doorToRotate.localRotation,
            targetRotation,
            rotateSpeed * Time.deltaTime
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            targetRotation = openRotation;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            targetRotation = closedRotation;
        }
    }
}