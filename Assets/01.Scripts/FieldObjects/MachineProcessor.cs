using System.Collections;
using UnityEngine;

// 아이템을 다른 아이템으로 변환
public class MachineProcessor : MonoBehaviour
{
    [SerializeField] private ItemStorage inputStorage;
    [SerializeField] private ItemStorage outputStorage;
    [SerializeField] private Carriable outputPrefab;
    [SerializeField] private float processTime = 1.0f;

    private Coroutine processCoroutine;

    private void Update()
    {
        if (processCoroutine == null)
            TryStartProcess();
    }

    private void TryStartProcess()
    {
        if (inputStorage == null || outputStorage == null || outputPrefab == null)
            return;

        if (inputStorage.Count <= 0)
            return;

        if (outputStorage.IsFull)
            return;

        processCoroutine = StartCoroutine(ProcessRoutine());
    }

    private IEnumerator ProcessRoutine()
    {
        yield return new WaitForSeconds(processTime);

        // 완료 시점에 다시 체크
        if (outputStorage == null || outputStorage.IsFull)
        {
            processCoroutine = null;
            yield break;
        }

        if (!inputStorage.TryTakeLast(out Carriable consumed))
        {
            processCoroutine = null;
            yield break;
        }

        consumed.Remove();

        Carriable produced = Instantiate(outputPrefab, transform.position, Quaternion.identity);

        if (!outputStorage.TryStore(produced))
        {
            produced.Remove(); // 또는 Destroy(produced.gameObject);
        }

        processCoroutine = null;
    }
}