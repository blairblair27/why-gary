#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

// Temporary diagnostic — attach to Main Camera, read Console output in Play mode.
// Remove once the room-visibility bug is fixed.
public class CameraDebug : MonoBehaviour
{
    void Start()
    {
        Debug.Log($"[CameraDebug] START  pos={transform.position}  fwd={transform.forward}");
    }

    void LateUpdate()
    {
        if (Time.frameCount <= 5 || Time.frameCount % 90 == 0)
            Debug.Log($"[CameraDebug] frame={Time.frameCount}  pos={transform.position}  fwd={transform.forward}");
    }
}
#endif
