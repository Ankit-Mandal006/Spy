using UnityEngine;
using StarterAssets;

public class EnemyDeath : MonoBehaviour
{
    public ThirdPersonController thirdPersonController;
    public Animator anim,enemyAnim;
    bool isPlayerinRange=false;
    public BoxCollider col;
    public CapsuleCollider cc;
    public GameObject RagDoll;
    public EnemyBehaviour eb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //enemyAnim=this.GetComponent<Animator>();
    }

    void ActivateRagDoll()
{
   // Detach ragdoll from parent
RagDoll.transform.parent = null;

// Place it at enemy position
RagDoll.transform.position = transform.position;
RagDoll.transform.rotation = transform.rotation;

// Enable ragdoll
RagDoll.SetActive(true);

// Reset rigidbodies
foreach (Rigidbody rb in RagDoll.GetComponentsInChildren<Rigidbody>())
{
    rb.linearVelocity = Vector3.zero;
    rb.angularVelocity = Vector3.zero;
}

// Disable agent
eb.agent.isStopped = true;
eb.agent.ResetPath();
eb.agent.velocity = Vector3.zero;
eb.agent.updatePosition = false;
eb.agent.updateRotation = false;
eb.agent.enabled = false;

// Optional: hide enemy model

enemyAnim.enabled = false;
//cc.enabled = false;
col.enabled = false;
thirdPersonController.enabled=true;
    
}

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)&&isPlayerinRange)
        {
            eb.HandleDeath();
            eb.isDead=true;
            anim.SetTrigger("Kill");
            enemyAnim.SetTrigger("isDead");
            col.enabled=false;
            isPlayerinRange=false;
            thirdPersonController.enabled=false;
            Invoke("ActivateRagDoll",2.25f);
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
