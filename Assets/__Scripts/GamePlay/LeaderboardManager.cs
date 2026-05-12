using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class LeaderboardEntry
{
    public string playerName;
    public string animalName;
    public int score;
    public string date;
}

[Serializable]
public class LeaderboardData
{
    public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
}

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    private const int MaxEntries = 20;
    private const string FileName = "derby_leaderboard.json";

    private LeaderboardData data;
    private string filePath;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        filePath = Path.Combine(Application.persistentDataPath, FileName);
        Load();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public IReadOnlyList<LeaderboardEntry> GetTopScores(int count = 10)
    {
        int n = Mathf.Min(count, data.entries.Count);
        return data.entries.GetRange(0, n);
    }

    public bool IsHighScore(int score, int topN = 10)
    {
        if (data.entries.Count < topN) return true;
        return score > data.entries[topN - 1].score;
    }

    public int GetRank(int score)
    {
        for (int i = 0; i < data.entries.Count; i++)
        {
            if (score > data.entries[i].score)
                return i + 1;
        }
        return data.entries.Count + 1;
    }

    public void AddEntry(string playerName, string animalName, int score)
    {
        LeaderboardEntry entry = new LeaderboardEntry
        {
            playerName = playerName,
            animalName = animalName,
            score = score,
            date = DateTime.Now.ToString("MM-dd")
        };

        // 按分数降序插入
        int insertIndex = data.entries.FindIndex(e => score > e.score);
        if (insertIndex < 0)
            data.entries.Add(entry);
        else
            data.entries.Insert(insertIndex, entry);

        // 裁剪超出的条目
        while (data.entries.Count > MaxEntries)
            data.entries.RemoveAt(data.entries.Count - 1);

        Save();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                data = JsonUtility.FromJson<LeaderboardData>(json);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LeaderboardManager] Failed to load: {e.Message}");
        }

        if (data == null)
            data = new LeaderboardData();
    }

    private void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(filePath, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LeaderboardManager] Failed to save: {e.Message}");
        }
    }

    public void ClearAllEntries()
    {
        data = new LeaderboardData();
        Save();
        Debug.Log("[LeaderboardManager] All entries cleared.");
    }

#if UNITY_EDITOR
    [ContextMenu("Clear All Entries")]
    private void ClearEntries()
    {
        data = new LeaderboardData();
        Save();
        Debug.Log("[LeaderboardManager] All entries cleared.");
    }

    [ContextMenu("Add Test Entry")]
    private void AddTestEntry()
    {
        AddEntry("TestPlayer", "COW", UnityEngine.Random.Range(100, 2000));
    }

    [ContextMenu("Print Leaderboard")]
    private void PrintLeaderboard()
    {
        Debug.Log("=== Leaderboard ===");
        for (int i = 0; i < data.entries.Count; i++)
        {
            var e = data.entries[i];
            Debug.Log($"{i + 1}. {e.playerName} ({e.animalName}) - {e.score} - {e.date}");
        }
    }
#endif
}
