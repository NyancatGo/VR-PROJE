using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using TrainingAnalytics;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[System.Serializable]
public enum DoctorPanelTab
{
    AIChat,
    MiniTest
}

[System.Serializable]
public class MiniTestQuestion
{
    [TextArea(2, 4)] public string soru = string.Empty;
    public string[] siklar = new string[3];
    [Range(0, 2)] public int dogruIndex;
}

/// <summary>
/// Proje icindeki yapay zeka sohbet akisini yonetir.
/// </summary>
public class AIManager : MonoBehaviour
{
    private const string RuntimeAnchorHolderName = "__AI_RuntimeAnchors";
    private const string RuntimeOpenAnchorName = "AI_RuntimeOpenAnchor";
    private const string PlayerMountName = "AI_PlayerCanvasMount";
    private const string CloseButtonName = "Close_Button";
    private const string CloseButtonTextName = "Close_ButtonText";
    private const string LeftControllerVisualName = "Controller Visual Left";
    private const string RightControllerVisualName = "Controller Visual Right";
    private const string TtsVoiceListUrl = "https://api.minimax.io/v1/get_voice";
    private const int DefaultChatMaxTokens = 360;
    private const int TriageHintMaxTokens = 120;
    private const int MinDoctorChatMessageWindow = 4;
    private const int DefaultDoctorChatMessageWindow = 8;
    private const int MaxDoctorChatMessageWindow = 10;
    private const float DefaultDoctorChatTemperature = 0.45f;
    private const string PreferredDoctorMinimaxModel = "MiniMax-M2.5";
    private const string DoctorReplyStylePrompt =
        "Her yaniti sadece Turkce ver. " +
        "Her zaman kisa, net, kolay anlasilir ve dogrudan yaz. " +
        "Genel, bos veya zayif cevap verme. " +
        "Kisa kal ama kararlilik gosteren, ise yarar ve sahaya yon veren cumleler kur. " +
        "En fazla 3 kisa cumle kullan. " +
        "Sadece duz cumleler kur. " +
        "Robot gibi konusma; dogal, sakin, karar veren ve kendinden emin bir saha doktoru gibi konus. " +
        "Saha durumu notu verilirse bunu arka plan bilgisi olarak kullan. " +
        "Kullanici ozellikle sonuc, sayi, triyaj, hastane durumu veya ilerleyis sormadikca bu bilgileri dogrudan tekrar etme. " +
        "Saha durumu verisini kelimesi kelimesine kopyalama; kisa tibbi yoruma cevir. " +
        "Bu tur icin ozel bir yonlendirme verildiyse ona uy. " +
        "En acil riske odaklan, gerekirse bir net tespit ve bir net yonlendirme ver. " +
        "Madde imi, numara, baslik, etiket, emoji, markdown, suslu anlatim, parantez icinde sahne notu veya gereksiz aciklama kullanma. " +
        "Sacma karakter, bozuk kodlama, sembol zinciri ve garip unicode karakterler kullanma. " +
        "Dogru Turkce karakterleri kullan; ornek harfler: \u00e7, \u011f, \u0131, \u0130, \u00f6, \u015f, \u00fc. " +
        "Gerekmedikce tani listesi verme. Pratik, sakin ve aksiyon odakli konus. " +
        "Kullanici basit bir soru sorarsa sicak ama oz cevap ver. Kullanici tibbi durum anlatirsa en muhtemel risk ve ilk bakilacak seyi kisa bicimde soyle. " +
        "Kullanici belirsiz, tek kelimelik veya eksik bir ifade yazarsa anlam uydurma; kisa bir netlestirici soru sor.";
    private static readonly string[] PreferredDoctorTtsModels =
    {
        "speech-2.8-hd",
        "speech-2.6-hd",
        "speech-02-hd"
    };
    private static readonly string[] PreferredDoctorVoiceIds =
    {
        "Chinese (Mandarin)_Reliable_Executive",
        "Chinese (Mandarin)_Gentleman",
        "Chinese (Mandarin)_Male_Announcer",
        "Chinese (Mandarin)_Radio_Host"
    };
    private static readonly Vector3 PlayerMountLocalPosition = new Vector3(0f, -0.01f, 1.38f);
    private static readonly Quaternion PlayerMountLocalRotation = Quaternion.identity;
    private static readonly Vector3 OpenCanvasLocalScale = AIChatCanvasLayout.PreferredCanvasScale;
    private static readonly Vector3 ControllerVisualOpenOffset = new Vector3(0f, -0.085f, 0f);
#if ENABLE_INPUT_SYSTEM
    private static readonly string[] SimulatorKeyboardActionNames =
    {
        "Keyboard X Translate",
        "Keyboard Y Translate",
        "Keyboard Z Translate",
        "Toggle Manipulate Left",
        "Toggle Manipulate Right",
        "Toggle Manipulate Body",
        "Manipulate Left",
        "Manipulate Right",
        "Hand-Controller Mode",
        "Cycle Devices",
        "Stop Manipulation",
        "Rotate Mode Override",
        "Toggle Mouse Transformation Mode",
        "X Constraint",
        "Y Constraint",
        "Z Constraint",
        "Reset",
        "Toggle Cursor Lock",
        "Toggle Primary 2D Axis Target",
        "Toggle Secondary 2D Axis Target",
        "Toggle Device Position Target"
    };

    private static readonly string[] ControllerKeyboardActionNames =
    {
        "Axis 2D",
        "Resting Hand Axis 2D",
        "Primary Button",
        "Secondary Button",
        "Menu",
        "Primary 2D Axis Click",
        "Secondary 2D Axis Click",
        "Primary 2D Axis Touch",
        "Secondary 2D Axis Touch",
        "Primary Touch",
        "Secondary Touch"
    };
#endif

    public static AIManager Instance;

    [Header("API Ayarlari")]
    [Tooltip("OpenAI API Key (sk- ile baslar)")]
    public string apiKey = "BURAYA_API_KEY_YAZILACAK";
    public string apiURL = "https://api.minimax.io/v1/chat/completions";
    public string aiModel = "MiniMax-M2.5";
    [SerializeField] private string minimaxFallbackModel = "MiniMax-M2.5";
    [SerializeField] private int requestTimeoutSeconds = 45;
    [SerializeField] private int requestTimeoutStepSeconds = 10;
    [SerializeField] private int maxRetryCount = 2;
    [SerializeField] private float retryDelaySeconds = 1.1f;
    [SerializeField] private int maxMessagesPerRequest = DefaultDoctorChatMessageWindow;
    [SerializeField] private bool enableFallbackOnLastRetry = true;

    [Header("Doktor Seslendirme")]
    [SerializeField] private bool enableDoctorSpeech = true;
    [SerializeField] private bool useElevenLabsTts = true;
    [SerializeField] private string elevenLabsApiKey = string.Empty;
    [SerializeField] private string elevenLabsVoiceId = "pNInz6obpgDQGcFmaJgB";
    [SerializeField] private string elevenLabsTtsUrl = "https://api.elevenlabs.io/v1/text-to-speech/";
    [SerializeField] private string ttsURL = "https://api.minimax.io/v1/t2a_v2";
    [SerializeField] private string ttsModel = "speech-02-hd";
    [SerializeField] private string ttsApiKeyOverride = string.Empty;
    [SerializeField] private string voiceID = "Chinese (Mandarin)_Reliable_Executive";
    [SerializeField] private int ttsSampleRate = 32000;
    [SerializeField] private int ttsTimeoutSeconds = 20;
    [SerializeField] [Range(0.8f, 1.2f)] private float doctorSpeechSpeed = 0.96f;
    [SerializeField] [Range(0.5f, 1.2f)] private float doctorSpeechVolume = 1f;
    [SerializeField] [Range(-12f, 12f)] private float doctorSpeechPitch = 0f;
    [SerializeField] private AudioSource doctorAudioSource;

    [Header("Doktor Mikrofonu")]
    [SerializeField] private string groqApiKey = string.Empty;
    [SerializeField] private string groqApiUrl = "https://api.groq.com/openai/v1/audio/transcriptions";
    [SerializeField] private string groqWhisperModel = "whisper-large-v3-turbo";
    [SerializeField] private string groqLanguage = "tr";
    [SerializeField] private int groqMaxRecordingSeconds = 8;

    [Header("Debug")]
    [SerializeField] private bool verboseConsoleLogs = false;

    [Header("UI Elemanlari")]
    public GameObject aiCanvas;

    [Tooltip("Tum konusmalarin yazildigi TMP alani")]
    public TextMeshProUGUI chatHistoryText;

    [Tooltip("Chat mesajlarini gosteren ScrollRect")]
    public ScrollRect chatScrollRect;

    [Tooltip("Oyuncunun girdi alani")]
    public TMP_InputField userInputField;

    [Tooltip("Gonder tusu")]
    public Button sendButton;

    [Tooltip("Paneli manuel kapatmak icin kullanilan buton")]
    public Button closeButton;

    [Header("Doktor Panel Sekmeleri")]
    public Button aiTabButton;
    public Button miniTestTabButton;
    public GameObject aiChatRoot;
    public GameObject miniTestRoot;

    [Header("Mini Test UI")]
    public TextMeshProUGUI miniTestQuestionText;
    public TextMeshProUGUI miniTestProgressText;
    public TextMeshProUGUI miniTestTimerText;
    public TextMeshProUGUI miniTestResultText;
    public Button[] miniTestOptionButtons = new Button[3];
    public TextMeshProUGUI[] miniTestOptionLabelTexts = new TextMeshProUGUI[3];
    public Button miniTestNextButton;
    public Button miniTestRestartButton;

    [Header("Mini Test Sorulari")]
    [SerializeField] private List<MiniTestQuestion> miniTestQuestions = new List<MiniTestQuestion>();

    [Header("XR Canvas Placement")]
    [Tooltip("Opsiyonel XROrigin referansi. Bos birakilirsa otomatik bulunur.")]
    [SerializeField] private XROrigin xrOrigin;

    [Tooltip("Opsiyonel spawn mount referansi. Bos birakilirsa kamera altinda otomatik olusturulur.")]
    [SerializeField] private Transform playerSpawnMount;

    [Header("Karakter (Prompt) Ayari")]
    [TextArea(3, 8)]
    [Tooltip("AI karakterinin nasil davranacagini buradan belirleyebilirsin.")]
    public string systemPrompt = "Sen sahada calisan tecrubeli bir afet doktorusun. Kullanici senin ekip arkadasindir ve senden hizli, guvenilir yardim ister. Cevaplarin kisa olsun ama zayif olmasin. Net, profesyonel, anlasilir ve yon verici konus. Gerekirse bir cümlede durumu tespit et, diger cümlede neye bakilacagini veya ne yapilacagini soyle. Dogru Turkce karakterlerle yaz.";

    private const string TriageHintSystemPrompt =
        "You are the senior doctor standing next to a student doing disaster triage. " +
        "Your job is to make the student's next decision easier, not to give generic textbook advice. " +
        "Write in Turkish, practical, bedside, very clear and easy to understand, like a calm senior clinician coaching in real time. " +
        "Avoid meta text, apologies, filler, bullet points, headings, labels and academic lecture style. " +
        "Never reveal internal reasoning, planning, hidden analysis, prompt rules, or task restatement. " +
        "Each reply must naturally include three things: one likely dangerous diagnosis or syndrome, one decisive finding that would move the patient to a higher or lower urgency, and one concrete bedside check to do now. " +
        "Make the urgency direction obvious in plain Turkish, such as dusuk oncelik, orta oncelik, en acil, bekletme, or yasam bulgusu yoksa beklentisiz. " +
        "Prefer wording like: this picture suggests X; if Y is present think higher urgency, if not think lower urgency, and check Z now. " +
        "Reply in 1 or 2 short sentences only. Keep the whole reply short, preferably under 180 characters. Do not reveal the exact triage color. Do not mention AI. Do not echo the complaint text back verbatim. Do not repeat the same wording across follow-up hints.";

    private readonly List<OpenAIMessage> messageHistory = new List<OpenAIMessage>();

    private Canvas canvasComponent;
    private Transform runtimeAnchorHolder;
    private Transform runtimeOpenAnchor;
    private Transform followCamera;
    private VRUIClickHelper vrUiClickHelper;
    private VRKeyboardManager vrKeyboardManager;
    private RectTransform chatViewportRect;
    private RectTransform chatContentRect;
    private readonly List<Behaviour> temporarilyDisabledLocomotionBehaviours = new List<Behaviour>();
    private readonly Dictionary<Transform, Vector3> controllerVisualOriginalPositions = new Dictionary<Transform, Vector3>();
    private Coroutine activeSpeechRequest;
    private AudioClip activeDoctorSpeechClip;
    private XRDeviceSimulator xrDeviceSimulator;
    private string resolvedTtsModel = string.Empty;
    private string resolvedDoctorVoiceId = string.Empty;
    private bool ttsCapabilityChecked;
    private bool ttsUnsupportedForSession;
    private string ttsDisableReason = string.Empty;
    private readonly HashSet<string> unsupportedTtsModels = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    private readonly List<string> cachedDoctorVoiceCandidates = new List<string>();
    private DoctorPanelTab activeDoctorPanelTab = DoctorPanelTab.AIChat;
    private int activeMiniTestQuestionIndex;
    private int correctMiniTestCount;
    private int selectedMiniTestIndex = -1;
    private bool miniTestStarted;
    private bool miniTestAnswered;
    private bool miniTestCompleted;
    private float miniTestStartRealtime = -1f;
    private float miniTestCompletedRealtime = -1f;
    private readonly UnityAction[] miniTestOptionListeners = new UnityAction[3];

    [ContextMenu("Test Doctor Voice")]
    public void TestDoctorVoice()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test sesi sadece Play modunda calisir.");
            return;
        }

        ResetDoctorSpeechSessionState();
        Speak("Merhaba evlat, ben doktor. Sesimi duyabiliyor musun?");
    }
#if ENABLE_INPUT_SYSTEM
    private readonly List<InputAction> temporarilyDisabledTextEntryActions = new List<InputAction>();
    private bool chatTextEntryLockActive;
#endif

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        CacheCanvasState();
        PrepareCanvasInteraction();
        HideCanvasOnStartup();
    }

    private void LogVerbose(string message)
    {
        if (!verboseConsoleLogs)
        {
            return;
        }

        Debug.Log(message);
    }

    private string BuildDoctorSystemPrompt(string latestUserMessage, bool isFirstDoctorReply)
    {
        string configuredPrompt = string.IsNullOrWhiteSpace(systemPrompt)
            ? "Sen bir acil durum ve afet doktorusun. Cevaplarin kisa, net ve sakin olsun."
            : systemPrompt.Trim();

        string mergedPrompt = configuredPrompt + "\n\n" + DoctorReplyStylePrompt;
        string situationalAwarenessPrompt = BuildSituationalAwarenessSystemPrompt();
        LogSituationalAwarenessPrompt(situationalAwarenessPrompt);
        StringBuilder promptBuilder = new StringBuilder(mergedPrompt);
        if (!string.IsNullOrWhiteSpace(situationalAwarenessPrompt))
        {
            promptBuilder.Append("\n\n");
            promptBuilder.Append(situationalAwarenessPrompt);
        }

        string turnDirective = BuildDoctorTurnDirective(latestUserMessage, isFirstDoctorReply, !string.IsNullOrWhiteSpace(situationalAwarenessPrompt));
        if (!string.IsNullOrWhiteSpace(turnDirective))
        {
            promptBuilder.Append("\n\n");
            promptBuilder.Append(turnDirective);
        }

        return promptBuilder.ToString();
    }

    private string BuildSituationalAwarenessSystemPrompt()
    {
        HospitalTriageManager hospitalManager = HospitalTriageManager.Instance;
        if (hospitalManager == null)
        {
            hospitalManager = FindObjectOfType<HospitalTriageManager>(true);
        }

        if (hospitalManager == null)
        {
            return string.Empty;
        }

        if (!hospitalManager.HospitalModeActive)
        {
            return string.Empty;
        }

        if (!hospitalManager.HospitalPhaseActive)
        {
            return "[SAHA DURUMU] Henuz hastaneye girip triyaj yapmadiniz. Bu bilgi arka planindir; gerekli oldugunda kisa ve dogal bicimde kullan.";
        }

        string context = hospitalManager.GetSituationalAwarenessContext();
        if (string.IsNullOrWhiteSpace(context))
        {
            return string.Empty;
        }

        return "[SAHA DURUMU] " + context.Trim() +
               " Bu bilgi arka planindir; kullanici sonucu veya durumu sorarsa dogal bicimde kullan.";
    }

    private string BuildDoctorTurnDirective(string latestUserMessage, bool isFirstDoctorReply, bool hasSituationalContext)
    {
        if (!hasSituationalContext)
        {
            return "BU TUR KURALI: Kullanici ozellikle hastane durumu veya sonuc sormadikca gereksiz istatistik verme.";
        }

        if (isFirstDoctorReply && IsGreetingLikeMessage(latestUserMessage))
        {
            return "BU TUR KURALI: Bu, sohbetin ilk doktor cevabi. Cevaba kisa bir selamla basla ve hemen ardindan en fazla bir cumlede mevcut durumu ozetle. Eger henuz triyaj yapilmadiysa bunu kisa ve dogal bicimde soyle. Bundan sonraki cevaplarda kullanici sormadikca bu ozeti tekrar etme.";
        }

        if (isFirstDoctorReply)
        {
            return "BU TUR KURALI: Bu, sohbetin ilk doktor cevabi. Gerekirse mevcut durumu en fazla bir kisa cumlede sezdir ama gereksiz istatistik kusma. Sonraki cevaplarda kullanici istemedikce durumu tekrar etme.";
        }

        return "BU TUR KURALI: Bu ilk cevap degil. Kullanici ozellikle sonuc, sayi, triyaj veya hastane durumu sormadikca durum ozeti verme.";
    }

    private bool IsGreetingLikeMessage(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return false;
        }

        string normalized = CollapseWhitespace(FixCommonTurkishMojibake(userMessage))
            .ToLowerInvariant()
            .Replace(".", " ")
            .Replace(",", " ")
            .Replace("!", " ")
            .Replace("?", " ")
            .Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        string[] tokens = normalized.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0 || tokens.Length > 4)
        {
            return false;
        }

        string[] allowedGreetingTokens =
        {
            "merhaba",
            "selam",
            "selamlar",
            "slm",
            "sa",
            "selamun",
            "aleykum",
            "selamunaleykum",
            "gunaydin",
            "iyi",
            "aksamlar",
            "gunler",
            "hey",
            "doktor",
            "hocam"
        };

        for (int i = 0; i < tokens.Length; i++)
        {
            if (System.Array.IndexOf(allowedGreetingTokens, tokens[i]) < 0)
            {
                return false;
            }
        }

        return true;
    }

    private string CategorizeAnalyticsQuestionType(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return "empty";
        }

        string normalized = userMessage.ToLowerInvariant();
        if (IsGreetingLikeMessage(userMessage))
        {
            return "greeting";
        }

        if (normalized.Contains("puan") || normalized.Contains("skor") || normalized.Contains("sonuc"))
        {
            return "score_status";
        }

        if (normalized.Contains("triyaj") || normalized.Contains("renk"))
        {
            return "triage_guidance";
        }

        if (normalized.Contains("nefes") || normalized.Contains("solunum"))
        {
            return "respiratory";
        }

        if (normalized.Contains("kanama") || normalized.Contains("kan"))
        {
            return "bleeding";
        }

        if (normalized.Contains("ne yap") || normalized.Contains("yardim") || normalized.Contains("tedavi"))
        {
            return "treatment_guidance";
        }

        return "general";
    }

    private void LogSituationalAwarenessPrompt(string situationalAwarenessPrompt)
    {
        if (string.IsNullOrWhiteSpace(situationalAwarenessPrompt))
        {
            LogVerbose("[AIManager] SAHA_DURUMU | state=empty");
            return;
        }

        string compactPrompt = situationalAwarenessPrompt.Replace('\n', ' ').Replace('\r', ' ').Trim();
        if (compactPrompt.Length > 280)
        {
            compactPrompt = compactPrompt.Substring(0, 280) + "...";
        }

        LogVerbose("[AIManager] SAHA_DURUMU | state=active | prompt=" + compactPrompt);
    }

    private void Start()
    {
        if (messageHistory.Count == 0)
        {
            messageHistory.Add(new OpenAIMessage { role = "system", content = BuildDoctorSystemPrompt(string.Empty, false) });
        }

        EnsureMiniTestQuestionBank();
        CacheCanvasState();
        TryAssignWorldCamera();
        PrepareCanvasInteraction();
        RegisterCanvasButtonListeners();
        ResetMiniTestState();
        HideCanvasOnStartup();
    }

    private void OnDestroy()
    {
        StopDoctorSpeech();
        ReleaseChatTextEntryLock();
        UnlockPlayerLocomotion();
        RestoreControllerVisualPositions();

        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnSendButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseAICanvas);
        }

        if (aiTabButton != null)
        {
            aiTabButton.onClick.RemoveListener(HandleAITabClicked);
        }

        if (miniTestTabButton != null)
        {
            miniTestTabButton.onClick.RemoveListener(HandleMiniTestTabClicked);
        }

        if (miniTestNextButton != null)
        {
            miniTestNextButton.onClick.RemoveListener(OnMiniTestNextClicked);
        }

        if (miniTestRestartButton != null)
        {
            miniTestRestartButton.onClick.RemoveListener(RestartMiniTest);
        }

        for (int i = 0; i < miniTestOptionButtons.Length; i++)
        {
            Button optionButton = miniTestOptionButtons[i];
            UnityAction listener = miniTestOptionListeners[i];
            if (optionButton != null && listener != null)
            {
                optionButton.onClick.RemoveListener(listener);
            }
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (aiCanvas == null || !aiCanvas.activeInHierarchy)
        {
            ReleaseChatTextEntryLock();
            return;
        }

        RefreshChatTextEntryLockState();
        TickMiniTestTimer();

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseAICanvas();
        }
#else
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAICanvas();
        }
#endif
    }

    /// <summary>
    /// Doktora tiklandiginda canvasi oyuncunun baktigi yerde acip dunya uzayinda sabitler.
    /// </summary>
    public void OpenAICanvas()
    {
        if (aiCanvas == null)
        {
            Debug.LogError("[AIManager] aiCanvas atanmis degil.");
            return;
        }

        XROrigin resolvedOrigin = ResolveXROrigin();
        if (resolvedOrigin != null && resolvedOrigin.Camera != null)
        {
            followCamera = resolvedOrigin.Camera.transform;
        }
        else
        {
            followCamera = XRCameraHelper.GetPlayerCameraTransform();
        }

        if (followCamera == null)
        {
            Debug.LogError("[AIManager] Kamera bulunamadi!");
            return;
        }

        Transform spawnMount = ResolvePlayerSpawnMount(followCamera);
        if (spawnMount == null)
        {
            Debug.LogError("[AIManager] Spawn mount olusturulamadi.");
            return;
        }

        Transform runtimeAnchor = EnsureRuntimeOpenAnchor();
        runtimeAnchor.SetPositionAndRotation(
            spawnMount.position,
            Quaternion.Euler(0f, followCamera.eulerAngles.y, 0f));

        aiCanvas.transform.SetParent(runtimeAnchor, false);
        aiCanvas.transform.localPosition = Vector3.zero;
        aiCanvas.transform.localRotation = Quaternion.identity;
        aiCanvas.transform.localScale = OpenCanvasLocalScale;

        CacheCanvasState();
        if (canvasComponent != null)
        {
            canvasComponent.worldCamera = followCamera.GetComponent<Camera>();
        }

        aiCanvas.SetActive(true);
        LockPlayerLocomotion();
        ApplyControllerVisualOffsets();
        PrepareCanvasInteraction();
        EnsureWelcomeMessage();
        RefreshChatLayout(true);
        ResetMiniTestState();
        SwitchTab(DoctorPanelTab.AIChat);

        // if (vrUiClickHelper != null)
        // {
        //     vrUiClickHelper.SuppressInput(0.15f);
        // }
        RefreshChatTextEntryLockState();

        TrainingAnalyticsFacade.OnAIPanelOpened(
            TrainingAnalyticsFacade.Module3Id,
            TrainingAnalyticsFacade.Module3Name,
            "doctor_ai_panel",
            "Doktor AI Paneli",
            new Dictionary<string, object>
            {
                { AnalyticsParams.ScenarioId, TrainingAnalyticsFacade.Module3ScenarioId },
                { AnalyticsParams.ScenarioName, TrainingAnalyticsFacade.Module3ScenarioName }
            });

        LogVerbose("[AIManager] Canvas acildi! Kamera: " + followCamera.name + " Pos: " + followCamera.position);
    }

    public void CloseAICanvas()
    {
        StopDoctorSpeech();

        if (vrKeyboardManager != null)
        {
            vrKeyboardManager.HideKeyboard();
        }

        if (aiCanvas != null)
        {
            aiCanvas.SetActive(false);
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (userInputField != null)
        {
            userInputField.DeactivateInputField();
        }

        ReleaseChatTextEntryLock();
        UnlockPlayerLocomotion();
        RestoreControllerVisualPositions();
    }

    public bool IsDoctorPanelOpen()
    {
        return IsCanvasOpen();
    }

    public void OnSendButtonClicked()
    {
        if (userInputField == null)
        {
            Debug.LogError("[AIManager] userInputField atanmis degil.");
            return;
        }

        string userMessage = userInputField.text?.Trim();
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return;
        }

        userInputField.text = string.Empty;

        AppendToChatUI("<b>Sen:</b> " + userMessage);
        messageHistory.Add(new OpenAIMessage { role = "user", content = userMessage });

        TrainingAnalyticsFacade.OnAIQuestionAsked(
            TrainingAnalyticsFacade.Module3Id,
            TrainingAnalyticsFacade.Module3Name,
            "doctor_ai_panel",
            "Doktor AI Paneli",
            CategorizeAnalyticsQuestionType(userMessage),
            new Dictionary<string, object>
            {
                { AnalyticsParams.ScenarioId, TrainingAnalyticsFacade.Module3ScenarioId },
                { AnalyticsParams.ScenarioName, TrainingAnalyticsFacade.Module3ScenarioName }
            });

        StartCoroutine(SendRequestToOpenAI());
        StartCoroutine(FocusInputFieldNextFrame());
    }

    public Coroutine RequestTriageHint(string complaint, IList<string> priorAssistantHints, System.Action<string> onSuccess, System.Action<string> onError)
    {
        if (!isActiveAndEnabled)
        {
            onError?.Invoke("AI yardimcisi su anda kullanilamiyor.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("BURAYA_API_KEY"))
        {
            onError?.Invoke("AI yardimcisi icin API anahtari ayarlanmamis.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(apiURL))
        {
            onError?.Invoke("AI yardimcisi icin API adresi eksik.");
            return null;
        }

        return StartCoroutine(SendTriageHintRequestRoutine(complaint, priorAssistantHints, onSuccess, onError));
    }

    public Coroutine RequestTriageHint(TriageCaseProfile caseProfile, IList<string> priorAssistantHints, System.Action<string> onSuccess, System.Action<string> onError)
    {
        int requestIndex = priorAssistantHints != null ? priorAssistantHints.Count : 0;
        if (caseProfile == null)
        {
            return RequestTriageHint(string.Empty, priorAssistantHints, onSuccess, onError);
        }

        if (CanUseRemoteTriageHintRequests())
        {
            return StartCoroutine(SendTriageHintRequestRoutine(caseProfile, priorAssistantHints, onSuccess, onError));
        }

        if (TryComposeLocalTriageHint(caseProfile, requestIndex, out string localHint))
        {
            onSuccess?.Invoke(localHint);
            return null;
        }

        string complaint = caseProfile != null ? caseProfile.ComplaintOrFallback : string.Empty;
        return RequestTriageHint(complaint, priorAssistantHints, onSuccess, onError);
    }

    public bool TryComposeLocalTriageHint(TriageCaseProfile caseProfile, int requestIndex, out string hint)
    {
        hint = string.Empty;
        if (caseProfile == null)
        {
            return false;
        }

        hint = MedicalHintComposer.Compose(caseProfile, requestIndex);
        return !string.IsNullOrWhiteSpace(hint);
    }

    private bool CanUseRemoteTriageHintRequests()
    {
        return isActiveAndEnabled &&
               !string.IsNullOrWhiteSpace(apiKey) &&
               !apiKey.Contains("BURAYA_API_KEY") &&
               !string.IsNullOrWhiteSpace(apiURL);
    }

    public void RegisterDoctorSpeaker(AudioSource speakerSource)
    {
        if (speakerSource == null)
        {
            return;
        }

        if (doctorAudioSource != speakerSource)
        {
            StopDoctorSpeech();
            doctorAudioSource = speakerSource;
        }

        ApplyDoctorAudioSourceSettings(doctorAudioSource);
    }

    private IEnumerator SendRequestToOpenAI()
    {
        if (sendButton != null)
        {
            sendButton.interactable = false;
        }

        AppendToChatUI(AIChatCanvasLayout.ThinkingRichText);

        string preferredModel = ResolveDoctorChatModel();
        string lastErrorBody = string.Empty;
        string lastErrorText = string.Empty;
        long lastResponseCode = 0;
        int retryCount = Mathf.Clamp(maxRetryCount, 0, 2);
        bool allowM27Fallback = enableFallbackOnLastRetry &&
                                IsM27Model(preferredModel) &&
                                ShouldUseFallbackModel(preferredModel);
        int totalAttemptCount = retryCount + 1;

        for (int attempt = 0; attempt < totalAttemptCount; attempt++)
        {
            bool useFallbackModel = allowM27Fallback && attempt > 0;
            string modelForAttempt = useFallbackModel ? minimaxFallbackModel : preferredModel;

            OpenAIRequest req = new OpenAIRequest
            {
                model = modelForAttempt,
                messages = BuildMessagesForRequest(),
                temperature = DefaultDoctorChatTemperature,
                max_tokens = DefaultChatMaxTokens
            };

            string jsonPayload = JsonUtility.ToJson(req);

            using (UnityWebRequest request = new UnityWebRequest(apiURL, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = Mathf.Max(15, requestTimeoutSeconds + (attempt * requestTimeoutStepSeconds));

                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);

                yield return request.SendWebRequest();

                bool requestFailed = request.result == UnityWebRequest.Result.ConnectionError ||
                                     request.result == UnityWebRequest.Result.ProtocolError;

                if (!requestFailed)
                {
                    try
                    {
                        string jsonResponse = request.downloadHandler.text;
                        OpenAIResponse res = JsonUtility.FromJson<OpenAIResponse>(jsonResponse);

                        if (res != null && res.choices != null && res.choices.Count > 0 && res.choices[0].message != null)
                        {
                            string aiMessage = res.choices[0].message.content;
                            string visibleMessage = ExtractVisibleAssistantMessage(aiMessage);
                            if (string.IsNullOrWhiteSpace(visibleMessage))
                            {
                                visibleMessage = ExtractVisibleAssistantMessage(ExtractAssistantMessageFromRawJson(jsonResponse));
                            }
                            bool hasVisibleMessage = !string.IsNullOrWhiteSpace(visibleMessage);
                            if (string.IsNullOrWhiteSpace(visibleMessage))
                            {
                                visibleMessage = "Doktor bu turda net bir yanit uretemedi.";
                            }

                            AppendToChatUI("<color=#1D9AF2><b>Doktor:</b> " + visibleMessage + "</color>");
                            if (hasVisibleMessage)
                            {
                                messageHistory.Add(new OpenAIMessage { role = "assistant", content = visibleMessage });
                                Speak(visibleMessage);
                            }
                            if (sendButton != null)
                            {
                                sendButton.interactable = true;
                            }
                            yield break;
                        }

                        LogVerbose("[AIManager] API cevabi bos veya beklenmeyen formatta.");
                        lastErrorText = "API cevabi bos veya beklenmeyen formatta";
                    }
                    catch (System.Exception e)
                    {
                        LogVerbose("[AIManager] JSON cozumleme hatasi: " + e.Message);
                        lastErrorText = "JSON Cozumleme Hatasi: " + e.Message;
                    }
                }
                else
                {
                    lastErrorBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                    lastResponseCode = request.responseCode;
                    lastErrorText = request.error;
                    LogVerbose("[AIManager] API Hatasi | deneme " + (attempt + 1) + " | model " + modelForAttempt + " | HTTP " + request.responseCode + " | " + request.error + " | " + lastErrorBody);
                }

                bool hasRetry = attempt < totalAttemptCount - 1;
                bool canRetry = hasRetry && IsRetryableFailure(request.result, request.responseCode, lastErrorText, lastErrorBody);
                if (canRetry)
                {
                    bool nextAttemptUsesFallback = allowM27Fallback && attempt == 0;
                    string retryMessage = nextAttemptUsesFallback
                        ? "Ana model takildi, doktor alternatif modla tekrar deniyor..."
                        : "Baglanti yavas, doktor yeniden deniyor...";
                    AppendToChatUI("<color=#93A8BF>" + retryMessage + "</color>");
                    float waitDuration = Mathf.Max(0f, retryDelaySeconds) * (attempt + 1);
                    if (waitDuration > 0f)
                    {
                        yield return new WaitForSeconds(waitDuration);
                    }
                }
                else if (!requestFailed)
                {
                    break;
                }
            }
        }

        if (sendButton != null)
        {
            sendButton.interactable = true;
        }

        string userFacingError = BuildDoctorChatUserErrorMessage(lastResponseCode, lastErrorText, lastErrorBody);
        AppendToChatUI("<color=red>" + userFacingError + "</color>");
        if (verboseConsoleLogs && (!string.IsNullOrWhiteSpace(lastErrorBody) || !string.IsNullOrWhiteSpace(lastErrorText)))
        {
            Debug.LogWarning("[AIManager] Doktor chat istegi basarisiz. HTTP " + lastResponseCode + " | " + lastErrorText + " | " + BuildCompactApiError(lastErrorBody));
        }
    }

    private IEnumerator SendTriageHintRequestRoutine(string complaint, IList<string> priorAssistantHints, System.Action<string> onSuccess, System.Action<string> onError)
    {
        yield return SendTriageHintRequestRoutine(
            BuildTriageHintMessages(complaint, priorAssistantHints),
            onSuccess,
            onError);
    }

    private IEnumerator SendTriageHintRequestRoutine(TriageCaseProfile caseProfile, IList<string> priorAssistantHints, System.Action<string> onSuccess, System.Action<string> onError)
    {
        yield return SendTriageHintRequestRoutine(
            BuildTriageHintMessages(caseProfile, priorAssistantHints),
            onSuccess,
            onError);
    }

    private IEnumerator SendTriageHintRequestRoutine(List<OpenAIMessage> requestMessages, System.Action<string> onSuccess, System.Action<string> onError)
    {
        string preferredModel = ResolveApiModel();
        string lastErrorBody = string.Empty;
        string lastErrorText = string.Empty;
        long lastResponseCode = 0;
        int retryCount = Mathf.Max(0, maxRetryCount);
        bool allowM27Fallback = enableFallbackOnLastRetry &&
                                IsM27Model(preferredModel) &&
                                ShouldUseFallbackModel(preferredModel);
        int totalAttemptCount = retryCount + 1 + (allowM27Fallback ? 1 : 0);

        for (int attempt = 0; attempt < totalAttemptCount; attempt++)
        {
            bool useFallbackModel = allowM27Fallback && attempt == totalAttemptCount - 1;
            string modelForAttempt = useFallbackModel ? minimaxFallbackModel : preferredModel;

            OpenAIRequest req = new OpenAIRequest
            {
                model = modelForAttempt,
                messages = requestMessages,
                temperature = 0.38f,
                max_tokens = TriageHintMaxTokens
            };

            string jsonPayload = JsonUtility.ToJson(req);

            using (UnityWebRequest request = new UnityWebRequest(apiURL, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = Mathf.Clamp(requestTimeoutSeconds + (attempt * 4), 8, 18);

                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);

                yield return request.SendWebRequest();

                bool requestFailed = request.result == UnityWebRequest.Result.ConnectionError ||
                                     request.result == UnityWebRequest.Result.ProtocolError;

                if (!requestFailed)
                {
                    try
                    {
                        string jsonResponse = request.downloadHandler.text;
                        OpenAIResponse res = JsonUtility.FromJson<OpenAIResponse>(jsonResponse);

                        if (res != null && res.choices != null && res.choices.Count > 0 && res.choices[0].message != null)
                        {
                            string aiMessage = res.choices[0].message.content;
                            string visibleMessage = ExtractVisibleAssistantMessage(aiMessage);
                            if (string.IsNullOrWhiteSpace(visibleMessage))
                            {
                                visibleMessage = ExtractVisibleAssistantMessage(ExtractAssistantMessageFromRawJson(jsonResponse));
                            }
                            if (string.IsNullOrWhiteSpace(visibleMessage))
                            {
                                onError?.Invoke("AI gorunur triage ipucu uretemedi.");
                                yield break;
                            }

                            onSuccess?.Invoke(visibleMessage);
                            yield break;
                        }

                        lastErrorText = "API cevabi bos veya beklenmeyen formatta";
                    }
                    catch (System.Exception e)
                    {
                        lastErrorText = "JSON Cozumleme Hatasi: " + e.Message;
                    }
                }
                else
                {
                    lastErrorBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                    lastResponseCode = request.responseCode;
                    lastErrorText = request.error;
                    Debug.LogWarning("[AIManager] Triage hint istegi basarisiz | deneme " + (attempt + 1) + " | model " + modelForAttempt + " | HTTP " + request.responseCode + " | " + request.error);
                }

                bool hasRetry = attempt < totalAttemptCount - 1;
                bool canRetry = hasRetry && IsRetryableFailure(request.result, request.responseCode, lastErrorText, lastErrorBody);
                if (canRetry)
                {
                    float waitDuration = Mathf.Max(0f, retryDelaySeconds) * (attempt + 1);
                    if (waitDuration > 0f)
                    {
                        yield return new WaitForSeconds(waitDuration);
                    }
                }
                else if (!requestFailed)
                {
                    break;
                }
            }
        }

        string errorMessage = "AI yardimcisi yanit uretemedi.";
        if (lastResponseCode > 0)
        {
            errorMessage += " HTTP " + lastResponseCode + " - " + lastErrorText;
        }
        else if (!string.IsNullOrWhiteSpace(lastErrorText))
        {
            errorMessage += " " + lastErrorText;
        }

        if (!string.IsNullOrWhiteSpace(lastErrorBody))
        {
            errorMessage += " " + BuildCompactApiError(lastErrorBody);
        }

        onError?.Invoke(errorMessage.Trim());
    }

    private List<OpenAIMessage> BuildTriageHintMessages(string complaint, IList<string> priorAssistantHints)
    {
        string safeComplaint = string.IsNullOrWhiteSpace(complaint)
            ? "Hasta sikayeti net degil, belirtiler gozleme dayali degerlendirilecek."
            : complaint.Trim();

        string diagnosticFocus = BuildTriageDiagnosticFocus(safeComplaint);

        List<OpenAIMessage> messages = new List<OpenAIMessage>
        {
            new OpenAIMessage
            {
                role = "system",
                content = TriageHintSystemPrompt
            },
            new OpenAIMessage
            {
                role = "user",
                content =
                    "Hasta sikayeti: " + safeComplaint + "\n" +
                    "Taniya yonlendiren klinik odak: " + diagnosticFocus + "\n" +
                    "Bu hasta icin ogrenciyi karar vermeye goturen pratik, kuvvetli ve kolay anlasilir bir ilk ipucu ver. " +
                    "Genel tavsiye verme; bu sikayette en olasi tehlikeli tablo ve karari degistiren kritik bulgu uzerine odaklan. " +
                    "Ilk cumlede en olasi taniyi veya patolojiyi acik adiyla yaz. " +
                    "Ikinci cumlede eger hangi bulgu varsa daha yuksek oncelik dusunulecegini, hangi durumda daha alt oncelik dusunulebilecegini ve tam olarak neye bakilacagini soyle. " +
                    "Gerekirse 'eger morarma veya SpO2 dusukse bekletme' gibi net konus. " +
                    "Renk adi vermeden hastanin bekleyebilir mi, orta acil mi, yoksa en acile mi yaklastigini hissettir. " +
                    "Baslik, madde imi veya etiket kullanmadan 1 ya da 2 kisa cumle yaz. " +
                    "Tam triyaj rengini dogrudan soyleme."
            }
        };

        if (priorAssistantHints == null || priorAssistantHints.Count == 0)
        {
            return messages;
        }

        int startIndex = Mathf.Max(0, priorAssistantHints.Count - 1);
        for (int i = startIndex; i < priorAssistantHints.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(priorAssistantHints[i]))
            {
                continue;
            }

            messages.Add(new OpenAIMessage
            {
                role = "assistant",
                content = priorAssistantHints[i]
            });
        }

        messages.Add(new OpenAIMessage
        {
            role = "user",
            content =
                "Ayni hasta icin onceki ifadeleri tekrar etmeden farkli bir tanisal aci sec. " +
                "Yeni bir olasi neden, yeni bir red flag veya yeni bir bedside kontrol oner. " +
                "Bu kez farkli bir risk mekanizmasi soyle ve karari degistiren esik bulguyu daha net adlandir. " +
                "Oncelik duzeyini yine sade dille hissettir; gerekirse eger varsa ya da yoksa diye karar koprusu kur. " +
                "Baslik kullanmadan 1 ya da 2 kisa cumle kur."
        });

        return messages;
    }

    private List<OpenAIMessage> BuildTriageHintMessages(TriageCaseProfile caseProfile, IList<string> priorAssistantHints)
    {
        string complaint = caseProfile != null ? caseProfile.ComplaintOrFallback : string.Empty;
        string safeComplaint = string.IsNullOrWhiteSpace(complaint)
            ? "Hasta sikayeti net degil, belirtiler gozleme dayali degerlendirilecek."
            : complaint.Trim();

        string diagnosticFocus = BuildTriageDiagnosticFocus(safeComplaint);
        string criticalObservation = caseProfile != null ? caseProfile.CriticalObservationOrFallback : string.Empty;
        string suspectedCondition = caseProfile != null ? caseProfile.SuspectedConditionOrFallback : string.Empty;
        string initialChecks = caseProfile != null ? caseProfile.InitialChecksOrFallback : string.Empty;
        string triageHint = caseProfile != null ? caseProfile.TriageHintOrFallback : string.Empty;
        string fieldTone = caseProfile != null ? caseProfile.ToneOrFallback : string.Empty;

        List<OpenAIMessage> messages = new List<OpenAIMessage>
        {
            new OpenAIMessage
            {
                role = "system",
                content = TriageHintSystemPrompt
            },
            new OpenAIMessage
            {
                role = "user",
                content =
                    "Vaka basligi: " + (caseProfile != null ? caseProfile.CaseNameOrFallback : "Saha vakasi") + "\n" +
                    "Hasta bildirimi: " + safeComplaint + "\n" +
                    "Sahadaki kritik gozlem: " + criticalObservation + "\n" +
                    "En olasi tehlikeli tablo: " + suspectedCondition + "\n" +
                    "Ilk bakilacaklar: " + initialChecks + "\n" +
                    "Oncelik yonu: " + triageHint + "\n" +
                    "Saha tonu: " + fieldTone + "\n" +
                    "Taniya yonlendiren klinik odak: " + diagnosticFocus + "\n" +
                    "Bu ogrenciye su an karar verdiren, tekrara dusmeyen ve hastanin ustune yapisan kisa bir ipucu ver. " +
                    "Birinci cumlede en olasi tehlikeli tabloyu veya sendromu adlandir. " +
                    "Ikinci cumlede karari degistiren esik bulguyu ve tam olarak neye bakilacagini soyle. " +
                    "Genel tavsiye, etiket, baslik, madde imi ve uzun aciklama yazma. " +
                    "1 ya da 2 kisa cumle kur; renk adini verme."
            }
        };

        if (priorAssistantHints == null || priorAssistantHints.Count == 0)
        {
            return messages;
        }

        int startIndex = Mathf.Max(0, priorAssistantHints.Count - 2);
        for (int i = startIndex; i < priorAssistantHints.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(priorAssistantHints[i]))
            {
                continue;
            }

            messages.Add(new OpenAIMessage
            {
                role = "assistant",
                content = priorAssistantHints[i]
            });
        }

        messages.Add(new OpenAIMessage
        {
            role = "user",
            content =
                "Ayni vaka icin onceki cumleleri tekrar etme. " +
                "Baska bir red flag, baska bir esik bulgu veya baska bir bedside kontrol sec. " +
                "Yine 1 ya da 2 kisa cumle yaz ve ifadeleri tazele."
        });

        return messages;
    }

    private string BuildTriageDiagnosticFocus(string complaint)
    {
        string normalizedComplaint = NormalizeTriagePromptText(complaint);
        List<string> focusItems = new List<string>();

        if (ContainsAnyTriageKeyword(normalizedComplaint, "nefes", "solunum", "gogus"))
        {
            focusItems.Add("solunum yetmezligi, hipoksi, gogus travmasi veya sok ihtimalini dusun; solunum sayisi, SpO2, gogus simetrisi, morarma ve yardimci kas kullanimina bak");
        }

        if (ContainsAnyTriageKeyword(normalizedComplaint, "yuruyebiliyorum", "kendi basima", "hafif", "siyrik", "sizlik"))
        {
            focusItems.Add("hasta yuruyebiliyor ve yakinma minorsa dusuk oncelik dusun; yuzeysel yumusak doku yaralanmasi, stabil bilinc ve kontrol altindaki kanama uzerine odaklan");
        }

        if (ContainsAnyTriageKeyword(normalizedComplaint, "bas", "bilinc", "bayilma", "sersem", "cevap vermiyor"))
        {
            focusItems.Add("norolojik bozulma, kafa travmasi, hipoksi veya hipoperfuzyon ihtimalini dusun; bilinc duzeyi, pupiller ve yaniti kontrol et");
        }

        if (ContainsAnyTriageKeyword(normalizedComplaint, "kanama", "kan"))
        {
            focusItems.Add("dis veya ic kanama ve hipovolemik sok ihtimalini dusun; aktif odak, nabiz, cilt rengi, kapiller dolum ve huzursuzluk bulgularina bak");
        }

        if (ContainsAnyTriageKeyword(normalizedComplaint, "basamiyorum", "derin", "metal parca", "hareket kisitli"))
        {
            focusItems.Add("ekstremite travmasi, derin yara veya kontrollu ama ciddi kanama varsa orta oncelik dusun; distal dolasim, deformite, aktif kanama ve ustune basabilme durumunu kontrol et");
        }

        if (ContainsAnyTriageKeyword(normalizedComplaint, "yanik", "duman"))
        {
            focusItems.Add("inhalasyon yaralanmasi ve hava yolu riski ihtimalini dusun; ses kisikligi, is bulgusu ve solunum eforunu kontrol et");
        }

        if (ContainsAnyTriageKeyword(normalizedComplaint, "nabiz alinmiyor", "solunum yok", "spontan solunum yok", "yanitsiz"))
        {
            focusItems.Add("yasam bulgusu yoksa beklentisiz hasta ihtimalini dusun; hava yolu acikligi sonrasi spontan solunum ve nabzi hizli kontrol et");
        }

        if (focusItems.Count == 0)
        {
            return "ABC onceligiyle dusun; hayati risk, sok, travma ve bilinc durumunu hizli ayir.";
        }

        return string.Join(" | ", focusItems);
    }

    private string NormalizeTriagePromptText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string lowered = value.ToLowerInvariant();
        return lowered
            .Replace("ğ", "g")
            .Replace("ü", "u")
            .Replace("ş", "s")
            .Replace("ı", "i")
            .Replace("ö", "o")
            .Replace("ç", "c");
    }

    private bool ContainsAnyTriageKeyword(string source, params string[] keywords)
    {
        if (string.IsNullOrWhiteSpace(source) || keywords == null)
        {
            return false;
        }

        for (int i = 0; i < keywords.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(keywords[i]) && source.Contains(keywords[i]))
            {
                return true;
            }
        }

        return false;
    }

    public void Speak(string text)
    {
        if (!enableDoctorSpeech)
        {
            return;
        }

        bool shouldUseElevenLabs = ShouldUseElevenLabsTts();
        if (ttsUnsupportedForSession && !shouldUseElevenLabs)
        {
            return;
        }

        if (!ShouldSpeakDoctorResponse(text))
        {
            return;
        }

        AudioSource source = ResolveDoctorAudioSource();
        if (source == null)
        {
            return;
        }

        string speakableText = BuildSpeakableDoctorText(text);
        if (string.IsNullOrWhiteSpace(speakableText))
        {
            return;
        }

        StopDoctorSpeech();
        if (shouldUseElevenLabs)
        {
            activeSpeechRequest = StartCoroutine(PostElevenLabsSpeechRequest(speakableText));
        }
        else
        {
            activeSpeechRequest = StartCoroutine(PostSpeechRequest(speakableText));
        }
    }

    private IEnumerator PostElevenLabsSpeechRequest(string text)
    {
        AudioSource source = ResolveDoctorAudioSource();
        if (source == null)
        {
            activeSpeechRequest = null;
            yield break;
        }

        string resolvedApiKey = ResolveElevenLabsApiKey();
        if (string.IsNullOrWhiteSpace(resolvedApiKey))
        {
            activeSpeechRequest = null;
            yield break;
        }

        string resolvedVoiceId = SanitizeElevenLabsValue(elevenLabsVoiceId, "pNInz6obpgDQGcFmaJgB");
        string requestUrl = BuildElevenLabsRequestUrl(resolvedVoiceId);

        ElevenLabsTtsRequest requestPayload = new ElevenLabsTtsRequest
        {
            text = text,
            model_id = "eleven_multilingual_v2",
            voice_settings = new ElevenLabsVoiceSettings
            {
                stability = 0.5f,
                similarity_boost = 0.75f,
                style = 0f,
                use_speaker_boost = true
            }
        };

        string jsonPayload = JsonUtility.ToJson(requestPayload);
        LogVerbose("[AIManager] ElevenLabs TTS payload: " + jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(requestUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = Mathf.Clamp(ttsTimeoutSeconds, 8, 40);

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "audio/mpeg");
            request.SetRequestHeader("xi-api-key", resolvedApiKey);

            yield return request.SendWebRequest();

            bool requestFailed = request.result == UnityWebRequest.Result.ConnectionError ||
                                 request.result == UnityWebRequest.Result.ProtocolError;
            if (requestFailed)
            {
                string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                if (request.responseCode == 401 || request.responseCode == 429)
                {
                    Debug.LogWarning("[AIManager] ElevenLabs TTS kullanilamadi. HTTP " + request.responseCode + " | " + request.error + " | Detay: " + responseText);
                    activeSpeechRequest = null;
                    yield break;
                }

                if (!string.IsNullOrWhiteSpace(request.error) &&
                    request.error.IndexOf("Cannot resolve destination host", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Debug.LogError("[AIManager] ElevenLabs host cozumlenemedi. URL: " + requestUrl + " | Inspector elevenLabsTtsUrl degerini kontrol et. HTTP " + request.responseCode + " | " + request.error + " | Detay: " + responseText);
                }
                else
                {
                    Debug.LogError("[AIManager] ElevenLabs TTS hatasi. URL: " + requestUrl + " | HTTP " + request.responseCode + " | " + request.error + " | Detay: " + responseText);
                }
                activeSpeechRequest = null;
                yield break;
            }

            byte[] audioBytes = request.downloadHandler != null ? request.downloadHandler.data : null;
            if (audioBytes == null || audioBytes.Length == 0)
            {
                Debug.LogWarning("[AIManager] ElevenLabs TTS ses verisi bos dondu.");
                activeSpeechRequest = null;
                yield break;
            }

            string tempFileName = "tts_elevenlabs_" + System.Guid.NewGuid().ToString("N") + ".mp3";
            string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, tempFileName);
            try
            {
                System.IO.File.WriteAllBytes(tempPath, audioBytes);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[AIManager] ElevenLabs MP3 gecici dosyaya yazilamadi: " + e.Message);
                activeSpeechRequest = null;
                yield break;
            }

            string fileUrl = "file://" + tempPath.Replace("\\", "/");
            using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(fileUrl, AudioType.MPEG))
            {
                yield return audioRequest.SendWebRequest();
                LogVerbose("[AIManager] ElevenLabs AudioClip yukleme sonucu: " + audioRequest.result
                    + " | Hata: " + audioRequest.error
                    + " | Bytes: " + (audioBytes != null ? audioBytes.Length.ToString() : "null"));

                bool audioRequestFailed = audioRequest.result == UnityWebRequest.Result.ConnectionError ||
                                          audioRequest.result == UnityWebRequest.Result.ProtocolError;
                if (audioRequestFailed)
                {
                    Debug.LogError("[AIManager] ElevenLabs MP3 AudioClip'e donusturulemedi. " + audioRequest.error);
                    TryDeleteTemporaryAudioFile(tempPath);
                    activeSpeechRequest = null;
                    yield break;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);
                TryDeleteTemporaryAudioFile(tempPath);
                if (clip == null)
                {
                    Debug.LogWarning("[AIManager] ElevenLabs AudioClip olusturulamadi.");
                    activeSpeechRequest = null;
                    yield break;
                }

                if (source != ResolveDoctorAudioSource())
                {
                    Destroy(clip);
                    activeSpeechRequest = null;
                    yield break;
                }

                resolvedTtsModel = "elevenlabs";
                resolvedDoctorVoiceId = resolvedVoiceId;
                ttsUnsupportedForSession = false;
                ttsDisableReason = string.Empty;

                ReleaseActiveDoctorSpeechClip();
                activeDoctorSpeechClip = clip;
                source.Stop();
                source.clip = clip;
                source.volume = 1f;
                source.Play();
                LogVerbose("[AIManager] ElevenLabs doktor sesi caliyor. Voice: " + resolvedVoiceId);
                activeSpeechRequest = null;
            }
        }
    }

    private IEnumerator PostSpeechRequest(string text)
    {
        AudioSource source = ResolveDoctorAudioSource();
        if (source == null)
        {
            activeSpeechRequest = null;
            yield break;
        }

        string ttsApiKey = ResolveTtsApiKey();
        if (string.IsNullOrWhiteSpace(ttsApiKey) || ttsApiKey.Contains("BURAYA_API_KEY"))
        {
            DisableTtsForSession("Doktor seslendirme icin TTS destekli bir MiniMax API anahtari gerekli.");
            activeSpeechRequest = null;
            yield break;
        }

        yield return EnsureDoctorVoiceCandidatesReady();

        List<string> modelCandidates = ResolveSupportedTtsModelCandidates();
        List<string> voiceCandidates = ResolveSupportedDoctorVoiceCandidates();
        if (modelCandidates.Count == 0)
        {
            DisableTtsForSession("Desteklenen bir TTS modeli bulunamadi.");
            activeSpeechRequest = null;
            yield break;
        }

        if (voiceCandidates.Count == 0)
        {
            DisableTtsForSession("Gecerli bir doktor sesi bulunamadi.");
            activeSpeechRequest = null;
            yield break;
        }

        for (int modelIndex = 0; modelIndex < modelCandidates.Count; modelIndex++)
        {
            string selectedModel = modelCandidates[modelIndex];
            bool shouldAdvanceModel = false;

            for (int voiceIndex = 0; voiceIndex < voiceCandidates.Count; voiceIndex++)
            {
                string selectedVoiceId = voiceCandidates[voiceIndex];
                MiniMaxTtsRequest req = new MiniMaxTtsRequest
                {
                    model = selectedModel,
                    text = text,
                    stream = false,
                    language_boost = "Turkish",
                    output_format = "hex",
                    voice_setting = new MiniMaxTtsVoiceSetting
                    {
                        voice_id = selectedVoiceId,
                        speed = doctorSpeechSpeed,
                        vol = Mathf.RoundToInt(doctorSpeechVolume * 10f),
                        pitch = Mathf.RoundToInt(doctorSpeechPitch)
                    },
                    audio_setting = new MiniMaxTtsAudioSetting
                    {
                        sample_rate = Mathf.Max(16000, ttsSampleRate),
                        bitrate = 128000,
                        format = "wav",
                        channel = 1
                    }
                };

                string jsonPayload = JsonUtility.ToJson(req);
                    LogVerbose("[AIManager] TTS payload: " + jsonPayload);

                using (UnityWebRequest request = new UnityWebRequest(ttsURL, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.timeout = Mathf.Clamp(ttsTimeoutSeconds, 8, 40);

                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Accept", "application/json");
                    request.SetRequestHeader("Authorization", "Bearer " + ttsApiKey);

                    yield return request.SendWebRequest();

                    string jsonResponse = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                    LogVerbose("[AIManager] TTS raw response: " + jsonResponse);

                    bool requestFailed = request.result == UnityWebRequest.Result.ConnectionError ||
                                         request.result == UnityWebRequest.Result.ProtocolError;
                    if (requestFailed)
                    {
                        string normalizedFailure = string.IsNullOrWhiteSpace(jsonResponse)
                            ? string.Empty
                            : jsonResponse.Trim().ToLowerInvariant();
                        if (IsUnsupportedTtsModelResponse(normalizedFailure))
                        {
                            unsupportedTtsModels.Add(selectedModel);
                            if (string.Equals(resolvedTtsModel, selectedModel, System.StringComparison.OrdinalIgnoreCase))
                            {
                                resolvedTtsModel = string.Empty;
                            }

                            shouldAdvanceModel = true;
                            break;
                        }

                        Debug.LogError("[AIManager] TTS API HATASI | HTTP " + request.responseCode + " | " + request.error + " | Detay: " + jsonResponse);
                        activeSpeechRequest = null;
                        yield break;
                    }

                    MiniMaxTtsResponse res = null;
                    try
                    {
                        res = JsonUtility.FromJson<MiniMaxTtsResponse>(jsonResponse);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning("[AIManager] Doktor TTS cevabi cozumlenemedi: " + e.Message);
                    }

                    if (res == null)
                    {
                        Debug.LogWarning("[AIManager] Doktor TTS cevabi bos dondu.");
                        activeSpeechRequest = null;
                        yield break;
                    }

                    if (res.base_resp != null && res.base_resp.status_code != 0)
                    {
                        string statusMessage = string.IsNullOrWhiteSpace(res.base_resp.status_msg)
                            ? string.Empty
                            : res.base_resp.status_msg.Trim().ToLowerInvariant();
                        Debug.LogWarning("[AIManager] Doktor TTS hatasi: " + res.base_resp.status_msg + " | Trace: " + res.trace_id + " | Model: " + selectedModel + " | Voice: " + selectedVoiceId + " | TTSKeyMode: " + (string.IsNullOrWhiteSpace(ttsApiKeyOverride) ? "SharedApiKey" : "TtsOverrideKey"));

                        if (IsUnsupportedTtsModelResponse(statusMessage))
                        {
                            unsupportedTtsModels.Add(selectedModel);
                            if (string.Equals(resolvedTtsModel, selectedModel, System.StringComparison.OrdinalIgnoreCase))
                            {
                                resolvedTtsModel = string.Empty;
                            }

                            shouldAdvanceModel = true;
                            break;
                        }

                        if (statusMessage.Contains("voice id not exist"))
                        {
                            RemoveCachedDoctorVoiceCandidate(selectedVoiceId);
                            continue;
                        }

                        activeSpeechRequest = null;
                        yield break;
                    }

                    if (res.data == null || string.IsNullOrWhiteSpace(res.data.audio))
                    {
                        Debug.LogWarning("[AIManager] Doktor TTS ses verisi bos dondu. Trace: " + res.trace_id + " | Model: " + selectedModel + " | Voice: " + selectedVoiceId);
                        continue;
                    }

                    byte[] audioBytes = null;
                    try
                    {
                        audioBytes = DecodeHexAudio(res.data.audio);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError("[AIManager] Hex ses verisi cozumlenemedi: " + ex.Message);
                    }

                    if (audioBytes == null || audioBytes.Length == 0)
                    {
                        Debug.LogWarning("[AIManager] Doktor TTS ses verisi cozumlenemedi veya bos.");
                        continue;
                    }

                    AudioClip clip = CreateAudioFromBytes(audioBytes);
                    if (clip == null)
                    {
                        Debug.LogWarning("[AIManager] Doktor TTS WAV klibi olusturulamadi.");
                        continue;
                    }

                    if (source != ResolveDoctorAudioSource())
                    {
                        Destroy(clip);
                        activeSpeechRequest = null;
                        yield break;
                    }

                    resolvedTtsModel = selectedModel;
                    resolvedDoctorVoiceId = selectedVoiceId;
                    ttsModel = selectedModel;
                    voiceID = selectedVoiceId;
                    ttsUnsupportedForSession = false;
                    ttsDisableReason = string.Empty;

                    ReleaseActiveDoctorSpeechClip();
                    activeDoctorSpeechClip = clip;
                    source.Stop();
                    source.clip = clip;
                    source.volume = 1f;
                    source.Play();
                LogVerbose("[AIManager] Doktor TTS secildi | Model: " + selectedModel + " | Voice: " + selectedVoiceId);
                    activeSpeechRequest = null;
                    yield break;
                }
            }

            if (shouldAdvanceModel)
            {
                continue;
            }
        }

        DisableTtsForSession(BuildTtsDisableReason());
        activeSpeechRequest = null;
    }

    private List<string> ResolveSupportedTtsModelCandidates()
    {
        List<string> candidates = new List<string>(PreferredDoctorTtsModels.Length + 2);
        AddUniqueCandidate(candidates, resolvedTtsModel, true);
        AddUniqueCandidate(candidates, ttsModel, true);

        for (int i = 0; i < PreferredDoctorTtsModels.Length; i++)
        {
            AddUniqueCandidate(candidates, PreferredDoctorTtsModels[i], true);
        }

        return candidates;
    }

    private IEnumerator EnsureDoctorVoiceCandidatesReady()
    {
        if (ttsCapabilityChecked)
        {
            if (cachedDoctorVoiceCandidates.Count == 0)
            {
                BuildFallbackDoctorVoiceCandidates(cachedDoctorVoiceCandidates);
            }

            yield break;
        }

        ttsCapabilityChecked = true;
        cachedDoctorVoiceCandidates.Clear();

        string ttsApiKey = ResolveTtsApiKey();
        if (string.IsNullOrWhiteSpace(ttsApiKey) || ttsApiKey.Contains("BURAYA_API_KEY") || string.IsNullOrWhiteSpace(TtsVoiceListUrl))
        {
            BuildFallbackDoctorVoiceCandidates(cachedDoctorVoiceCandidates);
            yield break;
        }

        MiniMaxVoiceQueryRequest voiceQuery = new MiniMaxVoiceQueryRequest
        {
            voice_type = "system"
        };

        string jsonPayload = JsonUtility.ToJson(voiceQuery);
        using (UnityWebRequest request = new UnityWebRequest(TtsVoiceListUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = Mathf.Clamp(ttsTimeoutSeconds, 8, 40);

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + ttsApiKey);

            yield return request.SendWebRequest();

            string jsonResponse = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            LogVerbose("[AIManager] TTS voice list response: " + jsonResponse);

            bool requestFailed = request.result == UnityWebRequest.Result.ConnectionError ||
                                 request.result == UnityWebRequest.Result.ProtocolError;
            if (requestFailed)
            {
                Debug.LogWarning("[AIManager] TTS voice list istegi basarisiz oldu. HTTP " + request.responseCode + " | " + request.error);
                BuildFallbackDoctorVoiceCandidates(cachedDoctorVoiceCandidates);
                yield break;
            }

            MiniMaxVoiceQueryResponse response = null;
            try
            {
                response = JsonUtility.FromJson<MiniMaxVoiceQueryResponse>(jsonResponse);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[AIManager] TTS voice list cevabi cozumlenemedi: " + e.Message);
            }

            if (response == null ||
                (response.base_resp != null && response.base_resp.status_code != 0) ||
                response.system_voice == null ||
                response.system_voice.Count == 0)
            {
                BuildFallbackDoctorVoiceCandidates(cachedDoctorVoiceCandidates);
                yield break;
            }

            CacheDoctorVoiceCandidatesFromResponse(response.system_voice);
            if (cachedDoctorVoiceCandidates.Count == 0)
            {
                BuildFallbackDoctorVoiceCandidates(cachedDoctorVoiceCandidates);
            }
        }
    }

    private List<string> ResolveSupportedDoctorVoiceCandidates()
    {
        List<string> candidates = new List<string>(cachedDoctorVoiceCandidates.Count + PreferredDoctorVoiceIds.Length + 1);
        AddUniqueCandidate(candidates, resolvedDoctorVoiceId, false);

        for (int i = 0; i < cachedDoctorVoiceCandidates.Count; i++)
        {
            AddUniqueCandidate(candidates, cachedDoctorVoiceCandidates[i], false);
        }

        if (candidates.Count == 0)
        {
            BuildFallbackDoctorVoiceCandidates(candidates);
        }

        return candidates;
    }

    private void CacheDoctorVoiceCandidatesFromResponse(List<MiniMaxVoiceDescriptor> systemVoices)
    {
        if (systemVoices == null || systemVoices.Count == 0)
        {
            return;
        }

        List<MiniMaxVoiceDescriptor> rankedVoices = new List<MiniMaxVoiceDescriptor>(systemVoices.Count);
        for (int i = 0; i < systemVoices.Count; i++)
        {
            MiniMaxVoiceDescriptor voice = systemVoices[i];
            if (voice != null && !string.IsNullOrWhiteSpace(voice.voice_id))
            {
                rankedVoices.Add(voice);
            }
        }

        rankedVoices.Sort((left, right) => ScoreDoctorVoice(right).CompareTo(ScoreDoctorVoice(left)));

        for (int i = 0; i < rankedVoices.Count; i++)
        {
            AddUniqueCandidate(cachedDoctorVoiceCandidates, rankedVoices[i].voice_id, false);
        }

        BuildFallbackDoctorVoiceCandidates(cachedDoctorVoiceCandidates);
    }

    private int ScoreDoctorVoice(MiniMaxVoiceDescriptor voice)
    {
        if (voice == null)
        {
            return int.MinValue;
        }

        StringBuilder descriptorBuilder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(voice.voice_id))
        {
            descriptorBuilder.Append(voice.voice_id).Append(' ');
        }

        if (!string.IsNullOrWhiteSpace(voice.voice_name))
        {
            descriptorBuilder.Append(voice.voice_name).Append(' ');
        }

        if (voice.description != null)
        {
            for (int i = 0; i < voice.description.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(voice.description[i]))
                {
                    descriptorBuilder.Append(voice.description[i]).Append(' ');
                }
            }
        }

        string descriptor = descriptorBuilder.ToString().Trim().ToLowerInvariant();
        int score = 0;

        if (descriptor.Contains("executive"))
        {
            score += 120;
        }

        if (descriptor.Contains("gentleman"))
        {
            score += 110;
        }

        if (descriptor.Contains("announcer"))
        {
            score += 100;
        }

        if (descriptor.Contains("host"))
        {
            score += 90;
        }

        if (descriptor.Contains("professor") || descriptor.Contains("teacher"))
        {
            score += 80;
        }

        if (descriptor.Contains("male"))
        {
            score += 70;
        }

        if (descriptor.Contains("reliable"))
        {
            score += 30;
        }

        if (descriptor.Contains("female"))
        {
            score -= 50;
        }

        if (!string.IsNullOrWhiteSpace(resolvedDoctorVoiceId))
        {
            string normalizedResolved = resolvedDoctorVoiceId.Trim().ToLowerInvariant();
            if (string.Equals(normalizedResolved, voice.voice_id, System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalizedResolved, voice.voice_name, System.StringComparison.OrdinalIgnoreCase))
            {
                score += 200;
            }
        }

        if (!string.IsNullOrWhiteSpace(voiceID))
        {
            string normalizedConfigured = voiceID.Trim().ToLowerInvariant();
            if (string.Equals(normalizedConfigured, voice.voice_id, System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalizedConfigured, voice.voice_name, System.StringComparison.OrdinalIgnoreCase))
            {
                score += 20;
            }
        }

        return score;
    }

    private void BuildFallbackDoctorVoiceCandidates(List<string> candidates)
    {
        AddUniqueCandidate(candidates, resolvedDoctorVoiceId, false);

        string configuredVoice = string.IsNullOrWhiteSpace(voiceID) ? string.Empty : voiceID.Trim();
        for (int i = 0; i < PreferredDoctorVoiceIds.Length; i++)
        {
            if (string.Equals(configuredVoice, PreferredDoctorVoiceIds[i], System.StringComparison.OrdinalIgnoreCase))
            {
                AddUniqueCandidate(candidates, configuredVoice, false);
                break;
            }
        }

        for (int i = 0; i < PreferredDoctorVoiceIds.Length; i++)
        {
            AddUniqueCandidate(candidates, PreferredDoctorVoiceIds[i], false);
        }
    }

    private static bool IsUnsupportedTtsModelResponse(string statusMessage)
    {
        if (string.IsNullOrWhiteSpace(statusMessage))
        {
            return false;
        }

        string normalized = statusMessage.Trim().ToLowerInvariant();
        return normalized.Contains("token plan not support model") ||
               normalized.Contains("not support model") ||
               normalized.Contains("unsupported model");
    }

    private string BuildTtsDisableReason()
    {
        if (unsupportedTtsModels.Count > 0)
        {
            List<string> failedModels = new List<string>(unsupportedTtsModels.Count);
            foreach (string model in unsupportedTtsModels)
            {
                failedModels.Add(model);
            }

            return "Bu MiniMax API anahtari doktor seslendirme modellerine erisemiyor. Reddedilen modeller: " + string.Join(", ", failedModels) + ". Inspector'da Tts Api Key Override alanina TTS destekli ayri bir anahtar gir.";
        }

        if (cachedDoctorVoiceCandidates.Count == 0)
        {
            return "MiniMax tarafinda gecerli doktor sesi bulunamadi.";
        }

        return "Doktor sesi bu oturumda kullanilamadi.";
    }

    private bool ShouldUseElevenLabsTts()
    {
        return useElevenLabsTts && !string.IsNullOrWhiteSpace(ResolveElevenLabsApiKey());
    }

    private static string SanitizeElevenLabsValue(string value, string fallback)
    {
        string resolved = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        resolved = resolved.Replace("\u200B", string.Empty)
                           .Replace("\u200C", string.Empty)
                           .Replace("\u200D", string.Empty)
                           .Replace("\"", string.Empty)
                           .Replace("'", string.Empty);
        return string.IsNullOrWhiteSpace(resolved) ? fallback : resolved;
    }

    private string BuildElevenLabsRequestUrl(string resolvedVoiceId)
    {
        string defaultBaseUrl = "https://api.elevenlabs.io/v1/text-to-speech/";
        string configuredBaseUrl = SanitizeElevenLabsValue(elevenLabsTtsUrl, defaultBaseUrl);
        string candidateUrl = configuredBaseUrl.TrimEnd('/') + "/" + resolvedVoiceId;

        if (System.Uri.TryCreate(candidateUrl, System.UriKind.Absolute, out System.Uri candidateUri) &&
            (candidateUri.Scheme == System.Uri.UriSchemeHttps || candidateUri.Scheme == System.Uri.UriSchemeHttp))
        {
            return candidateUri.ToString();
        }

        string fallbackUrl = defaultBaseUrl.TrimEnd('/') + "/" + resolvedVoiceId;
        Debug.LogWarning("[AIManager] elevenLabsTtsUrl gecersiz gorunuyor. Varsayilan endpoint kullaniliyor: " + fallbackUrl);
        return fallbackUrl;
    }

    private string ResolveElevenLabsApiKey()
    {
        return SanitizeElevenLabsValue(elevenLabsApiKey, string.Empty);
    }

    private string ResolveTtsApiKey()
    {
        if (!string.IsNullOrWhiteSpace(ttsApiKeyOverride))
        {
            return ttsApiKeyOverride.Trim();
        }

        return string.IsNullOrWhiteSpace(apiKey) ? string.Empty : apiKey.Trim();
    }

    private void TryDeleteTemporaryAudioFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }
        catch
        {
        }
    }

    private void DisableTtsForSession(string reason)
    {
        ttsUnsupportedForSession = true;
        ttsDisableReason = string.IsNullOrWhiteSpace(reason) ? "Doktor sesi kapatildi." : reason;
        Debug.LogWarning("[AIManager] Doktor TTS bu oturum icin sessize alindi: " + ttsDisableReason);
    }

    private void ResetDoctorSpeechSessionState()
    {
        resolvedTtsModel = string.Empty;
        resolvedDoctorVoiceId = string.Empty;
        ttsCapabilityChecked = false;
        ttsUnsupportedForSession = false;
        ttsDisableReason = string.Empty;
        unsupportedTtsModels.Clear();
        cachedDoctorVoiceCandidates.Clear();
    }

    private void RemoveCachedDoctorVoiceCandidate(string voiceCandidate)
    {
        if (string.IsNullOrWhiteSpace(voiceCandidate))
        {
            return;
        }

        for (int i = cachedDoctorVoiceCandidates.Count - 1; i >= 0; i--)
        {
            if (string.Equals(cachedDoctorVoiceCandidates[i], voiceCandidate, System.StringComparison.OrdinalIgnoreCase))
            {
                cachedDoctorVoiceCandidates.RemoveAt(i);
            }
        }

        if (string.Equals(resolvedDoctorVoiceId, voiceCandidate, System.StringComparison.OrdinalIgnoreCase))
        {
            resolvedDoctorVoiceId = string.Empty;
        }
    }

    private void AddUniqueCandidate(List<string> candidates, string candidate, bool excludeUnsupportedModels)
    {
        if (candidates == null || string.IsNullOrWhiteSpace(candidate))
        {
            return;
        }

        string trimmedCandidate = candidate.Trim();
        if (excludeUnsupportedModels && unsupportedTtsModels.Contains(trimmedCandidate))
        {
            return;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            if (string.Equals(candidates[i], trimmedCandidate, System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        candidates.Add(trimmedCandidate);
    }

    private AudioSource ResolveDoctorAudioSource()
    {
        if (doctorAudioSource == null)
        {
            DoctorInteractable interactable = FindObjectOfType<DoctorInteractable>(true);
            if (interactable != null)
            {
                doctorAudioSource = interactable.GetComponent<AudioSource>();
            }
        }

        if (doctorAudioSource != null)
        {
            ApplyDoctorAudioSourceSettings(doctorAudioSource);
        }

        return doctorAudioSource;
    }

    private void ApplyDoctorAudioSourceSettings(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = Mathf.Max(0.8f, source.minDistance);
        source.maxDistance = Mathf.Max(12f, source.maxDistance);
        source.dopplerLevel = 0f;
    }

    private void StopDoctorSpeech()
    {
        if (activeSpeechRequest != null)
        {
            StopCoroutine(activeSpeechRequest);
            activeSpeechRequest = null;
        }

        if (doctorAudioSource != null)
        {
            doctorAudioSource.Stop();
            if (doctorAudioSource.clip == activeDoctorSpeechClip)
            {
                doctorAudioSource.clip = null;
            }
        }

        ReleaseActiveDoctorSpeechClip();
    }

    private void ReleaseActiveDoctorSpeechClip()
    {
        if (activeDoctorSpeechClip == null)
        {
            return;
        }

        Destroy(activeDoctorSpeechClip);
        activeDoctorSpeechClip = null;
    }

    private bool ShouldSpeakDoctorResponse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string normalized = NormalizeSpeechText(text);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        return !normalized.Contains("net bir yanit uretemedi") &&
               !normalized.Contains("su anda yanit uretemedi") &&
               !normalized.StartsWith("hata");
    }

    private string BuildSpeakableDoctorText(string text)
    {
        string normalized = NormalizeSpeechText(text);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        normalized = normalized.Replace("\r\n", "\n").Replace('\r', '\n');
        string[] segments = normalized.Split('\n');
        StringBuilder builder = new StringBuilder(normalized.Length + 16);
        for (int i = 0; i < segments.Length; i++)
        {
            string segment = segments[i].Trim();
            if (string.IsNullOrWhiteSpace(segment))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(" <#0.18#> ");
            }

            builder.Append(segment);
        }

        string speakableText = builder.ToString().Trim();
        if (speakableText.Length <= 340)
        {
            return speakableText;
        }

        int cutIndex = speakableText.LastIndexOfAny(new[] { '.', '!', '?', ',' }, 320);
        if (cutIndex < 120)
        {
            cutIndex = 340;
        }

        speakableText = speakableText.Substring(0, Mathf.Min(cutIndex + 1, speakableText.Length)).Trim();
        if (!speakableText.EndsWith(".") && !speakableText.EndsWith("!") && !speakableText.EndsWith("?"))
        {
            speakableText += ".";
        }

        return speakableText;
    }

    private string NormalizeSpeechText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string stripped = StripRichText(text)
            .Replace("&nbsp;", " ")
            .Replace("\t", " ");

        StringBuilder builder = new StringBuilder(stripped.Length);
        bool previousWasWhitespace = false;
        for (int i = 0; i < stripped.Length; i++)
        {
            char character = stripped[i];
            if (char.IsWhiteSpace(character))
            {
                if (character == '\n')
                {
                    builder.Append('\n');
                    previousWasWhitespace = true;
                }
                else if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            builder.Append(character);
            previousWasWhitespace = false;
        }

        return builder.ToString().Trim();
    }

    private string StripRichText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char character = value[i];
            if (character == '<')
            {
                if (TryGetSupportedRichTextTagLength(value, i, out int richTextTagLength))
                {
                    i += richTextTagLength - 1;
                    continue;
                }
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    private bool TryGetSupportedRichTextTagLength(string value, int startIndex, out int tagLength)
    {
        tagLength = 0;
        if (string.IsNullOrWhiteSpace(value) || startIndex < 0 || startIndex >= value.Length || value[startIndex] != '<')
        {
            return false;
        }

        int endIndex = value.IndexOf('>', startIndex + 1);
        if (endIndex < 0)
        {
            return false;
        }

        int candidateLength = endIndex - startIndex + 1;
        if (candidateLength < 3 || candidateLength > 32)
        {
            return false;
        }

        string candidate = value.Substring(startIndex + 1, candidateLength - 2).Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        int separatorIndex = candidate.IndexOfAny(new[] { ' ', '=' });
        string tagName = separatorIndex >= 0
            ? candidate.Substring(0, separatorIndex)
            : candidate;

        switch (tagName.ToLowerInvariant())
        {
            case "b":
            case "/b":
            case "i":
            case "/i":
            case "u":
            case "/u":
            case "s":
            case "/s":
            case "color":
            case "/color":
            case "size":
            case "/size":
            case "alpha":
            case "/alpha":
            case "material":
            case "/material":
            case "mark":
            case "/mark":
            case "sprite":
            {
                tagLength = candidateLength;
                return true;
            }
        }

        return false;
    }

    private byte[] DecodeHexAudio(string hexAudio)
    {
        if (string.IsNullOrWhiteSpace(hexAudio))
        {
            return null;
        }

        string normalized = hexAudio.Trim();
        if (normalized.StartsWith("0x", System.StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(2);
        }

        if ((normalized.Length & 1) == 1)
        {
            return null;
        }

        byte[] audioBytes = new byte[normalized.Length / 2];
        for (int i = 0; i < audioBytes.Length; i++)
        {
            audioBytes[i] = System.Convert.ToByte(normalized.Substring(i * 2, 2), 16);
        }

        return audioBytes;
    }

    private AudioClip CreateAudioFromBytes(byte[] audioBytes)
    {
        if (audioBytes == null || audioBytes.Length < 44)
        {
            return null;
        }

        if (audioBytes[0] != 'R' || audioBytes[1] != 'I' || audioBytes[2] != 'F' || audioBytes[3] != 'F')
        {
            return null;
        }

        int channels = 1;
        int sampleRate = Mathf.Max(16000, ttsSampleRate);
        int bitsPerSample = 16;
        int formatCode = 1;
        int dataOffset = -1;
        int dataSize = 0;
        int position = 12;

        while (position + 8 <= audioBytes.Length)
        {
            string chunkId = Encoding.ASCII.GetString(audioBytes, position, 4);
            int chunkSize = System.BitConverter.ToInt32(audioBytes, position + 4);
            int chunkDataStart = position + 8;
            if (chunkSize < 0 || chunkDataStart + chunkSize > audioBytes.Length)
            {
                break;
            }

            if (chunkId == "fmt ")
            {
                formatCode = System.BitConverter.ToInt16(audioBytes, chunkDataStart);
                channels = System.BitConverter.ToInt16(audioBytes, chunkDataStart + 2);
                sampleRate = System.BitConverter.ToInt32(audioBytes, chunkDataStart + 4);
                bitsPerSample = System.BitConverter.ToInt16(audioBytes, chunkDataStart + 14);
            }
            else if (chunkId == "data")
            {
                dataOffset = chunkDataStart;
                dataSize = chunkSize;
                break;
            }

            position = chunkDataStart + chunkSize;
            if ((chunkSize & 1) == 1)
            {
                position++;
            }
        }

        if (dataOffset < 0 || dataSize <= 0 || channels <= 0)
        {
            return null;
        }

        int bytesPerSample = bitsPerSample / 8;
        // PCM (1), 1 veya 2 kanal, 8 veya 16 bit kontrolu
        if (formatCode != 1) 
        {
            Debug.LogError("[AIManager] WAV Hatasi: Sadece PCM formatlari desteklenir. Gelen Code: " + formatCode);
            return null;
        }

        if (channels < 1 || channels > 2)
        {
            Debug.LogError("[AIManager] WAV Hatasi: Gelen kanal sayisi (" + channels + ") desteklenmiyor.");
            return null;
        }

        if (bitsPerSample != 8 && bitsPerSample != 16)
        {
            Debug.LogError("[AIManager] WAV Hatasi: Gelen bit derinligi (" + bitsPerSample + ") desteklenmiyor.");
            return null;
        }

        if (sampleRate <= 0)
        {
            Debug.LogError("[AIManager] WAV Hatasi: Gecersiz sample rate: " + sampleRate);
            return null;
        }

        int sampleCount = dataSize / bytesPerSample;
        if (sampleCount <= 0)
        {
            return null;
        }

        float[] samples = new float[sampleCount];
        if (bitsPerSample == 16)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                short sample = System.BitConverter.ToInt16(audioBytes, dataOffset + (i * 2));
                samples[i] = sample / 32768f;
            }
        }
        else
        {
            for (int i = 0; i < sampleCount; i++)
            {
                samples[i] = (audioBytes[dataOffset + i] - 128f) / 128f;
            }
        }

        int frameCount = sampleCount / channels;
        if (frameCount <= 0)
        {
            return null;
        }

        AudioClip clip = AudioClip.Create("DoctorSpeechClip", frameCount, channels, sampleRate, false);
        clip.SetData(samples, 0);
        LogVerbose("[AIManager] Ses klibi basariyla olusturuldu ve oynatiliyor. Kanallar: " + channels + ", Hz: " + sampleRate);
        return clip;
    }

    private void AppendToChatUI(string text)
    {
        if (chatHistoryText == null)
        {
            Debug.LogError("[AIManager] chatHistoryText atanmis degil.");
            return;
        }

        if (string.IsNullOrWhiteSpace(chatHistoryText.text))
        {
            chatHistoryText.text = text;
        }
        else
        {
            chatHistoryText.text += "\n\n" + text;
        }

        chatHistoryText.ForceMeshUpdate();
        RefreshChatLayout(true);
    }

    private void CacheCanvasState()
    {
        if (aiCanvas == null)
        {
            return;
        }

        if (canvasComponent == null)
        {
            canvasComponent = aiCanvas.GetComponent<Canvas>();
        }
    }

    private void PrepareCanvasInteraction()
    {
        if (aiCanvas == null)
        {
            return;
        }

        CacheCanvasState();

        if (canvasComponent != null)
        {
            canvasComponent.renderMode = RenderMode.WorldSpace;
            canvasComponent.additionalShaderChannels =
                AdditionalCanvasShaderChannels.TexCoord1 |
                AdditionalCanvasShaderChannels.Normal |
                AdditionalCanvasShaderChannels.Tangent;
        }

        aiCanvas.transform.localScale = AIChatCanvasLayout.PreferredCanvasScale;

        GraphicRaycaster standardRaycaster = aiCanvas.GetComponent<GraphicRaycaster>();
        if (standardRaycaster != null)
        {
            standardRaycaster.enabled = false;
        }

        if (aiCanvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
        {
            aiCanvas.AddComponent<TrackedDeviceGraphicRaycaster>();
        }

        vrUiClickHelper = aiCanvas.GetComponent<VRUIClickHelper>();
        if (vrUiClickHelper == null)
        {
            vrUiClickHelper = aiCanvas.AddComponent<VRUIClickHelper>();
        }

        AIChatCanvasLayout.ApplyCanvasLayout(aiCanvas);
        AutoAssignDoctorPanelReferences();
        EnsureCloseButtonExists();
        AutoAssignDoctorPanelReferences();
        RegisterCanvasButtonListeners();
        EnsureMiniTestQuestionBank();
        VoiceInputManager voiceInputManager = aiCanvas.GetComponent<VoiceInputManager>();
        if (voiceInputManager == null)
        {
            voiceInputManager = aiCanvas.AddComponent<VoiceInputManager>();
        }

        if (voiceInputManager != null)
        {
            voiceInputManager.ConfigureGroqTranscription(
                groqApiKey,
                groqApiUrl,
                groqWhisperModel,
                groqLanguage,
                groqMaxRecordingSeconds);
        }

        EnsureEventSystemReady();
        EnsureXRInteractorsCanHitUI();
        SyncKeyboardBindings();
        EnsureCloseButtonExists();
        ApplyUILayerToCanvas();
        PrepareChatPresentation();
        PrepareMiniTestPresentation();
        NormalizeCanvasRaycasts();
    }

    private void SyncKeyboardBindings()
    {
        AIChatCanvasLayout.ApplyCanvasLayout(aiCanvas);
        vrKeyboardManager = VRKeyboardManager.EnsureKeyboardSetup(aiCanvas, userInputField, sendButton);
        if (vrKeyboardManager != null)
        {
            vrKeyboardManager.SyncReferences(userInputField, sendButton);
        }

        AIChatCanvasLayout.ApplyCanvasLayout(aiCanvas);
    }

    private void AutoAssignDoctorPanelReferences()
    {
        if (aiCanvas == null)
        {
            return;
        }

        aiChatRoot = FindCanvasTransformByPaths(
                         AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.AIChatRootName)
                     ?.gameObject ?? aiChatRoot;

        miniTestRoot = FindCanvasTransformByPaths(
                           AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.MiniTestRootName)
                       ?.gameObject ?? miniTestRoot;

        chatHistoryText = FindCanvasComponentByPaths<TextMeshProUGUI>(
                              AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.AIChatRootName + "/" +
                              AIChatCanvasLayout.ScrollViewName + "/" + AIChatCanvasLayout.ViewportName + "/" +
                              AIChatCanvasLayout.ContentName + "/" + AIChatCanvasLayout.ChatHistoryName,
                              AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.ScrollViewName + "/" +
                              AIChatCanvasLayout.ViewportName + "/" + AIChatCanvasLayout.ContentName + "/" +
                              AIChatCanvasLayout.ChatHistoryName)
                          ?? chatHistoryText;

        chatScrollRect = FindCanvasComponentByPaths<ScrollRect>(
                             AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.AIChatRootName + "/" +
                             AIChatCanvasLayout.ScrollViewName,
                             AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.ScrollViewName)
                         ?? chatScrollRect;

        userInputField = FindCanvasComponentByPaths<TMP_InputField>(
                             AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.AIChatRootName + "/" +
                             AIChatCanvasLayout.InputContainerName + "/" + AIChatCanvasLayout.InputFieldName,
                             AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.InputContainerName + "/" +
                             AIChatCanvasLayout.InputFieldName)
                         ?? userInputField;

        sendButton = FindCanvasComponentByPaths<Button>(
                         AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.AIChatRootName + "/" +
                         AIChatCanvasLayout.InputContainerName + "/" + AIChatCanvasLayout.SendButtonName,
                         AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.InputContainerName + "/" +
                         AIChatCanvasLayout.SendButtonName)
                     ?? sendButton;

        closeButton = FindCanvasComponentByPaths<Button>(
                          AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.CloseButtonName)
                      ?? closeButton;

        aiTabButton = FindCanvasComponentByPaths<Button>(
                          AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.TopTabBarName + "/" +
                          AIChatCanvasLayout.TabAIButtonName)
                      ?? aiTabButton;

        miniTestTabButton = FindCanvasComponentByPaths<Button>(
                                AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.TopTabBarName + "/" +
                                AIChatCanvasLayout.TabMiniTestButtonName)
                            ?? miniTestTabButton;

        miniTestQuestionText = FindCanvasComponentByPaths<TextMeshProUGUI>(
                                   AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.MiniTestRootName + "/" +
                                   AIChatCanvasLayout.MiniTestQuestionCardName + "/" +
                                   AIChatCanvasLayout.MiniTestQuestionTextName,
                                   AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.MiniTestRootName + "/" +
                                   AIChatCanvasLayout.MiniTestQuestionTextName)
                               ?? miniTestQuestionText;

        miniTestProgressText = FindCanvasComponentByPaths<TextMeshProUGUI>(
                                   AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.MiniTestRootName + "/" +
                                   AIChatCanvasLayout.MiniTestHeaderName + "/" +
                                   AIChatCanvasLayout.MiniTestProgressName)
                               ?? miniTestProgressText;

        miniTestTimerText = FindCanvasComponentByPaths<TextMeshProUGUI>(
                                AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.MiniTestRootName + "/" +
                                AIChatCanvasLayout.MiniTestHeaderName + "/" +
                                AIChatCanvasLayout.MiniTestTimerName)
                            ?? miniTestTimerText;

        miniTestResultText = FindCanvasComponentByPaths<TextMeshProUGUI>(
                                 AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.MiniTestRootName + "/" +
                                 AIChatCanvasLayout.MiniTestResultTextName)
                             ?? miniTestResultText;

        EnsureMiniTestReferenceArrays();
    }

    private void EnsureMiniTestReferenceArrays()
    {
        if (miniTestOptionButtons == null || miniTestOptionButtons.Length != 3)
        {
            miniTestOptionButtons = new Button[3];
        }

        if (miniTestOptionLabelTexts == null || miniTestOptionLabelTexts.Length != 3)
        {
            miniTestOptionLabelTexts = new TextMeshProUGUI[3];
        }

        string[] optionButtonPaths =
        {
            AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.MiniTestRootName + "/" +
            AIChatCanvasLayout.MiniTestOptionsName + "/" + AIChatCanvasLayout.OptionAButtonName,
            AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.MiniTestRootName + "/" +
            AIChatCanvasLayout.MiniTestOptionsName + "/" + AIChatCanvasLayout.OptionBButtonName,
            AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.MiniTestRootName + "/" +
            AIChatCanvasLayout.MiniTestOptionsName + "/" + AIChatCanvasLayout.OptionCButtonName
        };

        string[] optionLabelPaths =
        {
            optionButtonPaths[0] + "/" + AIChatCanvasLayout.OptionALabelName,
            optionButtonPaths[1] + "/" + AIChatCanvasLayout.OptionBLabelName,
            optionButtonPaths[2] + "/" + AIChatCanvasLayout.OptionCLabelName
        };

        for (int i = 0; i < optionButtonPaths.Length; i++)
        {
            miniTestOptionButtons[i] = FindCanvasComponentByPaths<Button>(optionButtonPaths[i]) ?? miniTestOptionButtons[i];
            miniTestOptionLabelTexts[i] = FindCanvasComponentByPaths<TextMeshProUGUI>(optionLabelPaths[i]) ?? miniTestOptionLabelTexts[i];
        }

        miniTestNextButton = FindCanvasComponentByPaths<Button>(
                                 AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.MiniTestRootName + "/" +
                                 AIChatCanvasLayout.MiniTestFooterName + "/" +
                                 AIChatCanvasLayout.MiniTestNextButtonName)
                             ?? miniTestNextButton;

        miniTestRestartButton = FindCanvasComponentByPaths<Button>(
                                    AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.MiniTestRootName + "/" +
                                    AIChatCanvasLayout.MiniTestFooterName + "/" +
                                    AIChatCanvasLayout.MiniTestRestartButtonName)
                                ?? miniTestRestartButton;
    }

    private void RegisterCanvasButtonListeners()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnSendButtonClicked);
            sendButton.onClick.AddListener(OnSendButtonClicked);
        }

        EnsureCloseButtonExists();
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseAICanvas);
            closeButton.onClick.AddListener(CloseAICanvas);
        }

        if (aiTabButton != null)
        {
            aiTabButton.onClick.RemoveListener(HandleAITabClicked);
            aiTabButton.onClick.AddListener(HandleAITabClicked);
        }

        if (miniTestTabButton != null)
        {
            miniTestTabButton.onClick.RemoveListener(HandleMiniTestTabClicked);
            miniTestTabButton.onClick.AddListener(HandleMiniTestTabClicked);
        }

        if (miniTestNextButton != null)
        {
            miniTestNextButton.onClick.RemoveListener(OnMiniTestNextClicked);
            miniTestNextButton.onClick.AddListener(OnMiniTestNextClicked);
        }

        if (miniTestRestartButton != null)
        {
            miniTestRestartButton.onClick.RemoveListener(RestartMiniTest);
            miniTestRestartButton.onClick.AddListener(RestartMiniTest);
        }

        for (int i = 0; i < miniTestOptionButtons.Length; i++)
        {
            Button optionButton = miniTestOptionButtons[i];
            if (optionButton == null)
            {
                continue;
            }

            if (miniTestOptionListeners[i] == null)
            {
                int capturedIndex = i;
                miniTestOptionListeners[i] = () => OnMiniTestOptionSelected(capturedIndex);
            }

            optionButton.onClick.RemoveListener(miniTestOptionListeners[i]);
            optionButton.onClick.AddListener(miniTestOptionListeners[i]);
        }
    }

    private void PrepareMiniTestPresentation()
    {
        AutoAssignDoctorPanelReferences();
        EnsureMiniTestQuestionBank();
        UpdateTabVisuals();

        if (!miniTestStarted)
        {
            RenderMiniTestIdleState();
            return;
        }

        if (miniTestCompleted)
        {
            ShowMiniTestResult();
            return;
        }

        RenderCurrentMiniTestQuestion();
    }

    private void EnsureMiniTestQuestionBank()
    {
        if (miniTestQuestions == null)
        {
            miniTestQuestions = new List<MiniTestQuestion>();
        }

        if (miniTestQuestions.Count == 0)
        {
            miniTestQuestions.AddRange(BuildDefaultMiniTestQuestions());
        }

        for (int i = 0; i < miniTestQuestions.Count; i++)
        {
            MiniTestQuestion question = miniTestQuestions[i];
            if (question == null)
            {
                miniTestQuestions[i] = BuildFallbackMiniTestQuestion(i);
                question = miniTestQuestions[i];
            }

            if (question.siklar == null || question.siklar.Length != 3)
            {
                string[] sanitizedOptions = new string[3];
                if (question.siklar != null)
                {
                    for (int optionIndex = 0; optionIndex < Mathf.Min(3, question.siklar.Length); optionIndex++)
                    {
                        sanitizedOptions[optionIndex] = question.siklar[optionIndex];
                    }
                }

                question.siklar = sanitizedOptions;
            }

            if (string.IsNullOrWhiteSpace(question.soru))
            {
                question.soru = "Bu soruda hangi triyaj onceligi daha uygundur?";
            }

            for (int optionIndex = 0; optionIndex < question.siklar.Length; optionIndex++)
            {
                if (string.IsNullOrWhiteSpace(question.siklar[optionIndex]))
                {
                    question.siklar[optionIndex] = "Secenek " + (optionIndex + 1);
                }
            }

            question.dogruIndex = Mathf.Clamp(question.dogruIndex, 0, 2);
        }
    }

    private List<MiniTestQuestion> BuildDefaultMiniTestQuestions()
    {
        return new List<MiniTestQuestion>
        {
            CreateMiniTestQuestion(
                "Afet triyajinin temel amaci nedir?",
                0,
                "Sinirli kaynaklarla oncelikli hastalari hizla belirlemek",
                "Tum hastalari gelis sirasina gore tek tek muayene etmek",
                "Kesin tani koymadan yonlendirmeyi tamamen bekletmek"),
            CreateMiniTestQuestion(
                "Hangi hasta sari kategoriye daha uygundur?",
                1,
                "Hava yolu acildiktan sonra solunumu olmayan hasta",
                "Dolasimi stabil, ciddi yarali ama gecikebilir hasta",
                "Yuruyebilen ve yalniz minor yarasi olan hasta"),
            CreateMiniTestQuestion(
                "Hangi bulgu hastanin aciliyetini en cok artirir?",
                0,
                "Bozulmus solunum veya perfuzyon bulgusu",
                "Iletisimi duzgun, hafif agrili ve yuruyebiliyor olmasi",
                "Yakininin hastayi ayrintili tarif edebilmesi"),
            CreateMiniTestQuestion(
                "START hizli degerlendirmesinde hangi alanlar kontrol edilir?",
                0,
                "Solunum, perfuzyon ve mental durum",
                "Agri skoru, laboratuvar ve goruntuleme",
                "Alerji oykusu, boy ve vucut agirligi"),
            CreateMiniTestQuestion(
                "Olay yeri guvenliyse dogru yaklasim hangisidir?",
                0,
                "Hastalari kisa araliklarla yeniden degerlendirmek",
                "Ilk renk verildikten sonra tum hastalari ayni birakmak",
                "Yalniz yuruyebilen hastalari yeniden kontrol etmek")
        };
    }

    private MiniTestQuestion BuildFallbackMiniTestQuestion(int index)
    {
        return CreateMiniTestQuestion(
            "Afet triyajinda ilk karar verilirken hangi ilke onceliklidir?",
            0,
            "Hayati tehdit eden bulgulara gore hizli oncelik vermek",
            "Her hastadan once ayrintili oyku almak",
            "Kesin tani koyana kadar yonlendirmeyi geciktirmek");
    }

    private MiniTestQuestion CreateMiniTestQuestion(string questionText, int correctIndex, string optionA, string optionB, string optionC)
    {
        return new MiniTestQuestion
        {
            soru = questionText,
            siklar = new[] { optionA, optionB, optionC },
            dogruIndex = Mathf.Clamp(correctIndex, 0, 2)
        };
    }

    private void ResetMiniTestState()
    {
        EnsureMiniTestQuestionBank();
        activeMiniTestQuestionIndex = 0;
        correctMiniTestCount = 0;
        selectedMiniTestIndex = -1;
        miniTestStarted = false;
        miniTestAnswered = false;
        miniTestCompleted = false;
        miniTestStartRealtime = -1f;
        miniTestCompletedRealtime = -1f;
        RenderMiniTestIdleState();
        UpdateTabVisuals();
        SetMiniTestTimerText(0f);
    }

    private void TickMiniTestTimer()
    {
        if (!miniTestStarted || miniTestCompleted || miniTestStartRealtime < 0f)
        {
            return;
        }

        if (miniTestTimerText == null)
        {
            return;
        }

        float elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - miniTestStartRealtime);
        SetMiniTestTimerText(elapsed);
    }

    private void SetMiniTestTimerText(float elapsedSeconds)
    {
        if (miniTestTimerText == null)
        {
            return;
        }

        int total = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds));
        int minutes = total / 60;
        int seconds = total % 60;
        miniTestTimerText.text = string.Format("Sure: {0:00}:{1:00}", minutes, seconds);
    }

    private void RenderMiniTestIdleState()
    {
        ApplyMiniTestQuestionTypography(false);

        if (miniTestProgressText != null)
        {
            miniTestProgressText.text = "Hazir";
        }

        if (miniTestQuestionText != null)
        {
            miniTestQuestionText.text = "Mini Test sekmesine gectiginde 5 soruluk genel triyaj tekrarina baslayacaksin.";
        }

        if (miniTestResultText != null)
        {
            miniTestResultText.text = "Sorular acildiginda seciminin geri bildirimini aninda goreceksin.";
        }

        for (int i = 0; i < miniTestOptionButtons.Length; i++)
        {
            if (miniTestOptionButtons[i] != null)
            {
                miniTestOptionButtons[i].gameObject.SetActive(true);
                miniTestOptionButtons[i].interactable = false;
            }

            if (miniTestOptionLabelTexts[i] != null)
            {
                miniTestOptionLabelTexts[i].text = "Secenek " + (i + 1);
            }
        }

        if (miniTestNextButton != null)
        {
            miniTestNextButton.gameObject.SetActive(true);
            miniTestNextButton.interactable = false;
        }

        if (miniTestRestartButton != null)
        {
            miniTestRestartButton.gameObject.SetActive(false);
        }

        RefreshMiniTestOptionVisuals();
    }

    private void HandleAITabClicked()
    {
        SwitchTab(DoctorPanelTab.AIChat);
    }

    private void HandleMiniTestTabClicked()
    {
        SwitchTab(DoctorPanelTab.MiniTest);
    }

    private void SwitchTab(DoctorPanelTab targetTab)
    {
        AutoAssignDoctorPanelReferences();
        activeDoctorPanelTab = targetTab;

        bool showAIChat = targetTab == DoctorPanelTab.AIChat;
        SetAIChatPanelVisibility(showAIChat);
        SetMiniTestPanelVisibility(!showAIChat);

        UpdateTabVisuals();

        if (showAIChat)
        {
            if (miniTestRoot != null)
            {
                miniTestRoot.SetActive(false);
            }

            StartCoroutine(FocusInputFieldNextFrame());
            return;
        }

        ClearChatInputFocus();
        if (!miniTestStarted)
        {
            StartMiniTest();
        }
        else if (miniTestCompleted)
        {
            ShowMiniTestResult();
        }
        else
        {
            RenderCurrentMiniTestQuestion();
        }
    }

    private void SetAIChatPanelVisibility(bool visible)
    {
        if (aiChatRoot != null)
        {
            aiChatRoot.SetActive(visible);
        }

        Transform keyboardDrawerTransform = FindCanvasTransformByPaths(
            AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.KeyboardDrawerName);
        if (keyboardDrawerTransform != null)
        {
            keyboardDrawerTransform.gameObject.SetActive(visible);
        }
    }

    private void SetMiniTestPanelVisibility(bool visible)
    {
        if (miniTestRoot != null)
        {
            miniTestRoot.SetActive(visible);
        }
    }

    private void SetCanvasChildVisibility(string childName, bool visible)
    {
        if (aiCanvas == null || string.IsNullOrWhiteSpace(childName))
        {
            return;
        }

        Transform child = FindChildTransformByName(aiCanvas.transform, childName);
        if (child != null)
        {
            child.gameObject.SetActive(visible);
        }
    }

    private void ClearChatInputFocus()
    {
        if (vrKeyboardManager != null)
        {
            vrKeyboardManager.ReleaseInputFocusForExternalUpdate();
            vrKeyboardManager.HideKeyboard();
        }

        if (userInputField != null)
        {
            userInputField.DeactivateInputField();
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        ReleaseChatTextEntryLock();
    }

    private void UpdateTabVisuals()
    {
        ApplyTabButtonVisual(aiTabButton, activeDoctorPanelTab == DoctorPanelTab.AIChat);
        ApplyTabButtonVisual(miniTestTabButton, activeDoctorPanelTab == DoctorPanelTab.MiniTest);
    }

    private void ApplyTabButtonVisual(Button button, bool isActive)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            Color targetColor = isActive ? AIChatCanvasLayout.TabButtonActiveColor : AIChatCanvasLayout.TabButtonColor;
            image.color = targetColor;
            image.raycastTarget = true;
            button.targetGraphic = image;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = isActive ? AIChatCanvasLayout.TabButtonActiveColor : AIChatCanvasLayout.TabButtonColor;
        colors.highlightedColor = AIChatCanvasLayout.TabButtonHoverColor;
        colors.pressedColor = AIChatCanvasLayout.TabButtonActiveColor;
        colors.selectedColor = AIChatCanvasLayout.TabButtonActiveColor;
        colors.disabledColor = new Color(0.24f, 0.28f, 0.34f, 0.55f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }

    private void StartMiniTest()
    {
        EnsureMiniTestQuestionBank();
        activeMiniTestQuestionIndex = 0;
        correctMiniTestCount = 0;
        selectedMiniTestIndex = -1;
        miniTestStarted = true;
        miniTestAnswered = false;
        miniTestCompleted = false;
        miniTestStartRealtime = Time.realtimeSinceStartup;
        miniTestCompletedRealtime = -1f;
        SetMiniTestTimerText(0f);

        TrainingAnalyticsFacade.OnQuizStarted(
            TrainingAnalyticsFacade.Module3Id,
            TrainingAnalyticsFacade.Module3Name,
            "doctor_mini_test",
            "Doktor Mini Test",
            miniTestQuestions != null ? miniTestQuestions.Count : 0,
            new Dictionary<string, object>
            {
                { AnalyticsParams.PanelId, "doctor_ai_panel" }
            });

        RenderCurrentMiniTestQuestion();
    }

    private void RenderCurrentMiniTestQuestion()
    {
        ApplyMiniTestQuestionTypography(false);

        if (miniTestQuestions == null || miniTestQuestions.Count == 0)
        {
            RenderMiniTestIdleState();
            return;
        }

        activeMiniTestQuestionIndex = Mathf.Clamp(activeMiniTestQuestionIndex, 0, miniTestQuestions.Count - 1);
        MiniTestQuestion question = miniTestQuestions[activeMiniTestQuestionIndex];
        if (question == null)
        {
            question = BuildFallbackMiniTestQuestion(activeMiniTestQuestionIndex);
            miniTestQuestions[activeMiniTestQuestionIndex] = question;
        }

        if (miniTestProgressText != null)
        {
            miniTestProgressText.text = "Soru " + (activeMiniTestQuestionIndex + 1) + "/" + miniTestQuestions.Count;
        }

        if (miniTestQuestionText != null)
        {
            miniTestQuestionText.text = question.soru;
        }

        if (miniTestResultText != null)
        {
            miniTestResultText.text = miniTestAnswered
                ? (selectedMiniTestIndex == question.dogruIndex
                    ? "Dogru. Bu secenek triyaj onceligini en iyi destekliyordu."
                    : "Yanlis. Dogru secenek burada daha uygun triyaj onceligini gosteriyor.")
                : "Dogru secenegi isaretle. Geri bildirim hemen gosterilecek.";
        }

        for (int i = 0; i < miniTestOptionButtons.Length; i++)
        {
            if (miniTestOptionButtons[i] != null)
            {
                miniTestOptionButtons[i].gameObject.SetActive(true);
                miniTestOptionButtons[i].interactable = !miniTestAnswered;
            }

            if (miniTestOptionLabelTexts[i] != null)
            {
                miniTestOptionLabelTexts[i].text = question.siklar != null && i < question.siklar.Length
                    ? question.siklar[i]
                    : "Secenek " + (i + 1);
            }
        }

        UpdateMiniTestActionButtons();
        RefreshMiniTestOptionVisuals();
    }

    private void OnMiniTestOptionSelected(int selectedIndex)
    {
        if (!miniTestStarted || miniTestCompleted || miniTestAnswered)
        {
            return;
        }

        if (miniTestQuestions == null || miniTestQuestions.Count == 0)
        {
            return;
        }

        MiniTestQuestion currentQuestion = miniTestQuestions[Mathf.Clamp(activeMiniTestQuestionIndex, 0, miniTestQuestions.Count - 1)];
        if (currentQuestion == null)
        {
            return;
        }

        selectedMiniTestIndex = Mathf.Clamp(selectedIndex, 0, 2);
        miniTestAnswered = true;

        if (selectedMiniTestIndex == currentQuestion.dogruIndex)
        {
            correctMiniTestCount++;
        }

        TrainingAnalyticsFacade.OnQuizAnswered(
            TrainingAnalyticsFacade.Module3Id,
            TrainingAnalyticsFacade.Module3Name,
            "doctor_mini_test",
            "Doktor Mini Test",
            activeMiniTestQuestionIndex + 1,
            selectedMiniTestIndex,
            currentQuestion.dogruIndex,
            selectedMiniTestIndex == currentQuestion.dogruIndex,
            miniTestQuestions != null ? miniTestQuestions.Count : 0,
            new Dictionary<string, object>
            {
                { AnalyticsParams.PanelId, "doctor_ai_panel" }
            });

        RenderCurrentMiniTestQuestion();
    }

    private void RefreshMiniTestOptionVisuals()
    {
        int correctIndex = -1;
        if (miniTestQuestions != null &&
            miniTestQuestions.Count > 0 &&
            activeMiniTestQuestionIndex >= 0 &&
            activeMiniTestQuestionIndex < miniTestQuestions.Count &&
            miniTestQuestions[activeMiniTestQuestionIndex] != null)
        {
            correctIndex = Mathf.Clamp(miniTestQuestions[activeMiniTestQuestionIndex].dogruIndex, 0, 2);
        }

        for (int i = 0; i < miniTestOptionButtons.Length; i++)
        {
            Button button = miniTestOptionButtons[i];
            TextMeshProUGUI label = i < miniTestOptionLabelTexts.Length ? miniTestOptionLabelTexts[i] : null;
            if (button == null)
            {
                continue;
            }

            Color buttonColor = AIChatCanvasLayout.MiniTestOptionColor;
            if (miniTestAnswered && i == correctIndex)
            {
                buttonColor = AIChatCanvasLayout.MiniTestOptionCorrectColor;
            }
            else if (miniTestAnswered && i == selectedMiniTestIndex && selectedMiniTestIndex != correctIndex)
            {
                buttonColor = AIChatCanvasLayout.MiniTestOptionWrongColor;
            }
            else if (!miniTestAnswered && i == selectedMiniTestIndex)
            {
                buttonColor = AIChatCanvasLayout.MiniTestOptionSelectedColor;
            }

            ApplyMiniTestOptionButtonStyle(button, label, buttonColor);
        }
    }

    private void ApplyMiniTestOptionButtonStyle(Button button, TextMeshProUGUI label, Color targetColor)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = targetColor;
            image.raycastTarget = true;
            button.targetGraphic = image;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = targetColor;
        colors.highlightedColor = AIChatCanvasLayout.TabButtonHoverColor;
        colors.pressedColor = targetColor;
        colors.selectedColor = targetColor;
        colors.disabledColor = targetColor;
        colors.colorMultiplier = 1f;
        button.colors = colors;

        if (label != null)
        {
            label.color = AIChatCanvasLayout.MiniTestTextColor;
        }
    }

    private void UpdateMiniTestActionButtons()
    {
        bool isLastQuestion = miniTestQuestions != null &&
                              miniTestQuestions.Count > 0 &&
                              activeMiniTestQuestionIndex >= miniTestQuestions.Count - 1;

        if (miniTestNextButton != null)
        {
            miniTestNextButton.gameObject.SetActive(!miniTestCompleted);
            miniTestNextButton.interactable = miniTestAnswered;
            TextMeshProUGUI nextLabel = miniTestNextButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (nextLabel != null)
            {
                nextLabel.text = isLastQuestion ? AIChatCanvasLayout.MiniTestFinishButtonText : AIChatCanvasLayout.MiniTestNextButtonText;
            }
        }

        if (miniTestRestartButton != null)
        {
            miniTestRestartButton.gameObject.SetActive(miniTestCompleted);
            miniTestRestartButton.interactable = miniTestCompleted;
        }
    }

    private void OnMiniTestNextClicked()
    {
        if (!miniTestStarted || miniTestCompleted || !miniTestAnswered)
        {
            return;
        }

        if (miniTestQuestions == null || miniTestQuestions.Count == 0)
        {
            return;
        }

        if (activeMiniTestQuestionIndex >= miniTestQuestions.Count - 1)
        {
            ShowMiniTestResult();
            return;
        }

        activeMiniTestQuestionIndex++;
        selectedMiniTestIndex = -1;
        miniTestAnswered = false;
        RenderCurrentMiniTestQuestion();
    }

    private void ShowMiniTestResult()
    {
        EnsureMiniTestQuestionBank();

        miniTestCompleted = true;
        miniTestAnswered = false;

        int totalQuestionCount = miniTestQuestions != null ? miniTestQuestions.Count : 0;
        int wrongMiniTestCount = Mathf.Max(0, totalQuestionCount - correctMiniTestCount);
        int percentage = totalQuestionCount > 0
            ? Mathf.RoundToInt(correctMiniTestCount * 100f / totalQuestionCount)
            : 0;

        if (miniTestStartRealtime < 0f)
        {
            miniTestStartRealtime = Time.realtimeSinceStartup;
        }

        if (miniTestCompletedRealtime < 0f || miniTestCompletedRealtime < miniTestStartRealtime)
        {
            miniTestCompletedRealtime = Time.realtimeSinceStartup;
        }

        float elapsedSeconds = Mathf.Max(0f, miniTestCompletedRealtime - miniTestStartRealtime);
        float averageSecondsPerQuestion = totalQuestionCount > 0
            ? elapsedSeconds / totalQuestionCount
            : 0f;

        SetMiniTestTimerText(elapsedSeconds);

        if (miniTestProgressText != null)
        {
            miniTestProgressText.text = "Sonuc Ozeti";
        }

        ApplyMiniTestQuestionTypography(true);

        if (miniTestQuestionText != null)
        {
            miniTestQuestionText.text =
                "<b>Mini test tamamlandi.</b>\n" +
                "Seviye: <color=#8FDFFF><b>" + BuildMiniTestGrade(percentage) + "</b></color>";
        }

        if (miniTestResultText != null)
        {
            miniTestResultText.text =
                BuildMiniTestFeedback(percentage) + "\n" +
                "Tempo: " + BuildMiniTestPaceFeedback(averageSecondsPerQuestion);
        }

        TrainingAnalyticsFacade.OnQuizCompleted(
            TrainingAnalyticsFacade.Module3Id,
            TrainingAnalyticsFacade.Module3Name,
            "doctor_mini_test",
            "Doktor Mini Test",
            totalQuestionCount,
            correctMiniTestCount,
            new Dictionary<string, object>
            {
                { AnalyticsParams.PanelId, "doctor_ai_panel" }
            });

        TrainingAnalyticsFacade.OnScoreRecorded(
            TrainingAnalyticsFacade.Module3Id,
            TrainingAnalyticsFacade.Module3Name,
            "mini_test_score",
            correctMiniTestCount,
            percentage,
            new Dictionary<string, object>
            {
                { AnalyticsParams.PanelId, "doctor_ai_panel" },
                { AnalyticsParams.TotalCount, totalQuestionCount }
            });

        RenderMiniTestResultCards(
            totalQuestionCount,
            wrongMiniTestCount,
            percentage,
            elapsedSeconds,
            averageSecondsPerQuestion);

        UpdateMiniTestActionButtons();
    }

    private void ApplyMiniTestQuestionTypography(bool resultMode)
    {
        if (miniTestQuestionText == null)
        {
            return;
        }

        miniTestQuestionText.enableWordWrapping = true;
        miniTestQuestionText.alignment = TextAlignmentOptions.TopLeft;
        miniTestQuestionText.overflowMode = resultMode ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;
        miniTestQuestionText.margin = resultMode
            ? new Vector4(14f, 10f, 14f, 10f)
            : new Vector4(14f, 12f, 14f, 8f);
        miniTestQuestionText.enableAutoSizing = resultMode;
        miniTestQuestionText.fontSizeMin = 16f;
        miniTestQuestionText.fontSizeMax = AIChatCanvasLayout.MiniTestQuestionFontSize;
        miniTestQuestionText.fontSize = resultMode ? 20f : AIChatCanvasLayout.MiniTestQuestionFontSize;
        miniTestQuestionText.fontStyle = FontStyles.Bold;
    }

    private string BuildMiniTestFeedback(int percentage)
    {
        if (percentage >= 70)
        {
            return "Genel triyaj onceliklendirmesini tutarli uyguluyorsun.";
        }

        if (percentage >= 40)
        {
            return "Temel mantik iyi; renk kararlarini ve kritik bulgulari biraz daha pekistir.";
        }

        return "Triyaj amaci, renkler ve yeniden degerlendirme mantigini yeniden gozden gecir.";
    }

    private string BuildMiniTestGrade(int percentage)
    {
        if (percentage >= 90)
        {
            return "Ust Duzey";
        }

        if (percentage >= 70)
        {
            return "Guclu";
        }

        if (percentage >= 40)
        {
            return "Gelisiyor";
        }

        return "Tekrar Gerekli";
    }

    private string BuildMiniTestPaceFeedback(float averageSecondsPerQuestion)
    {
        if (averageSecondsPerQuestion <= 18f)
        {
            return "Cok hizli karar veriyorsun; kritik bulguyu kacirmamaya dikkat et.";
        }

        if (averageSecondsPerQuestion <= 32f)
        {
            return "Hiz ve dogruluk dengesi iyi.";
        }

        if (averageSecondsPerQuestion <= 50f)
        {
            return "Dikkatli karar veriyorsun; pratikle sureyi kisaltabilirsin.";
        }

        return "Analiz odakli gidiyorsun; sahada daha hizli oncelik vermeyi dene.";
    }

    private string BuildMiniTestDurationText(float elapsedSeconds)
    {
        int roundedSeconds = Mathf.Max(0, Mathf.RoundToInt(elapsedSeconds));
        int minutes = roundedSeconds / 60;
        int seconds = roundedSeconds % 60;
        if (minutes <= 0)
        {
            return seconds + " sn";
        }

        return minutes + " dk " + seconds.ToString("00") + " sn";
    }

    private string BuildMiniTestCompactDuration(float elapsedSeconds)
    {
        int roundedSeconds = Mathf.Max(0, Mathf.RoundToInt(elapsedSeconds));
        int minutes = roundedSeconds / 60;
        int seconds = roundedSeconds % 60;
        if (minutes <= 0)
        {
            return seconds + " sn";
        }

        return minutes.ToString("00") + ":" + seconds.ToString("00");
    }

    private void RenderMiniTestResultCards(
        int totalQuestionCount,
        int wrongMiniTestCount,
        int percentage,
        float elapsedSeconds,
        float averageSecondsPerQuestion)
    {
        Color scoreCardColor = Color.Lerp(
            AIChatCanvasLayout.MiniTestOptionWrongColor,
            AIChatCanvasLayout.MiniTestOptionCorrectColor,
            Mathf.Clamp01(percentage / 100f));

        for (int i = 0; i < miniTestOptionButtons.Length; i++)
        {
            Button button = miniTestOptionButtons[i];
            TextMeshProUGUI label = miniTestOptionLabelTexts != null && i < miniTestOptionLabelTexts.Length
                ? miniTestOptionLabelTexts[i]
                : null;
            if (button == null)
            {
                continue;
            }

            button.gameObject.SetActive(true);
            button.interactable = false;

            if (label != null)
            {
                if (i == 0)
                {
                    label.text = "<b>SKOR</b>\nDogru " + correctMiniTestCount + "/" + totalQuestionCount + " | Yanlis " + wrongMiniTestCount;
                }
                else if (i == 1)
                {
                    label.text = "<b>BASARI</b>\n%" + percentage + " | " + BuildMiniTestGrade(percentage);
                }
                else
                {
                    label.text = "<b>SURE</b>\n" + BuildMiniTestDurationText(elapsedSeconds) + " | " + BuildMiniTestCompactDuration(averageSecondsPerQuestion) + " / soru";
                }
            }

            Color targetColor = i == 0
                ? scoreCardColor
                : (i == 1 ? AIChatCanvasLayout.TabButtonActiveColor : AIChatCanvasLayout.MiniTestOptionSelectedColor);

            ApplyMiniTestOptionButtonStyle(button, label, targetColor);
        }
    }

    private void RestartMiniTest()
    {
        StartMiniTest();
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

    private void ApplyUILayerToCanvas()
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0 || aiCanvas == null)
        {
            return;
        }

        SetLayerRecursively(aiCanvas.transform, uiLayer);
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

    private void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root)
        {
            SetLayerRecursively(child, layer);
        }
    }

    private void PrepareChatPresentation()
    {
        AutoAssignDoctorPanelReferences();
        if (chatHistoryText == null)
        {
            Debug.LogError("[AIManager] ChatHistory_Text atanmis degil.");
            return;
        }

        if (chatScrollRect == null)
        {
            Debug.LogError("[AIManager] chatScrollRect atanmis degil.");
            return;
        }

        chatViewportRect = chatScrollRect.viewport;
        chatContentRect = chatScrollRect.content;

        if (chatViewportRect == null)
        {
            Debug.LogError("[AIManager] ScrollRect viewport referansi eksik.");
            return;
        }

        if (chatContentRect == null)
        {
            Debug.LogError("[AIManager] ScrollRect content referansi eksik.");
            return;
        }

        if (chatHistoryText.rectTransform.parent != chatContentRect)
        {
            Debug.LogError("[AIManager] ChatHistory_Text parent'i Chat_Content olmali.");
            return;
        }

        EnsureViewportClipSetup();
        NormalizeCanonicalChatLayout();
        DisableLegacyChatLayoutComponents();

        chatScrollRect.horizontal = false;
        chatScrollRect.vertical = true;
        chatScrollRect.movementType = ScrollRect.MovementType.Clamped;
        chatScrollRect.viewport = chatViewportRect;
        chatScrollRect.content = chatContentRect;

        chatHistoryText.alignment = TextAlignmentOptions.TopLeft;
        chatHistoryText.enableWordWrapping = true;
        chatHistoryText.overflowMode = TextOverflowModes.Overflow;
        chatHistoryText.raycastTarget = false;
        chatHistoryText.richText = true;
        chatHistoryText.color = AIChatCanvasLayout.UserTextColor;
        chatHistoryText.fontSize = AIChatCanvasLayout.ChatFontSize;
        chatHistoryText.lineSpacing = AIChatCanvasLayout.ChatLineSpacing;
        chatHistoryText.paragraphSpacing = AIChatCanvasLayout.ChatParagraphSpacing;
        chatHistoryText.margin = new Vector4(0f, AIChatCanvasLayout.ChatTextTopMargin, 0f, 0f);
        chatHistoryText.enabled = true;
        chatHistoryText.gameObject.SetActive(true);

        EnsureWelcomeMessage();
        RefreshChatLayout(false);
    }

    private void EnsureViewportClipSetup()
    {
        if (chatViewportRect == null)
        {
            return;
        }

        Mask legacyMask = chatViewportRect.GetComponent<Mask>();
        if (legacyMask != null)
        {
            legacyMask.enabled = false;
        }

        if (chatViewportRect.GetComponent<RectMask2D>() == null)
        {
            chatViewportRect.gameObject.AddComponent<RectMask2D>();
        }

        Image viewportImage = chatViewportRect.GetComponent<Image>();
        if (viewportImage != null)
        {
            viewportImage.raycastTarget = false;
        }
    }

    private void NormalizeCanonicalChatLayout()
    {
        if (chatContentRect == null || chatHistoryText == null)
        {
            return;
        }

        chatContentRect.anchorMin = new Vector2(0f, 1f);
        chatContentRect.anchorMax = new Vector2(1f, 1f);
        chatContentRect.pivot = new Vector2(0.5f, 1f);
        chatContentRect.anchoredPosition = Vector2.zero;
        chatContentRect.localScale = Vector3.one;
        chatContentRect.sizeDelta = new Vector2(0f, AIChatCanvasLayout.ChatMinimumHeight + AIChatCanvasLayout.ChatBottomPadding);

        RectTransform chatRect = chatHistoryText.rectTransform;
        chatRect.anchorMin = new Vector2(0f, 1f);
        chatRect.anchorMax = new Vector2(1f, 1f);
        chatRect.pivot = new Vector2(0.5f, 1f);
        chatRect.anchoredPosition = new Vector2(0f, -AIChatCanvasLayout.ChatTopPadding);
        chatRect.sizeDelta = new Vector2(-AIChatCanvasLayout.ChatSidePadding, AIChatCanvasLayout.ChatMinimumHeight);
        chatRect.localScale = Vector3.one;
    }

    private void DisableLegacyChatLayoutComponents()
    {
        if (chatContentRect == null)
        {
            return;
        }

        LayoutGroup layoutGroup = chatContentRect.GetComponent<LayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
        }

        ContentSizeFitter contentSizeFitter = chatContentRect.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null)
        {
            contentSizeFitter.enabled = false;
        }
    }

    private void NormalizeCanvasRaycasts()
    {
        if (aiCanvas == null)
        {
            return;
        }

        foreach (TextMeshProUGUI text in aiCanvas.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            text.raycastTarget = false;
        }

        foreach (Button button in aiCanvas.GetComponentsInChildren<Button>(true))
        {
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.raycastTarget = true;
                button.targetGraphic = buttonImage;
            }
        }

        if (userInputField != null)
        {
            Image inputImage = userInputField.GetComponent<Image>();
            if (inputImage != null)
            {
                inputImage.raycastTarget = true;
            }
        }

        if (sendButton != null)
        {
            Image buttonImage = sendButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.raycastTarget = true;
            }
        }

        if (closeButton != null)
        {
            Image closeImage = closeButton.GetComponent<Image>();
            if (closeImage != null)
            {
                closeImage.raycastTarget = true;
            }
        }
    }

    private void EnsureWelcomeMessage()
    {
        if (chatHistoryText == null)
        {
            return;
        }

        if (AIChatCanvasLayout.ShouldUpgradeWelcomeText(chatHistoryText.text))
        {
            chatHistoryText.text = AIChatCanvasLayout.WelcomeMessage;
        }
    }

    private string ResolveApiModel()
    {
        if (!string.IsNullOrWhiteSpace(apiURL) && apiURL.Contains("minimax.io"))
        {
            if (string.IsNullOrWhiteSpace(aiModel) || aiModel.StartsWith("abab"))
            {
                return minimaxFallbackModel;
            }

            string normalizedModel = aiModel.Trim().ToLowerInvariant();
            if (normalizedModel.Contains("highspeed"))
            {
                return minimaxFallbackModel;
            }
        }

        return aiModel;
    }

    private string ResolveDoctorChatModel()
    {
        string preferredModel = ResolveApiModel();
        if (!string.IsNullOrWhiteSpace(preferredModel))
        {
            return preferredModel.Trim();
        }

        return PreferredDoctorMinimaxModel;
    }

    private bool ShouldUseFallbackModel(string preferredModel)
    {
        if (string.IsNullOrWhiteSpace(minimaxFallbackModel))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(preferredModel) ||
               !string.Equals(preferredModel.Trim(), minimaxFallbackModel.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsM27Model(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            return false;
        }

        string normalized = modelName.Trim().ToLowerInvariant();
        return normalized.Contains("m2.7");
    }

    private List<OpenAIMessage> BuildMessagesForRequest()
    {
        List<OpenAIMessage> conversationMessages = new List<OpenAIMessage>(messageHistory.Count);
        for (int i = 0; i < messageHistory.Count; i++)
        {
            OpenAIMessage message = messageHistory[i];
            if (message == null)
            {
                continue;
            }

            if (string.Equals(message.role, "system", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            conversationMessages.Add(message);
        }

        string latestUserMessage = GetLatestConversationUserMessage(conversationMessages);
        bool isFirstDoctorReply = !HasAssistantConversationMessage(conversationMessages);

        int systemMessageCount = 1;
        int targetCount = Mathf.Clamp(maxMessagesPerRequest, MinDoctorChatMessageWindow, MaxDoctorChatMessageWindow);
        int remainingSlots = Mathf.Max(1, targetCount - systemMessageCount);
        int startIndex = Mathf.Max(0, conversationMessages.Count - remainingSlots);

        List<OpenAIMessage> trimmed = new List<OpenAIMessage>(remainingSlots + systemMessageCount)
        {
            new OpenAIMessage
            {
                role = "system",
                content = BuildDoctorSystemPrompt(latestUserMessage, isFirstDoctorReply)
            }
        };

        for (int i = startIndex; i < conversationMessages.Count; i++)
        {
            trimmed.Add(conversationMessages[i]);
        }

        return trimmed;
    }

    private static string GetLatestConversationUserMessage(IList<OpenAIMessage> conversationMessages)
    {
        if (conversationMessages == null)
        {
            return string.Empty;
        }

        for (int i = conversationMessages.Count - 1; i >= 0; i--)
        {
            OpenAIMessage message = conversationMessages[i];
            if (message == null)
            {
                continue;
            }

            if (string.Equals(message.role, "user", System.StringComparison.OrdinalIgnoreCase))
            {
                return message.content ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static bool HasAssistantConversationMessage(IList<OpenAIMessage> conversationMessages)
    {
        if (conversationMessages == null)
        {
            return false;
        }

        for (int i = 0; i < conversationMessages.Count; i++)
        {
            OpenAIMessage message = conversationMessages[i];
            if (message == null)
            {
                continue;
            }

            if (string.Equals(message.role, "assistant", System.StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(message.content))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsRetryableFailure(UnityWebRequest.Result result, long responseCode, string errorText, string responseBody)
    {
        string normalizedError = string.IsNullOrWhiteSpace(errorText) ? string.Empty : errorText.ToLowerInvariant();
        string normalizedBody = string.IsNullOrWhiteSpace(responseBody) ? string.Empty : responseBody.ToLowerInvariant();

        if (result == UnityWebRequest.Result.ConnectionError)
        {
            return true;
        }

        if (responseCode == 400 ||
            normalizedBody.Contains("invalid chat setting") ||
            normalizedBody.Contains("invalid params") ||
            normalizedBody.Contains("bad_request_error"))
        {
            return false;
        }

        if (responseCode == 408 || responseCode == 429)
        {
            return true;
        }

        if (responseCode >= 500 && responseCode < 600)
        {
            return true;
        }

        if (normalizedError.Contains("timed out") || normalizedError.Contains("timeout"))
        {
            return true;
        }

        return normalizedBody.Contains("rate limit") ||
               normalizedBody.Contains("timeout") ||
               normalizedBody.Contains("temporarily unavailable") ||
               normalizedBody.Contains("server_error");
    }

    private static string BuildDoctorChatUserErrorMessage(long responseCode, string errorText, string responseBody)
    {
        string normalizedError = string.IsNullOrWhiteSpace(errorText) ? string.Empty : errorText.ToLowerInvariant();
        string normalizedBody = string.IsNullOrWhiteSpace(responseBody) ? string.Empty : responseBody.ToLowerInvariant();

        if (responseCode == 400 ||
            normalizedBody.Contains("invalid chat setting") ||
            normalizedBody.Contains("invalid params") ||
            normalizedBody.Contains("bad_request_error"))
        {
            return "Doktorun yanit ayarlarinda bir uyumsuzluk var. Sistem ayarlarini kontrol edip tekrar deneyin.";
        }

        if (responseCode == 408 ||
            responseCode == 429 ||
            (responseCode >= 500 && responseCode < 600) ||
            normalizedError.Contains("timed out") ||
            normalizedError.Contains("timeout") ||
            normalizedBody.Contains("rate limit") ||
            normalizedBody.Contains("temporarily unavailable") ||
            normalizedBody.Contains("server_error"))
        {
            return "Doktor servisi su an yanit vermiyor. Kisa bir sure sonra tekrar deneyin.";
        }

        if (normalizedError.Contains("cannot resolve") ||
            normalizedError.Contains("resolve host") ||
            normalizedError.Contains("connection"))
        {
            return "Doktor servisine su an erisilemiyor. API adresini kontrol edin veya biraz sonra tekrar deneyin.";
        }

        return "Doktordan su an yanit alinamadi. Lutfen tekrar deneyin.";
    }

    private string BuildCompactApiError(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return "Sunucudan bos hata cevabi dondu.";
        }

        string compact = responseBody
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("\"", "'");

        if (compact.Length > 220)
        {
            compact = compact.Substring(0, 220) + "...";
        }

        return compact;
    }

    private string ExtractVisibleAssistantMessage(string rawMessage)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
        {
            return string.Empty;
        }

        StringBuilder visibleBuilder = new StringBuilder(rawMessage.Length);
        int index = 0;

        while (index < rawMessage.Length)
        {
            int thinkStart = rawMessage.IndexOf("<think>", index, System.StringComparison.OrdinalIgnoreCase);
            if (thinkStart < 0)
            {
                visibleBuilder.Append(rawMessage.Substring(index));
                break;
            }

            if (thinkStart > index)
            {
                visibleBuilder.Append(rawMessage.Substring(index, thinkStart - index));
            }

            int thinkEnd = rawMessage.IndexOf("</think>", thinkStart, System.StringComparison.OrdinalIgnoreCase);
            if (thinkEnd < 0)
            {
                break;
            }

            index = thinkEnd + "</think>".Length;
        }

        string visibleMessage = visibleBuilder
            .ToString()
            .Replace("<think>", string.Empty)
            .Replace("</think>", string.Empty)
            .Trim();

        return NormalizeDoctorVisibleText(visibleMessage);
    }

    private string ExtractAssistantMessageFromRawJson(string jsonResponse)
    {
        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            return string.Empty;
        }

        string[] keys =
        {
            "\"content\":\"",
            "\"text\":\""
        };

        for (int i = 0; i < keys.Length; i++)
        {
            string extracted = ExtractJsonStringValue(jsonResponse, keys[i]);
            if (!string.IsNullOrWhiteSpace(extracted))
            {
                return extracted;
            }
        }

        return string.Empty;
    }

    private string ExtractJsonStringValue(string json, string keyToken)
    {
        if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(keyToken))
        {
            return string.Empty;
        }

        int searchIndex = 0;
        while (searchIndex < json.Length)
        {
            int tokenIndex = json.IndexOf(keyToken, searchIndex, System.StringComparison.Ordinal);
            if (tokenIndex < 0)
            {
                return string.Empty;
            }

            int valueStart = tokenIndex + keyToken.Length;
            StringBuilder builder = new StringBuilder();
            bool escaping = false;
            for (int i = valueStart; i < json.Length; i++)
            {
                char character = json[i];
                if (escaping)
                {
                    switch (character)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            builder.Append(character);
                            break;
                        case 'b':
                            builder.Append('\b');
                            break;
                        case 'f':
                            builder.Append('\f');
                            break;
                        case 'n':
                            builder.Append('\n');
                            break;
                        case 'r':
                            builder.Append('\r');
                            break;
                        case 't':
                            builder.Append('\t');
                            break;
                        case 'u':
                            if (i + 4 < json.Length)
                            {
                                string hex = json.Substring(i + 1, 4);
                                if (ushort.TryParse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out ushort codePoint))
                                {
                                    builder.Append((char)codePoint);
                                    i += 4;
                                }
                            }
                            break;
                        default:
                            builder.Append(character);
                            break;
                    }

                    escaping = false;
                    continue;
                }

                if (character == '\\')
                {
                    escaping = true;
                    continue;
                }

                if (character == '"')
                {
                    string candidate = builder.ToString();
                    if (!string.IsNullOrWhiteSpace(candidate))
                    {
                        return candidate;
                    }

                    searchIndex = valueStart;
                    break;
                }

                builder.Append(character);
            }

            if (searchIndex == valueStart)
            {
                searchIndex++;
            }
        }

        return string.Empty;
    }

    private string NormalizeDoctorVisibleText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string normalized = StripRichText(text)
            .Replace("&nbsp;", " ")
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Replace('•', ' ')
            .Replace('●', ' ')
            .Replace('▪', ' ')
            .Replace('■', ' ')
            .Replace('·', ' ')
            .Replace('…', '.')
            .Replace('“', '"')
            .Replace('”', '"')
            .Replace('‘', '\'')
            .Replace('’', '\'')
            .Replace('–', '-')
            .Replace('—', '-')
            .Replace('\t', ' ');

        normalized = FixCommonTurkishMojibake(normalized);
        normalized = normalized.Replace("\0", string.Empty);
        string result = BuildDoctorVisibleFallbackText(normalized);
        if (string.IsNullOrWhiteSpace(result))
        {
            return string.Empty;
        }

        if (LooksLikeLeakedReasoningText(result))
        {
            return string.Empty;
        }

        return result.Trim();
    }

    private bool LooksLikeLeakedReasoningText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string normalized = CollapseWhitespace(FixCommonTurkishMojibake(text)).ToLowerInvariant();
        string[] strongSignals =
        {
            "the user is asking",
            "i need to",
            "i should",
            "let me",
            "follow specific rules",
            "write in turkish",
            "the scenario is about",
            "my response should",
            "provide a short, practical",
            "internal reasoning",
            "hidden analysis",
            "thought:",
            "analysis:",
            "step by step"
        };

        int signalCount = 0;
        for (int i = 0; i < strongSignals.Length; i++)
        {
            if (normalized.Contains(strongSignals[i]))
            {
                signalCount++;
            }
        }

        if (signalCount >= 2)
        {
            return true;
        }

        bool startsLikeInnerMonologue =
            normalized.StartsWith("the user ") ||
            normalized.StartsWith("i need ") ||
            normalized.StartsWith("i should ") ||
            normalized.StartsWith("let me ");

        if (!startsLikeInnerMonologue)
        {
            return false;
        }

        return !LooksLikeTurkishMedicalGuidance(normalized);
    }

    private bool LooksLikeTurkishMedicalGuidance(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (text.IndexOfAny(new[] { 'ç', 'ğ', 'ı', 'İ', 'ö', 'ş', 'ü' }) >= 0)
        {
            return true;
        }

        string[] TurkishMedicalSignals =
        {
            " hasta ",
            " bulgu ",
            " simdi ",
            " solunum ",
            " nabiz ",
            " bilinc ",
            " hava yolu ",
            " acil ",
            " oncelik ",
            " bak ",
            " kontrol et"
        };

        string padded = " " + text + " ";
        for (int i = 0; i < TurkishMedicalSignals.Length; i++)
        {
            if (padded.Contains(TurkishMedicalSignals[i]))
            {
                return true;
            }
        }

        return false;
    }

    private string BuildDoctorVisibleFallbackText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string[] lines = text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        StringBuilder builder = new StringBuilder(text.Length);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = CollapseWhitespace(lines[i]);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            while (line.StartsWith("-") || line.StartsWith("*") || line.StartsWith("#") || line.StartsWith(">"))
            {
                line = line.Substring(1).TrimStart();
            }

            line = StripLeadingDoctorLabels(line);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(line);
        }

        string fallback = builder.Length > 0 ? builder.ToString() : text;
        fallback = StripLeadingDoctorLabels(fallback);
        fallback = fallback.Replace("\n", " ");
        return CollapseWhitespace(fallback);
    }

    private string FixCommonTurkishMojibake(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return text
            .Replace("Ã§", "ç")
            .Replace("Ã‡", "Ç")
            .Replace("Ã¶", "ö")
            .Replace("Ã–", "Ö")
            .Replace("Ã¼", "ü")
            .Replace("Ãœ", "Ü")
            .Replace("ÄŸ", "ğ")
            .Replace("Äž", "Ğ")
            .Replace("Ä±", "ı")
            .Replace("Ä°", "İ")
            .Replace("ÅŸ", "ş")
            .Replace("Åž", "Ş")
            .Replace("Â", string.Empty);
    }

    private string RemoveUnsupportedDoctorCharacters(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];
            if (char.IsLetterOrDigit(character) || char.IsWhiteSpace(character))
            {
                builder.Append(character);
                continue;
            }

            switch (character)
            {
                case '.':
                case ',':
                case '!':
                case '?':
                case ':':
                case ';':
                case '(':
                case ')':
                case '-':
                case '/':
                case '\'':
                case '"':
                case '%':
                    builder.Append(character);
                    break;
            }
        }

        return builder.ToString();
    }

    private string StripLeadingDoctorLabels(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string trimmed = text.Trim();
        string[] prefixes =
        {
            "Doktor:",
            "Doktor -",
            "Cevap:",
            "Yanit:",
            "Yanıt:",
            "Tanı:",
            "Tani:",
            "Öneri:",
            "Oneri:"
        };

        for (int i = 0; i < prefixes.Length; i++)
        {
            if (trimmed.StartsWith(prefixes[i], System.StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Substring(prefixes[i].Length).Trim();
            }
        }

        return trimmed;
    }

    private List<string> SplitIntoDoctorSentences(string text)
    {
        List<string> sentences = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return sentences;
        }

        StringBuilder builder = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];
            builder.Append(character);
            if (character == '.' || character == '!' || character == '?')
            {
                string sentence = builder.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(sentence))
                {
                    sentences.Add(sentence);
                }

                builder.Length = 0;
            }
        }

        if (builder.Length > 0)
        {
            string sentence = builder.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(sentence))
            {
                sentences.Add(sentence);
            }
        }

        return sentences;
    }

    private string EnsureSentenceEndsCleanly(string text)
    {
        string trimmed = CollapseWhitespace(text);
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        while (trimmed.EndsWith(",") || trimmed.EndsWith(":") || trimmed.EndsWith(";") || trimmed.EndsWith("-"))
        {
            trimmed = trimmed.Substring(0, trimmed.Length - 1).TrimEnd();
        }

        if (!trimmed.EndsWith(".") && !trimmed.EndsWith("!") && !trimmed.EndsWith("?"))
        {
            trimmed += ".";
        }

        return trimmed;
    }

    private string CollapseWhitespace(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(text.Length);
        bool previousWasWhitespace = false;
        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];
            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            builder.Append(character);
            previousWasWhitespace = false;
        }

        return builder.ToString().Trim();
    }

    private void RefreshChatLayout(bool scrollToBottom)
    {
        if (chatHistoryText == null || chatScrollRect == null || chatContentRect == null || chatViewportRect == null)
        {
            return;
        }

        RectTransform chatRect = chatHistoryText.rectTransform;
        float viewportWidth = chatViewportRect.rect.width;
        float targetWidth = viewportWidth > 60f
            ? viewportWidth - AIChatCanvasLayout.ChatSidePadding
            : 780f;

        chatHistoryText.ForceMeshUpdate();
        Vector2 preferredSize = chatHistoryText.GetPreferredValues(chatHistoryText.text, targetWidth, 0f);
        float textHeight = Mathf.Max(AIChatCanvasLayout.ChatMinimumHeight, preferredSize.y + 28f);
        float contentHeight = Mathf.Max(textHeight + AIChatCanvasLayout.ChatBottomPadding, chatViewportRect.rect.height);

        chatRect.anchorMin = new Vector2(0f, 1f);
        chatRect.anchorMax = new Vector2(1f, 1f);
        chatRect.pivot = new Vector2(0.5f, 1f);
        chatRect.anchoredPosition = new Vector2(0f, -AIChatCanvasLayout.ChatTopPadding);
        chatRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        chatRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);

        chatContentRect.anchorMin = new Vector2(0f, 1f);
        chatContentRect.anchorMax = new Vector2(1f, 1f);
        chatContentRect.pivot = new Vector2(0.5f, 1f);
        chatContentRect.anchoredPosition = Vector2.zero;
        chatContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, chatViewportRect.rect.width);
        chatContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);

        LayoutRebuilder.ForceRebuildLayoutImmediate(chatRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContentRect);
        Canvas.ForceUpdateCanvases();

        if (scrollToBottom)
        {
            chatScrollRect.StopMovement();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private IEnumerator FocusInputFieldNextFrame()
    {
        if (userInputField == null)
        {
            yield break;
        }

        yield return null;
        yield return null;

        if (userInputField == null ||
            !IsCanvasOpen() ||
            activeDoctorPanelTab != DoctorPanelTab.AIChat ||
            !userInputField.gameObject.activeInHierarchy)
        {
            yield break;
        }

        Canvas.ForceUpdateCanvases();
        userInputField.ForceLabelUpdate();

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(userInputField.gameObject);
        }

        userInputField.Select();
        userInputField.ActivateInputField();
        if (userInputField.isFocused)
        {
            userInputField.MoveTextEnd(false);
        }

        RefreshChatTextEntryLockState();
    }

    private void HideCanvasOnStartup()
    {
        if (aiCanvas != null)
        {
            aiCanvas.SetActive(false);
        }
    }

    private XROrigin ResolveXROrigin()
    {
        if (xrOrigin != null)
        {
            return xrOrigin;
        }

        xrOrigin = XRCameraHelper.GetXROrigin();
        return xrOrigin;
    }

    private void TryAssignWorldCamera()
    {
        if (canvasComponent == null)
        {
            return;
        }

        XROrigin resolvedOrigin = ResolveXROrigin();
        if (resolvedOrigin != null && resolvedOrigin.Camera != null)
        {
            canvasComponent.worldCamera = resolvedOrigin.Camera;
        }
    }

    private Transform ResolvePlayerSpawnMount(Transform cameraTransform)
    {
        if (playerSpawnMount != null)
        {
            return playerSpawnMount;
        }

        playerSpawnMount = EnsureCameraMount(cameraTransform);
        return playerSpawnMount;
    }

    private Transform EnsureCameraMount(Transform cameraTransform)
    {
        Transform mount = cameraTransform.Find(PlayerMountName);
        if (mount == null)
        {
            GameObject mountObject = new GameObject(PlayerMountName);
            mount = mountObject.transform;
            mount.SetParent(cameraTransform, false);
        }

        mount.localPosition = PlayerMountLocalPosition;
        mount.localRotation = PlayerMountLocalRotation;
        mount.localScale = Vector3.one;

        return mount;
    }

    private Transform EnsureRuntimeOpenAnchor()
    {
        if (runtimeAnchorHolder == null)
        {
            GameObject holder = GameObject.Find(RuntimeAnchorHolderName);
            if (holder == null)
            {
                holder = new GameObject(RuntimeAnchorHolderName);
            }

            runtimeAnchorHolder = holder.transform;
        }

        if (runtimeOpenAnchor == null)
        {
            Transform existingAnchor = runtimeAnchorHolder.Find(RuntimeOpenAnchorName);
            if (existingAnchor != null)
            {
                runtimeOpenAnchor = existingAnchor;
            }
            else
            {
                GameObject anchorObject = new GameObject(RuntimeOpenAnchorName);
                runtimeOpenAnchor = anchorObject.transform;
                runtimeOpenAnchor.SetParent(runtimeAnchorHolder, false);
            }
        }

        return runtimeOpenAnchor;
    }

    private void LockPlayerLocomotion()
    {
        if (temporarilyDisabledLocomotionBehaviours.Count > 0)
        {
            return;
        }

        XROrigin resolvedOrigin = ResolveXROrigin();
        if (resolvedOrigin == null)
        {
            return;
        }

        DisableLocomotionBehaviours(resolvedOrigin.GetComponentsInChildren<ContinuousMoveProviderBase>(true));
        DisableLocomotionBehaviours(resolvedOrigin.GetComponentsInChildren<ContinuousTurnProviderBase>(true));
        DisableLocomotionBehaviours(resolvedOrigin.GetComponentsInChildren<SnapTurnProviderBase>(true));
        DisableLocomotionBehaviours(resolvedOrigin.GetComponentsInChildren<TeleportationProvider>(true));
    }

    private void RefreshChatTextEntryLockState()
    {
#if ENABLE_INPUT_SYSTEM
        bool shouldLock = IsCanvasOpen() && IsChatTextEntryFocused();
        if (shouldLock)
        {
            ApplyChatTextEntryLock();
        }
        else
        {
            ReleaseChatTextEntryLock();
        }
#endif
    }

    private bool IsCanvasOpen()
    {
        return aiCanvas != null && aiCanvas.activeInHierarchy;
    }

    private bool IsChatTextEntryFocused()
    {
        if (userInputField != null && userInputField.isFocused)
        {
            return true;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
        {
            return false;
        }

        TMP_InputField selectedInputField = eventSystem.currentSelectedGameObject.GetComponentInParent<TMP_InputField>();
        return selectedInputField != null && (userInputField == null || selectedInputField == userInputField);
    }

    private void ApplyChatTextEntryLock()
    {
#if ENABLE_INPUT_SYSTEM
        if (chatTextEntryLockActive)
        {
            return;
        }

        XRDeviceSimulator simulator = ResolveXRDeviceSimulator();
        if (simulator != null)
        {
            DisableChatTextEntryActions(simulator.deviceSimulatorActionAsset, SimulatorKeyboardActionNames);
            DisableChatTextEntryActions(simulator.controllerActionAsset, ControllerKeyboardActionNames);
        }

        if (vrUiClickHelper != null)
        {
            vrUiClickHelper.SetKeyboardFallbackBlocked(true);
        }

        chatTextEntryLockActive = true;
#endif
    }

    private void ReleaseChatTextEntryLock()
    {
#if ENABLE_INPUT_SYSTEM
        if (!chatTextEntryLockActive && temporarilyDisabledTextEntryActions.Count == 0)
        {
            EnsureChatTextEntryActionsEnabled();

            if (vrUiClickHelper != null)
            {
                vrUiClickHelper.SetKeyboardFallbackBlocked(false);
            }

            return;
        }

        foreach (InputAction action in temporarilyDisabledTextEntryActions)
        {
            if (action != null)
            {
                action.Enable();
            }
        }

        temporarilyDisabledTextEntryActions.Clear();
        chatTextEntryLockActive = false;
        EnsureChatTextEntryActionsEnabled();

        if (vrUiClickHelper != null)
        {
            vrUiClickHelper.SetKeyboardFallbackBlocked(false);
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void EnsureChatTextEntryActionsEnabled()
    {
        XRDeviceSimulator simulator = ResolveXRDeviceSimulator();
        if (simulator == null)
        {
            return;
        }

        EnableChatTextEntryActions(simulator.deviceSimulatorActionAsset, SimulatorKeyboardActionNames);
        EnableChatTextEntryActions(simulator.controllerActionAsset, ControllerKeyboardActionNames);
    }
#endif

#if ENABLE_INPUT_SYSTEM
    private void DisableChatTextEntryActions(InputActionAsset actionAsset, IEnumerable<string> actionNames)
    {
        if (actionAsset == null || actionNames == null)
        {
            return;
        }

        foreach (string actionName in actionNames)
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                continue;
            }

            InputAction action = actionAsset.FindAction(actionName, false);
            if (action == null || !action.enabled || temporarilyDisabledTextEntryActions.Contains(action))
            {
                continue;
            }

            temporarilyDisabledTextEntryActions.Add(action);
            action.Disable();
        }
    }

    private static void EnableChatTextEntryActions(InputActionAsset actionAsset, IEnumerable<string> actionNames)
    {
        if (actionAsset == null || actionNames == null)
        {
            return;
        }

        foreach (string actionName in actionNames)
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                continue;
            }

            if (string.Equals(actionName, "Cycle Devices", System.StringComparison.Ordinal))
            {
                continue;
            }

            InputAction action = actionAsset.FindAction(actionName, false);
            if (action != null && !action.enabled)
            {
                action.Enable();
            }
        }
    }
#endif

    private void UnlockPlayerLocomotion()
    {
        if (temporarilyDisabledLocomotionBehaviours.Count == 0)
        {
            return;
        }

        foreach (Behaviour behaviour in temporarilyDisabledLocomotionBehaviours)
        {
            if (behaviour != null)
            {
                behaviour.enabled = true;
            }
        }

        temporarilyDisabledLocomotionBehaviours.Clear();
    }

    private void DisableLocomotionBehaviours<TBehaviour>(TBehaviour[] behaviours)
        where TBehaviour : Behaviour
    {
        foreach (TBehaviour behaviour in behaviours)
        {
            if (behaviour == null || !behaviour.enabled || temporarilyDisabledLocomotionBehaviours.Contains(behaviour))
            {
                continue;
            }

            temporarilyDisabledLocomotionBehaviours.Add(behaviour);
            behaviour.enabled = false;
        }
    }

    private void ApplyControllerVisualOffsets()
    {
        XROrigin resolvedOrigin = ResolveXROrigin();
        if (resolvedOrigin == null)
        {
            return;
        }

        ApplyControllerVisualOffset(FindChildTransformByName(resolvedOrigin.transform, LeftControllerVisualName));
        ApplyControllerVisualOffset(FindChildTransformByName(resolvedOrigin.transform, RightControllerVisualName));
    }

    private void ApplyControllerVisualOffset(Transform controllerVisual)
    {
        if (controllerVisual == null)
        {
            return;
        }

        if (!controllerVisualOriginalPositions.ContainsKey(controllerVisual))
        {
            controllerVisualOriginalPositions[controllerVisual] = controllerVisual.localPosition;
        }

        controllerVisual.localPosition = controllerVisualOriginalPositions[controllerVisual] + ControllerVisualOpenOffset;
    }

    private void RestoreControllerVisualPositions()
    {
        if (controllerVisualOriginalPositions.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<Transform, Vector3> pair in controllerVisualOriginalPositions)
        {
            if (pair.Key != null)
            {
                pair.Key.localPosition = pair.Value;
            }
        }

        controllerVisualOriginalPositions.Clear();
    }

    private Transform FindChildTransformByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == targetName)
            {
                return child;
            }
        }

        return null;
    }

    private T FindCanvasComponentByPaths<T>(params string[] relativePaths) where T : Component
    {
        Transform target = FindCanvasTransformByPaths(relativePaths);
        return target != null ? target.GetComponent<T>() : null;
    }

    private Transform FindCanvasTransformByPaths(params string[] relativePaths)
    {
        return aiCanvas != null ? FindTransformByPaths(aiCanvas.transform, relativePaths) : null;
    }

    private static Transform FindTransformByPaths(Transform root, params string[] relativePaths)
    {
        if (root == null || relativePaths == null)
        {
            return null;
        }

        for (int i = 0; i < relativePaths.Length; i++)
        {
            string relativePath = relativePaths[i];
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                continue;
            }

            Transform target = root.Find(relativePath);
            if (target != null)
            {
                return target;
            }
        }

        return null;
    }

    private XRDeviceSimulator ResolveXRDeviceSimulator()
    {
        if (xrDeviceSimulator != null)
        {
            return xrDeviceSimulator;
        }

        xrDeviceSimulator = FindObjectOfType<XRDeviceSimulator>(true);
        return xrDeviceSimulator;
    }

    private void EnsureCloseButtonExists()
    {
        if (aiCanvas == null)
        {
            return;
        }

        Transform existingButton = FindCanvasTransformByPaths(
            AIChatCanvasLayout.MainPanelName + "/" + AIChatCanvasLayout.CloseButtonName);
        if (existingButton != null)
        {
            closeButton = existingButton.GetComponent<Button>();
        }

        Transform mainPanel = FindCanvasTransformByPaths(AIChatCanvasLayout.MainPanelName);
        if (mainPanel == null)
        {
            return;
        }

        if (closeButton == null)
        {
            GameObject closeButtonObject = new GameObject(CloseButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            closeButtonObject.transform.SetParent(mainPanel, false);
            closeButton = closeButtonObject.GetComponent<Button>();
        }

        NormalizeCloseButton(mainPanel);
        closeButton.onClick.RemoveListener(CloseAICanvas);
        closeButton.onClick.AddListener(CloseAICanvas);
    }

    private void NormalizeCloseButton(Transform mainPanel)
    {
        if (closeButton == null || mainPanel == null)
        {
            return;
        }

        Transform closeTransform = closeButton.transform;
        if (closeTransform.parent != mainPanel)
        {
            closeTransform.SetParent(mainPanel, false);
        }

        closeTransform.SetAsLastSibling();

        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.sizeDelta = AIChatCanvasLayout.CloseButtonSize;
        closeRect.anchoredPosition = AIChatCanvasLayout.CloseButtonPosition;
        closeRect.localScale = Vector3.one;

        Image closeImage = closeButton.GetComponent<Image>();
        if (closeImage != null)
        {
            closeImage.color = AIChatCanvasLayout.CloseButtonColor;
            closeImage.raycastTarget = true;
            closeButton.targetGraphic = closeImage;
        }

        closeButton.transition = Selectable.Transition.ColorTint;
        closeButton.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = closeButton.colors;
        colors.normalColor = AIChatCanvasLayout.CloseButtonColor;
        colors.highlightedColor = AIChatCanvasLayout.CloseButtonHoverColor;
        colors.pressedColor = new Color(0.58f, 0.1f, 0.1f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        colors.colorMultiplier = 1f;
        closeButton.colors = colors;

        Transform labelTransform = closeTransform.Find(CloseButtonTextName);
        TextMeshProUGUI label;
        if (labelTransform == null)
        {
            GameObject labelObject = new GameObject(CloseButtonTextName, typeof(RectTransform));
            labelObject.transform.SetParent(closeTransform, false);
            label = labelObject.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            label = labelTransform.GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                label = labelTransform.gameObject.AddComponent<TextMeshProUGUI>();
            }
        }

        label.text = "X";
        label.fontSize = AIChatCanvasLayout.CloseButtonFontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;

        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.localScale = Vector3.one;
    }
}

#region JSON DTO Siniflari
[System.Serializable]
public class OpenAIMessage
{
    public string role;
    public string content;
}

[System.Serializable]
public class OpenAIRequest
{
    public string model;
    public List<OpenAIMessage> messages;
    public float temperature;
    public int max_tokens;
}

[System.Serializable]
public class OpenAIResponse
{
    public string id;
    public List<OpenAIChoice> choices;
}

[System.Serializable]
public class OpenAIChoice
{
    public int index;
    public OpenAIMessage message;
}

[System.Serializable]
public class MiniMaxTtsRequest
{
    public string model;
    public string text;
    public bool stream;
    public string language_boost;
    public string output_format;
    public MiniMaxTtsVoiceSetting voice_setting;
    public MiniMaxTtsAudioSetting audio_setting;
}

[System.Serializable]
public class MiniMaxTtsVoiceSetting
{
    public string voice_id;
    public float speed;
    public int vol;
    public int pitch;
}

[System.Serializable]
public class MiniMaxTtsAudioSetting
{
    public int sample_rate;
    public int bitrate;
    public string format;
    public int channel;
}

[System.Serializable]
public class MiniMaxTtsResponse
{
    public MiniMaxTtsData data;
    public MiniMaxTtsExtraInfo extra_info;
    public string trace_id;
    public MiniMaxTtsBaseResponse base_resp;
}

[System.Serializable]
public class MiniMaxTtsData
{
    public string audio;
    public int status;
}

[System.Serializable]
public class MiniMaxTtsExtraInfo
{
    public int audio_length;
    public int audio_sample_rate;
    public int audio_size;
    public int bitrate;
    public int usage_characters;
    public string audio_format;
    public int audio_channel;
}

[System.Serializable]
public class MiniMaxTtsBaseResponse
{
    public int status_code;
    public string status_msg;
}

[System.Serializable]
public class MiniMaxVoiceQueryRequest
{
    public string voice_type;
}

[System.Serializable]
public class MiniMaxVoiceQueryResponse
{
    public List<MiniMaxVoiceDescriptor> system_voice;
    public MiniMaxTtsBaseResponse base_resp;
}

[System.Serializable]
public class MiniMaxVoiceDescriptor
{
    public string voice_id;
    public string voice_name;
    public List<string> description;
}

[System.Serializable]
public class ElevenLabsTtsRequest
{
    public string text;
    public string model_id;
    public ElevenLabsVoiceSettings voice_settings;
}

[System.Serializable]
public class ElevenLabsVoiceSettings
{
    public float stability;
    public float similarity_boost;
    public float style;
    public bool use_speaker_boost;
}
#endregion
