using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    public OpenDoor door;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            door.Open();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            door.Close();
        }
    }
}