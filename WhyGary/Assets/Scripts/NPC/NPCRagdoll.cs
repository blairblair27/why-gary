using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class NPCRagdoll : MonoBehaviour
{
    public Material deadMaterial;

    Rigidbody[] _bodies;
    bool _ragdollActive;

    void Awake()
    {
        _bodies = GetComponentsInChildren<Rigidbody>();
        SetKinematic(true);
    }

    void Start()
    {
        // Auto-wire grab event — no manual Inspector wiring needed
        var grab = GetComponentInChildren<XRGrabInteractable>(true);
        if (grab != null)
            grab.selectEntered.AddListener(_ => EnableRagdoll());
    }

    public void EnableRagdoll(Vector3 hitPoint = default, Vector3 impulse = default)
    {
        if (_ragdollActive)
        {
            if (impulse.sqrMagnitude > 0.01f)
                ApplyImpulse(hitPoint, impulse);
            return;
        }

        _ragdollActive = true;
        SetKinematic(false);
        ApplyImpulse(hitPoint, impulse);

        if (deadMaterial != null)
        {
            foreach (var r in GetComponentsInChildren<MeshRenderer>())
                r.material = deadMaterial;
        }
    }

    public void OnGrabbed() => EnableRagdoll();

    void ApplyImpulse(Vector3 hitPoint, Vector3 impulse)
    {
        if (impulse.sqrMagnitude < 0.01f || _bodies.Length == 0) return;
        Rigidbody closest = _bodies.OrderBy(rb => Vector3.Distance(rb.position, hitPoint)).First();
        closest.AddForceAtPosition(impulse, hitPoint, ForceMode.Impulse);
    }

    void SetKinematic(bool value)
    {
        foreach (var rb in _bodies) rb.isKinematic = value;
    }
}
