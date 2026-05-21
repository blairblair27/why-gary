using UnityEngine;

namespace WhyGary.VRCore
{
    public static class TwoBoneIKSolver
    {
        public struct Result
        {
            public Vector3 elbowPosition;
        }

        /// <summary>
        /// Solves elbow position for a two-bone arm IK chain.
        /// </summary>
        /// <param name="shoulder">Root joint world position.</param>
        /// <param name="wristTarget">Desired wrist world position (will be clamped to reach limit).</param>
        /// <param name="upperArmLength">Shoulder-to-elbow bone length in metres.</param>
        /// <param name="forearmLength">Elbow-to-wrist bone length in metres.</param>
        /// <param name="elbowHintDir">World-space direction vector biasing elbow placement. The elbow bends toward this direction relative to the shoulder→wrist axis.</param>
        /// <param name="stretchFactor">Max reach as a fraction of total arm length before clamping (default 1.15 = 15% overshoot).</param>
        public static Result Solve(
            Vector3 shoulder,
            Vector3 wristTarget,
            float upperArmLength,
            float forearmLength,
            Vector3 elbowHintDir,
            float stretchFactor = 1.15f)
        {
            upperArmLength = Mathf.Max(upperArmLength, 0.001f);
            forearmLength  = Mathf.Max(forearmLength,  0.001f);

            float maxReach = (upperArmLength + forearmLength) * stretchFactor;
            Vector3 toWrist = wristTarget - shoulder;
            float toWristMag = toWrist.magnitude;
            if (toWristMag > maxReach)
                wristTarget = shoulder + (toWrist / toWristMag) * maxReach;

            float d = Mathf.Clamp(Vector3.Distance(shoulder, wristTarget),
                Mathf.Abs(upperArmLength - forearmLength) + 0.001f,
                upperArmLength + forearmLength - 0.001f);

            float cosA = (upperArmLength * upperArmLength + d * d - forearmLength * forearmLength)
                         / (2f * upperArmLength * d);
            cosA = Mathf.Clamp(cosA, -1f, 1f);
            float angleA = Mathf.Acos(cosA) * Mathf.Rad2Deg;

            Vector3 swDir = (wristTarget - shoulder).normalized;

            Vector3 hintPerp = elbowHintDir - Vector3.Dot(elbowHintDir, swDir) * swDir;
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
}
