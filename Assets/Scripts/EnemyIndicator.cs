using UnityEngine;

public class EnemyIndicator : MonoBehaviour
{
    public Renderer indicatorRenderer;
    public Color normalColor = Color.yellow;
    public Color alertColor = Color.red;
    public LayerMask enemyLayer;

    private int enemyCount = 0;

    void Start()
    {
        if (indicatorRenderer == null)
            indicatorRenderer = GetComponent<Renderer>();

        indicatorRenderer.material.color = normalColor;
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            enemyCount++;
            indicatorRenderer.material.color = alertColor;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            enemyCount--;

            if (enemyCount <= 0)
            {
                enemyCount = 0;
                indicatorRenderer.material.color = normalColor;
            }
        }
    }
}
