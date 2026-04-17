#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace TriyajModul3
{
    public static class OnayMenusuCreator
    {
        [MenuItem("VR Tools/Onay Menusu Olustur %g")]
        public static void CreateOnayMenusu()
        {
            // Canvas
            GameObject canvasObj = new GameObject("OnayMenusuCanvas");
            Undo.RegisterCreatedObjectUndo(canvasObj, "Canvas Olustur");
            
            // UI Layer'a ata (Layer 5)
            canvasObj.layer = LayerMask.NameToLayer("UI");
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
            
            canvasObj.AddComponent<CanvasScaler>();
            
            // !! KRİTİK DEĞİŞİKLİK: GraphicRaycaster yerine TrackedDeviceGraphicRaycaster kullan !!
            // Standart GraphicRaycaster VR controller'ları algılamaz!
            canvasObj.AddComponent<TrackedDeviceGraphicRaycaster>();
            
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(400, 260);
            canvasRect.localPosition = new Vector3(0, 1.5f, 2.0f);
            canvasRect.localScale = Vector3.one * 0.002f; // World Space için uygun ölçek

            // Background Panel
            GameObject mainBg = new GameObject("MainBackground");
            mainBg.transform.SetParent(canvasObj.transform);
            Undo.RegisterCreatedObjectUndo(mainBg, "MainBg Olustur");
            
            Image mainBgImage = mainBg.AddComponent<Image>();
            mainBgImage.color = new Color(0.06f, 0.06f, 0.1f, 0.98f);
            
            RectTransform mainBgRect = mainBg.GetComponent<RectTransform>();
            mainBgRect.anchorMin = Vector2.zero;
            mainBgRect.anchorMax = Vector2.one;
            mainBgRect.offsetMin = Vector2.zero;
            mainBgRect.offsetMax = Vector2.zero;
            mainBgRect.localPosition = Vector3.zero;
            mainBgRect.localScale = Vector3.one;

            // Header Bar
            GameObject headerBar = new GameObject("HeaderBar");
            headerBar.transform.SetParent(mainBg.transform);
            Undo.RegisterCreatedObjectUndo(headerBar, "HeaderBar Olustur");
            
            Image headerImage = headerBar.AddComponent<Image>();
            headerImage.color = new Color(0.12f, 0.35f, 0.85f, 1f);
            
            RectTransform headerRect = headerBar.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 0.82f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;
            headerRect.localPosition = Vector3.zero;
            headerRect.localScale = Vector3.one;

            // Header Title Text
            GameObject headerTitle = new GameObject("HeaderTitle");
            headerTitle.transform.SetParent(headerBar.transform);
            Undo.RegisterCreatedObjectUndo(headerTitle, "HeaderTitle Olustur");
            
            TextMeshProUGUI headerTitleText = headerTitle.AddComponent<TextMeshProUGUI>();
            headerTitleText.text = "BILGI";
            headerTitleText.fontSize = 20;
            headerTitleText.fontStyle = FontStyles.Bold;
            headerTitleText.color = Color.white;
            headerTitleText.alignment = TextAlignmentOptions.Left;
            
            RectTransform headerTitleRect = headerTitle.GetComponent<RectTransform>();
            headerTitleRect.anchorMin = new Vector2(0f, 0.2f);
            headerTitleRect.anchorMax = new Vector2(1f, 0.8f);
            headerTitleRect.offsetMin = new Vector2(15, 0);
            headerTitleRect.offsetMax = new Vector2(-10, 0);
            headerTitleRect.localPosition = Vector3.zero;
            headerTitleRect.localScale = Vector3.one;

            // Content Area Container
            GameObject contentArea = new GameObject("ContentArea");
            contentArea.transform.SetParent(mainBg.transform);
            Undo.RegisterCreatedObjectUndo(contentArea, "ContentArea Olustur");
            
            RectTransform contentRect = contentArea.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0.35f);
            contentRect.anchorMax = new Vector2(1f, 0.78f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            contentRect.localPosition = Vector3.zero;
            contentRect.localScale = Vector3.one;

            // Main Message Text
            GameObject messageTextObj = new GameObject("MainMessage");
            messageTextObj.transform.SetParent(contentArea.transform);
            Undo.RegisterCreatedObjectUndo(messageTextObj, "Message Olustur");
            
            TextMeshProUGUI messageText = messageTextObj.AddComponent<TextMeshProUGUI>();
            messageText.text = "Hastaneye girmek istiyor musunuz?";
            messageText.fontSize = 22;
            messageText.fontStyle = FontStyles.Bold;
            messageText.color = new Color(0.95f, 0.95f, 1f, 1f);
            messageText.alignment = TextAlignmentOptions.Center;
            
            RectTransform messageRect = messageTextObj.GetComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0.5f, 0.6f);
            messageRect.anchorMax = new Vector2(0.5f, 0.6f);
            messageRect.pivot = new Vector2(0.5f, 0.5f);
            messageRect.sizeDelta = new Vector2(350, 35);
            messageRect.localPosition = Vector3.zero;
            messageRect.localScale = Vector3.one;

            // Subtitle Text
            GameObject subtitleObj = new GameObject("SubtitleText");
            subtitleObj.transform.SetParent(contentArea.transform);
            Undo.RegisterCreatedObjectUndo(subtitleObj, "Subtitle Olustur");
            
            TextMeshProUGUI subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
            subtitleText.text = "Egitim alanindan ayrilip korku hastanesine gecis yapilacak";
            subtitleText.fontSize = 14;
            subtitleText.fontStyle = FontStyles.Normal;
            subtitleText.color = new Color(0.65f, 0.65f, 0.75f, 1f);
            subtitleText.alignment = TextAlignmentOptions.Center;
            
            RectTransform subtitleRect = subtitleObj.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.5f, 0.2f);
            subtitleRect.anchorMax = new Vector2(0.5f, 0.2f);
            subtitleRect.pivot = new Vector2(0.5f, 0.5f);
            subtitleRect.sizeDelta = new Vector2(350, 25);
            subtitleRect.localPosition = Vector3.zero;
            subtitleRect.localScale = Vector3.one;

            // Button Container
            GameObject buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(mainBg.transform);
            Undo.RegisterCreatedObjectUndo(buttonContainer, "ButtonContainer Olustur");
            
            RectTransform btnContainerRect = buttonContainer.AddComponent<RectTransform>();
            btnContainerRect.anchorMin = new Vector2(0.05f, 0.08f);
            btnContainerRect.anchorMax = new Vector2(0.95f, 0.32f);
            btnContainerRect.offsetMin = Vector2.zero;
            btnContainerRect.offsetMax = Vector2.zero;
            btnContainerRect.localPosition = Vector3.zero;
            btnContainerRect.localScale = Vector3.one;

            // EVET Button
            GameObject evetBtn = CreateButton(buttonContainer, "EvetButton", new Vector2(0.35f, 0.5f),
                new Color(0.12f, 0.7f, 0.3f, 1f), new Color(0.18f, 0.85f, 0.4f, 1f), "EVET");

            // HAYIR Button
            GameObject hayirBtn = CreateButton(buttonContainer, "HayirButton", new Vector2(0.65f, 0.5f),
                new Color(0.7f, 0.12f, 0.12f, 1f), new Color(0.85f, 0.18f, 0.18f, 1f), "HAYIR");

            // Manager Script
            OnayMenusuManager manager = canvasObj.AddComponent<OnayMenusuManager>();
            
            // Tüm child objeleri UI layer'a ata (kritik!)
            SetLayerRecursively(canvasObj, LayerMask.NameToLayer("UI"));

            Selection.activeGameObject = canvasObj;
            EditorUtility.DisplayDialog("VR Onay Menusu",
                "✅ Onay menusu VR uyumlu olarak olusturuldu!\n\n" +
                "TrackedDeviceGraphicRaycaster eklendi.\n" +
                "Tum objeler UI layer'ina atandi.\n\n" +
                "YAPMANIZ GEREKENLER:\n" +
                "1- HedefNokta'ya isinlanma Transform'u ata\n" +
                "2- XR Ray Interactor'larin Raycast Mask'inda 'UI' layer'inin secili oldugunu dogrula\n" +
                "3- EventSystem'de XR UI Input Module oldugunu kontrol et\n" +
                "4- EntranceNPCInteractable'a bu Canvas'i bagla", "Tamam");
        }
        
        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static GameObject CreateButton(GameObject parent, string name, Vector2 anchorPos, Color normalColor, Color hoverColor, string buttonText)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent.transform);
            Undo.RegisterCreatedObjectUndo(btnObj, name + " Olustur");

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = normalColor;
            btnImage.raycastTarget = true; // Buton tıklanabilir olmalı

            Button btnComp = btnObj.AddComponent<Button>();
            ColorBlock colors = btnComp.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = hoverColor;
            colors.pressedColor = new Color(normalColor.r - 0.15f, normalColor.g - 0.15f, normalColor.b - 0.15f, 1f);
            colors.selectedColor = hoverColor;
            colors.colorMultiplier = 1f;
            btnComp.colors = colors;
            btnComp.targetGraphic = btnImage;

            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = anchorPos;
            btnRect.anchorMax = anchorPos;
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = new Vector2(110, 45);
            btnRect.localPosition = Vector3.zero;
            btnRect.localScale = Vector3.one;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform);
            Undo.RegisterCreatedObjectUndo(textObj, "ButtonText Olustur");

            TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
            btnText.text = buttonText;
            btnText.fontSize = 20;
            btnText.fontStyle = FontStyles.Bold;
            btnText.color = Color.white;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.characterSpacing = 4;
            btnText.raycastTarget = false; // KRİTİK: Text tıklamayı engellemez!

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.localPosition = Vector3.zero;
            textRect.localScale = Vector3.one;

            return btnObj;
        }
    }
}
#endif
