#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using System.IO;
using System.Linq;

namespace FBXFixer
{
    /// <summary>
    /// James 5 Police ve benzeri FBX modellerinin texture/material sorunlarını çözer.
    /// URP uyumlu hale getirir.
    /// </summary>
    public class FixFBXMaterials : EditorWindow
    {
        [MenuItem("Tools/Fix FBX Materials")]
        public static void ShowWindow()
        {
            GetWindow<FixFBXMaterials>("FBX Material Fixer");
        }

        private string fbxPath = "Assets/Fbx/James 5 police.fbx";
        private string outputFolder = "Assets/Fbx/James_Materials";

        void OnGUI()
        {
            GUILayout.Label("FBX Texture & Material Onarıcı", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            GUILayout.Label("FBX Dosya Yolu:");
            fbxPath = EditorGUILayout.TextField(fbxPath);

            GUILayout.Label("Materyallerin Kaydedileceği Klasör:");
            outputFolder = EditorGUILayout.TextField(outputFolder);

            EditorGUILayout.Space();

            if (GUILayout.Button("1) Materyalleri Çıkart (Extract)", GUILayout.Height(40)))
            {
                ExtractMaterials();
            }

            if (GUILayout.Button("2) Shader'ları URP Lit Yap", GUILayout.Height(40)))
            {
                ConvertToURP();
            }

            if (GUILayout.Button("3) Texture'ları Bağla", GUILayout.Height(40)))
            {
                ReassignTextures();
            }

            EditorGUILayout.Space();
            GUILayout.Label("Tüm Adımları Sırayla Çalıştır!", EditorStyles.helpBox);
        }

        static void ExtractMaterials()
        {
            string fbxPath = "Assets/Fbx/James 5 police.fbx";
            string outputFolder = "Assets/Fbx/James_Materials";

            // Klasör oluştur
            if (!AssetDatabase.IsValidFolder(outputFolder))
            {
                AssetDatabase.CreateFolder("Assets/Fbx", "James_Materials");
            }

            // FBX'i al
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx == null)
            {
                Debug.LogError($"FBX bulunamadı: {fbxPath}");
                return;
            }

            // FBX'den material references al
            Renderer[] renderers = fbx.GetComponentsInChildren<Renderer>();
            int extractedCount = 0;

            foreach (Renderer renderer in renderers)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat == null) continue;

                    // Materyali kopyala ve yeni isim ver
                    string matName = mat.name.Replace(" ", "_");
                    string newMatPath = $"{outputFolder}/{matName}.mat";

                    // Varsa atla
                    if (AssetDatabase.LoadAssetAtPath<Material>(newMatPath) != null)
                    {
                        Debug.Log($"Materyal zaten var: {newMatPath}");
                        continue;
                    }

                    // Yeni materyal oluştur
                    Material newMat = new Material(mat);
                    AssetDatabase.CreateAsset(newMat, newMatPath);
                    extractedCount++;

                    Debug.Log($"Materyal çıkartıldı: {newMatPath}");
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Toplam {extractedCount} materyal çıkartıldı.");
        }

        static void ConvertToURP()
        {
            string outputFolder = "Assets/Fbx/James_Materials";

            // URP Lit shader al
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                Debug.LogError("URP Lit shader bulunamadı!");
                return;
            }

            // Tüm materyalleri al
            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { outputFolder });
            int convertedCount = 0;

            foreach (string guid in matGuids)
            {
                string matPath = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

                if (mat == null) continue;

                // Shader'ı URP Lit yap
                mat.shader = urpLitShader;
                EditorUtility.SetDirty(mat);

                // Temel URP ayarları
                mat.SetFloat("_Surface", 0); // Opaque
                mat.SetFloat("_Blend", 0);

                convertedCount++;
                Debug.Log($"Shader güncellendi: {mat.name}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Toplam {convertedCount} materyal URP Lit'e çevrildi.");
        }

        static void ReassignTextures()
        {
            string fbxPath = "Assets/Fbx/James 5 police.fbx";
            string outputFolder = "Assets/Fbx/James_Materials";

            // FBX'deki eski materyalleri al
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx == null)
            {
                Debug.LogError($"FBX bulunamadı: {fbxPath}");
                return;
            }

            // Texture dosyalarını bul
            string[] texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Fbx" });

            foreach (string texGuid in texGuids)
            {
                string texPath = AssetDatabase.GUIDToAssetPath(texGuid);
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if (tex == null) continue;

                string texName = tex.name.ToLower();
                Debug.Log($"Texture bulundu: {tex.name} | Path: {texPath}");

                // Materyalleri tara ve texture'ı ata
                string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { outputFolder });

                foreach (string matGuid in matGuids)
                {
                    string matPath = AssetDatabase.GUIDToAssetPath(matGuid);
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat == null) continue;

                    // Texture ismine göre materyale ata
                    if (texName.Contains("albedo") || texName.Contains("diffuse") || texName.Contains("basecolor") || texName.Contains("color"))
                    {
                        mat.SetTexture("_BaseMap", tex);
                        mat.SetTexture("_MainTex", tex);
                        Debug.Log($"  → {mat.name}'e BaseMap olarak atandı");
                    }
                    else if (texName.Contains("normal") || texName.Contains("nrm"))
                    {
                        mat.SetTexture("_BumpMap", tex);
                        mat.EnableKeyword("_NORMALMAP");
                        Debug.Log($"  → {mat.name}'e NormalMap olarak atandı");
                    }
                    else if (texName.Contains("metalic") || texName.Contains("metallic") || texName.Contains("metal"))
                    {
                        mat.SetTexture("_MetallicGlossMap", tex);
                        mat.EnableKeyword("_METALLICGLOSSMAP");
                        Debug.Log($"  → {mat.name}'e MetallicMap olarak atandı");
                    }
                    else if (texName.Contains("roughness") || texName.Contains("rough"))
                    {
                        mat.SetFloat("_GlossMapScale", 1f);
                        Debug.Log($"  → {mat.name}'e Roughness atandı");
                    }
                    else if (texName.Contains("ao") || texName.Contains("ambient"))
                    {
                        mat.SetTexture("_OcclusionMap", tex);
                        Debug.Log($"  → {mat.name}'e AO olarak atandı");
                    }
                    else if (texName.Contains("emissive") || texName.Contains("emission"))
                    {
                        mat.SetTexture("_EmissionMap", tex);
                        mat.EnableKeyword("_EMISSION");
                        Debug.Log($"  → {mat.name}'e Emission olarak atandı");
                    }

                    EditorUtility.SetDirty(mat);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Texture ataması tamamlandı!");
        }

        /// <summary>
        /// Sahnede seçili objenin materyallerini düzeltir
        /// </summary>
        [MenuItem("Tools/Fix Selected Object Materials")]
        static void FixSelectedObject()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("Bir obje seçili değil!");
                return;
            }

            Renderer[] renderers = selected.GetComponentsInChildren<Renderer>();
            int fixedCount = 0;

            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");

            foreach (Renderer renderer in renderers)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat == null) continue;

                    // Shader kontrolü
                    if (mat.shader.name.Contains("Standard") || !mat.shader.name.Contains("Universal"))
                    {
                        if (urpLitShader != null)
                        {
                            mat.shader = urpLitShader;
                            EditorUtility.SetDirty(mat);
                            fixedCount++;
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Seçili objedeki {fixedCount} materyal düzeltildi!");
        }

        /// <summary>
        /// Tüm FBX modellerini otomatik düzeltir
        /// </summary>
        [MenuItem("Tools/Fix All FBX In Project")]
        static void FixAllFBX()
        {
            string[] fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets" });
            int fixedCount = 0;

            foreach (string guid in fbxGuids)
            {
                string fbxPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!fbxPath.EndsWith(".fbx")) continue;

                // FBX import ayarlarını al
                ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
                if (importer == null) continue;

                // Import ayarlarını yeniden çalıştır
                importer.SaveAndReimport();
                fixedCount++;

                Debug.Log($"FBX düzeltildi: {fbxPath}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Toplam {fixedCount} FBX dosyası güncellendi!");
        }
    }
}
#endif
