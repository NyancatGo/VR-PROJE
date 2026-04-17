using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Legacy uyumluluk katmani: Eski VRKeyboard API cagrilarini VRKeyboardManager'a yonlendirir.
/// </summary>
[DisallowMultipleComponent]
public class VRKeyboard : MonoBehaviour
{
    [Header("Optional Overrides")]
    [SerializeField] private VRKeyboardManager keyboardManager;
    [SerializeField] private TMP_InputField targetInputField;
    [SerializeField] private Button sendButton;

    /// <summary>
    /// Eski API ile cagrilan acma islemi.
    /// </summary>
    public void Show(TextMeshProUGUI targetText)
    {
        EnsureKeyboardManager();
        if (keyboardManager == null)
        {
            Debug.LogWarning("[VRKeyboard] VRKeyboardManager bulunamadi.");
            return;
        }

        TMP_InputField resolvedInput = ResolveInputField(targetText);
        if (resolvedInput != null)
        {
            keyboardManager.SyncReferences(resolvedInput, sendButton);
            resolvedInput.Select();
            resolvedInput.ActivateInputField();
        }

        keyboardManager.ShowKeyboard();
    }

    /// <summary>
    /// Eski API icin kapatma destegi.
    /// </summary>
    public void Hide()
    {
        EnsureKeyboardManager();
        keyboardManager?.HideKeyboard();
    }

    private void EnsureKeyboardManager()
    {
        if (keyboardManager != null)
        {
            return;
        }

        keyboardManager = GetComponent<VRKeyboardManager>();
        if (keyboardManager == null)
        {
            keyboardManager = GetComponentInParent<VRKeyboardManager>(true);
        }

        if (keyboardManager == null)
        {
            keyboardManager = FindObjectOfType<VRKeyboardManager>(true);
        }
    }

    private TMP_InputField ResolveInputField(TextMeshProUGUI targetText)
    {
        if (targetInputField != null)
        {
            return targetInputField;
        }

        if (targetText != null)
        {
            TMP_InputField fromTarget = targetText.GetComponentInParent<TMP_InputField>(true);
            if (fromTarget != null)
            {
                return fromTarget;
            }
        }

        TMP_InputField fromChildren = GetComponentInChildren<TMP_InputField>(true);
        if (fromChildren != null)
        {
            return fromChildren;
        }

        return keyboardManager != null ? keyboardManager.GetComponentInChildren<TMP_InputField>(true) : null;
    }
}
