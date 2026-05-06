# 《Animal Baseball》玩法重构方案

## 背景

原设计目标是做一款相对完备的 VR 棒球游戏，包含击球、跑垒、投球、守备（接球+传球+追逐）四大环节。开发过程中发现工程量过大，且游戏的诙谐动物风格更适合轻量化、趣味化的方向。

因此决定**收缩交互范围**，以"纯挥棒击球"为核心，将游戏从"模拟完整棒球比赛"转向"棒球主题的击球挑战游戏"。

---

## 核心玩法定义

> 在微缩棒球桌上，用动物角色挥棒击球，挑战变化的球速/球路，打出最远距离，冲击排行榜。

**保留的交互**：挥棒击球（唯一核心操作）

**砍掉的内容**：跑垒、玩家主动投球、守备三段式交互、AI 对手球员、完整比赛状态机

---

## 第一阶段：经典模式（Classic Mode）

### 场景结构（已确认）

```
Guide Scene（教程引导，必经流程）
    │  IntroSequenceController 引导完成
    │  SceneTransitionFader 黑屏过渡
    │
    └──→ Main Scene（同一场景内双视角切换）
              │
              ├─ 小房间视角（默认起始，模式选择中枢）
              │    ├─ 角色选择交互（VRSelectionManagerPoke，保留但无属性差异）
              │    ├─ 排行榜 World Canvas（常驻）
              │    ├─ 游戏模式选择（第一阶段只有"经典模式"，第二阶段加"Roguelike 模式"）
              │    └─ 选择模式后 → 切换到击球区
              │
              ├─ 击球区视角（等比放大的巨大房间，玩家在桌上球场内）
              │    ├─ 根据所选模式加载不同规则
              │    ├─ 经典模式：AI 连续发球，玩家挥棒，10 球结算
              │    ├─ 计分板 World Canvas（常驻，显示当前球数/得分）
              │    └─ 允许随时退出回到小房间
              │
              └─ 视角切换由 ViewTransitionManager 同场景跨越完成（已有）
```

### 玩法循环

```
小房间
  ├─ 选角色（保留，纯外观）
  ├─ 看排行榜
  ├─ 选择模式（第一阶段仅"经典模式"）
  ├─ 确认 → 切到击球区
  │
击球区
  ├─ AI 发球（球速逐渐递增，击球失败时回落）
  ├─ 玩家挥棒
  ├─ 判定 + 计分
  ├─ 重复 10 次
  ├─ 按暂停键 → 时停 + 暂停面板（见下方）
  │
  └─ 10 球结束 → 切回小房间 → 结算面板弹出
```

### 暂停机制（已确认）

- **触发**：玩家按下手柄**菜单键（Menu Button）**暂停
- **表现**：
  - `Time.timeScale = 0`，游戏内一切物理/动画冻结
  - 玩家眼前（相机前方）浮现一个 World Space 暂停面板
  - 面板用已有的 PanelController 渐入方式展示
- **面板选项**：
  - **继续** → 关闭面板，`Time.timeScale = 1`，回到当前击球进度
  - **退回房间** → `Time.timeScale = 1`，放弃当前局，切回 RoomState
- **注意**：暂停期间 VR 手柄的射线交互不受 timeScale 影响（使用 `Time.unscaledDeltaTime`），确保面板按钮可点击

### 中途退出（B/Y 键）（已确认）

- 与暂停不同，B/Y 键是直接退出，不弹面板，不走结算
- 退出后回到小房间视角，当前局分数不保留

### 结算流程（已确认）

```
10球结束
  │
  ├─→ 切回小房间视角
  │
  ├─→ 弹出结算面板，显示总分
  │
  ├─→ 玩家点击"继续"
  │     │
  │     ├─ 如果分数进入 Top 10：
  │     │    ├─ 提示"你进入了前十名！"
  │     │    ├─ 弹出 World Canvas 虚拟键盘 → 输入名字（仅英文字符）或跳过
  │     │    ├─ 排行榜记录：玩家输入名 + 动物角色名
  │     │    └─ 保存到排行榜
  │     │
  │     └─ 如果未进入 Top 10：
  │          └─ 直接展示排行榜
  │
  └─→ 选项：
       ├─ "再来一局" → 重新切到击球区，开始新一局
       └─ "返回房间" → 关闭结算面板，停留在小房间
```

### 中途退出（已确认）

- 在击球区内，玩家按 VR 手柄的 **B 或 Y 按钮** 随时退出
- 退出后回到小房间视角
- 当前局的击球进度和分数**不保留**（直接放弃本局）
- 不调用结算流程，不写入排行榜

### 发球与回合节奏（已确认）

- **不区分好坏球**：每球都是好球，玩家只管挥棒
- **发球衔接流程**：
  ```
  上一球落地 → 判定 → 得分弹出动画（显示当次得分 + 总分）
       → 固定间隔后动画消失
       → 提示"第 N / 10 球"
       → 发球点出现 3、2、1 倒数动画
       → 自动发球
  ```
- 中间无玩家确认环节，纯自动化
- 全程可暂停（菜单键时停），不可调整

### 计分规则

| 判定结果 | 计分 |
|----------|------|
| 全垒打 | 球速系数 × 距离 × 200 |
| 有效落地 | 球速系数 × 距离 × 100 |
| 界外球 | 0 分 |
| 未击中（挥棒落空） | 0 分 |

> 注：**接杀**在经典模式中删除（无 AI 守备球员），保留到 Roguelike 模式中作为特殊规则使用。

**球速系数**：快球系数高（难打但回报大），慢球系数低（好打但分少）。具体参数后续调试确定，Pitcher 现有参数保留不动。

### 球速变化机制（已确认）

不发完全随机，而是**渐进递增 + 失败回落**，形成动态难度曲线：

```
初始速度（慢）
    │
    ├─ 击球成功（有效落地 / 全垒打）→ 下一球速度 +Δ
    ├─ 击球失败（界外 / 接杀 / 未击中）→ 下一球速度 − 回落一段
    │
    └─ 重复，10 球结束
```

- 速度用 Pitcher 的 `flightTime` 控制（值越小越快）
- 有上下限：最快不低于某个 flightTime，最慢不高于某个值
- 每次成功的 Δ 递增幅度一致
- 失败回落幅度 > 单次 Δ，让玩家有"喘息空间"但不会回到底部
- 节奏感受：越打越快 → 失误 → 慢下来重新适应 → 再加速

### 排行榜可见性（已确认）

- **小房间内**：World Canvas 常驻显示排行榜
- **击球区内**：计分板 World Canvas 常驻显示（当前球数 + 总分），排行榜可一并展示或折叠
- **结算时**：排行榜作为结算面板的一部分展示

### 角色选择（已确认）

- 第一阶段保留角色选择交互（VRSelectionManagerPoke）
- 在小房间内操作，纯外观选择，**无属性差异**
- 选中的角色外观带到击球区（你化身的就是这个小动物）

### 关于现有代码（已确认）

- **Passing.cs**：功能已废弃，后续可移除，第一阶段先不管
- **Pitcher.cs**：保留，其内部 Space/A 调试按键只在 Editor 生效，VR 端用 ClassicModeRoundManager 接管发球
- **所有键盘调试代码**：后续统一清理，第一阶段不处理

### 所需改动

| 文件 | 改动 |
|------|------|
| Pitcher.cs | 添加变速发球公开接口 |
| HitJudge.cs | 暴露落点距离供计分使用 |
| GameManager.cs（新） | 全局状态机 |
| ClassicModeRoundManager.cs（新） | 单局 10 球循环 + 计分 |
| LeaderboardManager.cs（新） | 排行榜存储与读取 |
| DerbyScoreboard.cs（新） | 击球区内计分板 UI |
| SettlementPanel.cs（新） | 结算面板（含排行榜展示 + 名字输入 + 操作按钮） |

### 不涉及

- 角色属性差异
- 多人/联网
- 复杂模式

---

## 第二阶段：Roguelike 模式扩展

### 设计理念

在第一阶段"变速击球→计分"的核心循环之上，叠加 **roguelike 元素**，让每局游戏都有不同的规则组合和成长路径，增加重复可玩性。

### Roguelike 要素拆分

#### 1. 关卡结构（Run 结构）

```
一轮 Run = 若干"回合"连续挑战

每回合 = 一组特殊规则 + 若干次击球机会

回合之间弹出"选择"（三选一 buff/debuff/规则变化）
```

与传统 roguelike 的"房间→选择→房间"类似，但这里的"房间"是击球回合。

#### 2. 回合内规则变化（类似 Slay the Spire 的"敌人意图"）

每回合开始时，揭示本回合的特殊条件：

**球路变化类**：
- 全是快球
- 全是慢球但界外判定变宽
- 左右随机偏移的变化球
- 连续两球同样速度（可预测）

**计分变化类**：
- 本回合全垒打分数翻倍
- 有效落地不计分（必须打全垒打）
- 连击有乘法加成

**限制/惩罚类**：
- 3 次击球机会（不是常规 10 次）
- 界外球扣分
- 好球区缩小

#### 3. 回合间选择（Buff/Debuff 三选一）

类似杀戮尖塔的"奖励选择"，但更轻量：

**正向 Buff 示例**：
- 力量提升：本 Run 剩余回合距离系数 +20%
- 精准打击：本 Run 全垒打判定距离降低 1m
- 稳定发挥：界外球不再消耗击球机会
- 金色时刻：每回合第一球必定为金色球（3 倍分）

**负面/风险类示例**：
- 选一个强力 Buff，但附带一个 Debuff（如"力量+30% 但好球区缩小"）
- "诅咒球"：每回合混入一个必定是坏球的投球，但你不知道是哪个

#### 4. 多层挑战结构

```
Run 开始
  │
  ├─ 回合 1（普通规则，热身）
  ├─ 选择 buff ──→ ┐
  ├─ 回合 2（+ 随机规则变化）  │
  ├─ 选择 buff ──→   │ 累计 buff
  ├─ 回合 3（+ 随机规则变化）  │
  ├─ ...            ┘
  ├─ 最终回合 / Boss 回合
  │
  └─ Run 结束 → 结算总分 → 排行榜（Roguelike 分类）
```

层数建议 5-8 回合为一个 Run，时长控制在 10-15 分钟。

#### 5. 元进度（Meta Progression）

即使 Run 失败了，也有积累：

- 累计全垒打数解锁新球棒外观/球衣配色（JerseyPack 复用）
- 累计分数解锁新的 Buff 选项加入随机池
- 解锁新动物角色（初始只有 1 个，逐步解锁 3 个）

---

## 迭代路线总览

```
第一阶段              第二阶段               后续
──────────────────────────────────────────────────
经典模式              Roguelike 模式
                      
变速击球    ────→    回合制 Run              外观解锁系统
10球结算            Buff/Debuff 选择          动物角色属性差异
本地排行榜           多种规则变化              每日挑战
                     元进度解锁                轮流赛模式
                     独立排行榜
```

---

## 架构设计：GameManager 层级

### 当前状态

DESIGN.md 规定了"由 GameManager 统一管理状态跳转"，但目前代码中**没有 GameManager**。现有的几个 Manager（PicoInputManager、ViewTransitionManager、SelectionManager、JerseyManager）都是领域专属的，互不统属，缺乏全局调度。

### 需要补两层管理

```
GameManager (全局状态机)          ← 缺失
  ├─ 小房间（角色选择 + 排行榜）
  ├─ 击球区（比赛中）
  │    └─ ClassicModeRoundManager  ← 缺失（单局 10 球循环）
  └─ 结算
```

### GameManager 职责

- 管理游戏顶层生命周期：Room ↔ Batting ↔ Result
- 确保同一时刻只处于一个状态（通过状态机回调驱动，禁止 Update 中 if 轮询）
- 负责状态切换时的全局协调：调用 ViewTransitionManager 切视角、切换 BGM、显示/隐藏对应 UI
- 持有全局单例服务的引用（PicoInputManager 等），为子状态提供上下文
- 不处理具体玩法逻辑——那是 ClassicModeRoundManager 的事

### 状态结构（已确认，含第二阶段规划）

```
GameManager
  │
  ├─ RoomState                — 小房间（默认起始，模式选择中枢）
  │    ├─ 角色选择交互（VRSelectionManagerPoke）
  │    ├─ 排行榜 World Canvas 常驻
  │    ├─ 模式选择（第一阶段仅"经典模式"，第二阶段加"Roguelike 模式"）
  │    ├─ 选择模式后 → 切到击球区，按对应规则开始
  │    └─ 教程入口（如有）
  │
  ├─ BattingState             — 击球区（第一阶段核心）
  │    ├─ 内部流程：发球 → 等待击球 → 判定 → 计分 → 循环 10 次
  │    ├─ 计分板 World Canvas 常驻
  │    ├─ B/Y 键 → 直接退出回 RoomState（不保留分数）
  │    ├─ 菜单键 → 进入 PauseState（时停 + 暂停面板）
  │    └─ 由 ClassicModeRoundManager 负责单局内循环
  │
  ├─ PauseState               — 暂停（时停叠加层）
  │    ├─ Time.timeScale = 0，物理/动画冻结
  │    ├─ 相机前方浮现暂停面板（World Space）
  │    ├─ "继续" → Time.timeScale = 1，回到 BattingState
  │    └─ "退回房间" → Time.timeScale = 1，切回 RoomState
  │
  ├─ ResultState              — 结算（切回小房间）
  │    ├─ 展示总分 → 点击继续
  │    ├─ 进入 Top 10 → World Canvas 虚拟键盘输入名字（仅英文）/ 跳过
  │    ├─ 展示排行榜
  │    ├─ "再来一局" → 切回 BattingState
  │    └─ "返回房间" → 切回 RoomState
  │
  └─ (第二阶段) RoguelikeModeState  — Roguelike 模式
       ├─ 内部流程：回合 → 选择 Buff → 回合 → ... → Run 结束
       └─ 由 RoguelikeRunManager 负责 Run 内的回合+选择循环
```

### GameManager 与子管理器的关系

- GameManager 只负责**状态切换决策**（"现在去击球区"还是"现在去结算"），不关心状态内部的玩法细节
- 进入 BattingState 时调用 ClassicModeRoundManager.StartRound()
- ClassicModeRoundManager 通过**事件**通知 GameManager：
  - `RoundEnded(int totalScore)` — 10 球结束，GameManager 收到后切到 ResultState
  - `PlayerQuit()` — 玩家中途按 B/Y 退出，GameManager 收到后切回 RoomState
- GameManager 收到事件后决定下一个状态，这遵循设计规范里的"事件总线驱动 + 状态机管理"原则

### GameManager 状态机实现方式

DESIGN.md 规范要求："禁止在 Update 中通过 if 轮询判断状态，应利用状态切换时的回调函数执行业务逻辑。"

用一个轻量枚举状态机：

```csharp
enum GameState { Room, Batting, Pause, Result }

void TransitionTo(GameState newState) {
    OnExit(currentState);    // 清理当前状态
    currentState = newState;
    OnEnter(currentState);   // 初始化新状态
}
```

- `OnEnter(Room)` → 激活小房间 UI + 角色选择 + 排行榜 + 模式选择
- `OnExit(Room)` → 隐藏房间 UI
- `OnEnter(Batting)` → ViewTransition 切换到击球区 + 启动 ClassicModeRoundManager
- `OnExit(Batting)` → 清理单局数据 + ViewTransition 切回小房间
- `OnEnter(Pause)` → Time.timeScale = 0 + 显示暂停面板
- `OnExit(Pause)` → Time.timeScale = 1 + 隐藏暂停面板
- `OnEnter(Result)` → 弹出结算面板
- `OnExit(Result)` → 关闭结算面板

### 第一阶段具体改动

新建脚本列表：

| 新增模块 | 职责 |
|----------|------|
| GameManager.cs | 全局状态机，管理 Menu → Classic → Result 的跳转 |
| ClassicModeRoundManager.cs | 单局 10 球循环（即之前说的 HomeRunDerbyManager，职责不变） |

之前提的 HomeRunDerbyManager 改名为 ClassicModeRoundManager，职责更明确——只管一局内部的循环，不管全局。

---

## 技术考量

### 第一阶段可直接复用的现有模块

| 模块 | 复用方式 |
|------|----------|
| Pitcher.cs 击球物理 | 添加变速接口，其余不动 |
| HitJudge.cs 判定 | 暴露距离属性，其余不动 |
| HitResultPopup 弹窗 | 直接复用，显示分数 |
| BallTrailController 拖尾 | 无需改动 |
| ViewTransitionManager 视角切换 | 无需改动 |
| PanelController UI 淡入淡出 | 计分板参考其模式 |
| JerseyManager/JerseyPack 球衣 | 先不用，第二阶段解锁系统再用 |
| VRSelectionManagerPoke 角色选择 | 先不用，第二阶段角色差异再用 |

### 第一阶段新建模块

| 模块 | 职责 |
|------|------|
| GameManager | 全局状态机，管理小房间 ↔ 击球区 ↔ 结算的跳转 |
| ClassicModeRoundManager | 单局 10 球循环，随机球速，计分，监听中途退出 |
| LeaderboardManager | JSON 排行榜存储与读取，Top 10 判定 |
| DerbyScoreboard | 击球区内 World Space 计分板（当前球数 + 总分 + 排行榜） |
| SettlementPanel | 结算面板（总分 + 排行榜展示 + 名字输入 + "再来一局"/"返回房间"） |

---

## 待讨论

- Roguelike 模式的 Run 长度（5 回合还是 8 回合？）
- 元进度解锁的具体节奏
- 是否需要"连击"作为基础机制（即使经典模式也可用）
- 排行榜是否分模式（经典 / Roguelike 分开排名）
