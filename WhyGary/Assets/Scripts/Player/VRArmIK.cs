using UnityEngine;
using WhyGary.VRCore;

public class VRArmIK : MonoBehaviour
{
    [Header("References")]
    public PlayerBodyDriver bodyDriver;
    public Transform        controllerTransform;
    public VRPrimitiveArm   arm;

    [Header("Config")]
    public bool  isRightArm    = true;
    public float upperArmLength = 0.28f;
    public float forearmLength  = 0.24f;

    [Tooltip("Body-local direction the elbow bends toward. Right arm default: elbow down-back-inward.")]
    [SerializeField] Vector3    elbowHintLocal      = new Vector3(-0.3f, -0.8f, -0.4f);

    [Tooltip("Rotates the hand model relative to the physical controller so grip looks natural.")]
    [SerializeField] Quaternion wristRotationOffset = Quaternion.identity;

    void LateUpdate()
    {
        if (bodyDriver == null || controllerTransform == null || arm == null) return;

        Vector3 shoulder    = isRightArm ? bodyDriver.RightShoulderWorld : bodyDriver.LeftShoulderWorld;
        Vector3 wristTarget = controllerTransform.position;

        Vector3 hintWorld = bodyDriver.bodyRoot.TransformDirection(elbowHintLocal.normalized);

        TwoBoneIKSolver.Result ik = TwoBoneIKSolver.Solve(
            shoulder, wristTarget, upperArmLength, forearmLength, hintWorld);

        PlaceCapsuleBetween(arm.UpperArmTf, shoulder,          ik.elbowPosition);
        arm.ElbowTf.position = ik.elbowPosition;
        PlaceCapsuleBetween(arm.ForearmTf,  ik.elbowPosition, wristTarget);

        Vector3 forearmDir = (wristTarget - ik.elbowPosition).normalized;
        arm.CuffTf.position = wristTarget - forearmDir * 0.01f;
        if (forearmDir.sqrMagnitude > 0.0001f)
            arm.CuffTf.rotation = Quaternion.FromToRotation(Vector3.up, forearmDir);

        if (arm.HandRootTf != null)
        {
            arm.HandRootTf.position = wristTarget;
            arm.HandRootTf.rotation = controllerTransform.rotation * wristRotationOffset;
        }
    }

    static void PlaceCapsuleBetween(Transform capsule, Vector3 from, Vector3 to)
    {
        capsule.position = (from + to) * 0.5f;
        Vector3 dir = to - from;
        if (dir.sqrMagnitude > 0.0001f)
            capsule.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
    }
}
