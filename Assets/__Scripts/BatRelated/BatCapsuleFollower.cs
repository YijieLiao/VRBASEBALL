using UnityEngine;

public class BatCapsuleFollower : MonoBehaviour
{
    private BatCapsule _batFollower; // 这里的变量名在图中为 _batFollower，实际指代目标
    private Rigidbody _rigidbody;
    private Vector3 _velocity;

    [SerializeField] private float _sensitivity = 100f;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // 获取目标位置
        Vector3 destination = _batFollower.transform.position;

        // 同步旋转
        _rigidbody.transform.rotation = transform.rotation;

        // 计算到达目标所需的物理速度：(目标位置 - 当前位置) * 灵敏度
        _velocity = (destination - _rigidbody.transform.position) * _sensitivity;

        // 应用物理速度
        _rigidbody.velocity = _velocity;

        // 再次同步旋转以确保对齐
        transform.rotation = _batFollower.transform.rotation;
    }

    public void SetFollowTarget(BatCapsule batFollower)
    {
        _batFollower = batFollower;
    }
}