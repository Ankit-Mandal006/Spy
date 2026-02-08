using StarterAssets;
using UnityEngine;

public class EnemyDeath : MonoBehaviour
{
    public EnemyBehaviour enemy;
    public ThirdPersonController playerController;
    public Animator playerAnim;
    public float killDistance = 1.5f;

    bool isPlayerInRange;

    void Update()
    {
        if (!isPlayerInRange) return;
        if (enemy.isDead) return;

        float dist = Vector3.Distance(
            transform.position,
            playerController.transform.position
        );

        if (dist > killDistance) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            // Lock movement (NOT animator)
            playerController.enabled = false;

            // Play kill animation
            playerAnim.SetTrigger("Kill");

            // Kill enemy
            enemy.Die();

            // Disable further interaction
            isPlayerInRange = false;
            GetComponent<Collider>().enabled = false;

            // Re-enable movement AFTER animation
            Invoke(nameof(EnablePlayer), 2.25f);
        }
    }

    void EnablePlayer()
    {
        playerController.enabled = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            isPlayerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            isPlayerInRange = false;
    }
}
