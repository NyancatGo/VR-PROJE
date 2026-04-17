using UnityEngine;
using UnityEditor;

public class CreateRedMaterial
{
    [MenuItem("Tools/Create Red First Aid Material")]
    public static void Execute()
    {
        // Create red material
        Material redMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        
        // Load the red albedo texture
        Texture2D redAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GeeKay3D/First-Aid-Set/Assets/Textures/red/FirstAidKit_red_AlbedoTransparency.png");
        Texture2D redNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GeeKay3D/First-Aid-Set/Assets/Textures/red/FirstAidKit_red_Normal.png");
        Texture2D redMetallic = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GeeKay3D/First-Aid-Set/Assets/Textures/red/FirstAidKit_red_MetallicSmoothness.png");
        Texture2D redAO = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GeeKay3D/First-Aid-Set/Assets/Textures/red/FirstAidKit_red_AmbientOcclusion.png");
        
        if (redAlbedo != null)
        {
            redMat.SetTexture("_BaseMap", redAlbedo);
            redMat.SetColor("_BaseColor", Color.white);
        }
        
        if (redNormal != null)
        {
            redMat.SetTexture("_BumpMap", redNormal);
            redMat.EnableKeyword("_NORMALMAP");
        }
        
        if (redMetallic != null)
        {
            redMat.SetTexture("_MetallicGlossMap", redMetallic);
            redMat.SetFloat("_Smoothness", 0.5f);
        }
        
        if (redAO != null)
        {
            redMat.SetTexture("_OcclusionMap", redAO);
        }
        
        // Save the material
        string savePath = "Assets/GeeKay3D/First-Aid-Set/Assets/Textures/red/Materials/FirstAidKit_red_AlbedoTransparency.mat";
        
        // Ensure directory exists
        string dir = System.IO.Path.GetDirectoryName(savePath);
        if (!System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }
        
        AssetDatabase.CreateAsset(redMat, savePath);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"Created red material at: {savePath}");
        
        // Now assign to red first aid kits
        GameObject redKit1 = GameObject.Find("TriyajAlanı/IlkYardimCantalari/FirstAidKit_Red");
        GameObject redKit2 = GameObject.Find("TriyajAlanı/IlkYardimCantalari/FirstAidKit_Red_2");
        
        Material createdRedMat = AssetDatabase.LoadAssetAtPath<Material>(savePath);
        
        if (redKit1 != null)
        {
            MeshRenderer mr = redKit1.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = createdRedMat;
                Debug.Log("Assigned red material to FirstAidKit_Red");
            }
        }
        
        if (redKit2 != null)
        {
            MeshRenderer mr = redKit2.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = createdRedMat;
                Debug.Log("Assigned red material to FirstAidKit_Red_2");
            }
        }
    }
}
