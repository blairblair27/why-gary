using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BulletProjectile : MonoBehaviour
{
    [SerializeField] float _headDamage  = 9999f;
    [SerializeField] float _torsoDamage = 40f;
    [SerializeField] float _armDamage   = 18f;
    [SerializeField] float _maxLifetime = 8f;

    void Awake()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void Start() => Destroy(gameObject, _maxLifetime);

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("NPCEscort"))
        {
            col.gameObject.GetComponentInParent<EscortAgent>()?.OnHitByBullet();
            Destroy(gameObject);
            return;
        }

        float damage = 0f;
        if      (col.gameObject.CompareTag("NPCHead"))  damage = _headDamage;
        else if (col.gameObject.CompareTag("NPCTorso")) damage = _torsoDamage;
        else if (col.gameObject.CompareTag("NPCArm"))   damage = _armDamage;

        if (damage > 0)
        {
            Vector3 hitPoint = col.contactCount > 0 ? col.contacts[0].point : transform.position;
            Vector3 hitDir = col.impulse.sqrMagnitude > 0.001f ? -col.impulse.normalized : transform.forward;
            col.gameObject.GetComponentInParent<NPCHealth>()?.TakeDamage(damage, hitPoint, hitDir);
        }

        Destroy(gameObject);
    }
}
