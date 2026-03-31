using UnityEngine;

public class PickupZone : MonoBehaviour
{
    [SerializeField] private ItemStorage sourceStorage;
    [SerializeField] private float transferInterval = 0.1f;

    private float timer;
    private Inventory currentInventory;

    private void Update()
    {
        if (currentInventory == null || sourceStorage == null)
            return;

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        timer = transferInterval;

        TryTransferOne();
    }

    private void TryTransferOne()
    {
        if (currentInventory.IsFull)
            return;

        if (!sourceStorage.TryTakeLast(out Carriable item))
            return;

        if (!currentInventory.Take(item))
        {
            // 실패 시 storage로 되돌리는 처리 가능
            sourceStorage.TryStore(item);
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