# 开发进度

## 今日目标

> 经典模式跑通：玩家进入击球区 → 点开始 → 10 球发球/击球/计分 → 结束。一整局可玩。

---

## 已完成（已验证）

| 模块 | 做了什么 | 状态 |
|------|----------|------|
| HitJudge.cs | 加 `LastHitDistance` 属性 | ✅ 已测 |
| Pitcher.cs | 加 `PitchWithSpeed()` 接口；`PitchBall` 保持原样 | ✅ 已测 |
| LeaderboardManager.cs | JSON 排行榜存储，Top 20，增删查 | ✅ 已测 |

---

## 今日要跑通的核心模块

| 模块 | 职责 | 状态 |
|------|------|------|
| **ClassicModeRoundManager.cs** | 10 球回合循环、倒数发球、计分、Editor 模拟 | 🔧 调试中 |
| **DerbyScoreboard.cs** | World Canvas 计分板（倒数/球数/得分/结果） | 🔧 待搭建 Canvas |

这两个是今天的主角。跑通后玩家就能打完整一局了。

---

## 已写好但今天暂不接入

这些脚本都写好了，等核心模式跑通后再逐个接入：

| 模块 | 职责 | 接入时机 |
|------|------|----------|
| GameManager.cs | 全局状态机（Room↔Batting↔Result） | 核心跑通后，需要做模式选择/结算流程时接入 |
| PausePanel.cs | 时停暂停面板（继续/退回） | GameManager 接入后 |
| SettlementPanel.cs | 结算面板（总分/排行榜/名字输入/再来一局） | GameManager + Leaderboard 接入后 |
| VirtualKeyboard.cs | World Canvas 虚拟键盘 | SettlementPanel 接入后 |

---

## 今日需要做的事

### 1. 确保 ClassicModeRoundManager 正常工作

- 球复位正常
- 每次发球球飞向击球区
- Editor Numpad 模拟击球结果正常
- 10 球结束自动停

### 2. 搭建 DerbyScoreboard Canvas

击球区建一个 World Space Canvas，挂 DerbyScoreboard，放 3 个 TMP_Text：
- `Ball Count Text` — 显示 "3 / 10"
- `Total Score Text` — 显示总分
- `Last Result Text` — 倒数时显示 3→2→1→⚾，命中后显示得分

### 3. 端到端跑一遍

```
Enter 开始 → 倒数 3-2-1 → 发球 → Numpad 模拟结果 → 计分 → 下一球 → ×10 → 结束
```

---

## 当前文件清单

### 新文件（我创建的）

```
GamePlay/
  ClassicModeRoundManager.cs   ← 今天核心
  LeaderboardManager.cs        ✅
  GameManager.cs               （暂未接入）

UI/
  DerbyScoreboard.cs           ← 今天核心
  PausePanel.cs                （暂未接入）
  SettlementPanel.cs           （暂未接入）
  VirtualKeyboard.cs           （暂未接入）

Document/
  GAMEPLAY_REDESIGN.md         ← 设计文档
  TEST_PLAN.md                 ← 测试计划
  DEVELOPMENT_PROGRESS.md      ← 本文件
```

### 修改过的旧文件

```
GamePlay/HitJudge.cs     — 加 LastHitDistance
GamePlay/Pitcher.cs      — 加 PitchWithSpeed()；PitchBall 恢复原样
```
