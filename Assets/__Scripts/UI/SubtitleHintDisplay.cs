using UnityEngine;

[RequireComponent(typeof(CanvasGroupFader))]
public class SubtitleHintDisplay : MonoBehaviour
{
    [SerializeField] private CanvasGroupFader fader;
    [SerializeField] private GameObject subtitleTextObject;

    private GameObject currentHint;

    void Awake()
    {
        if (fader == null)
            fader = GetComponent<CanvasGroupFader>();
        fader.deactivateOnHide = false;

        if (subtitleTextObject == null)
            subtitleTextObject = transform.Find("Text (TMP)")?.gameObject;
    }

    private static SubtitleHintDisplay GetActive()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            var d = cam.GetComponentInChildren<SubtitleHintDisplay>();
            if (d != null) return d;
        }
        return FindObjectOfType<SubtitleHintDisplay>();
    }

    public static void ShowHint(GameObject hintContent)
    {
        if (hintContent == null) return;
        var d = GetActive();
        if (d == null) return;
        d.Show(hintContent);
    }

    public static void HideHint()
    {
        var d = GetActive();
        if (d != null) d.Hide();
    }

    private void Show(GameObject hintContent)
    {
        if (currentHint != null && currentHint != hintContent)
            currentHint.SetActive(false);

        currentHint = hintContent;
        currentHint.SetActive(true);

        if (subtitleTextObject != null)
            subtitleTextObject.SetActive(false);

        fader.FadeIn();
    }

    private void Hide()
    {
        if (currentHint == null) return;
        var h = currentHint;
        currentHint = null;
        fader.FadeOut(() =>
        {
            h.SetActive(false);
            if (subtitleTextObject != null)
                subtitleTextObject.SetActive(true);
        });
    }
}
