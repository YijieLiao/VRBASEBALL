using System.Collections;
using TMPro;
using UnityEngine;

public class LeaderboardPanel : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text[] entryTexts;
    [SerializeField] private GameObject[] entryBackgrounds;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text playerRankText;

    [Header("显示字段")]
    [SerializeField] private bool showRank = true;
    [SerializeField] private bool showPlayerName = true;
    [SerializeField] private bool showAnimalName = true;
    [SerializeField] private bool showScore = true;
    [SerializeField] private bool showDate = false;

    [Header("颜色")]
    [SerializeField] private Color defaultColor = new Color(0.4f, 0.2f, 0.05f);  // 棕色
    [SerializeField] private Color highlightColor = Color.white;

    [Header("按钮")]
    [SerializeField] private UnityEngine.UI.Button playAgainButton;
    [SerializeField] private UnityEngine.UI.Button returnToRoomButton;

    [Header("房间常驻")]
    [Tooltip("勾上后，每次进入房间自动刷新数据并显示（用于房间常驻排行榜）")]
    [SerializeField] private bool refreshOnRoomEnter = false;

    [Header("动画")]
    [SerializeField] private float fadeDuration = 0.3f;

    private Coroutine fadeRoutine;
    private int highlightRank = -1;

    void Awake()
    {
        // 按钮事件在 Inspector 里绑定，代码不重复 AddListener
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    void Start()
    {
        if (refreshOnRoomEnter)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += OnGameStateChanged;
            // 直接显示，不设 highlight
            Show(-1);
        }
    }

    void OnDestroy()
    {
        if (refreshOnRoomEnter && GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    void OnGameStateChanged(GameState from, GameState to)
    {
        if (to == GameState.Room)
        {
            highlightRank = -1;
            RefreshDisplay();
        }
    }

    /// <summary>highlightRank: 玩家本局排名(1-based)，-1表示不高亮</summary>
    public void Show(int highlightRank = -1)
    {
        gameObject.SetActive(true);
        this.highlightRank = highlightRank;
        RefreshDisplay();
        FadeTo(1f);
    }

    public void Hide() => FadeTo(0f);

    public void RefreshDisplay()
    {
        if (LeaderboardManager.Instance == null) return;

        var entries = LeaderboardManager.Instance.GetTopScores(10);
        for (int i = 0; i < entryTexts.Length; i++)
        {
            if (entryTexts[i] == null) continue;

            int rank = i + 1;
            bool isPlayerEntry = (rank == highlightRank);

            if (i < entries.Count)
            {
                entryTexts[i].text = FormatEntry(rank, entries[i]);
            }
            else
            {
                entryTexts[i].text = showRank ? $"{rank}.  —" : "—";
            }

            entryTexts[i].color = isPlayerEntry ? highlightColor : defaultColor;

            if (entryBackgrounds != null && i < entryBackgrounds.Length && entryBackgrounds[i] != null)
                entryBackgrounds[i].SetActive(isPlayerEntry);
        }
    }

    string FormatEntry(int rank, LeaderboardEntry e)
    {
        var parts = new System.Text.StringBuilder();

        if (showRank)
            parts.Append(rank).Append(".  ");

        if (showPlayerName)
            parts.Append(e.playerName).Append("  ");

        if (showAnimalName)
            parts.Append("(").Append(e.animalName).Append(")  ");

        if (showScore)
            parts.Append(e.score).Append("  ");

        if (showDate)
            parts.Append(e.date);

        return parts.ToString().TrimEnd();
    }

    void FadeTo(float target)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        if (!gameObject.activeInHierarchy)
        {
            canvasGroup.alpha = target;
            return;
        }
        fadeRoutine = StartCoroutine(FadeRoutine(target));
    }

    IEnumerator FadeRoutine(float target)
    {
        if (target > 0f)
        {
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
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void OnPlayAgainClicked() => ResultCoordinator.Instance?.PlayAgain();
    public void OnReturnToRoomClicked() => ResultCoordinator.Instance?.ReturnToRoom();
}
