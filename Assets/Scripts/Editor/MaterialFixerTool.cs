using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
public class MaterialFixerTool : EditorWindow
{
    private string targetFolder = "Assets/school/Prefabs";

    [MenuItem("Tools/Material Fixer")]
    public static void ShowWindow()
    {
        GetWindow<MaterialFixerTool>("Material Fixer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Fix Prefab Material Slots", EditorStyles.boldLabel);
        targetFolder = EditorGUILayout.TextField("Target Folder", targetFolder);

        if (GUILayout.Button("Fix All Prefabs in Folder"))
        {
            FixPrefabsInFolder();
        }
    }

    private void FixPrefabsInFolder()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { targetFolder });
        int fixedCount = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab == null) continue;

            MeshRenderer[] renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
            bool modified = false;

            foreach (var renderer in renderers)
            {
                MeshFilter mf = renderer.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;

                int subMeshCount = mf.sharedMesh.subMeshCount;
                Material[] currentMats = renderer.sharedMaterials;

                // Check if we need to fix size or nulls
                bool needsFix = currentMats.Length > subMeshCount;
                if (!needsFix)
                {
                    // Even if size is OK, check if slot 0 is null
                    for(int i=0; i < subMeshCount; i++) if(i < currentMats.Length && currentMats[i] == null) needsFix = true;
                }

                if (needsFix)
                {
                    Material firstValid = null;
                    foreach (var m in currentMats) if (m != null) { firstValid = m; break; }

                    Material[] newMats = new Material[subMeshCount];
                    for (int i = 0; i < subMeshCount; i++)
                    {
                        if (i < currentMats.Length && currentMats[i] != null)
                            newMats[i] = currentMats[i];
                        else
                            newMats[i] = firstValid; // Default to first valid found
                    }

                    renderer.sharedMaterials = newMats;
                    modified = true;
                    Debug.Log($"Fixed renderer on {prefab.name} at {path}");
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(prefab);
                fixedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Finished. Fixed {fixedCount} prefabs.");
    }
}
#endif
