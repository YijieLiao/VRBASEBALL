using UnityEngine;

/// <summary>
/// 独立演示脚本（与正式游戏逻辑分开）：
/// 模拟真实“被击打”场景：
/// - 只在击打瞬间改变速度（突变）
/// - 不做位置瞬移
/// - 轨迹可连续保留，便于观察拖尾
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class BallTrailDemoMotion : MonoBehaviour
{
    [Header("Demo")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = true;

    [Header("Hit Points (Local)")]
    [SerializeField] private Vector3 localHitPointA = new Vector3(-3f, 1.2f, 0f);
    [SerializeField] private Vector3 localHitPointB = new Vector3(3f, 1.2f, 0f);

    [Tooltip("到达端点附近多少米触发下一次击打")]
    [SerializeField] private float hitRadius = 0.35f;

    [Tooltip("是否只在XZ平面判定命中端点（推荐开，避免Y高度导致一直下坠）")]
    [SerializeField] private bool hitCheckInXZOnly = true;

    [Header("Flight")]
    [Tooltip("击打初速度（m/s）")]
    [SerializeField] private float hitSpeed = 8.5f;

    [Tooltip("垂直抬升速度（m/s），形成弧线")]
    [SerializeField] private float upwardVelocity = 3.2f;

    [Tooltip("随机偏移（m/s），让每次轨迹略有不同")]
    [SerializeField] private float randomVelocityJitter = 0.45f;

    [Header("Rhythm")]
    [Tooltip("两次击打最小间隔（秒），避免重复触发")]
    [SerializeField] private float minHitInterval = 0.12f;

    [Tooltip("重力倍率（1=使用默认Physics重力）")]
    [SerializeField] private float gravityScale = 1f;

    [Header("Fail-safe (Demo only)")]
    [Tooltip("超过该时间仍未触发下一次击打时，自动补一次击打")]
    [SerializeField] private float maxFlightTimeBeforeAutoHit = 2.0f;

    [Tooltip("球低于两端点最低高度该阈值时，自动补一次击打")]
    [SerializeField] private float autoHitWhenBelowHeightOffset = 1.2f;

    private Rigidbody _rb;
    private bool _running;
    private bool _towardsB = true;
    private float _lastHitTime = -999f;

    private Vector3 _origin;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _origin = transform.localPosition;

        _rb.useGravity = true;
        _rb.isKinematic = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartDemo();
        }
    }

    private void FixedUpdate()
    {
        if (!_running)
            return;

        // 额外重力（用于调弧线快慢）
        if (gravityScale > 0f && Mathf.Abs(gravityScale - 1f) > 0.001f)
        {
            Vector3 extraGravity = Physics.gravity * (gravityScale - 1f);
            _rb.AddForce(extraGravity, ForceMode.Acceleration);
        }

        Vector3 targetLocal = _towardsB ? localHitPointB : localHitPointA;
        Vector3 targetWorld = transform.parent != null
            ? transform.parent.TransformPoint(targetLocal)
            : targetLocal;

        float dist = DistanceToTarget(transform.position, targetWorld);
        bool canHit = Time.time - _lastHitTime >= minHitInterval;

        if (dist <= hitRadius && canHit)
        {
            TriggerNextHit();
            return;
        }

        // 演示保底：防止一直下坠不再触发
        if (canHit)
        {
            bool overtime = Time.time - _lastHitTime > maxFlightTimeBeforeAutoHit;
            float minY = Mathf.Min(localHitPointA.y, localHitPointB.y);
            float worldMinY = transform.parent != null ? transform.parent.TransformPoint(new Vector3(0f, minY, 0f)).y : minY;
            bool tooLow = transform.position.y < worldMinY - autoHitWhenBelowHeightOffset;

            if (overtime || tooLow)
            {
                TriggerNextHit();
            }
        }
    }

    [ContextMenu("Start Demo")]
    public void StartDemo()
    {
        _running = true;
        _towardsB = true;

        transform.localPosition = localHitPointA;
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        _lastHitTime = Time.time;
        ApplyHitVelocity();
    }

    [ContextMenu("Stop Demo")]
    public void StopDemo()
    {
        _running = false;
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

    [ContextMenu("Reset Position")]
    public void ResetPosition()
    {
        _running = false;
        _towardsB = true;
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        transform.localPosition = _origin;
    }

    private void TriggerNextHit()
    {
        if (!_towardsB && !loop)
        {
            _running = false;
            return;
        }

        _towardsB = !_towardsB;
        _lastHitTime = Time.time;
        ApplyHitVelocity();
    }

    private float DistanceToTarget(Vector3 posWorld, Vector3 targetWorld)
    {
        if (!hitCheckInXZOnly)
        {
            return Vector3.Distance(posWorld, targetWorld);
        }

        Vector2 p = new Vector2(posWorld.x, posWorld.z);
        Vector2 t = new Vector2(targetWorld.x, targetWorld.z);
        return Vector2.Distance(p, t);
    }

    private void ApplyHitVelocity()
    {
        Vector3 targetLocal = _towardsB ? localHitPointB : localHitPointA;
        Vector3 targetWorld = transform.parent != null
            ? transform.parent.TransformPoint(targetLocal)
            : targetLocal;

        Vector3 dir = (targetWorld - transform.position);
        dir.y = 0f; // 水平朝向端点，竖直由 upwardVelocity 控制
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;

        Vector3 v = dir * hitSpeed;
        v.y = upwardVelocity;

        if (randomVelocityJitter > 0f)
        {
            v += new Vector3(
                Random.Range(-randomVelocityJitter, randomVelocityJitter),
                Random.Range(-randomVelocityJitter * 0.25f, randomVelocityJitter * 0.25f),
                Random.Range(-randomVelocityJitter, randomVelocityJitter)
            );
        }

        // 关键：只改速度，不改位置。
        _rb.velocity = v;
    }
}
