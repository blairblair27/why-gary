using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Attach to payphone_horn. Plays a procedurally generated North American dial tone (350+440 Hz)
// when grabbed. Tight spatial rolloff (~50 cm) means the tone is only audible near the player's ear.
// Snaps back to cradle position on release.
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(XRGrabInteractable))]
public class PayphoneHandset : MonoBehaviour
{
    // Wired by WhyGaryBuilder at scene build time
    [HideInInspector] public Renderer  staticCord;   // payphone_wire mesh — shown in cradle
    [HideInInspector] public PhoneCord dynamicCord;  // LineRenderer cord   — shown when held

    const float DialToneFreq1Hz    = 350f;
    const float DialToneFreq2Hz    = 440f;
    const float DialToneAmplitude  = 0.22f;
    const int   FallbackSampleRate = 44100;

    AudioSource        _audio;
    XRGrabInteractable _grab;
    Vector3            _cradleLocalPos;
    Quaternion         _cradleLocalRot;

    void Awake()
    {
        _cradleLocalPos = transform.localPosition;
        _cradleLocalRot = transform.localRotation;
    }

    void Start()
    {
        _audio = GetComponent<AudioSource>();
        _audio.clip         = GenerateDialTone();
        _audio.loop         = true;
        _audio.spatialBlend = 1.0f;
        _audio.rolloffMode  = AudioRolloffMode.Logarithmic;
        _audio.minDistance  = 0.04f;
        _audio.maxDistance  = 0.5f;
        _audio.volume       = 0.9f;
        _audio.playOnAwake  = false;
        _audio.dopplerLevel = 0f;

        _grab = GetComponent<XRGrabInteractable>();
        if (_grab != null)
        {
            _grab.selectEntered.AddListener(OnGrabbed);
            _grab.selectExited.AddListener(OnReleased);
        }

        if (dynamicCord != null) dynamicCord.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (_grab == null) return;
        _grab.selectEntered.RemoveListener(OnGrabbed);
        _grab.selectExited.RemoveListener(OnReleased);
    }

    void OnGrabbed(SelectEnterEventArgs _)
    {
        _audio.Play();
        if (staticCord  != null) staticCord.enabled = false;
        if (dynamicCord != null) dynamicCord.gameObject.SetActive(true);
    }

    void OnReleased(SelectExitEventArgs _)
    {
        _audio.Stop();
        transform.localPosition = _cradleLocalPos;
        transform.localRotation = _cradleLocalRot;
        if (staticCord  != null) staticCord.enabled = true;
        if (dynamicCord != null) dynamicCord.gameObject.SetActive(false);
    }

    static AudioClip GenerateDialTone()
    {
        int rate    = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : FallbackSampleRate;
        int samples = rate * 3;
        var clip    = AudioClip.Create("DialTone", samples, 1, rate, false);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            data[i] = DialToneAmplitude * (Mathf.Sin(2f * Mathf.PI * DialToneFreq1Hz * t)
                                         + Mathf.Sin(2f * Mathf.PI * DialToneFreq2Hz * t));
        }
        clip.SetData(data, 0);
        return clip;
    }
}
