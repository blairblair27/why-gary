using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class VRHolster : MonoBehaviour
{
    [Header("References")]
    public PlayerBodyDriver   bodyDriver;
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
        gunTransform?.SetParent(null);
        SetGunKinematic(false);
    }

    void OnGunReleased(SelectExitEventArgs args)
    {
        Vector3 interactorPos = args.interactorObject.transform.position;
        if (Vector3.Distance(interactorPos, _holsterAnchor.position) <= proximityRadius * 1.5f)
            Holster();
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
        gunTransform?.SetParent(null);
    }

    void SetGunKinematic(bool kinematic)
    {
        if (gunTransform == null) return;
        var rb = gunTransform.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = kinematic;
    }
}
