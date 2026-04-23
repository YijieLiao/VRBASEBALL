using UnityEngine;

public class BillboardFaceCamera : MonoBehaviour
{
    public Camera targetCamera;
    public bool onlyYaw = true;

    void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        Vector3 direction = transform.position - targetCamera.transform.position;

        if (onlyYaw)
            direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }
}
