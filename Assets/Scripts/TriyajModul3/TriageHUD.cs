using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TriageHUD : MonoBehaviour
{
    private static readonly Color PanelColor = new Color(0.04f, 0.1f, 0.18f, 0.68f);
    private static readonly Color ShadowColor = new Color(0.005f, 0.02f, 0.06f, 0.5f);
    private static readonly Color AccentColor = new Color(0.35f, 0.9f, 1f, 0.96f);
    private static readonly Color TrackColor = new Color(0.12f, 0.25f, 0.36f, 0.74f);
    private static readonly Color FillColor = new Color(0.42f, 0.95f, 1f, 0.98f);
    private static readonly Color TextColor = new Color(0.94f, 0.985f, 1f, 1f);

    [Header("Referanslar")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Image panelImage;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Image progressBarTrackImage;
    [SerializeField] private Image progressBarFillImage;

    [Header("Takip")]
    [SerializeField] private bool followPlayerCamera = true;
    [SerializeField] private float distanceFromCamera = 1.26f;
    [SerializeField] private float verticalOffset = 0.58f;

    private bool isVisible;

    public static TriageHUD CreateRuntimeHud()
    {
        GameObject root = new GameObject("TriageHUDCanvas");
        TriageHUD hud = root.AddComponent<TriageHUD>();
        hud.EnsureRuntimeScaffold();
        hud.RefreshProgress(0, 0);
        hud.SetVisible(false);
        return hud;
    }

    private void Awake()
    {
        EnsureRuntimeScaffold();
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (!isVisible || !followPlayerCamera)
        {
            return;
        }

        Transform cameraTransform = XRCameraHelper.GetPlayerCameraTransform();
        if (cameraTransform == null)
        {
            return;
        }

        Vector3 targetPosition = cameraTransform.position + cameraTransform.forward * distanceFromCamera;
        targetPosition += cameraTransform.up * verticalOffset;

        transform.position = targetPosition;
        Quaternion targetRotation = Quaternion.LookRotation(targetPosition - cameraTransform.position, cameraTransform.up);
        transform.rotation = targetRotation;
    }

    public void RefreshProgress(int completed, int total)
    {
        EnsureRuntimeScaffold();
        int safeCompleted = Mathf.Max(0, completed);
        int safeTotal = Mathf.Max(0, total);
        float progressRatio = safeTotal > 0 ? Mathf.Clamp01((float)safeCompleted / safeTotal) : 0f;

        if (progressText != null)
        {
            int progressPercent = Mathf.RoundToInt(progressRatio * 100f);
            progressText.text = $"<size=60%><color=#A5CBDE>MÜDAHALE EDİLEN HASTA</color></size>   <b><color=#80EAFF>{safeCompleted}</color><color=#EAF8FF> / {safeTotal}</color></b>   <size=56%><color=#93D7EC>%{progressPercent}</color></size>";
        }

        if (progressBarFillImage != null)
        {
            progressBarFillImage.fillAmount = progressRatio;
        }
    }

    public void SetVisible(bool visible)
    {
        EnsureRuntimeScaffold();
        isVisible = visible;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    private void EnsureRuntimeScaffold()
    {
        if (rootCanvas == null)
        {
            rootCanvas = GetComponent<Canvas>();
            if (rootCanvas == null)
            {
                rootCanvas = gameObject.AddComponent<Canvas>();
            }
        }

        rootCanvas.renderMode = RenderMode.WorldSpace;
        rootCanvas.worldCamera = XRCameraHelper.GetPlayerCamera();

        RectTransform rootRect = rootCanvas.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(860f, 116f);
        rootRect.localScale = new Vector3(0.00105f, 0.00105f, 0.00105f);

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        scaler.dynamicPixelsPerUnit = 16f;
        scaler.referencePixelsPerUnit = 100f;

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (panelRect == null)
        {
            panelRect = EnsureChildRectTransform(transform, "Panel");
        }

        if (panelImage == null)
        {
            panelImage = panelRect.GetComponent<Image>();
            if (panelImage == null)
            {
                panelImage = panelRect.gameObject.AddComponent<Image>();
            }
        }

        panelImage.color = PanelColor;
        SetStretch(panelRect, new Vector2(18f, 14f), new Vector2(-18f, -14f));
        EnsureTopAccent(panelRect);
        EnsureProgressBar(panelRect);

        EnsureShadowPlate(rootRect);

        if (progressText == null)
        {
            RectTransform textRect = EnsureChildRectTransform(panelRect, "ProgressText");
            progressText = textRect.GetComponent<TextMeshProUGUI>();
            if (progressText == null)
            {
                progressText = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            }
        }

        progressText.color = TextColor;
        progressText.richText = true;
        progressText.fontSize = 34f;
        progressText.fontWeight = FontWeight.SemiBold;
        progressText.alignment = TextAlignmentOptions.Center;
        progressText.enableWordWrapping = false;
        progressText.enableAutoSizing = true;
        progressText.fontSizeMin = 22f;
        progressText.fontSizeMax = 34f;
        progressText.characterSpacing = 1.1f;
        progressText.lineSpacing = -6f;
        progressText.outlineWidth = 0.06f;
        progressText.outlineColor = new Color(0.02f, 0.1f, 0.18f, 0.95f);
        SetStretch(progressText.rectTransform, new Vector2(30f, 10f), new Vector2(-30f, -22f));

        SetLayerRecursively(transform, ResolveUiLayer());
    }

    private void EnsureShadowPlate(RectTransform rootRect)
    {
        Transform existing = rootRect.Find("PanelShadow");
        RectTransform shadowRect = existing as RectTransform;
        if (shadowRect == null)
        {
            shadowRect = EnsureChildRectTransform(rootRect, "PanelShadow");
            shadowRect.SetSiblingIndex(0);
        }

        Image shadowImage = shadowRect.GetComponent<Image>();
        if (shadowImage == null)
        {
            shadowImage = shadowRect.gameObject.AddComponent<Image>();
        }

        shadowImage.color = ShadowColor;
        SetStretch(shadowRect, new Vector2(26f, 20f), new Vector2(8f, 8f));
    }

    private static void EnsureTopAccent(RectTransform panel)
    {
        Transform existing = panel.Find("TopAccent");
        RectTransform accentRect = existing as RectTransform;
        if (accentRect == null)
        {
            accentRect = EnsureChildRectTransform(panel, "TopAccent");
            accentRect.SetSiblingIndex(panel.childCount - 1);
        }

        Image accentImage = accentRect.GetComponent<Image>();
        if (accentImage == null)
        {
            accentImage = accentRect.gameObject.AddComponent<Image>();
        }

        accentImage.color = AccentColor;
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = new Vector2(0f, -1f);
        accentRect.sizeDelta = new Vector2(0f, 4f);
    }

    private void EnsureProgressBar(RectTransform panel)
    {
        RectTransform barTrackRect = EnsureChildRectTransform(panel, "ProgressBarTrack");
        if (progressBarTrackImage == null)
        {
            progressBarTrackImage = barTrackRect.GetComponent<Image>();
            if (progressBarTrackImage == null)
            {
                progressBarTrackImage = barTrackRect.gameObject.AddComponent<Image>();
            }
        }

        progressBarTrackImage.color = TrackColor;
        barTrackRect.anchorMin = new Vector2(0f, 0f);
        barTrackRect.anchorMax = new Vector2(1f, 0f);
        barTrackRect.pivot = new Vector2(0.5f, 0f);
        barTrackRect.anchoredPosition = new Vector2(0f, 8f);
        barTrackRect.sizeDelta = new Vector2(-56f, 8f);

        RectTransform barFillRect = EnsureChildRectTransform(barTrackRect, "ProgressBarFill");
        if (progressBarFillImage == null)
        {
            progressBarFillImage = barFillRect.GetComponent<Image>();
            if (progressBarFillImage == null)
            {
                progressBarFillImage = barFillRect.gameObject.AddComponent<Image>();
            }
        }

        progressBarFillImage.color = FillColor;
        progressBarFillImage.type = Image.Type.Filled;
        progressBarFillImage.fillMethod = Image.FillMethod.Horizontal;
        progressBarFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressBarFillImage.fillAmount = 0f;
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = Vector2.one;
        barFillRect.offsetMin = Vector2.zero;
        barFillRect.offsetMax = Vector2.zero;
    }

    private static RectTransform EnsureChildRectTransform(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing is RectTransform existingRect)
        {
            return existingRect;
        }

        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        RectTransform rect = child.AddComponent<RectTransform>();
        rect.localScale = Vector3.one;
        child.AddComponent<CanvasRenderer>();
        return rect;
    }

    private static void SetStretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static int ResolveUiLayer()
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        return uiLayer >= 0 ? uiLayer : 5;
    }

    private static void SetLayerRecursively(Transform root, int layer)
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
}
