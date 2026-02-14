using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class EnemyFOV : MonoBehaviour
{
    [Header("FOV Settings")]
    public float viewRadius = 6f;
    [Range(0, 360)] public float viewAngle = 90f;
    public int rayCount = 50;
    public float rayHeight = 1.5f; // eye level


    [Header("Layers")]
    public LayerMask obstacleLayer;

    [Header("Visual Offset")]
    public float groundHeight = 0.02f; // prevents z-fighting

    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;

    void Awake()
    {
        mesh = new Mesh { name = "Enemy FOV Mesh" };
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void LateUpdate()
    {
        DrawFOV();
    }

    void DrawFOV()
{
    int vertexCount = rayCount + 2;

    vertices = new Vector3[vertexCount];
    triangles = new int[rayCount * 3];
    uvs = new Vector2[vertexCount];

    vertices[0] = new Vector3(0, groundHeight, 0);
    uvs[0] = new Vector2(0.5f, 0f);

    float angleStep = viewAngle / rayCount;
    float angle = -viewAngle * 0.5f;

    Vector3 rayOrigin = transform.position;
    rayOrigin.y = rayHeight;   // ✅ FIX

    for (int i = 0; i <= rayCount; i++)
    {
        Vector3 dir = DirFromAngle(angle);
        float distance = viewRadius;

        if (Physics.Raycast(rayOrigin, dir, out RaycastHit hit, viewRadius, obstacleLayer))
            distance = hit.distance;

        Vector3 localPoint = dir * distance;
        localPoint.y = groundHeight;

        vertices[i + 1] = localPoint;

        float u = i / (float)rayCount;
        float v = distance / viewRadius;
        uvs[i + 1] = new Vector2(u, v);

        if (i < rayCount)
        {
            int idx = i * 3;
            triangles[idx] = 0;
            triangles[idx + 1] = i + 1;
            triangles[idx + 2] = i + 2;
        }

        angle += angleStep;
    }

    mesh.Clear();
    mesh.vertices = vertices;
    mesh.triangles = triangles;
    mesh.uv = uvs;
    mesh.RecalculateBounds();
}


    Vector3 DirFromAngle(float angle)
    {
        float rad = (angle + transform.eulerAngles.y) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
    }
}
