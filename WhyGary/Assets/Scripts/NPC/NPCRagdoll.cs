using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class NPCRagdoll : MonoBehaviour
{
    public Material deadMaterial;

    Rigidbody[] _bodies;
    MeshRenderer[] _renderers;
    bool _ragdollActive;

    void Awake()
    {
        _bodies = GetComponentsInChildren<Rigidbody>();
        _renderers = GetComponentsInChildren<MeshRenderer>();
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
    }

    // Call only on actual death — applies dead material separately from physics ragdoll
    public void OnDied()
    {
        if (deadMaterial == null) return;
        foreach (var r in _renderers)
            r.sharedMaterial = deadMaterial;
    }

    public void OnGrabbed() => EnableRagdoll();

    void ApplyImpulse(Vector3 hitPoint, Vector3 impulse)
    {
        if (impulse.sqrMagnitude < 0.01f || _bodies.Length == 0) return;
        Rigidbody closest = _bodies[0];
        float closestSqr = (closest.position - hitPoint).sqrMagnitude;
        for (int i = 1; i < _bodies.Length; i++)
        {
            float d = (_bodies[i].position - hitPoint).sqrMagnitude;
            if (d < closestSqr) { closestSqr = d; closest = _bodies[i]; }
        }
        closest.AddForceAtPosition(impulse, hitPoint, ForceMode.Impulse);
    }

    void SetKinematic(bool value)
    {
        foreach (var rb in _bodies)
        {
            rb.collisionDetectionMode = value ? CollisionDetectionMode.Discrete : CollisionDetectionMode.Continuous;
            rb.isKinematic = value;
        }
    }
}
