using UnityEngine;

public class MachineProcessor : MonoBehaviour
{
    [SerializeField] private ItemStorage inputStorage;
    [SerializeField] private ItemStorage outputStorage;
    [SerializeField] private Carriable outputPrefab;
    [SerializeField] private float processTime = 1.0f;

    private float timer;
    private bool isProcessing;

    private void Update()
    {
        if (!isProcessing)
        {
            TryStartProcess();
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
            CompleteProcess();
    }

    private void TryStartProcess()
    {
        if (inputStorage == null || outputStorage == null || outputPrefab == null)
            return;

        if (outputStorage.IsFull)
            return;

        if (inputStorage.Count <= 0)
            return;

        isProcessing = true;
        timer = processTime;
    }

    private void CompleteProcess()
    {
        isProcessing = false;

        if (!inputStorage.TryTakeLast(out Carriable consumed))
            return;

        consumed.Remove();

        Carriable produced = Instantiate(outputPrefab, transform.position, Quaternion.identity);
        outputStorage.TryStore(produced);
    }
}