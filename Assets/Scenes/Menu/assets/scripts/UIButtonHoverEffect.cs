using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class UIButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float scaleSpeed = 10f;

    [Header("Glow Settings")]
    [SerializeField] private float hoverGlow = 1.2f;
    [SerializeField] private float glowSpeed = 10f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private TextMeshProUGUI tmp;
    private Material buttonMaterial;   // UNIQUE material

    private float currentGlow = 0f;
    private float targetGlow = 0f;

    void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;

        tmp = GetComponentInChildren<TextMeshProUGUI>();

        if (tmp != null)
        {
            // IMPORTANT: Create unique material instance ONLY for this button
            buttonMaterial = new Material(tmp.fontMaterial);
            tmp.fontMaterial = buttonMaterial;

            buttonMaterial.SetFloat("_GlowPower", 0f);
        }
    }

    void Update()
    {
        // Smooth scaling
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);

        // Smooth glow
        if (buttonMaterial != null)
        {
            currentGlow = Mathf.Lerp(currentGlow, targetGlow, Time.deltaTime * glowSpeed);
            buttonMaterial.SetFloat("_GlowPower", currentGlow);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
        targetGlow = hoverGlow;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
        targetGlow = 0f;
    }
}
