using UnityEngine;

public class SimpleIsometricCamera : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 12f, -10f);
    [SerializeField] private float followSmoothTime = 0.15f;

    [Header("Rotation")]
    [SerializeField] private Vector3 fixedEulerAngles = new Vector3(45f, 45f, 0f);

    [Header("Focus")]
    [SerializeField] private float focusMoveSmoothTime = 0.2f;
    [SerializeField] private bool lookAtFocusTarget = false;
    [SerializeField] private Vector3 focusLookOffset = Vector3.zero;

    private Vector3 _currentVelocity;
    private Transform _focusTarget;
    private Vector3 _focusOffset;
    private bool _isFocusing;

    public Transform FollowTarget => followTarget;
    public bool IsFocusing => _isFocusing;

    private void Start()
    {
        transform.rotation = Quaternion.Euler(fixedEulerAngles);

        if (followTarget != null)
        {
            SnapToFollowTarget();
        }
    }

    private void LateUpdate()
    {
        Transform target = _isFocusing ? _focusTarget : followTarget;
        if (target == null) return;

        Vector3 offset = _isFocusing ? _focusOffset : followOffset;
        float smoothTime = _isFocusing ? focusMoveSmoothTime : followSmoothTime;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref _currentVelocity,
            smoothTime
        );

        if (_isFocusing && lookAtFocusTarget && _focusTarget != null)
        {
            Vector3 lookPoint = _focusTarget.position + focusLookOffset;
            Quaternion targetRotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
        }
        else
        {
            Quaternion targetRotation = Quaternion.Euler(fixedEulerAngles);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
        }
    }

    public void SetFollowTarget(Transform target, bool snapImmediately = false)
    {
        followTarget = target;

        if (snapImmediately && followTarget != null)
        {
            SnapToFollowTarget();
        }
    }

    public void FocusTarget(Transform target)
    {
        if (target == null) return;

        _focusTarget = target;
        _focusOffset = followOffset;
        _isFocusing = true;
    }

    public void FocusTarget(Transform target, Vector3 customOffset)
    {
        if (target == null) return;

        _focusTarget = target;
        _focusOffset = customOffset;
        _isFocusing = true;
    }

    public void ClearFocus()
    {
        _focusTarget = null;
        _isFocusing = false;
    }

    public void SnapToFollowTarget()
    {
        if (followTarget == null) return;

        transform.position = followTarget.position + followOffset;
        transform.rotation = Quaternion.Euler(fixedEulerAngles);
        _currentVelocity = Vector3.zero;
    }
}