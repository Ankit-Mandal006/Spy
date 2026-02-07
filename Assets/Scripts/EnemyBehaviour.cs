using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Animator anim;
    public AudioSource audioSource; // Optional: play footstep sounds

    [Header("Footstep")]
    public AudioClip[] footstepClips;
    [Range(0f, 1f)] public float footstepVolume = 0.5f;

    [Header("Patrol")]
    public Transform[] waypoints;
    public bool loop = true;
    public float waitTimeAtWaypoint = 2f;

    [Header("Vision")]
    public Transform player;
    public float viewDistance = 12f;
    public float viewAngle = 60f;
    public LayerMask obstacleMask;

    [Header("Suspicion")]
    public float suspicionTime = 2f;
    public float searchTime = 4f;

    [Header("State")]
    public bool isDead = false;

    private int index = 0;
    private float timer = 0f;
    private Vector3 lastKnownPlayerPos;

    private enum State
    {
        Patrolling,
        Waiting,
        Suspicious,
        Investigating,
        Chasing,
        Searching,
        Dead
    }

    private State currentState;

    void Start()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        currentState = State.Patrolling;

        if (waypoints.Length > 0)
            agent.SetDestination(waypoints[0].position);
    }

    void Update()
    {
        if (isDead)
        {
            HandleDeath();
            //UpdateAnimator(0f);
            return;
        }

        //UpdateAnimator();

        switch (currentState)
        {
            case State.Patrolling:
                Patrol();
                LookForPlayer();
                break;
            case State.Waiting:
                WaitAtWaypoint();
                LookForPlayer();
                break;
            case State.Suspicious:
                Suspicious();
                break;
            case State.Investigating:
                Investigate();
                break;
            case State.Chasing:
                Chase();
                break;
            case State.Searching:
                Search();
                break;
        }
    }

    // ---------------- STATES ----------------
    void Patrol()
    {
        anim.SetBool("Walk", true);
        if (agent == null || !agent.enabled) return;
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.isStopped = true;
            timer = 0f;
            currentState = State.Waiting;
        }
    }

    void WaitAtWaypoint()
    {
        if (agent == null || !agent.enabled) return;
        anim.SetBool("Walk", false);
        timer += Time.deltaTime;

        if (timer >= waitTimeAtWaypoint)
        {
            GoToNextWaypoint();
            agent.isStopped = false;
            currentState = State.Patrolling;
        }
    }

    void Suspicious()
    {
        timer += Time.deltaTime;

        if (CanSeePlayer())
        {
            currentState = State.Chasing;
            return;
        }

        if (timer >= suspicionTime)
        {
            if (agent != null && agent.enabled)
                agent.SetDestination(lastKnownPlayerPos);
            currentState = State.Investigating;
        }
    }

    void Investigate()
    {
        if (agent == null || !agent.enabled) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            timer = 0f;
            currentState = State.Searching;
        }

        if (CanSeePlayer())
            currentState = State.Chasing;
    }

    void Chase()
    {
        if (player == null || agent == null || !agent.enabled) return;
        anim.SetBool("Walk", true);
        agent.SetDestination(player.position);
        lastKnownPlayerPos = player.position;

        if (!CanSeePlayer())
        {
            timer = 0f;
            currentState = State.Searching;
        }
    }

    void Search()
    {
        if (agent == null || !agent.enabled) return;

        timer += Time.deltaTime;

        if (CanSeePlayer())
        {
            currentState = State.Chasing;
            return;
        }

        if (timer >= searchTime)
        {
            GoToNearestWaypoint();
            currentState = State.Patrolling;
        }
    }

    // ---------------- VISION ----------------
    void LookForPlayer()
    {
        if (CanSeePlayer())
        {
            lastKnownPlayerPos = player.position;
            timer = 0f;
            currentState = State.Suspicious;
        }
    }

    bool CanSeePlayer()
    {
        if (!player) return false;

        Vector3 dir = (player.position - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > viewDistance) return false;
        if (Vector3.Angle(transform.forward, dir) > viewAngle * 0.5f) return false;

        if (Physics.Raycast(transform.position + Vector3.up, dir, dist, obstacleMask))
            return false;

        return true;
    }

    // ---------------- HELPERS ----------------
    void GoToNextWaypoint()
    {
        if (waypoints.Length == 0 || agent == null || !agent.enabled) return;

        index++;
        if (index >= waypoints.Length)
            index = loop ? 0 : waypoints.Length - 1;

        agent.SetDestination(waypoints[index].position);
    }

    void GoToNearestWaypoint()
    {
        if (waypoints.Length == 0 || agent == null || !agent.enabled) return;

        float min = Mathf.Infinity;
        int nearest = 0;

        for (int i = 0; i < waypoints.Length; i++)
        {
            float d = Vector3.Distance(transform.position, waypoints[i].position);
            if (d < min)
            {
                min = d;
                nearest = i;
            }
        }

        index = nearest;
        agent.SetDestination(waypoints[index].position);
    }

    // ---------------- ANIMATOR ----------------
    void UpdateAnimator()
    {
        if (!anim || !agent) return;

        //float speed = agent.velocity.magnitude;
        //float normalizedSpeed = agent.speed > 0 ? speed / agent.speed : 0f;
        anim.SetFloat("Speed", 1f);
    }

    void UpdateAnimator(float speed)
    {
        if (!anim) return;
        anim.SetFloat("Speed", speed);
    }

    // ---------------- DEATH ----------------
    public void KillEnemy()
    {
        isDead = true;
        currentState = State.Dead;
        HandleDeath();
    }

    public void HandleDeath()
    {
        if (!agent || !agent.enabled) return;

        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;

        agent.updatePosition = false;
        agent.updateRotation = false;
        // Optional: agent.enabled = false;
    }

    // ---------------- GIZMOS ----------------
void OnDrawGizmos()
{
    if (waypoints == null || waypoints.Length == 0) return;

    Gizmos.color = Color.cyan;

    // Draw lines between waypoints
    for (int i = 0; i < waypoints.Length - 1; i++)
    {
        if (waypoints[i] != null && waypoints[i + 1] != null)
        {
            Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
    }

    // If loop, draw line from last to first
    if (loop && waypoints.Length > 1 && waypoints[0] != null && waypoints[^1] != null)
    {
        Gizmos.DrawLine(waypoints[^1].position, waypoints[0].position);
    }

    // Draw spheres at each waypoint
    Gizmos.color = Color.yellow;
    foreach (var wp in waypoints)
    {
        if (wp != null)
            Gizmos.DrawSphere(wp.position, 0.3f);
    }
}


    // ---------------- FOOTSTEP EVENT ----------------
    /*public void OnFootstep()
    {
        // This prevents the AnimationEvent warning
        if (audioSource != null && footstepClips.Length > 0)
        {
            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            audioSource.PlayOneShot(clip, footstepVolume);
        }
    }*/
    
}
