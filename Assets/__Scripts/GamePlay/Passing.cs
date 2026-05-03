using System.Linq;
using Unity.XR.PXR.Debugger;
using UnityEngine;

public class Passing : MonoBehaviour
{
    private Passing[] allOtherPlayers;
    private Ball ball;
    private float passForce = 5000f;

    private void Awake()
    {
        allOtherPlayers = FindObjectsOfType<Passing>().Where(t => t != this).ToArray();
        ball = FindObjectOfType<Ball>();

        // 打印找到的玩家数量和名字
        Debug.Log("我是：" + gameObject.name + "，找到的队友数量：" + allOtherPlayers.Length);
        foreach (var p in allOtherPlayers)
        {
            Debug.Log("队友：" + p.gameObject.name);
        }
    }


    private void Update()
    {
        if (IHaveBall())
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            Vector3 direction = new Vector3(horizontal, 0f, vertical);

            // 射线起点改成球的位置，而不是玩家的位置
            if (ball != null)
            {
                Debug.DrawRay(ball.transform.position, direction * 10f, Color.red);
            }

            var targetPlayer = FindPlayerInDirection(direction);
            UpdateRenderers(targetPlayer);

            if (targetPlayer != null && Input.GetButtonDown("Fire1"))
            {
                PassBallToPlayer(targetPlayer);
            }
        }
    }

    private void PassBallToPlayer(Passing targetPlayer)
    {
        if (ball == null) return;

        ball.transform.SetParent(null);         // 先解除父子关系
        ball.GetComponent<Rigidbody>().isKinematic = false; // 再关闭 Kinematic
        var direction = DirectionTo(targetPlayer);
        ball.GetComponent<Rigidbody>().AddForce(direction * passForce);
    }

    // 更新队友的颜色，高亮即将传球的目标
    private void UpdateRenderers(Passing targetPlayer)
    {
        // 先把所有玩家变回白色
        foreach (var p in allOtherPlayers)
        {
            if (p != null && p.GetComponent<Renderer>() != null)
            {
                p.GetComponent<Renderer>().material.color = Color.white;
            }
        }

        // 只在 targetPlayer 不为空时，才高亮目标玩家
        if (targetPlayer != null && targetPlayer.GetComponent<Renderer>() != null)
        {
            targetPlayer.GetComponent<Renderer>().material.color = Color.green;
        }
    }
    // 计算当前玩家到目标队友的方向向量（单位向量）
    private Vector3 DirectionTo(Passing player)
    {
        return Vector3.Normalize(player.transform.position - ball.transform.position);
    }

    private Passing FindPlayerInDirection(Vector3 direction)
    {
        // 如果没有输入方向，直接返回 null，不做匹配
        if (direction.magnitude < 0.1f)
            return null;

        var closestAngle = allOtherPlayers
            .OrderBy(t => Vector3.Angle(direction, DirectionTo(t)))
            .FirstOrDefault();

        return closestAngle;
    }
    // 判断当前玩家是否持球（通过判断球是否是自己的子物体）
    private bool IHaveBall()
    {
        return transform.childCount > 0;
    }


    private void OnTriggerEnter(Collider other)
    {
        Ball ball = other.GetComponent<Ball>();
        if (ball != null)
        {
            Debug.Log(gameObject.name + " 接到球，准备设置父物体");
            ball.GetComponent<Rigidbody>().velocity = Vector3.zero;
            ball.GetComponent<Rigidbody>().isKinematic = true;
            ball.transform.SetParent(transform);
            Debug.Log("父物体设置完成，当前子物体数：" + transform.childCount);
        }
    }


}
