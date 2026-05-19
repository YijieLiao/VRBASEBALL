using UnityEngine;

public class GameAudio : MonoBehaviour
{
    [Header("BGM")]
    [SerializeField] private AudioClip roomBGM;
    [SerializeField] private AudioClip battingBGM;

    [Header("UI SFX (2D)")]
    [SerializeField] private AudioClip buttonClickSFX;
    [SerializeField] private AudioClip buttonHoverSFX;
    [SerializeField] private AudioClip panelOpenSFX;
    [SerializeField] private AudioClip panelCloseSFX;

    [Header("Game SFX (3D)")]
    [SerializeField] private AudioClip homeRunSFX;
    [SerializeField] private AudioClip fairHitSFX;
    [SerializeField] private AudioClip foulSFX;
    [SerializeField] private AudioClip caughtSFX;
    [SerializeField] private AudioClip missSFX;

    [Header("Round SFX (2D)")]
    [SerializeField] private AudioClip countdownTickSFX;
    [SerializeField] private AudioClip countdownGoSFX;
    [SerializeField] private AudioClip roundStartSFX;
    [SerializeField] private AudioClip roundEndSFX;

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

        // 初始 BGM
        if (gameManager != null && gameManager.CurrentState == GameState.Room)
            AudioManager.Instance.PlayBGM(roomBGM);
    }

    void OnDestroy()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (gameManager != null)
            gameManager.OnStateChanged += OnGameStateChanged;

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
                AudioManager.Instance.PlayBGM(roomBGM);
                if (panelOpenSFX != null)
                    AudioManager.Instance.PlaySFX(panelOpenSFX);
                break;

            case GameState.Batting:
                AudioManager.Instance.PlayBGM(battingBGM);
                break;

            case GameState.Pause:
                AudioManager.Instance.PlaySFX(panelOpenSFX);
                break;

            case GameState.Result:
                AudioManager.Instance.PlaySFX(roundEndSFX);
                // 切回房间 BGM
                AudioManager.Instance.PlayBGM(roomBGM);
                break;
        }
    }

    // ==================== 游戏事件 ====================

    private void OnBallResult(BallResultInfo info)
    {
        AudioClip clip = info.result switch
        {
            HitResult.HomeRun    => homeRunSFX,
            HitResult.FairLanding => fairHitSFX,
            HitResult.Foul       => foulSFX,
            HitResult.Caught     => caughtSFX,
            HitResult.None       => missSFX,
            _                    => null
        };

        if (clip != null)
            AudioManager.Instance.PlaySFXAt(clip, info.worldPosition);
    }

    private void OnCountdownTick(int tick)
    {
        if (tick > 0)
            AudioManager.Instance.PlaySFX(countdownTickSFX);
        else
            AudioManager.Instance.PlaySFX(countdownGoSFX);
    }

    private void OnRoundEnded(int totalScore)
    {
        AudioManager.Instance.PlaySFX(roundEndSFX);
    }

    private void OnPhaseChanged(RoundPhase phase)
    {
        if (phase == RoundPhase.Countdown && roundStartSFX != null)
            AudioManager.Instance.PlaySFX(roundStartSFX);
    }

    // ==================== 公开方法（给 Panel 按钮调用） ====================

    public void PlayButtonClick()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(buttonClickSFX);
    }

    public void PlayButtonHover()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(buttonHoverSFX);
    }

    public void PlayPanelOpen()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(panelOpenSFX);
    }

    public void PlayPanelClose()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(panelCloseSFX);
    }
}
