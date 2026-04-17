using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FixNegativeColliders
{
    [MenuItem("Tools/Fix Negative BoxColliders (Scenes and Prefabs)")]
    public static void Fix()
    {
        int fixedCount = 0;

        // 1. Process all Prefabs in the project
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            bool modified = false;
            BoxCollider[] colliders = prefab.GetComponentsInChildren<BoxCollider>(true);
            foreach (var col in colliders)
            {
                if (FixCollider(col))
                {
                    modified = true;
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(prefab);
                fixedCount++;
            }
        }

        // 2. Process active scene objects
        BoxCollider[] sceneColliders = Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var col in sceneColliders)
        {
            // Skip prefabs, only modify non-prefab scene objects
            if (!PrefabUtility.IsPartOfPrefabAsset(col))
            {
                if (FixCollider(col))
                {
                    EditorUtility.SetDirty(col.gameObject);
                    fixedCount++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"FixNegativeColliders: Processed and fixed {fixedCount} assets/objects.");
    }

    private static bool FixCollider(BoxCollider col)
    {
        bool changed = false;

        // Note: Prefabs don't have accurate lossyScale if they're not in a scene,
        // but we can check if their local scale contains negatives up the hierarchy.
        Vector3 globalScale = GetGlobalScale(col.transform);
        if (globalScale.x < 0 || globalScale.y < 0 || globalScale.z < 0)
        {
            GameObject go = col.gameObject;
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                // Replace with MeshCollider
                Undo.RecordObject(go, "Replace BoxCollider with MeshCollider");
                Object.DestroyImmediate(col, true);
                MeshCollider mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = true;
                Debug.Log($"Replaced BoxCollider with MeshCollider on negatively scaled object: {GetFullPath(go)}", go);
                return true; // We destroyed it, no further checks needed on this component instance
            }
            else
            {
                // Try fixing the local scale instead if there is no mesh
                if (go.transform.localScale.x < 0 || go.transform.localScale.y < 0 || go.transform.localScale.z < 0)
                {
                    Undo.RecordObject(go.transform, "Fix Negative Scale");
                    go.transform.localScale = new Vector3(
                        Mathf.Abs(go.transform.localScale.x),
                        Mathf.Abs(go.transform.localScale.y),
                        Mathf.Abs(go.transform.localScale.z)
                    );
                    Debug.Log($"Fixed negative scale on object with BoxCollider: {GetFullPath(go)}", go);
                    changed = true;
                }
            }
        }

        // Check size
        if (col != null && (col.size.x < 0 || col.size.y < 0 || col.size.z < 0))
        {
            Undo.RecordObject(col, "Fix BoxCollider negative size");
            Vector3 newSize = col.size;
            newSize.x = Mathf.Abs(newSize.x);
            newSize.y = Mathf.Abs(newSize.y);
            newSize.z = Mathf.Abs(newSize.z);
            col.size = newSize;
            Debug.Log($"Fixed negative size on BoxCollider for: {GetFullPath(col.gameObject)}", col.gameObject);
            changed = true;
        }

        return changed;
    }

    private static Vector3 GetGlobalScale(Transform t)
    {
        Vector3 scale = t.localScale;
        while (t.parent != null)
        {
            t = t.parent;
            scale = Vector3.Scale(scale, t.localScale);
        }
        return scale;
    }

    private static string GetFullPath(GameObject go)
    {
        string path = go.name;
        Transform parent = go.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}
