using UnityEngine;

public class NPCAnimator : MonoBehaviour
{
    static readonly int SpeedHash = Animator.StringToHash("Speed");

    Animator _anim;
    Vector3 _lastPos;

    void Awake()
    {
        _anim = GetComponentInChildren<Animator>();
        _lastPos = transform.position;
    }

    void Update()
    {
        if (_anim == null) return;
        float speed = Vector3.Distance(transform.position, _lastPos) / Time.deltaTime;
        _lastPos = transform.position;
        _anim.SetFloat(SpeedHash, speed);
    }
}
