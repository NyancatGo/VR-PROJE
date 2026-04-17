#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Modul2_Guvenlik sahnesindeki bozuk referansları temizlemek için
/// sahneyi açıp kaydeder. Unity otomatik olarak bozuk referansları düzeltir.
/// Menü: Tools > Fix Scene References
/// </summary>
public static class FixSceneReferences
{
    [MenuItem("Tools/Fix Scene References (Modul2)")]
    public static void FixModul2Scene()
    {
        string scenePath = "Assets/Samples/XR Interaction Toolkit/2.6.5/XR Device Simulator/Scenes/Modul2_Guvenlik.unity";
        
        if (!EditorUtility.DisplayDialog("Sahne Düzeltme",
            "Modul2_Guvenlik sahnesindeki bozuk referanslar temizlenecek.\n" +
            "Mevcut sahnedeki kaydedilmemiş değişiklikler kaybolabilir.\n\nDevam etmek istiyor musunuz?",
            "Evet", "İptal"))
            return;
        
        // Sahneyi aç
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        // Bozuk bileşenleri temizle
        int cleanedCount = 0;
        var allObjects = Object.FindObjectsOfType<GameObject>();
        
        foreach (var go in allObjects)
        {
            // Fazla AudioListener'ları kaldır
            var listeners = go.GetComponents<AudioListener>();
            if (listeners.Length > 1)
            {
                for (int i = 1; i < listeners.Length; i++)
                {
                    Object.DestroyImmediate(listeners[i]);
                    cleanedCount++;
                }
            }
        }
        
        // XR Origin düzeltmeleri
        var xrOrigins = Object.FindObjectsOfType<Unity.XR.CoreUtils.XROrigin>();
        foreach (var xrOrigin in xrOrigins)
        {
            // Çift Rigidbody kaldır
            var rigidbodies = xrOrigin.GetComponents<Rigidbody>();
            if (rigidbodies.Length > 1)
            {
                for (int i = 1; i < rigidbodies.Length; i++)
                {
                    Object.DestroyImmediate(rigidbodies[i]);
                    cleanedCount++;
                }
            }
        }
        
        // Sahneyi dirty olarak işaretle ve kaydet
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        
        Debug.Log($"[FixSceneReferences] Modul2_Guvenlik düzeltildi. {cleanedCount} bozuk bileşen temizlendi.");
        EditorUtility.DisplayDialog("Tamamlandı", 
            $"Sahne düzeltildi ve kaydedildi.\n{cleanedCount} bozuk bileşen temizlendi.", "Tamam");
    }
}
#endif
