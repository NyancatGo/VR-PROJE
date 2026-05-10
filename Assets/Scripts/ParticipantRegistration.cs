using System;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using TrainingAnalytics;

public static class ParticipantManager
{
    private const string ParticipantKeyPref = "training.analytics.participant_key";
    private const string ParticipantNamePref = "training.analytics.participant_name";
    // Katılımcının ilk kez login yaptığı UTC ISO-8601 zaman damgası.
    // Sonraki update'lerde override EDİLMEZ — yalnızca ilk yazımda set edilir.
    // Böylece katilimcilar/{key} dokümanındaki "baslangic" alanı stabil kalır.
    private const string ParticipantBaslangicPref = "training.analytics.participant_baslangic";

    /// <summary>
    /// Bu uygulama oturumunda kullanıcı en az bir kez login formunu doldurmuş
    /// mu? Static olduğu için process açık kaldığı sürece korunur (sahneler
    /// arası geçişlerde sıfırlanmaz). Uygulama kapatılıp açıldığında otomatik
    /// olarak false döner — yani her oyun başlangıcında bir kez login istenir.
    /// </summary>
    public static bool HasLoggedInThisSession { get; private set; }

#if UNITY_EDITOR
    // Editor'da "Reload Domain" kapalı olsa bile her Play başlangıcında
    // sıfırlanmasını garanti edelim. Build'de zaten process restart ile sıfır.
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionStateOnPlayStart()
    {
        HasLoggedInThisSession = false;
    }
#endif

    public static bool HasParticipant =>
        !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(ParticipantKeyPref, string.Empty));

    public static string GetParticipantKey() =>
        PlayerPrefs.GetString(ParticipantKeyPref, string.Empty);

    public static string GetParticipantName() =>
        PlayerPrefs.GetString(ParticipantNamePref, string.Empty);

    public static string SaveParticipant(string ad, string soyad)
    {
        string fullName = (ad.Trim() + " " + soyad.Trim()).Trim();
        string key = NormalizeTurkishKey(fullName);

        bool isNewParticipant =
            !string.Equals(PlayerPrefs.GetString(ParticipantKeyPref, string.Empty), key, System.StringComparison.Ordinal);

        PlayerPrefs.SetString(ParticipantKeyPref, key);
        PlayerPrefs.SetString(ParticipantNamePref, fullName);

        // baslangic SADECE ilk kez bu key için set ediliyorsa veya pref daha
        // önce yazılmamışsa damgalanır. Aynı kullanıcı tekrar login yaparsa
        // (PlayerPrefs zaten dolu) baslangic eskisi kalır.
        string existingBaslangic = PlayerPrefs.GetString(ParticipantBaslangicPref, string.Empty);
        if (isNewParticipant || string.IsNullOrWhiteSpace(existingBaslangic))
        {
            PlayerPrefs.SetString(ParticipantBaslangicPref, System.DateTime.UtcNow.ToString("O"));
        }

        PlayerPrefs.Save();
        HasLoggedInThisSession = true;
        return key;
    }

    /// <summary>
    /// Katılımcı dokümanı için kalıcı "baslangic" zaman damgası. SaveParticipant
    /// ile yeni katılımcı oluşturulduğunda yazılır, sonraki güncellemelerde
    /// ezilmez. WriteParticipantProfile bu değeri Firestore'a aktarır.
    /// </summary>
    public static string GetParticipantBaslangic() =>
        PlayerPrefs.GetString(ParticipantBaslangicPref, string.Empty);

    /// <summary>
    /// Önceki oyun oturumundan kalan PlayerPrefs verisini "bu oturumda da
    /// kullanılır" olarak işaretler. Otomatik kullanım yok — sadece test/QA
    /// veya özel akışlar isterse çağırır.
    /// </summary>
    public static void MarkSessionLoggedIn()
    {
        HasLoggedInThisSession = true;
    }

    public static void ClearParticipant()
    {
        PlayerPrefs.DeleteKey(ParticipantKeyPref);
        PlayerPrefs.DeleteKey(ParticipantNamePref);
        PlayerPrefs.DeleteKey(ParticipantBaslangicPref);
        PlayerPrefs.Save();
        HasLoggedInThisSession = false;
    }

    public static string NormalizeTurkishKey(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "unknown";
        }

        string lowered = input.ToLowerInvariant();
        StringBuilder sb = new StringBuilder(lowered.Length);

        for (int i = 0; i < lowered.Length; i++)
        {
            char c = lowered[i];
            switch (c)
            {
                case '\u0131': sb.Append('i'); break; // ı
                case '\u015F': sb.Append('s'); break; // ş
                case '\u011F': sb.Append('g'); break; // ğ
                case '\u00FC': sb.Append('u'); break; // ü
                case '\u00F6': sb.Append('o'); break; // ö
                case '\u00E7': sb.Append('c'); break; // ç
                case '\u0130': sb.Append('i'); break; // İ (capital dotted I, lowercase form)
                case '\u015E': sb.Append('s'); break; // Ş
                case '\u011E': sb.Append('g'); break; // Ğ
                case '\u00DC': sb.Append('u'); break; // Ü
                case '\u00D6': sb.Append('o'); break; // Ö
                case '\u00C7': sb.Append('c'); break; // Ç
                case ' ':     sb.Append('_'); break;
                default:
                    if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_')
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }

        string result = sb.ToString().Trim('_');
        return string.IsNullOrWhiteSpace(result) ? "unknown" : result;
    }
}

[DisallowMultipleComponent]
public class LoginPanelController : MonoBehaviour
{
    [SerializeField] private CanvasGroup loginPanel;
    [SerializeField] private TMP_InputField adField;
    [SerializeField] private TMP_InputField soyadField;
    [SerializeField] private Button devamButton;
    [SerializeField] private VRKeyboardManager keyboardManager;
    [SerializeField] private TextMeshProUGUI warningText;

    public event Action OnLoginCompleted;
    private bool applyingKeyboardFocus;

    private void Start()
    {
        // Inspector referansları boş olabilir (panel runtime'da oluşturulmuş
        // veya sahne baked'inde unutulmuş). Bu durumda kendimiz arayalım.
        AutoResolveMissingReferences();

        if (devamButton != null)
        {
            // Listener zaten eklenmiş olabilir (component birden çok kez Start
            // alırsa). Önce kaldır, sonra ekle — duplicate fire yok.
            devamButton.onClick.RemoveListener(OnDevamClicked);
            devamButton.onClick.AddListener(OnDevamClicked);
        }
        else
        {
            Debug.LogWarning("[LoginPanelController] devamButton bulunamadi; DEVAM ET tıklamaları işlenmeyecek.");
        }

        if (adField != null)
        {
            adField.onSelect.AddListener(_ => SwitchKeyboardTarget(adField));
            EnableLoginInputTyping(adField);
        }

        if (soyadField != null)
        {
            soyadField.onSelect.AddListener(_ => SwitchKeyboardTarget(soyadField));
            EnableLoginInputTyping(soyadField);
        }

        AttachInputRouter(adField);
        AttachInputRouter(soyadField);
    }

    /// <summary>
    /// Inspector'da bağlanmamış referansları runtime'da kendi kendine bulur.
    /// Sahnede LoginPanelController unutulmuş veya panel runtime'da
    /// oluşturulmuşsa kullanıcının manuel sürükle-bırak yapmasına gerek
    /// kalmadan login akışı çalışır hale gelir.
    /// </summary>
    private void AutoResolveMissingReferences()
    {
        Transform root = transform;

        if (adField == null)
        {
            adField = FindInputFieldByName(root, "Ad_InputField", "AdField", "Ad");
        }
        if (soyadField == null)
        {
            soyadField = FindInputFieldByName(root, "Soyad_InputField", "SoyadField", "Soyad");
        }
        // Fallback: sıralı olarak ilk iki TMP_InputField → Ad ve Soyad sayılır.
        if (adField == null || soyadField == null)
        {
            TMP_InputField[] all = GetComponentsInChildren<TMP_InputField>(true);
            if (all.Length >= 1 && adField == null) adField = all[0];
            if (all.Length >= 2 && soyadField == null) soyadField = all[1];
        }

        if (devamButton == null)
        {
            devamButton = FindButtonByName(root, "DevamButton", "DevamEtButton", "Devam_Button", "DEVAM ET");
        }
        // Fallback: panel altında "DEVAM" / "Devam" içeren ilk Button.
        if (devamButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                string n = buttons[i].name ?? string.Empty;
                if (n.IndexOf("devam", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    devamButton = buttons[i];
                    break;
                }
                // Label kontrolü (button text "DEVAM ET" olabilir).
                TextMeshProUGUI label = buttons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null && label.text != null &&
                    label.text.IndexOf("devam", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    devamButton = buttons[i];
                    break;
                }
            }
        }

        if (keyboardManager == null)
        {
            keyboardManager = GetComponentInChildren<VRKeyboardManager>(true);
            if (keyboardManager == null)
            {
                keyboardManager = GetComponentInParent<VRKeyboardManager>();
            }
        }

        Debug.Log("[LoginPanelController] AutoResolve → ad=" + (adField != null) +
                  " soyad=" + (soyadField != null) +
                  " devam=" + (devamButton != null) +
                  " keyboard=" + (keyboardManager != null));
    }

    private static TMP_InputField FindInputFieldByName(Transform root, params string[] names)
    {
        if (root == null) return null;
        TMP_InputField[] all = root.GetComponentsInChildren<TMP_InputField>(true);
        for (int n = 0; n < names.Length; n++)
        {
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && string.Equals(all[i].name, names[n], System.StringComparison.OrdinalIgnoreCase))
                {
                    return all[i];
                }
            }
        }
        return null;
    }

    private static Button FindButtonByName(Transform root, params string[] names)
    {
        if (root == null) return null;
        Button[] all = root.GetComponentsInChildren<Button>(true);
        for (int n = 0; n < names.Length; n++)
        {
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && string.Equals(all[i].name, names[n], System.StringComparison.OrdinalIgnoreCase))
                {
                    return all[i];
                }
            }
        }
        return null;
    }

    private void OnDisable()
    {
        if (devamButton != null)
        {
            devamButton.onClick.RemoveListener(OnDevamClicked);
        }
    }

    public void ShowWarning(string message)
    {
        if (warningText == null) return;
        warningText.text = message;
        warningText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    private void OnDevamClicked()
    {
        if (adField == null || soyadField == null) return;
        ShowWarning(string.Empty);

        string ad = adField.text.Trim();
        string soyad = soyadField.text.Trim();
        TrySplitFullNameFromSingleField(ref ad, ref soyad);

        if (string.IsNullOrWhiteSpace(ad))
        {
            ShowWarning("Lutfen adinizi girin.");
            FocusLoginField(adField);
            return;
        }

        if (string.IsNullOrWhiteSpace(soyad))
        {
            ShowWarning("Lutfen soyadinizi girin.");
            FocusLoginField(soyadField);
            return;
        }

        string key = ParticipantManager.SaveParticipant(ad, soyad);
        string fullName = ParticipantManager.GetParticipantName();

        AnalyticsService service = AnalyticsService.EnsureInitializedSingleton();
        service.SetParticipantContext(key, fullName);
        service.WriteParticipantProfile();
        TrainingAnalyticsFacade.InitializeFullSessionReport();

        OnLoginCompleted?.Invoke();
    }

    private void TrySplitFullNameFromSingleField(ref string ad, ref string soyad)
    {
        if (!string.IsNullOrWhiteSpace(soyad) || string.IsNullOrWhiteSpace(ad))
        {
            return;
        }

        string[] parts = ad.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return;
        }

        soyad = parts[parts.Length - 1];
        ad = string.Join(" ", parts, 0, parts.Length - 1);
        if (adField != null)
        {
            adField.text = ad;
        }

        if (soyadField != null)
        {
            soyadField.text = soyad;
        }
    }

    private void SwitchKeyboardTarget(TMP_InputField field)
    {
        if (field == null || applyingKeyboardFocus)
        {
            return;
        }

        if (keyboardManager != null)
        {
            // SyncTargetInputField records the user's explicit intent so the
            // keyboard never silently snaps back to the previous field on the
            // next keystroke (the Ad/Soyad swap bug).
            keyboardManager.SyncTargetInputField(field);
            keyboardManager.ShowKeyboard();
        }

        EnableLoginInputTyping(field);

        applyingKeyboardFocus = true;
        try
        {
            DeactivateOtherLoginField(field);
            // SetSelectedGameObject'i sadece gerçekten farklı bir obje seçiliyse
            // çağır — aksi halde Unity "Attempting to select X while already
            // selecting an object" warning'i basıyor.
            EventSystem es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject != field.gameObject)
            {
                es.SetSelectedGameObject(field.gameObject);
            }
            if (!field.isFocused)
            {
                field.Select();
                field.ActivateInputField();
            }
            field.MoveTextEnd(false);
        }
        finally
        {
            applyingKeyboardFocus = false;
        }
    }

    public void FocusLoginField(TMP_InputField field)
    {
        if (field == null)
        {
            return;
        }

        SwitchKeyboardTarget(field);
    }

    private void DeactivateOtherLoginField(TMP_InputField activeField)
    {
        if (adField != null && adField != activeField)
        {
            adField.DeactivateInputField(false);
        }

        if (soyadField != null && soyadField != activeField)
        {
            soyadField.DeactivateInputField(false);
        }
    }

    private void AttachInputRouter(TMP_InputField field)
    {
        if (field == null)
        {
            return;
        }

        LoginInputFieldRouter router = field.GetComponent<LoginInputFieldRouter>();
        if (router == null)
        {
            router = field.gameObject.AddComponent<LoginInputFieldRouter>();
        }

        router.Configure(this, field);
    }

    private static void EnableLoginInputTyping(TMP_InputField field)
    {
        if (field == null)
        {
            return;
        }

        field.readOnly = false;
        field.onValidateInput = null;
        field.shouldHideMobileInput = true;
        field.resetOnDeActivation = false;
        field.restoreOriginalTextOnEscape = false;
    }

    public void Initialize(
        CanvasGroup panel,
        TMP_InputField ad,
        TMP_InputField soyad,
        Button devam,
        VRKeyboardManager keyboard)
    {
        loginPanel = panel;
        adField = ad;
        soyadField = soyad;
        devamButton = devam;
        keyboardManager = keyboard;
    }
}

[DisallowMultipleComponent]
public class LoginInputFieldRouter : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, ISelectHandler
{
    [SerializeField] private LoginPanelController controller;
    [SerializeField] private TMP_InputField inputField;

    public void Configure(LoginPanelController owner, TMP_InputField field)
    {
        controller = owner;
        inputField = field;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        FocusTarget();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        FocusTarget();
    }

    public void OnSelect(BaseEventData eventData)
    {
        FocusTarget();
    }

    private void FocusTarget()
    {
        if (controller != null && inputField != null)
        {
            controller.FocusLoginField(inputField);
        }
    }
}

/// <summary>
/// Login ekranindaki bir input field icin odaklanma gorselligi: secildiginde sol
/// accent bari ve alt cizgisi parlar, label rengi yogunlasir. Boylece kullanici
/// hangi alana yazdigini net gorur ve Ad/Soyad arasinda kayboldugunu hissetmez.
/// </summary>
[DisallowMultipleComponent]
public class LoginInputFieldFocus : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private UnityEngine.UI.Image leftAccentBar;
    [SerializeField] private UnityEngine.UI.Image bottomLine;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private UnityEngine.UI.Outline fieldOutline;

    private static readonly Color AccentIdle    = new Color(0.06f, 0.55f, 0.78f, 0.55f);
    private static readonly Color AccentFocused = new Color(0.10f, 0.95f, 1.00f, 1.00f);
    private static readonly Color LineIdle      = new Color(0.06f, 0.82f, 1.00f, 0.30f);
    private static readonly Color LineFocused   = new Color(0.10f, 0.95f, 1.00f, 1.00f);
    private static readonly Color LabelIdle     = new Color(0.06f, 0.82f, 1.00f, 0.55f);
    private static readonly Color LabelFocused  = new Color(0.10f, 0.95f, 1.00f, 1.00f);
    private static readonly Color OutlineIdle   = new Color(0.06f, 0.82f, 1.00f, 0.00f);
    private static readonly Color OutlineFocused = new Color(0.10f, 0.95f, 1.00f, 0.65f);

    public void Configure(
        TMP_InputField field,
        UnityEngine.UI.Image accent,
        UnityEngine.UI.Image line,
        TextMeshProUGUI labelText,
        UnityEngine.UI.Outline outline)
    {
        inputField = field;
        leftAccentBar = accent;
        bottomLine = line;
        label = labelText;
        fieldOutline = outline;
        ApplyIdleState();
    }

    private void OnEnable()
    {
        if (inputField == null) return;
        inputField.onSelect.RemoveListener(HandleSelected);
        inputField.onSelect.AddListener(HandleSelected);
        inputField.onDeselect.RemoveListener(HandleDeselected);
        inputField.onDeselect.AddListener(HandleDeselected);
        ApplyIdleState();
    }

    private void OnDisable()
    {
        if (inputField == null) return;
        inputField.onSelect.RemoveListener(HandleSelected);
        inputField.onDeselect.RemoveListener(HandleDeselected);
    }

    private void HandleSelected(string _)   => ApplyFocusedState();
    private void HandleDeselected(string _) => ApplyIdleState();

    private void ApplyFocusedState()
    {
        if (leftAccentBar != null) leftAccentBar.color = AccentFocused;
        if (bottomLine != null)    bottomLine.color    = LineFocused;
        if (label != null)         label.color         = LabelFocused;
        if (fieldOutline != null)
        {
            fieldOutline.effectColor = OutlineFocused;
            fieldOutline.effectDistance = new Vector2(2f, -2f);
        }
    }

    private void ApplyIdleState()
    {
        if (leftAccentBar != null) leftAccentBar.color = AccentIdle;
        if (bottomLine != null)    bottomLine.color    = LineIdle;
        if (label != null)         label.color         = LabelIdle;
        if (fieldOutline != null)
        {
            fieldOutline.effectColor = OutlineIdle;
            fieldOutline.effectDistance = new Vector2(1f, -1f);
        }
    }
}
