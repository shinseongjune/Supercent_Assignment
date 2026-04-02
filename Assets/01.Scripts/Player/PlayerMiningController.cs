using UnityEngine;

public class PlayerMiningController : MonoBehaviour
{
    private MiningTool currentTool;
    private MiningArea currentArea;

    [SerializeField] MiningTool firstTool;

    private void Start()
    {
        SetTool(firstTool);
    }

    public void SetTool(MiningTool tool)
    {
        if (currentTool == tool)
            return;

        if (currentTool != null)
            currentTool.OnUnequip();

        currentTool = tool;

        if (currentTool != null)
            currentTool.OnEquip(this);
    }

    public void EnterMiningArea(MiningArea area)
    {
        currentArea = area;
        currentTool?.BeginMining(area);
    }

    public void ExitMiningArea(MiningArea area)
    {
        if (currentArea != area)
            return;

        currentTool?.EndMining();
        currentArea = null;
    }

    public MiningArea CurrentArea => currentArea;
}