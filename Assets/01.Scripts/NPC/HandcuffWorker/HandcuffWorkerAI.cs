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

    private void UpdateGoToMachinePickup()
    {
        if (HasAnyHandcuff)
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

        if (HasAnyHandcuff && (IsHandcuffInventoryFull || !MachineHasSupply))
        {
            ChangeState(State.GoToHandoff);
            return;
        }

        if (!HasAnyHandcuff && !MachineHasSupply)
        {
            ChangeState(State.GoToHandoff);
            return;
        }

        if (HasAnyHandcuff && !MachineHasSupply)
        {
            ChangeState(State.GoToHandoff);
            return;
        }
    }

    private void UpdateGoToHandoff()
    {
        // 들고 있으면 무조건 handoff 우선
        if (!HasAnyHandcuff && MachineHasSupply)
        {
            ChangeState(State.GoToMachinePickup);
            return;
        }

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

        // 아직 손에 들고 있으면 DepositZone이 처리할 때까지 대기
        if (HasAnyHandcuff)
            return;

        // 손에 아무것도 없고 machine 쪽에 수갑이 있으면 바로 가지러 감
        if (MachineHasSupply)
        {
            ChangeState(State.GoToMachinePickup);
            return;
        }

        // 없으면 handoff에서 대기
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