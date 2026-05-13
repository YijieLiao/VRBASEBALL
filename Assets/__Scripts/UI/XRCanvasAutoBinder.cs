using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Awake 中自动绑定 World Space Canvas 到 XR Camera。
/// SetActive(true) 时同步执行，确保在 FadeTo 之前完成。
/// </summary>
public class XRCanvasAutoBinder : MonoBehaviour
{
    void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) return;

        Camera xrCamera = FindXRCamera();
        if (xrCamera == null) return;

        canvas.worldCamera = xrCamera;

        TrackedDeviceGraphicRaycaster raycaster = GetComponent<TrackedDeviceGraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = false;
            raycaster.enabled = true;
        }
    }

    Camera FindXRCamera()
    {
        GameObject xrOrigin = GameObject.Find("XR Origin");
        if (xrOrigin != null)
        {
            Camera cam = xrOrigin.GetComponentInChildren<Camera>(true);
            if (cam != null) return cam;
        }
        return Camera.main;
    }
}
