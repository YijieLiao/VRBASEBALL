using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRBaseInteractable))]
public class InteractableHintTrigger : MonoBehaviour
{
    private XRBaseInteractable interactable;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
    }

    void OnEnable()
    {
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
    }

    void OnDisable()
    {
        interactable.hoverEntered.RemoveListener(OnHoverEntered);
        interactable.hoverExited.RemoveListener(OnHoverExited);
    }

    private static GameObject GetHintContents()
    {
        var xrOrigin = GameObject.Find("XR Origin");
        if (xrOrigin == null) return null;

        var container = xrOrigin.transform.Find("Camera Offset/Main Camera/UI_SubtitleCanvas/HintContents");
        return container != null ? container.gameObject : null;
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        var hint = GetHintContents();
        if (hint != null)
            SubtitleHintDisplay.ShowHint(hint);
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        SubtitleHintDisplay.HideHint();
    }
}
