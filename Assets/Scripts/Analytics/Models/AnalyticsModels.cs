using System;

namespace TrainingAnalytics
{
    /// <summary>
    /// Summary data for a completed training session.
    /// </summary>
    [Serializable]
    public struct SessionSummary
    {
        public string installationId;
        public string sessionId;
        public string sessionStartedUtc;
        public float  sessionDurationSeconds;
        public int    totalEvents;
    }

    /// <summary>
    /// Summary data for a module that was started or completed.
    /// </summary>
    [Serializable]
    public struct ModuleProgressSummary
    {
        public string moduleId;
        public string moduleName;
        public bool   completed;
        public float  durationSeconds;
    }

    /// <summary>
    /// Result data for a task (start / progress / complete / fail).
    /// </summary>
    [Serializable]
    public struct TaskResult
    {
        public string moduleId;
        public string moduleName;
        public string scenarioId;
        public string taskId;
        public string taskName;
        public string targetId;
        public string targetName;
        public float  progress;
        public int    completedCount;
        public int    totalCount;
        public float  durationSeconds;
        public bool   success;
    }

    /// <summary>
    /// Result data for an AI panel interaction.
    /// </summary>
    [Serializable]
    public struct AIInteractionResult
    {
        public string moduleId;
        public string moduleName;
        public string panelId;
        public string panelName;
        public string questionType;
        public float  durationSeconds;
    }

    /// <summary>
    /// Result data for a quiz session (start / answer / complete).
    /// </summary>
    [Serializable]
    public struct QuizResult
    {
        public string moduleId;
        public string moduleName;
        public string quizId;
        public string quizName;
        public int    totalQuestionCount;
        public int    answeredCount;
        public int    correctCount;
        public int    questionIndex;
        public int    selectedAnswerIndex;
        public int    correctAnswerIndex;
        public bool   isCorrect;
        public bool   completed;
        public float  durationSeconds;
        public float  scorePercentage;
    }

    /// <summary>
    /// Result data for a learning content item being opened.
    /// </summary>
    [Serializable]
    public struct LearningContentResult
    {
        public string moduleId;
        public string moduleName;
        public string contentId;
        public string contentName;
        public int    openedCount;
        public int    totalCount;
        public bool   completed;
        public bool   isInfographic;
    }

}
