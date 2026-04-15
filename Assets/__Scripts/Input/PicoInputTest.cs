using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// PICO 输入测试脚本
/// 支持两种显示模式：World Space Canvas (VR内可见) 或 OnGUI (编辑器调试用)
/// </summary>
public class PicoInputTest : MonoBehaviour
{
    public enum DisplayMode { WorldSpaceCanvas, OnGUI }

    [Header("显示模式")]
    [Tooltip("WorldSpaceCanvas = VR内可见的Canvas文本\nOnGUI = 编辑器2D叠加（VR内看不到）")]
    public DisplayMode displayMode = DisplayMode.WorldSpaceCanvas;

    [Header("World Space Canvas 显示")]
    public TextMeshProUGUI tmpDisplay;      // TextMeshPro 版本
    public Text uiTextDisplay;              // 普通 UI Text 版本（TMP没有时用）

    [Header("OnGUI 显示（仅编辑器）")]
    public bool showOnGUI = false;          // 是否在 VR 中也显示 OnGUI
    public Rect guiPosition = new Rect(10, 10, 300, 400);

    [Header("震动测试")]
    public bool vibrateOnTrigger = true;
    public bool vibrateOnGrip = true;
    public float triggerVibrationStrength = 0.2f;
    public float gripVibrationStrength = 0.3f;

    [Header("调试输出")]
    public bool logToDebug = true;

    void Update()
    {
        if (PicoInputManager.Instance == null)
        {
            UpdateDisplay("PicoInputManager not found");
            return;
        }

        var input = PicoInputManager.Instance;

        // 构建状态字符串
        string status = BuildStatusString(input);
        UpdateDisplay(status);

        // 震动反馈测试
        HandleVibration(input);

        // 按钮事件日志
        LogButtonEvents(input);
    }

    private void UpdateDisplay(string text)
    {
        // World Space Canvas 模式
        if (displayMode == DisplayMode.WorldSpaceCanvas)
        {
            if (tmpDisplay != null)
                tmpDisplay.text = text;
            else if (uiTextDisplay != null)
                uiTextDisplay.text = text;
        }
    }

    private string BuildStatusString(PicoInputManager input)
    {
        string s = "";

        // 连接状态
        s += $"Left: {(input.LeftControllerConnected ? "ON" : "--")}  ";
        s += $"Right: {(input.RightControllerConnected ? "ON" : "--")}\n\n";

        // 扳机
        s += $"L-Trigger: {input.LeftTrigger:F2}\n";
        s += $"R-Trigger: {input.RightTrigger:F2}\n\n";

        // 握柄
        s += $"L-Grip: {input.LeftGrip:F2}\n";
        s += $"R-Grip: {input.RightGrip:F2}\n\n";

        // 摇杆
        s += $"L-Stick: {input.LeftStick.x:F2}, {input.LeftStick.y:F2}\n";
        s += $"R-Stick: {input.RightStick.x:F2}, {input.RightStick.y:F2}\n\n";

        // 按钮状态
        s += "Buttons: ";
        if (input.AButton) s += "A ";
        if (input.BButton) s += "B ";
        if (input.XButton) s += "X ";
        if (input.YButton) s += "Y ";
        if (input.LeftStickButton) s += "L3 ";
        if (input.RightStickButton) s += "R3 ";
        if (input.LeftMenu) s += "MenuL ";
        if (input.RightMenu) s += "MenuR ";

        return s;
    }

    private void HandleVibration(PicoInputManager input)
    {
        // 扳机震动
        if (vibrateOnTrigger)
        {
            if (input.LeftTrigger > 0.5f && input.LeftTriggerPressed)
                input.VibrateLeft(triggerVibrationStrength, 50);

            if (input.RightTrigger > 0.5f && input.RightTriggerPressed)
                input.VibrateRight(triggerVibrationStrength, 50);
        }

        // 握柄震动（按压时）
        if (vibrateOnGrip)
        {
            if (input.LeftGripPressed && input.LeftGrip > 0.8f)
                input.VibrateLeft(gripVibrationStrength, 100);

            if (input.RightGripPressed && input.RightGrip > 0.8f)
                input.VibrateRight(gripVibrationStrength, 100);
        }
    }

    private void LogButtonEvents(PicoInputManager input)
    {
        if (!logToDebug) return;

        if (input.AButtonDown) Debug.Log("[PicoInput] A Button Pressed");
        if (input.BButtonDown) Debug.Log("[PicoInput] B Button Pressed");
        if (input.XButtonDown) Debug.Log("[PicoInput] X Button Pressed");
        if (input.YButtonDown) Debug.Log("[PicoInput] Y Button Pressed");
        if (input.LeftStickButtonDown) Debug.Log("[PicoInput] Left Stick Pressed");
        if (input.RightStickButtonDown) Debug.Log("[PicoInput] Right Stick Pressed");
        if (input.LeftMenuDown) Debug.Log("[PicoInput] Left Menu Pressed");
        if (input.RightMenuDown) Debug.Log("[PicoInput] Right Menu Pressed");
    }

    // OnGUI 显示（编辑器或强制开启时使用）
    void OnGUI()
    {
        // 只在 Editor 模式或强制开启时显示
#if UNITY_EDITOR
        bool shouldShow = true;
#else
        bool shouldShow = showOnGUI;
#endif

        if (!shouldShow) return;
        if (PicoInputManager.Instance == null) return;

        var input = PicoInputManager.Instance;
        int y = (int)guiPosition.y;
        int h = 20;
        int width = (int)guiPosition.width;

        GUI.Box(guiPosition, "");

        GUI.Label(new Rect(guiPosition.x + 10, y, width - 20, h), $"L-Trigger: {input.LeftTrigger:F2}"); y += h;
        GUI.Label(new Rect(guiPosition.x + 10, y, width - 20, h), $"R-Trigger: {input.RightTrigger:F2}"); y += h;
        GUI.Label(new Rect(guiPosition.x + 10, y, width - 20, h), $"L-Grip: {input.LeftGrip:F2}"); y += h;
        GUI.Label(new Rect(guiPosition.x + 10, y, width - 20, h), $"R-Grip: {input.RightGrip:F2}"); y += h;
        y += 5;

        GUI.Label(new Rect(guiPosition.x + 10, y, width - 20, h), $"L-Stick: {input.LeftStick.x:F2}, {input.LeftStick.y:F2}"); y += h;
        GUI.Label(new Rect(guiPosition.x + 10, y, width - 20, h), $"R-Stick: {input.RightStick.x:F2}, {input.RightStick.y:F2}"); y += h;
        y += 5;

        string buttons = "";
        if (input.AButton) buttons += "A ";
        if (input.BButton) buttons += "B ";
        if (input.XButton) buttons += "X ";
        if (input.YButton) buttons += "Y ";
        if (input.LeftStickButton) buttons += "L3 ";
        if (input.RightStickButton) buttons += "R3 ";
        if (input.LeftMenu) buttons += "MenuL ";
        if (input.RightMenu) buttons += "MenuR ";

        GUI.Label(new Rect(guiPosition.x + 10, y, width - 20, h), $"Buttons: {buttons}"); y += h;
        GUI.Label(new Rect(guiPosition.x + 10, y, width - 20, h), $"Connected: L={input.LeftControllerConnected} R={input.RightControllerConnected}");
    }
}
