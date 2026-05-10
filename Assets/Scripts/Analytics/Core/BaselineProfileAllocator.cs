using UnityEngine;

namespace TrainingAnalytics
{
    /// <summary>
    /// Round-robin allocator over <see cref="BaselineProfileCatalog"/>. The
    /// counter is persisted in PlayerPrefs so each fresh session lands on the
    /// next deterministic profile (1..50, then wraps).
    /// </summary>
    public static class BaselineProfileAllocator
    {
        private const string CounterKey = "training_baseline_profile_counter_v1";

        public static int CurrentCounter
        {
            get { return PlayerPrefs.GetInt(CounterKey, 0); }
        }

        public static BaselineProfile AllocateNext()
        {
            int counter = PlayerPrefs.GetInt(CounterKey, 0);
            int index = counter % BaselineProfileCatalog.ProfileCount;
            BaselineProfile profile = BaselineProfileCatalog.GetProfile(index + 1);

            PlayerPrefs.SetInt(CounterKey, counter + 1);
            PlayerPrefs.Save();
            return profile;
        }

        public static BaselineProfile PeekCurrent()
        {
            int counter = PlayerPrefs.GetInt(CounterKey, 0);
            int index = counter % BaselineProfileCatalog.ProfileCount;
            return BaselineProfileCatalog.GetProfile(index + 1);
        }

        public static void ResetForTesting()
        {
            PlayerPrefs.SetInt(CounterKey, 0);
            PlayerPrefs.Save();
        }
    }
}
