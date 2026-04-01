using System.Collections.Generic;
using UnityEngine;

public class PrisonerQueue : MonoBehaviour
{
    [SerializeField] private Transform[] waitingSlots;

    private readonly List<PrisonerAI> prisoners = new();

    public bool HasEmptySlot => waitingSlots != null && prisoners.Count < waitingSlots.Length;
    public int SlotCount => waitingSlots != null ? waitingSlots.Length : 0;

    public PrisonerAI FrontPrisoner
    {
        get
        {
            if (prisoners.Count == 0)
                return null;

            return prisoners[0];
        }
    }

    public bool TryRegister(PrisonerAI prisoner)
    {
        if (prisoner == null)
            return false;

        if (prisoners.Contains(prisoner))
            return true;

        if (waitingSlots == null || prisoners.Count >= waitingSlots.Length)
            return false;

        prisoners.Add(prisoner);
        RefreshQueue();
        return true;
    }

    public void Remove(PrisonerAI prisoner)
    {
        if (prisoner == null)
            return;

        if (prisoners.Remove(prisoner))
        {
            RefreshQueue();
        }
    }

    public bool IsFront(PrisonerAI prisoner)
    {
        return prisoners.Count > 0 && prisoners[0] == prisoner;
    }

    public Transform GetSlot(int index)
    {
        if (waitingSlots == null || index < 0 || index >= waitingSlots.Length)
            return null;

        return waitingSlots[index];
    }

    private void RefreshQueue()
    {
        if (waitingSlots == null || waitingSlots.Length == 0)
            return;

        int startSlotIndex = waitingSlots.Length - prisoners.Count;

        for (int i = 0; i < prisoners.Count; i++)
        {
            int slotIndex = startSlotIndex + i;
            prisoners[i].AssignQueueSlot(slotIndex, waitingSlots[slotIndex]);
        }
    }
}