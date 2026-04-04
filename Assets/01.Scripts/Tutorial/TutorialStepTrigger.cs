using UnityEngine;

public class TutorialStepTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        MiningArea,
        MachineInput,
        MachineOutput,
        HandcuffDeposit,
        MoneyDeposit
    }

    [SerializeField] private TutorialSequenceController tutorial;
    [SerializeField] private TriggerType triggerType;
    [SerializeField] private bool triggerOnce = true;

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered && triggerOnce)
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;

        if (tutorial == null)
            return;

        switch (triggerType)
        {
            case TriggerType.MiningArea:
                tutorial.NotifyReachedMiningArea();
                break;

            case TriggerType.MachineInput:
                tutorial.NotifyReachedMachineInput();
                break;

            case TriggerType.MachineOutput:
                tutorial.NotifyReachedMachineOutput();
                break;

            case TriggerType.HandcuffDeposit:
                tutorial.NotifyReachedHandcuffDeposit();
                break;

            case TriggerType.MoneyDeposit:
                tutorial.NotifyReachedMoneyDeposit();
                break;
        }

        if (triggerOnce)
            triggered = true;
    }

    public void ResetTrigger()
    {
        triggered = false;
    }
}