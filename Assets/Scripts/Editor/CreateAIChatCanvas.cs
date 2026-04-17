#if UNITY_EDITOR
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace AIChatEditor
{
    public class CreateAIChatCanvas : EditorWindow
    {
        private static readonly Color MainPanelColor = AIChatCanvasLayout.MainPanelColor;
        private static readonly Color KeyboardPanelColor = AIChatCanvasLayout.KeyboardPanelColor;
        private static readonly Color KeyboardAccentColor = AIChatCanvasLayout.KeyboardAccentColor;
        private static readonly Color ScrollBackgroundColor = AIChatCanvasLayout.ScrollBackgroundColor;
        private static readonly Color InputBackgroundColor = AIChatCanvasLayout.InputBackgroundColor;
        private static readonly Color AccentColor = AIChatCanvasLayout.AccentColor;
        private static readonly Color SendButtonColor = AIChatCanvasLayout.SendButtonColor;
        private static readonly Color SendHoverColor = AIChatCanvasLayout.SendButtonHoverColor;
        private static readonly Color CloseButtonColor = AIChatCanvasLayout.CloseButtonColor;
        private static readonly Color CloseHoverColor = AIChatCanvasLayout.CloseButtonHoverColor;

        [MenuItem("Tools/Create AI Chat Canvas")]
        public static void ShowWindow()
        {
            GetWindow<CreateAIChatCanvas>("AI Chat Canvas Olustur");
        }

        private void OnGUI()
        {
            GUILayout.Label("VR Uyumlu AI Sohbet Arayuzu Olusturucu", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("AI Chat Canvas Olustur", GUILayout.Height(50)))
            {
                CreateAIChatInterface();
            }
        }

        public static void CreateAIChatInterface()
        {
            GameObject canvasObj = new GameObject("AI_Chat_Canvas", typeof(RectTransform), typeof(Canvas));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = FindPreferredUICamera();
            canvas.additionalShaderChannels =
                AdditionalCanvasShaderChannels.TexCoord1 |
                AdditionalCanvasShaderChannels.Normal |
                AdditionalCanvasShaderChannels.Tangent;

            GraphicRaycaster graphicRaycaster = canvasObj.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster != null)
            {
                graphicRaycaster.enabled = false;
            }

            canvasObj.AddComponent<TrackedDeviceGraphicRaycaster>();
            canvasObj.AddComponent<VRUIClickHelper>();

            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.localScale = AIChatCanvasLayout.PreferredCanvasScale;
            canvasRect.sizeDelta = AIChatCanvasLayout.PreferredCanvasSize;

            RectTransform mainPanelRect;
            Button closeButton;
            Button aiTabButton;
            Button miniTestTabButton;
            GameObject aiChatRoot;
            GameObject miniTestRoot;
            ScrollRect scrollRect;
            TextMeshProUGUI chatHistoryText;
            TMP_InputField inputField;
            Button sendButton;
            TextMeshProUGUI miniTestQuestionText;
            TextMeshProUGUI miniTestProgressText;
            TextMeshProUGUI miniTestResultText;
            Button[] miniTestOptionButtons;
            TextMeshProUGUI[] miniTestOptionLabelTexts;
            Button miniTestNextButton;
            Button miniTestRestartButton;
            CreateMainPanel(
                canvasObj.transform,
                out mainPanelRect,
                out closeButton,
                out aiTabButton,
                out miniTestTabButton,
                out aiChatRoot,
                out miniTestRoot,
                out scrollRect,
                out chatHistoryText,
                out inputField,
                out sendButton,
                out miniTestQuestionText,
                out miniTestProgressText,
                out miniTestResultText,
                out miniTestOptionButtons,
                out miniTestOptionLabelTexts,
                out miniTestNextButton,
                out miniTestRestartButton);

            CanvasGroup keyboardCanvasGroup;
            Button dismissOverlayButton;
            RectTransform keyboardDrawer;
            RectTransform keyboardPanel;
            CreateKeyboardDrawer(mainPanelRect, out keyboardDrawer, out keyboardCanvasGroup, out dismissOverlayButton, out keyboardPanel);

            VRKeyboardManager keyboardManager = canvasObj.AddComponent<VRKeyboardManager>();
            keyboardManager.Configure(inputField, sendButton, keyboardDrawer, keyboardCanvasGroup, dismissOverlayButton, keyboardPanel);
            CreateKeyboardLayout(keyboardPanel, keyboardManager);
            AIChatCanvasLayout.ApplyCanvasLayout(canvasObj);
            keyboardManager = VRKeyboardManager.EnsureKeyboardSetup(canvasObj, inputField, sendButton) ?? keyboardManager;

            EnsureEventSystemExists();

            GameObject aiManagerObj = new GameObject("AIManager");
            AIManager aiManager = aiManagerObj.AddComponent<AIManager>();
            XROrigin xrOrigin = Object.FindObjectOfType<XROrigin>();

            aiManager.aiCanvas = canvasObj;
            aiManager.chatHistoryText = chatHistoryText;
            aiManager.chatScrollRect = scrollRect;
            aiManager.userInputField = inputField;
            aiManager.sendButton = sendButton;
            aiManager.closeButton = closeButton;
            aiManager.aiTabButton = aiTabButton;
            aiManager.miniTestTabButton = miniTestTabButton;
            aiManager.aiChatRoot = aiChatRoot;
            aiManager.miniTestRoot = miniTestRoot;
            aiManager.miniTestQuestionText = miniTestQuestionText;
            aiManager.miniTestProgressText = miniTestProgressText;
            aiManager.miniTestResultText = miniTestResultText;
            aiManager.miniTestOptionButtons = miniTestOptionButtons;
            aiManager.miniTestOptionLabelTexts = miniTestOptionLabelTexts;
            aiManager.miniTestNextButton = miniTestNextButton;
            aiManager.miniTestRestartButton = miniTestRestartButton;

            SerializedObject serializedAI = new SerializedObject(aiManager);
            AssignReference(serializedAI, "xrOrigin", xrOrigin);
            serializedAI.ApplyModifiedPropertiesWithoutUndo();

            canvasObj.SetActive(false);

            Selection.activeGameObject = canvasObj;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("AI Chat Canvas ve premium VR klavye basariyla olusturuldu.");
            Debug.Log("Canvas inaktif baslatildi, root scale 0.002 olarak ayarlandi.");
        }

        private static void CreateMainPanel(
            Transform parent,
            out RectTransform mainPanelRect,
            out Button closeButton,
            out Button aiTabButton,
            out Button miniTestTabButton,
            out GameObject aiChatRoot,
            out GameObject miniTestRoot,
            out ScrollRect scrollRect,
            out TextMeshProUGUI chatHistoryText,
            out TMP_InputField inputField,
            out Button sendButton,
            out TextMeshProUGUI miniTestQuestionText,
            out TextMeshProUGUI miniTestProgressText,
            out TextMeshProUGUI miniTestResultText,
            out Button[] miniTestOptionButtons,
            out TextMeshProUGUI[] miniTestOptionLabelTexts,
            out Button miniTestNextButton,
            out Button miniTestRestartButton)
        {
            GameObject mainPanel = new GameObject("Main_Panel", typeof(RectTransform), typeof(Image), typeof(Outline));
            mainPanel.transform.SetParent(parent, false);

            Image mainPanelImage = mainPanel.GetComponent<Image>();
            mainPanelImage.color = MainPanelColor;

            Outline mainOutline = mainPanel.GetComponent<Outline>();
            mainOutline.effectColor = new Color(0.09f, 0.75f, 0.95f, 0.16f);
            mainOutline.effectDistance = new Vector2(2f, -2f);

            mainPanelRect = mainPanel.GetComponent<RectTransform>();
            mainPanelRect.anchorMin = AIChatCanvasLayout.MainPanelAnchorMin;
            mainPanelRect.anchorMax = AIChatCanvasLayout.MainPanelAnchorMax;
            mainPanelRect.offsetMin = Vector2.zero;
            mainPanelRect.offsetMax = Vector2.zero;

            closeButton = CreateCloseButton(mainPanel.transform);
            CreateTopTabBar(mainPanel.transform, out aiTabButton, out miniTestTabButton);
            aiChatRoot = CreatePanelRoot(mainPanel.transform, AIChatCanvasLayout.AIChatRootName);
            miniTestRoot = CreatePanelRoot(mainPanel.transform, AIChatCanvasLayout.MiniTestRootName);
            miniTestRoot.SetActive(false);

            CreateHeader(aiChatRoot.transform);
            scrollRect = CreateScrollView(aiChatRoot.transform, out chatHistoryText);
            CreateInputArea(aiChatRoot.transform, out inputField, out sendButton);
            CreateMiniTestPanel(
                miniTestRoot.transform,
                out miniTestQuestionText,
                out miniTestProgressText,
                out miniTestResultText,
                out miniTestOptionButtons,
                out miniTestOptionLabelTexts,
                out miniTestNextButton,
                out miniTestRestartButton);
        }

        private static void CreateHeader(Transform parent)
        {
            GameObject headerObj = CreateTextObject(
                parent,
                AIChatCanvasLayout.HeaderName,
                AIChatCanvasLayout.HeaderTitle,
                AIChatCanvasLayout.HeaderFontSize,
                AIChatCanvasLayout.HeaderColor,
                TextAlignmentOptions.Center);
            TextMeshProUGUI headerText = headerObj.GetComponent<TextMeshProUGUI>();
            headerText.fontStyle = FontStyles.Bold;

            RectTransform headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = AIChatCanvasLayout.HeaderAnchorMin;
            headerRect.anchorMax = AIChatCanvasLayout.HeaderAnchorMax;
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;
        }

        private static GameObject CreatePanelRoot(Transform parent, string objectName)
        {
            GameObject panelRoot = new GameObject(objectName, typeof(RectTransform));
            panelRoot.transform.SetParent(parent, false);

            RectTransform rootRect = panelRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = AIChatCanvasLayout.PanelRootAnchorMin;
            rootRect.anchorMax = AIChatCanvasLayout.PanelRootAnchorMax;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.localScale = Vector3.one;
            return panelRoot;
        }

        private static void CreateTopTabBar(Transform parent, out Button aiTabButton, out Button miniTestTabButton)
        {
            GameObject tabBarObject = new GameObject(
                AIChatCanvasLayout.TopTabBarName,
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup));
            tabBarObject.transform.SetParent(parent, false);

            RectTransform tabBarRect = tabBarObject.GetComponent<RectTransform>();
            tabBarRect.anchorMin = AIChatCanvasLayout.TopTabBarAnchorMin;
            tabBarRect.anchorMax = AIChatCanvasLayout.TopTabBarAnchorMax;
            tabBarRect.offsetMin = Vector2.zero;
            tabBarRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup tabLayout = tabBarObject.GetComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 12f;
            tabLayout.padding = new RectOffset(0, 0, 0, 0);
            tabLayout.childAlignment = TextAnchor.MiddleLeft;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = false;
            tabLayout.childForceExpandHeight = true;

            aiTabButton = CreateTabButton(tabBarObject.transform, AIChatCanvasLayout.TabAIButtonName, AIChatCanvasLayout.TabAIButtonTextName, AIChatCanvasLayout.AITabText);
            miniTestTabButton = CreateTabButton(tabBarObject.transform, AIChatCanvasLayout.TabMiniTestButtonName, AIChatCanvasLayout.TabMiniTestButtonTextName, AIChatCanvasLayout.MiniTestTabText);
        }

        private static Button CreateTabButton(Transform parent, string objectName, string labelName, string labelText)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Button), typeof(Outline));
            buttonObject.transform.SetParent(parent, false);

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.minWidth = AIChatCanvasLayout.TabButtonSize.x;
            layout.preferredWidth = AIChatCanvasLayout.TabButtonSize.x;
            layout.minHeight = AIChatCanvasLayout.TabButtonSize.y;
            layout.preferredHeight = AIChatCanvasLayout.TabButtonSize.y;
            layout.flexibleWidth = 0f;

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = AIChatCanvasLayout.TabButtonColor;

            Outline outline = buttonObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.14f, 0.72f, 1f, 0.14f);
            outline.effectDistance = new Vector2(1f, -1f);

            Button button = buttonObject.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = AIChatCanvasLayout.TabButtonColor;
            colors.highlightedColor = AIChatCanvasLayout.TabButtonHoverColor;
            colors.pressedColor = AIChatCanvasLayout.TabButtonActiveColor;
            colors.selectedColor = AIChatCanvasLayout.TabButtonActiveColor;
            colors.disabledColor = new Color(0.24f, 0.28f, 0.34f, 0.55f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            GameObject labelObject = CreateTextObject(
                buttonObject.transform,
                labelName,
                labelText,
                AIChatCanvasLayout.TabButtonFontSize,
                Color.white,
                TextAlignmentOptions.Center);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontStyle = FontStyles.Bold;
            StretchToFill(labelObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            return button;
        }

        private static Button CreateCloseButton(Transform parent)
        {
            GameObject closeButtonObj = new GameObject("Close_Button", typeof(RectTransform), typeof(Image), typeof(Button));
            closeButtonObj.transform.SetParent(parent, false);

            Image closeImage = closeButtonObj.GetComponent<Image>();
            closeImage.color = CloseButtonColor;

            Button closeButton = closeButtonObj.GetComponent<Button>();
            closeButton.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = closeButton.colors;
            colors.normalColor = CloseButtonColor;
            colors.highlightedColor = CloseHoverColor;
            colors.pressedColor = new Color(0.52f, 0.1f, 0.16f, 1f);
            colors.selectedColor = CloseHoverColor;
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            colors.colorMultiplier = 1f;
            closeButton.colors = colors;

            RectTransform closeRect = closeButtonObj.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = AIChatCanvasLayout.CloseButtonSize;
            closeRect.anchoredPosition = AIChatCanvasLayout.CloseButtonPosition;

            GameObject closeTextObj = CreateTextObject(
                closeButtonObj.transform,
                AIChatCanvasLayout.CloseButtonTextName,
                "X",
                AIChatCanvasLayout.CloseButtonFontSize,
                Color.white,
                TextAlignmentOptions.Center);
            TextMeshProUGUI closeText = closeTextObj.GetComponent<TextMeshProUGUI>();
            closeText.fontStyle = FontStyles.Bold;
            StretchToFill(closeTextObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            return closeButton;
        }

        private static ScrollRect CreateScrollView(Transform parent, out TextMeshProUGUI chatHistoryText)
        {
            GameObject scrollViewObj = new GameObject("Chat_ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollViewObj.transform.SetParent(parent, false);

            Image scrollBackground = scrollViewObj.GetComponent<Image>();
            scrollBackground.color = ScrollBackgroundColor;

            RectTransform scrollRectTransform = scrollViewObj.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = AIChatCanvasLayout.ScrollAnchorMin;
            scrollRectTransform.anchorMax = AIChatCanvasLayout.ScrollAnchorMax;
            scrollRectTransform.offsetMin = Vector2.zero;
            scrollRectTransform.offsetMax = Vector2.zero;

            ScrollRect scrollRect = scrollViewObj.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 52f;

            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportObj.transform.SetParent(scrollViewObj.transform, false);
            Image viewportImage = viewportObj.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            viewportImage.raycastTarget = false;
            RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
            StretchToFill(
                viewportRect,
                new Vector2(AIChatCanvasLayout.ChatViewportInsetHorizontal, AIChatCanvasLayout.ChatViewportInsetBottom),
                new Vector2(-AIChatCanvasLayout.ChatViewportInsetHorizontal, -AIChatCanvasLayout.ChatViewportInsetTop));
            scrollRect.viewport = viewportRect;

            GameObject contentObj = new GameObject("Chat_Content", typeof(RectTransform), typeof(Image));
            contentObj.transform.SetParent(viewportObj.transform, false);
            contentObj.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            RectTransform contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, AIChatCanvasLayout.ChatMinimumHeight + AIChatCanvasLayout.ChatBottomPadding);
            contentRect.anchoredPosition = Vector2.zero;
            scrollRect.content = contentRect;

            GameObject historyTextObj = CreateTextObject(
                contentObj.transform,
                AIChatCanvasLayout.ChatHistoryName,
                AIChatCanvasLayout.WelcomeMessage,
                AIChatCanvasLayout.ChatFontSize,
                AIChatCanvasLayout.UserTextColor,
                TextAlignmentOptions.TopLeft);
            chatHistoryText = historyTextObj.GetComponent<TextMeshProUGUI>();
            chatHistoryText.enableWordWrapping = true;
            chatHistoryText.overflowMode = TextOverflowModes.Overflow;
            chatHistoryText.richText = true;
            chatHistoryText.lineSpacing = AIChatCanvasLayout.ChatLineSpacing;
            chatHistoryText.paragraphSpacing = AIChatCanvasLayout.ChatParagraphSpacing;
            chatHistoryText.margin = new Vector4(0f, AIChatCanvasLayout.ChatTextTopMargin, 0f, 0f);

            RectTransform historyRect = historyTextObj.GetComponent<RectTransform>();
            historyRect.anchorMin = new Vector2(0f, 1f);
            historyRect.anchorMax = new Vector2(1f, 1f);
            historyRect.pivot = new Vector2(0.5f, 1f);
            historyRect.anchoredPosition = new Vector2(0f, -AIChatCanvasLayout.ChatTopPadding);
            historyRect.sizeDelta = new Vector2(-AIChatCanvasLayout.ChatSidePadding, AIChatCanvasLayout.ChatMinimumHeight);

            return scrollRect;
        }

        private static void CreateInputArea(Transform parent, out TMP_InputField inputField, out Button sendButton)
        {
            GameObject inputContainerObj = new GameObject("Input_Container", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            inputContainerObj.transform.SetParent(parent, false);

            RectTransform inputContainerRect = inputContainerObj.GetComponent<RectTransform>();
            inputContainerRect.anchorMin = AIChatCanvasLayout.InputAnchorMin;
            inputContainerRect.anchorMax = AIChatCanvasLayout.InputAnchorMax;
            inputContainerRect.offsetMin = Vector2.zero;
            inputContainerRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup layoutGroup = inputContainerObj.GetComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = AIChatCanvasLayout.InputSpacing;
            layoutGroup.padding = new RectOffset(10, 10, 8, 8);
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = true;

            GameObject inputFieldObj = new GameObject("User_InputField", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(TMP_InputField));
            inputFieldObj.transform.SetParent(inputContainerObj.transform, false);

            Image inputImage = inputFieldObj.GetComponent<Image>();
            inputImage.color = InputBackgroundColor;

            LayoutElement inputLayout = inputFieldObj.GetComponent<LayoutElement>();
            inputLayout.flexibleWidth = 1f;
            inputLayout.preferredHeight = AIChatCanvasLayout.InputPreferredHeight;

            inputField = inputFieldObj.GetComponent<TMP_InputField>();
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            inputField.lineLimit = 0;
            inputField.readOnly = false;
            inputField.customCaretColor = true;
            inputField.caretColor = AccentColor;
            inputField.selectionColor = new Color(0.06f, 0.84f, 1f, 0.22f);
            inputField.resetOnDeActivation = true;
            inputField.restoreOriginalTextOnEscape = false;
            inputField.scrollSensitivity = 24f;

            GameObject textAreaObj = new GameObject("Text_Area", typeof(RectTransform));
            textAreaObj.transform.SetParent(inputFieldObj.transform, false);
            RectTransform textAreaRect = textAreaObj.GetComponent<RectTransform>();
            StretchToFill(textAreaRect, new Vector2(16f, 12f), new Vector2(-16f, -12f));

            GameObject placeholderObj = CreateTextObject(
                textAreaObj.transform,
                AIChatCanvasLayout.PlaceholderName,
                AIChatCanvasLayout.PlaceholderText,
                AIChatCanvasLayout.PlaceholderFontSize,
                AIChatCanvasLayout.PlaceholderColor,
                TextAlignmentOptions.TopLeft);
            RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
            StretchToFill(placeholderRect, Vector2.zero, Vector2.zero);

            GameObject inputTextObj = CreateTextObject(
                textAreaObj.transform,
                AIChatCanvasLayout.InputTextName,
                string.Empty,
                AIChatCanvasLayout.InputFontSize,
                Color.white,
                TextAlignmentOptions.TopLeft);
            RectTransform inputTextRect = inputTextObj.GetComponent<RectTransform>();
            StretchToFill(inputTextRect, Vector2.zero, Vector2.zero);

            TextMeshProUGUI placeholderText = placeholderObj.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI inputText = inputTextObj.GetComponent<TextMeshProUGUI>();
            placeholderText.enableAutoSizing = false;
            placeholderText.autoSizeTextContainer = false;
            placeholderText.enableWordWrapping = true;
            placeholderText.overflowMode = TextOverflowModes.Masking;
            placeholderText.margin = Vector4.zero;
            inputText.enableAutoSizing = false;
            inputText.autoSizeTextContainer = false;
            inputText.enableWordWrapping = true;
            inputText.overflowMode = TextOverflowModes.Masking;
            inputText.margin = Vector4.zero;
            inputField.placeholder = placeholderText;
            inputField.textComponent = inputText;
            inputField.textViewport = textAreaRect;

            sendButton = CreateSendButton(inputContainerObj.transform);
        }

        private static Button CreateSendButton(Transform parent)
        {
            GameObject sendButtonObj = new GameObject("Send_Button", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Button));
            sendButtonObj.transform.SetParent(parent, false);

            Image sendImage = sendButtonObj.GetComponent<Image>();
            sendImage.color = SendButtonColor;

            LayoutElement buttonLayout = sendButtonObj.GetComponent<LayoutElement>();
            buttonLayout.preferredWidth = AIChatCanvasLayout.SendButtonPreferredWidth;
            buttonLayout.preferredHeight = AIChatCanvasLayout.SendButtonPreferredHeight;
            buttonLayout.flexibleWidth = 0f;

            Button sendButton = sendButtonObj.GetComponent<Button>();
            sendButton.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = sendButton.colors;
            colors.normalColor = SendButtonColor;
            colors.highlightedColor = SendHoverColor;
            colors.pressedColor = new Color(0.03f, 0.42f, 0.64f, 1f);
            colors.selectedColor = SendHoverColor;
            colors.disabledColor = new Color(0.28f, 0.32f, 0.36f, 0.6f);
            colors.colorMultiplier = 1f;
            sendButton.colors = colors;

            GameObject buttonTextObj = CreateTextObject(
                sendButtonObj.transform,
                AIChatCanvasLayout.SendButtonTextName,
                AIChatCanvasLayout.SendButtonText,
                AIChatCanvasLayout.SendButtonFontSize,
                Color.white,
                TextAlignmentOptions.Center);
            TextMeshProUGUI sendText = buttonTextObj.GetComponent<TextMeshProUGUI>();
            sendText.fontStyle = FontStyles.Bold;
            StretchToFill(buttonTextObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            return sendButton;
        }

        private static void CreateMiniTestPanel(
            Transform parent,
            out TextMeshProUGUI miniTestQuestionText,
            out TextMeshProUGUI miniTestProgressText,
            out TextMeshProUGUI miniTestResultText,
            out Button[] miniTestOptionButtons,
            out TextMeshProUGUI[] miniTestOptionLabelTexts,
            out Button miniTestNextButton,
            out Button miniTestRestartButton)
        {
            GameObject headerObject = new GameObject(
                AIChatCanvasLayout.MiniTestHeaderName,
                typeof(RectTransform),
                typeof(Image),
                typeof(HorizontalLayoutGroup),
                typeof(Outline));
            headerObject.transform.SetParent(parent, false);

            RectTransform headerRect = headerObject.GetComponent<RectTransform>();
            headerRect.anchorMin = AIChatCanvasLayout.MiniTestHeaderAnchorMin;
            headerRect.anchorMax = AIChatCanvasLayout.MiniTestHeaderAnchorMax;
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;

            Image headerImage = headerObject.GetComponent<Image>();
            headerImage.color = AIChatCanvasLayout.MiniTestCardColor;

            Outline headerOutline = headerObject.GetComponent<Outline>();
            headerOutline.effectColor = new Color(0.14f, 0.72f, 1f, 0.14f);
            headerOutline.effectDistance = new Vector2(2f, -2f);

            HorizontalLayoutGroup headerLayout = headerObject.GetComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(14, 14, 10, 10);
            headerLayout.spacing = 12f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            GameObject titleObject = CreateTextObject(
                headerObject.transform,
                AIChatCanvasLayout.MiniTestTitleName,
                AIChatCanvasLayout.MiniTestTitleText,
                AIChatCanvasLayout.MiniTestTitleFontSize,
                AIChatCanvasLayout.MiniTestTextColor,
                TextAlignmentOptions.Left);
            TextMeshProUGUI titleText = titleObject.GetComponent<TextMeshProUGUI>();
            titleText.fontStyle = FontStyles.Bold;
            LayoutElement titleLayout = titleObject.AddComponent<LayoutElement>();
            titleLayout.flexibleWidth = 1f;

            GameObject progressObject = CreateTextObject(
                headerObject.transform,
                AIChatCanvasLayout.MiniTestProgressName,
                "Soru 1/5",
                AIChatCanvasLayout.MiniTestProgressFontSize,
                AIChatCanvasLayout.MiniTestMutedTextColor,
                TextAlignmentOptions.Right);
            miniTestProgressText = progressObject.GetComponent<TextMeshProUGUI>();
            miniTestProgressText.fontStyle = FontStyles.Bold;
            LayoutElement progressLayout = progressObject.AddComponent<LayoutElement>();
            progressLayout.minWidth = 150f;
            progressLayout.preferredWidth = 150f;

            GameObject questionCardObject = new GameObject(
                AIChatCanvasLayout.MiniTestQuestionCardName,
                typeof(RectTransform),
                typeof(Image),
                typeof(Outline));
            questionCardObject.transform.SetParent(parent, false);
            RectTransform questionCardRect = questionCardObject.GetComponent<RectTransform>();
            questionCardRect.anchorMin = AIChatCanvasLayout.MiniTestQuestionAnchorMin;
            questionCardRect.anchorMax = AIChatCanvasLayout.MiniTestQuestionAnchorMax;
            questionCardRect.offsetMin = Vector2.zero;
            questionCardRect.offsetMax = Vector2.zero;
            questionCardRect.anchoredPosition = Vector2.zero;

            Image questionCardImage = questionCardObject.GetComponent<Image>();
            questionCardImage.color = AIChatCanvasLayout.MiniTestCardColor;

            Outline questionOutline = questionCardObject.GetComponent<Outline>();
            questionOutline.effectColor = new Color(0.14f, 0.72f, 1f, 0.14f);
            questionOutline.effectDistance = new Vector2(2f, -2f);

            GameObject questionObject = CreateTextObject(
                questionCardObject.transform,
                AIChatCanvasLayout.MiniTestQuestionTextName,
                "Mini test sekmesine geçtiğinde soru burada görünecek.",
                AIChatCanvasLayout.MiniTestQuestionFontSize,
                AIChatCanvasLayout.MiniTestTextColor,
                TextAlignmentOptions.TopLeft);
            miniTestQuestionText = questionObject.GetComponent<TextMeshProUGUI>();
            miniTestQuestionText.fontStyle = FontStyles.Bold;
            RectTransform questionRect = questionObject.GetComponent<RectTransform>();
            StretchToFill(questionRect, Vector2.zero, Vector2.zero);
            miniTestQuestionText.margin = new Vector4(14f, 12f, 14f, 8f);
            miniTestQuestionText.enableWordWrapping = true;

            GameObject optionsObject = new GameObject(
                AIChatCanvasLayout.MiniTestOptionsName,
                typeof(RectTransform),
                typeof(VerticalLayoutGroup));
            optionsObject.transform.SetParent(parent, false);
            RectTransform optionsRect = optionsObject.GetComponent<RectTransform>();
            optionsRect.anchorMin = AIChatCanvasLayout.MiniTestOptionsAnchorMin;
            optionsRect.anchorMax = AIChatCanvasLayout.MiniTestOptionsAnchorMax;
            optionsRect.offsetMin = Vector2.zero;
            optionsRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup optionsLayout = optionsObject.GetComponent<VerticalLayoutGroup>();
            optionsLayout.padding = new RectOffset(0, 0, 0, 0);
            optionsLayout.spacing = 10f;
            optionsLayout.childAlignment = TextAnchor.UpperCenter;
            optionsLayout.childControlWidth = true;
            optionsLayout.childControlHeight = true;
            optionsLayout.childForceExpandWidth = true;
            optionsLayout.childForceExpandHeight = false;

            miniTestOptionButtons = new Button[3];
            miniTestOptionLabelTexts = new TextMeshProUGUI[3];
            miniTestOptionButtons[0] = CreateMiniTestOptionButton(optionsObject.transform, AIChatCanvasLayout.OptionAButtonName, AIChatCanvasLayout.OptionALabelName, "Seçenek A", out miniTestOptionLabelTexts[0]);
            miniTestOptionButtons[1] = CreateMiniTestOptionButton(optionsObject.transform, AIChatCanvasLayout.OptionBButtonName, AIChatCanvasLayout.OptionBLabelName, "Seçenek B", out miniTestOptionLabelTexts[1]);
            miniTestOptionButtons[2] = CreateMiniTestOptionButton(optionsObject.transform, AIChatCanvasLayout.OptionCButtonName, AIChatCanvasLayout.OptionCLabelName, "Seçenek C", out miniTestOptionLabelTexts[2]);

            GameObject footerObject = new GameObject(
                AIChatCanvasLayout.MiniTestFooterName,
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup));
            footerObject.transform.SetParent(parent, false);
            RectTransform footerRect = footerObject.GetComponent<RectTransform>();
            footerRect.anchorMin = AIChatCanvasLayout.MiniTestFooterAnchorMin;
            footerRect.anchorMax = AIChatCanvasLayout.MiniTestFooterAnchorMax;
            footerRect.offsetMin = Vector2.zero;
            footerRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup footerLayout = footerObject.GetComponent<HorizontalLayoutGroup>();
            footerLayout.padding = new RectOffset(0, 0, 0, 0);
            footerLayout.spacing = 14f;
            footerLayout.childAlignment = TextAnchor.MiddleCenter;
            footerLayout.childControlWidth = true;
            footerLayout.childControlHeight = true;
            footerLayout.childForceExpandWidth = false;
            footerLayout.childForceExpandHeight = false;

            miniTestNextButton = CreateFooterButton(
                footerObject.transform,
                AIChatCanvasLayout.MiniTestNextButtonName,
                AIChatCanvasLayout.MiniTestNextButtonTextName,
                AIChatCanvasLayout.MiniTestNextButtonText,
                SendButtonColor);
            miniTestRestartButton = CreateFooterButton(
                footerObject.transform,
                AIChatCanvasLayout.MiniTestRestartButtonName,
                AIChatCanvasLayout.MiniTestRestartButtonTextName,
                AIChatCanvasLayout.MiniTestRestartButtonText,
                AIChatCanvasLayout.TabButtonColor);
            miniTestRestartButton.gameObject.SetActive(false);

            GameObject resultObject = CreateTextObject(
                parent,
                AIChatCanvasLayout.MiniTestResultTextName,
                "Sorular açıldığında seçiminin geri bildirimi burada görünecek.",
                AIChatCanvasLayout.MiniTestResultFontSize,
                AIChatCanvasLayout.MiniTestMutedTextColor,
                TextAlignmentOptions.TopLeft);
            miniTestResultText = resultObject.GetComponent<TextMeshProUGUI>();
            RectTransform resultRect = resultObject.GetComponent<RectTransform>();
            resultRect.anchorMin = AIChatCanvasLayout.MiniTestResultAnchorMin;
            resultRect.anchorMax = AIChatCanvasLayout.MiniTestResultAnchorMax;
            resultRect.offsetMin = Vector2.zero;
            resultRect.offsetMax = Vector2.zero;
            resultRect.anchoredPosition = Vector2.zero;
            miniTestResultText.enableWordWrapping = true;
        }

        private static Button CreateMiniTestOptionButton(
            Transform parent,
            string objectName,
            string labelName,
            string labelText,
            out TextMeshProUGUI label)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Button), typeof(Outline));
            buttonObject.transform.SetParent(parent, false);

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredHeight = AIChatCanvasLayout.MiniTestOptionSize.y;
            layout.minHeight = AIChatCanvasLayout.MiniTestOptionSize.y;
            layout.flexibleWidth = 1f;

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = AIChatCanvasLayout.MiniTestOptionColor;

            Outline outline = buttonObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.14f, 0.72f, 1f, 0.12f);
            outline.effectDistance = new Vector2(2f, -2f);

            Button button = buttonObject.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = AIChatCanvasLayout.MiniTestOptionColor;
            colors.highlightedColor = AIChatCanvasLayout.TabButtonHoverColor;
            colors.pressedColor = AIChatCanvasLayout.MiniTestOptionSelectedColor;
            colors.selectedColor = AIChatCanvasLayout.MiniTestOptionSelectedColor;
            colors.disabledColor = AIChatCanvasLayout.MiniTestOptionColor;
            colors.colorMultiplier = 1f;
            button.colors = colors;

            GameObject labelObject = CreateTextObject(
                buttonObject.transform,
                labelName,
                labelText,
                AIChatCanvasLayout.MiniTestOptionFontSize,
                AIChatCanvasLayout.MiniTestTextColor,
                TextAlignmentOptions.MidlineLeft);
            label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontStyle = FontStyles.Bold;
            label.enableWordWrapping = true;
            label.margin = new Vector4(18f, 0f, 18f, 0f);
            StretchToFill(labelObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            return button;
        }

        private static Button CreateFooterButton(
            Transform parent,
            string objectName,
            string labelName,
            string labelText,
            Color buttonColor)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Button), typeof(Outline));
            buttonObject.transform.SetParent(parent, false);

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.minWidth = AIChatCanvasLayout.MiniTestFooterButtonSize.x;
            layout.preferredWidth = AIChatCanvasLayout.MiniTestFooterButtonSize.x;
            layout.minHeight = AIChatCanvasLayout.MiniTestFooterButtonSize.y;
            layout.preferredHeight = AIChatCanvasLayout.MiniTestFooterButtonSize.y;

            Image image = buttonObject.GetComponent<Image>();
            image.color = buttonColor;

            Outline outline = buttonObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.14f, 0.72f, 1f, 0.14f);
            outline.effectDistance = new Vector2(1f, -1f);

            Button button = buttonObject.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = AIChatCanvasLayout.TabButtonHoverColor;
            colors.pressedColor = AIChatCanvasLayout.TabButtonActiveColor;
            colors.selectedColor = AIChatCanvasLayout.TabButtonActiveColor;
            colors.disabledColor = new Color(0.26f, 0.3f, 0.35f, 0.55f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            GameObject labelObject = CreateTextObject(
                buttonObject.transform,
                labelName,
                labelText,
                AIChatCanvasLayout.MiniTestFooterButtonFontSize,
                Color.white,
                TextAlignmentOptions.Center);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontStyle = FontStyles.Bold;
            StretchToFill(labelObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            return button;
        }

        private static void CreateKeyboardDrawer(
            Transform parent,
            out RectTransform keyboardDrawer,
            out CanvasGroup keyboardCanvasGroup,
            out Button dismissOverlayButton,
            out RectTransform keyboardPanel)
        {
            GameObject drawerObj = new GameObject("Keyboard_Drawer", typeof(RectTransform), typeof(CanvasGroup));
            drawerObj.transform.SetParent(parent, false);

            keyboardDrawer = drawerObj.GetComponent<RectTransform>();
            StretchToFill(keyboardDrawer, Vector2.zero, Vector2.zero);
            keyboardDrawer.SetAsLastSibling();

            keyboardCanvasGroup = drawerObj.GetComponent<CanvasGroup>();
            keyboardCanvasGroup.alpha = 0f;
            keyboardCanvasGroup.interactable = false;
            keyboardCanvasGroup.blocksRaycasts = false;

            GameObject overlayObj = new GameObject("Dismiss_Overlay", typeof(RectTransform), typeof(Image), typeof(Button));
            overlayObj.transform.SetParent(drawerObj.transform, false);
            RectTransform overlayRect = overlayObj.GetComponent<RectTransform>();
            overlayRect.anchorMin = AIChatCanvasLayout.DismissOverlayAnchorMin;
            overlayRect.anchorMax = AIChatCanvasLayout.DismissOverlayAnchorMax;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlayRect.anchoredPosition = Vector2.zero;
            overlayRect.localScale = Vector3.one;
            overlayRect.SetAsFirstSibling();

            Image overlayImage = overlayObj.GetComponent<Image>();
            overlayImage.color = new Color(0.01f, 0.04f, 0.08f, 0.015f);

            dismissOverlayButton = overlayObj.GetComponent<Button>();
            dismissOverlayButton.transition = Selectable.Transition.None;

            GameObject keyboardPanelObj = new GameObject("Keyboard_Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(Outline));
            keyboardPanelObj.transform.SetParent(drawerObj.transform, false);

            keyboardPanel = keyboardPanelObj.GetComponent<RectTransform>();
            keyboardPanel.anchorMin = AIChatCanvasLayout.KeyboardPanelAnchorMin;
            keyboardPanel.anchorMax = AIChatCanvasLayout.KeyboardPanelAnchorMax;
            keyboardPanel.offsetMin = Vector2.zero;
            keyboardPanel.offsetMax = Vector2.zero;
            keyboardPanel.anchoredPosition = Vector2.zero;
            keyboardPanel.SetAsLastSibling();

            Image keyboardPanelImage = keyboardPanelObj.GetComponent<Image>();
            keyboardPanelImage.color = KeyboardPanelColor;

            Outline keyboardOutline = keyboardPanelObj.GetComponent<Outline>();
            keyboardOutline.effectColor = new Color(0.08f, 0.78f, 1f, 0.14f);
            keyboardOutline.effectDistance = new Vector2(1f, -1f);

            VerticalLayoutGroup verticalLayout = keyboardPanelObj.GetComponent<VerticalLayoutGroup>();
            verticalLayout.padding = AIChatCanvasLayout.GetKeyboardPanelPadding();
            verticalLayout.spacing = AIChatCanvasLayout.KeyboardPanelSpacing;
            verticalLayout.childAlignment = TextAnchor.MiddleCenter;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = true;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;

            CreateKeyboardAccent(keyboardPanelObj.transform);
        }

        private static void CreateKeyboardLayout(RectTransform keyboardPanel, VRKeyboardManager keyboardManager)
        {
            CreateCharacterRow(keyboardPanel, "Row_0_Numbers", keyboardManager, "1234567890", 1f);
            CreateCharacterRow(keyboardPanel, "Row_1_Qwerty", keyboardManager, "QWERTYUIOP\u011E\u00DC", 1f);
            CreateCharacterRow(keyboardPanel, "Row_2_Asdf", keyboardManager, "ASDFGHJKL\u015e\u0130", 1f);
            CreateCharacterRow(keyboardPanel, "Row_3_Zxcv", keyboardManager, "ZXCVBNM\u00d6\u00c7", 1f);

            RectTransform controlsRow = CreateKeyboardRow(keyboardPanel, "Row_4_Controls");
            CreateKey(controlsRow, "Close_Key", keyboardManager, VRKeyType.Close, string.Empty, "Kapat", 1.65f);
            CreateKey(controlsRow, "Space_Key", keyboardManager, VRKeyType.Space, " ", "Bo\u015fluk", 5.65f);
            CreateKey(controlsRow, "Backspace_Key", keyboardManager, VRKeyType.Backspace, string.Empty, "Sil", 1.65f);
            CreateKey(controlsRow, "Enter_Key", keyboardManager, VRKeyType.Enter, string.Empty, "Enter", 1.85f);
        }

        private static void CreateCharacterRow(Transform parent, string rowName, VRKeyboardManager keyboardManager, string characters, float widthWeight)
        {
            RectTransform row = CreateKeyboardRow(parent, rowName);
            CreateCharacterKeys(row, keyboardManager, characters, widthWeight);
        }

        private static void CreateCharacterKeys(Transform row, VRKeyboardManager keyboardManager, string characters, float widthWeight)
        {
            foreach (char character in characters)
            {
                string value = character.ToString();
                CreateKey(row, value + "_Key", keyboardManager, VRKeyType.Character, value, value, widthWeight);
            }
        }

        private static RectTransform CreateKeyboardRow(Transform parent, string rowName)
        {
            GameObject rowObj = new GameObject(rowName, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            rowObj.transform.SetParent(parent, false);

            HorizontalLayoutGroup rowLayout = rowObj.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = AIChatCanvasLayout.KeyboardPanelSpacing;
            rowLayout.padding = ResolveRowPadding(rowName);
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            LayoutElement layoutElement = rowObj.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = AIChatCanvasLayout.KeyboardRowHeight;
            layoutElement.flexibleHeight = 1f;
            layoutElement.flexibleWidth = 1f;

            return rowObj.GetComponent<RectTransform>();
        }

        private static void CreateKey(
            Transform parent,
            string objectName,
            VRKeyboardManager keyboardManager,
            VRKeyType keyType,
            string value,
            string labelText,
            float widthWeight)
        {
            GameObject keyObj = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(Outline), typeof(VRKey));
            keyObj.transform.SetParent(parent, false);

            Image keyImage = keyObj.GetComponent<Image>();
            keyImage.color = ResolveKeyColor(keyType);

            Outline keyOutline = keyObj.GetComponent<Outline>();
            keyOutline.effectColor = AIChatCanvasLayout.GetKeyboardKeyOutlineColor(keyType);
            keyOutline.effectDistance = new Vector2(1f, -1f);

            Button keyButton = keyObj.GetComponent<Button>();
            keyButton.transition = Selectable.Transition.None;

            LayoutElement layoutElement = keyObj.GetComponent<LayoutElement>();
            layoutElement.flexibleWidth = 0f;
            layoutElement.preferredWidth = AIChatCanvasLayout.GetKeyboardKeyWidth(widthWeight);
            layoutElement.preferredHeight = AIChatCanvasLayout.KeyboardKeyHeight;
            layoutElement.minWidth = AIChatCanvasLayout.KeyboardKeyMinWidth;

            GameObject labelObj = CreateTextObject(
                keyObj.transform,
                "Label",
                labelText,
                AIChatCanvasLayout.GetKeyboardLabelFontSize(keyType),
                Color.white,
                TextAlignmentOptions.Center);
            TextMeshProUGUI label = labelObj.GetComponent<TextMeshProUGUI>();
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = false;
            label.color = AIChatCanvasLayout.GetKeyboardLabelColor(keyType);
            StretchToFill(
                labelObj.GetComponent<RectTransform>(),
                new Vector2(AIChatCanvasLayout.KeyboardLabelPaddingHorizontal, AIChatCanvasLayout.KeyboardLabelPaddingVertical),
                new Vector2(-AIChatCanvasLayout.KeyboardLabelPaddingHorizontal, -AIChatCanvasLayout.KeyboardLabelPaddingVertical));

            VRKey vrKey = keyObj.GetComponent<VRKey>();
            vrKey.Configure(keyboardManager, keyType, value, labelText);
        }

        private static Color ResolveKeyColor(VRKeyType keyType)
        {
            return AIChatCanvasLayout.GetKeyboardKeyColor(keyType);
        }

        private static void CreateKeyboardAccent(Transform panelRoot)
        {
            GameObject accentObj = new GameObject(AIChatCanvasLayout.KeyboardAccentName, typeof(RectTransform), typeof(Image));
            accentObj.transform.SetParent(panelRoot, false);

            RectTransform accentRect = accentObj.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.sizeDelta = new Vector2(0f, AIChatCanvasLayout.KeyboardAccentHeight);
            accentRect.anchoredPosition = new Vector2(0f, -8f);
            accentRect.SetAsFirstSibling();

            Image accentImage = accentObj.GetComponent<Image>();
            accentImage.color = KeyboardAccentColor;
            accentImage.raycastTarget = false;
        }

        private static RectOffset ResolveRowPadding(string rowName)
        {
            return AIChatCanvasLayout.GetKeyboardRowPadding(rowName);
        }

        private static GameObject CreateTextObject(
            Transform parent,
            string name,
            string text,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(parent, false);

            TextMeshProUGUI tmpText = textObj.GetComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = fontSize;
            tmpText.color = color;
            tmpText.alignment = alignment;
            tmpText.raycastTarget = false;

            return textObj;
        }

        private static void StretchToFill(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        private static void AssignReference(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void EnsureEventSystemExists()
        {
            EventSystem eventSystem = Object.FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystem = eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<XRUIInputModule>();
                return;
            }

            StandaloneInputModule standaloneInput = eventSystem.GetComponent<StandaloneInputModule>();
            if (standaloneInput != null)
            {
                standaloneInput.enabled = false;
            }

            if (eventSystem.GetComponent<XRUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<XRUIInputModule>();
            }
        }

        private static Camera FindPreferredUICamera()
        {
            XROrigin xrOrigin = Object.FindObjectOfType<XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                return xrOrigin.Camera;
            }

            if (Camera.main != null)
            {
                return Camera.main;
            }

            return Object.FindObjectOfType<Camera>();
        }
    }
}
#endif
