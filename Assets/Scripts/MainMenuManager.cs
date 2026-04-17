using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TrainingAnalytics;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;

    [Header("Data")]
    public List<ModuleData> modules = new List<ModuleData>();
    private int selectedModuleIndex = -1;

    [Header("Debug")]
    [Tooltip("Tiklenirse kayıtlı kullanıcı olsa bile login ekranı zorla gösterilir.")]
    [SerializeField] private bool forceShowLogin;

    [Header("UI Panels")]
    public CanvasGroup loginPanel;
    public CanvasGroup selectionPanel;
    public CanvasGroup contentHubPanel;
    public CanvasGroup slidePanel;
    public CanvasGroup videoPanel;
    public CanvasGroup infographicPanel;

    [Header("Hub UI Elements")]
    public TextMeshProUGUI hubTitle;
    public TextMeshProUGUI slideCounterText;
    public Image slideDisplay;
    public Image infographicDisplay;
    // Note: VideoPlayer would be added separately in the scene
    
    private int currentSlideIndex = 0;
    private LoginPanelController loginController;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private GameObject existingFuturisticPanel;
    private CanvasGroup futuristicCanvasGroup;

    private void Start()
    {
        // Orijinal mevcut arayüzü bul (FuturisticPanel)
        existingFuturisticPanel = GameObject.Find("sinif/UI_Canvas/FuturisticPanel");
        if (existingFuturisticPanel != null)
        {
            // Unity'nin UI sisteminin çökmemesi için (SIGSEGV hatası) GameObeject'i kapatmak yerine
            // CanvasGroup ile şeffaf ve tıklanamaz hale getiriyoruz. 
            // Böylece ContentLoader.cs vb. arkada çalışmaya devam edebilir.
            futuristicCanvasGroup = existingFuturisticPanel.GetComponent<CanvasGroup>();
            if (futuristicCanvasGroup == null) 
            {
                futuristicCanvasGroup = existingFuturisticPanel.AddComponent<CanvasGroup>();
            }
            
            futuristicCanvasGroup.alpha = 0f;
            futuristicCanvasGroup.interactable = false;
            futuristicCanvasGroup.blocksRaycasts = false;
        }

        HidePanel(selectionPanel);
        HidePanel(contentHubPanel);

        loginController = loginPanel != null ? loginPanel.GetComponent<LoginPanelController>() : null;
        if (loginController != null)
        {
            loginController.OnLoginCompleted += OnLoginCompleted;
        }

        if (ParticipantManager.HasParticipant && !forceShowLogin)
        {
            // Daha önce kayıt yapılmış — login ekranını atla
            HidePanel(loginPanel);
            if (futuristicCanvasGroup != null)
            {
                futuristicCanvasGroup.alpha = 1f;
                futuristicCanvasGroup.interactable = true;
                futuristicCanvasGroup.blocksRaycasts = true;
            }
            // Analytics bağlamını geri yükle
            AnalyticsService service = AnalyticsService.Instance;
            if (service != null)
            {
                service.SetParticipantContext(
                    ParticipantManager.GetParticipantKey(),
                    ParticipantManager.GetParticipantName());
            }
        }
        else
        {
            // Login ekranını en öne getir ve göster
            if (loginPanel != null)
            {
                loginPanel.transform.SetAsLastSibling();
            }
            ShowPanel(loginPanel);
        }
    }

    private void OnLoginCompleted()
    {
        HidePanel(loginPanel);
        
        // Asıl kullandığın mevcut UI'yi geri açıyoruz.
        if (futuristicCanvasGroup != null)
        {
            futuristicCanvasGroup.alpha = 1f;
            futuristicCanvasGroup.interactable = true;
            futuristicCanvasGroup.blocksRaycasts = true;
        }
    }

    #region Navigation
    
    public void SelectModule(int index)
    {
        if (index < 0 || index >= modules.Count) return;
        selectedModuleIndex = index;
    }

    public void BackToSelection()
    {
        // Eski sisteme geri dönüyoruz
    }

    public void TriggerLocalReportExport()
    {
        AnalyticsService.EnsureInitializedSingleton().ExportLocalReport();
    }

    public void OpenReportsFolder()
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, "Reports");
        if (!System.IO.Directory.Exists(path))
        {
            System.IO.Directory.CreateDirectory(path);
        }
        
        Debug.Log($"[Analytics] Raporlar Klasörü: {path}");

        // Windows için en güvenli klasör açma yöntemi (explorer.exe kullanır)
        string winPath = path.Replace("/", "\\");
        System.Diagnostics.Process.Start("explorer.exe", winPath);
    }

    public void LaunchModule()
    {
        if (selectedModuleIndex == -1) return;

        if (!ParticipantManager.HasParticipant)
        {
            loginController?.ShowWarning("Lütfen önce adınızı ve soyadınızı kaydedin.");
            if (loginPanel != null) loginPanel.transform.SetAsLastSibling();
            ShowPanel(loginPanel);
            return;
        }

        XRSceneRuntimeStabilizer.PrepareForSceneTransition();
        XRCameraHelper.ClearCache();
        SceneManager.LoadScene(modules[selectedModuleIndex].sceneName);
    }

    public void ShowSlides() => SwitchSubPanel(slidePanel);
    public void ShowVideo() => SwitchSubPanel(videoPanel);
    public void ShowInfographic() => SwitchSubPanel(infographicPanel);

    #endregion

    #region Slide Logic

    public void NextSlide()
    {
        var activeMod = modules[selectedModuleIndex];
        if (activeMod.slides.Count == 0) return;
        
        currentSlideIndex = (currentSlideIndex + 1) % activeMod.slides.Count;
        UpdateSlideUI();
    }

    public void PrevSlide()
    {
        var activeMod = modules[selectedModuleIndex];
        if (activeMod.slides.Count == 0) return;
        
        currentSlideIndex--;
        if (currentSlideIndex < 0) currentSlideIndex = activeMod.slides.Count - 1;
        UpdateSlideUI();
    }

    private void UpdateSlideUI()
    {
        var activeMod = modules[selectedModuleIndex];
        if (activeMod.slides.Count > 0)
        {
            slideDisplay.sprite = activeMod.slides[currentSlideIndex];
            slideCounterText.text = $"{currentSlideIndex + 1} / {activeMod.slides.Count}";
        }
    }

    #endregion

    #region Internal Helpers

    private void UpdateHubContent()
    {
        var activeMod = modules[selectedModuleIndex];
        hubTitle.text = activeMod.moduleTitle;
        currentSlideIndex = 0;
        UpdateSlideUI();
        
        if (activeMod.infographic != null)
            infographicDisplay.sprite = activeMod.infographic;
            
        ShowSlides(); // Default to slides
    }

    private void SwitchToPanel(CanvasGroup target)
    {
        HidePanel(loginPanel);
        HidePanel(selectionPanel);
        HidePanel(contentHubPanel);

        ShowPanel(target);
    }

    private void SwitchSubPanel(CanvasGroup target)
    {
        slidePanel.alpha = 0; slidePanel.blocksRaycasts = false;
        videoPanel.alpha = 0; videoPanel.blocksRaycasts = false;
        infographicPanel.alpha = 0; infographicPanel.blocksRaycasts = false;
        
        ShowPanel(target);
    }

    private void ShowPanel(CanvasGroup panel)
    {
        if (panel == null) return;
        panel.alpha = 1;
        panel.blocksRaycasts = true;
    }

    private void HidePanel(CanvasGroup panel)
    {
        if (panel == null) return;
        panel.alpha = 0;
        panel.blocksRaycasts = false;
    }

    #endregion

    #region Auto Setup
    
    [ContextMenu("Setup Main Menu UI")]
    public void SetupUI()
    {
#if UNITY_EDITOR
        // 1. Create/Find Canvas (DOĞRUDAN DOĞRU CANVAS'I BUL)
        Canvas canvas = null;
        GameObject targetCanvasObj = GameObject.Find("sinif/UI_Canvas");
        if (targetCanvasObj != null)
        {
            canvas = targetCanvasObj.GetComponent<Canvas>();
        }
        else
        {
            canvas = FindObjectOfType<Canvas>(); // Fallback
        }

        if (canvas == null)
        {
            GameObject cvObj = new GameObject("MainMenuCanvas");
            canvas = cvObj.AddComponent<Canvas>();
        }

        RectTransform root = canvas.GetComponent<RectTransform>();
        
        // Eğer hedef UI_Canvas ise onun orijinal pozisyonunu BOZMAMAK için pozisyon atamasını kapattık.
        // Asıl tahta zaten yerli yerinde duruyor. 
        if (targetCanvasObj == null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            root.position = new Vector3(70.5f, 1.4f, 20.8f);
            root.rotation = Quaternion.Euler(0, 0, 0); 
            root.localScale = new Vector3(0.002f, 0.002f, 0.002f);
            root.sizeDelta = new Vector2(1920, 1080);
        }

        if (canvas.gameObject.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        var xrRaycaster = canvas.gameObject.GetComponent("TrackedDeviceGraphicRaycaster");
        if (xrRaycaster == null)
        {
            canvas.gameObject.AddComponent(System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit") ?? typeof(GraphicRaycaster));
        }

        // ESKİLERİ TEMİZLE Kİ ÜST ÜSTE BİNMESİN!
        Transform oldLogin = root.Find("LoginPanel");
        if (oldLogin != null) DestroyImmediate(oldLogin.gameObject);
        
        Transform oldHub = root.Find("ContentHubPanel");
        if (oldHub != null) DestroyImmediate(oldHub.gameObject);

        Transform oldSel = root.Find("SelectionPanel");
        if (oldSel != null) DestroyImmediate(oldSel.gameObject);

        // ═══════════════════════════════════════════════════════════════
        // 2. LOGIN PANEL
        // ═══════════════════════════════════════════════════════════════
        loginPanel = CreatePanel(root, "LoginPanel", new Color(0.04f, 0.06f, 0.10f, 0.97f));

        // ── CARD (960 × 500, upper-centre of canvas) ──────────────────
        // Canvas = 1920×1080. Card top ≈ y=460, bottom ≈ y=-40.
        // Keyboard host is 380px tall at the very bottom (y=-540 … y=-160).
        // Gap between card bottom (-40) and keyboard top (-160) = 120px.  ✓
        GameObject cardObj = new GameObject("LoginCard");
        cardObj.transform.SetParent(loginPanel.transform, false);
        Image cardImg = cardObj.AddComponent<Image>();
        cardImg.color = new Color(0.07f, 0.10f, 0.15f, 1f);
        Outline cardOutline = cardObj.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.06f, 0.82f, 1f, 0.70f);
        cardOutline.effectDistance = new Vector2(4f, -4f);
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(960f, 500f);
        cardRect.anchoredPosition = new Vector2(0f, 210f);

        // ── Header strip (top 85px of card) ─────────────────────────
        GameObject hdrObj = new GameObject("CardHeader");
        hdrObj.transform.SetParent(cardObj.transform, false);
        Image hdrImg = hdrObj.AddComponent<Image>();
        hdrImg.color = new Color(0.04f, 0.06f, 0.11f, 1f);
        RectTransform hdrRect = hdrObj.GetComponent<RectTransform>();
        hdrRect.anchorMin = new Vector2(0f, 1f); hdrRect.anchorMax = new Vector2(1f, 1f);
        hdrRect.pivot     = new Vector2(0.5f, 1f);
        hdrRect.offsetMin = Vector2.zero; hdrRect.offsetMax = Vector2.zero;
        hdrRect.sizeDelta = new Vector2(0f, 85f);

        // Cyan top-border inside header
        GameObject topBar = new GameObject("TopBar");
        topBar.transform.SetParent(hdrObj.transform, false);
        topBar.AddComponent<Image>().color = new Color(0.06f, 0.82f, 1f, 1f);
        RectTransform tbRect = topBar.GetComponent<RectTransform>();
        tbRect.anchorMin = new Vector2(0f, 1f); tbRect.anchorMax = new Vector2(1f, 1f);
        tbRect.pivot = new Vector2(0.5f, 1f);
        tbRect.offsetMin = Vector2.zero; tbRect.offsetMax = Vector2.zero;
        tbRect.sizeDelta = new Vector2(0f, 5f);

        // Title text centred in header
        var hdrTitle = CreateText(hdrObj.transform, "Title", "KATILIMCI KAYIT", 38, new Vector2(0f, -40f));
        hdrTitle.color = new Color(0.06f, 0.88f, 1f, 1f);

        // Subtle header-bottom line
        GameObject hdrLine = new GameObject("HeaderLine");
        hdrLine.transform.SetParent(cardObj.transform, false);
        hdrLine.AddComponent<Image>().color = new Color(0.06f, 0.82f, 1f, 0.22f);
        RectTransform hlRect = hdrLine.GetComponent<RectTransform>();
        hlRect.anchorMin = new Vector2(0f, 1f); hlRect.anchorMax = new Vector2(1f, 1f);
        hlRect.pivot = new Vector2(0.5f, 1f);
        hlRect.anchoredPosition = new Vector2(0f, -85f);
        hlRect.offsetMin = Vector2.zero; hlRect.offsetMax = Vector2.zero;
        hlRect.sizeDelta = new Vector2(0f, 1f);

        // Subtitle
        var subTxt = CreateText(cardObj.transform, "Subtitle",
            "Lütfen adınızı ve soyadınızı giriniz.", 20, new Vector2(0f, 115f));
        subTxt.color = new Color(0.50f, 0.65f, 0.78f, 1f);

        // Input fields (840 px wide inside 960 px card → 60 px margin each side)
        TMP_InputField adInput    = BuildLoginInputField(cardObj.transform, "Ad_InputField",    "Ad",    new Vector2(0f,  38f));
        TMP_InputField soyadInput = BuildLoginInputField(cardObj.transform, "Soyad_InputField", "Soyad", new Vector2(0f, -58f));
        adInput   .GetComponent<RectTransform>().sizeDelta = new Vector2(840f, 68f);
        soyadInput.GetComponent<RectTransform>().sizeDelta = new Vector2(840f, 68f);

        // DEVAM ET button
        Button devamBtn = CreateButton(cardObj.transform, "DevamButton", "DEVAM ET",
            new Vector2(0f, -172f), new Vector2(380f, 72f));
        devamBtn.image.color = new Color(0.04f, 0.68f, 0.18f, 1f);
        var devamLabel = devamBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (devamLabel != null) { devamLabel.fontSize = 26; devamLabel.fontStyle = FontStyles.Bold; }

        // ── REPORT FOLDER BUTTON (on login screen) ──
        Button openFolderBtnLogin = CreateButton(cardObj.transform, "OpenFolderButtonLogin", "KLASÖRÜ AÇ",
            new Vector2(380f, -172f), new Vector2(180f, 72f));
        openFolderBtnLogin.image.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        openFolderBtnLogin.onClick.AddListener(OpenReportsFolder);

        // ── KEYBOARD HOST (fixed 1200 × 380px, bottom-centre) ────────
        //
        // KEY FIX: AIChatCanvasLayout keyboard panel anchors assume a 1070×710
        // canvas. In a stretch rect the keyboard becomes 1821px wide × 117px
        // tall (completely squished). Using a fixed-size host of 1200×380 and
        // overriding the panel/overlay anchors gives correct proportions.
        //
        // Host occupies canvas Y = -540 … -160 (380 px, bottom-centre).
        // Dismiss overlay is clamped to the top 18% of the host (≈ 68px strip)
        // which stays below Y = -160 + 68 = -92, well below all card elements. ✓
        GameObject kbHostObj = new GameObject("KeyboardHost");
        kbHostObj.transform.SetParent(loginPanel.transform, false);
        RectTransform kbHostRect = kbHostObj.AddComponent<RectTransform>();
        kbHostRect.anchorMin = new Vector2(0.5f, 0f);
        kbHostRect.anchorMax = new Vector2(0.5f, 0f);
        kbHostRect.pivot     = new Vector2(0.5f, 0f);
        kbHostRect.sizeDelta = new Vector2(1200f, 380f);
        kbHostRect.anchoredPosition = new Vector2(0f, 14f);

        VRKeyboardManager loginKeyboard = loginPanel.gameObject.AddComponent<VRKeyboardManager>();
        RectTransform loginDrawer  = VRKeyboardManager.EnsureKeyboardDrawer(kbHostRect, out CanvasGroup loginDrawerCg);
        Button        loginOverlay = VRKeyboardManager.EnsureDismissOverlay(loginDrawer);
        RectTransform loginKbPanel = VRKeyboardManager.EnsureKeyboardPanel(loginDrawer);
        VRKeyboardManager.EnsureKeyboardRows(loginKbPanel, loginKeyboard);

        // Override keyboard panel to fill 82% of the host height
        // (default AIChatCanvasLayout anchor is only 27% → squished at 1200px wide)
        loginKbPanel.anchorMin = new Vector2(0.01f, 0.01f);
        loginKbPanel.anchorMax = new Vector2(0.99f, 0.82f);
        loginKbPanel.offsetMin = Vector2.zero;
        loginKbPanel.offsetMax = Vector2.zero;

        // Shrink dismiss overlay to the top 18% strip so it never blocks inputs
        var kbOverlayRect = loginOverlay.GetComponent<RectTransform>();
        kbOverlayRect.anchorMin = new Vector2(0f, 0.82f);
        kbOverlayRect.anchorMax = new Vector2(1f, 1f);
        kbOverlayRect.offsetMin = Vector2.zero;
        kbOverlayRect.offsetMax = Vector2.zero;

        loginKeyboard.Configure(adInput, devamBtn, loginDrawer, loginDrawerCg, loginOverlay, loginKbPanel);
        loginKeyboard.SyncReferences(adInput, devamBtn);

        LoginPanelController loginCtrl = loginPanel.gameObject.AddComponent<LoginPanelController>();
        loginCtrl.Initialize(loginPanel, adInput, soyadInput, devamBtn, loginKeyboard);

        // ═══════════════════════════════════════════════════════════════
        // 3. GLOBAL REPORT BUTTONS (At the Top Right Corner)
        // ═══════════════════════════════════════════════════════════════
        GameObject reportGroup = new GameObject("ReportButtons");
        reportGroup.transform.SetParent(root, false);
        RectTransform rgRect = reportGroup.AddComponent<RectTransform>();
        rgRect.anchorMin = new Vector2(1, 1);
        rgRect.anchorMax = new Vector2(1, 1);
        rgRect.pivot = new Vector2(1, 1);
        rgRect.anchoredPosition = new Vector2(-20, -20);
        rgRect.sizeDelta = new Vector2(400, 100);

        Button exportBtnGlobal = CreateButton(reportGroup.transform, "ExportBtnGlobal", "EXCEL KAYDET", new Vector2(-110, -30), new Vector2(200, 60));
        exportBtnGlobal.image.color = new Color(0.12f, 0.45f, 0.75f, 1f);
        exportBtnGlobal.onClick.AddListener(TriggerLocalReportExport);

        Button openFolderBtnGlobal = CreateButton(reportGroup.transform, "OpenFolderBtnGlobal", "KLASÖR", new Vector2(100, -30), new Vector2(150, 60));
        openFolderBtnGlobal.image.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        openFolderBtnGlobal.onClick.AddListener(OpenReportsFolder);

        // 4. Content Hub Panel (Hidden for now)
        contentHubPanel = CreatePanel(root, "ContentHubPanel", new Color(0.08f, 0.1f, 0.12f, 1f));
        contentHubPanel.alpha = 0; contentHubPanel.blocksRaycasts = false;
        
        hubTitle = CreateText(contentHubPanel.transform, "HubTitle", "MODÜL İSMİ", 40, new Vector2(0, 450));
        Button backBtn = CreateButton(contentHubPanel.transform, "BackButton", "GERİ", new Vector2(-750, 450), new Vector2(150, 50));
        backBtn.onClick.AddListener(BackToSelection);

        // NavBar
        GameObject navObj = new GameObject("NavBar");
        navObj.transform.SetParent(contentHubPanel.transform, false);
        RectTransform navRect = navObj.AddComponent<RectTransform>();
        navRect.anchoredPosition = new Vector2(0, 350);
        navRect.sizeDelta = new Vector2(800, 60);
        var navLayout = navObj.AddComponent<HorizontalLayoutGroup>();
        navLayout.spacing = 10;
        navLayout.childControlWidth = true;
        navLayout.childForceExpandWidth = true;

        CreateButton(navObj.transform, "SlideTab", "SLAYTLAR").onClick.AddListener(ShowSlides);
        CreateButton(navObj.transform, "VideoTab", "VİDEO").onClick.AddListener(ShowVideo);
        CreateButton(navObj.transform, "InfoTab", "İNFOGRAFİK").onClick.AddListener(ShowInfographic);

        // Display Area
        slidePanel = CreateSubPanel(contentHubPanel.transform, "SlidePanel");
        slideDisplay = CreateImage(slidePanel.transform, "SlideDisplay", new Vector2(1000, 560));
        slideCounterText = CreateText(slidePanel.transform, "Counter", "1 / 1", 24, new Vector2(0, -300));
        CreateButton(slidePanel.transform, "Prev", "<", new Vector2(-550, 0), new Vector2(60, 60)).onClick.AddListener(PrevSlide);
        CreateButton(slidePanel.transform, "Next", ">", new Vector2(550, 0), new Vector2(60, 60)).onClick.AddListener(NextSlide);

        videoPanel = CreateSubPanel(contentHubPanel.transform, "VideoPanel");
        CreateText(videoPanel.transform, "Placeholder", "EĞİTİM VİDEOSU BURADA OYNAYACAK", 30, Vector2.zero);

        infographicPanel = CreateSubPanel(contentHubPanel.transform, "InfographicPanel");
        infographicDisplay = CreateImage(infographicPanel.transform, "InfoDisplay", new Vector2(800, 600));

        Button launchBtn = CreateButton(contentHubPanel.transform, "LaunchButton", "EĞİTİMİ BAŞLAT", new Vector2(0, -420), new Vector2(400, 80));
        launchBtn.image.color = new Color(0.1f, 0.8f, 0.2f, 1f);
        launchBtn.onClick.AddListener(LaunchModule);

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esObj = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            // Use XR UI Input Module instead of Standalone for VR
            esObj.AddComponent(System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit") ?? typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        Debug.Log("Main Menu UI Setup Complete!");
#endif
    }

#if UNITY_EDITOR
    private CanvasGroup CreatePanel(RectTransform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
        Image img = obj.AddComponent<Image>();
        img.color = color;
        return obj.AddComponent<CanvasGroup>();
    }

    private CanvasGroup CreateSubPanel(Transform parent, string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = new Vector2(0, -250);
        return obj.AddComponent<CanvasGroup>();
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string content, int size, Vector2 pos)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.anchoredPosition = pos;
        text.rectTransform.sizeDelta = new Vector2(1000, size + 10);
        return text;
    }

    private Image CreateImage(Transform parent, string name, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.rectTransform.sizeDelta = size;
        img.color = Color.gray;
        return img;
    }

    private TMP_InputField BuildLoginInputField(Transform parent, string name, string placeholder, Vector2 pos)
    {
        // Container: dark tinted background with a left cyan accent stripe
        GameObject fieldRoot = new GameObject(name);
        fieldRoot.transform.SetParent(parent, false);
        Image fieldBg = fieldRoot.AddComponent<Image>();
        fieldBg.color = new Color(0.10f, 0.13f, 0.18f, 1f);
        RectTransform fieldRect = fieldRoot.GetComponent<RectTransform>();
        fieldRect.sizeDelta = new Vector2(840f, 68f);    // overridable after call
        fieldRect.anchoredPosition = pos;

        // Left cyan accent bar (4 px)
        GameObject leftBar = new GameObject("LeftBar");
        leftBar.transform.SetParent(fieldRoot.transform, false);
        leftBar.AddComponent<Image>().color = new Color(0.06f, 0.82f, 1f, 0.85f);
        RectTransform lbRect = leftBar.GetComponent<RectTransform>();
        lbRect.anchorMin = new Vector2(0f, 0f); lbRect.anchorMax = new Vector2(0f, 1f);
        lbRect.pivot = new Vector2(0f, 0.5f);
        lbRect.offsetMin = Vector2.zero; lbRect.offsetMax = Vector2.zero;
        lbRect.sizeDelta = new Vector2(4f, 0f);

        // Bottom border line (1 px, full width)
        GameObject bottomLine = new GameObject("BottomLine");
        bottomLine.transform.SetParent(fieldRoot.transform, false);
        bottomLine.AddComponent<Image>().color = new Color(0.06f, 0.82f, 1f, 0.35f);
        RectTransform blRect = bottomLine.GetComponent<RectTransform>();
        blRect.anchorMin = new Vector2(0f, 0f); blRect.anchorMax = new Vector2(1f, 0f);
        blRect.pivot = new Vector2(0.5f, 0f);
        blRect.offsetMin = Vector2.zero; blRect.offsetMax = Vector2.zero;
        blRect.sizeDelta = new Vector2(0f, 1f);

        // Label above field
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(fieldRoot.transform, false);
        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = placeholder.ToUpperInvariant();
        label.fontSize = 13;
        label.color = new Color(0.06f, 0.82f, 1f, 0.75f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform lblRect = labelObj.GetComponent<RectTransform>();
        lblRect.anchorMin = new Vector2(0f, 1f); lblRect.anchorMax = new Vector2(1f, 1f);
        lblRect.pivot = new Vector2(0f, 0f);
        lblRect.anchoredPosition = new Vector2(14f, 2f);
        lblRect.sizeDelta = new Vector2(-14f, 20f);

        // Text area (with left margin to clear the accent bar)
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(fieldRoot.transform, false);
        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero; textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(16f, 3f); textAreaRect.offsetMax = new Vector2(-10f, -3f);

        // Placeholder
        GameObject phObj = new GameObject("Placeholder");
        phObj.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI ph = phObj.AddComponent<TextMeshProUGUI>();
        ph.text = placeholder;
        ph.fontSize = 26;
        ph.color = new Color(0.38f, 0.47f, 0.58f, 1f);
        ph.alignment = TextAlignmentOptions.MidlineLeft;
        ph.fontStyle = FontStyles.Italic;
        RectTransform phRect = phObj.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero; phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero; phRect.offsetMax = Vector2.zero;

        // Text
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.fontSize = 26;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero; txtRect.offsetMax = Vector2.zero;

        TMP_InputField input = fieldRoot.AddComponent<TMP_InputField>();
        input.textViewport = textAreaRect;
        input.textComponent = txt;
        input.placeholder = ph;
        input.pointSize = 26;
        return input;
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 pos = default, Vector2 size = default)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.25f, 0.3f, 1f);
        Button btn = obj.AddComponent<Button>();
        RectTransform rect = obj.GetComponent<RectTransform>();
        if (size != default) rect.sizeDelta = size; else rect.sizeDelta = new Vector2(200, 50);
        rect.anchoredPosition = pos;

        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(obj.transform, false);
        TextMeshProUGUI txt = textObj.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 20;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        txt.rectTransform.anchorMin = Vector2.zero; txt.rectTransform.anchorMax = Vector2.one; txt.rectTransform.sizeDelta = Vector2.zero;

        return btn;
    }
#endif
    #endregion
    
    #region Debug Tools
#if UNITY_EDITOR
    [ContextMenu("Debug: Clear Participant Data")]
    public void ClearParticipantData()
    {
        PlayerPrefs.DeleteKey("training.analytics.participant_key");
        PlayerPrefs.DeleteKey("training.analytics.participant_name");
        PlayerPrefs.Save();
        Debug.Log("<color=yellow>Katılımcı verileri silindi. Oyunu yeniden başlatınca Login ekranı gelecektir.</color>");
    }
#endif
    #endregion
}
