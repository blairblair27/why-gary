using UnityEngine;

public class NPCGroupPatrol : MonoBehaviour
{
    [Tooltip("Empty GameObjects placed in scene defining the walk path")]
    public Transform[] waypoints;
    [Tooltip("The EscortGroup root — this whole object gets moved")]
    public Transform groupRoot;
    public float moveSpeed = 1.2f;

    int _current;
    bool _reachedEnd;
    WhyGaryScenario _scenario;

    void Start() => _scenario = FindAnyObjectByType<WhyGaryScenario>();

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0 || groupRoot == null) return;

        Vector3 target = waypoints[_current].position;
        groupRoot.position = Vector3.MoveTowards(groupRoot.position, target, moveSpeed * Time.deltaTime);

        Vector3 dir = target - groupRoot.position;
        if (dir.sqrMagnitude > 0.01f)
            groupRoot.rotation = Quaternion.LookRotation(dir, Vector3.up);

        if (Vector3.Distance(groupRoot.position, target) < 0.1f)
        {
            if (_current < waypoints.Length - 1)
                _current++;
            else if (!_reachedEnd)
            {
                _reachedEnd = true;
                enabled = false;
                _scenario?.OnEscortReachedEnd();
            }
        }
    }
}
