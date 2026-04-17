using System.Collections.Generic;
using System.Text;
using TrainingAnalytics;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class HospitalTriageManager : MonoBehaviour
{
    private static bool isApplicationQuitting;

    private const string PreferredHospitalStartAnchorName = "spawn";
    private const string HospitalRetryAnchorName = "HospitalRetryAnchor";
    private const string BaseReturnAnchorName = "BaseReturnAnchor";
    private const string BaseSpawnFallbackName = "VR_SpawnPoint";
    private const string GeneralScenarioComplaint =
        "Genel durumu degisken gorunuyor. Sistematik triyaj degerlendirmesi gerekiyor.";
    private static readonly string[] HospitalRetryAnchorFallbackNames =
    {
        HospitalRetryAnchorName,
        "HospitalSpawn"
    };
    private static readonly TriageScenario[] SeededScenarios =
    {
        new TriageScenario(
            "Kolumda ve bacagimda hafif siyriklar var, biraz sizliyor ama kendi basima yuruyebiliyorum. Sadece biraz korktum.",
            TriageCategory.Green),
        new TriageScenario(
            "Enkazdan cikarken bacagima derin bir metal parcasi girdi. Cok kaniyor ve ayagimin uzerine basamiyorum. Hareketlerimde kisitlilik var.",
            TriageCategory.Yellow),
        new TriageScenario(
            "Gogus kafesimde siddetli baski hissediyorum, nefes almakta cok zorlaniyorum. Dudaklarim uyusmaya basladi, lutfen acele edin.",
            TriageCategory.Red),
        new TriageScenario(
            "Hasta yanit vermiyor. Spontan solunum ve nabiz alinmiyor.",
            TriageCategory.Black)
    };

    private struct TriageScenario
    {
        public readonly string complaint;
        public readonly TriageCategory category;

        public TriageScenario(string complaint, TriageCategory category)
        {
            this.complaint = complaint;
            this.category = category;
        }
    }

    private struct CompletedTriageRecord
    {
        public readonly int npcInstanceId;
        public readonly int order;
        public readonly string npcName;
        public readonly string complaint;
        public readonly TriageCategory givenCategory;
        public readonly TriageCategory actualCategory;
        public readonly bool isCorrect;
        public readonly float decisionDurationSeconds;
        public readonly float completedAtRealtime;

        public CompletedTriageRecord(
            int npcInstanceId,
            int order,
            string npcName,
            string complaint,
            TriageCategory givenCategory,
            TriageCategory actualCategory,
            bool isCorrect,
            float decisionDurationSeconds,
            float completedAtRealtime)
        {
            this.npcInstanceId = npcInstanceId;
            this.order = order;
            this.npcName = npcName;
            this.complaint = complaint;
            this.givenCategory = givenCategory;
            this.actualCategory = actualCategory;
            this.isCorrect = isCorrect;
            this.decisionDurationSeconds = decisionDurationSeconds;
            this.completedAtRealtime = completedAtRealtime;
        }
    }

    public struct TriageResultSnapshot
    {
        public int totalCount;
        public int completedCount;
        public int pendingCount;
        public int correctCount;
        public int incorrectCount;
        public float accuracyPercent;
        public float completionPercent;
        public float durationSeconds;
        public float activeDecisionSeconds;
        public float averageDecisionSeconds;
        public float patientsPerMinute;
        public float fastestDecisionSeconds;
        public float medianDecisionSeconds;
        public float slowestDecisionSeconds;
        public int underTriageCount;
        public int overTriageCount;
        public int criticalMismatchCount;
        public int longestCorrectStreak;
        public int greenActualCount;
        public int yellowActualCount;
        public int redActualCount;
        public int blackActualCount;
        public int greenGivenCount;
        public int yellowGivenCount;
        public int redGivenCount;
        public int blackGivenCount;
        public int greenCorrectCount;
        public int yellowCorrectCount;
        public int redCorrectCount;
        public int blackCorrectCount;
        public float greenAccuracyPercent;
        public float yellowAccuracyPercent;
        public float redAccuracyPercent;
        public float blackAccuracyPercent;
    }

    public static HospitalTriageManager Instance;

    [Header("Durum")]
    [SerializeField] private bool hospitalModeActive = true;
    [SerializeField] private bool lockNewInteractionWhileNpcActive = true;
    [SerializeField] private bool hospitalPhaseActive;

    [Header("UI")]
    [SerializeField] private TriageResultPanel resultPanel;
    [SerializeField] private Transform resultPanelSpawnAnchor;
    [SerializeField] private bool spawnResultPanelInFrontOfCamera = true;
    [SerializeField] private float resultPanelDistance = 1.6f;
    [SerializeField] private TriageHUD progressHud;

    [Header("Spawn Anchor")]
    [SerializeField] private Transform hospitalStartAnchor;
    [SerializeField] private float hospitalStartYawOffset = 180f;
    [SerializeField] private Transform baseReturnAnchor;

    [Header("Skor")]
    [SerializeField] private int toplamYarali;
    [SerializeField] private int tamamlananTriyaj;
    [SerializeField] private int dogruTriyaj;
    [SerializeField] private int yanlisTriyaj;

    private readonly HashSet<NPCTriageInteractable> registeredNpcs = new HashSet<NPCTriageInteractable>();
    private readonly HashSet<Object> uiFocusOwners = new HashSet<Object>();
    private readonly Dictionary<Behaviour, bool> locomotionStateSnapshot = new Dictionary<Behaviour, bool>();
    private readonly List<CompletedTriageRecord> completedTriageRecords = new List<CompletedTriageRecord>();
    private readonly Dictionary<int, float> npcDialogStartTimes = new Dictionary<int, float>();

    private NPCTriageInteractable activeNpc;
    private bool resultShown;
    private bool locomotionSnapshotCaptured;
    private int triageRecordOrder;
    private bool completionAnalyticsSent;
    private float hospitalPhaseStartRealtime = -1f;
    private float firstTriageRealtime = -1f;
    private float lastTriageRealtime = -1f;
    private float completionRealtime = -1f;

    public bool HospitalModeActive => hospitalModeActive;
    public int ToplamYarali => toplamYarali;
    public int TamamlananTriyaj => tamamlananTriyaj;
    public int KalanYarali => Mathf.Max(0, toplamYarali - tamamlananTriyaj);
    public int DogruTriyaj => dogruTriyaj;
    public int YanlisTriyaj => yanlisTriyaj;
    public bool IsInteractionLocked => lockNewInteractionWhileNpcActive && activeNpc != null;
    public NPCTriageInteractable ActiveNpc => activeNpc;
    public bool HospitalPhaseActive => hospitalPhaseActive;

    public string GetSituationalAwarenessContext()
    {
        bool hasRecordedTriage = completedTriageRecords.Count > 0 || tamamlananTriyaj > 0 || toplamYarali > 0;
        if (!hospitalModeActive)
        {
            if (!hasRecordedTriage)
            {
                return string.Empty;
            }
        }

        if (!hospitalPhaseActive && !hasRecordedTriage)
        {
            return "Henuz hastaneye girip triyaj yapmadiniz.";
        }

        int totalPatients = Mathf.Max(0, toplamYarali);
        int completedPatients = Mathf.Clamp(tamamlananTriyaj, 0, totalPatients);
        int pendingPatients = Mathf.Max(0, totalPatients - completedPatients);

        StringBuilder builder = new StringBuilder(192);
        builder.Append("Toplam ");
        builder.Append(totalPatients);
        builder.Append(". Tamamlanan ");
        builder.Append(completedPatients);
        builder.Append(". Bekleyen ");
        builder.Append(pendingPatients);
        builder.Append(". Dogru ");
        builder.Append(Mathf.Max(0, dogruTriyaj));
        builder.Append(". Yanlis ");
        builder.Append(Mathf.Max(0, yanlisTriyaj));
        builder.Append('.');

        if (!hospitalModeActive)
        {
            builder.Append(" Hastane triyaj modu su an kapali; son bilinen sonuclar gosteriliyor.");
        }
        else if (!hospitalPhaseActive)
        {
            builder.Append(" Hastane fazi su an aktif degil; son kayitlar korunuyor.");
        }

        if (completedPatients <= 0)
        {
            builder.Append(" Henuz hastane icinde triyaj kaydi yok.");
        }
        else
        {
            AppendCompletedTriageSummary(builder);
        }

        AppendPendingComplaintSummary(builder);
        return builder.ToString().Trim();
    }

    private void Awake()
    {
        isApplicationQuitting = false;

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        ResolveSceneReferences();
        resultPanel?.HidePanel();
        UpdateHudVisibility();
    }

    private void Start()
    {
        EnsureVrUiRuntimeReadiness();
        ResolveSceneReferences();
        RebuildNpcRegistryFromScene();
        UpdateResultPanelLive();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            ReleaseAllUiFocusImmediate();
            Instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    private void EnsureVrUiRuntimeReadiness()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            eventSystem = FindObjectOfType<EventSystem>();
        }

        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        if (eventSystem != null && eventSystem.GetComponent<XRUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<XRUIInputModule>();
        }

        if (eventSystem != null)
        {
            StandaloneInputModule standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (standaloneInputModule != null)
            {
                standaloneInputModule.enabled = false;
            }
        }

        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || !canvas.isActiveAndEnabled || !canvas.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            }

            GraphicRaycaster graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster != null)
            {
                graphicRaycaster.enabled = false;
            }
        }
    }

    public void EnterHospitalPhase()
    {
        hospitalPhaseActive = true;
        completionAnalyticsSent = false;
        ResetTriageTimingState(true);
        TrainingAnalyticsFacade.OnModuleEntered(
            TrainingAnalyticsFacade.Module3Id,
            TrainingAnalyticsFacade.Module3Name,
            new Dictionary<string, object>
            {
                { AnalyticsParams.SelectionSource, "hospital_phase" }
            });
        TrainingAnalyticsFacade.OnScenarioStarted(
            TrainingAnalyticsFacade.Module3Id,
            TrainingAnalyticsFacade.Module3Name,
            TrainingAnalyticsFacade.Module3ScenarioId,
            TrainingAnalyticsFacade.Module3ScenarioName,
            new Dictionary<string, object>
            {
                { AnalyticsParams.TotalCount, toplamYarali }
            });
        TrainingAnalyticsFacade.OnTriageStarted(
            TrainingAnalyticsFacade.Module3Id,
            TrainingAnalyticsFacade.Module3Name,
            TrainingAnalyticsFacade.Module3ScenarioId,
            TrainingAnalyticsFacade.Module3ScenarioName,
            new Dictionary<string, object>
            {
                { AnalyticsParams.TotalCount, toplamYarali }
            });
        UpdateHudVisibility();
    }

    public void ExitHospitalPhaseToBase()
    {
        hospitalPhaseActive = false;
        UpdateHudVisibility();
    }

    public void AcquireUiFocus(Object owner)
    {
        if (owner == null)
        {
            return;
        }

        if (!uiFocusOwners.Add(owner))
        {
            return;
        }

        if (uiFocusOwners.Count == 1)
        {
            CaptureAndDisablePlayerLocomotion();
        }
    }

    public void ReleaseUiFocus(Object owner)
    {
        if (owner == null)
        {
            return;
        }

        if (!uiFocusOwners.Remove(owner))
        {
            return;
        }

        if (uiFocusOwners.Count == 0)
        {
            RestorePlayerLocomotionFromSnapshot();
        }
    }

    public void RebuildNpcRegistryFromScene()
    {
        ResolveSceneReferences();
        registeredNpcs.Clear();
        completedTriageRecords.Clear();
        npcDialogStartTimes.Clear();
        triageRecordOrder = 0;
        NPCTriageInteractable[] npcs = FindObjectsOfType<NPCTriageInteractable>(true);
        SortNpcsForScenarioSeeding(npcs);
        ApplyScenarioSeeds(npcs);

        for (int i = 0; i < npcs.Length; i++)
        {
            RegisterNpc(npcs[i], false);
        }

        toplamYarali = registeredNpcs.Count;
        RecalculateCompletedTriageStats();
        resultShown = false;
        completionAnalyticsSent = false;
        ResetTriageTimingState(hospitalPhaseActive);
        resultPanel?.HidePanel();
        UpdateHudVisibility();
    }

    [ContextMenu("Yaratici Vakalari Dagit")]
    public void DistributeCreativeCases()
    {
        NPCTriageInteractable[] npcs = FindObjectsOfType<NPCTriageInteractable>(true);
        SortNpcsForScenarioSeeding(npcs);
        ApplyScenarioSeeds(npcs);
        RefreshCachedUi();
    }

    public void RegisterNpc(NPCTriageInteractable npc)
    {
        RegisterNpc(npc, true);
    }

    public void UnregisterNpc(NPCTriageInteractable npc)
    {
        if (npc == null)
        {
            return;
        }

        if (activeNpc == npc)
        {
            RemoveNpcDialogStart(npc);
            activeNpc = null;
        }

        RemoveCompletedRecordForNpc(npc.GetInstanceID());

        if (registeredNpcs.Remove(npc))
        {
            toplamYarali = registeredNpcs.Count;
            RecalculateCompletedTriageStats();
            RefreshCachedUi();
        }
    }

    public int GetNpcDisplayIndex(NPCTriageInteractable npc)
    {
        if (npc == null)
        {
            return -1;
        }

        List<NPCTriageInteractable> orderedNpcs = BuildOrderedNpcList();
        for (int i = 0; i < orderedNpcs.Count; i++)
        {
            if (orderedNpcs[i] == npc)
            {
                return i + 1;
            }
        }

        return -1;
    }

    public bool TrySetActiveNpc(NPCTriageInteractable npc)
    {
        if (!hospitalModeActive)
        {
            return true;
        }

        if (npc == null)
        {
            return false;
        }

        if (activeNpc == null || activeNpc == npc || !lockNewInteractionWhileNpcActive)
        {
            SetActiveNpc(npc);
            return true;
        }

        if (ShouldReleaseActiveNpcLock(activeNpc))
        {
            SetActiveNpc(npc);
            return true;
        }

        return false;
    }

    public void ClearActiveNpc(NPCTriageInteractable npc)
    {
        if (npc != null && activeNpc == npc)
        {
            RemoveNpcDialogStart(npc);
            activeNpc = null;
        }
    }

    public void RegisterTriage(TriageCategory verilen, TriageCategory dogru)
    {
        RegisterTriage(null, verilen, dogru);
    }

    public void RegisterTriage(MonoBehaviour npc, TriageCategory verilen, TriageCategory dogru)
    {
        if (!hospitalModeActive)
        {
            return;
        }

        float now = Time.realtimeSinceStartup;
        if (hospitalPhaseStartRealtime < 0f)
        {
            hospitalPhaseStartRealtime = now;
        }

        if (firstTriageRealtime < 0f)
        {
            firstTriageRealtime = now;
        }

        lastTriageRealtime = now;
        completionRealtime = -1f;

        float decisionDurationSeconds = ConsumeNpcDecisionDuration(npc, now);
        RecordCompletedTriage(npc, verilen, dogru, decisionDurationSeconds, now);
        RecalculateCompletedTriageStats();
        if (activeNpc != null)
        {
            RemoveNpcDialogStart(activeNpc);
        }
        activeNpc = null;
        UpdateResultPanelLive();

        string victimId = npc is NPCTriageInteractable triageNpc
            ? triageNpc.CaseId
            : "victim_unknown";
        string victimName = npc is NPCTriageInteractable namedNpc
            ? namedNpc.PatientTitle
            : "Bilinmeyen Hasta";
        float progress = toplamYarali > 0 ? (float)tamamlananTriyaj / toplamYarali : 0f;

        TrainingAnalyticsFacade.OnTaskProgress(
            TrainingAnalyticsFacade.Module3Id,
            TrainingAnalyticsFacade.Module3Name,
            "hospital_triage_progress",
            "Hastane Triyaj Ilerlemesi",
            progress,
            new Dictionary<string, object>
            {
                { AnalyticsParams.ScenarioId, TrainingAnalyticsFacade.Module3ScenarioId },
                { AnalyticsParams.ScenarioName, TrainingAnalyticsFacade.Module3ScenarioName },
                { AnalyticsParams.VictimId, victimId },
                { AnalyticsParams.VictimName, victimName },
                { AnalyticsParams.CompletedCount, tamamlananTriyaj },
                { AnalyticsParams.TotalCount, toplamYarali },
                { AnalyticsParams.CorrectCount, dogruTriyaj },
                { AnalyticsParams.IncorrectCount, yanlisTriyaj }
            });

        if (!resultShown && toplamYarali > 0 && tamamlananTriyaj >= toplamYarali)
        {
            completionRealtime = now;
            ShowResultPanel();
        }
    }

    public void ShowResultPanel()
    {
        ResolveSceneReferences();
        if (resultPanel == null)
        {
            return;
        }

        TriageResultSnapshot snapshot = GetResultSnapshot();

        if (resultPanelSpawnAnchor != null)
        {
            resultPanel.transform.position = resultPanelSpawnAnchor.position;
            resultPanel.transform.rotation = resultPanelSpawnAnchor.rotation;
        }
        else if (spawnResultPanelInFrontOfCamera)
        {
            Transform cameraTransform = XRCameraHelper.GetPlayerCameraTransform();
            if (cameraTransform != null)
            {
                Vector3 position = cameraTransform.position + cameraTransform.forward * resultPanelDistance;
                resultPanel.transform.position = position;
                resultPanel.transform.rotation =
                    Quaternion.LookRotation(resultPanel.transform.position - cameraTransform.position, cameraTransform.up);
            }
        }

        if (!completionAnalyticsSent)
        {
            float scorePercent = Mathf.Round(snapshot.accuracyPercent * 100f) / 100f;
            Dictionary<string, object> completionParameters = new Dictionary<string, object>
            {
                { AnalyticsParams.CompletedCount, snapshot.completedCount },
                { AnalyticsParams.TotalCount, snapshot.totalCount },
                { AnalyticsParams.CorrectCount, snapshot.correctCount },
                { AnalyticsParams.IncorrectCount, snapshot.incorrectCount },
                { "completion_percent", Mathf.Round(snapshot.completionPercent * 100f) / 100f },
                { "duration_seconds", Mathf.Round(snapshot.durationSeconds * 100f) / 100f },
                { "active_decision_seconds", Mathf.Round(snapshot.activeDecisionSeconds * 100f) / 100f },
                { "average_decision_seconds", Mathf.Round(snapshot.averageDecisionSeconds * 100f) / 100f },
                { "patients_per_minute", Mathf.Round(snapshot.patientsPerMinute * 100f) / 100f },
                { "under_triage_count", snapshot.underTriageCount },
                { "over_triage_count", snapshot.overTriageCount },
                { "critical_mismatch_count", snapshot.criticalMismatchCount },
                { "longest_correct_streak", snapshot.longestCorrectStreak }
            };

            TrainingAnalyticsFacade.OnScoreRecorded(
                TrainingAnalyticsFacade.Module3Id,
                TrainingAnalyticsFacade.Module3Name,
                "triage_accuracy",
                snapshot.correctCount,
                scorePercent,
                completionParameters);

            TrainingAnalyticsFacade.OnScenarioCompleted(
                TrainingAnalyticsFacade.Module3Id,
                TrainingAnalyticsFacade.Module3Name,
                TrainingAnalyticsFacade.Module3ScenarioId,
                TrainingAnalyticsFacade.Module3ScenarioName,
                completionParameters);

            TrainingAnalyticsFacade.OnModuleCompleted(
                TrainingAnalyticsFacade.Module3Id,
                TrainingAnalyticsFacade.Module3Name,
                completionParameters);

            completionAnalyticsSent = true;
        }

        resultShown = true;
        UpdateHudVisibility();
        resultPanel.ShowPanel(snapshot);
    }

    public void HideResultPanel()
    {
        resultShown = false;
        resultPanel?.HidePanel();
        UpdateHudVisibility();
    }

    public bool ResetHospitalScenario()
    {
        if (!hospitalModeActive)
        {
            return false;
        }

        activeNpc = null;
        resultShown = false;
        completionAnalyticsSent = false;
        dogruTriyaj = 0;
        yanlisTriyaj = 0;
        completedTriageRecords.Clear();
        npcDialogStartTimes.Clear();
        triageRecordOrder = 0;
        ResetTriageTimingState(hospitalPhaseActive);

        NPCTriageInteractable[] npcs = FindObjectsOfType<NPCTriageInteractable>(true);
        registeredNpcs.Clear();
        SortNpcsForScenarioSeeding(npcs);

        for (int i = 0; i < npcs.Length; i++)
        {
            NPCTriageInteractable npc = npcs[i];
            if (npc == null)
            {
                continue;
            }

            npc.ResetTriageState();
        }

        ApplyScenarioSeeds(npcs);

        for (int i = 0; i < npcs.Length; i++)
        {
            if (npcs[i] != null)
            {
                registeredNpcs.Add(npcs[i]);
            }
        }

        toplamYarali = registeredNpcs.Count;
        tamamlananTriyaj = 0;

        TriyajManager triyajManager = TriyajManager.Instance;
        if (triyajManager == null)
        {
            triyajManager = FindObjectOfType<TriyajManager>();
        }

        triyajManager?.ResetHospitalStats();
        UpdateResultPanelLive();
        HideResultPanel();
        return true;
    }

    public bool RespawnPlayerAtHospitalStart()
    {
        ResolveSceneReferences();
        Transform targetAnchor = ResolveHospitalStartAnchor();
        if (targetAnchor == null)
        {
            Debug.LogWarning("[HospitalTriageManager] Hastane retry anchor bulunamadı.");
            return false;
        }

        Vector3 targetPosition = targetAnchor.position;
        float targetYaw = targetAnchor.eulerAngles.y + hospitalStartYawOffset;
        return VRSpawnPoint.TryRespawnPlayerRigRoot(this, targetPosition, targetYaw, 4);
    }

    public bool ReturnPlayerToBase()
    {
        ResolveSceneReferences();
        Transform targetAnchor = ResolveBaseReturnAnchor();
        if (targetAnchor == null)
        {
            Debug.LogWarning("[HospitalTriageManager] Üsse dönüş için base anchor bulunamadı.");
            return false;
        }

        Vector3 targetPosition = ResolveSafeSpawnPosition(targetAnchor.position);
        return VRSpawnPoint.TryRespawnPlayerRigRoot(this, targetPosition, targetAnchor.eulerAngles.y, 4);
    }

    private void RegisterNpc(NPCTriageInteractable npc, bool recalculateTotal)
    {
        if (npc == null)
        {
            return;
        }

        if (registeredNpcs.Add(npc) && recalculateTotal)
        {
            toplamYarali = registeredNpcs.Count;
            UpdateResultPanelLive();
        }
    }

    private void RecordCompletedTriage(
        MonoBehaviour npc,
        TriageCategory givenCategory,
        TriageCategory actualCategory,
        float decisionDurationSeconds,
        float completedAtRealtime)
    {
        int npcInstanceId = 0;
        string npcName = "Bilinmeyen Hasta";
        string complaint = string.Empty;

        if (npc != null)
        {
            npcInstanceId = npc.GetInstanceID();
            npcName = string.IsNullOrWhiteSpace(npc.name) ? npcName : npc.name.Trim();

            NPCTriageInteractable npcInteractable = npc as NPCTriageInteractable;
            if (npcInteractable != null)
            {
                npcName = string.IsNullOrWhiteSpace(npcInteractable.PatientTitle) ? npcName : npcInteractable.PatientTitle;
                complaint = NormalizeComplaintForContext(npcInteractable.ComplaintText);
            }

            RemoveCompletedRecordForNpc(npcInstanceId);
        }

        triageRecordOrder++;
        completedTriageRecords.Add(
            new CompletedTriageRecord(
                npcInstanceId,
                triageRecordOrder,
                npcName,
                complaint,
                givenCategory,
                actualCategory,
                givenCategory == actualCategory,
                decisionDurationSeconds,
                completedAtRealtime));
    }

    private void RemoveCompletedRecordForNpc(int npcInstanceId)
    {
        if (npcInstanceId == 0 || completedTriageRecords.Count == 0)
        {
            return;
        }

        for (int i = completedTriageRecords.Count - 1; i >= 0; i--)
        {
            if (completedTriageRecords[i].npcInstanceId == npcInstanceId)
            {
                completedTriageRecords.RemoveAt(i);
            }
        }
    }

    private void RecalculateCompletedTriageStats()
    {
        int correctCount = 0;
        for (int i = 0; i < completedTriageRecords.Count; i++)
        {
            if (completedTriageRecords[i].isCorrect)
            {
                correctCount++;
            }
        }

        tamamlananTriyaj = Mathf.Min(completedTriageRecords.Count, toplamYarali);
        dogruTriyaj = correctCount;
        yanlisTriyaj = Mathf.Max(0, tamamlananTriyaj - dogruTriyaj);

        if (tamamlananTriyaj <= 0)
        {
            firstTriageRealtime = -1f;
            lastTriageRealtime = -1f;
            completionRealtime = -1f;
        }
        else if (toplamYarali > 0 && tamamlananTriyaj >= toplamYarali && completionRealtime < 0f && lastTriageRealtime >= 0f)
        {
            completionRealtime = lastTriageRealtime;
        }
    }

    public TriageResultSnapshot GetResultSnapshot()
    {
        TriageResultSnapshot snapshot = new TriageResultSnapshot
        {
            totalCount = Mathf.Max(0, toplamYarali)
        };

        snapshot.completedCount = Mathf.Clamp(tamamlananTriyaj, 0, snapshot.totalCount);
        snapshot.pendingCount = Mathf.Max(0, snapshot.totalCount - snapshot.completedCount);
        snapshot.correctCount = Mathf.Clamp(dogruTriyaj, 0, snapshot.completedCount);
        snapshot.incorrectCount = Mathf.Clamp(yanlisTriyaj, 0, snapshot.completedCount);
        snapshot.accuracyPercent = snapshot.completedCount > 0
            ? (snapshot.correctCount * 100f) / snapshot.completedCount
            : 0f;
        snapshot.completionPercent = snapshot.totalCount > 0
            ? (snapshot.completedCount * 100f) / snapshot.totalCount
            : 0f;

        FillTimelineMetrics(ref snapshot);
        FillCategoryMetrics(ref snapshot);
        FillRiskMetrics(ref snapshot);
        return snapshot;
    }

    private void FillTimelineMetrics(ref TriageResultSnapshot snapshot)
    {
        float startRealtime = hospitalPhaseStartRealtime;
        if (startRealtime < 0f)
        {
            startRealtime = firstTriageRealtime;
        }

        if (startRealtime < 0f && completedTriageRecords.Count > 0)
        {
            startRealtime = completedTriageRecords[0].completedAtRealtime;
            for (int i = 1; i < completedTriageRecords.Count; i++)
            {
                if (completedTriageRecords[i].completedAtRealtime < startRealtime)
                {
                    startRealtime = completedTriageRecords[i].completedAtRealtime;
                }
            }
        }

        float endRealtime = completionRealtime;
        if (endRealtime < 0f)
        {
            endRealtime = lastTriageRealtime;
        }

        if (endRealtime < 0f && snapshot.completedCount > 0)
        {
            endRealtime = Time.realtimeSinceStartup;
        }

        if (startRealtime >= 0f && endRealtime >= startRealtime)
        {
            snapshot.durationSeconds = endRealtime - startRealtime;
        }

        if (firstTriageRealtime >= 0f && lastTriageRealtime >= firstTriageRealtime)
        {
            snapshot.activeDecisionSeconds = lastTriageRealtime - firstTriageRealtime;
        }
        else
        {
            snapshot.activeDecisionSeconds = snapshot.durationSeconds;
        }

        if (snapshot.activeDecisionSeconds <= 0f && snapshot.durationSeconds > 0f)
        {
            snapshot.activeDecisionSeconds = snapshot.durationSeconds;
        }

        snapshot.averageDecisionSeconds = snapshot.completedCount > 0
            ? snapshot.activeDecisionSeconds / snapshot.completedCount
            : 0f;

        float speedBasisSeconds = snapshot.activeDecisionSeconds > 0f
            ? snapshot.activeDecisionSeconds
            : snapshot.durationSeconds;
        snapshot.patientsPerMinute = speedBasisSeconds > 0.01f
            ? snapshot.completedCount / (speedBasisSeconds / 60f)
            : 0f;

        List<float> decisionDurations = new List<float>(completedTriageRecords.Count);
        for (int i = 0; i < completedTriageRecords.Count; i++)
        {
            float duration = completedTriageRecords[i].decisionDurationSeconds;
            if (duration > 0.01f)
            {
                decisionDurations.Add(duration);
            }
        }

        if (decisionDurations.Count > 0)
        {
            decisionDurations.Sort();
            snapshot.fastestDecisionSeconds = decisionDurations[0];
            snapshot.slowestDecisionSeconds = decisionDurations[decisionDurations.Count - 1];

            int medianIndex = decisionDurations.Count / 2;
            if (decisionDurations.Count % 2 == 0)
            {
                snapshot.medianDecisionSeconds = (decisionDurations[medianIndex - 1] + decisionDurations[medianIndex]) * 0.5f;
            }
            else
            {
                snapshot.medianDecisionSeconds = decisionDurations[medianIndex];
            }
        }
        else if (snapshot.completedCount > 0)
        {
            snapshot.fastestDecisionSeconds = snapshot.averageDecisionSeconds;
            snapshot.medianDecisionSeconds = snapshot.averageDecisionSeconds;
            snapshot.slowestDecisionSeconds = snapshot.averageDecisionSeconds;
        }
    }

    private void FillCategoryMetrics(ref TriageResultSnapshot snapshot)
    {
        int actualGreen = 0;
        int actualYellow = 0;
        int actualRed = 0;
        int actualBlack = 0;

        foreach (NPCTriageInteractable npc in registeredNpcs)
        {
            if (npc == null)
            {
                continue;
            }

            IncrementCategoryCount(npc.ActualCategory, ref actualGreen, ref actualYellow, ref actualRed, ref actualBlack);
        }

        int givenGreen = 0;
        int givenYellow = 0;
        int givenRed = 0;
        int givenBlack = 0;
        int correctGreen = 0;
        int correctYellow = 0;
        int correctRed = 0;
        int correctBlack = 0;

        for (int i = 0; i < completedTriageRecords.Count; i++)
        {
            CompletedTriageRecord record = completedTriageRecords[i];
            IncrementCategoryCount(record.givenCategory, ref givenGreen, ref givenYellow, ref givenRed, ref givenBlack);

            if (record.isCorrect)
            {
                IncrementCategoryCount(record.actualCategory, ref correctGreen, ref correctYellow, ref correctRed, ref correctBlack);
            }
        }

        if (actualGreen + actualYellow + actualRed + actualBlack <= 0 && completedTriageRecords.Count > 0)
        {
            for (int i = 0; i < completedTriageRecords.Count; i++)
            {
                IncrementCategoryCount(
                    completedTriageRecords[i].actualCategory,
                    ref actualGreen,
                    ref actualYellow,
                    ref actualRed,
                    ref actualBlack);
            }
        }

        snapshot.greenActualCount = actualGreen;
        snapshot.yellowActualCount = actualYellow;
        snapshot.redActualCount = actualRed;
        snapshot.blackActualCount = actualBlack;

        snapshot.greenGivenCount = givenGreen;
        snapshot.yellowGivenCount = givenYellow;
        snapshot.redGivenCount = givenRed;
        snapshot.blackGivenCount = givenBlack;

        snapshot.greenCorrectCount = correctGreen;
        snapshot.yellowCorrectCount = correctYellow;
        snapshot.redCorrectCount = correctRed;
        snapshot.blackCorrectCount = correctBlack;

        snapshot.greenAccuracyPercent = CalculateCategoryAccuracy(actualGreen, correctGreen);
        snapshot.yellowAccuracyPercent = CalculateCategoryAccuracy(actualYellow, correctYellow);
        snapshot.redAccuracyPercent = CalculateCategoryAccuracy(actualRed, correctRed);
        snapshot.blackAccuracyPercent = CalculateCategoryAccuracy(actualBlack, correctBlack);
    }

    private void FillRiskMetrics(ref TriageResultSnapshot snapshot)
    {
        if (completedTriageRecords.Count <= 0)
        {
            snapshot.underTriageCount = 0;
            snapshot.overTriageCount = 0;
            snapshot.criticalMismatchCount = 0;
            snapshot.longestCorrectStreak = 0;
            return;
        }

        List<CompletedTriageRecord> orderedRecords = new List<CompletedTriageRecord>(completedTriageRecords);
        orderedRecords.Sort((left, right) => left.order.CompareTo(right.order));

        int underCount = 0;
        int overCount = 0;
        int criticalCount = 0;
        int longestStreak = 0;
        int currentStreak = 0;

        for (int i = 0; i < orderedRecords.Count; i++)
        {
            CompletedTriageRecord record = orderedRecords[i];
            if (record.isCorrect)
            {
                currentStreak++;
                if (currentStreak > longestStreak)
                {
                    longestStreak = currentStreak;
                }

                continue;
            }

            currentStreak = 0;

            int actualRank = GetTriageUrgencyRank(record.actualCategory);
            int givenRank = GetTriageUrgencyRank(record.givenCategory);
            if (actualRank >= 0 && givenRank >= 0)
            {
                if (actualRank > givenRank)
                {
                    underCount++;
                }
                else if (actualRank < givenRank)
                {
                    overCount++;
                }
            }

            bool redMiss = record.actualCategory == TriageCategory.Red && record.givenCategory != TriageCategory.Red;
            bool blackGivenWrong = record.actualCategory != TriageCategory.Black && record.givenCategory == TriageCategory.Black;
            bool blackMiss = record.actualCategory == TriageCategory.Black && record.givenCategory != TriageCategory.Black;
            if (redMiss || blackGivenWrong || blackMiss)
            {
                criticalCount++;
            }
        }

        snapshot.underTriageCount = underCount;
        snapshot.overTriageCount = overCount;
        snapshot.criticalMismatchCount = criticalCount;
        snapshot.longestCorrectStreak = longestStreak;
    }

    private static void IncrementCategoryCount(
        TriageCategory category,
        ref int greenCount,
        ref int yellowCount,
        ref int redCount,
        ref int blackCount)
    {
        switch (category)
        {
            case TriageCategory.Green:
                greenCount++;
                break;

            case TriageCategory.Yellow:
                yellowCount++;
                break;

            case TriageCategory.Red:
                redCount++;
                break;

            case TriageCategory.Black:
                blackCount++;
                break;
        }
    }

    private static float CalculateCategoryAccuracy(int actualCount, int correctCount)
    {
        if (actualCount <= 0)
        {
            return 0f;
        }

        return (correctCount * 100f) / actualCount;
    }

    private static int GetTriageUrgencyRank(TriageCategory category)
    {
        switch (category)
        {
            case TriageCategory.Red:
                return 3;

            case TriageCategory.Yellow:
                return 2;

            case TriageCategory.Green:
                return 1;

            case TriageCategory.Black:
                return 0;

            default:
                return -1;
        }
    }

    private void SetActiveNpc(NPCTriageInteractable npc)
    {
        if (npc == null)
        {
            activeNpc = null;
            return;
        }

        if (activeNpc != null && activeNpc != npc)
        {
            RemoveNpcDialogStart(activeNpc);
        }

        activeNpc = npc;
        int npcInstanceId = npc.GetInstanceID();
        if (!npcDialogStartTimes.ContainsKey(npcInstanceId))
        {
            npcDialogStartTimes[npcInstanceId] = Time.realtimeSinceStartup;
        }

        if (hospitalPhaseStartRealtime < 0f && hospitalPhaseActive)
        {
            hospitalPhaseStartRealtime = Time.realtimeSinceStartup;
        }
    }

    private void ResetTriageTimingState(bool startNow)
    {
        float now = Time.realtimeSinceStartup;
        hospitalPhaseStartRealtime = startNow ? now : -1f;
        firstTriageRealtime = -1f;
        lastTriageRealtime = -1f;
        completionRealtime = -1f;
        npcDialogStartTimes.Clear();
    }

    private void RemoveNpcDialogStart(MonoBehaviour npc)
    {
        if (npc == null)
        {
            return;
        }

        npcDialogStartTimes.Remove(npc.GetInstanceID());
    }

    private float ConsumeNpcDecisionDuration(MonoBehaviour npc, float now)
    {
        if (npc == null)
        {
            return 0f;
        }

        int npcInstanceId = npc.GetInstanceID();
        if (!npcDialogStartTimes.TryGetValue(npcInstanceId, out float startTime))
        {
            return 0f;
        }

        npcDialogStartTimes.Remove(npcInstanceId);
        return Mathf.Max(0f, now - startTime);
    }

    private void ApplyScenarioSeeds(NPCTriageInteractable[] npcs)
    {
        if (npcs == null)
        {
            return;
        }
        TriageCaseCatalog.ApplyProfiles(npcs);
    }

    private TriageScenario CreateGeneralScenario(NPCTriageInteractable npc)
    {
        if (npc == null)
        {
            return new TriageScenario(BuildScenarioComplaintForCategory(TriageCategory.Yellow), TriageCategory.Yellow);
        }

        TriageCategory category = npc.ActualCategory == TriageCategory.Unassigned
            ? TriageCategory.Yellow
            : npc.ActualCategory;
        return new TriageScenario(BuildScenarioComplaintForCategory(category), category);
    }

    private string BuildScenarioComplaintForCategory(TriageCategory category)
    {
        switch (category)
        {
            case TriageCategory.Green:
                return "Kolumda hafif siyriklar var, yuruyebiliyorum ve genel durumum iyi.";

            case TriageCategory.Yellow:
                return "Bacagimda derin yara var, kaniyor ve ustune basamiyorum.";

            case TriageCategory.Red:
                return "Nefes almakta zorlaniyorum, gogsumde siddetli baski hissediyorum.";

            case TriageCategory.Black:
                return "Hasta yanitsiz. Spontan solunum ve nabiz alinmiyor.";

            default:
                return GeneralScenarioComplaint;
        }
    }

    private void SortNpcsForScenarioSeeding(NPCTriageInteractable[] npcs)
    {
        if (npcs == null || npcs.Length < 2)
        {
            return;
        }

        System.Array.Sort(npcs, CompareNpcSeedOrder);
    }

    private List<NPCTriageInteractable> BuildOrderedNpcList()
    {
        List<NPCTriageInteractable> orderedNpcs = new List<NPCTriageInteractable>(registeredNpcs.Count);
        foreach (NPCTriageInteractable npc in registeredNpcs)
        {
            if (npc != null)
            {
                orderedNpcs.Add(npc);
            }
        }

        orderedNpcs.Sort(CompareNpcSeedOrder);
        return orderedNpcs;
    }

    private int CompareNpcSeedOrder(NPCTriageInteractable left, NPCTriageInteractable right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        int sceneComparison = string.CompareOrdinal(left.gameObject.scene.path, right.gameObject.scene.path);
        if (sceneComparison != 0)
        {
            return sceneComparison;
        }

        return string.CompareOrdinal(GetHierarchyOrderKey(left.transform), GetHierarchyOrderKey(right.transform));
    }

    private string GetHierarchyOrderKey(Transform target)
    {
        if (target == null)
        {
            return string.Empty;
        }

        string orderKey = target.GetSiblingIndex().ToString("D4") + "_" + target.name;
        Transform current = target.parent;
        while (current != null)
        {
            orderKey = current.GetSiblingIndex().ToString("D4") + "_" + current.name + "/" + orderKey;
            current = current.parent;
        }

        return orderKey;
    }

    private void UpdateResultPanelLive()
    {
        ResolveSceneReferences();
        RefreshCachedUi();
    }

    private void UpdateHudVisibility()
    {
        if (progressHud == null)
        {
            return;
        }

        progressHud.RefreshProgress(tamamlananTriyaj, toplamYarali);
        progressHud.SetVisible(hospitalPhaseActive && !resultShown);
    }

    private void ResolveSceneReferences()
    {
        if (!CanResolveSceneReferences())
        {
            return;
        }

        if (resultPanel == null)
        {
            resultPanel = FindObjectOfType<TriageResultPanel>(true);
        }

        if (progressHud == null)
        {
            progressHud = FindObjectOfType<TriageHUD>(true);
        }

        if (progressHud == null)
        {
            progressHud = TriageHUD.CreateRuntimeHud();
        }

        if (hospitalStartAnchor == null)
        {
            hospitalStartAnchor = FindNamedTransform(HospitalRetryAnchorName);
        }

        if (baseReturnAnchor == null)
        {
            baseReturnAnchor = FindNamedTransform(BaseReturnAnchorName);
        }
    }

    private void RefreshCachedUi()
    {
        resultPanel?.UpdateStats(GetResultSnapshot());

        if (progressHud != null)
        {
            progressHud.RefreshProgress(tamamlananTriyaj, toplamYarali);
        }

        UpdateHudVisibility();
    }

    private bool CanResolveSceneReferences()
    {
        if (isApplicationQuitting)
        {
            return false;
        }

        if (this == null || gameObject == null)
        {
            return false;
        }

        Scene currentScene = gameObject.scene;
        return currentScene.IsValid() && currentScene.isLoaded;
    }

    private Transform ResolveHospitalStartAnchor()
    {
        // Priority 1: preferred named spawn marker (legacy expected behavior)
        Transform preferredAnchor = FindNamedTransform(PreferredHospitalStartAnchorName);
        if (preferredAnchor != null)
        {
            hospitalStartAnchor = preferredAnchor;
            return hospitalStartAnchor;
        }

        // Priority 2: Inspector-assigned anchor
        if (hospitalStartAnchor != null)
        {
            return hospitalStartAnchor;
        }

        // Priority 3: retry anchor by name
        Transform namedAnchor = FindNamedTransform(HospitalRetryAnchorName);
        if (namedAnchor != null)
        {
            hospitalStartAnchor = namedAnchor;
            return hospitalStartAnchor;
        }

        // Priority 4: legacy pose from OnayMenusuManager hedefNokta
        if (TryResolveLegacyHospitalStartPose(out Vector3 targetPos, out float targetYaw))
        {
            GameObject runtimeAnchor = new GameObject(HospitalRetryAnchorName);
            runtimeAnchor.transform.position = targetPos;
            runtimeAnchor.transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
            hospitalStartAnchor = runtimeAnchor.transform;
            return hospitalStartAnchor;
        }

        // Priority 5: scene-wide fallback search
        Transform fallbackAnchor = FindHospitalRetryAnchorFromScene();
        if (fallbackAnchor != null)
        {
            hospitalStartAnchor = fallbackAnchor;
            return hospitalStartAnchor;
        }

        Debug.LogError("[HospitalTriageManager] Hiçbir spawn anchor bulunamadı. hospitalStartAnchor Inspector'da atanmalı.");
        return null;
    }

    private Transform ResolveBaseReturnAnchor()
    {
        Transform namedAnchor = FindNamedTransform(BaseReturnAnchorName);
        if (namedAnchor != null)
        {
            baseReturnAnchor = namedAnchor;
            return baseReturnAnchor;
        }

        if (baseReturnAnchor != null)
        {
            return baseReturnAnchor;
        }

        Transform fallbackSpawn = FindNamedTransform(BaseSpawnFallbackName);
        if (fallbackSpawn == null)
        {
            return null;
        }

        GameObject runtimeAnchor = new GameObject(BaseReturnAnchorName);
        runtimeAnchor.transform.position = fallbackSpawn.position;
        runtimeAnchor.transform.rotation = fallbackSpawn.rotation;
        baseReturnAnchor = runtimeAnchor.transform;
        return baseReturnAnchor;
    }

    private void CaptureAndDisablePlayerLocomotion()
    {
        if (locomotionSnapshotCaptured)
        {
            return;
        }

        locomotionStateSnapshot.Clear();

        XROrigin xrOrigin = XRCameraHelper.GetXROrigin();

        if (xrOrigin == null)
        {
            return;
        }

        SnapshotAndDisable(xrOrigin.GetComponent<LocomotionSystem>());
        SnapshotAndDisable(xrOrigin.GetComponentInChildren<LocomotionSystem>(true));
        SnapshotAndDisableRange(xrOrigin.GetComponentsInChildren<ContinuousMoveProviderBase>(true));
        SnapshotAndDisableRange(xrOrigin.GetComponentsInChildren<ContinuousTurnProviderBase>(true));
        SnapshotAndDisableRange(xrOrigin.GetComponentsInChildren<SnapTurnProviderBase>(true));
        SnapshotAndDisableRange(xrOrigin.GetComponentsInChildren<TeleportationProvider>(true));

        locomotionSnapshotCaptured = true;
    }

    private void RestorePlayerLocomotionFromSnapshot()
    {
        if (!locomotionSnapshotCaptured)
        {
            return;
        }

        foreach (KeyValuePair<Behaviour, bool> pair in locomotionStateSnapshot)
        {
            if (pair.Key != null)
            {
                pair.Key.enabled = pair.Value;
            }
        }

        locomotionStateSnapshot.Clear();
        locomotionSnapshotCaptured = false;
    }

    private void ReleaseAllUiFocusImmediate()
    {
        uiFocusOwners.Clear();
        RestorePlayerLocomotionFromSnapshot();
    }

    private void SnapshotAndDisable(Behaviour behaviour)
    {
        if (behaviour == null || locomotionStateSnapshot.ContainsKey(behaviour))
        {
            return;
        }

        locomotionStateSnapshot.Add(behaviour, behaviour.enabled);
        behaviour.enabled = false;
    }

    private void SnapshotAndDisableRange<TBehaviour>(TBehaviour[] behaviours) where TBehaviour : Behaviour
    {
        if (behaviours == null)
        {
            return;
        }

        for (int i = 0; i < behaviours.Length; i++)
        {
            SnapshotAndDisable(behaviours[i]);
        }
    }

    private int CountTriagedNpcs()
    {
        int count = 0;
        foreach (NPCTriageInteractable npc in registeredNpcs)
        {
            if (npc != null && npc.IsTriaged)
            {
                count++;
            }
        }

        return count;
    }

    private void AppendPendingComplaintSummary(StringBuilder builder)
    {
        if (builder == null)
        {
            return;
        }

        List<NPCTriageInteractable> pendingNpcs = new List<NPCTriageInteractable>();
        foreach (NPCTriageInteractable npc in registeredNpcs)
        {
            if (npc == null || npc.IsTriaged)
            {
                continue;
            }

            pendingNpcs.Add(npc);
        }

        if (pendingNpcs.Count == 0)
        {
            return;
        }

        pendingNpcs.Sort(CompareNpcSeedOrder);

        builder.Append(" Bekleyen sikayetler: ");
        int maxComplaintCount = Mathf.Min(3, pendingNpcs.Count);
        int appendedComplaintCount = 0;
        for (int i = 0; i < pendingNpcs.Count && appendedComplaintCount < maxComplaintCount; i++)
        {
            string complaint = NormalizeComplaintForContext(pendingNpcs[i] != null ? pendingNpcs[i].ComplaintText : null);
            if (string.IsNullOrWhiteSpace(complaint))
            {
                continue;
            }

            appendedComplaintCount++;
            builder.Append(appendedComplaintCount);
            builder.Append(") ");
            builder.Append(complaint);

            if (appendedComplaintCount < maxComplaintCount)
            {
                builder.Append(' ');
            }
        }

        int remainingComplaintCount = Mathf.Max(0, pendingNpcs.Count - appendedComplaintCount);
        if (remainingComplaintCount > 0)
        {
            if (appendedComplaintCount > 0)
            {
                builder.Append(' ');
            }

            builder.Append('+');
            builder.Append(remainingComplaintCount);
            builder.Append(" hasta daha bekliyor.");
            return;
        }

        if (appendedComplaintCount > 0)
        {
            builder.Append('.');
        }
    }

    private void AppendCompletedTriageSummary(StringBuilder builder)
    {
        if (builder == null || completedTriageRecords.Count == 0)
        {
            return;
        }

        List<CompletedTriageRecord> orderedRecords = new List<CompletedTriageRecord>(completedTriageRecords);
        orderedRecords.Sort((left, right) => right.order.CompareTo(left.order));

        int maxRecordCount = Mathf.Min(4, orderedRecords.Count);
        builder.Append(" Son triyajlar: ");
        for (int i = 0; i < maxRecordCount; i++)
        {
            CompletedTriageRecord record = orderedRecords[i];
            builder.Append(i + 1);
            builder.Append(") ");
            builder.Append(record.npcName);
            if (!string.IsNullOrWhiteSpace(record.complaint))
            {
                builder.Append(" [");
                builder.Append(record.complaint);
                builder.Append(']');
            }

            builder.Append(" secim=");
            builder.Append(record.givenCategory);
            builder.Append(", gercek=");
            builder.Append(record.actualCategory);
            builder.Append(", sonuc=");
            builder.Append(record.isCorrect ? "dogru" : "yanlis");

            if (i < maxRecordCount - 1)
            {
                builder.Append(' ');
            }
            else
            {
                builder.Append('.');
            }
        }
    }

    private static string NormalizeComplaintForContext(string complaint)
    {
        if (string.IsNullOrWhiteSpace(complaint))
        {
            return string.Empty;
        }

        string normalized = complaint.Replace('\n', ' ').Replace('\r', ' ').Trim();
        while (normalized.Contains("  "))
        {
            normalized = normalized.Replace("  ", " ");
        }

        if (normalized.Length > 56)
        {
            normalized = normalized.Substring(0, 53).TrimEnd() + "...";
        }

        return normalized;
    }

    private static bool ShouldReleaseActiveNpcLock(NPCTriageInteractable npc)
    {
        return npc == null || !npc.isActiveAndEnabled || npc.IsTriaged || !npc.IsDialogOpen;
    }

    private static Transform FindHospitalRetryAnchorFromScene()
    {
        for (int i = 0; i < HospitalRetryAnchorFallbackNames.Length; i++)
        {
            Transform candidate = FindNamedTransform(HospitalRetryAnchorFallbackNames[i]);
            if (candidate != null)
            {
                return candidate;
            }
        }

        return null;
    }

    private static Vector3 ResolveSafeSpawnPosition(Vector3 targetPosition)
    {
        return VRSpawnPoint.ResolveCameraTargetAboveGround(targetPosition);
    }

    private static Transform FindNamedTransform(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        // Fast path: only searches active objects
        GameObject targetObject = GameObject.Find(objectName);
        if (targetObject != null)
        {
            return targetObject.transform;
        }

        // Slow path: scene root traversal includes inactive objects
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindInChildrenByName(roots[i].transform, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform FindInChildrenByName(Transform parent, string name)
    {
        if (parent.name == name)
        {
            return parent;
        }
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindInChildrenByName(parent.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    private static bool TryResolveLegacyHospitalStartPose(out Vector3 targetPos, out float targetYaw)
    {
        targetPos = Vector3.zero;
        targetYaw = 0f;

        TriyajModul3.OnayMenusuManager[] teleportMenus = FindObjectsOfType<TriyajModul3.OnayMenusuManager>(true);
        for (int i = 0; i < teleportMenus.Length; i++)
        {
            TriyajModul3.OnayMenusuManager teleportMenu = teleportMenus[i];
            if (teleportMenu == null || teleportMenu.hedefNokta == null)
            {
                continue;
            }

            targetPos = teleportMenu.hedefNokta.position + teleportMenu.spawnOffset;
            targetYaw = teleportMenu.hedefNokta.eulerAngles.y + teleportMenu.spawnRotationOffset;
            return true;
        }

        return false;
    }
}
