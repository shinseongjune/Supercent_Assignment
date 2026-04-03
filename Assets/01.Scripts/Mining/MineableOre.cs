using System.Collections;
using UnityEngine;

public class MineableOre : MonoBehaviour
{
    [Header("Ore HP")]
    [SerializeField] private int maxHp = 5;
    [SerializeField] private bool respawnEnabled = true;
    [SerializeField] private float respawnDelay = 5f;

    [Header("Reward")]
    [SerializeField] private Carriable orePrefab;
    [SerializeField] private int dropCount = 1;
    [SerializeField] private Transform dropPoint;

    [Header("Visual / Collision")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Collider hitCollider;

    [SerializeField] private AudioClip breakClip;

    private int currentHp;
    private bool isDepleted;
    private Coroutine respawnRoutine;

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsAvailable => !isDepleted;
    public bool IsDepleted => isDepleted;

    private void Awake()
    {
        currentHp = maxHp;
        ApplyAliveState();
    }

    public bool TakeMineDamage(int damage, PlayerMiningController miner)
    {
        if (isDepleted)
            return false;

        if (damage <= 0)
            damage = 1;

        currentHp -= damage;

        if (currentHp > 0)
            return true;

        if (breakClip != null)
        {
            AudioManager.Instance?.Play(breakClip);
        }
        BreakOreToMiner(miner);
        return true;
    }

    public bool TakeMineDamageToStorage(int damage, ItemStorage targetStorage)
    {
        if (isDepleted)
            return false;

        if (targetStorage == null)
            return false;

        if (!CanRewardToStorage(targetStorage))
            return false;

        if (damage <= 0)
            damage = 1;

        currentHp -= damage;

        if (currentHp > 0)
            return true;

        if (breakClip != null)
        {
            AudioManager.Instance?.Play(breakClip);
        }
        BreakOreToStorage(targetStorage);
        return true;
    }

    public void ResetOre()
    {
        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
        }

        currentHp = maxHp;
        isDepleted = false;
        ApplyAliveState();
    }

    private bool CanRewardToStorage(ItemStorage targetStorage)
    {
        if (orePrefab == null || targetStorage == null)
            return false;

        if (targetStorage.AcceptedType != orePrefab.type)
            return false;

        if (targetStorage.Count + dropCount > targetStorage.Capacity)
            return false;

        return true;
    }

    private void BreakOreToMiner(PlayerMiningController miner)
    {
        if (isDepleted)
            return;

        isDepleted = true;
        currentHp = 0;

        GiveRewardToMiner(miner);
        ApplyDepletedState();

        if (respawnEnabled)
            respawnRoutine = StartCoroutine(CoRespawn());
    }

    private void BreakOreToStorage(ItemStorage targetStorage)
    {
        if (isDepleted)
            return;

        isDepleted = true;
        currentHp = 0;

        GiveRewardToStorage(targetStorage);
        ApplyDepletedState();

        if (respawnEnabled)
            respawnRoutine = StartCoroutine(CoRespawn());
    }

    private void GiveRewardToMiner(PlayerMiningController miner)
    {
        if (orePrefab == null || miner == null)
            return;

        Inventory inventory = miner.GetComponent<Inventory>();
        if (inventory == null)
            inventory = miner.GetComponentInParent<Inventory>();

        if (inventory == null)
            return;

        for (int i = 0; i < dropCount; i++)
        {
            if (inventory.IsFull(orePrefab.type))
            {
                if (inventory.popupTarget != null)
                {
                    inventory.popupTarget.ShowIfFull();
                }
                return;
            }

            Vector3 spawnPos = dropPoint != null ? dropPoint.position : miner.transform.position;
            Carriable spawned = Instantiate(orePrefab, spawnPos, Quaternion.identity);

            if (!inventory.TryReceiveFromZone(spawned))
            {
                Destroy(spawned.gameObject);
                return;
            }
        }
    }

    private void GiveRewardToStorage(ItemStorage targetStorage)
    {
        if (orePrefab == null || targetStorage == null)
            return;

        for (int i = 0; i < dropCount; i++)
        {
            Carriable spawned = Instantiate(
                orePrefab,
                dropPoint != null ? dropPoint.position : targetStorage.GetNextWorldPosition(),
                Quaternion.identity
            );

            if (!targetStorage.TryStore(spawned))
            {
                spawned.Remove();
                return;
            }
        }
    }

    private IEnumerator CoRespawn()
    {
        yield return new WaitForSeconds(respawnDelay);

        currentHp = maxHp;
        isDepleted = false;
        ApplyAliveState();
        respawnRoutine = null;
    }

    private void ApplyAliveState()
    {
        if (visualRoot != null)
            visualRoot.SetActive(true);

        if (hitCollider != null)
            hitCollider.enabled = true;
    }

    private void ApplyDepletedState()
    {
        if (visualRoot != null)
            visualRoot.SetActive(false);

        if (hitCollider != null)
            hitCollider.enabled = false;
    }

    public Vector3 GetMinePoint(Vector3 from)
    {
        if (hitCollider != null)
            return hitCollider.ClosestPoint(from);

        return transform.position;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxHp < 1)
            maxHp = 1;

        if (dropCount < 1)
            dropCount = 1;

        if (respawnDelay < 0f)
            respawnDelay = 0f;
    }
#endif
}