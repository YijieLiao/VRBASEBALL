using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MicroPitcher : MonoBehaviour
{
    [Header("发球与落点坐标 (本地坐标)")]
    public Vector3 launchPosition = new Vector3(0.28f, 0.65f, -0.057f);
    public Vector3 targetPosition = new Vector3(0.5f, 0.65f, -0.057f);

    [Header("微缩物理调控")]
    public float flightTime = 0.5f;
    public float microGravityScale = 0.1f;

    private Rigidbody rb;
    private bool isPitching = false;
    private Vector3 customGravity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        ResetBall();
    }

    void Update()
    {
        // 测试按键
        if (Input.GetKeyDown(KeyCode.A))
        {
            ResetBall();
        }
        else if (Input.GetKeyDown(KeyCode.Space) && !isPitching)
        {
            PitchBall();
        }

        // 【安全网】如果球掉下去了（比如 Y 小于 0），自动重置，防止掉到 -79
        if (transform.localPosition.y < 0f)
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

    public void ResetBall()
    {
        isPitching = false;

        // 先清空速度（此时 isKinematic 还是 false，所以不会报错）
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 然后再开启动力学模式，锁死物理模拟
        rb.isKinematic = true;

        // 最后回到起始点
        transform.localPosition = launchPosition;
    }
    private void PitchBall()
    {
        transform.localPosition = launchPosition;
        rb.isKinematic = false;
        isPitching = true;

        // 设置向下的微缩重力
        customGravity = new Vector3(0, -9.81f * microGravityScale, 0);

        // 1. 获取纯本地坐标下的位移距离
        Vector3 localDisplacement = targetPosition - launchPosition;

        // 2. 计算本地 XZ 平面的水平速度
        Vector3 localVelocityXZ = new Vector3(localDisplacement.x, 0, localDisplacement.z) / flightTime;

        // 3. 计算本地 Y 轴的抛物线初始速度
        float initialVelocityY = (localDisplacement.y / flightTime) - (0.5f * customGravity.y * flightTime);
        Vector3 localVelocityY = new Vector3(0, initialVelocityY, 0);

        // 4. 将本地速度方向转换为世界方向，赋予刚体
        // TransformDirection 不受父物体缩放(Scale)的影响，保证速度计算绝对精确，不再出现 X 过大的问题
        if (transform.parent != null)
        {
            rb.velocity = transform.parent.TransformDirection(localVelocityXZ + localVelocityY);
        }
        else
        {
            rb.velocity = localVelocityXZ + localVelocityY;
        }
    }
}