using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MicroPitcher : MonoBehaviour
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

    private Rigidbody rb;
    private bool isPitching = false;
    private Vector3 customGravity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        // 建议：防止穿模的必备设置
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

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
            // 持续施加微缩重力
            rb.AddForce(customGravity, ForceMode.Acceleration);
        }
    }

    // --- 核心改动：碰撞检测 ---
    private void OnCollisionEnter(Collision collision)
    {
        if (isPitching)
        {
            // 落地瞬间大幅增加阻力
            // 这不会影响之前的飞行轨迹，因为此时球已经撞击目标了
            rb.drag = groundDrag;
            rb.angularDrag = groundAngularDrag;

            // 可选：如果不希望落地后继续受自定义重力干扰，可以关闭
            // isPitching = false; 
        }
    }

    public void ResetBall()
    {
        if (launchPoint == null) return;

        isPitching = false;

        // 重置物理参数，确保下次飞行依然精准
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.drag = 0f;          // 飞行时阻力必须为0
        rb.angularDrag = 0.05f; // 恢复默认旋转阻力

        transform.position = launchPoint.position;
        transform.rotation = launchPoint.rotation;
    }

    private void PitchBall()
    {
        if (launchPoint == null || targetPoint == null) return;

        transform.position = launchPoint.position;

        // 发射前清空阻力，保证初速度不受干扰
        rb.isKinematic = false;
        rb.drag = 0f;
        rb.angularDrag = 0.05f;

        isPitching = true;
        customGravity = new Vector3(0, -9.81f * microGravityScale, 0);

        Vector3 worldDisplacement = targetPoint.position - launchPoint.position;
        Vector3 velocityXZ = new Vector3(worldDisplacement.x, 0, worldDisplacement.z) / flightTime;

        float dy = worldDisplacement.y;
        float g = customGravity.y;
        float velocityY_val = (dy / flightTime) - (0.5f * g * flightTime);
        Vector3 velocityY = new Vector3(0, velocityY_val, 0);

        rb.velocity = velocityXZ + velocityY;
    }
}