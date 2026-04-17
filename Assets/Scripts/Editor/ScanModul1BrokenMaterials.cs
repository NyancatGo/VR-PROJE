#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Modul1MaterialDiagnostics
{
    public static class ScanModul1BrokenMaterials
    {
        private const string ScenePath =
            "Assets/Samples/XR Interaction Toolkit/2.6.5/XR Device Simulator/Scenes/Modul1.unity";

        [MenuItem("Tools/Scan Modul1 Broken Materials")]
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
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var findings = new List<string>();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material material = materials[i];
                        if (material == null)
                        {
                            findings.Add($"NULL_MATERIAL | {GetPath(renderer.transform)} | slot:{i}");
                            continue;
                        }

                        Shader shader = material.shader;
                        string shaderName = shader != null ? shader.name : "<null>";

                        if (shader == null || shaderName == "Hidden/InternalErrorShader")
                        {
                            findings.Add(
                                $"BROKEN_SHADER | {GetPath(renderer.transform)} | material:{material.name} | shader:{shaderName} | asset:{AssetDatabase.GetAssetPath(material)}");
                        }
                    }
                }
            }

            if (findings.Count == 0)
            {
                Debug.Log("[ScanModul1BrokenMaterials] Broken material bulunamadi.");
                return;
            }

            foreach (string finding in findings)
            {
                Debug.Log($"[ScanModul1BrokenMaterials] {finding}");
            }
        }

        private static string GetPath(Transform current)
        {
            if (current == null)
                return "<null>";

            var parts = new Stack<string>();
            while (current != null)
            {
                parts.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", parts.ToArray());
        }
    }
}
#endif
