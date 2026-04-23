using UnityEngine;

public class GuideTeleportArrivalTrigger : MonoBehaviour
{
    public enum TriggerTarget
    {
        TeleportPointOne,
        TeleportPointTwo,
        TeleportTablePoint
    }

    public IntroSequenceController controller;
    public TriggerTarget triggerTarget;
    public Transform playerCamera;
    public float triggerDistance = 0.75f;
    public bool triggerOnce = true;

    private bool hasTriggered;

    void Update()
    {
        if (hasTriggered && triggerOnce)
            return;

        if (controller == null)
            return;

        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        if (playerCamera == null)
            return;

        float distance = Vector3.Distance(playerCamera.position, transform.position);
        if (distance > triggerDistance)
            return;

        hasTriggered = true;

        switch (triggerTarget)
        {
            case TriggerTarget.TeleportPointOne:
                controller.ReachTeleportPointOne();
                break;
            case TriggerTarget.TeleportPointTwo:
                controller.ReachTeleportPointTwo();
                break;
            case TriggerTarget.TeleportTablePoint:
                controller.ReachTeleportTablePoint();
                break;
        }
    }
}
