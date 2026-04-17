using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class AxeSnap : MonoBehaviour
{
    [SerializeField] private XRSocketInteractor socket;
    [SerializeField] private float fallbackSnapDistance = 1.2f;
    [SerializeField] private Vector3 snapLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 snapLocalEuler = Vector3.zero;
    [SerializeField] private bool disableGrabAfterSnap = true;
    [SerializeField] private bool enableHeldProximitySnap = true;
    [SerializeField] private bool enableReleasedProximitySnap = true;
    [SerializeField] private float releasedSnapCheckInterval = 0.1f;

    private bool hasTriggered;
    private bool isHeld;
    private bool isSnapping;
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private float nextReleasedSnapCheckTime;
    private bool socketListenersBound;

    private const string DefaultSocketName = "BackEquipPoint";
    private const string EquipmentId = "axe";

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        if (fallbackSnapDistance <= 0f)
        {
            fallbackSnapDistance = 1.2f;
        }

        if (socket == null)
        {
            socket = FindSocketByName(DefaultSocketName);
        }

        if (socket == null)
        {
            Debug.LogWarning("[AxeSnap] Socket reference is missing. Looking for BackEquipPoint at runtime.");
        }
        else
        {
            ValidateInteractionManagerConfiguration();
        }
    }

    private void OnEnable()
    {
        BindSocketListeners();

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnAxeGrabbed);
            grabInteractable.selectExited.AddListener(OnAxeReleased);
        }

        nextReleasedSnapCheckTime = 0f;
    }

    private void OnDisable()
    {
        UnbindSocketListeners();

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnAxeGrabbed);
            grabInteractable.selectExited.RemoveListener(OnAxeReleased);
        }
    }

    private void Update()
    {
        if (hasTriggered || isSnapping)
        {
            return;
        }

        if (socket == null)
        {
            socket = FindSocketByName(DefaultSocketName);
            if (socket != null)
            {
                BindSocketListeners();
                Debug.Log("[AxeSnap] Socket was reacquired by runtime lookup.");
                ValidateInteractionManagerConfiguration();
            }

            return;
        }

        if (IsSelectedBySocket())
        {
            CompleteAndNotify("socket-poll");
            return;
        }

        if (isHeld && enableHeldProximitySnap)
        {
            TryProximitySnap("held-proximity");
            return;
        }

        if (!isHeld && enableReleasedProximitySnap && Time.time >= nextReleasedSnapCheckTime)
        {
            nextReleasedSnapCheckTime = Time.time + Mathf.Max(0.02f, releasedSnapCheckInterval);
            TryProximitySnap("released-proximity");
        }
    }

    private void OnAxeGrabbed(SelectEnterEventArgs args)
    {
        if (hasTriggered)
        {
            return;
        }

        isHeld = true;
        nextReleasedSnapCheckTime = 0f;
    }

    private void OnAxeReleased(SelectExitEventArgs args)
    {
        isHeld = false;
        nextReleasedSnapCheckTime = 0f;

        if (hasTriggered || isSnapping)
        {
            return;
        }

        TryProximitySnap("grab-select-exited");
    }

    private void OnSocketSelectEntered(SelectEnterEventArgs args)
    {
        if (hasTriggered || isSnapping)
        {
            return;
        }

        if (!IsTargetAxe(args.interactableObject))
        {
            return;
        }

        CompleteAndNotify("socket-select-entered");
    }

    private void OnSocketSelectExited(SelectExitEventArgs args)
    {
        if (hasTriggered || isSnapping)
        {
            return;
        }

        if (!IsTargetAxe(args.interactableObject))
        {
            return;
        }

        isHeld = false;
        TryProximitySnap("socket-select-exited");
    }

    private static XRSocketInteractor FindSocketByName(string objectName)
    {
        var sockets = FindObjectsOfType<XRSocketInteractor>(true);
        for (var i = 0; i < sockets.Length; i++)
        {
            if (sockets[i] != null && sockets[i].name == objectName)
            {
                return sockets[i];
            }
        }

        return null;
    }

    private bool IsTargetAxe(IXRSelectInteractable interactableObject)
    {
        if (interactableObject == null)
        {
            return false;
        }

        if (grabInteractable != null && ReferenceEquals(interactableObject, grabInteractable))
        {
            return true;
        }

        return ReferenceEquals(interactableObject.transform, transform);
    }

    private bool IsSelectedBySocket()
    {
        if (socket == null || grabInteractable == null || !socket.hasSelection)
        {
            return false;
        }

        var selectedInteractables = socket.interactablesSelected;
        for (var i = 0; i < selectedInteractables.Count; i++)
        {
            if (ReferenceEquals(selectedInteractables[i], grabInteractable))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryProximitySnap(string reason)
    {
        if (hasTriggered || isSnapping || socket == null)
        {
            return false;
        }

        var target = socket.attachTransform != null ? socket.attachTransform : socket.transform;
        if (target == null)
        {
            return false;
        }

        var sqrDistance = (transform.position - target.position).sqrMagnitude;
        if (sqrDistance > fallbackSnapDistance * fallbackSnapDistance)
        {
            return false;
        }

        SnapToTarget(target, reason);
        return true;
    }

    private void SnapToTarget(Transform target, string reason)
    {
        if (target == null || hasTriggered || isSnapping)
        {
            return;
        }

        isSnapping = true;

        try
        {
            ForceReleaseFromCurrentInteractor();

            isHeld = false;

            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            transform.SetParent(target, false);
            transform.localPosition = snapLocalPosition;
            transform.localRotation = Quaternion.Euler(snapLocalEuler);

            if (disableGrabAfterSnap && grabInteractable != null)
            {
                grabInteractable.enabled = false;
            }

            CompleteAndNotify(reason);
        }
        finally
        {
            isSnapping = false;
        }
    }

    private void ForceReleaseFromCurrentInteractor()
    {
        if (grabInteractable == null || !grabInteractable.isSelected)
        {
            return;
        }

        var selectingInteractor = grabInteractable.firstInteractorSelecting;
        var interactionManager = grabInteractable.interactionManager;
        if (interactionManager == null && socket != null)
        {
            interactionManager = socket.interactionManager;
        }

        if (selectingInteractor != null && interactionManager != null)
        {
            interactionManager.SelectExit(selectingInteractor, grabInteractable);
            return;
        }

        Debug.LogWarning("[AxeSnap] Could not force-release axe before manual snap.");
    }

    private void CompleteAndNotify(string reason)
    {
        if (hasTriggered)
        {
            return;
        }

        hasTriggered = true;
        Debug.Log("[AxeSnap] Axe equipped (" + reason + ").");
        NotifyTaskManager(reason);
    }

    private void NotifyTaskManager(string reason)
    {
        var taskManager = TaskManager.Instance;
        if (taskManager == null)
        {
            Debug.LogWarning("[AxeSnap] TaskManager.Instance is null. Axe equip counted locally only (" + reason + ").");
            return;
        }

        taskManager.OnEquipmentEquipped(EquipmentId);
        Debug.Log("[AxeSnap] TaskManager notified for axe equip (" + reason + ").");
    }

    private void ValidateInteractionManagerConfiguration()
    {
        if (socket == null || grabInteractable == null)
        {
            return;
        }

        var socketManager = socket.interactionManager;
        var grabManager = grabInteractable.interactionManager;
        if (socketManager != null && grabManager != null && socketManager != grabManager)
        {
            Debug.LogWarning("[AxeSnap] Socket and axe use different XR Interaction Managers. This can break equip events.");
        }
    }

    private void BindSocketListeners()
    {
        if (socket == null || socketListenersBound)
        {
            return;
        }

        socket.selectEntered.AddListener(OnSocketSelectEntered);
        socket.selectExited.AddListener(OnSocketSelectExited);
        socketListenersBound = true;
    }

    private void UnbindSocketListeners()
    {
        if (socket == null || !socketListenersBound)
        {
            return;
        }

        socket.selectEntered.RemoveListener(OnSocketSelectEntered);
        socket.selectExited.RemoveListener(OnSocketSelectExited);
        socketListenersBound = false;
    }
}
