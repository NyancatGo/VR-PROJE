using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CreateFirstAidKits
{
    [MenuItem("Tools/Fix First Aid Kits")]
    public static void Execute()
    {
        // Get the scene root
        GameObject root = GameObject.Find("TriyajAlanı");
        if (root == null)
        {
            root = new GameObject("TriyajAlanı");
        }
        
        // Find or create TriageArea parent
        Transform parent = root.transform.Find("IlkYardimCantalari");
        if (parent == null)
        {
            GameObject parentObj = new GameObject("IlkYardimCantalari");
            parentObj.transform.SetParent(root.transform);
            parent = parentObj.transform;
        }
        
        // Try to load the mesh from Survival Tools
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Survival Tools/Meshes/firstaid.fbx");
        
        // Load materials
        Material greenMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Survival Tools/Materials/FirstAid.mat");
        Material redMat = null;
        Material whiteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/GeeKay3D/First-Aid-Set/Assets/Textures/white/Materials/FirstAidKit_white_AlbedoTransparency.mat");
        
        // Check for red material
        string[] redMats = new string[] {
            "Assets/Survival Tools/Materials/FirstAid.mat",
            "Assets/GeeKay3D/First-Aid-Set/Assets/Textures/red/Materials/FirstAidKit_red_AlbedoTransparency.mat"
        };
        
        foreach (string matPath in redMats)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (m != null)
            {
                redMat = m;
                break;
            }
        }
        
        // Create first aid kits
        CreateKit(parent, "FirstAidKit_Green", new Vector3(-1.409f, 0.5f, 17.442f), Quaternion.identity, greenMat, mesh, new Vector3(0.468f, 0.468f, 0.468f));
        CreateKit(parent, "FirstAidKit_Red", new Vector3(2.4f, 0.5f, 17.476f), Quaternion.identity, redMat, mesh, new Vector3(0.475f, 0.475f, 0.475f));
        CreateKit(parent, "FirstAidKit_Red_2", new Vector3(-3.91f, 0.5f, 17.39f), Quaternion.identity, redMat, mesh, new Vector3(0.41f, 0.41f, 0.41f));
        CreateKit(parent, "FirstAidKit_White", new Vector3(4.71f, 0.5f, 17.492f), Quaternion.identity, whiteMat, mesh, new Vector3(0.402f, 0.402f, 0.402f));
        
        Debug.Log("First Aid Kits fixed successfully!");
    }
    
    private static void CreateKit(Transform parent, string name, Vector3 localPos, Quaternion localRot, Material material, Mesh mesh, Vector3 scale)
    {
        GameObject kit = new GameObject(name);
        kit.transform.SetParent(parent);
        kit.transform.localPosition = localPos;
        kit.transform.localRotation = localRot;
        kit.transform.localScale = scale;
        
        if (mesh != null)
        {
            MeshFilter mf = kit.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
        }
        
        MeshRenderer mr = kit.AddComponent<MeshRenderer>();
        if (material != null)
        {
            mr.sharedMaterial = material;
        }
        
        // Add collider
        BoxCollider col = kit.AddComponent<BoxCollider>();
        col.size = new Vector3(1f, 0.6f, 0.3f);
        col.center = new Vector3(0f, 0f, 0f);
        
        Debug.Log($"Created {name}");
    }
}
