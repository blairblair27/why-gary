using UnityEngine;

public class PlayerBodyDriver : MonoBehaviour
{
    [Header("XR Sources")]
    public Transform hmdTransform;
    public Transform leftControllerTransform;
    public Transform rightControllerTransform;

    [Header("Body Parts")]
    public Transform bodyRoot;
    public Transform headTarget;
    public Transform torsoTarget;
    public Transform leftHandTarget;
    public Transform rightHandTarget;

    [Header("Settings")]
    public float standingHeight = 1.65f;
    public float bodyFollowSpeed = 15f;
    public float bodyYawFollowSpeed = 3f;
    public float headHeightOffset = -0.1f;

    void LateUpdate()
    {
        if (hmdTransform == null) return;

        // Head follows HMD, only Y rotation so it doesn't tilt weirdly
        headTarget.position = hmdTransform.position + Vector3.up * headHeightOffset;
        float headYaw = hmdTransform.eulerAngles.y;
        headTarget.rotation = Quaternion.Euler(0, headYaw, 0);

        // Body root infers standing position from HMD height
        Vector3 targetBodyPos = new Vector3(
            hmdTransform.position.x,
            hmdTransform.position.y - standingHeight,
            hmdTransform.position.z
        );
        bodyRoot.position = Vector3.Lerp(bodyRoot.position, targetBodyPos, Time.deltaTime * bodyFollowSpeed);
        bodyRoot.rotation = Quaternion.Lerp(
            bodyRoot.rotation,
            Quaternion.Euler(0, headYaw, 0),
            Time.deltaTime * bodyYawFollowSpeed
        );

        // Torso sits at shoulder height above body root
        torsoTarget.position = bodyRoot.position + Vector3.up * 1.2f;
        torsoTarget.rotation = bodyRoot.rotation;

        // Hands directly copy controller pose — no lerp so there's zero lag
        leftHandTarget.position = leftControllerTransform.position;
        leftHandTarget.rotation = leftControllerTransform.rotation;
        rightHandTarget.position = rightControllerTransform.position;
        rightHandTarget.rotation = rightControllerTransform.rotation;
    }
}
