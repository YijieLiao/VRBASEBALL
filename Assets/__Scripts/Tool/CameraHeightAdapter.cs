using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// 自动适配 Camera Y Offset：
/// - Editor/PLP 模式下用 editorOffset（PLP 自行叠加追踪高度）
/// - 打包到设备后用 deviceOffset（设备需要手动设置真实眼高）
/// 挂在 XR Origin 所在的 GameObject 上
/// </summary>
public class CameraHeightAdapter : MonoBehaviour
{
    [Header("编辑器/PLP 模式")]
    [Tooltip("PLP 会自己叠加 HMD 追踪高度，这里给 0 或很小的值即可")]
    public float editorOffset = 0f;

    [Header("设备打包模式")]
    [Tooltip("设备上追踪坐标系在脚下，需要设为人眼高度")]
    public float deviceOffset = 1.75f;

    private XROrigin xrOrigin;

    void Awake()
    {
        xrOrigin = GetComponent<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("CameraHeightAdapter: 需要挂在有 XROrigin 组件的 GameObject 上", this);
            return;
        }

        ApplyOffset();
    }

    private void ApplyOffset()
    {
#if UNITY_EDITOR
        xrOrigin.CameraYOffset = editorOffset;
        Debug.Log($"[CameraHeightAdapter] Editor/PLP mode: CameraYOffset = {editorOffset}");
#else
        xrOrigin.CameraYOffset = deviceOffset;
        Debug.Log($"[CameraHeightAdapter] Device build: CameraYOffset = {deviceOffset}");
#endif
    }
}
