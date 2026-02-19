using UnityEngine;
using StarterAssets;

public class Player_MoveWhileAim : MonoBehaviour
{
    [Header("References")]
    public ThirdPersonController tpc;
    public Camera mainCamera;
    public LayerMask groundLayer;

    [Header("Rotation")]
    public float rotationSpeed = 15f;

    private Animator animator;
    private StarterAssetsInputs input;
    private CharacterController characterController;

    public bool IsAiming { get; private set; }

    // Animator parameter hashes
    private int aimID;
    private int movXID;
    private int movYID; // named MovY in Animator but represents Z (forward/back)

    void Start()
    {
        animator = GetComponent<Animator>();
        input = GetComponent<StarterAssetsInputs>();
        characterController = GetComponent<CharacterController>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        aimID  = Animator.StringToHash("Aim");
        movXID = Animator.StringToHash("MovX");
        movYID = Animator.StringToHash("MovY"); // must match exactly in Animator window
    }

    void Update()
{
    bool wasAiming = IsAiming;
    IsAiming = Input.GetMouseButton(1);

    if (tpc != null)
        tpc.AimMode = IsAiming;

    animator.SetBool(aimID, IsAiming);

    // Clear jump input the moment we stop aiming
    // so the buffered jump doesn't fire instantly
    if (wasAiming && !IsAiming)
        input.jump = false;

    if (IsAiming)
    {
        // Also suppress jump every frame while aiming
        input.jump = false;

        RotateTowardMouse();
        UpdateStrafeAnimation();

        if (tpc != null)
            tpc.SetSprintEnabled(false);
    }
    else
    {
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
                    Time.deltaTime * rotationSpeed
                );
            }
        }
    }

    void UpdateStrafeAnimation()
{
    Vector3 velocity = characterController.velocity;
    velocity.y = 0f;

    float localX = Mathf.Round(Vector3.Dot(velocity.normalized, transform.right));
    float localZ = Mathf.Round(Vector3.Dot(velocity.normalized, transform.forward));

    // Using direct strings to debug - check these match EXACTLY in Animator window
    animator.SetFloat("MovX", localX, 0.1f, Time.deltaTime);
    animator.SetFloat("MovY", localZ, 0.1f, Time.deltaTime);
}
}