using UnityEngine;

public class VelocityDebugger : MonoBehaviour
{
    [SerializeField] private float maxVelocity = 20f;

    private void Update()
    {
        // 动态根据速度改变材质颜色
        GetComponent<Renderer>().material.color = ColorForVelocity();
    }

    private Color ColorForVelocity()
    {
        // 获取刚体的速度大小
        float velocity = GetComponent<Rigidbody>().velocity.magnitude;
        // 在绿色（慢）和红色（快）之间插值
        return Color.Lerp(Color.green, Color.red, velocity / maxVelocity);
    }
}