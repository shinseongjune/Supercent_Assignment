using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemCapacityEntry
{
    public Carriable.Type type;
    public int capacity = 1;
}

public class Inventory : MonoBehaviour
{
    [Header("Pickup")]
    [SerializeField] private LayerMask carriableMask;
    [SerializeField] private float pickupRadius = 1.25f;
    [SerializeField] private float pickupInterval = 0.05f;

    [Header("Stack Layout")]
    [SerializeField] private float backwardBaseOffset = 0.6f;
    [SerializeField] private float forwardBaseOffset = 0.6f;
    [SerializeField] private float laneLateralOffset = 0f;
    [SerializeField] private float baseHeight = 0.5f;

    [Header("Capacity By Type")]
    [SerializeField] private ItemCapacityEntry[] capacities;

    private readonly List<Carriable> items = new();
    private readonly Collider[] pickupBuffer = new Collider[32];

    private float pickupTimer = 0f;
    private int orderCounter = 0;

    private readonly Dictionary<Carriable, int> insertionOrders = new();

    public IReadOnlyList<Carriable> Items => items;

    private void Update()
    {
        pickupTimer -= Time.deltaTime;
        if (pickupTimer > 0f)
            return;

        pickupTimer = pickupInterval;
        TryAutoPickup();
    }

    public int GetCapacity(Carriable.Type type)
    {
        for (int i = 0; i < capacities.Length; i++)
        {
            if (capacities[i].type == type)
                return Mathf.Max(0, capacities[i].capacity);
        }

        return 0;
    }

    public int GetCount(Carriable.Type type)
    {
        int count = 0;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].type == type)
                count++;
        }

        return count;
    }

    public bool IsFull(Carriable.Type type)
    {
        return GetCount(type) >= GetCapacity(type);
    }

    public void SetCapacity(Carriable.Type type, int newCapacity)
    {
        for (int i = 0; i < capacities.Length; i++)
        {
            if (capacities[i].type == type)
            {
                capacities[i].capacity = Mathf.Max(0, newCapacity);
                return;
            }
        }
    }

    private void TryAutoPickup()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            pickupRadius,
            pickupBuffer,
            carriableMask
        );

        Carriable bestItem = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = pickupBuffer[i];
            if (col == null)
                continue;

            Carriable item = col.GetComponentInParent<Carriable>();
            if (item == null)
                continue;

            if (!CanTake(item))
                continue;

            float distSq = (item.transform.position - transform.position).sqrMagnitude;

            // 가까운 순 + 우선순위 약간 반영
            float score = distSq + item.CarryPriority * 0.001f;

            if (score < bestScore)
            {
                bestScore = score;
                bestItem = item;
            }
        }

        if (bestItem != null)
            Take(bestItem);
    }

    public bool CanTake(Carriable item)
    {
        if (item == null)
            return false;

        if (items.Contains(item))
            return false;

        if (item.state != Carriable.State.Grounded)
            return false;

        if (IsFull(item.type))
            return false;

        return true;
    }

    public bool CanReceiveFromZone(Carriable item)
    {
        if (item == null)
            return false;

        if (items.Contains(item))
            return false;

        if (IsFull(item.type))
            return false;

        return true;
    }

    public bool TryReceiveFromZone(Carriable item)
    {
        if (!CanReceiveFromZone(item))
            return false;

        items.Add(item);
        insertionOrders[item] = orderCounter++;
        SortItems();
        RefreshCarryLayout(item);
        return true;
    }

    public bool Take(Carriable item)
    {
        if (!CanTake(item))
            return false;

        items.Add(item);
        insertionOrders[item] = orderCounter++;
        SortItems();
        RefreshCarryLayout(item);
        return true;
    }

    public bool Remove(Carriable item)
    {
        if (item == null)
            return false;

        bool removed = items.Remove(item);
        if (!removed)
            return false;

        insertionOrders.Remove(item);
        RefreshCarryLayout();
        return true;
    }

    public bool TryReleaseLast(out Carriable releasedItem)
    {
        releasedItem = null;

        if (items.Count == 0)
            return false;

        releasedItem = items[items.Count - 1];
        items.RemoveAt(items.Count - 1);
        insertionOrders.Remove(releasedItem);
        RefreshCarryLayout();
        return true;
    }

    public bool TryReleaseType(Carriable.Type type, out Carriable releasedItem)
    {
        releasedItem = null;

        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].type != type)
                continue;

            releasedItem = items[i];
            items.RemoveAt(i);
            insertionOrders.Remove(releasedItem);
            RefreshCarryLayout();
            return true;
        }

        return false;
    }

    private void SortItems()
    {
        items.Sort(CompareItems);
    }

    private int CompareItems(Carriable a, Carriable b)
    {
        // 1. 레인
        int laneCompare = a.carryPosition.CompareTo(b.carryPosition);
        if (laneCompare != 0)
            return laneCompare;

        // 2. 우선순위
        int priorityCompare = a.CarryPriority.CompareTo(b.CarryPriority);
        if (priorityCompare != 0)
            return priorityCompare;

        // 3. 타입별 그룹 유지
        int typeCompare = a.type.CompareTo(b.type);
        if (typeCompare != 0)
            return typeCompare;

        // 4. 먼저 들어온 순서 유지
        int aOrder = insertionOrders.TryGetValue(a, out int ao) ? ao : 0;
        int bOrder = insertionOrders.TryGetValue(b, out int bo) ? bo : 0;
        return aOrder.CompareTo(bOrder);
    }

    private void RefreshCarryLayout(Carriable newlyTakenItem = null)
    {
        LayoutLane(Carriable.CarryPosition.Backward, newlyTakenItem);
        LayoutLane(Carriable.CarryPosition.Forward, newlyTakenItem);
    }

    private void LayoutLane(Carriable.CarryPosition lane, Carriable newlyTakenItem)
    {
        float depthOffset = lane == Carriable.CarryPosition.Backward
            ? backwardBaseOffset
            : forwardBaseOffset;

        int index = 0;

        while (index < items.Count)
        {
            Carriable first = items[index];

            if (first.carryPosition != lane)
            {
                index++;
                continue;
            }

            // 같은 lane + 같은 priority + 같은 type 을 한 그룹으로 본다
            int start = index;
            int end = index + 1;

            while (end < items.Count)
            {
                Carriable next = items[end];

                if (next.carryPosition != lane) break;
                if (next.CarryPriority != first.CarryPriority) break;
                if (next.type != first.type) break;

                end++;
            }

            // 그룹 배치: 같은 깊이에서 위로 쌓기
            for (int i = start; i < end; i++)
            {
                Carriable item = items[i];
                int stackIndex = i - start;

                float y = baseHeight + item.VerticalStackSpacing * stackIndex;
                float z = lane == Carriable.CarryPosition.Backward ? -depthOffset : depthOffset;

                Vector3 localPos = new Vector3(
                    laneLateralOffset,
                    y,
                    z
                );

                item.Taken(
                    transform,
                    localPos,
                    item.CarriedEulerRotation,
                    item == newlyTakenItem
                );
            }

            // 그룹 하나가 깊이 한 칸 차지
            depthOffset += first.StackSpacing;
            index = end;
        }
    }

    public Vector3 GetReleasePosition(float distance = 1.0f, float yOffset = 0f)
    {
        return transform.position + transform.forward * distance + Vector3.up * yOffset;
    }

    public Vector3 GetReleaseEuler()
    {
        return transform.eulerAngles;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}