# 结算流程：制作方案 & 非VR测试方案

## 目标

在非VR场景中用鼠标跑通完整结算流程：
> 模拟对局结束 → 判定是否进榜 → 输入名字 → 写入排行榜 → 展示榜单 → 再来一局

---

## 一、需要新建/修改的文件

```
Assets/__Scripts/
  UI/
    KeyboardManager.cs          [修改]  +OnSubmit事件 +Submit()
    NameInputPanel.cs           [新建]  名字输入面板
    LeaderboardPanel.cs         [新建]  排行榜展示面板
    ResultCoordinator.cs        [新建]  结算流程协调器
  Test/
    SettlementTestDriver.cs     [新建]  测试驱动脚本
    SettlementTestDriver.cs.meta [新建]

Assets/___Scenes/
  Test_Settlement.unity         [新建]  非VR测试场景
```

`SettlementPanel.cs` 和 `VirtualKeyboard.cs` 废弃不动。

---

## 二、每个脚本的完整规格

### 2.1 KeyboardManager.cs [修改]

在当前文件基础上加：

```csharp
// === 新增事件 ===
public event System.Action<string> OnSubmit;

// === 新增方法 ===
public void Submit()
{
    string final = string.IsNullOrWhiteSpace(Input) ? "Player" : Input.Trim();
    OnSubmit?.Invoke(final);
}

public void SetInputText(string text)
{
    inputText.text = text;
}

// GenerateInput 里加一个事件，方便 NameInputPanel 实时刷新槽位
public event System.Action<string> OnInputChanged;

public void GenerateInput(string s)
{
    if (Input.Length >= maxInputLength) return;
    Input += s;
    OnInputChanged?.Invoke(Input);
}

public void Backspace()
{
    if (Input.Length > 0)
        Input = Input.Remove(Input.Length - 1);
    else
        return;
    OnInputChanged?.Invoke(Input);
}

public void Clear()
{
    Input = "";
    OnInputChanged?.Invoke(Input);
}
```

### 2.2 NameInputPanel.cs [新建]

```csharp
using TMPro;
using UnityEngine;
using VRKeyboard.Utils;

public class NameInputPanel : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text[] slots;  // 6个槽位，按0-5顺序拖入
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private KeyboardManager keyboardManager;

    [Header("按钮")]
    [SerializeField] private UnityEngine.UI.Button confirmButton;
    [SerializeField] private UnityEngine.UI.Button skipButton;

    public event System.Action<string> OnNameConfirmed;

    private string currentName = "";

    void OnEnable()
    {
        if (keyboardManager != null)
        {
            keyboardManager.OnInputChanged += OnInputChanged;
            keyboardManager.OnSubmit += OnSubmit;
        }
        ResetPanel();
    }

    void OnDisable()
    {
        if (keyboardManager != null)
        {
            keyboardManager.OnInputChanged -= OnInputChanged;
            keyboardManager.OnSubmit -= OnSubmit;
        }
    }

    void Start()
    {
        confirmButton?.onClick.AddListener(() => keyboardManager?.Submit());
        skipButton?.onClick.AddListener(OnSkipClicked);
    }

    public void Show(string prompt)
    {
        if (promptText != null)
            promptText.text = prompt;
        SetVisible(true);
    }

    public void Hide() => SetVisible(false);

    void OnInputChanged(string text)
    {
        currentName = text;
        RefreshSlots();
    }

    void OnSubmit(string name)
    {
        SetVisible(false);
        OnNameConfirmed?.Invoke(name);
    }

    public void OnSkipClicked()
    {
        SetVisible(false);
        OnNameConfirmed?.Invoke("Player");
    }

    void ResetPanel()
    {
        currentName = "";
        RefreshSlots();
        keyboardManager?.Clear();
        keyboardManager?.SetInputText("");
    }

    void RefreshSlots()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            slots[i].text = i < currentName.Length ? currentName[i].ToString() : "_";
        }
    }

    void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}
```

### 2.3 LeaderboardPanel.cs [新建]

```csharp
using TMPro;
using UnityEngine;

public class LeaderboardPanel : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text[] entryTexts;  // 10个排行条目
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text playerRankText;  // 可选：显示玩家排名

    [Header("按钮")]
    [SerializeField] private UnityEngine.UI.Button playAgainButton;
    [SerializeField] private UnityEngine.UI.Button returnToRoomButton;

    void Start()
    {
        playAgainButton?.onClick.AddListener(OnPlayAgainClicked);
        returnToRoomButton?.onClick.AddListener(OnReturnToRoomClicked);
    }

    public void Show()
    {
        RefreshDisplay();
        SetVisible(true);
    }

    public void Hide() => SetVisible(false);

    public void RefreshDisplay()
    {
        if (LeaderboardManager.Instance == null) return;

        var entries = LeaderboardManager.Instance.GetTopScores(10);
        for (int i = 0; i < entryTexts.Length; i++)
        {
            if (entryTexts[i] == null) continue;
            if (i < entries.Count)
            {
                var e = entries[i];
                entryTexts[i].text = $"{i + 1}.  {e.playerName}  ({e.animalName})   {e.score}";
            }
            else
            {
                entryTexts[i].text = $"{i + 1}.  —";
            }
        }
    }

    void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }

    void OnPlayAgainClicked() => ResultCoordinator.Instance?.PlayAgain();
    void OnReturnToRoomClicked() => ResultCoordinator.Instance?.ReturnToRoom();
}
```

### 2.4 ResultCoordinator.cs [新建]

```csharp
using UnityEngine;

public class ResultCoordinator : MonoBehaviour
{
    public static ResultCoordinator Instance { get; private set; }

    [Header("面板引用")]
    [SerializeField] private NameInputPanel nameInputPanel;
    [SerializeField] private LeaderboardPanel leaderboardPanel;

    [Header("设置")]
    [SerializeField] private int topN = 10;
    [SerializeField] private string animalName = "COW";  // 后续从选角系统传入

    private int currentScore;
    private bool isHighScore;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        HideAll();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ==================== 公开接口 ====================

    /// <summary>外部调用（GameManager或TestDriver），传入最终总分</summary>
    public void ShowResult(int totalScore)
    {
        currentScore = totalScore;
        isHighScore = LeaderboardManager.Instance != null
                   && LeaderboardManager.Instance.IsHighScore(totalScore, topN);

        if (isHighScore)
        {
            int rank = LeaderboardManager.Instance.GetRank(totalScore);
            nameInputPanel.Show($"你进入了前 {topN} 名！（第 {rank} 名）\n输入你的名字：");
        }
        else
        {
            ShowLeaderboard();
        }
    }

    /// <summary>名字确认后的回调</summary>
    public void OnNameConfirmed(string playerName)
    {
        LeaderboardManager.Instance?.AddEntry(playerName, animalName, currentScore);
        ShowLeaderboard();
    }

    public void PlayAgain()
    {
        HideAll();
        // 通知 GameManager 再来一局
        GameManager.Instance?.PlayAgain();
    }

    public void ReturnToRoom()
    {
        HideAll();
        GameManager.Instance?.ReturnToRoom();
    }

    // ==================== 内部 ====================

    void ShowLeaderboard()
    {
        leaderboardPanel.Show();
    }

    void HideAll()
    {
        nameInputPanel?.Hide();
        leaderboardPanel?.Hide();
    }
}
```

### 2.5 SettlementTestDriver.cs [新建]（仅测试用）

```csharp
using UnityEngine;

/// <summary>
/// 非VR测试驱动。场景中放几个按钮，模拟不同对局结果，鼠标点击即可测试完整流程。
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

    // 以下方法绑定到测试按钮的 OnClick

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
```

> LeaderboardManager 需要加一个 `ClearAllEntries()` 公开方法（目前只在 `#if UNITY_EDITOR` 下有 `ClearEntries` context menu）。

---

## 三、测试场景搭建步骤

### 3.1 创建场景

1. `Assets/___Scenes/` → 右键 → Create → Scene → 命名 `Test_Settlement`
2. 删除默认的 Main Camera 和 Directional Light

### 3.2 场景基本结构

```
Test_Settlement
  ├─ Main Camera (普通Camera, 非XR)
  │    Position: (0, 0, -10)
  │    Clear Flags: Solid Color
  │
  ├─ EventSystem
  │    └─ Standalone Input Module (默认鼠标输入)
  │
  ├─ TestCanvas (Screen Space - Overlay)
  │    ├─ Canvas Scaler: Scale With Screen Size, 1920x1080
  │    │
  │    ├─ [管理物体]
  │    │    ├─ ResultCoordinator (空GameObject + ResultCoordinator脚本)
  │    │    ├─ LeaderboardManager (空GameObject + LeaderboardManager脚本)
  │    │    └─ TestDriver (空GameObject + SettlementTestDriver脚本)
  │    │
  │    ├─ [测试按钮区] (左上角常驻)
  │    │    ├─ "填充假数据" Button → TestDriver.SeedFakeLeaderboard
  │    │    ├─ "模拟高分" Button   → TestDriver.SimulateHighScore
  │    │    ├─ "模拟低分" Button   → TestDriver.SimulateLowScore
  │    │    └─ "清空排行榜" Button → TestDriver.ClearLeaderboard
  │    │
  │    ├─ NameInputPanel (Panel, 初始隐藏 alpha=0)
  │    │    ├─ CanvasGroup
  │    │    ├─ NameInputPanel 脚本
  │    │    ├─ PromptText (TMP) — "你进入了前10名！"
  │    │    ├─ SlotsGroup (HorizontalLayout)
  │    │    │    ├─ Slot_0 (TMP_Text, text: "_")
  │    │    │    ├─ Slot_1 ~ Slot_5 同上
  │    │    ├─ Keyboard (拖入 _VRKeyboard/Prefab/Keyboard.prefab)
  │    │    │    └─ KeyboardManager: maxInputLength=6, isUppercase=true
  │    │    ├─ "确认" Button → KeyboardManager.Submit
  │    │    └─ "跳过" Button → NameInputPanel.OnSkipClicked
  │    │
  │    └─ LeaderboardPanel (Panel, 初始隐藏 alpha=0)
  │         ├─ CanvasGroup
  │         ├─ LeaderboardPanel 脚本
  │         ├─ TitleText (TMP) — "排行榜"
  │         ├─ PlayerRankText (TMP, 可选)
  │         ├─ EntriesGroup (VerticalLayout)
  │         │    ├─ Entry_0 (TMP_Text)
  │         │    ├─ Entry_1 ~ Entry_9 同上
  │         ├─ "再来一局" Button → LeaderboardPanel.OnPlayAgainClicked
  │         └─ "返回房间" Button → LeaderboardPanel.OnReturnToRoomClicked
  │
  └─ Directional Light
```

### 3.3 Keyboard Prefab 处理

由于测试用 Screen Space Overlay Canvas，而 Keyboard.prefab 可能自带 Canvas：

1. 拖入 Keyboard.prefab 到 NameInputPanel 下
2. 如果 Prefab 自带 Canvas → 删除该 Canvas，把按键直接放在 TestCanvas 下
3. 精简按键：删除 Symbol/Shift/Clear/Space 相关 GameObject
4. 调 KeyboardManager: `maxInputLength = 6`, `isUppercase = true`
5. 调整键盘按键布局，排列整齐

### 3.4 Inspector 连线

#### NameInputPanel
| 字段 | 拖入 |
|------|------|
| Canvas Group | NameInputPanel 自身 CanvasGroup |
| Slots | Slot_0 ~ Slot_5 依次拖入 |
| Prompt Text | PromptText TMP |
| Keyboard Manager | Keyboard 实例上的 KeyboardManager |
| Confirm Button | "确认" Button |
| Skip Button | "跳过" Button |

#### LeaderboardPanel
| 字段 | 拖入 |
|------|------|
| Canvas Group | LeaderboardPanel 自身 CanvasGroup |
| Entry Texts | Entry_0 ~ Entry_9 依次拖入 |
| Title Text | TitleText TMP |
| Player Rank Text | PlayerRankText TMP |
| Play Again Button | "再来一局" Button |
| Return To Room Button | "返回房间" Button |

#### ResultCoordinator
| 字段 | 拖入 |
|------|------|
| Name Input Panel | NameInputPanel 实例 |
| Leaderboard Panel | LeaderboardPanel 实例 |

#### SettlementTestDriver
| 字段 | 拖入 |
|------|------|
| Result Coordinator | ResultCoordinator 实例 |
| Leaderboard Manager | LeaderboardManager 实例 |

---

## 四、测试流程

### 测试 1：高分进榜 → 输入名字 → 查看榜单

```
1. 点 "清空排行榜"
2. 点 "填充假数据" → Console 确认 15 条已生成
3. 点 "模拟高分" (1500分)
4. → NameInputPanel 弹出，提示 "你进入了前10名！（第X名）"
5. → 6格槽位显示 [_][_][_][_][_][_]
6. 鼠标点按键: T → E → S → T → 1 → 2
7. → 槽位显示 [T][E][S][T][1][2]
8. 点 "确认"
9. → NameInputPanel 隐藏，LeaderboardPanel 显示
10. → 榜单第1行或前几行出现 "TEST12 (COW) 1500"
11. 点 "再来一局" → 面板隐藏（或报 GameManager null 警告，测试环境正常）
```

### 测试 2：低分不进榜

```
1. 点 "模拟低分" (100分)
2. → NameInputPanel 不显示
3. → 直接显示 LeaderboardPanel，榜单无变化
4. 点 "返回房间" → 面板隐藏
```

### 测试 3：未满6位点确认

```
1. 点 "模拟高分"
2. 输入 "AB" (2位)
3. 点 "确认"
4. → 名字以 "AB" 正常提交，榜单出现 "AB (COW) 1500"
```

### 测试 4：跳过

```
1. 点 "模拟高分"
2. 直接点 "跳过"
3. → 榜单出现 "Player (COW) 1500"
```

### 测试 5：退格

```
1. 点 "模拟高分"
2. 输入 "TEST" → 点 退格 → 点 退格 → 显示 "TE"
3. 继续输入 "ST" → 显示 "TEST"
4. 确认提交
```

### 测试 6：空榜进榜

```
1. 点 "清空排行榜"
2. 点 "模拟高分"
3. 输入 "FIRST" → 确认
4. → 榜单只有 1 条：1. FIRST (COW) 1500，其余全显示 "—"
```

---

## 五、实施顺序

```
Step 1: 改 KeyboardManager.cs（+OnSubmit, +OnInputChanged, +Submit, +SetInputText）
Step 2: 新建 NameInputPanel.cs
Step 3: 新建 LeaderboardPanel.cs
Step 4: 新建 ResultCoordinator.cs
Step 5: 改 LeaderboardManager.cs（暴露 ClearAllEntries 公开方法）
Step 6: 新建 SettlementTestDriver.cs
Step 7: 创建 Test_Settlement.unity，按第三节搭建
Step 8: 按第四节跑全部 6 个测试用例
Step 9: 测试通过后，更新 DEVELOPMENT_PROGRESS.md
```

---

## 六、后续 VR 场景迁移

测试通过后，迁移到 VR 主场景：

1. Canvas RenderMode 从 Screen Space → **World Space**
2. Canvas 从 TestCanvas 取出，挂到房间世界坐标位置
3. GraphicRaycaster → **TrackedDeviceGraphicRaycaster**
4. EventSystem 的 Standalone Input Module → **XR UI Input Module**
5. GameManager.OnShowResult 接入 ResultCoordinator.ShowResult
6. 删除 SettlementTestDriver
