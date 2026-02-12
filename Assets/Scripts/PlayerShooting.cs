using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("References")]
    public Player_MoveWhileAim aimController;
    public Transform firePoint;
    public Camera mainCamera;

    [Header("Layers")]
    public LayerMask enemyLayer;
    public LayerMask groundLayer;

    [Header("Weapon Settings")]
    public float taserRange = 3f;
    public float cooldown = 0.6f;

    [Header("Aim Assist (Isometric Friendly)")]
    [Tooltip("How wide the aim assist cone is")]
    public float assistAngle = 10f;

    [Tooltip("How close the bullet must pass to snap")]
    public float assistRadius = 1.2f;

    private float lastShootTime;
    private Vector3 aimPoint;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UpdateAimPoint();

        if (!aimController.IsAiming)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    // =========================
    // AIMING
    // =========================
    void UpdateAimPoint()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, groundLayer))
        {
            aimPoint = hit.point;

            Vector3 lookDir = aimPoint - firePoint.position;
            lookDir.y = 0f;

            if (lookDir.sqrMagnitude > 0.001f)
                firePoint.forward = lookDir.normalized;
        }
    }

    // =========================
    // SHOOTING
    // =========================
    void Shoot()
    {
        if (Time.time < lastShootTime + cooldown)
            return;

        lastShootTime = Time.time;

        Vector3 baseDir = aimPoint - firePoint.position;
        baseDir.y = 0f;
        baseDir.Normalize();

        Vector3 finalDir = GetAssistedDirection(baseDir);

        Debug.DrawRay(firePoint.position, finalDir * taserRange, Color.red, 1f);

        if (Physics.Raycast(firePoint.position, finalDir, out RaycastHit hit, taserRange))
        {
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.Die();
            }
        }
    }

    // =========================
    // AIM ASSIST LOGIC
    // =========================
    Vector3 GetAssistedDirection(Vector3 baseDir)
    {
        Collider[] enemies = Physics.OverlapSphere(
            firePoint.position,
            taserRange,
            enemyLayer
        );

        Transform bestTarget = null;
        float bestScore = Mathf.Infinity;

        foreach (Collider col in enemies)
        {
            Vector3 toEnemy = col.transform.position - firePoint.position;
            toEnemy.y = 0f;

            float distance = toEnemy.magnitude;
            if (distance <= 0.01f)
                continue;

            Vector3 dirToEnemy = toEnemy.normalized;

            // Angle check (cone)
            float angle = Vector3.Angle(baseDir, dirToEnemy);
            if (angle > assistAngle)
                continue;

            // Distance from shot ray
            float perpendicularDist =
                Vector3.Cross(baseDir, toEnemy).magnitude;

            if (perpendicularDist > assistRadius)
                continue;

            // Lower score = better target
            float score = angle * 0.7f + perpendicularDist * 0.3f;

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = col.transform;
            }
        }

        if (bestTarget != null)
        {
            Vector3 assistedDir = bestTarget.position - firePoint.position;
            assistedDir.y = 0f;
            return assistedDir.normalized;
        }

        return baseDir;
    }
}
