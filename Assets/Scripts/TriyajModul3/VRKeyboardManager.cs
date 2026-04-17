using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Afet Doktoru YZ panelindeki TMP input alani icin sanal VR klavye akisini yonetir.
/// </summary>
[DisallowMultipleComponent]
public class VRKeyboardManager : MonoBehaviour
{
    private static readonly Vector2 PreferredCanvasSize = AIChatCanvasLayout.PreferredCanvasSize;
    private static readonly Vector2 MainPanelAnchorMin = AIChatCanvasLayout.MainPanelAnchorMin;
    private static readonly Vector2 MainPanelAnchorMax = AIChatCanvasLayout.MainPanelAnchorMax;
    private static readonly Vector2 KeyboardPanelAnchorMin = AIChatCanvasLayout.KeyboardPanelAnchorMin;
    private static readonly Vector2 KeyboardPanelAnchorMax = AIChatCanvasLayout.KeyboardPanelAnchorMax;
    private static readonly Color KeyboardPanelColor = AIChatCanvasLayout.KeyboardPanelColor;
    private static readonly Color KeyboardAccentColor = AIChatCanvasLayout.KeyboardAccentColor;
    private static readonly Color OverlayColor = AIChatCanvasLayout.OverlayColor;

    [Header("Target UI")]
    [SerializeField] private TMP_InputField targetInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button micButton;

    [Header("Keyboard Layout")]
    [SerializeField] private RectTransform keyboardDrawer;
    [SerializeField] private CanvasGroup keyboardCanvasGroup;
    [SerializeField] private Button dismissOverlayButton;
    [SerializeField] private RectTransform keyboardPanel;

    [Header("Animation")]
    [SerializeField] private float showDuration = 0.16f;
    [SerializeField] private float hideDuration = 0.12f;
    [SerializeField] private Vector2 shownPanelAnchoredPosition = default;
    [SerializeField] private Vector2 hiddenPanelAnchoredPosition = default;

    private Coroutine transitionRoutine;
    private Coroutine deselectRoutine;
    private Coroutine restoreInputFocusRoutine;
    private bool isVisible;
    private float suppressAutoHideUntil;
    private float keyboardInteractionGraceUntil;
    private int virtualCaretPosition;
    private bool suppressInputSelectedSync;
    private RectTransform virtualCaretIndicator;
    private Image virtualCaretImage;

    private void Awake()
    {
        AutoAssignReferences();
        ApplyAnimationPreset();
        ApplyHiddenStateInstant();
    }

    private void OnEnable()
    {
        AutoAssignReferences();
        RegisterListeners();
    }

    private void OnDisable()
    {
        UnregisterListeners();
        StopActiveRoutines();
        ApplyHiddenStateInstant();
    }

    private void OnValidate()
    {
        AutoAssignReferences();
        ApplyAnimationPreset();
    }

    private void LateUpdate()
    {
        bool shouldShowCaret = (isVisible || restoreInputFocusRoutine != null) &&
                               targetInputField != null &&
                               targetInputField.textComponent != null;

        if (!shouldShowCaret)
        {
            SetVirtualCaretVisible(false);
            return;
        }

        if (restoreInputFocusRoutine != null || suppressInputSelectedSync)
        {
            ApplyVirtualCaretToInputField();
        }
        else
        {
            CollapseTmpSelectionToCaret();
        }

        RefreshVirtualCaretIndicator();
    }

    public static VRKeyboardManager EnsureKeyboardSetup(GameObject canvasObject, TMP_InputField inputField, Button sendButtonReference)
    {
        if (canvasObject == null)
        {
            return null;
        }

        VRKeyboardManager manager = canvasObject.GetComponent<VRKeyboardManager>();
        if (manager == null)
        {
            manager = canvasObject.AddComponent<VRKeyboardManager>();
        }

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        if (canvasRect != null)
        {
            canvasRect.sizeDelta = PreferredCanvasSize;
            canvasRect.localScale = AIChatCanvasLayout.PreferredCanvasScale;
        }

        AIChatCanvasLayout.ApplyCanvasLayout(canvasObject);

        RectTransform mainPanelRect = FindChildRect(canvasObject.transform, "Main_Panel");
        if (mainPanelRect != null)
        {
            mainPanelRect.anchorMin = MainPanelAnchorMin;
            mainPanelRect.anchorMax = MainPanelAnchorMax;
            mainPanelRect.offsetMin = Vector2.zero;
            mainPanelRect.offsetMax = Vector2.zero;
            mainPanelRect.SetAsLastSibling();
        }

        NormalizeMainPanelLayout(canvasObject.transform);

        Transform keyboardHost = mainPanelRect != null ? mainPanelRect.transform : canvasObject.transform;
        RectTransform drawer = EnsureKeyboardDrawer(keyboardHost, out CanvasGroup drawerCanvasGroup);
        Button overlayButton = EnsureDismissOverlay(drawer);
        RectTransform panel = EnsureKeyboardPanel(drawer);
        EnsureKeyboardRows(panel, manager);

        AIChatCanvasLayout.ApplyCanvasLayout(canvasObject);
        drawer.SetAsLastSibling();

        manager.Configure(inputField, sendButtonReference, drawer, drawerCanvasGroup, overlayButton, panel);
        manager.SyncReferences(inputField, sendButtonReference);
        manager.ApplyHiddenStateInstant();
        return manager;
    }

    public void Configure(
        TMP_InputField inputField,
        Button sendButtonReference,
        RectTransform drawer,
        CanvasGroup drawerCanvasGroup,
        Button overlayButton,
        RectTransform panel)
    {
        targetInputField = inputField;
        sendButton = sendButtonReference;
        keyboardDrawer = drawer;
        keyboardCanvasGroup = drawerCanvasGroup;
        dismissOverlayButton = overlayButton;
        keyboardPanel = panel;
        ApplyAnimationPreset();
        ApplyVirtualKeyboardInputMode();
        SyncVirtualCaretWithInput();
    }

    public void SyncReferences(TMP_InputField inputField, Button sendButtonReference)
    {
        bool shouldRebind = isActiveAndEnabled;
        if (shouldRebind)
        {
            UnregisterListeners();
        }

        targetInputField = inputField;
        sendButton = sendButtonReference;
        AutoAssignReferences();
        ApplyAnimationPreset();
        ApplyVirtualKeyboardInputMode();
        SyncVirtualCaretWithInput();

        if (shouldRebind)
        {
            RegisterListeners();
        }
    }

    /// <summary>
    /// Klavyeyi acar ve raycast alacak hale getirir.
    /// </summary>
    public void ShowKeyboard()
    {
        AutoAssignReferences();
        if (keyboardDrawer == null || keyboardCanvasGroup == null || keyboardPanel == null)
        {
            return;
        }

        ExtendKeyboardVisibility(0.18f);
        isVisible = true;

        keyboardDrawer.gameObject.SetActive(true);
        keyboardCanvasGroup.interactable = true;
        keyboardCanvasGroup.blocksRaycasts = true;
        RefreshVirtualCaretIndicator();

        StartTransition(1f, shownPanelAnchoredPosition, showDuration, true);
    }

    public void NotifyKeyboardPointerInteraction()
    {
        ExtendKeyboardVisibility(0.75f);
        isVisible = true;

        if (deselectRoutine != null)
        {
            StopCoroutine(deselectRoutine);
            deselectRoutine = null;
        }
    }

    public bool IsKeyboardTarget(GameObject candidate)
    {
        return candidate != null && IsWithin(candidate.transform, keyboardPanel);
    }

    /// <summary>
    /// Klavyeyi gizler ancak secili objeyi zorla sifirlamaz.
    /// </summary>
    public void HideKeyboard()
    {
        HideKeyboardInternal(false);
    }

    /// <summary>
    /// Harf veya rakam tusundan gelen karakteri hedef input alana yazar.
    /// </summary>
    public void HandleCharacter(string character)
    {
        if (string.IsNullOrEmpty(character) || targetInputField == null)
        {
            return;
        }

        PrepareForKeyboardInput();
        ReplaceSelection(character);
    }

    public void HandleSpace()
    {
        HandleCharacter(" ");
    }

    public void HandleBackspace()
    {
        if (targetInputField == null)
        {
            return;
        }

        PrepareForKeyboardInput();

        string currentText = targetInputField.text ?? string.Empty;
        int caret = Mathf.Clamp(virtualCaretPosition, 0, currentText.Length);
        if (caret <= 0)
        {
            FocusInputField(caret, caret);
            return;
        }

        string newText = currentText.Remove(caret - 1, 1);
        int newCaret = caret - 1;
        ApplyTextAndCaret(newText, newCaret, newCaret);
    }

    public void HandleClearAll()
    {
        if (targetInputField == null)
        {
            return;
        }

        PrepareForKeyboardInput();
        ApplyTextAndCaret(string.Empty, 0, 0);
    }

    public void HandleMoveCaretLeft()
    {
        if (targetInputField == null)
        {
            return;
        }

        PrepareForKeyboardInput();
        virtualCaretPosition = Mathf.Max(0, virtualCaretPosition - 1);
        FocusInputField(virtualCaretPosition, virtualCaretPosition);
    }

    public void HandleMoveCaretRight()
    {
        if (targetInputField == null)
        {
            return;
        }

        PrepareForKeyboardInput();
        int textLength = (targetInputField.text ?? string.Empty).Length;
        virtualCaretPosition = Mathf.Min(textLength, virtualCaretPosition + 1);
        FocusInputField(virtualCaretPosition, virtualCaretPosition);
    }

    public void HandleEnter()
    {
        PrepareForKeyboardInput();

        if (sendButton != null && sendButton.interactable)
        {
            sendButton.onClick.Invoke();
        }

        virtualCaretPosition = 0;

        StartCoroutine(RestoreInputFocusNextFrame());
    }

    public void HandleClose()
    {
        HideKeyboardInternal(true);
    }

    public void ReleaseInputFocusForExternalUpdate()
    {
        if (restoreInputFocusRoutine != null)
        {
            StopCoroutine(restoreInputFocusRoutine);
            restoreInputFocusRoutine = null;
        }

        if (deselectRoutine != null)
        {
            StopCoroutine(deselectRoutine);
            deselectRoutine = null;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && targetInputField != null && eventSystem.currentSelectedGameObject == targetInputField.gameObject)
        {
            eventSystem.SetSelectedGameObject(null);
        }

        if (targetInputField != null)
        {
            if (targetInputField.isFocused)
            {
                targetInputField.DeactivateInputField(true);
            }

            targetInputField.ReleaseSelection();
            targetInputField.ForceLabelUpdate();
            virtualCaretPosition = Mathf.Clamp(targetInputField.text != null ? targetInputField.text.Length : 0, 0, int.MaxValue);
        }

        SetVirtualCaretVisible(false);
    }

    private void HandleInputSelected(string _)
    {
        if (!suppressInputSelectedSync)
        {
            SyncVirtualCaretWithInput();
        }
        else
        {
            ApplyVirtualCaretToInputField();
            RefreshVirtualCaretIndicator();
        }

        ShowKeyboard();
    }

    private void HandleInputDeselected(string _)
    {
        if (!isVisible || !isActiveAndEnabled)
        {
            return;
        }

        if (deselectRoutine != null)
        {
            StopCoroutine(deselectRoutine);
        }

        deselectRoutine = StartCoroutine(EvaluateDeselectNextFrame());
    }

    private IEnumerator EvaluateDeselectNextFrame()
    {
        yield return null;

        if (!isVisible || IsAutoHideSuppressed())
        {
            yield break;
        }

        if (ShouldRemainVisible())
        {
            yield break;
        }

        HideKeyboardInternal(false);
    }

    private bool ShouldRemainVisible()
    {
        if (IsAutoHideSuppressed() || restoreInputFocusRoutine != null)
        {
            return true;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
        {
            return false;
        }

        GameObject selected = eventSystem.currentSelectedGameObject;
        return IsWithin(selected.transform, targetInputField != null ? targetInputField.transform : null) ||
               IsWithin(selected.transform, keyboardPanel) ||
               IsWithin(selected.transform, micButton != null ? micButton.transform : null) ||
               IsWithin(selected.transform, sendButton != null ? sendButton.transform : null);
    }

    private static bool IsWithin(Transform current, Transform root)
    {
        if (current == null || root == null)
        {
            return false;
        }

        return current == root || current.IsChildOf(root);
    }

    private void PrepareForKeyboardInput()
    {
        ExtendKeyboardVisibility(0.75f);
        if (deselectRoutine != null)
        {
            StopCoroutine(deselectRoutine);
            deselectRoutine = null;
        }

        if (restoreInputFocusRoutine != null)
        {
            StopCoroutine(restoreInputFocusRoutine);
            restoreInputFocusRoutine = null;
        }

        if (targetInputField != null)
        {
            int textLength = (targetInputField.text ?? string.Empty).Length;
            virtualCaretPosition = Mathf.Clamp(virtualCaretPosition, 0, textLength);
            DetachTmpInputSelection();
        }
    }

    private void ReplaceSelection(string insertedText)
    {
        string currentText = targetInputField.text ?? string.Empty;
        int selectionStart;
        int selectionEnd;
        GetSelectionBounds(currentText, out selectionStart, out selectionEnd);

        string updated = currentText.Substring(0, selectionStart) +
                         insertedText +
                         currentText.Substring(selectionEnd);

        int caret = selectionStart + insertedText.Length;
        ApplyTextAndCaret(updated, caret, caret);
    }

    private void GetSelectionBounds(string currentText, out int selectionStart, out int selectionEnd)
    {
        int textLength = currentText.Length;
        int caret = Mathf.Clamp(virtualCaretPosition, 0, textLength);
        selectionStart = caret;
        selectionEnd = caret;
    }

    private void ApplyTextAndCaret(string updatedText, int anchor, int focus)
    {
        targetInputField.text = updatedText;
        targetInputField.ForceLabelUpdate();
        virtualCaretPosition = Mathf.Clamp(focus, 0, updatedText != null ? updatedText.Length : 0);
        ApplyVirtualCaretToInputField();
        RefreshVirtualCaretIndicator();
    }

    private void FocusInputField(int anchorPosition, int focusPosition)
    {
        if (targetInputField == null)
        {
            return;
        }

        string currentText = targetInputField.text ?? string.Empty;
        virtualCaretPosition = Mathf.Clamp(focusPosition, 0, currentText.Length);
        ApplyVirtualCaretToInputField();
        RefreshVirtualCaretIndicator();
    }

    private void DetachTmpInputSelection()
    {
        if (targetInputField == null)
        {
            return;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.currentSelectedGameObject == targetInputField.gameObject)
        {
            eventSystem.SetSelectedGameObject(null);
        }

        if (targetInputField.isFocused)
        {
            targetInputField.DeactivateInputField(true);
        }

        targetInputField.ReleaseSelection();
        targetInputField.ForceLabelUpdate();
    }

    private void RequestFocusInputField()
    {
        if (restoreInputFocusRoutine != null)
        {
            StopCoroutine(restoreInputFocusRoutine);
        }

        restoreInputFocusRoutine = StartCoroutine(RestoreInputFocusNextFrame());
    }

    private IEnumerator RestoreInputFocusNextFrame()
    {
        ExtendKeyboardVisibility(0.75f);
        yield return null;
        yield return null;

        if (targetInputField == null || !isActiveAndEnabled)
        {
            restoreInputFocusRoutine = null;
            yield break;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            suppressInputSelectedSync = true;
            eventSystem.SetSelectedGameObject(targetInputField.gameObject);
        }

        Canvas.ForceUpdateCanvases();
        targetInputField.ForceLabelUpdate();
        targetInputField.Select();
        targetInputField.ActivateInputField();
        if (targetInputField.isFocused)
        {
            ApplyVirtualCaretToInputField();
        }
        ShowKeyboard();
        suppressInputSelectedSync = false;
        restoreInputFocusRoutine = null;
    }

    private void ExtendKeyboardVisibility(float durationSeconds)
    {
        float until = Time.unscaledTime + Mathf.Max(0f, durationSeconds);
        suppressAutoHideUntil = Mathf.Max(suppressAutoHideUntil, until);
        keyboardInteractionGraceUntil = Mathf.Max(keyboardInteractionGraceUntil, until);
    }

    private bool IsAutoHideSuppressed()
    {
        float keepVisibleUntil = Mathf.Max(suppressAutoHideUntil, keyboardInteractionGraceUntil);
        return Time.unscaledTime < keepVisibleUntil;
    }

    private void ApplyVirtualCaretToInputField()
    {
        if (targetInputField == null)
        {
            return;
        }

        string currentText = targetInputField.text ?? string.Empty;
        int caret = Mathf.Clamp(virtualCaretPosition, 0, currentText.Length);

        targetInputField.ReleaseSelection();
        targetInputField.stringPosition = caret;
        targetInputField.selectionStringAnchorPosition = caret;
        targetInputField.selectionStringFocusPosition = caret;
        targetInputField.caretPosition = caret;
        targetInputField.selectionAnchorPosition = caret;
        targetInputField.selectionFocusPosition = caret;
    }

    private void CollapseTmpSelectionToCaret()
    {
        if (targetInputField == null)
        {
            return;
        }

        string currentText = targetInputField.text ?? string.Empty;
        int textLength = currentText.Length;

        bool inputIsSelected = targetInputField.isFocused;
        EventSystem eventSystem = EventSystem.current;
        if (!inputIsSelected && eventSystem != null)
        {
            inputIsSelected = eventSystem.currentSelectedGameObject == targetInputField.gameObject;
        }

        if (!inputIsSelected)
        {
            return;
        }

        int preferredCaret = GetResolvedTmpCaretPosition(textLength);
        virtualCaretPosition = preferredCaret;
        ApplyVirtualCaretToInputField();
    }

    private int GetResolvedTmpCaretPosition(int textLength)
    {
        int stringPosition = Mathf.Clamp(targetInputField.stringPosition, 0, textLength);
        int caretPosition = Mathf.Clamp(targetInputField.caretPosition, 0, textLength);
        int selectionAnchor = Mathf.Clamp(targetInputField.selectionStringAnchorPosition, 0, textLength);
        int selectionFocus = Mathf.Clamp(targetInputField.selectionStringFocusPosition, 0, textLength);

        bool hasSelection = selectionAnchor != selectionFocus ||
                            targetInputField.selectionAnchorPosition != targetInputField.selectionFocusPosition;

        if (hasSelection)
        {
            return selectionAnchor;
        }

        if (stringPosition != virtualCaretPosition)
        {
            return stringPosition;
        }

        if (caretPosition != virtualCaretPosition)
        {
            return caretPosition;
        }

        return Mathf.Clamp(virtualCaretPosition, 0, textLength);
    }

    private void EnsureVirtualCaretIndicator()
    {
        if (targetInputField == null || targetInputField.textComponent == null)
        {
            return;
        }

        if (virtualCaretIndicator != null && virtualCaretImage != null)
        {
            return;
        }

        Transform existing = targetInputField.textComponent.transform.Find("Virtual_Caret");
        GameObject indicatorObject;
        if (existing != null)
        {
            indicatorObject = existing.gameObject;
        }
        else
        {
            indicatorObject = new GameObject("Virtual_Caret", typeof(RectTransform), typeof(Image));
            indicatorObject.transform.SetParent(targetInputField.textComponent.transform, false);
        }

        virtualCaretIndicator = indicatorObject.GetComponent<RectTransform>();
        virtualCaretImage = indicatorObject.GetComponent<Image>();

        virtualCaretIndicator.anchorMin = new Vector2(0.5f, 0.5f);
        virtualCaretIndicator.anchorMax = new Vector2(0.5f, 0.5f);
        virtualCaretIndicator.pivot = new Vector2(0f, 0f);
        virtualCaretIndicator.localScale = Vector3.one;
        virtualCaretIndicator.sizeDelta = new Vector2(3f, 28f);
        virtualCaretIndicator.SetAsLastSibling();

        virtualCaretImage.color = KeyboardAccentColor;
        virtualCaretImage.raycastTarget = false;
    }

    private void RefreshVirtualCaretIndicator()
    {
        EnsureVirtualCaretIndicator();
        if (virtualCaretIndicator == null || virtualCaretImage == null || targetInputField == null || targetInputField.textComponent == null)
        {
            return;
        }

        TMP_Text textComponent = targetInputField.textComponent;
        Canvas.ForceUpdateCanvases();
        textComponent.ForceMeshUpdate();

        string currentText = targetInputField.text ?? string.Empty;
        int caret = Mathf.Clamp(virtualCaretPosition, 0, currentText.Length);

        RectTransform textRect = textComponent.rectTransform;
        Vector3 localPosition;
        float caretHeight;

        TMP_TextInfo textInfo = textComponent.textInfo;
        if (textInfo == null || textInfo.characterCount <= 0 || currentText.Length == 0)
        {
            float fallbackHeight = Mathf.Max(24f, textComponent.fontSize * 1.2f);
            localPosition = new Vector3(textRect.rect.xMin + 2f, textRect.rect.yMax - fallbackHeight, 0f);
            caretHeight = fallbackHeight;
        }
        else
        {
            int characterIndex = Mathf.Clamp(caret, 0, textInfo.characterCount - 1);
            TMP_CharacterInfo characterInfo = textInfo.characterInfo[characterIndex];
            int lineIndex = Mathf.Clamp(characterInfo.lineNumber, 0, textInfo.lineCount - 1);

            if (caret >= textInfo.characterCount)
            {
                TMP_CharacterInfo lastCharacter = textInfo.characterInfo[textInfo.characterCount - 1];
                lineIndex = Mathf.Clamp(lastCharacter.lineNumber, 0, textInfo.lineCount - 1);
                characterInfo = lastCharacter;
                localPosition = new Vector3(lastCharacter.xAdvance, textInfo.lineInfo[lineIndex].descender, 0f);
            }
            else
            {
                localPosition = new Vector3(characterInfo.origin, textInfo.lineInfo[lineIndex].descender, 0f);
            }

            caretHeight = Mathf.Max(24f, textInfo.lineInfo[lineIndex].ascender - textInfo.lineInfo[lineIndex].descender);
        }

        virtualCaretIndicator.localPosition = localPosition;
        virtualCaretIndicator.sizeDelta = new Vector2(3f, caretHeight);
        SetVirtualCaretVisible(true);
    }

    private void SetVirtualCaretVisible(bool visible)
    {
        if (virtualCaretIndicator == null)
        {
            return;
        }

        if (virtualCaretIndicator.gameObject.activeSelf != visible)
        {
            virtualCaretIndicator.gameObject.SetActive(visible);
        }
    }

    private void SyncVirtualCaretWithInput()
    {
        if (targetInputField == null)
        {
            virtualCaretPosition = 0;
            SetVirtualCaretVisible(false);
            return;
        }

        string currentText = targetInputField.text ?? string.Empty;
        bool inputIsSelected = targetInputField.isFocused;
        EventSystem eventSystem = EventSystem.current;
        if (!inputIsSelected && eventSystem != null)
        {
            inputIsSelected = eventSystem.currentSelectedGameObject == targetInputField.gameObject;
        }

        virtualCaretPosition = inputIsSelected
            ? GetResolvedTmpCaretPosition(currentText.Length)
            : currentText.Length;

        if (inputIsSelected)
        {
            ApplyVirtualCaretToInputField();
        }

        RefreshVirtualCaretIndicator();
    }

    private void HideKeyboardInternal(bool clearInputSelection)
    {
        if (keyboardDrawer == null || keyboardCanvasGroup == null || keyboardPanel == null)
        {
            return;
        }

        isVisible = false;
        suppressAutoHideUntil = 0f;
        keyboardInteractionGraceUntil = 0f;
        SetVirtualCaretVisible(false);

        if (clearInputSelection)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(null);
            }

            if (targetInputField != null)
            {
                targetInputField.DeactivateInputField();
            }
        }

        keyboardCanvasGroup.interactable = false;
        keyboardCanvasGroup.blocksRaycasts = false;
        StartTransition(0f, hiddenPanelAnchoredPosition, hideDuration, false);
    }

    private void StartTransition(float targetAlpha, Vector2 targetPosition, float duration, bool keepDrawerActive)
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            if (keyboardCanvasGroup != null)
            {
                keyboardCanvasGroup.alpha = targetAlpha;
            }

            if (keyboardPanel != null)
            {
                keyboardPanel.anchoredPosition = targetPosition;
            }

            if (keyboardDrawer != null)
            {
                keyboardDrawer.gameObject.SetActive(keepDrawerActive);
            }

            transitionRoutine = null;
            return;
        }

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(AnimateKeyboard(targetAlpha, targetPosition, duration, keepDrawerActive));
    }

    private IEnumerator AnimateKeyboard(float targetAlpha, Vector2 targetPosition, float duration, bool keepDrawerActive)
    {
        if (keyboardDrawer == null || keyboardCanvasGroup == null || keyboardPanel == null)
        {
            yield break;
        }

        if (keepDrawerActive)
        {
            keyboardDrawer.gameObject.SetActive(true);
        }

        float startAlpha = keyboardCanvasGroup.alpha;
        Vector2 startPosition = keyboardPanel.anchoredPosition;
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            keyboardCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
            keyboardPanel.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, eased);
            yield return null;
        }

        keyboardCanvasGroup.alpha = targetAlpha;
        keyboardPanel.anchoredPosition = targetPosition;

        if (!keepDrawerActive)
        {
            keyboardDrawer.gameObject.SetActive(false);
        }

        transitionRoutine = null;
    }

    private void ApplyHiddenStateInstant()
    {
        if (keyboardPanel != null)
        {
            keyboardPanel.anchoredPosition = hiddenPanelAnchoredPosition;
        }

        if (keyboardCanvasGroup != null)
        {
            keyboardCanvasGroup.alpha = 0f;
            keyboardCanvasGroup.interactable = false;
            keyboardCanvasGroup.blocksRaycasts = false;
        }

        if (keyboardDrawer != null)
        {
            keyboardDrawer.gameObject.SetActive(false);
        }

        isVisible = false;
        SetVirtualCaretVisible(false);
    }

    internal static RectTransform EnsureKeyboardDrawer(Transform canvasRoot, out CanvasGroup drawerCanvasGroup)
    {
        Transform existing = canvasRoot.Find("Keyboard_Drawer");
        if (existing == null && canvasRoot.parent != null)
        {
            Transform legacyDrawer = canvasRoot.parent.Find("Keyboard_Drawer");
            if (legacyDrawer != null)
            {
                existing = legacyDrawer;
                existing.SetParent(canvasRoot, false);
            }
        }

        GameObject drawerObject = existing != null
            ? existing.gameObject
            : FindOrCreateChild(canvasRoot, "Keyboard_Drawer", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform drawer = drawerObject.GetComponent<RectTransform>();
        StretchToFill(drawer);
        drawer.SetAsLastSibling();

        drawerCanvasGroup = GetOrAddComponent<CanvasGroup>(drawerObject);
        drawerCanvasGroup.alpha = 0f;
        drawerCanvasGroup.interactable = false;
        drawerCanvasGroup.blocksRaycasts = false;
        return drawer;
    }

    internal static Button EnsureDismissOverlay(RectTransform drawer)
    {
        GameObject overlayObject = FindOrCreateChild(drawer, "Dismiss_Overlay", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = AIChatCanvasLayout.DismissOverlayAnchorMin;
        overlayRect.anchorMax = AIChatCanvasLayout.DismissOverlayAnchorMax;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayRect.anchoredPosition = Vector2.zero;
        overlayRect.localScale = Vector3.one;
        overlayRect.SetAsFirstSibling();

        Image overlayImage = overlayObject.GetComponent<Image>();
        overlayImage.color = OverlayColor;

        Button overlayButton = overlayObject.GetComponent<Button>();
        overlayButton.transition = Selectable.Transition.None;
        return overlayButton;
    }

    internal static RectTransform EnsureKeyboardPanel(RectTransform drawer)
    {
        GameObject panelObject = FindOrCreateChild(drawer, "Keyboard_Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(Outline));
        RectTransform panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = KeyboardPanelAnchorMin;
        panel.anchorMax = KeyboardPanelAnchorMax;
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
        panel.SetAsLastSibling();

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = KeyboardPanelColor;

        Outline outline = panelObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.08f, 0.78f, 1f, 0.14f);
        outline.effectDistance = new Vector2(1f, -1f);

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = AIChatCanvasLayout.GetKeyboardPanelPadding();
        layout.spacing = AIChatCanvasLayout.KeyboardPanelSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        EnsureKeyboardAccent(panelObject.transform);

        return panel;
    }

    internal static void EnsureKeyboardRows(RectTransform keyboardPanel, VRKeyboardManager manager)
    {
        RemoveLegacyKeyboardLayout(keyboardPanel);

        EnsureCharacterRow(keyboardPanel, "Row_0_Numbers", manager, "1234567890", 1f);
        EnsureCharacterRow(keyboardPanel, "Row_1_Qwerty", manager, "QWERTYUIOP\u011E\u00DC", 1f);
        EnsureCharacterRow(keyboardPanel, "Row_2_Asdf", manager, "ASDFGHJKL\u015e\u0130", 1f);
        EnsureCharacterRow(keyboardPanel, "Row_3_Zxcv", manager, "ZXCVBNM\u00d6\u00c7", 1f);

        RectTransform controlsRow = EnsureKeyboardRow(keyboardPanel, "Row_4_Controls");
        EnsureKey(controlsRow, "Close_Key", manager, VRKeyType.Close, string.Empty, "Kapat", 1.45f);
        EnsureKey(controlsRow, "MoveLeft_Key", manager, VRKeyType.MoveLeft, string.Empty, "Sol", 1.05f);
        EnsureKey(controlsRow, "MoveRight_Key", manager, VRKeyType.MoveRight, string.Empty, "Sag", 1.05f);
        EnsureKey(controlsRow, "Space_Key", manager, VRKeyType.Space, " ", "Bo\u015fluk", 4.15f);
        EnsureKey(controlsRow, "Backspace_Key", manager, VRKeyType.Backspace, string.Empty, "Sil", 1.45f);
        EnsureKey(controlsRow, "Clear_Key", manager, VRKeyType.Clear, string.Empty, "Temiz", 1.45f);
        EnsureKey(controlsRow, "Enter_Key", manager, VRKeyType.Enter, string.Empty, "Enter", 1.65f);
    }

    private static void RemoveLegacyKeyboardLayout(RectTransform keyboardPanel)
    {
        if (keyboardPanel == null)
        {
            return;
        }

        RemoveLegacyChild(keyboardPanel, "Row_4_Space");
        RemoveLegacyRowKey(keyboardPanel, "Row_2_Asdf", "Backspace_Key");
        RemoveLegacyRowKey(keyboardPanel, "Row_3_Zxcv", "Close_Key");
        RemoveLegacyRowKey(keyboardPanel, "Row_3_Zxcv", "Enter_Key");
    }

    private static void RemoveLegacyRowKey(RectTransform keyboardPanel, string rowName, string keyName)
    {
        Transform row = keyboardPanel.Find(rowName);
        if (row == null)
        {
            return;
        }

        RemoveLegacyChild(row, keyName);
    }

    private static void RemoveLegacyChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            return;
        }

        DestroyGameObject(child.gameObject);
    }

    private static void EnsureCharacterRow(Transform parent, string rowName, VRKeyboardManager manager, string characters, float widthWeight)
    {
        RectTransform row = EnsureKeyboardRow(parent, rowName);
        EnsureCharacterKeys(row, manager, characters, widthWeight);
    }

    private static void EnsureCharacterKeys(Transform row, VRKeyboardManager manager, string characters, float widthWeight)
    {
        foreach (char character in characters)
        {
            string value = character.ToString();
            EnsureKey(row, value + "_Key", manager, VRKeyType.Character, value, value, widthWeight);
        }
    }

    private static RectTransform EnsureKeyboardRow(Transform parent, string rowName)
    {
        GameObject rowObject = FindOrCreateChild(parent, rowName, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        RectTransform row = rowObject.GetComponent<RectTransform>();

        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = AIChatCanvasLayout.KeyboardPanelSpacing;
        layout.padding = ResolveRowPadding(rowName);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = AIChatCanvasLayout.KeyboardRowHeight;
        layoutElement.flexibleHeight = 1f;
        layoutElement.flexibleWidth = 1f;

        return row;
    }

    private static void EnsureKey(
        Transform parent,
        string objectName,
        VRKeyboardManager manager,
        VRKeyType keyType,
        string value,
        string labelText,
        float widthWeight)
    {
        GameObject keyObject = FindOrCreateChild(parent, objectName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(VRKey));
        Image keyImage = keyObject.GetComponent<Image>();
        keyImage.color = ResolveKeyColor(keyType);

        Outline keyOutline = GetOrAddComponent<Outline>(keyObject);
        keyOutline.effectColor = AIChatCanvasLayout.GetKeyboardKeyOutlineColor(keyType);
        keyOutline.effectDistance = new Vector2(1f, -1f);

        Button keyButton = keyObject.GetComponent<Button>();
        keyButton.transition = Selectable.Transition.None;

        LayoutElement layoutElement = keyObject.GetComponent<LayoutElement>();
        layoutElement.flexibleWidth = 0f;
        layoutElement.preferredWidth = AIChatCanvasLayout.GetKeyboardKeyWidth(widthWeight);
        layoutElement.preferredHeight = AIChatCanvasLayout.KeyboardKeyHeight;
        layoutElement.minWidth = AIChatCanvasLayout.KeyboardKeyMinWidth;

        GameObject labelObject = FindOrCreateChild(keyObject.transform, "Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        StretchToFill(
            labelRect,
            new Vector2(AIChatCanvasLayout.KeyboardLabelPaddingHorizontal, AIChatCanvasLayout.KeyboardLabelPaddingVertical),
            new Vector2(-AIChatCanvasLayout.KeyboardLabelPaddingHorizontal, -AIChatCanvasLayout.KeyboardLabelPaddingVertical));

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = AIChatCanvasLayout.GetKeyboardLabelFontSize(keyType);
        label.color = AIChatCanvasLayout.GetKeyboardLabelColor(keyType);
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.raycastTarget = false;

        VRKey key = keyObject.GetComponent<VRKey>();
        key.Configure(manager, keyType, value, labelText);
    }

    private static Color ResolveKeyColor(VRKeyType keyType)
    {
        return AIChatCanvasLayout.GetKeyboardKeyColor(keyType);
    }

    private static void EnsureKeyboardAccent(Transform panelRoot)
    {
        GameObject accentObject = FindOrCreateChild(panelRoot, AIChatCanvasLayout.KeyboardAccentName, typeof(RectTransform), typeof(Image));
        RectTransform accentRect = accentObject.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.sizeDelta = new Vector2(0f, AIChatCanvasLayout.KeyboardAccentHeight);
        accentRect.anchoredPosition = new Vector2(0f, -8f);
        accentRect.SetAsFirstSibling();

        Image accentImage = accentObject.GetComponent<Image>();
        accentImage.color = KeyboardAccentColor;
        accentImage.raycastTarget = false;
    }

    private static RectOffset ResolveRowPadding(string rowName)
    {
        return AIChatCanvasLayout.GetKeyboardRowPadding(rowName);
    }

    private static void NormalizeMainPanelLayout(Transform canvasRoot)
    {
        AIChatCanvasLayout.ApplyCanvasLayout(canvasRoot.gameObject);

        RectTransform scrollRect = FindChildRectByPaths(
            canvasRoot,
            "Main_Panel/" + AIChatCanvasLayout.AIChatRootName + "/Chat_ScrollView",
            "Main_Panel/Chat_ScrollView");
        if (scrollRect != null)
        {
            scrollRect.anchorMin = AIChatCanvasLayout.ScrollAnchorMin;
            scrollRect.anchorMax = AIChatCanvasLayout.ScrollAnchorMax;
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;
        }

        RectTransform inputContainerRect = FindChildRectByPaths(
            canvasRoot,
            "Main_Panel/" + AIChatCanvasLayout.AIChatRootName + "/Input_Container",
            "Main_Panel/Input_Container");
        if (inputContainerRect != null)
        {
            inputContainerRect.anchorMin = AIChatCanvasLayout.InputAnchorMin;
            inputContainerRect.anchorMax = AIChatCanvasLayout.InputAnchorMax;
            inputContainerRect.offsetMin = Vector2.zero;
            inputContainerRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup inputLayoutGroup = inputContainerRect.GetComponent<HorizontalLayoutGroup>();
            if (inputLayoutGroup != null)
            {
                inputLayoutGroup.spacing = AIChatCanvasLayout.InputSpacing;
                inputLayoutGroup.padding = new RectOffset(10, 10, 8, 8);
            }
        }

        RectTransform inputFieldRect = FindChildRectByPaths(
            canvasRoot,
            "Main_Panel/" + AIChatCanvasLayout.AIChatRootName + "/Input_Container/User_InputField",
            "Main_Panel/Input_Container/User_InputField");
        if (inputFieldRect != null)
        {
            LayoutElement inputLayout = inputFieldRect.GetComponent<LayoutElement>();
            if (inputLayout != null)
            {
                inputLayout.preferredHeight = AIChatCanvasLayout.InputPreferredHeight;
            }
        }

        RectTransform sendButtonRect = FindChildRectByPaths(
            canvasRoot,
            "Main_Panel/" + AIChatCanvasLayout.AIChatRootName + "/Input_Container/Send_Button",
            "Main_Panel/Input_Container/Send_Button");
        if (sendButtonRect != null)
        {
            LayoutElement sendLayout = sendButtonRect.GetComponent<LayoutElement>();
            if (sendLayout != null)
            {
                sendLayout.preferredWidth = AIChatCanvasLayout.SendButtonPreferredWidth;
                sendLayout.preferredHeight = AIChatCanvasLayout.SendButtonPreferredHeight;
            }
        }

        RectTransform closeButtonRect = FindChildRect(canvasRoot, "Main_Panel/Close_Button");
        if (closeButtonRect != null)
        {
            closeButtonRect.sizeDelta = AIChatCanvasLayout.CloseButtonSize;
            closeButtonRect.anchoredPosition = AIChatCanvasLayout.CloseButtonPosition;
        }
    }

    private static RectTransform FindChildRect(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<RectTransform>() : null;
    }

    private static RectTransform FindChildRectByPaths(Transform parent, params string[] paths)
    {
        Transform child = FindChildByPaths(parent, paths);
        return child != null ? child.GetComponent<RectTransform>() : null;
    }

    private static Transform FindChildByPaths(Transform parent, params string[] paths)
    {
        if (parent == null || paths == null)
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

            Transform child = parent.Find(path);
            if (child != null)
            {
                return child;
            }
        }

        return null;
    }

    private static GameObject FindOrCreateChild(Transform parent, string childName, params Type[] componentTypes)
    {
        Transform existing = parent.Find(childName);
        GameObject childObject = existing != null ? existing.gameObject : new GameObject(childName, componentTypes);
        if (existing == null)
        {
            childObject.transform.SetParent(parent, false);
        }

        foreach (Type componentType in componentTypes)
        {
            if (childObject.GetComponent(componentType) == null)
            {
                childObject.AddComponent(componentType);
            }
        }

        return childObject;
    }

    private static void DestroyGameObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
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

    private static void StretchToFill(RectTransform rectTransform)
    {
        StretchToFill(rectTransform, Vector2.zero, Vector2.zero);
    }

    private static void StretchToFill(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private void ApplyVirtualKeyboardInputMode()
    {
        if (targetInputField == null)
        {
            return;
        }

        // Sadece fiziksel klavye karakterlerini engelle, mic ve sanal klavye calismali
        targetInputField.readOnly = false;
        targetInputField.shouldHideMobileInput = true;
        targetInputField.customCaretColor = true;
        targetInputField.caretColor = new Color(0f, 0f, 0f, 0f);
        targetInputField.selectionColor = new Color(0.06f, 0.84f, 1f, 0.06f);
        targetInputField.resetOnDeActivation = true;
        targetInputField.restoreOriginalTextOnEscape = false;
        targetInputField.onValidateInput = BlockHardwareKeyboardCharacter;
    }

    private char BlockHardwareKeyboardCharacter(string text, int charIndex, char addedChar)
    {
        return '\0';
    }

    private void AutoAssignReferences()
    {
        ApplyAnimationPreset();

        if (targetInputField == null)
        {
            targetInputField = GetComponentInChildren<TMP_InputField>(true);
        }

        if (sendButton == null)
        {
            Transform sendTransform = FindChildByPaths(
                transform,
                "Main_Panel/" + AIChatCanvasLayout.AIChatRootName + "/Input_Container/Send_Button",
                "Main_Panel/Input_Container/Send_Button");
            if (sendTransform != null)
            {
                sendButton = sendTransform.GetComponent<Button>();
            }
        }

        if (micButton == null)
        {
            Transform micTransform = FindChildByPaths(
                transform,
                "Main_Panel/" + AIChatCanvasLayout.AIChatRootName + "/Input_Container/Mic_Button",
                "Main_Panel/Input_Container/Mic_Button");
            if (micTransform != null)
            {
                micButton = micTransform.GetComponent<Button>();
            }
        }

        if (keyboardDrawer == null)
        {
            Transform drawerTransform = transform.Find("Main_Panel/Keyboard_Drawer");
            if (drawerTransform == null)
            {
                drawerTransform = transform.Find("Keyboard_Drawer");
            }
            if (drawerTransform != null)
            {
                keyboardDrawer = drawerTransform.GetComponent<RectTransform>();
            }
        }

        if (keyboardCanvasGroup == null && keyboardDrawer != null)
        {
            keyboardCanvasGroup = keyboardDrawer.GetComponent<CanvasGroup>();
        }

        if (keyboardPanel == null && keyboardDrawer != null)
        {
            Transform panelTransform = keyboardDrawer.Find("Keyboard_Panel");
            if (panelTransform != null)
            {
                keyboardPanel = panelTransform.GetComponent<RectTransform>();
            }
        }

        if (dismissOverlayButton == null && keyboardDrawer != null)
        {
            Transform overlayTransform = keyboardDrawer.Find("Dismiss_Overlay");
            if (overlayTransform != null)
            {
                dismissOverlayButton = overlayTransform.GetComponent<Button>();
            }
        }

        ApplyVirtualKeyboardInputMode();
    }

    private void ApplyAnimationPreset()
    {
        shownPanelAnchoredPosition = AIChatCanvasLayout.KeyboardShownPosition;
        hiddenPanelAnchoredPosition = AIChatCanvasLayout.KeyboardHiddenPosition;
    }

    private void RegisterListeners()
    {
        ApplyVirtualKeyboardInputMode();

        if (targetInputField != null)
        {
            targetInputField.onSelect.RemoveListener(HandleInputSelected);
            targetInputField.onSelect.AddListener(HandleInputSelected);
            targetInputField.onDeselect.RemoveListener(HandleInputDeselected);
            targetInputField.onDeselect.AddListener(HandleInputDeselected);
        }

        if (dismissOverlayButton != null)
        {
            dismissOverlayButton.onClick.RemoveListener(HandleClose);
            dismissOverlayButton.onClick.AddListener(HandleClose);
        }
    }

    private void UnregisterListeners()
    {
        if (targetInputField != null)
        {
            targetInputField.onSelect.RemoveListener(HandleInputSelected);
            targetInputField.onDeselect.RemoveListener(HandleInputDeselected);
        }

        if (dismissOverlayButton != null)
        {
            dismissOverlayButton.onClick.RemoveListener(HandleClose);
        }
    }

    private void StopActiveRoutines()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        if (deselectRoutine != null)
        {
            StopCoroutine(deselectRoutine);
            deselectRoutine = null;
        }

        if (restoreInputFocusRoutine != null)
        {
            StopCoroutine(restoreInputFocusRoutine);
            restoreInputFocusRoutine = null;
        }
    }
}
