using UnityEngine;

public class Enemy : MonoBehaviour
{
    public GameObject DeadEnemy;
    public GameObject _Enemy;

    public void Die()
    {
        if (DeadEnemy != null)
            Instantiate(DeadEnemy, transform.position, Quaternion.identity);

        Destroy(_Enemy.gameObject);
    }
}
