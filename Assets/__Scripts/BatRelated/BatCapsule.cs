using UnityEngine;

[DisallowMultipleComponent]
public class BatCapsule : MonoBehaviour
{
    [SerializeField] private BatCapsuleFollower _batCapsuleFollowerPrefab;

    private BatCapsuleFollower _spawnedFollower;

    private void Start()
    {
        SpawnBatCapsuleFollower();
        ApplyFollowerActiveState();
    }

    private void OnEnable()
    {
        ApplyFollowerActiveState();
    }

    private void OnDisable()
    {
        ApplyFollowerActiveState();
    }

    private void SpawnBatCapsuleFollower()
    {
        if (_spawnedFollower != null)
            return;

        if (_batCapsuleFollowerPrefab == null)
        {
            Debug.LogError($"{nameof(BatCapsule)} on {name} is missing {nameof(_batCapsuleFollowerPrefab)}.", this);
            return;
        }

        _spawnedFollower = Instantiate(_batCapsuleFollowerPrefab, transform.position, transform.rotation);
        _spawnedFollower.SetFollowTarget(this);
    }

    private void ApplyFollowerActiveState()
    {
        if (_spawnedFollower != null)
            _spawnedFollower.gameObject.SetActive(isActiveAndEnabled);
    }
}