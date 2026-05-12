using System.Collections;
using TMPro;
using UnityEngine;
using VRKeyboard.Utils;

public class NameInputPanel : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text[] slots;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private KeyboardManager keyboardManager;

    [Header("按钮")]
    [SerializeField] private UnityEngine.UI.Button confirmButton;
    [SerializeField] private UnityEngine.UI.Button skipButton;

    [Header("动画")]
    [SerializeField] private float fadeDuration = 0.3f;

    public event System.Action<string> OnNameConfirmed;

    private string lastInputText = "";
    private bool isVisible;
    private Coroutine fadeRoutine;

    void Start()
    {
        // 按钮事件在 Inspector 里绑定，代码不重复 AddListener（否则双击触发两次）

        if (keyboardManager != null)
            keyboardManager.OnSubmit += OnSubmit;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    void OnDestroy()
    {
        if (keyboardManager != null)
            keyboardManager.OnSubmit -= OnSubmit;
    }

    void Update()
    {
        if (!isVisible || keyboardManager == null) return;

        string current = keyboardManager.inputText.text;
        if (current != lastInputText)
        {
            lastInputText = current;
            RefreshSlots();
        }
    }

    public void Show(string prompt)
    {
        if (promptText != null)
            promptText.text = prompt;
        if (keyboardManager != null)
        {
            keyboardManager.Clear();
            keyboardManager.SetInputText("");
        }
        lastInputText = "";
        RefreshSlots();
        isVisible = true;
        FadeTo(1f);
    }

    public void Hide()
    {
        isVisible = false;
        FadeTo(0f);
    }

    void OnSubmit(string name)
    {
        Hide();
        OnNameConfirmed?.Invoke(name);
    }

    public void OnSkipClicked()
    {
        Hide();
        OnNameConfirmed?.Invoke("Player");
    }

    void RefreshSlots()
    {
        string current = keyboardManager != null ? keyboardManager.inputText.text : "";
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            slots[i].text = i < current.Length ? current[i].ToString() : "_";
        }
    }

    void FadeTo(float target)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(target));
    }

    IEnumerator FadeRoutine(float target)
    {
        if (target > 0f)
        {
            // 淡入：先开交互，再渐显
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        float start = canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = target;

        if (target <= 0f)
        {
            // 淡出：渐隐结束后关闭交互
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}
