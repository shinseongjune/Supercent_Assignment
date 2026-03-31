using UnityEngine;

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
            // 실패하면 다시 되돌리는 로직을 나중에 넣을 수 있음
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