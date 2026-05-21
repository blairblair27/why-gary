using UnityEngine;

// Draws a drooping phone cord between two transforms using a LineRenderer.
// Toggle this object's active state to swap between static mesh cord and live cord.
[RequireComponent(typeof(LineRenderer))]
public class PhoneCord : MonoBehaviour
{
    [HideInInspector] public Transform cordStart;  // where cord leaves the phone body
    [HideInInspector] public Transform cordEnd;    // handset end

    [SerializeField] int   _segments = 14;
    [SerializeField] float _sag      = 0.05f;

    LineRenderer _lr;

    void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.positionCount = _segments;
        _lr.useWorldSpace = true;
    }

    void LateUpdate()
    {
        if (cordStart == null || cordEnd == null) return;
        for (int i = 0; i < _segments; i++)
        {
            float t   = (float)i / (_segments - 1);
            float sag = _sag * 4f * t * (1f - t);
            _lr.SetPosition(i, Vector3.Lerp(cordStart.position, cordEnd.position, t) + Vector3.down * sag);
        }
    }
}
