using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Auto Aim")]
    public float autoAimRadius = 1.5f;

    [Header("Taser Settings")]
    public float taserRange = 3f;
    public float cooldown = 0.6f;

    [Header("References")]
    public Transform firePoint;
    public LayerMask enemyLayer;
    public LayerMask groundLayer;
    public Camera mainCamera;

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

        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void UpdateAimPoint()
{
    Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
    RaycastHit hit;

    if (Physics.Raycast(ray, out hit, 200f, groundLayer))
    {
        aimPoint = hit.point;

        // 🔍 Find enemies near cursor point
        Collider[] enemies = Physics.OverlapSphere(
            aimPoint,
            autoAimRadius,
            enemyLayer
        );

        Transform closestEnemy = null;
        float closestDist = Mathf.Infinity;

        foreach (Collider col in enemies)
        {
            float dist = Vector3.Distance(aimPoint, col.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestEnemy = col.transform;
            }
        }

        // 🎯 If enemy found, auto-aim to it
        if (closestEnemy != null)
        {
            aimPoint = closestEnemy.position;
        }

        // Rotate gun / player (flat)
        Vector3 lookDir = aimPoint - firePoint.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.001f)
            firePoint.forward = lookDir.normalized;
    }
}


    void Shoot()
{
    if (Time.time < lastShootTime + cooldown)
        return;

    lastShootTime = Time.time;

    Vector3 shootDir = aimPoint - firePoint.position;
    shootDir.y = 0f;                 // 🔥 LOCK Y AXIS
    shootDir = shootDir.normalized;

    Debug.DrawRay(firePoint.position, shootDir * taserRange, Color.red, 1f);

    RaycastHit hit;
    if (Physics.Raycast(firePoint.position, shootDir, out hit, taserRange))
    {
        Debug.Log("Hit: " + hit.collider.gameObject.name);

        Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.Die();
        }
    }
    else
    {
        Debug.Log("No Hit");
    }
}


}
