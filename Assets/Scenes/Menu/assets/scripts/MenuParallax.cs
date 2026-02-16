using UnityEngine;

public class MenuParallax : MonoBehaviour
{
    public float offsetAmount = 20f;
    public float smoothSpeed = 5f;

    private RectTransform rectTransform;
    private Vector2 startPos;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startPos = rectTransform.anchoredPosition;
    }

    void Update()
    {
        Vector2 mousePos = Input.mousePosition;
        Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);

        Vector2 offset = (mousePos - center) / center;

        Vector2 targetPos = startPos + offset * offsetAmount;

        rectTransform.anchoredPosition =
            Vector2.Lerp(rectTransform.anchoredPosition, targetPos, Time.deltaTime * smoothSpeed);
    }
}
