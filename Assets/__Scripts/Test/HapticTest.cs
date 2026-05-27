using UnityEngine;
using Unity.XR.PXR;

/// <summary>
/// 手柄震动 API 对比测试
/// 手柄 A / 键盘 1 → Unity XR API  (InputDevice.SendHapticImpulse)
/// 手柄 B / 键盘 2 → PICO 原生 API  (PXR_Input.SendHapticImpulse)
/// </summary>
public class HapticTest : MonoBehaviour
{
    [Header("通用参数")]
    [Range(0.1f, 1f)]
    public float amplitude = 0.6f;
    public int durationMs = 200;

    [Header("PICO 原生 API")]
    [Range(50, 500)]
    public int frequency = 150;

    [Header("调试")]
    [SerializeField] private bool logToConsole = true;

    void Update()
    {
        var input = PicoInputManager.Instance;
        if (input == null) return;

        if (input.AButtonDown)
            TestUnityXR();

        if (input.BButtonDown)
            TestPicoNative();
    }

    private void TestUnityXR()
    {
        if (logToConsole)
            Debug.Log($"[HapticTest] UnityXR: amp={amplitude}, dur={durationMs}ms");
        PicoInputManager.Instance.VibrateRight(amplitude, durationMs);
    }

    private void TestPicoNative()
    {
        if (logToConsole)
            Debug.Log($"[HapticTest] PICO Native: amp={amplitude}, dur={durationMs}ms, freq={frequency}Hz");
        PXR_Input.SendHapticImpulse(PXR_Input.VibrateType.RightController, amplitude, durationMs, frequency);
    }
}
