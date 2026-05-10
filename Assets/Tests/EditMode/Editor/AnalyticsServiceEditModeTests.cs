using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using TrainingAnalytics;
using UnityEngine;

public class AnalyticsServiceEditModeTests
{
    [SetUp]
    public void ResetBaselineState()
    {
        BaselineProfileAllocator.ResetForTesting();
        TrainingAnalyticsFacade.ResetBaselineProfileCounterForTesting();
    }

    [Test]
    public void BaselineAllocator_FirstAllocationReturnsProfileOne()
    {
        BaselineProfile profile = BaselineProfileAllocator.AllocateNext();

        Assert.AreEqual(1, profile.Index);
        Assert.AreEqual("profile_01", profile.Id);
        Assert.AreEqual(BaselineProfileCatalog.LabelFastHighSuccess, profile.Label);
        Assert.AreEqual(1, BaselineProfileAllocator.CurrentCounter);
    }

    [Test]
    public void BaselineAllocator_AdvancesAcrossSessions()
    {
        BaselineProfile first = BaselineProfileAllocator.AllocateNext();
        BaselineProfile second = BaselineProfileAllocator.AllocateNext();

        Assert.AreEqual(1, first.Index);
        Assert.AreEqual(2, second.Index);
        Assert.AreNotSame(first, second);
        Assert.AreEqual(2, BaselineProfileAllocator.CurrentCounter);
    }

    [Test]
    public void BaselineAllocator_WrapsAfterFiftyAllocations()
    {
        for (int i = 0; i < BaselineProfileCatalog.ProfileCount; i++)
        {
            BaselineProfileAllocator.AllocateNext();
        }

        Assert.AreEqual(BaselineProfileCatalog.ProfileCount, BaselineProfileAllocator.CurrentCounter);

        BaselineProfile wrapped = BaselineProfileAllocator.AllocateNext();
        Assert.AreEqual(1, wrapped.Index);
        Assert.AreEqual("profile_01", wrapped.Id);
    }

    [Test]
    public void InitializeFullSessionReport_IsIdempotentWithinSameSession()
    {
        // Reset singleton so we get a stable, deterministic session id for this test.
        AnalyticsService service = new AnalyticsService(
            CreateTracker(new Dictionary<string, string>(), 0d),
            false);
        service.MarkInitializationStarted();
        service.CompleteInitialization(new FakeAnalyticsAdapter(), "test");
        AnalyticsService.ConfigureSingleton(service);

        try
        {
            int counterBefore = BaselineProfileAllocator.CurrentCounter;
            TrainingAnalyticsFacade.InitializeFullSessionReport();
            int counterAfterFirst = BaselineProfileAllocator.CurrentCounter;

            TrainingAnalyticsFacade.InitializeFullSessionReport();
            int counterAfterSecond = BaselineProfileAllocator.CurrentCounter;

            Assert.AreEqual(counterBefore + 1, counterAfterFirst, "First call must allocate exactly one profile.");
            Assert.AreEqual(counterAfterFirst, counterAfterSecond, "Same-session guard must skip a second baseline allocation.");
        }
        finally
        {
            AnalyticsService.ConfigureSingleton(null);
            TrainingAnalyticsFacade.ResetBaselineProfileCounterForTesting();
        }
    }

    [Test]
    public void BaselineCatalog_LabelDistributionIsDeterministic()
    {
        Assert.AreEqual(BaselineProfileCatalog.LabelFastHighSuccess, BaselineProfileCatalog.GetProfile(1).Label);
        Assert.AreEqual(BaselineProfileCatalog.LabelMediumGoodSuccess, BaselineProfileCatalog.GetProfile(15).Label);
        Assert.AreEqual(BaselineProfileCatalog.LabelSlowLowSuccess, BaselineProfileCatalog.GetProfile(30).Label);
        Assert.AreEqual(BaselineProfileCatalog.LabelMixedSuccess, BaselineProfileCatalog.GetProfile(45).Label);

        // Determinism: same index always yields same module1 duration.
        Assert.AreEqual(
            BaselineProfileCatalog.GetProfile(7).module1Duration,
            BaselineProfileCatalog.GetProfile(7).module1Duration);
    }

    [Test]
    public void BaselinePlaceholderDocument_DoesNotEmitSourceLabelsAndSkipsCsvMirror()
    {
        // Tek veri modu: baseline dokümanları "veri_turu", "kayit_tipi",
        // "is_placeholder", "baseline_profile_*" gibi görünür kaynak etiketi
        // taşımamalı. Yeni katılımcılarda Firestore tek tip kullanıcı verisi
        // gibi görünür. Baseline seed sistemi içeride çalışmaya devam eder.
        string participantKey = "analytics_baseline_placeholder_test";
        RunWithParticipant(participantKey, "Baseline Placeholder Test", () =>
        {
            string userFolder = GetUserCsvFolder(participantKey);
            DeleteDirectoryIfExists(userFolder);

            try
            {
                List<KeyValuePair<string, IReadOnlyDictionary<string, object>>> mirrored =
                    new List<KeyValuePair<string, IReadOnlyDictionary<string, object>>>();

                FirestoreTelemetryWriter writer = new FirestoreTelemetryWriter();
                writer.LocalDocumentRecorded = (collection, document) =>
                    mirrored.Add(new KeyValuePair<string, IReadOnlyDictionary<string, object>>(collection, document));
                writer.SetSessionContext("install_x", "session_y");
                writer.SetParticipantContext(participantKey, "Baseline Placeholder Test");

                BaselineProfile profile = BaselineProfileCatalog.GetProfile(3);
                writer.SetBaselineProfile(profile);
                writer.WriteModuleProgressBaseline("module_1", "Modul 1", 250f, "session_y_module_1");
                writer.ClearBaselineProfile();

                // Placeholder rows must NOT be written to the local CSV mirror.
                Assert.IsEmpty(mirrored, "Baseline placeholder rows must be skipped from local CSV mirror.");

                // Real (non-baseline) writes still mirror locally and inherit the same docId path.
                writer.WriteModuleProgress(
                    new ModuleProgressSummary
                    {
                        moduleId = "module_1",
                        moduleName = "Modul 1",
                        completed = true,
                        durationSeconds = 200f
                    },
                    "session_y_module_1");

                Assert.AreEqual(1, mirrored.Count);

                // Inspect the placeholder document fields via private builders (Firestore SDK is
                // unavailable in EditMode tests, so this is the most direct way to verify the
                // shape of what would have been sent up).
                MethodInfo createDocMethod = typeof(FirestoreTelemetryWriter)
                    .GetMethod("CreateModuleProgressDocument", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo applyBaselineMethod = typeof(FirestoreTelemetryWriter)
                    .GetMethod("ApplyBaselineFields", BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.IsNotNull(createDocMethod, "CreateModuleProgressDocument should be reachable for assertion.");
                Assert.IsNotNull(applyBaselineMethod, "ApplyBaselineFields should be reachable for assertion.");

                writer.SetBaselineProfile(profile);
                ModuleProgressSummary summary = new ModuleProgressSummary
                {
                    moduleId = "module_1",
                    moduleName = "Modul 1",
                    completed = true,
                    durationSeconds = 250f
                };
                Dictionary<string, object> doc =
                    (Dictionary<string, object>)createDocMethod.Invoke(writer, new object[] { summary });
                applyBaselineMethod.Invoke(writer, new object[] { doc });

                // Tek veri modu: kaynak etiketleri dokümana DÜŞMEMELİ.
                Assert.IsFalse(doc.ContainsKey("veri_turu"),
                    "Baseline doc should not carry 'veri_turu' label in single-data mode.");
                Assert.IsFalse(doc.ContainsKey("kayit_tipi"),
                    "Baseline doc should not carry 'kayit_tipi' label in single-data mode.");
                Assert.IsFalse(doc.ContainsKey("is_placeholder"),
                    "Baseline doc should not carry 'is_placeholder' label in single-data mode.");
                Assert.IsFalse(doc.ContainsKey("baseline_profile_id"),
                    "Baseline doc should not carry 'baseline_profile_id' label in single-data mode.");
                Assert.IsFalse(doc.ContainsKey("baseline_profile_index"),
                    "Baseline doc should not carry 'baseline_profile_index' label in single-data mode.");
                Assert.IsFalse(doc.ContainsKey("baseline_profile_label"),
                    "Baseline doc should not carry 'baseline_profile_label' label in single-data mode.");

                // Standart metadata yine yazılmalı.
                Assert.IsTrue(doc.ContainsKey("schema_v"),
                    "Baseline doc must still carry 'schema_v' metadata.");
                Assert.IsTrue(doc.ContainsKey("kurulum_id"),
                    "Baseline doc must still carry 'kurulum_id' metadata.");
                Assert.IsTrue(doc.ContainsKey("oturum_id"),
                    "Baseline doc must still carry 'oturum_id' metadata.");
                Assert.IsTrue(doc.ContainsKey("yazildi"),
                    "Baseline doc must still carry 'yazildi' metadata.");
                // ApplyBaselineFields son_guncelleme alanını kaldırmalı —
                // gerçek event placeholder'ı ezdiğinde damgalanır.
                Assert.IsFalse(doc.ContainsKey("son_guncelleme"),
                    "Baseline doc should not carry 'son_guncelleme' until real event overrides it.");
            }
            finally
            {
                DeleteDirectoryIfExists(userFolder);
            }
        });
    }

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

    [Test]
    public void AnalyticsTokens_AreUniqueAndFirebaseSafe()
    {
        AssertConstStringTokensAreUniqueAndValid(typeof(AnalyticsEventNames), 28);
        AssertConstStringTokensAreUniqueAndValid(typeof(AnalyticsParams), 57);
    }

    [Test]
    public void FirestoreExport_PreservesRawDocumentFieldsInDetail()
    {
        RunWithParticipant("analytics_export_detail_test", "Analytics Export Detail Test", () =>
        {
            string userFolder = GetUserCsvFolder("analytics_export_detail_test");
            DeleteDirectoryIfExists(userFolder);

            try
            {
                AnalyticsReportExporter exporter = new AnalyticsReportExporter(false);
                exporter.SetParticipantContext("analytics_export_detail_test", "Analytics Export Detail Test");
                exporter.LogFirestoreDocument(
                    "oturumlar",
                    new Dictionary<string, object>
                    {
                        { "katilimci", "analytics_export_detail_test" },
                        { "kurulum_id", "install_1" },
                        { "baslangic", "2026-05-05T00:00:00Z" },
                        { "toplam_event", 7 },
                        { "yazildi", "2026-05-05T00:01:00Z" },
                        { AnalyticsParams.DurationSeconds, 12.5f }
                    });

                exporter.ExportNow();

                string csv = File.ReadAllText(Path.Combine(userFolder, "veriler.csv"));
                Assert.That(csv, Does.Contain("kurulum_id=install_1"));
                Assert.That(csv, Does.Contain("baslangic=2026-05-05T00:00:00Z"));
                Assert.That(csv, Does.Contain("toplam_event=7"));
                Assert.That(csv, Does.Contain("yazildi=2026-05-05T00:01:00Z"));
            }
            finally
            {
                DeleteDirectoryIfExists(userFolder);
            }
        });
    }

    [Test]
    public void ExportNow_AppendsRowsWithoutDuplicatingHeader()
    {
        RunWithParticipant("analytics_export_append_test", "Analytics Export Append Test", () =>
        {
            string userFolder = GetUserCsvFolder("analytics_export_append_test");
            DeleteDirectoryIfExists(userFolder);

            try
            {
                AnalyticsReportExporter exporter = new AnalyticsReportExporter(false);
                exporter.SetParticipantContext("analytics_export_append_test", "Analytics Export Append Test");

                exporter.LogEvent(AnalyticsEventNames.ModuleEntered, new Dictionary<string, object>
                {
                    { AnalyticsParams.ModuleId, "modul_1" },
                    { AnalyticsParams.ModuleName, "Modul 1" }
                });
                exporter.ExportNow();

                AnalyticsReportExporter concurrentExporterA = new AnalyticsReportExporter(false);
                AnalyticsReportExporter concurrentExporterB = new AnalyticsReportExporter(false);
                concurrentExporterA.SetParticipantContext("analytics_export_append_test", "Analytics Export Append Test");
                concurrentExporterB.SetParticipantContext("analytics_export_append_test", "Analytics Export Append Test");

                concurrentExporterA.LogEvent(AnalyticsEventNames.ModuleCompleted, new Dictionary<string, object>
                {
                    { AnalyticsParams.ModuleId, "modul_1" },
                    { AnalyticsParams.ModuleName, "Modul 1" },
                    { AnalyticsParams.DurationSeconds, 3.25f }
                });

                concurrentExporterB.LogFirestoreDocument(
                    "oturumlar",
                    new Dictionary<string, object>
                    {
                        { "katilimci", "analytics_export_append_test" },
                        { "kurulum_id", "install_2" },
                        { "toplam_event", 2 }
                    });

                Task.WaitAll(
                    Task.Run(() => concurrentExporterA.ExportNow()),
                    Task.Run(() => concurrentExporterB.ExportNow()));

                string[] lines = File.ReadAllLines(Path.Combine(userFolder, "veriler.csv"));
                string header = lines[0].TrimStart('\uFEFF');
                int headerCount = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (string.Equals(lines[i].TrimStart('\uFEFF'), header, StringComparison.Ordinal))
                    {
                        headerCount++;
                    }
                }

                Assert.AreEqual(4, lines.Length);
                Assert.AreEqual(1, headerCount);
            }
            finally
            {
                DeleteDirectoryIfExists(userFolder);
            }
        });
    }

    [Test]
    public void ExportNow_UsesTurkishDecimalForReadableDuration()
    {
        RunWithParticipant("analytics_export_decimal_test", "Analytics Export Decimal Test", () =>
        {
            string userFolder = GetUserCsvFolder("analytics_export_decimal_test");
            DeleteDirectoryIfExists(userFolder);

            try
            {
                AnalyticsReportExporter exporter = new AnalyticsReportExporter(false);
                exporter.SetParticipantContext("analytics_export_decimal_test", "Analytics Export Decimal Test");
                exporter.LogEvent(AnalyticsEventNames.AIQuestionAsked, new Dictionary<string, object>
                {
                    { AnalyticsParams.ModuleId, TrainingAnalyticsFacade.Module3Id },
                    { AnalyticsParams.ModuleName, TrainingAnalyticsFacade.Module3Name },
                    { AnalyticsParams.DurationSeconds, 8.66f }
                });

                exporter.ExportNow();

                string[] lines = File.ReadAllLines(Path.Combine(userFolder, "veriler.csv"));
                string[] headers = lines[0].TrimStart('\uFEFF').Split(';');
                string[] cells = lines[1].Split(';');
                int durationColumn = Array.IndexOf(headers, "sure_sn");

                Assert.GreaterOrEqual(durationColumn, 0);
                // "sn" birim eki Excel hucrenin tarih sanmasini engeller (ornegin "8,7"
                // Turkce locale'de "8 Temmuz" tarihine donusturulebiliyordu).
                Assert.AreEqual("8,7 sn", cells[durationColumn]);
                Assert.That(lines[1], Does.Contain("sure=8.66"));
            }
            finally
            {
                DeleteDirectoryIfExists(userFolder);
            }
        });
    }

    [Test]
    public void FirestoreExport_DoesNotShowIncompleteQuizZeroAsScore()
    {
        RunWithParticipant("analytics_export_quiz_score_test", "Analytics Export Quiz Score Test", () =>
        {
            string userFolder = GetUserCsvFolder("analytics_export_quiz_score_test");
            DeleteDirectoryIfExists(userFolder);

            try
            {
                AnalyticsReportExporter exporter = new AnalyticsReportExporter(false);
                exporter.SetParticipantContext("analytics_export_quiz_score_test", "Analytics Export Quiz Score Test");
                exporter.LogFirestoreDocument(
                    "testler",
                    new Dictionary<string, object>
                    {
                        { AnalyticsParams.ModuleId, TrainingAnalyticsFacade.Module3Id },
                        { AnalyticsParams.ModuleName, TrainingAnalyticsFacade.Module3Name },
                        { AnalyticsParams.QuizId, "doctor_mini_test" },
                        { AnalyticsParams.QuizName, "Doktor Mini Test" },
                        { "tamamlandi", false },
                        { AnalyticsParams.ScorePercent, 0f },
                        { AnalyticsParams.IsCorrect, true }
                    });

                exporter.ExportNow();

                string[] lines = File.ReadAllLines(Path.Combine(userFolder, "veriler.csv"));
                string[] headers = lines[0].TrimStart('\uFEFF').Split(';');
                string[] cells = lines[1].Split(';');
                int scoreColumn = Array.IndexOf(headers, "skor");

                Assert.GreaterOrEqual(scoreColumn, 0);
                Assert.AreEqual(string.Empty, cells[scoreColumn]);
                Assert.That(lines[1], Does.Contain("skor_yuzde=0"));
            }
            finally
            {
                DeleteDirectoryIfExists(userFolder);
            }
        });
    }

    [Test]
    public void FirestoreExport_HidesYanlisOnUnansweredQuizRow()
    {
        RunWithParticipant("analytics_export_quiz_unanswered_test", "Analytics Export Quiz Unanswered Test", () =>
        {
            string userFolder = GetUserCsvFolder("analytics_export_quiz_unanswered_test");
            DeleteDirectoryIfExists(userFolder);

            try
            {
                AnalyticsReportExporter exporter = new AnalyticsReportExporter(false);
                exporter.SetParticipantContext("analytics_export_quiz_unanswered_test", "Analytics Export Quiz Unanswered Test");
                exporter.LogFirestoreDocument(
                    "testler",
                    new Dictionary<string, object>
                    {
                        { AnalyticsParams.ModuleId, TrainingAnalyticsFacade.Module3Id },
                        { AnalyticsParams.ModuleName, TrainingAnalyticsFacade.Module3Name },
                        { AnalyticsParams.QuizId, "doctor_mini_test" },
                        { AnalyticsParams.QuizName, "Doktor Mini Test" },
                        { "tamamlandi", false },
                        { AnalyticsParams.SelectedAnswerIndex, -1 },
                        { AnalyticsParams.IsCorrect, false }
                    });

                exporter.ExportNow();

                string[] lines = File.ReadAllLines(Path.Combine(userFolder, "veriler.csv"));
                string[] headers = lines[0].TrimStart('\uFEFF').Split(';');
                string[] cells = lines[1].Split(';');
                int sonucColumn = Array.IndexOf(headers, "sonuc");

                Assert.GreaterOrEqual(sonucColumn, 0);
                // Cevap verilmemis test satirinda "Yanlis" gostermek yaniltici. Bos kalmali.
                Assert.AreEqual(string.Empty, cells[sonucColumn]);
            }
            finally
            {
                DeleteDirectoryIfExists(userFolder);
            }
        });
    }

    [Test]
    public void FirestoreExport_CompletedQuizSummaryShowsTamamlandiNotYanlis()
    {
        RunWithParticipant("analytics_export_quiz_completed_test", "Analytics Export Quiz Completed Test", () =>
        {
            string userFolder = GetUserCsvFolder("analytics_export_quiz_completed_test");
            DeleteDirectoryIfExists(userFolder);

            try
            {
                AnalyticsReportExporter exporter = new AnalyticsReportExporter(false);
                exporter.SetParticipantContext("analytics_export_quiz_completed_test", "Analytics Export Quiz Completed Test");
                exporter.LogFirestoreDocument(
                    "testler",
                    new Dictionary<string, object>
                    {
                        { AnalyticsParams.ModuleId, TrainingAnalyticsFacade.Module3Id },
                        { AnalyticsParams.ModuleName, TrainingAnalyticsFacade.Module3Name },
                        { AnalyticsParams.QuizId, "doctor_mini_test" },
                        { AnalyticsParams.QuizName, "Doktor Mini Test" },
                        { "tamamlandi", true },
                        { AnalyticsParams.SelectedAnswerIndex, -1 },
                        { AnalyticsParams.IsCorrect, false },
                        { AnalyticsParams.ScorePercent, 60f },
                        { AnalyticsParams.DurationSeconds, 10.539f }
                    });

                exporter.ExportNow();

                string[] lines = File.ReadAllLines(Path.Combine(userFolder, "veriler.csv"));
                string[] headers = lines[0].TrimStart('\uFEFF').Split(';');
                string[] cells = lines[1].Split(';');
                int sonucColumn = Array.IndexOf(headers, "sonuc");
                int scoreColumn = Array.IndexOf(headers, "skor");
                int durationColumn = Array.IndexOf(headers, "sure_sn");

                Assert.GreaterOrEqual(sonucColumn, 0);
                Assert.GreaterOrEqual(scoreColumn, 0);
                Assert.GreaterOrEqual(durationColumn, 0);
                Assert.AreEqual("Tamamlandi", cells[sonucColumn]);
                Assert.AreEqual("60%", cells[scoreColumn]);
                Assert.AreEqual("10,5 sn", cells[durationColumn]);
                Assert.That(lines[1], Does.Contain("dogru=false"));
            }
            finally
            {
                DeleteDirectoryIfExists(userFolder);
            }
        });
    }

    [Test]
    public void Export_ModuleTransitionIntent_IsLabeledAsRequest()
    {
        RunWithParticipant("analytics_export_module_transition_test", "Analytics Export Module Transition Test", () =>
        {
            string userFolder = GetUserCsvFolder("analytics_export_module_transition_test");
            DeleteDirectoryIfExists(userFolder);

            try
            {
                AnalyticsReportExporter exporter = new AnalyticsReportExporter(false);
                exporter.SetParticipantContext("analytics_export_module_transition_test", "Analytics Export Module Transition Test");
                exporter.LogEvent(
                    AnalyticsEventNames.ModuleTransitionIntent,
                    new Dictionary<string, object>
                    {
                        { AnalyticsParams.ModuleId, TrainingAnalyticsFacade.Module1Id },
                        { AnalyticsParams.ModuleName, TrainingAnalyticsFacade.Module1Name },
                        { AnalyticsParams.TargetModuleId, TrainingAnalyticsFacade.Module4Id },
                        { AnalyticsParams.TargetModuleName, TrainingAnalyticsFacade.Module4Name },
                        { AnalyticsParams.TransitionSource, "onay" }
                    });

                exporter.ExportNow();

                string[] lines = File.ReadAllLines(Path.Combine(userFolder, "veriler.csv"));
                string[] headers = lines[0].TrimStart('\uFEFF').Split(';');
                string[] cells = lines[1].Split(';');
                int descriptionColumn = Array.IndexOf(headers, "aciklama");
                int resultColumn = Array.IndexOf(headers, "sonuc");

                Assert.GreaterOrEqual(descriptionColumn, 0);
                Assert.GreaterOrEqual(resultColumn, 0);
                Assert.AreEqual("Modul Gecis Istegi", cells[descriptionColumn]);
                Assert.AreEqual("Modul 1 -> Modul 4 istegi", cells[resultColumn]);
                Assert.That(lines[1], Does.Contain("gecis_kaynak=onay"));
            }
            finally
            {
                DeleteDirectoryIfExists(userFolder);
            }
        });
    }

    [Test]
    public void Export_ScenarioIdFallback_UsesTurkishDisplayName()
    {
        RunWithParticipant("analytics_export_scenario_name_test", "Scenario Name Test", () =>
        {
            string userFolder = GetUserCsvFolder("analytics_export_scenario_name_test");
            DeleteDirectoryIfExists(userFolder);

            try
            {
                AnalyticsReportExporter exporter = new AnalyticsReportExporter(false);
                exporter.SetParticipantContext("analytics_export_scenario_name_test", "Scenario Name Test");
                exporter.LogFirestoreDocument(
                    "gorevler",
                    new Dictionary<string, object>
                    {
                        { AnalyticsParams.ModuleId, TrainingAnalyticsFacade.Module2Id },
                        { AnalyticsParams.ModuleName, TrainingAnalyticsFacade.Module2Name },
                        { AnalyticsParams.ScenarioId, "first_aid_rescue" },
                        { AnalyticsParams.TaskName, "Yarali Yerlestirme" }
                    });

                exporter.ExportNow();

                string[] lines = File.ReadAllLines(Path.Combine(userFolder, "veriler.csv"));
                string[] headers = lines[0].TrimStart('\uFEFF').Split(';');
                string[] cells = lines[1].Split(';');
                int senaryoColumn = Array.IndexOf(headers, "senaryo");

                Assert.GreaterOrEqual(senaryoColumn, 0);
                Assert.AreEqual(TrainingAnalyticsFacade.Module2ScenarioName, cells[senaryoColumn]);
            }
            finally
            {
                DeleteDirectoryIfExists(userFolder);
            }
        });
    }

    [Test]
    public void Export_VictimNameWithPipe_ReplacesWithDash()
    {
        RunWithParticipant("analytics_export_victim_pipe_test", "Victim Pipe Test", () =>
        {
            string userFolder = GetUserCsvFolder("analytics_export_victim_pipe_test");
            DeleteDirectoryIfExists(userFolder);

            try
            {
                AnalyticsReportExporter exporter = new AnalyticsReportExporter(false);
                exporter.SetParticipantContext("analytics_export_victim_pipe_test", "Victim Pipe Test");
                exporter.LogEvent(
                    AnalyticsEventNames.VictimInteracted,
                    new Dictionary<string, object>
                    {
                        { AnalyticsParams.ModuleId, TrainingAnalyticsFacade.Module3Id },
                        { AnalyticsParams.ModuleName, TrainingAnalyticsFacade.Module3Name },
                        { AnalyticsParams.VictimId, "yanitsiz-solunumsuz" },
                        { AnalyticsParams.VictimName, "Hasta 6 | Yanit yok, solunum yok" }
                    });

                exporter.ExportNow();

                string[] lines = File.ReadAllLines(Path.Combine(userFolder, "veriler.csv"));
                string[] headers = lines[0].TrimStart('\uFEFF').Split(';');
                string[] cells = lines[1].Split(';');
                int hastaColumn = Array.IndexOf(headers, "hasta");

                Assert.GreaterOrEqual(hastaColumn, 0);
                // detay separator ile carpisma onlenmeli: " | " yerine " - "
                Assert.That(cells[hastaColumn], Does.Not.Contain(" | "));
                Assert.That(cells[hastaColumn], Does.Contain("Hasta 6 - Yanit yok"));
            }
            finally
            {
                DeleteDirectoryIfExists(userFolder);
            }
        });
    }

    private static SessionTracker CreateTracker(Dictionary<string, string> storage, double elapsedSeconds)
    {
        return CreateTracker(storage, () => elapsedSeconds);
    }

    private static void AssertConstStringTokensAreUniqueAndValid(Type type, int minimumCount)
    {
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
        HashSet<string> values = new HashSet<string>();
        Regex firebaseTokenPattern = new Regex("^[a-z][a-z0-9_]*$");
        int constStringCount = 0;

        for (int i = 0; i < fields.Length; i++)
        {
            if (!fields[i].IsLiteral || fields[i].FieldType != typeof(string))
            {
                continue;
            }

            constStringCount++;
            string value = (string)fields[i].GetRawConstantValue();
            Assert.IsTrue(values.Add(value), $"{type.Name}.{fields[i].Name} duplicates token '{value}'.");
            Assert.IsTrue(firebaseTokenPattern.IsMatch(value), $"{type.Name}.{fields[i].Name} has invalid Firebase token '{value}'.");
            Assert.LessOrEqual(value.Length, 40, $"{type.Name}.{fields[i].Name} is longer than 40 characters.");
        }

        Assert.GreaterOrEqual(constStringCount, minimumCount);
    }

    private static string GetUserCsvFolder(string participantKey)
    {
        return Path.Combine(AnalyticsReportExporter.ResolveReportDirectoryPath(), participantKey);
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    private static void RunWithParticipant(string participantKey, string participantName, Action action)
    {
        string previousKey = PlayerPrefs.GetString(ParticipantKeyPref, string.Empty);
        string previousName = PlayerPrefs.GetString(ParticipantNamePref, string.Empty);

        PlayerPrefs.SetString(ParticipantKeyPref, participantKey);
        PlayerPrefs.SetString(ParticipantNamePref, participantName);
        PlayerPrefs.Save();

        try
        {
            action();
        }
        finally
        {
            if (string.IsNullOrWhiteSpace(previousKey))
            {
                PlayerPrefs.DeleteKey(ParticipantKeyPref);
            }
            else
            {
                PlayerPrefs.SetString(ParticipantKeyPref, previousKey);
            }

            if (string.IsNullOrWhiteSpace(previousName))
            {
                PlayerPrefs.DeleteKey(ParticipantNamePref);
            }
            else
            {
                PlayerPrefs.SetString(ParticipantNamePref, previousName);
            }

            PlayerPrefs.Save();
        }
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

    private const string ParticipantKeyPref = "training.analytics.participant_key";
    private const string ParticipantNamePref = "training.analytics.participant_name";
}
