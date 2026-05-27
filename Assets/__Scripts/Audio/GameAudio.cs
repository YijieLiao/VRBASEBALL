using UnityEngine;

public class GameAudio : MonoBehaviour
{
    [Header("BGM")]
    [SerializeField] private AudioClip roomBGM;
    [SerializeField] private AudioClip battingBGM;
    [Range(0, 1)] public float bgmVolume = 0.8f;

    [Header("环境音")]
    [SerializeField] private AudioClip roomAmbient;
    [Range(0, 1)] public float ambientVolume = 0.5f;

    [Header("UI SFX (2D)")]
    [SerializeField] private AudioClip buttonClickSFX;
    [SerializeField] private AudioClip buttonHoverSFX;
    [SerializeField] private AudioClip panelOpenSFX;
    [SerializeField] private AudioClip panelCloseSFX;
    [Range(0, 3)] public float uiSFXVolume = 1f;

    [Header("Round SFX (2D)")]
    [SerializeField] private AudioClip countdownTickSFX;
    [SerializeField] private AudioClip countdownGoSFX;
    [SerializeField] private AudioClip roundStartSFX;
    [SerializeField] private AudioClip roundEndSFX;
    [Range(0, 3)] public float roundSFXVolume = 1f;

    private GameManager gameManager;
    private ClassicModeRoundManager roundManager;

    void Start()
    {
        gameManager = GameManager.Instance;
        roundManager = FindObjectOfType<ClassicModeRoundManager>();

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("GameAudio: AudioManager.Instance is null. Ensure AudioManager is in the scene before GameAudio.");
            return;
        }

        Subscribe();

        // 初始 BGM + 环境音
        if (gameManager != null && gameManager.CurrentState == GameState.Room)
        {
            AudioManager.Instance.PlayBGM(roomBGM, bgmVolume);
            AudioManager.Instance.PlayAmbient(roomAmbient, ambientVolume);
        }
    }

    void OnDestroy()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (gameManager != null)
            gameManager.OnStateChanged += OnGameStateChanged;

        // Classic 模式回合事件
        if (roundManager != null)
        {
            roundManager.OnBallResult += OnBallResult;
            roundManager.OnCountdownTick += OnCountdownTick;
            roundManager.OnRoundEnded += OnRoundEnded;
            roundManager.OnPhaseChanged += OnPhaseChanged;
        }
    }

    private void Unsubscribe()
    {
        if (gameManager != null)
            gameManager.OnStateChanged -= OnGameStateChanged;

        if (roundManager != null)
        {
            roundManager.OnBallResult -= OnBallResult;
            roundManager.OnCountdownTick -= OnCountdownTick;
            roundManager.OnRoundEnded -= OnRoundEnded;
            roundManager.OnPhaseChanged -= OnPhaseChanged;
        }
    }

    // ==================== BGM ====================

    private void OnGameStateChanged(GameState oldState, GameState newState)
    {
        switch (newState)
        {
            case GameState.Room:
                AudioManager.Instance.PlayAmbient(roomAmbient, ambientVolume);
                AudioManager.Instance.PlayBGM(roomBGM, bgmVolume);
                if (panelOpenSFX != null)
                    AudioManager.Instance.PlaySFX(panelOpenSFX, uiSFXVolume);
                break;

            case GameState.Batting:
                AudioManager.Instance.StopAmbient();
                AudioManager.Instance.PlayBGM(battingBGM, bgmVolume);
                break;

            case GameState.Pause:
                AudioManager.Instance.PlaySFX(panelOpenSFX, uiSFXVolume);
                break;

            case GameState.Result:
                AudioManager.Instance.PlaySFX(roundEndSFX, roundSFXVolume);
                // BGM 保持当前，等回到 Room 时自然切换
                break;
        }
    }

    // ==================== 游戏事件 ====================

    // Classic 模式每球结果（击球音效由 Pitcher.ApplyBatHit 在击中瞬间播放，不在此处理）
    private void OnBallResult(BallResultInfo info) { }

    private void OnCountdownTick(int tick)
    {
        if (tick > 0)
            AudioManager.Instance.PlaySFX(countdownTickSFX, roundSFXVolume);
        else
            AudioManager.Instance.PlaySFX(countdownGoSFX, roundSFXVolume);
    }

    private void OnRoundEnded(int totalScore)
    {
        AudioManager.Instance.PlaySFX(roundEndSFX, roundSFXVolume);
    }

    private void OnPhaseChanged(RoundPhase phase)
    {
        if (phase == RoundPhase.Countdown && roundStartSFX != null)
            AudioManager.Instance.PlaySFX(roundStartSFX, roundSFXVolume);
    }

    // ==================== 公开方法（给 Panel 按钮调用） ====================

    public void PlayButtonClick()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(buttonClickSFX, uiSFXVolume);
    }

    public void PlayButtonHover()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(buttonHoverSFX, uiSFXVolume);
    }

    public void PlayPanelOpen()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(panelOpenSFX, uiSFXVolume);
    }

    public void PlayPanelClose()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(panelCloseSFX, uiSFXVolume);
    }
}
