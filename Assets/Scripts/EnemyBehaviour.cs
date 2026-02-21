using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Animator     anim;
    public GameObject   ragdoll;
    public EnemyFOV     fov;

    [Header("Vision")]
    public Transform player;
    public float viewDistance = 12f;
    public float viewAngle    = 60f;
    public float eyeHeight    = 1.5f;
    public LayerMask obstacleMask;

    [Header("Patrol")]
    public Transform[] waypoints;
    public bool  loop               = true;
    public float waitTimeAtWaypoint = 2f;

    [Header("Look Around (Waiting)")]
    public float lookAngle = 45f;
    public float lookSpeed = 1.2f;

    [Header("Chase / Investigate")]
    public float investigateTime = 4f;   // how long enemy searches last known pos before giving up

    // ── Private ────────────────────────────────────────────────────
    private int        _index;
    private float      _timer;
    private Vector3    _lastKnownPos;
    private Quaternion _baseRotation;
    private bool       _canSee;

    public bool isDead { get; private set; }

    private enum State
    {
        Patrolling,     // walking between waypoints
        Waiting,        // standing at a waypoint, looking around
        Chasing,        // actively following player
        Investigating,  // walking to last known position
        Searching,      // standing at last known pos, looking around before giving up
        Dead
    }
    private State _state;

    // ──────────────────────────────────────────────────────────────
    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (fov    == null) fov  = GetComponentInChildren<EnemyFOV>();

        _state = State.Patrolling;

        if (waypoints.Length > 0)
            agent.SetDestination(waypoints[0].position);

        if (ragdoll) ragdoll.SetActive(false);
    }

    void Update()
    {
        if (_state == State.Dead) return;

        _canSee = CanSeePlayer();
        if (fov != null) fov.SetAlert(_canSee);

        switch (_state)
        {
            case State.Patrolling:    OnPatrol();    break;
            case State.Waiting:       OnWait();      break;
            case State.Chasing:       OnChase();     break;
            case State.Investigating: OnInvestigate(); break;
            case State.Searching:     OnSearch();    break;
        }
    }

    // ── PATROL ────────────────────────────────────────────────────
    // Walk between waypoints in order. Spot player → Chase.
    void OnPatrol()
    {
        anim.SetBool("Walk", true);

        if (_canSee) { EnterChase(); return; }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.isStopped = true;
            _timer          = 0f;
            _baseRotation   = transform.rotation;
            _state          = State.Waiting;
        }
    }

    // ── WAIT ──────────────────────────────────────────────────────
    // Stand at waypoint, look left/right. Spot player → Chase.
    void OnWait()
    {
        anim.SetBool("Walk", false);
        _timer += Time.deltaTime;

        // Sine-wave head turn
        float yaw          = Mathf.Sin(_timer * lookSpeed) * lookAngle;
        transform.rotation = _baseRotation * Quaternion.Euler(0f, yaw, 0f);

        if (_canSee) { EnterChase(); return; }

        if (_timer >= waitTimeAtWaypoint)
        {
            transform.rotation = _baseRotation;
            agent.isStopped    = false;
            GoToNextWaypoint();
            _state = State.Patrolling;
        }
    }

    // ── CHASE ─────────────────────────────────────────────────────
    // Follow the player. Lose sight → Investigate last known position.
    void OnChase()
    {
        anim.SetBool("Walk", true);
        agent.isStopped = false;
        agent.SetDestination(player.position);
        _lastKnownPos = player.position;

        if (!_canSee)
        {
            // Lost the player — go investigate where we last saw them
            agent.SetDestination(_lastKnownPos);
            _timer = 0f;
            _state = State.Investigating;
        }
    }

    // ── INVESTIGATE ───────────────────────────────────────────────
    // Walk to last known position. Spot player again → Chase.
    // Arrive at destination → Search (look around briefly).
    void OnInvestigate()
    {
        anim.SetBool("Walk", true);
        agent.isStopped = false;

        if (_canSee) { EnterChase(); return; }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            _timer        = 0f;
            _baseRotation = transform.rotation;
            agent.isStopped = true;
            _state        = State.Searching;
        }
    }

    // ── SEARCH ────────────────────────────────────────────────────
    // Stand at last known position, look around. Spot player → Chase.
    // Timer expires → resume patrol.
    void OnSearch()
    {
        anim.SetBool("Walk", false);
        _timer += Time.deltaTime;

        // Look around while searching
        float yaw          = Mathf.Sin(_timer * lookSpeed * 0.7f) * lookAngle * 1.4f;
        transform.rotation = _baseRotation * Quaternion.Euler(0f, yaw, 0f);

        if (_canSee) { EnterChase(); return; }

        if (_timer >= investigateTime)
        {
            transform.rotation = _baseRotation;
            agent.isStopped    = false;
            GoToNearestWaypoint();
            _state = State.Patrolling;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────
    void EnterChase()
    {
        _lastKnownPos   = player.position;
        agent.isStopped = false;
        _state          = State.Chasing;
    }

    // ── Vision ─────────────────────────────────────────────────────
    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 toPlayer = player.position - transform.position;
        float   dist     = toPlayer.magnitude;

        if (dist > viewDistance) return false;
        if (Vector3.Angle(transform.forward, toPlayer) > viewAngle * 0.5f) return false;

        // Eye-to-eye raycast — obstacleMask must NOT include the player's layer
        Vector3 eyeOrigin = transform.position + Vector3.up * eyeHeight;
        Vector3 eyeTarget = player.position    + Vector3.up * eyeHeight;
        float   eyeDist   = Vector3.Distance(eyeOrigin, eyeTarget);

        if (Physics.Raycast(eyeOrigin, (eyeTarget - eyeOrigin).normalized, eyeDist, obstacleMask))
            return false;

        return true;
    }

   
  
    // ── Waypoint helpers ───────────────────────────────────────────
    void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;
        _index = (_index + 1) % (loop ? waypoints.Length : Mathf.Max(waypoints.Length, 1));
        if (!loop) _index = Mathf.Min(_index, waypoints.Length - 1);
        agent.SetDestination(waypoints[_index].position);
    }

    void GoToNearestWaypoint()
    {
        if (waypoints.Length == 0) return;
        float min = Mathf.Infinity; int nearest = 0;
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