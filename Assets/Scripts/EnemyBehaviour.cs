using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Animator anim;
    public GameObject ragdoll;
    public EnemyFOV fov;
    public Transform player;

    [Header("Turning")]
    public float idleTurnSpeed = 2f;
    public float alertTurnSpeed = 6f;

    [Header("Vision")]
    public float viewDistance = 12f;
    public float viewAngle = 60f;
    public float eyeHeight = 1.5f;
    public LayerMask obstacleMask;

    [Header("Patrol")]
    public Transform[] waypoints;
    public float waitTimeAtWaypoint = 2f;
    public bool loop = true;

    [Header("Look Around")]
    public float lookAngle = 45f;
    public float lookSpeed = 1.2f;

    [Header("Investigate")]
    public float investigateTime = 4f;

    [Header("Hearing (MIMIC)")]
    public float hearingRadius = 8f;
    public float instantDetectDistance = 1.5f;

    [Header("Detection Timers")]
    public float detectTime = 0.6f;
    public float loseTime = 0.8f;

    // ── Runtime ─────────────────────────────
    bool _canSee;
    float _detectTimer;
    float _loseTimer;

    Vector3 _lastKnownPos;
    Quaternion _baseRotation;
    float _timer;
    int _wpIndex;
    int _index;

    public bool CanSeeTarget => _canSee;

    enum State
    {
        Patrolling,
        Waiting,
        Suspicious,
        Investigating,
        Searching,
        Chasing,
        Dead
    }
    State _state;

    int walkID;

    void Start()
    {
        agent ??= GetComponent<NavMeshAgent>();
        agent.updateRotation = false;

        walkID = Animator.StringToHash("Walk");

        if (fov)
        {
            fov.viewRadius = viewDistance;
            fov.viewAngle = viewAngle;
        }

        if (waypoints.Length > 0)
            agent.SetDestination(waypoints[0].position);

        ChangeState(State.Patrolling);

        if (ragdoll) ragdoll.SetActive(false);
    }

    void Update()
    {
        if (_state == State.Dead) return;

        UpdateVision();
        RunState();
        UpdateAnimation();
    }

    // ── STATE MACHINE ──────────────────────
    void RunState()
    {
        switch (_state)
        {
            case State.Patrolling: Patrol(); break;
            case State.Waiting: Wait(); break;
            case State.Suspicious: Suspicious(); break;
            case State.Investigating: Investigate(); break;
            case State.Searching: Search(); break;
            case State.Chasing: Chase(); break;
        }
    }

    void ChangeState(State next)
    {
        _state = next;
        _timer = 0f;

        agent.isStopped =
            next == State.Waiting ||
            next == State.Suspicious ||
            next == State.Searching;
    }

    // ── STATES ─────────────────────────────
    void Patrol()
{
    RotateTowardsMovement(idleTurnSpeed);

    if (_canSee)
        ChangeState(State.Suspicious);

    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
    {
        _baseRotation = transform.rotation;
        ChangeState(State.Waiting);
    }
}

    void Wait()
    {
        LookAround();

        if (_canSee)
            ChangeState(State.Suspicious);

        if (_timer >= waitTimeAtWaypoint)
        {
            GoNextWaypoint();
            ChangeState(State.Patrolling);
        }
    }

    void Suspicious()
    {
        SmoothTurnTowards(player.position, alertTurnSpeed);

        if (_detectTimer >= detectTime)
            ChangeState(State.Chasing);

        if (_loseTimer >= loseTime)
            ChangeState(State.Patrolling);
    }

    void Chase()
{
    agent.SetDestination(player.position);
    _lastKnownPos = player.position;

    RotateTowardsMovement(alertTurnSpeed);

    if (!_canSee)
        ChangeState(State.Investigating);
}

    void Investigate()
{
    agent.SetDestination(_lastKnownPos);

    RotateTowardsMovement(alertTurnSpeed);

    if (_canSee)
        ChangeState(State.Chasing);

    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
    {
        _baseRotation = transform.rotation;
        ChangeState(State.Searching);
    }
}

    void Search()
    {
        LookAround(1.4f);

        if (_canSee)
            ChangeState(State.Chasing);

        if (_timer >= investigateTime)
        {
            GoNearestWaypoint();
            ChangeState(State.Patrolling);
        }
    }

    // ── HEARING (MIMIC SYSTEM) ──────────────
    public void ReceiveNoise(Vector3 noisePos, float loudness)
    {
        if (_state == State.Dead) return;

        float dist = Vector3.Distance(transform.position, noisePos);

        if (dist > hearingRadius * loudness && dist > instantDetectDistance)
            return;

        _lastKnownPos = noisePos;

        if (_state == State.Patrolling || _state == State.Waiting)
        {
            ChangeState(State.Suspicious);
        }
        else if (_state == State.Suspicious)
        {
            ChangeState(State.Investigating);
        }
    }

    // ── VISION ─────────────────────────────
    void UpdateVision()
    {
        bool sees = CanSeePlayer();

        if (sees)
        {
            _detectTimer += Time.deltaTime;
            _loseTimer = 0f;
        }
        else
        {
            _detectTimer = 0f;
            _loseTimer += Time.deltaTime;
        }

        _canSee = sees;
        if (fov) fov.SetAlert(sees);
    }

    bool CanSeePlayer()
    {
        if (!player) return false;

        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 target = player.position + Vector3.up * eyeHeight;

        Vector3 dir = target - eye;
        float dist = dir.magnitude;

        if (dist > viewDistance) return false;
        if (Vector3.Angle(transform.forward, dir) > viewAngle * 0.5f) return false;
        if (Physics.Raycast(eye, dir.normalized, dist, obstacleMask)) return false;

        return true;
    }

    // ── HELPERS ────────────────────────────
    void SmoothTurnTowards(Vector3 worldPos, float speed)
    {
        Vector3 dir = worldPos - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            target,
            Time.deltaTime * speed
        );
    }

    void LookAround(float mult = 1f)
    {
        _timer += Time.deltaTime;
        float yaw = Mathf.Sin(_timer * lookSpeed) * lookAngle * mult;
        transform.rotation = _baseRotation * Quaternion.Euler(0, yaw, 0);
    }

    void GoNextWaypoint()
    {
        if (waypoints.Length == 0) return;

        _wpIndex++;
        if (_wpIndex >= waypoints.Length)
            _wpIndex = loop ? 0 : waypoints.Length - 1;

        agent.SetDestination(waypoints[_wpIndex].position);
    }
    void RotateTowardsMovement(float turnSpeed)
{
    if (agent.velocity.sqrMagnitude < 0.05f) return;

    Vector3 dir = agent.velocity.normalized;
    dir.y = 0f;

    Quaternion targetRot = Quaternion.LookRotation(dir);

    transform.rotation = Quaternion.Slerp(
        transform.rotation,
        targetRot,
        Time.deltaTime * turnSpeed
    );
}

    void GoNearestWaypoint()
    {
        if (waypoints.Length == 0) return;

        float min = float.MaxValue;
        for (int i = 0; i < waypoints.Length; i++)
        {
            float d = Vector3.Distance(transform.position, waypoints[i].position);
            if (d < min) { min = d; _wpIndex = i; }
        }
        agent.SetDestination(waypoints[_wpIndex].position);
    }

    void UpdateAnimation()
    {
        bool moving = agent.velocity.magnitude > 0.1f && !agent.isStopped;
        anim.SetBool(walkID, moving);
    }

    // ── Editor Gizmos ──────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        DrawWaypoints();
        if (fov != null) DrawViewCone();
        if (Application.isPlaying) DrawStateLabel();
    }

    void DrawWaypoints()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            // Sphere
            bool isCurrent = Application.isPlaying && i == _index;
            Gizmos.color   = isCurrent ? Color.yellow : Color.cyan;
            Gizmos.DrawSphere(waypoints[i].position, 0.28f);
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(waypoints[i].position, 0.28f);

            // Label
            UnityEditor.Handles.Label(
                waypoints[i].position + Vector3.up * 0.55f,
                $"  WP {i}",
                new GUIStyle { normal = { textColor = Color.white }, fontStyle = FontStyle.Bold }
            );

            // Arrow to next
            int  next       = (i + 1) % waypoints.Length;
            bool isLastStop = !loop && i == waypoints.Length - 1;
            if (waypoints[next] == null) continue;

            Gizmos.color = isLastStop
                ? new Color(1f, 0.3f, 0.3f, 0.7f)
                : new Color(0.2f, 1f, 0.75f, 0.85f);

            DrawArrow(waypoints[i].position, waypoints[next].position);
        }
    }

    void DrawArrow(Vector3 from, Vector3 to)
    {
        Gizmos.DrawLine(from, to);
        Vector3 dir = (to - from).normalized;
        Vector3 mid = Vector3.Lerp(from, to, 0.82f);
        Gizmos.DrawLine(mid, mid + Quaternion.Euler(0,  28f, 0) * (-dir) * 0.35f);
        Gizmos.DrawLine(mid, mid + Quaternion.Euler(0, -28f, 0) * (-dir) * 0.35f);
    }

    void DrawViewCone()
    {
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.12f);
        Vector3 origin = transform.position + Vector3.up * 0.05f;
        float   half   = fov.viewAngle * 0.5f;
        int     steps  = 24;
        Vector3 prev   = origin + GizmoDir(-half) * fov.viewRadius;
        Gizmos.DrawLine(origin, prev);
        for (int i = 1; i <= steps; i++)
        {
            Vector3 next = origin + GizmoDir(Mathf.Lerp(-half, half, i / (float)steps)) * fov.viewRadius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
        Gizmos.DrawLine(prev, origin);
    }

    // Floating state label above the enemy at runtime
    void DrawStateLabel()
    {
        Color labelColor = _state switch
        {
            State.Chasing       => Color.red,
            State.Investigating => new Color(1f, 0.5f, 0f),
            State.Searching     => Color.yellow,
            State.Waiting       => Color.cyan,
            _                   => Color.white
        };

        GUIStyle style = new GUIStyle
        {
            normal    = { textColor = labelColor },
            fontStyle = FontStyle.Bold,
            fontSize  = 11
        };

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2.4f,
            $"  [{_state}]",
            style
        );

        // Draw line to last known pos when investigating/searching
        if ((_state == State.Investigating || _state == State.Searching) && _lastKnownPos != Vector3.zero)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            Gizmos.DrawLine(transform.position + Vector3.up, _lastKnownPos + Vector3.up);
            Gizmos.DrawSphere(_lastKnownPos + Vector3.up * 0.1f, 0.2f);
        }
    }

    Vector3 GizmoDir(float angleDeg)
    {
        float rad = (angleDeg + transform.eulerAngles.y) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
    }
#endif
}