using System.Collections.Generic;
using UnityEngine;

namespace TrainingAnalytics
{
    public static class TrainingAnalyticsFacade
    {
        public const string Module1Id = "module_1";
        public const string Module1Name = "Modul 1";
        public const string Module2Id = "module_2";
        public const string Module2Name = "Modul 2";
        public const string Module3Id = "module_3";
        public const string Module3Name = "Modul 3";
        public const string Module4Id = "module_4";
        public const string Module4Name = "Modul 4";

        public const string Module2ScenarioId = "first_aid_rescue";
        public const string Module2ScenarioName = "Ilk Yardim Kurtarma Senaryosu";
        public const string Module3ScenarioId = "hospital_triage";
        public const string Module3ScenarioName = "Hastane Triyaj Senaryosu";

        private static AnalyticsService Service => AnalyticsService.EnsureInitializedSingleton();

        public static void OnModuleEntered(string moduleId, string moduleName, IDictionary<string, object> extraParameters = null)
        {
            Service.SessionTracker.BeginModule(moduleId, moduleName);
            TrackEvent(AnalyticsEventNames.ModuleEntered, moduleId, moduleName, extraParameters);
        }

        public static void OnModuleCompleted(string moduleId, string moduleName, IDictionary<string, object> extraParameters = null)
        {
            ModuleProgressSummary summary = Service.SessionTracker.CompleteModule(moduleId, moduleName);
            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.DurationSeconds] = summary.durationSeconds;
            TrackEvent(AnalyticsEventNames.ModuleCompleted, moduleId, moduleName, parameters);
        }

        public static void OnContentOpened(
            string moduleId,
            string moduleName,
            string contentId,
            string contentName,
            int expectedTotalCount = 0,
            IDictionary<string, object> extraParameters = null)
        {
            LearningContentResult result = Service.SessionTracker.RecordLearningContentOpen(
                moduleId,
                moduleName,
                contentId,
                contentName,
                expectedTotalCount,
                false);

            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.ContentId] = contentId;
            parameters[AnalyticsParams.ContentName] = contentName;
            parameters[AnalyticsParams.OpenedCount] = result.openedCount;
            parameters[AnalyticsParams.TotalCount] = result.totalCount;
            TrackEvent(AnalyticsEventNames.ContentOpened, moduleId, moduleName, parameters);
        }

        public static void OnVideoStarted(
            string moduleId,
            string moduleName,
            string contentId,
            string contentName,
            IDictionary<string, object> extraParameters = null)
        {
            Service.SessionTracker.StartTask(moduleId, moduleName, contentId, contentName, targetId: contentId, targetName: contentName);
            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.ContentId] = contentId;
            parameters[AnalyticsParams.ContentName] = contentName;
            parameters[AnalyticsParams.ContentType] = "video";
            TrackEvent(AnalyticsEventNames.VideoStarted, moduleId, moduleName, parameters);
        }

        public static void OnVideoProgress(
            string moduleId,
            string moduleName,
            string contentId,
            string contentName,
            float progress,
            IDictionary<string, object> extraParameters = null)
        {
            TaskResult result = Service.SessionTracker.GetTaskProgress(
                moduleId,
                moduleName,
                contentId,
                contentName,
                progress,
                Mathf.RoundToInt(progress * 100f),
                100,
                targetId: contentId,
                targetName: contentName);

            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.ContentId] = contentId;
            parameters[AnalyticsParams.ContentName] = contentName;
            parameters[AnalyticsParams.ContentType] = "video";
            parameters[AnalyticsParams.TaskProgress] = result.progress;
            parameters[AnalyticsParams.DurationSeconds] = result.durationSeconds;
            TrackEvent(AnalyticsEventNames.VideoProgress, moduleId, moduleName, parameters);
        }

        public static void OnVideoCompleted(
            string moduleId,
            string moduleName,
            string contentId,
            string contentName,
            IDictionary<string, object> extraParameters = null)
        {
            TaskResult result = Service.SessionTracker.CompleteTask(
                moduleId,
                moduleName,
                contentId,
                contentName,
                1,
                1,
                targetId: contentId,
                targetName: contentName);

            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.ContentId] = contentId;
            parameters[AnalyticsParams.ContentName] = contentName;
            parameters[AnalyticsParams.ContentType] = "video";
            parameters[AnalyticsParams.DurationSeconds] = result.durationSeconds;
            TrackEvent(AnalyticsEventNames.VideoCompleted, moduleId, moduleName, parameters);
        }

        public static void OnInfographicOpened(
            string moduleId,
            string moduleName,
            string infographicId,
            string infographicName,
            IDictionary<string, object> extraParameters = null)
        {
            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.ContentId] = infographicId;
            parameters[AnalyticsParams.ContentName] = infographicName;
            parameters[AnalyticsParams.ContentType] = "infographic";
            TrackEvent(AnalyticsEventNames.InfographicOpened, moduleId, moduleName, parameters);
        }

        public static void OnLearningContentCompleted(
            string moduleId,
            string moduleName,
            string contentGroupId,
            string contentGroupName,
            int completedCount,
            int totalCount,
            IDictionary<string, object> extraParameters = null)
        {
            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.ContentGroupId] = contentGroupId;
            parameters[AnalyticsParams.ContentGroupName] = contentGroupName;
            parameters[AnalyticsParams.CompletedCount] = completedCount;
            parameters[AnalyticsParams.TotalCount] = totalCount;
            TrackEvent(
                AnalyticsEventNames.LearningContentCompleted,
                moduleId,
                moduleName,
                parameters,
                true,
                moduleId + "_learning_completed_" + contentGroupId);
        }

        public static void OnTaskStarted(
            string moduleId,
            string moduleName,
            string taskId,
            string taskName,
            IDictionary<string, object> extraParameters = null)
        {
            Service.SessionTracker.StartTask(moduleId, moduleName, taskId, taskName);
            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.TaskId] = taskId;
            parameters[AnalyticsParams.TaskName] = taskName;
            TrackEvent(AnalyticsEventNames.TaskStarted, moduleId, moduleName, parameters);
        }

        public static void OnTaskProgress(
            string moduleId,
            string moduleName,
            string taskId,
            string taskName,
            float progress,
            IDictionary<string, object> extraParameters = null)
        {
            int completedCount = ExtractInteger(extraParameters, AnalyticsParams.CompletedCount);
            int totalCount = ExtractInteger(extraParameters, AnalyticsParams.TotalCount);
            string scenarioId = ExtractString(extraParameters, AnalyticsParams.ScenarioId);
            string targetId = ExtractString(extraParameters, AnalyticsParams.VictimId);
            string targetName = ExtractString(extraParameters, AnalyticsParams.VictimName);

            TaskResult result = Service.SessionTracker.GetTaskProgress(
                moduleId,
                moduleName,
                taskId,
                taskName,
                progress,
                completedCount,
                totalCount,
                scenarioId,
                targetId,
                targetName);

            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.TaskId] = taskId;
            parameters[AnalyticsParams.TaskName] = taskName;
            parameters[AnalyticsParams.TaskProgress] = result.progress;
            parameters[AnalyticsParams.DurationSeconds] = result.durationSeconds;
            TrackEvent(AnalyticsEventNames.TaskProgress, moduleId, moduleName, parameters);
        }

        public static void OnTaskFailed(
            string moduleId,
            string moduleName,
            string taskId,
            string taskName,
            IDictionary<string, object> extraParameters = null)
        {
            TaskResult result = Service.SessionTracker.FailTask(
                moduleId,
                moduleName,
                taskId,
                taskName,
                ExtractString(extraParameters, AnalyticsParams.ScenarioId),
                ExtractString(extraParameters, AnalyticsParams.VictimId),
                ExtractString(extraParameters, AnalyticsParams.VictimName));

            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.TaskId] = taskId;
            parameters[AnalyticsParams.TaskName] = taskName;
            parameters[AnalyticsParams.TaskStatus] = "failed";
            parameters[AnalyticsParams.DurationSeconds] = result.durationSeconds;
            TrackEvent(AnalyticsEventNames.TaskFailed, moduleId, moduleName, parameters);
        }

        public static void OnTaskCompleted(
            string moduleId,
            string moduleName,
            string taskId,
            string taskName,
            IDictionary<string, object> extraParameters = null)
        {
            int completedCount = ExtractInteger(extraParameters, AnalyticsParams.CompletedCount, 1);
            int totalCount = ExtractInteger(extraParameters, AnalyticsParams.TotalCount, 1);
            string scenarioId = ExtractString(extraParameters, AnalyticsParams.ScenarioId);
            string targetId = ExtractString(extraParameters, AnalyticsParams.VictimId);
            string targetName = ExtractString(extraParameters, AnalyticsParams.VictimName);

            TaskResult result = Service.SessionTracker.CompleteTask(
                moduleId,
                moduleName,
                taskId,
                taskName,
                completedCount,
                totalCount,
                scenarioId,
                targetId,
                targetName);

            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.TaskId] = taskId;
            parameters[AnalyticsParams.TaskName] = taskName;
            parameters[AnalyticsParams.TaskStatus] = "completed";
            parameters[AnalyticsParams.DurationSeconds] = result.durationSeconds;
            TrackEvent(AnalyticsEventNames.TaskCompleted, moduleId, moduleName, parameters);
        }

        public static void OnHelpRequested(
            string moduleId,
            string moduleName,
            string helpContext,
            IDictionary<string, object> extraParameters = null)
        {
            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.HelpContext] = helpContext;
            TrackEvent(AnalyticsEventNames.HelpRequested, moduleId, moduleName, parameters);
        }

        public static void OnTriageStarted(
            string moduleId,
            string moduleName,
            string scenarioId,
            string scenarioName,
            IDictionary<string, object> extraParameters = null)
        {
            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.ScenarioId] = scenarioId;
            parameters[AnalyticsParams.ScenarioName] = scenarioName;
            TrackEvent(AnalyticsEventNames.TriageStarted, moduleId, moduleName, parameters);
        }

        public static void OnVictimInteracted(
            string moduleId,
            string moduleName,
            string victimId,
            string victimName,
            IDictionary<string, object> extraParameters = null)
        {
            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.VictimId] = victimId;
            parameters[AnalyticsParams.VictimName] = victimName;
            TrackEvent(AnalyticsEventNames.VictimInteracted, moduleId, moduleName, parameters);
        }

        public static void OnVictimTagged(
            string moduleId,
            string moduleName,
            string victimId,
            string victimName,
            TriageCategory selectedCategory,
            TriageCategory actualCategory,
            IDictionary<string, object> extraParameters = null)
        {
            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.VictimId] = victimId;
            parameters[AnalyticsParams.VictimName] = victimName;
            parameters[AnalyticsParams.AssignedTriage] = selectedCategory.ToString();
            parameters[AnalyticsParams.ActualTriage] = actualCategory.ToString();
            parameters[AnalyticsParams.IsCorrect] = selectedCategory == actualCategory;
            TrackEvent(AnalyticsEventNames.VictimTagged, moduleId, moduleName, parameters);
        }

        public static void OnAIPanelOpened(
            string moduleId,
            string moduleName,
            string panelId,
            string panelName,
            IDictionary<string, object> extraParameters = null)
        {
            AIInteractionResult result = Service.SessionTracker.OpenAiPanel(moduleId, moduleName, panelId, panelName);
            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.PanelId] = result.panelId;
            parameters[AnalyticsParams.PanelName] = result.panelName;
            TrackEvent(AnalyticsEventNames.AIPanelOpened, moduleId, moduleName, parameters);
        }

        public static void OnAIQuestionAsked(
            string moduleId,
            string moduleName,
            string panelId,
            string panelName,
            string aiQuestionType,
            IDictionary<string, object> extraParameters = null)
        {
            AIInteractionResult result = Service.SessionTracker.BuildAiInteraction(
                moduleId,
                moduleName,
                panelId,
                panelName,
                aiQuestionType,
                false);

            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.PanelId] = result.panelId;
            parameters[AnalyticsParams.PanelName] = result.panelName;
            parameters[AnalyticsParams.AiQuestionType] = aiQuestionType;
            parameters[AnalyticsParams.DurationSeconds] = result.durationSeconds;
            TrackEvent(AnalyticsEventNames.AIQuestionAsked, moduleId, moduleName, parameters);
        }

        public static void OnQuizStarted(
            string moduleId,
            string moduleName,
            string quizId,
            string quizName,
            int totalQuestionCount,
            IDictionary<string, object> extraParameters = null)
        {
            Service.SessionTracker.StartQuiz(moduleId, moduleName, quizId, quizName, totalQuestionCount);
            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.QuizId] = quizId;
            parameters[AnalyticsParams.QuizName] = quizName;
            parameters[AnalyticsParams.TotalCount] = totalQuestionCount;
            TrackEvent(AnalyticsEventNames.QuizStarted, moduleId, moduleName, parameters);
        }

        public static void OnQuizAnswered(
            string moduleId,
            string moduleName,
            string quizId,
            string quizName,
            int questionIndex,
            int selectedIndex,
            int correctIndex,
            bool isCorrect,
            int totalQuestionCount,
            IDictionary<string, object> extraParameters = null)
        {
            QuizResult result = Service.SessionTracker.RecordQuizAnswer(
                moduleId,
                moduleName,
                quizId,
                quizName,
                questionIndex,
                selectedIndex,
                correctIndex,
                isCorrect);

            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.QuizId] = quizId;
            parameters[AnalyticsParams.QuizName] = quizName;
            parameters[AnalyticsParams.QuestionIndex] = questionIndex;
            parameters[AnalyticsParams.SelectedAnswerIndex] = selectedIndex;
            parameters[AnalyticsParams.CorrectAnswerIndex] = correctIndex;
            parameters[AnalyticsParams.IsCorrect] = isCorrect;
            parameters[AnalyticsParams.AnsweredCount] = result.answeredCount;
            parameters[AnalyticsParams.TotalCount] = Mathf.Max(totalQuestionCount, result.totalQuestionCount);
            parameters[AnalyticsParams.CorrectCount] = result.correctCount;
            TrackEvent(AnalyticsEventNames.QuizAnswered, moduleId, moduleName, parameters);
        }

        public static void OnQuizCompleted(
            string moduleId,
            string moduleName,
            string quizId,
            string quizName,
            int totalQuestionCount,
            int correctCount,
            IDictionary<string, object> extraParameters = null)
        {
            QuizResult result = Service.SessionTracker.CompleteQuiz(
                moduleId,
                moduleName,
                quizId,
                quizName,
                totalQuestionCount,
                correctCount);

            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.QuizId] = quizId;
            parameters[AnalyticsParams.QuizName] = quizName;
            parameters[AnalyticsParams.TotalCount] = result.totalQuestionCount;
            parameters[AnalyticsParams.CorrectCount] = result.correctCount;
            parameters[AnalyticsParams.AnsweredCount] = result.answeredCount;
            parameters[AnalyticsParams.ScorePercent] = result.scorePercentage;
            parameters[AnalyticsParams.DurationSeconds] = result.durationSeconds;
            TrackEvent(AnalyticsEventNames.QuizCompleted, moduleId, moduleName, parameters);
        }

        public static void OnScoreRecorded(
            string moduleId,
            string moduleName,
            string scoreType,
            float scoreValue,
            float scorePercentage,
            IDictionary<string, object> extraParameters = null)
        {
            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.TaskType] = scoreType;
            parameters[AnalyticsParams.ScoreValue] = scoreValue;
            parameters[AnalyticsParams.ScorePercent] = scorePercentage;
            TrackEvent(AnalyticsEventNames.ScoreRecorded, moduleId, moduleName, parameters);
        }

        public static void OnScenarioStarted(
            string moduleId,
            string moduleName,
            string scenarioId,
            string scenarioName,
            IDictionary<string, object> extraParameters = null)
        {
            Service.SessionTracker.BeginScenario(moduleId, scenarioId);
            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.ScenarioId] = scenarioId;
            parameters[AnalyticsParams.ScenarioName] = scenarioName;
            TrackEvent(AnalyticsEventNames.ScenarioStarted, moduleId, moduleName, parameters);
        }

        public static void OnCriticalActionTaken(
            string moduleId,
            string moduleName,
            string actionId,
            string actionName,
            IDictionary<string, object> extraParameters = null)
        {
            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.ActionId] = actionId;
            parameters[AnalyticsParams.ActionName] = actionName;
            TrackEvent(AnalyticsEventNames.CriticalActionTaken, moduleId, moduleName, parameters);
        }

        public static void OnScenarioTaskCompleted(
            string moduleId,
            string moduleName,
            string scenarioId,
            string scenarioName,
            string taskId,
            string taskName,
            IDictionary<string, object> extraParameters = null)
        {
            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.ScenarioId] = scenarioId;
            parameters[AnalyticsParams.ScenarioName] = scenarioName;
            parameters[AnalyticsParams.TaskId] = taskId;
            parameters[AnalyticsParams.TaskName] = taskName;
            TrackEvent(AnalyticsEventNames.ScenarioTaskCompleted, moduleId, moduleName, parameters);
        }

        public static void OnScenarioCompleted(
            string moduleId,
            string moduleName,
            string scenarioId,
            string scenarioName,
            IDictionary<string, object> extraParameters = null)
        {
            float duration = Service.SessionTracker.CompleteScenario(moduleId, scenarioId);
            Dictionary<string, object> parameters = CreateParameters(moduleId, moduleName, extraParameters);
            parameters[AnalyticsParams.ScenarioId] = scenarioId;
            parameters[AnalyticsParams.ScenarioName] = scenarioName;
            parameters[AnalyticsParams.DurationSeconds] = duration;
            TrackEvent(AnalyticsEventNames.ScenarioCompleted, moduleId, moduleName, parameters);
        }

        internal static void TrackEvent(
            string eventName,
            string moduleId,
            string moduleName,
            IDictionary<string, object> extraParameters = null,
            bool suppressDuplicate = false,
            string duplicateKey = null)
        {
            Service.TrackEvent(
                eventName,
                CreateParameters(moduleId, moduleName, extraParameters),
                suppressDuplicate,
                duplicateKey);
        }

        internal static void EnsureScenarioStarted(
            string moduleId,
            string moduleName,
            string scenarioId,
            string scenarioName,
            IDictionary<string, object> extraParameters = null,
            string duplicateKey = null)
        {
            string resolvedDuplicateKey = string.IsNullOrWhiteSpace(duplicateKey)
                ? moduleId + "_" + scenarioId + "_started"
                : duplicateKey;

            if (!Service.SessionTracker.TryRegisterUniqueEvent(resolvedDuplicateKey))
            {
                return;
            }

            OnScenarioStarted(moduleId, moduleName, scenarioId, scenarioName, extraParameters);
        }

        internal static void EnsureModuleEntered(
            string moduleId,
            string moduleName,
            IDictionary<string, object> extraParameters = null,
            string duplicateKey = null)
        {
            string resolvedDuplicateKey = string.IsNullOrWhiteSpace(duplicateKey)
                ? moduleId + "_entered"
                : duplicateKey;

            if (!Service.SessionTracker.TryRegisterUniqueEvent(resolvedDuplicateKey))
            {
                return;
            }

            OnModuleEntered(moduleId, moduleName, extraParameters);
        }

        internal static void TrackModuleTransitionIntent(
            string fromModuleId,
            string fromModuleName,
            string targetModuleId,
            string targetModuleName,
            string transitionSource)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { AnalyticsParams.TargetModuleId, targetModuleId },
                { AnalyticsParams.TargetModuleName, targetModuleName },
                { AnalyticsParams.TransitionSource, transitionSource }
            };

            TrackEvent(AnalyticsEventNames.ModuleTransitionIntent, fromModuleId, fromModuleName, parameters);
        }

        internal static void RecordModule2VictimCompletion(
            Component victimComponent,
            int fallbackIndex,
            string completionSource,
            string scenarioName = null,
            int totalVictimCount = 3)
        {
            string victimId = ResolveVictimId(victimComponent, fallbackIndex);
            string victimName = ResolveVictimName(victimComponent, fallbackIndex);
            int completedVictimCount = Service.SessionTracker.RegisterCompletion("module2_completed_victims", victimId);

            Dictionary<string, object> taskParameters = new Dictionary<string, object>
            {
                { AnalyticsParams.ScenarioId, Module2ScenarioId },
                { AnalyticsParams.ScenarioName, string.IsNullOrWhiteSpace(scenarioName) ? Module2ScenarioName : scenarioName },
                { AnalyticsParams.VictimId, victimId },
                { AnalyticsParams.VictimName, victimName },
                { AnalyticsParams.CompletedCount, completedVictimCount },
                { AnalyticsParams.TotalCount, totalVictimCount },
                { AnalyticsParams.CompletionSource, completionSource }
            };

            TrackEvent(
                AnalyticsEventNames.ScenarioTaskCompleted,
                Module2Id,
                Module2Name,
                taskParameters,
                true,
                "module2_victim_completion_" + victimId);

            if (completedVictimCount < totalVictimCount)
            {
                return;
            }

            if (!Service.SessionTracker.TryRegisterUniqueEvent("module2_training_complete"))
            {
                return;
            }

            Dictionary<string, object> completionParameters = new Dictionary<string, object>(taskParameters);
            OnScenarioCompleted(Module2Id, Module2Name, Module2ScenarioId, Module2ScenarioName, completionParameters);
            OnModuleCompleted(Module2Id, Module2Name, completionParameters);
        }

        internal static string ResolveVictimId(Component component, int fallbackIndex = -1)
        {
            if (component != null)
            {
                IlkyardimNPCIndex npcIndex = component.GetComponent<IlkyardimNPCIndex>();
                if (npcIndex == null)
                {
                    npcIndex = component.GetComponentInParent<IlkyardimNPCIndex>();
                }

                if (npcIndex != null)
                {
                    return "victim_" + (npcIndex.index + 1);
                }
            }

            if (fallbackIndex >= 0)
            {
                return "victim_" + (fallbackIndex + 1);
            }

            if (component != null)
            {
                return AnalyticsService.SanitizeToken(component.gameObject.name, 40, "victim");
            }

            return "victim_unknown";
        }

        internal static string ResolveVictimName(Component component, int fallbackIndex = -1)
        {
            if (component is NPCTriageInteractable triageInteractable)
            {
                return triageInteractable.PatientTitle;
            }

            if (component != null)
            {
                NPCTriageInteractable parentTriageInteractable = component.GetComponentInParent<NPCTriageInteractable>();
                if (parentTriageInteractable != null)
                {
                    return parentTriageInteractable.PatientTitle;
                }

                if (component.transform.parent != null && !string.IsNullOrWhiteSpace(component.transform.parent.name))
                {
                    return component.transform.parent.name.Trim();
                }

                return string.IsNullOrWhiteSpace(component.gameObject.name)
                    ? "Yarali " + (fallbackIndex + 1)
                    : component.gameObject.name.Trim();
            }

            return fallbackIndex >= 0 ? "Yarali " + (fallbackIndex + 1) : "Bilinmeyen Yarali";
        }

        private static Dictionary<string, object> CreateParameters(
            string moduleId,
            string moduleName,
            IDictionary<string, object> extraParameters)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(moduleId))
            {
                parameters[AnalyticsParams.ModuleId] = moduleId;
            }

            if (!string.IsNullOrWhiteSpace(moduleName))
            {
                parameters[AnalyticsParams.ModuleName] = moduleName;
            }

            if (extraParameters == null)
            {
                return parameters;
            }

            foreach (KeyValuePair<string, object> pair in extraParameters)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    continue;
                }

                parameters[pair.Key] = pair.Value;
            }

            return parameters;
        }

        private static string ExtractString(IDictionary<string, object> parameters, string key)
        {
            if (parameters == null || string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            return parameters.TryGetValue(key, out object value) && value != null
                ? value.ToString()
                : string.Empty;
        }

        private static int ExtractInteger(IDictionary<string, object> parameters, string key, int defaultValue = 0)
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

            return int.TryParse(value.ToString(), out int parsedValue) ? parsedValue : defaultValue;
        }
    }
}
