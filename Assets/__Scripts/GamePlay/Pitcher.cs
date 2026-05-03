using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
public class Pitcher : MonoBehaviour
{
    [Header("发球与落点引用")]
    public Transform launchPoint;
    public Transform targetPoint;

    [Header("微缩物理调控")]
    public float flightTime = 0.5f;
    public float microGravityScale = 0.1f;

    [Header("落地停止调控")]
    [Tooltip("落地后的线性阻力")]
    public float groundDrag = 2f;
    [Tooltip("落地后的旋转阻力（越大球越不爱滚）")]
    public float groundAngularDrag = 20f;

    [Header("碰撞分类")]
    public LayerMask groundLayers;
    public LayerMask batLayers;

    [Header("击球辅助")]
    [Tooltip("有效挥杆后最低出球速度。只有球棒速度接近吃满辅助速度时才会完整使用这个值。")]
    [SerializeField] private float minHitSpeed = 13f;

    [Tooltip("轻微碰到球棒时的辅助出球速度。越低，静止挡到球时越不会飞远。")]
    [SerializeField] private float weakHitSpeed = 4f;

    [Tooltip("球棒达到这个速度后，最低出球速度才完整生效。越高越需要真的挥杆。")]
    [SerializeField] private float fullAssistBatSpeed = 3f;

    [Tooltip("击中后最高出球速度。防止挥棒过快或追踪抖动把球打飞到离谱速度。")]
    [SerializeField] private float maxHitSpeed = 24f;

    [Tooltip("球棒速度转换成出球速度的倍率。越大越吃挥棒速度，高手挥快时球飞得更远。")]
    [SerializeField] private float hitPowerMultiplier = 2.4f;

    [Tooltip("击球后额外保证的最低向上速度。越大球越容易起高飞，越小越容易贴地滚。")]
    [SerializeField] private float upwardBoost = 2.2f;

    [Tooltip("触发有效击球所需的最低球棒速度。越低越容易轻触发，越高越需要真正挥动。")]
    [SerializeField] private float minBatSpeedToHit = 0.2f;

    [Tooltip("一次有效击球后，多少秒内不再接受下一次球棒击球，避免连续碰撞把球打回身后。")]
    [SerializeField] private float hitCooldown = 0.3f;

    [Tooltip("击球后多少秒内暂时不恢复地面阻力，避免地滚球刚被打出就被地面阻力吃掉。")]
    [SerializeField] private float ignoreGroundAfterHitTime = 0.25f;

    [Tooltip("出球方向受碰撞反弹方向影响的比例。0=完全按球场方向，1=更接近真实反弹；不会直接把球打回身后。")]
    [Range(0f, 1f)]
    [FormerlySerializedAs("batVelocityDirectionWeight")]
    [SerializeField] private float reflectionDirectionWeight = 0.25f;

    [Tooltip("最终出球方向的最低向前程度。越高越稳定，越不容易因为挥杆方向让球飞近或飞歪。")]
    [Range(-1f, 1f)]
    [SerializeField] private float minForwardDot = 0.25f;

    [Header("落点判定")]
    [Tooltip("可选的落点判定组件，用于计算安打/全垒打/出界")]
    public HitJudge hitJudge;

    private Rigidbody rb;
    private TrailRenderer[] trailRenderers;
    private bool isPitching = false;
    private Vector3 customGravity;
    private float lastBatHitTime = -999f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        trailRenderers = GetComponentsInChildren<TrailRenderer>(true);
        if (hitJudge == null)
            hitJudge = FindObjectOfType<HitJudge>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (launchPoint == null || targetPoint == null)
        {
            Debug.LogError("请在 Inspector 面板中分配 Launch Point 和 Target Point！");
        }

        ResetBall();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            ResetBall();
        }
        else if (Input.GetKeyDown(KeyCode.Space) && !isPitching)
        {
            PitchBall();
        }

        if (transform.position.y < -10f)
        {
            ResetBall();
        }
    }

    void FixedUpdate()
    {
        if (isPitching)
        {
            rb.AddForce(customGravity, ForceMode.Acceleration);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsInLayerMask(collision.gameObject.layer, batLayers))
        {
            ApplyBatHit(collision);
            return;
        }

        if (IsInLayerMask(collision.gameObject.layer, groundLayers))
        {
            hitJudge?.OnBallGrounded(collision.contacts[0].point);
            EnterGroundState();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (Time.time - lastBatHitTime < ignoreGroundAfterHitTime)
            return;

        if (IsInLayerMask(collision.gameObject.layer, groundLayers))
        {
            EnterGroundState();
        }
    }

    public void ResetBall()
    {
        if (launchPoint == null) return;

        isPitching = false;
        lastBatHitTime = -999f;
        hitJudge?.ResetState();

        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.drag = 0f;
        rb.angularDrag = 0.05f;

        transform.position = launchPoint.position;
        transform.rotation = launchPoint.rotation;
        ClearTrails();
    }

    private void ClearTrails()
    {
        if (trailRenderers == null)
            return;

        foreach (TrailRenderer trailRenderer in trailRenderers)
        {
            if (trailRenderer == null)
                continue;

            bool wasEmitting = trailRenderer.emitting;
            trailRenderer.emitting = false;
            trailRenderer.Clear();
            trailRenderer.emitting = wasEmitting;
        }
    }

    public void PitchBall()
    {
        if (launchPoint == null || targetPoint == null) return;

        transform.position = launchPoint.position;
        transform.rotation = launchPoint.rotation;
        ClearTrails();

        EnterFlightState();

        Vector3 worldDisplacement = targetPoint.position - launchPoint.position;
        Vector3 velocityXZ = new Vector3(worldDisplacement.x, 0, worldDisplacement.z) / flightTime;

        float dy = worldDisplacement.y;
        float g = customGravity.y;
        float velocityYVal = (dy / flightTime) - (0.5f * g * flightTime);
        Vector3 velocityY = new Vector3(0, velocityYVal, 0);

        rb.velocity = velocityXZ + velocityY;
    }

    private void ApplyBatHit(Collision collision)
    {
        if (Time.time - lastBatHitTime < hitCooldown)
            return;

        Vector3 incomingVelocity = rb.velocity;
        Vector3 batVelocity = GetBatHitVelocity(collision);
        Vector3 batPlanarVelocity = batVelocity;
        batPlanarVelocity.y = 0f;

        float batSpeed = batPlanarVelocity.magnitude;
        if (batSpeed < minBatSpeedToHit)
            return;

        EnterFlightState();
        lastBatHitTime = Time.time;
        hitJudge?.OnBallHit();

        Vector3 hitDirection = GetAssistedHitDirection(collision, incomingVelocity, batVelocity);
        float exitSpeed = CalculateExitSpeed(batSpeed);
        Vector3 hitVelocity = hitDirection * exitSpeed;
        hitVelocity.y = Mathf.Max(hitVelocity.y, upwardBoost);

        rb.velocity = hitVelocity;
        rb.angularVelocity = Vector3.zero;
    }

    private Vector3 GetBatHitVelocity(Collision collision)
    {
        BatCapsuleFollower follower = collision.collider.GetComponentInParent<BatCapsuleFollower>();
        if (follower != null)
            return follower.CurrentVelocity;

        Rigidbody batRigidbody = collision.rigidbody;
        return batRigidbody != null ? batRigidbody.velocity : Vector3.zero;
    }

    private float CalculateExitSpeed(float batSpeed)
    {
        float fullAssistSpeed = Mathf.Max(fullAssistBatSpeed, minBatSpeedToHit + 0.001f);
        float swingStrength = Mathf.InverseLerp(minBatSpeedToHit, fullAssistSpeed, batSpeed);
        float assistedMinimum = Mathf.Lerp(weakHitSpeed, minHitSpeed, swingStrength);
        float powerSpeed = batSpeed * hitPowerMultiplier;
        return Mathf.Clamp(Mathf.Max(assistedMinimum, powerSpeed), weakHitSpeed, maxHitSpeed);
    }

    private Vector3 GetAssistedHitDirection(Collision collision, Vector3 incomingVelocity, Vector3 batVelocity)
    {
        Vector3 fieldDirection = GetFieldHitDirection();
        Vector3 relativeVelocity = incomingVelocity - batVelocity;
        if (relativeVelocity.sqrMagnitude < 0.0001f)
            return fieldDirection;

        ContactPoint contact = collision.GetContact(0);
        Vector3 reflectedDirection = Vector3.Reflect(relativeVelocity.normalized, contact.normal).normalized;
        reflectedDirection.y = Mathf.Max(reflectedDirection.y, 0f);

        if (reflectedDirection.sqrMagnitude < 0.0001f)
            return fieldDirection;

        reflectedDirection.Normalize();
        Vector3 hitDirection = Vector3.Slerp(fieldDirection, reflectedDirection, reflectionDirectionWeight).normalized;
        return Vector3.Dot(hitDirection, fieldDirection) < minForwardDot ? fieldDirection : hitDirection;
    }

    private Vector3 GetFieldHitDirection()
    {
        if (launchPoint != null && targetPoint != null)
        {
            Vector3 direction = launchPoint.position - targetPoint.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                return direction.normalized;
        }

        Vector3 fallback = -rb.velocity;
        fallback.y = 0f;
        if (fallback.sqrMagnitude > 0.0001f)
            return fallback.normalized;

        fallback = transform.forward;
        fallback.y = 0f;
        return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;
    }

    private void EnterFlightState()
    {
        rb.isKinematic = false;
        rb.drag = 0f;
        rb.angularDrag = 0.05f;
        customGravity = new Vector3(0, -9.81f * microGravityScale, 0);
        isPitching = true;
    }

    private void EnterGroundState()
    {
        rb.drag = groundDrag;
        rb.angularDrag = groundAngularDrag;
        isPitching = false;
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }
}
