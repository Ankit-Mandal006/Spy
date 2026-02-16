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
        IsAiming = Input.GetMouseButton(1); // RMB to aim

        animator.SetBool(aimID, IsAiming);

        if (IsAiming)
        {
            RotateTowardMouse();
            UpdateStrafeMovement();

            if (tpc != null)
                tpc.SetSprintEnabled(false); // disable sprint while aiming
        }
        else
        {
            // Reset blend tree smoothly
            animator.SetFloat(movXID, 0f, 0.1f, Time.deltaTime);
            animator.SetFloat(movYID, 0f, 0.1f, Time.deltaTime);
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
        // Direct input values (THIS fixes W/S animation)
        float movX = input.move.x; // A / D
        float movY = input.move.y; // W / S

        animator.SetFloat(movXID, movX, 0.1f, Time.deltaTime);
        animator.SetFloat(movYID, movY, 0.1f, Time.deltaTime);
    }
}
