using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class IlkyardimKurulumEditor : EditorWindow
{
    [MenuItem("Sistem Kurulumu/İlkyardım UI Kurulumu Yap")]
    public static void KurulumYap()
    {
        // Sistem Objesi Yarat/Bul
        GameObject ilkyardimSistemi = GameObject.Find("IlkyardimSistemi");
        if (ilkyardimSistemi == null) {
            ilkyardimSistemi = new GameObject("IlkyardimSistemi");
        }
        
        var globalMenajer = ilkyardimSistemi.GetComponent<IlkyardimGlobalMenajer>();
        if (globalMenajer == null)
            globalMenajer = ilkyardimSistemi.AddComponent<IlkyardimGlobalMenajer>();
            
        var uiMenajer = ilkyardimSistemi.GetComponent<IlkyardimUIMenajeri>();
        if (uiMenajer == null)
            uiMenajer = ilkyardimSistemi.AddComponent<IlkyardimUIMenajeri>();

        // Canvas Yarat/Bul
        GameObject canvasObj = GameObject.Find("Ilkyardim_Canvas");
        TextMeshProUGUI durumMetni = null;
        Button islemButonu = null;
        
        if (canvasObj == null) {
            canvasObj = new GameObject("Ilkyardim_Canvas", typeof(Canvas), typeof(CanvasScaler));
            
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.transform.position = new Vector3(0, 1.5f, 2f);
            canvasObj.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);
            
            var graphicRaycaster = canvasObj.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster != null) {
                DestroyImmediate(graphicRaycaster);
            }
            if (canvasObj.GetComponent<TrackedDeviceGraphicRaycaster>() == null) {
                canvasObj.AddComponent<TrackedDeviceGraphicRaycaster>();
            }
            
            GameObject panelObj = new GameObject("ArkaPlanPaneli", typeof(Image), typeof(VerticalLayoutGroup));
            panelObj.transform.SetParent(canvasObj.transform, false);
            panelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 500);
            panelObj.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            VerticalLayoutGroup vlg = panelObj.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(40, 40, 40, 40);
            vlg.spacing = 30;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;
            
            GameObject txtObj = new GameObject("DurumMetni", typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(panelObj.transform, false);
            durumMetni = txtObj.GetComponent<TextMeshProUGUI>();
            durumMetni.text = "Hazır.";
            durumMetni.alignment = TextAlignmentOptions.Center;
            durumMetni.fontSize = 40;
            durumMetni.color = Color.white;
            
            GameObject btnObj = new GameObject("IslemButonu", typeof(Image), typeof(Button), typeof(LayoutElement));
            btnObj.transform.SetParent(panelObj.transform, false);
            islemButonu = btnObj.GetComponent<Button>();
            islemButonu.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f, 1f);
            var layoutEl = btnObj.GetComponent<LayoutElement>();
            layoutEl.minHeight = 80;
            layoutEl.minWidth = 300;
            
            GameObject btnTxtObj = new GameObject("Text", typeof(TextMeshProUGUI));
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            var btnTxt = btnTxtObj.GetComponent<TextMeshProUGUI>();
            btnTxt.text = "Müdahale Et";
            btnTxt.color = Color.white;
            btnTxt.fontSize = 32;
            btnTxt.alignment = TextAlignmentOptions.Center;
            
        } else {
            durumMetni = canvasObj.GetComponentInChildren<TextMeshProUGUI>();
            islemButonu = canvasObj.GetComponentInChildren<Button>();
            
            if (canvasObj.GetComponent<GraphicRaycaster>()) {
                DestroyImmediate(canvasObj.GetComponent<GraphicRaycaster>());
            }
            if (canvasObj.GetComponent<TrackedDeviceGraphicRaycaster>() == null) {
                canvasObj.AddComponent<TrackedDeviceGraphicRaycaster>();
            }
        }
        
        uiMenajer.uiCanvas = canvasObj;
        uiMenajer.durumMetni = durumMetni;
        uiMenajer.islemButonu = islemButonu;

        // NPC'lerde IlkyardimNPCIndex güncelleme ve XRSimpleInteractable ekleme
        var npcler = GameObject.FindObjectsOfType<YaraliController>(true);
        int index = 0;
        foreach (var controller in npcler) {
            if (index >= 3) break;
            GameObject npc = controller.gameObject;
            
            var interactable = npc.GetComponent<XRSimpleInteractable>();
            if (interactable == null) {
                interactable = npc.AddComponent<XRSimpleInteractable>();
            }
            
            // Etkileşim tıklandığında UIMenajer'i aç
            interactable.activated.RemoveAllListeners();
            int nIndex = index;
            interactable.activated.AddListener((args) => {
                GameObject.FindObjectOfType<IlkyardimUIMenajeri>()?.ArayuzAc(nIndex);
            });
            
            var npcIndexScript = npc.GetComponent<IlkyardimNPCIndex>();
            if (npcIndexScript == null) {
                npcIndexScript = npc.AddComponent<IlkyardimNPCIndex>();
            }
            npcIndexScript.index = index;
            
            EditorUtility.SetDirty(npc);
            index++;
        }
        
        EditorUtility.SetDirty(ilkyardimSistemi);
        if (canvasObj != null) EditorUtility.SetDirty(canvasObj);
        
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("Sistem, orijinal kodlarına dokunulmadan VR uyumlu şekilde kuruldu.");
    }
}