using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class EnemyFOV : MonoBehaviour
{
    [Header("FOV Settings")]
    public float viewRadius = 6f;
    [Range(0, 360)] public float viewAngle = 90f;
    public int   rayCount  = 50;
    public float rayHeight = 1.5f;

    [Header("Layers")]
    [Tooltip("Walls and cover only — do NOT include the player layer")]
    public LayerMask obstacleLayer;

    [Header("Visual")]
    public float groundHeight = 0.02f;
    public Color normalColor  = new Color(1f, 0.92f, 0f, 0.35f);   // yellow
    public Color alertColor   = new Color(1f, 0.1f,  0f, 0.55f);   // red

    [Header("References")]
    [Tooltip("The enemy root GameObject (with EnemyBehaviour)")]
    public Transform enemy;

    // ── Private ────────────────────────────────────────────────────
    Mesh                  _mesh;
    MeshRenderer          _renderer;
    MaterialPropertyBlock _propBlock;

    Vector3[] _vertices;
    int[]     _triangles;
    Vector2[] _uvs;
    int       _cachedRayCount = -1;

    void Awake()
    {
        _mesh = new Mesh { name = "Enemy FOV Mesh" };
        GetComponent<MeshFilter>().mesh = _mesh;

        _renderer  = GetComponent<MeshRenderer>();
        _propBlock = new MaterialPropertyBlock();

        // Start yellow
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_Color", normalColor);
        _renderer.SetPropertyBlock(_propBlock);

        if (enemy == null && transform.parent != null)
            enemy = transform.parent;
    }

    void LateUpdate()
    {
        if (enemy == null) return;
        DrawFOV();
    }

    // Called by EnemyBehaviour every frame with its detection result
    public void SetAlert(bool seeing)
    {
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_Color", seeing ? alertColor : normalColor);
        _renderer.SetPropertyBlock(_propBlock);
    }

    // ── FOV Mesh ───────────────────────────────────────────────────
    void DrawFOV()
    {
        if (_cachedRayCount != rayCount)
        {
            _vertices       = new Vector3[rayCount + 2];
            _triangles      = new int[rayCount * 3];
            _uvs            = new Vector2[rayCount + 2];
            _cachedRayCount = rayCount;
        }

        Vector3 rayOrigin = new Vector3(enemy.position.x, rayHeight, enemy.position.z);

        _vertices[0] = transform.InverseTransformPoint(
            new Vector3(enemy.position.x, enemy.position.y + groundHeight, enemy.position.z));
        _uvs[0] = new Vector2(0.5f, 0f);

        float angleStep = viewAngle / rayCount;
        float angle     = -viewAngle * 0.5f;

        for (int i = 0; i <= rayCount; i++)
        {
            Vector3 dir      = DirFromAngle(angle);
            float   distance = viewRadius;

            if (Physics.Raycast(rayOrigin, dir, out RaycastHit hit, viewRadius, obstacleLayer))
                distance = hit.distance;

            Vector3 worldPoint = new Vector3(
                rayOrigin.x + dir.x * distance,
                enemy.position.y + groundHeight,
                rayOrigin.z + dir.z * distance);

            _vertices[i + 1] = transform.InverseTransformPoint(worldPoint);
            _uvs[i + 1]      = new Vector2(i / (float)rayCount, distance / viewRadius);

            if (i < rayCount)
            {
                int idx           = i * 3;
                _triangles[idx]   = 0;
                _triangles[idx+1] = i + 1;
                _triangles[idx+2] = i + 2;
            }

            angle += angleStep;
        }

        _mesh.Clear();
        _mesh.vertices  = _vertices;
        _mesh.triangles = _triangles;
        _mesh.uv        = _uvs;
        _mesh.RecalculateBounds();
    }

    Vector3 DirFromAngle(float angleDeg)
    {
        float rad = (angleDeg + enemy.eulerAngles.y) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
    }
}