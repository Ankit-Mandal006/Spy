using UnityEngine;
using StarterAssets;

public class Crouched : MonoBehaviour
{
    private ThirdPersonController thirdPersonController;
    public Animator anim;

    public bool crouched = false;

    void Start()
    {
        thirdPersonController = GetComponent<ThirdPersonController>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            crouched = !crouched;
        }

        if (crouched)
        {
            anim.SetBool("Crouched", true);

            thirdPersonController.MoveSpeed = 1f;
            thirdPersonController.SetSprintEnabled(false); 
        }
        else
        {
            anim.SetBool("Crouched", false);

            thirdPersonController.MoveSpeed = 2f;
            thirdPersonController.SetSprintEnabled(true); 
        }
    }
}
