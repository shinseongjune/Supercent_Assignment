using UnityEngine;

// Inventory에서 ItemStorage로 아이템을 옮겨주는 장소
public class DepositZone : MonoBehaviour
{
    [SerializeField] private ItemStorage targetStorage;
    [SerializeField] private float transferInterval = 0.15f;

    private float timer;
    private Inventory currentInventory;

    private void Update()
    {
        if (currentInventory == null || targetStorage == null)
            return;

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        timer = transferInterval;

        TryTransferOne();
    }

    private void TryTransferOne()
    {
        if (targetStorage.IsFull)
            return;

        // 인벤토리에서 대상 타입 아이템 1개 꺼내기
        if (!currentInventory.TryReleaseType(targetStorage.AcceptedType, out Carriable item))
            return;

        if (!targetStorage.TryStore(item))
        {
            currentInventory.Take(item);
            return;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Inventory inv = other.GetComponentInParent<Inventory>();
        if (inv != null)
            currentInventory = inv;
    }

    private void OnTriggerExit(Collider other)
    {
        Inventory inv = other.GetComponentInParent<Inventory>();
        if (inv != null && inv == currentInventory)
            currentInventory = null;
    }
}