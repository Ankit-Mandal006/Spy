using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class DominoLoadingEffect : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string sceneToLoad = "New Scene";

    [Header("Domino Cycles (How many times dots run)")]
    [SerializeField] private int dominoCycles = 3;

    [Header("Dots")]
    [SerializeField] private List<RectTransform> dots;
    [SerializeField] private float jumpHeight = 40f;
    [SerializeField] private float jumpSpeed = 6f;
    [SerializeField] private float delayBetweenDots = 0.15f;

    [Header("3D Boom VFX")]
    [SerializeField] private GameObject boomVFX;
    [SerializeField] private float boomDelayBeforeLoad = 2f;

    [Header("Loading Canvas")]
    [SerializeField] private Canvas loadingCanvas;

    private List<Vector2> originalPositions = new List<Vector2>();

    private void Awake()
    {
        if (boomVFX != null)
            boomVFX.SetActive(false);
    }

    public void StartLoading()
    {
        originalPositions.Clear();

        foreach (var dot in dots)
            originalPositions.Add(dot.anchoredPosition);

        StartCoroutine(LoadingRoutine());
    }


    IEnumerator LoadingRoutine()
    {
        // 🔁 Run full domino wave X times
        for (int cycle = 0; cycle < dominoCycles; cycle++)
        {
            for (int i = 0; i < dots.Count; i++)
            {
                StartCoroutine(JumpDot(i));
                yield return new WaitForSeconds(delayBetweenDots);
            }

            // Small pause so last dot finishes cleanly
            yield return new WaitForSeconds(1f / jumpSpeed);
        }

        // Reset dots to original position
        for (int i = 0; i < dots.Count; i++)
            dots[i].anchoredPosition = originalPositions[i];

        // 🔥 Activate Boom VFX
        if (boomVFX != null)
        {
            boomVFX.SetActive(true);

            ParticleSystem ps = boomVFX.GetComponent<ParticleSystem>();
            if (ps != null)
                ps.Play();
        }

        // 🔥 Hide Loading Canvas when boom starts
        if (loadingCanvas != null)
            loadingCanvas.gameObject.SetActive(false);

        // Wait before loading next scene
        yield return new WaitForSeconds(boomDelayBeforeLoad);

        SceneManager.LoadScene(sceneToLoad);
    }

    IEnumerator JumpDot(int index)
    {
        RectTransform dot = dots[index];
        Vector2 startPos = originalPositions[index];

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * jumpSpeed;
            float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            dot.anchoredPosition = startPos + Vector2.up * height;
            yield return null;
        }

        dot.anchoredPosition = startPos;
    }
}
