using UnityEngine;
using StarterAssets;

public class Player_MoveWhileAim : MonoBehaviour
{
    [Header("References")]
    public ThirdPersonController tpc;
    public Camera mainCamera;
    public LayerMask groundLayer;

    private Animator animator;
    private StarterAssetsInputs input;

    public bool IsAiming { get; private set; }

    private int aimID;
    private int movXID;
    private int movYID;

    void Start()
    {
        animator = GetComponent<Animator>();
        input = GetComponent<StarterAssetsInputs>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        aimID = Animator.StringToHash("Aim");
        movXID = Animator.StringToHash("MovX");
        movYID = Animator.StringToHash("MovY");
    }

    void Update()
    {
        IsAiming = Input.GetMouseButton(1); // RMB

        animator.SetBool(aimID, IsAiming);

        if (IsAiming)
        {
            RotateTowardMouse();
            UpdateStrafeMovement();
            tpc.SetSprintEnabled(false); // no sprint while aiming
        }
        else
        {
            animator.SetFloat(movXID, 0);
            animator.SetFloat(movYID, 0);
        }
    }

    void RotateTowardMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, groundLayer))
        {
            Vector3 dir = hit.point - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    Time.deltaTime * 15f
                );
            }
        }
    }

    void UpdateStrafeMovement()
    {
        // Input relative to player forward/right
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        Vector3 move =
            forward * input.move.y +
            right * input.move.x;

        float movX = Vector3.Dot(move, right);
        float movY = Vector3.Dot(move, forward);

        animator.SetFloat(movXID, movX, 0.1f, Time.deltaTime);
        animator.SetFloat(movYID, movY, 0.1f, Time.deltaTime);
    }
}
