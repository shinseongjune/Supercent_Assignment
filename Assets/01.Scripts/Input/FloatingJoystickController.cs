using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FloatingJoystickController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform root;
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform knob;
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float radius = 100f;
    [SerializeField] private bool hideWhenIdle = true;

    public Vector2 MoveInput { get; private set; }
    public bool IsDragging => isDragging;

    private bool isDragging;
    private int activePointerId = int.MinValue;
    private Vector2 startScreenPos;
    private Vector2 startAnchoredPos;

    private Camera UiCamera
    {
        get
        {
            if (canvas == null) return null;
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return canvas.worldCamera;
        }
    }

    private void Awake()
    {
        MoveInput = Vector2.zero;

        if (root != null && hideWhenIdle)
            root.gameObject.SetActive(false);

        // 조이스틱 UI가 입력을 막지 않도록
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        DisableRaycastTarget(background);
        DisableRaycastTarget(knob);
    }

    private void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        // InputManager가 생성될 때까지 대기
        while (InputManager.Instance == null)
            yield return null;

        InputManager.Instance.OnGameplayInput += HandleInput;
        InputManager.Instance.OnUIInput += HandleInput;

        // GameManager가 생성될 때까지 대기
        while (GameManager.Instance == null)
            yield return null;

        GameManager.Instance.OnInputModeChanged += HandleInputModeChanged;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnGameplayInput -= HandleInput;
            InputManager.Instance.OnUIInput -= HandleInput;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnInputModeChanged -= HandleInputModeChanged;
    }

    private void HandleInputModeChanged(InputMode mode)
    {
        if (mode != InputMode.Gameplay)
            ForceReset();
    }

    private void HandleInput(InputFrame input)
    {
        // 새 입력 시작은 UI 위가 아닐 때만
        if (input.pointerDown && !input.pressedOnUI && !isDragging)
        {
            Begin(input.screenPosition, input.pointerId);
            return;
        }

        // 이미 잡고 있는 포인터는 UI 위로 올라가도 계속 추적
        if (isDragging && input.pointerId == activePointerId)
        {
            if (input.pointerHeld)
            {
                Drag(input.screenPosition, input.pointerId);
                return;
            }

            if (input.pointerUp)
            {
                End(input.pointerId);
                return;
            }
        }
    }

    public void Begin(Vector2 screenPos, int pointerId)
    {
        isDragging = true;
        activePointerId = pointerId;
        startScreenPos = screenPos;
        MoveInput = Vector2.zero;

        if (root != null)
            root.gameObject.SetActive(true);

        RectTransform parentRect = root != null ? root.parent as RectTransform : null;
        if (parentRect == null)
        {
            Debug.LogWarning("FloatingJoystickController: root parent RectTransform is missing.");
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPos,
            UiCamera,
            out startAnchoredPos
        );

        if (root != null)
            root.anchoredPosition = startAnchoredPos;

        if (background != null)
            background.anchoredPosition = Vector2.zero;

        if (knob != null)
            knob.anchoredPosition = Vector2.zero;
    }

    public void Drag(Vector2 screenPos, int pointerId)
    {
        if (!isDragging || pointerId != activePointerId)
            return;

        Vector2 delta = screenPos - startScreenPos;
        Vector2 clamped = Vector2.ClampMagnitude(delta, radius);

        if (knob != null)
            knob.anchoredPosition = clamped;

        MoveInput = clamped / radius;
    }

    public void End(int pointerId)
    {
        if (!isDragging || pointerId != activePointerId)
            return;

        ForceReset();
    }

    public void ForceReset()
    {
        isDragging = false;
        activePointerId = int.MinValue;
        MoveInput = Vector2.zero;

        if (knob != null)
            knob.anchoredPosition = Vector2.zero;

        if (root != null && hideWhenIdle)
            root.gameObject.SetActive(false);
    }

    private void DisableRaycastTarget(RectTransform target)
    {
        if (target == null) return;

        var graphic = target.GetComponent<Graphic>();
        if (graphic != null)
            graphic.raycastTarget = false;
    }
}