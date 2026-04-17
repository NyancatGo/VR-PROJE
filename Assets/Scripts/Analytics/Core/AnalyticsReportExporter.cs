using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace TrainingAnalytics
{
    public class AnalyticsReportExporter : IAnalyticsAdapter
    {
        private readonly bool emitLogs;
        private readonly string reportDirectory;
        private readonly object syncRoot = new object();
        private readonly List<Dictionary<string, object>> analyticsEventRows = new List<Dictionary<string, object>>();
        private readonly Dictionary<string, List<Dictionary<string, object>>> firestoreRowsByCollection =
            new Dictionary<string, List<Dictionary<string, object>>>();

        private string participantKeyContext = string.Empty;
        private string participantNameContext = string.Empty;
        private int localSequence;

        // Semicolon is friendlier for Turkish Excel regional settings.
        private const string CsvSeparator = ";";

        private static readonly string[] EventPreferredColumns =
        {
            "sira_no",
            "kayit_utc",
            "event_name",
            "event_label",
            "participant_key",
            "participant_name",
            AnalyticsParams.InstallationId,
            AnalyticsParams.SessionId,
            AnalyticsParams.BuildVersion,
            AnalyticsParams.SceneName,
            AnalyticsParams.RuntimePlatform,
            AnalyticsParams.ModuleId,
            AnalyticsParams.ModuleName,
            AnalyticsParams.ScenarioId,
            AnalyticsParams.ScenarioName,
            AnalyticsParams.ContentId,
            AnalyticsParams.ContentName,
            AnalyticsParams.ContentType,
            AnalyticsParams.TaskId,
            AnalyticsParams.TaskName,
            AnalyticsParams.TaskType,
            AnalyticsParams.TaskStatus,
            AnalyticsParams.TaskProgress,
            AnalyticsParams.VictimId,
            AnalyticsParams.VictimName,
            AnalyticsParams.AssignedTriage,
            AnalyticsParams.ActualTriage,
            AnalyticsParams.IsCorrect,
            AnalyticsParams.QuizId,
            AnalyticsParams.QuizName,
            AnalyticsParams.QuestionIndex,
            AnalyticsParams.SelectedAnswerIndex,
            AnalyticsParams.CorrectAnswerIndex,
            AnalyticsParams.ScoreValue,
            AnalyticsParams.ScorePercent,
            AnalyticsParams.DurationSeconds,
            "tum_parametreler"
        };

        private static readonly string[] FirestorePreferredColumns =
        {
            "sira_no",
            "kayit_utc",
            "collection",
            "written_utc",
            "last_seen_utc",
            "installation_id",
            "session_id",
            "participant_key",
            "participant_name",
            "full_name",
            "module_id",
            "module_name",
            "scenario_id",
            "scenario_name",
            "event_name",
            "task_id",
            "task_name",
            "target_id",
            "target_name",
            "victim_id",
            "victim_name",
            "assigned_triage",
            "actual_triage",
            "quiz_id",
            "quiz_name",
            "panel_id",
            "panel_name",
            "ai_question_type",
            "completed",
            "success",
            "is_correct",
            "task_progress",
            "completed_count",
            "total_count",
            "total_question_count",
            "answered_count",
            "correct_count",
            "question_index",
            "selected_answer_index",
            "correct_answer_index",
            "duration_seconds",
            "session_duration_seconds",
            "score_value",
            "score_percent",
            "score_percentage",
            "total_events"
        };

        private static readonly string[] SummaryColumns =
        {
            "alan",
            "deger"
        };

        private static readonly string[] FirestoreCollectionOrder =
        {
            "participants",
            "training_session_summaries",
            "training_module_progress",
            "training_task_results",
            "training_quiz_results",
            "training_ai_interactions",
            "training_triage_results"
        };

        public AnalyticsReportExporter(bool emitLogs = false)
        {
            this.emitLogs = emitLogs;
            reportDirectory = ResolveReportDirectoryPath();

            if (!Directory.Exists(reportDirectory))
            {
                Directory.CreateDirectory(reportDirectory);
            }
        }

        public string AdapterName => "local_csv_exporter";
        public bool IsOperational => true;

        public void SetParticipantContext(string participantKey, string participantFullName)
        {
            lock (syncRoot)
            {
                participantKeyContext = NormalizeText(participantKey);
                participantNameContext = NormalizeText(participantFullName);
            }
        }

        public void LogEvent(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            Dictionary<string, object> row = CreateBaseLocalRow();
            row["event_name"] = eventName.Trim();
            row["event_label"] = FormatEventName(eventName);
            row["tum_parametreler"] = BuildParameterSummary(parameters);
            CopyParameters(parameters, row);

            lock (syncRoot)
            {
                analyticsEventRows.Add(row);
            }

            if (emitLogs)
            {
                Debug.Log($"[AnalyticsReportExporter] Firebase event yerel rapora alindi: {eventName}");
            }
        }

        public void LogFirestoreDocument(string collectionName, IReadOnlyDictionary<string, object> document)
        {
            string resolvedCollection = string.IsNullOrWhiteSpace(collectionName)
                ? "firestore_unknown"
                : collectionName.Trim();

            Dictionary<string, object> row = CreateBaseLocalRow();
            row["collection"] = resolvedCollection;
            CopyParameters(document, row);

            lock (syncRoot)
            {
                if (!firestoreRowsByCollection.TryGetValue(resolvedCollection, out List<Dictionary<string, object>> rows))
                {
                    rows = new List<Dictionary<string, object>>();
                    firestoreRowsByCollection.Add(resolvedCollection, rows);
                }

                rows.Add(row);
            }

            if (emitLogs)
            {
                Debug.Log($"[AnalyticsReportExporter] Firestore dokumani yerel rapora alindi: {resolvedCollection}");
            }
        }

        public void ExportNow()
        {
            CreateSnapshots(
                out List<Dictionary<string, object>> eventSnapshot,
                out Dictionary<string, List<Dictionary<string, object>>> firestoreSnapshot);

            int firestoreRowCount = CountRows(firestoreSnapshot);
            if (eventSnapshot.Count == 0 && firestoreRowCount == 0)
            {
                Debug.LogWarning("[AnalyticsReportExporter] Disa aktarilacak Firebase verisi yok. Rapor dosyasi olusturulmadi.");
                return;
            }

            try
            {
                string participantName = ResolveParticipantName("Genel");
                string safeParticipantName = SanitizeFileNameSegment(participantName, "Genel");
                string exportFolderName = $"{safeParticipantName}_Firebase_Rapor_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
                string exportFolder = Path.Combine(reportDirectory, exportFolderName);
                Directory.CreateDirectory(exportFolder);

                List<Dictionary<string, object>> summaryRows = BuildSummaryRows(exportFolder, eventSnapshot, firestoreSnapshot);
                WriteDynamicCsv(Path.Combine(exportFolder, "00_Rapor_Ozeti.csv"), summaryRows, SummaryColumns);

                if (eventSnapshot.Count > 0)
                {
                    WriteDynamicCsv(
                        Path.Combine(exportFolder, "01_Firebase_Analytics_Olaylari.csv"),
                        eventSnapshot,
                        EventPreferredColumns);
                }

                if (firestoreRowCount > 0)
                {
                    List<Dictionary<string, object>> combinedRows = CombineFirestoreRows(firestoreSnapshot);
                    WriteDynamicCsv(
                        Path.Combine(exportFolder, "02_Firestore_Tum_Dokumanlar.csv"),
                        combinedRows,
                        FirestorePreferredColumns);

                    WriteFirestoreCollectionFiles(exportFolder, firestoreSnapshot);
                }

                Debug.Log($"<color=green>[AnalyticsReportExporter] Firebase rapor paketi olusturuldu: {exportFolder}</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnalyticsReportExporter] Rapor olustururken hata: {ex.Message}");
            }
        }

        private Dictionary<string, object> CreateBaseLocalRow()
        {
            return new Dictionary<string, object>
            {
                { "sira_no", GetNextSequence() },
                { "kayit_utc", DateTime.UtcNow.ToString("O") },
                { "participant_key", ResolveParticipantKey("Bilinmiyor") },
                { "participant_name", ResolveParticipantName("Bilinmiyor") }
            };
        }

        private int GetNextSequence()
        {
            lock (syncRoot)
            {
                localSequence++;
                return localSequence;
            }
        }

        private void CreateSnapshots(
            out List<Dictionary<string, object>> eventSnapshot,
            out Dictionary<string, List<Dictionary<string, object>>> firestoreSnapshot)
        {
            lock (syncRoot)
            {
                eventSnapshot = CloneRows(analyticsEventRows);
                firestoreSnapshot = new Dictionary<string, List<Dictionary<string, object>>>();

                foreach (KeyValuePair<string, List<Dictionary<string, object>>> pair in firestoreRowsByCollection)
                {
                    firestoreSnapshot[pair.Key] = CloneRows(pair.Value);
                }
            }
        }

        private static List<Dictionary<string, object>> CloneRows(List<Dictionary<string, object>> source)
        {
            List<Dictionary<string, object>> copy = new List<Dictionary<string, object>>();
            if (source == null)
            {
                return copy;
            }

            for (int i = 0; i < source.Count; i++)
            {
                copy.Add(new Dictionary<string, object>(source[i]));
            }

            return copy;
        }

        private static int CountRows(Dictionary<string, List<Dictionary<string, object>>> tables)
        {
            if (tables == null)
            {
                return 0;
            }

            int count = 0;
            foreach (KeyValuePair<string, List<Dictionary<string, object>>> pair in tables)
            {
                if (pair.Value != null)
                {
                    count += pair.Value.Count;
                }
            }

            return count;
        }

        private List<Dictionary<string, object>> BuildSummaryRows(
            string exportFolder,
            List<Dictionary<string, object>> eventRows,
            Dictionary<string, List<Dictionary<string, object>>> firestoreRows)
        {
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
            AddSummaryRow(rows, "rapor_olusturma_zamani", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            AddSummaryRow(rows, "rapor_klasoru", exportFolder);
            AddSummaryRow(rows, "katilimci_anahtari", ResolveParticipantKey("Bilinmiyor"));
            AddSummaryRow(rows, "katilimci_adi", ResolveParticipantName("Bilinmiyor"));
            AddSummaryRow(rows, "firebase_analytics_event_sayisi", eventRows.Count.ToString(CultureInfo.InvariantCulture));
            AddSummaryRow(rows, "firestore_dokuman_sayisi", CountRows(firestoreRows).ToString(CultureInfo.InvariantCulture));
            AddSummaryRow(rows, "session_id", FindLatestValue(eventRows, firestoreRows, AnalyticsParams.SessionId));
            AddSummaryRow(rows, "installation_id", FindLatestValue(eventRows, firestoreRows, AnalyticsParams.InstallationId));

            AddEventCountRows(rows, eventRows);
            AddFirestoreCountRows(rows, firestoreRows);

            return rows;
        }

        private static void AddSummaryRow(List<Dictionary<string, object>> rows, string field, string value)
        {
            rows.Add(new Dictionary<string, object>
            {
                { "alan", field },
                { "deger", string.IsNullOrWhiteSpace(value) ? string.Empty : value }
            });
        }

        private static void AddEventCountRows(List<Dictionary<string, object>> rows, List<Dictionary<string, object>> eventRows)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            List<string> order = new List<string>();

            for (int i = 0; i < eventRows.Count; i++)
            {
                string eventName = GetString(eventRows[i], "event_name", "unknown_event");
                if (!counts.ContainsKey(eventName))
                {
                    counts[eventName] = 0;
                    order.Add(eventName);
                }

                counts[eventName]++;
            }

            for (int i = 0; i < order.Count; i++)
            {
                string eventName = order[i];
                AddSummaryRow(rows, "event_sayisi_" + eventName, counts[eventName].ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void AddFirestoreCountRows(
            List<Dictionary<string, object>> rows,
            Dictionary<string, List<Dictionary<string, object>>> firestoreRows)
        {
            List<string> orderedCollections = GetOrderedCollectionNames(firestoreRows);
            for (int i = 0; i < orderedCollections.Count; i++)
            {
                string collection = orderedCollections[i];
                int count = firestoreRows.TryGetValue(collection, out List<Dictionary<string, object>> table) && table != null
                    ? table.Count
                    : 0;
                AddSummaryRow(rows, "firestore_sayisi_" + collection, count.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static List<Dictionary<string, object>> CombineFirestoreRows(
            Dictionary<string, List<Dictionary<string, object>>> firestoreRows)
        {
            List<Dictionary<string, object>> combined = new List<Dictionary<string, object>>();
            List<string> orderedCollections = GetOrderedCollectionNames(firestoreRows);

            for (int i = 0; i < orderedCollections.Count; i++)
            {
                string collection = orderedCollections[i];
                if (!firestoreRows.TryGetValue(collection, out List<Dictionary<string, object>> rows) || rows == null)
                {
                    continue;
                }

                for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    combined.Add(new Dictionary<string, object>(rows[rowIndex]));
                }
            }

            return combined;
        }

        private void WriteFirestoreCollectionFiles(
            string exportFolder,
            Dictionary<string, List<Dictionary<string, object>>> firestoreRows)
        {
            List<string> orderedCollections = GetOrderedCollectionNames(firestoreRows);
            int fileIndex = 3;

            for (int i = 0; i < orderedCollections.Count; i++)
            {
                string collection = orderedCollections[i];
                if (!firestoreRows.TryGetValue(collection, out List<Dictionary<string, object>> rows) || rows == null || rows.Count == 0)
                {
                    continue;
                }

                string fileName = $"{fileIndex:00}_Firestore_{SanitizeFileNameSegment(collection, "collection")}.csv";
                WriteDynamicCsv(Path.Combine(exportFolder, fileName), rows, FirestorePreferredColumns);
                fileIndex++;
            }
        }

        private static List<string> GetOrderedCollectionNames(Dictionary<string, List<Dictionary<string, object>>> firestoreRows)
        {
            List<string> ordered = new List<string>();
            if (firestoreRows == null)
            {
                return ordered;
            }

            for (int i = 0; i < FirestoreCollectionOrder.Length; i++)
            {
                string collection = FirestoreCollectionOrder[i];
                if (firestoreRows.ContainsKey(collection))
                {
                    ordered.Add(collection);
                }
            }

            foreach (KeyValuePair<string, List<Dictionary<string, object>>> pair in firestoreRows)
            {
                if (!ordered.Contains(pair.Key))
                {
                    ordered.Add(pair.Key);
                }
            }

            return ordered;
        }

        private static string FindLatestValue(
            List<Dictionary<string, object>> eventRows,
            Dictionary<string, List<Dictionary<string, object>>> firestoreRows,
            string key)
        {
            string fromEvents = FindLatestValue(eventRows, key);
            if (!string.IsNullOrWhiteSpace(fromEvents))
            {
                return fromEvents;
            }

            List<string> collections = GetOrderedCollectionNames(firestoreRows);
            for (int i = collections.Count - 1; i >= 0; i--)
            {
                string collection = collections[i];
                if (!firestoreRows.TryGetValue(collection, out List<Dictionary<string, object>> rows))
                {
                    continue;
                }

                string value = FindLatestValue(rows, key);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static string FindLatestValue(List<Dictionary<string, object>> rows, string key)
        {
            if (rows == null || string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            for (int i = rows.Count - 1; i >= 0; i--)
            {
                string value = GetString(rows[i], key, string.Empty);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static string GetString(IReadOnlyDictionary<string, object> row, string key, string defaultValue)
        {
            if (row == null || string.IsNullOrWhiteSpace(key) || !row.TryGetValue(key, out object value) || value == null)
            {
                return defaultValue;
            }

            string text = ConvertCellToString(value);
            return string.IsNullOrWhiteSpace(text) ? defaultValue : text;
        }

        private static void CopyParameters(IReadOnlyDictionary<string, object> parameters, IDictionary<string, object> row)
        {
            if (parameters == null || row == null)
            {
                return;
            }

            foreach (KeyValuePair<string, object> pair in parameters)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    continue;
                }

                string columnName = AnalyticsService.SanitizeToken(pair.Key, 80, "field");
                object value = NormalizeExportValue(pair.Value);

                if (row.ContainsKey(columnName) && !string.Equals(ConvertCellToString(row[columnName]), ConvertCellToString(value), StringComparison.Ordinal))
                {
                    row["firebase_" + columnName] = value;
                    continue;
                }

                row[columnName] = value;
            }
        }

        private static object NormalizeExportValue(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            switch (value)
            {
                case bool boolValue:
                    return boolValue ? "true" : "false";
                case float floatValue:
                    return Math.Round(floatValue, 3);
                case double doubleValue:
                    return Math.Round(doubleValue, 3);
                case decimal decimalValue:
                    return Math.Round(decimalValue, 3);
                case DateTime dateTime:
                    return dateTime.ToString("O");
            }

            return value;
        }

        private static string BuildParameterSummary(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            foreach (KeyValuePair<string, object> pair in parameters)
            {
                if (builder.Length > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(pair.Key);
                builder.Append('=');
                builder.Append(ConvertCellToString(NormalizeExportValue(pair.Value)));
            }

            return builder.ToString();
        }

        private void WriteDynamicCsv(
            string fullPath,
            List<Dictionary<string, object>> rows,
            string[] preferredColumns)
        {
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            List<string> headers = BuildHeaders(rows, preferredColumns);
            Encoding utf8Bom = new UTF8Encoding(true);

            using (StreamWriter writer = new StreamWriter(fullPath, false, utf8Bom))
            {
                writer.WriteLine(BuildCsvLine(headers));

                for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    List<string> cells = new List<string>(headers.Count);
                    for (int headerIndex = 0; headerIndex < headers.Count; headerIndex++)
                    {
                        string header = headers[headerIndex];
                        rows[rowIndex].TryGetValue(header, out object value);
                        cells.Add(ConvertCellToString(value));
                    }

                    writer.WriteLine(BuildCsvLine(cells));
                }
            }
        }

        private static List<string> BuildHeaders(List<Dictionary<string, object>> rows, string[] preferredColumns)
        {
            List<string> headers = new List<string>();
            HashSet<string> included = new HashSet<string>();

            if (preferredColumns != null)
            {
                for (int i = 0; i < preferredColumns.Length; i++)
                {
                    AddHeader(headers, included, preferredColumns[i]);
                }
            }

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                foreach (KeyValuePair<string, object> pair in rows[rowIndex])
                {
                    AddHeader(headers, included, pair.Key);
                }
            }

            return headers;
        }

        private static void AddHeader(List<string> headers, HashSet<string> included, string header)
        {
            if (string.IsNullOrWhiteSpace(header) || included.Contains(header))
            {
                return;
            }

            headers.Add(header);
            included.Add(header);
        }

        private static string BuildCsvLine(List<string> cells)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < cells.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(CsvSeparator);
                }

                builder.Append(EscapeCsvCell(cells[i]));
            }

            return builder.ToString();
        }

        private static string EscapeCsvCell(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            bool shouldQuote =
                value.Contains(CsvSeparator) ||
                value.Contains("\"") ||
                value.Contains("\n") ||
                value.Contains("\r");

            if (!shouldQuote)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string ConvertCellToString(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            switch (value)
            {
                case string text:
                    return text;
                case float floatValue:
                    return floatValue.ToString("0.###", CultureInfo.InvariantCulture);
                case double doubleValue:
                    return doubleValue.ToString("0.###", CultureInfo.InvariantCulture);
                case decimal decimalValue:
                    return decimalValue.ToString("0.###", CultureInfo.InvariantCulture);
                case DateTime dateTime:
                    return dateTime.ToString("O");
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return value.ToString();
            }
        }

        private string ResolveParticipantKey(string fallback)
        {
            if (ParticipantManager.HasParticipant)
            {
                string playerPrefsKey = ParticipantManager.GetParticipantKey();
                if (!string.IsNullOrWhiteSpace(playerPrefsKey))
                {
                    return playerPrefsKey.Trim();
                }
            }

            lock (syncRoot)
            {
                return string.IsNullOrWhiteSpace(participantKeyContext) ? fallback : participantKeyContext;
            }
        }

        private string ResolveParticipantName(string fallback)
        {
            if (ParticipantManager.HasParticipant)
            {
                string playerPrefsName = ParticipantManager.GetParticipantName();
                if (!string.IsNullOrWhiteSpace(playerPrefsName))
                {
                    return playerPrefsName.Trim();
                }
            }

            lock (syncRoot)
            {
                return string.IsNullOrWhiteSpace(participantNameContext) ? fallback : participantNameContext;
            }
        }

        private static string ResolveReportDirectoryPath()
        {
            // Consistent with MainMenuManager OpenReportsFolder logic.
            return Path.Combine(Application.persistentDataPath, "Reports");
        }

        private static string SanitizeFileNameSegment(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (Array.IndexOf(invalidChars, current) >= 0 || char.IsControl(current))
                {
                    builder.Append('_');
                    continue;
                }

                builder.Append(char.IsWhiteSpace(current) ? '_' : current);
            }

            string sanitized = builder.ToString().Trim('_');
            return string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
        }

        private static string NormalizeText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private string FormatEventName(string rawEvent)
        {
            switch (rawEvent)
            {
                case AnalyticsEventNames.ModuleEntered: return "Modul Girisi";
                case AnalyticsEventNames.ModuleCompleted: return "Modul Bitirildi";
                case AnalyticsEventNames.ModuleTransitionIntent: return "Modul Gecis Niyeti";
                case AnalyticsEventNames.ContentOpened: return "Icerik Acildi";
                case AnalyticsEventNames.VideoStarted: return "Video Baslatildi";
                case AnalyticsEventNames.VideoProgress: return "Video Ilerlemesi";
                case AnalyticsEventNames.VideoCompleted: return "Video Tamamlandi";
                case AnalyticsEventNames.InfographicOpened: return "Infografik Acildi";
                case AnalyticsEventNames.LearningContentCompleted: return "Ogrenme Icerigi Tamamlandi";
                case AnalyticsEventNames.TaskStarted: return "Gorev Baslatildi";
                case AnalyticsEventNames.TaskProgress: return "Gorev Ilerlemesi";
                case AnalyticsEventNames.TaskFailed: return "Gorev Basarisiz";
                case AnalyticsEventNames.TaskCompleted: return "Gorev Tamamlandi";
                case AnalyticsEventNames.HelpRequested: return "Yardim Istendi";
                case AnalyticsEventNames.TriageStarted: return "Triyaj Baslatildi";
                case AnalyticsEventNames.VictimInteracted: return "Yaraliyla Etkilesim";
                case AnalyticsEventNames.VictimTagged: return "Triyaj Atamasi";
                case AnalyticsEventNames.AIPanelOpened: return "AI Panel Acildi";
                case AnalyticsEventNames.AIQuestionAsked: return "AI Soru Soruldu";
                case AnalyticsEventNames.QuizStarted: return "Test Baslatildi";
                case AnalyticsEventNames.QuizAnswered: return "Soru Cevaplandi";
                case AnalyticsEventNames.QuizCompleted: return "Test Bitti";
                case AnalyticsEventNames.ScoreRecorded: return "Skor Kaydedildi";
                case AnalyticsEventNames.ScenarioStarted: return "Senaryo Baslatildi";
                case AnalyticsEventNames.CriticalActionTaken: return "Kritik Aksiyon";
                case AnalyticsEventNames.ScenarioTaskCompleted: return "Senaryo Gorevi Tamamlandi";
                case AnalyticsEventNames.ScenarioCompleted: return "Senaryo Bitti";
                case AnalyticsEventNames.TriageDialogOpened: return "Triyaj Diyalogu Acildi";
                default: return rawEvent;
            }
        }
    }
}
