using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ContentViewerManager : MonoBehaviour
{
    [System.Serializable]
    public class ContentItem
    {
        public string title;
        public Sprite thumbnail;
        public string description;
        public Sprite fullImage; // Infografik icin
        public VideoClip videoClip; // Video icin
        public int contentType; // 0: Infografik, 1: Sunum, 2: Video
    }

    [SerializeField] private List<ContentItem> infographicsContent = new List<ContentItem>();
    [SerializeField] private List<ContentItem> presentationsContent = new List<ContentItem>();
    [SerializeField] private List<ContentItem> videosContent = new List<ContentItem>();

    public void SetContentList(int contentType, List<ContentItem> items)
    {
        if (items == null) return;
        if (contentType == 0) infographicsContent = items;
        else if (contentType == 1) presentationsContent = items;
        else if (contentType == 2) videosContent = items;
    }

    [Header("Panel Referanslari")]
    [SerializeField] private GameObject infographicsPanel;
    [SerializeField] private GameObject presentationsPanel;
    [SerializeField] private GameObject videosPanel;

    [Header("Tab Butonlari")]
    [SerializeField] private Button tab1Button; // Infografikler
    [SerializeField] private Button tab2Button; // Sunumlar
    [SerializeField] private Button tab3Button; // Videolar

    [Header("Content Viewer Panel")]
    [SerializeField] private GameObject contentViewerPanel;
    [Header("Input Ayarlari")]
    [SerializeField] private bool enableVruiClickHelper;

    private Canvas canvas;
    private Image contentArea;
    private RawImage videoArea;
    private TextMeshProUGUI titleText;
    private Button closeButton;
    private Button leftArrowButton;
    private Button rightArrowButton;
    private VideoPlayer videoPlayer;
    private RenderTexture videoRenderTexture;
    private bool isInitialized;
    private Coroutine playVideoRoutine;
    private string lastVideoErrorMessage;

    private int currentContentType = -1; // -1: kapali, 0: infografik, 1: sunum, 2: video
    private int currentIndex = 0;
    private List<ContentItem> currentContentList;
    private readonly List<Sprite> directSlides = new List<Sprite>();
    private int directSlideIndex = -1;
    private bool isDirectPresentationMode;
    private int lastNavigationFrame = -1;
    private float lastNavigationTime = -1f;
    private const float NavigationDebounceSeconds = 0.08f;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void Start()
    {
        EnsureInitialized();
    }

    private void Update()
    {
        // VR ve PC'de paneller acik iken hizli kapatma kolayligi (Escape veya Controller B/Y tusu)
        if (contentViewerPanel != null && contentViewerPanel.activeSelf)
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnCloseContentViewer();
            }
#else
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnCloseContentViewer();
            }
#endif
            
            // OVR / XR controller escape fallback (Secondary Button / B / Y)
            var rightHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            if (rightHand.isValid && rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool rSecondary) && rSecondary)
            {
                OnCloseContentViewer();
            }
            var leftHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            if (leftHand.isValid && leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool lSecondary) && lSecondary)
            {
                OnCloseContentViewer();
            }
        }
    }

    private void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        InitializeUI();
        if (canvas == null)
        {
            return;
        }

        SetupButtons();
        SetupTabButtons();
        HideAllPanels();
        HideContentViewer();
        isInitialized = true;
    }

    private void InitializeUI()
    {
        // UI_Canvas'i bul a=" sahnedeki World Space canvas
        GameObject canvasObj = GameObject.Find("sinif/UI_Canvas");
        if (canvasObj != null)
            canvas = canvasObj.GetComponent<Canvas>();
        
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;
        }

        // DoAYru hiyerarsi yollari:
        // sinif/UI_Canvas/FuturisticPanel/GlassContainer/ContentRoot/...
        Transform glassContainer = canvas.transform.Find("FuturisticPanel/GlassContainer");
        if (glassContainer == null)
        {
            Debug.LogWarning("[ContentViewerManager] GlassContainer bulunamadi, alternatif arama...");
            glassContainer = FindDeepChild(canvas.transform, "GlassContainer");
        }

        if (glassContainer != null)
        {
            // Panel referanslarini otomatik bul
            Transform contentRoot = glassContainer.Find("ContentRoot");
            if (contentRoot != null)
            {
                if (infographicsPanel == null)
                    infographicsPanel = contentRoot.Find("Infografikler")?.gameObject;
                if (presentationsPanel == null)
                    presentationsPanel = contentRoot.Find("SunumlarPanel")?.gameObject;
                if (videosPanel == null)
                    videosPanel = contentRoot.Find("VideolarPanel")?.gameObject;
            }

            // Tab butonlarini otomatik bul
            if (tab1Button == null)
                tab1Button = glassContainer.Find("Tab1")?.GetComponent<Button>();
            if (tab2Button == null)
                tab2Button = glassContainer.Find("Tab2")?.GetComponent<Button>();
            if (tab3Button == null)
                tab3Button = glassContainer.Find("Tab3")?.GetComponent<Button>();
        }

        // Content Viewer Panel a=" yoksa runtime'da olustur
        if (contentViewerPanel == null)
        {
            Transform panelTransform = canvas.transform.Find("ContentViewerPanel");
            if (panelTransform != null)
                contentViewerPanel = panelTransform.gameObject;
        }

        if (contentViewerPanel == null)
        {
            CreateContentViewerPanel();
        }

        if (contentViewerPanel == null) return;

        ConfigureClickHelper();

        Transform contentAreaTransform = contentViewerPanel.transform.Find("ContentArea");
        if (contentAreaTransform != null) contentArea = contentAreaTransform.GetComponent<Image>();

        Transform videoAreaTransform = contentViewerPanel.transform.Find("VideoArea");
        if (videoAreaTransform != null) videoArea = videoAreaTransform.GetComponent<RawImage>();

        Transform titleTransform = contentViewerPanel.transform.Find("Title");
        if (titleTransform != null) titleText = titleTransform.GetComponent<TextMeshProUGUI>();

        Transform closeTransform = contentViewerPanel.transform.Find("CloseButton");
        if (closeTransform != null) closeButton = closeTransform.GetComponent<Button>();

        Transform leftTransform = contentViewerPanel.transform.Find("LeftArrow");
        if (leftTransform != null) leftArrowButton = leftTransform.GetComponent<Button>();

        Transform rightTransform = contentViewerPanel.transform.Find("RightArrow");
        if (rightTransform != null) rightArrowButton = rightTransform.GetComponent<Button>();

        // VideoPlayer ekle (sadece bir kere)
        videoPlayer = contentViewerPanel.GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            videoPlayer = contentViewerPanel.AddComponent<VideoPlayer>();
        }

        ConfigureVideoPlayer();

        Debug.Log("[ContentViewerManager] UI basariyla baslatildi.");
    }

    /// <summary>
    /// Icerik goruntuleme panelini runtime'da olusturur.
    /// Tam ekran overlay a=" arka plan, gorsel, baslik, kapat butonu.
    /// </summary>
    private void ConfigureVideoPlayer()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.errorReceived -= OnVideoErrorReceived;
        videoPlayer.errorReceived += OnVideoErrorReceived;
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.skipOnDrop = true;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        // Audio: Direct cikis - AudioSource olmadan en guvenli yol
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        EnsureVideoRenderTarget();
    }

    private void EnsureVideoRenderTarget()
    {
        const int targetWidth = 1920;
        const int targetHeight = 1080;

        if (videoRenderTexture == null)
        {
            videoRenderTexture = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
            videoRenderTexture.name = "ContentViewerVideoRT";
            videoRenderTexture.Create();
        }

        if (videoPlayer != null)
        {
            videoPlayer.targetTexture = videoRenderTexture;
        }

        if (videoArea != null)
        {
            videoArea.texture = videoRenderTexture;
        }
    }

    private void SetVideoVisible(bool visible)
    {
        if (videoArea != null)
        {
            videoArea.gameObject.SetActive(visible);
        }

        if (!visible && videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
    }

    private void OnVideoErrorReceived(VideoPlayer source, string message)
    {
        lastVideoErrorMessage = message;
        Debug.LogWarning($"[ContentViewerManager] VideoPlayer error: {message}");
    }

    private void CreateContentViewerPanel()
    {
        if (canvas == null)
        {
            GameObject canvasObj = GameObject.Find("sinif/UI_Canvas");
            if (canvasObj != null)
            {
                canvas = canvasObj.GetComponent<Canvas>();
            }

            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
            }

            if (canvas == null)
            {
                Debug.LogError("[ContentViewerManager] Canvas bulunamadi, ContentViewerPanel olusturulamadi.");
                return;
            }
        }

        // ContentViewerPanel a=" Canvas'in en ustunde, tum alani kaplar
        contentViewerPanel = new GameObject("ContentViewerPanel");
        contentViewerPanel.transform.SetParent(canvas.transform, false);
        contentViewerPanel.transform.SetAsLastSibling(); // En onde

        RectTransform panelRect = contentViewerPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Yari saydam koyu arka plan
        Image panelBg = contentViewerPanel.AddComponent<Image>();
        panelBg.color = new Color(0.02f, 0.03f, 0.06f, 0.96f);

        // CanvasGroup (fade efekti icin)
        CanvasGroup cg = contentViewerPanel.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.blocksRaycasts = true;

        // a"=a"=a"= Baslik a"=a"=a"=
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(contentViewerPanel.transform, false);

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -15f);
        titleRect.sizeDelta = new Vector2(-120f, 60f);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "";
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.raycastTarget = false; // Etkilesimi engellemesin

        // a"=a"=a"= ContentArea (Gorsel) a"=a"=a"=
        GameObject contentAreaObj = new GameObject("ContentArea");
        contentAreaObj.transform.SetParent(contentViewerPanel.transform, false);

        RectTransform caRect = contentAreaObj.AddComponent<RectTransform>();
        caRect.anchorMin = new Vector2(0.05f, 0.05f);
        caRect.anchorMax = new Vector2(0.95f, 0.88f);
        caRect.offsetMin = Vector2.zero;
        caRect.offsetMax = Vector2.zero;

        contentArea = contentAreaObj.AddComponent<Image>();
        contentArea.color = Color.white;
        contentArea.preserveAspect = true;

        GameObject videoAreaObj = new GameObject("VideoArea");
        videoAreaObj.transform.SetParent(contentViewerPanel.transform, false);

        RectTransform vaRect = videoAreaObj.AddComponent<RectTransform>();
        vaRect.anchorMin = new Vector2(0.05f, 0.05f);
        vaRect.anchorMax = new Vector2(0.95f, 0.88f);
        vaRect.offsetMin = Vector2.zero;
        vaRect.offsetMax = Vector2.zero;

        videoArea = videoAreaObj.AddComponent<RawImage>();
        videoArea.color = Color.white;
        videoArea.raycastTarget = false;
        videoArea.gameObject.SetActive(false);

        ConfigureClickHelper();

        // === Kapat Butonu (saAY ust) ===
        GameObject closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(contentViewerPanel.transform, false);

        RectTransform closeRect = closeObj.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-30f, -30f);
        closeRect.sizeDelta = new Vector2(100f, 100f);

        Image closeBg = closeObj.AddComponent<Image>();
        closeBg.color = new Color(0.8f, 0.15f, 0.15f, 0.9f);
        closeBg.raycastTarget = true; // Tiklanabilir olmali

        closeButton = closeObj.AddComponent<Button>();

        // X metni
        GameObject closeTextObj = new GameObject("CloseText");
        closeTextObj.transform.SetParent(closeObj.transform, false);

        RectTransform ctRect = closeTextObj.AddComponent<RectTransform>();
        ctRect.anchorMin = Vector2.zero;
        ctRect.anchorMax = Vector2.one;
        ctRect.offsetMin = Vector2.zero;
        ctRect.offsetMax = Vector2.zero;

        TextMeshProUGUI closeText = closeTextObj.AddComponent<TextMeshProUGUI>();
        closeText.text = "X";
        closeText.fontSize = 64;
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.color = Color.white;
        closeText.raycastTarget = false; // Buton tiklamasini engellemesin

        // a"=a"=a"= Sol Ok a"=a"=a"=
        GameObject leftObj = new GameObject("LeftArrow");
        leftObj.transform.SetParent(contentViewerPanel.transform, false);

        RectTransform leftRect = leftObj.AddComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0f, 0.5f);
        leftRect.anchorMax = new Vector2(0f, 0.5f);
        leftRect.pivot = new Vector2(0f, 0.5f);
        leftRect.anchoredPosition = new Vector2(20f, 0f);
        leftRect.sizeDelta = new Vector2(80f, 120f);

        Image leftBg = leftObj.AddComponent<Image>();
        leftBg.color = new Color(0.1f, 0.15f, 0.25f, 0.8f);
        leftBg.raycastTarget = true; // Tiklanabilir olmali
        leftArrowButton = leftObj.AddComponent<Button>();

        GameObject leftText = new GameObject("Text");
        leftText.transform.SetParent(leftObj.transform, false);
        RectTransform ltRect = leftText.AddComponent<RectTransform>();
        ltRect.anchorMin = Vector2.zero;
        ltRect.anchorMax = Vector2.one;
        ltRect.offsetMin = Vector2.zero;
        ltRect.offsetMax = Vector2.zero;
        TextMeshProUGUI lt = leftText.AddComponent<TextMeshProUGUI>();
        lt.text = "<";
        lt.fontSize = 64;
        lt.alignment = TextAlignmentOptions.Center;
        lt.color = Color.white;
        lt.raycastTarget = false; // Ok butonunu engellemesin

        // a"=a"=a"= SaAY Ok a"=a"=a"=
        GameObject rightObj = new GameObject("RightArrow");
        rightObj.transform.SetParent(contentViewerPanel.transform, false);

        RectTransform rightRect = rightObj.AddComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(1f, 0.5f);
        rightRect.anchorMax = new Vector2(1f, 0.5f);
        rightRect.pivot = new Vector2(1f, 0.5f);
        rightRect.anchoredPosition = new Vector2(-20f, 0f);
        rightRect.sizeDelta = new Vector2(80f, 120f);

        Image rightBg = rightObj.AddComponent<Image>();
        rightBg.color = new Color(0.1f, 0.15f, 0.25f, 0.8f);
        rightBg.raycastTarget = true; // Tiklanabilir olmali
        rightArrowButton = rightObj.AddComponent<Button>();

        GameObject rightText = new GameObject("Text");
        rightText.transform.SetParent(rightObj.transform, false);
        RectTransform rtRect = rightText.AddComponent<RectTransform>();
        rtRect.anchorMin = Vector2.zero;
        rtRect.anchorMax = Vector2.one;
        rtRect.offsetMin = Vector2.zero;
        rtRect.offsetMax = Vector2.zero;
        TextMeshProUGUI rt = rightText.AddComponent<TextMeshProUGUI>();
        rt.text = ">";
        rt.fontSize = 64;
        rt.alignment = TextAlignmentOptions.Center;
        rt.color = Color.white;
        rt.raycastTarget = false; // Ok butonunu engellemesin

        // Baslangicta gizle
        contentViewerPanel.SetActive(false);

        if (videoPlayer == null)
        {
            videoPlayer = contentViewerPanel.GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                videoPlayer = contentViewerPanel.AddComponent<VideoPlayer>();
            }
        }
        ConfigureVideoPlayer();

        Debug.Log("[ContentViewerManager] ContentViewerPanel runtime'da olusturuldu.");
    }

    /// <summary>
    /// Tum alt hiyerarside isimle arama yapar.
    /// </summary>
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private void SetupButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseContentViewer);
        }

        if (leftArrowButton != null)
        {
            leftArrowButton.onClick.RemoveAllListeners();
            leftArrowButton.onClick.AddListener(ShowPreviousContent);
        }

        if (rightArrowButton != null)
        {
            rightArrowButton.onClick.RemoveAllListeners();
            rightArrowButton.onClick.AddListener(ShowNextContent);
        }
    }

    private void ConfigureClickHelper()
    {
        if (contentViewerPanel == null)
        {
            return;
        }

        VRUIClickHelper clickHelper = contentViewerPanel.GetComponent<VRUIClickHelper>();
        if (enableVruiClickHelper)
        {
            if (clickHelper == null)
            {
                contentViewerPanel.AddComponent<VRUIClickHelper>();
            }
            else
            {
                clickHelper.enabled = true;
            }

            return;
        }

        if (clickHelper != null)
        {
            clickHelper.enabled = false;
        }
    }

    private void SetupTabButtons()
    {
        if (tab1Button != null)
        {
            tab1Button.onClick.RemoveAllListeners();
            tab1Button.onClick.AddListener(() => OnTabClicked(0)); // Infografikler
        }

        if (tab2Button != null)
        {
            tab2Button.onClick.RemoveAllListeners();
            tab2Button.onClick.AddListener(() => OnTabClicked(1)); // Sunumlar
        }

        if (tab3Button != null)
        {
            tab3Button.onClick.RemoveAllListeners();
            tab3Button.onClick.AddListener(() => OnTabClicked(2)); // Videolar
        }
    }

    /// <summary>
    /// Tab'a tiklandiAYinda ilgili paneli gosterir, diAYerlerini gizler
    /// </summary>
    public void OnTabClicked(int tabIndex)
    {
        EnsureInitialized();

        // Content Viewer'i kapat
        HideContentViewer();

        // Tum panelleri gizle
        HideAllPanels();

        // Aktif Tab'i guncelle
        UpdateActiveTab(tabIndex);

        // Ilgili paneli goster
        switch (tabIndex)
        {
            case 0: // Infografikler
                if (infographicsPanel != null)
                    infographicsPanel.SetActive(true);
                break;
            case 1: // Sunumlar
                if (presentationsPanel != null)
                    presentationsPanel.SetActive(true);
                break;
            case 2: // Videolar
                if (videosPanel != null)
                    videosPanel.SetActive(true);
                break;
        }
    }

    private void UpdateActiveTab(int activeTabIndex)
    {
        // ActiveLine child'larini guncelle
        UpdateTabActiveLine(tab1Button?.transform, activeTabIndex == 0);
        UpdateTabActiveLine(tab2Button?.transform, activeTabIndex == 1);
        UpdateTabActiveLine(tab3Button?.transform, activeTabIndex == 2);
    }

    private void UpdateTabActiveLine(Transform tabTransform, bool isActive)
    {
        if (tabTransform == null) return;
        Transform activeLine = tabTransform.Find("ActiveLine");
        if (activeLine != null)
            activeLine.gameObject.SetActive(isActive);
    }

    private void HideAllPanels()
    {
        if (infographicsPanel != null)
            infographicsPanel.SetActive(false);
        if (presentationsPanel != null)
            presentationsPanel.SetActive(false);
        if (videosPanel != null)
            videosPanel.SetActive(false);
    }

    private void SetupCardButtons()
    {
        // Infografik kartlari (0-2)
        for (int i = 0; i < 3; i++)
        {
            var cardPath = $"sinif/UI_Canvas/FuturisticPanel/GlassContainer/ContentRoot/Infografikler/Card_{i + 1}/OpenButton";
            var cardButton = FindButtonInHierarchy(cardPath);
            if (cardButton != null)
            {
                int index = i;
                // Onceki listener'lari temizle
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(() => ShowContent(0, index));
            }
        }

        // Sunum kartlari (0-2)
        for (int i = 0; i < 3; i++)
        {
            var cardPath = $"sinif/UI_Canvas/FuturisticPanel/GlassContainer/ContentRoot/SunumlarPanel/SunumCard_{i + 1}/OpenButton";
            var cardButton = FindButtonInHierarchy(cardPath);
            if (cardButton != null)
            {
                int index = i;
                // Onceki listener'lari temizle
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(() => ShowContent(1, index));
            }
        }

        // Video kartlari (0-2)
        for (int i = 0; i < 3; i++)
        {
            var cardPath = $"sinif/UI_Canvas/FuturisticPanel/GlassContainer/ContentRoot/VideolarPanel/VideoCard_{i + 1}/OpenButton";
            var cardButton = FindButtonInHierarchy(cardPath);
            if (cardButton != null)
            {
                int index = i;
                // Onceki listener'lari temizle
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(() => ShowContent(2, index));
            }
        }
    }

    private Button FindButtonInHierarchy(string path)
    {
        var parts = path.Split('/');
        Transform current = null;

        foreach (var part in parts)
        {
            if (current == null)
            {
                current = GameObject.Find(part)?.transform;
            }
            else
            {
                current = current.Find(part);
            }

            if (current == null) return null;
        }

        return current?.GetComponent<Button>();
    }

    public void ShowContent(int contentType, int index)
    {
        EnsureInitialized();
        isDirectPresentationMode = false;
        directSlides.Clear();
        directSlideIndex = -1;
        currentContentType = contentType;
        currentIndex = index;

        switch (contentType)
        {
            case 0: // Infografik
                currentContentList = infographicsContent;
                break;
            case 1: // Sunum
                currentContentList = presentationsContent;
                break;
            case 2: // Video
                currentContentList = videosContent;
                break;
        }

        if (currentContentList != null && currentContentList.Count > 0)
        {
            DisplayContent(currentIndex);
            ShowContentViewer();
        }
    }

    /// <summary>
    /// ContentLoader'dan doAYrudan Sprite/VideoClip ile icerik gosterimi.
    /// Icerik listelerine gerek duymadan, parametreleri direkt kullanir.
    /// </summary>
    public void ShowContentDirect(string title, Sprite image, VideoClip video, int contentType)
    {
        EnsureInitialized();
        Debug.Log($"[ContentViewerManager] ShowContentDirect caAYrildi: '{title}' tip={contentType} image={image != null} video={video != null}");
        isDirectPresentationMode = false;
        directSlides.Clear();
        directSlideIndex = -1;
        currentContentList = null;
        bool isLikelyVideoTitle = IsLikelyVideoTitle(title);
        VideoClip resolvedVideo = video;

        // IÃ§erik tipi video (2) ise veya baÅŸlÄ±kta aÃ§Ä±kÃ§a "video" geÃ§iyorsa fallback ara
        if (contentType == 2 || isLikelyVideoTitle)
        {
            if (resolvedVideo == null) resolvedVideo = ResolveModule1VideoByTitle(title);
            currentContentType = 2;
        }
        else
        {
            currentContentType = contentType;
        }

        // Content viewer panelinin varliAYini kontrol et
        if (contentViewerPanel == null)
        {
            Debug.LogWarning("[ContentViewerManager] ContentViewerPanel bulunamadi! Yeniden olusturuluyor...");
            CreateContentViewerPanel();
            // Bilesenleri yeniden baAYla
            if (contentViewerPanel != null)
            {
                Transform contentAreaTransform = contentViewerPanel.transform.Find("ContentArea");
                if (contentAreaTransform != null) contentArea = contentAreaTransform.GetComponent<Image>();
                
                Transform videoAreaTransform = contentViewerPanel.transform.Find("VideoArea");
                if (videoAreaTransform != null) videoArea = videoAreaTransform.GetComponent<RawImage>();
                
                Transform titleTransform = contentViewerPanel.transform.Find("Title");
                if (titleTransform != null) titleText = titleTransform.GetComponent<TextMeshProUGUI>();
                
                // Butonları tazeleyerek bağla
                Transform closeTransform = contentViewerPanel.transform.Find("CloseButton");
                if (closeTransform != null) closeButton = closeTransform.GetComponent<Button>();
                
                Transform leftTransform = contentViewerPanel.transform.Find("LeftArrow");
                if (leftTransform != null) leftArrowButton = leftTransform.GetComponent<Button>();
                
                Transform rightTransform = contentViewerPanel.transform.Find("RightArrow");
                if (rightTransform != null) rightArrowButton = rightTransform.GetComponent<Button>();

                SetupButtons();

                videoPlayer = contentViewerPanel.GetComponent<VideoPlayer>();
                if (videoPlayer == null)
                {
                    videoPlayer = contentViewerPanel.AddComponent<VideoPlayer>();
                }
                ConfigureVideoPlayer();
            }
            if (contentViewerPanel == null)
            {
                Debug.LogError("[ContentViewerManager] ContentViewerPanel olusturulamadi!");
                return;
            }
        }

        // BasliAYi guncelle
        if (titleText != null)
            titleText.text = title;

        // Icerik tipine gore goster
        if (currentContentType == 2 && resolvedVideo != null)
        {
            // Video
            if (contentArea != null)
            {
                contentArea.enabled = true;
                contentArea.sprite = image;
                contentArea.preserveAspect = true;
                contentArea.color = Color.white;
            }

            SetVideoVisible(true);
            EnsureVideoRenderTarget();
            StartVideoPlayback(resolvedVideo, image);
        }
        else if (image != null)
        {
            // Gorsel (infografik veya sunum)
            if (contentArea != null)
            {
                contentArea.gameObject.SetActive(true);
                contentArea.sprite = image;
                contentArea.enabled = true;
                contentArea.preserveAspect = true;
                contentArea.color = Color.white;
            }

            SetVideoVisible(false);
        }
        else
        {
            // Hem image hem video null a=" yine de paneli ac ama bilgilendir
            Debug.LogWarning($"[ContentViewerManager] '{title}' icin gorsel veya video atanmamis!");
            if (contentArea != null)
            {
                contentArea.gameObject.SetActive(true);
                contentArea.sprite = null;
                contentArea.enabled = true;
                contentArea.color = new Color(0.1f, 0.15f, 0.25f, 1f);
            }
            SetVideoVisible(false);
        }

        ShowContentViewer();
    }

    private static bool IsLikelyVideoTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        return title.IndexOf("video", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static VideoClip ResolveModule1VideoByTitle(string title)
    {
        string category = ResolveVideoCategoryFromTitle(title);
        if (string.IsNullOrEmpty(category))
        {
            return null;
        }

        ContentLoader loader = UnityEngine.Object.FindObjectOfType<ContentLoader>(true);
        if (loader != null)
        {
            VideoClip clipFromLoader = GetVideoFromLoader(loader, category);
            if (clipFromLoader != null)
            {
                return clipFromLoader;
            }
        }

#if UNITY_EDITOR
        string path = GetModule1VideoAssetPath(category);
        if (!string.IsNullOrEmpty(path))
        {
            VideoClip clipFromAsset = AssetDatabase.LoadAssetAtPath<VideoClip>(path);
            if (clipFromAsset != null)
            {
                return clipFromAsset;
            }
        }
#endif

        return null;
    }

    private static string ResolveVideoCategoryFromTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        string lower = title.ToLowerInvariant();
        if (lower.Contains("deprem"))
        {
            return "Deprem";
        }

        if (lower.Contains("triyaj"))
        {
            return "Triyaj";
        }

        if (lower.Contains("yang"))
        {
            return "Yangin";
        }

        return string.Empty;
    }

    private static VideoClip GetVideoFromLoader(ContentLoader loader, string category)
    {
        if (loader == null || string.IsNullOrEmpty(category))
        {
            return null;
        }

        switch (category)
        {
            case "Deprem":
                return loader.depremVideo;
            case "Triyaj":
                return loader.triyajVideo;
            case "Yangin":
                return loader.yanginVideo;
            default:
                return null;
        }
    }

#if UNITY_EDITOR
    private static string GetModule1VideoAssetPath(string category)
    {
        switch (category)
        {
            case "Deprem":
                return "Assets/Content/Module1/Deprem/Video/deprem1.mp4";
            case "Triyaj":
                return "Assets/Content/Module1/Triyaj/Video/triyaj1.mp4";
            case "Yangin":
                return "Assets/Content/Module1/Yangin/Video/yangin1.mp4";
            default:
                return string.Empty;
        }
    }
#endif

    public void ShowPresentationSlidesDirect(string title, List<Sprite> slides, Sprite fallbackCover = null)
    {
        EnsureInitialized();

        isDirectPresentationMode = true;
        currentContentType = 1;
        currentContentList = null;
        directSlides.Clear();
        directSlideIndex = 0;

        if (slides != null)
        {
            for (int i = 0; i < slides.Count; i++)
            {
                if (slides[i] != null)
                {
                    directSlides.Add(slides[i]);
                }
            }
        }

        if (directSlides.Count == 0 && fallbackCover != null)
        {
            directSlides.Add(fallbackCover);
        }

        if (directSlides.Count == 0)
        {
            ShowContentDirect(title, fallbackCover, null, 1);
            return;
        }

        if (titleText != null)
        {
            titleText.text = title;
        }

        if (contentArea != null)
        {
            contentArea.gameObject.SetActive(true);
            contentArea.sprite = directSlides[0];
            contentArea.enabled = true;
            contentArea.preserveAspect = true;
            contentArea.color = Color.white;
        }

        SetVideoVisible(false);
        ShowContentViewer();
    }

    private void DisplayContent(int index)
    {
        if (index < 0 || index >= currentContentList.Count) return;

        var content = currentContentList[index];
        
        if (titleText != null)
            titleText.text = content.title;

        if (contentArea != null)
        {
            if (currentContentType == 0 || currentContentType == 1) // Infografik veya Sunum
            {
                contentArea.gameObject.SetActive(true);
                contentArea.sprite = content.fullImage;
                contentArea.enabled = true;
                contentArea.preserveAspect = true;
                contentArea.color = Color.white;
                SetVideoVisible(false);
            }
            else if (currentContentType == 2) // Video
            {
                if (content.videoClip != null && videoPlayer != null)
                {
                    contentArea.gameObject.SetActive(true);
                    contentArea.enabled = true;
                    contentArea.sprite = content.fullImage;
                    contentArea.preserveAspect = true;
                    contentArea.color = Color.white;
                    SetVideoVisible(true);
                    EnsureVideoRenderTarget();
                    StartVideoPlayback(content.videoClip, content.fullImage);
                }
                else
                {
                    SetVideoVisible(false);
                    contentArea.gameObject.SetActive(true);
                    contentArea.enabled = true;
                    contentArea.sprite = content.fullImage;
                    contentArea.color = content.fullImage != null ? Color.white : new Color(0.1f, 0.15f, 0.25f, 1f);
                }
            }
        }
    }

    private void ShowNextContent()
    {
        if (!TryConsumeNavigationInput())
        {
            return;
        }

        if (isDirectPresentationMode)
        {
            if (directSlides.Count == 0) return;
            directSlideIndex = (directSlideIndex + 1) % directSlides.Count;
            ShowDirectSlide(directSlideIndex);
            return;
        }

        // Fallback: list null ise guncel tipe gore yukle (ic gucunu korumak icin)
        if (currentContentList == null || currentContentList.Count == 0)
        {
            if (currentContentType == 0) currentContentList = infographicsContent;
            else if (currentContentType == 2) currentContentList = videosContent;
        }

        if (currentContentList == null || currentContentList.Count == 0)
        {
            Debug.LogWarning("[ContentViewerManager] Navigasyon basarisiz: Icerik listesi bos.");
            return;
        }

        currentIndex = (currentIndex + 1) % currentContentList.Count;
        DisplayContent(currentIndex);
    }

    private void ShowPreviousContent()
    {
        if (!TryConsumeNavigationInput())
        {
            return;
        }

        if (isDirectPresentationMode)
        {
            if (directSlides.Count == 0) return;
            directSlideIndex = (directSlideIndex - 1 + directSlides.Count) % directSlides.Count;
            ShowDirectSlide(directSlideIndex);
            return;
        }

        if (currentContentList == null || currentContentList.Count == 0)
        {
            if (currentContentType == 0) currentContentList = infographicsContent;
            else if (currentContentType == 2) currentContentList = videosContent;
        }

        if (currentContentList == null || currentContentList.Count == 0)
        {
            Debug.LogWarning("[ContentViewerManager] Navigasyon basarisiz: Icerik listesi bos.");
            return;
        }

        currentIndex = (currentIndex - 1 + currentContentList.Count) % currentContentList.Count;
        DisplayContent(currentIndex);
    }

    private bool TryConsumeNavigationInput()
    {
        int frame = Time.frameCount;
        float now = Time.unscaledTime;

        if (frame == lastNavigationFrame)
        {
            return false;
        }

        if (lastNavigationTime >= 0f && now - lastNavigationTime < NavigationDebounceSeconds)
        {
            return false;
        }

        lastNavigationFrame = frame;
        lastNavigationTime = now;
        return true;
    }

    private void ShowDirectSlide(int index)
    {
        if (index < 0 || index >= directSlides.Count)
        {
            return;
        }

        if (contentArea != null)
        {
            contentArea.gameObject.SetActive(true);
            contentArea.sprite = directSlides[index];
            contentArea.enabled = true;
            contentArea.preserveAspect = true;
            contentArea.color = Color.white;
        }

        SetVideoVisible(false);
    }

    private IEnumerator PlayVideoClipRoutine(VideoClip clip)
    {
        if (videoPlayer == null || clip == null)
        {
            yield break;
        }

        if (!videoPlayer.enabled)
        {
            videoPlayer.enabled = true;
        }

        lastVideoErrorMessage = string.Empty;

        // Once VideoClip ile dene
        videoPlayer.Stop();
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = clip;

        Debug.Log($"[ContentViewerManager] Video prepare baslatiliyor: {clip.name}");
        videoPlayer.Prepare();

        float timeout = Time.unscaledTime + 30f;
        while (!videoPlayer.isPrepared && Time.unscaledTime < timeout)
        {
            if (!string.IsNullOrEmpty(lastVideoErrorMessage))
            {
                Debug.LogWarning($"[ContentViewerManager] VideoClip error detected early: {lastVideoErrorMessage}");
                break;
            }
            yield return null;
        }

        // VideoClip basarisiz olduysa URL-based fallback dene
        if (!videoPlayer.isPrepared)
        {
            string reason = string.IsNullOrEmpty(lastVideoErrorMessage) ? "timeout" : lastVideoErrorMessage;
            Debug.LogWarning($"[ContentViewerManager] VideoClip prepare failed: {clip.name} ({reason}). URL fallback deneniyor...");

            string urlPath = TryResolveVideoUrl(clip.name);
            if (!string.IsNullOrEmpty(urlPath))
            {
                yield return StartCoroutine(PlayVideoUrlRoutine(urlPath));
                yield break;
            }
            else
            {
                Debug.LogWarning($"[ContentViewerManager] URL fallback bulunamadi: {clip.name}");
                SetVideoVisible(false);
                yield break;
            }
        }

        Debug.Log($"[ContentViewerManager] Video prepared OK: {clip.name}, starting playback");

        // RenderTexture baglantilarini yeniden zorla
        EnsureVideoRenderTarget();
        videoPlayer.targetTexture = videoRenderTexture;

        if (videoArea != null)
        {
            videoArea.texture = videoRenderTexture;
            videoArea.gameObject.SetActive(true);
            videoArea.color = Color.white;
            videoArea.enabled = true;
        }

        if (contentArea != null)
        {
            contentArea.gameObject.SetActive(false);
        }

        videoPlayer.Play();
        Debug.Log($"[ContentViewerManager] videoPlayer.Play() cagrildi. isPlaying={videoPlayer.isPlaying}, targetTexture={videoPlayer.targetTexture != null}, videoArea.texture={videoArea?.texture != null}");
    }

    /// <summary>
    /// URL-based video oynatma (VideoClip import basarisiz oldugunda fallback).
    /// </summary>
    private IEnumerator PlayVideoUrlRoutine(string url)
    {
        if (videoPlayer == null || string.IsNullOrEmpty(url))
        {
            SetVideoVisible(false);
            yield break;
        }

        if (!videoPlayer.enabled)
        {
            videoPlayer.enabled = true;
        }

        lastVideoErrorMessage = string.Empty;
        videoPlayer.Stop();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;

        Debug.Log($"[ContentViewerManager] URL fallback prepare: {url}");
        videoPlayer.Prepare();

        float timeout = Time.unscaledTime + 30f;
        while (!videoPlayer.isPrepared && Time.unscaledTime < timeout)
        {
            if (!string.IsNullOrEmpty(lastVideoErrorMessage))
            {
                break;
            }
            yield return null;
        }

        if (!videoPlayer.isPrepared)
        {
            string reason = string.IsNullOrEmpty(lastVideoErrorMessage) ? "timeout" : lastVideoErrorMessage;
            Debug.LogWarning($"[ContentViewerManager] URL fallback also failed: {url} ({reason})");
            SetVideoVisible(false);
            yield break;
        }

        Debug.Log($"[ContentViewerManager] URL video prepared OK, playing: {url}");

        EnsureVideoRenderTarget();
        videoPlayer.targetTexture = videoRenderTexture;

        if (videoArea != null)
        {
            videoArea.texture = videoRenderTexture;
            videoArea.gameObject.SetActive(true);
            videoArea.color = Color.white;
            videoArea.enabled = true;
        }

        if (contentArea != null)
        {
            contentArea.gameObject.SetActive(false);
        }

        videoPlayer.Play();
    }

    /// <summary>
    /// VideoClip adindan dosya yolunu cozumler (URL fallback icin).
    /// </summary>
    private static string TryResolveVideoUrl(string clipName)
    {
        if (string.IsNullOrEmpty(clipName))
        {
            return string.Empty;
        }

        string lower = clipName.ToLowerInvariant();
        string category = string.Empty;

        if (lower.Contains("deprem"))
        {
            category = "Deprem";
        }
        else if (lower.Contains("triyaj"))
        {
            category = "Triyaj";
        }
        else if (lower.Contains("yang"))
        {
            category = "Yangin";
        }

        if (string.IsNullOrEmpty(category))
        {
            return string.Empty;
        }

        // Application.streamingAssetsPath veya dataPath kullan
        string basePath = Application.dataPath; // Assets klasoru
        string videoFileName = category == "Deprem" ? "deprem1.mp4" : category == "Triyaj" ? "triyaj1.mp4" : "yangin1.mp4";
        string fullPath = System.IO.Path.Combine(basePath, "Content", "Module1", category, "Video", videoFileName);

        if (System.IO.File.Exists(fullPath))
        {
            Debug.Log($"[ContentViewerManager] URL fallback path found: {fullPath}");
            return fullPath;
        }

        Debug.LogWarning($"[ContentViewerManager] URL fallback file not found: {fullPath}");
        return string.Empty;
    }

    private void StartVideoPlayback(VideoClip clip, Sprite fallbackImage)
    {
        if (playVideoRoutine != null)
        {
            StopCoroutine(playVideoRoutine);
            playVideoRoutine = null;
        }

        if (clip == null || videoPlayer == null)
        {
            SetVideoVisible(false);
            if (contentArea != null)
            {
                contentArea.enabled = true;
                contentArea.sprite = fallbackImage;
            }
            return;
        }

        if (contentViewerPanel != null && !contentViewerPanel.activeSelf)
        {
            contentViewerPanel.SetActive(true);
        }

        if (videoPlayer != null && !videoPlayer.gameObject.activeInHierarchy)
        {
            videoPlayer.gameObject.SetActive(true);
        }

        if (!videoPlayer.enabled)
        {
            videoPlayer.enabled = true;
        }

        playVideoRoutine = StartCoroutine(PlayVideoClipRoutine(clip));
    }

    private void ShowContentViewer()
    {
        if (contentViewerPanel == null)
        {
            Debug.LogWarning("[ContentViewerManager] ShowContentViewer: panel null!");
            return;
        }

        // Paneli aktif et
        contentViewerPanel.SetActive(true);

        // EN ONE GETIR a=" FuturisticPanel'in ustunde gorunsun
        contentViewerPanel.transform.SetAsLastSibling();

        var canvasGroup = contentViewerPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        Debug.Log("[ContentViewerManager] ContentViewerPanel gosterildi.");
    }

    public void OnCloseContentViewer()
    {
        HideContentViewer();

        // 1. Olası tüm çakışmaları engellemek için önce her şeyi gizle
        HideAllPanels();

        // 2. Icerik tipine gore asıl paneli tekrar goster
        switch (currentContentType)
        {
            case 0:
                if (infographicsPanel != null) infographicsPanel.SetActive(true);
                break;
            case 1:
                if (presentationsPanel != null) presentationsPanel.SetActive(true);
                break;
            case 2:
                if (videosPanel != null) videosPanel.SetActive(true);
                break;
            default:
                // Fallback: panel tipi bilinmiyorsa varsayılanı (örneğin videoları) aç
                if (videosPanel != null) videosPanel.SetActive(true);
                break;
        }
        
        currentContentType = -1;
    }

    private void HideContentViewer()
    {
        if (contentViewerPanel == null) return;
        contentViewerPanel.SetActive(false);

        if (playVideoRoutine != null)
        {
            StopCoroutine(playVideoRoutine);
            playVideoRoutine = null;
        }

        var canvasGroup = contentViewerPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        SetVideoVisible(false);
    }

    private void OnDestroy()
    {
        if (playVideoRoutine != null)
        {
            StopCoroutine(playVideoRoutine);
            playVideoRoutine = null;
        }

        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        if (videoRenderTexture != null)
        {
            if (videoRenderTexture.IsCreated())
            {
                videoRenderTexture.Release();
            }

            Destroy(videoRenderTexture);
            videoRenderTexture = null;
        }
    }
}
