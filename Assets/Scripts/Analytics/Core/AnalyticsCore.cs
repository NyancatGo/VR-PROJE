using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrainingAnalytics
{
    public interface IAnalyticsAdapter
    {
        string AdapterName { get; }
        bool IsOperational { get; }
        void LogEvent(string eventName, IReadOnlyDictionary<string, object> parameters);
    }

    public static class AnalyticsEventNames
    {
        public const string ModuleEntered = "modul_ac";
        public const string ModuleCompleted = "modul_bitti";
        public const string ModuleTransitionIntent = "modul_gecis";
        public const string ContentOpened = "icerik_ac";
        public const string VideoStarted = "video_baslat";
        public const string VideoProgress = "video_ilerle";
        public const string VideoCompleted = "video_bitti";
        public const string InfographicOpened = "infografik_ac";
        public const string LearningContentCompleted = "icerik_bitti";
        public const string TaskStarted = "gorev_baslat";
        public const string TaskProgress = "gorev_ilerle";
        public const string TaskFailed = "gorev_basarisiz";
        public const string TaskCompleted = "gorev_bitti";
        public const string HelpRequested = "yardim";
        public const string TriageStarted = "triage_baslat";
        public const string VictimInteracted = "hasta_dokun";
        public const string VictimTagged = "hasta_etiket";
        public const string AIPanelOpened = "ai_panel_ac";
        public const string AIQuestionAsked = "ai_soru";
        public const string QuizStarted = "test_baslat";
        public const string QuizAnswered = "cevap_ver";
        public const string QuizCompleted = "test_bitti";
        public const string ScoreRecorded = "skor_kaydet";
        public const string ScenarioStarted = "senaryo_baslat";
        public const string CriticalActionTaken = "kritik_aksiyon";
        public const string ScenarioTaskCompleted = "senaryo_gorev_bitti";
        public const string ScenarioCompleted = "senaryo_bitti";
        public const string TriageDialogOpened = "triage_dialog_ac";
    }

    public static class AnalyticsParams
    {
        public const string InstallationId = "kurulum_id";
        public const string SessionId = "oturum_id";
        public const string BuildVersion = "surum";
        public const string SceneName = "sahne";
        public const string RuntimePlatform = "platform";
        public const string ModuleId = "modul_id";
        public const string ModuleName = "modul_adi";
        public const string ScenarioId = "senaryo_id";
        public const string ScenarioName = "senaryo_adi";
        public const string ContentId = "icerik_id";
        public const string ContentName = "icerik_adi";
        public const string ContentType = "icerik_turu";
        public const string ContentGroupId = "grup_id";
        public const string ContentGroupName = "grup_adi";
        public const string OpenedCount = "acilma";
        public const string CompletedCount = "tamamlanan";
        public const string TotalCount = "toplam";
        public const string DurationSeconds = "sure";
        public const string TaskId = "gorev_id";
        public const string TaskName = "gorev_adi";
        public const string TaskType = "gorev_turu";
        public const string TaskStatus = "gorev_durum";
        public const string TaskProgress = "gorev_ilerleme";
        public const string VictimId = "hasta_id";
        public const string VictimName = "hasta_adi";
        public const string VictimIndex = "hasta_idx";
        public const string AssignedTriage = "atanan";
        public const string ActualTriage = "sonuc";
        public const string IsCorrect = "dogru";
        public const string CorrectCount = "dogru_adet";
        public const string IncorrectCount = "yanlis_adet";
        public const string HelpContext = "yardim_yeri";
        public const string PanelId = "panel_id";
        public const string PanelName = "panel_adi";
        public const string AiQuestionType = "ai_soru_turu";
        public const string QuizId = "test_id";
        public const string QuizName = "test_adi";
        public const string AnsweredCount = "cevaplanan";
        public const string QuestionIndex = "soru_idx";
        public const string SelectedAnswerIndex = "secilen";
        public const string CorrectAnswerIndex = "dogru_secenek";
        public const string ScoreValue = "skor";
        public const string ScorePercent = "skor_yuzde";
        public const string ActionId = "aksiyon_id";
        public const string ActionName = "aksiyon_adi";
        public const string CompletionSource = "tamamlama_kaynak";
        public const string EntrySource = "giris_kaynak";
        public const string SelectionSource = "secim_kaynak";
        public const string Source = "kaynak";
        public const string StepIndex = "adim_idx";
        public const string StepName = "adim_adi";
        public const string PlacementCount = "yerlestirme";
        public const string TotalPlacements = "toplam_yerlestirme";
        public const string TargetModuleId = "hedef_modul_id";
        public const string TargetModuleName = "hedef_modul_adi";
        public const string TransitionSource = "gecis_kaynak";
        public const string CompletionPercent = "tamamlanma_yuzde";
        public const string ActiveDecisionSeconds = "aktif_karar_sure";
        public const string AverageDecisionSeconds = "ortalama_karar_sure";
        public const string PatientsPerMinute = "dakika_hasta";
        public const string UnderTriageCount = "eksik_triage_adet";
        public const string OverTriageCount = "fazla_triage_adet";
        public const string CriticalMismatchCount = "kritik_uyumsuz_adet";
        public const string LongestCorrectStreak = "en_uzun_dogru_seri";
    }

    [DefaultExecutionOrder(-10000)]
    public sealed class AnalyticsRuntimeBootstrap : MonoBehaviour
    {
        private static AnalyticsRuntimeBootstrap instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeFirstScene()
        {
            if (instance != null)
            {
                return;
            }

            GameObject bootstrapObject = new GameObject(nameof(AnalyticsRuntimeBootstrap));
            DontDestroyOnLoad(bootstrapObject);
            instance = bootstrapObject.AddComponent<AnalyticsRuntimeBootstrap>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            AnalyticsService service = AnalyticsService.GetOrCreateSingleton();
            if (!service.IsInitializationStarted)
            {
                StartCoroutine(InitializeAnalyticsRoutine(service));
            }
        }

        private IEnumerator InitializeAnalyticsRoutine(AnalyticsService service)
        {
            service.MarkInitializationStarted();
            yield return FirebaseBootstrap.Initialize(service);

            if (!service.IsInitializationCompleted)
            {
                service.CompleteInitialization(
                    new DevelopmentAnalyticsAdapter(service.DevelopmentLoggingEnabled),
                    "bootstrap_fallback");
            }

            // Önceki oturumdan kayıtlı katılımcı varsa bağlamı geri yükle
            if (ParticipantManager.HasParticipant)
            {
                service.SetParticipantContext(
                    ParticipantManager.GetParticipantKey(),
                    ParticipantManager.GetParticipantName());
            }
        }

        private void OnApplicationQuit()
        {
            AnalyticsService service = AnalyticsService.Instance;
            if (service != null)
            {
                try
                {
                    service.ExportLocalReport();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AnalyticsRuntimeBootstrap] Cikista otomatik CSV export hatasi: {ex.Message}");
                }
            }

            ParticipantManager.ClearParticipant();
        }
    }

    public sealed class AnalyticsService
    {
        private readonly Queue<AnalyticsEnvelope> queuedEvents = new Queue<AnalyticsEnvelope>();
        private readonly SessionTracker sessionTracker;
        private readonly FirestoreTelemetryWriter firestoreTelemetryWriter;
        private readonly AnalyticsReportExporter reportExporter;
        private IAnalyticsAdapter adapter;
        private string initializationStatus = "pending";

        public static AnalyticsService Instance { get; private set; }

        public AnalyticsService(SessionTracker tracker = null, bool enableDevelopmentLogging = true)
        {
            sessionTracker = tracker ?? new SessionTracker();
            reportExporter = new AnalyticsReportExporter(enableDevelopmentLogging);
            firestoreTelemetryWriter = new FirestoreTelemetryWriter();
            firestoreTelemetryWriter.LocalDocumentRecorded = reportExporter.LogFirestoreDocument;
            firestoreTelemetryWriter.SetSessionContext(sessionTracker.InstallationId, sessionTracker.SessionId);
            DevelopmentLoggingEnabled = enableDevelopmentLogging;
            adapter = new NoOpAnalyticsAdapter();
        }

        public bool DevelopmentLoggingEnabled { get; }
        public bool IsInitializationStarted { get; private set; }
        public bool IsInitializationCompleted { get; private set; }
        public string InitializationStatus => initializationStatus;
        public SessionTracker SessionTracker => sessionTracker;
        public int QueuedEventCount => queuedEvents.Count;

        public static AnalyticsService CreateDefaultService()
        {
            bool devLogging = Application.isEditor || Debug.isDebugBuild;
            return new AnalyticsService(new SessionTracker(), devLogging);
        }

        public static AnalyticsService GetOrCreateSingleton()
        {
            if (Instance == null)
            {
                Instance = CreateDefaultService();
            }

            return Instance;
        }

        public static AnalyticsService EnsureInitializedSingleton()
        {
            AnalyticsService service = GetOrCreateSingleton();
            if (!service.IsInitializationCompleted && !service.IsInitializationStarted)
            {
                service.MarkInitializationStarted();
                service.CompleteInitialization(
                    new DevelopmentAnalyticsAdapter(service.DevelopmentLoggingEnabled),
                    "lazy_default");
            }

            return service;
        }

        public static void ConfigureSingleton(AnalyticsService service)
        {
            Instance = service ?? CreateDefaultService();
        }

        public void MarkInitializationStarted()
        {
            IsInitializationStarted = true;
        }

        public void CompleteInitialization(IAnalyticsAdapter resolvedAdapter, string status)
        {
            adapter = resolvedAdapter ?? new NoOpAnalyticsAdapter();
            initializationStatus = string.IsNullOrWhiteSpace(status) ? "initialized" : status.Trim();
            IsInitializationStarted = true;
            IsInitializationCompleted = true;
            FlushQueuedEvents();
            firestoreTelemetryWriter.FlushSession();
        }

        public void TrackEvent(
            string eventName,
            IDictionary<string, object> eventParameters = null,
            bool suppressDuplicate = false,
            string duplicateKey = null)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            string sanitizedEventName = SanitizeToken(eventName, 40, "event");
            Dictionary<string, object> mergedParameters = BuildMergedParameters(eventParameters);

            if (suppressDuplicate)
            {
                string resolvedDuplicateKey = string.IsNullOrWhiteSpace(duplicateKey)
                    ? sanitizedEventName
                    : duplicateKey;

                if (!sessionTracker.TryRegisterUniqueEvent(resolvedDuplicateKey))
                {
                    return;
                }
            }

            TryWriteFirestoreTelemetry(sanitizedEventName, mergedParameters);
            firestoreTelemetryWriter.WriteDetailedEvent(sanitizedEventName, mergedParameters);

            if (string.Equals(sanitizedEventName, AnalyticsEventNames.ModuleTransitionIntent, StringComparison.Ordinal))
            {
                FlushSessionTelemetry();
            }

            AnalyticsEnvelope envelope = new AnalyticsEnvelope(sanitizedEventName, mergedParameters);
            reportExporter.LogEvent(sanitizedEventName, mergedParameters);

            if (!IsInitializationCompleted)
            {
                queuedEvents.Enqueue(envelope);
                return;
            }

            Dispatch(envelope);
        }

        public void SetParticipantContext(string participantKey, string participantFullName)
        {
            reportExporter.SetParticipantContext(participantKey, participantFullName);
            firestoreTelemetryWriter.SetParticipantContext(participantKey, participantFullName);
        }

        public void WriteParticipantProfile()
        {
            firestoreTelemetryWriter.WriteParticipantProfile();
        }

        public void FlushSessionTelemetry()
        {
            firestoreTelemetryWriter.WriteSessionSummary(sessionTracker.BuildSessionSummary());
            firestoreTelemetryWriter.FlushSession();
        }

        public void SetBaselineProfile(BaselineProfile profile)
        {
            firestoreTelemetryWriter.SetBaselineProfile(profile);
        }

        public void ClearBaselineProfile()
        {
            firestoreTelemetryWriter.ClearBaselineProfile();
        }

        public void WriteModuleBaseline(string moduleId, string moduleName, float durationSeconds)
        {
            firestoreTelemetryWriter.WriteModuleProgressBaseline(
                moduleId,
                moduleName,
                durationSeconds,
                BuildModuleDocId(moduleId));
        }

        public void WriteTaskBaseline(TaskResult taskResult)
        {
            firestoreTelemetryWriter.WriteTaskResultBaseline(taskResult, BuildTaskDocId(taskResult));
        }

        public void WriteQuizBaseline(QuizResult quizResult)
        {
            firestoreTelemetryWriter.WriteQuizResultBaseline(quizResult, BuildQuizDocId(quizResult));
        }

        public void WriteAIInteractionBaseline(AIInteractionResult interaction)
        {
            firestoreTelemetryWriter.WriteAIInteractionBaseline(interaction, BuildAIInteractionDocId(interaction));
        }

        public void WriteTriageBaseline(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            firestoreTelemetryWriter.WriteTriageResultBaseline(eventName, parameters, BuildTriageDocId(eventName, parameters));
        }

        private string BuildModuleDocId(string moduleId)
        {
            if (string.IsNullOrWhiteSpace(moduleId) || string.IsNullOrWhiteSpace(sessionTracker.SessionId))
            {
                return null;
            }

            return sessionTracker.SessionId + "_" + moduleId;
        }

        private string BuildTaskDocId(TaskResult taskResult)
        {
            return BuildDetailDocId("task", taskResult.moduleId, taskResult.taskId, taskResult.targetId);
        }

        private string BuildQuizDocId(QuizResult quizResult)
        {
            return BuildDetailDocId("quiz", quizResult.moduleId, quizResult.quizId);
        }

        private string BuildAIInteractionDocId(AIInteractionResult interaction)
        {
            return BuildDetailDocId("ai", interaction.moduleId, interaction.panelId);
        }

        private string BuildTriageDocId(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            string moduleId = ExtractStringParameter(parameters, AnalyticsParams.ModuleId, string.Empty);
            string victimId = ExtractStringParameter(parameters, AnalyticsParams.VictimId, string.Empty);
            if (!string.IsNullOrWhiteSpace(victimId))
            {
                return BuildDetailDocId("triage", moduleId, "victim", victimId);
            }

            string scoreType = ExtractStringParameter(parameters, AnalyticsParams.TaskType, string.Empty);
            if (string.Equals(eventName, AnalyticsEventNames.ScoreRecorded, StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(scoreType))
            {
                return BuildDetailDocId("triage", moduleId, "score", scoreType);
            }

            return BuildDetailDocId("triage", moduleId, eventName);
        }

        private string BuildDetailDocId(string category, params string[] parts)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(sessionTracker.SessionId))
            {
                return null;
            }

            StringBuilder builder = new StringBuilder(sessionTracker.SessionId);
            builder.Append('_');
            builder.Append(SanitizeToken(category, 32, "detail"));

            if (parts != null)
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(parts[i]))
                    {
                        continue;
                    }

                    builder.Append('_');
                    builder.Append(SanitizeToken(parts[i], 48, "item"));
                }
            }

            return builder.ToString();
        }

        public void ExportLocalReport()
        {
            FlushSessionTelemetry();
            reportExporter.ExportNow();
        }

        private void TryWriteFirestoreTelemetry(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            switch (eventName)
            {
                case AnalyticsEventNames.ModuleCompleted:
                    // moduller koleksiyonu yalnızca "modül tamamlandı" özetini
                    // tutar. ModuleEntered random docId ile append edildiğinde
                    // moduller koleksiyonunda "completed=false" gerçek satırlar
                    // bırakıyordu (ahmet_uysal: 2 gerçek doc, ikisi de entered,
                    // hiç completion yok). Entered olayı zaten event log /
                    // detaylar tarafında izleniyor; moduller'da tekrar tutmak
                    // raporu kirletiyor. Bu yüzden artık sadece Completed
                    // moduller'a deterministic SetAsync ile yazılıyor.
                    ModuleProgressSummary moduleProgress = new ModuleProgressSummary
                    {
                        moduleId = ExtractStringParameter(parameters, AnalyticsParams.ModuleId, string.Empty),
                        moduleName = ExtractStringParameter(parameters, AnalyticsParams.ModuleName, string.Empty),
                        completed = true,
                        durationSeconds = ExtractFloatParameter(parameters, AnalyticsParams.DurationSeconds, 0f)
                    };
                    firestoreTelemetryWriter.WriteModuleProgress(
                        moduleProgress,
                        BuildModuleDocId(moduleProgress.moduleId));
                    break;

                case AnalyticsEventNames.ModuleEntered:
                    // Sessizce geç — modül girişi event log'unda tutuluyor
                    // (TrackEvent event_log akışı). moduller koleksiyonu
                    // sadece tamamlanma özetini bekler.
                    break;

                case AnalyticsEventNames.TaskStarted:
                case AnalyticsEventNames.TaskProgress:
                case AnalyticsEventNames.TaskFailed:
                case AnalyticsEventNames.TaskCompleted:
                case AnalyticsEventNames.VideoStarted:
                case AnalyticsEventNames.VideoProgress:
                case AnalyticsEventNames.VideoCompleted:
                case AnalyticsEventNames.ScenarioTaskCompleted:
                    TaskResult taskResult = BuildTaskResultFromParameters(eventName, parameters);
                    firestoreTelemetryWriter.WriteTaskResult(taskResult, BuildTaskDocId(taskResult));
                    break;

                case AnalyticsEventNames.QuizStarted:
                case AnalyticsEventNames.QuizAnswered:
                case AnalyticsEventNames.QuizCompleted:
                    QuizResult quizResult = BuildQuizResultFromParameters(eventName, parameters);
                    firestoreTelemetryWriter.WriteQuizResult(quizResult, BuildQuizDocId(quizResult));
                    break;

                case AnalyticsEventNames.AIPanelOpened:
                case AnalyticsEventNames.AIQuestionAsked:
                    AIInteractionResult aiInteraction = BuildAiInteractionFromParameters(eventName, parameters);
                    firestoreTelemetryWriter.WriteAIInteraction(aiInteraction, BuildAIInteractionDocId(aiInteraction));
                    break;
            }

            if (string.Equals(eventName, AnalyticsEventNames.VictimTagged, StringComparison.Ordinal) ||
                string.Equals(eventName, AnalyticsEventNames.ScoreRecorded, StringComparison.Ordinal))
            {
                firestoreTelemetryWriter.WriteTriageResult(eventName, parameters, BuildTriageDocId(eventName, parameters));
            }
        }

        private static TaskResult BuildTaskResultFromParameters(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            bool isFailedEvent = string.Equals(eventName, AnalyticsEventNames.TaskFailed, StringComparison.Ordinal);
            bool isCompletedEvent =
                string.Equals(eventName, AnalyticsEventNames.TaskCompleted, StringComparison.Ordinal) ||
                string.Equals(eventName, AnalyticsEventNames.VideoCompleted, StringComparison.Ordinal) ||
                string.Equals(eventName, AnalyticsEventNames.ScenarioTaskCompleted, StringComparison.Ordinal);

            float defaultProgress = isCompletedEvent ? 1f : 0f;
            string taskStatus = ExtractStringParameter(parameters, AnalyticsParams.TaskStatus, string.Empty);
            bool success = string.IsNullOrWhiteSpace(taskStatus)
                ? !isFailedEvent
                : !string.Equals(taskStatus, "failed", StringComparison.OrdinalIgnoreCase);

            int totalCount = ExtractIntParameter(parameters, AnalyticsParams.TotalCount, 0);
            int completedCountDefault = isCompletedEvent ? Math.Max(1, totalCount) : 0;
            string taskId = ExtractStringParameter(parameters, AnalyticsParams.TaskId, string.Empty);
            string taskName = ExtractStringParameter(parameters, AnalyticsParams.TaskName, string.Empty);

            if (string.IsNullOrWhiteSpace(taskId))
            {
                taskId = ExtractStringParameter(parameters, AnalyticsParams.ContentId, "task");
            }

            if (string.IsNullOrWhiteSpace(taskName))
            {
                taskName = ExtractStringParameter(parameters, AnalyticsParams.ContentName, "Task");
            }

            return new TaskResult
            {
                moduleId = ExtractStringParameter(parameters, AnalyticsParams.ModuleId, string.Empty),
                moduleName = ExtractStringParameter(parameters, AnalyticsParams.ModuleName, string.Empty),
                scenarioId = ExtractStringParameter(parameters, AnalyticsParams.ScenarioId, string.Empty),
                taskId = taskId,
                taskName = taskName,
                targetId = ExtractStringParameter(
                    parameters,
                    AnalyticsParams.VictimId,
                    ExtractStringParameter(parameters, AnalyticsParams.ContentId, string.Empty)),
                targetName = ExtractStringParameter(
                    parameters,
                    AnalyticsParams.VictimName,
                    ExtractStringParameter(parameters, AnalyticsParams.ContentName, string.Empty)),
                progress = Mathf.Clamp01(ExtractFloatParameter(parameters, AnalyticsParams.TaskProgress, defaultProgress)),
                completedCount = ExtractIntParameter(parameters, AnalyticsParams.CompletedCount, completedCountDefault),
                totalCount = totalCount,
                durationSeconds = ExtractFloatParameter(parameters, AnalyticsParams.DurationSeconds, 0f),
                success = success
            };
        }

        private static QuizResult BuildQuizResultFromParameters(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            bool isCompleted = string.Equals(eventName, AnalyticsEventNames.QuizCompleted, StringComparison.Ordinal);

            return new QuizResult
            {
                moduleId = ExtractStringParameter(parameters, AnalyticsParams.ModuleId, string.Empty),
                moduleName = ExtractStringParameter(parameters, AnalyticsParams.ModuleName, string.Empty),
                quizId = ExtractStringParameter(parameters, AnalyticsParams.QuizId, string.Empty),
                quizName = ExtractStringParameter(parameters, AnalyticsParams.QuizName, string.Empty),
                totalQuestionCount = ExtractIntParameter(parameters, AnalyticsParams.TotalCount, 0),
                answeredCount = ExtractIntParameter(parameters, AnalyticsParams.AnsweredCount, 0),
                correctCount = ExtractIntParameter(parameters, AnalyticsParams.CorrectCount, 0),
                questionIndex = ExtractIntParameter(parameters, AnalyticsParams.QuestionIndex, 0),
                selectedAnswerIndex = ExtractIntParameter(parameters, AnalyticsParams.SelectedAnswerIndex, -1),
                correctAnswerIndex = ExtractIntParameter(parameters, AnalyticsParams.CorrectAnswerIndex, -1),
                isCorrect = ExtractBoolParameter(parameters, AnalyticsParams.IsCorrect, false),
                completed = isCompleted || ExtractBoolParameter(parameters, "completed", false),
                durationSeconds = ExtractFloatParameter(parameters, AnalyticsParams.DurationSeconds, 0f),
                scorePercentage = ExtractFloatParameter(parameters, AnalyticsParams.ScorePercent, 0f)
            };
        }

        private static AIInteractionResult BuildAiInteractionFromParameters(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            string questionType = ExtractStringParameter(parameters, AnalyticsParams.AiQuestionType, string.Empty);
            if (string.IsNullOrWhiteSpace(questionType) &&
                string.Equals(eventName, AnalyticsEventNames.AIPanelOpened, StringComparison.Ordinal))
            {
                questionType = "panel_opened";
            }

            return new AIInteractionResult
            {
                moduleId = ExtractStringParameter(parameters, AnalyticsParams.ModuleId, string.Empty),
                moduleName = ExtractStringParameter(parameters, AnalyticsParams.ModuleName, string.Empty),
                panelId = ExtractStringParameter(parameters, AnalyticsParams.PanelId, string.Empty),
                panelName = ExtractStringParameter(parameters, AnalyticsParams.PanelName, string.Empty),
                questionType = questionType,
                durationSeconds = ExtractFloatParameter(parameters, AnalyticsParams.DurationSeconds, 0f)
            };
        }

        private static string ExtractStringParameter(IReadOnlyDictionary<string, object> parameters, string key, string defaultValue)
        {
            if (parameters == null || string.IsNullOrWhiteSpace(key))
            {
                return defaultValue;
            }

            if (!parameters.TryGetValue(key, out object value) || value == null)
            {
                return defaultValue;
            }

            string text = value.ToString();
            return string.IsNullOrWhiteSpace(text) ? defaultValue : text.Trim();
        }

        private static int ExtractIntParameter(IReadOnlyDictionary<string, object> parameters, string key, int defaultValue)
        {
            if (parameters == null || string.IsNullOrWhiteSpace(key))
            {
                return defaultValue;
            }

            if (!parameters.TryGetValue(key, out object value) || value == null)
            {
                return defaultValue;
            }

            switch (value)
            {
                case int intValue:
                    return intValue;
                case long longValue:
                    return (int)longValue;
                case float floatValue:
                    return Mathf.RoundToInt(floatValue);
                case double doubleValue:
                    return Mathf.RoundToInt((float)doubleValue);
            }

            return int.TryParse(value.ToString(), out int parsed) ? parsed : defaultValue;
        }

        private static float ExtractFloatParameter(IReadOnlyDictionary<string, object> parameters, string key, float defaultValue)
        {
            if (parameters == null || string.IsNullOrWhiteSpace(key))
            {
                return defaultValue;
            }

            if (!parameters.TryGetValue(key, out object value) || value == null)
            {
                return defaultValue;
            }

            switch (value)
            {
                case float floatValue:
                    return floatValue;
                case double doubleValue:
                    return (float)doubleValue;
                case int intValue:
                    return intValue;
                case long longValue:
                    return longValue;
            }

            return float.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                ? parsed
                : defaultValue;
        }

        private static bool ExtractBoolParameter(IReadOnlyDictionary<string, object> parameters, string key, bool defaultValue)
        {
            if (parameters == null || string.IsNullOrWhiteSpace(key))
            {
                return defaultValue;
            }

            if (!parameters.TryGetValue(key, out object value) || value == null)
            {
                return defaultValue;
            }

            switch (value)
            {
                case bool boolValue:
                    return boolValue;
                case int intValue:
                    return intValue != 0;
                case long longValue:
                    return longValue != 0L;
                case float floatValue:
                    return !Mathf.Approximately(floatValue, 0f);
                case double doubleValue:
                    return Math.Abs(doubleValue) > double.Epsilon;
            }

            if (bool.TryParse(value.ToString(), out bool parsedBool))
            {
                return parsedBool;
            }

            return int.TryParse(value.ToString(), out int parsedInt)
                ? parsedInt != 0
                : defaultValue;
        }

        public Dictionary<string, object> BuildMergedParameters(IDictionary<string, object> eventParameters = null)
        {
            Dictionary<string, object> mergedParameters = sessionTracker.BuildCommonParameters();
            mergedParameters[AnalyticsParams.SceneName] = ResolveCurrentSceneName();
            mergedParameters[AnalyticsParams.RuntimePlatform] = NormalizeValue(Application.platform.ToString());

            if (eventParameters == null)
            {
                return mergedParameters;
            }

            foreach (KeyValuePair<string, object> pair in eventParameters)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    continue;
                }

                mergedParameters[SanitizeToken(pair.Key, 40, "param")] = NormalizeValue(pair.Value);
            }

            return mergedParameters;
        }

        public static string SanitizeToken(string rawValue, int maxLength = 40, string fallback = "unknown")
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return fallback;
            }

            StringBuilder builder = new StringBuilder(rawValue.Length);
            bool previousWasUnderscore = false;

            for (int i = 0; i < rawValue.Length; i++)
            {
                char current = char.ToLowerInvariant(rawValue[i]);
                bool isAllowed = (current >= 'a' && current <= 'z') || (current >= '0' && current <= '9');
                if (isAllowed)
                {
                    builder.Append(current);
                    previousWasUnderscore = false;
                    continue;
                }

                if (previousWasUnderscore)
                {
                    continue;
                }

                builder.Append('_');
                previousWasUnderscore = true;
            }

            string sanitized = builder.ToString().Trim('_');
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                sanitized = fallback;
            }

            if (sanitized.Length > maxLength)
            {
                sanitized = sanitized.Substring(0, maxLength).TrimEnd('_');
            }

            if (sanitized.Length == 0)
            {
                sanitized = fallback;
            }

            if (char.IsDigit(sanitized[0]))
            {
                sanitized = "x_" + sanitized;
            }

            return sanitized;
        }

        public static object NormalizeValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            switch (value)
            {
                case string text:
                    return LimitText(text, 100);

                case bool booleanValue:
                    return booleanValue ? 1L : 0L;

                case Enum enumValue:
                    return LimitText(enumValue.ToString(), 100);

                case byte byteValue:
                    return (long)byteValue;

                case sbyte sbyteValue:
                    return (long)sbyteValue;

                case short shortValue:
                    return (long)shortValue;

                case ushort ushortValue:
                    return (long)ushortValue;

                case int intValue:
                    return (long)intValue;

                case uint uintValue:
                    return (long)uintValue;

                case long longValue:
                    return longValue;

                case ulong ulongValue:
                    return (double)ulongValue;

                case float floatValue:
                    return Math.Round(floatValue, 3);

                case double doubleValue:
                    return Math.Round(doubleValue, 3);

                case decimal decimalValue:
                    return Math.Round((double)decimalValue, 3);

                case TimeSpan timeSpan:
                    return Math.Round(timeSpan.TotalSeconds, 3);
            }

            return LimitText(Convert.ToString(value, CultureInfo.InvariantCulture), 100);
        }

        private void FlushQueuedEvents()
        {
            while (queuedEvents.Count > 0)
            {
                AnalyticsEnvelope envelope = queuedEvents.Dequeue();
                Dispatch(envelope);
            }
        }

        private void Dispatch(AnalyticsEnvelope envelope)
        {
            try
            {
                adapter.LogEvent(envelope.EventName, envelope.Parameters);
                sessionTracker.RegisterDispatchedEvent();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AnalyticsService] Event dispatch failed, switching to safe fallback: " + ex.Message);
                CrashlyticsBridge.LogException(ex, "analytics_dispatch_failed");

                adapter = DevelopmentLoggingEnabled
                    ? new DevelopmentAnalyticsAdapter(true)
                    : (IAnalyticsAdapter)new NoOpAnalyticsAdapter();

                adapter.LogEvent(envelope.EventName, envelope.Parameters);
                sessionTracker.RegisterDispatchedEvent();
            }
        }

        private static string LimitText(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string trimmed = text.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
        }

        private static string ResolveCurrentSceneName()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                return "bootstrap";
            }

            return string.IsNullOrWhiteSpace(activeScene.name) ? "unnamed_scene" : activeScene.name.Trim();
        }

        private readonly struct AnalyticsEnvelope
        {
            public AnalyticsEnvelope(string eventName, Dictionary<string, object> parameters)
            {
                EventName = eventName;
                Parameters = parameters ?? new Dictionary<string, object>();
            }

            public string EventName { get; }
            public Dictionary<string, object> Parameters { get; }
        }
    }

    public sealed class DevelopmentAnalyticsAdapter : IAnalyticsAdapter
    {
        private readonly bool emitLogs;

        public DevelopmentAnalyticsAdapter(bool emitLogs)
        {
            this.emitLogs = emitLogs;
        }

        public string AdapterName => "development_logger";
        public bool IsOperational => true;

        public void LogEvent(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            if (!emitLogs)
            {
                return;
            }

            StringBuilder builder = new StringBuilder(128);
            builder.Append("[Analytics] ");
            builder.Append(eventName);

            if (parameters != null && parameters.Count > 0)
            {
                builder.Append(" | ");
                bool first = true;
                foreach (KeyValuePair<string, object> pair in parameters)
                {
                    if (!first)
                    {
                        builder.Append(", ");
                    }

                    first = false;
                    builder.Append(pair.Key);
                    builder.Append('=');
                    builder.Append(pair.Value);
                }
            }

            Debug.Log(builder.ToString());
        }
    }

    internal sealed class NoOpAnalyticsAdapter : IAnalyticsAdapter
    {
        public string AdapterName => "noop";
        public bool IsOperational => true;

        public void LogEvent(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
        }
    }
}
