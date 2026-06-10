using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class ComboFireworkConfig
{
    [Tooltip("触发连击数")]
    public int threshold;

    [Tooltip("此连击对应的烟花预制体")]
    public GameObject fireworksPrefab;

    [Tooltip("此连击对应的弹出文字（空则不弹）")]
    public string popupText;
}

/// <summary>
/// 监听 ClassicModeRoundManager 事件，在落地和连击时生成烟花 + 连击文字弹出。
/// 有效落地：fairLandingPositions 全点位同时放。
/// 全垒打：homeRunPositions 从左到右依次放。
/// 连击：comboPositions 从左到右依次放 + 弹出 COMBO Sprite。
/// </summary>
public class FireworksSpawner : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private ClassicModeRoundManager roundManager;

    [Header("有效落地 — 全点位同时")]
    [SerializeField] private GameObject fairLandingFireworkPrefab;
    [SerializeField] private Transform[] fairLandingPositions;

    [Header("全垒打 — 从左到右依次")]
    [SerializeField] private GameObject homeRunFireworkPrefab;
    [SerializeField] private Transform[] homeRunPositions;
    [SerializeField] private float homeRunSpawnInterval = 0.1f;

    [Header("连击烟花 — 从左到右依次")]
    [SerializeField] private Transform[] comboPositions;
    [SerializeField] private float comboSpawnInterval = 0.1f;

    [Header("连击文字弹出")]
    [SerializeField] private ComboPopup comboPopup;
    [SerializeField] private Transform comboPopupSpawnPoint;

    [Header("连击触发配置")]
    [SerializeField] private ComboFireworkConfig[] comboConfigs;

    private int lastCombo;
    private Coroutine fireworkRoutine;

    void Awake()
    {
        if (roundManager == null)
            roundManager = FindObjectOfType<ClassicModeRoundManager>();
    }

    void OnEnable()
    {
        if (roundManager != null)
        {
            roundManager.OnBallResult += OnBallResult;
            roundManager.OnComboChanged += OnComboChanged;
            Debug.Log($"[FW] 已订阅。popup={comboPopup}, spawnPt={comboPopupSpawnPoint}, configs={comboConfigs?.Length ?? 0}");
        }
        else
            Debug.LogWarning("[FW] roundManager null，未订阅！");
    }

    void OnDisable()
    {
        if (roundManager != null)
        {
            roundManager.OnBallResult -= OnBallResult;
            roundManager.OnComboChanged -= OnComboChanged;
        }
    }

    private void OnBallResult(BallResultInfo info)
    {
        if (IsComboMilestone(lastCombo))
            return; // 连击已接管

        switch (info.result)
        {
            case HitResult.HomeRun:
                if (homeRunFireworkPrefab != null)
                {
                    if (fireworkRoutine != null) StopCoroutine(fireworkRoutine);
                    fireworkRoutine = StartCoroutine(SpawnSequential(homeRunFireworkPrefab, homeRunPositions, homeRunSpawnInterval));
                }
                break;

            case HitResult.FairLanding:
                if (fairLandingFireworkPrefab != null)
                {
                    foreach (var pos in fairLandingPositions)
                        if (pos != null)
                            Instantiate(fairLandingFireworkPrefab, pos.position, Quaternion.identity);
                }
                break;
        }
    }

    private bool IsComboMilestone(int combo)
    {
        if (comboConfigs == null) return false;
        foreach (var cfg in comboConfigs)
            if (cfg.threshold == combo) return true;
        return false;
    }

    private void OnComboChanged(int combo, float multiplier)
    {
        if (combo <= lastCombo)
        {
            Debug.Log($"[FW] OnComboChanged combo={combo} <= lastCombo={lastCombo}, 更新lastCombo后返回");
            lastCombo = combo;
            return;
        }
        Debug.Log($"[FW] OnComboChanged combo={combo} > lastCombo={lastCombo}");
        lastCombo = combo;

        if (comboConfigs == null) { Debug.Log("[FW] comboConfigs null"); return; }

        foreach (var cfg in comboConfigs)
        {
            if (cfg.threshold == combo)
            {
                Debug.Log($"[FW] 匹配! threshold={cfg.threshold} popupText=\"{cfg.popupText}\" popup={comboPopup} spawnPt={comboPopupSpawnPoint}");
                // 烟花 — 从左到右
                if (cfg.fireworksPrefab != null && comboPositions != null && comboPositions.Length > 0)
                {
                    if (fireworkRoutine != null) StopCoroutine(fireworkRoutine);
                    fireworkRoutine = StartCoroutine(SpawnSequential(cfg.fireworksPrefab, comboPositions, comboSpawnInterval));
                }

                // 文字弹出
                if (!string.IsNullOrEmpty(cfg.popupText) && comboPopup != null && comboPopupSpawnPoint != null)
                {
                    Debug.Log($"[FW] Show popup \"{cfg.popupText}\" at {comboPopupSpawnPoint.position}");
                    comboPopup.Show(cfg.popupText, comboPopupSpawnPoint.position);
                }
                else Debug.Log($"[FW] 跳过弹出: text={cfg.popupText} popup={comboPopup} spawnPt={comboPopupSpawnPoint}");

                return;
            }
        }
    }

    private IEnumerator SpawnSequential(GameObject prefab, Transform[] positions, float interval)
    {
        foreach (var pos in positions)
        {
            if (pos != null)
                Instantiate(prefab, pos.position, Quaternion.identity);
            yield return new WaitForSeconds(interval);
        }
        fireworkRoutine = null;
    }
}
