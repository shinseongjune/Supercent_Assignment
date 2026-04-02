using UnityEngine;

public class DrillTool : MiningTool
{
    [SerializeField] private float mineInterval = 0.2f;
    [SerializeField] private int damage = 1;

    private float timer;

    private void Update()
    {
        if (!isMining || currentArea == null || owner == null)
            return;

        MineableOre ore = FindCurrentTarget();
        if (ore == null)
        {
            timer = 0f;
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        timer = mineInterval;
        ore.TakeMineDamage(damage, owner);
    }

    public override void BeginMining(MiningArea area)
    {
        base.BeginMining(area);
        timer = 0f;
    }
}