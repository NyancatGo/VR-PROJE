using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Doktor AI sohbet paneli icin ortak layout ve stil degerlerini tutar.
/// Editor olusturucusu, runtime onarimi ve sahne batch duzeltmeleri ayni kaynagi kullanir.
/// </summary>
public static class AIChatCanvasLayout
{
    public const string CanvasRootName = "AI_Chat_Canvas";
    public const string MainPanelName = "Main_Panel";
    public const string HeaderName = "Header_Text";
    public const string CloseButtonName = "Close_Button";
    public const string CloseButtonTextName = "Close_ButtonText";
    public const string TopTabBarName = "Top_TabBar";
    public const string TabAIButtonName = "Tab_AI_Button";
    public const string TabAIButtonTextName = "Tab_AI_Label";
    public const string TabMiniTestButtonName = "Tab_MiniTest_Button";
    public const string TabMiniTestButtonTextName = "Tab_MiniTest_Label";
    public const string AIChatRootName = "AIChatRoot";
    public const string MiniTestRootName = "MiniTestRoot";
    public const string ScrollViewName = "Chat_ScrollView";
    public const string ViewportName = "Viewport";
    public const string ContentName = "Chat_Content";
    public const string ChatHistoryName = "ChatHistory_Text";
    public const string InputContainerName = "Input_Container";
    public const string InputFieldName = "User_InputField";
    public const string TextAreaName = "Text_Area";
    public const string PlaceholderName = "Placeholder";
    public const string InputTextName = "InputText";
    public const string SendButtonName = "Send_Button";
    public const string SendButtonTextName = "ButtonText";
    public const string KeyboardDrawerName = "Keyboard_Drawer";
    public const string DismissOverlayName = "Dismiss_Overlay";
    public const string KeyboardPanelName = "Keyboard_Panel";
    public const string KeyboardAccentName = "Top_Glow";
    public const string MiniTestHeaderName = "MiniTest_Header";
    public const string MiniTestTitleName = "MiniTest_Title";
    public const string MiniTestProgressName = "MiniTest_Progress";
    public const string MiniTestTimerName = "MiniTest_Timer";
    public const string MiniTestTimerDefaultText = "Sure: 00:00";
    public const string MiniTestQuestionCardName = "MiniTest_QuestionCard";
    public const string MiniTestQuestionTextName = "MiniTest_QuestionText";
    public const string MiniTestOptionsName = "MiniTest_Options";
    public const string OptionAButtonName = "OptionA_Button";
    public const string OptionBButtonName = "OptionB_Button";
    public const string OptionCButtonName = "OptionC_Button";
    public const string OptionALabelName = "OptionA_Label";
    public const string OptionBLabelName = "OptionB_Label";
    public const string OptionCLabelName = "OptionC_Label";
    public const string MiniTestFooterName = "MiniTest_Footer";
    public const string MiniTestNextButtonName = "MiniTest_NextButton";
    public const string MiniTestNextButtonTextName = "MiniTest_Next_Label";
    public const string MiniTestRestartButtonName = "MiniTest_RestartButton";
    public const string MiniTestRestartButtonTextName = "MiniTest_Restart_Label";
    public const string MiniTestResultTextName = "MiniTest_ResultText";

    public const string HeaderTitle = "Afet Doktoru YZ";
    public const string AITabText = "Yapay Zeka";
    public const string MiniTestTabText = "Mini Test";
    public const string MiniTestTitleText = "Afet Mini Test";
    public const string MiniTestNextButtonText = "Sonraki";
    public const string MiniTestFinishButtonText = "Bitir";
    public const string MiniTestRestartButtonText = "Tekrar Baslat";
    public const string WelcomeMessage = "Afet Doktoru YZ'ye ho\u015f geldiniz!\nNas\u0131l yard\u0131mc\u0131 olabilirim?";
    public const string LegacyWelcomeMessage = "Afet Doktoru YZ'ye hos geldiniz!\nNasil yardimci olabilirim?";
    public const string MojibakeWelcomeMessage = "Afet Doktoru YZ'ye ho\u00c5\u0178 geldiniz!\nNas\u00c4\u00b1l yard\u00c4\u00b1mc\u00c4\u00b1 olabilirim?";
    public const string PlaceholderText = "Mesaj\u0131n\u0131z\u0131 yaz\u0131n...";
    public const string LegacyPlaceholderText = "Mesajinizi yazin...";
    public const string SendButtonText = "G\u00f6nder";
    public const string LegacySendButtonText = "Gonder";
    public const string ThinkingRichText = "<color=#93A8BF><i>Doktor d\u00fc\u015f\u00fcn\u00fcyor...</i></color>";
    public const string LegacyThinkingRichText = "<color=#888888><i>Doktor dusunuyor...</i></color>";

    public static readonly Vector2 PreferredCanvasSize = new Vector2(1070f, 710f);
    public static readonly Vector3 PreferredCanvasScale = new Vector3(0.002f, 0.002f, 0.002f);

    public static readonly Vector2 MainPanelAnchorMin = new Vector2(0.018f, 0.055f);
    public static readonly Vector2 MainPanelAnchorMax = new Vector2(0.982f, 0.978f);
    public static readonly Vector2 KeyboardPanelAnchorMin = new Vector2(0.026f, 0.022f);
    public static readonly Vector2 KeyboardPanelAnchorMax = new Vector2(0.974f, 0.292f);

    public static readonly Vector2 HeaderAnchorMin = new Vector2(0.085f, 0.918f);
    public static readonly Vector2 HeaderAnchorMax = new Vector2(0.785f, 0.956f);
    public static readonly Vector2 TopTabBarAnchorMin = new Vector2(0.04f, 0.835f);
    public static readonly Vector2 TopTabBarAnchorMax = new Vector2(0.855f, 0.898f);
    public static readonly Vector2 PanelRootAnchorMin = Vector2.zero;
    public static readonly Vector2 PanelRootAnchorMax = Vector2.one;
    public static readonly Vector2 ScrollAnchorMin = new Vector2(0.008f, 0.425f);
    public static readonly Vector2 ScrollAnchorMax = new Vector2(0.992f, 0.892f);
    public static readonly Vector2 InputAnchorMin = new Vector2(0.038f, 0.286f);
    public static readonly Vector2 InputAnchorMax = new Vector2(0.962f, 0.432f);
    public static readonly Vector2 DismissOverlayAnchorMin = new Vector2(0.008f, 0.387f);
    public static readonly Vector2 DismissOverlayAnchorMax = new Vector2(0.992f, 0.82f);
    public static readonly Vector2 MiniTestHeaderAnchorMin = new Vector2(0.05f, 0.72f);
    public static readonly Vector2 MiniTestHeaderAnchorMax = new Vector2(0.95f, 0.81f);
    public static readonly Vector2 MiniTestQuestionAnchorMin = new Vector2(0.05f, 0.58f);
    public static readonly Vector2 MiniTestQuestionAnchorMax = new Vector2(0.95f, 0.7f);
    public static readonly Vector2 MiniTestOptionsAnchorMin = new Vector2(0.05f, 0.24f);
    public static readonly Vector2 MiniTestOptionsAnchorMax = new Vector2(0.95f, 0.56f);
    public static readonly Vector2 MiniTestFooterAnchorMin = new Vector2(0.05f, 0.12f);
    public static readonly Vector2 MiniTestFooterAnchorMax = new Vector2(0.95f, 0.2f);
    public static readonly Vector2 MiniTestResultAnchorMin = new Vector2(0.05f, 0.03f);
    public static readonly Vector2 MiniTestResultAnchorMax = new Vector2(0.95f, 0.09f);

    public static readonly Vector2 CloseButtonSize = new Vector2(64f, 64f);
    public static readonly Vector2 CloseButtonPosition = new Vector2(-10f, -8f);
    public static readonly Vector2 KeyboardShownPosition = Vector2.zero;
    public static readonly Vector2 KeyboardHiddenPosition = new Vector2(0f, -20f);
    public static readonly Vector2 TabButtonSize = new Vector2(178f, 42f);
    public static readonly Vector2 MiniTestOptionSize = new Vector2(0f, 64f);
    public static readonly Vector2 MiniTestFooterButtonSize = new Vector2(196f, 50f);

    public const float HeaderFontSize = 29f;
    public const float ChatFontSize = 25f;
    public const float InputFontSize = 20f;
    public const float PlaceholderFontSize = 18f;
    public const float SendButtonFontSize = 22f;
    public const float CloseButtonFontSize = 30f;
    public const float ChatLineSpacing = 7f;
    public const float ChatParagraphSpacing = 10f;
    public const float ChatMinimumHeight = 348f;
    public const float ChatTopPadding = 18f;
    public const float ChatSidePadding = 4f;
    public const float ChatBottomPadding = 20f;
    public const float ChatViewportInsetHorizontal = 4f;
    public const float ChatViewportInsetTop = 14f;
    public const float ChatViewportInsetBottom = 8f;
    public const float ChatTextTopMargin = 6f;
    public const float InputPreferredHeight = 96f;
    public const float SendButtonPreferredWidth = 182f;
    public const float SendButtonPreferredHeight = 60f;
    public const float InputSpacing = 12f;
    public const float KeyboardPanelSpacing = 10f;
    public const float KeyboardRowHeight = 52f;
    public const float KeyboardKeyBaseWidth = 66f;
    public const float KeyboardKeyMinWidth = 50f;
    public const float KeyboardKeyHeight = 50f;
    public const float KeyboardCharacterFontSize = 22f;
    public const float KeyboardActionFontSize = 18f;
    public const float KeyboardAccentHeight = 4f;
    public const float KeyboardLabelPaddingHorizontal = 10f;
    public const float KeyboardLabelPaddingVertical = 5f;
    public const float TabButtonFontSize = 18f;
    public const float MiniTestTitleFontSize = 24f;
    public const float MiniTestProgressFontSize = 19f;
    public const float MiniTestTimerFontSize = 19f;
    public const float MiniTestQuestionFontSize = 24f;
    public const float MiniTestOptionFontSize = 20f;
    public const float MiniTestResultFontSize = 18f;
    public const float MiniTestFooterButtonFontSize = 20f;

    public static readonly Color MainPanelColor = new Color(0.032f, 0.055f, 0.102f, 0.96f);
    public static readonly Color MainOutlineColor = new Color(0.1f, 0.84f, 1f, 0.24f);
    public static readonly Color ScrollBackgroundColor = new Color(0.02f, 0.03f, 0.06f, 0.965f);
    public static readonly Color InputBackgroundColor = new Color(0.08f, 0.11f, 0.18f, 0.98f);
    public static readonly Color AccentColor = new Color(0.08f, 0.88f, 1f, 1f);
    public static readonly Color HeaderColor = new Color(0.12f, 0.9f, 1f, 1f);
    public static readonly Color SendButtonColor = new Color(0.03f, 0.58f, 0.84f, 0.98f);
    public static readonly Color SendButtonHoverColor = new Color(0.1f, 0.76f, 0.98f, 1f);
    public static readonly Color CloseButtonColor = new Color(0.78f, 0.18f, 0.18f, 0.94f);
    public static readonly Color CloseButtonHoverColor = new Color(0.92f, 0.32f, 0.32f, 1f);
    public static readonly Color PlaceholderColor = new Color(0.67f, 0.72f, 0.78f, 0.95f);
    public static readonly Color UserTextColor = new Color(1f, 1f, 1f, 1f);
    public static readonly Color AssistantTextColor = new Color(0.18f, 0.68f, 1f, 1f);
    public static readonly Color KeyboardPanelColor = new Color(0.038f, 0.062f, 0.112f, 0.99f);
    public static readonly Color KeyboardAccentColor = new Color(0.14f, 0.82f, 1f, 0.34f);
    public static readonly Color OverlayColor = new Color(0.01f, 0.04f, 0.08f, 0.015f);
    public static readonly Color TabButtonColor = new Color(0.068f, 0.12f, 0.2f, 0.96f);
    public static readonly Color TabButtonActiveColor = new Color(0.05f, 0.56f, 0.84f, 0.98f);
    public static readonly Color TabButtonHoverColor = new Color(0.12f, 0.74f, 0.98f, 1f);
    public static readonly Color MiniTestCardColor = new Color(0.05f, 0.085f, 0.14f, 0.94f);
    public static readonly Color MiniTestOptionColor = new Color(0.08f, 0.12f, 0.19f, 0.98f);
    public static readonly Color MiniTestOptionSelectedColor = new Color(0.11f, 0.32f, 0.46f, 0.98f);
    public static readonly Color MiniTestOptionCorrectColor = new Color(0.12f, 0.55f, 0.35f, 0.98f);
    public static readonly Color MiniTestOptionWrongColor = new Color(0.66f, 0.22f, 0.24f, 0.98f);
    public static readonly Color MiniTestTextColor = new Color(0.95f, 0.98f, 1f, 1f);
    public static readonly Color MiniTestMutedTextColor = new Color(0.71f, 0.8f, 0.9f, 0.98f);

    public static void ApplyCanvasLayout(GameObject canvasObject)
    {
        if (canvasObject == null)
        {
            return;
        }

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        if (canvasRect != null)
        {
            canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
            canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.anchoredPosition = Vector2.zero;
            canvasRect.sizeDelta = PreferredCanvasSize;
            canvasRect.localScale = PreferredCanvasScale;
        }

        Transform root = canvasObject.transform;
        EnsureTabbedPanelStructure(root);
        NormalizeMainPanel(FindRect(root, MainPanelName), FindImage(root, MainPanelName), FindOutline(root, MainPanelName));
        NormalizePanelRoot(FindRectByPaths(root, MainPanelName + "/" + AIChatRootName));
        NormalizePanelRoot(FindRectByPaths(root, MainPanelName + "/" + MiniTestRootName));
        NormalizeHeader(FindTextByPaths(root, MainPanelName + "/" + AIChatRootName + "/" + HeaderName, MainPanelName + "/" + HeaderName));
        NormalizeCloseButton(FindButton(root, MainPanelName + "/" + CloseButtonName));
        NormalizeTopTabBar(FindRect(root, MainPanelName + "/" + TopTabBarName));
        NormalizeTabButton(FindButton(root, MainPanelName + "/" + TopTabBarName + "/" + TabAIButtonName), AITabText, TabAIButtonTextName);
        NormalizeTabButton(FindButton(root, MainPanelName + "/" + TopTabBarName + "/" + TabMiniTestButtonName), MiniTestTabText, TabMiniTestButtonTextName);
        NormalizeScrollView(FindScrollRectByPaths(root, MainPanelName + "/" + AIChatRootName + "/" + ScrollViewName, MainPanelName + "/" + ScrollViewName));
        NormalizeChatContent(FindRectByPaths(root, MainPanelName + "/" + AIChatRootName + "/" + ScrollViewName + "/" + ViewportName + "/" + ContentName, MainPanelName + "/" + ScrollViewName + "/" + ViewportName + "/" + ContentName));
        NormalizeChatHistory(FindTextByPaths(root, MainPanelName + "/" + AIChatRootName + "/" + ScrollViewName + "/" + ViewportName + "/" + ContentName + "/" + ChatHistoryName, MainPanelName + "/" + ScrollViewName + "/" + ViewportName + "/" + ContentName + "/" + ChatHistoryName));
        NormalizeInputContainer(FindRectByPaths(root, MainPanelName + "/" + AIChatRootName + "/" + InputContainerName, MainPanelName + "/" + InputContainerName));
        NormalizeInputField(FindInputFieldByPaths(root, MainPanelName + "/" + AIChatRootName + "/" + InputContainerName + "/" + InputFieldName, MainPanelName + "/" + InputContainerName + "/" + InputFieldName));
        NormalizeSendButton(FindButtonByPaths(root, MainPanelName + "/" + AIChatRootName + "/" + InputContainerName + "/" + SendButtonName, MainPanelName + "/" + InputContainerName + "/" + SendButtonName));
        NormalizeMiniTestHeader(FindRect(root, MainPanelName + "/" + MiniTestRootName + "/" + MiniTestHeaderName));
        NormalizeMiniTestTitle(FindText(root, MainPanelName + "/" + MiniTestRootName + "/" + MiniTestHeaderName + "/" + MiniTestTitleName));
        NormalizeMiniTestTimer(FindText(root, MainPanelName + "/" + MiniTestRootName + "/" + MiniTestHeaderName + "/" + MiniTestTimerName));
        NormalizeMiniTestProgress(FindText(root, MainPanelName + "/" + MiniTestRootName + "/" + MiniTestHeaderName + "/" + MiniTestProgressName));
        NormalizeMiniTestQuestionCard(FindRect(root, MainPanelName + "/" + MiniTestRootName + "/" + MiniTestQuestionCardName));
        NormalizeMiniTestQuestion(FindTextByPaths(
            root,
            MainPanelName + "/" + MiniTestRootName + "/" + MiniTestQuestionCardName + "/" + MiniTestQuestionTextName,
            MainPanelName + "/" + MiniTestRootName + "/" + MiniTestQuestionTextName));
        NormalizeMiniTestOptions(FindRect(root, MainPanelName + "/" + MiniTestRootName + "/" + MiniTestOptionsName));
        NormalizeMiniTestOptionButton(FindButton(root, MainPanelName + "/" + MiniTestRootName + "/" + MiniTestOptionsName + "/" + OptionAButtonName), OptionALabelName);
        NormalizeMiniTestOptionButton(FindButton(root, MainPanelName + "/" + MiniTestRootName + "/" + MiniTestOptionsName + "/" + OptionBButtonName), OptionBLabelName);
        NormalizeMiniTestOptionButton(FindButton(root, MainPanelName + "/" + MiniTestRootName + "/" + MiniTestOptionsName + "/" + OptionCButtonName), OptionCLabelName);
        NormalizeMiniTestFooter(FindRect(root, MainPanelName + "/" + MiniTestRootName + "/" + MiniTestFooterName));
        NormalizeMiniTestFooterButton(FindButton(root, MainPanelName + "/" + MiniTestRootName + "/" + MiniTestFooterName + "/" + MiniTestNextButtonName), MiniTestNextButtonText, MiniTestNextButtonTextName, SendButtonColor);
        NormalizeMiniTestFooterButton(FindButton(root, MainPanelName + "/" + MiniTestRootName + "/" + MiniTestFooterName + "/" + MiniTestRestartButtonName), MiniTestRestartButtonText, MiniTestRestartButtonTextName, TabButtonColor);
        NormalizeMiniTestResult(FindText(root, MainPanelName + "/" + MiniTestRootName + "/" + MiniTestResultTextName));
        NormalizeKeyboardDrawer(FindRect(root, MainPanelName + "/" + KeyboardDrawerName));
        NormalizeDismissOverlay(FindButton(root, MainPanelName + "/" + KeyboardDrawerName + "/" + DismissOverlayName));
        NormalizeKeyboardPanel(FindRect(root, MainPanelName + "/" + KeyboardDrawerName + "/" + KeyboardPanelName));
        NormalizeKeyboardAccent(FindImage(root, MainPanelName + "/" + KeyboardDrawerName + "/" + KeyboardPanelName + "/" + KeyboardAccentName));
    }

    public static bool ShouldUpgradeWelcomeText(string currentText)
    {
        if (string.IsNullOrWhiteSpace(currentText))
        {
            return true;
        }

        string normalized = currentText.Trim();
        return normalized == LegacyWelcomeMessage ||
               normalized == WelcomeMessage ||
               normalized == MojibakeWelcomeMessage;
    }

    public static RectOffset GetKeyboardPanelPadding()
    {
        return new RectOffset(18, 18, 14, 16);
    }

    public static RectOffset GetKeyboardRowPadding(string rowName)
    {
        switch (rowName)
        {
            case "Row_0_Numbers":
                return new RectOffset(78, 78, 0, 0);
            case "Row_1_Qwerty":
                return new RectOffset(10, 10, 0, 0);
            case "Row_2_Asdf":
                return new RectOffset(42, 42, 0, 0);
            case "Row_3_Zxcv":
                return new RectOffset(108, 108, 0, 0);
            case "Row_4_Controls":
                return new RectOffset(18, 18, 0, 0);
            default:
                return new RectOffset(0, 0, 0, 0);
        }
    }

    public static float GetKeyboardKeyWidth(float widthWeight)
    {
        return KeyboardKeyBaseWidth * widthWeight;
    }

    public static Color GetKeyboardKeyColor(VRKeyType keyType)
    {
        switch (keyType)
        {
            case VRKeyType.Enter:
                return new Color(0.05f, 0.41f, 0.66f, 0.99f);
            case VRKeyType.Close:
                return new Color(0.22f, 0.08f, 0.1f, 0.98f);
            case VRKeyType.Backspace:
            case VRKeyType.Clear:
            case VRKeyType.MoveLeft:
            case VRKeyType.MoveRight:
                return new Color(0.09f, 0.15f, 0.21f, 0.985f);
            case VRKeyType.Space:
                return new Color(0.08f, 0.14f, 0.205f, 0.985f);
            default:
                return new Color(0.085f, 0.12f, 0.18f, 0.985f);
        }
    }

    public static Color GetKeyboardKeyOutlineColor(VRKeyType keyType)
    {
        switch (keyType)
        {
            case VRKeyType.Enter:
                return new Color(0.12f, 0.78f, 1f, 0.34f);
            case VRKeyType.Close:
                return new Color(1f, 0.3f, 0.32f, 0.22f);
            case VRKeyType.Backspace:
            case VRKeyType.Clear:
            case VRKeyType.MoveLeft:
            case VRKeyType.MoveRight:
            case VRKeyType.Space:
                return new Color(0.25f, 0.65f, 0.92f, 0.18f);
            default:
                return new Color(0.18f, 0.76f, 1f, 0.16f);
        }
    }

    public static Color GetKeyboardLabelColor(VRKeyType keyType)
    {
        switch (keyType)
        {
            case VRKeyType.Close:
                return new Color(1f, 0.9f, 0.92f, 1f);
            case VRKeyType.Enter:
            case VRKeyType.Backspace:
            case VRKeyType.Clear:
            case VRKeyType.MoveLeft:
            case VRKeyType.MoveRight:
            case VRKeyType.Space:
                return new Color(0.94f, 0.98f, 1f, 1f);
            default:
                return new Color(0.98f, 0.99f, 1f, 1f);
        }
    }

    public static float GetKeyboardLabelFontSize(VRKeyType keyType)
    {
        return keyType == VRKeyType.Character ? KeyboardCharacterFontSize : KeyboardActionFontSize;
    }

    private static void EnsureTabbedPanelStructure(Transform root)
    {
        Transform mainPanel = root != null ? root.Find(MainPanelName) : null;
        if (mainPanel == null)
        {
            return;
        }

        RectTransform aiChatRoot = EnsureChildRect(mainPanel, AIChatRootName);
        RectTransform miniTestRoot = EnsureChildRect(mainPanel, MiniTestRootName);
        RectTransform tabBar = EnsureChildRect(mainPanel, TopTabBarName);

        RemoveDuplicateDirectChildren(mainPanel, AIChatRootName);
        RemoveDuplicateDirectChildren(mainPanel, MiniTestRootName);
        RemoveDuplicateDirectChildren(mainPanel, TopTabBarName);
        RemoveDuplicateDirectChildren(mainPanel, CloseButtonName);

        PromoteLegacyChild(mainPanel, aiChatRoot, HeaderName);
        PromoteLegacyChild(mainPanel, aiChatRoot, ScrollViewName);
        PromoteLegacyChild(mainPanel, aiChatRoot, InputContainerName);
        RemoveAllDirectChildrenByName(mainPanel, HeaderName);
        RemoveAllDirectChildrenByName(mainPanel, ScrollViewName);
        RemoveAllDirectChildrenByName(mainPanel, InputContainerName);

        EnsureTabButton(tabBar, TabAIButtonName, TabAIButtonTextName, AITabText);
        EnsureTabButton(tabBar, TabMiniTestButtonName, TabMiniTestButtonTextName, MiniTestTabText);
        RemoveDuplicateDirectChildren(tabBar, TabAIButtonName);
        RemoveDuplicateDirectChildren(tabBar, TabMiniTestButtonName);
        EnsureMiniTestStructure(miniTestRoot);
    }

    private static void EnsureMiniTestStructure(RectTransform miniTestRoot)
    {
        if (miniTestRoot == null)
        {
            return;
        }

        MoveNamedDescendantToParent(miniTestRoot, miniTestRoot, MiniTestHeaderName);
        RectTransform headerRect = EnsureChildRect(miniTestRoot, MiniTestHeaderName);
        RemoveDuplicateDescendantsByName(miniTestRoot, headerRect, MiniTestHeaderName);
        TextMeshProUGUI titleText = EnsureChildText(headerRect, MiniTestTitleName, MiniTestTitleText, MiniTestTextColor, TextAlignmentOptions.Left);
        TextMeshProUGUI timerText = EnsureChildText(headerRect, MiniTestTimerName, MiniTestTimerDefaultText, MiniTestMutedTextColor, TextAlignmentOptions.Center);
        TextMeshProUGUI progressText = EnsureChildText(headerRect, MiniTestProgressName, "Soru 1/5", MiniTestMutedTextColor, TextAlignmentOptions.Right);
        if (titleText != null) titleText.transform.SetSiblingIndex(0);
        if (timerText != null) timerText.transform.SetSiblingIndex(1);
        if (progressText != null) progressText.transform.SetSiblingIndex(2);
        RemoveDuplicateDirectChildren(headerRect, MiniTestTitleName);
        RemoveDuplicateDirectChildren(headerRect, MiniTestTimerName);
        RemoveDuplicateDirectChildren(headerRect, MiniTestProgressName);
        RemoveDuplicateDescendantsByName(miniTestRoot, titleText != null ? titleText.transform : null, MiniTestTitleName);
        RemoveDuplicateDescendantsByName(miniTestRoot, timerText != null ? timerText.transform : null, MiniTestTimerName);
        RemoveDuplicateDescendantsByName(miniTestRoot, progressText != null ? progressText.transform : null, MiniTestProgressName);

        MoveNamedDescendantToParent(miniTestRoot, miniTestRoot, MiniTestQuestionCardName);
        RectTransform questionCardRect = EnsureChildRect(miniTestRoot, MiniTestQuestionCardName);
        RemoveDuplicateDescendantsByName(miniTestRoot, questionCardRect, MiniTestQuestionCardName);
        MoveNamedDescendantToParent(miniTestRoot, questionCardRect, MiniTestQuestionTextName);
        TextMeshProUGUI questionText = EnsureChildText(questionCardRect, MiniTestQuestionTextName, "Mini test sekmesine gectiginde soru burada gorunecek.", MiniTestTextColor, TextAlignmentOptions.TopLeft);
        RemoveDuplicateDirectChildren(questionCardRect, MiniTestQuestionTextName);
        RemoveDuplicateDescendantsByName(miniTestRoot, questionText != null ? questionText.transform : null, MiniTestQuestionTextName);

        MoveNamedDescendantToParent(miniTestRoot, miniTestRoot, MiniTestOptionsName);
        RectTransform optionsRect = EnsureChildRect(miniTestRoot, MiniTestOptionsName);
        RemoveDuplicateDescendantsByName(miniTestRoot, optionsRect, MiniTestOptionsName);
        MoveNamedDescendantToParent(miniTestRoot, optionsRect, OptionAButtonName);
        MoveNamedDescendantToParent(miniTestRoot, optionsRect, OptionBButtonName);
        MoveNamedDescendantToParent(miniTestRoot, optionsRect, OptionCButtonName);
        Button optionAButton = EnsureOptionButton(optionsRect, OptionAButtonName, OptionALabelName, "Secenek A");
        Button optionBButton = EnsureOptionButton(optionsRect, OptionBButtonName, OptionBLabelName, "Secenek B");
        Button optionCButton = EnsureOptionButton(optionsRect, OptionCButtonName, OptionCLabelName, "Secenek C");
        RemoveDuplicateDirectChildren(optionsRect, OptionAButtonName);
        RemoveDuplicateDirectChildren(optionsRect, OptionBButtonName);
        RemoveDuplicateDirectChildren(optionsRect, OptionCButtonName);
        RemoveDuplicateDescendantsByName(miniTestRoot, optionAButton != null ? optionAButton.transform : null, OptionAButtonName);
        RemoveDuplicateDescendantsByName(miniTestRoot, optionBButton != null ? optionBButton.transform : null, OptionBButtonName);
        RemoveDuplicateDescendantsByName(miniTestRoot, optionCButton != null ? optionCButton.transform : null, OptionCButtonName);

        MoveNamedDescendantToParent(miniTestRoot, miniTestRoot, MiniTestFooterName);
        RectTransform footerRect = EnsureChildRect(miniTestRoot, MiniTestFooterName);
        RemoveDuplicateDescendantsByName(miniTestRoot, footerRect, MiniTestFooterName);
        MoveNamedDescendantToParent(miniTestRoot, footerRect, MiniTestNextButtonName);
        MoveNamedDescendantToParent(miniTestRoot, footerRect, MiniTestRestartButtonName);
        Button nextButton = EnsureFooterButton(footerRect, MiniTestNextButtonName, MiniTestNextButtonTextName, MiniTestNextButtonText);
        Button restartButton = EnsureFooterButton(footerRect, MiniTestRestartButtonName, MiniTestRestartButtonTextName, MiniTestRestartButtonText);
        RemoveDuplicateDirectChildren(footerRect, MiniTestNextButtonName);
        RemoveDuplicateDirectChildren(footerRect, MiniTestRestartButtonName);
        RemoveDuplicateDescendantsByName(miniTestRoot, nextButton != null ? nextButton.transform : null, MiniTestNextButtonName);
        RemoveDuplicateDescendantsByName(miniTestRoot, restartButton != null ? restartButton.transform : null, MiniTestRestartButtonName);

        MoveNamedDescendantToParent(miniTestRoot, miniTestRoot, MiniTestResultTextName);
        TextMeshProUGUI resultText = EnsureChildText(miniTestRoot, MiniTestResultTextName, "Sorular acildiginda seciminin geri bildirimi burada gorunecek.", MiniTestMutedTextColor, TextAlignmentOptions.TopLeft);
        RemoveDuplicateDirectChildren(miniTestRoot, MiniTestResultTextName);
        RemoveDuplicateDescendantsByName(miniTestRoot, resultText != null ? resultText.transform : null, MiniTestResultTextName);

        RemoveDuplicateDirectChildren(miniTestRoot, MiniTestHeaderName);
        RemoveDuplicateDirectChildren(miniTestRoot, MiniTestQuestionCardName);
        RemoveDuplicateDirectChildren(miniTestRoot, MiniTestOptionsName);
        RemoveDuplicateDirectChildren(miniTestRoot, MiniTestFooterName);
        headerRect.SetSiblingIndex(0);
        questionCardRect.SetSiblingIndex(1);
        optionsRect.SetSiblingIndex(2);
        footerRect.SetSiblingIndex(3);
        if (resultText != null)
        {
            resultText.transform.SetSiblingIndex(4);
        }
    }

    private static RectTransform EnsureChildRect(Transform parent, string childName)
    {
        Transform existing = parent != null ? parent.Find(childName) : null;
        if (existing != null && IsIncompatibleContainerChild(existing))
        {
            existing = ReplaceDirectChild(parent, existing, childName, typeof(RectTransform));
        }

        if (existing != null)
        {
            return GetOrAddComponent<RectTransform>(existing.gameObject);
        }

        GameObject childObject = new GameObject(childName, typeof(RectTransform));
        childObject.transform.SetParent(parent, false);
        return childObject.GetComponent<RectTransform>();
    }

    private static bool IsIncompatibleContainerChild(Transform child)
    {
        if (child == null)
        {
            return false;
        }

        return child.GetComponent<TextMeshProUGUI>() != null ||
               child.GetComponent<Button>() != null ||
               child.GetComponent<TMP_InputField>() != null;
    }

    private static Transform ReplaceDirectChild(Transform parent, Transform existing, string childName, params System.Type[] componentTypes)
    {
        if (parent == null || existing == null)
        {
            return existing;
        }

        int siblingIndex = existing.GetSiblingIndex();
        GameObject replacementObject = componentTypes != null && componentTypes.Length > 0
            ? new GameObject(childName, componentTypes)
            : new GameObject(childName, typeof(RectTransform));
        replacementObject.transform.SetParent(parent, false);
        replacementObject.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, Mathf.Max(0, parent.childCount - 1)));

        while (existing.childCount > 0)
        {
            existing.GetChild(0).SetParent(replacementObject.transform, false);
        }

        DestroyChildObject(existing.gameObject);
        return replacementObject.transform;
    }

    private static void PromoteLegacyChild(Transform sourceParent, Transform targetParent, string childName)
    {
        if (sourceParent == null || targetParent == null)
        {
            return;
        }

        Transform child = sourceParent.Find(childName);
        if (child == null || child == targetParent || child.parent == targetParent)
        {
            return;
        }

        child.SetParent(targetParent, false);
    }

    private static void MoveNamedDescendantToParent(Transform searchRoot, Transform targetParent, string childName)
    {
        if (searchRoot == null || targetParent == null || string.IsNullOrWhiteSpace(childName))
        {
            return;
        }

        if (targetParent.Find(childName) != null)
        {
            return;
        }

        Transform descendant = FindDescendantByName(searchRoot, childName);
        if (descendant == null || descendant == targetParent)
        {
            return;
        }

        descendant.SetParent(targetParent, false);
    }

    private static Transform FindDescendantByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < descendants.Length; i++)
        {
            Transform descendant = descendants[i];
            if (descendant != null && descendant != root && descendant.name == childName)
            {
                return descendant;
            }
        }

        return null;
    }

    private static void RemoveDuplicateDescendantsByName(Transform root, Transform keep, string childName)
    {
        if (root == null || keep == null || string.IsNullOrWhiteSpace(childName))
        {
            return;
        }

        Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
        List<GameObject> duplicates = new List<GameObject>();
        for (int i = 0; i < descendants.Length; i++)
        {
            Transform descendant = descendants[i];
            if (descendant != null && descendant != root && descendant != keep && descendant.name == childName)
            {
                duplicates.Add(descendant.gameObject);
            }
        }

        for (int i = 0; i < duplicates.Count; i++)
        {
            DestroyChildObject(duplicates[i]);
        }
    }

    private static void RemoveDuplicateDirectChildren(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
        {
            return;
        }

        Transform keep = parent.Find(childName);
        if (keep == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child != null && child != keep && child.name == childName)
            {
                DestroyChildObject(child.gameObject);
            }
        }
    }

    private static void RemoveAllDirectChildrenByName(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child != null && child.name == childName)
            {
                DestroyChildObject(child.gameObject);
            }
        }
    }

    private static void DestroyChildObject(GameObject childObject)
    {
        if (childObject == null)
        {
            return;
        }

        childObject.SetActive(false);
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Object.DestroyImmediate(childObject);
            return;
        }
#endif
        Object.Destroy(childObject);
    }

    private static Button EnsureTabButton(Transform parent, string buttonName, string labelName, string labelText)
    {
        GameObject buttonObject = FindOrCreateChild(parent, buttonName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(Outline));
        Button button = GetOrAddComponent<Button>(buttonObject);
        EnsureChildText(button.transform, labelName, labelText, Color.white, TextAlignmentOptions.Center);
        return button;
    }

    private static Button EnsureOptionButton(Transform parent, string buttonName, string labelName, string labelText)
    {
        GameObject buttonObject = FindOrCreateChild(parent, buttonName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(Outline));
        Button button = GetOrAddComponent<Button>(buttonObject);
        EnsureChildText(button.transform, labelName, labelText, MiniTestTextColor, TextAlignmentOptions.MidlineLeft);
        return button;
    }

    private static Button EnsureFooterButton(Transform parent, string buttonName, string labelName, string labelText)
    {
        GameObject buttonObject = FindOrCreateChild(parent, buttonName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(Outline));
        Button button = GetOrAddComponent<Button>(buttonObject);
        EnsureChildText(button.transform, labelName, labelText, Color.white, TextAlignmentOptions.Center);
        return button;
    }

    private static TextMeshProUGUI EnsureChildText(Transform parent, string textName, string textValue, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = FindOrCreateChild(parent, textName, typeof(RectTransform), typeof(TextMeshProUGUI));
        TextMeshProUGUI text = GetOrAddComponent<TextMeshProUGUI>(textObject);
        text.text = textValue;
        text.color = color;
        text.alignment = alignment;
        return text;
    }

    private static GameObject FindOrCreateChild(Transform parent, string childName, params System.Type[] componentTypes)
    {
        List<System.Type> uniqueComponentTypes = new List<System.Type>();
        if (componentTypes != null)
        {
            for (int i = 0; i < componentTypes.Length; i++)
            {
                System.Type type = componentTypes[i];
                if (type == null || uniqueComponentTypes.Contains(type))
                {
                    continue;
                }

                uniqueComponentTypes.Add(type);
            }
        }

        Transform existing = parent != null ? parent.Find(childName) : null;
        if (existing != null)
        {
            if (HasIncompatibleChildSetup(existing.gameObject, uniqueComponentTypes))
            {
                existing = ReplaceDirectChild(
                    parent,
                    existing,
                    childName,
                    uniqueComponentTypes.Count > 0 ? uniqueComponentTypes.ToArray() : new[] { typeof(RectTransform) });
            }

            for (int i = 0; i < uniqueComponentTypes.Count; i++)
            {
                System.Type componentType = uniqueComponentTypes[i];
                if (componentType != null && existing.GetComponent(componentType) == null)
                {
                    existing.gameObject.AddComponent(componentType);
                }
            }

            return existing.gameObject;
        }

        GameObject childObject = uniqueComponentTypes.Count > 0
            ? new GameObject(childName, uniqueComponentTypes.ToArray())
            : new GameObject(childName, typeof(RectTransform));
        childObject.transform.SetParent(parent, false);
        return childObject;
    }

    private static bool HasIncompatibleChildSetup(GameObject existingObject, List<System.Type> requiredComponentTypes)
    {
        if (existingObject == null || requiredComponentTypes == null || requiredComponentTypes.Count == 0)
        {
            return false;
        }

        bool requiresButton = requiredComponentTypes.Contains(typeof(Button));
        bool requiresText = requiredComponentTypes.Contains(typeof(TextMeshProUGUI));
        System.Type requiredGraphicType = null;
        for (int i = 0; i < requiredComponentTypes.Count; i++)
        {
            System.Type componentType = requiredComponentTypes[i];
            if (componentType != null && typeof(Graphic).IsAssignableFrom(componentType))
            {
                requiredGraphicType = componentType;
                break;
            }
        }

        Graphic existingGraphic = existingObject.GetComponent<Graphic>();
        if (existingGraphic != null &&
            requiredGraphicType != null &&
            existingGraphic.GetType() != requiredGraphicType)
        {
            return true;
        }

        if (requiresButton &&
            (existingObject.GetComponent<TextMeshProUGUI>() != null ||
             existingObject.GetComponent<TMP_InputField>() != null))
        {
            return true;
        }

        if (requiresText &&
            (existingObject.GetComponent<Button>() != null ||
             existingObject.GetComponent<TMP_InputField>() != null))
        {
            return true;
        }

        return false;
    }

    private static void NormalizePanelRoot(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = PanelRootAnchorMin;
        rect.anchorMax = PanelRootAnchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void NormalizeTopTabBar(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = TopTabBarAnchorMin;
        rect.anchorMax = TopTabBarAnchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;

        HorizontalLayoutGroup layout = GetOrAddComponent<HorizontalLayoutGroup>(rect.gameObject);
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
    }

    private static void NormalizeTabButton(Button button, string labelText, string labelName)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = TabButtonSize;
        rect.localScale = Vector3.one;

        LayoutElement layout = GetOrAddComponent<LayoutElement>(button.gameObject);
        layout.minWidth = TabButtonSize.x;
        layout.preferredWidth = TabButtonSize.x;
        layout.minHeight = TabButtonSize.y;
        layout.preferredHeight = TabButtonSize.y;
        layout.flexibleWidth = 0f;

        Image image = GetOrAddComponent<Image>(button.gameObject);
        image.color = TabButtonColor;
        image.raycastTarget = true;
        button.targetGraphic = image;

        Outline outline = GetOrAddComponent<Outline>(button.gameObject);
        outline.effectColor = new Color(0.14f, 0.72f, 1f, 0.14f);
        outline.effectDistance = new Vector2(1f, -1f);

        button.transition = Selectable.Transition.ColorTint;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = button.colors;
        colors.normalColor = TabButtonColor;
        colors.highlightedColor = TabButtonHoverColor;
        colors.pressedColor = new Color(0.04f, 0.4f, 0.62f, 1f);
        colors.selectedColor = TabButtonActiveColor;
        colors.disabledColor = new Color(0.22f, 0.24f, 0.29f, 0.55f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        TextMeshProUGUI label = FindText(button.transform, labelName);
        if (label != null)
        {
            label.text = labelText;
            label.fontSize = TabButtonFontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            StretchToFill(label.rectTransform, Vector2.zero, Vector2.zero);
        }
    }

    private static void NormalizeMiniTestHeader(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = MiniTestHeaderAnchorMin;
        rect.anchorMax = MiniTestHeaderAnchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;

        HorizontalLayoutGroup layout = GetOrAddComponent<HorizontalLayoutGroup>(rect.gameObject);
        layout.padding = new RectOffset(14, 14, 10, 10);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        layout.childAlignment = TextAnchor.MiddleLeft;

        Image image = GetOrAddComponent<Image>(rect.gameObject);
        image.color = MiniTestCardColor;
        image.raycastTarget = false;

        Outline outline = GetOrAddComponent<Outline>(rect.gameObject);
        outline.effectColor = new Color(0.14f, 0.72f, 1f, 0.14f);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    private static void NormalizeMiniTestTitle(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        text.text = MiniTestTitleText;
        text.fontSize = MiniTestTitleFontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = MiniTestTextColor;
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = false;
        text.raycastTarget = false;

        LayoutElement layout = GetOrAddComponent<LayoutElement>(text.gameObject);
        layout.flexibleWidth = 1f;
        layout.preferredHeight = 34f;
    }

    private static void NormalizeMiniTestProgress(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        text.fontSize = MiniTestProgressFontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = MiniTestMutedTextColor;
        text.alignment = TextAlignmentOptions.Right;
        text.enableWordWrapping = false;
        text.raycastTarget = false;

        LayoutElement layout = GetOrAddComponent<LayoutElement>(text.gameObject);
        layout.minWidth = 150f;
        layout.preferredWidth = 150f;
        layout.preferredHeight = 30f;
        layout.flexibleWidth = 0f;
    }

    private static void NormalizeMiniTestTimer(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(text.text) || text.text == "New Text")
        {
            text.text = MiniTestTimerDefaultText;
        }
        text.fontSize = MiniTestTimerFontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(0.55f, 0.9f, 1f, 1f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;

        LayoutElement layout = GetOrAddComponent<LayoutElement>(text.gameObject);
        layout.minWidth = 130f;
        layout.preferredWidth = 150f;
        layout.preferredHeight = 30f;
        layout.flexibleWidth = 0f;
    }

    private static void NormalizeMiniTestQuestionCard(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = MiniTestQuestionAnchorMin;
        rect.anchorMax = MiniTestQuestionAnchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;

        Image image = GetOrAddComponent<Image>(rect.gameObject);
        if (image != null)
        {
            image.color = MiniTestCardColor;
            image.raycastTarget = false;
        }

        Outline outline = GetOrAddComponent<Outline>(rect.gameObject);
        if (outline != null)
        {
            outline.effectColor = new Color(0.14f, 0.72f, 1f, 0.14f);
            outline.effectDistance = new Vector2(2f, -2f);
        }
    }

    private static void NormalizeMiniTestQuestion(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        RectTransform rect = text.rectTransform;
        if (rect == null)
        {
            Debug.LogWarning("[AIChatCanvasLayout] Mini test soru metni icin RectTransform bulunamadi.");
            return;
        }

        StretchToFill(rect, Vector2.zero, Vector2.zero);

        text.fontSize = MiniTestQuestionFontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = MiniTestTextColor;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.margin = new Vector4(14f, 12f, 14f, 8f);
        text.raycastTarget = false;
    }

    private static void NormalizeMiniTestOptions(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = MiniTestOptionsAnchorMin;
        rect.anchorMax = MiniTestOptionsAnchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;

        VerticalLayoutGroup layout = GetOrAddComponent<VerticalLayoutGroup>(rect.gameObject);
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static void NormalizeMiniTestOptionButton(Button button, string labelName)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;

        LayoutElement layout = GetOrAddComponent<LayoutElement>(button.gameObject);
        layout.preferredHeight = MiniTestOptionSize.y;
        layout.minHeight = MiniTestOptionSize.y;
        layout.flexibleWidth = 1f;

        Image image = GetOrAddComponent<Image>(button.gameObject);
        image.color = MiniTestOptionColor;
        image.raycastTarget = true;
        button.targetGraphic = image;

        Outline outline = GetOrAddComponent<Outline>(button.gameObject);
        outline.effectColor = new Color(0.14f, 0.72f, 1f, 0.12f);
        outline.effectDistance = new Vector2(2f, -2f);

        button.transition = Selectable.Transition.ColorTint;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = button.colors;
        colors.normalColor = MiniTestOptionColor;
        colors.highlightedColor = TabButtonHoverColor;
        colors.pressedColor = MiniTestOptionSelectedColor;
        colors.selectedColor = MiniTestOptionSelectedColor;
        colors.disabledColor = MiniTestOptionColor;
        colors.colorMultiplier = 1f;
        button.colors = colors;

        TextMeshProUGUI label = FindText(button.transform, labelName);
        if (label != null)
        {
            label.fontSize = MiniTestOptionFontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = MiniTestTextColor;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;
            label.margin = new Vector4(18f, 0f, 18f, 0f);
            label.raycastTarget = false;
            StretchToFill(label.rectTransform, Vector2.zero, Vector2.zero);
        }
    }

    private static void NormalizeMiniTestFooter(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = MiniTestFooterAnchorMin;
        rect.anchorMax = MiniTestFooterAnchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;

        HorizontalLayoutGroup layout = GetOrAddComponent<HorizontalLayoutGroup>(rect.gameObject);
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private static void NormalizeMiniTestFooterButton(Button button, string defaultLabel, string labelName, Color normalColor)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;

        LayoutElement layout = GetOrAddComponent<LayoutElement>(button.gameObject);
        layout.preferredWidth = MiniTestFooterButtonSize.x;
        layout.minWidth = MiniTestFooterButtonSize.x;
        layout.preferredHeight = MiniTestFooterButtonSize.y;
        layout.minHeight = MiniTestFooterButtonSize.y;
        layout.flexibleWidth = 0f;

        Image image = GetOrAddComponent<Image>(button.gameObject);
        image.color = normalColor;
        image.raycastTarget = true;
        button.targetGraphic = image;

        Outline outline = GetOrAddComponent<Outline>(button.gameObject);
        outline.effectColor = new Color(0.14f, 0.72f, 1f, 0.14f);
        outline.effectDistance = new Vector2(1f, -1f);

        button.transition = Selectable.Transition.ColorTint;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = TabButtonHoverColor;
        colors.pressedColor = new Color(0.04f, 0.4f, 0.62f, 1f);
        colors.selectedColor = TabButtonHoverColor;
        colors.disabledColor = new Color(0.26f, 0.3f, 0.35f, 0.55f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        TextMeshProUGUI label = FindText(button.transform, labelName);
        if (label != null)
        {
            label.text = defaultLabel;
            label.fontSize = MiniTestFooterButtonFontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            StretchToFill(label.rectTransform, Vector2.zero, Vector2.zero);
        }
    }

    private static void NormalizeMiniTestResult(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        RectTransform rect = text.rectTransform;
        rect.anchorMin = MiniTestResultAnchorMin;
        rect.anchorMax = MiniTestResultAnchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;

        text.fontSize = MiniTestResultFontSize;
        text.fontStyle = FontStyles.Normal;
        text.color = MiniTestMutedTextColor;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.margin = new Vector4(4f, 2f, 4f, 0f);
        text.raycastTarget = false;
    }

    private static void NormalizeMainPanel(RectTransform rect, Image image, Outline outline)
    {
        if (rect == null)
        {
            return;
        }

        if (image == null)
        {
            image = GetOrAddComponent<Image>(rect.gameObject);
        }

        if (outline == null)
        {
            outline = GetOrAddComponent<Outline>(rect.gameObject);
        }

        rect.anchorMin = MainPanelAnchorMin;
        rect.anchorMax = MainPanelAnchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;

        if (image != null)
        {
            image.color = MainPanelColor;
        }

        if (outline != null)
        {
            outline.effectColor = MainOutlineColor;
            outline.effectDistance = new Vector2(3f, -3f);
        }
    }

    private static void NormalizeHeader(TextMeshProUGUI headerText)
    {
        if (headerText == null)
        {
            return;
        }

        headerText.text = HeaderTitle;
        headerText.fontSize = HeaderFontSize;
        headerText.fontStyle = FontStyles.Bold;
        headerText.color = HeaderColor;
        headerText.alignment = TextAlignmentOptions.Center;
        headerText.enableAutoSizing = false;
        headerText.raycastTarget = false;
        headerText.characterSpacing = 0f;

        RectTransform rect = headerText.rectTransform;
        rect.anchorMin = HeaderAnchorMin;
        rect.anchorMax = HeaderAnchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void NormalizeCloseButton(Button closeButton)
    {
        if (closeButton == null)
        {
            return;
        }

        RectTransform rect = closeButton.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = CloseButtonSize;
        rect.anchoredPosition = CloseButtonPosition;
        rect.localScale = Vector3.one;

        closeButton.transition = Selectable.Transition.ColorTint;
        closeButton.navigation = new Navigation { mode = Navigation.Mode.None };

        Image image = closeButton.GetComponent<Image>();
        if (image != null)
        {
            image.color = CloseButtonColor;
            image.raycastTarget = true;
            closeButton.targetGraphic = image;
        }

        ColorBlock colors = closeButton.colors;
        colors.normalColor = CloseButtonColor;
        colors.highlightedColor = CloseButtonHoverColor;
        colors.pressedColor = new Color(0.58f, 0.1f, 0.1f, 1f);
        colors.selectedColor = CloseButtonHoverColor;
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        colors.colorMultiplier = 1f;
        closeButton.colors = colors;

        TextMeshProUGUI label = FindText(closeButton.transform, CloseButtonTextName);
        if (label != null)
        {
            label.text = "X";
            label.fontSize = CloseButtonFontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelRect.anchoredPosition = Vector2.zero;
        }
    }

    private static void NormalizeScrollView(ScrollRect scrollRect)
    {
        if (scrollRect == null)
        {
            return;
        }

        RectTransform rect = scrollRect.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = ScrollAnchorMin;
            rect.anchorMax = ScrollAnchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        Image image = scrollRect.GetComponent<Image>();
        if (image == null)
        {
            image = GetOrAddComponent<Image>(scrollRect.gameObject);
        }

        if (image != null)
        {
            image.color = ScrollBackgroundColor;
            image.raycastTarget = true;
        }

        Outline outline = scrollRect.GetComponent<Outline>();
        if (outline == null)
        {
            outline = GetOrAddComponent<Outline>(scrollRect.gameObject);
        }

        outline.effectColor = new Color(0.14f, 0.72f, 1f, 0.12f);
        outline.effectDistance = new Vector2(2f, -2f);

        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 52f;

        if (scrollRect.viewport != null)
        {
            RectTransform viewport = scrollRect.viewport;
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(ChatViewportInsetHorizontal, ChatViewportInsetBottom);
            viewport.offsetMax = new Vector2(-ChatViewportInsetHorizontal, -ChatViewportInsetTop);
            viewport.anchoredPosition = Vector2.zero;

            Image viewportImage = viewport.GetComponent<Image>();
            if (viewportImage != null)
            {
                viewportImage.raycastTarget = false;
                viewportImage.color = new Color(0f, 0f, 0f, 0f);
            }
        }
    }

    private static void NormalizeChatContent(RectTransform contentRect)
    {
        if (contentRect == null)
        {
            return;
        }

        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, ChatMinimumHeight + ChatBottomPadding);
        contentRect.localScale = Vector3.one;

        LayoutGroup layoutGroup = contentRect.GetComponent<LayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
        }

        ContentSizeFitter fitter = contentRect.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            fitter.enabled = false;
        }

        Image image = contentRect.GetComponent<Image>();
        if (image == null)
        {
            image = GetOrAddComponent<Image>(contentRect.gameObject);
        }

        if (image != null)
        {
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = false;
        }
    }

    private static void NormalizeChatHistory(TextMeshProUGUI chatHistoryText)
    {
        if (chatHistoryText == null)
        {
            return;
        }

        if (ShouldUpgradeWelcomeText(chatHistoryText.text))
        {
            chatHistoryText.text = WelcomeMessage;
        }

        chatHistoryText.fontSize = ChatFontSize;
        chatHistoryText.fontStyle = FontStyles.Normal;
        chatHistoryText.color = UserTextColor;
        chatHistoryText.alignment = TextAlignmentOptions.TopLeft;
        chatHistoryText.enableWordWrapping = true;
        chatHistoryText.overflowMode = TextOverflowModes.Overflow;
        chatHistoryText.richText = true;
        chatHistoryText.raycastTarget = false;
        chatHistoryText.lineSpacing = ChatLineSpacing;
        chatHistoryText.paragraphSpacing = ChatParagraphSpacing;
        chatHistoryText.margin = new Vector4(0f, ChatTextTopMargin, 0f, 0f);

        RectTransform rect = chatHistoryText.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -ChatTopPadding);
        rect.sizeDelta = new Vector2(-ChatSidePadding, ChatMinimumHeight);
        rect.localScale = Vector3.one;
    }

    private static void NormalizeInputContainer(RectTransform containerRect)
    {
        if (containerRect == null)
        {
            return;
        }

        containerRect.anchorMin = InputAnchorMin;
        containerRect.anchorMax = InputAnchorMax;
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.localScale = Vector3.one;

        HorizontalLayoutGroup layout = GetOrAddComponent<HorizontalLayoutGroup>(containerRect.gameObject);
        layout.spacing = InputSpacing;
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
    }

    private static void NormalizeInputField(TMP_InputField inputField)
    {
        if (inputField == null)
        {
            return;
        }

        RectTransform rect = inputField.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
        }

        LayoutElement layout = GetOrAddComponent<LayoutElement>(inputField.gameObject);
        layout.flexibleWidth = 1f;
        layout.preferredHeight = InputPreferredHeight;

        Image image = inputField.GetComponent<Image>();
        if (image == null)
        {
            image = GetOrAddComponent<Image>(inputField.gameObject);
        }

        image.color = InputBackgroundColor;
        image.raycastTarget = true;

        Outline outline = inputField.GetComponent<Outline>();
        if (outline == null)
        {
            outline = GetOrAddComponent<Outline>(inputField.gameObject);
        }

        outline.effectColor = new Color(0.14f, 0.72f, 1f, 0.12f);
        outline.effectDistance = new Vector2(1f, -1f);

        inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
        inputField.lineLimit = 0;
        inputField.readOnly = false;
        inputField.richText = false;
        inputField.isRichTextEditingAllowed = false;
        inputField.customCaretColor = true;
        inputField.caretColor = AccentColor;
        inputField.selectionColor = new Color(0.06f, 0.84f, 1f, 0.22f);
        inputField.resetOnDeActivation = true;
        inputField.restoreOriginalTextOnEscape = false;
        inputField.scrollSensitivity = 24f;

        RectTransform textAreaRect = EnsureTextArea(inputField.transform);
        StretchToFill(textAreaRect, new Vector2(16f, 12f), new Vector2(-16f, -12f));

        TextMeshProUGUI placeholder = EnsureTextChild(textAreaRect, PlaceholderName);
        placeholder.text = PlaceholderText;
        placeholder.fontSize = PlaceholderFontSize;
        placeholder.color = PlaceholderColor;
        placeholder.alignment = TextAlignmentOptions.TopLeft;
        placeholder.raycastTarget = false;
        placeholder.richText = false;
        placeholder.enableAutoSizing = false;
        placeholder.autoSizeTextContainer = false;
        placeholder.enableWordWrapping = true;
        placeholder.overflowMode = TextOverflowModes.Masking;
        placeholder.margin = Vector4.zero;
        StretchToFill(placeholder.rectTransform, Vector2.zero, Vector2.zero);

        TextMeshProUGUI inputText = EnsureTextChild(textAreaRect, InputTextName);
        inputText.fontSize = InputFontSize;
        inputText.color = Color.white;
        inputText.alignment = TextAlignmentOptions.TopLeft;
        inputText.raycastTarget = false;
        inputText.richText = false;
        inputText.enableAutoSizing = false;
        inputText.autoSizeTextContainer = false;
        inputText.enableWordWrapping = true;
        inputText.overflowMode = TextOverflowModes.Masking;
        inputText.margin = Vector4.zero;
        StretchToFill(inputText.rectTransform, Vector2.zero, Vector2.zero);

        inputField.textViewport = textAreaRect;
        inputField.placeholder = placeholder;
        inputField.textComponent = inputText;
    }

    private static void NormalizeSendButton(Button sendButton)
    {
        if (sendButton == null)
        {
            return;
        }

        LayoutElement layout = GetOrAddComponent<LayoutElement>(sendButton.gameObject);
        layout.preferredWidth = SendButtonPreferredWidth;
        layout.preferredHeight = SendButtonPreferredHeight;
        layout.flexibleWidth = 0f;

        Image image = sendButton.GetComponent<Image>();
        if (image == null)
        {
            image = GetOrAddComponent<Image>(sendButton.gameObject);
        }

        image.color = SendButtonColor;
        image.raycastTarget = true;
        sendButton.targetGraphic = image;

        sendButton.transition = Selectable.Transition.ColorTint;
        sendButton.navigation = new Navigation { mode = Navigation.Mode.None };

        ColorBlock colors = sendButton.colors;
        colors.normalColor = SendButtonColor;
        colors.highlightedColor = SendButtonHoverColor;
        colors.pressedColor = new Color(0.03f, 0.42f, 0.64f, 1f);
        colors.selectedColor = SendButtonHoverColor;
        colors.disabledColor = new Color(0.28f, 0.32f, 0.36f, 0.6f);
        colors.colorMultiplier = 1f;
        sendButton.colors = colors;

        TextMeshProUGUI label = FindText(sendButton.transform, SendButtonTextName);
        if (label != null)
        {
            label.text = SendButtonText;
            label.fontSize = SendButtonFontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            StretchToFill(label.rectTransform, Vector2.zero, Vector2.zero);
        }
    }

    private static void NormalizeKeyboardDrawer(RectTransform drawerRect)
    {
        if (drawerRect == null)
        {
            return;
        }

        drawerRect.anchorMin = Vector2.zero;
        drawerRect.anchorMax = Vector2.one;
        drawerRect.offsetMin = Vector2.zero;
        drawerRect.offsetMax = Vector2.zero;
        drawerRect.anchoredPosition = Vector2.zero;
        drawerRect.localScale = Vector3.one;
        drawerRect.SetAsLastSibling();
    }

    private static void NormalizeDismissOverlay(Button overlayButton)
    {
        if (overlayButton == null)
        {
            return;
        }

        RectTransform rect = overlayButton.GetComponent<RectTransform>();
        rect.anchorMin = DismissOverlayAnchorMin;
        rect.anchorMax = DismissOverlayAnchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.SetAsFirstSibling();

        Image image = overlayButton.GetComponent<Image>();
        if (image == null)
        {
            image = GetOrAddComponent<Image>(overlayButton.gameObject);
        }

        image.color = OverlayColor;

        overlayButton.transition = Selectable.Transition.None;
        overlayButton.navigation = new Navigation { mode = Navigation.Mode.None };
    }

    private static void NormalizeKeyboardPanel(RectTransform keyboardPanel)
    {
        if (keyboardPanel == null)
        {
            return;
        }

        keyboardPanel.anchorMin = KeyboardPanelAnchorMin;
        keyboardPanel.anchorMax = KeyboardPanelAnchorMax;
        keyboardPanel.offsetMin = Vector2.zero;
        keyboardPanel.offsetMax = Vector2.zero;
        keyboardPanel.anchoredPosition = Vector2.zero;
        keyboardPanel.localScale = Vector3.one;

        Image image = keyboardPanel.GetComponent<Image>();
        if (image == null)
        {
            image = GetOrAddComponent<Image>(keyboardPanel.gameObject);
        }

        image.color = KeyboardPanelColor;

        Outline outline = keyboardPanel.GetComponent<Outline>();
        if (outline == null)
        {
            outline = GetOrAddComponent<Outline>(keyboardPanel.gameObject);
        }

        outline.effectColor = new Color(0.1f, 0.82f, 1f, 0.18f);
        outline.effectDistance = new Vector2(2f, -2f);

        VerticalLayoutGroup layout = keyboardPanel.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = GetOrAddComponent<VerticalLayoutGroup>(keyboardPanel.gameObject);
        }

        layout.padding = GetKeyboardPanelPadding();
        layout.spacing = KeyboardPanelSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        keyboardPanel.SetAsLastSibling();
    }

    private static void NormalizeKeyboardAccent(Image accentImage)
    {
        if (accentImage == null)
        {
            return;
        }

        RectTransform rect = accentImage.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, KeyboardAccentHeight);
        rect.anchoredPosition = new Vector2(0f, -8f);
        rect.localScale = Vector3.one;
        rect.SetAsFirstSibling();

        accentImage.color = KeyboardAccentColor;
        accentImage.raycastTarget = false;
    }

    private static RectTransform FindRect(Transform root, string path)
    {
        Transform target = root != null ? root.Find(path) : null;
        return target != null ? target.GetComponent<RectTransform>() : null;
    }

    private static RectTransform FindRectByPaths(Transform root, params string[] paths)
    {
        Transform target = FindTransformByPaths(root, paths);
        return target != null ? target.GetComponent<RectTransform>() : null;
    }

    private static TextMeshProUGUI FindText(Transform root, string path)
    {
        Transform target = root != null ? root.Find(path) : null;
        return target != null ? target.GetComponent<TextMeshProUGUI>() : null;
    }

    private static TextMeshProUGUI FindTextByPaths(Transform root, params string[] paths)
    {
        Transform target = FindTransformByPaths(root, paths);
        return target != null ? target.GetComponent<TextMeshProUGUI>() : null;
    }

    private static Image FindImage(Transform root, string path)
    {
        Transform target = root != null ? root.Find(path) : null;
        return target != null ? target.GetComponent<Image>() : null;
    }

    private static Outline FindOutline(Transform root, string path)
    {
        Transform target = root != null ? root.Find(path) : null;
        return target != null ? target.GetComponent<Outline>() : null;
    }

    private static Button FindButton(Transform root, string path)
    {
        Transform target = root != null ? root.Find(path) : null;
        return target != null ? target.GetComponent<Button>() : null;
    }

    private static Button FindButtonByPaths(Transform root, params string[] paths)
    {
        Transform target = FindTransformByPaths(root, paths);
        return target != null ? target.GetComponent<Button>() : null;
    }

    private static ScrollRect FindScrollRect(Transform root, string path)
    {
        Transform target = root != null ? root.Find(path) : null;
        return target != null ? target.GetComponent<ScrollRect>() : null;
    }

    private static ScrollRect FindScrollRectByPaths(Transform root, params string[] paths)
    {
        Transform target = FindTransformByPaths(root, paths);
        return target != null ? target.GetComponent<ScrollRect>() : null;
    }

    private static TMP_InputField FindInputField(Transform root, string path)
    {
        Transform target = root != null ? root.Find(path) : null;
        return target != null ? target.GetComponent<TMP_InputField>() : null;
    }

    private static TMP_InputField FindInputFieldByPaths(Transform root, params string[] paths)
    {
        Transform target = FindTransformByPaths(root, paths);
        return target != null ? target.GetComponent<TMP_InputField>() : null;
    }

    private static Transform FindTransformByPaths(Transform root, params string[] paths)
    {
        if (root == null || paths == null)
        {
            return null;
        }

        for (int i = 0; i < paths.Length; i++)
        {
            string path = paths[i];
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            Transform target = root.Find(path);
            if (target != null)
            {
                return target;
            }
        }

        return null;
    }

    private static RectTransform EnsureTextArea(Transform inputFieldRoot)
    {
        Transform existing = inputFieldRoot.Find(TextAreaName);
        RectTransform textAreaRect;
        if (existing != null)
        {
            textAreaRect = existing.GetComponent<RectTransform>();
        }
        else
        {
            var textAreaObject = new GameObject(TextAreaName, typeof(RectTransform));
            textAreaRect = textAreaObject.GetComponent<RectTransform>();
            textAreaRect.SetParent(inputFieldRoot, false);
        }

        GetOrAddComponent<RectMask2D>(textAreaRect.gameObject);

        PromoteLegacyTextChild(inputFieldRoot, textAreaRect, PlaceholderName);
        PromoteLegacyTextChild(inputFieldRoot, textAreaRect, InputTextName);
        return textAreaRect;
    }

    private static void PromoteLegacyTextChild(Transform inputFieldRoot, RectTransform textAreaRect, string childName)
    {
        Transform child = inputFieldRoot.Find(childName);
        if (child == null || child.parent == textAreaRect)
        {
            return;
        }

        child.SetParent(textAreaRect, false);
    }

    private static TextMeshProUGUI EnsureTextChild(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        GameObject childObject;
        if (existing != null)
        {
            childObject = existing.gameObject;
        }
        else
        {
            childObject = new GameObject(childName, typeof(RectTransform), typeof(TextMeshProUGUI));
            childObject.transform.SetParent(parent, false);
        }

        return GetOrAddComponent<TextMeshProUGUI>(childObject);
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
        {
            component = target.AddComponent<T>();
        }

        return component;
    }

    private static void StretchToFill(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }
}
