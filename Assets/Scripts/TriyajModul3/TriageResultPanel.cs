using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TriageResultPanel : MonoBehaviour
{
    private static readonly Color PanelColor = new Color(0.04f, 0.1f, 0.18f, 0.9f);
    private static readonly Color PanelShadowColor = new Color(0.005f, 0.03f, 0.08f, 0.46f);
    private static readonly Color AccentGlowColor = new Color(0.36f, 0.88f, 1f, 0.95f);
    private static readonly Color TitleColor = new Color(0.96f, 0.985f, 1f, 1f);
    private static readonly Color LabelColor = new Color(0.79f, 0.9f, 0.98f, 1f);
    private static readonly Color ValueColor = new Color(0.96f, 0.985f, 1f, 1f);
    private static readonly Color BadgeColor = new Color(0.16f, 0.48f, 0.82f, 0.95f);
    private static readonly Color BadgeExcellentColor = new Color(0.2f, 0.72f, 0.48f, 0.97f);
    private static readonly Color BadgeGoodColor = new Color(0.17f, 0.57f, 0.85f, 0.97f);
    private static readonly Color BadgeAverageColor = new Color(0.86f, 0.62f, 0.21f, 0.97f);
    private static readonly Color BadgePoorColor = new Color(0.74f, 0.29f, 0.34f, 0.97f);
    private static readonly Color RetryColor = new Color(0.21f, 0.62f, 0.9f, 0.92f);
    private static readonly Color MenuColor = new Color(0.16f, 0.73f, 0.56f, 0.92f);
    private static readonly Color VignetteColor = new Color(0.02f, 0.08f, 0.14f, 0.42f);
    private static readonly Color BottomAccentColor = new Color(0.18f, 0.74f, 0.95f, 0.72f);
    private static readonly Color SummaryCardColor = new Color(0.1f, 0.28f, 0.44f, 0.42f);
    private static readonly Color ScoreCardColor = new Color(0.12f, 0.34f, 0.52f, 0.52f);
    private static readonly Color AdvancedCardColor = new Color(0.08f, 0.22f, 0.38f, 0.42f);
    private static readonly Color TotalStatCardColor = new Color(0.11f, 0.31f, 0.5f, 0.52f);
    private static readonly Color CorrectStatCardColor = new Color(0.11f, 0.37f, 0.3f, 0.56f);
    private static readonly Color IncorrectStatCardColor = new Color(0.44f, 0.18f, 0.24f, 0.56f);
    private static readonly Color DividerColor = new Color(0.36f, 0.88f, 1f, 0.4f);

    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI summaryText;
    [SerializeField] private TextMeshProUGUI successText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private TextMeshProUGUI totalLabelText;
    [SerializeField] private TextMeshProUGUI totalValueText;
    [SerializeField] private TextMeshProUGUI correctLabelText;
    [SerializeField] private TextMeshProUGUI correctValueText;
    [SerializeField] private TextMeshProUGUI incorrectLabelText;
    [SerializeField] private TextMeshProUGUI incorrectValueText;
    [SerializeField] private TextMeshProUGUI advancedStatsText;
    [SerializeField] private TextMeshProUGUI scoreBadgeText;
    [SerializeField] private Image scoreBadgeImage;

    [Header("Animasyon")]
    [SerializeField] private float openDuration = 0.25f;
    [SerializeField] private float closeDuration = 0.16f;
    [SerializeField] private float hiddenPanelScale = 0f;

    [Header("Yerleşim")]
    [SerializeField] private Vector2 actionButtonSize = new Vector2(300f, 108f);
    [SerializeField] private float actionButtonBottomY = -250f;
    [SerializeField] private float actionButtonHorizontalOffset = 190f;
    [SerializeField] private float badgePulseSpeed = 2.2f;
    [SerializeField] private float badgePulseAmount = 0.055f;

    private bool actionInProgress;
    private RectTransform panelRect;
    private Coroutine visibilityRoutine;
    private Coroutine badgePulseRoutine;
    private Image summaryCardImage;
    private Image scoreCardImage;
    private Image advancedCardImage;
    private Image totalStatCardImage;
    private Image correctStatCardImage;
    private Image incorrectStatCardImage;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (titleText != null)
        {
            panelRect = titleText.transform.parent as RectTransform;
        }

        if (panelRect == null)
        {
            panelRect = transform.Find("Panel") as RectTransform;
        }

        ResolveMissingReferences();
        EnsurePanelLayout();
        ApplyBlueGlassStyle();

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(ReloadScene);
            retryButton.onClick.AddListener(ReloadScene);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(LoadMenuScene);
            menuButton.onClick.AddListener(LoadMenuScene);
        }

        HidePanelImmediate();
    }

    private void OnDestroy()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(ReloadScene);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(LoadMenuScene);
        }

        HospitalTriageManager.Instance?.ReleaseUiFocus(this);
    }

    public void ShowPanel(int toplam, int dogru, int yanlis)
    {
        UpdateStats(toplam, dogru, yanlis);
        ShowPanelInternal();
    }

    public void ShowPanel(HospitalTriageManager.TriageResultSnapshot snapshot)
    {
        UpdateStats(snapshot);
        ShowPanelInternal();
    }

    private void ShowPanelInternal()
    {
        if (titleText != null)
        {
            titleText.text = "TRİYAJ SONUÇLARI";
        }

        actionInProgress = false;
        if (retryButton != null)
        {
            retryButton.interactable = true;
        }

        if (menuButton != null)
        {
            menuButton.interactable = true;
        }

        StartBadgePulse();
        AnimateVisibility(true);
    }

    public void HidePanel()
    {
        StopBadgePulse();
        AnimateVisibility(false);
    }

    public void UpdateStats(int toplam, int dogru, int yanlis)
    {
        HospitalTriageManager manager = HospitalTriageManager.Instance;
        if (manager != null)
        {
            HospitalTriageManager.TriageResultSnapshot liveSnapshot = manager.GetResultSnapshot();
            bool hasLiveData = liveSnapshot.totalCount > 0 || liveSnapshot.completedCount > 0;
            if (hasLiveData)
            {
                UpdateStats(liveSnapshot);
                return;
            }
        }

        UpdateStats(BuildFallbackSnapshot(toplam, dogru, yanlis));
    }

    public void UpdateStats(HospitalTriageManager.TriageResultSnapshot snapshot)
    {
        int safeTotal = Mathf.Max(0, snapshot.totalCount);
        int safeCompleted = Mathf.Clamp(snapshot.completedCount, 0, safeTotal);
        int safeCorrect = Mathf.Clamp(snapshot.correctCount, 0, safeCompleted);
        int safeIncorrect = Mathf.Clamp(snapshot.incorrectCount, 0, safeCompleted);
        int safePending = Mathf.Max(0, safeTotal - safeCompleted);
        float accuracyScore = Mathf.Clamp(snapshot.accuracyPercent, 0f, 100f);
        string gradeLabel = ResolvePerformanceLabel(accuracyScore);

        if (totalLabelText != null)
        {
            totalLabelText.text = "TAMAMLANAN";
        }

        if (totalValueText != null)
        {
            totalValueText.text = $"{safeCompleted}/{safeTotal}";
        }

        if (correctLabelText != null)
        {
            correctLabelText.text = "DOĞRU";
        }

        if (correctValueText != null)
        {
            correctValueText.text = safeCorrect.ToString();
        }

        if (incorrectLabelText != null)
        {
            incorrectLabelText.text = "YANLIŞ";
        }

        if (incorrectValueText != null)
        {
            incorrectValueText.text = safeIncorrect.ToString();
        }

        string totalDuration = FormatDuration(snapshot.durationSeconds);
        string activeDuration = FormatDuration(snapshot.activeDecisionSeconds);
        string avgDuration = FormatDuration(snapshot.averageDecisionSeconds);
        string correctRatio = safeCompleted > 0 ? $"{safeCorrect}/{safeCompleted}" : "0/0";

        if (summaryText != null)
        {
            summaryText.color = ValueColor;
            summaryText.alignment = TextAlignmentOptions.TopLeft;
            summaryText.fontWeight = FontWeight.SemiBold;
            summaryText.text =
                $"<size=108%><b><color=#9ED9F8>Genel Durum: {gradeLabel}</color></b></size>\n" +
                $"<size=89%><color=#CAE7FF>Tamamlanma</color> %{snapshot.completionPercent:0.0}   •   " +
                $"<color=#CAE7FF>Doğruluk</color> %{accuracyScore:0.0}   •   <color=#CAE7FF>Bekleyen</color> {safePending}</size>\n" +
                $"<size=82%><color=#CAE7FF>Toplam</color> {totalDuration}   •   " +
                $"<color=#CAE7FF>Aktif</color> {activeDuration}   •   " +
                $"<color=#CAE7FF>Ort. karar</color> {avgDuration}</size>";
        }

        if (advancedStatsText != null)
        {
            advancedStatsText.text = BuildAdvancedStatsText(snapshot);
        }

        if (scoreBadgeText != null)
        {
            scoreBadgeText.text =
                "<size=64%><b>TEMPO</b></size>\n" +
                $"<size=145%><b>{snapshot.patientsPerMinute:0.00}</b></size>\n" +
                "<size=58%>hasta/dk</size>\n" +
                $"<size=48%>Seri {snapshot.longestCorrectStreak} • Kritik {snapshot.criticalMismatchCount}</size>";
        }

        if (successText != null)
        {
            successText.alignment = TextAlignmentOptions.Center;
            successText.text =
                $"<size=208%><b>%{accuracyScore:0}</b></size>\n" +
                $"<size=76%>{gradeLabel}</size>\n" +
                $"<size=60%>{correctRatio} doğru • %{snapshot.completionPercent:0} tamamlanma</size>";
        }

        ApplyScoreTheme(accuracyScore);
    }

    private static HospitalTriageManager.TriageResultSnapshot BuildFallbackSnapshot(int toplam, int dogru, int yanlis)
    {
        HospitalTriageManager.TriageResultSnapshot snapshot = new HospitalTriageManager.TriageResultSnapshot();
        snapshot.totalCount = Mathf.Max(0, toplam);
        snapshot.correctCount = Mathf.Clamp(dogru, 0, snapshot.totalCount);
        snapshot.incorrectCount = Mathf.Clamp(yanlis, 0, snapshot.totalCount);
        snapshot.completedCount = Mathf.Clamp(snapshot.correctCount + snapshot.incorrectCount, 0, snapshot.totalCount);
        snapshot.pendingCount = Mathf.Max(0, snapshot.totalCount - snapshot.completedCount);
        snapshot.accuracyPercent = snapshot.completedCount > 0
            ? (snapshot.correctCount * 100f) / snapshot.completedCount
            : 0f;
        snapshot.completionPercent = snapshot.totalCount > 0
            ? (snapshot.completedCount * 100f) / snapshot.totalCount
            : 0f;
        snapshot.averageDecisionSeconds = 0f;
        snapshot.durationSeconds = 0f;
        snapshot.activeDecisionSeconds = 0f;
        snapshot.patientsPerMinute = 0f;
        return snapshot;
    }

    public void ReloadScene()
    {
        if (actionInProgress)
        {
            return;
        }

        StartCoroutine(ReloadSceneRoutine());
    }

    public void LoadMenuScene()
    {
        if (actionInProgress)
        {
            return;
        }

        StartCoroutine(LoadMenuSceneRoutine());
    }

    private IEnumerator ReloadSceneRoutine()
    {
        BeginUiAction();
        yield return null;

        HospitalTriageManager hospitalManager = HospitalTriageManager.Instance;
        if (hospitalManager == null)
        {
            Debug.LogError("[TriageResultPanel] Hastane yöneticisi bulunamadı, tekrar dene akışı çalıştırılamadı.");
            RestoreUiAfterCancelledAction();
            yield break;
        }

        hospitalManager.EnterHospitalPhase();
        if (!hospitalManager.ResetHospitalScenario())
        {
            RestoreUiAfterCancelledAction();
            yield break;
        }

        if (!hospitalManager.RespawnPlayerAtHospitalStart())
        {
            Debug.LogError("[TriageResultPanel] Hastane retry anchor çözülemediği için tekrar dene tamamlanamadı.");
            RestoreUiAfterCancelledAction();
            yield break;
        }

        actionInProgress = false;
    }

    private IEnumerator LoadMenuSceneRoutine()
    {
        BeginUiAction();
        yield return null;

        HospitalTriageManager hospitalManager = HospitalTriageManager.Instance;
        if (hospitalManager == null)
        {
            Debug.LogError("[TriageResultPanel] Hastane yöneticisi bulunamadı, üsse dönüş akışı çalıştırılamadı.");
            RestoreUiAfterCancelledAction();
            yield break;
        }

        if (!hospitalManager.ResetHospitalScenario())
        {
            RestoreUiAfterCancelledAction();
            yield break;
        }

        if (!hospitalManager.ReturnPlayerToBase())
        {
            Debug.LogError("[TriageResultPanel] Base dönüş anchor çözülemediği için üsse dönüş tamamlanamadı.");
            RestoreUiAfterCancelledAction();
            yield break;
        }

        hospitalManager.ExitHospitalPhaseToBase();
        actionInProgress = false;
    }

    private void BeginUiAction()
    {
        actionInProgress = true;

        if (retryButton != null)
        {
            retryButton.interactable = false;
        }

        if (menuButton != null)
        {
            menuButton.interactable = false;
        }

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
        }

        // VRUIClickHelper clickHelper = GetComponent<VRUIClickHelper>();
        // clickHelper?.SuppressInput(0.25f);
    }

    private void RestoreUiAfterCancelledAction()
    {
        actionInProgress = false;

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (retryButton != null)
        {
            retryButton.interactable = true;
        }

        if (menuButton != null)
        {
            menuButton.interactable = true;
        }

        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private void ResolveMissingReferences()
    {
        if (panelRect == null)
        {
            panelRect = transform.Find("Panel") as RectTransform;
        }

        titleText ??= FindPanelComponent<TextMeshProUGUI>("TitleText");
        summaryText ??= FindPanelComponent<TextMeshProUGUI>("SummaryText");
        successText ??= FindPanelComponent<TextMeshProUGUI>("SuccessText");
        totalLabelText ??= FindPanelComponent<TextMeshProUGUI>("TotalLabelText");
        totalValueText ??= FindPanelComponent<TextMeshProUGUI>("TotalValueText");
        correctLabelText ??= FindPanelComponent<TextMeshProUGUI>("CorrectLabelText");
        correctValueText ??= FindPanelComponent<TextMeshProUGUI>("CorrectValueText");
        incorrectLabelText ??= FindPanelComponent<TextMeshProUGUI>("IncorrectLabelText");
        incorrectValueText ??= FindPanelComponent<TextMeshProUGUI>("IncorrectValueText");
        scoreBadgeText ??= FindPanelComponent<TextMeshProUGUI>("ScoreBadgeText");
        advancedStatsText ??= FindPanelComponent<TextMeshProUGUI>("AdvancedStatsText");

        if (scoreBadgeImage == null)
        {
            scoreBadgeImage = FindPanelComponent<Image>("ScoreBadge");
        }

        retryButton ??= FindPanelComponent<Button>("RetryButton");
        menuButton ??= FindPanelComponent<Button>("MenuButton");
    }

    private T FindPanelComponent<T>(string objectName) where T : Component
    {
        Transform searchRoot = panelRect != null ? panelRect : transform;
        if (searchRoot == null)
        {
            return null;
        }

        Transform directMatch = searchRoot.Find(objectName);
        if (directMatch != null)
        {
            return directMatch.GetComponent<T>();
        }

        T[] components = searchRoot.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && components[i].name == objectName)
            {
                return components[i];
            }
        }

        return null;
    }

    private void EnsureAdvancedStatsText()
    {
        if (panelRect == null)
        {
            return;
        }

        if (advancedStatsText == null)
        {
            advancedStatsText = FindPanelComponent<TextMeshProUGUI>("AdvancedStatsText");
        }

        if (advancedStatsText == null)
        {
            GameObject statsObject = new GameObject("AdvancedStatsText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            statsObject.transform.SetParent(panelRect, false);
            advancedStatsText = statsObject.GetComponent<TextMeshProUGUI>();
        }

        if (advancedStatsText == null)
        {
            return;
        }

        RectTransform statsRect = advancedStatsText.rectTransform;
        statsRect.anchorMin = new Vector2(0.08f, 0.22f);
        statsRect.anchorMax = new Vector2(0.92f, 0.34f);
        statsRect.offsetMin = Vector2.zero;
        statsRect.offsetMax = Vector2.zero;
        statsRect.pivot = new Vector2(0.5f, 0.5f);

        if (retryButton != null)
        {
            int retryIndex = retryButton.transform.GetSiblingIndex();
            if (advancedStatsText.transform.GetSiblingIndex() > retryIndex)
            {
                advancedStatsText.transform.SetSiblingIndex(retryIndex);
            }
        }
    }

    private void ApplyBlueGlassStyle()
    {
        ResolveMissingReferences();
        EnsurePanelLayout();

        if (panelRect != null)
        {
            Image panelImage = panelRect.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = PanelColor;
            }
        }

        EnsurePanelShadow();
        EnsureTopAccent();
        EnsureBottomAccent();
        EnsureInnerVignette();
        EnsureVisualSections();

        if (titleText != null)
        {
            titleText.text = "TRİYAJ SONUÇLARI";
            titleText.color = TitleColor;
            titleText.fontWeight = FontWeight.Bold;
            titleText.fontSize = Mathf.Max(titleText.fontSize, 60f);
            titleText.characterSpacing = 7f;
            titleText.alignment = TextAlignmentOptions.Center;
        }

        if (summaryText != null)
        {
            summaryText.color = ValueColor;
            summaryText.fontWeight = FontWeight.SemiBold;
            summaryText.fontSize = Mathf.Max(summaryText.fontSize, 32f);
            summaryText.enableAutoSizing = true;
            summaryText.fontSizeMin = 20f;
            summaryText.fontSizeMax = 34f;
            summaryText.lineSpacing = 4f;
            summaryText.overflowMode = TextOverflowModes.Truncate;
        }

        if (successText != null)
        {
            successText.color = TitleColor;
            successText.fontWeight = FontWeight.Bold;
            successText.fontSize = Mathf.Max(successText.fontSize, 52f);
            successText.enableAutoSizing = true;
            successText.fontSizeMin = 28f;
            successText.fontSizeMax = 56f;
            successText.lineSpacing = 3f;
        }

        if (advancedStatsText != null)
        {
            advancedStatsText.color = LabelColor;
            advancedStatsText.fontWeight = FontWeight.SemiBold;
            advancedStatsText.alignment = TextAlignmentOptions.TopLeft;
            advancedStatsText.fontSize = Mathf.Max(advancedStatsText.fontSize, 22f);
            advancedStatsText.enableAutoSizing = true;
            advancedStatsText.fontSizeMin = 16f;
            advancedStatsText.fontSizeMax = 24f;
            advancedStatsText.lineSpacing = 4f;
            advancedStatsText.overflowMode = TextOverflowModes.Truncate;
            advancedStatsText.raycastTarget = false;
        }

        ApplyLabelStyle(totalLabelText);
        ApplyLabelStyle(correctLabelText);
        ApplyLabelStyle(incorrectLabelText);
        ApplyValueStyle(totalValueText);
        ApplyValueStyle(correctValueText);
        ApplyValueStyle(incorrectValueText);

        if (scoreBadgeImage != null)
        {
            scoreBadgeImage.color = BadgeColor;
        }

        if (scoreBadgeText != null)
        {
            scoreBadgeText.color = TitleColor;
            scoreBadgeText.fontWeight = FontWeight.Bold;
            scoreBadgeText.alignment = TextAlignmentOptions.Center;
            scoreBadgeText.fontSize = Mathf.Max(scoreBadgeText.fontSize, 36f);
            scoreBadgeText.characterSpacing = 1.1f;
            scoreBadgeText.enableAutoSizing = true;
            scoreBadgeText.fontSizeMin = 17f;
            scoreBadgeText.fontSizeMax = 40f;
            scoreBadgeText.lineSpacing = 2f;
        }

        StyleButton(retryButton, RetryColor, "TEKRAR DENE");
        StyleButton(menuButton, MenuColor, "ÜSSE DÖN");
    }

    private void EnsurePanelLayout()
    {
        EnsureAdvancedStatsText();

        if (panelRect != null)
        {
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            Vector2 panelSize = panelRect.sizeDelta;
            panelRect.sizeDelta = new Vector2(
                Mathf.Max(panelSize.x, 1180f),
                Mathf.Max(panelSize.y, 760f));
        }

        if (titleText != null)
        {
            SetRectAnchor(titleText.rectTransform, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.95f));
        }

        if (summaryText != null)
        {
            SetRectAnchor(summaryText.rectTransform, new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.81f));
        }

        LayoutStatPair(
            totalLabelText,
            totalValueText,
            new Vector2(0.08f, 0.59f),
            new Vector2(0.32f, 0.64f),
            new Vector2(0.08f, 0.53f),
            new Vector2(0.32f, 0.6f));

        LayoutStatPair(
            correctLabelText,
            correctValueText,
            new Vector2(0.38f, 0.59f),
            new Vector2(0.62f, 0.64f),
            new Vector2(0.38f, 0.53f),
            new Vector2(0.62f, 0.6f));

        LayoutStatPair(
            incorrectLabelText,
            incorrectValueText,
            new Vector2(0.68f, 0.59f),
            new Vector2(0.92f, 0.64f),
            new Vector2(0.68f, 0.53f),
            new Vector2(0.92f, 0.6f));

        if (scoreBadgeImage != null)
        {
            SetRectAnchor(scoreBadgeImage.rectTransform, new Vector2(0.63f, 0.36f), new Vector2(0.92f, 0.54f));
        }

        if (scoreBadgeText != null)
        {
            SetRectAnchor(scoreBadgeText.rectTransform, new Vector2(0.63f, 0.36f), new Vector2(0.92f, 0.54f));
        }

        if (successText != null)
        {
            SetRectAnchor(successText.rectTransform, new Vector2(0.08f, 0.37f), new Vector2(0.6f, 0.52f));
        }

        if (advancedStatsText != null)
        {
            SetRectAnchor(advancedStatsText.rectTransform, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.34f));
        }

        if (retryButton != null)
        {
            RectTransform retryRect = retryButton.transform as RectTransform;
            if (retryRect != null)
            {
                retryRect.anchorMin = new Vector2(0.5f, 0.5f);
                retryRect.anchorMax = new Vector2(0.5f, 0.5f);
                retryRect.pivot = new Vector2(0.5f, 0.5f);
                retryRect.anchoredPosition = new Vector2(-actionButtonHorizontalOffset, actionButtonBottomY);
                retryRect.sizeDelta = actionButtonSize;
                retryRect.localScale = Vector3.one;
            }
        }

        if (menuButton != null)
        {
            RectTransform menuRect = menuButton.transform as RectTransform;
            if (menuRect != null)
            {
                menuRect.anchorMin = new Vector2(0.5f, 0.5f);
                menuRect.anchorMax = new Vector2(0.5f, 0.5f);
                menuRect.pivot = new Vector2(0.5f, 0.5f);
                menuRect.anchoredPosition = new Vector2(actionButtonHorizontalOffset, actionButtonBottomY);
                menuRect.sizeDelta = actionButtonSize;
                menuRect.localScale = Vector3.one;
            }
        }
    }

    private static void LayoutStatPair(
        TextMeshProUGUI label,
        TextMeshProUGUI value,
        Vector2 labelAnchorMin,
        Vector2 labelAnchorMax,
        Vector2 valueAnchorMin,
        Vector2 valueAnchorMax)
    {
        if (label != null)
        {
            SetRectAnchor(label.rectTransform, labelAnchorMin, labelAnchorMax);
        }

        if (value != null)
        {
            SetRectAnchor(value.rectTransform, valueAnchorMin, valueAnchorMax);
        }
    }

    private static void SetRectAnchor(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private void EnsurePanelShadow()
    {
        if (panelRect == null)
        {
            return;
        }

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
        shadowRect.anchorMin = Vector2.zero;
        shadowRect.anchorMax = Vector2.one;
        shadowRect.offsetMin = new Vector2(30f, -28f);
        shadowRect.offsetMax = new Vector2(12f, -10f);
    }

    private void EnsureTopAccent()
    {
        if (panelRect == null)
        {
            return;
        }

        Transform existing = panelRect.Find("TopAccent");
        RectTransform accentRect = existing as RectTransform;
        if (accentRect == null)
        {
            GameObject accent = new GameObject("TopAccent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            accent.transform.SetParent(panelRect, false);
            accentRect = accent.GetComponent<RectTransform>();
            accentRect.SetAsLastSibling();
        }

        Image accentImage = accentRect.GetComponent<Image>();
        accentImage.color = AccentGlowColor;
        accentImage.raycastTarget = false;
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = new Vector2(0f, -4f);
        accentRect.sizeDelta = new Vector2(0f, 6f);
    }

    private void EnsureBottomAccent()
    {
        if (panelRect == null)
        {
            return;
        }

        Transform existing = panelRect.Find("BottomAccent");
        RectTransform accentRect = existing as RectTransform;
        if (accentRect == null)
        {
            GameObject accent = new GameObject("BottomAccent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            accent.transform.SetParent(panelRect, false);
            accentRect = accent.GetComponent<RectTransform>();
            accentRect.SetAsFirstSibling();
        }

        Image accentImage = accentRect.GetComponent<Image>();
        accentImage.color = BottomAccentColor;
        accentImage.raycastTarget = false;
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(1f, 0f);
        accentRect.pivot = new Vector2(0.5f, 0f);
        accentRect.anchoredPosition = new Vector2(0f, 4f);
        accentRect.sizeDelta = new Vector2(0f, 4f);
    }

    private void EnsureInnerVignette()
    {
        if (panelRect == null)
        {
            return;
        }

        Transform existing = panelRect.Find("PanelVignette");
        RectTransform vignetteRect = existing as RectTransform;
        if (vignetteRect == null)
        {
            GameObject vignette = new GameObject("PanelVignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            vignette.transform.SetParent(panelRect, false);
            vignetteRect = vignette.GetComponent<RectTransform>();
            vignetteRect.SetAsFirstSibling();
        }

        Image vignetteImage = vignetteRect.GetComponent<Image>();
        vignetteImage.color = VignetteColor;
        vignetteImage.raycastTarget = false;
        vignetteRect.anchorMin = Vector2.zero;
        vignetteRect.anchorMax = Vector2.one;
        vignetteRect.offsetMin = new Vector2(18f, 18f);
        vignetteRect.offsetMax = new Vector2(-18f, -18f);
    }

    private void EnsureVisualSections()
    {
        if (panelRect == null)
        {
            return;
        }

        summaryCardImage = EnsureSectionCard(
            "SummaryCard",
            new Vector2(0.06f, 0.66f),
            new Vector2(0.94f, 0.83f),
            SummaryCardColor,
            summaryText != null ? summaryText.transform : null);

        totalStatCardImage = EnsureSectionCard(
            "TotalStatCard",
            new Vector2(0.07f, 0.52f),
            new Vector2(0.33f, 0.65f),
            TotalStatCardColor,
            GetEarliestSiblingTransform(
                totalLabelText != null ? totalLabelText.transform : null,
                totalValueText != null ? totalValueText.transform : null));

        correctStatCardImage = EnsureSectionCard(
            "CorrectStatCard",
            new Vector2(0.37f, 0.52f),
            new Vector2(0.63f, 0.65f),
            CorrectStatCardColor,
            GetEarliestSiblingTransform(
                correctLabelText != null ? correctLabelText.transform : null,
                correctValueText != null ? correctValueText.transform : null));

        incorrectStatCardImage = EnsureSectionCard(
            "IncorrectStatCard",
            new Vector2(0.67f, 0.52f),
            new Vector2(0.93f, 0.65f),
            IncorrectStatCardColor,
            GetEarliestSiblingTransform(
                incorrectLabelText != null ? incorrectLabelText.transform : null,
                incorrectValueText != null ? incorrectValueText.transform : null));

        scoreCardImage = EnsureSectionCard(
            "ScoreCard",
            new Vector2(0.06f, 0.35f),
            new Vector2(0.94f, 0.55f),
            ScoreCardColor,
            GetEarliestSiblingTransform(
                successText != null ? successText.transform : null,
                scoreBadgeImage != null ? scoreBadgeImage.transform : null,
                scoreBadgeText != null ? scoreBadgeText.transform : null));

        advancedCardImage = EnsureSectionCard(
            "AdvancedCard",
            new Vector2(0.06f, 0.2f),
            new Vector2(0.94f, 0.35f),
            AdvancedCardColor,
            advancedStatsText != null ? advancedStatsText.transform : null);

        EnsureDividerLine("DividerTop", 0.655f);
        EnsureDividerLine("DividerBottom", 0.35f);
    }

    private static Transform GetEarliestSiblingTransform(params Transform[] transforms)
    {
        Transform earliestTransform = null;
        int earliestIndex = int.MaxValue;
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current == null)
            {
                continue;
            }

            int index = current.GetSiblingIndex();
            if (index < earliestIndex)
            {
                earliestIndex = index;
                earliestTransform = current;
            }
        }

        return earliestTransform;
    }

    private Image EnsureSectionCard(
        string cardName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color,
        Transform contentTransform)
    {
        Transform existing = panelRect.Find(cardName);
        RectTransform cardRect = existing as RectTransform;
        if (cardRect == null)
        {
            GameObject card = new GameObject(cardName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(panelRect, false);
            cardRect = card.GetComponent<RectTransform>();
        }

        cardRect.anchorMin = anchorMin;
        cardRect.anchorMax = anchorMax;
        cardRect.offsetMin = Vector2.zero;
        cardRect.offsetMax = Vector2.zero;
        cardRect.pivot = new Vector2(0.5f, 0.5f);

        Image cardImage = cardRect.GetComponent<Image>();
        cardImage.color = color;
        cardImage.raycastTarget = false;

        if (contentTransform != null)
        {
            int contentIndex = contentTransform.GetSiblingIndex();
            int targetIndex = Mathf.Max(0, contentIndex - 1);
            if (cardRect.GetSiblingIndex() != targetIndex)
            {
                cardRect.SetSiblingIndex(targetIndex);
            }
        }

        return cardImage;
    }

    private void EnsureDividerLine(string dividerName, float normalizedY)
    {
        Transform existing = panelRect.Find(dividerName);
        RectTransform dividerRect = existing as RectTransform;
        if (dividerRect == null)
        {
            GameObject divider = new GameObject(dividerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            divider.transform.SetParent(panelRect, false);
            dividerRect = divider.GetComponent<RectTransform>();
        }

        dividerRect.anchorMin = new Vector2(0.08f, normalizedY);
        dividerRect.anchorMax = new Vector2(0.92f, normalizedY);
        dividerRect.offsetMin = new Vector2(0f, -1f);
        dividerRect.offsetMax = new Vector2(0f, 1f);
        dividerRect.pivot = new Vector2(0.5f, 0.5f);

        Image dividerImage = dividerRect.GetComponent<Image>();
        dividerImage.color = DividerColor;
        dividerImage.raycastTarget = false;
    }

    private void ApplyLabelStyle(TextMeshProUGUI label)
    {
        if (label == null)
        {
            return;
        }

        label.color = LabelColor;
        label.fontWeight = FontWeight.SemiBold;
        label.fontSize = Mathf.Max(label.fontSize, 24f);
        label.enableAutoSizing = true;
        label.fontSizeMin = 16f;
        label.fontSizeMax = 26f;
        label.alignment = TextAlignmentOptions.Center;
        label.characterSpacing = 1.6f;
    }

    private void ApplyValueStyle(TextMeshProUGUI value)
    {
        if (value == null)
        {
            return;
        }

        value.color = ValueColor;
        value.fontWeight = FontWeight.Bold;
        value.fontSize = Mathf.Max(value.fontSize, 44f);
        value.enableAutoSizing = true;
        value.fontSizeMin = 28f;
        value.fontSizeMax = 54f;
        value.alignment = TextAlignmentOptions.Center;
        value.characterSpacing = 1.2f;
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
            label.color = ValueColor;
            label.fontWeight = FontWeight.Bold;
            label.fontSize = Mathf.Max(label.fontSize, 28f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 19f;
            label.fontSizeMax = 30f;
            label.characterSpacing = 1.2f;
            label.alignment = TextAlignmentOptions.Center;
        }

        TriageButtonHoverFeedback hoverFeedback = button.GetComponent<TriageButtonHoverFeedback>();
        if (hoverFeedback == null)
        {
            hoverFeedback = button.gameObject.AddComponent<TriageButtonHoverFeedback>();
        }

        hoverFeedback.Configure(baseColor);

        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = button.gameObject.AddComponent<LayoutElement>();
        }

        layout.minWidth = actionButtonSize.x;
        layout.minHeight = actionButtonSize.y;
        layout.preferredWidth = actionButtonSize.x;
        layout.preferredHeight = actionButtonSize.y;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;
    }

    private static string BuildAdvancedStatsText(HospitalTriageManager.TriageResultSnapshot snapshot)
    {
        StringBuilder builder = new StringBuilder(320);
        builder.Append("<size=86%><color=#BFE7FF><b>RİSK</b></color>  ");
        builder.Append("Eksik ").Append(snapshot.underTriageCount).Append("  •  ");
        builder.Append("Fazla ").Append(snapshot.overTriageCount).Append("  •  ");
        builder.Append("Kritik ").Append(snapshot.criticalMismatchCount).Append("  •  ");
        builder.Append("Seri ").Append(snapshot.longestCorrectStreak).Append("</size>\n");

        builder.Append("<size=84%><color=#BFE7FF><b>SÜRE</b></color>  ");
        builder.Append("Hızlı ").Append(FormatDuration(snapshot.fastestDecisionSeconds)).Append("  •  ");
        builder.Append("Medyan ").Append(FormatDuration(snapshot.medianDecisionSeconds)).Append("  •  ");
        builder.Append("Yavaş ").Append(FormatDuration(snapshot.slowestDecisionSeconds)).Append("</size>\n");

        builder.Append("<size=82%><color=#BFE7FF><b>KATEGORİ DOĞRULUK</b></color>  ");
        builder.Append("Yeşil %").Append(snapshot.greenAccuracyPercent.ToString("0")).Append("  •  ");
        builder.Append("Sarı %").Append(snapshot.yellowAccuracyPercent.ToString("0")).Append("  •  ");
        builder.Append("Kırmızı %").Append(snapshot.redAccuracyPercent.ToString("0")).Append("  •  ");
        builder.Append("Siyah %").Append(snapshot.blackAccuracyPercent.ToString("0")).Append("</size>");

        return builder.ToString();
    }

    private static string FormatDuration(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int remainingSeconds = totalSeconds % 60;

        if (hours > 0)
        {
            return $"{hours} sa {minutes} dk {remainingSeconds} sn";
        }

        if (minutes > 0)
        {
            return $"{minutes} dk {remainingSeconds} sn";
        }

        return $"{remainingSeconds} sn";
    }

    private static string ResolvePerformanceLabel(float score)
    {
        if (score >= 90f)
        {
            return "Mükemmel";
        }

        if (score >= 75f)
        {
            return "Çok İyi";
        }

        if (score >= 55f)
        {
            return "Gelişiyor";
        }

        return "Tekrar Gerekli";
    }

    private void ApplyScoreTheme(float score)
    {
        Color targetColor = BadgePoorColor;
        if (score >= 90f)
        {
            targetColor = BadgeExcellentColor;
        }
        else if (score >= 75f)
        {
            targetColor = BadgeGoodColor;
        }
        else if (score >= 55f)
        {
            targetColor = BadgeAverageColor;
        }

        if (scoreBadgeImage != null)
        {
            scoreBadgeImage.color = targetColor;
        }

        if (successText != null)
        {
            successText.color = Color.Lerp(TitleColor, targetColor, 0.35f);
        }

        if (scoreBadgeText != null)
        {
            scoreBadgeText.color = Color.Lerp(TitleColor, targetColor, 0.18f);
        }

        if (advancedStatsText != null)
        {
            advancedStatsText.color = Color.Lerp(LabelColor, targetColor, 0.18f);
        }

        if (summaryCardImage != null)
        {
            summaryCardImage.color = Color.Lerp(SummaryCardColor, targetColor, 0.2f);
        }

        if (scoreCardImage != null)
        {
            scoreCardImage.color = Color.Lerp(ScoreCardColor, targetColor, 0.26f);
        }

        if (advancedCardImage != null)
        {
            advancedCardImage.color = Color.Lerp(AdvancedCardColor, targetColor, 0.18f);
        }

        if (totalStatCardImage != null)
        {
            totalStatCardImage.color = Color.Lerp(TotalStatCardColor, targetColor, 0.12f);
        }

        if (correctStatCardImage != null)
        {
            correctStatCardImage.color = Color.Lerp(CorrectStatCardColor, targetColor, 0.08f);
        }

        if (incorrectStatCardImage != null)
        {
            incorrectStatCardImage.color = Color.Lerp(IncorrectStatCardColor, targetColor, 0.08f);
        }
    }

    private void AnimateVisibility(bool visible)
    {
        if (visible)
        {
            HospitalTriageManager.Instance?.AcquireUiFocus(this);
        }
        else if (!gameObject.activeInHierarchy)
        {
            HidePanelImmediate();
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

        visibilityRoutine = StartCoroutine(AnimateVisibilityRoutine(visible));
    }

    private IEnumerator AnimateVisibilityRoutine(bool visible)
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
            float eased = visible
                ? 1f - Mathf.Pow(1f - t, 3f)
                : t * t * (3f - 2f * t);
            float scaleOvershoot = visible
                ? 1f + Mathf.Sin(t * Mathf.PI) * 0.035f
                : 1f;

            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
            if (panelRect != null)
            {
                panelRect.localScale = Vector3.Lerp(startScale, targetScale * scaleOvershoot, eased);
            }

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        if (panelRect != null)
        {
            panelRect.localScale = targetScale;
        }

        canvasGroup.interactable = visible && !actionInProgress;
        canvasGroup.blocksRaycasts = visible;
        visibilityRoutine = null;

        if (!visible)
        {
            StopBadgePulse();
            HospitalTriageManager.Instance?.ReleaseUiFocus(this);
            gameObject.SetActive(false);
        }
    }

    private void HidePanelImmediate()
    {
        StopBadgePulse();

        if (panelRect != null)
        {
            panelRect.localScale = Vector3.one * hiddenPanelScale;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        HospitalTriageManager.Instance?.ReleaseUiFocus(this);
        gameObject.SetActive(false);
    }

    private void StartBadgePulse()
    {
        if (scoreBadgeImage == null)
        {
            return;
        }

        if (badgePulseRoutine != null)
        {
            StopCoroutine(badgePulseRoutine);
        }

        badgePulseRoutine = StartCoroutine(BadgePulseRoutine());
    }

    private void StopBadgePulse()
    {
        if (badgePulseRoutine != null)
        {
            StopCoroutine(badgePulseRoutine);
            badgePulseRoutine = null;
        }

        if (scoreBadgeImage != null)
        {
            scoreBadgeImage.transform.localScale = Vector3.one;
        }
    }

    private IEnumerator BadgePulseRoutine()
    {
        while (gameObject.activeInHierarchy && scoreBadgeImage != null)
        {
            float wave = 1f + Mathf.Sin(Time.unscaledTime * badgePulseSpeed) * badgePulseAmount;
            scoreBadgeImage.transform.localScale = new Vector3(wave, wave, 1f);
            yield return null;
        }

        badgePulseRoutine = null;
    }
}
