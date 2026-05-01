using UnityEngine;

public class EscortAgent : MonoBehaviour
{
    enum State { Patrolling, Tackling }

    State _state = State.Patrolling;
    Transform _playerTarget;
    const float TackleSpeed = 3f;

    // Called by CorkProjectile when tag == "NPCEscort"
    public void OnHitByCork()
    {
        FindFirstObjectByType<WhyGaryScenario>()?.OnEscortHit();
    }

    public void StartTackle(Transform player)
    {
        _state = State.Tackling;
        _playerTarget = player;
    }

    void Update()
    {
        if (_state != State.Tackling || _playerTarget == null) return;

        transform.position = Vector3.MoveTowards(
            transform.position, _playerTarget.position, TackleSpeed * Time.deltaTime);

        Vector3 lookDir = _playerTarget.position - transform.position;
        if (lookDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
    }
}
