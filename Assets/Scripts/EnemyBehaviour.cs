using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Animator anim;
    public GameObject ragdoll;

    [Header("Vision")]
    public Transform player;
    public float viewDistance = 12f;
    public float viewAngle = 60f;
    public LayerMask obstacleMask;

    [Header("Patrol")]
    public Transform[] waypoints;
    public bool loop = true;
    public float waitTimeAtWaypoint = 2f;

    private int index;
    private float timer;
    private Vector3 lastKnownPlayerPos;

    public bool isDead { get; private set; }

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
        agent = GetComponent<NavMeshAgent>();
        currentState = State.Patrolling;

        if (waypoints.Length > 0)
            agent.SetDestination(waypoints[0].position);

        if (ragdoll)
            ragdoll.SetActive(false);
    }

    void Update()
    {
        if (currentState == State.Dead)
            return;

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


    public void Die()
    {
        if (isDead) return;

        isDead = true;
        currentState = State.Dead;

        // STOP NAVMESH MOVEMENT BUT KEEP IT ENABLED
        agent.isStopped = true;
        agent.ResetPath();

        // Freeze navmesh updates (no more movement)
        agent.updatePosition = false;
        agent.updateRotation = false;

        // Optional safety snap
        transform.position = agent.nextPosition;

        // Play death animation
        anim.SetTrigger("isDead");

        // Switch to ragdoll after animation
        Invoke(nameof(ActivateRagdoll), 2.25f);
    }


    void ActivateRagdoll()
    {
        anim.enabled = false;

        // Detach ragdoll
        ragdoll.transform.SetParent(null);
        ragdoll.transform.position = transform.position;
        ragdoll.transform.rotation = transform.rotation;
        ragdoll.SetActive(true);

        // Reset ragdoll physics
        foreach (Rigidbody rb in ragdoll.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Disable ONLY non-ragdoll colliders
        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            if (!c.transform.IsChildOf(ragdoll.transform))
                c.enabled = false;
        }
    }


    void Patrol()
    {
        anim.SetBool("Walk", true);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.isStopped = true;
            timer = 0f;
            currentState = State.Waiting;
        }
    }

    void WaitAtWaypoint()
    {
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

        if (timer >= 2f)
        {
            agent.SetDestination(lastKnownPlayerPos);
            currentState = State.Investigating;
        }
    }

    void Investigate()
    {
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
        timer += Time.deltaTime;

        if (CanSeePlayer())
        {
            currentState = State.Chasing;
            return;
        }

        if (timer >= 4f)
        {
            GoToNearestWaypoint();
            currentState = State.Patrolling;
        }
    }


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
        Vector3 dir = (player.position - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > viewDistance) return false;
        if (Vector3.Angle(transform.forward, dir) > viewAngle * 0.5f) return false;

        if (Physics.Raycast(transform.position + Vector3.up, dir, dist, obstacleMask))
            return false;

        return true;
    }


    void GoToNextWaypoint()
    {
        index++;
        if (index >= waypoints.Length)
            index = loop ? 0 : waypoints.Length - 1;

        agent.SetDestination(waypoints[index].position);
    }

    void GoToNearestWaypoint()
    {
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
}
