using UnityEngine;
using UnityEngine.Events;

// 돈을 소모하는 장소
public class MoneyConsumerZone : MonoBehaviour
{
    [SerializeField] private int targetCost = 10; // 아이템 1개 당 5원
    [SerializeField] private float consumeInterval = 0.1f;
    [SerializeField] private UnityEvent onCompleted;

    private int currentValue;
    private float timer;
    private Inventory currentInventory;
    private bool completed;

    private void Update()
    {
        if (completed || currentInventory == null)
            return;

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        timer = consumeInterval;
        TryConsumeOne();
    }

    private void TryConsumeOne()
    {
        if (!currentInventory.TryReleaseType(Carriable.Type.Money, out Carriable item))
            return;

        item.Remove();
        currentValue++;

        if (currentValue >= targetCost)
        {
            completed = true;
            onCompleted?.Invoke();
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