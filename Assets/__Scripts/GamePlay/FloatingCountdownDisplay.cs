using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 挂在 AnimalPitcher GameObject 上。
/// 从 ClassicModeRoundManager 的 OnCountdownTick 事件驱动，
/// 在动物头顶显示倒数文字（3-2-1-准备击球）。
///
/// FloatingText 子物体的 TextMeshPro 组件可直接在 Inspector 中配置字体/大小/颜色。
/// 运行时文字会自动挂到动物实例下跟随移动。
/// </summary>
public class FloatingCountdownDisplay : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private ClassicModeRoundManager roundManager;
    [SerializeField] private AnimalPitcher animalPitcher;
    [SerializeField] private TextMeshPro floatingText;

    [Header("头顶偏移")]
    [SerializeField] private float heightAboveHead = 0.6f;

    [Header("文案")]
    [SerializeField] private string countdownText3 = "3";
    [SerializeField] private string countdownText2 = "2";
    [SerializeField] private string countdownText1 = "1";
    [SerializeField] private string readyText = "准备击球";

    [Header("动画（秒）")]
    [SerializeField] private float popInTime = 0.08f;
    [SerializeField] private float holdTime = 0.27f;
    [SerializeField] private float fadeOutTime = 0.15f;
    [SerializeField] private float floatUpDistance = 0.3f;
    [SerializeField] private float popMaxScale = 1.2f;

    private Camera playerCamera;
    private Coroutine animRoutine;
    private bool attached;

    void Awake()
    {
        if (roundManager == null) roundManager = FindObjectOfType<ClassicModeRoundManager>();
        if (animalPitcher == null) animalPitcher = GetComponent<AnimalPitcher>();
        if (floatingText != null) floatingText.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (roundManager != null) roundManager.OnCountdownTick += OnCountdownTick;
    }

    void OnDisable()
    {
        if (roundManager != null) roundManager.OnCountdownTick -= OnCountdownTick;
        if (animRoutine != null) StopCoroutine(animRoutine);
    }

    void LateUpdate()
    {
        if (floatingText != null && floatingText.gameObject.activeSelf)
        {
            if (playerCamera == null) playerCamera = Camera.main;
            if (playerCamera != null)
                floatingText.transform.forward = playerCamera.transform.forward;
        }
    }

    private void OnCountdownTick(int tick)
    {
        if (floatingText == null || animalPitcher == null) return;

        string text = tick switch
        {
            3 => countdownText3,
            2 => countdownText2,
            1 => countdownText1,
            _ => readyText
        };

        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimateText(text));
    }

    private IEnumerator AnimateText(string text)
    {
        // 首次使用时挂到动物实例下，之后自动跟随
        TryAttachToAnimal();

        floatingText.text = text;
        floatingText.gameObject.SetActive(true);

        // 重置本地偏移（动画可能动过）
        floatingText.transform.localPosition = Vector3.up * heightAboveHead;

        Color c = floatingText.color;
        Transform t = floatingText.transform;
        Vector3 baseLocalPos = t.localPosition;

        // 弹入
        float elapsed = 0f;
        while (elapsed < popInTime)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / popInTime);
            t.localScale = Vector3.one * Mathf.Lerp(0f, popMaxScale, EaseOut(p));
            c.a = p;
            floatingText.color = c;
            yield return null;
        }

        // 缩回正常
        float settle = 0f;
        float settleTime = 0.05f;
        while (settle < settleTime)
        {
            settle += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(popMaxScale, 1f, settle / settleTime);
            yield return null;
        }
        t.localScale = Vector3.one;
        c.a = 1f;
        floatingText.color = c;

        // 停留
        yield return new WaitForSeconds(holdTime);

        // 淡出 + 上浮
        elapsed = 0f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / fadeOutTime);
            t.localPosition = baseLocalPos + Vector3.up * (floatUpDistance * p);
            c.a = 1f - p;
            floatingText.color = c;
            yield return null;
        }

        floatingText.gameObject.SetActive(false);
        animRoutine = null;
    }

    private void TryAttachToAnimal()
    {
        if (attached) return;

        var animal = animalPitcher.AnimalTransform;
        if (animal == null) return;

        floatingText.transform.SetParent(animal, worldPositionStays: true);
        floatingText.transform.localPosition = Vector3.up * heightAboveHead;
        attached = true;
    }

    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
}
