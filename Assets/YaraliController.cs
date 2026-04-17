using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using TMPro;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

#pragma warning disable CS0414

/// <summary>
/// Yaralı NPC kontrolcüsü.
/// XRRigAnchorUtility'ye BAĞIMLI DEĞİLDİR — kendi sahneden bulma sistemini kullanır.
/// VR hover + trigger, VR proximity + trigger, ve fare/raycast destekler.
/// </summary>
[RequireComponent(typeof(Collider))]
public class YaraliController : MonoBehaviour
{
    [Header("Back Anchor (boş bırakılırsa otomatik)")]
    [SerializeField] private Transform backAnchor;

    [Header("Pickup Mesafe Eşiği (metre, proximity fallback için)")]
    [SerializeField] private float pickupDistance = 3f;

    // Taşıma & bırakma rotasyonları
    private static readonly Vector3    CARRYPOS = new Vector3(0f, 0.2f, -0.3f);
    private static readonly Quaternion CARRYROT = Quaternion.Euler(90f, 0f, 0f);
    private Collider col;
    
    // BUNU PUBLIC YAPTIK (SafeZone'un rahatça okuması için)
    public bool isCarried = false;
    public bool isPlacedAtYaraliYeri = false;

    [Header("Ilk Mudahale UI")]
    [SerializeField] private string panelTitle = "Depremzede Ilk Mudahale";
    [SerializeField] private string[] firstAidActions =
    {
        "1) Guvenligi sagla, cevreyi kontrol et.",
        "2) Bilinc kontrolu yap ve kendini tanit.",
        "3) 112'yi ara / acil destek iste.",
        "4) Solunum ve nabiz kontrol et.",
        "5) Kanama varsa dogrudan basinc uygula.",
        "6) Supheli kirikta bolgeyi sabitle.",
        "7) Sok bulgusunda ustunu ort ve sakinlestir.",
        "8) Gerekliyse guvenli pozisyona al, gereksiz tasima yapma."
    };

    private static Canvas uiCanvas;
    private static GameObject uiPanel;
    private static Transform uiButtonsRoot;

    [Header("Triage Settings")]
    public TriageCategory ActualTriageState = TriageCategory.Unassigned;
    // Sırt üstü yatış: -90° X → spine yukarı, yüz gökyüzüne.
    // YaralıYeri uzun ekseni X, NPC baş-ayak ekseni de X → Y rotasyonu 0.
    public  static readonly Quaternion DROPROT  = Quaternion.Euler(-90f, 8.576f, -99.4f);
    public TriageCategory AssignedTriageState = TriageCategory.Unassigned;
    public bool isTriaged = false;

    private XRSimpleInteractable xrInteract;
    private bool isHovered = false;

    void OnEnable()
    {
        if (xrInteract == null)
            xrInteract = GetComponent<XRSimpleInteractable>();
        
        if (xrInteract != null)
        {
            // Triage işlemi için Activated (Trigger), selectEntered (Grip) ise Taşıma
            xrInteract.activated.AddListener(OnInteract);
            xrInteract.hoverEntered.AddListener(OnHoverEnter);
            xrInteract.hoverExited.AddListener(OnHoverExit);
        }
    }

    void OnDisable()
    {
        if (xrInteract != null)
        {
            xrInteract.activated.RemoveListener(OnInteract);
            xrInteract.hoverEntered.RemoveListener(OnHoverEnter);
            xrInteract.hoverExited.RemoveListener(OnHoverExit);
        }
    }
    
    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (TriyajManager.Instance != null) 
            TriyajManager.Instance.SetHoveredYarali(this);
    }

    // ------------------------------------------------------------------ Awake
    void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = false;

        xrInteract = GetComponent<XRSimpleInteractable>();
        if (xrInteract != null)
        {
            // Interaction Layer: hepsini aktif et (bitmask = -1 = tüm layer'lar)
            xrInteract.interactionLayers = InteractionLayerMask.GetMask(InteractionLayerMask.LayerToName(0),
                                                                          InteractionLayerMask.LayerToName(1),
                                                                          InteractionLayerMask.LayerToName(2),
                                                                          InteractionLayerMask.LayerToName(3));
            
            xrInteract.hoverEntered.AddListener(e =>
            {
                isHovered = true;
                Debug.Log($"[Yarali] HOVER ↑ by {(e.interactorObject as MonoBehaviour)?.name}");
            });
            xrInteract.hoverExited.AddListener(e =>
            {
                isHovered = false;
            });
            xrInteract.selectEntered.AddListener(e =>
            {
                Debug.Log($"[Yarali] selectEntered (GRIP) by {(e.interactorObject as MonoBehaviour)?.name}. Taşıma işlemi başlatılıyor.");
                Pickup();
            });
        }
        else
        {
            Debug.LogWarning($"[Yarali] {gameObject.name} üzerinde XRSimpleInteractable YOK!");
        }
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        if (TriyajManager.Instance != null) 
            TriyajManager.Instance.ClearHoveredYarali(this);
    }

    private void OnInteract(ActivateEventArgs args)
    {
        if (isPlacedAtYaraliYeri)
        {
            ShowFirstAidPanel();
            return;
        }

        Debug.Log("[YaraliController] VR Trigger ile tiklandi, Triyaj degistiriliyor.");
        CycleTriage();
    }

    void Update()
    {
        if (isCarried)
            return;

        bool isClicked = false;
        Vector2 mousePos = Vector2.zero;

        try
        {
            if (Input.GetMouseButtonDown(0))
            {
                isClicked = true;
                mousePos = Input.mousePosition;
            }
        }
        catch
        {
        }

#if ENABLE_INPUT_SYSTEM
        if (!isClicked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            isClicked = true;
            mousePos = Mouse.current.position.ReadValue();
        }
#endif

        if (!isClicked)
            return;

        Transform cameraTransform = XRRigAnchorUtility.ResolveCameraTransform();
        Camera cam = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;
        if (cam == null)
            return;

        Ray rayMouse = cam.ScreenPointToRay(mousePos);
        Ray rayCenter = new Ray(cam.transform.position, cam.transform.forward);

        bool didHit = false;

        // 1. Önce fare pozisyonu ile dene
        foreach (RaycastHit hit in Physics.RaycastAll(rayMouse, 15f))
        {
            if (hit.collider.gameObject == gameObject || hit.collider == col)
            {
                didHit = true; break;
            }
        }

        // 2. XR simülatör durumları için tam kameranın baktığı yere (Crosshair) göre dene
        if (!didHit)
        {
            foreach (RaycastHit hit in Physics.RaycastAll(rayCenter, 15f))
            {
                if (hit.collider.gameObject == gameObject || hit.collider == col)
                {
                    didHit = true; break;
                }
            }
        }

        if (didHit)
        {
            if (isPlacedAtYaraliYeri)
            {
                ShowFirstAidPanel();
                return;
            }

            Debug.Log("[YaraliController] Tiklama veya Bakis basarili! Triyaj degistiriliyor.");
            CycleTriage();
        }
    }

    public void CycleTriage()
    {
        TriageCategory nextCategory = AssignedTriageState;
        
        switch(nextCategory)
        {
            case TriageCategory.Unassigned: nextCategory = TriageCategory.Green; break;
            case TriageCategory.Green: nextCategory = TriageCategory.Yellow; break;
            case TriageCategory.Yellow: nextCategory = TriageCategory.Red; break;
            case TriageCategory.Red: nextCategory = TriageCategory.Black; break;
            case TriageCategory.Black: nextCategory = TriageCategory.Green; break;
            default: nextCategory = TriageCategory.Green; break;
        }

        if (TriyajManager.Instance != null)
        {
            TriyajManager.Instance.ApplyTriage(this, nextCategory);
        }
        else
        {
            AssignTriage(nextCategory);
        }
    }

    // ------------------------------------------------------------------ Helpers
    private bool IsVRPickupPressed()
    {
#if ENABLE_INPUT_SYSTEM
        foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
        {
            var trig = device.TryGetChildControl<AxisControl>("trigger");
            if (trig != null && trig.ReadValue() > 0.5f) return true;

            var trigBtn = device.TryGetChildControl<ButtonControl>("triggerButton");
            if (trigBtn != null && trigBtn.wasPressedThisFrame) return true;

            var grip = device.TryGetChildControl<AxisControl>("grip");
            if (grip != null && grip.ReadValue() > 0.5f) return true;

            var gripBtn = device.TryGetChildControl<ButtonControl>("gripButton");
            if (gripBtn != null && gripBtn.wasPressedThisFrame) return true;
        }
        foreach (var gp in Gamepad.all)
        {
            if (gp.rightTrigger.wasPressedThisFrame || gp.leftTrigger.wasPressedThisFrame) return true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
#endif
        if (Input.GetMouseButton(0))      return true;
        if (Input.GetButtonDown("Fire1")) return true;
        return false;
    }

    private bool TryMouseRaycast()
    {
        Vector2 mpos = Vector2.zero;
        bool clicked = false;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        { clicked = true; mpos = Mouse.current.position.ReadValue(); }
#endif
        if (!clicked && Input.GetMouseButtonDown(0))
        { clicked = true; mpos = Input.mousePosition; }

        if (!clicked) return false;

        Camera cam = FindCamera();
        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(mpos);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            if (hit.collider.gameObject == gameObject || hit.collider == col)
            {
                Debug.Log("[Yarali] Fare raycast pickup");
                return true;
            }
        }
        return false;
    }

    private Camera FindCamera()
    {
        var xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin != null && xrOrigin.Camera != null)
            return xrOrigin.Camera;

        return Camera.main;
    }

    private Transform FindBackAnchor()
    {
        if (backAnchor != null) return backAnchor;

        var go = GameObject.Find("PlayerBackPoint");
        if (go != null)
        {
            backAnchor = go.transform;
            return backAnchor;
        }

        var xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin != null)
        {
            var child = FindChild(xrOrigin.transform, "PlayerBackPoint");
            if (child != null) { backAnchor = child; return backAnchor; }
        }

        Debug.LogWarning("[Yarali] PlayerBackPoint bulunamadı, XROrigin kök\u00FC kullanılıyor.");
        if (xrOrigin != null)
        {
            backAnchor = xrOrigin.transform;
            return backAnchor;
        }

        return null;
    }

    private static Transform FindChild(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform c in root)
        {
            var f = FindChild(c, name);
            if (f != null) return f;
        }
        return null;
    }

    // ------------------------------------------------------------------ Pickup
    public void Pickup()
    {
        if (isCarried) return;

        if (isPlacedAtYaraliYeri)
        {
            Debug.Log("[Yarali] YaraliYeri'ne birakilan yarali tekrar tasinamaz.");
            return;
        }

        if (RescueManager.Instance != null && !RescueManager.Instance.CanCarry())
        {
            Debug.LogWarning("[Yarali] Sırtınızda zaten bir yaralı var, yeni yaralı alınamaz.");
            return;
        }

        HideExistingFirstAidUI();

        Transform anchor = FindBackAnchor();
        if (anchor == null)
        {
            Debug.LogWarning("[Yarali] Anchor bulunamadı, pickup iptal!");
            return;
        }

        isCarried = true;
        isHovered = false;
        col.isTrigger = true;

        // Rigidbody varsa kısıtlamaları kaldır (taşıma sırasında parent transform yönetir)
        Rigidbody rbPickup = GetComponent<Rigidbody>();
        if (rbPickup != null)
        {
            rbPickup.constraints = RigidbodyConstraints.None;
            rbPickup.isKinematic = true; // parent transform kontrol\u00FCnde
        }

        transform.SetParent(anchor, false);
        transform.localPosition = CARRYPOS;
        transform.localRotation = CARRYROT;

        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = true;

        if (xrInteract != null) xrInteract.enabled = false;

        if (RescueManager.Instance != null)
            RescueManager.Instance.AddCarried();

        Debug.Log($"[Yarali] \u2713 {gameObject.name} taşıma pozuna alındı! Anchor: {anchor.name}");
    }

    public void DropAtSafeZone(Transform safeZoneParent)
    {
        Transform spot = RescueManager.Instance?.GetNextDropSpot();
        if (spot != null) { DropAtSpot(spot); return; }

        transform.SetParent(null);
        Collider zc = safeZoneParent.GetComponent<Collider>();
        Vector3 c  = zc != null ? zc.bounds.center  : safeZoneParent.position;
        float hx   = zc != null ? zc.bounds.extents.x * 0.5f : 1f;
        float hz   = zc != null ? zc.bounds.extents.z * 0.5f : 1f;

        transform.position = new Vector3(
            c.x + Random.Range(-hx, hx), c.y + 0.05f, c.z + Random.Range(-hz, hz));
        transform.rotation = DROPROT; // Ekran görüntüsündeki rotasyon: -90, 8.576, -99.4
        
        isCarried = false;
        isPlacedAtYaraliYeri = true;
        col.isTrigger = false;

        // Rigidbody varsa fiziği dondur
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity        = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic     = true;
            rb.constraints     = RigidbodyConstraints.FreezeAll;
        }
        
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = true;
        if (xrInteract != null) xrInteract.enabled = true;
    }

    public void DropAtSpot(Transform spot)
    {
        if (!isCarried)
            return;

        transform.SetParent(null);
        transform.position = spot.position;
        transform.rotation = DROPROT; // Ekran görüntüsündeki rotasyon: -90, 8.576, -99.4

        isCarried = false;
        isPlacedAtYaraliYeri = true;
        col.isTrigger = false;

        // Rigidbody varsa fiziği dondur - yoksa karakter atak yapıp kalkar
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity        = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic     = true;
            rb.constraints     = RigidbodyConstraints.FreezeAll;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
            renderer.enabled = true;

        if (xrInteract != null) xrInteract.enabled = true;
        Debug.Log($"[Yarali] {gameObject.name} spot noktasına ({spot.name}) devredildi, rotasyon kilitlendi.");
    }

    private void ShowFirstAidPanel()
    {
        if (IlkyardimGlobalMenajer.Instance != null)
        {
            IlkyardimGlobalMenajer.Instance.KarakterYerlestirildi(transform);
            return;
        }

        if (TryShowWorldFirstAidCanvas())
        {
            return;
        }

        EnsureFirstAidUIBuilt();
        if (uiPanel == null)
            return;

        foreach (Transform child in uiButtonsRoot)
            Destroy(child.gameObject);

        for (int i = 0; i < firstAidActions.Length; i++)
        {
            CreateActionButton(firstAidActions[i]);
        }

        uiPanel.SetActive(true);
    }

    private bool TryShowWorldFirstAidCanvas()
    {
        NPCInteraction npcInteraction = GetComponent<NPCInteraction>();
        if (npcInteraction != null && npcInteraction.myWorldCanvas != null)
        {
            npcInteraction.myWorldCanvas.ShowCanvas();
            return true;
        }

        NPCWorldCanvas worldCanvas = GetComponentInChildren<NPCWorldCanvas>(true);
        if (worldCanvas != null)
        {
            worldCanvas.ShowCanvas();
            return true;
        }

        return false;
    }

    private void HideExistingFirstAidUI()
    {
        NPCWorldCanvas worldCanvas = GetComponentInChildren<NPCWorldCanvas>(true);
        if (worldCanvas != null)
        {
            worldCanvas.HideCanvas();
        }

        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }
    }

    private void EnsureFirstAidUIBuilt()
    {
        if (uiPanel != null)
            return;

        if (uiCanvas == null)
        {
            GameObject canvasGo = new GameObject("IlkMudahaleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            uiCanvas = canvasGo.GetComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        uiPanel = new GameObject("IlkMudahalePanel", typeof(Image));
        uiPanel.transform.SetParent(uiCanvas.transform, false);

        RectTransform panelRect = uiPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(840f, 620f);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImage = uiPanel.GetComponent<Image>();
        panelImage.color = new Color(0.06f, 0.1f, 0.14f, 0.95f);

        var titleGO = new GameObject("Title", typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(uiPanel.transform, false);
        var titleText = titleGO.GetComponent<TextMeshProUGUI>();
        titleText.text = panelTitle;
        titleText.fontSize = 40;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.98f, 0.93f, 0.7f, 1f);
        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 70f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);

        var buttonsGO = new GameObject("Buttons", typeof(RectTransform), typeof(VerticalLayoutGroup));
        buttonsGO.transform.SetParent(uiPanel.transform, false);
        uiButtonsRoot = buttonsGO.transform;

        var buttonsRect = buttonsGO.GetComponent<RectTransform>();
        buttonsRect.anchorMin = new Vector2(0f, 0f);
        buttonsRect.anchorMax = new Vector2(1f, 1f);
        buttonsRect.offsetMin = new Vector2(40f, 100f);
        buttonsRect.offsetMax = new Vector2(-40f, -100f);

        var layout = buttonsGO.GetComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 10f;
        layout.padding = new RectOffset(0, 0, 0, 0);

        var closeBtn = CreateRawButton("Kapat", uiPanel.transform, new Color(0.72f, 0.2f, 0.2f, 1f));
        var closeBtnRect = closeBtn.GetComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(0.5f, 0f);
        closeBtnRect.anchorMax = new Vector2(0.5f, 0f);
        closeBtnRect.pivot = new Vector2(0.5f, 0f);
        closeBtnRect.sizeDelta = new Vector2(200f, 56f);
        closeBtnRect.anchoredPosition = new Vector2(0f, 24f);
        closeBtn.onClick.AddListener(() => uiPanel.SetActive(false));

        uiPanel.SetActive(false);
    }

    private void CreateActionButton(string label)
    {
        if (uiButtonsRoot == null)
            return;

        var btn = CreateRawButton(label, uiButtonsRoot, new Color(0.12f, 0.45f, 0.45f, 0.95f));
        var rect = btn.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 54f);
    }

    private Button CreateRawButton(string label, Transform parent, Color bgColor)
    {
        var btnGO = new GameObject("ActionButton", typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);

        var image = btnGO.GetComponent<Image>();
        image.color = bgColor;

        var button = btnGO.GetComponent<Button>();

        var textGO = new GameObject("Label", typeof(TextMeshProUGUI));
        textGO.transform.SetParent(btnGO.transform, false);
        var text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 24;
        text.color = Color.white;

        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 6f);
        textRect.offsetMax = new Vector2(-12f, -6f);

        return button;
    }

    // --- TRIAGE LOGIC ---

    public void AssignTriage(TriageCategory assignedCategory)
    {
        AssignedTriageState = assignedCategory;
        isTriaged = true;

        Debug.Log($"[Triyaj] {gameObject.name} adli yaraliya {assignedCategory} etiketi atandi. (Gercek Durum: {ActualTriageState})");

        YaraliWallhack wallhack = GetComponent<YaraliWallhack>();
        if (wallhack != null)
        {
            wallhack.UpdateWallhackColorByTriage(assignedCategory);
        }
    }
}

#pragma warning restore CS0414