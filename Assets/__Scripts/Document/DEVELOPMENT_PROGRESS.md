# 开发进度

## 当前状态

> 经典模式核心闭环已跑通。结算流程代码已写完，待场景搭建测试。

---

## 已完成（已验证）

| 模块 | 做了什么 | 状态 |
|------|----------|------|
| HitJudge.cs | 加 `LastHitDistance` 属性 | ✅ 已测 |
| Pitcher.cs | 加 `PitchWithSpeed()` 接口；`PitchBall` 保持原样 | ✅ 已测 |
| LeaderboardManager.cs | JSON 排行榜存储，Top 20，增删查，ClearAllEntries | ✅ 已测 |
| ClassicModeRoundManager.cs | 10 球回合循环、倒数发球、计分、Editor 模拟 | ✅ 已接入调试完成 |
| DerbyScoreboard.cs | World Canvas 计分板（倒数/球数/得分/结果/连击） | ✅ 已接入调试完成 |
| GameManager.cs | 全局状态机（Room↔Batting↔Pause↔Result），Canvas管理 | ✅ 已接入调试完成 |

---

## 新完成（代码已写，待场景测试）

| 模块 | 职责 | 状态 |
|------|------|------|
| KeyboardManager.cs | _VRKeyboard，+OnSubmit +OnInputChanged +Submit | ✅ 代码已写 |
| NameInputPanel.cs | 6格槽位 + 嵌入键盘 + 确认/跳过 | ✅ 代码已写 |
| LeaderboardPanel.cs | Top10 排行榜展示 + 再来一局/返回 | ✅ 代码已写 |
| ResultCoordinator.cs | 协调面板切换 + 数据写入 | ✅ 代码已写 |
| SettlementTestDriver.cs | 测试驱动：填充假数据/模拟高低分 | ✅ 代码已写 |

---

## 待场景搭建

用户自行在 Unity Editor 中：
1. 创建 `Test_Settlement.unity`（Screen Space Canvas）
2. 按 [SETTLEMENT_KEYBOARD_PLAN.md](SETTLEMENT_KEYBOARD_PLAN.md) 第三节搭建层级
3. 跑第四节 6 个测试用例

---

## 废弃

| 模块 | 原因 |
|------|------|
| SettlementPanel.cs | 被 NameInputPanel + LeaderboardPanel + ResultCoordinator 替代 |
| VirtualKeyboard.cs | 被 _VRKeyboard/KeyboardManager 替代 |
| _VRKeyboard/GazeRaycaster.cs | 不使用（手柄射线交互） |

---

## 当前文件清单

```
GamePlay/
  ClassicModeRoundManager.cs   ✅
  LeaderboardManager.cs        ✅
  GameManager.cs               ✅
  ResultCoordinator.cs         新建（待测）

UI/
  DerbyScoreboard.cs           ✅
  NameInputPanel.cs            新建（待测）
  LeaderboardPanel.cs          新建（待测）
  PausePanel.cs                （待接入）
  SettlementPanel.cs           （废弃）
  VirtualKeyboard.cs           （废弃）

Test/
  SettlementTestDriver.cs      新建（待测）

_VRKeyboard/Scripts/
  KeyboardManager.cs           [修改] +Submit/+InputChanged/+SetInputText

Document/
  GAMEPLAY_REDESIGN.md         ← 轻量化设计
  ROGUELIKE_DESIGN.md          ← Roguelike 设计
  SETTLEMENT_KEYBOARD_PLAN.md  ← 结算流程搭建+测试方案
  TEST_PLAN.md                 ← 测试计划
  DEVELOPMENT_PROGRESS.md      ← 本文件
```
