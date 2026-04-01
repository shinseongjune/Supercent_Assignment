using UnityEngine;

public class PrisonerGenerator : MonoBehaviour
{
    [SerializeField] private PrisonerAI prisonerPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private PrisonerQueue queue;
    [SerializeField] private float spawnInterval = 2.0f;
    [SerializeField] private int maxAliveCount = 10;

    [Header("Next Flow")]
    [SerializeField] private Transform moneyDropPosition;
    [SerializeField] private ItemStorage moneyStorage;
    [SerializeField] private Carriable moneyPrefab;
    [SerializeField] private Transform prisonGatePosition;
    [SerializeField] private Transform prisonInsidePosition;

    private float timer;
    private int aliveCount;

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        timer = spawnInterval;
        TrySpawn();
    }

    private void TrySpawn()
    {
        if (prisonerPrefab == null || spawnPoint == null || queue == null)
            return;

        if (aliveCount >= maxAliveCount)
            return;

        if (!queue.HasEmptySlot)
            return;

        PrisonerAI prisoner = Instantiate(
            prisonerPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        prisoner.Initialize(
            queue,
            prisonGatePosition,
            prisonInsidePosition,
            moneyDropPosition,
            moneyStorage,
            moneyPrefab,
            this
        );

        aliveCount++;
    }

    public void NotifyPrisonerRemoved()
    {
        aliveCount = Mathf.Max(0, aliveCount - 1);
    }
}