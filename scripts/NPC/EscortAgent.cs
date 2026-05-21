using UnityEngine;

public class EscortAgent : MonoBehaviour
{
    enum State { Patrolling, Tackling }

    State _state = State.Patrolling;
    Transform _playerTarget;
    WhyGaryScenario _scenario;
    const float TackleSpeed = 3f;

    void Start() => _scenario = FindAnyObjectByType<WhyGaryScenario>();

    // Called by BulletProjectile when tag == "NPCEscort"
    public void OnHitByBullet()
    {
        _scenario?.OnEscortHit();
    }

    public void StartTackle(Transform player)
    {
        _state = State.Tackling;
        _playerTarget = player;
    }

    void Update()
    {
        if (_state != State.Tackling || _playerTarget == null) return;

        // Flatten to floor so escorts don't float up toward the HMD
        Vector3 flatTarget = new Vector3(_playerTarget.position.x, transform.position.y, _playerTarget.position.z);
        transform.position = Vector3.MoveTowards(transform.position, flatTarget, TackleSpeed * Time.deltaTime);

        Vector3 lookDir = flatTarget - transform.position;
        if (lookDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
    }
}
