using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrainingAnalytics;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance;

    public int totalCones = 5;
    private int placedCones = 0;

    public TextMeshProUGUI missionText;
    public TextMeshProUGUI counterText;

    public int equipmentNeeded = 2; // kask + balta
    public int totalFires = 10;
    private int equippedCount = 0;
    private int fireCount = 0;
    private readonly HashSet<string> equippedItems = new HashSet<string>();

    public string AnaSahneAdi;

    private bool coneTaskStarted;
    private bool coneTaskCompleted;
    private bool equipmentTaskStarted;
    private bool equipmentTaskCompleted;
    private bool rescueTaskStarted;
    private bool rescueTaskCompleted;
    private bool fireTaskStarted;
    private bool fireTaskCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[TaskManager] Duplicate instance detected, replacing previous reference.");
        }

        Instance = this;
    }

    private void Start()
    {
        BeginModule4Analytics();
        StartConeTaskAnalytics();
    }

    public void ConePlaced()
    {
        placedCones++;

        Debug.Log("Yerlestirilen: " + placedCones);

        if (counterText != null)
        {
            counterText.SetText(placedCones + "/" + totalCones);
        }

        TrackTaskProgress(
            "konum_konisi",
            "Konum Konisi Yerlestirme",
            placedCones,
            totalCones,
            "cone_snap");

        if (placedCones >= totalCones)
        {
            CompleteTask();
        }
    }

    private void CompleteTask()
    {
        if (coneTaskCompleted)
        {
            return;
        }

        coneTaskCompleted = true;
        Debug.Log("Gorev Tamamlandi!");

        if (missionText != null)
        {
            missionText.SetText("Gorev Tamamlandi");
            missionText.fontSize = 70;
        }

        CompleteScenarioTask("konum_konisi", "Konum Konisi Yerlestirme", placedCones, totalCones, "cone_snap");
        StartCoroutine(NextMission());
    }

    private IEnumerator NextMission()
    {
        yield return new WaitForSeconds(3);

        if (missionText != null)
        {
            missionText.fontSize = 40;
            missionText.SetText("2. GOREV: Itfaiye aracindaki ekipmanlari kusan (Balta, Kask)");
        }

        if (counterText != null)
        {
            counterText.SetText("0/2");
        }

        StartEquipmentTaskAnalytics();
    }

    public void OnEquipmentEquipped()
    {
        OnEquipmentEquipped(null);
    }

    public void OnEquipmentEquipped(string equipmentId)
    {
        if (!string.IsNullOrEmpty(equipmentId) && !equippedItems.Add(equipmentId))
        {
            Debug.Log("[TaskManager] Equipment already counted: " + equipmentId);
            return;
        }

        if (equippedCount >= equipmentNeeded)
        {
            Debug.Log("[TaskManager] Equipment goal already completed. Ignoring extra equip event.");
            return;
        }

        StartEquipmentTaskAnalytics();
        equippedCount++;

        if (counterText != null)
        {
            counterText.SetText(equippedCount + "/" + equipmentNeeded);
        }

        Debug.Log("[TaskManager] Equipment progress: " + equippedCount + "/" + equipmentNeeded + (string.IsNullOrEmpty(equipmentId) ? string.Empty : " (" + equipmentId + ")"));

        TrackCriticalAction(
            "ekipman_takildi",
            "Ekipman Takildi",
            string.IsNullOrEmpty(equipmentId) ? "equipment" : equipmentId);
        TrackTaskProgress(
            "ekipman_kusanma",
            "Ekipman Kusanma",
            equippedCount,
            equipmentNeeded,
            string.IsNullOrEmpty(equipmentId) ? "equipment" : equipmentId);

        if (equippedCount >= equipmentNeeded)
        {
            if (equipmentTaskCompleted)
            {
                return;
            }

            equipmentTaskCompleted = true;
            Debug.Log("[TaskManager] Equipment task completed.");

            if (missionText != null)
            {
                missionText.SetText("Gorev Tamamlandi");
                missionText.fontSize = 70;
            }

            CompleteScenarioTask("ekipman_kusanma", "Ekipman Kusanma", equippedCount, equipmentNeeded, "equipment");
            StartCoroutine(NextMission2());
        }
    }

    private IEnumerator NextMission2()
    {
        yield return new WaitForSeconds(3);

        if (missionText != null)
        {
            missionText.fontSize = 30;
            missionText.SetText("3. GOREV: Icerideki yaraliyi kurtar ve ambulansin yanina gotur. Evin kapisi disaridan kitli oldugu icin baltani kullanarak cami kir ve eve gir.");
        }

        if (counterText != null)
        {
            counterText.gameObject.SetActive(false);
        }

        StartRescueTaskAnalytics();
    }

    public void NPCCured()
    {
        if (rescueTaskCompleted)
        {
            return;
        }

        StartRescueTaskAnalytics();
        rescueTaskCompleted = true;
        Debug.Log("yarali gorevi tamamlandi!");

        if (missionText != null)
        {
            missionText.SetText("Gorev Tamamlandi");
            missionText.fontSize = 70;
        }

        TrackCriticalAction("yarali_kurtarildi", "Yarali Kurtarildi", "injured_npc");
        CompleteScenarioTask("yarali_kurtarma", "Yarali Kurtarma", 1, 1, "ambulance");
        StartCoroutine(NextMission3());
    }

    private IEnumerator NextMission3()
    {
        yield return new WaitForSeconds(3);

        if (missionText != null)
        {
            missionText.fontSize = 40;
            missionText.SetText("4. GOREV: Itfaiye aracindan yangin nozulunu al ve yangini sondur!.");
        }

        if (counterText != null)
        {
            counterText.gameObject.SetActive(true);
            counterText.SetText("%0");
        }

        StartFireTaskAnalytics();
    }

    public void Firefire()
    {
        if (fireTaskCompleted)
        {
            return;
        }

        StartFireTaskAnalytics();
        fireCount++;

        if (counterText != null)
        {
            int total = Mathf.Max(1, totalFires);
            int percent = Mathf.RoundToInt(Mathf.Clamp01((float)fireCount / total) * 100f);
            counterText.SetText("%" + percent);
        }

        TrackTaskProgress(
            "yangin_sondurme",
            "Yangin Sondurme",
            fireCount,
            totalFires,
            "water_nozzle");

        if (fireCount >= totalFires && !fireTaskCompleted)
        {
            fireTaskCompleted = true;

            if (missionText != null)
            {
                missionText.SetText("EGITIM TAMAMLANDI. TEBRIKLER :)");
            }

            TrackCriticalAction("yangin_sonduruldu", "Yangin Sonduruldu", "water_nozzle");
            CompleteScenarioTask("yangin_sondurme", "Yangin Sondurme", fireCount, totalFires, "water_nozzle");
            CompleteModule4Analytics();
        }
    }

    private IEnumerator Final()
    {
        yield return new WaitForSeconds(5);

        if (!string.IsNullOrEmpty(AnaSahneAdi))
        {
            XRSceneRuntimeStabilizer.PrepareForSceneTransition();
            XRCameraHelper.ClearCache();
            SceneManager.LoadScene(AnaSahneAdi);
        }
    }

    private void BeginModule4Analytics()
    {
        Dictionary<string, object> parameters = BuildModule4Parameters("scene_start");
        parameters[AnalyticsParams.TotalCount] = 4;

        TrainingAnalyticsFacade.EnsureModuleEntered(
            TrainingAnalyticsFacade.Module4Id,
            TrainingAnalyticsFacade.Module4Name,
            parameters,
            "module4_entered");

        TrainingAnalyticsFacade.EnsureScenarioStarted(
            TrainingAnalyticsFacade.Module4Id,
            TrainingAnalyticsFacade.Module4Name,
            TrainingAnalyticsFacade.Module4ScenarioId,
            TrainingAnalyticsFacade.Module4ScenarioName,
            parameters,
            "module4_scenario_started");
    }

    private void StartConeTaskAnalytics()
    {
        if (coneTaskStarted)
        {
            return;
        }

        coneTaskStarted = true;
        StartScenarioTask("konum_konisi", "Konum Konisi Yerlestirme", totalCones, "scene_start");
    }

    private void StartEquipmentTaskAnalytics()
    {
        if (equipmentTaskStarted)
        {
            return;
        }

        equipmentTaskStarted = true;
        StartScenarioTask("ekipman_kusanma", "Ekipman Kusanma", equipmentNeeded, "mission_step");
    }

    private void StartRescueTaskAnalytics()
    {
        if (rescueTaskStarted)
        {
            return;
        }

        rescueTaskStarted = true;
        StartScenarioTask("yarali_kurtarma", "Yarali Kurtarma", 1, "mission_step");
    }

    private void StartFireTaskAnalytics()
    {
        if (fireTaskStarted)
        {
            return;
        }

        fireTaskStarted = true;
        StartScenarioTask("yangin_sondurme", "Yangin Sondurme", totalFires, "mission_step");
    }

    private void StartScenarioTask(string taskId, string taskName, int totalCount, string source)
    {
        Dictionary<string, object> parameters = BuildModule4Parameters(source);
        parameters[AnalyticsParams.TaskType] = "module4_scenario_task";
        parameters[AnalyticsParams.CompletedCount] = 0;
        parameters[AnalyticsParams.TotalCount] = Mathf.Max(1, totalCount);

        TrainingAnalyticsFacade.OnTaskStarted(
            TrainingAnalyticsFacade.Module4Id,
            TrainingAnalyticsFacade.Module4Name,
            taskId,
            taskName,
            parameters);
    }

    private void TrackTaskProgress(string taskId, string taskName, int completedCount, int totalCount, string source)
    {
        int safeTotal = Mathf.Max(1, totalCount);
        int safeCompleted = Mathf.Clamp(completedCount, 0, safeTotal);

        Dictionary<string, object> parameters = BuildModule4Parameters(source);
        parameters[AnalyticsParams.TaskType] = "module4_scenario_task";
        parameters[AnalyticsParams.CompletedCount] = safeCompleted;
        parameters[AnalyticsParams.TotalCount] = safeTotal;

        TrainingAnalyticsFacade.OnTaskProgress(
            TrainingAnalyticsFacade.Module4Id,
            TrainingAnalyticsFacade.Module4Name,
            taskId,
            taskName,
            Mathf.Clamp01((float)safeCompleted / safeTotal),
            parameters);
    }

    private void CompleteScenarioTask(string taskId, string taskName, int completedCount, int totalCount, string source)
    {
        int safeTotal = Mathf.Max(1, totalCount);
        int safeCompleted = Mathf.Clamp(completedCount, 0, safeTotal);

        Dictionary<string, object> parameters = BuildModule4Parameters(source);
        parameters[AnalyticsParams.TaskType] = "module4_scenario_task";
        parameters[AnalyticsParams.CompletedCount] = safeCompleted;
        parameters[AnalyticsParams.TotalCount] = safeTotal;

        TrainingAnalyticsFacade.OnTaskCompleted(
            TrainingAnalyticsFacade.Module4Id,
            TrainingAnalyticsFacade.Module4Name,
            taskId,
            taskName,
            parameters);

        TrainingAnalyticsFacade.OnScenarioTaskCompleted(
            TrainingAnalyticsFacade.Module4Id,
            TrainingAnalyticsFacade.Module4Name,
            TrainingAnalyticsFacade.Module4ScenarioId,
            TrainingAnalyticsFacade.Module4ScenarioName,
            taskId,
            taskName,
            parameters);
    }

    private void TrackCriticalAction(string actionId, string actionName, string source)
    {
        TrainingAnalyticsFacade.OnCriticalActionTaken(
            TrainingAnalyticsFacade.Module4Id,
            TrainingAnalyticsFacade.Module4Name,
            actionId,
            actionName,
            BuildModule4Parameters(source));
    }

    private void CompleteModule4Analytics()
    {
        Dictionary<string, object> parameters = BuildModule4Parameters("module4_complete");
        parameters[AnalyticsParams.CompletedCount] = 4;
        parameters[AnalyticsParams.TotalCount] = 4;

        TrainingAnalyticsFacade.OnScoreRecorded(
            TrainingAnalyticsFacade.Module4Id,
            TrainingAnalyticsFacade.Module4Name,
            "yangin_mudahale_skor",
            100f,
            100f,
            parameters);

        TrainingAnalyticsFacade.OnScenarioCompleted(
            TrainingAnalyticsFacade.Module4Id,
            TrainingAnalyticsFacade.Module4Name,
            TrainingAnalyticsFacade.Module4ScenarioId,
            TrainingAnalyticsFacade.Module4ScenarioName,
            parameters);

        TrainingAnalyticsFacade.OnModuleCompleted(
            TrainingAnalyticsFacade.Module4Id,
            TrainingAnalyticsFacade.Module4Name,
            parameters);
    }

    private static Dictionary<string, object> BuildModule4Parameters(string source)
    {
        return new Dictionary<string, object>
        {
            { AnalyticsParams.ScenarioId, TrainingAnalyticsFacade.Module4ScenarioId },
            { AnalyticsParams.ScenarioName, TrainingAnalyticsFacade.Module4ScenarioName },
            { AnalyticsParams.Source, string.IsNullOrWhiteSpace(source) ? "module4" : source }
        };
    }
}
