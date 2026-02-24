using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    public EnemyBehaviour enemyAI;
    public Transform shootPoint;
    public LayerMask hitMask;

    public float fireRate = 0.8f;

    float nextFireTime;

    void Update()
    {
        if (!enemyAI || !enemyAI.player) return;
        if (!enemyAI.CanSeeTarget) return;

        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        Vector3 dir = (enemyAI.player.position - shootPoint.position).normalized;

        if (Physics.Raycast(
            shootPoint.position,
            dir,
            out RaycastHit hit,
            enemyAI.viewDistance,
            hitMask))
        {
            if (hit.transform == enemyAI.player)
            {
                PlayerDeath death = hit.transform.GetComponent<PlayerDeath>();
                if (death) death.Die();
            }
        }
    }
}