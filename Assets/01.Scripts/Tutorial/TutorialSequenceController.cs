using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TutorialSequenceController : MonoBehaviour
{
    public enum TutorialStep
    {
        None = 0,

        GoToMiningArea,
        GoToMachineInput,
        GoToMachineOutput,
        GoToHandcuffDeposit,
        GoToMoneyDeposit,

        RevealDrillUpgrade,
        WaitForDrillUpgradePurchase,

        RevealMiningWorkerAndVehicleUpgrade,
        WaitForMiningWorkerPurchase,

        RevealHandcuffWorkerUpgrade,
        WaitForHandcuffWorkerPurchase,

        WaitForPrisonBecomeFull,

        RevealPrisonUpgrade,
        WaitForPrisonUpgradePurchase,

        ChangePrisonModel,

        Complete
    }

    [Header("Step Targets")]
    [SerializeField] private Transform miningAreaTarget;
    [SerializeField] private Transform machineInputTarget;
    [SerializeField] private Transform machineOutputTarget;
    [SerializeField] private Transform handcuffDepositTarget;
    [SerializeField] private Transform moneyDepositTarget;

    [Header("Upgrade Targets")]
    [SerializeField] private Transform drillUpgradeTarget;
    [SerializeField] private Transform miningWorkerUpgradeTarget;
    [SerializeField] private Transform vehicleUpgradeTarget;
    [SerializeField] private Transform handcuffWorkerUpgradeTarget;
    [SerializeField] private Transform prisonUpgradeTarget;
    [SerializeField] private Transform prisonModelTarget;

    [Header("References")]
    [SerializeField] private SimpleIsometricCamera gameCamera;
    [SerializeField] private TutorialArrowController arrowController;
    [SerializeField] private PrisonManager prisonManager;
    [SerializeField] private PrisonModelChanger prisonModelChanger;

    [Header("Step Events")]
    [SerializeField] private UnityEvent onRevealDrillUpgrade;
    [SerializeField] private UnityEvent onRevealMiningWorkerAndVehicleUpgrade;
    [SerializeField] private UnityEvent onRevealHandcuffWorkerUpgrade;
    [SerializeField] private UnityEvent onRevealPrisonUpgrade;
    [SerializeField] private UnityEvent onGameComplete;

    [Header("Cutscene")]
    [SerializeField] private float cutsceneFocusDuration = 1.5f;
    [SerializeField] private float cutsceneReturnDelay = 0.25f;
    [SerializeField] private Vector3 cutsceneOffset = new Vector3(0f, 12f, -10f);

    [Header("Runtime")]
    [SerializeField] private TutorialStep currentStep = TutorialStep.None;
    [SerializeField] private bool playOnStart = true;

    private bool isBusy;

    public TutorialStep CurrentStep => currentStep;

    private void OnEnable()
    {
        if (prisonManager != null)
        {
            prisonManager = FindFirstObjectByType<PrisonManager>();
        }
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartTutorial();
        }
    }

    public void StartTutorial()
    {
        ChangeStep(TutorialStep.GoToMiningArea);
    }

    public void ChangeStep(TutorialStep nextStep)
    {
        if (currentStep == nextStep)
            return;

        currentStep = nextStep;
        HandleEnterStep(nextStep);
    }

    private void HandleEnterStep(TutorialStep step)
    {
        switch (step)
        {
            case TutorialStep.GoToMiningArea:
                SetGameplayGuide(miningAreaTarget);
                break;

            case TutorialStep.GoToMachineInput:
                SetGameplayGuide(machineInputTarget);
                break;

            case TutorialStep.GoToMachineOutput:
                SetGameplayGuide(machineOutputTarget);
                break;

            case TutorialStep.GoToHandcuffDeposit:
                SetGameplayGuide(handcuffDepositTarget);
                break;

            case TutorialStep.GoToMoneyDeposit:
                SetGameplayGuide(moneyDepositTarget);
                break;

            case TutorialStep.RevealDrillUpgrade:
                StartCoroutine(CoRevealUpgrade(
                    drillUpgradeTarget,
                    onRevealDrillUpgrade,
                    TutorialStep.WaitForDrillUpgradePurchase
                ));
                break;

            case TutorialStep.WaitForDrillUpgradePurchase:
                SetGameplayGuide(drillUpgradeTarget);
                break;

            case TutorialStep.RevealMiningWorkerAndVehicleUpgrade:
                StartCoroutine(CoRevealUpgrade(
                    miningWorkerUpgradeTarget != null ? miningWorkerUpgradeTarget : vehicleUpgradeTarget,
                    onRevealMiningWorkerAndVehicleUpgrade,
                    TutorialStep.WaitForMiningWorkerPurchase
                ));
                break;

            case TutorialStep.WaitForMiningWorkerPurchase:
                SetGameplayGuide(miningWorkerUpgradeTarget);
                break;

            case TutorialStep.RevealHandcuffWorkerUpgrade:
                StartCoroutine(CoRevealUpgrade(
                    handcuffWorkerUpgradeTarget,
                    onRevealHandcuffWorkerUpgrade,
                    TutorialStep.WaitForHandcuffWorkerPurchase
                ));
                break;

            case TutorialStep.WaitForHandcuffWorkerPurchase:
                SetGameplayGuide(handcuffWorkerUpgradeTarget);
                break;

            case TutorialStep.WaitForPrisonBecomeFull:
                ClearGuide();
                SetGameplayInput();
                break;

            case TutorialStep.RevealPrisonUpgrade:
                StartCoroutine(CoRevealUpgrade(
                    prisonUpgradeTarget,
                    onRevealPrisonUpgrade,
                    TutorialStep.WaitForPrisonUpgradePurchase
                ));
                break;

            case TutorialStep.WaitForPrisonUpgradePurchase:
                SetGameplayGuide(prisonUpgradeTarget);
                break;

            case TutorialStep.ChangePrisonModel:
                StartCoroutine(CoChangePrisonModelAndComplete());
                break;

            case TutorialStep.Complete:
                ClearGuide();
                SetDisabledInput();
                onGameComplete?.Invoke();
                break;
        }
    }

    private void SetGameplayGuide(Transform target)
    {
        SetGameplayInput();

        if (arrowController != null)
            arrowController.SetTarget(target);
    }

    private void ClearGuide()
    {
        if (arrowController != null)
            arrowController.ClearTarget();
    }

    private void SetGameplayInput()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetInputMode(InputMode.Gameplay);
    }

    private void SetCutsceneInput()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetInputMode(InputMode.Cutscene);
    }

    private void SetDisabledInput()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetInputMode(InputMode.Disabled);
    }

    private IEnumerator CoRevealUpgrade(
        Transform focusTarget,
        UnityEvent revealEvent,
        TutorialStep nextStepAfterReveal)
    {
        if (isBusy)
            yield break;

        isBusy = true;
        ClearGuide();
        SetCutsceneInput();

        if (gameCamera != null && focusTarget != null)
        {
            gameCamera.FocusTarget(focusTarget);
        }

        yield return new WaitForSeconds(cutsceneFocusDuration);

        revealEvent?.Invoke();

        yield return new WaitForSeconds(cutsceneReturnDelay);

        if (gameCamera != null)
            gameCamera.ClearFocus();

        isBusy = false;
        ChangeStep(nextStepAfterReveal);
    }

    private IEnumerator CoChangePrisonModelAndComplete()
    {
        if (isBusy)
            yield break;

        isBusy = true;
        ClearGuide();
        SetCutsceneInput();

        if (gameCamera != null && prisonModelTarget != null)
        {
            gameCamera.FocusTarget(prisonModelTarget);
        }

        yield return new WaitForSeconds(cutsceneFocusDuration);

        prisonModelChanger?.Event_ChangeModel();

        yield return new WaitForSeconds(cutsceneReturnDelay);

        if (gameCamera != null)
            gameCamera.ClearFocus();

        isBusy = false;
        ChangeStep(TutorialStep.Complete);
    }

    // ===== 외부 이벤트 진입점 =====

    public void NotifyReachedMiningArea()
    {
        if (currentStep == TutorialStep.GoToMiningArea)
            ChangeStep(TutorialStep.GoToMachineInput);
    }

    public void NotifyReachedMachineInput()
    {
        if (currentStep == TutorialStep.GoToMachineInput)
            ChangeStep(TutorialStep.GoToMachineOutput);
    }

    public void NotifyReachedMachineOutput()
    {
        if (currentStep == TutorialStep.GoToMachineOutput)
            ChangeStep(TutorialStep.GoToHandcuffDeposit);
    }

    public void NotifyReachedHandcuffDeposit()
    {
        if (currentStep == TutorialStep.GoToHandcuffDeposit)
            ChangeStep(TutorialStep.GoToMoneyDeposit);
    }

    public void NotifyReachedMoneyDeposit()
    {
        if (currentStep == TutorialStep.GoToMoneyDeposit)
            ChangeStep(TutorialStep.RevealDrillUpgrade);
    }

    public void NotifyDrillUpgradePurchased()
    {
        if (currentStep == TutorialStep.WaitForDrillUpgradePurchase)
            ChangeStep(TutorialStep.RevealMiningWorkerAndVehicleUpgrade);
    }

    public void NotifyMiningWorkerPurchased()
    {
        if (currentStep == TutorialStep.WaitForMiningWorkerPurchase)
            ChangeStep(TutorialStep.RevealHandcuffWorkerUpgrade);
    }

    public void NotifyHandcuffWorkerPurchased()
    {
        if (currentStep == TutorialStep.WaitForHandcuffWorkerPurchase)
            ChangeStep(TutorialStep.WaitForPrisonBecomeFull);
    }

    public void NotifyPrisonBecameFull()
    {
        if (currentStep == TutorialStep.WaitForPrisonBecomeFull)
            ChangeStep(TutorialStep.RevealPrisonUpgrade);
    }

    public void NotifyPrisonUpgradePurchased()
    {
        if (currentStep == TutorialStep.WaitForPrisonUpgradePurchase)
            ChangeStep(TutorialStep.ChangePrisonModel);
    }
}