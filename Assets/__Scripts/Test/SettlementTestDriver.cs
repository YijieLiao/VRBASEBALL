using UnityEngine;

/// <summary>
/// 非VR测试驱动。场景中放几个按钮，模拟不同对局结果，鼠标点击即可测试完整结算流程。
/// </summary>
public class SettlementTestDriver : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private ResultCoordinator resultCoordinator;
    [SerializeField] private LeaderboardManager leaderboardManager;

    [Header("测试分数")]
    [SerializeField] private int highScoreForTest = 1500;
    [SerializeField] private int lowScoreForTest = 100;

    void Start()
    {
        if (resultCoordinator == null)
            resultCoordinator = FindObjectOfType<ResultCoordinator>();
        if (leaderboardManager == null)
            leaderboardManager = FindObjectOfType<LeaderboardManager>();
    }

    /// <summary>模拟高分对局结束</summary>
    public void SimulateHighScore()
    {
        Debug.Log($"[TestDriver] 模拟高分: {highScoreForTest}");
        resultCoordinator?.ShowResult(highScoreForTest);
    }

    /// <summary>模拟低分对局结束</summary>
    public void SimulateLowScore()
    {
        Debug.Log($"[TestDriver] 模拟低分: {lowScoreForTest}");
        resultCoordinator?.ShowResult(lowScoreForTest);
    }

    /// <summary>填充假排行榜数据</summary>
    public void SeedFakeLeaderboard()
    {
        if (leaderboardManager == null) return;
        leaderboardManager.ClearAllEntries();

        string[] names = { "ACE", "FOX", "BEAR", "WOLF", "DEER", "OWL", "CAT", "DOG", "BIRD", "FISH", "LION", "TIGER", "HAWK", "DUCK", "FROG" };
        for (int i = 0; i < names.Length; i++)
        {
            leaderboardManager.AddEntry(names[i], "COW", 2000 - i * 120);
        }
        Debug.Log("[TestDriver] 已填充 15 条假排行榜数据");
    }

    /// <summary>清空排行榜</summary>
    public void ClearLeaderboard()
    {
        leaderboardManager?.ClearAllEntries();
        Debug.Log("[TestDriver] 已清空排行榜");
    }
}
