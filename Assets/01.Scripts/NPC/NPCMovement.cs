using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        RefreshAnimator();
    }

    private void Update()
    {
        if (animator != null)
        {
            if (agent.remainingDistance <= float.Epsilon)
            {
                animator.SetFloat("Velocity", 0);
            }
            else
            {
                animator.SetFloat("Velocity", 1);
            }
        }
    }

    public void SetDestination(Vector3 position)
    {
        int groundMask = 1 << LayerMask.NameToLayer("Ground");

        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 2.0f, groundMask))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.SetDestination(position);
        }
    }

    public bool HasReachedDestination(float threshold = 0.2f)
    {
        if (agent == null)
            return false;

        if (agent.pathPending)
            return false;

        if (agent.remainingDistance > threshold)
            return false;

        return !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f;
    }

    public void Stop()
    {
        if (agent == null)
            return;

        agent.ResetPath();
    }

    public void RefreshAnimator()
    {
        animator = GetComponentInChildren<Animator>();
    }
}