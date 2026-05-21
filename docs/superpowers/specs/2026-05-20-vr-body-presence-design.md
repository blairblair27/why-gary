# VR Body Presence Quality Pass — Design Spec
**Date:** 2026-05-20  
**Reference:** Onward (primary), Rec Room (visual style)  
**Scope:** Pistol only (M1911). Two-handed rifle deferred.

---

## Context

Current state:
- Hands are raw VR controller models — no arms, no geometry, no IK
- `PlayerBodyDriver` does direct controller→hand target mapping with no IK
- `GunController` has no `AttachTransform` wired (`fileID: 0`), no hand poses, no holster
- No Animation Rigging package installed
- No finger tracking
- No holster system

Goal: Onward-style first-person arm presence — arms visually connected to body, correct elbow behavior, pistol draw from hip holster, Valve Index finger tracking on grip/trigger.

---

## Visual Style

**Low-poly Rec Room style.** No FBX, no rigging, no Animator on arms. Arms assembled from Unity primitives positioned by IK code each frame.

### Per-arm geometry (4 objects):

| Object | Shape | Approx radius | Approx length | Notes |
|--------|-------|---------------|---------------|-------|
| UpperArm | Capsule | 0.07 | 0.28 | Fat — reads as puffy jacket sleeve |
| ElbowJoint | Sphere | 0.06 | — | Soft fabric bulge at joint |
| Forearm | Capsule | 0.05 | 0.24 | Tapers slightly toward wrist |
| WristCuff | Capsule (short, fat) | 0.065 | 0.06 | Jacket cuff before hand |

All values Inspector-tunable via `VRPrimitiveArm` config struct.

### Hand geometry:
- Flat box palm
- 5 finger chains × 3 capsule bones each (proximal/mid/distal)
- Driven by finger tracking — these are what animate

### Materials:
- **Jacket sleeve** (upper arm + elbow + forearm + cuff): matte fabric, bold solid color (navy/black)
- **Hand/fingers**: contrasting solid color

### `VRPrimitiveArm` MonoBehaviour:
Creates and positions all primitive GameObjects from serialized config. Repositioned each frame by the IK solver. No Animator involved.

---

## IK Architecture

### Shoulder estimation
Shoulders are not tracked — derived from body each frame:
```
leftShoulderWorld  = bodyRoot.TransformPoint(new Vector3(-0.18f, 0.14f, 0.04f))
rightShoulderWorld = bodyRoot.TransformPoint(new Vector3( 0.18f, 0.14f, 0.04f))
```
Exposed as `PlayerBodyDriver.LeftShoulderWorld` / `RightShoulderWorld` (public properties). Single source of truth — `VRArmIK` reads from `PlayerBodyDriver`, does not own its own shoulder logic.

### Two-bone solver (`VRArmIK`)
Runs in `LateUpdate` after `PlayerBodyDriver`.

**Inputs:** shoulder position, wrist target (controller position), upper arm length, forearm length, elbow hint direction.

**Solve:**
1. Clamp wrist distance to `upperArmLength + forearmLength * 1.15f` (15% stretch guard before locking)
2. Law of cosines → elbow angle
3. Place elbow in the plane of `(shoulder→wrist)` × `elbowHint`
4. Orient `UpperArm` from shoulder to elbow (LookAt + 90° Y correction for capsule axis)
5. Orient `Forearm` from elbow to wrist
6. Place `ElbowJoint` sphere at solved elbow position

### Elbow hint
Body-space vector biasing elbow direction. Default right arm: `(-0.3, -0.8, -0.4)` normalized (down, back, inward). Blends toward outward as hand rises above shoulder height. Inspector-exposed — very sensitive, needs in-headset tuning.

### Wrist rotation
`hand.rotation = controllerRotation * wristRotationOffset`  
`wristRotationOffset` is a serialized Quaternion on `VRArmIK` that corrects the visual hand model's rotation relative to the physical controller orientation (the controller sits at a different angle in your hand than the model should appear). This is independent of XRIT's `attachTransform` on the gun — XRIT moves the gun to align with the controller anchor; `VRArmIK` corrects the hand model's visual rotation separately. The offset is tuned once per controller type and stays constant.

---

## Finger Tracking & Hand Poses

### Input (no extra packages — OpenXR + XRI 3.x)

| Input | API | Drives |
|-------|-----|--------|
| `grip` float | `InputDevice.TryGetFeatureValue(CommonUsages.grip)` | Middle, ring, pinky curl |
| `trigger` float | `CommonUsages.trigger` | Index curl |
| `indexTouch` bool | `CommonUsages.indexTouch` | Index extended when false |
| `thumbTouch` bool | `CommonUsages.primaryTouch` / `secondaryTouch` | Thumb position |

Three independent finger zones. Full `XRHandSubsystem` per-joint tracking can be added later without architectural changes.

### `VRFingerTracker` MonoBehaviour
Reads input each frame, outputs `FingerState` struct (5 floats, 0=open, 1=curled):
- Middle/ring/pinky = grip float
- Index = trigger float (0 when `indexTouch` is false)
- Thumb = 0.6 if thumbTouch, 0.1 otherwise

### `VRHandPose` ScriptableObject
Stores 5 per-finger curl values + pose name.

| Pose | Thumb | Index | Middle | Ring | Pinky |
|------|-------|-------|--------|------|-------|
| Idle | 0.1 | 0.05 | 0.15 | 0.15 | 0.2 |
| PistolGrip | 0.6 | 0.0 | 0.85 | 0.9 | 0.9 |
| Trigger | 0.6 | 0.85 | 0.85 | 0.9 | 0.9 |

### `VRHandAnimator` MonoBehaviour
- `baseTargetPose`: live `FingerState` from `VRFingerTracker`
- `overridePose`: set to `PistolGrip` when gun grabbed (lerp over 0.12s), fades out over 0.2s on release
- When trigger pressed while holding gun: index curl overrides to Trigger value
- Per-joint rotation ranges: proximal 0→70°, middle 0→80°, distal 0→60° around local X. Tunable per-finger.

---

## Holster & Draw System

### Holster position
Body-relative, right hip. Recalculated in `LateUpdate`:
```
holsterAnchor.position = bodyRoot.TransformPoint(new Vector3(0.2f, -0.25f, 0.05f))
```
Inspector-tunable. Gun visible on hip at all times when not held (kinematic, parented to anchor, handle-up ~15° outward tilt).

### Draw
1. Right controller enters proximity sphere (radius 0.12m)
2. Player squeezes grip → gun detaches from holster, `XRGrabInteractable` activates
3. Hand pose lerps to `PistolGrip` over 0.12s
4. Gun becomes non-kinematic

### Re-holster
1. Gun held, right hand enters proximity zone
2. Player releases grip → gun lerps to holster rest pose over 0.15s, goes kinematic, re-parents to anchor
3. Hand pose fades to `Idle`

### `VRHolster` MonoBehaviour
- Owns holster anchor transform + proximity SphereCollider (trigger)
- Subscribes to XRIT `selectExited` on the gun → triggers re-holster if hand is near zone
- Exposes `Draw()` / `Holster()` for scenario scripting

### Gun `AttachTransform`
Currently `fileID: 0` on M1911 — **this is the primary cause of wrong grip feel.**  
Fix: add child transform `RightHandAttach` to M1911 prefab, positioned so handle sits correctly in palm. `XRGrabInteractable.attachTransform` references this child.

---

## Refactored `PlayerBodyDriver`

### Head-body angle clamping
Body yaw slow-follows HMD yaw as before. New: if angular difference exceeds 60°, body aggressively catches up (lerp speed multiplied 5×). Prevents extreme lag on fast head turns.

### Shoulder properties
```csharp
public Vector3 LeftShoulderWorld  { get; private set; }
public Vector3 RightShoulderWorld { get; private set; }
```
Computed each `Update()` from body root + serialized local offsets.

### Torso sway (optional, default off)
HMD lateral movement adds ±3° counter-rotation to shoulder targets. Tunable float `torsSwayAmount`, defaults to 0.

---

## Script Execution Order (Project Settings)

Enforced via **Edit → Project Settings → Script Execution Order** (not `[DefaultExecutionOrder]` attributes):

1. `PlayerBodyDriver` — moves body, exposes shoulder positions
2. `VRArmIK` — solves IK using updated shoulders
3. `VRHandAnimator` — applies finger poses after IK
4. `VRHolster` — repositions holster after body moves

---

## New Files

| File | Type | Purpose |
|------|------|---------|
| `Scripts/Player/VRPrimitiveArm.cs` | MonoBehaviour | Creates and owns arm primitive geometry |
| `Scripts/Player/VRArmIK.cs` | MonoBehaviour | Two-bone IK solver for one arm |
| `Scripts/Player/VRFingerTracker.cs` | MonoBehaviour | Reads OpenXR input, outputs FingerState |
| `Scripts/Player/VRHandAnimator.cs` | MonoBehaviour | Blends poses, drives finger bone rotations |
| `Scripts/Player/VRHandPose.cs` | ScriptableObject | Per-finger curl values for a named pose |
| `Scripts/Player/VRHolster.cs` | MonoBehaviour | Body-relative holster zone, draw/reholster logic |
| `Assets/HandPoses/HandPose_Idle.asset` | Asset | Idle hand pose values |
| `Assets/HandPoses/HandPose_PistolGrip.asset` | Asset | Pistol grip pose values |
| `Assets/HandPoses/HandPose_Trigger.asset` | Asset | Trigger-pressed pose values |

## Modified Files

| File | Change |
|------|--------|
| `Scripts/Player/PlayerBodyDriver.cs` | Add shoulder properties, head-body angle clamping, optional torso sway |
| `Scripts/Props/GunController.cs` | Add `rightHandAttach` Transform reference, `pistolGripPose` VRHandPose reference |
| M1911 prefab (scene) | Add `RightHandAttach` child transform, wire `attachTransform` on `XRGrabInteractable` |
