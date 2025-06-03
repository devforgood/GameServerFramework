using UnityEngine;

/// <summary>
/// 입력 이벤트의 기본 클래스
/// </summary>
public abstract class InputEvent
{
    public float Timestamp { get; private set; }

    protected InputEvent()
    {
        Timestamp = Time.time;
    }
}

/// <summary>
/// 마우스 입력 이벤트
/// </summary>
public class MouseInputEvent : InputEvent
{
    public Vector3 HitPoint { get; private set; }
    public bool IsHit { get; private set; }
    public string HitTag { get; private set; }
    public RaycastHit? HitInfo { get; private set; }
    public int Button { get; private set; }
    public bool IsPressed { get; private set; }
    public bool IsCtrlPressed { get; private set; }
    public bool IsAltPressed { get; private set; }
    public bool IsShiftPressed { get; private set; }

    public MouseInputEvent(
        Vector3 hitPoint,
        bool isHit,
        string hitTag,
        RaycastHit? hitInfo,
        int button,
        bool isPressed,
        bool isCtrlPressed,
        bool isAltPressed,
        bool isShiftPressed)
    {
        HitPoint = hitPoint;
        IsHit = isHit;
        HitTag = hitTag;
        HitInfo = hitInfo;
        Button = button;
        IsPressed = isPressed;
        IsCtrlPressed = isCtrlPressed;
        IsAltPressed = isAltPressed;
        IsShiftPressed = isShiftPressed;
    }
}

/// <summary>
/// 키보드 입력 이벤트
/// </summary>
public class KeyboardInputEvent : InputEvent
{
    public KeyCode Key { get; private set; }
    public bool IsPressed { get; private set; }
    public Vector3 HitPoint { get; private set; }
    public bool IsHit { get; private set; }
    public string HitTag { get; private set; }

    public KeyboardInputEvent(Vector3 hitPoint, bool isHit, string hitTag, KeyCode key, bool isPressed)
    {
        HitPoint = hitPoint;
        IsHit = isHit;
        HitTag = hitTag;
        Key = key;
        IsPressed = isPressed;
    }
} 