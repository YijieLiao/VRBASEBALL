using UnityEngine;

public class BatCapsule : MonoBehaviour
{
    [SerializeField] private BatCapsuleFollower _batCapsuleFollowerPrefab;

    private void Start()
    {
        SpawnBatCapsuleFollower();
    }

    private void SpawnBatCapsuleFollower()
    {
        // 实例化跟随者并设置初始位置和目标
        var follower = Instantiate(_batCapsuleFollowerPrefab);
        follower.transform.position = transform.position;
        follower.SetFollowTarget(this);
    }
}