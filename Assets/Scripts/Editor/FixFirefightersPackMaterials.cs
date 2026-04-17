#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FirefightersPackMaterialRepair
{
    public static class FixFirefightersPackMaterials
    {
        private const string RootPath =
            "Assets/Samples/XR Interaction Toolkit/2.6.5/XR Device Simulator/Scenes/Jucho/FirefightersPack";
        private const string BaseMaterialPath = RootPath + "/Materials/Material.mat";
        private const string LightsMaterialPath = RootPath + "/Materials/Lights.mat";
        private const string PaletteTexturePath = RootPath + "/Textures/Palette-Gradient.png";

        [MenuItem("Tools/Fix Firefighters Pack Materials")]
        public static void RunFromMenu()
        {
            RunInternal();
        }

        public static void RunBatch()
        {
            RunInternal();
        }

        private static void RunInternal()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                Debug.LogError("[FixFirefightersPackMaterials] Universal Render Pipeline/Lit shader bulunamadi.");
                return;
            }

            Texture2D paletteTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PaletteTexturePath);
            bool changedAny = false;

            changedAny |= FixMaterial(BaseMaterialPath, urpLit, paletteTexture, applyTexture: true);
            changedAny |= FixMaterial(LightsMaterialPath, urpLit, paletteTexture: null, applyTexture: false);

            AssetDatabase.ImportAsset(RootPath, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(changedAny
                ? "[FixFirefightersPackMaterials] FirefightersPack materyalleri onarildi."
                : "[FixFirefightersPackMaterials] Degisecek materyal bulunamadi.");
        }

        private static bool FixMaterial(string materialPath, Shader shader, Texture2D paletteTexture, bool applyTexture)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Debug.LogWarning($"[FixFirefightersPackMaterials] Materyal bulunamadi: {materialPath}");
                return false;
            }

            bool changed = false;

            if (material.shader != shader)
            {
                material.shader = shader;
                changed = true;
            }

            if (applyTexture && paletteTexture != null)
            {
                if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != paletteTexture)
                {
                    material.SetTexture("_BaseMap", paletteTexture);
                    changed = true;
                }

                if (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != paletteTexture)
                {
                    material.SetTexture("_MainTex", paletteTexture);
                    changed = true;
                }
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 0f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 2f);
            }

            if (material.HasProperty("_BaseColor") && material.name == "Lights")
            {
                material.SetColor("_BaseColor", new Color(0f, 0.45842022f, 1f, 1f));
                changed = true;
            }

            if (material.HasProperty("_Color") && material.name == "Lights")
            {
                material.SetColor("_Color", new Color(0f, 0.45842022f, 1f, 1f));
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(material);
                Debug.Log($"[FixFirefightersPackMaterials] Guncellendi: {materialPath}");
            }

            return changed;
        }
    }
}
#endif
