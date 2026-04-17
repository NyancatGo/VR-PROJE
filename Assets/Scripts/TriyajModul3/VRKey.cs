using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum VRKeyType
{
    Character,
    Space,
    Backspace,
    Clear,
    MoveLeft,
    MoveRight,
    Enter,
    Close
}

/// <summary>
/// Her VR klavye tusu icin hover animasyonu ve tiklama davranisini yonetir.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class VRKey : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private const float DuplicateClickGuardSeconds = 0.08f;

    [SerializeField] private VRKeyboardManager keyboardManager;
    [SerializeField] private VRKeyType keyType = VRKeyType.Character;
    [SerializeField] private string keyValue = string.Empty;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI keyLabel;
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float pressedScale = 0.955f;
    [SerializeField] private float scaleLerpSpeed = 20f;
    [SerializeField] private float colorLerpSpeed = 20f;
    [SerializeField] private Color hoverTint = new Color(0.2f, 0.76f, 1f, 0.98f);
    [SerializeField] private Color pressedTint = new Color(0.1f, 0.48f, 0.76f, 0.99f);
    [SerializeField] private Color labelHoverColor = new Color(1f, 1f, 1f, 1f);

    private Button button;
    private RectTransform rectTransform;
    private Vector3 initialScale = Vector3.one;
    private Color idleColor = Color.white;
    private Color idleLabelColor = Color.white;
    private bool isHovered;
    private bool isPressed;
    private float lastHandledClickTime = -10f;
    private int lastHandledClickFrame = -1;

    private void Awake()
    {
        CacheReferences();
        InitializeVisualState();
    }

    private void OnEnable()
    {
        CacheReferences();
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
            button.transition = Selectable.Transition.None;
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }

        isHovered = false;
        isPressed = false;
        ApplyVisualsInstant();
    }

    private void OnValidate()
    {
        CacheReferences();
        ApplyLabel();
    }

    private void Update()
    {
        if (rectTransform == null || backgroundImage == null)
        {
            return;
        }

        float targetScaleValue = isPressed ? pressedScale : (isHovered ? hoverScale : 1f);
        Color targetColor = isPressed ? BuildPressedColor() : (isHovered ? BuildHoverColor() : idleColor);
        Color targetLabelColor = isHovered || isPressed ? labelHoverColor : idleLabelColor;

        float scaleT = 1f - Mathf.Exp(-scaleLerpSpeed * Time.unscaledDeltaTime);
        float colorT = 1f - Mathf.Exp(-colorLerpSpeed * Time.unscaledDeltaTime);

        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, initialScale * targetScaleValue, scaleT);
        backgroundImage.color = Color.Lerp(backgroundImage.color, targetColor, colorT);

        if (keyLabel != null)
        {
            keyLabel.color = Color.Lerp(keyLabel.color, targetLabelColor, colorT);
        }
    }

    public void Configure(VRKeyboardManager manager, VRKeyType type, string value, string labelText)
    {
        keyboardManager = manager;
        keyType = type;
        keyValue = value;

        CacheReferences();

        if (keyLabel != null)
        {
            keyLabel.text = labelText;
            keyLabel.raycastTarget = false;
        }

        RefreshIdleVisualState();
        ApplyVisualsInstant();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    private void HandleClick()
    {
        int currentFrame = Time.frameCount;
        float currentTime = Time.unscaledTime;
        if (lastHandledClickFrame == currentFrame || currentTime - lastHandledClickTime < DuplicateClickGuardSeconds)
        {
            return;
        }

        lastHandledClickFrame = currentFrame;
        lastHandledClickTime = currentTime;

        if (keyboardManager == null)
        {
            keyboardManager = GetComponentInParent<VRKeyboardManager>(true);
            if (keyboardManager == null)
            {
                return;
            }
        }

        switch (keyType)
        {
            case VRKeyType.Space:
                keyboardManager.HandleSpace();
                break;
            case VRKeyType.Backspace:
                keyboardManager.HandleBackspace();
                break;
            case VRKeyType.Clear:
                keyboardManager.HandleClearAll();
                break;
            case VRKeyType.MoveLeft:
                keyboardManager.HandleMoveCaretLeft();
                break;
            case VRKeyType.MoveRight:
                keyboardManager.HandleMoveCaretRight();
                break;
            case VRKeyType.Enter:
                keyboardManager.HandleEnter();
                break;
            case VRKeyType.Close:
                keyboardManager.HandleClose();
                break;
            default:
                keyboardManager.HandleCharacter(keyValue);
                break;
        }
    }

    private void CacheReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        if (keyLabel == null)
        {
            keyLabel = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private void InitializeVisualState()
    {
        RefreshIdleVisualState();
        ApplyLabel();
        ApplyVisualsInstant();
    }

    private void RefreshIdleVisualState()
    {
        if (rectTransform != null)
        {
            initialScale = rectTransform.localScale;
        }

        if (backgroundImage != null)
        {
            idleColor = backgroundImage.color;
        }

        if (keyLabel != null)
        {
            idleLabelColor = keyLabel.color;
        }
    }

    private void ApplyLabel()
    {
        if (keyLabel == null)
        {
            return;
        }

        if (keyType == VRKeyType.Character && !string.IsNullOrEmpty(keyValue))
        {
            keyLabel.text = keyValue;
            return;
        }

        if (!string.IsNullOrEmpty(keyLabel.text))
        {
            return;
        }

        switch (keyType)
        {
            case VRKeyType.Space:
                keyLabel.text = "Bo\u015fluk";
                break;
            case VRKeyType.Backspace:
                keyLabel.text = "Sil";
                break;
            case VRKeyType.Clear:
                keyLabel.text = "Temiz";
                break;
            case VRKeyType.MoveLeft:
                keyLabel.text = "Sol";
                break;
            case VRKeyType.MoveRight:
                keyLabel.text = "Sag";
                break;
            case VRKeyType.Enter:
                keyLabel.text = "Enter";
                break;
            case VRKeyType.Close:
                keyLabel.text = "Kapat";
                break;
        }
    }

    private void ApplyVisualsInstant()
    {
        if (rectTransform != null)
        {
            rectTransform.localScale = initialScale;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = idleColor;
        }

        if (keyLabel != null)
        {
            keyLabel.color = idleLabelColor;
        }
    }

    private Color BuildHoverColor()
    {
        return Color.Lerp(idleColor, hoverTint, 0.72f);
    }

    private Color BuildPressedColor()
    {
        return Color.Lerp(idleColor, pressedTint, 0.88f);
    }
}
