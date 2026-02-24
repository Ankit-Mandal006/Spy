using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : MonoBehaviour
{
     [Header("References")]
    public NavMeshAgent agent;
    public Animator anim;
    public GameObject ragdoll;
    public EnemyFOV fov;

    [Header("Vision (SOURCE OF TRUTH)")]
    public Transform player;
    public float viewDistance = 12f;
    public float viewAngle = 60f;
    public float eyeHeight = 1.5f;
    public LayerMask obstacleMask;

    [Header("Patrol")]
    public Transform[] waypoints;
    public bool loop = true;
    public float waitTimeAtWaypoint = 2f;

    [Header("Look Around")]
    public float lookAngle = 45f;
    public float lookSpeed = 1.2f;

    [Header("Investigate")]
    public float investigateTime = 4f;

    // ── Runtime ─────────────────────────────
    bool _canSee;
    public bool CanSeeTarget => _canSee;

    Vector3 _lastKnownPos;
    Quaternion _baseRotation;
    float _timer;
    int _index;

    enum State { Patrolling, Waiting, Chasing, Investigating, Searching, Dead }
    State _state;

    void Start()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!fov) fov = GetComponentInChildren<EnemyFOV>();

        // 🔗 SYNC FOV VISUAL WITH LOGIC
        if (fov)
        {
            fov.viewRadius = viewDistance;
            fov.viewAngle = viewAngle;
        }

        _state = State.Patrolling;

        if (waypoints.Length > 0)
            agent.SetDestination(waypoints[0].position);

        if (ragdoll) ragdoll.SetActive(false);
    }

    void Update()
    {
        if (_state == State.Dead) return;

        _canSee = CanSeePlayer();
        if (fov) fov.SetAlert(_canSee);

        switch (_state)
        {
            case State.Patrolling: OnPatrol(); break;
            case State.Waiting: OnWait(); break;
            case State.Chasing: OnChase(); break;
            case State.Investigating: OnInvestigate(); break;
            case State.Searching: OnSearch(); break;
        }
    }

    // ── STATES ──────────────────────────────
    void OnPatrol()
    {
        anim.SetBool("Walk", true);

        if (_canSee) EnterChase();

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.isStopped = true;
            _timer = 0f;
            _baseRotation = transform.rotation;
            _state = State.Waiting;
        }
    }

    void OnWait()
    {
        anim.SetBool("Walk", false);
        _timer += Time.deltaTime;

        float yaw = Mathf.Sin(_timer * lookSpeed) * lookAngle;
        transform.rotation = _baseRotation * Quaternion.Euler(0, yaw, 0);

        if (_canSee) EnterChase();

        if (_timer >= waitTimeAtWaypoint)
        {
            transform.rotation = _baseRotation;
            agent.isStopped = false;
            GoToNextWaypoint();
            _state = State.Patrolling;
        }
    }

    void OnChase()
    {
        anim.SetBool("Walk", true);
        agent.isStopped = false;
        agent.SetDestination(player.position);
        _lastKnownPos = player.position;

        if (!_canSee)
        {
            agent.SetDestination(_lastKnownPos);
            _timer = 0f;
            _state = State.Investigating;
        }
    }

    void OnInvestigate()
    {
        anim.SetBool("Walk", true);

        if (_canSee) EnterChase();

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.isStopped = true;
            _timer = 0f;
            _baseRotation = transform.rotation;
            _state = State.Searching;
        }
    }

    void OnSearch()
    {
        anim.SetBool("Walk", false);
        _timer += Time.deltaTime;

        float yaw = Mathf.Sin(_timer * lookSpeed * 0.7f) * lookAngle * 1.4f;
        transform.rotation = _baseRotation * Quaternion.Euler(0, yaw, 0);

        if (_canSee) EnterChase();

        if (_timer >= investigateTime)
        {
            transform.rotation = _baseRotation;
            agent.isStopped = false;
            GoToNearestWaypoint();
            _state = State.Patrolling;
        }
    }

    void EnterChase()
    {
        _lastKnownPos = player.position;
        agent.isStopped = false;
        _state = State.Chasing;
    }

    // ── VISION ──────────────────────────────
    bool CanSeePlayer()
    {
        if (!player) return false;

        Vector3 eyeOrigin = transform.position + Vector3.up * eyeHeight;
        Vector3 eyeTarget = player.position + Vector3.up * eyeHeight;

        float dist = Vector3.Distance(eyeOrigin, eyeTarget);
        if (dist > viewDistance) return false;

        Vector3 dir = (eyeTarget - eyeOrigin).normalized;
        if (Vector3.Angle(transform.forward, dir) > viewAngle * 0.5f) return false;

        if (Physics.Raycast(eyeOrigin, dir, dist, obstacleMask))
            return false;

        return true;
    }

    // ── WAYPOINTS ───────────────────────────
    void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;
        _index = (_index + 1) % waypoints.Length;
        agent.SetDestination(waypoints[_index].position);
    }

    void GoToNearestWaypoint()
    {
        if (waypoints.Length == 0) return;

        float min = Mathf.Infinity;
        int nearest = 0;

        for (int i = 0; i < waypoints.Length; i++)
        {
            float d = Vector3.Distance(transform.position, waypoints[i].position);
            if (d < min) { min = d; nearest = i; }
        }

        _index = nearest;
        agent.SetDestination(waypoints[_index].position);
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