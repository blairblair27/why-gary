using UnityEngine;

public class CorkProjectile : MonoBehaviour
{
    void OnCollisionEnter(Collision col)
    {
        string tag = col.gameObject.tag;

        // Escort hit = lose condition, no damage calculation needed
        if (tag == "NPCEscort")
        {
            col.gameObject.GetComponentInParent<EscortAgent>()?.OnHitByCork();
            Destroy(gameObject);
            return;
        }

        float damage = tag switch
        {
            "NPCHead"  => 9999f,
            "NPCTorso" => 40f,
            "NPCArm"   => 18f,
            _          => 0f
        };

        if (damage > 0)
        {
            // col.impulse points away from the surface, so negate to get hit direction
            Vector3 hitDir = col.impulse.sqrMagnitude > 0.001f
                ? -col.impulse.normalized
                : transform.forward;

            col.gameObject.GetComponentInParent<NPCHealth>()
                ?.TakeDamage(damage, col.contacts[0].point, hitDir);
        }

        Destroy(gameObject);
    }
}
