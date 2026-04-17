using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Girişte bekleyen veya hastaneye geçiş onayı veren objeye/NPC'ye atanması gereken script.
/// </summary>
/// <summary>
/// Girişte bekleyen NPC'ye (örneğin sedyedeki) atanması gereken script.
/// Oyuncu etkileşime girdiğinde hastane sahnesine geçiş yaparız.
/// </summary>
public class EntranceNPCInteractable : MonoBehaviour
{
    [Header("Hedef Işınlanma Noktası")]
    [Tooltip("Hastanenin girişine boş bir obje koyup buraya sürükle!")]
    public Transform hedefHastaneNoktasi;
    [Header("Geçiş UI Menüsü")]
    [Tooltip("Etkileşime girince 'Gitmek istiyor musun?' menüsü çıkması için OnayMenusuCanvas'ı buraya sürükle.")]
    public GameObject onayMenusuCanvas;

    private Camera _playerCamera;

    void Awake()
    {
        var xrInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.XRSimpleInteractable>();
        if (xrInteractable != null)
        {
            xrInteractable.selectEntered.AddListener((args) => BaslatGecisOlayi());
        }

        if (onayMenusuCanvas != null && onayMenusuCanvas.activeSelf)
        {
            onayMenusuCanvas.SetActive(false);
        }
    }

    void Update()
    {
        bool isClicked = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
                isClicked = true;
        }
        if (!isClicked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            isClicked = true;
#else
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
            isClicked = true;
#endif

        if (isClicked)
        {
            if (TryInteract())
            {
                BaslatGecisOlayi();
            }
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

    public void BaslatGecisOlayi()
    {
        Debug.Log("Hastane NPC'si ile etkileşime geçildi.");
        
        if (onayMenusuCanvas != null)
        {
            var player = XRCameraHelper.GetXROrigin();
            
            if (player != null && player.Camera != null)
            {
                Transform playerCam = player.Camera.transform;
                onayMenusuCanvas.transform.position = playerCam.position + playerCam.forward * 1.5f;
                onayMenusuCanvas.transform.LookAt(playerCam);
                onayMenusuCanvas.transform.Rotate(0, 180, 0);
            }
            else
            {
                onayMenusuCanvas.transform.position = transform.position + transform.forward * 1.2f + Vector3.up * 1.5f;
                onayMenusuCanvas.transform.rotation = transform.rotation;
            }

            // Force a clean reopen so menu OnEnable gates stale held inputs.
            if (onayMenusuCanvas.activeSelf)
            {
                onayMenusuCanvas.SetActive(false);
            }

            onayMenusuCanvas.SetActive(true);
        }
        else
        {
            // Gidecek bir Canvas yoksa direkt Hastaneye ışınla (Test aşaması için iyi pratik)
            GoToHospital();
        }
    }

    public void GoToHospital()
    {
        if (hedefHastaneNoktasi != null)
        {
            Debug.Log("Aynı sahne içindeki Eğitim Alanına (Hastane) ışınlanıyor...");
            Unity.XR.CoreUtils.XROrigin player = XRCameraHelper.GetXROrigin();
            if (player != null)
            {
                Vector3 hedefPos = hedefHastaneNoktasi.position;
                Quaternion hedefRot = hedefHastaneNoktasi.rotation;

                if (Physics.Raycast(hedefPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore))
                {
                    hedefPos.y = hit.point.y;
                    Debug.Log($"Raycast ile yercekimi ayarlandı. Y: {hit.point.y}");
                }

                bool teleported = VRSpawnPoint.TryRespawnPlayerRigRoot(this, hedefPos, hedefRot.eulerAngles.y, 4);
                if (!teleported)
                {
                    Vector3 safeFallbackPos = VRSpawnPoint.ResolveCameraTargetAboveGround(hedefPos);
                    CharacterController fallbackController = player.GetComponent<CharacterController>();
                    if (fallbackController != null
                        && Physics.Raycast(
                            safeFallbackPos + Vector3.up * 4f,
                            Vector3.down,
                            out RaycastHit fallbackHit,
                            8f,
                            ~0,
                            QueryTriggerInteraction.Ignore))
                    {
                        float feetBelowOrigin = Mathf.Max(0f, (fallbackController.height * 0.5f) - fallbackController.center.y);
                        safeFallbackPos.y = Mathf.Max(safeFallbackPos.y, fallbackHit.point.y + feetBelowOrigin + 0.05f);
                    }

                    bool restoreController = fallbackController != null && fallbackController.enabled;
                    try
                    {
                        if (restoreController)
                        {
                            fallbackController.enabled = false;
                        }

                        player.transform.position = safeFallbackPos;
                        player.transform.rotation = Quaternion.Euler(0f, hedefRot.eulerAngles.y, 0f);
                    }
                    finally
                    {
                        if (restoreController && fallbackController != null)
                        {
                            fallbackController.enabled = true;
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("XROrigin bulunamadı!");
            }
        }
        else
        {
            Debug.LogError("Lütfen Inspector üzerinden 'Hedef Hastane Noktasi' kısmına bir transform/obje sürükleyin!");
        }
    }
}
