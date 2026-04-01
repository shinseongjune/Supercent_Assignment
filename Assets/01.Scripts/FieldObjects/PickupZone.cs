using UnityEngine;

// ItemStorage에서 Inventory로 아이템을 옮겨주는 장소
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
        if (sourceStorage.IsEmpty)
            return;

        if (currentInventory.IsFull(sourceStorage.AcceptedType))
            return;

        if (!sourceStorage.TryTakeLast(out Carriable item))
            return;

        if (!currentInventory.TryReceiveFromZone(item))
        {
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