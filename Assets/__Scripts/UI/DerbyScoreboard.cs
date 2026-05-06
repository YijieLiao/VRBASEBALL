using TMPro;
using UnityEngine;

public class DerbyScoreboard : MonoBehaviour
{
    [Header("文字组件")]
    [SerializeField] private TMP_Text ballCountText;
    [SerializeField] private TMP_Text totalScoreText;
    [SerializeField] private TMP_Text lastResultText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private TMP_Text speedText;

    [Header("文案格式")]
    [SerializeField] private string ballCountFormat = "回合数: {0} / {1}";
    [SerializeField] private string totalScoreFormat = "总分: {0}";
    [SerializeField] private string homeRunFormat = "全垒打! +{0}";
    [SerializeField] private string fairLandingFormat = "有效落地 +{0}";
    [SerializeField] private string foulFormat = "界外";
    [SerializeField] private string missFormat = "未击中";
    [SerializeField] private string roundEndFormat = "本局结束!";
    [SerializeField] private string countdownGoFormat = "等待击球";
    [SerializeField] private string comboFormat = "连击 {0}  ×{1:F1}";
    [SerializeField] private string comboIdleFormat = "";
    [SerializeField] private string maxComboFormat = "本局最高连击: {0}";

    [Header("动画")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float resultDisplayDuration = 1.8f;

    [Header("引用")]
    [SerializeField] private ClassicModeRoundManager roundManager;

    private void Awake()
    {
        if (roundManager == null) roundManager = FindObjectOfType<ClassicModeRoundManager>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        ClearDisplay();
        if (roundManager == null) return;
        roundManager.OnBallResult += OnBallResult;
        roundManager.OnBallNumberUpdate += OnBallNumberUpdate;
        roundManager.OnCountdownTick += OnCountdownTick;
        roundManager.OnRoundEnded += OnRoundEnded;
        roundManager.OnComboChanged += OnComboChanged;
    }

    private void OnDisable()
    {
        if (roundManager == null) return;
        roundManager.OnBallResult -= OnBallResult;
        roundManager.OnBallNumberUpdate -= OnBallNumberUpdate;
        roundManager.OnCountdownTick -= OnCountdownTick;
        roundManager.OnRoundEnded -= OnRoundEnded;
        roundManager.OnComboChanged -= OnComboChanged;
    }

    private void Start()
    {
        ClearDisplay();
    }

    private void OnBallNumberUpdate(int current, int total)
    {
        if (ballCountText != null)
            ballCountText.text = string.Format(ballCountFormat, current, total);

        if (current == 1 && totalScoreText != null)
            totalScoreText.text = string.Format(totalScoreFormat, 0);

        if (lastResultText != null)
            lastResultText.text = "";

        Show();
    }

    private void OnCountdownTick(int tick)
    {
        if (tick > 0 && lastResultText != null)
            lastResultText.text = tick.ToString();
        else if (tick == 0 && lastResultText != null)
            lastResultText.text = countdownGoFormat;
    }

    private void OnBallResult(BallResultInfo info)
    {
        if (totalScoreText != null)
            totalScoreText.text = string.Format(totalScoreFormat, info.totalScore);

        if (lastResultText != null)
        {
            lastResultText.text = GetResultString(info);
            lastResultText.color = GetResultColor(info.result);
        }

        if (speedText != null)
            speedText.text = "";
    }

    private void OnRoundEnded(int totalScore)
    {
        if (totalScoreText != null)
            totalScoreText.text = string.Format(totalScoreFormat, totalScore);

        if (ballCountText != null)
            ballCountText.text = "结束";

        if (lastResultText != null)
        {
            lastResultText.text = roundEndFormat;
            lastResultText.color = Color.white;
        }

        if (comboText != null && roundManager != null)
            comboText.text = string.Format(maxComboFormat, roundManager.MaxCombo);

        // 延迟隐藏
        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), resultDisplayDuration);
    }

    private void Show()
    {
        if (canvasGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeTo(1f));
        }
    }

    private void Hide()
    {
        if (canvasGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeTo(0f));
        }
    }

    private void OnComboChanged(int combo, float multiplier)
    {
        if (comboText == null) return;
        if (combo > 0)
            comboText.text = string.Format(comboFormat, combo, multiplier);
        else
            comboText.text = comboIdleFormat;
    }

    private void ClearDisplay()
    {
        if (ballCountText != null) ballCountText.text = "";
        if (totalScoreText != null) totalScoreText.text = "0";
        if (lastResultText != null) lastResultText.text = "";
        if (comboText != null) comboText.text = "";
        if (speedText != null) speedText.text = "";
    }

    private System.Collections.IEnumerator FadeTo(float target)
    {
        float start = canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = target;
    }

    private string GetResultString(BallResultInfo info)
    {
        switch (info.result)
        {
            case HitResult.HomeRun:
                return string.Format(homeRunFormat, info.ballScore);
            case HitResult.FairLanding:
                return string.Format(fairLandingFormat, info.ballScore);
            case HitResult.Foul:
                return foulFormat;
            case HitResult.None:
                return missFormat;
            default:
                return missFormat;
        }
    }

    private Color GetResultColor(HitResult result)
    {
        switch (result)
        {
            case HitResult.HomeRun:
                return new Color(1f, 0.82f, 0.15f);
            case HitResult.FairLanding:
                return new Color(0.25f, 1f, 0.35f);
            case HitResult.Foul:
                return new Color(1f, 0.5f, 0.2f);
            default:
                return new Color(0.8f, 0.3f, 0.3f);
        }
    }
}
