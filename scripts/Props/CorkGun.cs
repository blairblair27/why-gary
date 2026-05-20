using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class CorkGun : MonoBehaviour
{
    public Transform firePoint;
    public GameObject corkPrefab;
    public GameObject corkVisual;
    public float shootSpeed = 12f;

    XRGrabInteractable _grab;

    void Awake() => _grab = GetComponent<XRGrabInteractable>();

    void OnEnable()  { if (_grab != null) _grab.activated.AddListener(OnFire); }
    void OnDisable() { if (_grab != null) _grab.activated.RemoveListener(OnFire); }

    void OnFire(ActivateEventArgs _)
    {
        if (corkPrefab == null || firePoint == null) return;

        if (corkVisual != null) corkVisual.SetActive(false);

        var projectile = Instantiate(corkPrefab, firePoint.position, firePoint.rotation);
        projectile.GetComponent<Rigidbody>().linearVelocity = firePoint.forward * shootSpeed;
    }
}
