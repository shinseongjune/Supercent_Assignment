using System.Collections;
using UnityEngine;

public class Carriable : MonoBehaviour
{
    public enum Type
    {
        Ore,
        HandCuffs,
        Money,
    }

    public enum State
    {
        Grounded,
        Carried,
        Moving,
        Disposed,
        Removing,
    }

    public enum CarryPosition
    {
        Forward,
        Backward,
    }

    [Header("Item Info")]
    public Type type;
    public CarryPosition carryPosition = CarryPosition.Backward;

    [SerializeField] private int carryPriority = 0;
    [SerializeField] private float stackSpacing = 0.45f;
    [SerializeField] private Vector3 carriedEulerRotation = Vector3.zero;

    [SerializeField] private bool useTypeDefaultRotation = true;
    [SerializeField] private float verticalStackSpacing = 0.22f;
    
    public float VerticalStackSpacing => verticalStackSpacing;

    public State state { get; private set; } = State.Grounded;
    public int CarryPriority => carryPriority;
    public float StackSpacing => stackSpacing;
    public Vector3 CarriedEulerRotation
    {
        get
        {
            if (!useTypeDefaultRotation)
                return carriedEulerRotation;

            switch (type)
            {
                case Type.Ore:
                case Type.Money:
                    return new Vector3(0f, 90f, 0f);

                case Type.HandCuffs:
                default:
                    return carriedEulerRotation;
            }
        }
    }

    [Header("Follow")]
    [SerializeField] private float followPositionLerp = 1000f;
    [SerializeField] private float followRotationLerp = 1000f;

    [Header("MoveTo")]
    [SerializeField] private float takeMoveDuration = 0.18f;
    [SerializeField] private float releaseMoveDuration = 0.16f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Pulse")]
    [SerializeField] private float pulseScaleMultiplier = 1.5f;
    [SerializeField] private float pulseDuration = 0.12f;

    [Header("Disappear")]
    [SerializeField] private float disappearDuration = 0.15f;

    private Transform ownerTransform = null;
    private Vector3 relativeEulerRotation = Vector3.zero;
    public Vector3 relativePosition = Vector3.zero;

    private Coroutine moveCoroutine = null;
    private Coroutine pulseCoroutine = null;

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void LateUpdate()
    {
        if (ownerTransform != null && state == State.Carried)
        {
            Vector3 targetPosition = ownerTransform.TransformPoint(relativePosition);
            Quaternion targetRotation = ownerTransform.rotation * Quaternion.Euler(relativeEulerRotation);

            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                1f - Mathf.Exp(-followPositionLerp * Time.deltaTime)
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                1f - Mathf.Exp(-followRotationLerp * Time.deltaTime)
            );
        }
    }

    public void Taken(Transform owner, Vector3 relativePos, Vector3 eulerRotation, bool playPulse = false)
    {
        ownerTransform = owner;
        relativePosition = relativePos;
        relativeEulerRotation = eulerRotation;

        if (!playPulse && pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
            transform.localScale = baseScale;
        }

        StopMoveCoroutine();
        moveCoroutine = StartCoroutine(Coroutine_MoveTo(owner, relativePos, eulerRotation, State.Carried, playPulse));
    }

    public void Released(Vector3 position, Vector3 eulerRotation)
    {
        ownerTransform = null;
        relativePosition = Vector3.zero;
        relativeEulerRotation = Vector3.zero;

        StopMoveCoroutine();
        moveCoroutine = StartCoroutine(Coroutine_MoveTo(position, eulerRotation, State.Disposed));
    }

    public void SetGrounded()
    {
        ownerTransform = null;
        relativePosition = Vector3.zero;
        relativeEulerRotation = Vector3.zero;
        state = State.Grounded;
    }

    private IEnumerator Coroutine_MoveTo(Transform owner, Vector3 relativePos, Vector3 eulerRotation, State nextState, bool playPulse)
    {
        state = State.Moving;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 targetPos = owner.TransformPoint(relativePos);
        Quaternion targetRot = owner.rotation * Quaternion.Euler(eulerRotation);

        float elapsed = 0f;
        float duration = takeMoveDuration;

        while (elapsed < duration)
        {
            if (owner == null)
                yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = moveCurve.Evaluate(t);

            targetPos = owner.TransformPoint(relativePos);
            targetRot = owner.rotation * Quaternion.Euler(eulerRotation);

            transform.position = Vector3.Lerp(startPos, targetPos, curvedT);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, curvedT);

            yield return null;
        }

        transform.position = owner.TransformPoint(relativePos);
        transform.rotation = owner.rotation * Quaternion.Euler(eulerRotation);

        state = nextState;

        if (playPulse)
        {
            if (pulseCoroutine != null)
                StopCoroutine(pulseCoroutine);

            pulseCoroutine = StartCoroutine(Coroutine_Pulse());
        }

        moveCoroutine = null;
    }

    private IEnumerator Coroutine_MoveTo(Vector3 position, Vector3 eulerRotation, State nextState)
    {
        state = State.Moving;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 targetPos = position;
        Quaternion targetRot = Quaternion.Euler(eulerRotation);

        float elapsed = 0f;
        float duration = releaseMoveDuration;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = moveCurve.Evaluate(t);

            transform.position = Vector3.Lerp(startPos, targetPos, curvedT);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, curvedT);

            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;

        state = nextState;
        moveCoroutine = null;
    }

    private IEnumerator Coroutine_Pulse()
    {
        Vector3 startScale = baseScale;
        Vector3 peakScale = baseScale * pulseScaleMultiplier;

        float half = pulseDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            transform.localScale = Vector3.Lerp(startScale, peakScale, t);
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            transform.localScale = Vector3.Lerp(peakScale, startScale, t);
            yield return null;
        }

        transform.localScale = startScale;
        pulseCoroutine = null;
    }

    public void Remove()
    {
        if (state == State.Removing)
            return;

        state = State.Removing;

        StopMoveCoroutine();

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        StartCoroutine(Coroutine_Disappear());
    }

    private IEnumerator Coroutine_Disappear()
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < disappearDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / disappearDuration);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void StopMoveCoroutine()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
    }
}