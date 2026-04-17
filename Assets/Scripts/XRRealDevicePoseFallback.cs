using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
public sealed class XRRealDevicePoseFallback : MonoBehaviour
{
    [SerializeField] private XRNode xrNode = XRNode.CenterEye;
    [SerializeField] private bool applyPosition = true;
    [SerializeField] private bool applyRotation = true;

    private InputDevice cachedDevice;
    private XRNode cachedNode;

    public void Configure(XRNode node, bool position, bool rotation)
    {
        xrNode = node;
        applyPosition = position;
        applyRotation = rotation;
        cachedDevice = default;
        cachedNode = node;
    }

    private void LateUpdate()
    {
        if (XRSceneRuntimeStabilizer.IsSimulatorEnabledForCurrentSession())
        {
            return;
        }

        InputDevice device = GetDevice();
        if (!device.isValid)
        {
            return;
        }

        if (device.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked) && !isTracked)
        {
            return;
        }

        InputTrackingState trackingState = InputTrackingState.Position | InputTrackingState.Rotation;
        if (device.TryGetFeatureValue(CommonUsages.trackingState, out InputTrackingState reportedTrackingState))
        {
            trackingState = reportedTrackingState;
        }

        if (applyPosition
            && (trackingState & InputTrackingState.Position) != 0
            && device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
        {
            transform.localPosition = position;
        }

        if (applyRotation
            && (trackingState & InputTrackingState.Rotation) != 0
            && device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
        {
            transform.localRotation = rotation;
        }
    }

    private InputDevice GetDevice()
    {
        if (cachedDevice.isValid && cachedNode == xrNode)
        {
            return cachedDevice;
        }

        cachedNode = xrNode;
        cachedDevice = InputDevices.GetDeviceAtXRNode(xrNode);

        if (!cachedDevice.isValid && xrNode == XRNode.CenterEye)
        {
            cachedDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        }

        return cachedDevice;
    }
}
