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
    [Tooltip("Child transform at the grip handle — set as XRGrabInteractable.Attach Transform.")]
    public Transform      rightHandAttach;
    public VRHandAnimator rightHandAnimator;
    public VRHandPose     pistolGripPose;
    public VRHandPose     triggerPose;
    [SerializeField] float gripBlendTime    = 0.12f;
    [SerializeField] float releaseBlendTime = 0.20f;

    XRGrabInteractable _grab;
    bool               _held;
    VRFingerTracker    _tracker;

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
        if (!_held || rightHandAnimator == null || triggerPose == null || _tracker == null) return;

        if (_tracker.State.index > 0.5f)
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
        _held    = true;
        _tracker = rightHandAnimator != null ? rightHandAnimator.GetComponent<VRFingerTracker>() : null;
        if (rightHandAnimator != null && pistolGripPose != null)
            rightHandAnimator.SetOverridePose(pistolGripPose, gripBlendTime);
    }

    void OnReleased(SelectExitEventArgs _)
    {
        _held    = false;
        _tracker = null;
        rightHandAnimator?.ClearOverridePose(releaseBlendTime);
    }
}
