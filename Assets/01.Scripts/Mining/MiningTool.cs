using UnityEngine;

public abstract class MiningTool : MonoBehaviour
{
    protected PlayerMiningController owner;
    protected MiningArea currentArea;
    protected bool isMining;

    [Header("Targeting")]
    [SerializeField] protected Transform mineOrigin;
    [SerializeField] protected float mineRange = 1.1f;

    public virtual void OnEquip(PlayerMiningController controller)
    {
        owner = controller;
        gameObject.SetActive(false);
    }

    public virtual void OnUnequip()
    {
        EndMining();
        owner = null;
    }

    public virtual void BeginMining(MiningArea area)
    {
        currentArea = area;
        isMining = true;
        gameObject.SetActive(true);
    }

    public virtual void EndMining()
    {
        isMining = false;
        currentArea = null;
        gameObject.SetActive(false);
    }

    protected MineableOre FindCurrentTarget()
    {
        if (!isMining || currentArea == null || owner == null)
            return null;

        Vector3 origin = mineOrigin != null ? mineOrigin.position : owner.transform.position;
        return currentArea.FindBestOreInRange(origin, mineRange);
    }
}