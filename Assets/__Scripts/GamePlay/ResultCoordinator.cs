using UnityEngine;

public class ResultCoordinator : MonoBehaviour
{
    public static ResultCoordinator Instance { get; private set; }

    [Header("面板引用")]
    [SerializeField] private NameInputPanel nameInputPanel;
    [SerializeField] private LeaderboardPanel leaderboardPanel;

    [Header("设置")]
    [SerializeField] private int topN = 10;
    private string animalName => GameManager.Instance?.SelectedAnimalType ?? "COW";

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

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnShowResult += ShowResult;
            GameManager.Instance.OnStateChanged += OnGameStateChanged;
        }
    }

    void OnDestroy()
    {
        if (nameInputPanel != null)
            nameInputPanel.OnNameConfirmed -= OnNameConfirmed;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnShowResult -= ShowResult;
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
        }
        if (Instance == this) Instance = null;
    }

    void OnGameStateChanged(GameState from, GameState to)
    {
        // 离开 Result 状态时自动关闭面板
        if (from == GameState.Result && to != GameState.Result)
            HideAll();
    }

    /// <summary>外部调用，传入最终总分</summary>
    public void ShowResult(int totalScore)
    {
        currentScore = totalScore;

        if (nameInputPanel == null || leaderboardPanel == null)
        {
            Debug.LogWarning($"[ResultCoordinator] 面板未连线！NameInputPanel={(nameInputPanel != null ? "OK" : "NULL")}, LeaderboardPanel={(leaderboardPanel != null ? "OK" : "NULL")}");
            return;
        }

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
