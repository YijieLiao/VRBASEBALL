# 模块 1：VR 虚拟键盘 — 分工汇报材料

## 我负责什么

**一句话**：让玩家在 VR 里用虚拟键盘输入名字，6 个字符以内。

---

## 用到了哪些脚本

都在 `Assets` 下，共 8 个文件：

| 脚本 | 在哪 | 干嘛的 |
|------|------|--------|
| `KeyboardManager.cs` | `_VRKeyboard/Scripts/` | 键盘的大脑：管输入文字、大小写、最多几个字 |
| `NameInputPanel.cs` | `__Scripts/UI/` | 6 个空格的输入面板，键盘嵌在里面 |
| `Key.cs` | `_VRKeyboard/Scripts/Keys/` | 单个按键的基类，点一下就发一个字符 |
| `Alphabet.cs` | `_VRKeyboard/Scripts/Keys/` | 字母键，支持大写小写切换 |
| `Number.cs` | `_VRKeyboard/Scripts/Keys/` | 数字键 |
| `Symbol.cs` | `_VRKeyboard/Scripts/Keys/` | 符号键（游戏里没用到） |
| `Shift.cs` | `_VRKeyboard/Scripts/Keys/` | 换挡键，一个键上显示两个字符时切换 |
| `GazeRaycaster.cs` | `_VRKeyboard/Scripts/` | 注视点击功能（游戏里没用到，备用） |

---

## 我做了什么

### 1. 引入了一个现成的 VR 键盘资产

网上找的 `_VRKeyboard` 包，自带键盘布局和按键逻辑。每个按键就是一个按钮，点了就发一个字母出去。

### 2. 把它从"盯着看"改成"手柄点"

原版键盘是用**眼睛注视**的——盯着按键 0.5 秒自动触发。在 Pico 上体验很差。改成了**手柄射出一条激光，对准按键扣扳机**来点击。原理是 Canvas 上挂 `TrackedDeviceGraphicRaycaster`，手柄的 XR Ray Interactor 就能和按键交互。

### 3. 精简了键盘

原版有 26 个字母 + 10 个数字 + 符号 + Shift + 空格 + 清除，一大堆。游戏里只需要输入 6 位英文数字名字，所以只保留了 A-Z、0-9、退格、确认。删掉了符号、Shift、空格、清除。

### 4. 加了一个"确认提交"功能

原版键盘只能打字，没有"我打完了"的按钮。在 `KeyboardManager.cs` 上加了一个 `Submit()` 方法和一个事件，点确认键就把当前输入文字发出去。

### 5. 做了 6 格槽位显示

像街机高分榜那样，6 个空槽位 `[ _ ] [ _ ] [ _ ] [ _ ] [ _ ] [ _ ]`，输入一个字亮一格。用 `Update()` 每帧读键盘文字变化，变了就刷新槽位。

### 6. 每次打开自动清空

点"再来一局"后重新进入名字输入，上次的字会自动清掉，不会残留。

---

## 怎么实现的（核心思路）

```
玩家用激光对准键盘上的字母 "A" → 扣扳机
  → Button.onClick 触发
  → Key 脚本把字符 "A" 发给 KeyboardManager
  → KeyboardManager 把 "A" 拼到当前输入文字后面
  → NameInputPanel 每帧检测到文字变了
  → 把第 1 个槽位从 "_" 改成 "A"
```

- 键盘和面板是两个独立的东西，靠事件和轮询通信
- 键盘负责"打字"，面板负责"显示打出来的字"
- 之前用事件通知，发现有时序问题（谁先初始化不确定），改成了 Update 轮询，稳定可靠

---

## 遇到的坑

**最大的坑**：确认按钮被绑了两次——代码里 `Start()` 用 `AddListener` 绑了一次，Inspector 里 OnClick 又绑了一次。结果点一下确认，提交两次，排行榜里出现两条一模一样的记录。排查了半天发现是这问题，删掉代码里的绑定就好了。

---

## 涉及场景

- `Test_Settlement.unity`：非 VR 测试场景，World Space Canvas，鼠标点键盘
- `Indoor Scene (MAIN).unity`：VR 主场景，房间区域的名字输入面板
