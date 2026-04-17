using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

public class ModulePortal : MonoBehaviour
{
    [Header("Portal Settings")]
    public string nextSceneName = "Modul2_Guvenlik";
    public bool isLocked = false;

    private XRSimpleInteractable interactable;
    private bool isLoading;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable == null)
        {
            interactable = gameObject.AddComponent<XRSimpleInteractable>();
        }
    }

    private void OnEnable()
    {
        interactable.selectEntered.AddListener(OnPortalTriggered);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnPortalTriggered);
    }

    private void OnPortalTriggered(SelectEnterEventArgs args)
    {
        if (isLocked)
        {
            Debug.Log("Portal is locked!");
            return;
        }

        LoadNextModule();
    }

    public void LoadNextModule()
    {
        if (isLoading)
        {
            return;
        }

        isLoading = true;
        Debug.Log($"Loading Next Module: {nextSceneName}");
        XRSceneRuntimeStabilizer.PrepareForSceneTransition();
        XRCameraHelper.ClearCache();
        SceneManager.LoadScene(nextSceneName);
    }

    // Example trigger for physical entry
    private void OnTriggerEnter(Collider other)
    {
        if (isLocked)
            return;

        if (other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null
            || other.GetComponentInParent<PlayerPresence>() != null
            || other.GetComponentInChildren<Camera>() != null)
        {
            LoadNextModule();
        }
    }
}
