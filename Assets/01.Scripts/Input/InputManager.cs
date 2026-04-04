using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public event Action<InputFrame> OnGameplayInput;
    public event Action<InputFrame> OnUIInput;
    public event Action<InputFrame> OnModalInput;

    public event Action<float> OnIdleTimeChanged;
    public event Action OnBecameIdle;
    public event Action OnExitIdle;

    [SerializeField] private float idleThreshold = 4f;

    private Vector2 prevMousePosition;
    private float idleTime;
    private bool isIdle;

    private void Awake()
    {
        Instance = this;
        prevMousePosition = Input.mousePosition;
    }

    private void Update()
    {
        InputFrame input = CollectInput();

        switch (GameManager.Instance.CurrentInputMode)
        {
            case InputMode.Gameplay:
                UpdateIdle(input);
                RouteGameplay(input);
                break;

            case InputMode.Modal:
                OnModalInput?.Invoke(input);
                break;

            case InputMode.Cutscene:
            case InputMode.Disabled:
                break;
        }

        prevMousePosition = Input.mousePosition;
    }

    private void RouteGameplay(InputFrame input)
    {
        if (input.pressedOnUI)
        {
            OnUIInput?.Invoke(input);
            return;
        }

        OnGameplayInput?.Invoke(input);
    }

    private InputFrame CollectInput()
    {
        InputFrame frame = new InputFrame();

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            frame.pointerId = touch.fingerId;
            frame.screenPosition = touch.position;
            frame.delta = touch.deltaPosition;
            frame.pointerDown = touch.phase == TouchPhase.Began;
            frame.pointerHeld = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
            frame.pointerUp = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            frame.pressedOnUI = EventSystem.current != null &&
                                EventSystem.current.IsPointerOverGameObject(touch.fingerId);

            frame.hasAnyInput = true;
        }
        else
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 mouseDelta = mousePos - prevMousePosition;

            frame.pointerId = -1;
            frame.screenPosition = mousePos;
            frame.delta = mouseDelta;
            frame.pointerDown = Input.GetMouseButtonDown(0);
            frame.pointerHeld = Input.GetMouseButton(0);
            frame.pointerUp = Input.GetMouseButtonUp(0);
            frame.pressedOnUI = EventSystem.current != null &&
                                EventSystem.current.IsPointerOverGameObject();

            frame.hasAnyInput =
                frame.pointerDown ||
                frame.pointerHeld ||
                frame.pointerUp;
        }

        return frame;
    }

    private void UpdateIdle(InputFrame input)
    {
        if (input.hasAnyInput)
        {
            idleTime = 0f;

            if (isIdle)
            {
                isIdle = false;
                OnExitIdle?.Invoke();
            }
        }
        else
        {
            idleTime += Time.deltaTime;
            OnIdleTimeChanged?.Invoke(idleTime);

            if (!isIdle && idleTime >= idleThreshold)
            {
                isIdle = true;
                OnBecameIdle?.Invoke();
            }
        }
    }
}