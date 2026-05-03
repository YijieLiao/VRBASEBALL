using System.Collections;
using TMPro;
using UnityEngine;

public class HitResultPopup : MonoBehaviour
{
    [Header("文字")]
    [SerializeField] private TMP_Text label;

    [Header("动画")]
    [SerializeField] private float lifetime = 1.2f;
    [SerializeField] private float riseHeight = 0.45f;
    [SerializeField] private float startScale = 0.2f;
    [SerializeField] private float peakScale = 1.15f;
    [SerializeField] private float settleScale = 1f;
    [SerializeField] private float popDurationRatio = 0.25f;
    [SerializeField] private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Transform lookAtTarget;
    private Coroutine playRoutine;
    private Vector3 startPosition;
    private Color baseColor;

    private void Reset()
    {
        label = GetComponentInChildren<TMP_Text>(true);
    }

    private void LateUpdate()
    {
        FaceLookAtTarget();
    }

    public void Play(string text, Color color, Transform target)
    {
        if (label == null)
            label = GetComponentInChildren<TMP_Text>(true);

        lookAtTarget = target;
        startPosition = transform.position;
        baseColor = color;

        if (label != null)
        {
            label.gameObject.SetActive(true);
            label.text = text;
            label.color = WithAlpha(baseColor, 0f);
        }

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        float elapsed = 0f;
        float popDuration = Mathf.Max(0.01f, lifetime * popDurationRatio);

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);
            float popT = Mathf.Clamp01(elapsed / popDuration);
            float fadeOutT = Mathf.InverseLerp(0.65f, 1f, t);

            transform.position = startPosition + Vector3.up * (riseCurve.Evaluate(t) * riseHeight);
            float scale = popT < 1f
                ? Mathf.Lerp(startScale, peakScale, EaseOutBack(popT))
                : Mathf.Lerp(peakScale, settleScale, Mathf.InverseLerp(popDuration, lifetime * 0.45f, elapsed));
            transform.localScale = Vector3.one * scale;

            if (label != null)
            {
                float fadeIn = Mathf.Clamp01(elapsed / (popDuration * 0.6f));
                float alpha = fadeIn * (1f - fadeOutT);
                label.color = WithAlpha(baseColor, alpha);
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private void FaceLookAtTarget()
    {
        if (lookAtTarget == null)
            return;

        Vector3 direction = transform.position - lookAtTarget.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
