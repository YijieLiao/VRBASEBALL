using UnityEngine;

public class ResultCoordinator : MonoBehaviour
{
    public static ResultCoordinator Instance { get; private set; }

    [Header("面板引用")]
    [SerializeField] private NameInputPanel nameInputPanel;
    [SerializeField] private LeaderboardPanel leaderboardPanel;

    [Header("设置")]
    [SerializeField] private int topN = 10;
    [SerializeField] private string animalName = "COW";

    private int currentScore;
    private int currentRank;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (nameInputPanel != null)
            nameInputPanel.OnNameConfirmed += OnNameConfirmed;

        // 接上 GameManager 的结算事件（无需 Inspector 连线）
        if (GameManager.Instance != null)
            GameManager.Instance.OnShowResult += ShowResult;

        HideAll();
    }

    void OnDestroy()
    {
        if (nameInputPanel != null)
            nameInputPanel.OnNameConfirmed -= OnNameConfirmed;
        if (GameManager.Instance != null)
            GameManager.Instance.OnShowResult -= ShowResult;
        if (Instance == this) Instance = null;
    }

    /// <summary>外部调用，传入最终总分</summary>
    public void ShowResult(int totalScore)
    {
        currentScore = totalScore;
        bool isHighScore = LeaderboardManager.Instance != null
                        && LeaderboardManager.Instance.IsHighScore(totalScore, topN);

        if (isHighScore)
        {
            currentRank = LeaderboardManager.Instance.GetRank(totalScore);
            nameInputPanel.Show($"你进入了前 {topN} 名！（第 {currentRank} 名）\n输入你的名字：");
        }
        else
        {
            currentRank = -1;
            leaderboardPanel.Show(-1);
        }
    }

    /// <summary>NameInputPanel 确认后的回调</summary>
    public void OnNameConfirmed(string playerName)
    {
        LeaderboardManager.Instance?.AddEntry(playerName, animalName, currentScore);
        leaderboardPanel.Show(currentRank);
    }

    public void PlayAgain()
    {
        HideAll();
        GameManager.Instance?.PlayAgain();
    }

    public void ReturnToRoom()
    {
        HideAll();
        GameManager.Instance?.ReturnToRoom();
    }

    void HideAll()
    {
        nameInputPanel?.Hide();
        leaderboardPanel?.Hide();
    }
}
