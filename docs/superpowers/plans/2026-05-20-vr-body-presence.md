# VR Body Presence Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace floating controller hands with Onward-style first-person arms — low-poly Rec Room primitive geometry, custom two-bone IK, Valve Index finger tracking, and a hip holster draw system for the M1911 pistol.

**Architecture:** Primitive GameObjects (capsules/spheres) assembled and repositioned each frame by a custom two-bone IK solver. No Animation Rigging package. Valve Index finger curl driven through OpenXR's `InputDevice` API (grip/trigger/touch floats). Hip holster integrates with XRIT's existing `XRGrabInteractable`.

**Tech Stack:** Unity 6 LTS, URP, XR Interaction Toolkit 3.4.1, OpenXR, Valve Index

---

## File Map

| File | Action | Responsibility |
|------|--------|----------------|
| `Assets/Scripts/Player/VRCore/TwoBoneIKSolver.cs` | Create | Pure-math static IK solver (no MonoBehaviour) |
| `Assets/Scripts/Player/VRCore/WhyGary.VRCore.asmdef` | Create | Assembly def so IK solver is unit-testable |
| `Assets/Tests/EditMode/TwoBoneIKSolverTests.cs` | Create | Edit Mode unit tests for IK math |
| `Assets/Tests/EditMode/WhyGary.Tests.EditMode.asmdef` | Create | Test assembly def referencing VRCore |
| `Assets/Scripts/Player/VRHandPose.cs` | Create | ScriptableObject: 5 per-finger curl values |
| `Assets/Scripts/Player/PlayerBodyDriver.cs` | Modify | Add shoulder world properties + head-body clamping |
| `Assets/Scripts/Player/VRPrimitiveArm.cs` | Create | Creates arm/hand primitive geometry, exposes finger bone transforms |
| `Assets/Scripts/Player/VRArmIK.cs` | Create | Reads controller + shoulder, drives arm geometry each frame |
| `Assets/Scripts/Player/VRFingerTracker.cs` | Create | Reads OpenXR input, outputs `FingerState` struct |
| `Assets/Scripts/Player/VRHandAnimator.cs` | Create | Blends VRHandPose values onto finger bone transforms |
| `Assets/Scripts/Player/VRHolster.cs` | Create | Body-relative holster anchor, draw/reholster via XRIT events |
| `Assets/Scripts/Props/GunController.cs` | Modify | Add attach transform reference + selectEntered/Exited hand pose events |
| `Assets/HandPoses/HandPose_Idle.asset` | Create (in Editor) | Idle pose values |
| `Assets/HandPoses/HandPose_PistolGrip.asset` | Create (in Editor) | Pistol grip pose values |
| `Assets/HandPoses/HandPose_Trigger.asset` | Create (in Editor) | Trigger-pressed pose values |

All paths relative to `WhyGary/`.

---

## Task 1: TwoBoneIKSolver — pure math + assembly def

**Files:**
- Create: `Assets/Scripts/Player/VRCore/TwoBoneIKSolver.cs`
- Create: `Assets/Scripts/Player/VRCore/WhyGary.VRCore.asmdef`

- [ ] **Step 1: Create the VRCore assembly definition**

Create `WhyGary/Assets/Scripts/Player/VRCore/WhyGary.VRCore.asmdef`:

```json
{
    "name": "WhyGary.VRCore",
    "rootNamespace": "",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Create TwoBoneIKSolver**

Create `WhyGary/Assets/Scripts/Player/VRCore/TwoBoneIKSolver.cs`:

```csharp
using UnityEngine;

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
    /// <param name="wristTarget">Desired wrist world position.</param>
    /// <param name="upperArmLength">Shoulder-to-elbow bone length.</param>
    /// <param name="forearmLength">Elbow-to-wrist bone length.</param>
    /// <param name="elbowHintWorld">World-space vector biasing elbow direction.</param>
    /// <param name="stretchFactor">Max stretch as a fraction of total arm length (1.15 = 15% overshoot before locking).</param>
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

        // Law of cosines: angle at shoulder between shoulder→wrist and shoulder→elbow
        float cosA = (upperArmLength * upperArmLength + d * d - forearmLength * forearmLength)
                     / (2f * upperArmLength * d);
        cosA = Mathf.Clamp(cosA, -1f, 1f);
        float angleA = Mathf.Acos(cosA) * Mathf.Rad2Deg;

        Vector3 swDir = (wristTarget - shoulder).normalized;

        // Project hint perpendicular to shoulder→wrist to get bend-plane normal
        Vector3 hintPerp = elbowHintWorld - Vector3.Dot(elbowHintWorld, swDir) * swDir;
        if (hintPerp.sqrMagnitude < 0.001f)
        {
            hintPerp = Vector3.Cross(swDir, Vector3.up);
            if (hintPerp.sqrMagnitude < 0.001f)
                hintPerp = Vector3.Cross(swDir, Vector3.forward);
        }
        hintPerp = hintPerp.normalized;

        // Rotate swDir toward hintPerp by angleA — elbow bends toward hint
        Vector3 rotAxis = Vector3.Cross(swDir, hintPerp).normalized;
        Vector3 elbowDir = Quaternion.AngleAxis(angleA, rotAxis) * swDir;

        return new Result { elbowPosition = shoulder + elbowDir * upperArmLength };
    }
}
```

- [ ] **Step 3: Open Unity, let it compile. Verify no errors in Console.**

---

## Task 2: Edit Mode tests for TwoBoneIKSolver

**Files:**
- Create: `Assets/Tests/EditMode/WhyGary.Tests.EditMode.asmdef`
- Create: `Assets/Tests/EditMode/TwoBoneIKSolverTests.cs`

- [ ] **Step 1: Create test assembly definition**

Create `WhyGary/Assets/Tests/EditMode/WhyGary.Tests.EditMode.asmdef`:

```json
{
    "name": "WhyGary.Tests.EditMode",
    "rootNamespace": "",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "WhyGary.VRCore"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Create TwoBoneIKSolverTests**

Create `WhyGary/Assets/Tests/EditMode/TwoBoneIKSolverTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

public class TwoBoneIKSolverTests
{
    const float upperArm = 0.28f;
    const float forearm  = 0.24f;

    [Test]
    public void Solve_ArmHangsDown_ElbowBehindMidpoint()
    {
        // Wrist directly below shoulder, hint pushes elbow backward
        var result = TwoBoneIKSolver.Solve(
            shoulder:       Vector3.zero,
            wristTarget:    new Vector3(0f, -0.4f, 0f),
            upperArmLength: upperArm,
            forearmLength:  forearm,
            elbowHintWorld: Vector3.back
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
            elbowHintWorld: Vector3.back
        );

        float dist = Vector3.Distance(Vector3.zero, result.elbowPosition);
        Assert.AreEqual(upperArm, dist, 0.001f,
            "Elbow must always be exactly upperArmLength from shoulder");
    }

    [Test]
    public void Solve_WristBeyondMaxReach_ElbowIsStillUpperArmFromShoulder()
    {
        // Wrist 2m below shoulder — far past the 0.52m max reach
        var result = TwoBoneIKSolver.Solve(
            shoulder:       Vector3.zero,
            wristTarget:    new Vector3(0f, -2f, 0f),
            upperArmLength: upperArm,
            forearmLength:  forearm,
            elbowHintWorld: Vector3.back
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
```

- [ ] **Step 3: Open Unity. Go to Window → General → Test Runner → EditMode. Run all tests.**

Expected: 4 tests pass, green.

- [ ] **Step 4: Commit**

```bash
git add WhyGary/Assets/Scripts/Player/VRCore/ WhyGary/Assets/Tests/
git commit -m "feat: TwoBoneIKSolver with Edit Mode tests"
```

---

## Task 3: VRHandPose ScriptableObject

**Files:**
- Create: `Assets/Scripts/Player/VRHandPose.cs`

- [ ] **Step 1: Create VRHandPose**

Create `WhyGary/Assets/Scripts/Player/VRHandPose.cs`:

```csharp
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
```

- [ ] **Step 2: Open Unity, compile, verify no errors.**

- [ ] **Step 3: Create three HandPose assets in the Unity Editor**

In the Project window, right-click `Assets/HandPoses/` → Create → WhyGary → Hand Pose.

Create three assets with these exact values:

**HandPose_Idle** (`Assets/HandPoses/HandPose_Idle.asset`):
- thumb: 0.1, index: 0.05, middle: 0.15, ring: 0.15, pinky: 0.2

**HandPose_PistolGrip** (`Assets/HandPoses/HandPose_PistolGrip.asset`):
- thumb: 0.6, index: 0.0, middle: 0.85, ring: 0.9, pinky: 0.9

**HandPose_Trigger** (`Assets/HandPoses/HandPose_Trigger.asset`):
- thumb: 0.6, index: 0.85, middle: 0.85, ring: 0.9, pinky: 0.9

- [ ] **Step 4: Commit**

```bash
git add WhyGary/Assets/Scripts/Player/VRHandPose.cs WhyGary/Assets/HandPoses/
git commit -m "feat: VRHandPose ScriptableObject and three default pose assets"
```

---

## Task 4: Refactor PlayerBodyDriver

**Files:**
- Modify: `Assets/Scripts/Player/PlayerBodyDriver.cs`

- [ ] **Step 1: Replace PlayerBodyDriver.cs entirely**

`WhyGary/Assets/Scripts/Player/PlayerBodyDriver.cs`:

```csharp
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

    // Read by VRArmIK — single source of truth for shoulder positions
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

        // Aggressive catch-up when head has rotated more than maxHeadBodyAngle past body
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
```

- [ ] **Step 2: Open Unity, compile, verify no errors. Enter Play Mode briefly — body should still track HMD as before.**

- [ ] **Step 3: Commit**

```bash
git add WhyGary/Assets/Scripts/Player/PlayerBodyDriver.cs
git commit -m "feat: PlayerBodyDriver shoulder properties and head-body angle clamping"
```

---

## Task 5: VRPrimitiveArm — geometry creation

**Files:**
- Create: `Assets/Scripts/Player/VRPrimitiveArm.cs`

- [ ] **Step 1: Create VRPrimitiveArm**

Create `WhyGary/Assets/Scripts/Player/VRPrimitiveArm.cs`:

```csharp
using UnityEngine;

public class VRPrimitiveArm : MonoBehaviour
{
    [Header("Sleeve (jacket)")]
    [SerializeField] Color sleeveColor = new Color(0.08f, 0.08f, 0.12f);
    [SerializeField] float upperArmRadius = 0.07f;
    [SerializeField] float upperArmLength = 0.28f;
    [SerializeField] float elbowRadius    = 0.065f;
    [SerializeField] float forearmRadius  = 0.05f;
    [SerializeField] float forearmLength  = 0.24f;
    [SerializeField] float cuffRadius     = 0.068f;
    [SerializeField] float cuffLength     = 0.06f;

    [Header("Hand")]
    [SerializeField] Color handColor = new Color(0.75f, 0.65f, 0.55f);
    [SerializeField] float palmWidth  = 0.075f;
    [SerializeField] float palmHeight = 0.04f;
    [SerializeField] float palmDepth  = 0.09f;

    [Header("Fingers")]
    [SerializeField] float fingerRadius    = 0.009f;
    [SerializeField] float proximalLength  = 0.032f;
    [SerializeField] float middleLength    = 0.025f;
    [SerializeField] float distalLength    = 0.018f;

    // Arm segment transforms — positioned by VRArmIK each frame
    public Transform UpperArmTf { get; private set; }
    public Transform ElbowTf    { get; private set; }
    public Transform ForearmTf  { get; private set; }
    public Transform CuffTf     { get; private set; }
    public Transform HandRootTf { get; private set; }

    // fingerBones[finger][joint]: finger 0=thumb … 4=pinky, joint 0=proximal 1=middle 2=distal
    public Transform[,] FingerBones { get; private set; }

    // Finger root pivots in palm-local space (set by VRPrimitiveArm, read by VRArmIK to place correctly)
    static readonly Vector3[] FingerPalmOffsets = new Vector3[]
    {
        new Vector3(-0.033f,  0f,  0.035f), // thumb
        new Vector3(-0.018f,  0f,  0.045f), // index
        new Vector3(-0.006f,  0f,  0.048f), // middle
        new Vector3( 0.006f,  0f,  0.045f), // ring
        new Vector3( 0.018f,  0f,  0.040f), // pinky
    };

    Material _sleeveMat;
    Material _handMat;

    void Awake()
    {
        _sleeveMat = CreateMat(sleeveColor);
        _handMat   = CreateMat(handColor);

        UpperArmTf = CreateCapsule("UpperArm", upperArmRadius, upperArmLength, _sleeveMat);
        ElbowTf    = CreateSphere("Elbow",     elbowRadius,                    _sleeveMat);
        ForearmTf  = CreateCapsule("Forearm",  forearmRadius, forearmLength,   _sleeveMat);
        CuffTf     = CreateCapsule("Cuff",     cuffRadius,    cuffLength,      _sleeveMat);

        HandRootTf = new GameObject("HandRoot").transform;
        HandRootTf.SetParent(transform);
        CreateBox("Palm", new Vector3(palmWidth, palmHeight, palmDepth), _handMat, HandRootTf);

        FingerBones = new Transform[5, 3];
        for (int f = 0; f < 5; f++)
        {
            var fingerRoot = new GameObject($"Finger{f}").transform;
            fingerRoot.SetParent(HandRootTf);
            fingerRoot.localPosition = FingerPalmOffsets[f];
            fingerRoot.localRotation = Quaternion.identity;

            float[] lengths = { proximalLength, middleLength, distalLength };
            Transform prev = fingerRoot;
            for (int j = 0; j < 3; j++)
            {
                var bone = new GameObject($"Finger{f}_J{j}").transform;
                bone.SetParent(prev);
                bone.localPosition = j == 0 ? Vector3.zero : new Vector3(0f, 0f, lengths[j - 1]);
                bone.localRotation = Quaternion.identity;
                // Capsule extends along local Y by default; rotate 90° so it extends along Z (finger direction)
                var seg = CreateCapsule($"Seg{j}", fingerRadius, lengths[j], _handMat, bone);
                seg.localRotation = Quaternion.Euler(90f, 0f, 0f);
                seg.localPosition = new Vector3(0f, 0f, lengths[j] * 0.5f);
                FingerBones[f, j] = bone;
                prev = bone;
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    Transform CreateCapsule(string goName, float radius, float height, Material mat, Transform parent = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = goName;
        go.transform.SetParent(parent != null ? parent : transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        // Capsule default is 2 units tall, 1 unit diameter at scale 1
        go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
        Destroy(go.GetComponent<CapsuleCollider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }

    Transform CreateSphere(string goName, float radius, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = goName;
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one * radius * 2f;
        Destroy(go.GetComponent<SphereCollider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }

    void CreateBox(string goName, Vector3 size, Material mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = goName;
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = size;
        Destroy(go.GetComponent<BoxCollider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static Material CreateMat(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader);
        mat.color = color;
        mat.SetFloat("_Smoothness", 0.08f);
        return mat;
    }
}
```

- [ ] **Step 2: Open Unity, compile, verify no errors.**

- [ ] **Step 3: Commit**

```bash
git add WhyGary/Assets/Scripts/Player/VRPrimitiveArm.cs
git commit -m "feat: VRPrimitiveArm creates low-poly sleeve and hand geometry"
```

---

## Task 6: VRArmIK — two-bone IK driver

**Files:**
- Create: `Assets/Scripts/Player/VRArmIK.cs`

- [ ] **Step 1: Create VRArmIK**

Create `WhyGary/Assets/Scripts/Player/VRArmIK.cs`:

```csharp
using UnityEngine;

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
    [SerializeField] Vector3    elbowHintLocal       = new Vector3(-0.3f, -0.8f, -0.4f);

    [Tooltip("Rotates the hand model relative to the physical controller so grip looks natural.")]
    [SerializeField] Quaternion wristRotationOffset  = Quaternion.identity;

    void LateUpdate()
    {
        if (bodyDriver == null || controllerTransform == null || arm == null) return;

        Vector3 shoulder    = isRightArm ? bodyDriver.RightShoulderWorld : bodyDriver.LeftShoulderWorld;
        Vector3 wristTarget = controllerTransform.position;

        // World-space elbow hint derived from body orientation
        Vector3 hintWorld = bodyDriver.bodyRoot.TransformDirection(elbowHintLocal.normalized);

        TwoBoneIKSolver.Result ik = TwoBoneIKSolver.Solve(
            shoulder, wristTarget, upperArmLength, forearmLength, hintWorld);

        PlaceCapsuleBetween(arm.UpperArmTf, shoulder,             ik.elbowPosition);
        arm.ElbowTf.position = ik.elbowPosition;
        PlaceCapsuleBetween(arm.ForearmTf,  ik.elbowPosition,    wristTarget);
        PlaceCapsuleBetween(arm.CuffTf,     wristTarget + (wristTarget - ik.elbowPosition).normalized * -0.01f,
                                            wristTarget + (wristTarget - ik.elbowPosition).normalized *  0.05f);

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
```

- [ ] **Step 2: Open Unity, compile, verify no errors.**

- [ ] **Step 3: Commit**

```bash
git add WhyGary/Assets/Scripts/Player/VRArmIK.cs
git commit -m "feat: VRArmIK two-bone IK driver using TwoBoneIKSolver"
```

---

## Task 7: VRFingerTracker — OpenXR input

**Files:**
- Create: `Assets/Scripts/Player/VRFingerTracker.cs`

- [ ] **Step 1: Create VRFingerTracker**

Create `WhyGary/Assets/Scripts/Player/VRFingerTracker.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public struct FingerState
{
    public float thumb;
    public float index;
    public float middle;
    public float ring;
    public float pinky;
}

public class VRFingerTracker : MonoBehaviour
{
    [SerializeField] bool isRightHand = true;

    public FingerState State { get; private set; }

    InputDevice _device;
    readonly List<InputDevice> _deviceBuffer = new List<InputDevice>(2);

    void Update()
    {
        EnsureDevice();
        if (!_device.isValid)
        {
            State = default;
            return;
        }

        _device.TryGetFeatureValue(CommonUsages.grip,         out float grip);
        _device.TryGetFeatureValue(CommonUsages.trigger,      out float trigger);
        _device.TryGetFeatureValue(CommonUsages.indexTouch,   out bool  indexTouch);
        _device.TryGetFeatureValue(CommonUsages.primaryTouch, out bool  primaryTouch);
        _device.TryGetFeatureValue(CommonUsages.secondaryTouch, out bool secondaryTouch);

        bool thumbOnSomething = primaryTouch || secondaryTouch;

        State = new FingerState
        {
            thumb  = thumbOnSomething ? 0.6f : 0.1f,
            index  = indexTouch ? trigger : 0f,
            middle = grip,
            ring   = grip,
            pinky  = grip,
        };
    }

    void EnsureDevice()
    {
        if (_device.isValid) return;

        var chars = isRightHand
            ? InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller
            : InputDeviceCharacteristics.Left  | InputDeviceCharacteristics.Controller;

        InputDevices.GetDevicesWithCharacteristics(chars, _deviceBuffer);
        if (_deviceBuffer.Count > 0) _device = _deviceBuffer[0];
    }
}
```

- [ ] **Step 2: Open Unity, compile, verify no errors.**

- [ ] **Step 3: Commit**

```bash
git add WhyGary/Assets/Scripts/Player/VRFingerTracker.cs
git commit -m "feat: VRFingerTracker reads OpenXR grip/trigger/touch for Valve Index finger state"
```

---

## Task 8: VRHandAnimator — pose blending + finger drive

**Files:**
- Create: `Assets/Scripts/Player/VRHandAnimator.cs`

- [ ] **Step 1: Create VRHandAnimator**

Create `WhyGary/Assets/Scripts/Player/VRHandAnimator.cs`:

```csharp
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

    // Current blended curl values per finger (0=open, 1=fully curled)
    readonly float[] _currentCurl = new float[5];

    VRHandPose _overridePose;
    float      _overrideWeight;    // 0=no override, 1=full override
    float      _overrideBlendTime;
    bool       _overrideFadingIn;

    // Called by GunController when gun is grabbed
    public void SetOverridePose(VRHandPose pose, float blendTime)
    {
        _overridePose      = pose;
        _overrideBlendTime = blendTime > 0f ? blendTime : 0.001f;
        _overrideFadingIn  = true;
    }

    // Called by GunController when gun is released
    public void ClearOverridePose(float blendTime)
    {
        _overrideBlendTime = blendTime > 0f ? blendTime : 0.001f;
        _overrideFadingIn  = false;
    }

    void LateUpdate()
    {
        if (arm == null || arm.FingerBones == null) return;

        // Advance override blend weight
        float blendDelta = Time.deltaTime / _overrideBlendTime;
        _overrideWeight = _overrideFadingIn
            ? Mathf.MoveTowards(_overrideWeight, 1f, blendDelta)
            : Mathf.MoveTowards(_overrideWeight, 0f, blendDelta);

        // Get live curl from tracker (or zero if no tracker)
        float liveCurl0 = fingerTracker != null ? fingerTracker.State.thumb  : 0f;
        float liveCurl1 = fingerTracker != null ? fingerTracker.State.index  : 0f;
        float liveCurl2 = fingerTracker != null ? fingerTracker.State.middle : 0f;
        float liveCurl3 = fingerTracker != null ? fingerTracker.State.ring   : 0f;
        float liveCurl4 = fingerTracker != null ? fingerTracker.State.pinky  : 0f;

        // Blend toward override if active
        if (_overridePose != null && _overrideWeight > 0f)
        {
            float w = _overrideWeight;
            liveCurl0 = Mathf.Lerp(liveCurl0, _overridePose.thumb,  w);
            liveCurl1 = Mathf.Lerp(liveCurl1, _overridePose.index,  w);
            liveCurl2 = Mathf.Lerp(liveCurl2, _overridePose.middle, w);
            liveCurl3 = Mathf.Lerp(liveCurl3, _overridePose.ring,   w);
            liveCurl4 = Mathf.Lerp(liveCurl4, _overridePose.pinky,  w);
        }

        // Smooth the curl values to avoid jitter
        float smooth = Time.deltaTime * 18f;
        _currentCurl[0] = Mathf.Lerp(_currentCurl[0], liveCurl0, smooth);
        _currentCurl[1] = Mathf.Lerp(_currentCurl[1], liveCurl1, smooth);
        _currentCurl[2] = Mathf.Lerp(_currentCurl[2], liveCurl2, smooth);
        _currentCurl[3] = Mathf.Lerp(_currentCurl[3], liveCurl3, smooth);
        _currentCurl[4] = Mathf.Lerp(_currentCurl[4], liveCurl4, smooth);

        // Apply to finger bones
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
```

- [ ] **Step 2: Open Unity, compile, verify no errors.**

- [ ] **Step 3: Commit**

```bash
git add WhyGary/Assets/Scripts/Player/VRHandAnimator.cs
git commit -m "feat: VRHandAnimator blends hand poses and drives finger bone rotations"
```

---

## Task 9: VRHolster — hip holster draw/reholster

**Files:**
- Create: `Assets/Scripts/Player/VRHolster.cs`

- [ ] **Step 1: Create VRHolster**

Create `WhyGary/Assets/Scripts/Player/VRHolster.cs`:

```csharp
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class VRHolster : MonoBehaviour
{
    [Header("References")]
    public PlayerBodyDriver bodyDriver;
    public XRGrabInteractable gunInteractable;
    public Transform          gunTransform;

    [Header("Holster Position (body-local)")]
    [SerializeField] Vector3    holsterLocalPos = new Vector3(0.2f, -0.25f, 0.05f);
    [SerializeField] Quaternion holsterLocalRot = Quaternion.Euler(0f, 0f, 15f);
    [SerializeField] float      proximityRadius = 0.12f;

    Transform _holsterAnchor;
    bool      _isHolstered = true;

    void Awake()
    {
        _holsterAnchor = new GameObject("HolsterAnchor").transform;
        _holsterAnchor.SetParent(transform);
    }

    void OnEnable()
    {
        if (gunInteractable == null) return;
        gunInteractable.selectEntered.AddListener(OnGunGrabbed);
        gunInteractable.selectExited.AddListener(OnGunReleased);
    }

    void OnDisable()
    {
        if (gunInteractable == null) return;
        gunInteractable.selectEntered.RemoveListener(OnGunGrabbed);
        gunInteractable.selectExited.RemoveListener(OnGunReleased);
    }

    void LateUpdate()
    {
        if (bodyDriver == null || bodyDriver.bodyRoot == null) return;

        _holsterAnchor.position = bodyDriver.bodyRoot.TransformPoint(holsterLocalPos);
        _holsterAnchor.rotation = bodyDriver.bodyRoot.rotation * holsterLocalRot;

        if (_isHolstered && gunTransform != null)
        {
            gunTransform.position = _holsterAnchor.position;
            gunTransform.rotation = _holsterAnchor.rotation;
        }
    }

    void OnGunGrabbed(SelectEnterEventArgs _)
    {
        _isHolstered = false;
        if (gunTransform != null)
            gunTransform.SetParent(null);
        SetGunKinematic(false);
    }

    void OnGunReleased(SelectExitEventArgs args)
    {
        // Reholster if the releasing interactor is near the holster
        Vector3 interactorPos = args.interactorObject.transform.position;
        if (Vector3.Distance(interactorPos, _holsterAnchor.position) <= proximityRadius * 1.5f)
            Holster();
        // else: gun drops via physics
    }

    public void Holster()
    {
        if (gunTransform == null) return;
        _isHolstered = true;
        SetGunKinematic(true);
        gunTransform.SetParent(_holsterAnchor);
        gunTransform.localPosition = Vector3.zero;
        gunTransform.localRotation = Quaternion.identity;
    }

    public void Draw()
    {
        _isHolstered = false;
        SetGunKinematic(false);
        if (gunTransform != null)
            gunTransform.SetParent(null);
    }

    void SetGunKinematic(bool kinematic)
    {
        if (gunTransform == null) return;
        var rb = gunTransform.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = kinematic;
    }
}
```

- [ ] **Step 2: Open Unity, compile, verify no errors.**

- [ ] **Step 3: Commit**

```bash
git add WhyGary/Assets/Scripts/Player/VRHolster.cs
git commit -m "feat: VRHolster body-relative hip holster with draw and reholster via XRIT events"
```

---

## Task 10: Update GunController — attach transform + grip pose events

**Files:**
- Modify: `Assets/Scripts/Props/GunController.cs`

- [ ] **Step 1: Replace GunController.cs**

`WhyGary/Assets/Scripts/Props/GunController.cs`:

```csharp
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class GunController : MonoBehaviour
{
    [Header("Firing")]
    public Transform  firePoint;
    public GameObject bulletPrefab;
    public float      shootSpeed = 25f;

    [Header("VR Grip")]
    [Tooltip("Child transform positioned at the grip handle — set as XRGrabInteractable.Attach Transform.")]
    public Transform      rightHandAttach;
    public VRHandAnimator rightHandAnimator;
    public VRHandPose     pistolGripPose;
    public VRHandPose     triggerPose;
    [SerializeField] float gripBlendTime    = 0.12f;
    [SerializeField] float releaseBlendTime = 0.20f;

    XRGrabInteractable _grab;
    bool               _held;

    void Awake() => _grab = GetComponent<XRGrabInteractable>();

    void OnEnable()
    {
        if (_grab == null) return;
        _grab.activated.AddListener(OnFire);
        _grab.selectEntered.AddListener(OnGrabbed);
        _grab.selectExited.AddListener(OnReleased);
    }

    void OnDisable()
    {
        if (_grab == null) return;
        _grab.activated.RemoveListener(OnFire);
        _grab.selectEntered.RemoveListener(OnGrabbed);
        _grab.selectExited.RemoveListener(OnReleased);
    }

    void Update()
    {
        // While held, blend to trigger pose when trigger is sufficiently pressed
        if (!_held || rightHandAnimator == null || triggerPose == null) return;

        var tracker = rightHandAnimator.GetComponent<VRFingerTracker>();
        if (tracker == null) return;

        // Switch to trigger pose when index finger curls past halfway
        if (tracker.State.index > 0.5f)
            rightHandAnimator.SetOverridePose(triggerPose, gripBlendTime);
        else if (pistolGripPose != null)
            rightHandAnimator.SetOverridePose(pistolGripPose, gripBlendTime);
    }

    void OnFire(ActivateEventArgs _)
    {
        if (bulletPrefab == null || firePoint == null) return;
        var projectile = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        projectile.GetComponent<Rigidbody>().linearVelocity = firePoint.forward * shootSpeed;
    }

    void OnGrabbed(SelectEnterEventArgs _)
    {
        _held = true;
        if (rightHandAnimator != null && pistolGripPose != null)
            rightHandAnimator.SetOverridePose(pistolGripPose, gripBlendTime);
    }

    void OnReleased(SelectExitEventArgs _)
    {
        _held = false;
        rightHandAnimator?.ClearOverridePose(releaseBlendTime);
    }
}
```

- [ ] **Step 2: Open Unity, compile, verify no errors.**

- [ ] **Step 3: Commit**

```bash
git add WhyGary/Assets/Scripts/Props/GunController.cs
git commit -m "feat: GunController attach transform reference and grip/trigger pose events"
```

---

## Task 11: Set Script Execution Order

- [ ] **Step 1: In Unity, open Edit → Project Settings → Script Execution Order.**

- [ ] **Step 2: Click the + button and add these scripts in this order (lower number = earlier execution):**

| Script | Order Value |
|--------|-------------|
| `PlayerBodyDriver` | -100 |
| `VRArmIK` | 10 |
| `VRHandAnimator` | 20 |
| `VRHolster` | 30 |

(VRFingerTracker can stay at default 0 — it only reads input, doesn't depend on execution order.)

- [ ] **Step 3: Click Apply. Verify the order is saved.**

---

## Task 12: Wire scene — Left arm

This task and Task 13 are done in the Unity Editor in the WhyGary scene.

- [ ] **Step 1: Open `Assets/Scenes/WhyGary.unity`.**

- [ ] **Step 2: In the Hierarchy, find the Player's body root (the object with `PlayerBodyDriver` attached — called `PlayerBody` or similar).**

- [ ] **Step 3: Create a new empty GameObject as a child of the body root. Name it `LeftArm`.**

- [ ] **Step 4: On `LeftArm`, add component `VRPrimitiveArm`. In the Inspector, set:**
  - Sleeve Color: `(0.08, 0.08, 0.12, 1)` (dark navy)
  - Hand Color: `(0.75, 0.65, 0.55, 1)` (skin tone or adjust to taste)
  - Leave all radii/lengths at their defaults.

- [ ] **Step 5: On `LeftArm`, add component `VRArmIK`. In the Inspector, set:**
  - `Body Driver`: drag the GameObject that has `PlayerBodyDriver` attached
  - `Controller Transform`: drag the Left Controller transform from the XR Rig hierarchy (the transform that tracks the left Index controller — usually `LeftHand Controller` under `Camera Offset`)
  - `Arm`: drag `LeftArm` (the same GameObject — `VRPrimitiveArm` on it)
  - `Is Right Arm`: **unchecked** (this is the LEFT arm)
  - `Elbow Hint Local`: `(0.3, -0.8, -0.4)` — mirrored from right arm
  - Leave `Wrist Rotation Offset` at identity for now (tune in-headset in Task 14)

- [ ] **Step 6: On `LeftArm`, add component `VRFingerTracker`. Set `Is Right Hand`: unchecked.**

- [ ] **Step 7: On `LeftArm`, add component `VRHandAnimator`. In the Inspector:**
  - `Arm`: drag `LeftArm`
  - `Finger Tracker`: drag `LeftArm`

- [ ] **Step 8: Enter Play Mode. Verify left arm appears — puffy dark sleeve visible, elbow bends believably as you move the left controller. No errors in Console.**

---

## Task 13: Wire scene — Right arm + holster

- [ ] **Step 1: Repeat Task 12 steps 3–7 for the right arm. Name it `RightArm`. Key differences:**
  - `VRArmIK → Is Right Arm`: **checked**
  - `VRArmIK → Controller Transform`: right controller transform
  - `VRArmIK → Elbow Hint Local`: `(-0.3, -0.8, -0.4)`
  - `VRFingerTracker → Is Right Hand`: **checked**

- [ ] **Step 2: In the Hierarchy, find or create an empty GameObject on the Player body called `HolsterManager`. Add component `VRHolster`. Set:**
  - `Body Driver`: drag the PlayerBodyDriver object
  - `Gun Interactable`: drag the `XRGrabInteractable` component on the M1911 gun object in the scene
  - `Gun Transform`: drag the M1911 gun root transform
  - `Holster Local Pos`: `(0.2, -0.25, 0.05)` — right hip
  - `Proximity Radius`: `0.12`

- [ ] **Step 3: In the Hierarchy, find the M1911 gun (look for object named `GunBody` or the M1911 prefab instance). Add a child empty GameObject named `RightHandAttach`. Position it at the gun's grip handle — approximately `(0, 0.03, -0.08)` in local space (grip slightly below and behind center).**

- [ ] **Step 4: On the M1911's `XRGrabInteractable` component, set `Attach Transform` to the `RightHandAttach` child you just created.**

- [ ] **Step 5: On the M1911's `GunController` component, set:**
  - `Right Hand Attach`: drag `RightHandAttach`
  - `Right Hand Animator`: drag `RightArm` (the VRHandAnimator on it)
  - `Pistol Grip Pose`: drag `Assets/HandPoses/HandPose_PistolGrip.asset`
  - `Trigger Pose`: drag `Assets/HandPoses/HandPose_Trigger.asset`

- [ ] **Step 6: Enter Play Mode. Verify:**
  - Gun appears on right hip
  - Right arm is visible with sleeve
  - No Console errors

- [ ] **Step 7: Save the scene. Commit.**

```bash
git add WhyGary/Assets/Scenes/WhyGary.unity
git commit -m "feat: wire left/right primitive arms, VR finger trackers, and hip holster in WhyGary scene"
```

---

## Task 14: In-headset calibration

These steps require a Valve Index and SteamVR running.

- [ ] **Step 1: Enter Play Mode and put on the headset.**

- [ ] **Step 2: Tune `VRArmIK.Elbow Hint Local` on both arms.** Hold arms naturally at your sides. Elbows should hang down and slightly back. If they poke forward or look broken, adjust the Y component (more negative = lower elbow) and Z component (more negative = elbow further back).

- [ ] **Step 3: Tune `VRArmIK.Wrist Rotation Offset` on right arm.** Grip something. The gun handle should feel like it lines up with your physical grip. If the hand model appears rotated, adjust the Quaternion. In the Inspector you can edit as Euler angles by right-clicking the Quaternion field and choosing "Edit as Euler Angles".

- [ ] **Step 4: Tune `VRHolster.Holster Local Pos`.** Reach for your right hip. The gun should be where your hand naturally goes. Adjust X (left/right), Y (up/down), Z (forward/back) while in Play Mode — changes are live.

- [ ] **Step 5: Tune `PlayerBodyDriver.Left Shoulder Offset` and `Right Shoulder Offset`.** If the sleeves pop or stretch weirdly as you move your arms around, the shoulder offset is wrong. Raise Y if shoulders appear too low, shift X outward if arms cross the body at rest.

- [ ] **Step 6: Test the draw.** Reach to right hip, squeeze grip. Gun should come to your hand. Release while hand is near hip — gun should snap back. Release away from hip — gun should drop.

- [ ] **Step 7: Test finger tracking.** Watch your right hand. Fingers should curl as you squeeze the grip button. Index should extend when not pressing trigger, curl when pressing. Thumb should curl when resting on trackpad.

- [ ] **Step 8: When feel is acceptable, save Scene and commit calibrated values.**

```bash
git add WhyGary/Assets/Scenes/WhyGary.unity
git commit -m "chore: calibrate arm IK elbow hints, wrist offsets, and holster position"
```

---

## Task 15: Apply same changes to Sandbox scene

- [ ] **Step 1: Open `Assets/Scenes/Sandbox.unity`.**

- [ ] **Step 2: Repeat Tasks 12 and 13 for the Sandbox scene's player rig.** The Sandbox scene has the same `PlayerBodyDriver` pattern based on the scene search — replicate the same component wiring.

- [ ] **Step 3: Save and commit.**

```bash
git add WhyGary/Assets/Scenes/Sandbox.unity
git commit -m "feat: apply VR arm and holster setup to Sandbox scene"
```
