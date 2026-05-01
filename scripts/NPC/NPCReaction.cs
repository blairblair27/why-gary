using System.Collections;
using UnityEngine;

public class NPCReaction : MonoBehaviour
{
    public Transform npcHead;
    public float nodAngle = 15f;
    public float nodDuration = 0.8f;

    NPCHealth _health;

    void Awake() => _health = GetComponent<NPCHealth>();

    public void OnPlayerWaved()
    {
        if (_health != null && _health.IsDead) return;
        StopAllCoroutines();
        StartCoroutine(NodHead());
    }

    public void OnDeath()
    {
        StopAllCoroutines();
    }

    IEnumerator NodHead()
    {
        if (npcHead == null) yield break;
        Quaternion original = npcHead.localRotation;
        Quaternion down = Quaternion.Euler(nodAngle, 0, 0);
        Quaternion up   = Quaternion.Euler(-nodAngle * 0.4f, 0, 0);

        yield return LerpRot(npcHead, original, down, nodDuration * 0.4f);
        yield return LerpRot(npcHead, down,     up,   nodDuration * 0.3f);
        yield return LerpRot(npcHead, up,  original,  nodDuration * 0.3f);
    }

    IEnumerator LerpRot(Transform t, Quaternion from, Quaternion to, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            t.localRotation = Quaternion.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localRotation = to;
    }
}
