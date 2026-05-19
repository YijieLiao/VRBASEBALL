# 音频系统使用方式

## 架构

```
GameAudio.cs  →  监听游戏事件，决定"什么时候放什么声音"
      ↓
AudioManager.cs  →  单例，唯一拥有 AudioSource，决定"怎么播放"
      ↓
_Mixer.mixer  →  混音分组：Master / Music / SFX（已有）
```

## 场景设置（一次性）

1. 打开 `Indoor Scene(MAIN)`
2. 创建空 GameObject，命名 `Audio`
3. 挂上 `AudioManager` 组件
4. 挂上 `GameAudio` 组件
5. 在 `AudioManager` 的 Inspector 中，把 `_Dark UI/Sounds/_Mixer.mixer` 拖入 **Mixer** 字段
6. 在 `GameAudio` 的 Inspector 中，把对应音频文件拖入各个 Clip 字段

> 如果不拖 Mixer，AudioManager 会尝试从 Resources 加载 `_Mixer`，找不到也能工作（只是无法分组控制音量）。

## Inspector 字段说明

### AudioManager

| 字段 | 说明 |
|------|------|
| Mixer | 引用 `_Dark UI/Sounds/_Mixer.mixer` |

无需手动创建 AudioSource，代码会自动创建。

### GameAudio — BGM

| 字段 | 说明 |
|------|------|
| Room BGM | 房间模式的背景音乐（循环） |
| Batting BGM | 击球模式的背景音乐（循环） |

### GameAudio — UI SFX（2D）

| 字段 | 说明 |
|------|------|
| Button Click SFX | 按钮点击音效 |
| Button Hover SFX | 按钮悬停音效 |
| Panel Open SFX | 面板打开音效 |
| Panel Close SFX | 面板关闭音效 |

### GameAudio — Game SFX（3D，空间化）

| 字段 | 说明 |
|------|------|
| Home Run SFX | 全垒打音效 |
| Fair Hit SFX | 有效击球音效 |
| Foul SFX | 界外音效 |
| Caught SFX | 被接杀音效 |
| Miss SFX | 未击中音效 |

### GameAudio — Round SFX（2D）

| 字段 | 说明 |
|------|------|
| Countdown Tick SFX | 倒计时 3-2-1 的每拍音效 |
| Countdown Go SFX | 倒计时结束（Go!）音效 |
| Round Start SFX | 回合开始音效 |
| Round End SFX | 回合结束音效 |

## 在代码中调用

```csharp
// 播放 2D 音效（UI、全局通知）
AudioManager.Instance.PlaySFX(someClip);
AudioManager.Instance.PlaySFX(someClip, 0.5f);  // 50% 音量

// 播放 3D 音效（场景事件，带位置）
AudioManager.Instance.PlaySFXAt(someClip, worldPosition);
AudioManager.Instance.PlaySFXAt(someClip, worldPosition, 0.8f);

// 切换/停止 BGM
AudioManager.Instance.PlayBGM(newClip);          // 循环播放
AudioManager.Instance.PlayBGM(newClip, false);   // 单次播放
AudioManager.Instance.StopBGM();

// 音量（0-1 normalised）
AudioManager.Instance.SetMasterVolume(0.8f);
AudioManager.Instance.SetMusicVolume(0.6f);
AudioManager.Instance.SetSFXVolume(1f);
```

## 按钮绑定音频

**方法 1**：在按钮的 `OnClick` 事件中绑定 `GameAudio.PlayButtonClick()`

Unity Editor 操作：
1. 选中按钮 GameObject
2. 在 Inspector 中找到 Button 组件的 `OnClick` 事件列表
3. 点 `+` 添加条目
4. 把场景中挂有 `GameAudio` 的 GameObject 拖入对象字段
5. 在函数下拉中选择 `GameAudio` → `PlayButtonClick()`

**方法 2**：保留 Dark UI 的 `UIElementSound`，在按钮预制体上挂 `UIElementSound` 组件并配置 AudioClip

两种方式互不冲突，可同时使用。

## 自动播放的行为

| 游戏事件 | 自动播放 |
|----------|----------|
| 进入 Room 状态 | Room BGM + Panel Open SFX |
| 进入 Batting 状态 | Batting BGM |
| 进入 Pause 状态 | Panel Open SFX |
| 进入 Result 状态 | Round End SFX → 切回 Room BGM |
| 击球结果（全垒打/有效落地/界外/被接杀/未击中） | 对应 3D SFX（在事件位置播放） |
| 倒计时 3-2-1 | Countdown Tick SFX |
| 倒计时 Go! | Countdown Go SFX |
| 回合开始 | Round Start SFX |
| 回合结束 | Round End SFX |

## 音频文件位置

现有音频文件：`Assets/_Dark UI/Sounds/`

| 文件 | 用途建议 |
|------|----------|
| Background Ambient.ogg | Room BGM |
| Click.ogg | Button Click SFX |
| Hover.ogg | Button Hover SFX |
| Melting Transition.ogg | Panel Open/Close SFX |
| Notification.ogg | Round Start/End SFX、Countdown Go SFX |

游戏 SFX（Home Run、Hit、Foul、Miss、Countdown Tick）需要额外准备音频文件。
