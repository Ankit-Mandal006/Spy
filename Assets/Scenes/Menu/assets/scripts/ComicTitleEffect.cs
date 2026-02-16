using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ComicTitleEffect : MonoBehaviour
{
    [Header("Enter Settings")]
    [SerializeField] private float enterDuration = 0.8f;
    [SerializeField] private float enterDistance = 1500f;

    [Header("Infinite Zoom Settings")]
    [SerializeField] private float zoomScale = 1.15f;
    [SerializeField] private float zoomSpeed = 2f;

    [Header("Glow")]
    [SerializeField] private float maxGlow = 1f;

    [Header("Dash AfterImage")]
    [SerializeField] private bool enableDashEffect = true;
    [SerializeField] private float ghostSpawnRate = 0.05f;
    [SerializeField] private float ghostFadeSpeed = 4f;

    private RectTransform rect;
    private Vector3 baseScale;
    private Vector2 targetPos;
    private Vector2 startPos;

    private TextMeshProUGUI tmp;
    private Material glowMaterial;

    private float timer;
    private float ghostTimer;

    private bool enterFinished = false;

    private List<GameObject> activeGhosts = new List<GameObject>();

    void Start()
    {
        rect = GetComponent<RectTransform>();
        baseScale = rect.localScale;

        targetPos = rect.anchoredPosition;
        startPos = targetPos + Vector2.left * enterDistance;
        rect.anchoredPosition = startPos;

        tmp = GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            glowMaterial = Instantiate(tmp.fontMaterial);
            tmp.fontMaterial = glowMaterial;
        }
    }

    void Update()
    {
        if (!enterFinished)
        {
            EnterPhase();
        }
        else
        {
            InfiniteZoom();
        }

        UpdateGhosts();
    }

    void EnterPhase()
    {
        timer += Time.deltaTime;

        float t = timer / enterDuration;
        float ease = 1f - Mathf.Pow(1f - t, 3f);

        rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, ease);

        if (glowMaterial != null)
            glowMaterial.SetFloat("_GlowPower", Mathf.Lerp(maxGlow, 0f, ease));

        if (enableDashEffect)
            SpawnGhosts();

        if (t >= 1f)
        {
            rect.anchoredPosition = targetPos;
            timer = 0f;
            enterFinished = true;
        }
    }

    void InfiniteZoom()
    {
        // Smooth breathing zoom using sine wave
        float scale = 1f + Mathf.Sin(Time.time * zoomSpeed) * (zoomScale - 1f);
        rect.localScale = baseScale * scale;
    }

    void SpawnGhosts()
    {
        ghostTimer += Time.deltaTime;

        if (ghostTimer >= ghostSpawnRate)
        {
            ghostTimer = 0f;

            GameObject ghost = Instantiate(tmp.gameObject, rect.parent);
            ghost.transform.position = tmp.transform.position;
            ghost.transform.rotation = tmp.transform.rotation;
            ghost.transform.localScale = tmp.transform.localScale;

            TextMeshProUGUI ghostTMP = ghost.GetComponent<TextMeshProUGUI>();
            Color c = ghostTMP.color;
            c.a = 0.6f;
            ghostTMP.color = c;

            activeGhosts.Add(ghost);
        }
    }

    void UpdateGhosts()
    {
        for (int i = activeGhosts.Count - 1; i >= 0; i--)
        {
            if (activeGhosts[i] == null)
            {
                activeGhosts.RemoveAt(i);
                continue;
            }

            TextMeshProUGUI g = activeGhosts[i].GetComponent<TextMeshProUGUI>();
            Color c = g.color;
            c.a -= Time.deltaTime * ghostFadeSpeed;
            g.color = c;

            if (c.a <= 0)
            {
                Destroy(activeGhosts[i]);
                activeGhosts.RemoveAt(i);
            }
        }
    }
}
