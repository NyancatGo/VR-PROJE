using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace TrainingAnalytics
{
    public static class FirebaseBootstrap
    {
        private const string FirebaseAppTypeName = "Firebase.FirebaseApp, Firebase.App";
        private const string FirebaseAnalyticsTypeName = "Firebase.Analytics.FirebaseAnalytics, Firebase.Analytics";

        public static IEnumerator Initialize(AnalyticsService service)
        {
            if (service == null)
            {
                yield break;
            }

            Type firebaseAppType = Type.GetType(FirebaseAppTypeName, false);
            Type firebaseAnalyticsType = Type.GetType(FirebaseAnalyticsTypeName, false);

            if (firebaseAppType == null || firebaseAnalyticsType == null)
            {
                service.CompleteInitialization(
                    new DevelopmentAnalyticsAdapter(service.DevelopmentLoggingEnabled),
                    "firebase_sdk_absent");
                yield break;
            }

            MethodInfo dependencyCheckMethod = firebaseAppType.GetMethod(
                "CheckAndFixDependenciesAsync",
                BindingFlags.Public | BindingFlags.Static);

            if (dependencyCheckMethod != null)
            {
                object taskObject = null;
                try
                {
                    taskObject = dependencyCheckMethod.Invoke(null, null);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[FirebaseBootstrap] Dependency check could not start: " + ex.Message);
                    CrashlyticsBridge.LogException(ex, "firebase_dependency_check_start");
                    service.CompleteInitialization(
                        new DevelopmentAnalyticsAdapter(service.DevelopmentLoggingEnabled),
                        "firebase_dependency_check_error");
                    yield break;
                }

                if (taskObject is Task dependencyTask)
                {
                    while (!dependencyTask.IsCompleted)
                    {
                        yield return null;
                    }

                    if (dependencyTask.IsFaulted || dependencyTask.IsCanceled)
                    {
                        Exception taskException = dependencyTask.Exception ?? new Exception("Firebase dependency task failed.");
                        Debug.LogWarning("[FirebaseBootstrap] Firebase dependency task failed: " + taskException.Message);
                        CrashlyticsBridge.LogException(taskException, "firebase_dependency_check_task");
                        service.CompleteInitialization(
                            new DevelopmentAnalyticsAdapter(service.DevelopmentLoggingEnabled),
                            "firebase_dependency_check_failed");
                        yield break;
                    }

                    PropertyInfo resultProperty = taskObject.GetType().GetProperty("Result");
                    object dependencyResult = resultProperty != null ? resultProperty.GetValue(taskObject, null) : null;
                    if (dependencyResult != null &&
                        !string.Equals(dependencyResult.ToString(), "Available", StringComparison.OrdinalIgnoreCase))
                    {
                        service.CompleteInitialization(
                            new DevelopmentAnalyticsAdapter(service.DevelopmentLoggingEnabled),
                            "firebase_dependencies_" + dependencyResult);
                        yield break;
                    }
                }
            }

            try
            {
                PropertyInfo defaultInstanceProperty = firebaseAppType.GetProperty(
                    "DefaultInstance",
                    BindingFlags.Public | BindingFlags.Static);
                _ = defaultInstanceProperty?.GetValue(null, null);

                service.CompleteInitialization(
                    new FirebaseAnalyticsAdapter(firebaseAnalyticsType),
                    "firebase_ready");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[FirebaseBootstrap] Falling back to local analytics adapter: " + ex.Message);
                CrashlyticsBridge.LogException(ex, "firebase_adapter_create");
                service.CompleteInitialization(
                    new DevelopmentAnalyticsAdapter(service.DevelopmentLoggingEnabled),
                    "firebase_adapter_failed");
            }
        }
    }

    public sealed class FirebaseAnalyticsAdapter : IAnalyticsAdapter
    {
        private readonly Type firebaseAnalyticsType;
        private readonly Type firebaseParameterType;
        private readonly MethodInfo parameterizedLogEventMethod;
        private readonly MethodInfo simpleLogEventMethod;

        public FirebaseAnalyticsAdapter(Type firebaseAnalyticsType = null)
        {
            this.firebaseAnalyticsType = firebaseAnalyticsType ?? Type.GetType("Firebase.Analytics.FirebaseAnalytics, Firebase.Analytics", true);
            firebaseParameterType = Type.GetType("Firebase.Analytics.Parameter, Firebase.Analytics", false);

            if (this.firebaseAnalyticsType != null)
            {
                simpleLogEventMethod = this.firebaseAnalyticsType.GetMethod(
                    "LogEvent",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string) },
                    null);

                if (firebaseParameterType != null)
                {
                    parameterizedLogEventMethod = this.firebaseAnalyticsType.GetMethod(
                        "LogEvent",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new[] { typeof(string), firebaseParameterType.MakeArrayType() },
                        null);
                }
            }
        }

        public string AdapterName => "firebase_analytics";
        public bool IsOperational => firebaseAnalyticsType != null;

        public void LogEvent(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            if (!IsOperational || string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            string sanitizedEventName = AnalyticsService.SanitizeToken(eventName, 40, "event");
            Array firebaseParameters = BuildFirebaseParameterArray(parameters);

            if (parameterizedLogEventMethod != null && firebaseParameters != null && firebaseParameters.Length > 0)
            {
                parameterizedLogEventMethod.Invoke(null, new object[] { sanitizedEventName, firebaseParameters });
                return;
            }

            simpleLogEventMethod?.Invoke(null, new object[] { sanitizedEventName });
        }

        private Array BuildFirebaseParameterArray(IReadOnlyDictionary<string, object> parameters)
        {
            if (firebaseParameterType == null || parameters == null || parameters.Count == 0)
            {
                return null;
            }

            List<object> builtParameters = new List<object>(parameters.Count);
            foreach (KeyValuePair<string, object> pair in parameters)
            {
                object parameterObject = TryBuildParameter(pair.Key, pair.Value);
                if (parameterObject != null)
                {
                    builtParameters.Add(parameterObject);
                }
            }

            Array parameterArray = Array.CreateInstance(firebaseParameterType, builtParameters.Count);
            for (int i = 0; i < builtParameters.Count; i++)
            {
                parameterArray.SetValue(builtParameters[i], i);
            }

            return parameterArray;
        }

        private object TryBuildParameter(string rawKey, object rawValue)
        {
            string key = AnalyticsService.SanitizeToken(rawKey, 40, "param");
            object value = AnalyticsService.NormalizeValue(rawValue);
            if (value == null)
            {
                return null;
            }

            try
            {
                if (value is long longValue)
                {
                    return Activator.CreateInstance(firebaseParameterType, key, longValue);
                }

                if (value is double doubleValue)
                {
                    return Activator.CreateInstance(firebaseParameterType, key, doubleValue);
                }

                return Activator.CreateInstance(firebaseParameterType, key, value.ToString());
            }
            catch
            {
                return null;
            }
        }
    }

    public sealed class FirestoreTelemetryWriter
    {
        private const string FirebaseFirestoreTypeName = "Firebase.Firestore.FirebaseFirestore, Firebase.Firestore";
        private const string CollectionReferenceTypeName = "Firebase.Firestore.CollectionReference, Firebase.Firestore";
        private const string DocumentReferenceTypeName = "Firebase.Firestore.DocumentReference, Firebase.Firestore";
        private const string SessionSummaryCollection = "oturumlar";
        private const string ModuleProgressCollection = "moduller";
        private const string TaskResultCollection = "gorevler";
        private const string QuizResultCollection = "testler";
        private const string AiInteractionCollection = "ai_etkilesim";
        private const string TriageResultCollection = "triage_sonuc";
        private const string DetailedEventCollection = "detaylar";
        private const int MaxPendingWrites = 512;

        private readonly object syncRoot = new object();
        private readonly Queue<PendingWrite> pendingWrites = new Queue<PendingWrite>();
        private readonly Type firestoreType;
        private readonly PropertyInfo firestoreDefaultInstanceProperty;
        private readonly MethodInfo firestoreCollectionMethod;
        private readonly MethodInfo documentMethod;
        private readonly MethodInfo setAsyncMethod;

        private string installationId = string.Empty;
        private string sessionId = string.Empty;
        private string participantKey = string.Empty;
        private string participantFullName = string.Empty;
        private bool firestoreUnavailableLogged;
        private bool participantScopeMissingLogged;
        private BaselineProfile baselineProfile;
        private int detailSequence;

        public Action<string, IReadOnlyDictionary<string, object>> LocalDocumentRecorded { get; set; }

        public FirestoreTelemetryWriter()
        {
            firestoreType = Type.GetType(FirebaseFirestoreTypeName, false);
            if (firestoreType == null)
            {
                LogFirestoreUnavailable("Firebase.Firestore DLL bulunamadi.");
                return;
            }

            firestoreDefaultInstanceProperty = firestoreType.GetProperty(
                "DefaultInstance",
                BindingFlags.Public | BindingFlags.Static);

            firestoreCollectionMethod = firestoreType.GetMethod(
                "Collection",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(string) },
                null);

            if (firestoreDefaultInstanceProperty == null || firestoreCollectionMethod == null)
            {
                LogFirestoreUnavailable("Firestore API reflection ile cozumlenemedi.");
            }

            Type collectionReferenceType = Type.GetType(CollectionReferenceTypeName, false);
            if (collectionReferenceType != null)
            {
                documentMethod = collectionReferenceType.GetMethod(
                    "Document",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(string) },
                    null);
            }

            Type documentReferenceType = Type.GetType(DocumentReferenceTypeName, false);
            if (documentReferenceType != null)
            {
                setAsyncMethod = FindSetAsyncMethod(documentReferenceType);
            }
        }

        public void SetSessionContext(string installationId, string sessionId)
        {
            this.installationId = string.IsNullOrWhiteSpace(installationId) ? string.Empty : installationId.Trim();
            this.sessionId = string.IsNullOrWhiteSpace(sessionId) ? string.Empty : sessionId.Trim();
        }

        public void SetParticipantContext(string key, string fullName)
        {
            bool hadParticipantScope = !string.IsNullOrWhiteSpace(participantKey);
            this.participantKey = string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
            this.participantFullName = string.IsNullOrWhiteSpace(fullName) ? string.Empty : fullName.Trim();
            if (!string.IsNullOrWhiteSpace(this.participantKey))
            {
                participantScopeMissingLogged = false;
                if (!hadParticipantScope)
                {
                    FlushSession();
                }
            }
        }

        public bool WriteParticipantProfile()
        {
            if (string.IsNullOrWhiteSpace(participantKey))
            {
                return false;
            }

            var profileDoc = new Dictionary<string, object>
            {
                { "katilimci", participantKey },
                { "isim", participantFullName },
                { "son_gorulme", DateTime.UtcNow.ToString("O") },
                { "kurulum_id", string.IsNullOrWhiteSpace(installationId) ? "unknown" : installationId }
            };

            // baslangic kalıcı: ParticipantManager ilk SaveParticipant çağrısında
            // PlayerPrefs'e damgaladı, sonraki update'lerde değişmiyor. Audit
            // raporunda bu alan eksik göründüğü için (ahmet_uysal: WARN baslangic
            // missing) buraya ekliyoruz. SetAsync her seferinde aynı değeri
            // yazacak, ezme tehlikesi yok.
            string baslangic = string.Empty;
            try
            {
                baslangic = ParticipantManager.GetParticipantBaslangic();
            }
            catch (Exception)
            {
                // ParticipantManager mevcut değilse veya PlayerPrefs hatalıysa
                // sessizce geç — baslangic eklenmez, mevcut akış bozulmaz.
            }
            if (!string.IsNullOrWhiteSpace(baslangic))
            {
                profileDoc["baslangic"] = baslangic;
            }

            MirrorDocumentForLocalReport("katilimcilar", profileDoc);

            if (documentMethod == null || setAsyncMethod == null)
            {
                return false;
            }

            if (firestoreType == null || firestoreDefaultInstanceProperty == null || firestoreCollectionMethod == null)
            {
                return false;
            }

            object firestoreInstance;
            try
            {
                firestoreInstance = firestoreDefaultInstanceProperty.GetValue(null, null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[FirestoreTelemetryWriter] Firestore DefaultInstance alinamadi: " + ex.Message);
                return false;
            }

            if (firestoreInstance == null)
            {
                return false;
            }

            try
            {
                object collectionRef = firestoreCollectionMethod.Invoke(firestoreInstance, new object[] { "katilimcilar" });
                if (collectionRef == null) return false;

                object documentRef = documentMethod.Invoke(collectionRef, new object[] { participantKey });
                if (documentRef == null) return false;

                object[] profileSetArgs = setAsyncMethod.GetParameters().Length >= 2
                    ? new object[] { profileDoc, null }
                    : new object[] { profileDoc };
                setAsyncMethod.Invoke(documentRef, profileSetArgs);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[FirestoreTelemetryWriter] Participant profil yazilamadi: " + ex.Message);
                CrashlyticsBridge.LogException(ex, "participant_profile_write_failed");
                return false;
            }
        }

        public bool TryWriteSessionSummary(SessionSummary summary)
        {
            return WriteSessionSummary(summary);
        }

        public bool WriteSessionSummary(SessionSummary summary)
        {
            Dictionary<string, object> document = CreateBaseDocument();
            document["baslangic"] = summary.sessionStartedUtc;
            document["sure"] = Math.Max(0f, summary.sessionDurationSeconds);
            document["toplam_event"] = Math.Max(0, summary.totalEvents);
            // Aynı oturum birden fazla flush edildiğinde her seferinde yeni
            // random doc oluşmasını engellemek için sessionId'yi deterministic
            // docId olarak kullanıyoruz. Sonraki flush'lar mevcut özeti
            // SetAsync ile günceller (toplam_event, sure değerleri büyür).
            string sessionDocId = string.IsNullOrWhiteSpace(summary.sessionId) ? null : summary.sessionId;
            return EnqueueOrWrite(SessionSummaryCollection, document, sessionDocId);
        }

        public bool WriteModuleProgress(ModuleProgressSummary progress, string docId = null)
        {
            return EnqueueOrWrite(ModuleProgressCollection, CreateModuleProgressDocument(progress), docId);
        }

        public bool WriteModuleProgressBaseline(string moduleId, string moduleName, float durationSeconds, string docId)
        {
            Dictionary<string, object> document = CreateBaseDocument();
            document["modul_id"] = moduleId;
            document["modul_adi"] = moduleName;
            document["tamamlandi"] = true;
            document["sure"] = Math.Max(0f, durationSeconds);
            ApplyBaselineFields(document);
            // Placeholder satirlari yerel CSV raporuna yansitilmaz; sadece Firestore'da yer alir.
            return EnqueueOrWrite(ModuleProgressCollection, document, docId, mirrorLocally: false);
        }

        public bool WriteTaskResult(TaskResult taskResult, string docId = null)
        {
            return EnqueueOrWrite(TaskResultCollection, CreateTaskDocument(taskResult), docId);
        }

        public bool WriteTaskResultBaseline(TaskResult taskResult, string docId)
        {
            Dictionary<string, object> document = CreateTaskDocument(taskResult);
            ApplyBaselineFields(document);
            return EnqueueOrWrite(TaskResultCollection, document, docId, mirrorLocally: false);
        }

        public bool WriteQuizResult(QuizResult quizResult, string docId = null)
        {
            return EnqueueOrWrite(QuizResultCollection, CreateQuizDocument(quizResult), docId);
        }

        public bool WriteQuizResultBaseline(QuizResult quizResult, string docId)
        {
            Dictionary<string, object> document = CreateQuizDocument(quizResult);
            ApplyBaselineFields(document);
            return EnqueueOrWrite(QuizResultCollection, document, docId, mirrorLocally: false);
        }

        // AI etkileşim özetleri için aynı (session+module+panel) docId'sine
        // birden çok yazım gelebilir — örn. önce "panel_opened", sonra
        // "ai_soru". SetAsync ezme yaptığı için sonraki yazım önceki bilgiyi
        // tamamen sıfırlayabiliyordu (panel_opened gelirse ai_soru_adedi=0
        // yazılıyordu). Aynı session içinde docId bazlı in-memory akümülatör
        // tutarak monoton birleşim sağlıyoruz: sayaçlar artar, true bayraklar
        // false'a dönmez, AI metni hâlâ saklanmaz.
        private readonly Dictionary<string, AiSummaryAccumulator> aiSummaryByDocId =
            new Dictionary<string, AiSummaryAccumulator>(StringComparer.Ordinal);

        public bool WriteAIInteraction(AIInteractionResult interaction, string docId = null)
        {
            Dictionary<string, object> document = CreateAIInteractionDocument(interaction);

            if (!string.IsNullOrWhiteSpace(docId))
            {
                if (!aiSummaryByDocId.TryGetValue(docId, out AiSummaryAccumulator acc))
                {
                    acc = new AiSummaryAccumulator();
                    aiSummaryByDocId[docId] = acc;
                }
                acc.Merge(document);
                acc.WriteInto(document);
            }

            return EnqueueOrWrite(AiInteractionCollection, document, docId);
        }

        public bool WriteAIInteractionBaseline(AIInteractionResult interaction, string docId)
        {
            Dictionary<string, object> document = CreateAIInteractionDocument(interaction);
            ApplyBaselineFields(document);
            document["ai_panel_acildi_mi"] = true;
            document["ai_soru_soruldu_mu"] = true;
            document["ai_soru_adedi"] = EstimateBaselineAiQuestionCount(interaction.durationSeconds);
            document["ai_basarili_mi"] = true;
            document["ai_hata_kodu"] = string.Empty;
            return EnqueueOrWrite(AiInteractionCollection, document, docId, mirrorLocally: false);
        }

        public bool WriteTriageResult(string eventName, IReadOnlyDictionary<string, object> parameters, string docId = null)
        {
            return EnqueueOrWrite(TriageResultCollection, CreateTriageDocument(eventName, parameters), docId);
        }

        public bool WriteDetailedEvent(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            return EnqueueOrWrite(DetailedEventCollection, CreateDetailedEventDocument(eventName, parameters));
        }

        internal bool WriteDetailedEventBaseline(DetailEventTemplate template, string docId)
        {
            Dictionary<string, object> document = CreateDetailedEventDocument(template.EventName, template.Parameters);
            ApplyBaselineFields(document);
            return EnqueueOrWrite(DetailedEventCollection, document, docId, mirrorLocally: false);
        }

        public bool WriteTriageResultBaseline(string eventName, IReadOnlyDictionary<string, object> parameters, string docId)
        {
            Dictionary<string, object> document = CreateTriageDocument(eventName, parameters);
            ApplyBaselineFields(document);
            return EnqueueOrWrite(TriageResultCollection, document, docId, mirrorLocally: false);
        }

        public void SetBaselineProfile(BaselineProfile profile)
        {
            baselineProfile = profile;
        }

        public void ClearBaselineProfile()
        {
            baselineProfile = null;
        }

        private Dictionary<string, object> CreateModuleProgressDocument(ModuleProgressSummary progress)
        {
            Dictionary<string, object> document = CreateBaseDocument();
            document["modul_id"] = progress.moduleId;
            document["modul_adi"] = progress.moduleName;
            document["tamamlandi"] = progress.completed;
            document["sure"] = Math.Max(0f, progress.durationSeconds);
            return document;
        }

        private Dictionary<string, object> CreateTaskDocument(TaskResult taskResult)
        {
            Dictionary<string, object> document = CreateBaseDocument();
            document["modul_id"] = taskResult.moduleId;
            document["modul_adi"] = taskResult.moduleName;
            document["senaryo_id"] = taskResult.scenarioId;
            document["gorev_id"] = taskResult.taskId;
            document["gorev_adi"] = taskResult.taskName;
            document["hedef_id"] = taskResult.targetId;
            document["hedef_adi"] = taskResult.targetName;
            document["gorev_ilerleme"] = Mathf.Clamp01(taskResult.progress);
            document["tamamlanan"] = Math.Max(0, taskResult.completedCount);
            document["toplam"] = Math.Max(0, taskResult.totalCount);
            document["sure"] = Math.Max(0f, taskResult.durationSeconds);
            document["basarili"] = taskResult.success;
            return document;
        }

        private Dictionary<string, object> CreateQuizDocument(QuizResult quizResult)
        {
            Dictionary<string, object> document = CreateBaseDocument();
            document["modul_id"] = quizResult.moduleId;
            document["modul_adi"] = quizResult.moduleName;
            document["test_id"] = quizResult.quizId;
            document["test_adi"] = quizResult.quizName;
            document["toplam_soru"] = Math.Max(0, quizResult.totalQuestionCount);
            document["cevaplanan"] = Math.Max(0, quizResult.answeredCount);
            document["dogru_adet"] = Math.Max(0, quizResult.correctCount);
            document["soru_idx"] = Math.Max(0, quizResult.questionIndex);
            document["secilen"] = Math.Max(-1, quizResult.selectedAnswerIndex);
            document["dogru_secenek"] = Math.Max(-1, quizResult.correctAnswerIndex);
            document["dogru"] = quizResult.isCorrect;
            document["tamamlandi"] = quizResult.completed;
            document["sure"] = Math.Max(0f, quizResult.durationSeconds);
            document["skor_yuzde"] = Math.Max(0f, quizResult.scorePercentage);
            return document;
        }

        private Dictionary<string, object> CreateAIInteractionDocument(AIInteractionResult interaction)
        {
            Dictionary<string, object> document = CreateBaseDocument();
            document["modul_id"] = interaction.moduleId;
            document["modul_adi"] = interaction.moduleName;
            document["panel_id"] = interaction.panelId;
            document["panel_adi"] = interaction.panelName;
            document["ai_soru_turu"] = interaction.questionType;
            document["sure"] = Math.Max(0f, interaction.durationSeconds);
            bool questionAsked = !string.IsNullOrWhiteSpace(interaction.questionType) &&
                !string.Equals(interaction.questionType, "panel_opened", StringComparison.OrdinalIgnoreCase);
            document["ai_panel_acildi_mi"] = !string.IsNullOrWhiteSpace(interaction.panelId);
            document["ai_soru_soruldu_mu"] = questionAsked;
            document["ai_soru_adedi"] = questionAsked ? 1 : 0;
            document["ai_basarili_mi"] = true;
            document["ai_hata_kodu"] = string.Empty;
            return document;
        }

        private Dictionary<string, object> CreateTriageDocument(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            Dictionary<string, object> document = CreateBaseDocument();
            document["olay"] = AnalyticsService.SanitizeToken(eventName, 40, "event");

            CopyParameter(parameters, document, AnalyticsParams.ModuleId, "modul_id");
            CopyParameter(parameters, document, AnalyticsParams.ModuleName, "modul_adi");
            CopyParameter(parameters, document, AnalyticsParams.ScenarioId, "senaryo_id");
            CopyParameter(parameters, document, AnalyticsParams.ScenarioName, "senaryo_adi");
            CopyParameter(parameters, document, AnalyticsParams.VictimId, "hasta_id");
            CopyParameter(parameters, document, AnalyticsParams.VictimName, "hasta_adi");
            CopyParameter(parameters, document, AnalyticsParams.AssignedTriage, "atanan");
            CopyParameter(parameters, document, AnalyticsParams.ActualTriage, "sonuc");
            CopyParameter(parameters, document, AnalyticsParams.IsCorrect, "dogru");
            CopyParameter(parameters, document, AnalyticsParams.ScoreValue, "skor");
            CopyParameter(parameters, document, AnalyticsParams.ScorePercent, "skor_yuzde");
            CopyParameter(parameters, document, AnalyticsParams.DurationSeconds, "sure");
            CopyParameter(parameters, document, AnalyticsParams.CompletionPercent, "tamamlanma_yuzde");
            CopyParameter(parameters, document, AnalyticsParams.ActiveDecisionSeconds, "aktif_karar_sure");
            CopyParameter(parameters, document, AnalyticsParams.AverageDecisionSeconds, "ortalama_karar_sure");
            CopyParameter(parameters, document, AnalyticsParams.PatientsPerMinute, "dakika_hasta");
            CopyParameter(parameters, document, AnalyticsParams.UnderTriageCount, "eksik_triage_adet");
            CopyParameter(parameters, document, AnalyticsParams.OverTriageCount, "fazla_triage_adet");
            CopyParameter(parameters, document, AnalyticsParams.CriticalMismatchCount, "kritik_uyumsuz_adet");
            CopyParameter(parameters, document, AnalyticsParams.LongestCorrectStreak, "en_uzun_dogru_seri");

            return document;
        }

        private Dictionary<string, object> CreateDetailedEventDocument(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            Dictionary<string, object> document = CreateBaseDocument();
            document["detay_sira"] = ++detailSequence;
            document["olay"] = AnalyticsService.SanitizeToken(eventName, 40, "event");

            if (parameters != null)
            {
                foreach (KeyValuePair<string, object> pair in parameters)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
                    {
                        continue;
                    }

                    document[pair.Key] = pair.Value;
                }
            }

            return document;
        }

        /// <summary>
        /// Tek veri modunda ApplyBaselineFields görünür kaynak etiketi
        /// basmaz. Yalnızca placeholder'ın "henüz ezilmemiş" olduğunu
        /// işaretlemek için son_guncelleme alanını kaldırır — gerçek event
        /// geldiğinde CreateBaseDocument yeni bir son_guncelleme damgası
        /// yazar, böylece "bu kayıt artık gerçek aksiyonla güncellenmiş"
        /// bilgisi yine elde edilir (sadece etiket olarak değil, audit
        /// üzerinden zaman damgası farkıyla görünür).
        /// </summary>
        private void ApplyBaselineFields(Dictionary<string, object> document)
        {
            if (document == null)
            {
                return;
            }

            document.Remove("son_guncelleme");

            // Baseline seed sistemi içeride çalışmaya devam ediyor
            // (deterministic docId, ezme akışı), ama profil etiketleri
            // artık dokümana basılmıyor. Profil bilgisi yalnızca
            // BaselineProfileAllocator state'inde yaşar.
        }

        private int EstimateBaselineAiQuestionCount(float durationSeconds)
        {
            BaselineProfile profile = baselineProfile;
            if (profile == null || string.IsNullOrWhiteSpace(profile.Label))
            {
                return durationSeconds >= 60f ? 2 : 1;
            }

            if (string.Equals(profile.Label, BaselineProfileCatalog.LabelFastHighSuccess, StringComparison.Ordinal))
            {
                return 1;
            }

            if (string.Equals(profile.Label, BaselineProfileCatalog.LabelMediumGoodSuccess, StringComparison.Ordinal))
            {
                return 2;
            }

            if (string.Equals(profile.Label, BaselineProfileCatalog.LabelSlowLowSuccess, StringComparison.Ordinal))
            {
                return 3;
            }

            return profile.Index % 2 == 0 ? 3 : 2;
        }

        public bool FlushSession()
        {
            int pendingCount;
            lock (syncRoot)
            {
                pendingCount = pendingWrites.Count;
            }

            if (pendingCount <= 0)
            {
                return false;
            }

            bool wroteAny = false;
            Queue<PendingWrite> retryQueue = new Queue<PendingWrite>();
            for (int i = 0; i < pendingCount; i++)
            {
                PendingWrite pendingWrite;
                lock (syncRoot)
                {
                    if (pendingWrites.Count == 0)
                    {
                        break;
                    }

                    pendingWrite = pendingWrites.Dequeue();
                }

                if (TryWriteNow(pendingWrite.collection, pendingWrite.document, pendingWrite.docId))
                {
                    wroteAny = true;
                }
                else
                {
                    retryQueue.Enqueue(pendingWrite);
                }
            }

            lock (syncRoot)
            {
                while (retryQueue.Count > 0)
                {
                    pendingWrites.Enqueue(retryQueue.Dequeue());
                }
            }

            return wroteAny;
        }

        private bool EnqueueOrWrite(
            string collectionName,
            Dictionary<string, object> document,
            string docId = null,
            bool mirrorLocally = true)
        {
            if (mirrorLocally)
            {
                MirrorDocumentForLocalReport(collectionName, document);
            }

            if (TryWriteNow(collectionName, document, docId))
            {
                return true;
            }

            lock (syncRoot)
            {
                if (pendingWrites.Count >= MaxPendingWrites)
                {
                    pendingWrites.Dequeue();
                }

                pendingWrites.Enqueue(new PendingWrite(collectionName, document, docId));
            }

            return false;
        }

        private void MirrorDocumentForLocalReport(string collectionName, IReadOnlyDictionary<string, object> document)
        {
            if (document == null || LocalDocumentRecorded == null)
            {
                return;
            }

            try
            {
                LocalDocumentRecorded(collectionName, document);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[FirestoreTelemetryWriter] Yerel rapor aynasi yazilamadi: " + ex.Message);
            }
        }

        private bool TryWriteNow(string collectionName, Dictionary<string, object> document, string docId = null)
        {
            if (document == null)
            {
                return false;
            }

            // Pending queue'dan flush edilen dokümanlar, oluştuklarında
            // participant context henüz hazır olmayabiliyordu. Yazımdan
            // hemen önce eksik metadata'ları emniyetli şekilde tamamla —
            // mevcut alanları override etme, sadece boşları doldur.
            EnsureBaseMetadata(document);

            object collectionReference;
            if (!TryResolveCollectionReference(collectionName, out collectionReference))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(docId))
            {
                if (documentMethod == null || setAsyncMethod == null)
                {
                    LogFirestoreUnavailable("DocumentReference.SetAsync bulunamadi.");
                    return false;
                }

                try
                {
                    object documentRef = documentMethod.Invoke(collectionReference, new object[] { docId });
                    if (documentRef == null)
                    {
                        return false;
                    }

                    object[] setArgs = setAsyncMethod.GetParameters().Length >= 2
                        ? new object[] { document, null }
                        : new object[] { document };
                    _ = setAsyncMethod.Invoke(documentRef, setArgs);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[FirestoreTelemetryWriter] Firestore SetAsync yazimi basarisiz: " + ex.Message);
                    CrashlyticsBridge.LogException(ex, "firestore_setasync_failed");
                    return false;
                }
            }

            MethodInfo addAsyncMethod = FindAddAsyncMethod(collectionReference.GetType());
            if (addAsyncMethod == null)
            {
                LogFirestoreUnavailable("CollectionReference.AddAsync bulunamadi.");
                return false;
            }

            try
            {
                _ = addAsyncMethod.Invoke(collectionReference, new object[] { document });
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[FirestoreTelemetryWriter] Firestore yazimi basarisiz: " + ex.Message);
                CrashlyticsBridge.LogException(ex, "firestore_write_failed");
                return false;
            }
        }

        private bool TryResolveCollectionReference(string collectionName, out object collectionReference)
        {
            collectionReference = null;

            if (RequiresParticipantScope(collectionName) && string.IsNullOrWhiteSpace(participantKey))
            {
                if (!participantScopeMissingLogged)
                {
                    participantScopeMissingLogged = true;
                    Debug.LogWarning("[FirestoreTelemetryWriter] Katilimci baglami yokken analytics root koleksiyona yazilmadi; yazi kuyrukta tutulacak.");
                }

                return false;
            }

            if (firestoreType == null || firestoreDefaultInstanceProperty == null || firestoreCollectionMethod == null)
            {
                LogFirestoreUnavailable("Firestore bilesenleri hazir degil.");
                return false;
            }

            object firestoreInstance;
            try
            {
                firestoreInstance = firestoreDefaultInstanceProperty.GetValue(null, null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[FirestoreTelemetryWriter] Firestore DefaultInstance alinamadi: " + ex.Message);
                CrashlyticsBridge.LogException(ex, "firestore_default_instance_failed");
                return false;
            }

            if (firestoreInstance == null)
            {
                return false;
            }

            try
            {
                string resolvedPath = ResolveCollectionPath(collectionName);
                collectionReference = firestoreCollectionMethod.Invoke(firestoreInstance, new object[] { resolvedPath });
                return collectionReference != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[FirestoreTelemetryWriter] Firestore collection olusturulamadi: " + ex.Message);
                CrashlyticsBridge.LogException(ex, "firestore_collection_resolve_failed");
                return false;
            }
        }

        // Mevcut Firestore şemasının sürümü. Yeni alanlar eklendiğinde
        // artırılır; eski okuyucular hangi sürümle yazıldığını görür.
        private const int CurrentSchemaVersion = 1;

        /// <summary>
        /// Pending queue'dan flush edilen veya çağrı sırasında metadata
        /// eksik kalmış dokümanlarda <c>katilimci</c>, <c>kurulum_id</c>,
        /// <c>oturum_id</c>, <c>schema_v</c>, <c>kayit_tipi</c> gibi temel
        /// alanları emniyetli şekilde tamamlar. Mevcut değerleri ASLA
        /// override etmez — yalnızca null/boş alanları doldurur. Path
        /// mantığına dokunmaz, sadece doküman içi alan tamamlamasıdır.
        /// </summary>
        private void EnsureBaseMetadata(Dictionary<string, object> document)
        {
            if (document == null) return;

            if (!document.ContainsKey("schema_v"))
            {
                document["schema_v"] = CurrentSchemaVersion;
            }

            if (!document.ContainsKey("kurulum_id") || IsEmptyValue(document["kurulum_id"]))
            {
                document["kurulum_id"] = string.IsNullOrWhiteSpace(installationId)
                    ? "unknown_installation"
                    : installationId;
            }

            if (!document.ContainsKey("oturum_id") || IsEmptyValue(document["oturum_id"]))
            {
                document["oturum_id"] = string.IsNullOrWhiteSpace(sessionId)
                    ? "unknown_session"
                    : sessionId;
            }

            if ((!document.ContainsKey("katilimci") || IsEmptyValue(document["katilimci"])) &&
                !string.IsNullOrWhiteSpace(participantKey))
            {
                document["katilimci"] = participantKey;
            }

            if (!document.ContainsKey("yazildi") || IsEmptyValue(document["yazildi"]))
            {
                document["yazildi"] = DateTime.UtcNow.ToString("O");
            }

            // son_guncelleme — fall-back yazılmaz. Bu alan iki yoldan birinde
            // damgalanır:
            //   • CreateBaseDocument zaten gerçek event'lerde "yazildi" ile
            //     birlikte ekler (yeni yazımlar her zaman taşır).
            //   • ApplyBaselineFields baseline placeholder'larında bu alanı
            //     bilerek kaldırır; bir gerçek event placeholder'ı SetAsync
            //     ile ezene kadar boş kalır.
            // EnsureBaseMetadata burada doldurmaya çalışırsa baseline
            // dokümanları "güncellenmiş gibi" görünür ve test/audit
            // semantiğini bozar. Bu yüzden hiçbir şey yapmıyoruz.
        }

        private static bool IsEmptyValue(object value)
        {
            if (value == null) return true;
            if (value is string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return true;
                // Pending queue oluşurken katılımcı/oturum henüz hazır
                // değilse "unknown_*" yer tutucusu yazılıyor. Flush sırasında
                // gerçek değer geldiğinde bunları "boş" sayıp gerçek değerle
                // override edebilelim.
                if (s == "unknown_participant" ||
                    s == "unknown_session" ||
                    s == "unknown_installation")
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Aynı ai_etkilesim docId'sine birden çok yazım geldiğinde değerlerin
        /// monoton birleşmesini sağlar. ai_panel_acildi_mi / ai_soru_soruldu_mu
        /// bayrakları bir kez true olduktan sonra false'a düşmez. ai_soru_adedi
        /// sayacı artar (en yüksek değer korunur — yeni sorular geldikçe
        /// büyür). ai_soru_turu son anlamlı (boş/panel_opened olmayan) değeri
        /// korur. AI metni / prompt / cevap hiçbir zaman tutulmaz; sadece
        /// özet bayraklar ve sayaçlar.
        /// </summary>
        private sealed class AiSummaryAccumulator
        {
            private bool panelOpened;
            private bool questionAsked;
            private int questionCount;
            private string lastMeaningfulQuestionType = string.Empty;
            private bool successFlag = true;
            private string lastErrorCode = string.Empty;

            public void Merge(IReadOnlyDictionary<string, object> incoming)
            {
                if (incoming == null) return;

                if (incoming.TryGetValue("ai_panel_acildi_mi", out object pv) && pv is bool pb && pb)
                {
                    panelOpened = true;
                }

                // Bu yazım gerçekten bir soru event'i mi yoksa sadece panel
                // açılışı mı? CreateAIInteractionDocument bunu doğrudan
                // ai_soru_soruldu_mu üzerinden bildiriyor. Sayaç YALNIZCA
                // soru event'lerinde artar; panel_opened sayacı değiştirmez.
                bool incomingIsQuestion = false;
                if (incoming.TryGetValue("ai_soru_soruldu_mu", out object qv) && qv is bool qb && qb)
                {
                    questionAsked = true;
                    incomingIsQuestion = true;
                }

                if (incomingIsQuestion)
                {
                    // Gelen yazımın taşıdığı sayıyı oku — şu anki kod yolu her
                    // gerçek soru event'i için 1 yazıyor, ileride toplu (örn.
                    // n soru) yazım eklenirse bu kod onu da doğru toplar.
                    int incomingCount = 0;
                    if (incoming.TryGetValue("ai_soru_adedi", out object cv))
                    {
                        if (cv is int ci) incomingCount = ci;
                        else if (cv is long cl) incomingCount = (int)cl;
                        else if (cv is float cf) incomingCount = (int)cf;
                        else if (cv is double cd) incomingCount = (int)cd;
                    }
                    // En az 1 soru artır (incomingCount 0 veya negatif ise
                    // bile bir soru event'iyiz, kayıp olmasın). Daha büyük
                    // toplu sayım gelirse onu kullan.
                    questionCount += Math.Max(1, incomingCount);
                }

                if (incoming.TryGetValue("ai_soru_turu", out object tv) && tv is string ts &&
                    !string.IsNullOrWhiteSpace(ts) &&
                    !string.Equals(ts, "panel_opened", StringComparison.OrdinalIgnoreCase))
                {
                    lastMeaningfulQuestionType = ts;
                }

                if (incoming.TryGetValue("ai_basarili_mi", out object sv) && sv is bool sb && !sb)
                {
                    // Bir kez başarısız olmuş bir panel artık başarısız sayılır.
                    successFlag = false;
                }

                if (incoming.TryGetValue("ai_hata_kodu", out object ev) && ev is string es &&
                    !string.IsNullOrWhiteSpace(es))
                {
                    lastErrorCode = es;
                }
            }

            public void WriteInto(Dictionary<string, object> document)
            {
                if (document == null) return;
                document["ai_panel_acildi_mi"] = panelOpened;
                document["ai_soru_soruldu_mu"] = questionAsked;
                document["ai_soru_adedi"] = questionCount;
                if (!string.IsNullOrEmpty(lastMeaningfulQuestionType))
                {
                    document["ai_soru_turu"] = lastMeaningfulQuestionType;
                }
                document["ai_basarili_mi"] = successFlag;
                if (!string.IsNullOrEmpty(lastErrorCode))
                {
                    document["ai_hata_kodu"] = lastErrorCode;
                }
            }
        }

        private Dictionary<string, object> CreateBaseDocument()
        {
            string resolvedInstallationId = string.IsNullOrWhiteSpace(installationId)
                ? "unknown_installation"
                : installationId;
            string resolvedSessionId = string.IsNullOrWhiteSpace(sessionId)
                ? "unknown_session"
                : sessionId;

            string nowIso = DateTime.UtcNow.ToString("O");
            // Tek veri modu: dokümanlar artık "veri_turu / kayit_tipi /
            // is_placeholder / baseline_profile_*" gibi kaynak etiketi
            // taşımıyor. Yeni katılımcılarda Firestore tek tip kullanıcı
            // verisi gibi görünür. Baseline seed sistemi devam eder
            // (deterministic docId üzerinden gerçek event ezme akışı
            // çalışır), ama dış görünür etiket basılmaz.
            var document = new Dictionary<string, object>
            {
                { "schema_v", CurrentSchemaVersion },
                { "kurulum_id", resolvedInstallationId },
                { "oturum_id", resolvedSessionId },
                { "yazildi", nowIso },
                { "son_guncelleme", nowIso }
            };

            if (!string.IsNullOrWhiteSpace(participantKey))
            {
                document["katilimci"] = participantKey;
            }

            return document;
        }

        private string ResolveCollectionPath(string collectionName)
        {
            if (string.IsNullOrWhiteSpace(participantKey))
            {
                return collectionName;
            }

            return "katilimcilar/" + participantKey + "/" + collectionName;
        }

        private static bool RequiresParticipantScope(string collectionName)
        {
            return string.Equals(collectionName, SessionSummaryCollection, StringComparison.Ordinal) ||
                   string.Equals(collectionName, ModuleProgressCollection, StringComparison.Ordinal) ||
                   string.Equals(collectionName, TaskResultCollection, StringComparison.Ordinal) ||
                   string.Equals(collectionName, QuizResultCollection, StringComparison.Ordinal) ||
                   string.Equals(collectionName, AiInteractionCollection, StringComparison.Ordinal) ||
                   string.Equals(collectionName, TriageResultCollection, StringComparison.Ordinal) ||
                   string.Equals(collectionName, DetailedEventCollection, StringComparison.Ordinal);
        }

        private static void CopyParameter(
            IReadOnlyDictionary<string, object> source,
            IDictionary<string, object> destination,
            string sourceKey,
            string destinationKey)
        {
            if (source == null || destination == null || string.IsNullOrWhiteSpace(sourceKey) || string.IsNullOrWhiteSpace(destinationKey))
            {
                return;
            }

            if (!source.TryGetValue(sourceKey, out object value) || value == null)
            {
                return;
            }

            destination[destinationKey] = value;
        }

        private void LogFirestoreUnavailable(string reason)
        {
            if (firestoreUnavailableLogged)
            {
                return;
            }

            firestoreUnavailableLogged = true;
            string message = string.IsNullOrWhiteSpace(reason)
                ? "[FirestoreTelemetryWriter] Firestore baglantisi kullanilamiyor."
                : "[FirestoreTelemetryWriter] " + reason.Trim();
            Debug.LogWarning(message);
            CrashlyticsBridge.LogMessage(message);
        }

        private static MethodInfo FindAddAsyncMethod(Type collectionReferenceType)
        {
            if (collectionReferenceType == null)
            {
                return null;
            }

            MethodInfo[] methods = collectionReferenceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (!string.Equals(method.Name, "AddAsync", StringComparison.Ordinal))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 1)
                {
                    return method;
                }
            }

            return null;
        }

        private static MethodInfo FindSetAsyncMethod(Type documentReferenceType)
        {
            if (documentReferenceType == null)
            {
                return null;
            }

            MethodInfo[] methods = documentReferenceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (!string.Equals(method.Name, "SetAsync", StringComparison.Ordinal))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                // Tek parametreli overload (eski SDK) ya da ikinci parametresi optional olan overload (SDK 13.x+).
                if (parameters.Length == 1)
                {
                    return method;
                }

                if (parameters.Length == 2 && parameters[1].IsOptional)
                {
                    return method;
                }
            }

            return null;
        }

        private readonly struct PendingWrite
        {
            public PendingWrite(string collection, Dictionary<string, object> document, string docId = null)
            {
                this.collection = collection;
                this.document = document ?? new Dictionary<string, object>();
                this.docId = string.IsNullOrWhiteSpace(docId) ? null : docId;
            }

            public readonly string collection;
            public readonly Dictionary<string, object> document;
            public readonly string docId;
        }
    }

    public static class CrashlyticsBridge
    {
        private static readonly Type CrashlyticsType = Type.GetType("Firebase.Crashlytics.Crashlytics, Firebase.Crashlytics", false);
        private static readonly MethodInfo LogMethod = CrashlyticsType != null
            ? CrashlyticsType.GetMethod("Log", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null)
            : null;
        private static readonly MethodInfo LogExceptionMethod = CrashlyticsType != null
            ? CrashlyticsType.GetMethod("LogException", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Exception) }, null)
            : null;

        public static void LogMessage(string message)
        {
            if (LogMethod == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            try
            {
                LogMethod.Invoke(null, new object[] { message.Trim() });
            }
            catch
            {
            }
        }

        public static void LogException(Exception exception, string context = null)
        {
            if (!string.IsNullOrWhiteSpace(context))
            {
                LogMessage("[Analytics] " + context.Trim());
            }

            if (LogExceptionMethod == null || exception == null)
            {
                return;
            }

            try
            {
                LogExceptionMethod.Invoke(null, new object[] { exception });
            }
            catch
            {
            }
        }
    }
}
