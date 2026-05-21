using UnityEngine;

public class VRHandAnimator : MonoBehaviour
{
    [Header("References")]
    public VRPrimitiveArm  arm;
    public VRFingerTracker fingerTracker;

    [Header("Joint Rotation Ranges (degrees around local X)")]
    [SerializeField] float proximalMax = 70f;
    [SerializeField] float middleMax   = 80f;
    [SerializeField] float distalMax   = 60f;

    readonly float[] _currentCurl = new float[5];

    VRHandPose _overridePose;
    float      _overrideWeight;
    float      _overrideBlendTime = 0.001f;
    bool       _overrideFadingIn;

    public void SetOverridePose(VRHandPose pose, float blendTime)
    {
        _overridePose      = pose;
        _overrideBlendTime = blendTime > 0f ? blendTime : 0.001f;
        _overrideFadingIn  = true;
    }

    public void ClearOverridePose(float blendTime)
    {
        _overrideBlendTime = blendTime > 0f ? blendTime : 0.001f;
        _overrideFadingIn  = false;
    }

    void LateUpdate()
    {
        if (arm == null || arm.FingerBones == null) return;

        float blendDelta = Time.deltaTime / _overrideBlendTime;
        _overrideWeight = _overrideFadingIn
            ? Mathf.MoveTowards(_overrideWeight, 1f, blendDelta)
            : Mathf.MoveTowards(_overrideWeight, 0f, blendDelta);

        float liveCurl0 = fingerTracker != null ? fingerTracker.State.thumb  : 0f;
        float liveCurl1 = fingerTracker != null ? fingerTracker.State.index  : 0f;
        float liveCurl2 = fingerTracker != null ? fingerTracker.State.middle : 0f;
        float liveCurl3 = fingerTracker != null ? fingerTracker.State.ring   : 0f;
        float liveCurl4 = fingerTracker != null ? fingerTracker.State.pinky  : 0f;

        if (_overridePose != null && _overrideWeight > 0f)
        {
            float w = _overrideWeight;
            liveCurl0 = Mathf.Lerp(liveCurl0, _overridePose.thumb,  w);
            liveCurl1 = Mathf.Lerp(liveCurl1, _overridePose.index,  w);
            liveCurl2 = Mathf.Lerp(liveCurl2, _overridePose.middle, w);
            liveCurl3 = Mathf.Lerp(liveCurl3, _overridePose.ring,   w);
            liveCurl4 = Mathf.Lerp(liveCurl4, _overridePose.pinky,  w);
        }

        float smooth = Time.deltaTime * 18f;
        _currentCurl[0] = Mathf.Lerp(_currentCurl[0], liveCurl0, smooth);
        _currentCurl[1] = Mathf.Lerp(_currentCurl[1], liveCurl1, smooth);
        _currentCurl[2] = Mathf.Lerp(_currentCurl[2], liveCurl2, smooth);
        _currentCurl[3] = Mathf.Lerp(_currentCurl[3], liveCurl3, smooth);
        _currentCurl[4] = Mathf.Lerp(_currentCurl[4], liveCurl4, smooth);

        for (int f = 0; f < 5; f++)
        {
            float curl = _currentCurl[f];
            SetJointRotation(arm.FingerBones[f, 0], curl, proximalMax);
            SetJointRotation(arm.FingerBones[f, 1], curl, middleMax);
            SetJointRotation(arm.FingerBones[f, 2], curl, distalMax);
        }
    }

    static void SetJointRotation(Transform joint, float curl, float maxAngle)
    {
        if (joint == null) return;
        joint.localRotation = Quaternion.Euler(curl * maxAngle, 0f, 0f);
    }
}
