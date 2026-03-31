using UnityEngine;

public struct InputFrame
{
    public int pointerId;
    public Vector2 screenPosition;
    public Vector2 delta;
    public Vector2 moveAxis;

    public bool pointerDown;
    public bool pointerHeld;
    public bool pointerUp;

    public bool pressedOnUI;

    public bool hasAnyInput;
}