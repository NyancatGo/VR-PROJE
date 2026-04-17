using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public enum EquipmentType { Head, Hand, Back }

public class EquipmentManager : MonoBehaviour
{
    [Header("Configuration")]
    public EquipmentType type;
    public Vector3 wearPositionOffset;
    public Vector3 wearRotationOffset;
    public float snapDistance = 0.2f;

    [Header("XR Anchors")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Transform headAnchor;
    [SerializeField] private Transform backAnchor;

    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        ResolveAnchors();
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
            grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
            grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        CheckIfNearWearZone();
    }

    private void CheckIfNearWearZone()
    {
        var targetZone = GetTargetZone();
        if (targetZone == null)
            return;

        var distance = Vector3.Distance(transform.position, targetZone.position);
        if (distance <= snapDistance)
            Wear(targetZone);
    }

    private Transform GetTargetZone()
    {
        ResolveAnchors();

        switch (type)
        {
            case EquipmentType.Head:
                return headAnchor;
            case EquipmentType.Hand:
                return null;
            case EquipmentType.Back:
                return backAnchor != null ? backAnchor : playerRoot;
            default:
                return null;
        }
    }

    private void Wear(Transform parent)
    {
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        transform.SetParent(parent);
        transform.localPosition = wearPositionOffset;
        transform.localRotation = Quaternion.Euler(wearRotationOffset);

        Debug.Log($"{gameObject.name} is now worn on {parent.name}");
    }

    private void ResolveAnchors()
    {
        playerRoot = XRRigAnchorUtility.ResolveOriginTransform(playerRoot);
        headAnchor = XRRigAnchorUtility.ResolveCameraTransform(headAnchor);
        backAnchor = XRRigAnchorUtility.ResolveBackAnchor(backAnchor);
    }
}

internal static class XRRigAnchorUtility
{
    internal static XROrigin ResolveOrigin(XROrigin explicitOrigin = null)
    {
        if (explicitOrigin != null)
            return explicitOrigin;

        return XRCameraHelper.GetXROrigin();
    }

    internal static Transform ResolveOriginTransform(Transform explicitRoot = null)
    {
        if (explicitRoot != null)
            return explicitRoot;

        return ResolveOrigin()?.transform;
    }

    internal static Transform ResolveCameraTransform(Transform explicitCamera = null)
    {
        if (explicitCamera != null)
            return explicitCamera;

        var origin = ResolveOrigin();
        if (origin != null && origin.Camera != null)
            return origin.Camera.transform;

        return XRCameraHelper.GetPlayerCameraTransform();
    }

    internal static Transform ResolveBackAnchor(Transform explicitBackAnchor = null)
    {
        if (explicitBackAnchor != null)
            return explicitBackAnchor;

        var origin = ResolveOrigin();
        if (origin == null)
            return null;

        var backPoint = FindNamedChild(origin.transform, "PlayerBackPoint");
        if (backPoint != null)
            return backPoint;

        var cameraTransform = ResolveCameraTransform();
        return cameraTransform != null ? FindNamedChild(cameraTransform, "PlayerBackPoint") : null;
    }

    internal static Transform ResolvePlayerPresence(Transform explicitPresence = null)
    {
        if (explicitPresence != null)
            return explicitPresence;

        var origin = ResolveOrigin();
        if (origin != null)
        {
            var presence = FindNamedChild(origin.transform, "PlayerPresence");
            if (presence != null)
                return presence;
        }

        var taggedPlayer = GameObject.FindWithTag("Player");
        return taggedPlayer != null ? taggedPlayer.transform : null;
    }

    private static Transform FindNamedChild(Transform root, string childName)
    {
        if (root == null)
            return null;

        var children = root.GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
                return children[i];
        }

        return null;
    }
}

[DefaultExecutionOrder(-500)]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerPresence : MonoBehaviour
{
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private float radius = 0.2f;
    [SerializeField] private float minHeight = 1f;
    [SerializeField] private float maxHeight = 2.2f;

    private CapsuleCollider capsule;
    private bool originSearched = false;

    private void Reset()
    {
        capsule = GetComponent<CapsuleCollider>();
        capsule.isTrigger = true;
        capsule.direction = 1;
        capsule.radius = radius;
    }

    private void Awake()
    {
        capsule = GetComponent<CapsuleCollider>();
        capsule.isTrigger = true;
        capsule.direction = 1;

        if (xrOrigin == null)
            xrOrigin = GetComponentInParent<XROrigin>();
    }

    private void LateUpdate()
    {
        if (xrOrigin == null && !originSearched)
        {
            xrOrigin = GetComponentInParent<XROrigin>();
            if (xrOrigin == null) xrOrigin = XRCameraHelper.GetXROrigin();
            originSearched = true;
        }

        if (xrOrigin == null || xrOrigin.Camera == null)
            return;

        var cameraLocalPosition = xrOrigin.CameraInOriginSpacePos;
        transform.localPosition = new Vector3(cameraLocalPosition.x, 0f, cameraLocalPosition.z);

        var height = Mathf.Clamp(xrOrigin.CameraInOriginSpaceHeight, minHeight, maxHeight);
        capsule.radius = radius;
        capsule.height = Mathf.Max(height, radius * 2f);
        capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);
    }
}
