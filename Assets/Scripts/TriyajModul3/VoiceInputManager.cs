using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;

public class VoiceInputManager : MonoBehaviour
{
    private const string DefaultGroqApiUrl = "https://api.groq.com/openai/v1/audio/transcriptions";
    private const string MicButtonName = "Mic_Button";
    private const string MicButtonLabelName = "Mic_Label";
    private const float MicButtonPreferredWidth = 48f;
    private const float MicButtonPreferredHeight = 48f;
    private const float MinimumRecordingSeconds = 0.2f;
    private const float RecordingReadyTimeoutSeconds = 0.75f;
    private const int RecordingSampleRate = 16000;
    private const int BitsPerSample = 16;
    private const int MinimumRecordedSamples = RecordingSampleRate / 10;
    private const int TranscriptionTimeoutSeconds = 20;
    private const float ToggleDebounceSeconds = 0.22f;
    private static readonly Color MicButtonColor = new Color(0.08f, 0.44f, 0.66f, 0.96f);
    private static readonly Color MicButtonHoverColor = new Color(0.12f, 0.58f, 0.82f, 1f);
    private static readonly Color MicButtonPressedColor = new Color(0.04f, 0.34f, 0.52f, 1f);
    private static readonly Color MicButtonDisabledColor = new Color(0.28f, 0.32f, 0.36f, 0.6f);
    private static readonly Color MicButtonUploadingColor = new Color(0.95f, 0.62f, 0.19f, 0.96f);

    [SerializeField] private TMP_InputField targetInputField;
    [SerializeField] private Button micButton;
    [SerializeField] private Image micIconOutline;
    [SerializeField] private string groqApiKey = string.Empty;
    [SerializeField] private string groqApiUrl = DefaultGroqApiUrl;
    [SerializeField] private string groqModel = "whisper-large-v3-turbo";
    [SerializeField] private string groqLanguage = "tr";
    [SerializeField] private int maxRecordingSeconds = 8;

    private bool isRecording;
    private bool isUploading;
    private string committedText = string.Empty;
    private string defaultMicLabelText = "Mic";
    private string defaultPlaceholderText = string.Empty;
    private string speechStatusMessage = string.Empty;
    private string recordingDeviceName;
    private float recordingStartTime;
    private AudioClip recordingClip;
    private Coroutine autoStopRoutine;
    private Coroutine recordingReadyRoutine;
    private Coroutine transcriptionRoutine;
    private UnityWebRequest activeTranscriptionRequest;
    private Color normalButtonColor = Color.white;
    private readonly Color recordingButtonColor = new Color(0.88f, 0.22f, 0.22f, 1f);
    private TextMeshProUGUI micLabel;
    private TextMeshProUGUI placeholderText;
    private VRKeyboardManager keyboardManager;
    private bool recordingDeviceReady;
    private float lastToggleActionTime = -10f;
    [System.Serializable]
    private class GroqTranscriptionResponse
    {
        public string text = string.Empty;
    }

    private void Awake()
    {
        EnsureReferences();
        CacheButtonVisualState();
        ApplySpeechAvailabilityState();
        ValidateReferences();
    }

    private void OnEnable()
    {
        EnsureReferences();
        CacheButtonVisualState();
        ApplySpeechAvailabilityState();
        ValidateReferences();
        BindButtonListener();
    }

    private void OnDisable()
    {
        CancelActiveTranscription();
        CancelRecording(false);
        UnbindButtonListener();
        ApplySpeechAvailabilityState();
    }

    private void OnDestroy()
    {
        CancelActiveTranscription();
        CancelRecording(false);
        UnbindButtonListener();
    }

    public void ConfigureGroqTranscription(string apiKey, string apiUrl, string model, string language, int recordingSeconds)
    {
        groqApiKey = string.IsNullOrWhiteSpace(apiKey) ? string.Empty : apiKey.Trim();
        groqApiUrl = ResolveGroqApiUrl(apiUrl);

        if (!string.IsNullOrWhiteSpace(model))
        {
            groqModel = model.Trim();
        }

        if (!string.IsNullOrWhiteSpace(language))
        {
            groqLanguage = language.Trim();
        }

        maxRecordingSeconds = Mathf.Max(1, recordingSeconds);
        ApplySpeechAvailabilityState();
    }

    private void ToggleRecording()
    {
        float now = Time.unscaledTime;
        if (now - lastToggleActionTime < ToggleDebounceSeconds)
        {
            return;
        }

        lastToggleActionTime = now;
        EnsureReferences();
        if (targetInputField == null || micButton == null)
        {
            ValidateReferences();
            return;
        }

        if (isUploading)
        {
            ShowSpeechUnavailableState("Kayit durdu, yaziya cevriliyor...");
            KeepKeyboardVisible();
            return;
        }

        if (isRecording)
        {
            StopRecordingAndTranscribe();
            return;
        }

        StartRecording();
    }

    private void StartRecording()
    {
        ClearSpeechStatusMessage();

        if (string.IsNullOrWhiteSpace(groqApiKey))
        {
            ShowSpeechUnavailableState("Groq API key girilmemis.");
            KeepKeyboardVisible();
            return;
        }

        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            ShowSpeechUnavailableState("Mikrofon bulunamadi.");
            KeepKeyboardVisible();
            return;
        }

        CancelRecording(false);
        PrepareInputFieldForProgrammaticUpdate();

        recordingDeviceName = ResolveRecordingDeviceName();
        recordingClip = Microphone.Start(recordingDeviceName, false, Mathf.Max(1, maxRecordingSeconds), RecordingSampleRate);
        if (recordingClip == null)
        {
            ShowSpeechUnavailableState("Kayit baslatilamadi.");
            KeepKeyboardVisible();
            return;
        }

        recordingStartTime = Time.realtimeSinceStartup;
        recordingDeviceReady = false;
        committedText = targetInputField.text != null ? targetInputField.text.TrimEnd() : string.Empty;
        isRecording = true;
        ShowSpeechUnavailableState("Dinleniyor... Bitince tekrar Mic'e bas.");
        UpdateMicVisualState();
        KeepKeyboardVisible();

        if (autoStopRoutine != null)
        {
            StopCoroutine(autoStopRoutine);
        }

        autoStopRoutine = StartCoroutine(AutoStopRecording());

        if (recordingReadyRoutine != null)
        {
            StopCoroutine(recordingReadyRoutine);
        }

        recordingReadyRoutine = StartCoroutine(WaitForRecordingDeviceReady());
    }

    private IEnumerator AutoStopRecording()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(1, maxRecordingSeconds));
        if (isRecording)
        {
            StopRecordingAndTranscribe();
        }
    }

    private void StopRecordingAndTranscribe()
    {
        if (!isRecording)
        {
            return;
        }

        if (autoStopRoutine != null)
        {
            StopCoroutine(autoStopRoutine);
            autoStopRoutine = null;
        }

        int recordedSamples = GetRecordedSampleCount();
        float recordedDuration = Mathf.Max(0f, Time.realtimeSinceStartup - recordingStartTime);
        if (IsMicrophoneRecording())
        {
            Microphone.End(recordingDeviceName);
        }

        isRecording = false;
        StopRecordingReadyRoutine();

        if (recordingClip == null || !HasEnoughRecordedAudio(recordedSamples, recordedDuration))
        {
            DisposeRecordingClip();
            UpdateMicVisualState();
            ShowSpeechUnavailableState("Kayit cok kisa oldu. Biraz daha konusup tekrar dene.");
            KeepKeyboardVisible();
            return;
        }

        byte[] wavBytes = EncodeClipToWav(recordingClip, recordedSamples);
        DisposeRecordingClip();
        if (wavBytes == null || wavBytes.Length == 0)
        {
            UpdateMicVisualState();
            ShowSpeechUnavailableState("Ses dosyasi olusturulamadi.");
            KeepKeyboardVisible();
            return;
        }

        isUploading = true;
        ShowSpeechUnavailableState("Kayit durdu, yaziya cevriliyor...");
        UpdateMicVisualState();
        KeepKeyboardVisible();

        if (transcriptionRoutine != null)
        {
            StopCoroutine(transcriptionRoutine);
        }

        transcriptionRoutine = StartCoroutine(PostGroqTranscriptionRequest(wavBytes));
    }

    private IEnumerator PostGroqTranscriptionRequest(byte[] wavBytes)
    {
        string requestUrl = ResolveGroqApiUrl(groqApiUrl);
        List<IMultipartFormSection> formSections = new List<IMultipartFormSection>
        {
            new MultipartFormFileSection("file", wavBytes, "doctor_chat_mic.wav", "audio/wav"),
            new MultipartFormDataSection("model", groqModel),
            new MultipartFormDataSection("language", groqLanguage)
        };

        UnityWebRequest request = null;
        try
        {
            request = UnityWebRequest.Post(requestUrl, formSections);
            request.timeout = TranscriptionTimeoutSeconds;
            request.SetRequestHeader("Authorization", "Bearer " + groqApiKey);
            request.SetRequestHeader("Accept", "application/json");
            activeTranscriptionRequest = request;

            yield return request.SendWebRequest();

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            bool requestFailed = request.result != UnityWebRequest.Result.Success || request.responseCode >= 400;
            if (requestFailed)
            {
                Debug.LogWarning("[VoiceInputManager] Groq transcription hatasi | Code: " + request.responseCode +
                                 " | Error: " + request.error + " | Response: " + responseText, this);
                FinalizeTranscriptionFailure(BuildGroqErrorMessage(request.responseCode, request.error, responseText));
                yield break;
            }

            GroqTranscriptionResponse response = null;
            if (!string.IsNullOrWhiteSpace(responseText))
            {
                response = JsonUtility.FromJson<GroqTranscriptionResponse>(responseText);
            }

            string transcript = response != null && !string.IsNullOrWhiteSpace(response.text)
                ? SanitizeTranscriptText(response.text)
                : string.Empty;

            if (string.IsNullOrWhiteSpace(transcript))
            {
                Debug.LogWarning("[VoiceInputManager] Groq transcription bos dondu. Response: " + responseText, this);
                FinalizeTranscriptionFailure("Ses yaziya cevrilemedi. Tekrar dene.");
                yield break;
            }

            yield return StartCoroutine(ApplyTranscriptToInputAsync(transcript));
            FinalizeTranscriptionSuccess();
        }
        finally
        {
            if (request != null)
            {
                request.Dispose();
            }

            activeTranscriptionRequest = null;
            transcriptionRoutine = null;
        }
    }

    private void FinalizeTranscriptionSuccess()
    {
        isUploading = false;
        ClearSpeechStatusMessage();
        UpdateMicVisualState();
        KeepKeyboardVisible();
    }

    private void FinalizeTranscriptionFailure(string message)
    {
        isUploading = false;
        UpdateMicVisualState();
        ShowSpeechUnavailableState(message);
        KeepKeyboardVisible();
    }

    private void CancelRecording(bool resetStatus)
    {
        if (autoStopRoutine != null)
        {
            StopCoroutine(autoStopRoutine);
            autoStopRoutine = null;
        }

        StopRecordingReadyRoutine();

        if (IsMicrophoneRecording())
        {
            try
            {
                Microphone.End(recordingDeviceName);
            }
            catch
            {
            }
        }

        isRecording = false;
        recordingStartTime = 0f;
        recordingDeviceReady = false;
        DisposeRecordingClip();
        UpdateMicVisualState();

        if (resetStatus)
        {
            ClearSpeechStatusMessage();
        }
    }

    private void CancelActiveTranscription()
    {
        if (transcriptionRoutine != null)
        {
            StopCoroutine(transcriptionRoutine);
            transcriptionRoutine = null;
        }

        if (activeTranscriptionRequest != null)
        {
            try
            {
                activeTranscriptionRequest.Abort();
            }
            catch
            {
            }

            activeTranscriptionRequest.Dispose();
            activeTranscriptionRequest = null;
        }

        isUploading = false;
        UpdateMicVisualState();
    }

    private IEnumerator ApplyTranscriptToInputAsync(string transcript)
    {
        string safeTranscript = SanitizeTranscriptText(transcript);
        if (string.IsNullOrWhiteSpace(safeTranscript))
        {
            yield break;
        }

        string existingText = targetInputField != null && targetInputField.text != null
            ? targetInputField.text.TrimEnd()
            : string.Empty;

        string nextText = string.IsNullOrWhiteSpace(existingText)
            ? safeTranscript
            : existingText + " " + safeTranscript;

        PrepareInputFieldForProgrammaticUpdate();
        yield return null;
        yield return null;
        ApplyInputPreviewText(nextText);
    }

    private void ApplyInputPreviewText(string text)
    {
        if (targetInputField == null)
        {
            return;
        }

        string safeText = text ?? string.Empty;
        targetInputField.text = safeText;
        targetInputField.ForceLabelUpdate();
        Canvas.ForceUpdateCanvases();
        targetInputField.ReleaseSelection();
        targetInputField.ForceLabelUpdate();
        Canvas.ForceUpdateCanvases();
        UpdatePlaceholderVisual();
    }

    private void PrepareInputFieldForProgrammaticUpdate()
    {
        if (targetInputField == null)
        {
            return;
        }

        if (keyboardManager == null)
        {
            keyboardManager = GetComponent<VRKeyboardManager>();
        }

        if (keyboardManager != null)
        {
            keyboardManager.ReleaseInputFocusForExternalUpdate();
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
        Canvas.ForceUpdateCanvases();
    }

    private string SanitizeTranscriptText(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return string.Empty;
        }

        string normalized = transcript.Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace('\t', ' ')
            .Normalize(NormalizationForm.FormKC);

        StringBuilder builder = new StringBuilder(normalized.Length);
        bool previousWasWhitespace = false;

        for (int i = 0; i < normalized.Length; i++)
        {
            char character = normalized[i];
            if (character == '\uFFFD' || char.IsSurrogate(character))
            {
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (previousWasWhitespace)
                {
                    continue;
                }

                builder.Append(' ');
                previousWasWhitespace = true;
                continue;
            }

            if (char.IsControl(character))
            {
                continue;
            }

            if (character > 0x00FF &&
                character != '\u011e' && character != '\u011f' &&
                character != '\u0130' && character != '\u0131' &&
                character != '\u015e' && character != '\u015f')
            {
                continue;
            }

            builder.Append(character);
            previousWasWhitespace = false;
        }

        return builder.ToString().Trim();
    }

    private string ResolveRecordingDeviceName()
    {
        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            return null;
        }

        return Microphone.devices[0];
    }

    private bool IsMicrophoneRecording()
    {
        if (string.IsNullOrWhiteSpace(recordingDeviceName))
        {
            return Microphone.IsRecording(null);
        }

        return Microphone.IsRecording(recordingDeviceName);
    }

    private int GetRecordedSampleCount()
    {
        if (recordingClip == null)
        {
            return 0;
        }

        int recordedSamples = 0;
        try
        {
            recordedSamples = Microphone.GetPosition(recordingDeviceName);
        }
        catch
        {
        }

        if (recordedSamples <= 0 && recordingDeviceReady)
        {
            float elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - recordingStartTime);
            recordedSamples = Mathf.RoundToInt(elapsed * recordingClip.frequency);
        }

        return Mathf.Clamp(recordedSamples, 0, recordingClip.samples);
    }

    private byte[] EncodeClipToWav(AudioClip clip, int sampleCount)
    {
        if (clip == null || sampleCount <= 0)
        {
            return null;
        }

        int channels = Mathf.Max(1, clip.channels);
        int clampedSampleCount = Mathf.Clamp(sampleCount, 0, clip.samples);
        if (clampedSampleCount <= 0)
        {
            return null;
        }

        float[] samples = new float[clampedSampleCount * channels];
        if (!clip.GetData(samples, 0))
        {
            return null;
        }

        int dataSize = samples.Length * 2;
        using (MemoryStream stream = new MemoryStream(44 + dataSize))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(clip.frequency);
            writer.Write(clip.frequency * channels * BitsPerSample / 8);
            writer.Write((short)(channels * BitsPerSample / 8));
            writer.Write((short)BitsPerSample);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            for (int i = 0; i < samples.Length; i++)
            {
                float clampedSample = Mathf.Clamp(samples[i], -1f, 1f);
                short pcmValue = (short)Mathf.RoundToInt(clampedSample * short.MaxValue);
                writer.Write(pcmValue);
            }

            writer.Flush();
            return stream.ToArray();
        }
    }

    private string BuildGroqErrorMessage(long statusCode, string requestError, string responseText)
    {
        if (statusCode == 401 || statusCode == 403)
        {
            return "Groq API key gecersiz veya yetkisiz.";
        }

        if (statusCode == 429)
        {
            return "Groq limiti dolu. Biraz sonra tekrar dene.";
        }

        if (statusCode >= 500)
        {
            return "Groq su an yanit vermiyor. Tekrar dene.";
        }

        if (!string.IsNullOrWhiteSpace(requestError) &&
            (requestError.ToLowerInvariant().Contains("resolve") ||
             requestError.ToLowerInvariant().Contains("timed out") ||
             requestError.ToLowerInvariant().Contains("network")))
        {
            return "Internet baglantisini kontrol et ve tekrar dene.";
        }

        if (!string.IsNullOrWhiteSpace(responseText) && responseText.ToLowerInvariant().Contains("audio file is too short"))
        {
            return "Kayit cok kisa oldu. Biraz daha konusup tekrar dene.";
        }

        if (!string.IsNullOrWhiteSpace(responseText))
        {
            return "Ses yaziya cevrilemedi. Tekrar dene.";
        }

        return "Ses gonderilemedi. Tekrar dene.";
    }

    private void DisposeRecordingClip()
    {
        if (recordingClip == null)
        {
            return;
        }

        Destroy(recordingClip);
        recordingClip = null;
        recordingDeviceName = null;
        recordingStartTime = 0f;
        recordingDeviceReady = false;
    }

    private IEnumerator WaitForRecordingDeviceReady()
    {
        float startedAt = Time.realtimeSinceStartup;
        while (isRecording && Time.realtimeSinceStartup - startedAt < RecordingReadyTimeoutSeconds)
        {
            int currentPosition = 0;
            try
            {
                currentPosition = Microphone.GetPosition(recordingDeviceName);
            }
            catch
            {
            }

            if (currentPosition > 0)
            {
                recordingDeviceReady = true;
                recordingReadyRoutine = null;
                yield break;
            }

            yield return null;
        }

        recordingReadyRoutine = null;
    }

    private void StopRecordingReadyRoutine()
    {
        if (recordingReadyRoutine == null)
        {
            return;
        }

        StopCoroutine(recordingReadyRoutine);
        recordingReadyRoutine = null;
    }

    private bool HasEnoughRecordedAudio(int recordedSamples, float recordedDuration)
    {
        if (recordedSamples <= 0)
        {
            return false;
        }

        if (recordedDuration < MinimumRecordingSeconds)
        {
            return false;
        }

        if (!recordingDeviceReady && recordedSamples < MinimumRecordedSamples)
        {
            return false;
        }

        return recordedSamples >= MinimumRecordedSamples || recordedDuration >= 0.35f;
    }

    private string ResolveGroqApiUrl(string candidateUrl)
    {
        string trimmed = string.IsNullOrWhiteSpace(candidateUrl) ? DefaultGroqApiUrl : candidateUrl.Trim();
        if (System.Uri.TryCreate(trimmed, System.UriKind.Absolute, out System.Uri uri) &&
            !string.IsNullOrWhiteSpace(uri.Host))
        {
            return uri.ToString();
        }

        return DefaultGroqApiUrl;
    }

    private void BindButtonListener()
    {
        EnsureReferences();
        if (micButton == null)
        {
            return;
        }

        micButton.onClick.RemoveListener(ToggleRecording);
        micButton.onClick.AddListener(ToggleRecording);
    }

    private void UnbindButtonListener()
    {
        if (micButton == null)
        {
            return;
        }

        micButton.onClick.RemoveListener(ToggleRecording);
    }

    private void CacheButtonVisualState()
    {
        Image targetImage = micIconOutline != null ? micIconOutline : (micButton != null ? micButton.GetComponent<Image>() : null);
        if (targetImage != null && !isRecording && !isUploading)
        {
            normalButtonColor = targetImage.color;
        }
    }

    private void EnsureReferences()
    {
        if (targetInputField == null)
        {
            targetInputField = FindTargetInputField();
        }

        if (micButton == null)
        {
            micButton = EnsureMicButton();
        }

        if (micIconOutline == null && micButton != null)
        {
            micIconOutline = micButton.GetComponent<Image>();
        }

        if (micLabel == null && micButton != null)
        {
            Transform labelTransform = micButton.transform.Find(MicButtonLabelName);
            if (labelTransform != null)
            {
                micLabel = labelTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (placeholderText == null && targetInputField != null)
        {
            placeholderText = targetInputField.placeholder as TextMeshProUGUI;
        }

        if (keyboardManager == null)
        {
            keyboardManager = GetComponent<VRKeyboardManager>();
        }

        if (string.IsNullOrWhiteSpace(defaultPlaceholderText) && placeholderText != null)
        {
            defaultPlaceholderText = placeholderText.text;
        }

        if (micLabel != null && string.IsNullOrWhiteSpace(defaultMicLabelText))
        {
            defaultMicLabelText = string.IsNullOrWhiteSpace(micLabel.text) ? "Mic" : micLabel.text;
        }
    }

    private TMP_InputField FindTargetInputField()
    {
        TMP_InputField[] inputFields = GetComponentsInChildren<TMP_InputField>(true);
        if (inputFields == null || inputFields.Length == 0)
        {
            return null;
        }

        TMP_InputField fallback = null;
        for (int i = 0; i < inputFields.Length; i++)
        {
            TMP_InputField inputField = inputFields[i];
            if (inputField == null)
            {
                continue;
            }

            if (fallback == null)
            {
                fallback = inputField;
            }

            string lowerName = inputField.name.ToLowerInvariant();
            if (lowerName.Contains("user") || lowerName.Contains("input"))
            {
                return inputField;
            }
        }

        return fallback;
    }

    private Button EnsureMicButton()
    {
        if (targetInputField == null)
        {
            return null;
        }

        RectTransform inputContainer = targetInputField.transform.parent as RectTransform;
        if (inputContainer == null)
        {
            return null;
        }

        Transform existing = inputContainer.Find(MicButtonName);
        Button button = existing != null ? existing.GetComponent<Button>() : null;
        if (button == null)
        {
            GameObject buttonObject = new GameObject(MicButtonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.layer = inputContainer.gameObject.layer;
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.SetParent(inputContainer, false);
            buttonRect.localScale = Vector3.one;
            button = buttonObject.GetComponent<Button>();
        }

        ConfigureMicButton(button, inputContainer);
        return button;
    }

    private void ConfigureMicButton(Button button, RectTransform inputContainer)
    {
        if (button == null || inputContainer == null)
        {
            return;
        }

        button.gameObject.layer = inputContainer.gameObject.layer;

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            buttonRect.localScale = Vector3.one;
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(MicButtonPreferredWidth, MicButtonPreferredHeight);
        }

        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = button.gameObject.AddComponent<LayoutElement>();
        }

        layout.minWidth = MicButtonPreferredWidth;
        layout.preferredWidth = MicButtonPreferredWidth;
        layout.minHeight = MicButtonPreferredHeight;
        layout.preferredHeight = MicButtonPreferredHeight;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage == null)
        {
            buttonImage = button.gameObject.AddComponent<Image>();
        }

        buttonImage.color = MicButtonColor;
        buttonImage.raycastTarget = true;
        micIconOutline = buttonImage;

        button.targetGraphic = buttonImage;
        button.transition = Selectable.Transition.ColorTint;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        ColorBlock colors = button.colors;
        colors.normalColor = MicButtonColor;
        colors.highlightedColor = MicButtonHoverColor;
        colors.pressedColor = MicButtonPressedColor;
        colors.selectedColor = MicButtonHoverColor;
        colors.disabledColor = MicButtonDisabledColor;
        colors.colorMultiplier = 1f;
        button.colors = colors;

        EnsureMicButtonLabel(button);
        PositionMicButton(button.transform, inputContainer);
        UpdateMicVisualState();
    }

    private void EnsureMicButtonLabel(Button button)
    {
        if (button == null)
        {
            return;
        }

        Transform existing = button.transform.Find(MicButtonLabelName);
        TextMeshProUGUI label = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
        if (label == null)
        {
            GameObject labelObject = new GameObject(MicButtonLabelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.layer = button.gameObject.layer;
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(button.transform, false);
            labelRect.localScale = Vector3.one;
            label = labelObject.GetComponent<TextMeshProUGUI>();
        }

        RectTransform rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        label.text = string.IsNullOrWhiteSpace(defaultMicLabelText) ? "Mic" : defaultMicLabelText;
        label.fontSize = 16f;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.raycastTarget = false;

        if (targetInputField != null && targetInputField.textComponent != null && targetInputField.textComponent.font != null)
        {
            label.font = targetInputField.textComponent.font;
            label.fontSharedMaterial = targetInputField.textComponent.fontSharedMaterial;
        }

        micLabel = label;
    }

    private void PositionMicButton(Transform buttonTransform, RectTransform inputContainer)
    {
        if (buttonTransform == null || inputContainer == null)
        {
            return;
        }

        int siblingIndex = inputContainer.childCount - 1;
        for (int i = 0; i < inputContainer.childCount; i++)
        {
            Transform child = inputContainer.GetChild(i);
            if (child == null || child == buttonTransform)
            {
                continue;
            }

            if (child.name == "Send_Button" || child.name.ToLowerInvariant().Contains("send"))
            {
                siblingIndex = i;
                break;
            }
        }

        buttonTransform.SetSiblingIndex(siblingIndex);
    }

    private void ApplySpeechAvailabilityState()
    {
        EnsureReferences();
        if (micButton != null)
        {
            micButton.interactable = true;
        }

        UpdatePlaceholderVisual();
        UpdateMicVisualState();
    }

    private void UpdatePlaceholderVisual()
    {
        if (placeholderText == null)
        {
            return;
        }

        bool inputIsEmpty = targetInputField == null || string.IsNullOrWhiteSpace(targetInputField.text);
        if (inputIsEmpty && !string.IsNullOrWhiteSpace(speechStatusMessage))
        {
            placeholderText.text = speechStatusMessage;
            return;
        }

        if (!string.IsNullOrWhiteSpace(defaultPlaceholderText))
        {
            placeholderText.text = defaultPlaceholderText;
        }
    }

    private void ValidateReferences()
    {
    }

    private void KeepKeyboardVisible()
    {
        if (keyboardManager == null)
        {
            keyboardManager = GetComponent<VRKeyboardManager>();
        }

        if (keyboardManager != null)
        {
            keyboardManager.ShowKeyboard();
        }
    }

    private void UpdateMicVisualState()
    {
        Image targetImage = micIconOutline != null ? micIconOutline : (micButton != null ? micButton.GetComponent<Image>() : null);
        if (targetImage == null)
        {
            return;
        }

        if (isRecording)
        {
            targetImage.color = recordingButtonColor;
            if (micLabel != null)
            {
                micLabel.text = "Dur";
            }

            return;
        }

        if (isUploading)
        {
            targetImage.color = MicButtonUploadingColor;
            if (micLabel != null)
            {
                micLabel.text = "Bekle";
            }

            return;
        }

        targetImage.color = normalButtonColor;
        if (micLabel != null)
        {
            micLabel.text = string.IsNullOrWhiteSpace(defaultMicLabelText) ? "Mic" : defaultMicLabelText;
        }
    }

    private void ShowSpeechUnavailableState(string message)
    {
        speechStatusMessage = string.IsNullOrWhiteSpace(message)
            ? "Ses girisi baslatilamadi."
            : message.Trim();
        UpdatePlaceholderVisual();
        UpdateMicVisualState();
    }

    private void ClearSpeechStatusMessage()
    {
        if (string.IsNullOrEmpty(speechStatusMessage))
        {
            return;
        }

        speechStatusMessage = string.Empty;
        UpdatePlaceholderVisual();
    }
}
