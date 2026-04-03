using System.Collections;
using UnityEngine;

public class MaxPopupBillboard : MonoBehaviour
{
    [SerializeField] GameObject m_billboard;
    [SerializeField] private float defaultDuration = 1.0f;
    [SerializeField] private float forwardOffsetToCamera = 0.3f;

    private Camera targetCamera;
    private Transform followTarget;
    private Vector3 worldOffset;
    private Coroutine hideRoutine;

    private void Awake()
    {
        targetCamera = Camera.main;
        HideImmediate();
    }

    private void LateUpdate()
    {
        if (followTarget == null)
            return;

        if (targetCamera == null)
            targetCamera = Camera.main;

        Vector3 pos = followTarget.position + worldOffset;

        if (targetCamera != null)
        {
            Vector3 toCamera = (targetCamera.transform.position - pos).normalized;
            pos += toCamera * forwardOffsetToCamera;

            Vector3 lookDir = m_billboard.transform.position - targetCamera.transform.position;
            if (lookDir.sqrMagnitude > 0.0001f)
                m_billboard.transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        }

        transform.position = pos;
    }

    public void ShowForSeconds(Transform target, Vector3 offset, float duration = -1f)
    {
        followTarget = target;
        worldOffset = offset;

        m_billboard.SetActive(true);

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(HideAfter(duration > 0f ? duration : defaultDuration));
    }

    public void HideImmediate()
    {
        followTarget = null;

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        m_billboard.SetActive(false);
    }

    private IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        m_billboard.SetActive(false);

        followTarget = null;
        hideRoutine = null;
    }
}