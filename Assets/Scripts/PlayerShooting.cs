using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    Vector3 lastFacingDir;

    [Header("References")]
    public Player_MoveWhileAim aimController;
    public Transform player;
    public Transform firePoint;
    public Camera mainCamera;

    [Header("Layers")]
    public LayerMask enemyLayer;
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;

    [Header("Weapon Settings")]
    public float taserRange = 3f;
    public float cooldown = 0.6f;

    [Header("Aim Assist")]
    public float assistAngle = 12f;
    public float assistRadius = 1.2f;
    public float assistStrength = 0.6f;

    [Header("Aim Dots")]
    public GameObject dotPrefab;
    public int dotCount = 10;
    public float dotSpacing = 0.3f;

    [Header("Dot Visuals")]
    public Color normalDotColor = Color.yellow;
    public Color lockedDotColor = Color.red;
    public float dotBaseScale = 0.05f;

    GameObject[] dots;
    Renderer[] dotRenderers;

    bool isAimLocked;
    float lastShootTime;
    Vector3 baseAimDir;

    void Start()
    {
        if (!mainCamera)
            mainCamera = Camera.main;

        // 🔒 Cursor ALWAYS visible
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        dots = new GameObject[dotCount];
        dotRenderers = new Renderer[dotCount];

        lastFacingDir = player.forward;

        for (int i = 0; i < dotCount; i++)
        {
            dots[i] = Instantiate(dotPrefab);
            dotRenderers[i] = dots[i].GetComponent<Renderer>();
            dots[i].SetActive(false);
        }
    }

    void Update()
    {
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        UpdateBaseAim();

        if (!aimController.IsAiming)
        {
            HideDots();
            return;
        }

        Vector3 finalDir = GetAssistedDirection(baseAimDir);
        UpdateAimDots(finalDir);

        if (Input.GetMouseButtonDown(0))
            Shoot(finalDir);
    }

    // =========================
    // BASE AIM
    // =========================
    void UpdateBaseAim()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, groundLayer))
            return;

        Vector3 dir = hit.point - firePoint.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
            dir = lastFacingDir;

        dir.Normalize();

        // prevent backward aim
        if (Vector3.Dot(dir, player.forward) < 0f)
            dir = player.forward;

        baseAimDir = dir;

        // rotate only while RMB held
        if (Input.GetMouseButton(1))
        {
            player.forward = baseAimDir;
            lastFacingDir = baseAimDir;
        }
    }

    // =========================
    // AIM DOTS (STABLE)
    // =========================
    // =========================
    // AIM DOTS (STABLE & OPTIMIZED)
    // =========================
    void UpdateAimDots(Vector3 dir)
    {
        Vector3 start = firePoint.position;
        float maxDistance = taserRange;

        // 1. Raycast once to find the end point
        if (Physics.Raycast(start, dir, out RaycastHit hit, taserRange, obstacleLayer))
        {
            maxDistance = hit.distance;
        }

        for (int i = 0; i < dotCount; i++)
        {
            float dist = dotSpacing * (i + 1);
            bool shouldBeVisible = dist <= maxDistance;

            // ONLY change active state if it's different (prevents flickering)
            if (dots[i].activeSelf != shouldBeVisible)
            {
                dots[i].SetActive(shouldBeVisible);
            }

            if (shouldBeVisible)
            {
                // Add a small Y offset (0.01f) so dots don't "Z-fight" with the floor
                Vector3 targetPos = start + (dir * dist);
                dots[i].transform.position = targetPos;

                // Color update
                Color targetColor = isAimLocked ? lockedDotColor : normalDotColor;
                if (dotRenderers[i].material.color != targetColor)
                {
                    dotRenderers[i].material.color = targetColor;
                }

                // Scale update
                float t = dist / taserRange;
                float scale = Mathf.Lerp(1f, 0.6f, t);
                dots[i].transform.localScale = Vector3.one * (scale * dotBaseScale);
            }
        }
    }

    void HideDots()
    {
        // Optimization: only loop if dots are actually active
        for (int i = 0; i < dotCount; i++)
        {
            if (dots[i].activeInHierarchy) 
                dots[i].SetActive(false);
        }
    }

    // =========================
    // SHOOT
    // =========================
    void Shoot(Vector3 dir)
    {
        if (Time.time < lastShootTime + cooldown)
            return;

        lastShootTime = Time.time;

        if (Physics.Raycast(firePoint.position, dir, out RaycastHit hit, taserRange))
        {
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
            if (enemy)
                enemy.Die();
        }
    }

    // =========================
    // AIM ASSIST (NO WALL LOCK)
    // =========================
    Vector3 GetAssistedDirection(Vector3 baseDir)
    {
        isAimLocked = false;

        Collider[] enemies = Physics.OverlapSphere(
            firePoint.position,
            taserRange,
            enemyLayer
        );

        Transform bestTarget = null;
        float bestScore = float.MaxValue;

        foreach (Collider col in enemies)
        {
            Vector3 toEnemy = col.transform.position - firePoint.position;
            toEnemy.y = 0f;

            float dist = toEnemy.magnitude;
            if (dist < 0.1f)
                continue;

            Vector3 dirToEnemy = toEnemy.normalized;

            float angle = Vector3.Angle(baseDir, dirToEnemy);
            if (angle > assistAngle)
                continue;

            float perpendicular = Vector3.Cross(baseDir, toEnemy).magnitude;
            if (perpendicular > assistRadius)
                continue;

            float score = angle + perpendicular * 0.5f;

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = col.transform;
            }
        }

        if (!bestTarget)
            return baseDir;

        Vector3 toTarget = bestTarget.position - firePoint.position;
        toTarget.y = 0f;

        // blocked by obstacle → no lock
        if (Physics.Raycast(
            firePoint.position,
            toTarget.normalized,
            taserRange,
            obstacleLayer))
        {
            return baseDir;
        }

        isAimLocked = true;

        return Vector3.Slerp(
            baseDir,
            toTarget.normalized,
            assistStrength
        ).normalized;
    }
}
