using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PrisonerAI : MonoBehaviour
{
    private enum State
    {
        EnterQueue,
        MoveToQueueSlot,
        WaitInQueue,
        MoveToMoneyDrop,
        MoveToPrisonGate,
        MoveToPrisonInside,
        PrisonIdle,
        Finished
    }

    [Header("References")]
    [SerializeField] private PrisonerQueue queue;
    [SerializeField] private Transform prisonGatePosition;
    [SerializeField] private Transform prisonInsidePosition;
    [SerializeField] private Transform moneyDropPosition;
    [SerializeField] private ItemStorage moneyStorage;
    [SerializeField] private Carriable moneyPrefab;

    [Header("Visual")]
    [SerializeField] private GameObject[] normalModels;
    [SerializeField] private GameObject prisonerModel;
    [SerializeField] private Transform handoffTarget;

    [Header("Gameplay")]
    [SerializeField] private int handcuffDemand = 4;
    [SerializeField] private int moneyReward = 1;

    [Header("Money Fly")]
    [SerializeField] private float moneyFlyDuration = 0.3f;
    [SerializeField] private AnimationCurve moneyFlyHeightCurve;

    [Header("Prison Idle")]
    [SerializeField] private bool enableCollisionWhenPrisoned = true;

    private NPCMovement movement;
    private NavMeshAgent agent;
    private Rigidbody[] rigidbodies;
    private Collider[] colliders;
    private PrisonerGenerator ownerGenerator;

    private State state = State.EnterQueue;

    private Transform currentQueueSlot;
    private int currentQueueSlotIndex = -1;
    private bool registeredToQueue = false;

    private int currentHandcuffCount = 0;
    private bool convertedToPrisoner = false;
    private bool isDepositingMoney = false;

    private bool generatorReleased = false;

    public bool NeedsHandcuff => currentHandcuffCount < handcuffDemand;
    public bool IsReadyForHandoff => state == State.WaitInQueue && queue != null && queue.IsFront(this);
    public int CurrentHandcuffCount => currentHandcuffCount;
    public int HandcuffDemand => handcuffDemand;
    public Transform HandoffTarget => handoffTarget != null ? handoffTarget : transform;

    private void Awake()
    {
        movement = GetComponent<NPCMovement>();
        agent = GetComponent<NavMeshAgent>();
        rigidbodies = GetComponentsInChildren<Rigidbody>(true);
        colliders = GetComponentsInChildren<Collider>(true);

        ApplyRandomNormalVisual();
        SetPrisonCollisionActive(false);
    }

    private void Update()
    {
        Think();
    }

    public void Initialize(
        PrisonerQueue targetQueue,
        Transform gatePosition,
        Transform prisonPosition,
        Transform moneyDrop,
        ItemStorage targetMoneyStorage,
        Carriable targetMoneyPrefab,
        PrisonerGenerator generator)
    {
        queue = targetQueue;
        prisonGatePosition = gatePosition;
        prisonInsidePosition = prisonPosition;
        moneyDropPosition = moneyDrop;
        moneyStorage = targetMoneyStorage;
        moneyPrefab = targetMoneyPrefab;
        ownerGenerator = generator;
    }

    public void AssignQueueSlot(int slotIndex, Transform slot)
    {
        currentQueueSlotIndex = slotIndex;
        currentQueueSlot = slot;

        if (currentQueueSlot == null || movement == null)
            return;

        if (state == State.EnterQueue || state == State.MoveToQueueSlot || state == State.WaitInQueue)
        {
            movement.SetDestination(currentQueueSlot.position);
            state = State.MoveToQueueSlot;
        }
    }

    public bool CanReceiveHandcuff()
    {
        if (!NeedsHandcuff)
            return false;

        if (state != State.WaitInQueue)
            return false;

        if (queue == null)
            return false;

        return queue.IsFront(this);
    }

    public void ReceiveHandcuff()
    {
        if (!CanReceiveHandcuff())
            return;

        currentHandcuffCount++;

        if (currentHandcuffCount >= handcuffDemand)
        {
            LeaveQueueImmediately();
            ReleaseGeneratorSlot();

            BecomePrisoner();
            GoToNextProcessedStep();
        }
    }

    private void ReleaseGeneratorSlot()
    {
        if (generatorReleased)
            return;

        generatorReleased = true;

        if (ownerGenerator != null)
            ownerGenerator.NotifyPrisonerRemoved();
    }

    private void Think()
    {
        switch (state)
        {
            case State.EnterQueue:
                UpdateEnterQueue();
                break;

            case State.MoveToQueueSlot:
                UpdateMoveToQueueSlot();
                break;

            case State.WaitInQueue:
                UpdateWaitInQueue();
                break;

            case State.MoveToMoneyDrop:
                UpdateMoveToMoneyDrop();
                break;

            case State.MoveToPrisonGate:
                UpdateMoveToPrisonGate();
                break;

            case State.MoveToPrisonInside:
                UpdateMoveToPrisonInside();
                break;

            case State.PrisonIdle:
            case State.Finished:
                break;
        }
    }

    private void UpdateEnterQueue()
    {
        if (queue == null)
            return;

        if (registeredToQueue)
        {
            if (currentQueueSlot != null)
            {
                movement.SetDestination(currentQueueSlot.position);
                state = State.MoveToQueueSlot;
            }
            return;
        }

        registeredToQueue = queue.TryRegister(this);

        if (registeredToQueue && currentQueueSlot != null)
        {
            movement.SetDestination(currentQueueSlot.position);
            state = State.MoveToQueueSlot;
        }
    }

    private void UpdateMoveToQueueSlot()
    {
        if (currentQueueSlot == null)
            return;

        if (!movement.HasReachedDestination())
            return;

        // 회전 강제 없음. 도착하면 바로 대기.
        state = State.WaitInQueue;
    }

    private void UpdateWaitInQueue()
    {
        if (currentQueueSlot == null)
            return;

        // 앞사람이 빠져 슬롯 재배정되면 바로 이동
        if (Vector3.Distance(transform.position, currentQueueSlot.position) > 0.2f)
        {
            movement.SetDestination(currentQueueSlot.position);
            state = State.MoveToQueueSlot;
            return;
        }
    }

    private void UpdateMoveToMoneyDrop()
    {
        if (moneyDropPosition == null)
        {
            GoToPrisonGate();
            return;
        }

        if (!movement.HasReachedDestination())
            return;

        if (isDepositingMoney)
            return;

        StartCoroutine(DepositMoneySequence());
    }

    private IEnumerator DepositMoneySequence()
    {
        isDepositingMoney = true;

        yield return StartCoroutine(FlyMoneyToStorage());

        isDepositingMoney = false;
        GoToPrisonGate();
    }

    private IEnumerator FlyMoneyToStorage()
    {
        if (moneyStorage == null || moneyPrefab == null)
            yield break;

        for (int i = 0; i < moneyReward; i++)
        {
            if (moneyStorage.IsFull)
                yield break;

            Carriable money = Instantiate(
                moneyPrefab,
                moneyDropPosition != null ? moneyDropPosition.position : transform.position,
                Quaternion.identity
            );

            Transform moneyTf = money.transform;
            Vector3 startPos = moneyTf.position;
            Quaternion startRot = moneyTf.rotation;
            Vector3 targetPos = moneyStorage.GetNextWorldPosition();
            Quaternion targetRot = moneyStorage.GetStoredWorldRotation();

            moneyTf.SetParent(null);

            Rigidbody rb = money.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            Collider[] moneyColliders = money.GetComponentsInChildren<Collider>();
            for (int c = 0; c < moneyColliders.Length; c++)
            {
                moneyColliders[c].enabled = false;
            }

            float elapsed = 0f;
            while (elapsed < moneyFlyDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moneyFlyDuration);

                Vector3 pos = Vector3.Lerp(startPos, targetPos, t);

                float arc = 0f;
                if (moneyFlyHeightCurve != null && moneyFlyHeightCurve.length > 0)
                    arc = moneyFlyHeightCurve.Evaluate(t);

                pos.y += arc;
                moneyTf.position = pos;
                moneyTf.rotation = Quaternion.Slerp(startRot, targetRot, t);

                yield return null;
            }

            moneyTf.position = targetPos;
            moneyTf.rotation = targetRot;
            moneyStorage.TryStore(money);

            yield return null;
        }
    }

    private void UpdateMoveToPrisonGate()
    {
        if (prisonGatePosition == null)
        {
            EnterPrisonIdleState();
            return;
        }

        if (!movement.HasReachedDestination())
            return;

        if (prisonInsidePosition == null)
        {
            EnterPrisonIdleState();
            return;
        }

        movement.SetDestination(prisonInsidePosition.position);
        state = State.MoveToPrisonInside;
    }

    private void UpdateMoveToPrisonInside()
    {
        if (prisonInsidePosition == null)
        {
            EnterPrisonIdleState();
            return;
        }

        if (!movement.HasReachedDestination())
            return;

        EnterPrisonIdleState();
    }

    private void ApplyRandomNormalVisual()
    {
        if (normalModels != null)
        {
            for (int i = 0; i < normalModels.Length; i++)
            {
                if (normalModels[i] != null)
                    normalModels[i].SetActive(false);
            }
        }

        if (prisonerModel != null)
            prisonerModel.SetActive(false);

        if (normalModels == null || normalModels.Length == 0)
            return;

        int picked = Random.Range(0, normalModels.Length);
        if (normalModels[picked] != null)
            normalModels[picked].SetActive(true);
    }

    private void BecomePrisoner()
    {
        if (convertedToPrisoner)
            return;

        convertedToPrisoner = true;

        if (normalModels != null)
        {
            for (int i = 0; i < normalModels.Length; i++)
            {
                if (normalModels[i] != null)
                    normalModels[i].SetActive(false);
            }
        }

        if (prisonerModel != null)
            prisonerModel.SetActive(true);
    }

    private void LeaveQueueImmediately()
    {
        if (queue != null && registeredToQueue)
        {
            queue.Remove(this);
            registeredToQueue = false;
        }

        currentQueueSlot = null;
        currentQueueSlotIndex = -1;
    }

    private void GoToNextProcessedStep()
    {
        if (moneyDropPosition != null)
        {
            movement.SetDestination(moneyDropPosition.position);
            state = State.MoveToMoneyDrop;
        }
        else
        {
            GoToPrisonGate();
        }
    }

    private void GoToPrisonGate()
    {
        if (prisonGatePosition != null)
        {
            movement.SetDestination(prisonGatePosition.position);
            state = State.MoveToPrisonGate;
        }
        else
        {
            EnterPrisonIdleState();
        }
    }

    private void EnterPrisonIdleState()
    {
        if (state == State.PrisonIdle)
            return;

        state = State.PrisonIdle;

        if (movement != null)
            movement.Stop();

        if (agent != null)
        {
            agent.ResetPath();
            agent.enabled = false;
        }

        if (movement != null)
            movement.enabled = false;

        SetPrisonCollisionActive(enableCollisionWhenPrisoned);

        enabled = false;
    }

    private void SetPrisonCollisionActive(bool active)
    {
        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                if (col == null)
                    continue;

                if (col.isTrigger)
                    continue;

                col.enabled = active;
            }
        }

        if (rigidbodies != null)
        {
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                Rigidbody rb = rigidbodies[i];
                if (rb == null)
                    continue;

                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = !active;
            }
        }
    }

    private void OnDestroy()
    {
        if (queue != null && registeredToQueue)
        {
            queue.Remove(this);
        }

        if (!generatorReleased && ownerGenerator != null)
        {
            ownerGenerator.NotifyPrisonerRemoved();
            generatorReleased = true;
        }
    }
}