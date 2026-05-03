using UnityEngine;

public class SwingSpeedLogger : MonoBehaviour
{
    private enum SpeedSource
    {
        Auto,
        BatCapsuleFollower,
        Rigidbody,
        TransformDelta
    }

    [Header("速度来源")]
    [SerializeField] private SpeedSource speedSource = SpeedSource.Auto;
    [SerializeField] private BatCapsuleFollower targetFollower;
    [SerializeField] private Rigidbody targetRigidbody;

    [Header("采样")]
    [Tooltip("低于这个速度的样本不会计入平均挥棒速度。单位是 Unity units / second，通常可近似看成 m/s。")]
    [SerializeField] private float minSampleSpeed = 0.2f;

    [Tooltip("速度超过这个值时，认为一次挥棒开始。")]
    [SerializeField] private float swingStartSpeed = 1.2f;

    [Tooltip("挥棒开始后，速度低于这个值并保持一小段时间，认为挥棒结束。")]
    [SerializeField] private float swingEndSpeed = 0.5f;

    [Tooltip("速度低于结束阈值后，等待多久才结束本次挥棒。")]
    [SerializeField] private float swingEndGraceTime = 0.12f;

    [Tooltip("低于这个持续时间的挥棒会被忽略。")]
    [SerializeField] private float minimumSwingDuration = 0.08f;

    [Header("输出")]
    [Tooltip("每隔多少秒在 Console 输出一次汇总。0 表示只在每次挥棒结束时输出。")]
    [SerializeField] private float logInterval = 2f;

    [Tooltip("是否在屏幕左上角显示实时速度。")]
    [SerializeField] private bool showOnGUI = true;

    [Header("实时读数")]
    [SerializeField] private float currentSpeed;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float sampledAverageSpeed;
    [SerializeField] private int swingCount;
    [SerializeField] private float lastSwingPeakSpeed;
    [SerializeField] private float lastSwingAverageSpeed;
    [SerializeField] private float averageSwingPeakSpeed;
    [SerializeField] private float averageSwingAverageSpeed;

    private Vector3 _lastPosition;
    private bool _hasLastPosition;
    private float _sampledSpeedSum;
    private float _sampledDuration;
    private bool _isSwinging;
    private float _currentSwingPeak;
    private float _currentSwingSpeedSum;
    private float _currentSwingDuration;
    private float _belowEndSpeedDuration;
    private float _swingPeakSum;
    private float _swingAverageSum;
    private float _nextLogTime;

    private void Awake()
    {
        if (targetFollower == null)
            targetFollower = GetComponentInParent<BatCapsuleFollower>();

        if (targetRigidbody == null)
            targetRigidbody = GetComponentInParent<Rigidbody>();

        _lastPosition = transform.position;
        _hasLastPosition = true;
        _nextLogTime = Time.time + logInterval;
    }

    private void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;
        Vector3 velocity = GetVelocity(deltaTime);
        currentSpeed = velocity.magnitude;
        maxSpeed = Mathf.Max(maxSpeed, currentSpeed);

        if (currentSpeed >= minSampleSpeed)
        {
            _sampledSpeedSum += currentSpeed * deltaTime;
            _sampledDuration += deltaTime;
            sampledAverageSpeed = _sampledSpeedSum / _sampledDuration;
        }

        UpdateSwing(currentSpeed, deltaTime);

        if (logInterval > 0f && Time.time >= _nextLogTime)
        {
            LogSummary("定时测速");
            _nextLogTime = Time.time + logInterval;
        }
    }

    private Vector3 GetVelocity(float deltaTime)
    {
        SpeedSource resolvedSource = ResolveSpeedSource();

        if (resolvedSource == SpeedSource.BatCapsuleFollower && targetFollower != null)
            return targetFollower.CurrentVelocity;

        if (resolvedSource == SpeedSource.Rigidbody && targetRigidbody != null)
            return targetRigidbody.velocity;

        return GetTransformDeltaVelocity(deltaTime);
    }

    private SpeedSource ResolveSpeedSource()
    {
        if (speedSource != SpeedSource.Auto)
            return speedSource;

        if (targetFollower != null)
            return SpeedSource.BatCapsuleFollower;

        if (targetRigidbody != null)
            return SpeedSource.Rigidbody;

        return SpeedSource.TransformDelta;
    }

    private Vector3 GetTransformDeltaVelocity(float deltaTime)
    {
        if (!_hasLastPosition || deltaTime <= 0f)
        {
            _lastPosition = transform.position;
            _hasLastPosition = true;
            return Vector3.zero;
        }

        Vector3 velocity = (transform.position - _lastPosition) / deltaTime;
        _lastPosition = transform.position;
        return velocity;
    }

    private void UpdateSwing(float speed, float deltaTime)
    {
        if (!_isSwinging && speed >= swingStartSpeed)
            BeginSwing();

        if (!_isSwinging)
            return;

        _currentSwingPeak = Mathf.Max(_currentSwingPeak, speed);
        _currentSwingSpeedSum += speed * deltaTime;
        _currentSwingDuration += deltaTime;

        if (speed <= swingEndSpeed)
            _belowEndSpeedDuration += deltaTime;
        else
            _belowEndSpeedDuration = 0f;

        if (_belowEndSpeedDuration >= swingEndGraceTime)
            EndSwing();
    }

    private void BeginSwing()
    {
        _isSwinging = true;
        _currentSwingPeak = 0f;
        _currentSwingSpeedSum = 0f;
        _currentSwingDuration = 0f;
        _belowEndSpeedDuration = 0f;
    }

    private void EndSwing()
    {
        _isSwinging = false;

        if (_currentSwingDuration < minimumSwingDuration)
            return;

        lastSwingPeakSpeed = _currentSwingPeak;
        lastSwingAverageSpeed = _currentSwingSpeedSum / _currentSwingDuration;
        swingCount++;

        _swingPeakSum += lastSwingPeakSpeed;
        _swingAverageSum += lastSwingAverageSpeed;
        averageSwingPeakSpeed = _swingPeakSum / swingCount;
        averageSwingAverageSpeed = _swingAverageSum / swingCount;

        LogSummary("挥棒结束");
    }

    public void ResetStats()
    {
        currentSpeed = 0f;
        maxSpeed = 0f;
        sampledAverageSpeed = 0f;
        swingCount = 0;
        lastSwingPeakSpeed = 0f;
        lastSwingAverageSpeed = 0f;
        averageSwingPeakSpeed = 0f;
        averageSwingAverageSpeed = 0f;

        _sampledSpeedSum = 0f;
        _sampledDuration = 0f;
        _isSwinging = false;
        _currentSwingPeak = 0f;
        _currentSwingSpeedSum = 0f;
        _currentSwingDuration = 0f;
        _belowEndSpeedDuration = 0f;
        _swingPeakSum = 0f;
        _swingAverageSum = 0f;
    }

    private void LogSummary(string prefix)
    {
        Debug.Log($"[{nameof(SwingSpeedLogger)}] {prefix} | 当前 {currentSpeed:F2} | 最高 {maxSpeed:F2} | 采样平均 {sampledAverageSpeed:F2} | 挥棒次数 {swingCount} | 上次峰值 {lastSwingPeakSpeed:F2} | 上次平均 {lastSwingAverageSpeed:F2} | 平均峰值 {averageSwingPeakSpeed:F2} | 平均挥棒速度 {averageSwingAverageSpeed:F2}", this);
    }

    private void OnGUI()
    {
        if (!showOnGUI)
            return;

        GUI.Label(new Rect(20f, 20f, 520f, 120f),
            $"SwingSpeedLogger\n" +
            $"Current: {currentSpeed:F2}\n" +
            $"Max: {maxSpeed:F2}\n" +
            $"Sample Avg: {sampledAverageSpeed:F2}\n" +
            $"Swings: {swingCount}\n" +
            $"Last Peak / Avg: {lastSwingPeakSpeed:F2} / {lastSwingAverageSpeed:F2}\n" +
            $"Avg Peak / Avg Swing: {averageSwingPeakSpeed:F2} / {averageSwingAverageSpeed:F2}");
    }
}
