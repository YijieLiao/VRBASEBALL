using UnityEngine;

/// <summary>
/// 挂在球棒根节点上的持续流光特效。
/// 不依赖模型材质结构，而是在球棒周围生成一圈可见的粒子光环。
/// </summary>
[DisallowMultipleComponent]
public class MagicBatTurbulence : MonoBehaviour
{
    [Header("目标")]
    [Tooltip("留空时会自动搜索自身及子物体上的 Renderer，用来确定球棒大致中心")]
    [SerializeField] private Renderer[] targetRenderers;

    [Tooltip("如果球棒需要跟随某个动画/物体中心，默认会取这些 Renderer 的包围盒中心")]
    [SerializeField] private bool useRendererBoundsCenter = true;

    [Header("光环外观")]
    [SerializeField] private Color glowColor = new Color(1f, 0.82f, 0.32f, 0.75f);
    [SerializeField] private float ringRadius = 0.08f;
    [SerializeField] private float ringHeight = 0.02f;
    [SerializeField] private float orbitSpeed = 180f;
    [SerializeField] private int particleCount = 28;
    [SerializeField] private float particleSize = 0.03f;
    [SerializeField] private float particleLifetime = 0.65f;
    [SerializeField] private float particleStartSpeed = 0.04f;
    [SerializeField] private float noiseStrength = 0.12f;
    [SerializeField] private float pulseSpeed = 2.2f;
    [SerializeField] private float pulseAmount = 0.18f;

    [Header("材质")]
    [Tooltip("可选：粒子材质。留空会自动创建一个 URP/Particles Unlit 材质")]
    [SerializeField] private Material particleMaterial;

    [Tooltip("可选：星星/光点贴图")]
    [SerializeField] private Texture2D particleTexture;

    [Header("调试")]
    [SerializeField] private bool verboseLogs;

    private ParticleSystem _particleSystem;
    private ParticleSystemRenderer _particleRenderer;
    private Transform _effectRoot;
    private Bounds _cachedBounds;
    private bool _hasBounds;
    private ParticleSystem.Particle[] _particlesBuffer;

    private void EnsureParticleBuffer(int count)
    {
        if (_particlesBuffer == null || _particlesBuffer.Length < count)
            _particlesBuffer = new ParticleSystem.Particle[Mathf.Max(count, 128)];
    }

    private void Awake()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>(true);

        CacheBounds();
        EnsureEffectRoot();
        EnsureParticleSystem();
    }

    private void Start()
    {
        CacheBounds();
    }

    private void LateUpdate()
    {
        if (_particleSystem == null)
            return;

        UpdateEffectTransform();
        AnimateParticles();
    }

    private void OnValidate()
    {
        if (ringRadius < 0.01f)
            ringRadius = 0.01f;

        if (particleCount < 4)
            particleCount = 4;

        if (particleSize <= 0f)
            particleSize = 0.01f;
    }

    private void CacheBounds()
    {
        if (!useRendererBoundsCenter || targetRenderers == null || targetRenderers.Length == 0)
        {
            _hasBounds = false;
            return;
        }

        bool hasAny = false;
        Bounds bounds = default;
        foreach (Renderer renderer in targetRenderers)
        {
            if (renderer == null)
                continue;

            if (!hasAny)
            {
                bounds = renderer.bounds;
                hasAny = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        _hasBounds = hasAny;
        _cachedBounds = bounds;
    }

    private void EnsureEffectRoot()
    {
        if (_effectRoot != null)
            return;

        GameObject root = new GameObject("MagicBatTurbulence_EffectRoot");
        root.transform.SetParent(transform, false);
        _effectRoot = root.transform;
    }

    private void EnsureParticleSystem()
    {
        if (_particleSystem != null)
            return;

        GameObject fx = new GameObject("MagicBatTurbulence_Particles");
        fx.transform.SetParent(_effectRoot, false);
        _particleSystem = fx.AddComponent<ParticleSystem>();
        _particleRenderer = fx.GetComponent<ParticleSystemRenderer>();

        var main = _particleSystem.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = Mathf.Max(64, particleCount * 4);
        main.startLifetime = particleLifetime;
        main.startSpeed = particleStartSpeed;
        main.startSize = particleSize;
        main.startColor = glowColor;
        main.scalingMode = ParticleSystemScalingMode.Local;

        var emission = _particleSystem.emission;
        emission.rateOverTime = particleCount * 1.2f;
        emission.enabled = true;

        var shape = _particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = ringRadius;
        shape.radiusThickness = 1f;

        var velocityOverLifetime = _particleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.orbitalX = new ParticleSystem.MinMaxCurve(0f);
        velocityOverLifetime.orbitalY = new ParticleSystem.MinMaxCurve(orbitSpeed);
        velocityOverLifetime.orbitalZ = new ParticleSystem.MinMaxCurve(0f);

        var sizeOverLifetime = _particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.7f, 0.75f),
            new Keyframe(1f, 0f)));

        var colorOverLifetime = _particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new Gradient
        {
            alphaKeys = new[]
            {
                new GradientAlphaKey(glowColor.a, 0f),
                new GradientAlphaKey(glowColor.a, 0.65f),
                new GradientAlphaKey(0f, 1f)
            },
            colorKeys = new[]
            {
                new GradientColorKey(glowColor, 0f),
                new GradientColorKey(Color.Lerp(glowColor, Color.white, 0.35f), 0.5f),
                new GradientColorKey(glowColor, 1f)
            }
        };

        var noise = _particleSystem.noise;
        noise.enabled = true;
        noise.strength = noiseStrength;
        noise.frequency = 0.45f;
        noise.scrollSpeed = 0.18f;

        var textureSheet = _particleSystem.textureSheetAnimation;
        textureSheet.enabled = false;

        _particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        _particleRenderer.alignment = ParticleSystemRenderSpace.View;
        _particleRenderer.sortMode = ParticleSystemSortMode.Distance;
        _particleRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;

        if (particleMaterial == null)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                Shader.Find("Particles/Standard Unlit") ??
                Shader.Find("Unlit/Texture");

            if (shader != null)
                particleMaterial = new Material(shader);
        }

        if (particleMaterial != null)
        {
            _particleRenderer.material = particleMaterial;
            ApplyTexture(_particleRenderer.material, particleTexture);
        }
        else if (verboseLogs)
        {
            Debug.LogWarning($"{nameof(MagicBatTurbulence)} on {name} could not create a particle material.", this);
        }
    }

    private void UpdateEffectTransform()
    {
        Vector3 center = transform.position;
        if (_hasBounds)
            center = _cachedBounds.center;

        _effectRoot.position = center + transform.up * ringHeight;
        _effectRoot.rotation = transform.rotation;

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        var main = _particleSystem.main;
        main.startColor = new Color(glowColor.r, glowColor.g, glowColor.b, glowColor.a * pulse);
        main.startSize = particleSize * Mathf.Lerp(0.9f, 1.15f, pulse);

        var emission = _particleSystem.emission;
        emission.rateOverTime = particleCount * Mathf.Lerp(0.85f, 1.15f, pulse);
    }

    private void AnimateParticles()
    {
        int aliveCount = _particleSystem.GetParticles(_particlesBuffer);
        if (aliveCount <= 0)
            return;

        float life = Mathf.Max(0.0001f, particleLifetime);
        for (int i = 0; i < aliveCount; i++)
        {
            ParticleSystem.Particle p = _particlesBuffer[i];
            float age01 = 1f - Mathf.Clamp01(p.remainingLifetime / life);
            p.velocity = Quaternion.Euler(0f, orbitSpeed * Time.deltaTime, 0f) * p.velocity;
            p.startSize = Mathf.Lerp(particleSize, 0f, age01);
            _particlesBuffer[i] = p;
        }

        _particleSystem.SetParticles(_particlesBuffer, aliveCount);
    }

    private static void ApplyTexture(Material material, Texture2D texture)
    {
        if (material == null || texture == null)
            return;

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);
    }
}
