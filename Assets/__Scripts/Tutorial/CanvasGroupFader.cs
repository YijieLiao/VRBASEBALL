using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class CanvasGroupFader : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public float fadeDuration = 0.35f;
    public bool hideOnAwake = true;
    public bool includeChildSprites = true;
    public bool deactivateOnHide = true;

    private Coroutine currentCoroutine;
    private SpriteRenderer[] childSprites;
    private float[] originalSpriteAlphas;

    void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        CacheSprites();

        if (hideOnAwake)
            SetImmediate(false);
    }

    private void CacheSprites()
    {
        if (!includeChildSprites)
        {
            childSprites = new SpriteRenderer[0];
            originalSpriteAlphas = new float[0];
            return;
        }

        childSprites = GetComponentsInChildren<SpriteRenderer>(true);
        originalSpriteAlphas = new float[childSprites.Length];

        for (int i = 0; i < childSprites.Length; i++)
            originalSpriteAlphas[i] = childSprites[i].color.a;
    }

    public void SetImmediate(bool visible)
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        gameObject.SetActive(true);

        float alpha = visible ? 1f : 0f;
        canvasGroup.alpha = alpha;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
        ApplySpriteAlpha(alpha);

        if (!visible && deactivateOnHide)
            gameObject.SetActive(false);
    }

    public void FadeIn()
    {
        StartFade(1f, true, null);
    }

    public void FadeOut()
    {
        StartFade(0f, false, null);
    }

    public void FadeOut(Action onComplete)
    {
        StartFade(0f, false, onComplete);
    }

    private void StartFade(float targetAlpha, bool visibleAfterFade, Action onComplete)
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        gameObject.SetActive(true);
        currentCoroutine = StartCoroutine(FadeRoutine(targetAlpha, visibleAfterFade, onComplete));
    }

    private IEnumerator FadeRoutine(float targetAlpha, bool visibleAfterFade, Action onComplete)
    {
        if (visibleAfterFade)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            canvasGroup.alpha = alpha;
            ApplySpriteAlpha(alpha);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        ApplySpriteAlpha(targetAlpha);

        if (!visibleAfterFade)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        currentCoroutine = null;
        onComplete?.Invoke();

        if (!visibleAfterFade && deactivateOnHide)
            gameObject.SetActive(false);
    }

    private void ApplySpriteAlpha(float canvasAlpha)
    {
        if (childSprites == null)
            return;

        for (int i = 0; i < childSprites.Length; i++)
        {
            if (childSprites[i] == null)
                continue;

            Color color = childSprites[i].color;
            color.a = originalSpriteAlphas[i] * canvasAlpha;
            childSprites[i].color = color;
        }
    }
}
