using UnityEngine;

[RequireComponent(typeof(NPCMovement))]
public class MiningWorkerAI : MonoBehaviour
{
    private enum State
    {
        SearchOre,
        MoveToOre,
        Mine
    }

    [Header("References")]
    [SerializeField] private MiningArea miningArea;
    [SerializeField] private ItemStorage targetOreStorage;
    [SerializeField] private Transform mineOrigin;
    [SerializeField] private Animator animator;

    [Header("Mining")]
    [SerializeField] private float searchRange = 100f;
    [SerializeField] private float mineRange = 1.15f;
    [SerializeField] private float mineInterval = 0.5f;
    [SerializeField] private int damage = 1;

    [Header("Movement")]
    [SerializeField] private float repathInterval = 0.2f;

    private NPCMovement movement;
    private State state = State.SearchOre;
    private MineableOre currentTarget;
    private float mineTimer;
    private float repathTimer;

    private void Awake()
    {
        movement = GetComponent<NPCMovement>();
    }

    private void Update()
    {
        switch (state)
        {
            case State.SearchOre:
                UpdateSearchOre();
                break;

            case State.MoveToOre:
                UpdateMoveToOre();
                break;

            case State.Mine:
                UpdateMine();
                break;
        }
    }

    private void UpdateSearchOre()
    {
        if (miningArea == null)
            return;

        currentTarget = miningArea.FindBestOreInRange(transform.position, searchRange);

        if (currentTarget == null)
            return;

        state = State.MoveToOre;
        repathTimer = 0f;
    }

    private void UpdateMoveToOre()
    {
        if (currentTarget == null || !currentTarget.IsAvailable)
        {
            currentTarget = null;
            state = State.SearchOre;
            return;
        }

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = repathInterval;
            movement.SetDestination(currentTarget.transform.position);
        }

        Vector3 origin = mineOrigin != null ? mineOrigin.position : transform.position;
        float distSq = (currentTarget.transform.position - origin).sqrMagnitude;

        if (distSq <= mineRange * mineRange)
        {
            movement.Stop();
            mineTimer = 0f;
            state = State.Mine;
        }
    }

    private void UpdateMine()
    {
        if (currentTarget == null || !currentTarget.IsAvailable)
        {
            currentTarget = null;
            state = State.SearchOre;
            return;
        }

        if (targetOreStorage == null || targetOreStorage.IsFull)
            return;

        Vector3 origin = mineOrigin != null ? mineOrigin.position : transform.position;
        float distSq = (currentTarget.transform.position - origin).sqrMagnitude;

        if (distSq > mineRange * mineRange)
        {
            state = State.MoveToOre;
            return;
        }

        mineTimer -= Time.deltaTime;
        if (mineTimer > 0f)
            return;

        mineTimer = mineInterval;

        animator?.SetTrigger("Mine");
        currentTarget.TakeMineDamageToStorage(damage, targetOreStorage);
    }
}