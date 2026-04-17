using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR
public class ModuleDataGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Module Data Assets")]
    public static void Generate()
    {
        string folderPath = "Assets/Data/Modules";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            AssetDatabase.CreateFolder("Assets/Data", "Modules");
        }

        for (int i = 1; i <= 4; i++)
        {
            ModuleData data = ScriptableObject.CreateInstance<ModuleData>();
            data.moduleTitle = $"MODÜL {i}";
            data.sceneName = $"Modul{i}";
            data.moduleDescription = $"Modül {i} için eğitim içeriği ve senaryo açıklaması.";
            
            string assetPath = $"{folderPath}/Module{i}Data.asset";
            AssetDatabase.CreateAsset(data, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("4 tane Modül Veri dosyası 'Assets/Data/Modules' klasöründe oluşturuldu!");
    }
}
#endif
