using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class VRUISetupTool : EditorWindow
{
    private Vector2 scrollPos;

    [MenuItem("Tools/VR UI Setup")]
    public static void ShowWindow()
    {
        GetWindow<VRUISetupTool>("VR UI Setup");
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("VR Sahne Kurulum Aracı", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // --- Sahne Özeti ---
        EditorGUILayout.LabelField("Sahne Durumu", EditorStyles.boldLabel);
        DrawSceneSummary();
        EditorGUILayout.Space(10);

        // --- UI Yapılandırması ---
        EditorGUILayout.LabelField("1. UI Yapılandırması", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Canvas'ları World Space'e çevirir, EventSystem'i XR UI Input Module ile günceller, " +
            "Tracked Device Graphic Raycaster ekler.", MessageType.Info);
        if (GUILayout.Button("UI'ı VR İçin Yapılandır", GUILayout.Height(30)))
        {
            SetupVRUI();
        }
        EditorGUILayout.Space(10);

        // --- XR Device Simulator ---
        EditorGUILayout.LabelField("2. XR Device Simulator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Fiziksel VR gözlüğü olmadan test etmek için XR Device Simulator'ı sahneye ekler.\n" +
            "Kontroller: Fare sağ tık = bakış, WASD = hareket, Shift = Sol El, Space = Sağ El",
            MessageType.Info);
        if (GUILayout.Button("XR Device Simulator Ekle", GUILayout.Height(30)))
        {
            AddXRDeviceSimulator();
        }
        EditorGUILayout.Space(10);

        // --- Build Settings ---
        EditorGUILayout.LabelField("3. Build Settings", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Tüm sahneleri Build Settings'e ekler. Sahne geçişi için zorunludur.",
            MessageType.Info);
        if (GUILayout.Button("Tüm Sahneleri Build Settings'e Ekle", GUILayout.Height(30)))
        {
            AddAllScenesToBuildSettings();
        }
        EditorGUILayout.Space(10);

        // --- Hepsini Bir Seferde ---
        EditorGUILayout.LabelField("Hızlı Kurulum", EditorStyles.boldLabel);
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
        if (GUILayout.Button("TÜMÜNÜ UYGULA", GUILayout.Height(40)))
        {
            SetupVRUI();
            AddXRDeviceSimulator();
            AddAllScenesToBuildSettings();
            Debug.Log("[VR Setup] Tüm kurulumlar tamamlandı!");
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
    }

    // ============================================================
    // SAHNE ÖZETİ
    // ============================================================
    private void DrawSceneSummary()
    {
        EditorGUILayout.BeginVertical("box");

        Canvas[] canvases = FindObjectsOfType<Canvas>();
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        bool hasSimulator = FindObjectOfType(
            System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator, Unity.XR.Interaction.Toolkit") ??
            typeof(MonoBehaviour)
        ) != null && FindObjectOfType(
            System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator, Unity.XR.Interaction.Toolkit") ??
            typeof(MonoBehaviour)
        )?.GetType().Name == "XRDeviceSimulator";

        EditorGUILayout.LabelField($"Canvas sayısı: {canvases.Length}");
        foreach (var c in canvases)
        {
            string mode = c.renderMode == RenderMode.WorldSpace ? "✅ World Space" : "⚠️ " + c.renderMode;
            EditorGUILayout.LabelField($"  • {c.name}: {mode}");
        }

        if (eventSystem != null)
        {
            var standaloneInput = eventSystem.GetComponent<StandaloneInputModule>();
            var xrInput = eventSystem.GetComponent("XRUIInputModule");
            string inputStatus = xrInput != null ? "✅ XR UI Input Module" :
                                 standaloneInput != null ? "⚠️ Standalone Input Module" : "❌ Yok";
            EditorGUILayout.LabelField($"EventSystem: {inputStatus}");
        }
        else
        {
            EditorGUILayout.LabelField("EventSystem: ❌ Bulunamadı");
        }

        EditorGUILayout.LabelField($"XR Device Simulator: {(hasSimulator ? "✅ Mevcut" : "❌ Yok")}");

        int buildSceneCount = EditorBuildSettings.scenes.Length;
        EditorGUILayout.LabelField($"Build Settings sahne sayısı: {buildSceneCount}");

        EditorGUILayout.EndVertical();
    }

    // ============================================================
    // 1. VR UI YAPILANDIRMASI
    // ============================================================
    private static void SetupVRUI()
    {
        // Canvas'ları yapılandır
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (var canvas in canvases)
        {
            canvas.renderMode = RenderMode.WorldSpace;

            // Tracked Device Graphic Raycaster ekle
            var xrRaycasterType = System.Type.GetType(
                "UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (xrRaycasterType != null)
            {
                var existing = canvas.GetComponent(xrRaycasterType);
                if (existing == null)
                {
                    canvas.gameObject.AddComponent(xrRaycasterType);
                    // Standart raycaster'ı kaldır
                    var standardRaycaster = canvas.GetComponent<GraphicRaycaster>();
                    if (standardRaycaster != null && standardRaycaster.GetType() != xrRaycasterType)
                    {
                        DestroyImmediate(standardRaycaster);
                    }
                }
            }

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.dynamicPixelsPerUnit = 10;
            }

            EditorUtility.SetDirty(canvas);
        }

        // EventSystem yapılandır
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            eventSystem = esObj.AddComponent<EventSystem>();
        }

        // StandaloneInputModule kaldır
        var standalone = eventSystem.GetComponent<StandaloneInputModule>();
        if (standalone != null)
        {
            DestroyImmediate(standalone);
        }

        // XRUIInputModule ekle
        var xrInputModuleType = System.Type.GetType(
            "UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit");
        if (xrInputModuleType != null)
        {
            var xrInput = eventSystem.GetComponent(xrInputModuleType);
            if (xrInput == null)
            {
                eventSystem.gameObject.AddComponent(xrInputModuleType);
            }
        }

        EditorUtility.SetDirty(eventSystem);

        // Legacy Text uyarısı
        Text[] legacyTexts = FindObjectsOfType<Text>();
        if (legacyTexts.Length > 0)
        {
            Debug.LogWarning($"[VR Setup] Sahnede {legacyTexts.Length} adet eski Text bileşeni bulundu. VR netliği için TextMeshPro kullanılması önerilir.");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[VR Setup] UI yapılandırması tamamlandı.");
    }

    // ============================================================
    // 2. XR DEVICE SIMULATOR
    // ============================================================
    private static void AddXRDeviceSimulator()
    {
        // Sahnede zaten var mı kontrol et
        var simType = System.Type.GetType(
            "UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator, Unity.XR.Interaction.Toolkit");
        if (simType != null)
        {
            var existing = FindObjectOfType(simType);
            if (existing != null)
            {
                Debug.Log("[VR Setup] XR Device Simulator zaten sahnede mevcut.");
                return;
            }
        }

        // Prefab'ı bul
        string[] guids = AssetDatabase.FindAssets("XR Device Simulator t:Prefab");
        if (guids.Length == 0)
        {
            Debug.LogError("[VR Setup] XR Device Simulator prefab'ı bulunamadı! " +
                           "Package Manager'dan XR Interaction Toolkit > Samples > XR Device Simulator'ı import edin.");
            return;
        }

        string prefabPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[VR Setup] Prefab yüklenemedi: {prefabPath}");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "XR Device Simulator";

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[VR Setup] XR Device Simulator sahneye eklendi. " +
                  "Kontroller: Fare sağ tık = bakış, WASD = hareket, Shift = Sol El, Space = Sağ El");
    }

    // ============================================================
    // 3. BUILD SETTINGS
    // ============================================================
    private static void AddAllScenesToBuildSettings()
    {
        // Assets/Scenes klasöründeki tüm sahneleri bul
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });

        if (sceneGuids.Length == 0)
        {
            // Tüm projede ara
            sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
        }

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        HashSet<string> existingPaths = new HashSet<string>(buildScenes.Select(s => s.path));
        int addedCount = 0;

        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Samples ve Test sahnelerini atla
            if (path.Contains("/Samples/") || path.Contains("/Test/"))
                continue;

            if (!existingPaths.Contains(path))
            {
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
                addedCount++;
                Debug.Log($"[VR Setup] Build Settings'e eklendi: {path}");
            }
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();

        if (addedCount > 0)
        {
            Debug.Log($"[VR Setup] {addedCount} sahne Build Settings'e eklendi. Toplam: {buildScenes.Count}");
        }
        else
        {
            Debug.Log("[VR Setup] Tüm sahneler zaten Build Settings'te mevcut.");
        }
    }
}
