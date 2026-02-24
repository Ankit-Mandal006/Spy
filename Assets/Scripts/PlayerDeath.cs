using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    [Header("Ragdoll")]
    public GameObject ragdollPrefab;
    public Animator animator;

    bool isDead;

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // Disable player scripts
        foreach (MonoBehaviour m in GetComponents<MonoBehaviour>())
        {
            if (m != this) m.enabled = false;
        }

        SpawnRagdoll();
        gameObject.SetActive(false);
    }

    void SpawnRagdoll()
    {
        GameObject rag = Instantiate(ragdollPrefab,
            transform.position, transform.rotation);
        rag.SetActive(true);
        CopyPose(animator, rag.GetComponent<Animator>());
    }

    void CopyPose(Animator src, Animator dst)
    {
        if (!src || !dst) return;

        for (int i = 0; i < src.transform.childCount; i++)
        {
            Transform srcBone = src.transform.GetChild(i);
            Transform dstBone = dst.transform.Find(srcBone.name);

            if (dstBone)
            {
                dstBone.position = srcBone.position;
                dstBone.rotation = srcBone.rotation;
            }
        }
    }
}