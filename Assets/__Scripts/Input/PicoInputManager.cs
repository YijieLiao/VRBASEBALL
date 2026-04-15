using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// PICO 手柄输入管理器
/// 使用 Unity 标准 XR Input System，兼容 PICO
/// </summary>
public class PicoInputManager : MonoBehaviour
{
    public static PicoInputManager Instance { get; private set; }

    [Header("摇杆死区")]
    [Range(0.1f, 0.5f)]
    public float stickDeadzone = 0.2f;

    // 设备缓存
    private InputDevice leftController;
    private InputDevice rightController;

    // 上一帧按钮状态（用于检测按下/抬起）
    private HashSet<string> prevPressedButtons = new HashSet<string>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        RefreshDevices();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        // 如果设备断开，尝试重新获取
        if (!leftController.isValid || !rightController.isValid)
        {
            RefreshDevices();
        }

        // 更新上一帧状态
        prevPressedButtons.Clear();
        foreach (var btn in GetCurrentPressedButtons())
        {
            prevPressedButtons.Add(btn);
        }
    }

    private void RefreshDevices()
    {
        var leftHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left,
            leftHandDevices);

        var rightHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right,
            rightHandDevices);

        if (leftHandDevices.Count > 0)
            leftController = leftHandDevices[0];

        if (rightHandDevices.Count > 0)
            rightController = rightHandDevices[0];
    }

    #region 设备状态

    public bool LeftControllerConnected => leftController.isValid;
    public bool RightControllerConnected => rightController.isValid;

    #endregion

    #region 扳机 (Trigger)

    public float LeftTrigger => GetAxis(leftController, CommonUsages.trigger);
    public float RightTrigger => GetAxis(rightController, CommonUsages.trigger);
    public bool LeftTriggerPressed => LeftTrigger > 0.1f;
    public bool RightTriggerPressed => RightTrigger > 0.1f;

    #endregion

    #region 握柄 (Grip)

    public float LeftGrip => GetAxis(leftController, CommonUsages.grip);
    public float RightGrip => GetAxis(rightController, CommonUsages.grip);
    public bool LeftGripPressed => LeftGrip > 0.1f;
    public bool RightGripPressed => RightGrip > 0.1f;

    #endregion

    #region 摇杆 (Thumbstick)

    public Vector2 LeftStick => GetAxis2D(leftController, CommonUsages.primary2DAxis);
    public Vector2 RightStick => GetAxis2D(rightController, CommonUsages.primary2DAxis);
    public bool LeftStickMoved => LeftStick.magnitude > stickDeadzone;
    public bool RightStickMoved => RightStick.magnitude > stickDeadzone;

    public bool LeftStickButton => GetButton(leftController, CommonUsages.primary2DAxisClick);
    public bool RightStickButton => GetButton(rightController, CommonUsages.primary2DAxisClick);

    public bool LeftStickButtonDown => GetButtonDown(leftController, CommonUsages.primary2DAxisClick, "L_Stick");
    public bool RightStickButtonDown => GetButtonDown(rightController, CommonUsages.primary2DAxisClick, "R_Stick");

    #endregion

    #region 按钮 (A/B/X/Y)

    // 右手按钮
    public bool AButton => GetButton(rightController, CommonUsages.primaryButton);
    public bool BButton => GetButton(rightController, CommonUsages.secondaryButton);
    public bool AButtonDown => GetButtonDown(rightController, CommonUsages.primaryButton, "A");
    public bool BButtonDown => GetButtonDown(rightController, CommonUsages.secondaryButton, "B");

    // 左手按钮
    public bool XButton => GetButton(leftController, CommonUsages.primaryButton);
    public bool YButton => GetButton(leftController, CommonUsages.secondaryButton);
    public bool XButtonDown => GetButtonDown(leftController, CommonUsages.primaryButton, "X");
    public bool YButtonDown => GetButtonDown(leftController, CommonUsages.secondaryButton, "Y");

    // 菜单按钮
    public bool LeftMenu => GetButton(leftController, CommonUsages.menuButton);
    public bool RightMenu => GetButton(rightController, CommonUsages.menuButton);
    public bool LeftMenuDown => GetButtonDown(leftController, CommonUsages.menuButton, "MenuL");
    public bool RightMenuDown => GetButtonDown(rightController, CommonUsages.menuButton, "MenuR");

    #endregion

    #region 震动反馈

    /// <summary>
    /// 触发手柄震动
    /// </summary>
    public void Vibrate(bool left = true, bool right = true, float amplitude = 0.5f, int durationMs = 100)
    {
        if (left && leftController.isValid)
            leftController.SendHapticImpulse(0, amplitude, durationMs / 1000f);

        if (right && rightController.isValid)
            rightController.SendHapticImpulse(0, amplitude, durationMs / 1000f);
    }

    public void VibrateLeft(float amplitude = 0.5f, int durationMs = 100) => Vibrate(true, false, amplitude, durationMs);
    public void VibrateRight(float amplitude = 0.5f, int durationMs = 100) => Vibrate(false, true, amplitude, durationMs);

    #endregion

    #region 底层辅助

    private float GetAxis(InputDevice device, InputFeatureUsage<float> usage)
    {
        if (!device.isValid) return 0f;
        device.TryGetFeatureValue(usage, out float value);
        return value;
    }

    private Vector2 GetAxis2D(InputDevice device, InputFeatureUsage<Vector2> usage)
    {
        if (!device.isValid) return Vector2.zero;
        device.TryGetFeatureValue(usage, out Vector2 value);
        return value;
    }

    private bool GetButton(InputDevice device, InputFeatureUsage<bool> usage)
    {
        if (!device.isValid) return false;
        device.TryGetFeatureValue(usage, out bool value);
        return value;
    }

    private bool GetButtonDown(InputDevice device, InputFeatureUsage<bool> usage, string buttonId)
    {
        bool currentlyPressed = GetButton(device, usage);
        bool wasPressed = prevPressedButtons.Contains(buttonId);
        return currentlyPressed && !wasPressed;
    }

    private List<string> GetCurrentPressedButtons()
    {
        var list = new List<string>();
        if (LeftStickButton) list.Add("L_Stick");
        if (RightStickButton) list.Add("R_Stick");
        if (AButton) list.Add("A");
        if (BButton) list.Add("B");
        if (XButton) list.Add("X");
        if (YButton) list.Add("Y");
        if (LeftMenu) list.Add("MenuL");
        if (RightMenu) list.Add("MenuR");
        return list;
    }

    #endregion
}
