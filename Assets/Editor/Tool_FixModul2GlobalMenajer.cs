using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRAfet.Editor
{
    /// <summary>
    /// Modul2 First Aid UI Fix Tool
    ///
    /// Ensures exactly one IlkyardimGlobalMenajer component exists in Modul2_Guvenlik,
    /// preferably on the same GameObject as IlkyardimUIMenajeri.
    ///
    /// Usage:
    /// - Menu: VR AFET > Fix Modul2 - Add IlkyardimGlobalMenajer
    /// - OR: VR AFET > Verify Modul2 Setup
    /// </summary>
    public class Tool_FixModul2GlobalMenajer
    {
        private const string SCENE_PATH = "Assets/Samples/XR Interaction Toolkit/2.6.5/XR Device Simulator/Scenes/Modul2_Guvenlik.unity";

        [MenuItem("VR AFET/Fix Modul2 - Add IlkyardimGlobalMenajer")]
        public static void FixModul2()
        {
            try
            {
                Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
                if (!scene.IsValid())
                {
                    Debug.LogError($"[FixModul2] Sahne acilamadi: {SCENE_PATH}");
                    return;
                }

                IlkyardimGlobalMenajer[] existing = Object.FindObjectsOfType<IlkyardimGlobalMenajer>(true);

                if (existing.Length == 1)
                {
                    bool colocated = existing[0].GetComponent<IlkyardimUIMenajeri>() != null;
                    Debug.Log($"[FixModul2] IlkyardimGlobalMenajer zaten mevcut: {existing[0].gameObject.name} (UIMenajeri ile ayni obje: {colocated})");
                    EditorSceneManager.SaveScene(scene);
                    return;
                }

                if (existing.Length > 1)
                {
                    Debug.LogWarning($"[FixModul2] {existing.Length} adet IlkyardimGlobalMenajer bulundu! Fazlalar temizleniyor.");
                    CleanupDuplicates(existing);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("[FixModul2] Duplicate temizligi tamamlandi ve sahne kaydedildi.");
                    return;
                }

                // Hic yok - IlkyardimUIMenajeri'nin bulundugu objeye ekle (tercih edilen yaklasim)
                IlkyardimUIMenajeri uiManager = Object.FindObjectOfType<IlkyardimUIMenajeri>(true);
                if (uiManager != null)
                {
                    uiManager.gameObject.AddComponent<IlkyardimGlobalMenajer>();
                    EditorUtility.SetDirty(uiManager.gameObject);
                    Debug.Log($"[FixModul2] IlkyardimGlobalMenajer, mevcut UIMenajeri objesine eklendi: {uiManager.gameObject.name}");
                }
                else
                {
                    GameObject newGO = new GameObject("IlkyardimSistemi");
                    newGO.AddComponent<IlkyardimGlobalMenajer>();
                    newGO.AddComponent<IlkyardimUIMenajeri>();
                    EditorUtility.SetDirty(newGO);
                    Debug.Log("[FixModul2] Yeni IlkyardimSistemi objesi olusturuldu (GlobalMenajer + UIMenajeri).");
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[FixModul2] Sahne kaydedildi: {SCENE_PATH}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FixModul2] Hata: {ex.Message}\n{ex.StackTrace}");
            }
        }

        [MenuItem("VR AFET/Verify Modul2 Setup")]
        public static void VerifyModul2Setup()
        {
            try
            {
                Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
                if (!scene.IsValid())
                {
                    Debug.LogWarning($"[FixModul2] Sahne acilamadi: {SCENE_PATH}");
                    return;
                }

                IlkyardimGlobalMenajer[] globalManagers = Object.FindObjectsOfType<IlkyardimGlobalMenajer>(true);
                IlkyardimUIMenajeri[] uiManagers = Object.FindObjectsOfType<IlkyardimUIMenajeri>(true);
                GameObject ilkyardimCanvas = GameObject.Find("Ilkyardim_Canvas");

                Debug.Log("=== Modul2_Guvenlik Setup Verification ===");
                Debug.Log($"IlkyardimGlobalMenajer: {globalManagers.Length} adet {(globalManagers.Length == 1 ? "OK" : "BEKLENEN: 1")}");
                Debug.Log($"IlkyardimUIMenajeri: {uiManagers.Length} adet {(uiManagers.Length >= 1 ? "OK" : "EKSIK")}");
                Debug.Log($"Ilkyardim_Canvas: {(ilkyardimCanvas != null ? "OK" : "EKSIK")}");

                for (int i = 0; i < globalManagers.Length; i++)
                {
                    bool hasUI = globalManagers[i].GetComponent<IlkyardimUIMenajeri>() != null;
                    Debug.Log($"  GlobalMenajer[{i}]: obje={globalManagers[i].gameObject.name}, hasUIMenajeri={hasUI}, active={globalManagers[i].gameObject.activeInHierarchy}");
                }

                bool hasColocated = false;
                for (int i = 0; i < globalManagers.Length; i++)
                {
                    if (globalManagers[i].GetComponent<IlkyardimUIMenajeri>() != null)
                    {
                        hasColocated = true;
                        break;
                    }
                }

                if (globalManagers.Length != 1)
                {
                    Debug.LogWarning($"UYARI: IlkyardimGlobalMenajer sayisi {globalManagers.Length}! Beklenen: 1. Duzeltmek icin: VR AFET > Fix Modul2");
                }
                else if (!hasColocated)
                {
                    Debug.LogWarning("UYARI: IlkyardimGlobalMenajer, IlkyardimUIMenajeri ile ayni objede degil! Duzeltmek icin: VR AFET > Fix Modul2");
                }
                else
                {
                    Debug.Log("Tum bilesenler dogru kurulu!");
                }

                EditorSceneManager.SaveScene(scene);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FixModul2] Dogrulama hatasi: {ex.Message}");
            }
        }

        private static void CleanupDuplicates(IlkyardimGlobalMenajer[] instances)
        {
            // IlkyardimUIMenajeri ile ayni objede olan instance'i koru (en yuksek oncelik)
            IlkyardimGlobalMenajer keeper = null;
            for (int i = 0; i < instances.Length; i++)
            {
                if (instances[i].GetComponent<IlkyardimUIMenajeri>() != null)
                {
                    keeper = instances[i];
                    break;
                }
            }

            if (keeper == null)
            {
                keeper = instances[0];
            }

            for (int i = 0; i < instances.Length; i++)
            {
                if (instances[i] == keeper)
                {
                    continue;
                }

                GameObject go = instances[i].gameObject;
                Component[] components = go.GetComponents<Component>();

                // Sadece Transform + IlkyardimGlobalMenajer varsa tum objeyi sil
                if (components.Length <= 2)
                {
                    Debug.Log($"[FixModul2] Duplicate obje siliniyor: {go.name}");
                    Object.DestroyImmediate(go);
                }
                else
                {
                    Debug.Log($"[FixModul2] Duplicate component siliniyor: {go.name} (obje korunuyor, baska componentler var)");
                    Object.DestroyImmediate(instances[i]);
                }
            }

            Debug.Log($"[FixModul2] Korunan instance: {keeper.gameObject.name}");
        }
    }
}
