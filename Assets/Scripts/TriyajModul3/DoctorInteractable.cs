using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DoctorInteractable : MonoBehaviour
{
    private Camera _playerCamera;
    private XRSimpleInteractable _xrInteractable;
    private AudioSource _doctorAudioSource;

    private void Awake()
    {
        _xrInteractable = GetComponent<XRSimpleInteractable>();
        if (_xrInteractable != null)
        {
            _xrInteractable.selectEntered.RemoveListener(HandleSelectEntered);
            _xrInteractable.selectEntered.AddListener(HandleSelectEntered);
        }

        _doctorAudioSource = EnsureDoctorAudioSource();
    }

    private void OnDestroy()
    {
        if (_xrInteractable != null)
        {
            _xrInteractable.selectEntered.RemoveListener(HandleSelectEntered);
        }
    }

    private void Update()
    {
        if (IsDoctorPanelAlreadyOpen())
        {
            return;
        }

        bool isClicked = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            isClicked = true;
        if (!isClicked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            isClicked = true;
#else
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
            isClicked = true;
#endif

        if (isClicked && TryInteract())
        {
            BaslatYapayZekaSohbeti();
        }
    }

    private bool TryInteract()
    {
        if (_playerCamera == null)
            _playerCamera = XRCameraHelper.GetPlayerCamera();

        if (_playerCamera == null)
            return false;

        Vector2 mousePos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            mousePos = Mouse.current.position.ReadValue();
#else
        mousePos = Input.mousePosition;
#endif

        Ray rayMouse = _playerCamera.ScreenPointToRay(mousePos);
        Ray rayCenter = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);

        foreach (RaycastHit hit in Physics.RaycastAll(rayMouse, 15f))
        {
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                return true;
        }

        foreach (RaycastHit hit in Physics.RaycastAll(rayCenter, 15f))
        {
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                return true;
        }

        return false;
    }

    private void HandleSelectEntered(SelectEnterEventArgs args)
    {
        if (IsDoctorPanelAlreadyOpen())
        {
            return;
        }

        BaslatYapayZekaSohbeti();
    }

    public void BaslatYapayZekaSohbeti()
    {
        var manager = AIManager.Instance;
        if (manager == null)
        {
            manager = FindObjectOfType<AIManager>(true);
        }

        if (manager == null)
        {
            Debug.LogError("[DoctorInteractable] Sahnede aktif veya pasif bir AIManager bulunamadi.");
            return;
        }

        if (manager.IsDoctorPanelOpen())
        {
            return;
        }

        _doctorAudioSource = EnsureDoctorAudioSource();
        manager.RegisterDoctorSpeaker(_doctorAudioSource);
        manager.OpenAICanvas();
    }

    private static bool IsDoctorPanelAlreadyOpen()
    {
        AIManager manager = AIManager.Instance;
        return manager != null && manager.IsDoctorPanelOpen();
    }

    private AudioSource EnsureDoctorAudioSource()
    {
        AudioSource source = GetComponent<AudioSource>();
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = Mathf.Max(0.8f, source.minDistance);
        source.maxDistance = Mathf.Max(12f, source.maxDistance);
        source.dopplerLevel = 0f;

        return source;
    }
}
