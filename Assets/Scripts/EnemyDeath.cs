using UnityEngine;
using StarterAssets;

public class EnemyDeath : MonoBehaviour
{
    public ThirdPersonController thirdPersonController;
    public Animator anim,enemyAnim;
    bool isPlayerinRange=false;
    public BoxCollider col;
    public GameObject RagDoll;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemyAnim=this.GetComponent<Animator>();
    }

    void ActivateRagDoll()
    {
        enemyAnim.enabled=false;
        RagDoll.SetActive(true);
        thirdPersonController.enabled=true;
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)&&isPlayerinRange)
        {
            anim.SetTrigger("Kill");
            enemyAnim.SetTrigger("isDead");
            col.enabled=false;
            isPlayerinRange=false;
            thirdPersonController.enabled=false;
            Invoke("ActivateRagDoll",2.1f);
        }
    }
    void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Player"))
    {
        Debug.Log("PlayerEnter");
        isPlayerinRange=true;
    }
}
void OnTriggerExit(Collider other)
{
    if (other.CompareTag("Player"))
    {
        Debug.Log("PlayerExit");
        isPlayerinRange=false;
    }
}
}
