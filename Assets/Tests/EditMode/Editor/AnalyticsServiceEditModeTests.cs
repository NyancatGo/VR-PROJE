using System;
using System.Collections.Generic;
using NUnit.Framework;
using TrainingAnalytics;

public class AnalyticsServiceEditModeTests
{
    [Test]
    public void InstallationId_PersistsAcrossTrackers_AndSessionIdChanges()
    {
        Dictionary<string, string> storage = new Dictionary<string, string>();

        SessionTracker firstTracker = CreateTracker(storage, 0d);
        SessionTracker secondTracker = CreateTracker(storage, 42d);

        Assert.AreEqual(firstTracker.InstallationId, secondTracker.InstallationId);
        Assert.AreNotEqual(firstTracker.SessionId, secondTracker.SessionId);
    }

    [Test]
    public void TrackEvent_MergesCommonParameters()
    {
        Dictionary<string, string> storage = new Dictionary<string, string>();
        SessionTracker tracker = CreateTracker(storage, 0d);
        AnalyticsService service = new AnalyticsService(tracker, false);
        FakeAnalyticsAdapter adapter = new FakeAnalyticsAdapter();

        service.MarkInitializationStarted();
        service.CompleteInitialization(adapter, "test");

        service.TrackEvent(
            AnalyticsEventNames.ModuleEntered,
            new Dictionary<string, object>
            {
                { AnalyticsParams.ModuleId, TrainingAnalyticsFacade.Module1Id },
                { "custom_flag", true }
            });

        Assert.AreEqual(1, adapter.Events.Count);
        Assert.IsTrue(adapter.Events[0].Parameters.ContainsKey(AnalyticsParams.InstallationId));
        Assert.IsTrue(adapter.Events[0].Parameters.ContainsKey(AnalyticsParams.SessionId));
        Assert.AreEqual(TrainingAnalyticsFacade.Module1Id, adapter.Events[0].Parameters[AnalyticsParams.ModuleId]);
        Assert.AreEqual(1L, adapter.Events[0].Parameters["custom_flag"]);
    }

    [Test]
    public void QueuedEvents_FlushAfterInitialization()
    {
        AnalyticsService service = new AnalyticsService(CreateTracker(new Dictionary<string, string>(), 0d), false);
        FakeAnalyticsAdapter adapter = new FakeAnalyticsAdapter();

        service.TrackEvent(AnalyticsEventNames.ModuleEntered, new Dictionary<string, object> { { AnalyticsParams.ModuleId, TrainingAnalyticsFacade.Module1Id } });

        Assert.AreEqual(1, service.QueuedEventCount);

        service.MarkInitializationStarted();
        service.CompleteInitialization(adapter, "test");

        Assert.AreEqual(0, service.QueuedEventCount);
        Assert.AreEqual(1, adapter.Events.Count);
    }

    [Test]
    public void ModuleDuration_IsCalculatedFromTrackerTimers()
    {
        Dictionary<string, string> storage = new Dictionary<string, string>();
        double elapsedSeconds = 0d;
        SessionTracker tracker = CreateTracker(storage, () => elapsedSeconds);

        tracker.BeginModule(TrainingAnalyticsFacade.Module1Id, TrainingAnalyticsFacade.Module1Name);
        elapsedSeconds = 12.75d;

        ModuleProgressSummary summary = tracker.CompleteModule(TrainingAnalyticsFacade.Module1Id, TrainingAnalyticsFacade.Module1Name);

        Assert.That(summary.durationSeconds, Is.EqualTo(12.75f).Within(0.001f));
    }

    [Test]
    public void DuplicateSuppression_PreventsSecondDispatch()
    {
        AnalyticsService service = new AnalyticsService(CreateTracker(new Dictionary<string, string>(), 0d), false);
        FakeAnalyticsAdapter adapter = new FakeAnalyticsAdapter();

        service.MarkInitializationStarted();
        service.CompleteInitialization(adapter, "test");

        service.TrackEvent("duplicate_event", null, true, "same_key");
        service.TrackEvent("duplicate_event", null, true, "same_key");

        Assert.AreEqual(1, adapter.Events.Count);
    }

    private static SessionTracker CreateTracker(Dictionary<string, string> storage, double elapsedSeconds)
    {
        return CreateTracker(storage, () => elapsedSeconds);
    }

    private static SessionTracker CreateTracker(Dictionary<string, string> storage, Func<double> elapsedSecondsProvider)
    {
        return new SessionTracker(
            key => storage.TryGetValue(key, out string storedValue) ? storedValue : string.Empty,
            (key, value) => storage[key] = value,
            () => DateTime.UnixEpoch,
            elapsedSecondsProvider,
            "training.analytics.tests.installation_id");
    }

    private sealed class FakeAnalyticsAdapter : IAnalyticsAdapter
    {
        public string AdapterName => "fake";
        public bool IsOperational => true;
        public List<FakeEvent> Events { get; } = new List<FakeEvent>();

        public void LogEvent(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            Events.Add(new FakeEvent
            {
                EventName = eventName,
                Parameters = new Dictionary<string, object>(parameters)
            });
        }
    }

    private sealed class FakeEvent
    {
        public string EventName;
        public Dictionary<string, object> Parameters;
    }
}
