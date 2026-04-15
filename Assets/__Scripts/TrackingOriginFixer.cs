using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// 自动修正 XR 跟踪原点，确保打包后视角高度正确
/// 挂载到 XR Origin 上
/// </summary>
public class TrackingOriginFixer : MonoBehaviour
{
    [Header("跟踪原点设置")]
    [Tooltip("期望的跟踪原点模式")]
    public XROrigin.TrackingOriginMode trackingOriginMode = XROrigin.TrackingOriginMode.Floor;

    [Tooltip("如果设为 Device 模式，Y 轴偏移量（米）")]
    public float deviceYOffset = 1.6f;

    [Header("调试")]
    [Tooltip("是否在启动时强制应用设置")]
    public bool forceOnStart = true;

    private XROrigin xrOrigin;

    void Awake()
    {
        xrOrigin = GetComponent<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("[TrackingOriginFixer] 未找到 XROrigin 组件！");
            return;
        }
    }

    void Start()
    {
        if (forceOnStart)
        {
            ApplyTrackingOrigin();
        }

        // 输出当前相机高度用于调试
        var camera = Camera.main;
        if (camera != null)
        {
            Debug.Log($"[TrackingOriginFixer] 当前相机高度: {camera.transform.position.y}m");
        }
    }

    [ContextMenu("应用跟踪原点设置")]
    public void ApplyTrackingOrigin()
    {
        if (xrOrigin == null) return;

        xrOrigin.RequestedTrackingOriginMode = trackingOriginMode;

        if (trackingOriginMode == XROrigin.TrackingOriginMode.Device)
        {
            xrOrigin.CameraYOffset = deviceYOffset;
        }

        Debug.Log($"[TrackingOriginFixer] 已设置 TrackingOriginMode: {trackingOriginMode}");
    }

    // 运行时按键修正（测试用）
    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.R))
        {
            ApplyTrackingOrigin();
        }
#endif
    }
}
