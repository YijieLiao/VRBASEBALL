using UnityEngine;

/// <summary>
/// 挂在 Ball 上的击球火花特效。
/// 不修改 Pitcher，只监听球自己的碰撞，在撞到球棒时于接触点生成一次粒子爆发。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class BatImpactSparkVfx : MonoBehaviour
{
    [Header("识别球棒")]
    [Tooltip("可选：如果你已经有 HitJudge，就拖进来；有它时只有有效击球状态才播放")]
    [SerializeField] private HitJudge hitJudge;

    [Tooltip("球棒层名。默认和 Pitcher 里一致：BatFollower")]
    [SerializeField] private string batLayerName = "BatFollower";

    [Tooltip("球棒层掩码。留空时会自动根据 Layer 名称 BatFollower 计算")]
    [SerializeField] private LayerMask batLayers = 1 << 8;

    [Tooltip("如果场景里球棒有 BatCapsuleFollower 组件，也会被视为球棒")]
    [SerializeField] private bool recognizeBatCapsuleFollower = true;

    [Tooltip("如果场景里球棒有 BatCapsule 组件，也会被视为球棒")]
    [SerializeField] private bool recognizeBatCapsule = true;

    [Header("粒子外观")]
    [SerializeField] private Texture2D sparkTexture;
    [SerializeField] private Material sparkMaterial;
    [SerializeField] private Color sparkColor = new Color(1f, 0.93f, 0.25f, 1f);
    [SerializeField] private int burstCount = 10;
    [SerializeField] private float lifetime = 1.2f;
    [SerializeField] private float startSpeed = 2.1f;
    [SerializeField] private float particleSize = 0.5f;
    [SerializeField] private float spreadAngle = 55f;
    [SerializeField] private float emissionRadius = 0.02f;
    [SerializeField] private float collisionCooldown = 0.04f;
    [SerializeField] private bool inheritRelativeVelocity = true;
    [SerializeField] private float velocityBoostMultiplier = 0.12f;
    [SerializeField] private bool alignToImpactNormal = true;

    [Header("调试")]
    [SerializeField] private bool verboseLogs = false;

    private Rigidbody _rb;
    private ParticleSystem _particleSystem;
    private ParticleSystemRenderer _particleRenderer;
    private float _lastPlayTime = -999f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (hitJudge == null)
            hitJudge = FindObjectOfType<HitJudge>();

        EnsureParticleSystem();
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(batLayerName))
            batLayerName = "BatFollower";
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.contactCount <= 0)
            return;

        if (!IsBat(collision.collider))
            return;

        if (hitJudge != null && !hitJudge.HasActiveHit)
        {
            if (verboseLogs)
                Debug.Log($"BatImpactSparkVfx ignored because hit is inactive. collider={collision.collider.name}", this);
            return;
        }

        ContactPoint contact = collision.GetContact(0);
        Vector3 normal = contact.normal.sqrMagnitude > 0.0001f ? contact.normal : Vector3.up;
        Vector3 impactVelocity = inheritRelativeVelocity ? GetRelativeVelocity(collision.rigidbody) : Vector3.zero;
        Play(contact.point, normal, impactVelocity);
    }

    private bool IsBat(Collider other)
    {
        if (other == null)
            return false;

        if (batLayers.value != 0 && ((1 << other.gameObject.layer) & batLayers.value) != 0)
            return true;

        int layerByName = LayerMask.NameToLayer(batLayerName);
        if (layerByName >= 0 && other.gameObject.layer == layerByName)
            return true;

        if (recognizeBatCapsuleFollower && other.GetComponentInParent<BatCapsuleFollower>() != null)
            return true;

        if (recognizeBatCapsule && other.GetComponentInParent<BatCapsule>() != null)
            return true;

        return false;
    }

    private Vector3 GetRelativeVelocity(Rigidbody batRigidbody)
    {
        Vector3 ballVelocity = _rb != null ? _rb.velocity : Vector3.zero;
        Vector3 batVelocity = batRigidbody != null ? batRigidbody.velocity : Vector3.zero;
        Vector3 relative = ballVelocity - batVelocity;
        return relative.sqrMagnitude > 0.0001f ? relative : batVelocity;
    }

    public void Play(Vector3 worldPosition, Vector3 normal) => Play(worldPosition, normal, Vector3.zero);

    public void Play(Vector3 worldPosition, Vector3 normal, Vector3 inheritedVelocity)
    {
        if (Time.time - _lastPlayTime < collisionCooldown)
            return;

        _lastPlayTime = Time.time;
        EnsureParticleSystem();
        if (_particleSystem == null)
            return;

        _particleSystem.transform.SetPositionAndRotation(
            worldPosition,
            alignToImpactNormal && normal.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(normal.normalized) : Quaternion.identity);

        int count = Mathf.Max(1, burstCount);
        float extraSpeed = inheritRelativeVelocity ? Mathf.Clamp(inheritedVelocity.magnitude * velocityBoostMultiplier, 0f, startSpeed) : 0f;

        var main = _particleSystem.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = sparkColor;
        main.startLifetime = lifetime;
        main.startSpeed = startSpeed + extraSpeed;
        main.startSize = particleSize;
        main.maxParticles = Mathf.Max(16, count * 4);

        var emission = _particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count, (short)(count + 2), 1, 0.01f) });

        var shape = _particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = spreadAngle;
        shape.radius = emissionRadius;
        shape.radiusThickness = 1f;

        _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _particleSystem.Play(true);

        if (verboseLogs)
            Debug.Log($"BatImpactSparkVfx played at {worldPosition}", this);
    }

    private void EnsureParticleSystem()
    {
        if (_particleSystem != null)
            return;

        GameObject fx = new GameObject("BatImpactSparkParticles");
        fx.transform.SetParent(transform, false);
        _particleSystem = fx.AddComponent<ParticleSystem>();
        _particleRenderer = fx.GetComponent<ParticleSystemRenderer>();

        var main = _particleSystem.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = sparkColor;
        main.startLifetime = lifetime;
        main.startSpeed = startSpeed;
        main.startSize = particleSize;
        main.startSize3D = false;
        main.maxParticles = Mathf.Max(16, burstCount * 4);

        var emission = _particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        var shape = _particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = spreadAngle;
        shape.radius = emissionRadius;
        shape.radiusThickness = 1f;

        var sizeOverLifetime = _particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.separateAxes = false;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.75f, 0.35f),
            new Keyframe(1f, 0f)
        ));

        _particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        _particleRenderer.alignment = ParticleSystemRenderSpace.View;
        _particleRenderer.sortMode = ParticleSystemSortMode.Distance;

        if (sparkMaterial == null)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                Shader.Find("Particles/Standard Unlit") ??
                Shader.Find("Unlit/Texture");

            if (shader != null)
                sparkMaterial = new Material(shader);
        }

        if (sparkMaterial != null)
        {
            _particleRenderer.material = sparkMaterial;
            ApplyTextureToMaterial(_particleRenderer.material, sparkTexture);
        }
        else if (sparkTexture != null)
        {
            Debug.LogWarning($"BatImpactSparkVfx could not create a particle material on {name}. Please assign Spark Material manually.", this);
        }
    }

    private static void ApplyTextureToMaterial(Material material, Texture2D texture)
    {
        if (material == null || texture == null)
            return;

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);

        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);
    }
}
