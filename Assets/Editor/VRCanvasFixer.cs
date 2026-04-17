#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace TriyajModul3
{
    /// <summary>
    /// Mevcut Canvas objelerini VR uyumlu hale getirir.
    /// GraphicRaycaster'ı TrackedDeviceGraphicRaycaster ile değiştirir.
    /// </summary>
    public static class VRCanvasFixer
    {
        [MenuItem("VR Tools/Canvas'i VR Uyumlu Yap")]
        public static void FixSelectedCanvas()
        {
            GameObject selected = Selection.activeGameObject;
            
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Hata", "Lütfen Hierarchy'de bir Canvas seçin!", "Tamam");
                return;
            }
            
            Canvas canvas = selected.GetComponent<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Hata", "Seçilen obje bir Canvas değil!", "Tamam");
                return;
            }
            
            int fixCount = 0;
            
            // 1. GraphicRaycaster'ı kaldır, TrackedDeviceGraphicRaycaster ekle
            GraphicRaycaster oldRaycaster = selected.GetComponent<GraphicRaycaster>();
            TrackedDeviceGraphicRaycaster trackedRaycaster = selected.GetComponent<TrackedDeviceGraphicRaycaster>();
            
            if (oldRaycaster != null && trackedRaycaster == null)
            {
                Undo.DestroyObjectImmediate(oldRaycaster);
                Undo.AddComponent<TrackedDeviceGraphicRaycaster>(selected);
                fixCount++;
                Debug.Log("✓ GraphicRaycaster → TrackedDeviceGraphicRaycaster değiştirildi");
            }
            else if (trackedRaycaster == null)
            {
                Undo.AddComponent<TrackedDeviceGraphicRaycaster>(selected);
                fixCount++;
                Debug.Log("✓ TrackedDeviceGraphicRaycaster eklendi");
            }
            
            // 2. Tüm objeleri UI layer'a ata
            int layerUI = LayerMask.NameToLayer("UI");
            SetLayerRecursively(selected, layerUI, ref fixCount);
            
            // 3. Tüm Text/TMP'lerde raycastTarget = false yap
            foreach (var tmp in selected.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
            {
                if (tmp.raycastTarget)
                {
                    Undo.RecordObject(tmp, "TMP Raycast Fix");
                    tmp.raycastTarget = false;
                    fixCount++;
                    Debug.Log($"✓ {tmp.gameObject.name} TextMeshPro raycastTarget kapatıldı");
                }
            }
            
            // 4. Button Image'larında raycastTarget = true kontrolü
            foreach (var btn in selected.GetComponentsInChildren<Button>(true))
            {
                Image img = btn.GetComponent<Image>();
                if (img != null && !img.raycastTarget)
                {
                    Undo.RecordObject(img, "Button Image Raycast Fix");
                    img.raycastTarget = true;
                    fixCount++;
                    Debug.Log($"✓ {btn.gameObject.name} Button Image raycastTarget açıldı");
                }
            }
            
            EditorUtility.SetDirty(selected);
            
            EditorUtility.DisplayDialog("VR Canvas Düzeltici",
                $"✅ Canvas VR uyumlu hale getirildi!\n\n" +
                $"Toplam {fixCount} düzeltme yapıldı.\n\n" +
                "MANUEL KONTROL EDİN:\n" +
                "1- XR Ray Interactor Raycast Mask → UI layer işaretli mi?\n" +
                "2- EventSystem'de XR UI Input Module var mı?\n" +
                "3- Canvas Event Camera atanmış mı?", "Tamam");
        }
        
        private static void SetLayerRecursively(GameObject obj, int layer, ref int count)
        {
            if (obj.layer != layer)
            {
                Undo.RecordObject(obj, "Layer Change");
                obj.layer = layer;
                count++;
            }
            
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer, ref count);
            }
        }
        
        [MenuItem("VR Tools/EventSystem Kontrol Et")]
        public static void CheckEventSystem()
        {
            var eventSystem = Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            
            if (eventSystem == null)
            {
                EditorUtility.DisplayDialog("EventSystem Yok!", 
                    "Sahnede EventSystem bulunamadı!\n\n" +
                    "XR Interaction Setup prefab'ını sahneye ekleyin:\n" +
                    "Assets/Samples/XR Interaction Toolkit/2.6.5/Starter Assets/Prefabs/XR Interaction Setup.prefab", 
                    "Tamam");
                return;
            }
            
            string report = "EventSystem Durumu:\n\n";
            
            // XR UI Input Module kontrolü
            var xrUIInput = eventSystem.GetComponent<XRUIInputModule>();
            if (xrUIInput != null)
            {
                report += "✓ XR UI Input Module: MEVCUT\n";
                report += $"  - Enable XR Input: {xrUIInput.enableXRInput}\n";
            }
            else
            {
                report += "❌ XR UI Input Module: YOK!\n";
                report += "   → EventSystem'e XR UI Input Module ekleyin\n";
            }
            
            // Standalone Input Module kontrolü
            var standalone = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (standalone != null)
            {
                report += "⚠️ Standalone Input Module: MEVCUT (kaldırılmalı)\n";
            }
            
            EditorUtility.DisplayDialog("EventSystem Raporu", report, "Tamam");
        }
    }
}
#endif
