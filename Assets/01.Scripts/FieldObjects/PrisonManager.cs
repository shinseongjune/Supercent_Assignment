using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PrisonManager : MonoBehaviour
{
    [Header("Capacity")]
    [SerializeField] private int baseCapacity = 4;

    [Header("Path")]
    [SerializeField] private Transform frontPoint;
    [SerializeField] private Transform[] insideSlots;

    [Header("Events")]
    [SerializeField] private UnityEvent onPrisonStateChanged;
    [SerializeField] private UnityEvent onPrisonBecameFull;

    private readonly List<PrisonerAI> occupants = new();
    private readonly Queue<PrisonerAI> waitingQueue = new();
    private readonly Dictionary<PrisonerAI, int> reservedInsideSlots = new();

    private int addedCapacity = 0;
    private bool notifiedFull = false;

    public Transform FrontPoint => frontPoint;
    public int Capacity => Mathf.Min(baseCapacity + addedCapacity, insideSlots != null ? insideSlots.Length : 0);
    public int OccupantCount => occupants.Count;
    public bool IsFull => OccupantCount + reservedInsideSlots.Count >= Capacity;

    public bool HasFreeCell()
    {
        return !IsFull;
    }

    public bool TryReserveInsideSlot(PrisonerAI prisoner, out Transform slot, out int slotIndex)
    {
        slot = null;
        slotIndex = -1;

        if (prisoner == null)
            return false;

        if (reservedInsideSlots.TryGetValue(prisoner, out int alreadyReserved))
        {
            slotIndex = alreadyReserved;
            slot = GetInsideSlot(alreadyReserved);
            return slot != null;
        }

        int usableCapacity = Capacity;
        if (usableCapacity <= 0 || insideSlots == null || insideSlots.Length == 0)
            return false;

        for (int i = 0; i < usableCapacity; i++)
        {
            if (IsInsideSlotUsed(i))
                continue;

            reservedInsideSlots[prisoner] = i;
            slotIndex = i;
            slot = insideSlots[i];
            return true;
        }

        return false;
    }

    public void NotifyWaitingAtFront(PrisonerAI prisoner)
    {
        if (prisoner == null)
            return;

        if (occupants.Contains(prisoner))
            return;

        if (reservedInsideSlots.ContainsKey(prisoner))
            return;

        if (!waitingQueue.Contains(prisoner))
            waitingQueue.Enqueue(prisoner);

        RefreshState();
    }

    public void ConfirmEntered(PrisonerAI prisoner, int slotIndex)
    {
        if (prisoner == null)
            return;

        if (reservedInsideSlots.TryGetValue(prisoner, out int reservedIndex))
        {
            if (reservedIndex != slotIndex)
                slotIndex = reservedIndex;

            reservedInsideSlots.Remove(prisoner);
        }

        RemoveFromWaitingQueue(prisoner);

        if (!occupants.Contains(prisoner))
            occupants.Add(prisoner);

        RefreshState();
        TryPromoteWaitingPrisoners();
    }

    public void ReleaseOccupant(PrisonerAI prisoner)
    {
        if (prisoner == null)
            return;

        reservedInsideSlots.Remove(prisoner);
        RemoveFromWaitingQueue(prisoner);
        occupants.Remove(prisoner);

        RefreshState();
        TryPromoteWaitingPrisoners();
    }

    public void AddCapacity(int amount)
    {
        if (amount <= 0)
            return;

        addedCapacity += amount;
        RefreshState();
        TryPromoteWaitingPrisoners();
    }

    private void TryPromoteWaitingPrisoners()
    {
        if (waitingQueue.Count == 0)
        {
            RefreshState();
            return;
        }

        int loop = waitingQueue.Count + 8;

        while (waitingQueue.Count > 0 && HasFreeCell() && loop-- > 0)
        {
            PrisonerAI prisoner = waitingQueue.Peek();

            if (prisoner == null)
            {
                waitingQueue.Dequeue();
                continue;
            }

            if (TryReserveInsideSlot(prisoner, out Transform insideSlot, out int slotIndex))
            {
                waitingQueue.Dequeue();
                prisoner.MoveFromFrontToPrisonInside(insideSlot, slotIndex);
            }
            else
            {
                break;
            }
        }

        RefreshState();
    }

    private bool IsInsideSlotUsed(int slotIndex)
    {
        foreach (var pair in reservedInsideSlots)
        {
            if (pair.Value == slotIndex)
                return true;
        }

        for (int i = 0; i < occupants.Count; i++)
        {
            PrisonerAI prisoner = occupants[i];
            if (prisoner == null)
                continue;

            if (prisoner.AssignedInsideSlotIndex == slotIndex)
                return true;
        }

        return false;
    }

    private Transform GetInsideSlot(int index)
    {
        if (insideSlots == null || index < 0 || index >= insideSlots.Length)
            return null;

        return insideSlots[index];
    }

    private void RemoveFromWaitingQueue(PrisonerAI target)
    {
        if (waitingQueue.Count == 0 || target == null)
            return;

        Queue<PrisonerAI> rebuilt = new Queue<PrisonerAI>();

        while (waitingQueue.Count > 0)
        {
            PrisonerAI current = waitingQueue.Dequeue();
            if (current != null && current != target)
                rebuilt.Enqueue(current);
        }

        while (rebuilt.Count > 0)
            waitingQueue.Enqueue(rebuilt.Dequeue());
    }

    private void RefreshState()
    {
        onPrisonStateChanged?.Invoke();

        bool nowFull = IsFull && Capacity > 0;

        if (nowFull && !notifiedFull)
        {
            notifiedFull = true;
            onPrisonBecameFull?.Invoke();
        }
        else if (!nowFull)
        {
            notifiedFull = false;
        }
    }
}