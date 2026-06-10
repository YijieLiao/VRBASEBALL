using UnityEngine;

/// <summary>
/// 挂在 UI_SubtitleCanvas 上。单例，管理所有 InteractableHintTrigger 请求的提示显示。
/// 配合现有的 CanvasGroupFader 做淡入淡出。
/// </summary>
[RequireComponent(typeof(CanvasGroupFader))]
public class SubtitleHintDisplay : MonoBehaviour
{
    public static SubtitleHintDisplay Instance { get; private set; }

    [SerializeField] private CanvasGroupFader fader;

    private GameObject currentHint;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (fader == null)
            fader = GetComponent<CanvasGroupFader>();

        // 不能让 CanvasGroupFader 在 fade out 后把整个 Canvas deactivate，
        // 因为下次 hover 还需要用。由本脚本手动管理 hint GO 的激活状态。
        fader.deactivateOnHide = false;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Show(GameObject hintContent)
    {
        if (hintContent == null)
            return;

        // 如果已有其他 hint 在显示，先隐藏
        if (currentHint != null && currentHint != hintContent)
            currentHint.SetActive(false);

        currentHint = hintContent;
        hintContent.SetActive(true);
        fader.FadeIn();
    }

    public void Hide()
    {
        if (currentHint == null)
            return;

        var hintToDeactivate = currentHint;
        currentHint = null;
        fader.FadeOut(() => hintToDeactivate.SetActive(false));
    }
}
