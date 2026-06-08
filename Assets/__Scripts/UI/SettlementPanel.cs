using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SettlementPanel : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text totalScoreText;
    [SerializeField] private TMP_Text highScorePromptText;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private GameObject nameInputGroup;
    [SerializeField] private TMP_Text nameDisplayText;
    [SerializeField] private GameObject leaderboardGroup;
    [SerializeField] private Transform leaderboardEntryParent;
    [SerializeField] private TMP_Text leaderboardEntryPrefab;
    [SerializeField] private GameObject actionButtonsGroup;
    [SerializeField] private VirtualKeyboard virtualKeyboard;

    [Header("引用")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private LeaderboardManager leaderboardManager;

    [Header("设置")]
    [SerializeField] private int topN = 10;
    [SerializeField] private string defaultName = "Player";

    private int currentScore;
    private string playerName = "";
    private bool isHighScore;
    private string animalName => gameManager != null ? gameManager.SelectedAnimalType : "COW";

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (leaderboardManager == null) leaderboardManager = FindObjectOfType<LeaderboardManager>();
    }

    private void OnEnable()
    {
        if (gameManager != null)
            gameManager.OnShowResult += OnShowResult;
        if (virtualKeyboard != null)
            virtualKeyboard.OnSubmit += OnNameSubmitted;
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.OnShowResult -= OnShowResult;
        if (virtualKeyboard != null)
            virtualKeyboard.OnSubmit -= OnNameSubmitted;
    }

    private void Start()
    {
        SetVisible(false);
    }

    private void OnShowResult(int totalScore)
    {
        currentScore = totalScore;
        Show();
    }

    private void Show()
    {
        // 总分
        if (totalScoreText != null)
            totalScoreText.text = $"总分：{currentScore}";

        // 排行榜判定
        isHighScore = leaderboardManager != null && leaderboardManager.IsHighScore(currentScore, topN);
        int rank = leaderboardManager != null ? leaderboardManager.GetRank(currentScore) : 0;

        if (isHighScore)
        {
            if (highScorePromptText != null)
                highScorePromptText.text = $"你进入了前 {topN} 名！（第 {rank} 名）\n输入你的名字：";

            if (rankText != null)
                rankText.text = $"排名：第 {rank} 名";

            ShowNameInput();
        }
        else
        {
            if (highScorePromptText != null)
                highScorePromptText.text = rank <= 20
                    ? $"最终排名：第 {rank} 名"
                    : "未进入排行榜";

            if (rankText != null)
                rankText.text = "";

            ShowLeaderboardAndActions();
        }

        SetVisible(true);
    }

    private void ShowNameInput()
    {
        if (nameInputGroup != null) nameInputGroup.SetActive(true);
        if (leaderboardGroup != null) leaderboardGroup.SetActive(false);
        if (actionButtonsGroup != null) actionButtonsGroup.SetActive(false);

        playerName = "";
        if (nameDisplayText != null) nameDisplayText.text = "";

        if (virtualKeyboard != null)
            virtualKeyboard.gameObject.SetActive(true);
    }

    private void ShowLeaderboardAndActions()
    {
        if (nameInputGroup != null) nameInputGroup.SetActive(false);
        if (leaderboardGroup != null) leaderboardGroup.SetActive(true);
        if (actionButtonsGroup != null) actionButtonsGroup.SetActive(true);

        if (virtualKeyboard != null)
            virtualKeyboard.gameObject.SetActive(false);

        RefreshLeaderboardDisplay();
    }

    private void OnNameSubmitted(string name)
    {
        playerName = string.IsNullOrWhiteSpace(name) ? defaultName : name;

        if (leaderboardManager != null)
            leaderboardManager.AddEntry(playerName, animalName, currentScore);

        ShowLeaderboardAndActions();
    }

    public void OnSkipClicked()
    {
        OnNameSubmitted(defaultName);
    }

    private void RefreshLeaderboardDisplay()
    {
        if (leaderboardEntryParent == null || leaderboardEntryPrefab == null)
            return;

        // 清除旧条目
        foreach (Transform child in leaderboardEntryParent)
            Destroy(child.gameObject);

        IReadOnlyList<LeaderboardEntry> entries = leaderboardManager != null
            ? leaderboardManager.GetTopScores(topN)
            : new List<LeaderboardEntry>();

        for (int i = 0; i < entries.Count; i++)
        {
            TMP_Text entry = Instantiate(leaderboardEntryPrefab, leaderboardEntryParent);
            entry.text = $"{i + 1}. {entries[i].playerName} ({entries[i].animalName})  {entries[i].score}";
            entry.gameObject.SetActive(true);
        }
    }

    // ==================== 按钮回调 ====================

    public void OnPlayAgainClicked()
    {
        SetVisible(false);
        if (gameManager != null) gameManager.PlayAgain();
    }

    public void OnReturnToRoomClicked()
    {
        SetVisible(false);
        if (gameManager != null) gameManager.ReturnToRoom();
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}
