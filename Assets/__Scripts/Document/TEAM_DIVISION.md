# 小组分工 & 代码归属表

> 目标：每位组员清楚自己负责的脚本、所在场景、实现内容和思路，能独立向老师汇报。

---

## 分工总览

| 编号 | 模块 | 负责人 | 涉及场景 |
|------|------|--------|----------|
| 1 | VR 虚拟键盘 | 组员 A | Test_Settlement, Indoor Scene |
| 2 | 排行榜信息存储 | 组员 B | Indoor Scene |
| 3 | 棒球物理 & 落地判定 | 组员 C | Indoor Scene |
| 4 | GUI & 教学引导 | 组员 D | GUIDE Scene, Indoor Scene |

---

## 模块 1：VR 虚拟键盘

### 涉及脚本

| 脚本 | 路径 | 职责 |
|------|------|------|
| `KeyboardManager` | `_VRKeyboard/Scripts/KeyboardManager.cs` | 键盘核心：输入管理、大小写切换、事件派发、最大长度限制 |
| `Key` | `_VRKeyboard/Scripts/Keys/Key.cs` | 按键基类：绑定 Button.onClick → 触发 OnKeyClicked 事件 |
| `Alphabet` | `_VRKeyboard/Scripts/Keys/Alphabet.cs` | 字母键：支持 CapsLock 大小写切换 |
| `Number` | `_VRKeyboard/Scripts/Keys/Number.cs` | 数字键 |
| `Symbol` | `_VRKeyboard/Scripts/Keys/Symbol.cs` | 符号键（项目中未启用） |
| `Shift` | `_VRKeyboard/Scripts/Keys/Shift.cs` | Shift 键：主字符与副字符互换 |
| `GazeRaycaster` | `_VRKeyboard/Scripts/GazeRaycaster.cs` | 注视射线交互（项目中未使用，保留备用） |
| `NameInputPanel` | `__Scripts/UI/NameInputPanel.cs` | 名字输入面板：6格槽位 + 嵌入键盘 + 确认/跳过 |

### 实现了什么

- 第三方 VR 键盘资产 `_VRKeyboard` 的导入和适配
- 按键从注视交互改为 **手柄射线 + 扳机** 交互（Pico VR 适配）
- 精简键盘布局：仅保留 A-Z / 0-9 / 退格 / 确认
- **6 位字符限制**的名字输入面板，带槽位实时刷新
- 每次打开自动清空，淡入淡出动画
- 确认和跳过两种提交方式

### 实现思路

1. 下载 `_VRKeyboard` 资产，理解其事件驱动架构：每个按键是一个 `Key` 子类，点击时通过 `OnKeyClicked` 事件向 `KeyboardManager` 发送字符
2. 在 `KeyboardManager` 上扩展 `OnSubmit` 和 `OnInputChanged` 事件，使其支持"确认提交"和"实时输入变化"两种通知
3. 编写 `NameInputPanel`，用 `Update()` 轮询 `keyboardManager.inputText.text` 变化来刷新 6 个槽位（避免事件时序问题）
4. 精简 Prefab 中不需要的按键，调整 Canvas 为 World Space + TrackedDeviceGraphicRaycaster 以支持手柄交互

### 涉及场景

- `Test_Settlement.unity`（非 VR 测试场景，鼠标点键盘）
- `Indoor Scene (MAIN).unity`（房间区域的 NameInputPanel）

---

## 模块 2：排行榜信息存储

### 涉及脚本

| 脚本 | 路径 | 职责 |
|------|------|------|
| `LeaderboardManager` | `__Scripts/GamePlay/LeaderboardManager.cs` | 排行榜核心：JSON 本地持久化、增删查、Top N 查询、排名判定 |
| `LeaderboardEntry` | 同上文件 | 数据模型：玩家名、动物名、分数、日期 |
| `LeaderboardPanel` | `__Scripts/UI/LeaderboardPanel.cs` | 排行榜展示面板：10 行榜单、字段可选显示、玩家高亮 |
| `ResultCoordinator` | `__Scripts/GamePlay/ResultCoordinator.cs` | 结算流程协调器：判定是否进榜 → 输入名字 → 写入数据 → 展示榜单 |
| `SettlementTestDriver` | `__Scripts/Test/SettlementTestDriver.cs` | 非 VR 测试驱动：填充假数据、模拟高低分（仅测试用） |

### 实现了什么

- **JSON 本地存储**：排行榜数据持久化到 `Application.persistentDataPath/derby_leaderboard.json`，跨游戏会话保留
- **Top 20 排序**：按分数降序自动插入，超出 20 条自动裁剪
- **进榜判定**：`IsHighScore(score, topN)` 判断是否进入前 N 名
- **排名查询**：`GetRank(score)` 返回当前分数在榜中的位置
- **双面板架构**：
  - 房间常驻排行榜（`refreshOnRoomEnter=true`）：每次进入房间自动刷新，无高亮
  - 结算排行榜（`refreshOnRoomEnter=false`）：游戏结束后弹出，本局排名高亮白色 + 深色背景
- **5 个可选显示字段**：排名 / 玩家名 / 动物 / 分数 / 日期，Inspector 勾选控制
- **完整结算流程**：判定 → 输入名字 → 写入 → 展示 → 再来一局/返回房间
- 淡入淡出动画

### 实现思路

1. `LeaderboardManager` 使用 `JsonUtility` 序列化/反序列化，`Application.persistentDataPath` 确保 Pico 设备上路径正确
2. 数据按分数降序维护，`AddEntry` 时用 `FindIndex` 找到插入位置
3. `ResultCoordinator` 作为协调器，订阅 `GameManager.OnShowResult` 事件，在游戏结束时触发结算流程
4. `LeaderboardPanel` 通过 `CanvasGroup` 控制淡入淡出，`refreshOnRoomEnter` 开关区分房间常驻和结算两种用途
5. 玩家本局排名通过 `Show(highlightRank)` 传入，匹配的行切换颜色并激活背景

### 涉及场景

- `Indoor Scene (MAIN).unity`（房间常驻排行榜 + 结算排行榜）
- `Test_Settlement.unity`（测试用）

---

## 模块 3：棒球物理 & 落地判定

### 涉及脚本

| 脚本 | 路径 | 职责 |
|------|------|------|
| `Pitcher` | `__Scripts/GamePlay/Pitcher.cs` | 发球系统：抛物线初速度计算、微缩重力模拟、击球碰撞响应、出球速度辅助 |
| `HitJudge` | `__Scripts/GamePlay/HitJudge.cs` | 击球判定：界内/界外/全垒打/接杀，落点距离和角度计算 |
| `Ball` | `__Scripts/GamePlay/Ball.cs` | 棒球标记脚本（当前为空壳，后续可扩展） |
| `BatCapsule` | `__Scripts/BatRelated/BatCapsule.cs` | 球棒碰撞体：用于检测棒球碰撞 |
| `BatCapsuleFollower` | `__Scripts/BatRelated/BatCapsuleFollower.cs` | 球棒追踪：驱动物理球棒跟随手柄运动，提供速度数据 |
| `ClassicModeRoundManager` | `__Scripts/GamePlay/ClassicModeRoundManager.cs` | 10 球回合管理：倒计时发球 → 等待结果 → 计分 → 连击增益（调用 Pitcher 和 HitJudge） |

### 实现了什么

**发球系统 (Pitcher)**：
- **微缩物理**：自定义重力加速度 `microGravityScale`，适配桌面微缩场景（重力仅正常值的 10%）
- **抛物线计算**：根据 `launchPoint` 和 `targetPoint` 的本地坐标，反推出精准的抛物线初速度
- **击球碰撞系统**：
  - 检测球棒碰撞，读取 `BatCapsuleFollower` 的挥棒速度
  - 根据挥棒速度计算出球速度（弱击辅助 → 完整击球 → 封顶上限）
  - 出球方向混合：球场方向 + 碰撞反弹方向（`reflectionDirectionWeight` 控制比例）
  - 击球后额外向上速度 `upwardBoost`，确保球能起飞
  - 击球冷却 `hitCooldown`，防止连续碰撞
- **地面状态**：球落地后增大阻力和旋转阻力，模拟自然滚动停止
- **安全复位**：球掉出场景自动回到发球点

**击球判定 (HitJudge)**：
- **扇形界内区**：以本垒为顶点，`fairFoulHalfAngle`（默认 45°）为半角
- **判定结果**：
  - `None` — 距离不足（< fairLandingMinDistance）
  - `Foul` — 超出扇形范围
  - `FairLanding` — 界内有效落地
  - `HomeRun` — 距离超过 `homeRunMinDistance` 且落点高度 ≤ `homeRunMaxLandingHeight`
  - `Caught` — 空中接杀（Roguelike 模式预留，经典模式中不使用）
- **Gizmos 可视化**：Scene 视图中绘制扇形界内区、最短/最远距离圆环

**回合管理 (ClassicModeRoundManager)**：
- 10 球循环：倒计时 3-2-1 → 发球 → 等待结果（未击中超时 6s / 击中等待落地 20s）→ 计分
- **连击增益**：连续击中累加 combo，3连×1.2、4连×1.5、5连×1.6。未击中重置
- **Editor 测试**：Numpad 1-3 模拟不同击球结果，0 模拟未击中

### 实现思路

1. **发球**：通过运动学公式反推初速度。给定位移 d、飞行时间 t、重力 g，水平速度 = d_xz / t，垂直速度 = d_y/t - 0.5gt
2. **击球**：使用 `OnCollisionEnter` 检测球棒碰撞，物理反弹方向 + 球场方向混合得到出球方向，挥棒速度映射到出球速度
3. **判定**：球落地时（与 Ground Layer 碰撞），以本垒为原点计算落点角度和距离，对照阈值得出结果
4. **回合**：协程驱动的状态机（Idle → Countdown → Pitching → WaitingResult → Scoring → 下一球），事件通知 UI 更新

### 涉及场景

- `Indoor Scene (MAIN).unity`（击球区，核心 Gameplay）

---

## 模块 4：GUI & 教学引导（GUIDE Scene）

### 涉及脚本

| 脚本 | 路径 | 职责 |
|------|------|------|
| `IntroSequenceController` | `__Scripts/Tutorial/IntroSequenceController.cs` | 教学引导主控：13 步分阶段教程流程 |
| `SceneTransitionFader` | `__Scripts/Tutorial/SceneTransitionFader.cs` | 场景过渡：黑屏淡入淡出 + XR Origin 定位到房间 |
| `CanvasGroupFader` | `__Scripts/Tutorial/CanvasGroupFader.cs` | Canvas 淡入淡出工具组件 |
| `GuideTeleportArrivalTrigger` | `__Scripts/Tutorial/GuideTeleportArrivalTrigger.cs` | 传送到达检测：玩家传送到指定位置后触发下一步 |
| `BillboardFaceCamera` | `__Scripts/Tutorial/BillboardFaceCamera.cs` | 广告牌效果：UI 始终面向相机 |
| `IndoorSceneXRRuntimeBinder` | `__Scripts/Tutorial/IndoorSceneXRRuntimeBinder.cs` | XR 运行时绑定：自动查找并绑定 XR 组件 |
| `PersistentXRCore` | `__Scripts/Tutorial/PersistentXRCore.cs` | XR 持久化：跨场景保留 XR Rig |
| `XRControllerDeferredActivator` | `__Scripts/Tutorial/XRControllerDeferredActivator.cs` | 手柄延迟激活（避免过早初始化） |
| `TrackingOriginFixer` | `__Scripts/TrackingOriginFixer.cs` | 追踪原点修正 |
| `PanelController` | `__Scripts/UI/PanelController.cs` | 面板通用控制：Canvas 显示/隐藏 |
| `DropdownController` | `__Scripts/UI/DropdownController.cs` | 下拉菜单控制 |

### 实现了什么

**13 步分阶段教程**：
- 欢迎文字 → 展示面板 → 转向教学（左/右）→ 确认转向 → 传送教学（两个传送点）→ 传送至角色桌 → 确认到达 → 进入游戏
- 每步有独立的文字提示、UI 面板显示
- 每个"等待确认"阶段由玩家操作推进（按钮点击 / 传送到位检测）
- 支持跳过（X 键一键进入主场景）和重播（Y 键重新开始）

**场景过渡**：
- 教程结束后黑屏淡出 → 加载 Indoor Scene → 定位 XR Origin 到房间出生点 → 淡入
- 程序化生成翻转球体覆盖层 + 自定义 Shader 实现全屏淡入淡出
- 过渡期间停用 TrackedDeviceGraphicRaycaster，防止 XRI 报错

**XR 运行时支持**：
- `PersistentXRCore`：XR Rig 跨场景保留，避免重复初始化
- `IndoorSceneXRRuntimeBinder`：场景加载后自动查找 XR 组件并绑定
- `XRControllerDeferredActivator`：延迟手柄激活，确保场景就绪后再初始化
- `TrackingOriginFixer`：修正追踪原点偏移

**UI 辅助工具**：
- `CanvasGroupFader`：CanvasGroup alpha 淡入淡出，支持同时驱动子 SpriteRenderer
- `BillboardFaceCamera`：World Canvas 始终朝向玩家视线
- `PanelController`：通用面板显隐控制

### 实现思路

1. **教程状态机**：`GuideStage` 枚举定义 13 个阶段，每阶段在 `Update()` 中检查推进条件（玩家确认/传送到位/面板展示完毕），满足后进入下一阶段
2. **场景过渡**：翻转球体 Mesh + `Custom/ScreenFade` Shader 挂在相机下，通过 alpha 插值实现全屏遮罩淡入淡出。淡出期间执行异步场景加载，完成后淡入
3. **XR 绑定**：`FindObjectOfType` 自动查找 XR Origin / XR Camera / Controller 等组件并关联，无需手动 Inspector 拖拽
4. **传送检测**：`GuideTeleportArrivalTrigger` 挂载到传送目标点，使用 `OnTriggerEnter` 检测 XR Origin 是否到达

### 涉及场景

- `GUIDE Scene.unity`（教程引导，全程）

### 备注

双视角切换 (`ViewTransitionManager`)、游戏状态机 (`GameManager`)、输入系统 (`PicoInputManager`)、角色选择 (`SelectionManager`) 等为组长独立负责，不在本模块范围内。

---

## 汇报要点建议

每位组员向老师汇报时，按以下结构：

1. **我负责什么模块**（一句话）
2. **用到了哪些脚本**（列文件名）
3. **实现了什么功能**（挑 2-3 个亮点）
4. **怎么实现的**（核心思路，不需要读代码，画流程图或口头讲）
5. **遇到什么问题，怎么解决的**（选 1 个印象深的）

### 每组推荐亮点

| 模块 | 推荐汇报亮点 |
|------|-------------|
| VR 键盘 | 事件驱动架构 + 手柄适配 + 槽位实时刷新 |
| 排行榜 | JSON 本地持久化 + 双面板架构 + 字段可配置 |
| 棒球物理 | 微缩抛物线计算 + 挥棒速度映射 + 扇形判定 |
| GUI/教学 | 13 步状态机教程 + 黑屏场景过渡 + XR 运行时自动绑定 |
