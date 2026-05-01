using UnityEngine;

public class NPCHealth : MonoBehaviour
{
    public float maxHealth = 100f;

    float _hp;
    bool _dead;

    public bool IsDead => _dead;

    void Start() => _hp = maxHealth;

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (_dead) return;
        _hp -= amount;

        float impactForce = Mathf.Clamp(amount * 0.2f, 2f, 12f);
        GetComponent<NPCRagdoll>()?.EnableRagdoll(hitPoint, hitDirection * impactForce);

        if (_hp <= 0) Die(hitPoint, hitDirection);
    }

    void Die(Vector3 hitPoint, Vector3 hitDirection)
    {
        _dead = true;
        GetComponent<NPCRagdoll>()?.EnableRagdoll(hitPoint, hitDirection * 8f);
        GetComponent<NPCReaction>()?.OnDeath();
    }
}
