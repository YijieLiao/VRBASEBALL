# 动物投手系统 — 实现文档（审查修订版）

## 概述

在经典模式中，使用玩家在选角界面选择的陪练动物作为投手。动物在 10 个半弧形双排位置间跑动，发球位置绑定在动物身上，配合动画丰富画面。

## 改动清单

| 类型 | 文件 | 说明 |
|------|------|------|
| 新增 | `Assets/__Scripts/GamePlay/AnimalPitcher.cs` | 动物投手主控制器 |
| 修改 | `Assets/__Scripts/GamePlay/Pitcher.cs` | 新增 SetLaunchPoint / ResetLaunchPoint |
| 修改 | `Assets/__Scripts/GamePlay/GameManager.cs` | 新增 SelectedAnimalType + Initialize/cleanup AnimalPitcher |
| 修改 | `Assets/__Scripts/CharacterSelected/VRSelectionManagerPoke.cs` | 暴露选中动物名 |
| 修改 | `Assets/__Scripts/GamePlay/ClassicModeRoundManager.cs` | 集成 AnimalPitcher + AbortRound 清理 |
| 修改 | `Assets/__Scripts/GamePlay/ResultCoordinator.cs` | 动态动物名 |
| 修改 | `Assets/__Scripts/UI/SettlementPanel.cs` | 动态动物名 |

---

## 第〇步：Animator Controller 手动配置（场景工作）

**这是必须由人工在 Unity Editor 中完成的前置步骤。** 当前三个动物的 Animator Controller 各只有一个参数（Cow/Sheep: `Jump` trigger；Chick: `isFlying` bool），Run/Attack 状态虽然存在于控制器中但无进入过渡。

### 需要添加的参数

对每个控制器（`AC_Cow.controller`、`AC_Chick.controller`、`AC_Sheep.controller`）添加以下参数：

| 参数名 | 类型 | 用途 |
|--------|------|------|
| `Run` | Trigger (9) | 触发跑位动画 |
| `Attack` | Trigger (9) | 触发发球动画 |

### 需要添加的状态过渡

**Run 动画过渡：**
```
Idle_A → Run: 条件 Run trigger，Has Exit Time 关闭，Transition Duration 0.1s
Run → Idle_A: 条件 Exit Time 0.9，Transition Duration 0.15s
```

**Attack 动画过渡：**
```
Idle_A → Attack: 条件 Attack trigger，Has Exit Time 关闭，Transition Duration 0.05s
Attack → Idle_A: 条件 Exit Time 0.85，Transition Duration 0.2s
```

### 各动物 Anchored 发球点偏移量（待实测微调）

| 动物 | localPosition(x, y, z) | 说明 |
|------|------------------------|------|
| Cow | (0, 1.2, 0.35) | 嘴部/面部前方 |
| Chick | (0, 0.7, 0.25) | 嘴部前方（比 Cow 矮 ~0.5m）|
| Sheep | (0, 1.05, 0.3) | 面部前方 |

---

## 第一步：传递选中动物信息

### 1.1 VRSelectionManagerPoke.cs — 暴露选中结果

```
路径: Assets/__Scripts/CharacterSelected/VRSelectionManagerPoke.cs
位置: 类内部，现有字段之后
```

**新增代码：**

```csharp
/// <summary>
/// 玩家选中的动物类型：COW / CHICK / SHEEP。未选择时返回 COW。
/// </summary>
public string SelectedAnimalName
{
    get
    {
        if (currentSelectedDoll == null) return "COW";
        string name = currentSelectedDoll.name.ToUpper();
        if (name.Contains("COW")) return "COW";
        if (name.Contains("CHICK")) return "CHICK";
        if (name.Contains("SHEEP")) return "SHEEP";
        return "COW";
    }
}
```

### 1.2 GameManager.cs — 存储 + 初始化 AnimalPitcher

```
路径: Assets/__Scripts/GamePlay/GameManager.cs
```

**新增属性（放在现有字段区域）：**

```csharp
public string SelectedAnimalType { get; set; } = "COW";
```

**修改 `EnterState(GameState.Batting)`：** 当前代码结构为 `if (previousState == GameState.Pause) { resume } else { 初始化 }`。在 `else` 分支中，在 `ActivateModeCanvas()` 之前插入动物初始化逻辑：

```csharp
case GameState.Batting:
    if (previousState == GameState.Pause)
    {
        // 从暂停恢复：取消时停即可，不重新初始化
        if (roundManager != null) roundManager.Resume();
    }
    else
    {
        // === 新增：动物投手初始化 ===
        // Classic 和 FreePractice 都显示动物，区别在于经典模式动物会跑位
        if (currentMode == GameMode.Classic || currentMode == GameMode.FreePractice)
        {
            var animalPitcher = FindObjectOfType<AnimalPitcher>();
            if (animalPitcher != null)
            {
                animalPitcher.Cleanup(); // 幂等：无实例时不报错
                var selMgr = FindObjectOfType<VRSelectionManagerPoke>();
                if (selMgr != null)
                    SelectedAnimalType = selMgr.SelectedAnimalName;
                animalPitcher.Initialize(SelectedAnimalType);
            }
        }
        // === 结束 ===

        ActivateModeCanvas();
        if (viewTransition != null)
            viewTransition.TransitionTo(ViewTransitionManager.ViewMode.Batting);
    }
    break;
```

**在 `EnterState(GameState.Room)` 中补充清理：**

```csharp
case GameState.Room:
    if (previousState == GameState.Pause || previousState == GameState.Batting)
    {
        if (roundManager != null) roundManager.AbortRound();
        // 新增：清理动物投手
        var ap = FindObjectOfType<AnimalPitcher>();
        if (ap != null) ap.Cleanup();
    }
    currentMode = GameMode.None;
    // ...
    break;
```

> **注意：** FindObjectOfType 在同一个 VR 场景内可找到 Room 区域的 VRSelectionManagerPoke，因为 Room 和 Batting 是同一场景内的两个 XR Origin 位置（通过 ViewTransitionManager 切换）。如果未来拆分为独立场景，需在切换前缓存 SelectedAnimalType。

---

## 第二步：Pitcher 支持动态发球点

### 2.1 Pitcher.cs — 新增两个方法

```
路径: Assets/__Scripts/GamePlay/Pitcher.cs
位置: 公开接口区域
```

**新增字段（放在现有字段区域）：**

```csharp
private Transform originalLaunchPoint;
```

**修改已有 `Start()` 方法（约在第 93 行），在末尾加一行：**

```csharp
void Start()
{
    // ... 原有 Start 逻辑不变 ...
    originalLaunchPoint = launchPoint;  // 新增：保存原始发球点
}
```

**新增两个公开方法：**

```csharp
public void SetLaunchPoint(Transform newPoint) => launchPoint = newPoint;
public void ResetLaunchPoint() => launchPoint = originalLaunchPoint;
```

> **注意：** `launchPoint` 当前是 `public Transform`（非 `[SerializeField] private`），直接赋值安全。

---

## 第三步：AnimalPitcher 主控制器

### 3.1 AnimalPitcher.cs — 新建文件

```
路径: Assets/__Scripts/GamePlay/AnimalPitcher.cs
```

**字段：**

```csharp
[Header("Prefab 引用")]
[SerializeField] private GameObject cowPrefab;
[SerializeField] private GameObject chickPrefab;
[SerializeField] private GameObject sheepPrefab;

[Header("发球锚点偏移")]
[SerializeField] private Vector3 cowAnchorOffset = new Vector3(0, 1.2f, 0.35f);
[SerializeField] private Vector3 chickAnchorOffset = new Vector3(0, 0.7f, 0.25f);
[SerializeField] private Vector3 sheepAnchorOffset = new Vector3(0, 1.05f, 0.3f);

[Header("移动")]
[SerializeField] private float moveSpeed = 4f;
[SerializeField] private Transform homePlate;       // 面朝方向
[SerializeField] private Transform defaultPosition;  // 练习模式定点

private string animalType;
private GameObject animalInstance;
private Animator animator;
private Transform[] positions;
private Transform launchPointAnchor;
private List<int> unusedIndices;
private Coroutine moveCoroutine;

public Transform CurrentLaunchPoint => launchPointAnchor;
```

**Initialize：**

```csharp
public void Initialize(string type)
{
    animalType = type;

    GameObject prefab = animalType switch
    {
        "CHICK" => chickPrefab,
        "SHEEP" => sheepPrefab,
        _       => cowPrefab
    };

    animalInstance = Instantiate(prefab, transform);
    animator = animalInstance.GetComponentInChildren<Animator>();

    // 碰撞隔离
    int animalLayer = LayerMask.NameToLayer("Animal");
    foreach (Transform t in animalInstance.GetComponentsInChildren<Transform>(true))
        t.gameObject.layer = animalLayer;

    // 发球锚点
    Vector3 anchorOffset = animalType switch
    {
        "CHICK" => chickAnchorOffset,
        "SHEEP" => sheepAnchorOffset,
        _       => cowAnchorOffset
    };
    GameObject anchor = new GameObject("LaunchPoint");
    anchor.transform.SetParent(animalInstance.transform);
    anchor.transform.localPosition = anchorOffset;
    launchPointAnchor = anchor.transform;

    // 位置
    ReadPositionsFromScene();

    if (positions.Length > 0)
    {
        unusedIndices = new List<int>(positions.Length);
        for (int i = 0; i < positions.Length; i++)
            unusedIndices.Add(i);

        int startIdx = Random.Range(0, positions.Length);
        animalInstance.transform.position = positions[startIdx].position;
        unusedIndices.Remove(startIdx);
    }
    else
    {
        animalInstance.transform.position = defaultPosition.position;
    }

    FaceHomePlate();
}
```

**读场景位置：**

```csharp
private void ReadPositionsFromScene()
{
    GameObject container = GameObject.Find("PitcherPositions");
    if (container == null)
    {
        positions = new Transform[0];
        return;
    }

    int count = container.transform.childCount;
    positions = new Transform[count];
    for (int i = 0; i < count; i++)
        positions[i] = container.transform.GetChild(i);
}
```

**移动逻辑：**

```csharp
public IEnumerator MoveToNextPosition()
{
    if (positions.Length == 0)
        yield break;

    if (unusedIndices.Count == 0)
    {
        for (int i = 0; i < positions.Length; i++)
            unusedIndices.Add(i);
    }

    int pickIndex = Random.Range(0, unusedIndices.Count);
    int pick = unusedIndices[pickIndex];
    unusedIndices.RemoveAt(pickIndex);

    Vector3 target = positions[pick].position;

    if (animator != null)
        animator.Play("Run");

    StopCoroutine(moveCoroutine);
    moveCoroutine = StartCoroutine(MoveToTarget(target));
    yield return moveCoroutine;
    moveCoroutine = null;

    // 5. 过渡回 Idle，面朝本垒
    FaceHomePlate();
    if (animator != null)
        animator.CrossFade("Idle_A", 0.15f);
}

private IEnumerator MoveToTarget(Vector3 target)
{
    while (Vector3.Distance(animalInstance.transform.position, target) > 0.05f)
    {
        animalInstance.transform.position = Vector3.MoveTowards(
            animalInstance.transform.position, target, moveSpeed * Time.deltaTime);
        // 面向移动方向
        Vector3 dir = target - animalInstance.transform.position;
        dir.y = 0;
        if (dir.magnitude > 0.01f)
            animalInstance.transform.rotation = Quaternion.LookRotation(dir);
        yield return null;
    }
}

private void FaceHomePlate()
{
    if (homePlate == null) return;
    Vector3 dir = homePlate.position - animalInstance.transform.position;
    dir.y = 0;
    if (dir.magnitude > 0.01f)
        animalInstance.transform.rotation = Quaternion.LookRotation(dir);
}
```

**发球动画：**

```csharp
public void PlayPitchAnimation()
{
    if (animator != null)
        animator.Play("Attack");
}
```

**清理：**

```csharp
public void Cleanup()
{
    StopCoroutine(moveCoroutine);
    moveCoroutine = null;

    FindObjectOfType<Pitcher>()?.ResetLaunchPoint();

    if (animalInstance != null)
        Destroy(animalInstance);

    animalType = null;
    animalInstance = null;
    animator = null;
    launchPointAnchor = null;
    positions = null;
    unusedIndices = null;
}

/// <summary>
/// 停止正在进行的移动（不销毁实例，由 AbortRound 调用）。
/// </summary>
public void StopMovement()
{
    StopCoroutine(moveCoroutine);
    moveCoroutine = null;
}
```

**Prefab 引用方案：** 不走 `Resources.Load`（路径不存在），改为 `[SerializeField]` 引用，在 Inspector 中手动拖入三个动物 Prefab。

---

## 第四步：集成到回合循环

### 4.1 ClassicModeRoundManager.cs — 修改

```
路径: Assets/__Scripts/GamePlay/ClassicModeRoundManager.cs
```

**新增字段：**

```csharp
[Header("动物投手")]
[SerializeField] private AnimalPitcher animalPitcher;
[SerializeField] private float prePitchAnimationDelay = 0.3f; // 发球动画前摇
```

**Awake() 补自动查找：**

```csharp
void Awake()
{
    if (pitcher == null) pitcher = FindObjectOfType<Pitcher>();
    if (hitJudge == null) hitJudge = FindObjectOfType<HitJudge>();
    if (animalPitcher == null) animalPitcher = FindObjectOfType<AnimalPitcher>();
}
```

**PitchSequence() 改动：** 两种模式行为不同：

| 模式 | 动物行为 |
|------|---------|
| Classic | 倒数期间跑位 → 更新发球点 → 播放 Attack 动画 → 发球 |
| FreePractice | 仅更新发球点（动物定点不动，无动画）→ 发球 |

```csharp
private IEnumerator PitchSequence()
{
    bool useAnimal = animalPitcher != null;
    bool useMovement = useAnimal
        && GameManager.Instance.CurrentMode == GameManager.GameMode.Classic;

    // 1. 第 N 球提示
    SetPhase(RoundPhase.Countdown);
    OnBallNumberUpdate?.Invoke(currentBall, ballsPerRound);

    // 2. 经典模式：动物提前跑位（与倒数并行）
    //    练习模式：跳过跑位，动物停在定点
    Coroutine moveCoroutine = null;
    if (useMovement)
        moveCoroutine = StartCoroutine(animalPitcher.MoveToNextPosition());

    // 3. 倒数 3-2-1
    for (int i = 3; i >= 1; i--)
    {
        OnCountdownTick?.Invoke(i);
        yield return new WaitForSeconds(countdownInterval);
    }
    OnCountdownTick?.Invoke(0); // "Go!"

    // 4. 等待动物到达（此时应该已到或即将到达）
    if (moveCoroutine != null)
        yield return moveCoroutine;

    // 5. 更新发球点
    if (useAnimal)
    {
        pitcher.SetLaunchPoint(animalPitcher.CurrentLaunchPoint);
        // 经典模式：播发球动画 + 前摇；练习模式：直接发球
        if (useMovement)
        {
            animalPitcher.PlayPitchAnimation();
            yield return new WaitForSeconds(prePitchAnimationDelay);
        }
    }

    // 6. 发球
    SetPhase(RoundPhase.Pitching);
    pitcher.ResetBall();
    yield return null;
    pitcher.PitchBall();

    // 7. 等待结果（不变）
    SetPhase(RoundPhase.WaitingResult);
    // ... 后续不变 ...
}
```

**AbortRound() 补充清理：**

```csharp
public void AbortRound()
{
    StopRoundCoroutine();
    animalPitcher?.StopMovement();  // 新增：停止动物移动协程
    if (pitcher != null) pitcher.ResetBall();
    SetPhase(RoundPhase.Idle);
    OnPlayerQuit?.Invoke();
}
```

---

## 第五步：修复硬编码动物名

### 5.1 ResultCoordinator.cs — 修改 1 行

```
路径: Assets/__Scripts/GamePlay/ResultCoordinator.cs
```

```csharp
// 改前:
private string animalName = "COW"; // 后续从选角系统传入

// 改后:
private string animalName => GameManager.Instance?.SelectedAnimalType ?? "COW";
```

### 5.2 SettlementPanel.cs — 修改 1 行

```
路径: Assets/__Scripts/UI/SettlementPanel.cs
```

SettlementPanel 已有 `[SerializeField] private GameManager gameManager` 引用，直接使用：

```csharp
// 改前:
private string animalName = "COW"; // 后续从选角系统传入

// 改后:
private string animalName => gameManager != null ? gameManager.SelectedAnimalType : "COW";
```

---

## 第六步：场景设置（人工完成，本文档仅记录）

### PitcherPositions

```
层级: [击球场景根] / PitcherPositions
类型: 空 GameObject
子节点: 10 个空 Transform，命名 Pos_01 ~ Pos_10

布局:
  击球者 (HomePlate)
       |
  P1   P2   P3   P4   P5     ← 近排，距 HomePlate 约 4m
     P6   P7   P8   P9  P10   ← 远排，距 HomePlate 约 6m

  半弧形排列。具体坐标在 Editor 中根据实际场景比例调整。
```

### Inspector 引用配置

| 组件 | 字段 | 目标 |
|------|------|------|
| AnimalPitcher | cowPrefab | `Assets/__Animals自制/Prefabs/cow.prefab` |
| AnimalPitcher | chickPrefab | `Assets/__Animals自制/Prefabs/chick.prefab` |
| AnimalPitcher | sheepPrefab | `Assets/__Animals自制/Prefabs/sheep.prefab` |
| AnimalPitcher | homePlate | HitJudge 组件的 homePlate Transform |
| AnimalPitcher | defaultPosition | 练习模式动物定点位置（一个空 Transform）|
| ClassicModeRoundManager | animalPitcher | 场景中的 AnimalPitcher 组件 |

### Animal 物理层

在 `Tags & Layers` 中新建名为 `Animal` 的 Layer。确保：
- Bat 的 LayerMask **不含** Animal
- Ball 的 `batLayers` 和 `groundLayers` **不含** Animal
- Animal 实例化后自动设置为该 Layer

---

## 验证标准

1. **动物传递**：Room 选不同动物 → 进入经典模式 → 场上出现对应动物
2. **位置随机**：连续 10 球，动物每次位置不同，10 球覆盖全部 10 个位置
3. **发球绑定**：球从动物当前发球锚点飞出
4. **动画播放**：倒数时动物跑位（Run），"Go!"后动物发球（Attack）
5. **时序并行**：动物跑位与 3-2-1 倒数同步进行，Go 后无多余等待
6. **排行榜**：结算时显示正确动物名
7. **Play Again**：第二轮动物不重复创建，位置池重置
8. **中断清理**：按 Backspace/菜单退出后，动物停止移动，协程清理
9. **返回 Room**：动物被销毁，Pitcher launchPoint 恢复原始值
10. **降级**：场景无 PitcherPositions 或 AnimalPitcher 时，经典模式正常运行（固定发球点）
11. **Free Practice 动物定点**：自由练习模式动物出现但站在定点位置（defaultPosition），不跑位不播放 Attack 动画
12. **碰撞隔离**：球和棒不会碰撞到动物

---

## 风险与待定事项

| 项目 | 等级 | 说明 |
|------|------|------|
| Animator 参数添加 | **阻塞** | 第〇步必须在编码前完成，否则动画调用无效果 |
| Prefab 引用 | **阻塞** | Inspector 中拖入三个 Prefab，或从 VRSelectionManagerPoke 传引用 |
| 发球锚点微调 | 中 | 偏移量需在 Editor 中实测，各动物嘴部位置有差异 |
| 动物面朝方向 | 低 | 已在代码中使用 FaceHomePlate 处理 |
| 移动速度调优 | 低 | `moveSpeed = 4f` 是起始值，需根据位置间距实测调整 |
| 动画过渡时长 | 低 | 0.1-0.2s CrossFade 值与实际动画 clip 时长相关 |
