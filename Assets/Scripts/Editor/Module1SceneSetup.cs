#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Modül 1 sahnesine VR Klavye ve ContentLoader bileşenlerini ekleyen
/// Editor menü komutu. Sahne açıkken Tools menüsünden çalıştırılır.
/// 
/// Tüm içerikleri otomatik olarak atar:
/// - 9 infografik (Deprem x3, Triyaj x3, Yangın x3)
/// - 3 video (Deprem, Triyaj, Yangın)
/// </summary>
public static class Module1SceneSetup
{
    [MenuItem("Tools/Modül 1 Sahne Kurulumu")]
    public static void SetupModule1Scene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "Modul1")
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Uyarı",
                $"Aktif sahne '{activeScene.name}'. Bu ayarlar Modul1 sahnesi için tasarlanmıştır.\nDevam etmek istiyor musunuz?",
                "Devam Et", "İptal");

            if (!proceed) return;
        }

        // 1. Görsel import ayarlarını düzelt
        ReimportAllContentTextures();

        // 2. VR Keyboard ekle
        SetupVRKeyboard();

        // 3. UISetupExecutor ekle (tab + searchbar klavye)
        SetupUISetupExecutor();

        // 4. ContentViewerManager ekle
        SetupContentViewerManager();

        // 5. ContentLoader ekle ve içerikleri ata
        SetupContentLoader();

        // 6. Sahneyi dirty işaretle
        EditorSceneManager.MarkSceneDirty(activeScene);

        Debug.Log("[Module1SceneSetup] Sahne kurulumu tamamlandı!");
        EditorUtility.DisplayDialog("Kurulum Tamamlandı",
            "Tüm bileşenler eklendi ve içerikler atandı:\n\n" +
            "✓ 9 İnfografik (Deprem, Triyaj, Yangın)\n" +
            "✓ 3 Video (Deprem, Triyaj, Yangın)\n" +
            "✓ 3 Sunum (PNG dosyaları varsa)\n" +
            "✓ VR Sanal Klavye\n" +
            "✓ UISetupExecutor (Tab + SearchBar)\n" +
            "✓ ContentViewerManager (Paneller ve içerik görüntüleme)\n\n" +
            "Sahneyi kaydetmeyi unutmayın (Ctrl+S).",
            "Tamam");
    }

    static void SetupContentViewerManager()
    {
        GameObject sinif = GameObject.Find("sinif");
        if (sinif == null) return;

        ContentViewerManager existing = sinif.GetComponent<ContentViewerManager>();
        if (existing == null)
        {
            sinif.AddComponent<ContentViewerManager>();
            Debug.Log("[Module1SceneSetup] ContentViewerManager sinif objesine eklendi.");
        }
    }

    static void SetupUISetupExecutor()
    {
        GameObject sinif = GameObject.Find("sinif");
        if (sinif == null)
        {
            Debug.LogError("[Module1SceneSetup] sinif objesi bulunamadı!");
            return;
        }

        UISetupExecutor existing = sinif.GetComponent<UISetupExecutor>();
        if (existing == null)
        {
            sinif.AddComponent<UISetupExecutor>();
            Debug.Log("[Module1SceneSetup] UISetupExecutor sinif objesine eklendi.");
        }
        else
        {
            Debug.Log("[Module1SceneSetup] UISetupExecutor zaten mevcut.");
        }
    }

    static void ReimportAllContentTextures()
    {
        string[] paths = new string[]
        {
            // Deprem infografikler
            "Assets/Content/Module1/Deprem/Infografik/info1.jpeg",
            "Assets/Content/Module1/Deprem/Infografik/info2.jpeg",
            "Assets/Content/Module1/Deprem/Infografik/info3.jpeg",
            // Triyaj infografikler
            "Assets/Content/Module1/Triyaj/Infografik/info1.jpeg",
            "Assets/Content/Module1/Triyaj/Infografik/info2.jpeg",
            "Assets/Content/Module1/Triyaj/Infografik/info3.jpeg",
            // Yangın infografikler
            "Assets/Content/Module1/Yangin/Infografik/yangin1.png",
            "Assets/Content/Module1/Yangin/Infografik/yangin2.png",
            "Assets/Content/Module1/Yangin/Infografik/yangin3.png",
        };

        int count = 0;
        foreach (string path in paths)
        {
            if (EnsureSpriteImport(path))
                count++;
        }

        Debug.Log($"[Module1SceneSetup] {count} görsel Sprite olarak ayarlandı.");
    }

    static bool EnsureSpriteImport(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return false;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
            return true;
        }
        return false;
    }

    static void SetupVRKeyboard()
    {
        GameObject uiCanvas = GameObject.Find("sinif/UI_Canvas");
        if (uiCanvas == null)
        {
            Debug.LogError("[Module1SceneSetup] sinif/UI_Canvas bulunamadı!");
            return;
        }

        VRKeyboard existingKeyboard = Object.FindObjectOfType<VRKeyboard>(true);
        if (existingKeyboard != null)
        {
            Debug.Log("[Module1SceneSetup] VRKeyboard zaten mevcut, atlanıyor.");
            return;
        }

        GameObject keyboardObj = new GameObject("VRKeyboard");
        keyboardObj.transform.SetParent(uiCanvas.transform, false);

        RectTransform keyboardRect = keyboardObj.AddComponent<RectTransform>();
        keyboardRect.anchorMin = new Vector2(0.5f, 0f);
        keyboardRect.anchorMax = new Vector2(0.5f, 0f);
        keyboardRect.pivot = new Vector2(0.5f, 1f);
        keyboardRect.anchoredPosition = new Vector2(0f, -20f);
        keyboardRect.sizeDelta = new Vector2(1000f, 500f);

        keyboardObj.AddComponent<VRKeyboard>();
        Debug.Log("[Module1SceneSetup] VRKeyboard eklendi.");
    }

    static void SetupContentLoader()
    {
        GameObject sinif = GameObject.Find("sinif");
        if (sinif == null)
        {
            Debug.LogError("[Module1SceneSetup] sinif objesi bulunamadı!");
            return;
        }

        // Eski ContentLoader varsa kaldır (alan yapısı değişmiş olabilir)
        ContentLoader oldLoader = sinif.GetComponent<ContentLoader>();
        if (oldLoader != null)
        {
            Object.DestroyImmediate(oldLoader);
            Debug.Log("[Module1SceneSetup] Eski ContentLoader kaldırıldı.");
        }

        // Yeni ContentLoader ekle
        ContentLoader loader = sinif.AddComponent<ContentLoader>();

        // ═══ DEPREM İNFOGRAFİKLER ═══
        loader.depremInfo1 = LoadSprite("Assets/Content/Module1/Deprem/Infografik/info1.jpeg");
        loader.depremInfo2 = LoadSprite("Assets/Content/Module1/Deprem/Infografik/info2.jpeg");
        loader.depremInfo3 = LoadSprite("Assets/Content/Module1/Deprem/Infografik/info3.jpeg");

        // ═══ TRİYAJ İNFOGRAFİKLER ═══
        loader.triyajInfo1 = LoadSprite("Assets/Content/Module1/Triyaj/Infografik/info1.jpeg");
        loader.triyajInfo2 = LoadSprite("Assets/Content/Module1/Triyaj/Infografik/info2.jpeg");
        loader.triyajInfo3 = LoadSprite("Assets/Content/Module1/Triyaj/Infografik/info3.jpeg");

        // ═══ YANGIN İNFOGRAFİKLER ═══
        loader.yanginInfo1 = LoadSprite("Assets/Content/Module1/Yangin/Infografik/yangin1.png");
        loader.yanginInfo2 = LoadSprite("Assets/Content/Module1/Yangin/Infografik/yangin2.png");
        loader.yanginInfo3 = LoadSprite("Assets/Content/Module1/Yangin/Infografik/yangin3.png");

        // ═══ VİDEOLAR ═══
        loader.depremVideo = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>(
            "Assets/Content/Module1/Deprem/Video/deprem1.mp4");
        loader.triyajVideo = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>(
            "Assets/Content/Module1/Triyaj/Video/triyaj1.mp4");
        loader.yanginVideo = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>(
            "Assets/Content/Module1/Yangin/Video/yangin1.mp4");

        // Opsiyonel video preview gorselleri (varsa kartlarda kullanilir)
        loader.depremVideoPreviewImage = LoadSprite("Assets/Content/Module1/Deprem/Video/preview.png");
        loader.triyajVideoPreviewImage = LoadSprite("Assets/Content/Module1/Triyaj/Video/preview.png");
        loader.yanginVideoPreviewImage = LoadSprite("Assets/Content/Module1/Yangin/Video/preview.png");

        // ═══ SUNUMLAR (PNG slaytları otomatik bul) ═══
        AutoAssignSunumSlides(loader, "Deprem", "Assets/Content/Module1/Deprem/Sunum");
        AutoAssignSunumSlides(loader, "Triyaj", "Assets/Content/Module1/Triyaj/Sunum");
        AutoAssignSunumSlides(loader, "Yangin", "Assets/Content/Module1/Yangin/Sunum");

        // Log
        int infografik = 0;
        if (loader.depremInfo1 != null) infografik++;
        if (loader.depremInfo2 != null) infografik++;
        if (loader.depremInfo3 != null) infografik++;
        if (loader.triyajInfo1 != null) infografik++;
        if (loader.triyajInfo2 != null) infografik++;
        if (loader.triyajInfo3 != null) infografik++;
        if (loader.yanginInfo1 != null) infografik++;
        if (loader.yanginInfo2 != null) infografik++;
        if (loader.yanginInfo3 != null) infografik++;

        int video = 0;
        if (loader.depremVideo != null) video++;
        if (loader.triyajVideo != null) video++;
        if (loader.yanginVideo != null) video++;

        int sunum = 0;
        if (loader.depremSunumKapak != null) sunum++;
        if (loader.triyajSunumKapak != null) sunum++;
        if (loader.yanginSunumKapak != null) sunum++;

        Debug.Log($"[Module1SceneSetup] ContentLoader: {infografik}/9 infografik, {video}/3 video, {sunum}/3 sunum atandı.");

        EditorUtility.SetDirty(loader);
    }

    /// <summary>
    /// Belirtilen klasördeki PNG/JPG dosyalarını bulur,
    /// ilkini kapak olarak, hepsini slayt listesine atar.
    /// </summary>
    static void AutoAssignSunumSlides(ContentLoader loader, string category, string folderPath)
    {
        // Klasördeki tüm görselleri bul (PNG ve JPG)
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });

        if (guids.Length == 0)
        {
            Debug.Log($"[Module1SceneSetup] {category} sunum klasöründe görsel bulunamadı: {folderPath}");
            return;
        }

        // Sıralı liste oluştur
        var slides = new System.Collections.Generic.List<Sprite>();

        // Yolları sırala (slide1, slide2... veya Slayt1, Slayt2... sırasıyla)
        var paths = new System.Collections.Generic.List<string>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // PPTX değilse ekle (sadece görsel dosyalar)
            if (!path.EndsWith(".pptx") && !path.EndsWith(".ppt"))
            {
                paths.Add(path);
            }
        }
        paths.Sort(); // Alfabetik sırala

        foreach (string path in paths)
        {
            // Sprite olarak import et
            EnsureSpriteImport(path);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                slides.Add(sprite);
            }
        }

        if (slides.Count == 0) return;

        // Kapak görseli = ilk slayt
        Sprite kapak = slides[0];

        // Kategoriyle eşleştir
        switch (category)
        {
            case "Deprem":
                loader.depremSunumKapak = kapak;
                loader.depremSunumSlides = slides;
                break;
            case "Triyaj":
                loader.triyajSunumKapak = kapak;
                loader.triyajSunumSlides = slides;
                break;
            case "Yangin":
                loader.yanginSunumKapak = kapak;
                loader.yanginSunumSlides = slides;
                break;
        }

        Debug.Log($"[Module1SceneSetup] {category} sunum: {slides.Count} slayt bulundu ve atandı.");
    }

    static Sprite LoadSprite(string path)
    {
        EnsureSpriteImport(path);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
