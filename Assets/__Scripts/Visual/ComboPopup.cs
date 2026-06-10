using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 挂在场景中一个带 TextMeshPro 的物体上。Show/hide 控制动画。
/// </summary>
public class ComboPopup : MonoBehaviour
{
    [Header("动画")]
    [SerializeField] private float popInTime = 0.1f;
    [SerializeField] private float holdTime = 1.5f;
    [SerializeField] private float fadeOutTime = 0.4f;
    [SerializeField] private float floatUpDistance = 0.8f;
    [SerializeField] private float popMaxScale = 1.2f;

    private TextMeshPro tmp;
    private Coroutine routine;

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
        gameObject.SetActive(false);
    }

    public void Show(string text, Vector3 position)
    {
        if (tmp == null) return;

        if (routine != null) StopCoroutine(routine);

        tmp.text = text;
        transform.position = position;
        transform.localScale = Vector3.zero;
        gameObject.SetActive(true);

        routine = StartCoroutine(Animate());
    }

    public void HideImmediate()
    {
        if (routine != null) StopCoroutine(routine);
        gameObject.SetActive(false);
    }

    private IEnumerator Animate()
    {
        Color c = tmp.color;
        c.a = 0;
        tmp.color = c;

        // 弹入
        float t = 0f;
        while (t < popInTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / popInTime);
            transform.localScale = Vector3.one * Mathf.Lerp(0f, popMaxScale, EaseOut(p));
            c.a = p;
            tmp.color = c;
            yield return null;
        }

        transform.localScale = Vector3.one;
        c.a = 1f;
        tmp.color = c;

        // 停留
        yield return new WaitForSeconds(holdTime);

        // 上浮+淡出
        Vector3 startPos = transform.position;
        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeOutTime);
            transform.position = startPos + Vector3.up * (floatUpDistance * p);
            c.a = 1f - p;
            tmp.color = c;
            yield return null;
        }

        gameObject.SetActive(false);
        routine = null;
    }

    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
}
