using UnityEngine;

/// <summary>
/// 监听 HitJudge 的落地事件，在落点生成环形消散特效。
/// </summary>
public class LandingRingSpawner : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private HitJudge hitJudge;

    [Header("预制体")]
    [SerializeField] private GameObject ringPrefab;

    [Header("生成设置")]
    [SerializeField] private float groundYOffset = 0.02f;
    [SerializeField] private bool faceUp = true;

    [Header("颜色 — 界内")]
    [SerializeField] private Color fairColor = new Color(0.2f, 0.9f, 0.3f);    // 绿色
    [SerializeField] private Color homeRunColor = new Color(1f, 0.85f, 0f);    // 金色

    [Header("颜色 — 界外 / 无效")]
    [SerializeField] private Color foulColor = new Color(0.9f, 0.3f, 0.2f);     // 红色
    [SerializeField] private Color noneColor = new Color(0.5f, 0.5f, 0.5f);     // 灰色

    void Start()
    {
        if (hitJudge == null)
            hitJudge = FindObjectOfType<HitJudge>();
    }

    void OnEnable()
    {
        if (hitJudge != null)
            hitJudge.onHitResult.AddListener(OnBallLanded);
    }

    void OnDisable()
    {
        if (hitJudge != null)
            hitJudge.onHitResult.RemoveListener(OnBallLanded);
    }

    void OnBallLanded(HitResult result, Vector3 landingPosition)
    {
        if (ringPrefab == null) return;

        Color color = GetColorForResult(result);

        Vector3 spawnPos = landingPosition + Vector3.up * groundYOffset;
        Quaternion rotation = faceUp ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;

        GameObject ring = Instantiate(ringPrefab, spawnPos, rotation);
        LandingRingEffect effect = ring.GetComponent<LandingRingEffect>();
        if (effect == null)
            effect = ring.GetComponentInChildren<LandingRingEffect>();
        if (effect != null)
            effect.SetColor(color);
    }

    Color GetColorForResult(HitResult result)
    {
        switch (result)
        {
            case HitResult.HomeRun:     return homeRunColor;
            case HitResult.FairLanding: return fairColor;
            case HitResult.Foul:        return foulColor;
            default:                    return noneColor;
        }
    }
}
