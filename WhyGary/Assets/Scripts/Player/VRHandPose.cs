using UnityEngine;

[CreateAssetMenu(fileName = "HandPose", menuName = "WhyGary/Hand Pose")]
public class VRHandPose : ScriptableObject
{
    [Range(0f, 1f)] public float thumb  = 0.1f;
    [Range(0f, 1f)] public float index  = 0.05f;
    [Range(0f, 1f)] public float middle = 0.15f;
    [Range(0f, 1f)] public float ring   = 0.15f;
    [Range(0f, 1f)] public float pinky  = 0.2f;
}
