using UnityEngine;
using UnityEngine.Events;

public enum HitResult
{
    None,
    Foul,
    Caught,
    FairLanding,
    HomeRun
}

[System.Serializable]
public class HitResultEvent : UnityEvent<HitResult, Vector3> { }

public class HitJudge : MonoBehaviour
{
    [Header("参考点")]
    [Tooltip("本垒位置，击球起点")]
    public Transform homePlate;

    [Tooltip("球场前方方向参考点，从本垒指向球场中线")]
    public Transform fieldDirectionRef;

    [Header("界内/出界判定")]
    [Tooltip("界内区的半角范围。45 度表示左右边线总共 90 度")]
    [Range(0f, 90f)]
    public float fairFoulHalfAngle = 45f;

    [Tooltip("球必须落到这个距离之外，才算有效落地")]
    public float fairLandingMinDistance = 1.5f;

    [Header("全垒打判定")]
    [Tooltip("落地点超过这个距离，算全垒打")]
    public float homeRunMinDistance = 6f;

    [Tooltip("全垒打落点允许的最高高度")]
    public float homeRunMaxLandingHeight = 0.5f;

    [Header("事件")]
    public HitResultEvent onHitResult;

    [Header("调试")]
    [SerializeField] private bool logResults = true;

    private Vector3 homePosition;
    private Vector3 fieldForward;
    private bool hasHit;

    [field: SerializeField]
    public float LastHitDistance { get; private set; }
    public bool HasActiveHit => hasHit;

    private void Start()
    {
        UpdateReferencePoints();
    }

    private void UpdateReferencePoints()
    {
        homePosition = homePlate != null ? homePlate.position : transform.position;
        fieldForward = fieldDirectionRef != null
            ? fieldDirectionRef.position - homePosition
            : transform.forward;
        fieldForward.y = 0f;

        if (fieldForward.sqrMagnitude < 0.0001f)
            fieldForward = Vector3.forward;

        fieldForward.Normalize();
    }

    public void ResetState()
    {
        hasHit = false;
    }

    public void OnBallHit()
    {
        hasHit = true;
        UpdateReferencePoints();

        if (logResults)
            Debug.Log("HitJudge received valid hit and is waiting for first landing.", this);
    }

    public void OnBallCaught(Vector3 caughtPosition)
    {
        if (!hasHit)
            return;

        onHitResult?.Invoke(HitResult.Caught, caughtPosition);
        hasHit = false;
    }

    public void OnBallGrounded(Vector3 landingPosition)
    {
        if (!hasHit)
        {
            if (logResults)
                Debug.Log($"HitJudge ignored landing at {landingPosition} because no valid hit is active.", this);
            return;
        }

        HitResult result = JudgeHit(landingPosition);
        if (logResults)
            Debug.Log($"HitJudge result: {result} at {landingPosition}", this);
        onHitResult?.Invoke(result, landingPosition);
        hasHit = false;
    }

    public HitResult JudgeHit(Vector3 landingPosition)
    {
        Vector3 toLanding = landingPosition - homePosition;
        toLanding.y = 0f;

        float distance = toLanding.magnitude;
        LastHitDistance = distance;
        if (distance < fairLandingMinDistance)
            return HitResult.None;

        float angle = Vector3.Angle(fieldForward, toLanding / distance);
        if (angle > fairFoulHalfAngle)
            return HitResult.Foul;

        if (distance >= homeRunMinDistance && landingPosition.y <= homeRunMaxLandingHeight)
            return HitResult.HomeRun;

        return HitResult.FairLanding;
    }

    private void OnDrawGizmosSelected()
    {
        if (homePlate == null)
            return;

        Vector3 origin = homePlate.position;
        Vector3 forward = fieldDirectionRef != null
            ? fieldDirectionRef.position - origin
            : transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        forward.Normalize();

        Vector3 rightBoundary = Quaternion.AngleAxis(fairFoulHalfAngle, Vector3.up) * forward;
        Vector3 leftBoundary = Quaternion.AngleAxis(-fairFoulHalfAngle, Vector3.up) * forward;

        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
        DrawSector(origin, forward, fairFoulHalfAngle, homeRunMinDistance);

        Gizmos.color = Color.white;
        DrawCircle(origin, fairLandingMinDistance);

        Gizmos.color = Color.red;
        DrawCircle(origin, homeRunMinDistance);
        Gizmos.DrawLine(origin, origin + rightBoundary * homeRunMinDistance);
        Gizmos.DrawLine(origin, origin + leftBoundary * homeRunMinDistance);
    }

    private void DrawCircle(Vector3 center, float radius)
    {
        int segments = 32;
        float angleStep = 360f / segments;
        Vector3 prev = center + Vector3.forward * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 next = center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }

    private void DrawSector(Vector3 center, Vector3 forward, float halfAngle, float radius)
    {
        int segments = 16;
        Vector3 rightDir = Quaternion.AngleAxis(halfAngle, Vector3.up) * forward;
        Vector3 leftDir = Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward;

        for (int i = 0; i < segments; i++)
        {
            float t1 = (float)i / segments;
            float t2 = (float)(i + 1) / segments;
            Vector3 dir1 = Vector3.Slerp(rightDir, leftDir, t1);
            Vector3 dir2 = Vector3.Slerp(rightDir, leftDir, t2);
            Gizmos.DrawLine(center + dir1 * radius * 0.1f, center + dir1 * radius);
            Gizmos.DrawLine(center + dir1 * radius, center + dir2 * radius);
        }
    }
}
