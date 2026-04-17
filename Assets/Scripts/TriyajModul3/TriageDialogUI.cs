using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using TrainingAnalytics;

public class TriageDialogUI : MonoBehaviour
{
    private const string MissingComplaintText = "Sikayet bilgisi bulunamadi.";
    private const string ComplaintSectionLabel = "Hasta Bildirimi";
    private const string HintTitleLabel = "Medikal Asistan";
    private const string HintSupportLabel = "Karar verdiren tek klinik bulguya odaklan.";
    private const string DecisionSectionLabel = "Triyaj Karari";
    private const string CaseCounterFallbackLabel = "Vaka";
    private const string HintButtonDefaultLabel = "Ipucu Al";
    private const string HintButtonRepeatLabel = "Farkli Ipucu";
    private const string HintLoadingLabel = "Yukleniyor...";
    private const string HintDefaultText = "Ipucu Al'a bas. Burada kisa, net ve karar verdiren klinik ipucunu goreceksin. Uzarsa paneli surukleyerek kaydirabilirsin.";
    private const int MaxHintHistoryEntries = 4;
    private const int HintDisplayMaxCharacters = 320;
    private const int CaseHintDisplayMaxCharacters = 220;

    private static readonly Color PanelColor = new Color(0.05f, 0.12f, 0.2f, 0.8f);
    private static readonly Color PanelShadowColor = new Color(0.01f, 0.04f, 0.1f, 0.22f);
    private static readonly Color AccentGlowColor = new Color(0.36f, 0.84f, 1f, 0.92f);
    private static readonly Color ComplaintBlockColor = new Color(0.08f, 0.18f, 0.28f, 0.42f);
    private static readonly Color HintSectionColor = new Color(0.04f, 0.065f, 0.11f, 0.93f);
    private static readonly Color HintViewportColor = new Color(0.04f, 0.055f, 0.095f, 0.94f);
    private static readonly Color DecisionSectionColor = new Color(0.05f, 0.085f, 0.14f, 0.64f);
    private static readonly Color HintButtonColor = AIChatCanvasLayout.SendButtonColor;
    private static readonly Color SectionAccentColor = new Color(0.18f, 0.84f, 1f, 0.92f);
    private static readonly Color TextPrimary = new Color(0.96f, 0.985f, 1f, 1f);
    private static readonly Color TextSecondary = AIChatCanvasLayout.AssistantTextColor;
    private static readonly Color TextMuted = new Color(0.73f, 0.81f, 0.9f, 0.96f);
    private static readonly Color GreenAccent = new Color(0.19f, 0.69f, 0.54f, 0.9f);
    private static readonly Color YellowAccent = new Color(0.87f, 0.72f, 0.3f, 0.9f);
    private static readonly Color RedAccent = new Color(0.8f, 0.35f, 0.37f, 0.9f);
    private static readonly Color SlateAccent = new Color(0.23f, 0.27f, 0.36f, 0.9f);

    [Header("Referanslar")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI complaintText;
    [SerializeField] private Button greenButton;
    [SerializeField] private Button yellowButton;
    [SerializeField] private Button redButton;
    [SerializeField] private Button blackButton;

    [Header("Yerlesim")]
    [SerializeField] private bool followPlayerCamera = true;
    [SerializeField] private Vector2 canvasSize = new Vector2(960f, 594f);
    [SerializeField] private float distanceFromCamera = 1.25f;
    [SerializeField] private float verticalOffset = -0.05f;
    [SerializeField] private float minimumVerticalOffset = 0.08f;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private bool showOnlyWhenInteracting = true;
    [SerializeField] private Vector2 triageButtonCellSize = new Vector2(284f, 118f);
    [SerializeField] private Vector2 triageButtonSpacing = new Vector2(20f, 18f);
    [SerializeField] private float triageButtonAnchorY = 0.23f;
    [SerializeField] private Vector2 complaintPadding = new Vector2(24f, 18f);
    [SerializeField] private float followPositionSharpness = 12f;
    [SerializeField] private float followRotationSharpness = 14f;

    [Header("Animasyon")]
    [SerializeField] private float openDuration = 0.18f;
    [SerializeField] private float closeDuration = 0.12f;
    [SerializeField] private float hiddenPanelScale = 0.92f;

    private Transform followTarget;
    private Action<TriageCategory> onCategorySelected;
    private RectTransform panelRect;
    private TextMeshProUGUI caseTitleText;
    private TextMeshProUGUI caseMetaText;
    private RectTransform complaintBlockRect;
    private TextMeshProUGUI complaintHeaderText;
    private RectTransform hintSectionRect;
    private RectTransform decisionSectionRect;
    private RectTransform hintViewportRect;
    private RectTransform hintContentRect;
    private RectTransform buttonContainerRect;
    private TextMeshProUGUI hintTitleText;
    private TextMeshProUGUI hintSupportText;
    private TextMeshProUGUI hintText;
    private Button hintButton;
    private TextMeshProUGUI hintButtonLabel;
    private ScrollRect hintScrollRect;
    private TextMeshProUGUI decisionTitleText;
    private bool isOpen;
    private bool isHintLoading;
    private int hintRequestCount;
    private int hintSessionId;
    private string currentComplaintText = string.Empty;
    private NPCTriageInteractable currentNpc;
    private TriageCaseProfile currentCaseProfile;
    private Color currentAccentColor = AccentGlowColor;
    private Coroutine visibilityRoutine;
    private Coroutine hintRoutine;
    private readonly List<string> hintHistoryEntries = new List<string>();

    private enum HintCategory
    {
        Respiratory,
        Bleeding,
        Consciousness,
        BurnSmoke,
        General,
        EmptyComplaint
    }

    public bool IsOpen => isOpen;

    private void Awake()
    {
        EnsureReferences();
        ApplyRuntimeLayout();
        BindButtons();

        if (showOnlyWhenInteracting)
        {
            SetVisibleImmediate(false);
        }
    }

    private void OnEnable()
    {
        EnsureReferences();
        ApplyRuntimeLayout();
        BindButtons();
    }

    private void OnDestroy()
    {
        StopHintRoutine();
        UnbindButtons();
        HospitalTriageManager.Instance?.ReleaseUiFocus(this);
    }

    private void LateUpdate()
    {
        if (!isOpen || !followPlayerCamera || followTarget == null)
        {
            return;
        }

        ApplyFollowPose(false);
    }

    public void Open(string text, Transform npcTransform, Action<TriageCategory> onSelected)
    {
        OpenInternal(text, null, npcTransform, onSelected);
    }

    public void Open(NPCTriageInteractable npc, Action<TriageCategory> onSelected)
    {
        string complaint = npc != null ? npc.ComplaintText : string.Empty;
        Transform npcTransform = npc != null ? npc.transform : null;
        OpenInternal(complaint, npc, npcTransform, onSelected);
    }

    private void OpenInternal(string text, NPCTriageInteractable npc, Transform npcTransform, Action<TriageCategory> onSelected)
    {
        currentNpc = npc;
        currentCaseProfile = npc != null ? npc.CaseProfile : null;
        currentAccentColor = ResolveCurrentAccentColor();

        EnsureReferences();
        ApplyRuntimeLayout();
        BindButtons();

        onCategorySelected = onSelected;

        bool hasComplaint = !string.IsNullOrWhiteSpace(text);
        currentComplaintText = hasComplaint ? text.Trim() : string.Empty;

        if (complaintText != null)
        {
            complaintText.text = hasComplaint ? currentComplaintText : MissingComplaintText;
        }

        RefreshCasePresentation();
        ResetHintState(currentComplaintText);

        followTarget = XRCameraHelper.GetPlayerCameraTransform();
        ApplyFollowPose(true, npcTransform);

        TrainingAnalyticsFacade.TrackEvent(
            AnalyticsEventNames.TriageDialogOpened,
            TrainingAnalyticsFacade.Module3Id,
            TrainingAnalyticsFacade.Module3Name,
            new Dictionary<string, object>
            {
                { AnalyticsParams.ScenarioId, TrainingAnalyticsFacade.Module3ScenarioId },
                { AnalyticsParams.ScenarioName, TrainingAnalyticsFacade.Module3ScenarioName },
                { AnalyticsParams.VictimId, currentNpc != null ? currentNpc.CaseId : "victim_unknown" },
                { AnalyticsParams.VictimName, currentNpc != null ? currentNpc.PatientTitle : "Bilinmeyen Hasta" }
            });

        isOpen = true;
        SetVisible(true);
    }

    public void Close()
    {
        StopHintRoutine();
        RestoreHintButtonIdleState();
        isOpen = false;
        onCategorySelected = null;
        currentNpc = null;
        currentCaseProfile = null;
        SetVisible(false);
    }

    public void SelectGreen()
    {
        RaiseCategory(TriageCategory.Green);
    }

    public void SelectYellow()
    {
        RaiseCategory(TriageCategory.Yellow);
    }

    public void SelectRed()
    {
        RaiseCategory(TriageCategory.Red);
    }

    public void SelectBlack()
    {
        RaiseCategory(TriageCategory.Black);
    }

    private void RaiseCategory(TriageCategory category)
    {
        if (!isOpen)
        {
            return;
        }

        Action<TriageCategory> callback = onCategorySelected;
        Close();
        callback?.Invoke(category);
    }

    private void SetVisible(bool visible)
    {
        if (visible)
        {
            HospitalTriageManager.Instance?.AcquireUiFocus(this);
        }
        else if (!gameObject.activeInHierarchy)
        {
            SetVisibleImmediate(false);
            HospitalTriageManager.Instance?.ReleaseUiFocus(this);
            return;
        }

        if (visibilityRoutine != null)
        {
            StopCoroutine(visibilityRoutine);
        }

        if (visible && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        visibilityRoutine = StartCoroutine(AnimateVisibility(visible));
    }

    private IEnumerator AnimateVisibility(bool visible)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        float duration = visible ? openDuration : closeDuration;
        float time = 0f;
        float startAlpha = canvasGroup.alpha;
        float targetAlpha = visible ? 1f : 0f;

        Vector3 startScale = panelRect != null ? panelRect.localScale : Vector3.one * hiddenPanelScale;
        Vector3 targetScale = Vector3.one * (visible ? 1f : hiddenPanelScale);

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(time / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
            if (panelRect != null)
            {
                panelRect.localScale = Vector3.Lerp(startScale, targetScale, eased);
            }

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        if (panelRect != null)
        {
            panelRect.localScale = targetScale;
        }

        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
        visibilityRoutine = null;

        if (!visible)
        {
            HospitalTriageManager.Instance?.ReleaseUiFocus(this);
            if (showOnlyWhenInteracting)
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void SetVisibleImmediate(bool visible)
    {
        if (panelRect != null)
        {
            panelRect.localScale = Vector3.one * (visible ? 1f : hiddenPanelScale);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        if (!visible)
        {
            HospitalTriageManager.Instance?.ReleaseUiFocus(this);
        }

        if (showOnlyWhenInteracting)
        {
            gameObject.SetActive(visible);
        }
    }

    private void EnsureReferences()
    {
        if (rootCanvas == null)
        {
            rootCanvas = GetComponent<Canvas>();
            if (rootCanvas == null)
            {
                rootCanvas = gameObject.AddComponent<Canvas>();
            }
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        rootCanvas.renderMode = RenderMode.WorldSpace;
        rootCanvas.worldCamera = XRCameraHelper.GetPlayerCamera();

        if (panelRect == null)
        {
            panelRect = FindChildRecursive(transform, "Panel") as RectTransform;
        }

        if (caseTitleText == null && panelRect != null)
        {
            Transform caseTitleTransform = panelRect.Find("CaseTitle");
            if (caseTitleTransform != null)
            {
                caseTitleText = caseTitleTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (caseMetaText == null && panelRect != null)
        {
            Transform caseMetaTransform = panelRect.Find("CaseMeta");
            if (caseMetaTransform != null)
            {
                caseMetaText = caseMetaTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (complaintText == null)
        {
            Transform complaintTransform = FindChildRecursive(transform, "ComplaintText");
            if (complaintTransform != null)
            {
                complaintText = complaintTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        greenButton = ResolveButtonReference(greenButton, "Button_Yesil");
        yellowButton = ResolveButtonReference(yellowButton, "Button_Sari");
        redButton = ResolveButtonReference(redButton, "Button_Kirmizi");
        blackButton = ResolveButtonReference(blackButton, "Button_Siyah");

        if (panelRect != null)
        {
            if (complaintBlockRect == null)
            {
                complaintBlockRect = panelRect.Find("ComplaintBlock") as RectTransform;
            }

            if (complaintHeaderText == null && complaintBlockRect != null)
            {
                Transform complaintHeaderTransform = complaintBlockRect.Find("ComplaintTitle");
                if (complaintHeaderTransform != null)
                {
                    complaintHeaderText = complaintHeaderTransform.GetComponent<TextMeshProUGUI>();
                }
            }

            if (hintSectionRect == null)
            {
                hintSectionRect = panelRect.Find("HintSection") as RectTransform;
            }

            if (decisionSectionRect == null)
            {
                decisionSectionRect = panelRect.Find("DecisionSection") as RectTransform;
            }

            if (hintViewportRect == null && hintSectionRect != null)
            {
                Transform viewportTransform = FindChildRecursive(hintSectionRect, "HintViewport");
                hintViewportRect = viewportTransform as RectTransform;
            }

            if (hintScrollRect == null && hintSectionRect != null)
            {
                Transform scrollTransform = hintSectionRect.Find("HintScrollView");
                if (scrollTransform != null)
                {
                    hintScrollRect = scrollTransform.GetComponent<ScrollRect>();
                }
            }

            if (hintContentRect == null && hintViewportRect != null)
            {
                Transform contentTransform = hintViewportRect.Find("HintContent");
                if (contentTransform != null)
                {
                    hintContentRect = contentTransform as RectTransform;
                }
            }

            if (hintTitleText == null && hintSectionRect != null)
            {
                Transform titleTransform = hintSectionRect.Find("HintTitle");
                if (titleTransform != null)
                {
                    hintTitleText = titleTransform.GetComponent<TextMeshProUGUI>();
                }
            }

            if (hintSupportText == null && hintSectionRect != null)
            {
                Transform supportTransform = hintSectionRect.Find("HintSupport");
                if (supportTransform != null)
                {
                    hintSupportText = supportTransform.GetComponent<TextMeshProUGUI>();
                }
            }

            if (hintText == null && hintContentRect != null)
            {
                Transform hintTextTransform = hintContentRect.Find("HintText");
                if (hintTextTransform != null)
                {
                    hintText = hintTextTransform.GetComponent<TextMeshProUGUI>();
                }
            }

            if (hintButton == null && hintSectionRect != null)
            {
                Transform hintButtonTransform = hintSectionRect.Find("HintButton");
                if (hintButtonTransform != null)
                {
                    hintButton = hintButtonTransform.GetComponent<Button>();
                }
            }

            if (hintButtonLabel == null && hintButton != null)
            {
                hintButtonLabel = hintButton.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (decisionTitleText == null)
            {
                Transform decisionTitleTransform = panelRect.Find("DecisionTitle");
                if (decisionTitleTransform != null)
                {
                    decisionTitleText = decisionTitleTransform.GetComponent<TextMeshProUGUI>();
                }
            }
        }
    }

    private void ApplyRuntimeLayout()
    {
        if (panelRect == null)
        {
            return;
        }

        ApplyResolvedLayoutPreset();
        ApplyCanvasLayout();
        ApplyPanelStyle();
        ApplyPanelLayout();
        ApplyCaseHeaderLayout();
        ApplyComplaintLayout();
        ApplyHintLayout();
        ApplyDecisionSectionLayout();
        ApplyButtonGridLayout();
        ApplyButtonStyles();
        ApplyDecisionTitleLayout();
        EnsureVrUiSupport();
        NormalizeNonInteractiveRaycasts();
        EnsureTopAccent();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
    }

    private void ApplyResolvedLayoutPreset()
    {
        canvasSize = new Vector2(1100f, 688f);
        distanceFromCamera = 1.25f;
        verticalOffset = -0.05f;
        minimumVerticalOffset = 0.08f;
        worldOffset = new Vector3(0f, 1.2f, 0f);
        triageButtonCellSize = new Vector2(210f, 80f);
        triageButtonSpacing = new Vector2(14f, 12f);
        triageButtonAnchorY = 0.31f;
        complaintPadding = new Vector2(20f, 12f);
        followPositionSharpness = 12f;
        followRotationSharpness = 14f;
        openDuration = 0.18f;
        closeDuration = 0.12f;
        hiddenPanelScale = 0.92f;
    }

    private void ApplyCanvasLayout()
    {
        if (rootCanvas == null)
        {
            return;
        }

        RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            return;
        }

        canvasRect.sizeDelta = canvasSize;
    }

    private void ApplyPanelStyle()
    {
        Image panelImage = panelRect.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = PanelColor;
            panelImage.raycastTarget = false;
        }

        EnsurePanelShadow();
        EnsureTopAccent();
    }

    private void ApplyPanelLayout()
    {
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = new Vector2(16f, 14f);
        panelRect.offsetMax = new Vector2(-16f, -14f);
    }

    private void ApplyCaseHeaderLayout()
    {
        caseTitleText = EnsureCaseTitleText();
        caseMetaText = EnsureCaseMetaText();

        if (caseTitleText != null)
        {
            RectTransform titleRect = caseTitleText.rectTransform;
            titleRect.anchorMin = new Vector2(0.07f, 0.94f);
            titleRect.anchorMax = new Vector2(0.68f, 0.985f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            titleRect.pivot = new Vector2(0f, 1f);

            caseTitleText.text = ResolveCaseTitleText();
            caseTitleText.color = TextPrimary;
            caseTitleText.fontWeight = FontWeight.Bold;
            caseTitleText.fontSize = 23f;
            caseTitleText.enableAutoSizing = true;
            caseTitleText.fontSizeMin = 18f;
            caseTitleText.fontSizeMax = 23f;
            caseTitleText.alignment = TextAlignmentOptions.TopLeft;
            caseTitleText.enableWordWrapping = false;
            caseTitleText.overflowMode = TextOverflowModes.Ellipsis;
            caseTitleText.margin = Vector4.zero;
            caseTitleText.raycastTarget = false;
        }

        if (caseMetaText != null)
        {
            RectTransform metaRect = caseMetaText.rectTransform;
            metaRect.anchorMin = new Vector2(0.71f, 0.94f);
            metaRect.anchorMax = new Vector2(0.93f, 0.985f);
            metaRect.offsetMin = Vector2.zero;
            metaRect.offsetMax = Vector2.zero;
            metaRect.pivot = new Vector2(1f, 1f);

            caseMetaText.text = ResolveCaseMetaText();
            caseMetaText.color = GetSectionAccentColor();
            caseMetaText.fontWeight = FontWeight.Bold;
            caseMetaText.fontSize = 15f;
            caseMetaText.enableAutoSizing = true;
            caseMetaText.fontSizeMin = 13f;
            caseMetaText.fontSizeMax = 15f;
            caseMetaText.alignment = TextAlignmentOptions.TopRight;
            caseMetaText.enableWordWrapping = false;
            caseMetaText.overflowMode = TextOverflowModes.Ellipsis;
            caseMetaText.margin = Vector4.zero;
            caseMetaText.raycastTarget = false;
        }
    }

    private void ApplyComplaintLayout()
    {
        if (complaintText == null)
        {
            return;
        }

        complaintBlockRect = EnsureComplaintBlock();
        complaintHeaderText = EnsureComplaintHeaderText();

        RectTransform complaintRect = complaintText.rectTransform;
        complaintRect.SetParent(complaintBlockRect, false);
        complaintRect.anchorMin = Vector2.zero;
        complaintRect.anchorMax = Vector2.one;
        complaintRect.offsetMin = new Vector2(complaintPadding.x, complaintPadding.y);
        complaintRect.offsetMax = new Vector2(-complaintPadding.x, -36f);
        complaintRect.pivot = new Vector2(0.5f, 0.5f);
        complaintRect.anchoredPosition = Vector2.zero;

        complaintText.alignment = TextAlignmentOptions.TopLeft;
        complaintText.color = TextPrimary;
        complaintText.fontWeight = FontWeight.SemiBold;
        complaintText.fontSize = 24f;
        complaintText.enableWordWrapping = true;
        complaintText.enableAutoSizing = true;
        complaintText.fontSizeMin = 17f;
        complaintText.fontSizeMax = 24f;
        complaintText.overflowMode = TextOverflowModes.Overflow;
        complaintText.lineSpacing = 1.5f;
        complaintText.paragraphSpacing = 1f;
        complaintText.margin = Vector4.zero;
        complaintText.raycastTarget = false;
        complaintBlockRect.SetAsLastSibling();
    }

    private void ApplyHintLayout()
    {
        hintSectionRect = EnsureHintSection();
        hintScrollRect = EnsureHintScrollView();
        hintViewportRect = EnsureHintViewport();
        hintContentRect = EnsureHintContent();
        hintTitleText = EnsureHintTitleText();
        hintSupportText = EnsureHintSupportText();
        hintText = EnsureHintText();
        hintButton = EnsureHintButton();
        hintButtonLabel = EnsureHintButtonLabel();

        ConfigureHintScrollRect();
        ConfigureHintText();
        ConfigureHintButton();
        hintSectionRect.SetAsLastSibling();
    }

    private void ApplyDecisionSectionLayout()
    {
        decisionSectionRect = EnsureDecisionSection();
        if (decisionSectionRect == null)
        {
            return;
        }

        decisionSectionRect.anchorMin = new Vector2(0.07f, 0.055f);
        decisionSectionRect.anchorMax = new Vector2(0.93f, 0.29f);
        decisionSectionRect.offsetMin = Vector2.zero;
        decisionSectionRect.offsetMax = Vector2.zero;
        decisionSectionRect.pivot = new Vector2(0.5f, 0.5f);
        decisionSectionRect.anchoredPosition = Vector2.zero;
        decisionSectionRect.localScale = Vector3.one;
        decisionSectionRect.SetAsLastSibling();
    }

    private void ApplyButtonGridLayout()
    {
        Button[] orderedButtons = GetButtonsInOrder();
        if (orderedButtons.Length != 4)
        {
            return;
        }

        buttonContainerRect = EnsureButtonContainer();
        if (buttonContainerRect == null)
        {
            return;
        }

        for (int i = 0; i < orderedButtons.Length; i++)
        {
            Button button = orderedButtons[i];
            button.transform.SetParent(buttonContainerRect, false);
            button.transform.SetSiblingIndex(i);
            ApplyButtonLayoutElement(button);
            ConfigureButtonRect(button);
            ConfigureButtonLabel(button);
        }

        GridLayoutGroup gridLayout = buttonContainerRect.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = buttonContainerRect.gameObject.AddComponent<GridLayoutGroup>();
        }

        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 2;
        gridLayout.cellSize = triageButtonCellSize;
        gridLayout.spacing = triageButtonSpacing;
        gridLayout.padding = new RectOffset(0, 0, 0, 0);

        buttonContainerRect.anchorMin = new Vector2(0.5f, triageButtonAnchorY);
        buttonContainerRect.anchorMax = new Vector2(0.5f, triageButtonAnchorY);
        buttonContainerRect.pivot = new Vector2(0.5f, 0.5f);
        buttonContainerRect.anchoredPosition = new Vector2(0f, -4f);
        buttonContainerRect.sizeDelta = GetButtonContainerSize();
        buttonContainerRect.localScale = Vector3.one;
        buttonContainerRect.SetAsLastSibling();

        HideUnexpectedButtons(buttonContainerRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(buttonContainerRect);
    }

    private void ApplyDecisionTitleLayout()
    {
        decisionTitleText = EnsureDecisionTitleText();
        if (decisionTitleText == null)
        {
            return;
        }

        RectTransform titleRect = decisionTitleText.rectTransform;
        titleRect.SetParent(decisionSectionRect != null ? decisionSectionRect : panelRect, false);
        titleRect.anchorMin = new Vector2(0.5f, 0.83f);
        titleRect.anchorMax = new Vector2(0.5f, 0.83f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(250f, 24f);
        titleRect.SetAsLastSibling();

        decisionTitleText.text = DecisionSectionLabel;
        decisionTitleText.color = TextSecondary;
        decisionTitleText.fontWeight = FontWeight.Bold;
        decisionTitleText.fontSize = 16f;
        decisionTitleText.enableAutoSizing = false;
        decisionTitleText.alignment = TextAlignmentOptions.Center;
        decisionTitleText.enableWordWrapping = false;
        decisionTitleText.margin = Vector4.zero;
        decisionTitleText.raycastTarget = false;
    }

    private void ApplyButtonLayoutElement(Button button)
    {
        if (button == null)
        {
            return;
        }

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = button.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = triageButtonCellSize.x;
        layoutElement.minHeight = triageButtonCellSize.y;
        layoutElement.preferredWidth = triageButtonCellSize.x;
        layoutElement.preferredHeight = triageButtonCellSize.y;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
        layoutElement.ignoreLayout = false;
    }

    private void ConfigureButtonRect(Button button)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = triageButtonCellSize;
        rect.localScale = Vector3.one;
    }

    private void ConfigureButtonLabel(Button button)
    {
        TextMeshProUGUI label = button != null ? button.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        if (label == null)
        {
            return;
        }

        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 8f);
        labelRect.offsetMax = new Vector2(-12f, -8f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.pivot = new Vector2(0.5f, 0.5f);

        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.enableAutoSizing = true;
        label.fontSize = 20f;
        label.fontSizeMin = 16f;
        label.fontSizeMax = 20f;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.margin = Vector4.zero;
        label.raycastTarget = false;
    }

    private void EnsurePanelShadow()
    {
        Transform existing = transform.Find("PanelShadow");
        RectTransform shadowRect = existing as RectTransform;
        if (shadowRect == null)
        {
            GameObject shadow = new GameObject("PanelShadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            shadow.transform.SetParent(transform, false);
            shadowRect = shadow.GetComponent<RectTransform>();
            shadowRect.SetSiblingIndex(0);
        }

        Image shadowImage = shadowRect.GetComponent<Image>();
        shadowImage.color = PanelShadowColor;
        shadowImage.raycastTarget = false;

        shadowRect.anchorMin = Vector2.zero;
        shadowRect.anchorMax = Vector2.one;
        shadowRect.offsetMin = new Vector2(16f, -14f);
        shadowRect.offsetMax = new Vector2(8f, -6f);
    }

    private void EnsureTopAccent()
    {
        Transform existing = panelRect.Find("TopAccent");
        RectTransform accentRect = existing as RectTransform;
        if (accentRect == null)
        {
            GameObject accent = new GameObject("TopAccent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            accent.transform.SetParent(panelRect, false);
            accentRect = accent.GetComponent<RectTransform>();
        }

        Image accentImage = accentRect.GetComponent<Image>();
        accentImage.color = GetTopAccentColor();
        accentImage.raycastTarget = false;

        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = new Vector2(0f, -4f);
        accentRect.sizeDelta = new Vector2(0f, 6f);
        accentRect.SetAsLastSibling();
    }

    private TextMeshProUGUI EnsureCaseTitleText()
    {
        if (panelRect == null)
        {
            return null;
        }

        Transform existing = panelRect.Find("CaseTitle");
        RectTransform titleRect = existing as RectTransform;
        if (titleRect == null)
        {
            GameObject titleObject = new GameObject("CaseTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(panelRect, false);
            titleRect = titleObject.GetComponent<RectTransform>();
        }

        return titleRect.GetComponent<TextMeshProUGUI>();
    }

    private TextMeshProUGUI EnsureCaseMetaText()
    {
        if (panelRect == null)
        {
            return null;
        }

        Transform existing = panelRect.Find("CaseMeta");
        RectTransform metaRect = existing as RectTransform;
        if (metaRect == null)
        {
            GameObject metaObject = new GameObject("CaseMeta", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            metaObject.transform.SetParent(panelRect, false);
            metaRect = metaObject.GetComponent<RectTransform>();
        }

        return metaRect.GetComponent<TextMeshProUGUI>();
    }

    private RectTransform EnsureComplaintBlock()
    {
        Transform existing = panelRect.Find("ComplaintBlock");
        RectTransform blockRect = existing as RectTransform;
        if (blockRect == null)
        {
            GameObject block = new GameObject("ComplaintBlock", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            block.transform.SetParent(panelRect, false);
            blockRect = block.GetComponent<RectTransform>();
        }

        Image blockImage = blockRect.GetComponent<Image>();
        blockImage.color = ComplaintBlockColor;
        blockImage.raycastTarget = false;
        EnsureSectionAccent(blockRect, "ComplaintAccent");

        blockRect.anchorMin = new Vector2(0.07f, 0.76f);
        blockRect.anchorMax = new Vector2(0.93f, 0.90f);
        blockRect.offsetMin = Vector2.zero;
        blockRect.offsetMax = Vector2.zero;
        blockRect.pivot = new Vector2(0.5f, 1f);
        blockRect.anchoredPosition = Vector2.zero;
        return blockRect;
    }

    private TextMeshProUGUI EnsureComplaintHeaderText()
    {
        if (complaintBlockRect == null)
        {
            return null;
        }

        Transform existing = complaintBlockRect.Find("ComplaintTitle");
        RectTransform titleRect = existing as RectTransform;
        if (titleRect == null)
        {
            GameObject titleObject = new GameObject("ComplaintTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(complaintBlockRect, false);
            titleRect = titleObject.GetComponent<RectTransform>();
        }

        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -10f);
        titleRect.sizeDelta = new Vector2(0f, 22f);

        TextMeshProUGUI title = titleRect.GetComponent<TextMeshProUGUI>();
        title.text = ComplaintSectionLabel;
        title.color = GetSectionAccentColor();
        title.fontWeight = FontWeight.Bold;
        title.fontSize = 16f;
        title.enableAutoSizing = false;
        title.alignment = TextAlignmentOptions.TopLeft;
        title.enableWordWrapping = false;
        title.margin = new Vector4(18f, 0f, 18f, 0f);
        title.raycastTarget = false;
        return title;
    }

    private RectTransform EnsureHintSection()
    {
        Transform existing = panelRect.Find("HintSection");
        RectTransform sectionRect = existing as RectTransform;
        if (sectionRect == null)
        {
            GameObject section = new GameObject("HintSection", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            section.transform.SetParent(panelRect, false);
            sectionRect = section.GetComponent<RectTransform>();
        }

        Image sectionImage = sectionRect.GetComponent<Image>();
        sectionImage.color = HintSectionColor;
        sectionImage.raycastTarget = false;
        EnsureSectionAccent(sectionRect, "HintAccent");

        sectionRect.anchorMin = new Vector2(0.07f, 0.33f);
        sectionRect.anchorMax = new Vector2(0.93f, 0.73f);
        sectionRect.offsetMin = Vector2.zero;
        sectionRect.offsetMax = Vector2.zero;
        sectionRect.pivot = new Vector2(0.5f, 0.5f);
        sectionRect.anchoredPosition = Vector2.zero;
        sectionRect.localScale = Vector3.one;
        return sectionRect;
    }

    private ScrollRect EnsureHintScrollView()
    {
        if (hintSectionRect == null)
        {
            return null;
        }

        Transform existing = hintSectionRect.Find("HintScrollView");
        RectTransform scrollRectTransform = existing as RectTransform;
        if (scrollRectTransform == null)
        {
            GameObject scrollObject = new GameObject("HintScrollView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(hintSectionRect, false);
            scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        }

        scrollRectTransform.anchorMin = new Vector2(0.035f, 0.18f);
        scrollRectTransform.anchorMax = new Vector2(0.965f, 0.8f);
        scrollRectTransform.offsetMin = Vector2.zero;
        scrollRectTransform.offsetMax = Vector2.zero;
        scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
        scrollRectTransform.anchoredPosition = Vector2.zero;

        Image scrollImage = scrollRectTransform.GetComponent<Image>();
        scrollImage.color = HintViewportColor;
        scrollImage.raycastTarget = true;

        return scrollRectTransform.GetComponent<ScrollRect>();
    }

    private void EnsureSectionAccent(RectTransform parentRect, string accentName)
    {
        if (parentRect == null)
        {
            return;
        }

        Transform existing = parentRect.Find(accentName);
        RectTransform accentRect = existing as RectTransform;
        if (accentRect == null)
        {
            GameObject accentObject = new GameObject(accentName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            accentObject.transform.SetParent(parentRect, false);
            accentRect = accentObject.GetComponent<RectTransform>();
        }

        Image accentImage = accentRect.GetComponent<Image>();
        accentImage.color = GetSectionAccentColor();
        accentImage.raycastTarget = false;

        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.offsetMin = new Vector2(16f, -4f);
        accentRect.offsetMax = new Vector2(-16f, -1f);
        accentRect.SetSiblingIndex(0);
    }

    private RectTransform EnsureDecisionSection()
    {
        Transform existing = panelRect.Find("DecisionSection");
        RectTransform sectionRect = existing as RectTransform;
        if (sectionRect == null)
        {
            GameObject section = new GameObject("DecisionSection", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            section.transform.SetParent(panelRect, false);
            sectionRect = section.GetComponent<RectTransform>();
        }

        Image sectionImage = sectionRect.GetComponent<Image>();
        sectionImage.color = DecisionSectionColor;
        sectionImage.raycastTarget = false;
        EnsureSectionAccent(sectionRect, "DecisionAccent");
        return sectionRect;
    }

    private RectTransform EnsureHintViewport()
    {
        Transform parent = hintScrollRect != null ? hintScrollRect.transform : hintSectionRect;
        Transform existing = parent != null ? parent.Find("HintViewport") : null;
        if (existing == null && hintSectionRect != null)
        {
            existing = FindChildRecursive(hintSectionRect, "HintViewport");
            if (existing != null && parent != null && existing.parent != parent)
            {
                existing.SetParent(parent, false);
            }
        }

        RectTransform viewportRect = existing as RectTransform;
        if (viewportRect == null && parent != null)
        {
            GameObject viewport = new GameObject("HintViewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(parent, false);
            viewportRect = viewport.GetComponent<RectTransform>();
        }

        if (viewportRect == null)
        {
            return null;
        }

        Image viewportImage = viewportRect.GetComponent<Image>();
        viewportImage.color = HintViewportColor;
        viewportImage.raycastTarget = true;

        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(4f, 4f);
        viewportRect.offsetMax = new Vector2(-4f, -4f);
        viewportRect.pivot = new Vector2(0.5f, 0.5f);
        viewportRect.anchoredPosition = Vector2.zero;
        return viewportRect;
    }

    private RectTransform EnsureHintContent()
    {
        if (hintViewportRect == null)
        {
            return null;
        }

        Transform existing = hintViewportRect.Find("HintContent");
        RectTransform contentRect = existing as RectTransform;
        if (contentRect == null)
        {
            GameObject content = new GameObject("HintContent", typeof(RectTransform), typeof(CanvasRenderer));
            content.transform.SetParent(hintViewportRect, false);
            contentRect = content.GetComponent<RectTransform>();
        }

        Transform legacyText = hintViewportRect.Find("HintText");
        if (legacyText != null && legacyText.parent != contentRect)
        {
            legacyText.SetParent(contentRect, false);
        }

        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.localScale = Vector3.one;
        contentRect.sizeDelta = new Vector2(0f, 88f);
        return contentRect;
    }

    private TextMeshProUGUI EnsureHintTitleText()
    {
        if (hintSectionRect == null)
        {
            return null;
        }

        Transform existing = hintSectionRect.Find("HintTitle");
        RectTransform titleRect = existing as RectTransform;
        if (titleRect == null)
        {
            GameObject titleObject = new GameObject("HintTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(hintSectionRect, false);
            titleRect = titleObject.GetComponent<RectTransform>();
        }

        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -12f);
        titleRect.sizeDelta = new Vector2(0f, 24f);

        TextMeshProUGUI title = titleRect.GetComponent<TextMeshProUGUI>();
        title.text = HintTitleLabel;
        title.color = GetSectionAccentColor();
        title.fontWeight = FontWeight.Bold;
        title.fontSize = 22f;
        title.enableAutoSizing = false;
        title.alignment = TextAlignmentOptions.TopLeft;
        title.enableWordWrapping = false;
        title.margin = new Vector4(20f, 0f, 20f, 0f);
        title.raycastTarget = false;
        return title;
    }

    private TextMeshProUGUI EnsureHintSupportText()
    {
        if (hintSectionRect == null)
        {
            return null;
        }

        Transform existing = hintSectionRect.Find("HintSupport");
        RectTransform supportRect = existing as RectTransform;
        if (supportRect == null)
        {
            GameObject supportObject = new GameObject("HintSupport", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            supportObject.transform.SetParent(hintSectionRect, false);
            supportRect = supportObject.GetComponent<RectTransform>();
        }

        supportRect.anchorMin = new Vector2(0f, 1f);
        supportRect.anchorMax = new Vector2(1f, 1f);
        supportRect.pivot = new Vector2(0.5f, 1f);
        supportRect.anchoredPosition = new Vector2(0f, -36f);
        supportRect.sizeDelta = new Vector2(0f, 22f);

        TextMeshProUGUI support = supportRect.GetComponent<TextMeshProUGUI>();
        support.text = ResolveHintSupportText();
        support.color = TextMuted;
        support.fontWeight = FontWeight.Medium;
        support.fontSize = 14f;
        support.enableAutoSizing = false;
        support.alignment = TextAlignmentOptions.TopLeft;
        support.enableWordWrapping = false;
        support.margin = new Vector4(20f, 0f, 20f, 0f);
        support.raycastTarget = false;
        return support;
    }

    private TextMeshProUGUI EnsureHintText()
    {
        if (hintContentRect == null)
        {
            return null;
        }

        Transform existing = hintContentRect.Find("HintText");
        if (existing == null && hintViewportRect != null)
        {
            existing = FindChildRecursive(hintViewportRect, "HintText");
            if (existing != null && existing.parent != hintContentRect)
            {
                existing.SetParent(hintContentRect, false);
            }
        }

        RectTransform textRect = existing as RectTransform;
        if (textRect == null)
        {
            GameObject textObject = new GameObject("HintText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(hintContentRect, false);
            textRect = textObject.GetComponent<RectTransform>();
        }

        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(16f, 0f);
        textRect.offsetMax = new Vector2(-16f, 0f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = new Vector2(0f, -10f);

        return textRect.GetComponent<TextMeshProUGUI>();
    }

    private Button EnsureHintButton()
    {
        if (hintSectionRect == null)
        {
            return null;
        }

        Transform existing = hintSectionRect.Find("HintButton");
        RectTransform buttonRect = existing as RectTransform;
        if (buttonRect == null)
        {
            GameObject buttonObject = new GameObject("HintButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(hintSectionRect, false);
            buttonRect = buttonObject.GetComponent<RectTransform>();
        }

        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 12f);
        buttonRect.sizeDelta = new Vector2(194f, 40f);
        buttonRect.localScale = Vector3.one;
        buttonRect.SetAsLastSibling();

        Transform labelTransform = buttonRect.Find("Label");
        RectTransform labelRect = labelTransform as RectTransform;
        if (labelRect == null)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonRect, false);
            labelRect = labelObject.GetComponent<RectTransform>();
        }

        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 6f);
        labelRect.offsetMax = new Vector2(-8f, -6f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.pivot = new Vector2(0.5f, 0.5f);

        TextMeshProUGUI labelText = labelRect.GetComponent<TextMeshProUGUI>();
        if (labelText == null)
        {
            labelText = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        }

        labelText.text = HintButtonDefaultLabel;
        labelText.color = TextPrimary;
        labelText.fontWeight = FontWeight.Bold;
        labelText.fontSize = 15f;
        labelText.enableAutoSizing = false;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.enableWordWrapping = false;
        labelText.overflowMode = TextOverflowModes.Ellipsis;
        labelText.margin = Vector4.zero;
        labelText.raycastTarget = false;

        return buttonRect.GetComponent<Button>();
    }

    private TextMeshProUGUI EnsureHintButtonLabel()
    {
        if (hintButton == null)
        {
            return null;
        }

        TextMeshProUGUI label = hintButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.text = string.IsNullOrWhiteSpace(label.text) ? HintButtonDefaultLabel : label.text;
            return label;
        }

        RectTransform buttonRect = hintButton.GetComponent<RectTransform>();
        if (buttonRect == null)
        {
            return null;
        }

        Transform existing = buttonRect.Find("Label");
        RectTransform labelRect = existing as RectTransform;
        if (labelRect == null)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonRect, false);
            labelRect = labelObject.GetComponent<RectTransform>();
        }

        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 6f);
        labelRect.offsetMax = new Vector2(-8f, -6f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.pivot = new Vector2(0.5f, 0.5f);

        TextMeshProUGUI labelText = labelRect.GetComponent<TextMeshProUGUI>();
        if (labelText == null)
        {
            labelText = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        }

        labelText.text = HintButtonDefaultLabel;
        return labelText;
    }

    private void ConfigureHintScrollRect()
    {
        if (hintScrollRect == null || hintViewportRect == null || hintContentRect == null)
        {
            return;
        }

        hintScrollRect.viewport = hintViewportRect;
        hintScrollRect.content = hintContentRect;
        hintScrollRect.horizontal = false;
        hintScrollRect.vertical = true;
        hintScrollRect.movementType = ScrollRect.MovementType.Clamped;
        hintScrollRect.inertia = true;
        hintScrollRect.scrollSensitivity = 42f;
    }

    private RectTransform EnsureButtonContainer()
    {
        Transform parent = decisionSectionRect != null ? decisionSectionRect : panelRect;
        Transform existing = parent != null ? parent.Find("ButtonContainer") : null;
        RectTransform containerRect = existing as RectTransform;
        if (containerRect == null)
        {
            GameObject container = new GameObject("ButtonContainer", typeof(RectTransform), typeof(CanvasRenderer));
            container.transform.SetParent(parent, false);
            containerRect = container.GetComponent<RectTransform>();
        }
        else if (parent != null && containerRect.parent != parent)
        {
            containerRect.SetParent(parent, false);
        }

        return containerRect;
    }

    private TextMeshProUGUI EnsureDecisionTitleText()
    {
        if (panelRect == null)
        {
            return null;
        }

        Transform existing = panelRect.Find("DecisionTitle");
        RectTransform titleRect = existing as RectTransform;
        if (titleRect == null)
        {
            GameObject titleObject = new GameObject("DecisionTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(panelRect, false);
            titleRect = titleObject.GetComponent<RectTransform>();
        }

        return titleRect.GetComponent<TextMeshProUGUI>();
    }

    private void ConfigureHintText()
    {
        if (hintText == null)
        {
            return;
        }

        hintText.alignment = TextAlignmentOptions.TopLeft;
        hintText.color = TextPrimary;
        hintText.fontWeight = FontWeight.Medium;
        hintText.fontSize = 18f;
        hintText.enableAutoSizing = false;
        hintText.enableWordWrapping = true;
        hintText.overflowMode = TextOverflowModes.Overflow;
        hintText.lineSpacing = 5f;
        hintText.paragraphSpacing = 10f;
        hintText.margin = Vector4.zero;
        hintText.raycastTarget = true;
        hintText.richText = true;

        if (string.IsNullOrEmpty(hintText.text))
        {
            RenderHintChat(null, null, false);
        }
    }

    private void ConfigureHintButton()
    {
        if (hintButton == null)
        {
            return;
        }

        if (hintButtonLabel == null)
        {
            hintButtonLabel = EnsureHintButtonLabel();
        }

        Color hintButtonBaseColor = GetHintButtonColor();
        Image image = hintButton.GetComponent<Image>();
        if (image != null)
        {
            image.color = hintButtonBaseColor;
            image.raycastTarget = true;
            hintButton.targetGraphic = image;
        }

        hintButton.navigation = new Navigation { mode = Navigation.Mode.None };
        hintButton.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = hintButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.Lerp(hintButtonBaseColor, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(hintButtonBaseColor, Color.black, 0.16f);
        colors.selectedColor = Color.Lerp(hintButtonBaseColor, Color.white, 0.18f);
        colors.disabledColor = new Color(0.55f, 0.62f, 0.72f, 0.45f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        hintButton.colors = colors;

        if (hintButtonLabel != null)
        {
            hintButtonLabel.text = HintButtonDefaultLabel;
            hintButtonLabel.color = TextPrimary;
            hintButtonLabel.fontWeight = FontWeight.Bold;
            hintButtonLabel.fontSize = 15f;
            hintButtonLabel.enableAutoSizing = true;
            hintButtonLabel.fontSizeMin = 13f;
            hintButtonLabel.fontSizeMax = 15f;
            hintButtonLabel.alignment = TextAlignmentOptions.Center;
            hintButtonLabel.enableWordWrapping = false;
            hintButtonLabel.overflowMode = TextOverflowModes.Ellipsis;
            hintButtonLabel.margin = Vector4.zero;
            hintButtonLabel.raycastTarget = false;
        }

        TriageButtonHoverFeedback hoverFeedback = hintButton.GetComponent<TriageButtonHoverFeedback>();
        if (hoverFeedback == null)
        {
            hoverFeedback = hintButton.gameObject.AddComponent<TriageButtonHoverFeedback>();
        }

        hoverFeedback.Configure(hintButtonBaseColor);
    }

    private void ApplyButtonStyles()
    {
        StyleButton(greenButton, GreenAccent, "YESIL");
        StyleButton(yellowButton, YellowAccent, "SARI");
        StyleButton(redButton, RedAccent, "KIRMIZI");
        StyleButton(blackButton, SlateAccent, "SIYAH");
    }

    private void StyleButton(Button button, Color baseColor, string labelText)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = baseColor;
            image.raycastTarget = true;
        }

        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.97f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.84f, 0.91f, 0.96f, 1f);
        colors.selectedColor = new Color(0.97f, 1f, 1f, 1f);
        colors.disabledColor = new Color(0.55f, 0.62f, 0.72f, 0.45f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.text = labelText;
            label.color = TextPrimary;
            label.fontWeight = FontWeight.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 20f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 20f;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
        }

        TriageButtonHoverFeedback hoverFeedback = button.GetComponent<TriageButtonHoverFeedback>();
        if (hoverFeedback == null)
        {
            hoverFeedback = button.gameObject.AddComponent<TriageButtonHoverFeedback>();
        }

        hoverFeedback.Configure(baseColor);
    }

    private void EnsureVrUiSupport()
    {
        if (rootCanvas == null)
        {
            return;
        }

        if (rootCanvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
        {
            rootCanvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        }

        GraphicRaycaster graphicRaycaster = rootCanvas.GetComponent<GraphicRaycaster>();
        if (graphicRaycaster != null)
        {
            graphicRaycaster.enabled = false;
        }

        if (rootCanvas.GetComponent<VRUIClickHelper>() == null)
        {
            rootCanvas.gameObject.AddComponent<VRUIClickHelper>();
        }

        EnsureEventSystemReady();
        EnsureXRInteractorsCanHitUI();
        ApplyUILayerToCanvas();
    }

    private void EnsureEventSystemReady()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            eventSystem = FindObjectOfType<EventSystem>();
        }

        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        StandaloneInputModule standalone = eventSystem.GetComponent<StandaloneInputModule>();
        if (standalone != null)
        {
            standalone.enabled = false;
        }

        if (eventSystem.GetComponent<XRUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<XRUIInputModule>();
        }
    }

    private void EnsureXRInteractorsCanHitUI()
    {
        XRRayInteractor[] rayInteractors = FindObjectsOfType<XRRayInteractor>(true);
        foreach (XRRayInteractor rayInteractor in rayInteractors)
        {
            if (rayInteractor == null)
            {
                continue;
            }

            rayInteractor.enableUIInteraction = ShouldEnableUiInteractionForRay(rayInteractor);
        }
    }

    private static bool ShouldEnableUiInteractionForRay(XRRayInteractor rayInteractor)
    {
        if (rayInteractor == null)
        {
            return false;
        }

        string nameToken = rayInteractor.name != null ? rayInteractor.name.ToLowerInvariant() : string.Empty;
        string parentToken = rayInteractor.transform.parent != null
            ? rayInteractor.transform.parent.name.ToLowerInvariant()
            : string.Empty;
        string token = nameToken + " " + parentToken;

        return !token.Contains("teleport") && !token.Contains("isinlan");
    }

    private void ApplyUILayerToCanvas()
    {
        if (rootCanvas == null)
        {
            return;
        }

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0)
        {
            return;
        }

        SetLayerRecursively(rootCanvas.transform, uiLayer);
    }

    private void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null)
        {
            return;
        }

        root.gameObject.layer = layer;
        foreach (Transform child in root)
        {
            SetLayerRecursively(child, layer);
        }
    }

    private void NormalizeNonInteractiveRaycasts()
    {
        if (caseTitleText != null)
        {
            caseTitleText.raycastTarget = false;
        }

        if (caseMetaText != null)
        {
            caseMetaText.raycastTarget = false;
        }

        if (complaintText != null)
        {
            complaintText.raycastTarget = false;
        }

        if (complaintHeaderText != null)
        {
            complaintHeaderText.raycastTarget = false;
        }

        if (hintTitleText != null)
        {
            hintTitleText.raycastTarget = false;
        }

        if (hintSupportText != null)
        {
            hintSupportText.raycastTarget = false;
        }

        if (hintText != null)
        {
            hintText.raycastTarget = false;
        }

        if (decisionTitleText != null)
        {
            decisionTitleText.raycastTarget = false;
        }

        foreach (Button button in GetButtonsInOrder())
        {
            if (button == null)
            {
                continue;
            }

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.raycastTarget = false;
            }
        }

        if (hintButtonLabel != null)
        {
            hintButtonLabel.raycastTarget = false;
        }
    }

    private void ResetHintState(string complaint)
    {
        currentComplaintText = complaint ?? string.Empty;
        StopHintRoutine();
        hintHistoryEntries.Clear();
        hintRequestCount = 0;
        isHintLoading = false;
        RestoreHintButtonIdleState();
        RenderHintChat(null, null, true);
    }

    private void HandleHintButtonClicked()
    {
        if (!isOpen || isHintLoading || hintButton == null)
        {
            return;
        }

        int requestIndex = hintRequestCount;
        hintRequestCount++;

        TrainingAnalyticsFacade.OnHelpRequested(
            TrainingAnalyticsFacade.Module3Id,
            TrainingAnalyticsFacade.Module3Name,
            "triage_hint",
            new Dictionary<string, object>
            {
                { AnalyticsParams.ScenarioId, TrainingAnalyticsFacade.Module3ScenarioId },
                { AnalyticsParams.ScenarioName, TrainingAnalyticsFacade.Module3ScenarioName },
                { AnalyticsParams.VictimId, currentNpc != null ? currentNpc.CaseId : "victim_unknown" },
                { AnalyticsParams.VictimName, currentNpc != null ? currentNpc.PatientTitle : "Bilinmeyen Hasta" },
                { AnalyticsParams.QuestionIndex, requestIndex + 1 }
            });

        StopHintRoutine();
        hintRoutine = StartCoroutine(RevealHintRoutine(requestIndex));
    }

    private void StopHintRoutine()
    {
        hintSessionId++;

        if (hintRoutine != null)
        {
            StopCoroutine(hintRoutine);
            hintRoutine = null;
        }

        isHintLoading = false;
    }

    private IEnumerator RevealHintRoutine(int requestIndex)
    {
        isHintLoading = true;
        int sessionId = hintSessionId;
        string resolvedMessage = null;
        bool requestCompleted = false;
        TriageCaseProfile activeCaseProfile = currentCaseProfile != null ? currentCaseProfile.Clone() : null;

        if (hintButton != null)
        {
            hintButton.interactable = false;
        }

        if (hintButtonLabel != null)
        {
            hintButtonLabel.text = HintLoadingLabel;
        }

        RenderHintChat(null, BuildThinkingMarkup(), true);

        AIManager aiManager = AIManager.Instance;
        if (activeCaseProfile != null)
        {
            resolvedMessage = MedicalHintComposer.Compose(activeCaseProfile, requestIndex);
            requestCompleted = true;
        }
        else if (aiManager != null)
        {
            aiManager.RequestTriageHint(
                currentComplaintText,
                hintHistoryEntries,
                message =>
                {
                    if (sessionId != hintSessionId)
                    {
                        return;
                    }

                    resolvedMessage = message;
                    requestCompleted = true;
                },
                error =>
                {
                    if (sessionId != hintSessionId)
                    {
                        return;
                    }

                    Debug.LogWarning("[TriageDialogUI] AI triage hint fallback tetiklendi: " + error);
                    resolvedMessage = BuildHintTextNatural(currentComplaintText, requestIndex);
                    requestCompleted = true;
                });
        }
        else
        {
            Debug.LogWarning("[TriageDialogUI] AIManager bulunamadi, yerel triage hint fallback kullaniliyor.");
            resolvedMessage = BuildHintTextNatural(currentComplaintText, requestIndex);
            requestCompleted = true;
        }

        while (!requestCompleted && sessionId == hintSessionId)
        {
            yield return null;
        }

        if (sessionId != hintSessionId)
        {
            hintRoutine = null;
            yield break;
        }

        if (string.IsNullOrWhiteSpace(resolvedMessage))
        {
            resolvedMessage = activeCaseProfile != null
                ? MedicalHintComposer.Compose(activeCaseProfile, requestIndex)
                : BuildHintTextNatural(currentComplaintText, requestIndex);
        }

        resolvedMessage = activeCaseProfile != null
            ? NormalizeStructuredHintForDisplay(resolvedMessage)
            : FormatHintForDisplayV2(resolvedMessage, currentComplaintText);

        if (hintHistoryEntries.Count >= MaxHintHistoryEntries)
        {
            hintHistoryEntries.RemoveAt(0);
        }

        hintHistoryEntries.Add(resolvedMessage);
        RenderHintChat(resolvedMessage, null, true);

        RestoreHintButtonIdleState();
        isHintLoading = false;
        hintRoutine = null;
    }

    private void RestoreHintButtonIdleState()
    {
        if (hintButton != null)
        {
            hintButton.interactable = true;
        }

        if (hintButtonLabel == null)
        {
            hintButtonLabel = EnsureHintButtonLabel();
        }

        if (hintButtonLabel != null)
        {
            hintButtonLabel.text = hintHistoryEntries.Count > 0 ? HintButtonRepeatLabel : HintButtonDefaultLabel;
        }
    }

    private void RenderHintChat(string previewAssistantMessage, string transientMarkup, bool scrollToBottom)
    {
        if (hintText == null)
        {
            return;
        }

        hintText.text = BuildHintChatMarkup(previewAssistantMessage, transientMarkup);
        RefreshHintChatLayout(scrollToBottom);
    }

    private string BuildHintChatMarkup(string previewAssistantMessage, string transientMarkup)
    {
        StringBuilder builder = new StringBuilder(512);
        bool hasPreview = !string.IsNullOrWhiteSpace(previewAssistantMessage);
        bool hasTransient = !string.IsNullOrWhiteSpace(transientMarkup);
        string latestHistoryEntry = hintHistoryEntries.Count > 0 ? hintHistoryEntries[hintHistoryEntries.Count - 1] : null;

        if (string.IsNullOrWhiteSpace(latestHistoryEntry) && !hasPreview && !hasTransient)
        {
            AppendHintAssistantBlock(builder, HintDefaultText);
            return builder.ToString();
        }

        if (hasPreview)
        {
            AppendHintAssistantBlock(builder, previewAssistantMessage);
        }
        else if (!string.IsNullOrWhiteSpace(latestHistoryEntry))
        {
            AppendHintAssistantBlock(builder, latestHistoryEntry);
        }

        if (hasTransient)
        {
            if (builder.Length > 0)
            {
                builder.Append("\n\n");
            }

            builder.Append(transientMarkup);
        }

        return builder.ToString();
    }

    private void AppendHintAssistantBlock(StringBuilder builder, string message)
    {
        if (builder == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append("\n\n");
        }

        builder.Append("<color=#");
        builder.Append(ColorUtility.ToHtmlStringRGB(TextPrimary));
        builder.Append(">");
        builder.Append(ApplyHintRichTextEmphasis(message));
        builder.Append("</color>");
    }

    private string BuildThinkingMarkup()
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(AIChatCanvasLayout.AssistantTextColor) + "><i>" + HintLoadingLabel + " Klinik ipucu hazirlaniyor.</i></color>";
    }

    private string ApplyHintRichTextEmphasis(string message)
    {
        string formatted = EscapeRichText(message);

        formatted = HighlightHintHeading(formatted, "Olasi durum:");
        formatted = HighlightHintHeading(formatted, "Esik bulgu:");
        formatted = HighlightHintHeading(formatted, "Simdi bak:");
        formatted = HighlightHintHeading(formatted, "Oncelik:");
        formatted = HighlightHintHeading(formatted, "Kritik gozlem:");
        formatted = HighlightHintHeading(formatted, "Neyi dusundurur:");
        formatted = HighlightHintHeading(formatted, "Ilk bakilacaklar:");
        formatted = HighlightHintHeading(formatted, "Triyaj ipucu:");
        formatted = HighlightHintHeading(formatted, "Sahadaki ton:");
        formatted = HighlightHintPhrase(formatted, "en acil");
        formatted = HighlightHintPhrase(formatted, "orta oncelik");
        formatted = HighlightHintPhrase(formatted, "dusuk oncelik");
        formatted = HighlightHintPhrase(formatted, "beklentisiz");
        formatted = HighlightHintPhrase(formatted, "bekletme");
        formatted = HighlightHintPhrase(formatted, "daha ust oncelik");
        formatted = HighlightHintPhrase(formatted, "daha alt oncelik");
        formatted = HighlightHintPhrase(formatted, "SpO2");
        formatted = HighlightHintPhrase(formatted, "hava yolu");
        formatted = HighlightHintPhrase(formatted, "nabiz");
        formatted = HighlightHintPhrase(formatted, "bilinc");
        formatted = HighlightHintPhrase(formatted, "morarma");
        formatted = HighlightHintPhrase(formatted, "pupiller");
        formatted = HighlightHintPhrase(formatted, "hipoksi");
        formatted = HighlightHintPhrase(formatted, "sok");
        formatted = HighlightHintPhrase(formatted, "pnomotoraks");
        formatted = HighlightHintPhrase(formatted, "gogus travmasi");
        formatted = HighlightHintPhrase(formatted, "kafa travmasi");
        return formatted;
    }

    private string HighlightHintHeading(string source, string heading)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(heading) || !source.Contains(heading))
        {
            return source;
        }

        return source.Replace(
            heading,
            "<b><color=#" + ColorUtility.ToHtmlStringRGB(GetSectionAccentColor()) + ">" + heading + "</color></b>");
    }

    private string HighlightHintPhrase(string source, string phrase)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(phrase) || !source.Contains(phrase))
        {
            return source;
        }

        return source.Replace(
            phrase,
            "<b><color=#" + ColorUtility.ToHtmlStringRGB(GetSectionAccentColor()) + ">" + phrase + "</color></b>");
    }

    private string FormatHintForDisplay(string message, string complaint)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return BuildHintText(complaint, 0);
        }

        string normalized = message
            .Replace("\r", "\n")
            .Replace("**", string.Empty)
            .Replace("__", string.Empty)
            .Replace("•", string.Empty)
            .Trim();

        string[] rawLines = normalized.Split('\n');
        List<string> contentLines = new List<string>();
        string recommendationLine = null;

        for (int i = 0; i < rawLines.Length; i++)
        {
            string line = rawLines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string cleanedLine = line.Trim()
                .TrimStart('-', '*', ' ')
                .Replace("Pratik eylem:", "Oneri:")
                .Replace("Pratik Eylem:", "Oneri:")
                .Replace("Öneri:", "Oneri:")
                .Replace("öneri:", "Oneri:");

            if (string.IsNullOrWhiteSpace(cleanedLine))
            {
                continue;
            }

            if (NormalizeForHintMatching(cleanedLine).StartsWith("oneri"))
            {
                recommendationLine = LimitLineLength(cleanedLine, 72);
                continue;
            }

            contentLines.Add(cleanedLine);
        }

        string mainText = string.Join(" ", contentLines).Trim();
        if (string.IsNullOrWhiteSpace(mainText))
        {
            mainText = BuildHintText(complaint, 0);
        }

        string[] mainSentences = mainText.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        if (mainSentences.Length > 0)
        {
            mainText = mainSentences[0].Trim();
        }

        mainText = EnsureFindingPrefix(LimitLineLength(mainText, 88));

        if (string.IsNullOrWhiteSpace(recommendationLine))
        {
            recommendationLine = ExtractRecommendationLine(BuildHintText(complaint, 0));
        }

        recommendationLine = string.IsNullOrWhiteSpace(recommendationLine)
            ? "Oneri: Vital bulgulari tekrar kontrol et."
            : EnsureRecommendationPrefix(LimitLineLength(recommendationLine, 72));

        string finalMessage = mainText + "\n" + recommendationLine;
        if (finalMessage.Length <= HintDisplayMaxCharacters)
        {
            return finalMessage;
        }

        int remainingForMain = Mathf.Max(48, HintDisplayMaxCharacters - recommendationLine.Length - 1);
        return LimitLineLength(mainText, remainingForMain) + "\n" + recommendationLine;
    }

    private string ExtractRecommendationLine(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        string[] lines = message.Replace("\r", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (NormalizeForHintMatching(line).StartsWith("oneri"))
            {
                return EnsureRecommendationPrefix(line.Replace("Öneri:", "Oneri:"));
            }
        }

        return string.Empty;
    }

    private string EnsureRecommendationPrefix(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return "Oneri: Vital bulgulari tekrar kontrol et.";
        }

        string trimmed = line.Trim();
        return NormalizeForHintMatching(trimmed).StartsWith("oneri")
            ? trimmed
            : "Oneri: " + trimmed;
    }

    private string EnsureFindingPrefix(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return "Bulgu: Genel durum hizla tekrar degerlendirilmeli.";
        }

        string trimmed = line.Trim();
        return NormalizeForHintMatching(trimmed).StartsWith("bulgu")
            ? trimmed
            : "Bulgu: " + trimmed;
    }

    private string LimitLineLength(string line, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(line) || line.Length <= maxLength)
        {
            return line?.Trim() ?? string.Empty;
        }

        int cutIndex = line.LastIndexOf(' ', Mathf.Min(maxLength, line.Length - 1));
        if (cutIndex < 24)
        {
            cutIndex = maxLength;
        }

        return line.Substring(0, cutIndex).TrimEnd(' ', ',', ';', ':') + "...";
    }

    private string EscapeRichText(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        return message
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    private void RefreshHintChatLayout(bool scrollToBottom)
    {
        if (hintText == null || hintScrollRect == null || hintContentRect == null || hintViewportRect == null)
        {
            return;
        }

        RectTransform textRect = hintText.rectTransform;
        float viewportWidth = hintViewportRect.rect.width;
        float targetWidth = viewportWidth > 70f ? viewportWidth - 32f : 520f;

        hintText.ForceMeshUpdate();
        Vector2 preferredSize = hintText.GetPreferredValues(hintText.text, targetWidth, 0f);
        float textHeight = Mathf.Max(72f, preferredSize.y + 24f);
        float contentHeight = Mathf.Max(textHeight + 24f, hintViewportRect.rect.height);

        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = new Vector2(0f, -10f);
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);

        hintContentRect.anchorMin = new Vector2(0f, 1f);
        hintContentRect.anchorMax = new Vector2(1f, 1f);
        hintContentRect.pivot = new Vector2(0.5f, 1f);
        hintContentRect.anchoredPosition = Vector2.zero;
        hintContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, hintViewportRect.rect.width);
        hintContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);

        LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(hintContentRect);
        Canvas.ForceUpdateCanvases();

        if (scrollToBottom)
        {
            hintScrollRect.StopMovement();
            hintScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private string BuildHintText(string complaint, int requestIndex)
    {
        bool alternate = (requestIndex % 2) == 1;

        switch (ResolveHintCategory(complaint))
        {
            case HintCategory.Respiratory:
                return alternate
                    ? "Bulgu: Solunum eforu artiyorsa durum hizla agirlasabilir.\nOneri: Konusma, morarma ve gogus hareketini hemen kontrol et."
                    : "Bulgu: Nefes sikintisi kritik riske donebilir.\nOneri: Solunum sayisi ve oksijenlenmeyi hemen yeniden degerlendir.";

            case HintCategory.Bleeding:
                return alternate
                    ? "Bulgu: Kan kaybi sok riskini hizla artirabilir.\nOneri: Kanama odagini bul, basi uygula ve perfuzyonu izle."
                    : "Bulgu: Aktif kanama dolasimi kisa surede bozabilir.\nOneri: Kanamayi durdurup solukluk ve nabzi tekrar kontrol et.";

            case HintCategory.Consciousness:
                return alternate
                    ? "Bulgu: Bilincte azalma ciddi kotulesme belirtisi olabilir.\nOneri: Sozlu yanit, goz acma ve hava yolunu birlikte kontrol et."
                    : "Bulgu: Bilinc degisikligi acil risk isaretidir.\nOneri: Cevap duzeyi ve hava yolunu hemen yeniden degerlendir.";

            case HintCategory.BurnSmoke:
                return alternate
                    ? "Bulgu: Duman maruziyetinde hava yolu sessizce bozulabilir.\nOneri: Ses kisikligi, is bulgusu ve solunum eforuna bak."
                    : "Bulgu: Yanik ve duman hava yolunu gecikmeli etkileyebilir.\nOneri: Yuz-boyun tutulumu ve oksijenlenmeyi hizla kontrol et.";

            case HintCategory.EmptyComplaint:
                return alternate
                    ? "Bulgu: Belirti net degilse genel duruma odaklan.\nOneri: Solunum, dolasim ve bilinci hizli tarayip tekrar bak."
                    : "Bulgu: Net yakinma yoksa vital kontrol one cikar.\nOneri: Hava yolu, kanama ve bilinci birlikte degerlendir.";

            default:
                return alternate
                    ? "Bulgu: Sikayet tek basina yeterli olmayabilir.\nOneri: Solunum, dolasim ve bilinci birlikte yeniden kontrol et."
                    : "Bulgu: Once hayati riskleri hizla tara.\nOneri: Vital bulgulara gore uygun triyaj rengini sec.";
        }
    }

    private string NormalizeStructuredHintForDisplay(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return HintDefaultText;
        }

        string normalizedMessage = message
            .Replace("\r", "\n")
            .Replace("**", string.Empty)
            .Replace("__", string.Empty)
            .Replace("Ã¢â‚¬Â¢", string.Empty)
            .Trim();

        string normalizedForMatch = NormalizeForHintMatching(normalizedMessage);
        if (normalizedForMatch.Contains("ic dusunce") ||
            normalizedForMatch.Contains("gorunur bir yanit") ||
            normalizedForMatch.Contains("yanit uretemedi"))
        {
            return HintDefaultText;
        }

        bool looksStructuredCard = normalizedForMatch.Contains("olasi durum:") ||
                                   normalizedForMatch.Contains("esik bulgu:") ||
                                   normalizedForMatch.Contains("simdi bak:") ||
                                   normalizedForMatch.Contains("oncelik:");
        if (looksStructuredCard)
        {
            string[] lines = normalizedMessage.Replace("\r", "\n").Split('\n');
            List<string> preservedLines = new List<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                string collapsedLine = CollapseHintWhitespace(lines[i]);
                if (!string.IsNullOrWhiteSpace(collapsedLine))
                {
                    preservedLines.Add(collapsedLine);
                }
            }

            return preservedLines.Count > 0
                ? string.Join("\n", preservedLines)
                : HintDefaultText;
        }

        string findingText = StripHintLeadPrefix(ExtractFindingLineV2(normalizedMessage));
        string differentialText = StripHintLeadPrefix(ExtractDifferentialLineV2(normalizedMessage));
        string recommendationText = StripHintLeadPrefix(ExtractRecommendationLineV2(normalizedMessage));

        List<string> sentences = new List<string>();
        if (!string.IsNullOrWhiteSpace(findingText))
        {
            sentences.Add(EnsureSentenceEnding(findingText));
        }

        if (!string.IsNullOrWhiteSpace(differentialText) &&
            NormalizeForHintMatching(differentialText) != NormalizeForHintMatching(findingText))
        {
            sentences.Add(EnsureSentenceEnding(differentialText));
        }

        if (!string.IsNullOrWhiteSpace(recommendationText))
        {
            sentences.Add(EnsureSentenceEnding(recommendationText));
        }

        string proseMessage = sentences.Count > 0
            ? CollapseHintWhitespace(string.Join(" ", sentences))
            : CollapseHintWhitespace(normalizedMessage.Replace("\n", " "));

        if (string.IsNullOrWhiteSpace(proseMessage))
        {
            return HintDefaultText;
        }

        return LimitLineLength(proseMessage, CaseHintDisplayMaxCharacters);
    }

    private string FormatHintForDisplayV2(string message, string complaint)
    {
        string fallback = BuildHintTextNatural(complaint, 0);
        if (string.IsNullOrWhiteSpace(message))
        {
            return fallback;
        }

        string normalizedMessage = message
            .Replace("\r", "\n")
            .Replace("**", string.Empty)
            .Replace("__", string.Empty)
            .Replace("â€¢", string.Empty)
            .Trim();

        string normalizedForMatch = NormalizeForHintMatching(normalizedMessage);
        if (normalizedForMatch.Contains("ic dusunce") ||
            normalizedForMatch.Contains("gorunur bir yanit") ||
            normalizedForMatch.Contains("yanit uretemedi"))
        {
            return fallback;
        }

        bool looksStructured = normalizedForMatch.Contains("bulgu") ||
                               normalizedForMatch.Contains("olasi durum") ||
                               normalizedForMatch.Contains("olasi neden") ||
                               normalizedForMatch.Contains("oneri");

        string findingText = StripHintLeadPrefix(ExtractFindingLineV2(normalizedMessage));
        string differentialText = StripHintLeadPrefix(ExtractDifferentialLineV2(normalizedMessage));
        string recommendationText = StripHintLeadPrefix(ExtractRecommendationLineV2(normalizedMessage));
        string proseMessage;

        if (looksStructured)
        {
            List<string> sentences = new List<string>();

            if (!string.IsNullOrWhiteSpace(findingText))
            {
                sentences.Add(EnsureSentenceEnding(findingText));
            }

            if (!string.IsNullOrWhiteSpace(differentialText) &&
                NormalizeForHintMatching(differentialText) != NormalizeForHintMatching(findingText))
            {
                sentences.Add(EnsureSentenceEnding(differentialText));
            }

            if (!string.IsNullOrWhiteSpace(recommendationText))
            {
                sentences.Add(EnsureSentenceEnding(recommendationText));
            }

            proseMessage = CollapseHintWhitespace(string.Join(" ", sentences));
        }
        else
        {
            proseMessage = CollapseHintWhitespace(normalizedMessage.Replace("\n", " "));
        }

        if (string.IsNullOrWhiteSpace(proseMessage))
        {
            return fallback;
        }

        string proseMatch = NormalizeForHintMatching(proseMessage);
        bool lacksSpecificCheck = !proseMatch.Contains("spo2") &&
                                  !proseMatch.Contains("nabiz") &&
                                  !proseMatch.Contains("bilinc") &&
                                  !proseMatch.Contains("gogus") &&
                                  !proseMatch.Contains("hava yolu") &&
                                  !proseMatch.Contains("morarma") &&
                                  !proseMatch.Contains("pupiller") &&
                                  !proseMatch.Contains("solunum");
        bool lacksLikelyCondition = !proseMatch.Contains("hipoksi") &&
                                    !proseMatch.Contains("sok") &&
                                    !proseMatch.Contains("travma") &&
                                    !proseMatch.Contains("kanama") &&
                                    !proseMatch.Contains("pnomotoraks") &&
                                    !proseMatch.Contains("inhalasyon") &&
                                    !proseMatch.Contains("perfuzyon") &&
                                    !proseMatch.Contains("dusuk oncelik") &&
                                    !proseMatch.Contains("orta oncelik") &&
                                    !proseMatch.Contains("en acil") &&
                                    !proseMatch.Contains("beklentisiz") &&
                                    !proseMatch.Contains("yasam bulgusu") &&
                                    !proseMatch.Contains("minor") &&
                                    !proseMatch.Contains("yuzeysel");
        bool lacksDecisionCue = !proseMatch.Contains("yaklasir") &&
                                !proseMatch.Contains("daha ust") &&
                                !proseMatch.Contains("daha alt") &&
                                !proseMatch.Contains("bekletme") &&
                                !proseMatch.Contains("bekleyebilir") &&
                                !proseMatch.Contains("varsa") &&
                                !proseMatch.Contains("yoksa") &&
                                !proseMatch.Contains("oncelik");

        if (lacksSpecificCheck || lacksLikelyCondition || lacksDecisionCue)
        {
            proseMessage = fallback;
        }

        if (proseMessage.Length <= HintDisplayMaxCharacters)
        {
            return proseMessage;
        }

        return LimitLineLength(proseMessage, HintDisplayMaxCharacters);
    }

    private string StripHintLeadPrefix(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return string.Empty;
        }

        string trimmed = line.Trim();
        string normalized = NormalizeForHintMatching(trimmed);
        string[] prefixes =
        {
            "bulgu:",
            "olasi durum:",
            "olasi neden:",
            "olasi tani:",
            "ayirici tani:",
            "tani:",
            "oneri:"
        };

        for (int i = 0; i < prefixes.Length; i++)
        {
            if (!normalized.StartsWith(prefixes[i]))
            {
                continue;
            }

            int colonIndex = trimmed.IndexOf(':');
            return colonIndex >= 0 ? trimmed.Substring(colonIndex + 1).Trim() : trimmed;
        }

        return trimmed;
    }

    private string EnsureSentenceEnding(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string trimmed = text.Trim();
        char lastCharacter = trimmed[trimmed.Length - 1];
        if (lastCharacter == '.' || lastCharacter == '!' || lastCharacter == '?')
        {
            return trimmed;
        }

        return trimmed + ".";
    }

    private string CollapseHintWhitespace(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string collapsed = text.Replace("\r", " ").Replace("\n", " ").Trim();
        while (collapsed.Contains("  "))
        {
            collapsed = collapsed.Replace("  ", " ");
        }

        return collapsed;
    }

    private string ExtractFindingLineV2(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        string[] lines = message.Replace("\r", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string cleanedLine = lines[i].Trim().TrimStart('-', '*', ' ');
            if (string.IsNullOrWhiteSpace(cleanedLine))
            {
                continue;
            }

            if (NormalizeForHintMatching(cleanedLine).StartsWith("bulgu"))
            {
                return cleanedLine;
            }
        }

        for (int i = 0; i < lines.Length; i++)
        {
            string cleanedLine = lines[i].Trim().TrimStart('-', '*', ' ');
            if (string.IsNullOrWhiteSpace(cleanedLine))
            {
                continue;
            }

            string normalized = NormalizeForHintMatching(cleanedLine);
            if (normalized.StartsWith("olasi") ||
                normalized.StartsWith("ayirici") ||
                normalized.StartsWith("tani") ||
                normalized.StartsWith("oneri"))
            {
                continue;
            }

            return cleanedLine;
        }

        return string.Empty;
    }

    private string ExtractDifferentialLineV2(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        string[] lines = message.Replace("\r", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string cleanedLine = lines[i].Trim().TrimStart('-', '*', ' ');
            if (string.IsNullOrWhiteSpace(cleanedLine))
            {
                continue;
            }

            string normalized = NormalizeForHintMatching(cleanedLine);
            if (normalized.StartsWith("olasi durum") ||
                normalized.StartsWith("olasi neden") ||
                normalized.StartsWith("ayirici tani") ||
                normalized.StartsWith("olasi tani") ||
                normalized.StartsWith("tani"))
            {
                return cleanedLine;
            }
        }

        return string.Empty;
    }

    private string ExtractRecommendationLineV2(string message)
    {
        string existingRecommendation = ExtractRecommendationLine(message);
        if (!string.IsNullOrWhiteSpace(existingRecommendation))
        {
            return existingRecommendation;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        string[] lines = message.Replace("\r", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string cleanedLine = lines[i].Trim().TrimStart('-', '*', ' ');
            if (string.IsNullOrWhiteSpace(cleanedLine))
            {
                continue;
            }

            string normalized = NormalizeForHintMatching(cleanedLine);
            if (normalized.Contains("kontrol") ||
                normalized.Contains("degerlendir") ||
                normalized.Contains("bak") ||
                normalized.Contains("izle"))
            {
                return "Oneri: " + cleanedLine;
            }
        }

        return string.Empty;
    }

    private string EnsureDifferentialPrefix(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return "Olasi durum: Sok, hipoksi veya travma dislanmali.";
        }

        string trimmed = line.Trim();
        string normalized = NormalizeForHintMatching(trimmed);
        if (normalized.StartsWith("olasi durum"))
        {
            return trimmed;
        }

        if (normalized.StartsWith("olasi neden") ||
            normalized.StartsWith("olasi tani") ||
            normalized.StartsWith("ayirici tani") ||
            normalized.StartsWith("tani"))
        {
            int colonIndex = trimmed.IndexOf(':');
            string content = colonIndex >= 0 ? trimmed.Substring(colonIndex + 1).Trim() : trimmed;
            return "Olasi durum: " + content;
        }

        return "Olasi durum: " + trimmed;
    }

    private string BuildHintTextRefined(string complaint, int requestIndex)
    {
        bool alternate = (requestIndex % 2) == 1;
        string normalizedComplaint = NormalizeForHintMatching(complaint);
        bool hasRespiratory = ContainsAny(normalizedComplaint, "nefes", "solunum");
        bool hasChest = ContainsAny(normalizedComplaint, "gogus");
        bool hasHead = ContainsAny(normalizedComplaint, "bas", "bas agrisi", "basim");

        if (hasRespiratory && hasHead)
        {
            return alternate
                ? "Bulgu: Zor solunumla birlikte bas yakini varsa hasta fizyolojik olarak hizla bozulabilir.\nOlasi durum: Hipoksi, kapali gogus yaralanmasi veya kafa travmasina eslik eden dolasim bozuklugu dislanmali.\nOneri: Konusma gucu, morarma, pupiller ve oksijenlenmeyi hemen kontrol et."
                : "Bulgu: Bas agrisi ile nefes darligi birlikteliginde sorun sadece agri olmayabilir, oksijen dususu gozden kacabilir.\nOlasi durum: Hipoksi, gogus travmasi, duman etkisi veya sok dusunulmeli.\nOneri: Solunum sayisi, SpO2, bilinc ve gogus hareketini ayni anda kontrol et.";
        }

        switch (ResolveHintCategory(complaint))
        {
            case HintCategory.Respiratory:
                return alternate
                    ? "Bulgu: Artan solunum eforu triyajda yuksek risk isaretidir.\nOlasi durum: Akciger kontuzyonu, kapali gogus yaralanmasi veya dolasim bozuklugu dislanmali.\nOneri: Solunum sayisi, yardimci kas kullanimi ve oksijenlenmeyi yeniden degerlendir."
                    : "Bulgu: Nefes sikintisi olan hasta ilk bakista stabil gorunse de kisa surede yorulabilir.\nOlasi durum: Hipoksi, gogus travmasi, pnomotoraks veya sok dusunulmeli.\nOneri: Konusma gucu, morarma, gogus simetrisi ve SpO2 degerini hemen kontrol et.";

            case HintCategory.Bleeding:
                return alternate
                    ? "Bulgu: Solukluk ve huzursuzluk kan kaybinin erken alarmi olabilir.\nOlasi durum: Hipovolemik sok veya gizli ic kanama dislanmamali.\nOneri: Basiyi surdur, kapiller dolum, nabiz ve bilinci hemen yeniden bak."
                    : "Bulgu: Kan kaybi hasta konusuyor olsa bile perfuzyonu sessizce bozabilir.\nOlasi durum: Dis kanama varsa kontrolsuz kayip, odak yoksa ic kanama ve hipovolemik sok dusunulmeli.\nOneri: Kanama odagini bul, dogrudan basi uygula, nabiz ve cilt rengini kontrol et.";

            case HintCategory.Consciousness:
                return alternate
                    ? "Bulgu: Sorun sadece bas agrisi degil, bilinc degisimi varsa sistemik bozulma olabilir.\nOlasi durum: Kafa travmasi, gizli hipoksi veya perfuzyon bozuklugu dislanmamali.\nOneri: Yanit duzeyi, pupiller, solunum ve nabzi ayni turda yeniden degerlendir."
                    : "Bulgu: Bilincte azalma travmada en onemli kotulesme bulgularindan biridir.\nOlasi durum: Kafa travmasi, hipoksi, sok veya hipoperfuzyon dusunulmeli.\nOneri: Sozlu yanit, goz acma, pupiller ve hava yolunu birlikte hemen kontrol et.";

            case HintCategory.BurnSmoke:
                return alternate
                    ? "Bulgu: Yuz-boyun etkilenimi olan duman hastasi ilk anda konussa bile hava yolu daralabilir.\nOlasi durum: Inhalasyon yaralanmasi, ust hava yolu odemi veya karbonmonoksit etkisi dusunulmeli.\nOneri: Ses kisikligi, kurum, stridor ve oksijenlenmeyi hemen kontrol et."
                    : "Bulgu: Duman maruziyetinde hasta konusuyor olsa bile hava yolu sonradan bozulabilir.\nOlasi durum: Inhalasyon yaralanmasi, karbonmonoksit etkisi veya ust hava yolu odemi dusunulmeli.\nOneri: Ses kisikligi, yuz-boyun yanigi, kurum ve solunum eforunu hemen kontrol et.";

            case HintCategory.EmptyComplaint:
                return alternate
                    ? "Bulgu: Belirti net degilse taniya en cok genel gorunum ve vital bozulma yol gosterir.\nOlasi durum: Sok, gizli kanama, hipoksi veya travmatik bozulma dislanmamali.\nOneri: Solunum, dolasim, bilinc ve dis kanama kontrolunu sirayla yap."
                    : "Bulgu: Net sikayet yoksa kararini vital bulgular ve genel gorunum belirler.\nOlasi durum: Hipoksi, perfuzyon bozuklugu veya gizli travma dusunulmeli.\nOneri: Hava yolu, solunum, nabiz ve bilinci sistematik sekilde yeniden degerlendir.";

            default:
                if (hasHead)
                {
                    return alternate
                        ? "Bulgu: Siddetli bas yakini travmada norolojik etkilenimi dusundurur.\nOlasi durum: Kafa travmasi, hipoksi veya perfuzyon bozuklugu dislanmamali.\nOneri: Bilinc, pupiller, kusma oykusu ve dengeyi hemen kontrol et."
                        : "Bulgu: Bas yakini hafif gorunse bile norolojik bozulma eslik ediyor olabilir.\nOlasi durum: Kafa travmasi veya hipoperfuzyon dusunulmeli.\nOneri: Yanit duzeyi, pupiller ve solunumu ayni anda yeniden degerlendir.";
                }

                if (hasChest)
                {
                    return alternate
                        ? "Bulgu: Gogus yakinmasi olan hasta sakin gorunse de aniden bozulabilir.\nOlasi durum: Gogus travmasi, hipoksi veya dolasim bozuklugu dusunulmeli.\nOneri: Agri yeri, solunum, nabiz ve gogus simetrisini yeniden kontrol et."
                        : "Bulgu: Gogus bolgesi sikayetinde ilk bakista atlanan ciddi tablo olabilir.\nOlasi durum: Kapali gogus yaralanmasi, hipoksi veya erken sok dislanmamali.\nOneri: Solunum eforu, konusma gucu ve oksijenlenmeyi hemen yeniden bak.";
                }

                return alternate
                    ? "Bulgu: Hasta anlatimi hafif gorunse de gizli bozulma bulgusu aranmalidir.\nOlasi durum: Erken sok, hipoksi veya ic yaralanma dusunulmeli.\nOneri: Konusma, nabiz, solunum ve cilt perfuzyonunu birlikte degerlendir."
                    : "Bulgu: Sikayet tek basina tani koydurmaz; belirleyici olan vital bozulmanin derecesidir.\nOlasi durum: Sok, hipoksi, travmatik etkilenim veya bilinc bozulmasi dislanmamali.\nOneri: Solunum, dolasim ve bilinci ayni turda birlikte yeniden kontrol et.";
        }
    }

    private string BuildHintTextNatural(string complaint, int requestIndex)
    {
        bool alternate = (requestIndex % 2) == 1;
        string normalizedComplaint = NormalizeForHintMatching(complaint);
        bool hasRespiratory = ContainsAny(normalizedComplaint, "nefes", "solunum");
        bool hasChest = ContainsAny(normalizedComplaint, "gogus");
        bool hasHead = ContainsAny(normalizedComplaint, "bas", "bas agrisi", "basim");
        bool canWalk = ContainsAny(normalizedComplaint, "yuruyebiliyorum", "kendi basima");
        bool hasMinorInjury = ContainsAny(normalizedComplaint, "hafif", "siyrik", "sizliyor");
        bool hasNoSignsOfLife = ContainsAny(normalizedComplaint, "yanit vermiyor", "nabiz alinmiyor", "spontan solunum", "solunum yok");
        bool hasModerateExtremityInjury = ContainsAny(normalizedComplaint, "basamiyorum", "derin", "metal parcasi", "hareketlerimde kisitlilik");

        if (hasNoSignsOfLife)
        {
            return alternate
                ? "Hasta yanitsiz ve spontan solunumu yoksa bu tablo beklentisiz gruba yakindir. Hava yolunu acip kisa tekrar bak; yine nabiz ve solunum yoksa yasam bulgusu olmayan hasta gibi dusun."
                : "Bu hastada yanit, nabiz ve spontan solunum yoksa beklentisiz yon dusun. Hava yolu acikligindan sonra nabiz ve solunumu hemen tekrar kontrol et.";
        }

        if (canWalk && hasMinorInjury)
        {
            return alternate
                ? "Hasta yuruyebiliyor ve yakinmasi yuzeysel ise dusuk oncelige yakindir. Bilinc tam, kanama kontrol altinda ve distal dolasim normalse hafif yarali gibi dusun."
                : "Bu hasta kendi basina yuruyebiliyor ve sikayeti hafifse en acil gruba benzemez, dusuk oncelige yaklasir. Kanama, bilinc ve ekstremite dolasiminda bozulma yoksa bekleyebilecek hasta gibi dusun.";
        }

        if (hasModerateExtremityInjury)
        {
            return alternate
                ? "Derin yara ve ustune basamama orta oncelikli ciddi ekstremite travmasini dusundurur. Distal nabiz bozuksa, kanama artiyorsa veya belirgin deformite varsa daha ust oncelik dusun; bunlari hemen kontrol et."
                : "Hasta konusuyor olsa da derin yara ve basamama varsa dusuk oncelik dusunme, orta oncelige daha yakindir. Distal dolasim, aktif kanama ve kirik deformitesini hemen degerlendir.";
        }

        if (hasRespiratory && hasHead)
        {
            return alternate
                ? "Bas agrisi ile zor solunum bir aradaysa hipoksi, gizli gogus yaralanmasi veya kafa travmasina eslik eden perfuzyon bozuklugu dusun ve hasta en acile yaklasir. Morarma, pupiller, bilinc ve oksijenlenmeyi hemen kontrol et."
                : "Bas agrisi ile nefes darligi birlikteyse sorun sadece agri olmayabilir; hipoksi, gogus travmasi veya sok dusunulmeli ve bu tablo en acile yakindir. Solunum sayisi, SpO2, bilinc ve gogus hareketini hemen birlikte kontrol et.";
        }

        switch (ResolveHintCategory(complaint))
        {
            case HintCategory.Respiratory:
                return alternate
                    ? "Artan solunum eforu bu hastanin hizla kotulesebilecegini gosterir; hipoksi, kapali gogus yaralanmasi veya dolasim bozuklugu dusun ve en acil yone yaklas. Yardimci kas kullanimi, tek kelimeyle konusma ve oksijenlenmeyi hemen kontrol et."
                    : "Nefes sikintisi hipoksi, pnomotoraks veya gogus travmasini dusundurur ve hasta en acile yaklasir. Morarma, konusma gucu, gogus simetrisi ve SpO2 degerini hemen kontrol et.";

            case HintCategory.Bleeding:
                return alternate
                    ? "Solukluk ve huzursuzluk kan kaybinin erken alarmi olabilir; sok bulgusu yoksa orta oncelik, varsa daha ust aciliyet dusun. Basiyi surdur, kapiller dolum, nabiz ve bilinci hemen tekrar bak."
                    : "Aktif kanama hipovolemik sok riskini dusundurur; hasta konusuyor diye dusuk gorme. Nabiz zayifsa, cilt soluksa veya kapiller dolum bozuksa daha yuksek oncelik dusun ve kanamayi hemen kontrol et.";

            case HintCategory.Consciousness:
                return alternate
                    ? "Bilinc degisimi varsa dusuk oncelik dusunme; kafa travmasi, hipoksi veya perfuzyon bozuklugu nedeniyle hasta en acile yaklasir. Yanit duzeyi, pupiller, solunum ve nabzi ayni turda hemen kontrol et."
                    : "Bilincte azalma travmada en onemli kotulesme bulgularindan biridir; kafa travmasi, hipoksi veya sok dusunulmeli ve bu tablo en acil yone gider. Sozlu yanit, goz acma, pupiller ve hava yolunu birlikte hemen kontrol et.";

            case HintCategory.BurnSmoke:
                return alternate
                    ? "Yuz ve boyun etkilenimi olan duman hastasinda ust hava yolu odemi veya inhalasyon yaralanmasi dusun; bu nedenle yuksek oncelik dusun. Ses kisikligi, kurum, stridor ve oksijenlenmeyi hemen kontrol et."
                    : "Duman maruziyetinde hasta ilk anda iyi gorunse bile hava yolu sonradan bozulabilir; inhalasyon yaralanmasi veya karbonmonoksit etkisi dusun ve bu hastayi bekletme. Ses kisikligi, yuz-boyun yanigi, kurum ve solunum eforunu hemen kontrol et.";

            case HintCategory.EmptyComplaint:
                return alternate
                    ? "Belirti net degilse karari genel gorunum ve vital bozulma verir; hasta yuruyebiliyor mu, bilinci acik mi, solunumu rahat mi buna bakarak onceligi ciz. Solunum, dolasim, bilinc ve dis kanamayi sirayla kontrol et."
                    : "Net sikayet yoksa yine karar verilebilir; hayati risk yoksa dusuk, belirgin bozulma varsa daha yuksek oncelik dusun. Hava yolu, solunum, nabiz ve bilinci sistematik sekilde yeniden degerlendir.";

            default:
                if (hasHead)
                {
                    return alternate
                        ? "Siddetli bas yakini travmada norolojik etkilenimi dusundurur; bilinc etkileniyorsa hasta en acile yaklasir. Kafa travmasi, hipoksi veya perfuzyon bozuklugu icin bilinc, pupiller ve kusma oykusunu hemen kontrol et."
                        : "Bas yakini hafif gorunse bile norolojik bozulma eslik ediyor olabilir; kafa travmasi veya hipoperfuzyon dusun. Bilinc normalse daha dusuk, bozuluyorsa daha yuksek oncelik dusun ve pupillerle solunumu birlikte kontrol et.";
                }

                if (hasChest)
                {
                    return alternate
                        ? "Gogus yakinmasi olan hasta sakin gorunse de aniden bozulabilir; bu nedenle orta ya da daha ust oncelik dusun. Gogus travmasi, hipoksi veya dolasim bozuklugu icin agri yeri, solunum, nabiz ve gogus simetrisini yeniden kontrol et."
                        : "Gogus bolgesi sikayetinde kapali gogus yaralanmasi, hipoksi veya erken sok dislanmamali. Solunum eforu artiyorsa hasta en acile yaklasir; konusma gucu ve oksijenlenmeyi hemen tekrar bak.";
                }

                return alternate
                    ? "Hasta anlatimi hafif gorunse de gizli bozulma bulgusu aranmalidir; erken sok, hipoksi veya ic yaralanma dusunulmeli. Sadece sikayete degil, konusma, nabiz, solunum ve cilt perfuzyonuna bakarak onceligi belirle."
                    : "Sikayet tek basina karar verdirmez; onceligi hayati riskin derecesi belirler. Solunum, dolasim ve bilinci birlikte kontrol et, belirgin bozulma varsa daha yuksek oncelik dusun.";
        }
    }

    private HintCategory ResolveHintCategory(string complaint)
    {
        if (string.IsNullOrWhiteSpace(complaint))
        {
            return HintCategory.EmptyComplaint;
        }

        string normalizedComplaint = NormalizeForHintMatching(complaint);
        if (ContainsAny(normalizedComplaint, "nefes", "solunum", "gogus"))
        {
            return HintCategory.Respiratory;
        }

        if (ContainsAny(normalizedComplaint, "kanama", "kan"))
        {
            return HintCategory.Bleeding;
        }

        if (ContainsAny(normalizedComplaint, "bilinc", "bayilma", "cevap vermiyor"))
        {
            return HintCategory.Consciousness;
        }

        if (ContainsAny(normalizedComplaint, "yanik", "duman"))
        {
            return HintCategory.BurnSmoke;
        }

        return HintCategory.General;
    }

    private bool ContainsAny(string source, params string[] keywords)
    {
        if (string.IsNullOrEmpty(source) || keywords == null)
        {
            return false;
        }

        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];
            if (string.IsNullOrEmpty(keyword))
            {
                continue;
            }

            if (source.IndexOf(keyword, StringComparison.Ordinal) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private string NormalizeForHintMatching(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string normalizedText = value.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new StringBuilder(normalizedText.Length);

        for (int i = 0; i < normalizedText.Length; i++)
        {
            char current = normalizedText[i];
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(normalizedText, i);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private void RefreshCasePresentation()
    {
        currentAccentColor = ResolveCurrentAccentColor();

        if (caseTitleText != null)
        {
            caseTitleText.text = ResolveCaseTitleText();
        }

        if (caseMetaText != null)
        {
            caseMetaText.text = ResolveCaseMetaText();
            caseMetaText.color = GetSectionAccentColor();
        }

        if (complaintHeaderText != null)
        {
            complaintHeaderText.color = GetSectionAccentColor();
        }

        if (hintTitleText != null)
        {
            hintTitleText.color = GetSectionAccentColor();
        }

        if (hintSupportText != null)
        {
            hintSupportText.text = ResolveHintSupportText();
        }
    }

    private string ResolveCaseTitleText()
    {
        if (currentCaseProfile != null)
        {
            return currentCaseProfile.PatientTitleOrFallback;
        }

        if (currentNpc != null)
        {
            return currentNpc.PatientTitle;
        }

        return "Hasta degerlendirmesi";
    }

    private string ResolveCaseMetaText()
    {
        int currentIndex = HospitalTriageManager.Instance != null && currentNpc != null
            ? HospitalTriageManager.Instance.GetNpcDisplayIndex(currentNpc)
            : -1;

        int totalCount = HospitalTriageManager.Instance != null
            ? Mathf.Max(HospitalTriageManager.Instance.ToplamYarali, TriageCaseCatalog.Count)
            : TriageCaseCatalog.Count;

        if (currentIndex <= 0)
        {
            return CaseCounterFallbackLabel + " | " + totalCount;
        }

        return CaseCounterFallbackLabel + " " + currentIndex + " / " + totalCount;
    }

    private string ResolveHintSupportText()
    {
        if (currentCaseProfile != null)
        {
            return currentCaseProfile.ToneOrFallback;
        }

        if (currentNpc != null && !string.IsNullOrWhiteSpace(currentNpc.FieldTone))
        {
            return currentNpc.FieldTone;
        }

        return HintSupportLabel;
    }

    private Color ResolveCurrentAccentColor()
    {
        if (currentCaseProfile != null)
        {
            return currentCaseProfile.AccentColorOrFallback;
        }

        if (currentNpc != null)
        {
            return currentNpc.AccentColor;
        }

        return AccentGlowColor;
    }

    private Color GetTopAccentColor()
    {
        return Color.Lerp(currentAccentColor, Color.white, 0.1f);
    }

    private Color GetSectionAccentColor()
    {
        return Color.Lerp(currentAccentColor, Color.white, 0.08f);
    }

    private Color GetHintButtonColor()
    {
        return Color.Lerp(HintButtonColor, currentAccentColor, 0.62f);
    }

    private void ApplyFollowPose(bool instant, Transform npcTransform = null)
    {
        Vector3 targetPosition;
        Quaternion targetRotation;

        if (!TryGetFollowPose(out targetPosition, out targetRotation))
        {
            if (npcTransform == null)
            {
                return;
            }

            targetPosition = npcTransform.position + worldOffset;

            Transform cameraTransform = XRCameraHelper.GetPlayerCameraTransform();
            Vector3 panelForward = cameraTransform != null ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up) : -npcTransform.forward;
            if (panelForward.sqrMagnitude < 0.0001f)
            {
                panelForward = Vector3.forward;
            }

            targetRotation = Quaternion.LookRotation(panelForward.normalized, Vector3.up);
        }

        if (instant || !Application.isPlaying)
        {
            transform.SetPositionAndRotation(targetPosition, targetRotation);
            return;
        }

        float positionT = 1f - Mathf.Exp(-followPositionSharpness * Time.unscaledDeltaTime);
        float rotationT = 1f - Mathf.Exp(-followRotationSharpness * Time.unscaledDeltaTime);

        transform.position = Vector3.Lerp(transform.position, targetPosition, positionT);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationT);
    }

    private bool TryGetFollowPose(out Vector3 targetPosition, out Quaternion targetRotation)
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;

        if (!followPlayerCamera)
        {
            return false;
        }

        Transform cameraTransform = followTarget != null ? followTarget : XRCameraHelper.GetPlayerCameraTransform();
        if (cameraTransform == null)
        {
            return false;
        }

        Vector3 planarForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
        if (planarForward.sqrMagnitude < 0.0001f)
        {
            planarForward = Vector3.forward;
        }

        planarForward.Normalize();
        targetPosition = cameraTransform.position + planarForward * distanceFromCamera;
        targetPosition += Vector3.up * Mathf.Max(verticalOffset, minimumVerticalOffset);

        Vector3 panelForward = planarForward;
        if (panelForward.sqrMagnitude < 0.0001f)
        {
            panelForward = Vector3.forward;
        }

        targetRotation = Quaternion.LookRotation(panelForward.normalized, Vector3.up);
        return true;
    }

    private void HideUnexpectedButtons(RectTransform containerRect)
    {
        if (containerRect == null)
        {
            return;
        }

        Button[] expectedButtons = GetButtonsInOrder();
        for (int i = 0; i < containerRect.childCount; i++)
        {
            Transform child = containerRect.GetChild(i);
            Button childButton = child.GetComponent<Button>();
            if (childButton == null)
            {
                continue;
            }

            bool shouldStayVisible = Array.IndexOf(expectedButtons, childButton) >= 0;
            child.gameObject.SetActive(shouldStayVisible);
        }
    }

    private Button[] GetButtonsInOrder()
    {
        Button[] ordered = { greenButton, yellowButton, redButton, blackButton };
        int count = 0;
        for (int i = 0; i < ordered.Length; i++)
        {
            if (ordered[i] != null)
            {
                count++;
            }
        }

        Button[] result = new Button[count];
        int index = 0;
        for (int i = 0; i < ordered.Length; i++)
        {
            if (ordered[i] == null)
            {
                continue;
            }

            result[index] = ordered[i];
            index++;
        }

        return result;
    }

    private Vector2 GetButtonContainerSize()
    {
        float width = (triageButtonCellSize.x * 2f) + triageButtonSpacing.x;
        float height = (triageButtonCellSize.y * 2f) + triageButtonSpacing.y;
        return new Vector2(width, height);
    }

    private Button ResolveButtonReference(Button currentReference, string buttonName)
    {
        if (currentReference != null)
        {
            return currentReference;
        }

        Transform root = panelRect != null ? panelRect : transform;
        Transform child = FindChildRecursive(root, buttonName);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private void BindButtons()
    {
        if (greenButton != null)
        {
            greenButton.onClick.RemoveListener(SelectGreen);
            greenButton.onClick.AddListener(SelectGreen);
        }

        if (yellowButton != null)
        {
            yellowButton.onClick.RemoveListener(SelectYellow);
            yellowButton.onClick.AddListener(SelectYellow);
        }

        if (redButton != null)
        {
            redButton.onClick.RemoveListener(SelectRed);
            redButton.onClick.AddListener(SelectRed);
        }

        if (blackButton != null)
        {
            blackButton.onClick.RemoveListener(SelectBlack);
            blackButton.onClick.AddListener(SelectBlack);
        }

        if (hintButton != null)
        {
            hintButton.onClick.RemoveListener(HandleHintButtonClicked);
            hintButton.onClick.AddListener(HandleHintButtonClicked);
        }
    }

    private void UnbindButtons()
    {
        if (greenButton != null)
        {
            greenButton.onClick.RemoveListener(SelectGreen);
        }

        if (yellowButton != null)
        {
            yellowButton.onClick.RemoveListener(SelectYellow);
        }

        if (redButton != null)
        {
            redButton.onClick.RemoveListener(SelectRed);
        }

        if (blackButton != null)
        {
            blackButton.onClick.RemoveListener(SelectBlack);
        }

        if (hintButton != null)
        {
            hintButton.onClick.RemoveListener(HandleHintButtonClicked);
        }
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        if (string.Equals(root.name, childName, StringComparison.Ordinal))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}

public class TriageButtonHoverFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float hoverScale = 1.03f;
    [SerializeField] private float pressedScale = 0.98f;
    [SerializeField] private float scaleLerpSpeed = 18f;
    [SerializeField] private float glowLerpSpeed = 18f;
    [SerializeField] private Color glowColor = new Color(0.36f, 0.84f, 1f, 0.92f);
    [SerializeField] private float idleGlowAlpha = 0.08f;
    [SerializeField] private float hoverGlowAlpha = 0.22f;
    [SerializeField] private float pressedGlowAlpha = 0.32f;

    private Button button;
    private RectTransform rectTransform;
    private Image glowImage;
    private Vector3 initialScale = Vector3.one;
    private bool isHovered;
    private bool isPressed;
    private float currentGlowAlpha;

    private void Awake()
    {
        EnsureReferences();
        SetImmediateVisuals();
    }

    private void OnEnable()
    {
        EnsureReferences();
        SetImmediateVisuals();
    }

    private void OnDisable()
    {
        isHovered = false;
        isPressed = false;
        SetImmediateVisuals();
    }

    public void Configure(Color accentColor)
    {
        glowColor = accentColor;
        UpdateGlowColor(currentGlowAlpha);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractive())
        {
            return;
        }

        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractive())
        {
            return;
        }

        isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    private void Update()
    {
        EnsureReferences();

        Vector3 targetScale = GetTargetScale();
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, scaleLerpSpeed * Time.unscaledDeltaTime);

        float targetGlowAlpha = GetTargetGlowAlpha();
        currentGlowAlpha = Mathf.Lerp(currentGlowAlpha, targetGlowAlpha, glowLerpSpeed * Time.unscaledDeltaTime);
        UpdateGlowColor(currentGlowAlpha);
    }

    private void EnsureReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }

        if (rectTransform == null)
        {
            return;
        }

        if (initialScale == Vector3.zero)
        {
            initialScale = rectTransform.localScale == Vector3.zero ? Vector3.one : rectTransform.localScale;
        }

        EnsureGlowVisual();
    }

    private void EnsureGlowVisual()
    {
        if (rectTransform == null)
        {
            return;
        }

        if (glowImage == null)
        {
            Transform existing = transform.Find("HoverGlow");
            if (existing != null)
            {
                glowImage = existing.GetComponent<Image>();
            }
        }

        if (glowImage == null)
        {
            GameObject glowObject = new GameObject("HoverGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            glowObject.transform.SetParent(transform, false);
            glowObject.transform.SetSiblingIndex(0);
            glowImage = glowObject.GetComponent<Image>();
        }

        RectTransform glowRect = glowImage.rectTransform;
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-4f, -4f);
        glowRect.offsetMax = new Vector2(4f, 4f);
        glowRect.anchoredPosition = Vector2.zero;
        glowRect.pivot = new Vector2(0.5f, 0.5f);

        glowImage.raycastTarget = false;
        glowImage.maskable = true;
        UpdateGlowColor(currentGlowAlpha);
    }

    private bool IsInteractive()
    {
        return isActiveAndEnabled && (button == null || button.interactable);
    }

    private Vector3 GetTargetScale()
    {
        if (!IsInteractive())
        {
            return initialScale;
        }

        if (isPressed)
        {
            return initialScale * pressedScale;
        }

        if (isHovered)
        {
            return initialScale * hoverScale;
        }

        return initialScale;
    }

    private float GetTargetGlowAlpha()
    {
        if (!IsInteractive())
        {
            return idleGlowAlpha * 0.5f;
        }

        if (isPressed)
        {
            return pressedGlowAlpha;
        }

        if (isHovered)
        {
            return hoverGlowAlpha;
        }

        return idleGlowAlpha;
    }

    private void UpdateGlowColor(float alpha)
    {
        if (glowImage == null)
        {
            return;
        }

        Color color = glowColor;
        color.a = alpha;
        glowImage.color = color;
    }

    private void SetImmediateVisuals()
    {
        EnsureReferences();
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.localScale = GetTargetScale();
        currentGlowAlpha = GetTargetGlowAlpha();
        UpdateGlowColor(currentGlowAlpha);
    }
}
