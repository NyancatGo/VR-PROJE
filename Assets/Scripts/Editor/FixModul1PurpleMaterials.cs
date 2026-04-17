#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FixModul1PurpleMaterials
{
    private const string ScenePath = "Assets/Samples/XR Interaction Toolkit/2.6.5/XR Device Simulator/Scenes/Modul1.unity";
    private const string FallbackMaterialPath = "Assets/Materials/Modul1_Fallback_URP_Lit.mat";

    [MenuItem("Tools/Modul1/Fix Purple Materials")]
    public static void Run()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[FixModul1PurpleMaterials] URP/Lit shader bulunamadi. URP package ve Graphics ayarlarini kontrol et.");
            return;
        }

        Material fallbackMaterial = AssetDatabase.LoadAssetAtPath<Material>(FallbackMaterialPath);
        if (fallbackMaterial == null)
        {
            string folder = "Assets/Materials";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            fallbackMaterial = new Material(urpLit);
            fallbackMaterial.name = "Modul1_Fallback_URP_Lit";
            fallbackMaterial.SetColor("_BaseColor", new Color(0.75f, 0.75f, 0.75f, 1f));
            AssetDatabase.CreateAsset(fallbackMaterial, FallbackMaterialPath);
            AssetDatabase.SaveAssets();
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int fixedRendererCount = 0;
        int fixedSlotCount = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                Material[] mats = renderer.sharedMaterials;
                bool rendererChanged = false;

                for (int i = 0; i < mats.Length; i++)
                {
                    Material mat = mats[i];
                    bool isBroken = mat == null || mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader";
                    if (isBroken)
                    {
                        mats[i] = fallbackMaterial;
                        fixedSlotCount++;
                        rendererChanged = true;
                    }
                }

                if (rendererChanged)
                {
                    renderer.sharedMaterials = mats;
                    EditorUtility.SetDirty(renderer);
                    fixedRendererCount++;
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[FixModul1PurpleMaterials] Tamamlandi. Renderers={fixedRendererCount}, MaterialSlots={fixedSlotCount}");
    }
}
#endif
