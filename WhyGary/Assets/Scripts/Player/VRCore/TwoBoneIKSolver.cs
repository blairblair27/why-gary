using UnityEngine;

public static class TwoBoneIKSolver
{
    public struct Result
    {
        public Vector3 elbowPosition;
    }

    public static Result Solve(
        Vector3 shoulder,
        Vector3 wristTarget,
        float upperArmLength,
        float forearmLength,
        Vector3 elbowHintWorld,
        float stretchFactor = 1.15f)
    {
        float maxReach = (upperArmLength + forearmLength) * stretchFactor;
        Vector3 toWrist = wristTarget - shoulder;
        if (toWrist.magnitude > maxReach)
            wristTarget = shoulder + toWrist.normalized * maxReach;

        float d = Mathf.Clamp(Vector3.Distance(shoulder, wristTarget),
            Mathf.Abs(upperArmLength - forearmLength) + 0.001f,
            upperArmLength + forearmLength - 0.001f);

        float cosA = (upperArmLength * upperArmLength + d * d - forearmLength * forearmLength)
                     / (2f * upperArmLength * d);
        cosA = Mathf.Clamp(cosA, -1f, 1f);
        float angleA = Mathf.Acos(cosA) * Mathf.Rad2Deg;

        Vector3 swDir = (wristTarget - shoulder).normalized;

        Vector3 hintPerp = elbowHintWorld - Vector3.Dot(elbowHintWorld, swDir) * swDir;
        if (hintPerp.sqrMagnitude < 0.001f)
        {
            hintPerp = Vector3.Cross(swDir, Vector3.up);
            if (hintPerp.sqrMagnitude < 0.001f)
                hintPerp = Vector3.Cross(swDir, Vector3.forward);
        }
        hintPerp = hintPerp.normalized;

        Vector3 rotAxis = Vector3.Cross(swDir, hintPerp).normalized;
        Vector3 elbowDir = Quaternion.AngleAxis(angleA, rotAxis) * swDir;

        return new Result { elbowPosition = shoulder + elbowDir * upperArmLength };
    }
}
