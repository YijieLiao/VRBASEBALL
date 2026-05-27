using EPOOutline;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// 挂到带 XRGrabInteractable + Outlinable 的物体上。射线瞄准和抓取时自动显示描边。
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Outlinable))]
public class GrabOutline : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    private Outlinable outlinable;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        outlinable = GetComponent<Outlinable>();
        outlinable.OutlineParameters.Enabled = false;
    }

    void OnEnable()
    {
        grabInteractable.hoverEntered.AddListener(OnHoverEntered);
        grabInteractable.hoverExited.AddListener(OnHoverExited);
        grabInteractable.selectEntered.AddListener(OnSelectEntered);
        grabInteractable.selectExited.AddListener(OnSelectExited);
    }

    void OnDisable()
    {
        grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
        grabInteractable.hoverExited.RemoveListener(OnHoverExited);
        grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
        grabInteractable.selectExited.RemoveListener(OnSelectExited);
    }

    private void OnHoverEntered(HoverEnterEventArgs args) => outlinable.OutlineParameters.Enabled = true;

    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (!grabInteractable.isSelected)
            outlinable.OutlineParameters.Enabled = false;
    }

    private void OnSelectEntered(SelectEnterEventArgs args) => outlinable.OutlineParameters.Enabled = true;

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (!grabInteractable.isHovered)
            outlinable.OutlineParameters.Enabled = false;
    }
}
