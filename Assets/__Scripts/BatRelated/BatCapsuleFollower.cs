using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class BatCapsuleFollower : MonoBehaviour
{
    [Header("Position Follow")]
    [SerializeField] private float positionFollowGain = 45f;
    [SerializeField] private float maxLinearSpeed = 12f;
    [SerializeField] private float positionDeadZone = 0.01f;
    [SerializeField] private float teleportDistance = 0.35f;

    [Header("Rotation Follow")]
    [SerializeField] private float rotationFollowGain = 25f;
    [SerializeField] private float maxAngularSpeed = 30f;
    [SerializeField] private float rotationDeadZoneDegrees = 2f;
    [SerializeField] private float snapRotationDegrees = 50f;

    private BatCapsule _followTarget;
    private Rigidbody _rigidbody;

    public Vector3 CurrentVelocity { get; private set; }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            Debug.LogError($"{nameof(BatCapsuleFollower)} on {name} requires a Rigidbody.", this);
            enabled = false;
            return;
        }

        _rigidbody.useGravity = false;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void FixedUpdate()
    {
        if (_rigidbody == null)
            return;

        if (_followTarget == null)
        {
            StopMotion();
            return;
        }

        FollowPosition();
        FollowRotation();
    }

    public void SetFollowTarget(BatCapsule followTarget)
    {
        _followTarget = followTarget;
        if (_followTarget == null || _rigidbody == null)
            return;

        _rigidbody.position = _followTarget.transform.position;
        _rigidbody.rotation = _followTarget.transform.rotation;
        StopMotion();
    }

    private void FollowPosition()
    {
        Vector3 positionDelta = _followTarget.transform.position - _rigidbody.position;
        float distance = positionDelta.magnitude;

        if (distance >= teleportDistance)
        {
            _rigidbody.position = _followTarget.transform.position;
            CurrentVelocity = Vector3.zero;
            _rigidbody.velocity = CurrentVelocity;
            return;
        }

        if (distance <= positionDeadZone)
        {
            CurrentVelocity = Vector3.zero;
            _rigidbody.velocity = CurrentVelocity;
            return;
        }

        Vector3 desiredVelocity = positionDelta * positionFollowGain;
        CurrentVelocity = Vector3.ClampMagnitude(desiredVelocity, maxLinearSpeed);
        _rigidbody.velocity = CurrentVelocity;
    }

    private void FollowRotation()
    {
        Quaternion targetRotation = _followTarget.transform.rotation;
        Quaternion rotationDelta = targetRotation * Quaternion.Inverse(_rigidbody.rotation);
        rotationDelta.ToAngleAxis(out float angleDegrees, out Vector3 axis);

        if (float.IsNaN(axis.x) || axis.sqrMagnitude < 0.0001f)
        {
            _rigidbody.angularVelocity = Vector3.zero;
            return;
        }

        if (angleDegrees > 180f)
            angleDegrees -= 360f;

        float absAngle = Mathf.Abs(angleDegrees);
        if (absAngle >= snapRotationDegrees)
        {
            _rigidbody.rotation = targetRotation;
            _rigidbody.angularVelocity = Vector3.zero;
            return;
        }

        if (absAngle <= rotationDeadZoneDegrees)
        {
            _rigidbody.angularVelocity = Vector3.zero;
            return;
        }

        Vector3 angularVelocity = axis.normalized * (angleDegrees * Mathf.Deg2Rad * rotationFollowGain);
        _rigidbody.angularVelocity = Vector3.ClampMagnitude(angularVelocity, maxAngularSpeed);
    }

    private void StopMotion()
    {
        CurrentVelocity = Vector3.zero;
        _rigidbody.velocity = CurrentVelocity;
        _rigidbody.angularVelocity = Vector3.zero;
    }
}
