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
        private const string SessionSummaryCollection = "training_session_summaries";
        private const string ModuleProgressCollection = "training_module_progress";
        private const string TaskResultCollection = "training_task_results";
        private const string QuizResultCollection = "training_quiz_results";
        private const string AiInteractionCollection = "training_ai_interactions";
        private const string TriageResultCollection = "training_triage_results";
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
            this.participantKey = string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
            this.participantFullName = string.IsNullOrWhiteSpace(fullName) ? string.Empty : fullName.Trim();
        }

        public bool WriteParticipantProfile()
        {
            if (string.IsNullOrWhiteSpace(participantKey))
            {
                return false;
            }

            var profileDoc = new Dictionary<string, object>
            {
                { "participant_key", participantKey },
                { "full_name", participantFullName },
                { "last_seen_utc", DateTime.UtcNow.ToString("O") },
                { "installation_id", string.IsNullOrWhiteSpace(installationId) ? "unknown" : installationId }
            };

            MirrorDocumentForLocalReport("participants", profileDoc);

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
                object collectionRef = firestoreCollectionMethod.Invoke(firestoreInstance, new object[] { "participants" });
                if (collectionRef == null) return false;

                object documentRef = documentMethod.Invoke(collectionRef, new object[] { participantKey });
                if (documentRef == null) return false;

                setAsyncMethod.Invoke(documentRef, new object[] { profileDoc });
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
            document["session_started_utc"] = summary.sessionStartedUtc;
            document["session_duration_seconds"] = Math.Max(0f, summary.sessionDurationSeconds);
            document["total_events"] = Math.Max(0, summary.totalEvents);
            return EnqueueOrWrite(SessionSummaryCollection, document);
        }

        public bool WriteModuleProgress(ModuleProgressSummary progress)
        {
            Dictionary<string, object> document = CreateBaseDocument();
            document["module_id"] = progress.moduleId;
            document["module_name"] = progress.moduleName;
            document["completed"] = progress.completed;
            document["duration_seconds"] = Math.Max(0f, progress.durationSeconds);
            return EnqueueOrWrite(ModuleProgressCollection, document);
        }

        public bool WriteTaskResult(TaskResult taskResult)
        {
            Dictionary<string, object> document = CreateBaseDocument();
            document["module_id"] = taskResult.moduleId;
            document["module_name"] = taskResult.moduleName;
            document["scenario_id"] = taskResult.scenarioId;
            document["task_id"] = taskResult.taskId;
            document["task_name"] = taskResult.taskName;
            document["target_id"] = taskResult.targetId;
            document["target_name"] = taskResult.targetName;
            document["task_progress"] = Mathf.Clamp01(taskResult.progress);
            document["completed_count"] = Math.Max(0, taskResult.completedCount);
            document["total_count"] = Math.Max(0, taskResult.totalCount);
            document["duration_seconds"] = Math.Max(0f, taskResult.durationSeconds);
            document["success"] = taskResult.success;
            return EnqueueOrWrite(TaskResultCollection, document);
        }

        public bool WriteQuizResult(QuizResult quizResult)
        {
            Dictionary<string, object> document = CreateBaseDocument();
            document["module_id"] = quizResult.moduleId;
            document["module_name"] = quizResult.moduleName;
            document["quiz_id"] = quizResult.quizId;
            document["quiz_name"] = quizResult.quizName;
            document["total_question_count"] = Math.Max(0, quizResult.totalQuestionCount);
            document["answered_count"] = Math.Max(0, quizResult.answeredCount);
            document["correct_count"] = Math.Max(0, quizResult.correctCount);
            document["question_index"] = Math.Max(0, quizResult.questionIndex);
            document["selected_answer_index"] = Math.Max(-1, quizResult.selectedAnswerIndex);
            document["correct_answer_index"] = Math.Max(-1, quizResult.correctAnswerIndex);
            document["is_correct"] = quizResult.isCorrect;
            document["completed"] = quizResult.completed;
            document["duration_seconds"] = Math.Max(0f, quizResult.durationSeconds);
            document["score_percentage"] = Math.Max(0f, quizResult.scorePercentage);
            return EnqueueOrWrite(QuizResultCollection, document);
        }

        public bool WriteAIInteraction(AIInteractionResult interaction)
        {
            Dictionary<string, object> document = CreateBaseDocument();
            document["module_id"] = interaction.moduleId;
            document["module_name"] = interaction.moduleName;
            document["panel_id"] = interaction.panelId;
            document["panel_name"] = interaction.panelName;
            document["ai_question_type"] = interaction.questionType;
            document["duration_seconds"] = Math.Max(0f, interaction.durationSeconds);
            return EnqueueOrWrite(AiInteractionCollection, document);
        }

        public bool WriteTriageResult(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            Dictionary<string, object> document = CreateBaseDocument();
            document["event_name"] = AnalyticsService.SanitizeToken(eventName, 40, "event");

            CopyParameter(parameters, document, AnalyticsParams.ModuleId, "module_id");
            CopyParameter(parameters, document, AnalyticsParams.ModuleName, "module_name");
            CopyParameter(parameters, document, AnalyticsParams.ScenarioId, "scenario_id");
            CopyParameter(parameters, document, AnalyticsParams.ScenarioName, "scenario_name");
            CopyParameter(parameters, document, AnalyticsParams.VictimId, "victim_id");
            CopyParameter(parameters, document, AnalyticsParams.VictimName, "victim_name");
            CopyParameter(parameters, document, AnalyticsParams.AssignedTriage, "assigned_triage");
            CopyParameter(parameters, document, AnalyticsParams.ActualTriage, "actual_triage");
            CopyParameter(parameters, document, AnalyticsParams.IsCorrect, "is_correct");
            CopyParameter(parameters, document, AnalyticsParams.ScoreValue, "score_value");
            CopyParameter(parameters, document, AnalyticsParams.ScorePercent, "score_percent");

            return EnqueueOrWrite(TriageResultCollection, document);
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

                if (TryWriteNow(pendingWrite.collection, pendingWrite.document))
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

        private bool EnqueueOrWrite(string collectionName, Dictionary<string, object> document)
        {
            MirrorDocumentForLocalReport(collectionName, document);

            if (TryWriteNow(collectionName, document))
            {
                return true;
            }

            lock (syncRoot)
            {
                if (pendingWrites.Count >= MaxPendingWrites)
                {
                    pendingWrites.Dequeue();
                }

                pendingWrites.Enqueue(new PendingWrite(collectionName, document));
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

        private bool TryWriteNow(string collectionName, Dictionary<string, object> document)
        {
            if (document == null)
            {
                return false;
            }

            object collectionReference;
            if (!TryResolveCollectionReference(collectionName, out collectionReference))
            {
                return false;
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

        private Dictionary<string, object> CreateBaseDocument()
        {
            string resolvedInstallationId = string.IsNullOrWhiteSpace(installationId)
                ? "unknown_installation"
                : installationId;
            string resolvedSessionId = string.IsNullOrWhiteSpace(sessionId)
                ? "unknown_session"
                : sessionId;

            var document = new Dictionary<string, object>
            {
                { "installation_id", resolvedInstallationId },
                { "session_id", resolvedSessionId },
                { "written_utc", DateTime.UtcNow.ToString("O") }
            };

            if (!string.IsNullOrWhiteSpace(participantKey))
            {
                document["participant_key"] = participantKey;
            }

            return document;
        }

        private string ResolveCollectionPath(string collectionName)
        {
            if (string.IsNullOrWhiteSpace(participantKey))
            {
                return collectionName;
            }

            return "participants/" + participantKey + "/" + collectionName;
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
                if (parameters.Length == 1)
                {
                    return method;
                }
            }

            return null;
        }

        private readonly struct PendingWrite
        {
            public PendingWrite(string collection, Dictionary<string, object> document)
            {
                this.collection = collection;
                this.document = document ?? new Dictionary<string, object>();
            }

            public readonly string collection;
            public readonly Dictionary<string, object> document;
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
