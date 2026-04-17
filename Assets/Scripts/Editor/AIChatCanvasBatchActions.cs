#if UNITY_EDITOR
using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AIChatCanvasBatchActions
{
    private const string Modul3TriyajScenePath =
        "Assets/Samples/XR Interaction Toolkit/2.6.5/XR Device Simulator/Scenes/Mod\u00FCl3_Triyaj.unity";

    [MenuItem("Tools/Triyaj/Refresh Doctor AI Chat Layout")]
    public static void RefreshModul3TriyajDoctorCanvas()
    {
        var scene = EditorSceneManager.OpenScene(Modul3TriyajScenePath, OpenSceneMode.Single);
        var manager = UnityEngine.Object.FindObjectOfType<AIManager>(true);
        if (manager == null)
        {
            throw new InvalidOperationException(
                $"No {nameof(AIManager)} found in scene '{Modul3TriyajScenePath}'.");
        }

        GameObject canvasObject = manager.aiCanvas;
        if (canvasObject == null)
        {
            GameObject fallbackCanvas = GameObject.Find(AIChatCanvasLayout.CanvasRootName);
            canvasObject = fallbackCanvas;
        }

        if (canvasObject == null)
        {
            throw new InvalidOperationException(
                $"No '{AIChatCanvasLayout.CanvasRootName}' found in scene '{Modul3TriyajScenePath}'.");
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            EditorUtility.SetDirty(canvas);
        }

        AIChatCanvasLayout.ApplyCanvasLayout(canvasObject);
        InvokePrivateMethod(manager, "EnsureCloseButtonExists");
        InvokePrivateMethod(manager, "AutoAssignDoctorPanelReferences");

        TMP_InputField inputField = FindComponentByName<TMP_InputField>(canvasObject.transform, AIChatCanvasLayout.InputFieldName) ??
                                   manager.userInputField;
        Button sendButton = FindComponentByName<Button>(canvasObject.transform, AIChatCanvasLayout.SendButtonName) ??
                            manager.sendButton;
        Button closeButton = FindComponentByName<Button>(canvasObject.transform, AIChatCanvasLayout.CloseButtonName) ??
                             manager.closeButton;
        ScrollRect scrollRect = FindComponentByName<ScrollRect>(canvasObject.transform, AIChatCanvasLayout.ScrollViewName) ??
                                manager.chatScrollRect;
        TextMeshProUGUI chatText = FindComponentByName<TextMeshProUGUI>(canvasObject.transform, AIChatCanvasLayout.ChatHistoryName) ??
                                   manager.chatHistoryText;
        Button aiTabButton = FindComponentByName<Button>(canvasObject.transform, AIChatCanvasLayout.TabAIButtonName) ??
                             manager.aiTabButton;
        Button miniTestTabButton = FindComponentByName<Button>(canvasObject.transform, AIChatCanvasLayout.TabMiniTestButtonName) ??
                                   manager.miniTestTabButton;
        GameObject aiChatRoot = FindTransformByName(canvasObject.transform, AIChatCanvasLayout.AIChatRootName)?.gameObject ??
                                manager.aiChatRoot;
        GameObject miniTestRoot = FindTransformByName(canvasObject.transform, AIChatCanvasLayout.MiniTestRootName)?.gameObject ??
                                  manager.miniTestRoot;
        TextMeshProUGUI miniTestQuestionText = FindComponentByName<TextMeshProUGUI>(canvasObject.transform, AIChatCanvasLayout.MiniTestQuestionTextName) ??
                                               manager.miniTestQuestionText;
        TextMeshProUGUI miniTestProgressText = FindComponentByName<TextMeshProUGUI>(canvasObject.transform, AIChatCanvasLayout.MiniTestProgressName) ??
                                               manager.miniTestProgressText;
        TextMeshProUGUI miniTestResultText = FindComponentByName<TextMeshProUGUI>(canvasObject.transform, AIChatCanvasLayout.MiniTestResultTextName) ??
                                             manager.miniTestResultText;
        Button miniTestNextButton = FindComponentByName<Button>(canvasObject.transform, AIChatCanvasLayout.MiniTestNextButtonName) ??
                                    manager.miniTestNextButton;
        Button miniTestRestartButton = FindComponentByName<Button>(canvasObject.transform, AIChatCanvasLayout.MiniTestRestartButtonName) ??
                                       manager.miniTestRestartButton;
        Button[] optionButtons =
        {
            FindComponentByName<Button>(canvasObject.transform, AIChatCanvasLayout.OptionAButtonName) ??
                GetArrayItem(manager.miniTestOptionButtons, 0),
            FindComponentByName<Button>(canvasObject.transform, AIChatCanvasLayout.OptionBButtonName) ??
                GetArrayItem(manager.miniTestOptionButtons, 1),
            FindComponentByName<Button>(canvasObject.transform, AIChatCanvasLayout.OptionCButtonName) ??
                GetArrayItem(manager.miniTestOptionButtons, 2)
        };
        TextMeshProUGUI[] optionLabels =
        {
            FindComponentByName<TextMeshProUGUI>(canvasObject.transform, AIChatCanvasLayout.OptionALabelName) ??
                GetArrayItem(manager.miniTestOptionLabelTexts, 0),
            FindComponentByName<TextMeshProUGUI>(canvasObject.transform, AIChatCanvasLayout.OptionBLabelName) ??
                GetArrayItem(manager.miniTestOptionLabelTexts, 1),
            FindComponentByName<TextMeshProUGUI>(canvasObject.transform, AIChatCanvasLayout.OptionCLabelName) ??
                GetArrayItem(manager.miniTestOptionLabelTexts, 2)
        };

        if (inputField == null || sendButton == null || closeButton == null || scrollRect == null || chatText == null)
        {
            throw new InvalidOperationException(
                "Doctor AI chat canvas is missing one or more required UI references. " +
                "Missing: " + BuildMissingReferenceList(inputField, sendButton, closeButton, scrollRect, chatText));
        }

        VRKeyboardManager keyboardManager = VRKeyboardManager.EnsureKeyboardSetup(canvasObject, inputField, sendButton);

        manager.aiCanvas = canvasObject;
        manager.chatHistoryText = chatText;
        manager.chatScrollRect = scrollRect;
        manager.userInputField = inputField;
        manager.sendButton = sendButton;
        manager.closeButton = closeButton;
        manager.aiTabButton = aiTabButton;
        manager.miniTestTabButton = miniTestTabButton;
        manager.aiChatRoot = aiChatRoot;
        manager.miniTestRoot = miniTestRoot;
        manager.miniTestQuestionText = miniTestQuestionText;
        manager.miniTestProgressText = miniTestProgressText;
        manager.miniTestResultText = miniTestResultText;
        manager.miniTestOptionButtons = optionButtons;
        manager.miniTestOptionLabelTexts = optionLabels;
        manager.miniTestNextButton = miniTestNextButton;
        manager.miniTestRestartButton = miniTestRestartButton;

        if (AIChatCanvasLayout.ShouldUpgradeWelcomeText(chatText.text))
        {
            chatText.text = AIChatCanvasLayout.WelcomeMessage;
        }

        EditorUtility.SetDirty(canvasObject);
        EditorUtility.SetDirty(chatText);
        EditorUtility.SetDirty(inputField);
        EditorUtility.SetDirty(sendButton);
        EditorUtility.SetDirty(closeButton);
        EditorUtility.SetDirty(scrollRect);
        EditorUtility.SetDirty(manager);

        if (keyboardManager != null)
        {
            EditorUtility.SetDirty(keyboardManager);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new InvalidOperationException($"Failed to save scene '{Modul3TriyajScenePath}'.");
        }
    }

    private static T FindComponentByName<T>(Transform root, string childName) where T : Component
    {
        Transform target = FindTransformByName(root, childName);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static T GetArrayItem<T>(T[] array, int index) where T : class
    {
        if (array == null || index < 0 || index >= array.Length)
        {
            return null;
        }

        return array[index];
    }

    private static string BuildMissingReferenceList(
        TMP_InputField inputField,
        Button sendButton,
        Button closeButton,
        ScrollRect scrollRect,
        TextMeshProUGUI chatText)
    {
        string missing = string.Empty;
        if (inputField == null)
        {
            missing += nameof(inputField) + ", ";
        }

        if (sendButton == null)
        {
            missing += nameof(sendButton) + ", ";
        }

        if (closeButton == null)
        {
            missing += nameof(closeButton) + ", ";
        }

        if (scrollRect == null)
        {
            missing += nameof(scrollRect) + ", ";
        }

        if (chatText == null)
        {
            missing += nameof(chatText) + ", ";
        }

        return missing.TrimEnd(' ', ',');
    }

    private static void InvokePrivateMethod(AIManager manager, string methodName)
    {
        if (manager == null || string.IsNullOrWhiteSpace(methodName))
        {
            return;
        }

        MethodInfo method = typeof(AIManager).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method?.Invoke(manager, null);
    }

    private static Transform FindTransformByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }
}
#endif
