using UnityEngine;

public class NPCHealth : MonoBehaviour
{
    public float maxHealth = 100f;

    float _hp;
    bool _dead;
    NPCRagdoll _ragdoll;
    NPCReaction _reaction;

    public bool IsDead => _dead;

    void Awake() { _ragdoll = GetComponent<NPCRagdoll>(); _reaction = GetComponent<NPCReaction>(); }

    void Start() => _hp = maxHealth;

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (_dead) return;
        _hp -= amount;
        if (_hp <= 0)
        {
            Die(hitPoint, hitDirection);
        }
        else
        {
            float impactForce = Mathf.Clamp(amount * 0.2f, 2f, 12f);
            _ragdoll?.EnableRagdoll(hitPoint, hitDirection * impactForce);
        }
    }

    void Die(Vector3 hitPoint, Vector3 hitDirection)
    {
        _dead = true;
        _ragdoll?.EnableRagdoll(hitPoint, hitDirection * 8f);
        _ragdoll?.OnDied();
        _reaction?.OnDeath();
    }
}
