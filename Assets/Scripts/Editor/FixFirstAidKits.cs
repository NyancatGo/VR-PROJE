using UnityEngine;

public class FixFirstAidKits : MonoBehaviour
{
    [Header("First Aid Kit Settings")]
    public GameObject firstAidKitPrefab;
    public Material[] kitMaterials;
    
    void Start()
    {
        // This script is for runtime reference - actual creation done via editor
    }
    
    public static void CreateFixedFirstAidKit(Transform parent, Vector3 position, Quaternion rotation, Material material, string name)
    {
        // Create a new GameObject
        GameObject kit = new GameObject(name);
        kit.transform.SetParent(parent);
        kit.transform.localPosition = position;
        kit.transform.localRotation = rotation;
        kit.transform.localScale = Vector3.one;
        
        // Add MeshFilter with the firstaid mesh
        MeshFilter meshFilter = kit.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = Resources.Load<Mesh>("Meshes/firstaid");
        
        // Add MeshRenderer with material
        MeshRenderer renderer = kit.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        
        // Add BoxCollider for interaction
        BoxCollider collider = kit.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.3f, 0.2f, 0.15f);
        collider.center = new Vector3(0f, 0f, 0f);
        
        Debug.Log($"Created {name} with material: {material?.name ?? "null"}");
    }
}
