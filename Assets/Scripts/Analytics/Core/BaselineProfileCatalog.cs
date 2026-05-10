using UnityEngine;

namespace TrainingAnalytics
{
    /// <summary>
    /// Deterministic baseline profile used to seed Firestore placeholder
    /// documents for a brand-new session. Stable across runs (no Random).
    /// </summary>
    public sealed class BaselineProfile
    {
        public string Id;
        public int Index;            // 1..50
        public string Label;         // fast_high_success | medium_good_success | slow_low_success | mixed_success

        // Module durations (seconds)
        public float module1Duration;
        public float module2Duration;
        public float module3Duration;
        public float module4Duration;

        // Module 1
        public float module1LearningOverviewDuration;
        public int module1LearningCompleted;
        public int module1LearningTotal;
        public bool module1LearningSuccess;
        public int module1QuizAnswered;
        public int module1QuizCorrect;
        public int module1QuizTotal;
        public float module1QuizScorePercent;
        public float module1QuizDuration;
        public float module1AiDuration;
        public int module1SafetyCorrect;
        public int module1SafetyTotal;
        public float module1SafetyScorePercent;
        public float module1SafetyDuration;

        // Module 2
        public int module2VictimPlacementCompleted;
        public int module2VictimPlacementTotal;
        public bool module2VictimPlacementSuccess;
        public float module2VictimPlacementDuration;
        public float module2VictimPerStepDuration; // per first-aid step
        public float module2VictimPerVictimDuration; // per victim_placement_<id>
        public int module2QuizAnswered;
        public int module2QuizCorrect;
        public int module2QuizTotal;
        public float module2QuizScorePercent;
        public float module2QuizDuration;
        public float module2AiDuration;

        // Module 3
        public float module3PerCaseDuration;
        public float module3VictimDecisionDuration;
        public int module3TriageCorrect;
        public int module3TriageTotal;
        public float module3TriageScorePercent;
        public float module3TriageDuration;
        public float module3AiDuration;
        public int module3QuizAnswered;
        public int module3QuizCorrect;
        public int module3QuizTotal;
        public float module3QuizScorePercent;
        public float module3QuizDuration;

        // Module 4
        public int module4ConePlacementCompleted;
        public int module4ConePlacementTotal;
        public float module4ConePlacementDuration;
        public int module4EquipmentCompleted;
        public int module4EquipmentTotal;
        public float module4EquipmentDuration;
        public float module4RescueDuration;
        public int module4FireExtinguishCompleted;
        public int module4FireExtinguishTotal;
        public float module4FireExtinguishDuration;
        public int module4QuizAnswered;
        public int module4QuizCorrect;
        public int module4QuizTotal;
        public float module4QuizScorePercent;
        public float module4QuizDuration;
        public float module4AiDuration;
        public int module4SafetyCorrect;
        public int module4SafetyTotal;
        public float module4SafetyScorePercent;
        public float module4SafetyDuration;
    }

    public static class BaselineProfileCatalog
    {
        public const int ProfileCount = 50;
        public const string LabelFastHighSuccess = "fast_high_success";
        public const string LabelMediumGoodSuccess = "medium_good_success";
        public const string LabelSlowLowSuccess = "slow_low_success";
        public const string LabelMixedSuccess = "mixed_success";

        // Deterministic per-profile seed values. Index runs 0..49 (profile 1..50).
        // Layout per row:
        //   m1, m2, m3, m4,
        //   m1Learning, m1Quiz%, m1Quiz s, m1AI s, m1Safety%, m1Safety s,
        //   m2VictimAll s, m2VictimEach s, m2Step s, m2Quiz%, m2Quiz s, m2AI s,
        //   m3PerCase s, m3VictimDecision s, m3Triage%, m3Triage s, m3AI s, m3Quiz%, m3Quiz s,
        //   m4Cones s, m4Equipment s, m4Rescue s, m4Fire s, m4Quiz%, m4Quiz s, m4AI s, m4Safety%, m4Safety s
        // Quiz totals are always 5 (consistent with worktree); triage scores follow profile correctness band.
        // Values chosen to land neatly inside the requested ranges and to vary across profiles.
        private static readonly float[][] Seeds =
        {
            // ---- profiles 1-10: fast_high_success (low durations, near perfect correctness) ----
            new float[] { 200f, 450f, 550f, 380f,  180f, 100f, 70f, 20f, 100f, 80f,  90f, 25f, 35f, 100f, 90f, 25f,  35f, 35f, 100f, 520f, 45f, 100f, 85f,  50f, 45f, 110f, 90f, 100f, 80f, 20f, 100f, 380f },
            new float[] { 210f, 460f, 560f, 390f,  185f, 100f, 72f, 22f, 100f, 82f,  92f, 27f, 37f, 100f, 92f, 27f,  37f, 38f, 100f, 530f, 50f, 100f, 88f,  52f, 47f, 113f, 92f, 100f, 82f, 22f, 100f, 385f },
            new float[] { 220f, 470f, 570f, 400f,  190f, 100f, 74f, 24f, 100f, 84f,  94f, 29f, 39f, 100f, 94f, 29f,  39f, 40f, 100f, 540f, 55f, 100f, 90f,  54f, 50f, 116f, 94f, 100f, 84f, 24f, 100f, 390f },
            new float[] { 205f, 455f, 555f, 385f,  182f, 100f, 71f, 21f, 100f, 81f,  91f, 26f, 36f, 100f, 91f, 26f,  36f, 36f, 100f, 525f, 47f, 100f, 86f,  51f, 46f, 111f, 91f, 100f, 81f, 21f, 100f, 382f },
            new float[] { 215f, 465f, 565f, 395f,  187f, 100f, 73f, 23f, 100f, 83f,  93f, 28f, 38f, 100f, 93f, 28f,  38f, 39f, 100f, 535f, 52f, 100f, 89f,  53f, 49f, 115f, 93f, 100f, 83f, 23f, 100f, 388f },
            new float[] { 225f, 475f, 575f, 405f,  192f, 100f, 76f, 26f, 100f, 86f,  96f, 31f, 41f, 100f, 96f, 31f,  41f, 42f, 100f, 545f, 58f, 100f, 92f,  56f, 52f, 118f, 96f, 100f, 86f, 26f, 100f, 395f },
            new float[] { 230f, 480f, 580f, 410f,  195f, 100f, 78f, 28f, 100f, 88f,  98f, 33f, 43f, 100f, 98f, 33f,  43f, 44f, 100f, 550f, 60f, 100f, 95f,  58f, 54f, 120f, 98f, 100f, 88f, 28f, 100f, 400f },
            new float[] { 235f, 485f, 585f, 415f,  198f, 100f, 80f, 30f, 100f, 90f,  100f, 35f, 45f, 100f, 100f, 35f,  45f, 46f, 100f, 555f, 62f, 100f, 97f,  60f, 56f, 122f, 100f, 100f, 90f, 30f, 100f, 405f },
            new float[] { 240f, 490f, 590f, 420f,  200f, 100f, 82f, 32f, 100f, 92f,  102f, 37f, 47f, 100f, 102f, 37f,  47f, 48f, 100f, 560f, 64f, 100f, 99f,  62f, 58f, 124f, 102f, 100f, 92f, 32f, 100f, 410f },
            new float[] { 245f, 495f, 595f, 425f,  202f, 100f, 84f, 34f, 100f, 94f,  104f, 39f, 49f, 100f, 104f, 39f,  49f, 50f, 100f, 565f, 66f, 100f, 101f, 64f, 60f, 126f, 104f, 100f, 94f, 34f, 100f, 415f },

            // ---- profiles 11-25: medium_good_success ----
            new float[] { 260f, 505f, 615f, 440f,  220f, 90f, 90f, 35f, 90f, 88f,  115f, 38f, 50f, 90f, 110f, 40f,  55f, 55f, 90f, 580f, 70f, 90f, 100f, 65f, 60f, 130f, 110f, 90f, 95f, 35f, 90f, 420f },
            new float[] { 265f, 510f, 620f, 445f,  225f, 92f, 92f, 37f, 90f, 90f,  117f, 40f, 52f, 90f, 112f, 42f,  57f, 57f, 90f, 585f, 72f, 90f, 102f, 67f, 62f, 132f, 112f, 90f, 97f, 37f, 90f, 425f },
            new float[] { 270f, 515f, 625f, 450f,  230f, 88f, 95f, 40f, 88f, 92f,  120f, 42f, 55f, 88f, 115f, 45f,  60f, 60f, 88f, 590f, 75f, 88f, 105f, 70f, 65f, 135f, 115f, 88f, 100f, 40f, 88f, 430f },
            new float[] { 275f, 520f, 630f, 455f,  235f, 86f, 97f, 42f, 86f, 94f,  122f, 44f, 57f, 86f, 117f, 47f,  62f, 62f, 86f, 595f, 77f, 86f, 107f, 72f, 67f, 137f, 117f, 86f, 102f, 42f, 86f, 435f },
            new float[] { 280f, 525f, 635f, 460f,  240f, 84f, 100f, 45f, 84f, 96f,  125f, 46f, 60f, 84f, 120f, 50f,  65f, 65f, 84f, 600f, 80f, 84f, 110f, 75f, 70f, 140f, 120f, 84f, 105f, 45f, 84f, 440f },
            new float[] { 285f, 530f, 640f, 465f,  245f, 82f, 102f, 47f, 82f, 98f,  127f, 48f, 62f, 82f, 122f, 52f,  67f, 67f, 82f, 605f, 82f, 82f, 112f, 77f, 72f, 142f, 122f, 82f, 107f, 47f, 82f, 445f },
            new float[] { 290f, 535f, 645f, 470f,  250f, 80f, 105f, 50f, 80f, 100f, 130f, 50f, 65f, 80f, 125f, 55f,  70f, 70f, 80f, 610f, 85f, 80f, 115f, 80f, 75f, 145f, 125f, 80f, 110f, 50f, 80f, 450f },
            new float[] { 295f, 540f, 650f, 475f,  255f, 85f, 107f, 52f, 85f, 102f, 132f, 52f, 67f, 85f, 127f, 57f,  72f, 72f, 85f, 615f, 87f, 85f, 117f, 82f, 77f, 147f, 127f, 85f, 112f, 52f, 85f, 455f },
            new float[] { 300f, 545f, 655f, 480f,  260f, 90f, 110f, 55f, 90f, 104f, 135f, 55f, 70f, 90f, 130f, 60f,  75f, 75f, 90f, 620f, 90f, 90f, 120f, 85f, 80f, 150f, 130f, 90f, 115f, 55f, 90f, 460f },
            new float[] { 305f, 550f, 660f, 485f,  265f, 92f, 112f, 57f, 92f, 106f, 137f, 57f, 72f, 92f, 132f, 62f,  77f, 77f, 92f, 625f, 92f, 92f, 122f, 87f, 82f, 152f, 132f, 92f, 117f, 57f, 92f, 465f },
            new float[] { 270f, 555f, 665f, 490f,  235f, 88f, 95f, 38f, 88f, 90f,  120f, 40f, 52f, 88f, 115f, 42f,  58f, 58f, 88f, 588f, 73f, 88f, 103f, 68f, 63f, 133f, 113f, 88f, 98f, 38f, 88f, 432f },
            new float[] { 280f, 560f, 670f, 495f,  240f, 86f, 98f, 41f, 86f, 92f,  123f, 43f, 55f, 86f, 118f, 45f,  61f, 61f, 86f, 593f, 76f, 86f, 106f, 71f, 66f, 136f, 116f, 86f, 101f, 41f, 86f, 437f },
            new float[] { 290f, 565f, 675f, 500f,  245f, 84f, 101f, 44f, 84f, 94f,  126f, 46f, 58f, 84f, 121f, 48f,  64f, 64f, 84f, 598f, 79f, 84f, 109f, 74f, 69f, 139f, 119f, 84f, 104f, 44f, 84f, 442f },
            new float[] { 300f, 570f, 680f, 505f,  250f, 82f, 104f, 47f, 82f, 96f,  129f, 49f, 61f, 82f, 124f, 51f,  67f, 67f, 82f, 603f, 82f, 82f, 112f, 77f, 72f, 142f, 122f, 82f, 107f, 47f, 82f, 447f },
            new float[] { 310f, 575f, 685f, 510f,  255f, 80f, 107f, 50f, 80f, 98f,  132f, 52f, 64f, 80f, 127f, 54f,  70f, 70f, 80f, 608f, 85f, 80f, 115f, 80f, 75f, 145f, 125f, 80f, 110f, 50f, 80f, 452f },

            // ---- profiles 26-40: slow_low_success (high durations, some failures) ----
            new float[] { 320f, 580f, 700f, 490f,  280f, 70f, 110f, 60f, 60f, 110f, 140f, 50f, 65f, 70f, 130f, 65f,  78f, 78f, 70f, 660f, 110f, 60f, 125f, 88f, 80f, 160f, 140f, 75f, 115f, 55f, 70f, 470f },
            new float[] { 325f, 585f, 705f, 495f,  285f, 70f, 112f, 62f, 60f, 112f, 142f, 52f, 67f, 70f, 132f, 67f,  80f, 80f, 70f, 665f, 112f, 60f, 127f, 90f, 82f, 162f, 142f, 75f, 117f, 57f, 70f, 475f },
            new float[] { 330f, 590f, 710f, 500f,  290f, 65f, 115f, 65f, 60f, 115f, 145f, 55f, 70f, 65f, 135f, 70f,  82f, 82f, 65f, 670f, 115f, 60f, 130f, 92f, 85f, 165f, 145f, 70f, 120f, 60f, 65f, 480f },
            new float[] { 335f, 595f, 715f, 505f,  295f, 65f, 117f, 67f, 60f, 117f, 147f, 57f, 72f, 65f, 137f, 72f,  84f, 84f, 65f, 675f, 117f, 60f, 132f, 94f, 87f, 167f, 147f, 70f, 122f, 62f, 65f, 485f },
            new float[] { 340f, 600f, 720f, 510f,  300f, 60f, 120f, 70f, 60f, 120f, 150f, 60f, 75f, 60f, 140f, 75f,  86f, 86f, 60f, 680f, 120f, 60f, 135f, 96f, 90f, 170f, 150f, 65f, 125f, 65f, 60f, 490f },
            new float[] { 345f, 555f, 690f, 515f,  290f, 65f, 116f, 64f, 60f, 116f, 144f, 54f, 69f, 65f, 134f, 69f,  79f, 79f, 65f, 668f, 113f, 60f, 128f, 89f, 81f, 161f, 141f, 70f, 116f, 56f, 65f, 472f },
            new float[] { 350f, 560f, 695f, 520f,  295f, 60f, 119f, 67f, 60f, 119f, 147f, 57f, 72f, 60f, 137f, 72f,  82f, 82f, 60f, 673f, 116f, 60f, 131f, 92f, 84f, 164f, 144f, 65f, 119f, 59f, 60f, 477f },
            new float[] { 320f, 565f, 685f, 460f,  282f, 70f, 111f, 61f, 60f, 111f, 141f, 51f, 66f, 70f, 131f, 66f,  79f, 79f, 70f, 661f, 111f, 60f, 126f, 89f, 81f, 161f, 141f, 75f, 116f, 56f, 70f, 471f },
            new float[] { 325f, 570f, 690f, 465f,  287f, 65f, 113f, 63f, 60f, 113f, 143f, 53f, 68f, 65f, 133f, 68f,  81f, 81f, 65f, 666f, 113f, 60f, 128f, 91f, 83f, 163f, 143f, 70f, 118f, 58f, 65f, 476f },
            new float[] { 330f, 575f, 700f, 470f,  292f, 65f, 116f, 66f, 60f, 116f, 146f, 56f, 71f, 65f, 136f, 71f,  83f, 83f, 65f, 671f, 116f, 60f, 131f, 93f, 86f, 166f, 146f, 70f, 121f, 61f, 65f, 481f },
            new float[] { 335f, 580f, 710f, 475f,  297f, 60f, 118f, 68f, 60f, 118f, 148f, 58f, 73f, 60f, 138f, 73f,  85f, 85f, 60f, 676f, 118f, 60f, 133f, 95f, 88f, 168f, 148f, 65f, 123f, 63f, 60f, 486f },
            new float[] { 340f, 585f, 715f, 480f,  302f, 60f, 121f, 71f, 60f, 121f, 151f, 61f, 76f, 60f, 141f, 76f,  87f, 87f, 60f, 681f, 121f, 60f, 136f, 97f, 91f, 171f, 151f, 65f, 126f, 66f, 60f, 491f },
            new float[] { 345f, 590f, 720f, 485f,  307f, 60f, 124f, 74f, 60f, 124f, 154f, 64f, 79f, 60f, 144f, 79f,  89f, 89f, 60f, 686f, 124f, 60f, 139f, 99f, 94f, 174f, 154f, 65f, 129f, 69f, 60f, 496f },
            new float[] { 350f, 595f, 730f, 490f,  312f, 60f, 127f, 77f, 60f, 127f, 157f, 67f, 82f, 60f, 147f, 82f,  91f, 91f, 60f, 691f, 127f, 60f, 142f, 101f, 97f, 177f, 157f, 65f, 132f, 72f, 60f, 501f },
            new float[] { 350f, 600f, 740f, 495f,  315f, 60f, 130f, 80f, 60f, 130f, 160f, 70f, 85f, 60f, 150f, 85f,  93f, 93f, 60f, 696f, 130f, 60f, 145f, 103f, 100f, 180f, 160f, 65f, 135f, 75f, 60f, 506f },

            // ---- profiles 41-50: mixed_success (mixed per-module) ----
            new float[] { 220f, 580f, 600f, 450f,  200f, 100f, 80f, 28f, 100f, 90f,  100f, 33f, 45f, 70f, 130f, 65f,  45f, 45f, 100f, 540f, 65f, 70f, 122f, 60f, 55f, 125f, 110f, 100f, 95f, 30f, 100f, 410f },
            new float[] { 320f, 460f, 700f, 400f,  280f, 70f, 115f, 65f, 60f, 115f, 92f, 28f, 38f, 100f, 92f, 28f,  85f, 85f, 65f, 670f, 115f, 100f, 88f, 90f, 85f, 165f, 145f, 70f, 120f, 60f, 100f, 388f },
            new float[] { 240f, 540f, 720f, 480f,  205f, 100f, 84f, 30f, 100f, 92f,  120f, 45f, 60f, 80f, 120f, 50f,  90f, 90f, 60f, 690f, 125f, 60f, 135f, 60f, 55f, 130f, 115f, 100f, 100f, 32f, 100f, 460f },
            new float[] { 320f, 500f, 580f, 450f,  290f, 65f, 115f, 65f, 60f, 115f, 105f, 35f, 48f, 90f, 105f, 35f,  55f, 55f, 90f, 590f, 80f, 90f, 105f, 95f, 88f, 170f, 150f, 70f, 125f, 65f, 65f, 460f },
            new float[] { 230f, 600f, 690f, 510f,  195f, 100f, 78f, 26f, 100f, 88f,  140f, 60f, 75f, 60f, 140f, 75f,  82f, 82f, 65f, 660f, 110f, 65f, 130f, 65f, 60f, 130f, 115f, 100f, 100f, 35f, 100f, 500f },
            new float[] { 350f, 470f, 750f, 420f,  300f, 60f, 120f, 70f, 60f, 120f, 95f, 30f, 40f, 100f, 95f, 30f,  90f, 90f, 60f, 700f, 130f, 100f, 90f, 100f, 95f, 175f, 155f, 70f, 130f, 70f, 100f, 415f },
            new float[] { 220f, 560f, 600f, 510f,  200f, 100f, 80f, 28f, 100f, 90f,  115f, 40f, 53f, 80f, 115f, 45f,  50f, 50f, 100f, 530f, 60f, 70f, 120f, 95f, 90f, 170f, 150f, 65f, 125f, 65f, 100f, 500f },
            new float[] { 340f, 470f, 720f, 400f,  295f, 65f, 117f, 67f, 60f, 117f, 100f, 33f, 45f, 100f, 100f, 33f,  88f, 88f, 60f, 685f, 122f, 100f, 92f, 60f, 55f, 130f, 115f, 100f, 100f, 32f, 100f, 388f },
            new float[] { 250f, 590f, 640f, 460f,  220f, 90f, 92f, 36f, 90f, 95f,  130f, 50f, 65f, 70f, 130f, 65f,  60f, 60f, 80f, 600f, 85f, 75f, 110f, 70f, 65f, 140f, 120f, 90f, 105f, 42f, 90f, 425f },
            new float[] { 310f, 510f, 690f, 500f,  275f, 75f, 108f, 58f, 70f, 108f, 110f, 40f, 53f, 85f, 110f, 40f,  78f, 78f, 70f, 660f, 105f, 80f, 115f, 85f, 80f, 160f, 140f, 75f, 120f, 60f, 80f, 470f }
        };

        // Quiz totals are constant across profiles (worktree uses 5 questions per quiz).
        private const int QuizTotalQuestions = 5;

        public static BaselineProfile GetProfile(int index1Based)
        {
            int safeIndex = index1Based - 1;
            if (safeIndex < 0)
            {
                safeIndex = 0;
            }

            safeIndex = safeIndex % ProfileCount;
            float[] s = Seeds[safeIndex];
            int oneBased = safeIndex + 1;

            string label;
            if (safeIndex < 10)
            {
                label = LabelFastHighSuccess;
            }
            else if (safeIndex < 25)
            {
                label = LabelMediumGoodSuccess;
            }
            else if (safeIndex < 40)
            {
                label = LabelSlowLowSuccess;
            }
            else
            {
                label = LabelMixedSuccess;
            }

            int m1QuizCorrect = QuizCorrectFromPercent(s[5]);
            int m2QuizCorrect = QuizCorrectFromPercent(s[13]);
            int m3QuizCorrect = QuizCorrectFromPercent(s[21]);
            int m4QuizCorrect = QuizCorrectFromPercent(s[27]);

            int m1SafetyCorrect = SafetyCorrectFromPercent(s[8], 5);
            int m4SafetyCorrect = SafetyCorrectFromPercent(s[29], 5);

            // Triage breakdown — total cases come from TriageCaseCatalog at runtime; we
            // expose percentages and let the facade derive correctCount.
            return new BaselineProfile
            {
                Id = "profile_" + oneBased.ToString("00"),
                Index = oneBased,
                Label = label,

                module1Duration = s[0],
                module2Duration = s[1],
                module3Duration = s[2],
                module4Duration = s[3],

                module1LearningOverviewDuration = s[4],
                module1LearningCompleted = 4,
                module1LearningTotal = 4,
                module1LearningSuccess = true,
                module1QuizAnswered = QuizTotalQuestions,
                module1QuizCorrect = m1QuizCorrect,
                module1QuizTotal = QuizTotalQuestions,
                module1QuizScorePercent = s[5],
                module1QuizDuration = s[6],
                module1AiDuration = s[7],
                module1SafetyCorrect = m1SafetyCorrect,
                module1SafetyTotal = 5,
                module1SafetyScorePercent = s[8],
                module1SafetyDuration = s[9],

                module2VictimPlacementCompleted = 3,
                module2VictimPlacementTotal = 3,
                module2VictimPlacementSuccess = true,
                module2VictimPlacementDuration = s[10],
                module2VictimPerVictimDuration = s[11],
                module2VictimPerStepDuration = s[12],
                module2QuizAnswered = QuizTotalQuestions,
                module2QuizCorrect = m2QuizCorrect,
                module2QuizTotal = QuizTotalQuestions,
                module2QuizScorePercent = s[13],
                module2QuizDuration = s[14],
                module2AiDuration = s[15],

                module3PerCaseDuration = s[16],
                module3VictimDecisionDuration = s[17],
                module3TriageScorePercent = s[18],
                module3TriageDuration = s[19],
                module3AiDuration = s[20],
                module3QuizAnswered = QuizTotalQuestions,
                module3QuizCorrect = m3QuizCorrect,
                module3QuizTotal = QuizTotalQuestions,
                module3QuizScorePercent = s[21],
                module3QuizDuration = s[22],

                module4ConePlacementCompleted = 5,
                module4ConePlacementTotal = 5,
                module4ConePlacementDuration = s[23],
                module4EquipmentCompleted = 2,
                module4EquipmentTotal = 2,
                module4EquipmentDuration = s[24],
                module4RescueDuration = s[25],
                module4FireExtinguishCompleted = 10,
                module4FireExtinguishTotal = 10,
                module4FireExtinguishDuration = s[26],
                module4QuizAnswered = QuizTotalQuestions,
                module4QuizCorrect = m4QuizCorrect,
                module4QuizTotal = QuizTotalQuestions,
                module4QuizScorePercent = s[27],
                module4QuizDuration = s[28],
                module4AiDuration = s[29],
                module4SafetyCorrect = m4SafetyCorrect,
                module4SafetyTotal = 5,
                module4SafetyScorePercent = s[30],
                module4SafetyDuration = s[31]
            };
        }

        private static int QuizCorrectFromPercent(float percent)
        {
            // 5-question quiz: clamp to 0..5 by rounding the percentage of 5.
            int correct = Mathf.RoundToInt((percent / 100f) * QuizTotalQuestions);
            return Mathf.Clamp(correct, 0, QuizTotalQuestions);
        }

        private static int SafetyCorrectFromPercent(float percent, int total)
        {
            int correct = Mathf.RoundToInt((percent / 100f) * total);
            return Mathf.Clamp(correct, 0, total);
        }
    }
}
