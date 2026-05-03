using UnityEngine;

public class HitResultPopupSpawner : MonoBehaviour
{
    private enum SpawnMode
    {
        PlayerFront,
        SpawnPoint,
        LandingPosition
    }

    [Header("引用")]
    [SerializeField] private HitJudge hitJudge;
    [SerializeField] private HitResultPopup popupPrefab;
    [SerializeField] private Transform lookAtTarget;

    [Header("显示位置")]
    [SerializeField] private SpawnMode spawnMode = SpawnMode.PlayerFront;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Vector3 spawnPositionOffset;

    [Header("玩家前方显示")]
    [SerializeField] private float playerForwardDistance = 1.2f;
    [SerializeField] private float playerVerticalOffset = -0.25f;
    [SerializeField] private bool followPlayerYawOnly = true;

    [Header("结果过滤")]
    [SerializeField] private bool hideNoneResult = true;
    [SerializeField] private bool logResults = true;

    private HitJudge subscribedHitJudge;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
    }

    private void Start()
    {
        ResolveReferences();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void ResolveReferences()
    {
        if (hitJudge == null)
            hitJudge = FindObjectOfType<HitJudge>();

        if (lookAtTarget == null && Camera.main != null)
            lookAtTarget = Camera.main.transform;
    }

    private void Subscribe()
    {
        if (subscribedHitJudge == hitJudge)
            return;

        Unsubscribe();

        if (hitJudge == null)
            return;

        hitJudge.onHitResult.AddListener(ShowResult);
        subscribedHitJudge = hitJudge;
    }

    private void Unsubscribe()
    {
        if (subscribedHitJudge == null)
            return;

        subscribedHitJudge.onHitResult.RemoveListener(ShowResult);
        subscribedHitJudge = null;
    }

    public void ShowResult(HitResult result, Vector3 landingPosition)
    {
        if (logResults)
            Debug.Log($"Hit result popup: {result} at landing {landingPosition}", this);

        if (popupPrefab == null)
        {
            Debug.LogWarning("HitResultPopupSpawner is missing popupPrefab.", this);
            return;
        }

        if (hideNoneResult && result == HitResult.None)
            return;

        Vector3 spawnPosition = GetSpawnPosition(landingPosition);
        HitResultPopup popup = Instantiate(popupPrefab, spawnPosition, Quaternion.identity);
        popup.gameObject.SetActive(true);
        popup.Play(GetResultText(result), GetResultColor(result), lookAtTarget);
    }

    private Vector3 GetSpawnPosition(Vector3 landingPosition)
    {
        switch (spawnMode)
        {
            case SpawnMode.SpawnPoint:
                if (spawnPoint != null)
                    return spawnPoint.position + spawnPositionOffset;
                return GetPlayerFrontPosition() + spawnPositionOffset;

            case SpawnMode.LandingPosition:
                return landingPosition + spawnPositionOffset;

            default:
                return GetPlayerFrontPosition() + spawnPositionOffset;
        }
    }

    private Vector3 GetPlayerFrontPosition()
    {
        if (lookAtTarget == null)
            return transform.position;

        Vector3 forward = lookAtTarget.forward;
        if (followPlayerYawOnly)
            forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
            forward = lookAtTarget.forward;

        forward.Normalize();
        return lookAtTarget.position + forward * playerForwardDistance + Vector3.up * playerVerticalOffset;
    }

    private string GetResultText(HitResult result)
    {
        switch (result)
        {
            case HitResult.Foul:
                return "出界";
            case HitResult.Caught:
                return "接杀";
            case HitResult.FairLanding:
                return "有效落地";
            case HitResult.HomeRun:
                return "全垒打";
            default:
                return "无效";
        }
    }

    private Color GetResultColor(HitResult result)
    {
        switch (result)
        {
            case HitResult.Foul:
                return new Color(1f, 0.2f, 0.15f, 1f);
            case HitResult.Caught:
                return new Color(1f, 0.55f, 0.1f, 1f);
            case HitResult.FairLanding:
                return new Color(0.25f, 1f, 0.35f, 1f);
            case HitResult.HomeRun:
                return new Color(1f, 0.82f, 0.15f, 1f);
            default:
                return new Color(0.75f, 0.75f, 0.75f, 1f);
        }
    }
}
