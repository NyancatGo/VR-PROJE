using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TrainingAnalytics
{
    public class ModuleTracker : MonoBehaviour
    {
        [SerializeField] private string moduleId = TrainingAnalyticsFacade.Module1Id;
        [SerializeField] private string moduleName = TrainingAnalyticsFacade.Module1Name;
        [SerializeField] private bool trackEnterOnEnable = true;

        private bool enteredThisEnable;

        private void OnEnable()
        {
            if (!trackEnterOnEnable || enteredThisEnable)
            {
                return;
            }

            enteredThisEnable = true;
            TrainingAnalyticsFacade.OnModuleEntered(moduleId, moduleName);
        }

        public void TrackModuleCompleted()
        {
            TrainingAnalyticsFacade.OnModuleCompleted(moduleId, moduleName);
        }
    }

    public class TaskTracker : MonoBehaviour
    {
        [SerializeField] private string moduleId = TrainingAnalyticsFacade.Module2Id;
        [SerializeField] private string moduleName = TrainingAnalyticsFacade.Module2Name;
        [SerializeField] private string taskId = "task";
        [SerializeField] private string taskName = "Task";
        [SerializeField] private string scenarioId = string.Empty;
        [SerializeField] private string scenarioName = string.Empty;

        public void TrackStarted()
        {
            TrainingAnalyticsFacade.OnTaskStarted(moduleId, moduleName, taskId, taskName, BuildScenarioParameters());
        }

        public void TrackProgress(float progress)
        {
            TrainingAnalyticsFacade.OnTaskProgress(moduleId, moduleName, taskId, taskName, progress, BuildScenarioParameters());
        }

        public void TrackFailed()
        {
            TrainingAnalyticsFacade.OnTaskFailed(moduleId, moduleName, taskId, taskName, BuildScenarioParameters());
        }

        public void TrackCompleted()
        {
            TrainingAnalyticsFacade.OnTaskCompleted(moduleId, moduleName, taskId, taskName, BuildScenarioParameters());
        }

        private Dictionary<string, object> BuildScenarioParameters()
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(scenarioId))
            {
                parameters[AnalyticsParams.ScenarioId] = scenarioId;
            }

            if (!string.IsNullOrWhiteSpace(scenarioName))
            {
                parameters[AnalyticsParams.ScenarioName] = scenarioName;
            }

            return parameters;
        }
    }

    public class VideoTracker : MonoBehaviour
    {
        [SerializeField] private string moduleId = TrainingAnalyticsFacade.Module1Id;
        [SerializeField] private string moduleName = TrainingAnalyticsFacade.Module1Name;
        [SerializeField] private string videoId = "video";
        [SerializeField] private string videoName = "Video";
        [SerializeField] private VideoPlayer videoPlayer;

        private int lastProgressBucket = -1;
        private bool started;

        private void OnEnable()
        {
            if (videoPlayer == null)
            {
                videoPlayer = GetComponent<VideoPlayer>();
            }

            if (videoPlayer == null)
            {
                return;
            }

            videoPlayer.started += HandleVideoStarted;
            videoPlayer.loopPointReached += HandleVideoCompleted;
        }

        private void OnDisable()
        {
            if (videoPlayer == null)
            {
                return;
            }

            videoPlayer.started -= HandleVideoStarted;
            videoPlayer.loopPointReached -= HandleVideoCompleted;
            started = false;
            lastProgressBucket = -1;
        }

        private void Update()
        {
            if (!started || videoPlayer == null || !videoPlayer.isPlaying || videoPlayer.length <= 0d)
            {
                return;
            }

            float progress = Mathf.Clamp01((float)(videoPlayer.time / videoPlayer.length));
            int currentBucket = Mathf.FloorToInt(progress * 4f);
            if (currentBucket <= lastProgressBucket)
            {
                return;
            }

            lastProgressBucket = currentBucket;
            TrainingAnalyticsFacade.OnVideoProgress(moduleId, moduleName, videoId, videoName, progress);
        }

        public void TrackVideoStarted()
        {
            HandleVideoStarted(videoPlayer);
        }

        public void TrackVideoCompleted()
        {
            HandleVideoCompleted(videoPlayer);
        }

        private void HandleVideoStarted(VideoPlayer source)
        {
            started = true;
            lastProgressBucket = -1;
            TrainingAnalyticsFacade.OnVideoStarted(moduleId, moduleName, videoId, videoName);
        }

        private void HandleVideoCompleted(VideoPlayer source)
        {
            started = false;
            TrainingAnalyticsFacade.OnVideoCompleted(moduleId, moduleName, videoId, videoName);
        }
    }

    public class InfographicTracker : MonoBehaviour
    {
        [SerializeField] private string moduleId = TrainingAnalyticsFacade.Module1Id;
        [SerializeField] private string moduleName = TrainingAnalyticsFacade.Module1Name;
        [SerializeField] private string infographicId = "infographic";
        [SerializeField] private string infographicName = "Infographic";

        public void TrackOpened()
        {
            TrainingAnalyticsFacade.OnInfographicOpened(moduleId, moduleName, infographicId, infographicName);
        }
    }

    public class QuizTracker : MonoBehaviour
    {
        [SerializeField] private string moduleId = TrainingAnalyticsFacade.Module3Id;
        [SerializeField] private string moduleName = TrainingAnalyticsFacade.Module3Name;
        [SerializeField] private string quizId = "quiz";
        [SerializeField] private string quizName = "Quiz";
        [SerializeField] private int totalQuestionCount = 0;

        public void TrackStarted()
        {
            TrainingAnalyticsFacade.OnQuizStarted(moduleId, moduleName, quizId, quizName, totalQuestionCount);
        }

        public void TrackAnswered(int questionIndex, int selectedIndex, int correctIndex, bool isCorrect)
        {
            TrainingAnalyticsFacade.OnQuizAnswered(
                moduleId,
                moduleName,
                quizId,
                quizName,
                questionIndex,
                selectedIndex,
                correctIndex,
                isCorrect,
                totalQuestionCount);
        }

        public void TrackCompleted(int correctCount)
        {
            TrainingAnalyticsFacade.OnQuizCompleted(moduleId, moduleName, quizId, quizName, totalQuestionCount, correctCount);
        }
    }

    public class TriageTracker : MonoBehaviour
    {
        [SerializeField] private string moduleId = TrainingAnalyticsFacade.Module3Id;
        [SerializeField] private string moduleName = TrainingAnalyticsFacade.Module3Name;
        [SerializeField] private string scenarioId = TrainingAnalyticsFacade.Module3ScenarioId;
        [SerializeField] private string scenarioName = TrainingAnalyticsFacade.Module3ScenarioName;

        public void TrackStarted()
        {
            TrainingAnalyticsFacade.OnTriageStarted(moduleId, moduleName, scenarioId, scenarioName);
        }

        public void TrackVictimInteracted(string victimId, string victimName)
        {
            TrainingAnalyticsFacade.OnVictimInteracted(moduleId, moduleName, victimId, victimName);
        }

        public void TrackVictimTagged(string victimId, string victimName, TriageCategory selectedCategory, TriageCategory actualCategory)
        {
            TrainingAnalyticsFacade.OnVictimTagged(moduleId, moduleName, victimId, victimName, selectedCategory, actualCategory);
        }
    }

    public class AIInteractionTracker : MonoBehaviour
    {
        [SerializeField] private string moduleId = TrainingAnalyticsFacade.Module3Id;
        [SerializeField] private string moduleName = TrainingAnalyticsFacade.Module3Name;
        [SerializeField] private string panelId = "ai_panel";
        [SerializeField] private string panelName = "AI Panel";

        public void TrackOpened()
        {
            TrainingAnalyticsFacade.OnAIPanelOpened(moduleId, moduleName, panelId, panelName);
        }

        public void TrackQuestionAsked(string questionType)
        {
            TrainingAnalyticsFacade.OnAIQuestionAsked(moduleId, moduleName, panelId, panelName, questionType);
        }
    }

    public class ContentOpenTracker : MonoBehaviour
    {
        [SerializeField] private string moduleId = TrainingAnalyticsFacade.Module1Id;
        [SerializeField] private string moduleName = TrainingAnalyticsFacade.Module1Name;
        [SerializeField] private string contentGroupId = "module_1_learning_content";
        [SerializeField] private string contentGroupName = "Modul 1 Ogrenme Icerigi";
        [SerializeField] private string[] contentIds = new string[0];
        [SerializeField] private string[] contentNames = new string[0];
        [SerializeField] private int expectedContentCount;

        private readonly HashSet<string> openedContentIds = new HashSet<string>();
        private bool learningContentCompleted;

        public void TrackSelection(int index, GameObject[] contentRoots, Button[] tabButtons)
        {
            string contentId = ResolveContentId(index, contentRoots, tabButtons);
            string contentName = ResolveContentName(index, contentRoots, tabButtons);
            int totalCount = ResolveExpectedContentCount(contentRoots);
            bool isInfographic = LooksLikeInfographic(contentId, contentName);

            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { AnalyticsParams.ContentGroupId, contentGroupId },
                { AnalyticsParams.ContentGroupName, contentGroupName }
            };

            TrainingAnalyticsFacade.OnContentOpened(moduleId, moduleName, contentId, contentName, totalCount, parameters);

            if (isInfographic)
            {
                TrainingAnalyticsFacade.OnInfographicOpened(moduleId, moduleName, contentId, contentName, parameters);
            }

            openedContentIds.Add(contentId);
            if (learningContentCompleted || totalCount <= 0 || openedContentIds.Count < totalCount)
            {
                return;
            }

            learningContentCompleted = true;
            TrainingAnalyticsFacade.OnLearningContentCompleted(
                moduleId,
                moduleName,
                contentGroupId,
                contentGroupName,
                openedContentIds.Count,
                totalCount,
                parameters);
        }

        private string ResolveContentId(int index, GameObject[] contentRoots, Button[] tabButtons)
        {
            if (contentIds != null && index >= 0 && index < contentIds.Length && !string.IsNullOrWhiteSpace(contentIds[index]))
            {
                return AnalyticsService.SanitizeToken(contentIds[index], 40, "content");
            }

            if (contentRoots != null && index >= 0 && index < contentRoots.Length && contentRoots[index] != null)
            {
                return AnalyticsService.SanitizeToken(contentRoots[index].name, 40, "content");
            }

            if (tabButtons != null && index >= 0 && index < tabButtons.Length && tabButtons[index] != null)
            {
                return AnalyticsService.SanitizeToken(tabButtons[index].name, 40, "content");
            }

            return "content_" + index;
        }

        private string ResolveContentName(int index, GameObject[] contentRoots, Button[] tabButtons)
        {
            if (contentNames != null && index >= 0 && index < contentNames.Length && !string.IsNullOrWhiteSpace(contentNames[index]))
            {
                return contentNames[index].Trim();
            }

            if (tabButtons != null && index >= 0 && index < tabButtons.Length && tabButtons[index] != null)
            {
                Text label = tabButtons[index].GetComponentInChildren<Text>(true);
                if (label != null && !string.IsNullOrWhiteSpace(label.text))
                {
                    return label.text.Trim();
                }
            }

            if (contentRoots != null && index >= 0 && index < contentRoots.Length && contentRoots[index] != null)
            {
                return contentRoots[index].name;
            }

            return "Content " + (index + 1);
        }

        private int ResolveExpectedContentCount(GameObject[] contentRoots)
        {
            if (expectedContentCount > 0)
            {
                return expectedContentCount;
            }

            if (contentIds != null && contentIds.Length > 0)
            {
                return contentIds.Length;
            }

            return contentRoots != null ? contentRoots.Length : 0;
        }

        private static bool LooksLikeInfographic(string contentId, string contentName)
        {
            string combined = (contentId + " " + contentName).ToLowerInvariant();
            return combined.Contains("infographic") || combined.Contains("infografik");
        }
    }

    public class ScenarioTracker : MonoBehaviour
    {
        [SerializeField] private string moduleId = TrainingAnalyticsFacade.Module2Id;
        [SerializeField] private string moduleName = TrainingAnalyticsFacade.Module2Name;
        [SerializeField] private string scenarioId = "scenario";
        [SerializeField] private string scenarioName = "Scenario";

        public void TrackStarted()
        {
            TrainingAnalyticsFacade.OnScenarioStarted(moduleId, moduleName, scenarioId, scenarioName);
        }

        public void TrackCriticalAction(string actionId, string actionName)
        {
            TrainingAnalyticsFacade.OnCriticalActionTaken(moduleId, moduleName, actionId, actionName);
        }

        public void TrackTaskCompleted(string taskId, string taskName)
        {
            TrainingAnalyticsFacade.OnScenarioTaskCompleted(moduleId, moduleName, scenarioId, scenarioName, taskId, taskName);
        }

        public void TrackCompleted()
        {
            TrainingAnalyticsFacade.OnScenarioCompleted(moduleId, moduleName, scenarioId, scenarioName);
        }
    }
}
