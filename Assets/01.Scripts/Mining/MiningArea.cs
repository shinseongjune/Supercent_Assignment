using UnityEngine;

public class MiningArea : MonoBehaviour
{
    [SerializeField] private Transform seatPoint;
    [SerializeField] private MineableOre[] ores;

    public Transform SeatPoint => seatPoint;

    public MineableOre FindBestOreInRange(Vector3 from, float range)
    {
        MineableOre best = null;
        float bestDist = float.MaxValue;
        float rangeSq = range * range;

        for (int i = 0; i < ores.Length; i++)
        {
            MineableOre ore = ores[i];
            if (ore == null || !ore.IsAvailable)
                continue;

            float distSq = (ore.transform.position - from).sqrMagnitude;
            if (distSq > rangeSq)
                continue;

            if (distSq < bestDist)
            {
                bestDist = distSq;
                best = ore;
            }
        }

        return best;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMiningController mining = other.GetComponentInParent<PlayerMiningController>();
        if (mining != null)
            mining.EnterMiningArea(this);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerMiningController mining = other.GetComponentInParent<PlayerMiningController>();
        if (mining != null)
            mining.ExitMiningArea(this);
    }
}