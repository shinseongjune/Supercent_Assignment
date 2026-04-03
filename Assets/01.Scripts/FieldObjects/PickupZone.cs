using System.Collections.Generic;
using UnityEngine;

// Storage에서 Inventory로 아이템을 옮겨주는 장소
public class PickupZone : MonoBehaviour
{
    [SerializeField] private ItemStorage sourceStorage;
    [SerializeField] private float transferInterval = 0.1f;

    private float timer;
    private readonly HashSet<Inventory> inventoriesInside = new();

    private void Update()
    {
        if (sourceStorage == null || inventoriesInside.Count == 0)
            return;

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        timer = transferInterval;

        Inventory target = GetBestInventory();
        if (target != null)
            TryTransferOne(target);
    }

    private Inventory GetBestInventory()
    {
        Inventory best = null;
        float bestDist = float.MaxValue;

        foreach (var inv in inventoriesInside)
        {
            if (inv == null)
                continue;

            float distSq = (inv.transform.position - transform.position).sqrMagnitude;
            if (distSq < bestDist)
            {
                bestDist = distSq;
                best = inv;
            }
        }

        return best;
    }

    private void TryTransferOne(Inventory targetInventory)
    {
        if (sourceStorage.IsEmpty)
            return;

        if (targetInventory.IsFull(sourceStorage.AcceptedType))
            return;

        if (!sourceStorage.TryTakeLast(out Carriable item))
            return;

        if (!targetInventory.TryReceiveFromZone(item))
            sourceStorage.TryStore(item);
    }

    private void OnTriggerEnter(Collider other)
    {
        Inventory inv = other.GetComponentInParent<Inventory>();
        if (inv != null)
            inventoriesInside.Add(inv);
    }

    private void OnTriggerExit(Collider other)
    {
        Inventory inv = other.GetComponentInParent<Inventory>();
        if (inv != null)
            inventoriesInside.Remove(inv);
    }
}