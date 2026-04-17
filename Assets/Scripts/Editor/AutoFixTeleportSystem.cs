using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using TriyajModul3;

public class AutoFixTeleportSystem : EditorWindow
{
    [MenuItem("Tools/1 TIKLA: Kapi NPC ve Isinlanma Sistemini Birlestir")]
    public static void FixEverything()
    {
        // 1. NPC'yi (Bodyguard) bul
        EntranceNPCInteractable npc = FindObjectOfType<EntranceNPCInteractable>();
        if (npc == null)
        {
            Debug.LogError("❌ Sahnede EntranceNPCInteractable atanmış bir Bodyguard bulamadım!");
            return;
        }

        // 2. OnayMenusuCanvas objesini bul
        GameObject onayCanvas = GameObject.Find("OnayMenusuCanvas");
        if (onayCanvas == null)
        {
            // Belki inaktiftir, inaktifleri de tarayalım
            Transform[] allObjects = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform t in allObjects)
            {
                if (t.name == "OnayMenusuCanvas" && t.gameObject.hideFlags == HideFlags.None)
                {
                    onayCanvas = t.gameObject;
                    break;
                }
            }
        }

        if (onayCanvas == null)
        {
            Debug.LogError("❌ Sahnede OnayMenusuCanvas isimli objeyi bulamadım!");
            return;
        }

        // Canvas'ı NPC'ye bağla
        npc.onayMenusuCanvas = onayCanvas;
        EditorUtility.SetDirty(npc);

        // 3. OnayMenusuCanvas üstünde OnayMenusuManager var mı kontrol et, yoksa ekle
        OnayMenusuManager manager = onayCanvas.GetComponent<OnayMenusuManager>();
        if (manager == null)
        {
            manager = onayCanvas.AddComponent<OnayMenusuManager>();
        }

        // 4. Gerekli Ayarları (XR Origin ve Hastane hedefi) Otomatik Doldur
        XROrigin player = FindObjectOfType<XROrigin>();
        if (player != null) manager.xrOrigin = player;

        GameObject hastane = GameObject.Find("Triyaj_hastane");
        if (hastane != null) 
        {
            manager.hedefNokta = hastane.transform;
        }

        // 5. Evet ve Hayır butonlarını otomatik bulup tıklandığında ne yapacaklarını bağla
        Button[] buttons = onayCanvas.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            string btnName = btn.gameObject.name.ToLower();
            
            // Tüm eski tıklama olaylarını temizle (Çakışma olmasın)
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(btn.onClick, manager.EvetTiklandi);
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(btn.onClick, manager.HayirTiklandi);

            // Evet butonuysa "EvetTiklandi" fonksiyonunu bağla
            if (btnName.Contains("evet") || btnName.Contains("yes") || btnName.Contains("onay"))
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, manager.EvetTiklandi);
            }
            // Hayır butonuysa "HayirTiklandi" fonksiyonunu bağla
            else if (btnName.Contains("hayir") || btnName.Contains("hayır") || btnName.Contains("no") || btnName.Contains("iptal"))
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, manager.HayirTiklandi);
            }
        }

        EditorUtility.SetDirty(manager);

        Debug.Log("✅ [BAŞARILI] Bodyguard kapıcı sistemi, Canvas'ı ve Butonları otomatik olarak birbirine bağlandı! Play'e basıp deneyebilirsin.");
    }
}
