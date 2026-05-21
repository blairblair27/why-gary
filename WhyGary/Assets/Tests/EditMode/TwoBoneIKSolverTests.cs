using NUnit.Framework;
using UnityEngine;
using WhyGary.VRCore;

public class TwoBoneIKSolverTests
{
    const float upperArm = 0.28f;
    const float forearm  = 0.24f;

    [Test]
    public void Solve_ArmHangsDown_ElbowBehindMidpoint()
    {
        var result = TwoBoneIKSolver.Solve(
            shoulder:       Vector3.zero,
            wristTarget:    new Vector3(0f, -0.4f, 0f),
            upperArmLength: upperArm,
            forearmLength:  forearm,
            elbowHintDir:   Vector3.back
        );

        Assert.Less(result.elbowPosition.z, 0f,
            "Elbow should be behind the shoulder→wrist line when hint is Vector3.back");
    }

    [Test]
    public void Solve_ElbowDistanceFromShoulder_EqualsUpperArmLength()
    {
        var result = TwoBoneIKSolver.Solve(
            shoulder:       Vector3.zero,
            wristTarget:    new Vector3(0.3f, -0.3f, 0f),
            upperArmLength: upperArm,
            forearmLength:  forearm,
            elbowHintDir:   Vector3.back
        );

        float dist = Vector3.Distance(Vector3.zero, result.elbowPosition);
        Assert.AreEqual(upperArm, dist, 0.001f,
            "Elbow must always be exactly upperArmLength from shoulder");
    }

    [Test]
    public void Solve_WristBeyondMaxReach_ElbowIsStillUpperArmFromShoulder()
    {
        var result = TwoBoneIKSolver.Solve(
            shoulder:       Vector3.zero,
            wristTarget:    new Vector3(0f, -2f, 0f),
            upperArmLength: upperArm,
            forearmLength:  forearm,
            elbowHintDir:   Vector3.back
        );

        float dist = Vector3.Distance(Vector3.zero, result.elbowPosition);
        Assert.AreEqual(upperArm, dist, 0.001f,
            "Elbow distance from shoulder must remain correct when wrist is clamped");
    }

    [Test]
    public void Solve_HintDirectionOpposite_ElbowBendsOpposite()
    {
        var shoulder = Vector3.zero;
        var wrist    = new Vector3(0f, -0.4f, 0f);

        var backResult    = TwoBoneIKSolver.Solve(shoulder, wrist, upperArm, forearm, Vector3.back);
        var forwardResult = TwoBoneIKSolver.Solve(shoulder, wrist, upperArm, forearm, Vector3.forward);

        Assert.Less(backResult.elbowPosition.z, 0f,    "back hint → elbow behind midline");
        Assert.Greater(forwardResult.elbowPosition.z, 0f, "forward hint → elbow in front of midline");
    }
}
