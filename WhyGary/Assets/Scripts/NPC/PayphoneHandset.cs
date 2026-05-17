using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Attach to a phone handset GameObject that also has XRGrabInteractable + AudioSource.
// Plays a procedurally generated North American dial tone (350 Hz + 440 Hz) when grabbed.
// Spatial audio with a tight rolloff (~22 cm) means only the ear touching the receiver hears it.
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(XRGrabInteractable))]
public class PayphoneHandset : MonoBehaviour
{
    const int   FallbackSampleRate  = 44100;
    const float DialToneFreq1Hz     = 350f;   // NANP dial tone component 1
    const float DialToneFreq2Hz     = 440f;   // NANP dial tone component 2
    const float DialToneAmplitude   = 0.22f;

    AudioSource _audio;
    XRGrabInteractable _grab;

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
    }

    void OnDestroy()
    {
        if (_grab != null)
        {
            _grab.selectEntered.RemoveListener(OnGrabbed);
            _grab.selectExited.RemoveListener(OnReleased);
        }
    }

    void OnGrabbed(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs _) => _audio.Play();
    void OnReleased(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs _) => _audio.Stop();

    static AudioClip GenerateDialTone()
    {
        int rate    = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : FallbackSampleRate;
        int samples = rate * 3;   // 3-second buffer — both 350 Hz and 440 Hz complete whole cycles
        var clip    = AudioClip.Create("DialTone", samples, 1, rate, false);

        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t  = (float)i / rate;
            data[i]  = DialToneAmplitude * (Mathf.Sin(2f * Mathf.PI * DialToneFreq1Hz * t)
                                          + Mathf.Sin(2f * Mathf.PI * DialToneFreq2Hz * t));
        }
        clip.SetData(data, 0);
        return clip;
    }
}
