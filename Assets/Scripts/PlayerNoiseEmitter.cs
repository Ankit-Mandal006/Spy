using UnityEngine;
using StarterAssets;

public class PlayerNoiseEmitter : MonoBehaviour
{
    [Header("Noise Strength (Design Friendly)")]
    public float walkNoise = 0.35f;
    public float runNoise = 0.7f;
    public float sprintNoise = 1.0f;
    public float jumpNoise = 0.9f;
    public float crouchNoise = 0.15f;
    public float aimWalkNoise = 0.25f;

    [Header("Noise Timing")]
    public float stepInterval = 0.35f;

    float _stepTimer;

    ThirdPersonController tpc;
    CharacterController controller;
    StarterAssetsInputs input;
    Crouched crouch;
    Player_MoveWhileAim aim;

    void Start()
    {
        tpc = GetComponent<ThirdPersonController>();
        controller = GetComponent<CharacterController>();
        input = GetComponent<StarterAssetsInputs>();
        crouch = GetComponent<Crouched>();
        aim = GetComponent<Player_MoveWhileAim>();
    }

    void Update()
    {
        HandleMovementNoise();
        HandleJumpNoise();
    }

    void HandleMovementNoise()
    {
        if (!controller.isGrounded) return;
        if (controller.velocity.magnitude < 0.1f) return;

        _stepTimer += Time.deltaTime;
        if (_stepTimer < stepInterval) return;
        _stepTimer = 0f;

        float loudness = GetMovementLoudness();
        EmitNoise(loudness);
    }

    void HandleJumpNoise()
    {
        if (!controller.isGrounded && input.jump)
        {
            EmitNoise(jumpNoise);
        }
    }

    float GetMovementLoudness()
    {
        if (crouch != null && crouch.crouched)
            return crouchNoise;

        if (aim != null && aim.IsAiming)
            return aimWalkNoise;

        if (input.sprint)
            return sprintNoise;

        if (controller.velocity.magnitude > 2.5f)
            return runNoise;

        return walkNoise;
    }

    void EmitNoise(float loudness)
    {
        EnemyBehaviour[] enemies = FindObjectsOfType<EnemyBehaviour>();

        foreach (EnemyBehaviour enemy in enemies)
        {
            enemy.ReceiveNoise(transform.position, loudness);
        }
    }
}