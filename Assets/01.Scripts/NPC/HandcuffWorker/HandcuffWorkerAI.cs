using UnityEngine;

[RequireComponent(typeof(NPCMovement))]
[RequireComponent(typeof(Inventory))]
public class HandcuffWorkerAI : MonoBehaviour
{
    private enum State
    {
        GoToMachinePickup,
        WaitAtMachinePickup,
        GoToHandoff,
        WaitAtHandoff
    }

    [Header("References")]
    [SerializeField] private ItemStorage machineOutputHandcuffStorage;
    [SerializeField] private ItemStorage handoffHandcuffStorage;
    [SerializeField] private Transform machinePickupPoint;
    [SerializeField] private Transform handoffPoint;

    [Header("Movement")]
    [SerializeField] private float repathInterval = 0.25f;
    [SerializeField] private float arriveThreshold = 0.5f;

    private NPCMovement movement;
    private Inventory inventory;
    private State state;
    private float repathTimer;

    private void Awake()
    {
        movement = GetComponent<NPCMovement>();
        inventory = GetComponent<Inventory>();
    }

    private void Start()
    {
        state = State.GoToHandoff;
    }

    private void Update()
    {
        switch (state)
        {
            case State.GoToMachinePickup:
                UpdateGoToMachinePickup();
                break;

            case State.WaitAtMachinePickup:
                UpdateWaitAtMachinePickup();
                break;

            case State.GoToHandoff:
                UpdateGoToHandoff();
                break;

            case State.WaitAtHandoff:
                UpdateWaitAtHandoff();
                break;
        }
    }

    private int HandcuffCount => inventory != null ? inventory.GetCount(Carriable.Type.Handcuff) : 0;
    private bool HasAnyHandcuff => HandcuffCount > 0;
    private bool IsHandcuffInventoryFull => inventory != null && inventory.IsFull(Carriable.Type.Handcuff);

    private bool MachineHasSupply =>
        machineOutputHandcuffStorage != null && !machineOutputHandcuffStorage.IsEmpty;

    private bool HandoffHasSupply =>
        handoffHandcuffStorage != null && !handoffHandcuffStorage.IsEmpty;

    private bool ShouldStayAtHandoff =>
        HasAnyHandcuff || HandoffHasSupply;

    private void UpdateGoToMachinePickup()
    {
        // handoff 쪽에 아직 줄 수갑이 남아 있으면 handoff 우선
        if (ShouldStayAtHandoff)
        {
            ChangeState(State.GoToHandoff);
            return;
        }

        if (!MachineHasSupply)
        {
            ChangeState(State.GoToHandoff);
            return;
        }

        MoveRepeated(machinePickupPoint);

        if (HasArrived(machinePickupPoint))
        {
            movement.Stop();
            ChangeState(State.WaitAtMachinePickup);
        }
    }

    private void UpdateWaitAtMachinePickup()
    {
        if (!HasArrived(machinePickupPoint))
        {
            ChangeState(State.GoToMachinePickup);
            return;
        }

        // 손에 들었거나 handoff에 재고가 생기면 handoff 우선
        if (ShouldStayAtHandoff)
        {
            ChangeState(State.GoToHandoff);
            return;
        }

        // 더 이상 가져올 것도 없으면 handoff로 돌아가 대기
        if (!MachineHasSupply)
        {
            ChangeState(State.GoToHandoff);
            return;
        }

        // 아직 더 담을 수 있고 machine에도 남아 있으면 여기서 계속 대기
        if (HasAnyHandcuff && IsHandcuffInventoryFull)
        {
            ChangeState(State.GoToHandoff);
            return;
        }
    }

    private void UpdateGoToHandoff()
    {
        MoveRepeated(handoffPoint);

        if (HasArrived(handoffPoint))
        {
            movement.Stop();
            ChangeState(State.WaitAtHandoff);
        }
    }

    private void UpdateWaitAtHandoff()
    {
        if (!HasArrived(handoffPoint))
        {
            ChangeState(State.GoToHandoff);
            return;
        }

        // 손에 있거나 handoff 저장소에 남아 있으면 계속 여기 대기
        if (ShouldStayAtHandoff)
            return;

        // handoff 쪽이 완전히 비었을 때만 machine으로 복귀
        if (MachineHasSupply)
        {
            ChangeState(State.GoToMachinePickup);
            return;
        }

        // 둘 다 없으면 여기서 대기
    }

    private void ChangeState(State next)
    {
        if (state == next)
            return;

        state = next;
        repathTimer = 0f;
    }

    private void MoveRepeated(Transform target)
    {
        if (target == null)
            return;

        repathTimer -= Time.deltaTime;
        if (repathTimer > 0f)
            return;

        repathTimer = repathInterval;
        movement.SetDestination(target.position);
    }

    private bool HasArrived(Transform target)
    {
        if (target == null)
            return false;

        return Vector3.Distance(transform.position, target.position) <= arriveThreshold
               || movement.HasReachedDestination(arriveThreshold);
    }
}