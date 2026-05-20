using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class GunController : MonoBehaviour
{
    public Transform firePoint;
    public GameObject bulletPrefab;
    public float shootSpeed = 25f;

    XRGrabInteractable _grab;

    void Awake() => _grab = GetComponent<XRGrabInteractable>();

    void OnEnable()  { if (_grab != null) _grab.activated.AddListener(OnFire); }
    void OnDisable() { if (_grab != null) _grab.activated.RemoveListener(OnFire); }

    void OnFire(ActivateEventArgs _)
    {
        if (bulletPrefab == null || firePoint == null) return;
        var projectile = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        projectile.GetComponent<Rigidbody>().linearVelocity = firePoint.forward * shootSpeed;
    }
}
