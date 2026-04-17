using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ModÃ¼l 1 sahnesinin tÃ¼m iÃ§erik kartlarÄ±nÄ± runtime'da dinamik oluÅŸturur.
/// 
/// Ä°Ã§erik yapÄ±sÄ±:
/// - Ä°nfografikler: 9 kart (Deprem x3, Triyaj x3, YangÄ±n x3) + ScrollRect
/// - Sunumlar: 3 kart (Deprem, Triyaj, YangÄ±n)
/// - Videolar: 3 kart (Deprem, Triyaj, YangÄ±n)
/// </summary>
public class ContentLoader : MonoBehaviour
{
    private ContentViewerManager cachedViewerManager;

    // â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 
    //  Ä°NFOGRAFÄ°K Ä°Ã‡ERÄ°KLER (9 adet)
    // â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 

    [Header("â• â• â•  DEPREM Ä°NFOGRAFÄ°KLER â• â• â• ")]
    public Sprite depremInfo1;
    public Sprite depremInfo2;
    public Sprite depremInfo3;

    [Header("â• â• â•  TRÄ°YAJ Ä°NFOGRAFÄ°KLER â• â• â• ")]
    public Sprite triyajInfo1;
    public Sprite triyajInfo2;
    public Sprite triyajInfo3;

    [Header("â• â• â•  YANGIN Ä°NFOGRAFÄ°KLER â• â• â• ")]
    public Sprite yanginInfo1;
    public Sprite yanginInfo2;
    public Sprite yanginInfo3;

    // â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 
    //  SUNUM Ä°Ã‡ERÄ°KLER (3 adet)
    // â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 

    [Header("â• â• â•  SUNUM GÃ–RSELLERÄ° â• â• â• ")]
    [Tooltip("Deprem sunumu kapak gÃ¶rseli (PNG'ye dÃ¶nÃ¼ÅŸtÃ¼rÃ¼lmÃ¼ÅŸ slayt)")]
    public Sprite depremSunumKapak;
    [Tooltip("Triyaj sunumu kapak gÃ¶rseli")]
    public Sprite triyajSunumKapak;
    [Tooltip("YangÄ±n sunumu kapak gÃ¶rseli")]
    public Sprite yanginSunumKapak;

    [Header("Sunum SlaytlarÄ± (Opsiyonel)")]
    public List<Sprite> depremSunumSlides = new List<Sprite>();
    public List<Sprite> triyajSunumSlides = new List<Sprite>();
    public List<Sprite> yanginSunumSlides = new List<Sprite>();

    // â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 
    //  VÄ°DEO Ä°Ã‡ERÄ°KLER (3 adet)
    // â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 

    [Header("â• â• â•  VÄ°DEOLAR â• â• â• ")]
    public VideoClip depremVideo;
    public VideoClip triyajVideo;
    public VideoClip yanginVideo;

    [Header("Video Preview Images (Optional)")]
    public Sprite depremVideoPreviewImage;
    public Sprite triyajVideoPreviewImage;
    public Sprite yanginVideoPreviewImage;

    // â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 
    //  KART TASARIMI AYARLARI
    // â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 

    [Header("â• â• â•  KART TASARIMI â• â• â• ")]
    public float cardWidth = 520f;
    public float cardHeight = 740f;
    public float cardSpacing = 25f;
    public Color cardBgColor = new Color(0.058f, 0.086f, 0.16f, 1f);
    public Color cardOutlineColor = new Color(0f, 0.898f, 1f, 0.25f);
    public Color titleColor = Color.white;
    public Color descColor = new Color(0.53f, 0.53f, 0.67f, 1f);
    public Color depremAccent = new Color(0.2f, 0.6f, 1f, 1f);
    public Color triyajAccent = new Color(0.2f, 0.9f, 0.4f, 1f);
    public Color yanginAccent = new Color(1f, 0.4f, 0.2f, 1f);

    // Ä°Ã§erik verileri
    private struct CardData
    {
        public string title;
        public string description;
        public Sprite thumbnail;
        public Sprite fullImage;
        public VideoClip video;
        public List<Sprite> slideDeck;
        public Color accentColor;
        public string category; // "Deprem", "Triyaj", "YangÄ±n"
    }

    void Start()
    {
        EnsureContentViewerManager();
        AutoPopulateMissingContentReferencesInEditor();
        StartCoroutine(RebuildAllPanelsAsync());
    }

    System.Collections.IEnumerator RebuildAllPanelsAsync()
    {
        // Bir frame bekle â€” diÄŸer scriptler de baÅŸlasÄ±n
        yield return null;

        // Canvas'Ä±n XR etkileÅŸim iÃ§in TrackedDeviceGraphicRaycaster'a sahip olduÄŸundan emin ol
        EnsureCanvasXRRaycaster();

        RebuildInfographicsPanel();
        yield return null; // Frame ver

        RebuildPresentationsPanel();
        yield return null; // Frame ver

        RebuildVideosPanel();

        Debug.Log("[ContentLoader] TÃ¼m paneller oluÅŸturuldu: 9 infografik, 3 sunum, 3 video.");
    }

    /// <summary>
    /// Canvas'Ä±n XR ray etkileÅŸimi iÃ§in gerekli bileÅŸenlere sahip olduÄŸunu garanti eder.
    /// </summary>
    void EnsureCanvasXRRaycaster()
    {
        GameObject canvasObj = GameObject.Find("sinif/UI_Canvas");
        if (canvasObj == null)
        {
            // Fallback: herhangi bir Canvas bul
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null) canvasObj = canvas.gameObject;
        }

        if (canvasObj == null)
        {
            Debug.LogWarning("[ContentLoader] Canvas bulunamadÄ± â€” XR raycaster eklenemedi.");
            return;
        }

        // TrackedDeviceGraphicRaycaster yoksa ekle
        if (canvasObj.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
        {
            canvasObj.AddComponent<TrackedDeviceGraphicRaycaster>();
            Debug.Log("[ContentLoader] TrackedDeviceGraphicRaycaster Canvas'a eklendi.");
        }

        // Standart GraphicRaycaster yoksa da ekle (simÃ¼latÃ¶r iÃ§in)
        if (canvasObj.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObj.AddComponent<GraphicRaycaster>();
            Debug.Log("[ContentLoader] GraphicRaycaster Canvas'a eklendi.");
        }
    }

    ContentViewerManager EnsureContentViewerManager()
    {
        if (cachedViewerManager != null)
        {
            return cachedViewerManager;
        }

        cachedViewerManager = FindObjectOfType<ContentViewerManager>(true);
        if (cachedViewerManager != null)
        {
            if (!cachedViewerManager.enabled)
            {
                cachedViewerManager.enabled = true;
            }
            return cachedViewerManager;
        }

        GameObject host = GameObject.Find("sinif");
        if (host == null)
        {
            host = GameObject.Find("sinif/UI_Canvas");
        }

        if (host == null)
        {
            host = gameObject;
        }

        cachedViewerManager = host.GetComponent<ContentViewerManager>();
        if (cachedViewerManager == null)
        {
            cachedViewerManager = host.AddComponent<ContentViewerManager>();
            Debug.Log($"[ContentLoader] ContentViewerManager otomatik eklendi: {host.name}");
        }
        else if (!cachedViewerManager.enabled)
        {
            cachedViewerManager.enabled = true;
        }

        return cachedViewerManager;
    }

    Sprite ResolveSprite(params Sprite[] candidates)
    {
        if (candidates == null)
        {
            return null;
        }

        for (int i = 0; i < candidates.Length; i++)
        {
            if (candidates[i] != null)
            {
                return candidates[i];
            }
        }

        return null;
    }

    Sprite FirstSlide(List<Sprite> slides, Sprite fallback = null)
    {
        if (slides != null && slides.Count > 0 && slides[0] != null)
        {
            return slides[0];
        }

        return fallback;
    }

    void AutoPopulateMissingContentReferencesInEditor()
    {
#if UNITY_EDITOR
        bool changed = false;

        depremInfo1 = AssignSpriteIfMissing(depremInfo1, ref changed,
            "Assets/Content/Module1/Deprem/Infografik/info1.jpeg",
            "Assets/Textures/AFAD_ToplanmaAlani.png",
            "Assets/Content/Module1/Deprem/Sunum/deprem1/Slayt1.PNG");
        depremInfo2 = AssignSpriteIfMissing(depremInfo2, ref changed,
            "Assets/Content/Module1/Deprem/Infografik/info2.jpeg",
            "Assets/Textures/AFAD_GuvenliBolge.png",
            "Assets/Content/Module1/Deprem/Sunum/deprem1/Slayt2.PNG");
        depremInfo3 = AssignSpriteIfMissing(depremInfo3, ref changed,
            "Assets/Content/Module1/Deprem/Infografik/info3.jpeg",
            "Assets/Content/Module1/Deprem/Sunum/deprem1/Slayt3.PNG");

        triyajInfo1 = AssignSpriteIfMissing(triyajInfo1, ref changed,
            "Assets/Content/Module1/Triyaj/Infografik/info1.jpeg");
        triyajInfo2 = AssignSpriteIfMissing(triyajInfo2, ref changed,
            "Assets/Content/Module1/Triyaj/Infografik/info2.jpeg");
        triyajInfo3 = AssignSpriteIfMissing(triyajInfo3, ref changed,
            "Assets/Content/Module1/Triyaj/Infografik/info3.jpeg");

        yanginInfo1 = AssignSpriteIfMissing(yanginInfo1, ref changed,
            "Assets/Content/Module1/Yangin/Infografik/yangin1.png");
        yanginInfo2 = AssignSpriteIfMissing(yanginInfo2, ref changed,
            "Assets/Content/Module1/Yangin/Infografik/yangin2.png");
        yanginInfo3 = AssignSpriteIfMissing(yanginInfo3, ref changed,
            "Assets/Content/Module1/Yangin/Infografik/yangin3.png");

        depremSunumKapak = AssignSpriteIfMissing(depremSunumKapak, ref changed,
            "Assets/Content/Module1/Deprem/Sunum/deprem1/Slayt1.PNG",
            "Assets/Textures/AFAD_ToplanmaAlani.png");
        triyajSunumKapak = AssignSpriteIfMissing(triyajSunumKapak, ref changed,
            "Assets/Content/Module1/Triyaj/Sunum/triyaj1/Slayt1.PNG");
        yanginSunumKapak = AssignSpriteIfMissing(yanginSunumKapak, ref changed,
            "Assets/Content/Module1/Yangin/Sunum/yangin1/Slayt1.PNG");

        depremSunumSlides = AssignSlidesIfMissing(depremSunumSlides, ref changed,
            "Assets/Content/Module1/Deprem/Sunum/deprem1");
        triyajSunumSlides = AssignSlidesIfMissing(triyajSunumSlides, ref changed,
            "Assets/Content/Module1/Triyaj/Sunum/triyaj1");
        yanginSunumSlides = AssignSlidesIfMissing(yanginSunumSlides, ref changed,
            "Assets/Content/Module1/Yangin/Sunum/yangin1");

        depremVideo = AssignVideoIfMissing(depremVideo, ref changed,
            "Assets/Content/Module1/Deprem/Video/deprem1.mp4",
            "Assets/Content/Module1/Deprem/Video/deprem.mp4");
        triyajVideo = AssignVideoIfMissing(triyajVideo, ref changed,
            "Assets/Content/Module1/Triyaj/Video/triyaj1.mp4",
            "Assets/Content/Module1/Triyaj/Video/triyaj.mp4");
        yanginVideo = AssignVideoIfMissing(yanginVideo, ref changed,
            "Assets/Content/Module1/Yangin/Video/yangin1.mp4",
            "Assets/Content/Module1/Yangin/Video/yangin.mp4");

        depremVideoPreviewImage = AssignSpriteIfMissing(depremVideoPreviewImage, ref changed,
            "Assets/Content/Module1/Deprem/Video/preview.png",
            "Assets/Content/Module1/Deprem/Video/deprem1_preview.png",
            "Assets/Content/Module1/Deprem/Video/deprem1_thumb.png",
            "Assets/Content/Module1/Deprem/Video/deprem1_thumbnail.png");
        triyajVideoPreviewImage = AssignSpriteIfMissing(triyajVideoPreviewImage, ref changed,
            "Assets/Content/Module1/Triyaj/Video/preview.png",
            "Assets/Content/Module1/Triyaj/Video/triyaj1_preview.png",
            "Assets/Content/Module1/Triyaj/Video/triyaj1_thumb.png",
            "Assets/Content/Module1/Triyaj/Video/triyaj1_thumbnail.png");
        yanginVideoPreviewImage = AssignSpriteIfMissing(yanginVideoPreviewImage, ref changed,
            "Assets/Content/Module1/Yangin/Video/preview.png",
            "Assets/Content/Module1/Yangin/Video/yangin1_preview.png",
            "Assets/Content/Module1/Yangin/Video/yangin1_thumb.png",
            "Assets/Content/Module1/Yangin/Video/yangin1_thumbnail.png");

        if (changed)
        {
            EditorUtility.SetDirty(this);
            Debug.Log("[ContentLoader] Eksik iÃ§erik referanslarÄ± editor fallback ile tamamlandÄ±.");
        }
#endif
    }

#if UNITY_EDITOR
    Sprite AssignSpriteIfMissing(Sprite current, ref bool changed, params string[] assetPaths)
    {
        if (current != null)
        {
            return current;
        }

        for (int i = 0; i < assetPaths.Length; i++)
        {
            string path = assetPaths[i];
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            Sprite loaded = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (loaded == null)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                    loaded = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }

            if (loaded != null)
            {
                changed = true;
                return loaded;
            }
        }

        return null;
    }

    VideoClip AssignVideoIfMissing(VideoClip current, ref bool changed, params string[] assetPaths)
    {
        if (current != null)
        {
            return current;
        }

        for (int i = 0; i < assetPaths.Length; i++)
        {
            string path = assetPaths[i];
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            VideoClip loaded = AssetDatabase.LoadAssetAtPath<VideoClip>(path);
            if (loaded != null)
            {
                changed = true;
                return loaded;
            }
        }

        return null;
    }

    List<Sprite> AssignSlidesIfMissing(List<Sprite> current, ref bool changed, params string[] folders)
    {
        if (current != null && current.Count > 0)
        {
            return current;
        }

        var loadedSlides = new List<Sprite>();
        if (folders == null)
        {
            return loadedSlides;
        }

        for (int i = 0; i < folders.Length; i++)
        {
            string folder = folders[i];
            if (string.IsNullOrWhiteSpace(folder) || !AssetDatabase.IsValidFolder(folder))
            {
                continue;
            }

            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
            var paths = new List<string>(guids.Length);
            for (int g = 0; g < guids.Length; g++)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[g]));
            }

            paths.Sort(System.StringComparer.OrdinalIgnoreCase);
            for (int p = 0; p < paths.Count; p++)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(paths[p]);
                if (sprite != null)
                {
                    loadedSlides.Add(sprite);
                }
            }

            if (loadedSlides.Count > 0)
            {
                break;
            }
        }

        if (loadedSlides.Count > 0)
        {
            changed = true;
        }

        return loadedSlides;
    }
#endif

    // â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 
    //  Ä°NFOGRAFÄ°K PANELÄ° (9 kart + scroll)
    // â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 

    void RebuildInfographicsPanel()
    {
        GameObject panel = GameObject.Find(
            "sinif/UI_Canvas/FuturisticPanel/GlassContainer/ContentRoot/Infografikler");
        if (panel == null)
        {
            Debug.LogWarning("[ContentLoader] Infografikler paneli bulunamadÄ±.");
            return;
        }

        // Mevcut kartlarÄ± temizle
        ClearChildren(panel.transform);

        // ScrollRect ekle
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect == null) return;

        // LayoutElement'i kaldÄ±r (scroll ile Ã§eliÅŸebilir)
        LayoutElement le = panel.GetComponent<LayoutElement>();
        if (le != null) Object.Destroy(le);

        // VerticalLayoutGroup kaldÄ±r
        VerticalLayoutGroup vlg = panel.GetComponent<VerticalLayoutGroup>();
        if (vlg != null) Object.Destroy(vlg);

        // Panel'in anchor/size ayarlarÄ±
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // ScrollRect oluÅŸtur
        ScrollRect scrollRect = panel.GetComponent<ScrollRect>();
        if (scrollRect == null)
            scrollRect = panel.AddComponent<ScrollRect>();

        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.elasticity = 0.1f;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;
        scrollRect.scrollSensitivity = 30f;

        // Mask ekle
        Mask mask = panel.GetComponent<Mask>();
        if (mask == null)
            mask = panel.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        // Panel arka plan (mask'in Ã§alÄ±ÅŸmasÄ± iÃ§in Image gerekli)
        Image panelBg = panel.GetComponent<Image>();
        if (panelBg == null)
            panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.01f); // Neredeyse ÅŸeffaf

        // Content container
        GameObject contentObj = new GameObject("ScrollContent");
        contentObj.transform.SetParent(panel.transform, false);

        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0f);
        contentRect.anchorMax = new Vector2(0f, 1f);
        contentRect.pivot = new Vector2(0f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;

        // Horizontal layout
        HorizontalLayoutGroup hlg = contentObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = cardSpacing;
        hlg.padding = new RectOffset(20, 20, 20, 40);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.content = contentRect;

        // Eksik gorselleri runtime fallback ile tamamla (kartlarin bos gorunmesini onler).
        depremInfo1 = ResolveSprite(depremInfo1, depremSunumKapak, triyajInfo1, yanginInfo1);
        depremInfo2 = ResolveSprite(depremInfo2, depremSunumKapak, triyajInfo2, yanginInfo2);
        depremInfo3 = ResolveSprite(depremInfo3, depremSunumKapak, triyajInfo3, yanginInfo3);
        triyajInfo1 = ResolveSprite(triyajInfo1, triyajSunumKapak, depremInfo1, yanginInfo1);
        triyajInfo2 = ResolveSprite(triyajInfo2, triyajSunumKapak, depremInfo2, yanginInfo2);
        triyajInfo3 = ResolveSprite(triyajInfo3, triyajSunumKapak, depremInfo3, yanginInfo3);
        yanginInfo1 = ResolveSprite(yanginInfo1, yanginSunumKapak, triyajInfo1, depremInfo1);
        yanginInfo2 = ResolveSprite(yanginInfo2, yanginSunumKapak, triyajInfo2, depremInfo2);
        yanginInfo3 = ResolveSprite(yanginInfo3, yanginSunumKapak, triyajInfo3, depremInfo3);

        // 9 infografik kartı oluştur
        CardData[] infografikler = new CardData[]
        {
            MakeInfoCard("Deprem Acil Durum Planı", "Acil durum planlama adımları ve uygulama noktaları", depremInfo1, depremAccent, "Deprem"),
            MakeInfoCard("Deprem Çantası İçeriği", "Deprem çantasında bulunması gereken temel ekipmanlar", depremInfo2, depremAccent, "Deprem"),
            MakeInfoCard("Toplanma ve Tahliye Rehberi", "Toplanma alanı seçimi ve güvenli tahliye kuralları", depremInfo3, depremAccent, "Deprem"),

            MakeInfoCard("Triyaj Bilgi Kartı 1", "Triyaj sınıflandırma sistemi ve renk kodları", triyajInfo1, triyajAccent, "Triyaj"),
            MakeInfoCard("Triyaj Bilgi Kartı 2", "Yaralı değerlendirme kriterleri ve öncelik sırası", triyajInfo2, triyajAccent, "Triyaj"),
            MakeInfoCard("Triyaj Bilgi Kartı 3", "Saha triyaj uygulama adımları", triyajInfo3, triyajAccent, "Triyaj"),

            MakeInfoCard("Yangın Güvenlik Bilgi Kartı 1", "Yangın söndürücü kullanımı ve yangın türleri", yanginInfo1, yanginAccent, "Yangın"),
            MakeInfoCard("Yangın Güvenlik Bilgi Kartı 2", "Yangın tahliye planı ve kaçış yolları", yanginInfo2, yanginAccent, "Yangın"),
            MakeInfoCard("Yangın Güvenlik Bilgi Kartı 3", "İlk müdahale ve yangın önleme yöntemleri", yanginInfo3, yanginAccent, "Yangın"),
        };

        System.Array.Reverse(infografikler);

        var viewer = EnsureContentViewerManager();
        if (viewer != null)
        {
            var infoItems = new List<ContentViewerManager.ContentItem>();
            foreach(var c in infografikler)
            {
                infoItems.Add(new ContentViewerManager.ContentItem {
                    title = c.title, description = c.description,
                    thumbnail = c.thumbnail, fullImage = c.fullImage,
                    videoClip = c.video, contentType = 0
                });
            }
            viewer.SetContentList(0, infoItems);
        }

        for (int i = 0; i < infografikler.Length; i++)
        {
            CreateCardUI(contentObj.transform, infografikler[i], 0, i);
        }

        // Scroll gÃ¶stergesi (opsiyonel alt Ã§izgi)
        CreateScrollIndicator(panel.transform);
    }

    // â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 
    //  SUNUM PANELÄ° (3 kart)
    // â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 

    void RebuildPresentationsPanel()
    {
        GameObject panel = GameObject.Find(
            "sinif/UI_Canvas/FuturisticPanel/GlassContainer/ContentRoot/SunumlarPanel");
        if (panel == null)
        {
            Debug.LogWarning("[ContentLoader] SunumlarPanel bulunamadÄ±.");
            return;
        }

        ClearChildren(panel.transform);

        // Layout grubu kaldÄ±r ve yeniden oluÅŸtur
        VerticalLayoutGroup vlg = panel.GetComponent<VerticalLayoutGroup>();
        if (vlg != null) Object.Destroy(vlg);
        LayoutElement le = panel.GetComponent<LayoutElement>();
        if (le != null) Object.Destroy(le);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Yatay layout
        HorizontalLayoutGroup hlg = panel.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null)
            hlg = panel.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = cardSpacing;
        hlg.padding = new RectOffset(40, 40, 20, 50);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        depremSunumKapak = ResolveSprite(depremSunumKapak, FirstSlide(depremSunumSlides), depremInfo1, triyajSunumKapak);
        triyajSunumKapak = ResolveSprite(triyajSunumKapak, FirstSlide(triyajSunumSlides), triyajInfo1, depremSunumKapak);
        yanginSunumKapak = ResolveSprite(yanginSunumKapak, FirstSlide(yanginSunumSlides), yanginInfo1, triyajSunumKapak);

        CardData[] sunumlar = new CardData[]
        {
            new CardData
            {
                title = "Deprem Bilinçlendirme",
                description = "Deprem hazırlık ve müdahale sunumu",
                thumbnail = depremSunumKapak,
                fullImage = depremSunumKapak,
                slideDeck = depremSunumSlides,
                accentColor = depremAccent,
                category = "Deprem"
            },
            new CardData
            {
                title = "Triyaj Eğitim Sunumu",
                description = "Triyaj sınıflandırma ve uygulama sunumu",
                thumbnail = triyajSunumKapak,
                fullImage = triyajSunumKapak,
                slideDeck = triyajSunumSlides,
                accentColor = triyajAccent,
                category = "Triyaj"
            },
            new CardData
            {
                title = "Yangın Müdahale Sunumu",
                description = "Yangın söndürme ve tahliye sunumu",
                thumbnail = yanginSunumKapak,
                fullImage = yanginSunumKapak,
                slideDeck = yanginSunumSlides,
                accentColor = yanginAccent,
                category = "Yangın"
            }
        };

        foreach (var card in sunumlar)
        {
            CreateCardUI(panel.transform, card, 1);
        }
    }

    // â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 
    //  VÄ°DEO PANELÄ° (3 kart)
    // â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 

    void RebuildVideosPanel()
    {
        GameObject panel = GameObject.Find(
            "sinif/UI_Canvas/FuturisticPanel/GlassContainer/ContentRoot/VideolarPanel");
        if (panel == null)
        {
            Debug.LogWarning("[ContentLoader] VideolarPanel bulunamadÄ±.");
            return;
        }

        ClearChildren(panel.transform);

        VerticalLayoutGroup vlg = panel.GetComponent<VerticalLayoutGroup>();
        if (vlg != null) Object.Destroy(vlg);
        LayoutElement le = panel.GetComponent<LayoutElement>();
        if (le != null) Object.Destroy(le);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup hlg = panel.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null)
            hlg = panel.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = cardSpacing;
        hlg.padding = new RectOffset(40, 40, 10, 10);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        Sprite depremVideoPreview = ResolveSprite(depremVideoPreviewImage);
        Sprite triyajVideoPreview = ResolveSprite(triyajVideoPreviewImage);
        Sprite yanginVideoPreview = ResolveSprite(yanginVideoPreviewImage);

        CardData[] videolar = new CardData[]
        {
            new CardData
            {
                title = "Deprem Eğitim Videosu",
                description = "Deprem anında yapılması gerekenler",
                thumbnail = depremVideoPreview,
                fullImage = depremVideoPreview,
                video = depremVideo,
                accentColor = depremAccent,
                category = "Deprem"
            },
            new CardData
            {
                title = "Triyaj Eğitim Videosu",
                description = "Triyaj sınıflandırma uygulaması",
                thumbnail = triyajVideoPreview,
                fullImage = triyajVideoPreview,
                video = triyajVideo,
                accentColor = triyajAccent,
                category = "Triyaj"
            },
            new CardData
            {
                title = "Yangın Müdahale Videosu",
                description = "Yangın söndürme eğitimi",
                thumbnail = yanginVideoPreview,
                fullImage = yanginVideoPreview,
                video = yanginVideo,
                accentColor = yanginAccent,
                category = "Yangın"
            }
        };

        var viewer = EnsureContentViewerManager();
        if (viewer != null)
        {
            var videoItems = new List<ContentViewerManager.ContentItem>();
            foreach(var c in videolar)
            {
                videoItems.Add(new ContentViewerManager.ContentItem {
                    title = c.title, description = c.description,
                    thumbnail = c.thumbnail, fullImage = c.fullImage,
                    videoClip = c.video, contentType = 2
                });
            }
            viewer.SetContentList(2, videoItems);
        }

        for (int i = 0; i < videolar.Length; i++)
        {
            CreateCardUI(panel.transform, videolar[i], 2, i);
        }
    }

    // â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 
    //  KART UI OLUÅžTURUCU
    // â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 

    /// <summary>
    /// Tek bir iÃ§erik kartÄ± oluÅŸturur.
    /// contentType: 0=infografik, 1=sunum, 2=video
    /// </summary>
    void CreateCardUI(Transform parent, CardData data, int contentType, int contentIndex = -1)
    {
        // Ana kart container
        GameObject cardObj = new GameObject($"Card_{data.category}_{data.title.GetHashCode():X8}");
        cardObj.transform.SetParent(parent, false);

        RectTransform cardRect = cardObj.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);

        // Kart arka plan
        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.color = cardBgColor;

        // Outline
        Outline outline = cardObj.AddComponent<Outline>();
        outline.effectColor = cardOutlineColor;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // Layout element
        LayoutElement cardLE = cardObj.AddComponent<LayoutElement>();
        cardLE.preferredWidth = cardWidth;
        cardLE.minWidth = cardWidth;

        // â”€â”€â”€ Kategori etiketi (sol Ã¼st kÃ¶ÅŸe) â”€â”€â”€
        GameObject categoryObj = new GameObject("Category");
        categoryObj.transform.SetParent(cardObj.transform, false);

        RectTransform catRect = categoryObj.AddComponent<RectTransform>();
        catRect.anchorMin = new Vector2(0f, 1f);
        catRect.anchorMax = new Vector2(0f, 1f);
        catRect.pivot = new Vector2(0f, 1f);
        catRect.anchoredPosition = new Vector2(12f, -12f);
        catRect.sizeDelta = new Vector2(120f, 28f);

        Image catBg = categoryObj.AddComponent<Image>();
        catBg.color = new Color(data.accentColor.r, data.accentColor.g, data.accentColor.b, 0.25f);
        catBg.raycastTarget = false; // Buton tÄ±klamasÄ±nÄ± engellemesin

        GameObject catTextObj = new GameObject("CatText");
        catTextObj.transform.SetParent(categoryObj.transform, false);

        RectTransform catTextRect = catTextObj.AddComponent<RectTransform>();
        catTextRect.anchorMin = Vector2.zero;
        catTextRect.anchorMax = Vector2.one;
        catTextRect.offsetMin = Vector2.zero;
        catTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI catText = catTextObj.AddComponent<TextMeshProUGUI>();
        catText.text = data.category;
        catText.fontSize = 18;
        catText.alignment = TextAlignmentOptions.Center;
        catText.color = data.accentColor;
        catText.fontStyle = FontStyles.Bold;
        catText.raycastTarget = false; // Buton tÄ±klamasÄ±nÄ± engellemesin

        // â”€â”€â”€ Thumbnail â”€â”€â”€
        GameObject thumbObj = new GameObject("Thumbnail");
        thumbObj.transform.SetParent(cardObj.transform, false);

        RectTransform thumbRect = thumbObj.AddComponent<RectTransform>();
        thumbRect.anchorMin = new Vector2(0f, 1f);
        thumbRect.anchorMax = new Vector2(1f, 1f);
        thumbRect.pivot = new Vector2(0.5f, 1f);
        thumbRect.anchoredPosition = new Vector2(0f, -24f);
        thumbRect.sizeDelta = new Vector2(-24f, 400f);

        Image thumbImage = thumbObj.AddComponent<Image>();
        thumbImage.raycastTarget = false; // Buton tÄ±klamasÄ±nÄ± engellemesin
        if (data.thumbnail != null)
        {
            thumbImage.sprite = data.thumbnail;
            thumbImage.color = Color.white;
            thumbImage.preserveAspect = true;
            if (contentType == 2)
            {
                CreatePlayIcon(thumbObj.transform);
            }
        }
        else if (contentType == 2) // Video â€” play ikonu
        {
            thumbImage.color = new Color(0.05f, 0.08f, 0.15f, 1f);
            CreatePlayIcon(thumbObj.transform);
        }
        else
        {
            thumbImage.color = new Color(0.1f, 0.15f, 0.25f, 1f);
        }

        // â”€â”€â”€ Accent line â”€â”€â”€
        GameObject lineObj = new GameObject("AccentLine");
        lineObj.transform.SetParent(cardObj.transform, false);

        RectTransform lineRect = lineObj.AddComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0f, 1f);
        lineRect.anchorMax = new Vector2(1f, 1f);
        lineRect.pivot = new Vector2(0.5f, 1f);
        lineRect.anchoredPosition = new Vector2(0f, -434f);
        lineRect.sizeDelta = new Vector2(-24f, 3f);

        Image lineImage = lineObj.AddComponent<Image>();
        lineImage.color = data.accentColor;
        lineImage.raycastTarget = false; // Buton tÄ±klamasÄ±nÄ± engellemesin

        // â”€â”€â”€ BaÅŸlÄ±k â”€â”€â”€
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(cardObj.transform, false);

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(18f, -445f);
        titleRect.sizeDelta = new Vector2(-36f, 45f);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = data.title;
        titleText.fontSize = 26;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.TopLeft;
        titleText.color = titleColor;
        titleText.enableWordWrapping = true;
        titleText.overflowMode = TextOverflowModes.Ellipsis;
        titleText.raycastTarget = false; // Buton tÄ±klamasÄ±nÄ± engellemesin

        // â”€â”€â”€ AÃ§Ä±klama â”€â”€â”€
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(cardObj.transform, false);

        RectTransform descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0f, 1f);
        descRect.anchorMax = new Vector2(1f, 1f);
        descRect.pivot = new Vector2(0f, 1f);
        descRect.anchoredPosition = new Vector2(18f, -495f);
        descRect.sizeDelta = new Vector2(-36f, 50f);

        TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = data.description;
        descText.fontSize = 20;
        descText.alignment = TextAlignmentOptions.TopLeft;
        descText.color = descColor;
        descText.enableWordWrapping = true;
        descText.overflowMode = TextOverflowModes.Ellipsis;
        descText.raycastTarget = false; // Buton tÄ±klamasÄ±nÄ± engellemesin

        // â”€â”€â”€ "AÃ§" Butonu â”€â”€â”€
        GameObject btnObj = new GameObject("OpenButton");
        btnObj.transform.SetParent(cardObj.transform, false);

        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1f, 0f);
        btnRect.anchorMax = new Vector2(1f, 0f);
        btnRect.pivot = new Vector2(1f, 0f);
        btnRect.anchoredPosition = new Vector2(-18f, 25f);
        btnRect.sizeDelta = new Vector2(128f, 44f);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.raycastTarget = true; // Bu butonun tÄ±klanabilir olmasÄ± ÅžART
        btnBg.color = new Color(data.accentColor.r * 0.6f, data.accentColor.g * 0.6f,
            data.accentColor.b * 0.6f, 0.9f);

        Outline btnOutline = btnObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(data.accentColor.r, data.accentColor.g,
            data.accentColor.b, 0.6f);
        btnOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.85f, 0.95f, 1f, 1f);
        colors.pressedColor = new Color(0.7f, 0.9f, 1f, 1f);
        btn.colors = colors;

        GameObject btnTextObj = new GameObject("BtnText");
        btnTextObj.transform.SetParent(btnObj.transform, false);

        RectTransform btnTextRect = btnTextObj.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = contentType == 2 ? "Oynat" : "Ac";
        btnText.fontSize = 22;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        btnText.raycastTarget = false; // Butonun Ã¼stÃ¼ndeki metin raycast'Ä± engellemesin

        // Buton tÄ±klama iÅŸlevi
        int ct = contentType;
        Sprite fi = data.fullImage != null ? data.fullImage : data.thumbnail;
        VideoClip vc = data.video;
        List<Sprite> sd = data.slideDeck;
        string t = data.title;

        btn.onClick.AddListener(() =>
        {
            Debug.Log($"[ContentLoader] 'AÃ§' butonuna tÄ±klandÄ±: {t} (tip={ct})");
            var viewer = EnsureContentViewerManager();
            if (viewer != null)
            {
                if (ct == 1 && sd != null && sd.Count > 0)
                {
                    viewer.ShowPresentationSlidesDirect(t, sd, fi);
                }
                else if (contentIndex >= 0)
                {
                    viewer.ShowContent(ct, contentIndex);
                }
                else
                {
                    viewer.ShowContentDirect(t, fi, vc, ct);
                }
            }
            else
            {
                Debug.LogWarning($"[ContentLoader] ContentViewerManager bulunamadÄ±: {t}");
            }
        });
    }

    // â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 
    //  YARDIMCI METOTLAR
    // â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• â• 

    CardData MakeInfoCard(string title, string desc, Sprite image, Color accent, string category)
    {
        return new CardData
        {
            title = title,
            description = desc,
            thumbnail = image,
            fullImage = image,
            accentColor = accent,
            category = category
        };
    }

    void CreatePlayIcon(Transform parent)
    {
        GameObject iconObj = new GameObject("PlayIcon");
        iconObj.transform.SetParent(parent, false);

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(80f, 80f);

        TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
        iconText.text = "PLAY";
        iconText.fontSize = 60;
        iconText.alignment = TextAlignmentOptions.Center;
        iconText.color = new Color(1f, 1f, 1f, 0.7f);
        iconText.raycastTarget = false;
    }

    void CreateScrollIndicator(Transform parent)
    {
        GameObject indicatorObj = new GameObject("ScrollIndicator");
        indicatorObj.transform.SetParent(parent, false);

        RectTransform indRect = indicatorObj.AddComponent<RectTransform>();
        indRect.anchorMin = new Vector2(0.3f, 0f);
        indRect.anchorMax = new Vector2(0.7f, 0f);
        indRect.pivot = new Vector2(0.5f, 0f);
        indRect.anchoredPosition = new Vector2(0f, 20f);
        indRect.sizeDelta = new Vector2(0f, 20f);

        TextMeshProUGUI indText = indicatorObj.AddComponent<TextMeshProUGUI>();
        indText.text = "< Kaydirarak daha fazla icerik gorun >";
        indText.fontSize = 16;
        indText.alignment = TextAlignmentOptions.Center;
        indText.color = new Color(0f, 0.898f, 1f, 0.5f);
        indText.fontStyle = FontStyles.Italic;
    }

    void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(parent.GetChild(i).gameObject);
        }
    }
}
