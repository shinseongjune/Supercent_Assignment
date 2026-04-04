using UnityEngine;

public class TutorialArrowController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform player;
    [SerializeField] private SimpleIsometricCamera isoCamera;

    [Header("Target")]
    [SerializeField] private Transform currentTarget;

    [Header("World Placement")]
    [SerializeField] private float worldHeightOffset = 3.5f;
    [SerializeField] private float bounceAmplitude = 0.35f;
    [SerializeField] private float bounceSpeed = 3f;

    [Header("Offscreen Fallback")]
    [SerializeField] private float playerFallbackDistance = 4.5f;
    [SerializeField] private float playerFallbackHeight = 3f;
    [SerializeField] private bool clampToPlayerPlane = true;

    [Header("View Check")]
    [SerializeField] private float viewportMargin = 0.05f;

    [Header("Rotation")]
    [SerializeField] private bool alignToCameraYaw = true;
    [SerializeField] private Vector3 additionalEuler = Vector3.zero;

    private bool hasTarget;
    private Vector3 basePosition;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (isoCamera == null && targetCamera != null)
            isoCamera = targetCamera.GetComponent<SimpleIsometricCamera>();

        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!hasTarget || currentTarget == null || targetCamera == null)
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
            return;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        bool isVisible = IsTargetInView(currentTarget.position);

        if (isVisible)
        {
            basePosition = currentTarget.position + Vector3.up * worldHeightOffset;
        }
        else
        {
            basePosition = GetOffscreenFallbackPosition();
        }

        float bounce = Mathf.Sin(Time.time * bounceSpeed) * bounceAmplitude;
        transform.position = basePosition + Vector3.up * bounce;

        UpdateRotation();
    }

    public void SetTarget(Transform target)
    {
        currentTarget = target;
        hasTarget = target != null;
        gameObject.SetActive(hasTarget);
    }

    public void ClearTarget()
    {
        currentTarget = null;
        hasTarget = false;
        gameObject.SetActive(false);
    }

    private bool IsTargetInView(Vector3 worldPos)
    {
        Vector3 viewport = targetCamera.WorldToViewportPoint(worldPos);

        if (viewport.z <= 0f)
            return false;

        if (viewport.x < viewportMargin || viewport.x > 1f - viewportMargin)
            return false;

        if (viewport.y < viewportMargin || viewport.y > 1f - viewportMargin)
            return false;

        return true;
    }

    private Vector3 GetOffscreenFallbackPosition()
    {
        if (player == null || currentTarget == null)
        {
            return currentTarget != null
                ? currentTarget.position + Vector3.up * worldHeightOffset
                : transform.position;
        }

        Vector3 fromPlayerToTarget = currentTarget.position - player.position;

        if (clampToPlayerPlane)
            fromPlayerToTarget.y = 0f;

        if (fromPlayerToTarget.sqrMagnitude < 0.0001f)
            fromPlayerToTarget = targetCamera.transform.forward;

        Vector3 dir = fromPlayerToTarget.normalized;

        Vector3 pos = player.position + dir * playerFallbackDistance;
        pos.y += playerFallbackHeight;

        return pos;
    }

    private void UpdateRotation()
    {
        if (!alignToCameraYaw || targetCamera == null)
        {
            transform.rotation = Quaternion.Euler(additionalEuler);
            return;
        }

        Vector3 camForward = targetCamera.transform.forward;
        camForward.y = 0f;

        if (camForward.sqrMagnitude < 0.0001f)
            camForward = Vector3.forward;

        Quaternion lookRot = Quaternion.LookRotation(camForward.normalized, Vector3.up);
        transform.rotation = lookRot * Quaternion.Euler(additionalEuler);
    }
}