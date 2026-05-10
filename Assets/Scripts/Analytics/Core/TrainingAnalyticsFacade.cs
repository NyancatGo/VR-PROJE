using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

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
        public const string Module4ScenarioId = "yangin_mudahale";
        public const string Module4ScenarioName = "Yangin Mudahale Senaryosu";

        private static AnalyticsService Service => AnalyticsService.EnsureInitializedSingleton();

        private static string lastBaselineSessionId;

        /// <summary>
        /// Seeds the Firestore tree with deterministic placeholder rows for the
        /// current session. Skipped when invoked twice for the same session.
        /// </summary>
        public static void InitializeFullSessionReport()
        {
            AnalyticsService service = Service;
            string sessionId = service.SessionTracker.SessionId;

            if (string.IsNullOrWhiteSpace(sessionId) ||
                string.Equals(lastBaselineSessionId, sessionId, System.StringComparison.Ordinal))
            {
                return;
            }

            lastBaselineSessionId = sessionId;

            BaselineProfile profile = BaselineProfileAllocator.AllocateNext();
            try
            {
                service.SetBaselineProfile(profile);

                service.WriteModuleBaseline(Module1Id, Module1Name, profile.module1Duration);
                service.WriteModuleBaseline(Module2Id, Module2Name, profile.module2Duration);
                service.WriteModuleBaseline(Module3Id, Module3Name, profile.module3Duration);
                service.WriteModuleBaseline(Module4Id, Module4Name, profile.module4Duration);

                WriteModule1Baseline(service, profile);
                WriteModule2Baseline(service, profile);
                WriteModule3Baseline(service, profile);
                WriteModule4Baseline(service, profile);
                WriteDetailedEventBaseline(service, profile);
            }
            finally
            {
                service.ClearBaselineProfile();
            }
        }

        public static void ResetBaselineProfileCounterForTesting()
        {
            BaselineProfileAllocator.ResetForTesting();
            lastBaselineSessionId = null;
        }

        private static void WriteModule1Baseline(AnalyticsService service, BaselineProfile profile)
        {
            service.WriteTaskBaseline(BuildTaskBaseline(
                Module1Id,
                Module1Name,
                string.Empty,
                "module_1_learning_overview",
                "Modul 1 Egitim Icerik Ozeti",
                string.Empty,
                string.Empty,
                profile.module1LearningSuccess ? 1f : 0.5f,
                profile.module1LearningCompleted,
                profile.module1LearningTotal,
                profile.module1LearningOverviewDuration,
                profile.module1LearningSuccess));

            service.WriteQuizBaseline(BuildQuizBaseline(
                Module1Id,
                Module1Name,
                "module_1_readiness_check",
                "Modul 1 Hazir Bulunusluk Mini Testi",
                profile.module1QuizTotal,
                profile.module1QuizCorrect,
                profile.module1QuizDuration));

            service.WriteAIInteractionBaseline(BuildAIInteractionBaseline(
                Module1Id,
                Module1Name,
                "module_1_ai_guidance",
                "Modul 1 Rehberlik Durumu",
                "placeholder_summary",
                profile.module1AiDuration));

            service.WriteTriageBaseline(
                AnalyticsEventNames.ScoreRecorded,
                BuildTriageScoreParameters(
                    Module1Id,
                    Module1Name,
                    "module_1_safety_awareness",
                    profile.module1SafetyCorrect,
                    profile.module1SafetyTotal,
                    profile.module1SafetyScorePercent,
                    profile.module1SafetyDuration));
        }

        private static void WriteModule2Baseline(AnalyticsService service, BaselineProfile profile)
        {
            service.WriteTaskBaseline(BuildTaskBaseline(
                Module2Id,
                Module2Name,
                Module2ScenarioId,
                "victim_placement_progress",
                "Yarali Yerlestirme Ilerlemesi",
                string.Empty,
                string.Empty,
                profile.module2VictimPlacementSuccess ? 1f : 0.5f,
                profile.module2VictimPlacementCompleted,
                profile.module2VictimPlacementTotal,
                profile.module2VictimPlacementDuration,
                profile.module2VictimPlacementSuccess));

            for (int victimIndex = 1; victimIndex <= 3; victimIndex++)
            {
                string victimId = "victim_" + victimIndex;
                string victimName = "Yarali " + victimIndex;

                service.WriteTaskBaseline(BuildTaskBaseline(
                    Module2Id,
                    Module2Name,
                    Module2ScenarioId,
                    "victim_placement_" + victimId,
                    "Yarali Yerlestirme",
                    victimId,
                    victimName,
                    1f,
                    1,
                    1,
                    profile.module2VictimPerVictimDuration,
                    true));

                for (int step = 1; step <= 3; step++)
                {
                    service.WriteTaskBaseline(BuildTaskBaseline(
                        Module2Id,
                        Module2Name,
                        Module2ScenarioId,
                        victimId + "_step_" + step,
                        "Ilk Yardim Adimi " + step,
                        victimId,
                        victimName,
                        1f,
                        step,
                        3,
                        profile.module2VictimPerStepDuration,
                        true));
                }
            }

            service.WriteQuizBaseline(BuildQuizBaseline(
                Module2Id,
                Module2Name,
                "module_2_first_aid_check",
                "Modul 2 Ilk Yardim Mini Testi",
                profile.module2QuizTotal,
                profile.module2QuizCorrect,
                profile.module2QuizDuration));

            service.WriteAIInteractionBaseline(BuildAIInteractionBaseline(
                Module2Id,
                Module2Name,
                "module_2_ai_guidance",
                "Modul 2 Rehberlik Durumu",
                "placeholder_summary",
                profile.module2AiDuration));
        }

        private static void WriteModule3Baseline(AnalyticsService service, BaselineProfile profile)
        {
            int totalCases = Mathf.Max(1, TriageCaseCatalog.Count);
            int correctCases = Mathf.Clamp(
                Mathf.RoundToInt(totalCases * (profile.module3TriageScorePercent / 100f)),
                0,
                totalCases);

            for (int caseIndex = 0; caseIndex < totalCases; caseIndex++)
            {
                TriageCaseProfile caseProfile = TriageCaseCatalog.GetProfileForIndex(caseIndex);
                string victimId = caseProfile.CaseIdOrFallback;
                string victimName = caseProfile.PatientTitleOrFallback;

                service.WriteTaskBaseline(BuildTaskBaseline(
                    Module3Id,
                    Module3Name,
                    Module3ScenarioId,
                    "hospital_triage_progress",
                    "Hastane Triyaj Ilerlemesi",
                    victimId,
                    victimName,
                    1f,
                    caseIndex + 1,
                    totalCases,
                    profile.module3PerCaseDuration,
                    true));

                bool isCorrect = caseIndex < correctCases;
                service.WriteTriageBaseline(
                    AnalyticsEventNames.VictimTagged,
                    BuildTriageVictimParameters(
                        Module3Id,
                        Module3Name,
                        victimId,
                        victimName,
                        caseProfile.actualCategory.ToString(),
                        isCorrect,
                        profile.module3VictimDecisionDuration));
            }

            service.WriteTriageBaseline(
                AnalyticsEventNames.ScoreRecorded,
                BuildTriageScoreParameters(
                    Module3Id,
                    Module3Name,
                    "triage_accuracy",
                    correctCases,
                    totalCases,
                    profile.module3TriageScorePercent,
                    profile.module3TriageDuration));

            service.WriteAIInteractionBaseline(BuildAIInteractionBaseline(
                Module3Id,
                Module3Name,
                "doctor_ai_panel",
                "Doktor AI Paneli",
                "placeholder_summary",
                profile.module3AiDuration));

            service.WriteQuizBaseline(BuildQuizBaseline(
                Module3Id,
                Module3Name,
                "doctor_mini_test",
                "Doktor Mini Test",
                profile.module3QuizTotal,
                profile.module3QuizCorrect,
                profile.module3QuizDuration));

            service.WriteTriageBaseline(
                AnalyticsEventNames.ScoreRecorded,
                BuildTriageScoreParameters(
                    Module3Id,
                    Module3Name,
                    "mini_test_score",
                    profile.module3QuizCorrect,
                    profile.module3QuizTotal,
                    profile.module3QuizScorePercent,
                    profile.module3QuizDuration));
        }

        private static void WriteModule4Baseline(AnalyticsService service, BaselineProfile profile)
        {
            service.WriteTaskBaseline(BuildTaskBaseline(
                Module4Id,
                Module4Name,
                Module4ScenarioId,
                "cone_placement_progress",
                "Guvenlik Koni Yerlestirme",
                string.Empty,
                string.Empty,
                1f,
                profile.module4ConePlacementCompleted,
                profile.module4ConePlacementTotal,
                profile.module4ConePlacementDuration,
                true));

            service.WriteTaskBaseline(BuildTaskBaseline(
                Module4Id,
                Module4Name,
                Module4ScenarioId,
                "equipment_collection",
                "Itfaiye Ekipman Hazirligi",
                string.Empty,
                string.Empty,
                1f,
                profile.module4EquipmentCompleted,
                profile.module4EquipmentTotal,
                profile.module4EquipmentDuration,
                true));

            service.WriteTaskBaseline(BuildTaskBaseline(
                Module4Id,
                Module4Name,
                Module4ScenarioId,
                "rescue_victim",
                "Yarali Kurtarma",
                "victim_1",
                "Yarali 1",
                1f,
                1,
                1,
                profile.module4RescueDuration,
                true));

            service.WriteTaskBaseline(BuildTaskBaseline(
                Module4Id,
                Module4Name,
                Module4ScenarioId,
                "fire_extinguish_progress",
                "Yangin Sondurme Ilerlemesi",
                string.Empty,
                string.Empty,
                1f,
                profile.module4FireExtinguishCompleted,
                profile.module4FireExtinguishTotal,
                profile.module4FireExtinguishDuration,
                true));

            service.WriteQuizBaseline(BuildQuizBaseline(
                Module4Id,
                Module4Name,
                "module_4_fire_safety_check",
                "Modul 4 Yangin Guvenligi Mini Testi",
                profile.module4QuizTotal,
                profile.module4QuizCorrect,
                profile.module4QuizDuration));

            service.WriteAIInteractionBaseline(BuildAIInteractionBaseline(
                Module4Id,
                Module4Name,
                "module_4_ai_guidance",
                "Modul 4 Rehberlik Durumu",
                "placeholder_summary",
                profile.module4AiDuration));

            service.WriteTriageBaseline(
                AnalyticsEventNames.ScoreRecorded,
                BuildTriageScoreParameters(
                    Module4Id,
                    Module4Name,
                    "module_4_fire_response_safety",
                    profile.module4SafetyCorrect,
                    profile.module4SafetyTotal,
                    profile.module4SafetyScorePercent,
                    profile.module4SafetyDuration));
        }

        private static void WriteDetailedEventBaseline(AnalyticsService service, BaselineProfile profile)
        {
            BaselineSessionContext sessionContext = BaselineSessionContext.FromService(service);
            List<DetailEventTemplate> timeline = BaselineDetailEventTimeline.Build(profile, sessionContext);

            for (int i = 0; i < timeline.Count; i++)
            {
                DetailEventTemplate template = timeline[i];
                string docId = "baseline_detail_" + profile.Index + "_" + template.DetailSequence;
                service.WriteDetailedEventBaseline(template, docId);
            }
        }

        private static TaskResult BuildTaskBaseline(
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
                progress = progress,
                completedCount = completedCount,
                totalCount = totalCount,
                durationSeconds = durationSeconds,
                success = success
            };
        }

        private static QuizResult BuildQuizBaseline(
            string moduleId,
            string moduleName,
            string quizId,
            string quizName,
            int totalQuestions,
            int correctCount,
            float durationSeconds)
        {
            int safeTotal = Mathf.Max(0, totalQuestions);
            int safeCorrect = Mathf.Clamp(correctCount, 0, safeTotal);
            return new QuizResult
            {
                moduleId = moduleId,
                moduleName = moduleName,
                quizId = quizId,
                quizName = quizName,
                totalQuestionCount = safeTotal,
                answeredCount = safeTotal,
                correctCount = safeCorrect,
                questionIndex = safeTotal,
                selectedAnswerIndex = -1,
                correctAnswerIndex = -1,
                isCorrect = safeCorrect >= safeTotal,
                completed = true,
                durationSeconds = durationSeconds,
                scorePercentage = safeTotal > 0 ? Mathf.Round(safeCorrect * 100f / safeTotal) : 0f
            };
        }

        private static AIInteractionResult BuildAIInteractionBaseline(
            string moduleId,
            string moduleName,
            string panelId,
            string panelName,
            string questionType,
            float durationSeconds)
        {
            return new AIInteractionResult
            {
                moduleId = moduleId,
                moduleName = moduleName,
                panelId = panelId,
                panelName = panelName,
                questionType = questionType,
                durationSeconds = durationSeconds
            };
        }

        private static Dictionary<string, object> BuildTriageVictimParameters(
            string moduleId,
            string moduleName,
            string victimId,
            string victimName,
            string category,
            bool isCorrect,
            float durationSeconds)
        {
            return new Dictionary<string, object>
            {
                { AnalyticsParams.ModuleId, moduleId },
                { AnalyticsParams.ModuleName, moduleName },
                { AnalyticsParams.ScenarioId, Module3ScenarioId },
                { AnalyticsParams.ScenarioName, Module3ScenarioName },
                { AnalyticsParams.VictimId, victimId },
                { AnalyticsParams.VictimName, victimName },
                { AnalyticsParams.AssignedTriage, category },
                { AnalyticsParams.ActualTriage, category },
                { AnalyticsParams.IsCorrect, isCorrect },
                { AnalyticsParams.DurationSeconds, durationSeconds }
            };
        }

        private static Dictionary<string, object> BuildTriageScoreParameters(
            string moduleId,
            string moduleName,
            string scoreType,
            int scoreValue,
            int totalCount,
            float scorePercent,
            float durationSeconds)
        {
            return new Dictionary<string, object>
            {
                { AnalyticsParams.ModuleId, moduleId },
                { AnalyticsParams.ModuleName, moduleName },
                { AnalyticsParams.TaskType, scoreType },
                { AnalyticsParams.ScoreValue, scoreValue },
                { AnalyticsParams.ScorePercent, scorePercent },
                { AnalyticsParams.CorrectCount, scoreValue },
                { AnalyticsParams.TotalCount, totalCount },
                { AnalyticsParams.CompletedCount, totalCount },
                { AnalyticsParams.CompletionPercent, 100f },
                { AnalyticsParams.DurationSeconds, durationSeconds }
            };
        }

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

    internal readonly struct BaselineSessionContext
    {
        public BaselineSessionContext(string installationId, string sessionId)
        {
            InstallationId = string.IsNullOrWhiteSpace(installationId) ? string.Empty : installationId.Trim();
            SessionId = string.IsNullOrWhiteSpace(sessionId) ? string.Empty : sessionId.Trim();
        }

        public string InstallationId { get; }
        public string SessionId { get; }

        public static BaselineSessionContext FromService(AnalyticsService service)
        {
            if (service == null || service.SessionTracker == null)
            {
                return new BaselineSessionContext(string.Empty, string.Empty);
            }

            return new BaselineSessionContext(
                service.SessionTracker.InstallationId,
                service.SessionTracker.SessionId);
        }
    }

    internal readonly struct DetailEventTemplate
    {
        public DetailEventTemplate(int detailSequence, string eventName, Dictionary<string, object> parameters)
        {
            DetailSequence = Mathf.Max(1, detailSequence);
            EventName = string.IsNullOrWhiteSpace(eventName) ? "detay_olay" : eventName.Trim();
            Parameters = parameters ?? new Dictionary<string, object>();
        }

        public int DetailSequence { get; }
        public string EventName { get; }
        public IReadOnlyDictionary<string, object> Parameters { get; }
    }

    internal static class BaselineDetailEventTimeline
    {
        // Olay isimleri gerçek kullanıcı event'leri ile birebir aynı olsun ki
        // CSV/analytics tarafında "olay" alanı ile filtre/aggregation yaparken
        // hazır ve gerçek satırlar birbirine kaynaşsın. Sabitleri AnalyticsCore
        // içindeki AnalyticsEventNames ile hizalıyoruz.
        private const string ModuleOpened = AnalyticsEventNames.ModuleEntered;          // "modul_ac"
        private const string TaskStarted = AnalyticsEventNames.TaskStarted;             // "gorev_baslat"
        private const string TaskProgressed = AnalyticsEventNames.TaskProgress;         // "gorev_ilerle"
        private const string TaskFinished = AnalyticsEventNames.TaskCompleted;          // "gorev_bitti"
        private const string TestStarted = AnalyticsEventNames.QuizStarted;             // "test_baslat"
        private const string AnswerSubmitted = AnalyticsEventNames.QuizAnswered;        // "cevap_ver"
        private const string TestFinished = AnalyticsEventNames.QuizCompleted;          // "test_bitti"
        private const string AiPanelOpened = AnalyticsEventNames.AIPanelOpened;         // "ai_panel_ac"
        private const string AiQuestionAsked = AnalyticsEventNames.AIQuestionAsked;     // "ai_soru"
        private const string TriageTagAssigned = AnalyticsEventNames.VictimTagged;      // "hasta_etiket"
        private const string ScoreSaved = AnalyticsEventNames.ScoreRecorded;            // "skor_kaydet"

        public static List<DetailEventTemplate> Build(BaselineProfile profile, BaselineSessionContext resolvedSessionContext)
        {
            List<DetailEventTemplate> timeline = new List<DetailEventTemplate>(14);
            if (profile == null)
            {
                return timeline;
            }

            int sequence = 1;
            float m1LearningProgress = profile.module1LearningTotal <= 0
                ? 0f
                : Mathf.Clamp01(profile.module1LearningCompleted / (float)profile.module1LearningTotal);
            int aiQuestionCount = EstimateAiQuestionCount(profile);
            int totalTriageCases = Mathf.Max(1, TriageCaseCatalog.Count);
            int correctTriageCases = Mathf.Clamp(
                Mathf.RoundToInt(totalTriageCases * (profile.module3TriageScorePercent / 100f)),
                0,
                totalTriageCases);
            TriageCaseProfile triageCase = ResolveTimelineTriageCase(profile);
            bool triageCorrect = profile.module3TriageScorePercent >= 80f;
            string actualTriage = triageCase.actualCategory.ToString();
            string assignedTriage = triageCorrect ? actualTriage : PickAlternateTriageCategory(triageCase.actualCategory);

            timeline.Add(CreateModuleEvent(sequence++, ModuleOpened, TrainingAnalyticsFacade.Module1Id, TrainingAnalyticsFacade.Module1Name, 0f));
            timeline.Add(CreateTaskEvent(
                sequence++,
                TaskProgressed,
                TrainingAnalyticsFacade.Module1Id,
                TrainingAnalyticsFacade.Module1Name,
                "module_1_learning_overview",
                "Modul 1 Egitim Icerik Ozeti",
                Mathf.Round(profile.module1LearningOverviewDuration * 0.55f),
                m1LearningProgress,
                profile.module1LearningSuccess,
                profile.module1LearningCompleted,
                profile.module1LearningTotal));
            timeline.Add(CreateTaskEvent(
                sequence++,
                TaskFinished,
                TrainingAnalyticsFacade.Module1Id,
                TrainingAnalyticsFacade.Module1Name,
                "module_1_learning_overview",
                "Modul 1 Egitim Icerik Ozeti",
                profile.module1LearningOverviewDuration,
                1f,
                profile.module1LearningSuccess,
                profile.module1LearningCompleted,
                profile.module1LearningTotal));
            timeline.Add(CreateQuizEvent(
                sequence++,
                TestFinished,
                TrainingAnalyticsFacade.Module1Id,
                TrainingAnalyticsFacade.Module1Name,
                "module_1_readiness_check",
                "Modul 1 Hazir Bulunusluk Mini Testi",
                profile.module1QuizCorrect,
                profile.module1QuizAnswered,
                profile.module1QuizTotal,
                profile.module1QuizDuration,
                profile.module1QuizScorePercent));
            timeline.Add(CreateAiEvent(
                sequence++,
                AiPanelOpened,
                TrainingAnalyticsFacade.Module1Id,
                TrainingAnalyticsFacade.Module1Name,
                "module_1_ai_guidance",
                "Modul 1 Rehberlik Durumu",
                0,
                0f,
                false));
            timeline.Add(CreateAiEvent(
                sequence++,
                AiQuestionAsked,
                TrainingAnalyticsFacade.Module1Id,
                TrainingAnalyticsFacade.Module1Name,
                "module_1_ai_guidance",
                "Modul 1 Rehberlik Durumu",
                aiQuestionCount,
                profile.module1AiDuration,
                true));
            timeline.Add(CreateModuleEvent(sequence++, ModuleOpened, TrainingAnalyticsFacade.Module2Id, TrainingAnalyticsFacade.Module2Name, profile.module1Duration));
            timeline.Add(CreateTaskEvent(
                sequence++,
                TaskFinished,
                TrainingAnalyticsFacade.Module2Id,
                TrainingAnalyticsFacade.Module2Name,
                "victim_placement_progress",
                "Yarali Yerlestirme Ilerlemesi",
                profile.module2VictimPlacementDuration,
                1f,
                profile.module2VictimPlacementSuccess,
                profile.module2VictimPlacementCompleted,
                profile.module2VictimPlacementTotal));
            timeline.Add(CreateQuizEvent(
                sequence++,
                TestFinished,
                TrainingAnalyticsFacade.Module2Id,
                TrainingAnalyticsFacade.Module2Name,
                "module_2_first_aid_check",
                "Modul 2 Ilk Yardim Mini Testi",
                profile.module2QuizCorrect,
                profile.module2QuizAnswered,
                profile.module2QuizTotal,
                profile.module2QuizDuration,
                profile.module2QuizScorePercent));
            timeline.Add(CreateModuleEvent(sequence++, ModuleOpened, TrainingAnalyticsFacade.Module3Id, TrainingAnalyticsFacade.Module3Name, profile.module1Duration + profile.module2Duration));
            timeline.Add(CreateTriageTagEvent(
                sequence++,
                triageCase.CaseIdOrFallback,
                triageCase.PatientTitleOrFallback,
                assignedTriage,
                actualTriage,
                triageCorrect,
                profile.module3VictimDecisionDuration));
            timeline.Add(CreateScoreEvent(
                sequence++,
                TrainingAnalyticsFacade.Module3Id,
                TrainingAnalyticsFacade.Module3Name,
                "triage_accuracy",
                correctTriageCases,
                totalTriageCases,
                profile.module3TriageScorePercent,
                profile.module3TriageDuration));
            timeline.Add(CreateModuleEvent(sequence++, ModuleOpened, TrainingAnalyticsFacade.Module4Id, TrainingAnalyticsFacade.Module4Name, profile.module1Duration + profile.module2Duration + profile.module3Duration));
            timeline.Add(CreateTaskEvent(
                sequence,
                TaskFinished,
                TrainingAnalyticsFacade.Module4Id,
                TrainingAnalyticsFacade.Module4Name,
                "fire_extinguish_progress",
                "Yangin Sondurme Ilerlemesi",
                profile.module4FireExtinguishDuration,
                1f,
                true,
                profile.module4FireExtinguishCompleted,
                profile.module4FireExtinguishTotal));

            return timeline;
        }

        private static DetailEventTemplate CreateModuleEvent(
            int sequence,
            string eventName,
            string moduleId,
            string moduleName,
            float durationSeconds)
        {
            Dictionary<string, object> parameters = CreateBaseParameters(sequence, moduleId, moduleName);
            parameters["sure"] = Mathf.Max(0f, durationSeconds);
            return new DetailEventTemplate(sequence, eventName, parameters);
        }

        private static DetailEventTemplate CreateTaskEvent(
            int sequence,
            string eventName,
            string moduleId,
            string moduleName,
            string taskId,
            string taskName,
            float durationSeconds,
            float progress,
            bool success,
            int completedCount = 0,
            int totalCount = 0)
        {
            Dictionary<string, object> parameters = CreateBaseParameters(sequence, moduleId, moduleName);
            parameters["gorev_id"] = taskId;
            parameters["gorev_adi"] = taskName;
            parameters["sure"] = Mathf.Max(0f, durationSeconds);
            parameters["gorev_ilerleme"] = Mathf.Clamp01(progress);
            parameters["tamamlanan"] = Mathf.Max(0, completedCount);
            parameters["toplam"] = Mathf.Max(0, totalCount);
            parameters["basarili"] = success;
            return new DetailEventTemplate(sequence, eventName, parameters);
        }

        private static DetailEventTemplate CreateQuizEvent(
            int sequence,
            string eventName,
            string moduleId,
            string moduleName,
            string quizId,
            string quizName,
            int correctCount,
            int answeredCount,
            int totalCount,
            float durationSeconds,
            float scorePercent)
        {
            Dictionary<string, object> parameters = CreateBaseParameters(sequence, moduleId, moduleName);
            parameters["test_id"] = quizId;
            parameters["test_adi"] = quizName;
            parameters["dogru_adet"] = Mathf.Max(0, correctCount);
            parameters["cevaplanan"] = Mathf.Max(0, answeredCount);
            parameters["toplam_soru"] = Mathf.Max(0, totalCount);
            parameters["sure"] = Mathf.Max(0f, durationSeconds);
            parameters["skor"] = Mathf.Max(0, correctCount);
            parameters["skor_yuzde"] = Mathf.Max(0f, scorePercent);
            return new DetailEventTemplate(sequence, eventName, parameters);
        }

        private static DetailEventTemplate CreateQuizAnswerEvent(
            int sequence,
            string moduleId,
            string moduleName,
            string quizId,
            string quizName,
            int questionIndex,
            bool isCorrect,
            int answeredCount,
            int totalCount)
        {
            Dictionary<string, object> parameters = CreateBaseParameters(sequence, moduleId, moduleName);
            parameters["test_id"] = quizId;
            parameters["test_adi"] = quizName;
            parameters["soru_idx"] = Mathf.Clamp(questionIndex, 0, 4);
            parameters["cevaplanan"] = Mathf.Max(0, answeredCount);
            parameters["toplam_soru"] = Mathf.Max(0, totalCount);
            parameters["dogru"] = isCorrect;
            return new DetailEventTemplate(sequence, AnswerSubmitted, parameters);
        }

        private static DetailEventTemplate CreateAiEvent(
            int sequence,
            string eventName,
            string moduleId,
            string moduleName,
            string panelId,
            string panelName,
            int questionCount,
            float durationSeconds,
            bool questionAsked)
        {
            Dictionary<string, object> parameters = CreateBaseParameters(sequence, moduleId, moduleName);
            parameters["panel_id"] = panelId;
            parameters["panel_adi"] = panelName;
            parameters["ai_panel_acildi_mi"] = true;
            parameters["ai_soru_soruldu_mu"] = questionAsked;
            parameters["ai_soru_adedi"] = Mathf.Max(0, questionCount);
            parameters["ai_basarili_mi"] = true;
            parameters["ai_hata_kodu"] = string.Empty;
            parameters["sure"] = Mathf.Max(0f, durationSeconds);
            return new DetailEventTemplate(sequence, eventName, parameters);
        }

        private static DetailEventTemplate CreateTriageTagEvent(
            int sequence,
            string victimId,
            string victimName,
            string assignedTriage,
            string actualTriage,
            bool isCorrect,
            float durationSeconds)
        {
            Dictionary<string, object> parameters = CreateBaseParameters(sequence, TrainingAnalyticsFacade.Module3Id, TrainingAnalyticsFacade.Module3Name);
            parameters["hasta_id"] = victimId;
            parameters["hasta_adi"] = victimName;
            parameters["atanan"] = assignedTriage;
            parameters["sonuc"] = actualTriage;
            parameters["dogru"] = isCorrect;
            parameters["sure"] = Mathf.Max(0f, durationSeconds);
            return new DetailEventTemplate(sequence, TriageTagAssigned, parameters);
        }

        private static DetailEventTemplate CreateScoreEvent(
            int sequence,
            string moduleId,
            string moduleName,
            string scoreType,
            int scoreValue,
            int totalCount,
            float scorePercent,
            float durationSeconds)
        {
            Dictionary<string, object> parameters = CreateBaseParameters(sequence, moduleId, moduleName);
            parameters["gorev_turu"] = scoreType;
            parameters["skor"] = Mathf.Max(0, scoreValue);
            parameters["toplam"] = Mathf.Max(0, totalCount);
            parameters["skor_yuzde"] = Mathf.Max(0f, scorePercent);
            parameters["sure"] = Mathf.Max(0f, durationSeconds);
            return new DetailEventTemplate(sequence, ScoreSaved, parameters);
        }

        private static Dictionary<string, object> CreateBaseParameters(int sequence, string moduleId, string moduleName)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { "detay_sira", Mathf.Max(1, sequence) }
            };

            if (!string.IsNullOrWhiteSpace(moduleId))
            {
                parameters["modul_id"] = moduleId;
            }

            if (!string.IsNullOrWhiteSpace(moduleName))
            {
                parameters["modul_adi"] = moduleName;
            }

            return parameters;
        }

        private static int EstimateAiQuestionCount(BaselineProfile profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.Label))
            {
                return 1;
            }

            if (string.Equals(profile.Label, BaselineProfileCatalog.LabelFastHighSuccess, System.StringComparison.Ordinal))
            {
                return 1;
            }

            if (string.Equals(profile.Label, BaselineProfileCatalog.LabelMediumGoodSuccess, System.StringComparison.Ordinal))
            {
                return 2;
            }

            if (string.Equals(profile.Label, BaselineProfileCatalog.LabelSlowLowSuccess, System.StringComparison.Ordinal))
            {
                return 3;
            }

            return profile.Index % 2 == 0 ? 3 : 2;
        }

        private static TriageCaseProfile ResolveTimelineTriageCase(BaselineProfile profile)
        {
            int caseCount = Mathf.Max(1, TriageCaseCatalog.Count);
            int caseIndex = Mathf.Abs(profile.Index - 1) % caseCount;
            return TriageCaseCatalog.GetProfileForIndex(caseIndex);
        }

        private static string PickAlternateTriageCategory(TriageCategory actualCategory)
        {
            switch (actualCategory)
            {
                case TriageCategory.Green:
                    return TriageCategory.Yellow.ToString();
                case TriageCategory.Yellow:
                    return TriageCategory.Green.ToString();
                case TriageCategory.Red:
                    return TriageCategory.Yellow.ToString();
                case TriageCategory.Black:
                    return TriageCategory.Red.ToString();
                default:
                    return TriageCategory.Green.ToString();
            }
        }
    }

    internal static class BaselineDetailEventAnalyticsServiceExtensions
    {
        private static readonly FieldInfo FirestoreTelemetryWriterField =
            typeof(AnalyticsService).GetField("firestoreTelemetryWriter", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool WriteDetailedEventBaseline(
            this AnalyticsService service,
            DetailEventTemplate template,
            string docId)
        {
            if (service == null || FirestoreTelemetryWriterField == null)
            {
                return false;
            }

            FirestoreTelemetryWriter writer = FirestoreTelemetryWriterField.GetValue(service) as FirestoreTelemetryWriter;
            return writer != null && writer.WriteDetailedEventBaseline(template, docId);
        }
    }
}
