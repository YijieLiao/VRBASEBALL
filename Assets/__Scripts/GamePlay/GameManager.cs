using System;
using UnityEngine;

public enum GameState
{
    Room,
    Batting,
    Pause,
    Result
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameMode { None, FreePractice, Classic }

    [Header("引用")]
    [SerializeField] private ViewTransitionManager viewTransition;
    [SerializeField] private ClassicModeRoundManager roundManager;
    [SerializeField] private LeaderboardManager leaderboardManager;

    [Header("模式面板")]
    [Tooltip("自由练习的World Canvas（击球区内）")]
    [SerializeField] private GameObject freePracticeCanvas;
    [Tooltip("经典模式的World Canvas（击球区内）")]
    [SerializeField] private GameObject classicCanvas;

    public event Action<GameState, GameState> OnStateChanged;
    public event Action<int> OnShowResult;

    public GameState CurrentState => currentState;
    public GameMode CurrentMode => currentMode;

    private GameState currentState = GameState.Room;
    private GameState previousState;
    private GameMode currentMode = GameMode.None;
    private int lastRoundScore;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        ResolveReferences();
        EnterState(currentState);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        HandlePauseInput();
        HandleAbortInput();
    }

    // ==================== 状态机 ====================

    public void TransitionTo(GameState target)
    {
        if (target == currentState) return;

        previousState = currentState;
        ExitState(currentState);
        currentState = target;
        EnterState(currentState);

        OnStateChanged?.Invoke(previousState, currentState);
        Debug.Log($"[GameManager] {previousState} → {currentState}");
    }

    private void EnterState(GameState state)
    {
        switch (state)
        {
            case GameState.Room:
                if (previousState == GameState.Pause || previousState == GameState.Batting)
                {
                    if (roundManager != null) roundManager.AbortRound();
                }
                currentMode = GameMode.None;
                // 不关 Canvas —— 让 ViewTransitionManager 的渐隐协程自己管控 alpha
                if (viewTransition != null)
                    viewTransition.TransitionTo(ViewTransitionManager.ViewMode.Room);
                break;

            case GameState.Batting:
                if (previousState == GameState.Pause)
                {
                    // 从暂停恢复：取消时停即可，不重新初始化
                    if (roundManager != null) roundManager.Resume();
                }
                else
                {
                    // 从房间或结算页进入击球区：先激活 Canvas 再切视角，这样 Canvas 能纳入渐显
                    ActivateModeCanvas();
                    if (viewTransition != null)
                        viewTransition.TransitionTo(ViewTransitionManager.ViewMode.Batting);
                }
                break;

            case GameState.Pause:
                if (roundManager != null) roundManager.Pause();
                break;

            case GameState.Result:
                if (viewTransition != null)
                    viewTransition.TransitionTo(ViewTransitionManager.ViewMode.Room);
                OnShowResult?.Invoke(lastRoundScore);
                break;
        }
    }

    private void ExitState(GameState state)
    {
        // 所有清理逻辑交由 EnterState 根据 previousState 判断
        // ExitState 不做破坏性操作，避免在 Batting→Pause 等场景误清理
    }

    // ==================== 事件回调（Inspector 绑定） ====================

    public void OnRoundEnded(int totalScore)
    {
        lastRoundScore = totalScore;
        TransitionTo(GameState.Result);
    }

    public void OnPlayerQuit()
    {
        TransitionTo(GameState.Room);
    }

    // ==================== 输入 ====================

    private void HandlePauseInput()
    {
        if (currentState != GameState.Batting && currentState != GameState.Pause)
            return;

        bool pausePressed = false;

#if UNITY_EDITOR
        pausePressed = Input.GetKeyDown(KeyCode.Escape);
#endif
        if (PicoInputManager.Instance != null)
        {
            if (PicoInputManager.Instance.LeftMenuDown || PicoInputManager.Instance.RightMenuDown)
                pausePressed = true;
        }

        if (!pausePressed) return;

        if (currentState == GameState.Batting)
            TransitionTo(GameState.Pause);
        else if (currentState == GameState.Pause)
            TransitionTo(GameState.Batting);
    }

    private void HandleAbortInput()
    {
        if (currentState != GameState.Batting) return;

        bool abortPressed = false;

#if UNITY_EDITOR
        abortPressed = Input.GetKeyDown(KeyCode.Q);
#endif
        if (PicoInputManager.Instance != null)
        {
            if (PicoInputManager.Instance.BButtonDown || PicoInputManager.Instance.YButtonDown)
                abortPressed = true;
        }

        if (abortPressed)
            TransitionTo(GameState.Room);
    }

    // ==================== 辅助 ====================

    private void ResolveReferences()
    {
        if (viewTransition == null) viewTransition = FindObjectOfType<ViewTransitionManager>();
        if (roundManager == null) roundManager = FindObjectOfType<ClassicModeRoundManager>();
        if (leaderboardManager == null) leaderboardManager = FindObjectOfType<LeaderboardManager>();
    }

    // ==================== 公开快捷方法 ====================

    public void StartFreePractice()
    {
        if (currentState != GameState.Room) return;
        currentMode = GameMode.FreePractice;
        TransitionTo(GameState.Batting);
    }

    public void StartClassicMode()
    {
        if (currentState != GameState.Room) return;
        currentMode = GameMode.Classic;
        TransitionTo(GameState.Batting);
    }

    public void StartRound()
    {
        Debug.Log($"[GameManager] StartRound called. State={currentState}, Mode={currentMode}, RoundManager={(roundManager != null ? roundManager.CurrentPhase.ToString() : "null")}");
        if (currentState != GameState.Batting)
            Debug.LogWarning("[GameManager] StartRound failed — not in Batting state.");
        else if (roundManager == null)
            Debug.LogWarning("[GameManager] StartRound failed — roundManager is null.");
        else
            roundManager.StartRound();
    }

    public void ReturnToRoom()
    {
        TransitionTo(GameState.Room);
    }

    public void PlayAgain()
    {
        // 保持当前模式，重新进入击球区
        if (currentState == GameState.Result)
            TransitionTo(GameState.Batting);
    }

    // ==================== Canvas 管理 ====================

    private void ActivateModeCanvas()
    {
        // 先关再开，强制触发 OnEnable → ClearDisplay
        if (freePracticeCanvas != null)
        {
            freePracticeCanvas.SetActive(false);
            freePracticeCanvas.SetActive(currentMode == GameMode.FreePractice);
        }
        if (classicCanvas != null)
        {
            classicCanvas.SetActive(false);
            classicCanvas.SetActive(currentMode == GameMode.Classic);
        }
    }

    private void DeactivateAllCanvases()
    {
        if (freePracticeCanvas != null) freePracticeCanvas.SetActive(false);
        if (classicCanvas != null) classicCanvas.SetActive(false);
    }

#if UNITY_EDITOR
    [ContextMenu("DEBUG: Go Classic Mode")]
    private void DebugGoClassic() { StartClassicMode(); }

    [ContextMenu("DEBUG: Go Free Practice")]
    private void DebugGoFreePractice() { StartFreePractice(); }

    [ContextMenu("DEBUG: Go Room")]
    private void DebugGoRoom() { ReturnToRoom(); }

    [ContextMenu("DEBUG: Simulate Round End")]
    private void DebugSimulateRoundEnd()
    {
        lastRoundScore = 1250;
        TransitionTo(GameState.Result);
    }
#endif
}
