using UnityEngine;

/// <summary>
/// 棒球轨迹控制：
/// - 球在运动时才发射轨迹
/// - 停止后不再发射，新尾巴按 time 自然均匀消失
/// - 形状与颜色可分开控制：
///   - 形状（长度/宽度）可由脚本统一
///   - 颜色可保留给 Inspector 手动调
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TrailRenderer))]
public class BallTrailController : MonoBehaviour
{
    [Header("Motion Threshold")]
    [Tooltip("优先使用刚体速度（有 Rigidbody 时建议开启）")]
    [SerializeField] private bool useRigidbodyVelocity = true;

    [Tooltip("开始发射速度阈值（m/s）")]
    [SerializeField] private float startEmitSpeed = 0.05f;

    [Tooltip("停止发射速度阈值（m/s），应小于开始阈值")]
    [SerializeField] private float stopEmitSpeed = 0.02f;

    [Tooltip("速度平滑，避免阈值附近闪烁")]
    [Range(1f, 30f)]
    [SerializeField] private float speedSmoothing = 18f;

    [Header("Style Override")]
    [Tooltip("开启后脚本会覆盖长度/宽度/对齐等几何样式（建议开，保证头粗尾细）")]
    [SerializeField] private bool overrideTrailShape = true;

    [Tooltip("开启后脚本会覆盖颜色渐变为白色；关闭后你可在 TrailRenderer 里自由改颜色")]
    [SerializeField] private bool overrideTrailColor = false;

    [Header("Trail Shape (When Shape Override Enabled)")]
    [Tooltip("尾巴消失时间（秒）")]
    [SerializeField] private float trailTime = 0.28f;

    [Tooltip("头部宽度")]
    [SerializeField] private float startWidth = 0.08f;

    [Tooltip("尾部宽度")]
    [SerializeField] private float endWidth = 0.012f;

    [Tooltip("轨迹顶点间距，越大越省性能")]
    [SerializeField] private float minVertexDistance = 0.005f;

    private TrailRenderer _trail;
    private Rigidbody _rb;
    private Vector3 _lastPosition;
    private float _smoothedSpeed;

    private void Awake()
    {
        _trail = GetComponent<TrailRenderer>();
        _rb = GetComponent<Rigidbody>();
        _lastPosition = transform.position;

        ApplyTrailStyleIfNeeded();

        _trail.Clear();
        _trail.emitting = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 编辑模式下不显示拖尾，避免干扰视野。
        if (Application.isPlaying)
            return;

        if (_trail == null)
            _trail = GetComponent<TrailRenderer>();

        if (_trail != null)
        {
            _trail.emitting = false;
            _trail.Clear();
        }
    }
#endif

    private void OnValidate()
    {
        if (stopEmitSpeed > startEmitSpeed)
            stopEmitSpeed = startEmitSpeed * 0.5f;

        if (trailTime < 0.01f) trailTime = 0.01f;
        if (startWidth < 0.001f) startWidth = 0.001f;
        if (endWidth < 0f) endWidth = 0f;
        if (minVertexDistance < 0.001f) minVertexDistance = 0.001f;
    }

    private void Update()
    {
        float currentSpeed = GetCurrentSpeed();
        _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, currentSpeed, 1f - Mathf.Exp(-speedSmoothing * Time.deltaTime));

        bool shouldEmit = _trail.emitting ? _smoothedSpeed > stopEmitSpeed : _smoothedSpeed >= startEmitSpeed;
        _trail.emitting = shouldEmit;
    }

    private float GetCurrentSpeed()
    {
        Vector3 currentPosition = transform.position;
        float transformSpeed = (currentPosition - _lastPosition).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        _lastPosition = currentPosition;

        float rbSpeed = 0f;
        if (useRigidbodyVelocity && _rb != null)
        {
            rbSpeed = _rb.velocity.magnitude;
        }

        // 取较大值，避免某些驱动方式导致 velocity 为 0 但物体实际在移动。
        return Mathf.Max(transformSpeed, rbSpeed);
    }

    private void ApplyTrailStyleIfNeeded()
    {
        if (overrideTrailShape)
        {
            _trail.time = trailTime;
            _trail.minVertexDistance = minVertexDistance;
            _trail.alignment = LineAlignment.View;
            _trail.textureMode = LineTextureMode.Stretch;
            _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _trail.receiveShadows = false;

            AnimationCurve widthCurve = new AnimationCurve(
                new Keyframe(0f, startWidth),
                new Keyframe(1f, endWidth)
            );
            _trail.widthCurve = widthCurve;
        }

        if (overrideTrailColor)
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.45f, 0.65f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            _trail.colorGradient = g;
        }
    }
}
