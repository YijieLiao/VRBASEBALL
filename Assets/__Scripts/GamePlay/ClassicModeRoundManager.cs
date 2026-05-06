using System;
using System.Collections;
using UnityEngine;

public enum RoundPhase
{
    Idle,
    Countdown,
    Pitching,
    WaitingResult,
    Scoring,
    RoundEnd
}

public struct BallResultInfo
{
    public int ballNumber;
    public HitResult result;
    public float distance;
    public int ballScore;
    public int totalScore;
}

public class ClassicModeRoundManager : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Pitcher pitcher;
    [SerializeField] private HitJudge hitJudge;

    [Header("回合设置")]
    [SerializeField] private int ballsPerRound = 10;

    [Header("计分")]
    [SerializeField] private float homeRunMultiplier = 200f;
    [SerializeField] private float fairHitMultiplier = 100f;

    [Header("连击增益")]
    [Tooltip("连击数门槛和对应倍率。按连击数从小到大排列。例：3连×1.2, 4连×1.5, 5连×1.6")]
    [SerializeField] private int[] comboThresholds = { 3, 4, 5 };
    [SerializeField] private float[] comboMultipliers = { 1.2f, 1.5f, 1.6f };

    [Header("时序")]
    [SerializeField] private float countdownInterval = 0.7f;
    [SerializeField] private float scoreDisplayDuration = 2f;
    [SerializeField] private float missTimeout = 6f;
    [SerializeField] private float hitLandTimeout = 20f;

    // 事件 —— 由 DerbyScoreboard 等 UI 订阅
    public event Action<BallResultInfo> OnBallResult;
    public event Action<int> OnRoundEnded;
    public event Action OnPlayerQuit;
    public event Action<int> OnCountdownTick;
    public event Action<int, int> OnBallNumberUpdate;
    public event Action<RoundPhase> OnPhaseChanged;
    public event Action<int, float> OnComboChanged; // combo数, 当前倍率

    [SerializeField] private int currentBall;
    [SerializeField] private int totalScore;
    private int currentCombo;
    private int maxCombo;
    private float activeMultiplier = 1f;
    private Coroutine roundCoroutine;
    private bool resultReceived;
    [SerializeField] private RoundPhase phase = RoundPhase.Idle;

    public RoundPhase CurrentPhase => phase;
    public int CurrentBall => currentBall;
    public int TotalScore => totalScore;
    public int CurrentCombo => currentCombo;
    public int MaxCombo => maxCombo;
    public float ActiveMultiplier => activeMultiplier;

    void Awake()
    {
        if (pitcher == null) pitcher = FindObjectOfType<Pitcher>();
        if (hitJudge == null) hitJudge = FindObjectOfType<HitJudge>();
    }

    void OnEnable()
    {
        if (hitJudge != null)
            hitJudge.onHitResult.AddListener(OnHitResult);
    }

    void OnDisable()
    {
        if (hitJudge != null)
            hitJudge.onHitResult.RemoveListener(OnHitResult);
    }

    void Update()
    {
#if UNITY_EDITOR
        HandleEditorInput();
#endif
    }

    // ==================== 公开接口 ====================

    public void StartRound()
    {
        Debug.Log($"[ClassicMode] StartRound called. Phase={phase}, Pitcher={(pitcher != null ? "OK" : "NULL")}, HitJudge={(hitJudge != null ? "OK" : "NULL")}");
        if (phase != RoundPhase.Idle && phase != RoundPhase.RoundEnd)
        {
            Debug.LogWarning($"[ClassicMode] Cannot start — phase is {phase}, expected Idle or RoundEnd.");
            return;
        }

        ResetRound();
        roundCoroutine = StartCoroutine(RunRound());
        Debug.Log("[ClassicMode] Round started.");
    }

    public void AbortRound()
    {
        StopRoundCoroutine();
        if (pitcher != null) pitcher.ResetBall();
        SetPhase(RoundPhase.Idle);
        OnPlayerQuit?.Invoke();
    }

    public void Pause()  { Time.timeScale = 0f; }
    public void Resume() { Time.timeScale = 1f; }

    // ==================== 回合循环 ====================

    private void ResetRound()
    {
        currentBall = 0;
        totalScore = 0;
        currentCombo = 0;
        maxCombo = 0;
        activeMultiplier = 1f;
        OnComboChanged?.Invoke(0, 1f);
    }

    private IEnumerator RunRound()
    {
        for (currentBall = 1; currentBall <= ballsPerRound; currentBall++)
        {
            yield return StartCoroutine(PitchSequence());
        }

        SetPhase(RoundPhase.RoundEnd);
        OnRoundEnded?.Invoke(totalScore);
        roundCoroutine = null;
    }

    private IEnumerator PitchSequence()
    {
        // 1. 第 N 球提示
        SetPhase(RoundPhase.Countdown);
        OnBallNumberUpdate?.Invoke(currentBall, ballsPerRound);

        // 2. 倒数 3-2-1
        for (int i = 3; i >= 1; i--)
        {
            OnCountdownTick?.Invoke(i);
            yield return new WaitForSeconds(countdownInterval);
        }
        OnCountdownTick?.Invoke(0);

        // 3. 复位并等待一帧，然后发球
        SetPhase(RoundPhase.Pitching);
        pitcher.ResetBall();
        yield return null;
        pitcher.PitchBall();

        // 4. 等待结果
        SetPhase(RoundPhase.WaitingResult);
        resultReceived = false;
        float timer = 0f;
        float timeout = missTimeout;
        float minFlight = pitcher != null ? pitcher.flightTime + 0.8f : 2f;
        while (!resultReceived && timer < timeout)
        {
            timer += Time.deltaTime;
            // 球被击中 → 切换长超时等落地
            if (hitJudge != null && hitJudge.HasActiveHit)
                timeout = hitLandTimeout;
            // 球已落地且未被击中 → 立刻结束等待（不用傻等 missTimeout）
            if (timer > minFlight && pitcher != null && !pitcher.IsInFlight)
                break;
            yield return null;
        }

        if (!resultReceived)
            HandleMiss();

        // 5. 展示得分
        if (phase == RoundPhase.Scoring)
            yield return new WaitForSeconds(scoreDisplayDuration);
    }

    // ==================== 击球结果 ====================

    private void OnHitResult(HitResult result, Vector3 landingPosition)
    {
        if (phase != RoundPhase.WaitingResult) return;
        resultReceived = true;

        UpdateCombo(result);

        float distance = hitJudge != null ? hitJudge.LastHitDistance : 0f;
        int ballScore = CalculateScore(result, distance);
        totalScore += ballScore;

        SetPhase(RoundPhase.Scoring);

        OnBallResult?.Invoke(new BallResultInfo
        {
            ballNumber = currentBall,
            result = result,
            distance = distance,
            ballScore = ballScore,
            totalScore = totalScore
        });
    }

    private void HandleMiss()
    {
        UpdateCombo(HitResult.None);

        SetPhase(RoundPhase.Scoring);

        OnBallResult?.Invoke(new BallResultInfo
        {
            ballNumber = currentBall,
            result = HitResult.None,
            distance = 0f,
            ballScore = 0,
            totalScore = totalScore
        });
    }

    // ==================== 计分 ====================

    private int CalculateScore(HitResult result, float distance)
    {
        int baseScore;
        switch (result)
        {
            case HitResult.HomeRun:
                baseScore = Mathf.RoundToInt(distance * homeRunMultiplier);
                break;
            case HitResult.FairLanding:
                baseScore = Mathf.RoundToInt(distance * fairHitMultiplier);
                break;
            default:
                return 0;
        }
        return Mathf.RoundToInt(baseScore * activeMultiplier);
    }

    private void UpdateCombo(HitResult result)
    {
        bool success = result == HitResult.HomeRun || result == HitResult.FairLanding;
        if (success)
        {
            currentCombo++;
            if (currentCombo > maxCombo) maxCombo = currentCombo;
            // 从门槛数组中找到当前连击数对应的倍率
            float bestMult = 1f;
            for (int i = 0; i < comboThresholds.Length; i++)
            {
                if (currentCombo >= comboThresholds[i])
                    bestMult = comboMultipliers[i];
            }
            activeMultiplier = bestMult;
        }
        else
        {
            currentCombo = 0;
            activeMultiplier = 1f;
        }
        OnComboChanged?.Invoke(currentCombo, activeMultiplier);
    }

    // ==================== 辅助 ====================

    private void SetPhase(RoundPhase newPhase)
    {
        phase = newPhase;
        OnPhaseChanged?.Invoke(phase);
    }

    private void StopRoundCoroutine()
    {
        if (roundCoroutine != null)
        {
            StopCoroutine(roundCoroutine);
            roundCoroutine = null;
        }
    }

    // ==================== Editor 测试 ====================

#if UNITY_EDITOR
    private void HandleEditorInput()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (phase == RoundPhase.Idle || phase == RoundPhase.RoundEnd)
                StartRound();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (phase != RoundPhase.Idle && phase != RoundPhase.RoundEnd)
                AbortRound();
            return;
        }

        if (phase != RoundPhase.WaitingResult) return;

        HitResult? simulated = null;
        float fakeDistance = 3f;

        if (Input.GetKeyDown(KeyCode.Keypad1))
            { simulated = HitResult.HomeRun; fakeDistance = 8f; }
        else if (Input.GetKeyDown(KeyCode.Keypad2))
            { simulated = HitResult.FairLanding; fakeDistance = 3.5f; }
        else if (Input.GetKeyDown(KeyCode.Keypad3))
            { simulated = HitResult.Foul; fakeDistance = 2f; }
        else if (Input.GetKeyDown(KeyCode.Keypad0))
            { resultReceived = true; HandleMiss(); return; }

        if (simulated.HasValue && hitJudge != null)
        {
            resultReceived = true;
            hitJudge.OnBallHit();
            hitJudge.OnBallGrounded(transform.position + transform.forward * fakeDistance);
        }
    }
#endif
}
