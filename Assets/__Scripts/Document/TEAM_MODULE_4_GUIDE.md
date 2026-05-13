# 模块 4：GUI & 教学引导 — 分工汇报材料

## 我负责什么

**一句话**：玩家进入游戏前的新手教程全流程，包括分步引导、场景切换黑屏动画、XR 组件自动设置。

---

## 用到了哪些脚本

都在 `Assets/__Scripts/` 下，共 11 个文件：

| 脚本 | 在哪 | 干嘛的 |
|------|------|--------|
| `IntroSequenceController.cs` | `Tutorial/` | 教程主控：13 步按顺序走，每步等玩家操作再推进 |
| `SceneTransitionFader.cs` | `Tutorial/` | 场景切换黑屏动画：全屏变黑→加载场景→变亮 |
| `CanvasGroupFader.cs` | `Tutorial/` | UI 淡入淡出工具：让面板平滑出现/消失 |
| `GuideTeleportArrivalTrigger.cs` | `Tutorial/` | 传送检测：玩家移到指定位置后自动触发下一步 |
| `BillboardFaceCamera.cs` | `Tutorial/` | 广告牌：UI 文字始终朝向玩家眼睛 |
| `IndoorSceneXRRuntimeBinder.cs` | `Tutorial/` | XR 自动绑定：进场景后自动找 XR 组件连起来 |
| `PersistentXRCore.cs` | `Tutorial/` | XR 持久化：换场景时 XR Rig 不销毁，带到下一个场景 |
| `XRControllerDeferredActivator.cs` | `Tutorial/` | 手柄延迟激活：等场景加载完再启用手柄 |
| `TrackingOriginFixer.cs` | `Tutorial/` | 追踪原点修正 |
| `PanelController.cs` | `UI/` | 面板通用控制：显示/隐藏 |
| `DropdownController.cs` | `UI/` | 下拉菜单控制 |

> 双视角切换、游戏状态机、手柄输入、角色选择是组长负责的，不在我这个模块。

---

## 我做了什么

### 1. 确认13 步的新手教程的设计

玩家第一次进游戏，按下面的顺序一步步学操作：

```
① 欢迎文字（"你好，欢迎进入动物棒球"）
② 展示面板（介绍游戏基本玩法）
③ 提示文字（"试试转动身体"）
④ 左转教学（提示玩家向左转，有面板+箭头指引）
⑤ 右转教学（向右转）
⑥ 提示文字（"很好，你已经学会了转向"）
⑦ 传送教学——第一个传送点
⑧ 传送教学——第二个传送点
⑨ 传送到角色选择桌
⑩ 到达桌子提示
⑪ 确认面板（"你已经来到角色桌前"）
⑫ 最终提示（"做得很好"）
⑬ 教程完成，进入游戏
```

每一步：
- 有对应的文字或面板显示
- 需要玩家做完某个操作才能进入下一步（点按钮确认 / 传送到指定位置等）
- 支持**跳过**（按 X 键，直接进入游戏场景，不用走完教程）
- 支持**重播**（按 Y 键，教程从头开始）

### 2. 场景切换黑屏动画

教程结束后进入主游戏场景：

```
教程场景 GUIDE Scene
  ↓
全屏慢慢变黑（淡出，约 0.5 秒）
  ↓
加载主场景 Indoor Scene
  ↓
把玩家放到房间的出生位置
  ↓
全屏慢慢变亮（淡入）
  ↓
开始玩
```

这个黑屏不是简单的黑方块，是挂在 VR 相机上的一个翻转球体，配合自定义 Shader 做全屏遮罩。过渡期间会暂停某些 XR 组件防止报错。

### 3. XR 组件的自动设置

VR 项目有很多需要连线的组件（XR Origin、XR Camera、左右手柄等），手动拖容易出错。写了几个脚本让它们**自动干活**：

- `PersistentXRCore`：场景 A 切换到场景 B 时，XR Rig 不会跟着被销毁，而是带到新场景继续用（避免重复加载）
- `IndoorSceneXRRuntimeBinder`：进新场景后自动 `FindObjectOfType` 找到所有 XR 相关物体，把引用连好
- `XRControllerDeferredActivator`：手柄不要一进场景就激活，等场景完全加载好再激活（避免初始化时序错误）
- `TrackingOriginFixer`：修正追踪原点的偏差

### 4. UI 辅助工具

- `CanvasGroupFader`：一个通用的淡入淡出组件，挂上就能让面板平滑出现/消失。支持同时控制 CanvasGroup alpha 和子物体 SpriteRenderer 透明度（VR 里有些 UI 同时用了这两种）
- `BillboardFaceCamera`：让 World Space UI 始终面朝玩家视线，不会斜着看
- `PanelController`：简单的显示/隐藏面板，挂载到按钮点击事件就能控制任意面板
- `DropdownController`：下拉菜单逻辑

---

## 怎么实现的（核心思路）

**教程状态机**：
- 把 13 个步骤定义成枚举：`Idle → IntroSubtitle → FirstBoard → ... → Completed`
- 每帧 `Update()` 检查当前阶段的条件有没有满足
- 满足了就切到下一阶段
- 每个阶段的"展示 UI""等待确认""播放动画"逻辑封装在对应的代码块里

**场景过渡**：
- 运行时生成一个翻转球体 Mesh 当遮罩
- 把遮罩挂在相机正前方（以相机为父物体）
- 用 `Custom/ScreenFade` Shader 控制遮罩的透明度
- Alpha 从 0 到 1 = 淡出（黑屏），1 到 0 = 淡入
- 全黑的时候执行场景加载

**XR 自动绑定**：
- `PersistentXRCore` 用了 `DontDestroyOnLoad` 保持跨场景存活
- `IndoorSceneXRRuntimeBinder` 在 `Start()` 中用 `FindObjectOfType` 扫描整个场景，把 XR Origin / Camera / Controller 等组件引用自动赋值
- 手柄延迟激活用了协程，等一两帧再调用激活

**传送检测**：
- 在场景里放看不见的触发区域（Collider + IsTrigger）
- 玩家用 VR 传送到这个区域时，`OnTriggerEnter` 触发
- `GuideTeleportArrivalTrigger` 检测到到达，通知教程主控推进

---

## 遇到的坑

**最大的坑**：场景切换时报 `IndexOutOfRangeException: renderPassIndex` 错误。排查发现是因为 Unity 的 Scene 窗口和 Game 窗口同时开着 VR 预览时会冲突。解决方法是切换场景前先停用 TrackedDeviceGraphicRaycaster，切完再启用。

另一个问题：教程第 5 步"右转教学"和第 6 步"确认完成"之间，玩家如果转太快，面板还没显示完就走了。后来加了延迟和条件检查，确保每步 UI 展示完毕才接受下一步操作。

---

## 涉及场景

- `GUIDE Scene.unity`（教程引导场景，全部流程在这里）
