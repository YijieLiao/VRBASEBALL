using System.Collections;
using UnityEngine;

/// <summary>
/// 落地环形特效：放大 + 淡出 + 自毁。
/// 挂到环形 Sprite 预制体上。
/// </summary>
public class LandingRingEffect : MonoBehaviour
{
    [Header("动画参数")]
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private float startScale = 0.3f;
    [SerializeField] private float endScale = 2.5f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("颜色")]
    [SerializeField] private Color ringColor = Color.white;
    [Tooltip("落地时短暂闪光，然后渐隐")]
    [SerializeField] private float flashDuration = 0.1f;

    private SpriteRenderer spriteRenderer;
    private Transform ringTransform;
    private Color appliedColor;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        ringTransform = spriteRenderer != null ? spriteRenderer.transform : transform;
        appliedColor = ringColor;
    }

    /// <summary>生成器调用，运行时覆盖颜色</summary>
    public void SetColor(Color color)
    {
        appliedColor = color;
        if (spriteRenderer != null)
            spriteRenderer.color = color;
    }

    void Start()
    {
        if (spriteRenderer != null && appliedColor == ringColor)
        {
            spriteRenderer.color = ringColor;
        }
        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        // 先设到初始大小，避免 Prefab 默认 x1 闪一下
        ringTransform.localScale = new Vector3(startScale, startScale, 1f);
        float elapsed = 0f;

        // 短暂闪光
        if (spriteRenderer != null && flashDuration > 0f)
        {
            Color flash = appliedColor;
            flash.a = Mathf.Min(flash.a * 1.5f, 1f);
            spriteRenderer.color = flash;
            yield return new WaitForSeconds(flashDuration);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = Mathf.Lerp(startScale, endScale, scaleCurve.Evaluate(t));
            ringTransform.localScale = new Vector3(scale, scale, 1f);

            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}
