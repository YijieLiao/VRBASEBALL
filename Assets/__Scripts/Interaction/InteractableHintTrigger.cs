using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// 挂在任何 XRBaseInteractable（XRGrabInteractable / XRSimpleInteractable）物体上。
/// 当玩家射线瞄准该物体时，通知 SubtitleHintDisplay 显示对应的提示内容。
/// </summary>
[RequireComponent(typeof(XRBaseInteractable))]
public class InteractableHintTrigger : MonoBehaviour
{
    [Header("提示内容")]
    [Tooltip("拖入你在 UI_SubtitleCanvas 下配置好的提示 GameObject（含 TMP 文字 + Image）。")]
    [SerializeField] private GameObject hintContent;

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

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (hintContent != null && SubtitleHintDisplay.Instance != null)
            SubtitleHintDisplay.Instance.Show(hintContent);
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (SubtitleHintDisplay.Instance != null)
            SubtitleHintDisplay.Instance.Hide();
    }
}
