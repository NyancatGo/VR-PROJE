using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainingAnalytics
{
    public sealed class SessionTracker
    {
        private const string DefaultInstallationIdPrefsKey = "training.analytics.installation_id";

        private readonly Func<string, string> loadValue;
        private readonly Action<string, string> saveValue;
        private readonly Func<DateTime> utcNowProvider;
        private readonly Func<double> elapsedSecondsProvider;
        private readonly string installationIdPrefsKey;
        private readonly Dictionary<string, double> activeTimers = new Dictionary<string, double>();
        private readonly Dictionary<string, QuizProgressState> quizStates = new Dictionary<string, QuizProgressState>();
        private readonly Dictionary<string, HashSet<string>> contentSeenByModule = new Dictionary<string, HashSet<string>>();
        private readonly Dictionary<string, HashSet<string>> completionSets = new Dictionary<string, HashSet<string>>();
        private readonly HashSet<string> uniqueEventKeys = new HashSet<string>();

        private int totalDispatchedEvents;

        public SessionTracker(
            Func<string, string> loadValue = null,
            Action<string, string> saveValue = null,
            Func<DateTime> utcNowProvider = null,
            Func<double> elapsedSecondsProvider = null,
            string installationIdPrefsKey = DefaultInstallationIdPrefsKey)
        {
            this.loadValue = loadValue ?? LoadFromPlayerPrefs;
            this.saveValue = saveValue ?? SaveToPlayerPrefs;
            this.utcNowProvider = utcNowProvider ?? (() => DateTime.UtcNow);
            this.elapsedSecondsProvider = elapsedSecondsProvider ?? (() => Time.realtimeSinceStartupAsDouble);
            this.installationIdPrefsKey = string.IsNullOrWhiteSpace(installationIdPrefsKey)
                ? DefaultInstallationIdPrefsKey
                : installationIdPrefsKey.Trim();

            InstallationId = EnsureInstallationId();
            SessionId = Guid.NewGuid().ToString("N");
            SessionStartedUtc = this.utcNowProvider();
        }

        public string InstallationId { get; }
        public string SessionId { get; }
        public DateTime SessionStartedUtc { get; }

        public Dictionary<string, object> BuildCommonParameters()
        {
            return new Dictionary<string, object>
            {
                { AnalyticsParams.InstallationId, InstallationId },
                { AnalyticsParams.SessionId, SessionId },
                { AnalyticsParams.BuildVersion, ResolveBuildVersion() }
            };
        }

        public SessionSummary BuildSessionSummary()
        {
            return new SessionSummary
            {
                installationId = InstallationId,
                sessionId = SessionId,
                sessionStartedUtc = SessionStartedUtc.ToString("O"),
                sessionDurationSeconds = (float)Math.Max(0d, elapsedSecondsProvider()),
                totalEvents = totalDispatchedEvents
            };
        }

        public void RegisterDispatchedEvent()
        {
            totalDispatchedEvents++;
        }

        public bool TryRegisterUniqueEvent(string duplicateKey)
        {
            string resolvedKey = AnalyticsService.SanitizeToken(duplicateKey, 96, "duplicate");
            return uniqueEventKeys.Add(resolvedKey);
        }

        public int RegisterCompletion(string setKey, string itemId)
        {
            string resolvedSetKey = AnalyticsService.SanitizeToken(setKey, 64, "set");
            if (!completionSets.TryGetValue(resolvedSetKey, out HashSet<string> completionSet))
            {
                completionSet = new HashSet<string>();
                completionSets.Add(resolvedSetKey, completionSet);
            }

            completionSet.Add(AnalyticsService.SanitizeToken(itemId, 64, "item"));
            return completionSet.Count;
        }

        public ModuleProgressSummary BeginModule(string moduleId, string moduleName)
        {
            BeginTimer(GetModuleTimerKey(moduleId), true);
            return new ModuleProgressSummary
            {
                moduleId = moduleId,
                moduleName = moduleName,
                completed = false,
                durationSeconds = 0f
            };
        }

        public ModuleProgressSummary CompleteModule(string moduleId, string moduleName)
        {
            return new ModuleProgressSummary
            {
                moduleId = moduleId,
                moduleName = moduleName,
                completed = true,
                durationSeconds = ConsumeDuration(GetModuleTimerKey(moduleId))
            };
        }

        public void BeginScenario(string moduleId, string scenarioId)
        {
            BeginTimer(GetScenarioTimerKey(moduleId, scenarioId), false);
        }

        public float CompleteScenario(string moduleId, string scenarioId)
        {
            return ConsumeDuration(GetScenarioTimerKey(moduleId, scenarioId));
        }

        public TaskResult StartTask(
            string moduleId,
            string moduleName,
            string taskId,
            string taskName,
            string scenarioId = null,
            string targetId = null,
            string targetName = null)
        {
            BeginTimer(GetTaskTimerKey(moduleId, taskId, targetId), true);
            return new TaskResult
            {
                moduleId = moduleId,
                moduleName = moduleName,
                scenarioId = scenarioId,
                taskId = taskId,
                taskName = taskName,
                targetId = targetId,
                targetName = targetName,
                success = true
            };
        }

        public TaskResult GetTaskProgress(
            string moduleId,
            string moduleName,
            string taskId,
            string taskName,
            float progress,
            int completedCount,
            int totalCount,
            string scenarioId = null,
            string targetId = null,
            string targetName = null)
        {
            return BuildTaskResult(
                moduleId,
                moduleName,
                scenarioId,
                taskId,
                taskName,
                targetId,
                targetName,
                progress,
                completedCount,
                totalCount,
                PeekDuration(GetTaskTimerKey(moduleId, taskId, targetId)),
                true);
        }

        public TaskResult CompleteTask(
            string moduleId,
            string moduleName,
            string taskId,
            string taskName,
            int completedCount,
            int totalCount,
            string scenarioId = null,
            string targetId = null,
            string targetName = null)
        {
            return BuildTaskResult(
                moduleId,
                moduleName,
                scenarioId,
                taskId,
                taskName,
                targetId,
                targetName,
                1f,
                completedCount,
                totalCount,
                ConsumeDuration(GetTaskTimerKey(moduleId, taskId, targetId)),
                true);
        }

        public TaskResult FailTask(
            string moduleId,
            string moduleName,
            string taskId,
            string taskName,
            string scenarioId = null,
            string targetId = null,
            string targetName = null)
        {
            return BuildTaskResult(
                moduleId,
                moduleName,
                scenarioId,
                taskId,
                taskName,
                targetId,
                targetName,
                0f,
                0,
                0,
                ConsumeDuration(GetTaskTimerKey(moduleId, taskId, targetId)),
                false);
        }

        public AIInteractionResult OpenAiPanel(string moduleId, string moduleName, string panelId, string panelName)
        {
            BeginTimer(GetAiPanelTimerKey(moduleId, panelId), true);
            return new AIInteractionResult
            {
                moduleId = moduleId,
                moduleName = moduleName,
                panelId = panelId,
                panelName = panelName,
                durationSeconds = 0f
            };
        }

        public AIInteractionResult BuildAiInteraction(
            string moduleId,
            string moduleName,
            string panelId,
            string panelName,
            string questionType,
            bool consumeDuration)
        {
            float duration = consumeDuration
                ? ConsumeDuration(GetAiPanelTimerKey(moduleId, panelId))
                : PeekDuration(GetAiPanelTimerKey(moduleId, panelId));

            return new AIInteractionResult
            {
                moduleId = moduleId,
                moduleName = moduleName,
                panelId = panelId,
                panelName = panelName,
                questionType = questionType,
                durationSeconds = duration
            };
        }

        public QuizResult StartQuiz(
            string moduleId,
            string moduleName,
            string quizId,
            string quizName,
            int totalQuestionCount)
        {
            string quizStateKey = GetQuizStateKey(moduleId, quizId);
            quizStates[quizStateKey] = new QuizProgressState
            {
                quizName = quizName,
                totalQuestionCount = Mathf.Max(0, totalQuestionCount)
            };

            BeginTimer(GetQuizTimerKey(moduleId, quizId), true);

            return new QuizResult
            {
                moduleId = moduleId,
                moduleName = moduleName,
                quizId = quizId,
                quizName = quizName,
                totalQuestionCount = Mathf.Max(0, totalQuestionCount)
            };
        }

        public QuizResult RecordQuizAnswer(
            string moduleId,
            string moduleName,
            string quizId,
            string quizName,
            int questionIndex,
            int selectedIndex,
            int correctIndex,
            bool isCorrect)
        {
            string quizStateKey = GetQuizStateKey(moduleId, quizId);
            if (!quizStates.TryGetValue(quizStateKey, out QuizProgressState state))
            {
                state = new QuizProgressState
                {
                    quizName = quizName
                };
                quizStates.Add(quizStateKey, state);
            }

            state.answeredCount++;
            if (isCorrect)
            {
                state.correctCount++;
            }

            return new QuizResult
            {
                moduleId = moduleId,
                moduleName = moduleName,
                quizId = quizId,
                quizName = string.IsNullOrWhiteSpace(state.quizName) ? quizName : state.quizName,
                answeredCount = state.answeredCount,
                totalQuestionCount = Mathf.Max(state.totalQuestionCount, questionIndex),
                correctCount = state.correctCount,
                questionIndex = questionIndex,
                selectedAnswerIndex = selectedIndex,
                correctAnswerIndex = correctIndex,
                isCorrect = isCorrect,
                durationSeconds = PeekDuration(GetQuizTimerKey(moduleId, quizId)),
                scorePercentage = CalculatePercentage(state.correctCount, Mathf.Max(state.totalQuestionCount, questionIndex))
            };
        }

        public QuizResult CompleteQuiz(
            string moduleId,
            string moduleName,
            string quizId,
            string quizName,
            int totalQuestionCount,
            int correctCount)
        {
            string quizStateKey = GetQuizStateKey(moduleId, quizId);
            int answeredCount = totalQuestionCount;

            if (quizStates.TryGetValue(quizStateKey, out QuizProgressState state))
            {
                answeredCount = Mathf.Max(state.answeredCount, totalQuestionCount);
                correctCount = Mathf.Max(state.correctCount, correctCount);
                quizStates.Remove(quizStateKey);
            }

            return new QuizResult
            {
                moduleId = moduleId,
                moduleName = moduleName,
                quizId = quizId,
                quizName = quizName,
                answeredCount = answeredCount,
                totalQuestionCount = totalQuestionCount,
                correctCount = correctCount,
                durationSeconds = ConsumeDuration(GetQuizTimerKey(moduleId, quizId)),
                completed = true,
                scorePercentage = CalculatePercentage(correctCount, totalQuestionCount)
            };
        }

        public LearningContentResult RecordLearningContentOpen(
            string moduleId,
            string moduleName,
            string contentId,
            string contentName,
            int expectedTotalCount,
            bool isInfographic)
        {
            string contentSetKey = AnalyticsService.SanitizeToken(moduleId, 32, "module");
            if (!contentSeenByModule.TryGetValue(contentSetKey, out HashSet<string> openedContent))
            {
                openedContent = new HashSet<string>();
                contentSeenByModule.Add(contentSetKey, openedContent);
            }

            openedContent.Add(AnalyticsService.SanitizeToken(contentId, 64, "content"));

            return new LearningContentResult
            {
                moduleId = moduleId,
                moduleName = moduleName,
                contentId = contentId,
                contentName = contentName,
                openedCount = openedContent.Count,
                totalCount = Mathf.Max(0, expectedTotalCount),
                completed = expectedTotalCount > 0 && openedContent.Count >= expectedTotalCount,
                isInfographic = isInfographic
            };
        }

        private TaskResult BuildTaskResult(
            string moduleId,
            string moduleName,
            string scenarioId,
            string taskId,
            string taskName,
            string targetId,
            string targetName,
            float progress,
            int completedCount,
            int totalCount,
            float durationSeconds,
            bool success)
        {
            return new TaskResult
            {
                moduleId = moduleId,
                moduleName = moduleName,
                scenarioId = scenarioId,
                taskId = taskId,
                taskName = taskName,
                targetId = targetId,
                targetName = targetName,
                progress = Mathf.Clamp01(progress),
                completedCount = Mathf.Max(0, completedCount),
                totalCount = Mathf.Max(0, totalCount),
                durationSeconds = Mathf.Max(0f, durationSeconds),
                success = success
            };
        }

        private void BeginTimer(string timerKey, bool overwriteExisting)
        {
            string resolvedTimerKey = AnalyticsService.SanitizeToken(timerKey, 96, "timer");
            if (!overwriteExisting && activeTimers.ContainsKey(resolvedTimerKey))
            {
                return;
            }

            activeTimers[resolvedTimerKey] = elapsedSecondsProvider();
        }

        private float PeekDuration(string timerKey)
        {
            string resolvedTimerKey = AnalyticsService.SanitizeToken(timerKey, 96, "timer");
            if (!activeTimers.TryGetValue(resolvedTimerKey, out double startTime))
            {
                return 0f;
            }

            return (float)Math.Max(0d, elapsedSecondsProvider() - startTime);
        }

        private float ConsumeDuration(string timerKey)
        {
            string resolvedTimerKey = AnalyticsService.SanitizeToken(timerKey, 96, "timer");
            if (!activeTimers.TryGetValue(resolvedTimerKey, out double startTime))
            {
                return 0f;
            }

            activeTimers.Remove(resolvedTimerKey);
            return (float)Math.Max(0d, elapsedSecondsProvider() - startTime);
        }

        private string EnsureInstallationId()
        {
            string storedId = loadValue(installationIdPrefsKey);
            if (!string.IsNullOrWhiteSpace(storedId))
            {
                return storedId.Trim();
            }

            string generatedId = Guid.NewGuid().ToString("N");
            saveValue(installationIdPrefsKey, generatedId);
            return generatedId;
        }

        private static string ResolveBuildVersion()
        {
            return string.IsNullOrWhiteSpace(Application.version)
                ? "unknown_version"
                : Application.version.Trim();
        }

        private static string LoadFromPlayerPrefs(string key)
        {
            return PlayerPrefs.GetString(key, string.Empty);
        }

        private static void SaveToPlayerPrefs(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        private static float CalculatePercentage(int numerator, int denominator)
        {
            if (denominator <= 0)
            {
                return 0f;
            }

            return (float)Math.Round((double)numerator * 100d / denominator, 2);
        }

        private static string GetModuleTimerKey(string moduleId)
        {
            return "module_" + moduleId;
        }

        private static string GetScenarioTimerKey(string moduleId, string scenarioId)
        {
            return "scenario_" + moduleId + "_" + scenarioId;
        }

        private static string GetTaskTimerKey(string moduleId, string taskId, string targetId)
        {
            return "task_" + moduleId + "_" + taskId + "_" + targetId;
        }

        private static string GetAiPanelTimerKey(string moduleId, string panelId)
        {
            return "ai_panel_" + moduleId + "_" + panelId;
        }

        private static string GetQuizTimerKey(string moduleId, string quizId)
        {
            return "quiz_" + moduleId + "_" + quizId;
        }

        private static string GetQuizStateKey(string moduleId, string quizId)
        {
            return "quiz_state_" + moduleId + "_" + quizId;
        }

        private sealed class QuizProgressState
        {
            public string quizName;
            public int totalQuestionCount;
            public int answeredCount;
            public int correctCount;
        }
    }
}
