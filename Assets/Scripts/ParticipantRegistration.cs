using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrainingAnalytics;

public static class ParticipantManager
{
    private const string ParticipantKeyPref = "training.analytics.participant_key";
    private const string ParticipantNamePref = "training.analytics.participant_name";

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
        PlayerPrefs.SetString(ParticipantKeyPref, key);
        PlayerPrefs.SetString(ParticipantNamePref, fullName);
        PlayerPrefs.Save();
        return key;
    }

    public static void ClearParticipant()
    {
        PlayerPrefs.DeleteKey(ParticipantKeyPref);
        PlayerPrefs.DeleteKey(ParticipantNamePref);
        PlayerPrefs.Save();
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

    private void Start()
    {
        // Burada da daha önceden var olan auto-login bypas'ını kaldırdık.
        // Form hep gösterilecek.

        if (devamButton != null)
        {
            devamButton.onClick.AddListener(OnDevamClicked);
        }

        if (adField != null)
        {
            adField.onSelect.AddListener(_ => SwitchKeyboardTarget(adField));
        }

        if (soyadField != null)
        {
            soyadField.onSelect.AddListener(_ => SwitchKeyboardTarget(soyadField));
        }
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

        if (string.IsNullOrWhiteSpace(ad) || string.IsNullOrWhiteSpace(soyad))
        {
            return;
        }

        string key = ParticipantManager.SaveParticipant(ad, soyad);
        string fullName = ParticipantManager.GetParticipantName();

        AnalyticsService service = AnalyticsService.Instance;
        if (service != null)
        {
            service.SetParticipantContext(key, fullName);
            service.WriteParticipantProfile();
        }

        OnLoginCompleted?.Invoke();
    }

    private void SwitchKeyboardTarget(TMP_InputField field)
    {
        if (keyboardManager != null && field != null)
        {
            keyboardManager.SyncReferences(field, devamButton);
        }
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
