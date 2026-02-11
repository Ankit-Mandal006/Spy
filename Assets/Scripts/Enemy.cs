using UnityEngine;

public class Enemy : MonoBehaviour
{
    public GameObject deathEffect;

    public void Die()
    {
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
