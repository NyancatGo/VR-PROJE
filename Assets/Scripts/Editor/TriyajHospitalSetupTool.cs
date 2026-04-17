#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class TriyajHospitalSetupTool
{
    private readonly struct HospitalCaseDefinition
    {
        public readonly string complaintText;
        public readonly TriageCategory actualCategory;

        public HospitalCaseDefinition(string complaintText, TriageCategory actualCategory)
        {
            this.complaintText = complaintText;
            this.actualCategory = actualCategory;
        }
    }

    private static readonly Color GlassPanelColor = new Color(0.05f, 0.12f, 0.2f, 0.84f);
    private static readonly Color GlassShadowColor = new Color(0.01f, 0.04f, 0.1f, 0.34f);
    private static readonly Color GlassTextPrimary = new Color(0.96f, 0.985f, 1f, 1f);
    private static readonly Color GlassTextSecondary = new Color(0.79f, 0.9f, 0.98f, 1f);
    private static readonly Color GlassAccentBlue = new Color(0.21f, 0.62f, 0.9f, 0.92f);
    private static readonly Color GlassAccentGreen = new Color(0.16f, 0.73f, 0.56f, 0.92f);
    private static readonly Color GlassAccentYellow = new Color(0.94f, 0.75f, 0.28f, 0.92f);
    private static readonly Color GlassAccentRed = new Color(0.86f, 0.33f, 0.36f, 0.92f);
    private static readonly Color GlassAccentSlate = new Color(0.2f, 0.25f, 0.35f, 0.92f);
    private static readonly Color GlassAccentGlow = new Color(0.36f, 0.84f, 1f, 0.92f);
    private static readonly Vector2 TriageButtonCellSize = new Vector2(320f, 140f);
    private static readonly Vector2 TriageButtonSpacing = new Vector2(30f, 30f);
    private static readonly Vector2 TriageButtonContainerSize = new Vector2(670f, 310f);
    private static readonly Vector2 TriageButtonContainerOffset = new Vector2(0f, 78f);
    private static readonly string[] RequiredHospitalSceneNames = { "Modul1", "Modül3_Triyaj" };

    private static readonly HospitalCaseDefinition[] CaseLibrary =
    {
        new HospitalCaseDefinition("Ayakta hafif burkulma var, kendi başına yürüyebiliyor ve ağrısı tolere edilebilir düzeyde.", TriageCategory.Green),
        new HospitalCaseDefinition("Ön kolda yüzeysel kesik var, kanama kontrol altında ve vital bulguları stabil.", TriageCategory.Green),
        new HospitalCaseDefinition("Düşme sonrası dizinde morluk ve hafif şişlik var, destekle yürüyebiliyor.", TriageCategory.Green),
        new HospitalCaseDefinition("Baş ağrısı ve hafif mide bulantısı tarifliyor, bilinç açık ve genel durumu iyi.", TriageCategory.Green),
        new HospitalCaseDefinition("El sırtında küçük yanık alanı var, solunumu rahat ve dolaşımı stabil.", TriageCategory.Green),
        new HospitalCaseDefinition("Bileğinde ağrı var ama deformite yok, parmak hareketleri ve dolaşımı normal.", TriageCategory.Green),
        new HospitalCaseDefinition("Ön kolunda belirgin şekil bozukluğu var, dolaşımı korunmuş ama şiddetli ağrısı mevcut.", TriageCategory.Yellow),
        new HospitalCaseDefinition("Karın ağrısı ve tekrarlayan kusması var, bilinç açık fakat yakın takip gerekiyor.", TriageCategory.Yellow),
        new HospitalCaseDefinition("Orta şiddette nefes darlığı var, cümle kurabiliyor ama yardımcı solunum kaslarını kullanıyor.", TriageCategory.Yellow),
        new HospitalCaseDefinition("Kafa travması sonrası baş dönmesi ve kısa süreli baygınlık öyküsü var, şu an uyanık.", TriageCategory.Yellow),
        new HospitalCaseDefinition("Bacakta derin kesi mevcut, kanama bası ile azalıyor ancak acil müdahale gerekiyor.", TriageCategory.Yellow),
        new HospitalCaseDefinition("Yüksek ateş, halsizlik ve sıvı kaybı bulguları var; hemodinamik olarak sınırda ama stabil.", TriageCategory.Yellow),
        new HospitalCaseDefinition("Şiddetli dış kanaması var, cilt soğuk-terli ve nabız zayıf alınıyor.", TriageCategory.Red),
        new HospitalCaseDefinition("Solunumu yüzeyel ve çok hızlı, dudaklarda morarma başlamış.", TriageCategory.Red),
        new HospitalCaseDefinition("Bilinç dalgalanıyor, komutlara zor yanıt veriyor ve nabzı çok zayıf.", TriageCategory.Red),
        new HospitalCaseDefinition("Açık femur kırığı var, ciddi ağrı ve dolaşım bozukluğu bulguları mevcut.", TriageCategory.Red),
        new HospitalCaseDefinition("Anafilaksi düşündüren yaygın döküntü, wheezing ve hızla artan solunum sıkıntısı var.", TriageCategory.Red),
        new HospitalCaseDefinition("Göğüs travması sonrası tek taraflı solunum sesleri azalmış ve ağır dispnesi var.", TriageCategory.Red),
        new HospitalCaseDefinition("Hasta solunumsuz, nabız alınamıyor ve temel yaşam bulgusu saptanamıyor.", TriageCategory.Black),
        new HospitalCaseDefinition("Ağır kafa travması mevcut, pupiller yanıtsız ve spontan solunum yok.", TriageCategory.Black),
        new HospitalCaseDefinition("İleri derecede yanık ve yaşam bulgusu alınamayan hasta, resüsitasyon yanıtı yok.", TriageCategory.Black),
        new HospitalCaseDefinition("Uzun süre enkaz altında kalmış, dolaşım ve solunum bulgusu saptanamıyor.", TriageCategory.Black),
        new HospitalCaseDefinition("Çoklu travma sonrası apneik ve nabızsız, hava yolu açılmasına rağmen yanıt yok.", TriageCategory.Black),
        new HospitalCaseDefinition("Masif göğüs ve karın travması var, spontan hareket ve yaşam belirtisi izlenmiyor.", TriageCategory.Black),
    };

    private static bool useDefaultSavePaths;
    private const string BrokenSchoolMaterialPath = "Assets/school/material/Materials/1.mat";
    private const string BrokenSchoolTexturePath = "Assets/school/material/1.png";
    private const string RecoveredWallMaterialPath = "Assets/school/material/Materials/Recovered_Wall.mat";
    private const string RecoveredFloorMaterialPath = "Assets/school/material/Materials/Recovered_Floor.mat";
    private const string RecoveredDoorMaterialPath = "Assets/school/material/Materials/Recovered_Door.mat";
    private const string SchoolMaterialFolderPath = "Assets/school/material";
    private const string HospitalMaterialFolderPath = "Assets/Dnk_Dev/HospitalHorrorPack/Models/Materials";
    private const string HospitalWallMaterialPath = "Assets/Dnk_Dev/HospitalHorrorPack/Models/Materials/Mat_Walllime01_C.mat";
    private const string HospitalFloorMaterialPath = "Assets/Dnk_Dev/HospitalHorrorPack/Models/Materials/Mat_Tile01.mat";
    private const string HospitalDoorMaterialPath = "Assets/Dnk_Dev/HospitalHorrorPack/Models/Materials/Mat_Door_01.mat";
    private const string HospitalMetalMaterialPath = "Assets/Dnk_Dev/HospitalHorrorPack/Models/Materials/Mat_Bed_Metal_01.mat";
    private const string HospitalBeddingMaterialPath = "Assets/Dnk_Dev/HospitalHorrorPack/Models/Materials/M_BedBedding.mat";
    private const string HospitalLampMaterialPath = "Assets/Dnk_Dev/HospitalHorrorPack/Models/Materials/Mat_Lamp_Off.mat";

    [MenuItem("Tools/Triyaj Hastane/1 TIKLA Varsayilan Prefablari Olustur")]
    public static void BuildDefaultHospitalAssets()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/Triyaj");

        useDefaultSavePaths = true;
        CreateTriageButtonsCanvas();
        CreateResultCanvas();
        CreateProgressHudCanvas();
        useDefaultSavePaths = false;

        StandardizeSceneHospitalFlow();
        EnsureHospitalScenesInBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Triyaj Hastane/Sahneyi Standardize Et")]
    public static void StandardizeSceneHospitalFlow()
    {
        UpgradeSceneUiIfPresent();
        Transform retryAnchor = EnsureHospitalRetryAnchor();
        Transform baseAnchor = EnsureBaseReturnAnchor();
        WireDialogCanvasToSceneNpcs();
        AssignUniqueCasesToSceneNpcs();
        HospitalTriageManager manager = EnsureHospitalManagerExists();
        WireHospitalManager(manager, retryAnchor, baseAnchor);
        EnsureSceneEventSystemUsesXRUiInput();
        EnsureHospitalScenesInBuildSettings();
        MarkActiveSceneDirty();
    }

    [MenuItem("Tools/Triyaj Hastane/Hastane Materyallerini Onar")]
    public static void RepairHospitalMaterials()
    {
        GameObject hospitalRoot = FindHospitalRoot();
        if (hospitalRoot == null)
        {
            Debug.LogWarning("[TriyajHospitalSetupTool] Triyaj_hastane koku aktif sahnede bulunamadi.");
            return;
        }

        Material wallMaterial = AssetDatabase.LoadAssetAtPath<Material>(HospitalWallMaterialPath);
        Material floorMaterial = AssetDatabase.LoadAssetAtPath<Material>(HospitalFloorMaterialPath);
        Material doorMaterial = AssetDatabase.LoadAssetAtPath<Material>(HospitalDoorMaterialPath);
        Material metalMaterial = AssetDatabase.LoadAssetAtPath<Material>(HospitalMetalMaterialPath);
        Material beddingMaterial = AssetDatabase.LoadAssetAtPath<Material>(HospitalBeddingMaterialPath);
        Material lampMaterial = AssetDatabase.LoadAssetAtPath<Material>(HospitalLampMaterialPath);

        if (wallMaterial == null || floorMaterial == null || doorMaterial == null || metalMaterial == null || beddingMaterial == null)
        {
            Debug.LogError("[TriyajHospitalSetupTool] Hastane onarim materyalleri yuklenemedi.");
            return;
        }

        Renderer[] renderers = hospitalRoot.GetComponentsInChildren<Renderer>(true);
        int updatedRendererCount = 0;
        int updatedSlotCount = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material[] sharedMaterials = renderer.sharedMaterials;
            if (sharedMaterials == null || sharedMaterials.Length == 0)
            {
                continue;
            }

            bool rendererChanged = false;
            for (int slotIndex = 0; slotIndex < sharedMaterials.Length; slotIndex++)
            {
                Material currentMaterial = sharedMaterials[slotIndex];
                Material replacementMaterial = ResolveHospitalReplacementMaterial(renderer.transform, wallMaterial, floorMaterial, doorMaterial, metalMaterial, beddingMaterial, lampMaterial);
                if (replacementMaterial == null)
                {
                    continue;
                }

                if (ReferenceEquals(currentMaterial, replacementMaterial))
                {
                    continue;
                }

                if (!IsBrokenHospitalMaterial(currentMaterial) && !ShouldForceHospitalReplacement(currentMaterial, replacementMaterial))
                {
                    continue;
                }

                sharedMaterials[slotIndex] = replacementMaterial;
                rendererChanged = true;
                updatedSlotCount++;
            }

            if (!rendererChanged)
            {
                continue;
            }

            Undo.RecordObject(renderer, "Repair hospital materials");
            renderer.sharedMaterials = sharedMaterials;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            EditorUtility.SetDirty(renderer);
            updatedRendererCount++;
        }

        if (updatedRendererCount == 0)
        {
            Debug.Log("[TriyajHospitalSetupTool] Onarilacak bozuk hastane materyali bulunamadi.");
            return;
        }

        MarkActiveSceneDirty();
        Selection.activeGameObject = hospitalRoot;
        Debug.Log($"[TriyajHospitalSetupTool] {updatedRendererCount} renderer ve {updatedSlotCount} materyal slotu hastane gorunumu icin onarildi.");
    }

    [MenuItem("Tools/Triyaj Hastane/Hospital Packi Zorla Reimport Et")]
    public static void ForceReimportHospitalPack()
    {
        string[] importTargets =
        {
            "Assets/Dnk_Dev/HospitalHorrorPack/Textures",
            "Assets/Dnk_Dev/HospitalHorrorPack/Models/Materials",
            "Assets/Dnk_Dev/HospitalHorrorPack/Prefab"
        };

        for (int i = 0; i < importTargets.Length; i++)
        {
            string target = importTargets[i];
            if (!AssetDatabase.IsValidFolder(target))
            {
                continue;
            }

            AssetDatabase.ImportAsset(target, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        RepairHospitalMaterials();
        Debug.Log("[TriyajHospitalSetupTool] HospitalHorrorPack klasoru zorla yeniden import edildi.");
    }

    [MenuItem("Tools/Triyaj Hastane/Olustur/Triyaj Buton Canvas")]
    public static void CreateTriageButtonsCanvas()
    {
        GameObject root = CreateCanvasRoot("TriageButtonsCanvas", new Vector2(980f, 720f), true);
        Canvas canvas = root.GetComponent<Canvas>();
        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();

        RectTransform panelRect;
        Image panelImage;
        CreateGlassPanel(root.transform, out panelRect, out panelImage);

        TextMeshProUGUI complaint = CreateText(panelRect, "ComplaintText", "Şikayet: ...", 46f, GlassTextPrimary, TextAlignmentOptions.TopLeft);
        complaint.fontWeight = FontWeight.SemiBold;
        SetRect(complaint.rectTransform, new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.92f));

        RectTransform buttonContainer = EnsureTriageButtonContainer(panelRect);
        Button greenButton = CreateGridButton(buttonContainer, "Button_Yesil", "YEŞİL", GlassAccentGreen, 31f);
        Button yellowButton = CreateGridButton(buttonContainer, "Button_Sari", "SARI", GlassAccentYellow, 31f);
        Button redButton = CreateGridButton(buttonContainer, "Button_Kirmizi", "KIRMIZI", GlassAccentRed, 31f);
        Button blackButton = CreateGridButton(buttonContainer, "Button_Siyah", "SİYAH", GlassAccentSlate, 31f);

        root.AddComponent<VRUIClickHelper>();
        TriageDialogUI dialogUI = root.AddComponent<TriageDialogUI>();
        SerializedObject dialogSerialized = new SerializedObject(dialogUI);
        SetSerializedReference(dialogSerialized, "rootCanvas", canvas);
        SetSerializedReference(dialogSerialized, "canvasGroup", canvasGroup);
        SetSerializedReference(dialogSerialized, "complaintText", complaint);
        SetSerializedReference(dialogSerialized, "greenButton", greenButton);
        SetSerializedReference(dialogSerialized, "yellowButton", yellowButton);
        SetSerializedReference(dialogSerialized, "redButton", redButton);
        SetSerializedReference(dialogSerialized, "blackButton", blackButton);
        dialogSerialized.ApplyModifiedPropertiesWithoutUndo();

        ApplyVrCanvasStandards(root, true);
        SavePrefabAndDestroyTemp(root, "Assets/Prefabs/Triyaj/TriageButtonsCanvas.prefab", "Triyaj Buton Canvas Prefab Kaydet", "TriageButtonsCanvas");
    }

    [MenuItem("Tools/Triyaj Hastane/Olustur/Sonuc Panel Canvas")]
    public static void CreateResultCanvas()
    {
        GameObject root = CreateCanvasRoot("TriageResultCanvas", new Vector2(1000f, 700f), true);
        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();

        RectTransform panelRect;
        Image panelImage;
        CreateGlassPanel(root.transform, out panelRect, out panelImage);

        TextMeshProUGUI titleText = CreateText(panelRect, "TitleText", "TRİYAJ SONUÇLARI", 54f, GlassTextPrimary, TextAlignmentOptions.Center);
        titleText.fontWeight = FontWeight.Bold;
        SetRect(titleText.rectTransform, new Vector2(0.08f, 0.77f), new Vector2(0.92f, 0.94f));

        TextMeshProUGUI totalLabelText = CreateText(panelRect, "TotalLabelText", "TOPLAM", 24f, GlassTextSecondary, TextAlignmentOptions.Left);
        SetRect(totalLabelText.rectTransform, new Vector2(0.12f, 0.57f), new Vector2(0.42f, 0.66f));
        TextMeshProUGUI totalValueText = CreateText(panelRect, "TotalValueText", "0", 32f, GlassTextPrimary, TextAlignmentOptions.Right);
        totalValueText.fontWeight = FontWeight.Bold;
        SetRect(totalValueText.rectTransform, new Vector2(0.44f, 0.55f), new Vector2(0.56f, 0.67f));

        TextMeshProUGUI correctLabelText = CreateText(panelRect, "CorrectLabelText", "DOĞRU", 24f, GlassTextSecondary, TextAlignmentOptions.Left);
        SetRect(correctLabelText.rectTransform, new Vector2(0.12f, 0.46f), new Vector2(0.42f, 0.55f));
        TextMeshProUGUI correctValueText = CreateText(panelRect, "CorrectValueText", "0", 32f, GlassTextPrimary, TextAlignmentOptions.Right);
        correctValueText.fontWeight = FontWeight.Bold;
        SetRect(correctValueText.rectTransform, new Vector2(0.44f, 0.44f), new Vector2(0.56f, 0.56f));

        TextMeshProUGUI incorrectLabelText = CreateText(panelRect, "IncorrectLabelText", "YANLIŞ", 24f, GlassTextSecondary, TextAlignmentOptions.Left);
        SetRect(incorrectLabelText.rectTransform, new Vector2(0.12f, 0.35f), new Vector2(0.42f, 0.44f));
        TextMeshProUGUI incorrectValueText = CreateText(panelRect, "IncorrectValueText", "0", 32f, GlassTextPrimary, TextAlignmentOptions.Right);
        incorrectValueText.fontWeight = FontWeight.Bold;
        SetRect(incorrectValueText.rectTransform, new Vector2(0.44f, 0.33f), new Vector2(0.56f, 0.45f));

        RectTransform badgeRect = CreateUIObject("ScoreBadge", panelRect).GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.62f, 0.34f);
        badgeRect.anchorMax = new Vector2(0.88f, 0.58f);
        badgeRect.offsetMin = Vector2.zero;
        badgeRect.offsetMax = Vector2.zero;
        Image scoreBadgeImage = badgeRect.gameObject.AddComponent<Image>();
        scoreBadgeImage.color = GlassAccentBlue;
        TextMeshProUGUI scoreBadgeText = CreateText(badgeRect, "ScoreBadgeText", "%0\n<size=60%>Başarı</size>", 42f, GlassTextPrimary, TextAlignmentOptions.Center);
        scoreBadgeText.fontWeight = FontWeight.Bold;
        SetRect(scoreBadgeText.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));

        Button retryButton = CreateButton(panelRect, "Button_Tekrar", "TEKRAR DENE", GlassAccentBlue, new Vector2(-190f, -250f), new Vector2(300f, 108f), 29f);
        Button menuButton = CreateButton(panelRect, "Button_Menu", "ÜSSE DÖN", GlassAccentGreen, new Vector2(190f, -250f), new Vector2(300f, 108f), 29f);

        TriageResultPanel resultPanel = root.AddComponent<TriageResultPanel>();
        SerializedObject resultSerialized = new SerializedObject(resultPanel);
        SetSerializedReference(resultSerialized, "canvasGroup", canvasGroup);
        SetSerializedReference(resultSerialized, "titleText", titleText);
        SetSerializedReference(resultSerialized, "retryButton", retryButton);
        SetSerializedReference(resultSerialized, "menuButton", menuButton);
        SetSerializedReference(resultSerialized, "totalLabelText", totalLabelText);
        SetSerializedReference(resultSerialized, "totalValueText", totalValueText);
        SetSerializedReference(resultSerialized, "correctLabelText", correctLabelText);
        SetSerializedReference(resultSerialized, "correctValueText", correctValueText);
        SetSerializedReference(resultSerialized, "incorrectLabelText", incorrectLabelText);
        SetSerializedReference(resultSerialized, "incorrectValueText", incorrectValueText);
        SetSerializedReference(resultSerialized, "scoreBadgeText", scoreBadgeText);
        SetSerializedReference(resultSerialized, "scoreBadgeImage", scoreBadgeImage);
        resultSerialized.ApplyModifiedPropertiesWithoutUndo();

        ApplyVrCanvasStandards(root, true);
        SavePrefabAndDestroyTemp(root, "Assets/Prefabs/Triyaj/TriageResultCanvas.prefab", "Triyaj Sonuç Canvas Prefab Kaydet", "TriageResultCanvas");
    }

    [MenuItem("Tools/Triyaj Hastane/Olustur/Ilerleme HUD Canvas")]
    public static void CreateProgressHudCanvas()
    {
        GameObject root = CreateCanvasRoot("TriageHUDCanvas", new Vector2(760f, 160f), false);
        Canvas canvas = root.GetComponent<Canvas>();
        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();

        RectTransform panelRect = CreateUIObject("Panel", root.transform).GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = new Vector2(24f, 20f);
        panelRect.offsetMax = new Vector2(-24f, -20f);
        Image panelImage = panelRect.gameObject.AddComponent<Image>();
        panelImage.color = GlassPanelColor;

        RectTransform shadowRect = CreateUIObject("PanelShadow", root.transform).GetComponent<RectTransform>();
        shadowRect.SetSiblingIndex(0);
        shadowRect.anchorMin = Vector2.zero;
        shadowRect.anchorMax = Vector2.one;
        shadowRect.offsetMin = new Vector2(40f, 32f);
        shadowRect.offsetMax = new Vector2(-8f, -8f);
        Image shadowImage = shadowRect.gameObject.AddComponent<Image>();
        shadowImage.color = GlassShadowColor;

        TextMeshProUGUI progressText = CreateText(panelRect, "ProgressText", "Müdahale Edilen Hasta: 0/0", 46f, GlassTextPrimary, TextAlignmentOptions.Center);
        progressText.fontWeight = FontWeight.Bold;
        SetRect(progressText.rectTransform, new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.85f));

        TriageHUD hud = root.AddComponent<TriageHUD>();
        SerializedObject hudSerialized = new SerializedObject(hud);
        SetSerializedReference(hudSerialized, "rootCanvas", canvas);
        SetSerializedReference(hudSerialized, "canvasGroup", canvasGroup);
        SetSerializedReference(hudSerialized, "panelRect", panelRect);
        SetSerializedReference(hudSerialized, "panelImage", panelImage);
        SetSerializedReference(hudSerialized, "progressText", progressText);
        hudSerialized.ApplyModifiedPropertiesWithoutUndo();

        ApplyVrCanvasStandards(root, false);
        SavePrefabAndDestroyTemp(root, "Assets/Prefabs/Triyaj/TriageHUDCanvas.prefab", "Triyaj HUD Canvas Prefab Kaydet", "TriageHUDCanvas");
    }

    [MenuItem("Tools/Triyaj Hastane/Secilen NPC'ye Triyaj Bileseni Ekle")]
    public static void SetupSelectedNpc()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            return;
        }

        EnsureNpcComponents(selected, out NPCTriageInteractable triageInteractable);

        TriageDialogUI dialogUI = UnityEngine.Object.FindObjectOfType<TriageDialogUI>(true);
        if (dialogUI != null)
        {
            SerializedObject serializedTriage = new SerializedObject(triageInteractable);
            SetSerializedReference(serializedTriage, "triageButtonsCanvas", dialogUI);
            serializedTriage.ApplyModifiedPropertiesWithoutUndo();
        }

        EnsureSceneEventSystemUsesXRUiInput();
        EnsureHospitalScenesInBuildSettings();
        EditorUtility.SetDirty(selected);
        if (PrefabUtility.IsPartOfAnyPrefab(selected))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(selected);
        }
    }

    [MenuItem("Tools/Triyaj Hastane/HospitalTriageManager Olustur")]
    public static void CreateHospitalManager()
    {
        HospitalTriageManager manager = EnsureHospitalManagerExists();
        WireHospitalManager(manager, EnsureHospitalRetryAnchor(), EnsureBaseReturnAnchor());
        EnsureSceneEventSystemUsesXRUiInput();
        EnsureHospitalScenesInBuildSettings();
        Selection.activeGameObject = manager.gameObject;
        MarkActiveSceneDirty();
    }

    [MenuItem("Tools/Triyaj Hastane/Build Settings/Hastane Sahnelerini Ekle")]
    public static void EnsureHospitalScenesInBuildSettingsMenu()
    {
        EnsureHospitalScenesInBuildSettings();
    }

    private static void UpgradeSceneUiIfPresent()
    {
        GameObject buttonsCanvas = GameObject.Find("TriageButtonsCanvas");
        if (buttonsCanvas != null)
        {
            ApplyVrCanvasStandards(buttonsCanvas, true);
            NormalizeTriageButtonGrid(buttonsCanvas.transform);
            EnsureButtonHoverEffects(buttonsCanvas.transform);
            EditorUtility.SetDirty(buttonsCanvas);
        }

        GameObject resultCanvas = GameObject.Find("TriageResultCanvas");
        if (resultCanvas != null)
        {
            ApplyVrCanvasStandards(resultCanvas, true);
            EnsureButtonHoverEffects(resultCanvas.transform);
            EditorUtility.SetDirty(resultCanvas);
        }

        GameObject hudCanvas = GameObject.Find("TriageHUDCanvas");
        if (hudCanvas != null)
        {
            ApplyVrCanvasStandards(hudCanvas, false);
            EditorUtility.SetDirty(hudCanvas);
        }

        EnsureSceneEventSystemUsesXRUiInput();
    }

    private static GameObject CreateCanvasRoot(string name, Vector2 size, bool interactive)
    {
        GameObject root = new GameObject(name);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = XRCameraHelper.GetPlayerCamera();

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        ConfigureWorldCanvas(canvasRect, size);

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 16f;
        scaler.referencePixelsPerUnit = 100f;

        GraphicRaycaster raycaster = root.AddComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        if (interactive)
        {
            root.AddComponent<TrackedDeviceGraphicRaycaster>();
        }

        root.AddComponent<CanvasGroup>();
        return root;
    }

    private static void CreateGlassPanel(Transform parent, out RectTransform panelRect, out Image panelImage)
    {
        RectTransform shadowRect = CreateUIObject("PanelShadow", parent).GetComponent<RectTransform>();
        shadowRect.anchorMin = Vector2.zero;
        shadowRect.anchorMax = Vector2.one;
        shadowRect.offsetMin = new Vector2(32f, -30f);
        shadowRect.offsetMax = new Vector2(10f, -12f);
        Image shadowImage = shadowRect.gameObject.AddComponent<Image>();
        shadowImage.color = GlassShadowColor;

        panelRect = CreateUIObject("Panel", parent).GetComponent<RectTransform>();
        SetStretch(panelRect, new Vector2(18f, 16f), new Vector2(-18f, -16f));
        panelImage = panelRect.gameObject.AddComponent<Image>();
        panelImage.color = GlassPanelColor;

        RectTransform accentRect = CreateUIObject("TopAccent", panelRect).GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = new Vector2(0f, -4f);
        accentRect.sizeDelta = new Vector2(0f, 6f);
        Image accentImage = accentRect.gameObject.AddComponent<Image>();
        accentImage.color = GlassAccentGlow;
        accentImage.raycastTarget = false;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        RectTransform rect = obj.AddComponent<RectTransform>();
        obj.transform.SetParent(parent, false);
        rect.localScale = Vector3.one;
        obj.AddComponent<CanvasRenderer>();
        return obj;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string content, float size, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUIObject(name, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = true;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, Color color, Vector2 anchoredPosition, Vector2 size, float fontSize)
    {
        GameObject buttonObject = CreateUIObject(name, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = color;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;

        TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, fontSize, GlassTextPrimary, TextAlignmentOptions.Center);
        text.fontWeight = FontWeight.Bold;
        SetRect(text.rectTransform, Vector2.zero, Vector2.one);

        ApplyButtonStyle(button, color);
        return button;
    }

    private static Button CreateGridButton(Transform parent, string name, string label, Color color, float fontSize)
    {
        GameObject buttonObject = CreateUIObject(name, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = color;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = TriageButtonCellSize;

        TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, fontSize, GlassTextPrimary, TextAlignmentOptions.Center);
        text.fontWeight = FontWeight.Bold;
        SetRect(text.rectTransform, Vector2.zero, Vector2.one);

        ApplyButtonStyle(button, color);
        return button;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetStretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void ApplyVrCanvasStandards(GameObject root, bool addClickHelper)
    {
        if (root == null)
        {
            return;
        }

        GraphicRaycaster raycaster = root.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = root.AddComponent<GraphicRaycaster>();
        }

        raycaster.enabled = false;

        if (root.GetComponent<Canvas>() != null && root.GetComponent<TriageHUD>() == null && root.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
        {
            root.AddComponent<TrackedDeviceGraphicRaycaster>();
        }

        if (root.GetComponent<TriageHUD>() != null)
        {
            TrackedDeviceGraphicRaycaster tracked = root.GetComponent<TrackedDeviceGraphicRaycaster>();
            if (tracked != null)
            {
                UnityEngine.Object.DestroyImmediate(tracked);
            }
        }

        if (addClickHelper && root.GetComponent<VRUIClickHelper>() == null)
        {
            root.AddComponent<VRUIClickHelper>();
        }

        SetLayerRecursively(root.transform, GetUiLayer());
        EnsureButtonHoverEffects(root.transform);
        EditorUtility.SetDirty(root);
    }

    private static void ApplyButtonStyle(Button button, Color accentColor)
    {
        if (button == null)
        {
            return;
        }

        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.97f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.84f, 0.91f, 0.96f, 1f);
        colors.selectedColor = new Color(0.97f, 1f, 1f, 1f);
        colors.disabledColor = new Color(0.55f, 0.62f, 0.72f, 0.45f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        TriageButtonHoverFeedback hoverFeedback = button.GetComponent<TriageButtonHoverFeedback>();
        if (hoverFeedback == null)
        {
            hoverFeedback = button.gameObject.AddComponent<TriageButtonHoverFeedback>();
        }

        hoverFeedback.Configure(accentColor);
    }

    private static void ConfigureWorldCanvas(RectTransform canvasRect, Vector2 size)
    {
        if (canvasRect == null)
        {
            return;
        }

        canvasRect.sizeDelta = size;
        canvasRect.localScale = new Vector3(0.002f, 0.002f, 0.002f);
    }

    private static void EnsureSceneEventSystemUsesXRUiInput()
    {
        EventSystem eventSystem = UnityEngine.Object.FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            return;
        }

        StandaloneInputModule standalone = eventSystem.GetComponent<StandaloneInputModule>();
        if (standalone != null)
        {
            standalone.enabled = false;
        }

        if (eventSystem.GetComponent<XRUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<XRUIInputModule>();
        }

        EditorUtility.SetDirty(eventSystem.gameObject);
    }

    private static RectTransform EnsureTriageButtonContainer(RectTransform panelRect)
    {
        if (panelRect == null)
        {
            return null;
        }

        Transform existing = panelRect.Find("ButtonContainer");
        RectTransform containerRect = existing as RectTransform;
        if (containerRect == null)
        {
            containerRect = CreateUIObject("ButtonContainer", panelRect).GetComponent<RectTransform>();
        }

        containerRect.anchorMin = new Vector2(0.5f, 0f);
        containerRect.anchorMax = new Vector2(0.5f, 0f);
        containerRect.pivot = new Vector2(0.5f, 0f);
        containerRect.anchoredPosition = TriageButtonContainerOffset;
        containerRect.sizeDelta = TriageButtonContainerSize;

        GridLayoutGroup gridLayout = containerRect.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = containerRect.gameObject.AddComponent<GridLayoutGroup>();
        }

        gridLayout.cellSize = TriageButtonCellSize;
        gridLayout.spacing = TriageButtonSpacing;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 2;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;
        gridLayout.padding = new RectOffset(0, 0, 0, 0);

        return containerRect;
    }

    private static void NormalizeTriageButtonGrid(Transform canvasRoot)
    {
        if (canvasRoot == null)
        {
            return;
        }

        RectTransform panelRect = canvasRoot.Find("Panel") as RectTransform;
        if (panelRect == null)
        {
            return;
        }

        RectTransform buttonContainer = EnsureTriageButtonContainer(panelRect);
        if (buttonContainer == null)
        {
            return;
        }

        string[] buttonNames =
        {
            "Button_Yesil",
            "Button_Sari",
            "Button_Kirmizi",
            "Button_Siyah"
        };

        for (int i = 0; i < buttonNames.Length; i++)
        {
            Transform buttonTransform = FindChildRecursive(canvasRoot, buttonNames[i]);
            if (buttonTransform == null)
            {
                continue;
            }

            buttonTransform.SetParent(buttonContainer, false);
            buttonTransform.SetSiblingIndex(i);

            RectTransform buttonRect = buttonTransform as RectTransform;
            if (buttonRect != null)
            {
                buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
                buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
                buttonRect.pivot = new Vector2(0.5f, 0.5f);
                buttonRect.anchoredPosition = Vector2.zero;
                buttonRect.sizeDelta = TriageButtonCellSize;
                buttonRect.localScale = Vector3.one;
            }

            TextMeshProUGUI label = buttonTransform.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.alignment = TextAlignmentOptions.Center;
                RectTransform labelRect = label.rectTransform;
                SetRect(labelRect, Vector2.zero, Vector2.one);
            }
        }
    }

    private static void AssignUniqueCasesToSceneNpcs()
    {
        NPCTriageInteractable[] npcs = UnityEngine.Object.FindObjectsOfType<NPCTriageInteractable>(true);
        if (npcs == null || npcs.Length == 0)
        {
            return;
        }

        Array.Sort(npcs, (left, right) => string.CompareOrdinal(GetHierarchyPath(left.transform), GetHierarchyPath(right.transform)));

        HospitalCaseDefinition[] shuffledCases = ShuffleCases();
        int assignCount = Mathf.Min(npcs.Length, shuffledCases.Length);
        for (int i = 0; i < assignCount; i++)
        {
            npcs[i].ConfigureScenario(shuffledCases[i].complaintText, shuffledCases[i].actualCategory);
            EditorUtility.SetDirty(npcs[i]);
            if (PrefabUtility.IsPartOfAnyPrefab(npcs[i]))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(npcs[i]);
            }
        }

        if (npcs.Length > shuffledCases.Length)
        {
            Debug.LogError($"[TriyajHospitalSetupTool] Sahnedeki NPC sayısı ({npcs.Length}) vaka havuzunu ({shuffledCases.Length}) aşıyor. Fazladan NPC'ler değiştirilmedi.");
        }
    }

    private static void WireDialogCanvasToSceneNpcs()
    {
        TriageDialogUI dialogUI = UnityEngine.Object.FindObjectOfType<TriageDialogUI>(true);
        if (dialogUI == null)
        {
            return;
        }

        NPCTriageInteractable[] npcs = UnityEngine.Object.FindObjectsOfType<NPCTriageInteractable>(true);
        for (int i = 0; i < npcs.Length; i++)
        {
            NPCTriageInteractable npc = npcs[i];
            if (npc == null)
            {
                continue;
            }

            EnsureNpcComponents(npc.gameObject, out NPCTriageInteractable interactable);
            SerializedObject serializedTriage = new SerializedObject(interactable);
            SetSerializedReference(serializedTriage, "triageButtonsCanvas", dialogUI);
            serializedTriage.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(interactable);
            if (PrefabUtility.IsPartOfAnyPrefab(interactable))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(interactable);
            }
        }
    }

    private static void EnsureNpcComponents(GameObject root, out NPCTriageInteractable triageInteractable)
    {
        Collider collider = root.GetComponent<Collider>();
        if (collider == null)
        {
            collider = root.AddComponent<CapsuleCollider>();
        }

        XRSimpleInteractable xrInteractable = root.GetComponent<XRSimpleInteractable>();
        if (xrInteractable == null)
        {
            xrInteractable = root.AddComponent<XRSimpleInteractable>();
        }

        triageInteractable = root.GetComponent<NPCTriageInteractable>();
        if (triageInteractable == null)
        {
            triageInteractable = root.AddComponent<NPCTriageInteractable>();
        }
    }

    private static HospitalTriageManager EnsureHospitalManagerExists()
    {
        HospitalTriageManager manager = UnityEngine.Object.FindObjectOfType<HospitalTriageManager>(true);
        if (manager != null)
        {
            return manager;
        }

        GameObject managerObject = new GameObject("HospitalTriageManager");
        manager = managerObject.AddComponent<HospitalTriageManager>();
        EditorUtility.SetDirty(managerObject);
        return manager;
    }

    private static void WireHospitalManager(HospitalTriageManager manager, Transform retryAnchor, Transform baseAnchor)
    {
        if (manager == null)
        {
            return;
        }

        SerializedObject serializedManager = new SerializedObject(manager);
        SetSerializedReference(serializedManager, "resultPanel", UnityEngine.Object.FindObjectOfType<TriageResultPanel>(true));
        SetSerializedReference(serializedManager, "progressHud", UnityEngine.Object.FindObjectOfType<TriageHUD>(true));
        SetSerializedReference(serializedManager, "hospitalStartAnchor", retryAnchor);
        SetSerializedReference(serializedManager, "baseReturnAnchor", baseAnchor);
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager.gameObject);
    }

    private static Transform EnsureHospitalRetryAnchor()
    {
        GameObject existing = GameObject.Find("HospitalRetryAnchor");
        if (existing != null)
        {
            return existing.transform;
        }

        if (!TryResolveLegacyHospitalStartPose(out Vector3 position, out float yaw))
        {
            return null;
        }

        GameObject anchor = new GameObject("HospitalRetryAnchor");
        anchor.transform.position = position;
        anchor.transform.rotation = Quaternion.Euler(0f, yaw - 180f, 0f);
        EditorUtility.SetDirty(anchor);
        return anchor.transform;
    }

    private static Transform EnsureBaseReturnAnchor()
    {
        GameObject existing = GameObject.Find("BaseReturnAnchor");
        if (existing != null)
        {
            return existing.transform;
        }

        GameObject sceneSpawn = GameObject.Find("VR_SpawnPoint");
        if (sceneSpawn == null)
        {
            return null;
        }

        GameObject anchor = new GameObject("BaseReturnAnchor");
        anchor.transform.position = sceneSpawn.transform.position;
        anchor.transform.rotation = sceneSpawn.transform.rotation;
        EditorUtility.SetDirty(anchor);
        return anchor.transform;
    }

    private static void EnsureButtonHoverEffects(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            Image image = button.GetComponent<Image>();
            Color accentColor = image != null ? image.color : GlassAccentBlue;
            ApplyButtonStyle(button, accentColor);
        }
    }

    private static GameObject FindHospitalRoot()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
        {
            return null;
        }

        GameObject[] roots = activeScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
            {
                continue;
            }

            if (string.Equals(root.name, "Triyaj_hastane", StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }

            Transform nestedRoot = FindChildRecursive(root.transform, "Triyaj_hastane");
            if (nestedRoot != null)
            {
                return nestedRoot.gameObject;
            }
        }

        return null;
    }

    private static bool IsBrokenHospitalMaterial(Material material)
    {
        if (material == null)
        {
            return true;
        }

        string materialPath = AssetDatabase.GetAssetPath(material);
        if (string.IsNullOrWhiteSpace(materialPath))
        {
            return false;
        }

        if (string.Equals(materialPath, BrokenSchoolMaterialPath, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(materialPath, RecoveredWallMaterialPath, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(materialPath, RecoveredFloorMaterialPath, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(materialPath, RecoveredDoorMaterialPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        Texture baseTexture = null;
        if (material.HasProperty("_BaseMap"))
        {
            baseTexture = material.GetTexture("_BaseMap");
        }
        else if (material.HasProperty("_MainTex"))
        {
            baseTexture = material.GetTexture("_MainTex");
        }

        string texturePath = baseTexture != null ? AssetDatabase.GetAssetPath(baseTexture) : string.Empty;
        return string.Equals(texturePath, BrokenSchoolTexturePath, StringComparison.OrdinalIgnoreCase);
    }

    private static Material ResolveHospitalReplacementMaterial(
        Transform target,
        Material wallMaterial,
        Material floorMaterial,
        Material doorMaterial,
        Material metalMaterial,
        Material beddingMaterial,
        Material lampMaterial)
    {
        if (target == null)
        {
            return null;
        }

        string name = target.name.ToLowerInvariant();
        string path = GetHierarchyPath(target).ToLowerInvariant();

        if (ContainsAny(name, path, "floor", "ground", "tile", "zemin"))
        {
            return floorMaterial;
        }

        if (ContainsAny(name, path, "door", "kapi", "gate"))
        {
            return doorMaterial;
        }

        if (ContainsAny(name, path, "sheet", "blanket", "bedding", "mattress", "yatakortu"))
        {
            return beddingMaterial;
        }

        if (ContainsAny(name, path, "lamp", "light", "isik"))
        {
            return lampMaterial;
        }

        if (ContainsAny(name, path, "rack", "shelf", "cabinet", "locker", "bed", "stretcher", "trolley", "table", "chair", "sink", "fridge", "refrigerator", "metal"))
        {
            return metalMaterial;
        }

        if (ContainsAny(name, path, "wall", "room", "corridor", "hospital", "partition", "panel", "window", "jalousie", "showcase"))
        {
            return wallMaterial;
        }

        return null;
    }

    private static bool ShouldForceHospitalReplacement(Material currentMaterial, Material replacementMaterial)
    {
        if (replacementMaterial == null)
        {
            return false;
        }

        string currentMaterialPath = currentMaterial != null ? AssetDatabase.GetAssetPath(currentMaterial) : string.Empty;
        string replacementMaterialPath = AssetDatabase.GetAssetPath(replacementMaterial);

        if (string.Equals(currentMaterialPath, replacementMaterialPath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(currentMaterialPath))
        {
            return true;
        }

        if (currentMaterialPath.StartsWith(HospitalMaterialFolderPath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (currentMaterialPath.StartsWith(SchoolMaterialFolderPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return true;
    }

    private static bool ContainsAny(string primaryValue, string secondaryValue, params string[] keywords)
    {
        if (keywords == null || keywords.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            if ((!string.IsNullOrWhiteSpace(primaryValue) && primaryValue.Contains(keyword)) ||
                (!string.IsNullOrWhiteSpace(secondaryValue) && secondaryValue.Contains(keyword)))
            {
                return true;
            }
        }

        return false;
    }

    private static HospitalCaseDefinition[] ShuffleCases()
    {
        HospitalCaseDefinition[] cases = (HospitalCaseDefinition[])CaseLibrary.Clone();
        System.Random random = new System.Random(Environment.TickCount);
        for (int i = cases.Length - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            HospitalCaseDefinition temp = cases[i];
            cases[i] = cases[swapIndex];
            cases[swapIndex] = temp;
        }

        return cases;
    }

    private static bool TryResolveLegacyHospitalStartPose(out Vector3 targetPos, out float targetYaw)
    {
        targetPos = Vector3.zero;
        targetYaw = 0f;

        TriyajModul3.OnayMenusuManager[] teleportMenus = UnityEngine.Object.FindObjectsOfType<TriyajModul3.OnayMenusuManager>(true);
        for (int i = 0; i < teleportMenus.Length; i++)
        {
            TriyajModul3.OnayMenusuManager teleportMenu = teleportMenus[i];
            if (teleportMenu == null || teleportMenu.hedefNokta == null)
            {
                continue;
            }

            targetPos = teleportMenu.hedefNokta.position + teleportMenu.spawnOffset;
            targetYaw = teleportMenu.hedefNokta.eulerAngles.y + teleportMenu.spawnRotationOffset;
            return true;
        }

        return false;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
        {
            return string.Empty;
        }

        List<string> segments = new List<string>();
        Transform current = transform;
        while (current != null)
        {
            segments.Add(current.name);
            current = current.parent;
        }

        segments.Reverse();
        return string.Join("/", segments);
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static int GetUiLayer()
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        return uiLayer >= 0 ? uiLayer : 5;
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null)
        {
            return;
        }

        root.gameObject.layer = layer;
        foreach (Transform child in root)
        {
            SetLayerRecursively(child, layer);
        }
    }

    private static void EnsureHospitalScenesInBuildSettings()
    {
        HashSet<string> requiredSceneNames = new HashSet<string>(RequiredHospitalSceneNames);
        Dictionary<string, string> scenePathsByName = new Dictionary<string, string>();
        string[] sceneGuids = AssetDatabase.FindAssets("t:SceneAsset", new[] { "Assets" });

        for (int i = 0; i < sceneGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
            string sceneName = Path.GetFileNameWithoutExtension(path);
            if (requiredSceneNames.Contains(sceneName) && !scenePathsByName.ContainsKey(sceneName))
            {
                scenePathsByName.Add(sceneName, path);
            }
        }

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        HashSet<string> existingPaths = new HashSet<string>();
        for (int i = 0; i < buildScenes.Count; i++)
        {
            existingPaths.Add(buildScenes[i].path);
        }

        bool changed = false;
        for (int i = 0; i < RequiredHospitalSceneNames.Length; i++)
        {
            string sceneName = RequiredHospitalSceneNames[i];
            if (!scenePathsByName.TryGetValue(sceneName, out string scenePath))
            {
                Debug.LogWarning($"[TriyajHospitalSetupTool] '{sceneName}' sahnesi Assets altında bulunamadı.");
                continue;
            }

            if (existingPaths.Contains(scenePath))
            {
                continue;
            }

            buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            existingPaths.Add(scenePath);
            changed = true;
        }

        if (changed)
        {
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }
    }

    private static void SavePrefabAndDestroyTemp(GameObject root, string defaultPath, string title, string defaultName)
    {
        string savePath = ResolveSavePath(defaultPath, title, defaultName);
        if (string.IsNullOrWhiteSpace(savePath))
        {
            Selection.activeGameObject = root;
            return;
        }

        PrefabUtility.SaveAsPrefabAsset(root, savePath);
        UnityEngine.Object prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(savePath);
        UnityEngine.Object.DestroyImmediate(root);
        Selection.activeObject = prefabAsset;
    }

    private static void SetSerializedReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        if (serializedObject == null || string.IsNullOrWhiteSpace(propertyName))
        {
            return;
        }

        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static string ResolveSavePath(string defaultPath, string title, string defaultName)
    {
        if (useDefaultSavePaths)
        {
            return defaultPath;
        }

        return EditorUtility.SaveFilePanelInProject(title, defaultName, "prefab", "Kaydedilecek prefab yolunu seç.");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string[] segments = path.Split('/');
        string currentPath = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            string nextPath = currentPath + "/" + segments[i];
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, segments[i]);
            }

            currentPath = nextPath;
        }
    }

    private static void MarkActiveSceneDirty()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
        }
    }
}
#endif
