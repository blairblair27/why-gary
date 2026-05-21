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

    [Header("Body Settings")]
    public float standingHeight    = 1.65f;
    public float bodyFollowSpeed   = 15f;
    public float bodyYawFollowSpeed = 3f;
    public float headHeightOffset  = -0.1f;
    [SerializeField] float torsoHeightOffset = 1.2f;
    [SerializeField] float maxHeadBodyAngle  = 60f;

    [Header("Shoulder Offsets (body-local)")]
    [SerializeField] Vector3 leftShoulderOffset  = new Vector3(-0.18f,  0.14f, 0.04f);
    [SerializeField] Vector3 rightShoulderOffset = new Vector3( 0.18f,  0.14f, 0.04f);

    public Vector3 LeftShoulderWorld  { get; private set; }
    public Vector3 RightShoulderWorld { get; private set; }

    float _headYaw;

    void Awake()
    {
        Debug.Assert(bodyRoot        != null, "[PlayerBodyDriver] bodyRoot is not assigned.",        this);
        Debug.Assert(headTarget      != null, "[PlayerBodyDriver] headTarget is not assigned.",      this);
        Debug.Assert(torsoTarget     != null, "[PlayerBodyDriver] torsoTarget is not assigned.",     this);
        Debug.Assert(leftHandTarget  != null, "[PlayerBodyDriver] leftHandTarget is not assigned.",  this);
        Debug.Assert(rightHandTarget != null, "[PlayerBodyDriver] rightHandTarget is not assigned.", this);
    }

    void Update()
    {
        if (hmdTransform == null) return;

        Vector3 fwd = Vector3.ProjectOnPlane(hmdTransform.forward, Vector3.up);
        _headYaw = fwd.sqrMagnitude > 0.001f ? Quaternion.LookRotation(fwd).eulerAngles.y : 0f;

        float angleDiff  = Mathf.Abs(Mathf.DeltaAngle(bodyRoot.eulerAngles.y, _headYaw));
        float followSpeed = angleDiff > maxHeadBodyAngle ? bodyYawFollowSpeed * 5f : bodyYawFollowSpeed;

        Vector3 targetPos = new Vector3(
            hmdTransform.position.x,
            hmdTransform.position.y - standingHeight,
            hmdTransform.position.z
        );

        bodyRoot.position = Vector3.Lerp(bodyRoot.position, targetPos, Time.deltaTime * bodyFollowSpeed);
        bodyRoot.rotation = Quaternion.Lerp(
            bodyRoot.rotation,
            Quaternion.Euler(0f, _headYaw, 0f),
            Time.deltaTime * followSpeed);

        if (torsoTarget != null)
        {
            torsoTarget.position = bodyRoot.position + Vector3.up * torsoHeightOffset;
            torsoTarget.rotation = bodyRoot.rotation;
        }

        LeftShoulderWorld  = bodyRoot.TransformPoint(leftShoulderOffset);
        RightShoulderWorld = bodyRoot.TransformPoint(rightShoulderOffset);
    }

    void LateUpdate()
    {
        if (hmdTransform == null) return;

        if (headTarget != null)
        {
            headTarget.position = hmdTransform.position + Vector3.up * headHeightOffset;
            headTarget.rotation = hmdTransform.rotation;
        }

        if (leftControllerTransform != null && leftHandTarget != null)
        {
            leftHandTarget.position = leftControllerTransform.position;
            leftHandTarget.rotation = leftControllerTransform.rotation;
        }

        if (rightControllerTransform != null && rightHandTarget != null)
        {
            rightHandTarget.position = rightControllerTransform.position;
            rightHandTarget.rotation = rightControllerTransform.rotation;
        }
    }
}
