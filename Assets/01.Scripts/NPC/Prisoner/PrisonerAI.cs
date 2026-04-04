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
        MoveToPrisonFront,
        WaitAtPrisonFront,
        MoveToPrisonInside,
        PrisonIdle
    }

    [Header("References")]
    [SerializeField] private PrisonerQueue queue;
    [SerializeField] private PrisonManager prisonManager;
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

    private NPCMovement movement;
    private NavMeshAgent agent;
    private Rigidbody[] rigidbodies;
    private PrisonerGenerator ownerGenerator;

    private State state = State.EnterQueue;

    private Transform currentQueueSlot;
    private bool registeredToQueue = false;

    private int currentHandcuffCount = 0;
    private bool convertedToPrisoner = false;
    private bool isDepositingMoney = false;
    private bool generatorReleased = false;

    private Transform assignedPrisonInsideSlot;
    private int assignedInsideSlotIndex = -1;

    public bool NeedsHandcuff => currentHandcuffCount < handcuffDemand;
    public bool IsReadyForHandoff => state == State.WaitInQueue && queue != null && queue.IsFront(this);
    public Transform HandoffTarget => handoffTarget != null ? handoffTarget : transform;
    public int AssignedInsideSlotIndex => assignedInsideSlotIndex;

    private void Awake()
    {
        movement = GetComponent<NPCMovement>();
        agent = GetComponent<NavMeshAgent>();
        rigidbodies = GetComponentsInChildren<Rigidbody>(true);

        ApplyRandomNormalVisual();
        SetPrisonRigidbodiesActive(false);
    }

    private void Update()
    {
        Think();
    }

    public void Initialize(
        PrisonerQueue targetQueue,
        PrisonManager targetPrisonManager,
        Transform moneyDrop,
        ItemStorage targetMoneyStorage,
        Carriable targetMoneyPrefab,
        PrisonerGenerator generator)
    {
        queue = targetQueue;
        prisonManager = targetPrisonManager;
        moneyDropPosition = moneyDrop;
        moneyStorage = targetMoneyStorage;
        moneyPrefab = targetMoneyPrefab;
        ownerGenerator = generator;
    }

    public void AssignQueueSlot(int slotIndex, Transform slot)
    {
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

    public void MoveFromFrontToPrisonInside(Transform slot, int slotIndex)
    {
        assignedPrisonInsideSlot = slot;
        assignedInsideSlotIndex = slotIndex;

        if (assignedPrisonInsideSlot == null || movement == null)
            return;

        movement.SetDestination(assignedPrisonInsideSlot.position);
        state = State.MoveToPrisonInside;
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
            case State.MoveToPrisonFront:
                UpdateMoveToPrisonFront();
                break;
            case State.WaitAtPrisonFront:
                UpdateWaitAtPrisonFront();
                break;
            case State.MoveToPrisonInside:
                UpdateMoveToPrisonInside();
                break;
            case State.PrisonIdle:
                break;
        }
    }

    private void UpdateEnterQueue()
    {
        if (queue == null)
            return;

        if (!registeredToQueue)
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

        state = State.WaitInQueue;
    }

    private void UpdateWaitInQueue()
    {
        if (currentQueueSlot == null)
            return;

        if (Vector3.Distance(transform.position, currentQueueSlot.position) > 0.2f)
        {
            movement.SetDestination(currentQueueSlot.position);
            state = State.MoveToQueueSlot;
        }
    }

    private void UpdateMoveToMoneyDrop()
    {
        if (moneyDropPosition == null)
        {
            MoveToPrisonFront();
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

        MoveToPrisonFront();
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
                moneyColliders[c].enabled = false;

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
        }
    }

    private void MoveToPrisonFront()
    {
        if (prisonManager == null || prisonManager.FrontPoint == null)
        {
            EnterPrisonIdleState();
            return;
        }

        movement.SetDestination(prisonManager.FrontPoint.position);
        state = State.MoveToPrisonFront;
    }

    private void UpdateMoveToPrisonFront()
    {
        if (prisonManager == null || prisonManager.FrontPoint == null)
        {
            EnterPrisonIdleState();
            return;
        }

        if (!movement.HasReachedDestination())
            return;

        TryEnterPrisonFromFront();
    }

    private void UpdateWaitAtPrisonFront()
    {
        if (prisonManager == null)
            return;

        if (prisonManager.HasFreeCell())
        {
            TryEnterPrisonFromFront();
            return;
        }

        if (prisonManager.FrontPoint != null &&
            Vector3.Distance(transform.position, prisonManager.FrontPoint.position) > 0.2f)
        {
            movement.SetDestination(prisonManager.FrontPoint.position);
            state = State.MoveToPrisonFront;
        }
    }

    private void TryEnterPrisonFromFront()
    {
        if (prisonManager == null)
        {
            EnterPrisonIdleState();
            return;
        }

        if (prisonManager.TryReserveInsideSlot(this, out Transform insideSlot, out int slotIndex))
        {
            MoveFromFrontToPrisonInside(insideSlot, slotIndex);
            return;
        }

        prisonManager.NotifyWaitingAtFront(this);
        state = State.WaitAtPrisonFront;

        if (movement != null)
            movement.Stop();
    }

    private void UpdateMoveToPrisonInside()
    {
        if (assignedPrisonInsideSlot == null)
        {
            EnterPrisonIdleState();
            return;
        }

        if (!movement.HasReachedDestination())
            return;

        if (prisonManager != null)
            prisonManager.ConfirmEntered(this, assignedInsideSlotIndex);

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

        movement.RefreshAnimator();
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

        movement.RefreshAnimator();
    }

    private void LeaveQueueImmediately()
    {
        if (queue != null && registeredToQueue)
        {
            queue.Remove(this);
            registeredToQueue = false;
        }

        currentQueueSlot = null;
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
            MoveToPrisonFront();
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

    private void EnterPrisonIdleState()
    {
        if (state == State.PrisonIdle)
            return;

        state = State.PrisonIdle;

        if (movement != null)
        {
            movement.Stop();
            movement.enabled = false;
        }

        if (agent != null)
        {
            agent.ResetPath();
            agent.enabled = false;
        }

        Animator animator = GetComponentInChildren<Animator>();
        animator.SetFloat("Velocity", 0);

        SetPrisonRigidbodiesActive(true);
        enabled = false;
    }

    private void SetPrisonRigidbodiesActive(bool active)
    {
        if (rigidbodies == null)
            return;

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody rb = rigidbodies[i];
            if (rb == null)
                continue;

            rb.isKinematic = !active;
            if (active)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.detectCollisions = active;
        }
    }

    private void OnDestroy()
    {
        if (queue != null && registeredToQueue)
            queue.Remove(this);

        if (prisonManager != null)
            prisonManager.ReleaseOccupant(this);

        if (!generatorReleased && ownerGenerator != null)
        {
            ownerGenerator.NotifyPrisonerRemoved();
            generatorReleased = true;
        }
    }
}