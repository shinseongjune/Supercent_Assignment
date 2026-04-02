using UnityEngine;

public class PickaxeTool : MiningTool
{
    [SerializeField] private float mineInterval = 0.5f;
    [SerializeField] private int damage = 1;
    [SerializeField] private Animator animator;

    private float timer;

    private void Update()
    {
        if (!isMining || currentArea == null || owner == null)
            return;

        MineableOre ore = FindCurrentTarget();
        if (ore == null)
        {
            timer = 0f;   // ore가 다시 들어오면 즉시 반응하게
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        timer = mineInterval;

        animator?.SetTrigger("Mine");
        ore.TakeMineDamage(damage, owner);
    }

    public override void BeginMining(MiningArea area)
    {
        base.BeginMining(area);
        timer = 0f;
    }
}