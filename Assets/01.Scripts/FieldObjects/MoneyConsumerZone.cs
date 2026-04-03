using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 돈을 소모하는 장소
public class MoneyConsumerZone : MonoBehaviour
{
    [SerializeField] private int targetCost = 10; // 아이템 1개 당 5원
    [SerializeField] private float consumeInterval = 0.1f;
    [SerializeField] private UnityEvent onCompleted;

    [SerializeField] private TextMeshProUGUI moneyDemandText;

    [SerializeField] private AudioClip consumeClip;
    [SerializeField] private AudioClip upgradeClip;

    [SerializeField] private Image fillBackground;

    private int currentValue;
    private float timer;
    private Inventory currentInventory;
    private bool completed;

    private void Update()
    {
        if (completed) return;

        if (moneyDemandText != null)
        {
            int demand = (targetCost - currentValue) * 5;
            moneyDemandText.text = demand.ToString();
        }

        if (fillBackground != null)
        {
            fillBackground.fillAmount = (float)currentValue / targetCost;
        }

        if (currentInventory == null)
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
            if (upgradeClip != null)
            {
                AudioManager.Instance?.Play(upgradeClip);
            }

            completed = true;
            onCompleted?.Invoke();
        }
        else
        {
            if (consumeClip != null)
            {
                AudioManager.Instance?.Play(consumeClip);
            }
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