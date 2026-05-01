using UnityEngine;
using UnityEngine.Events;

public class WaveDetector : MonoBehaviour
{
    [Header("Tracked Transforms")]
    public Transform rightHandTransform;
    public Transform leftHandTransform;
    public Transform hmdTransform;

    [Header("Wave Thresholds")]
    public float shoulderHeightOffset = -0.3f;
    public float velocityThreshold = 0.8f;
    public float waveWindowSeconds = 1.5f;
    public int swingsRequired = 3;
    public float cooldownSeconds = 2f;

    [Header("Events")]
    public UnityEvent onWaveDetected;

    Vector3 _lastRightPos, _lastLeftPos;
    int _rightSwings, _leftSwings;
    float _rightWindowTimer, _leftWindowTimer;
    bool _lastRightPositive, _lastLeftPositive;
    float _cooldownTimer;

    void Start()
    {
        _lastRightPos = rightHandTransform.position;
        _lastLeftPos = leftHandTransform.position;
    }

    void Update()
    {
        _cooldownTimer -= Time.deltaTime;

        float rightLateral = (rightHandTransform.position.x - _lastRightPos.x) / Time.deltaTime;
        float leftLateral  = (leftHandTransform.position.x  - _lastLeftPos.x)  / Time.deltaTime;
        _lastRightPos = rightHandTransform.position;
        _lastLeftPos  = leftHandTransform.position;

        float shoulderHeight = hmdTransform.position.y + shoulderHeightOffset;

        CheckWave(rightHandTransform.position.y, shoulderHeight, rightLateral,
                  ref _rightSwings, ref _rightWindowTimer, ref _lastRightPositive);
        CheckWave(leftHandTransform.position.y,  shoulderHeight, leftLateral,
                  ref _leftSwings,  ref _leftWindowTimer,  ref _lastLeftPositive);
    }

    void CheckWave(float handY, float shoulderY, float lateralVel,
                   ref int swings, ref float timer, ref bool lastPositive)
    {
        timer -= Time.deltaTime;
        if (timer <= 0) swings = 0;
        if (handY <= shoulderY) return;

        bool positive = lateralVel > 0;
        if (Mathf.Abs(lateralVel) > velocityThreshold && positive != lastPositive)
        {
            swings++;
            lastPositive = positive;
            timer = waveWindowSeconds;

            if (swings >= swingsRequired && _cooldownTimer <= 0)
            {
                onWaveDetected.Invoke();
                _cooldownTimer = cooldownSeconds;
                swings = 0;
            }
        }
    }
}
