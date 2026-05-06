# 第一阶段 测试清单

## 已完成

1. HitJudge.cs ✅
2. Pitcher.cs ✅
3. LeaderboardManager.cs ✅

---

## 4. ClassicModeRoundManager.cs

### 前置搭建

**4a. 挂 ClassicModeRoundManager**
- 场景中新建空 GameObject，命名 "ClassicModeRoundManager"
- 挂 `ClassicModeRoundManager` 脚本
- 所有引用留空（自动 FindObjectOfType）

**4b. 搭 DerbyScoreboard（World Canvas）**
- 在击球区场景中创建 **World Space Canvas**
- Canvas 上挂 `DerbyScoreboard` 脚本
- Canvas 下建 **3 个 TMP_Text** 子物体，分别命名：BallCount、TotalScore、LastResult
- DerbyScoreboard Inspector 中：
  - `Ball Count Text` → 拖 BallCount
  - `Total Score Text` → 拖 TotalScore
  - `Last Result Text` → 拖 LastResult
  - `Round Manager` → 拖 ClassicModeRoundManager GameObject

**4c. 确认场景中已有**
- 带 Pitcher 组件的球体（launchPoint / targetPoint 已设好）
- 带 HitJudge 组件的 GameObject（homePlate / fieldDirectionRef 已设好）

### 测试步骤

| # | 操作 | 看哪里 | 预期 |
|---|------|--------|------|
| 4.1 | Play，按 **Enter** | Game 画面 | 屏幕为空，等 Idle 结束 |
| 4.2 | — | World Canvas | `BallCount` 显示 "1 / 10" |
| 4.3 | — | `LastResult` | 依次显示 **3 → 2 → 1 → ⚾** |
| 4.4 | — | 场景 | 球从 launchPoint 自动发出 |
| 4.5 | 球飞到目标后等 6 秒（超时） | `LastResult` | 显示 "未击中"（红色），`TotalScore` 不变 |
| 4.6 | — | `BallCount` | 自动进入下一球 "2 / 10" |
| 4.7 | 下一球发球后按 **Numpad 1** | `LastResult` | 显示 "全垒打! +xxx"（金色），`TotalScore` 增加 |
| 4.8 | 下一球按 **Numpad 2** | `LastResult` | 显示 "有效落地 +xxx"（绿色） |
| 4.9 | 下一球按 **Numpad 3** | `LastResult` | 显示 "界外"（橙色），`TotalScore` 不变 |
| 4.10 | 下一球按 **Numpad 0** | `LastResult` | 显示 "未击中"（红色） |
| 4.11 | 打到第 10 球结束 | `BallCount` | 显示 "结束"，`LastResult` "本局结束！" |
| 4.12 | 回合中按 **Backspace** | — | 中止，计数归零，可以按 Enter 重新开始 |

### 通过标准
- 10 球流程完整走通，无卡死
- 每种 Numpad 结果正确显示在 Canvas 上
- 球发出后落地自然滚动，不会被秒复位
- 中止后能重新开始

---

## 5. GameManager.cs

### 前置搭建

- 新建空 GameObject，命名 "GameManager"
- 挂 `GameManager` 脚本
- Inspector 拖入：
  - `View Transition` → ViewTransitionManager（场景中已有的）
  - `Round Manager` → ClassicModeRoundManager
  - `Leaderboard Manager` → LeaderboardManager
- **重要**：在 ClassicModeRoundManager 的 Inspector 里，把 `OnRoundEnded` 事件点 `+`，拖 GameManager，选 `GameManager.OnRoundEnded`

### 测试步骤

| # | 操作 | 预期 |
|---|------|------|
| 5.1 | Play | Console: 初始 Room 状态 |
| 5.2 | GameManager 右键 → "DEBUG: Go Batting" | 视角切到击球区 |
| 5.3 | 在击球区点"开始游戏"按钮（或直接按 Enter） | ClassicModeRoundManager 开始跑 10 球 |
| 5.4 | 10 球结束后 | Console: "[GameManager] Batting → Result" |
| 5.5 | 按 **Escape**（在 Batting 中） | Console: "[GameManager] Batting → Pause"，游戏时停 |
| 5.6 | 再按 **Escape** | Console: "[GameManager] Pause → Batting"，回合从暂停点恢复 |
| 5.7 | 暂停中 → GameManager 右键 "DEBUG: Go Room" | 回到房间，回合被清理 |
| 5.8 | Batting 中按 **Q** | 直接退出回 Room，回合被清理 |
| 5.9 | Result 状态 → GameManager 右键 "DEBUG: Go Batting" | 正常开始新一局（PlayAgain 逻辑） |

### 通过标准
- 5 个状态流转路径全部走通：Room→Batting、Batting→Pause→Batting、Batting→Pause→Room、Batting→Room(Q)、Batting→Result→Batting

---

## 6. PausePanel.cs

### 前置搭建

- 在击球区 World Canvas 下新建 Panel，挂 `PausePanel`
- Panel 下建：
  - 一个 TMP_Text（标题"暂停"）→ 拖到 `Title Text`
  - "继续" Button → OnClick 绑 `PausePanel.OnContinueClicked`
  - "退回房间" Button → OnClick 绑 `PausePanel.OnReturnToRoomClicked`
- `Game Manager` 引用拖入 GameManager

### 测试步骤

| # | 操作 | 预期 |
|---|------|------|
| 6.1 | Batting 中按 Escape | PausePanel 显示 |
| 6.2 | 点"继续" | PausePanel 隐藏，回合继续 |
| 6.3 | 再按 Escape，点"退回房间" | PausePanel 隐藏，回到 Room |

---

## 7. VirtualKeyboard.cs

### 前置搭建

- 在 World Canvas 下建 VirtualKeyboard Panel，挂 `VirtualKeyboard`
- 建一个 TMP_Text → 拖到 `Display Text`
- 建按钮：A-Z 26 个、0-9 10 个、Space、Backspace、Submit
- 每个字母按钮 OnClick 绑定对应方法（TypeA、TypeB...）
- Space → `TypeSpace`，Backspace → `Backspace`，Submit → `Submit`

### 测试步骤

| # | 操作 | 预期 |
|---|------|------|
| 7.1 | 点 T、E、S、T | displayText 显示 "TEST" |
| 7.2 | 点 Backspace | 变 "TES" |
| 7.3 | 点 Submit | `OnSubmit` 事件触发（需有其他脚本订阅验证） |
| 7.4 | 一直输入超过 12 个字符 | 停在第 12 个 |

---

## 8. SettlementPanel.cs

### 前置搭建

- 在房间 World Canvas 下建 SettlementPanel，挂 `SettlementPanel`
- 按脚本 SerializeField 列表创建所有子物体：
  - `Total Score Text` — 总分
  - `High Score Prompt Text` — 提示文字
  - `Rank Text` — 排名
  - `Name Input Group` — 包含 VirtualKeyboard + 跳过按钮
  - `Leaderboard Group` — 包含 `Leaderboard Entry Parent`（空Transform）
  - `Leaderboard Entry Prefab` — 一个 TMP_Text 预制体
  - `Action Buttons Group` — 包含"再来一局"和"返回房间"按钮
- Inspector 拖入：GameManager、LeaderboardManager、VirtualKeyboard 引用
- "再来一局" → `OnPlayAgainClicked`，"返回房间" → `OnReturnToRoomClicked`，"跳过" → `OnSkipClicked`

### 测试步骤

| # | 操作 | 预期 |
|---|------|------|
| 8.1 | 先清空排行榜（LeaderboardManager 右键 Clear All），加 15 条高分测试数据 | — |
| 8.2 | 打一局全 Numpad3 界外（0 分） | 结算面板显示总分 0，提示未进榜 |
| 8.3 | 排行榜列表显示 | Top 10 正确 |
| 8.4 | 名字输入区 | 不显示 |
| 8.5 | 点"再来一局" | 面板隐藏，进入新一局 |
| 8.6 | 清空排行榜，打一局全 Numpad1 全垒打（高分） | 提示进入前 10，显示名字输入 |
| 8.7 | VirtualKeyboard 输入 "ABC" 点 Submit | 排行榜新增 "ABC (COW) — 分数" |
| 8.8 | 点"返回房间" | 面板隐藏，回 Room |

---

## 已知局限

- Numpad 模拟不经过真实物理，距离是假数据
- `animalName` 硬编码 "COW"，选角接入后替换
- ViewTransitionManager 在无 XR 设备时可能报错但不影响核心逻辑测试
