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
        private readonly List<Dictionary<string, object>> pendingRows = new List<Dictionary<string, object>>();

        private string participantKeyContext = string.Empty;
        private string participantNameContext = string.Empty;
        private int localSequence;

        private const string CsvSeparator = ";";
        private const string CsvFileName = "veriler.csv";
        private static readonly object ExportSyncRoot = new object();

        private static readonly string[] PreferredColumns =
        {
            "sira_no",
            "tarih",
            "saat",
            "kaynak",
            "olay",
            "aciklama",
            "katilimci_adi",
            "modul",
            "senaryo",
            "gorev",
            "hasta",
            "test",
            "sonuc",
            "skor",
            "sure_sn",
            "detay"
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

            Dictionary<string, object> row = CreateBaseRow();
            row["kaynak"] = "Oyun";
            row["olay"] = eventName.Trim();
            row["aciklama"] = FormatEventName(eventName);

            ExtractReadableFields(parameters, row);
            row["detay"] = BuildParameterSummary(parameters);

            lock (syncRoot)
            {
                pendingRows.Add(row);
            }

            if (emitLogs)
            {
                Debug.Log($"[AnalyticsReportExporter] Event: {eventName}");
            }
        }

        public void LogFirestoreDocument(string collectionName, IReadOnlyDictionary<string, object> document)
        {
            string resolvedCollection = string.IsNullOrWhiteSpace(collectionName)
                ? "firestore"
                : collectionName.Trim();

            Dictionary<string, object> row = CreateBaseRow();
            row["kaynak"] = "Kayit";
            row["olay"] = resolvedCollection;
            row["aciklama"] = FormatCollectionName(resolvedCollection);

            ExtractReadableFields(document, row);
            row["detay"] = BuildParameterSummary(document);

            lock (syncRoot)
            {
                pendingRows.Add(row);
            }

            if (emitLogs)
            {
                Debug.Log($"[AnalyticsReportExporter] Firestore: {resolvedCollection}");
            }
        }

        public void ExportNow()
        {
            List<Dictionary<string, object>> snapshot;
            lock (syncRoot)
            {
                if (pendingRows.Count == 0)
                {
                    Debug.LogWarning("[AnalyticsReportExporter] Disa aktarilacak veri yok.");
                    return;
                }

                snapshot = new List<Dictionary<string, object>>(pendingRows);
                pendingRows.Clear();
            }

            try
            {
                string participantKey = ResolveParticipantKey("genel");
                string safeKey = SanitizeFileName(participantKey, "genel");
                string userFolder = Path.Combine(reportDirectory, safeKey);

                List<string> headers = BuildHeaders(PreferredColumns);
                Encoding utf8Bom = new UTF8Encoding(true);

                lock (ExportSyncRoot)
                {
                    if (!Directory.Exists(userFolder))
                    {
                        Directory.CreateDirectory(userFolder);
                    }

                    string filePath = Path.Combine(userFolder, CsvFileName);
                    EnsureHeaderIsCompatible(filePath, headers);
                    bool fileExists = File.Exists(filePath) && new FileInfo(filePath).Length > 0;

                    using (StreamWriter writer = new StreamWriter(filePath, true, utf8Bom))
                    {
                        if (!fileExists)
                        {
                            writer.WriteLine(BuildCsvLine(headers));
                        }

                        for (int i = 0; i < snapshot.Count; i++)
                        {
                            List<string> cells = new List<string>(headers.Count);
                            for (int j = 0; j < headers.Count; j++)
                            {
                                snapshot[i].TryGetValue(headers[j], out object value);
                                cells.Add(ConvertCellToString(value));
                            }

                            writer.WriteLine(BuildCsvLine(cells));
                        }
                    }
                }

                Debug.Log($"<color=green>[AnalyticsReportExporter] {snapshot.Count} satir kaydedildi: {Path.Combine(userFolder, CsvFileName)}</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnalyticsReportExporter] CSV hatasi: {ex.Message}");

                lock (syncRoot)
                {
                    pendingRows.AddRange(snapshot);
                }
            }
        }

        private Dictionary<string, object> CreateBaseRow()
        {
            DateTime now = DateTime.Now;
            return new Dictionary<string, object>
            {
                { "sira_no", GetNextSequence() },
                { "tarih", now.ToString("dd.MM.yyyy") },
                { "saat", now.ToString("HH:mm:ss") },
                { "kaynak", "" },
                { "olay", "" },
                { "aciklama", "" },
                { "katilimci_adi", ResolveParticipantName("Bilinmiyor") },
                { "modul", "" },
                { "senaryo", "" },
                { "gorev", "" },
                { "hasta", "" },
                { "test", "" },
                { "sonuc", "" },
                { "skor", "" },
                { "sure_sn", "" },
                { "detay", "" }
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

        private static void ExtractReadableFields(IReadOnlyDictionary<string, object> parameters, IDictionary<string, object> row)
        {
            if (parameters == null)
            {
                return;
            }

            string modulAdi = GetString(parameters, AnalyticsParams.ModuleName);
            string modulId = GetString(parameters, AnalyticsParams.ModuleId);
            if (!string.IsNullOrWhiteSpace(modulAdi))
            {
                row["modul"] = modulAdi;
            }
            else if (!string.IsNullOrWhiteSpace(modulId))
            {
                row["modul"] = "Modul " + modulId;
            }

            string senaryoAdi = GetString(parameters, AnalyticsParams.ScenarioName);
            string senaryoId = GetString(parameters, AnalyticsParams.ScenarioId);
            if (!string.IsNullOrWhiteSpace(senaryoAdi))
            {
                row["senaryo"] = senaryoAdi;
            }
            else if (!string.IsNullOrWhiteSpace(senaryoId))
            {
                row["senaryo"] = ResolveScenarioDisplayName(senaryoId);
            }

            string gorevAdi = GetString(parameters, AnalyticsParams.TaskName);
            if (!string.IsNullOrWhiteSpace(gorevAdi))
            {
                row["gorev"] = gorevAdi;
            }

            string hastaAdi = GetString(parameters, AnalyticsParams.VictimName);
            string hastaId = GetString(parameters, AnalyticsParams.VictimId);
            if (!string.IsNullOrWhiteSpace(hastaAdi))
            {
                // detay kolonu " | " ile ayrildigi icin hasta isminde ayni separator
                // varsa raporu okuyan kisi yanilir. " - " ile guvenli hale getir.
                row["hasta"] = hastaAdi.Replace(" | ", " - ");
            }
            else if (!string.IsNullOrWhiteSpace(hastaId))
            {
                row["hasta"] = "Hasta " + hastaId;
            }

            string testAdi = GetString(parameters, AnalyticsParams.QuizName);
            if (!string.IsNullOrWhiteSpace(testAdi))
            {
                row["test"] = testAdi;
            }

            string sonuc = BuildSonuc(parameters);
            if (!string.IsNullOrWhiteSpace(sonuc))
            {
                row["sonuc"] = sonuc;
            }

            string skor = BuildSkor(parameters);
            if (!string.IsNullOrWhiteSpace(skor))
            {
                row["skor"] = skor;
            }

            string sure = GetString(parameters, AnalyticsParams.DurationSeconds);
            if (!string.IsNullOrWhiteSpace(sure) && double.TryParse(sure, NumberStyles.Float, CultureInfo.InvariantCulture, out double sn))
            {
                // "sn" birim eki: Excel hucreyi sayi degil metin olarak okur ve "8,7"
                // gibi degerleri yanlislikla "8.Tem" (8 Temmuz) tarihine cevirmez.
                row["sure_sn"] = FormatDisplayNumber(Math.Round(sn, 1)) + " sn";
            }
        }

        private static string BuildSonuc(IReadOnlyDictionary<string, object> parameters)
        {
            string dogru = GetString(parameters, AnalyticsParams.IsCorrect);
            string basarili = GetString(parameters, "basarili");
            string gorevDurum = GetString(parameters, AnalyticsParams.TaskStatus);

            if (!string.IsNullOrWhiteSpace(dogru))
            {
                // Test ozet satirlari secilen=-1 tasir; dogru=false burada cevap sonucu
                // degil varsayilan degerdir. Bu satirlari "Yanlis" diye gostermek yaniltir.
                string secilen = GetString(parameters, AnalyticsParams.SelectedAnswerIndex);
                string tamamlandi = GetString(parameters, "tamamlandi");
                if (string.Equals(secilen, "-1", StringComparison.Ordinal))
                {
                    if (IsFalseValue(tamamlandi))
                    {
                        return string.Empty;
                    }

                    if (IsTrueValue(tamamlandi))
                    {
                        return "Tamamlandi";
                    }
                }

                return IsTrueValue(dogru)
                    ? "Dogru"
                    : "Yanlis";
            }

            if (!string.IsNullOrWhiteSpace(basarili))
            {
                return IsTrueValue(basarili)
                    ? "Basarili"
                    : "Basarisiz";
            }

            if (!string.IsNullOrWhiteSpace(gorevDurum))
            {
                return string.Equals(gorevDurum, "failed", StringComparison.OrdinalIgnoreCase)
                    ? "Basarisiz"
                    : "Tamamlandi";
            }

            string hedefModul = GetString(parameters, AnalyticsParams.TargetModuleName);
            if (!string.IsNullOrWhiteSpace(hedefModul))
            {
                string modul = GetString(parameters, AnalyticsParams.ModuleName);
                return string.IsNullOrWhiteSpace(modul)
                    ? hedefModul + " istegi"
                    : modul + " -> " + hedefModul + " istegi";
            }

            string atanan = GetString(parameters, AnalyticsParams.AssignedTriage);
            string sonuc = GetString(parameters, AnalyticsParams.ActualTriage);
            if (!string.IsNullOrWhiteSpace(atanan) && !string.IsNullOrWhiteSpace(sonuc))
            {
                return $"{atanan} -> {sonuc}";
            }

            return string.Empty;
        }

        private static string BuildSkor(IReadOnlyDictionary<string, object> parameters)
        {
            string yuzde = GetString(parameters, AnalyticsParams.ScorePercent);
            string deger = GetString(parameters, AnalyticsParams.ScoreValue);
            string tamamlandi = GetString(parameters, "tamamlandi");
            bool explicitlyIncomplete = IsFalseValue(tamamlandi);

            if (!string.IsNullOrWhiteSpace(yuzde) && double.TryParse(yuzde, NumberStyles.Float, CultureInfo.InvariantCulture, out double pct))
            {
                if (explicitlyIncomplete && string.IsNullOrWhiteSpace(deger))
                {
                    return string.Empty;
                }

                return FormatDisplayNumber(Math.Round(pct, 1)) + "%";
            }

            if (!string.IsNullOrWhiteSpace(deger))
            {
                return deger;
            }

            return string.Empty;
        }

        private static bool IsTrueValue(string value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFalseValue(string value)
        {
            return string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "0", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetString(IReadOnlyDictionary<string, object> dict, string key)
        {
            if (dict == null || string.IsNullOrWhiteSpace(key) || !dict.TryGetValue(key, out object value) || value == null)
            {
                return string.Empty;
            }

            return ConvertCellToString(value);
        }

        private string ResolveParticipantKey(string fallback)
        {
            lock (syncRoot)
            {
                if (!string.IsNullOrWhiteSpace(participantKeyContext))
                {
                    return participantKeyContext;
                }
            }

            if (ParticipantManager.HasParticipant)
            {
                string key = ParticipantManager.GetParticipantKey();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    return key.Trim();
                }
            }

            return fallback;
        }

        private string ResolveParticipantName(string fallback)
        {
            lock (syncRoot)
            {
                if (!string.IsNullOrWhiteSpace(participantNameContext))
                {
                    return participantNameContext;
                }
            }

            if (ParticipantManager.HasParticipant)
            {
                string name = ParticipantManager.GetParticipantName();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name.Trim();
                }
            }

            return fallback;
        }

        public static string ResolveReportDirectoryPath()
        {
            return Path.GetFullPath(Application.dataPath + "/../CSV Dosyalari");
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

        private static void EnsureHeaderIsCompatible(string filePath, List<string> expectedHeaders)
        {
            if (string.IsNullOrWhiteSpace(filePath) || expectedHeaders == null)
            {
                return;
            }

            if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0)
            {
                return;
            }

            string expectedHeader = BuildCsvLine(expectedHeaders);
            string existingHeader;
            using (StreamReader reader = new StreamReader(filePath, Encoding.UTF8, true))
            {
                existingHeader = reader.ReadLine();
            }

            existingHeader = string.IsNullOrEmpty(existingHeader)
                ? string.Empty
                : existingHeader.TrimStart('\uFEFF');

            if (string.Equals(existingHeader, expectedHeader, StringComparison.Ordinal))
            {
                return;
            }

            string backupPath = BuildBackupFilePath(filePath);
            File.Move(filePath, backupPath);
            Debug.LogWarning($"[AnalyticsReportExporter] CSV basligi uyumsuzdu, eski dosya yedeklendi: {backupPath}");
        }

        private static string BuildBackupFilePath(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string candidate = Path.Combine(directory, $"{fileName}_{timestamp}{extension}");
            int suffix = 1;

            while (File.Exists(candidate))
            {
                candidate = Path.Combine(directory, $"{fileName}_{timestamp}_{suffix}{extension}");
                suffix++;
            }

            return candidate;
        }

        private static List<string> BuildHeaders(string[] preferredColumns)
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

        private static string FormatDisplayNumber(double value)
        {
            return value.ToString("0.#", CultureInfo.GetCultureInfo("tr-TR"));
        }

        private static string SanitizeFileName(string value, string fallback)
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

        private static string FormatEventName(string rawEvent)
        {
            switch (rawEvent)
            {
                case AnalyticsEventNames.ModuleEntered: return "Modul Acildi";
                case AnalyticsEventNames.ModuleCompleted: return "Modul Tamamlandi";
                case AnalyticsEventNames.ModuleTransitionIntent: return "Modul Gecis Istegi";
                case AnalyticsEventNames.ContentOpened: return "Icerik Acildi";
                case AnalyticsEventNames.VideoStarted: return "Video Izlenmeye Baslandi";
                case AnalyticsEventNames.VideoProgress: return "Video Izleniyor";
                case AnalyticsEventNames.VideoCompleted: return "Video Tamamlandi";
                case AnalyticsEventNames.InfographicOpened: return "Infografik Acildi";
                case AnalyticsEventNames.LearningContentCompleted: return "Icerik Tamamlandi";
                case AnalyticsEventNames.TaskStarted: return "Goreve Baslandi";
                case AnalyticsEventNames.TaskProgress: return "Gorev Devam Ediyor";
                case AnalyticsEventNames.TaskFailed: return "Gorev Basarisiz";
                case AnalyticsEventNames.TaskCompleted: return "Gorev Tamamlandi";
                case AnalyticsEventNames.HelpRequested: return "Yardim Istendi";
                case AnalyticsEventNames.TriageStarted: return "Triyaj Baslandi";
                case AnalyticsEventNames.VictimInteracted: return "Hastaya Dokunuldu";
                case AnalyticsEventNames.VictimTagged: return "Hasta Etiketlendi";
                case AnalyticsEventNames.AIPanelOpened: return "AI Panel Acildi";
                case AnalyticsEventNames.AIQuestionAsked: return "AI Soru Soruldu";
                case AnalyticsEventNames.QuizStarted: return "Teste Baslandi";
                case AnalyticsEventNames.QuizAnswered: return "Soru Cevaplandi";
                case AnalyticsEventNames.QuizCompleted: return "Test Tamamlandi";
                case AnalyticsEventNames.ScoreRecorded: return "Skor Kaydedildi";
                case AnalyticsEventNames.ScenarioStarted: return "Senaryo Baslandi";
                case AnalyticsEventNames.CriticalActionTaken: return "Kritik Aksiyon Alindi";
                case AnalyticsEventNames.ScenarioTaskCompleted: return "Senaryo Gorevi Tamamlandi";
                case AnalyticsEventNames.ScenarioCompleted: return "Senaryo Tamamlandi";
                case AnalyticsEventNames.TriageDialogOpened: return "Triyaj Dialogu Acildi";
                default: return rawEvent;
            }
        }

        private static string ResolveScenarioDisplayName(string scenarioId)
        {
            // Bilinen senaryo ID'lerini insan okur Turkce isimlere maple. Boylece CSV'nin
            // 'senaryo' kolonu raw "Senaryo first_aid_rescue" yerine duzgun gorunur.
            switch (scenarioId)
            {
                case "first_aid_rescue":
                    return TrainingAnalyticsFacade.Module2ScenarioName;
                case "hospital_triage":
                    return TrainingAnalyticsFacade.Module3ScenarioName;
                case "yangin_mudahale":
                    return TrainingAnalyticsFacade.Module4ScenarioName;
                default:
                    return "Senaryo " + scenarioId;
            }
        }

        private static string FormatCollectionName(string collection)
        {
            switch (collection)
            {
                case "katilimcilar": return "Katilimci Profili Kaydedildi";
                case "oturumlar": return "Oturum Ozeti Kaydedildi";
                case "moduller": return "Modul Ilerlemesi Kaydedildi";
                case "gorevler": return "Gorev Sonucu Kaydedildi";
                case "testler": return "Test Sonucu Kaydedildi";
                case "ai_etkilesim": return "AI Etkilesim Kaydedildi";
                case "triage_sonuc": return "Triage Sonucu Kaydedildi";
                default: return collection + " Kaydedildi";
            }
        }
    }
}
