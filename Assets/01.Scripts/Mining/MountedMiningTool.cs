using UnityEngine;

public class MountedMiningTool : MiningTool
{
    [SerializeField] private float mineInterval = 0.15f;
    [SerializeField] private int damage = 1;
    [SerializeField] private Animator animator;

    [SerializeField] private AudioClip clip;

    private float timer;

    public override void OnEquip(PlayerMiningController controller)
    {
        base.OnEquip(controller);

        controller.GetComponent<Inventory>().SetCapacity(Carriable.Type.Ore, 35);
    }

    public override void BeginMining(MiningArea area)
    {
        base.BeginMining(area);
        timer = 0f;

        if (owner != null && area != null)
        {
            owner.transform.GetChild(0).transform.localPosition = new Vector3(0, 1, 0);
        }

        animator?.SetBool("IsMountedMining", true);
    }

    public override void EndMining()
    {
        if (owner != null)
        {
            owner.transform.GetChild(0).transform.localPosition = Vector3.zero;
        }
        animator?.SetBool("IsMountedMining", false);
        base.EndMining();
    }

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
        if (clip != null)
        {
            AudioManager.Instance?.Play(clip);
        }
        ore.TakeMineDamage(damage, owner);
    }
}