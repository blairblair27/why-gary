using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public struct FingerState
{
    public float thumb;
    public float index;
    public float middle;
    public float ring;
    public float pinky;
}

public class VRFingerTracker : MonoBehaviour
{
    [SerializeField] bool isRightHand = true;

    public FingerState State { get; private set; }

    InputDevice _device;
    readonly List<InputDevice> _deviceBuffer = new List<InputDevice>(2);

    void Update()
    {
        EnsureDevice();
        if (!_device.isValid)
        {
            State = default;
            return;
        }

        _device.TryGetFeatureValue(CommonUsages.grip,          out float grip);
        _device.TryGetFeatureValue(CommonUsages.trigger,       out float trigger);
        _device.TryGetFeatureValue(CommonUsages.indexTouch,    out bool  indexTouch);
        _device.TryGetFeatureValue(CommonUsages.primaryTouch,  out bool  primaryTouch);
        _device.TryGetFeatureValue(CommonUsages.secondaryTouch, out bool secondaryTouch);

        State = new FingerState
        {
            thumb  = (primaryTouch || secondaryTouch) ? 0.6f : 0.1f,
            index  = indexTouch ? trigger : 0f,
            middle = grip,
            ring   = grip,
            pinky  = grip,
        };
    }

    void EnsureDevice()
    {
        if (_device.isValid) return;

        var chars = isRightHand
            ? InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller
            : InputDeviceCharacteristics.Left  | InputDeviceCharacteristics.Controller;

        InputDevices.GetDevicesWithCharacteristics(chars, _deviceBuffer);
        if (_deviceBuffer.Count > 0) _device = _deviceBuffer[0];
    }
}
