using System.Collections.Generic;
using UnityEngine;

// Inventory에서 ItemStorage로 아이템을 옮겨주는 장소
public class DepositZone : MonoBehaviour
{
    [SerializeField] private ItemStorage targetStorage;
    [SerializeField] private StorageMaxPopupTarget popupTarget;

    [SerializeField] private float transferInterval = 0.15f;

    private float timer;
    private readonly HashSet<Inventory> inventoriesInside = new();

    private void Update()
    {
        if (inventoriesInside.Count == 0 || targetStorage == null)
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
        if (targetStorage.IsFull)
        {
            if (popupTarget != null)
            {
                popupTarget.ShowIfFull();
            }
            return;
        }

        // 인벤토리에서 대상 타입 아이템 1개 꺼내기
        if (!targetInventory.TryReleaseType(targetStorage.AcceptedType, out Carriable item))
            return;

        if (!targetStorage.TryStore(item))
        {
            targetInventory.Take(item);
            return;
        }
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