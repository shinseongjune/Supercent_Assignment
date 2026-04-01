using System.Collections;
using UnityEngine;

public class HandcuffHandoffZone : MonoBehaviour
{
    [SerializeField] private PrisonerQueue queue;
    [SerializeField] private ItemStorage handcuffStorage;
    [SerializeField] private float transferInterval = 0.2f;
    [SerializeField] private float flyDuration = 0.25f;
    [SerializeField] private AnimationCurve flyHeightCurve;

    private int workerCountInside = 0;
    private float timer;
    private bool isTransferring = false;

    private void Update()
    {
        if (queue == null || handcuffStorage == null)
            return;

        if (workerCountInside <= 0)
            return;

        if (isTransferring)
            return;

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        timer = transferInterval;
        TryTransferOne();
    }

    private void TryTransferOne()
    {
        PrisonerAI frontPrisoner = queue.FrontPrisoner;
        if (frontPrisoner == null)
            return;

        if (!frontPrisoner.CanReceiveHandcuff())
            return;

        if (handcuffStorage.IsEmpty)
            return;

        if (!handcuffStorage.TryTakeLast(out Carriable item))
            return;

        if (item == null)
            return;

        StartCoroutine(FlyHandcuffToPrisoner(item, frontPrisoner));
    }

    private IEnumerator FlyHandcuffToPrisoner(Carriable item, PrisonerAI prisoner)
    {
        isTransferring = true;

        Transform itemTf = item.transform;
        Vector3 startPos = itemTf.position;
        Quaternion startRot = itemTf.rotation;

        itemTf.SetParent(null);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Collider[] colliders = item.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        float elapsed = 0f;

        while (elapsed < flyDuration)
        {
            if (prisoner == null)
                break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flyDuration);

            Vector3 targetPos = prisoner.HandoffTarget.position;
            Vector3 pos = Vector3.Lerp(startPos, targetPos, t);

            float arc = 0f;
            if (flyHeightCurve != null && flyHeightCurve.length > 0)
                arc = flyHeightCurve.Evaluate(t);

            pos.y += arc;

            itemTf.position = pos;
            itemTf.rotation = Quaternion.Slerp(startRot, Quaternion.identity, t);

            yield return null;
        }

        if (prisoner != null && prisoner.CanReceiveHandcuff())
        {
            prisoner.ReceiveHandcuff();
        }

        if (item != null)
            item.Remove();

        isTransferring = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<Inventory>() != null)
        {
            workerCountInside++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<Inventory>() != null)
        {
            workerCountInside = Mathf.Max(0, workerCountInside - 1);
        }
    }
}