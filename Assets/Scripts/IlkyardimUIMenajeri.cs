using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrainingAnalytics;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class Senaryo
{
    public string senaryoAdi;
    public string adim1;
    public string adim2;
    public string adim3;
}

public class IlkyardimUIMenajeri : MonoBehaviour
{
    private const int TotalScenarioSteps = 3;
    private const int TotalVictimCount = 3;

    [Header("UI Referanslari (Inspector'dan atanmis referanslar korunur)")]
    [SerializeField] public GameObject uiCanvas;
    [SerializeField] public TextMeshProUGUI durumMetni;
    [SerializeField] public Button islemButonu;

    [Header("Runtime Guard")]
    [SerializeField] private bool openUiOnPlacement = true;
    [SerializeField] private bool requireAllCharactersPlacedToOpenUi = true;

    [Header("Pozisyon Ayarlari")]
    [SerializeField] private float canvasHeightOffset = 1.2f;
    [SerializeField] private float canvasForwardOffset = -1.5f;
    [SerializeField] private float canvasDistanceFromCamera = 1.5f;
    [SerializeField] private float canvasVerticalOffsetFromCamera = 0.2f;
    [SerializeField] private Vector3 canvasWorldScale = new Vector3(0.002f, 0.002f, 0.002f);
    [SerializeField] private bool flattenCameraForwardOnGround = true;
    [SerializeField] private bool flipCanvasFacing = false;

    [Header("Senaryolar")]
    public Senaryo[] senaryolar = new Senaryo[3];

    private int aktifYaraliIndex = -1;
    private int aktifSenaryoAdimi = 0;
    private bool islemDevamEdiyor = false;
    private bool _scenariosInitialized = false;
    private Transform _lastVictimTransform;
    private CanvasGroup _cachedCanvasGroup;
    private Button _boundIslemButonu;

    public bool IsCanvasCurrentlyVisible
    {
        get
        {
            if (uiCanvas == null || !uiCanvas.activeInHierarchy)
            {
                return false;
            }

            CanvasGroup canvasGroup = _cachedCanvasGroup != null ? _cachedCanvasGroup : uiCanvas.GetComponent<CanvasGroup>();
            return canvasGroup == null || canvasGroup.alpha > 0.01f;
        }
    }

    private void Awake()
    {
        InitializeScenariosOnce();
        ResolveReferencesIfNeeded(true);
        EnsureButtonListener();
    }

    private void Start()
    {
        InitializeScenariosOnce();
        ResolveReferencesIfNeeded(true);
        EnsureButtonListener();
        ForceCloseUI();
    }

    private void OnDestroy()
    {
        RemoveButtonListener();
    }

    public void OpenForPlacement(int placementIndex, Transform victimTransform)
    {
        InitializeScenariosOnce();
        ResolveReferencesIfNeeded(true);
        EnsureButtonListener();

        aktifYaraliIndex = Mathf.Clamp(placementIndex, 0, TotalVictimCount - 1);
        aktifSenaryoAdimi = 0;
        _lastVictimTransform = victimTransform;

        Debug.Log($"[IlkyardimUIMenajeri] OpenForPlacement called - victimTransform={victimTransform?.name}, position={victimTransform?.position}");

        NPCWorldCanvas.HideAllCanvases();
        ForceOpenUI();
        SyncVRHealthPanel(victimTransform, aktifYaraliIndex); // GUVENCE: Canvas icindeki detayli verileri hedef yaraliya gore guncelle
        PositionCanvasNearVictim(victimTransform);
        ArayuzGuncelle();

        Debug.Log($"[IlkyardimUIMenajeri] UI opened - canvas active={uiCanvas?.activeInHierarchy}, pos={uiCanvas?.transform.position}");

        TrainingAnalyticsFacade.EnsureScenarioStarted(
            TrainingAnalyticsFacade.Module2Id,
            TrainingAnalyticsFacade.Module2Name,
            TrainingAnalyticsFacade.Module2ScenarioId,
            TrainingAnalyticsFacade.Module2ScenarioName,
            BuildParameters(),
            "module2_first_aid_scenario_started");

        TrainingAnalyticsFacade.OnVictimInteracted(
            TrainingAnalyticsFacade.Module2Id,
            TrainingAnalyticsFacade.Module2Name,
            GetVictimIdForAnalytics(),
            GetVictimNameForAnalytics(),
            BuildParameters());
    }

    public void ArayuzAc(int npcIndex)
    {
        ArayuzAc(npcIndex, false, null);
    }

    public void ArayuzAc(int npcIndex, bool bypassPlacementGate)
    {
        ArayuzAc(npcIndex, bypassPlacementGate, null);
    }

    public void ArayuzAc(int npcIndex, bool bypassPlacementGate, Transform victimTransform)
    {
        InitializeScenariosOnce();
        ResolveReferencesIfNeeded(true);
        EnsureButtonListener();

        if (ShouldBlockDirectOpen(bypassPlacementGate))
        {
            return;
        }

        aktifYaraliIndex = NormalizeIndex(npcIndex);
        aktifSenaryoAdimi = 0;
        _lastVictimTransform = victimTransform;

        NPCWorldCanvas.HideAllCanvases();
        ForceOpenUI();
        PositionCanvasNearVictim(victimTransform);
        ArayuzGuncelle();
    }

    public void KapatUI()
    {
        if (uiCanvas != null)
        {
            uiCanvas.SetActive(false);
        }

        if (_cachedCanvasGroup != null)
        {
            _cachedCanvasGroup.alpha = 0f;
            _cachedCanvasGroup.interactable = false;
            _cachedCanvasGroup.blocksRaycasts = false;
        }

        aktifYaraliIndex = -1;
        aktifSenaryoAdimi = 0;
        islemDevamEdiyor = false;
        _lastVictimTransform = null;
    }

    private void ForceOpenUI()
    {
        ResolveReferencesIfNeeded(true);
        EnsureButtonListener();

        if (uiCanvas == null)
        {
            Debug.LogWarning("[IlkyardimUIMenajeri] UI canvas resolve edilemedi.");
            return;
        }

        PrepareCanvasForManagedFlow();
        uiCanvas.SetActive(true);

        Canvas canvas = uiCanvas.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = true;
            canvas.renderMode = RenderMode.WorldSpace;
        }

        _cachedCanvasGroup = uiCanvas.GetComponent<CanvasGroup>();
        if (_cachedCanvasGroup != null)
        {
            _cachedCanvasGroup.alpha = 1f;
            _cachedCanvasGroup.interactable = true;
            _cachedCanvasGroup.blocksRaycasts = true;
            _cachedCanvasGroup.ignoreParentGroups = true;
        }

        // --- VR ETKİLEŞİM GÜVENCESİ ---
        var raycaster = uiCanvas.GetComponent("TrackedDeviceGraphicRaycaster");
        if (raycaster == null)
        {
            System.Type raycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (raycasterType != null)
            {
                uiCanvas.gameObject.AddComponent(raycasterType);
                Debug.Log("[IlkyardimUIMenajeri] TrackedDeviceGraphicRaycaster eklendi.");
                
                var standardRaycaster = uiCanvas.GetComponent<GraphicRaycaster>();
                if (standardRaycaster != null && standardRaycaster.GetType() != raycasterType)
                {
                    Destroy(standardRaycaster);
                }
            }
        }

        Canvas[] allCanvases = uiCanvas.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < allCanvases.Length; i++)
        {
            if (allCanvases[i] != null)
            {
                allCanvases[i].enabled = true;
            }
        }

        CanvasGroup[] allGroups = uiCanvas.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < allGroups.Length; i++)
        {
            if (allGroups[i] != null)
            {
                allGroups[i].alpha = 1f;
                allGroups[i].interactable = true;
                allGroups[i].blocksRaycasts = true;
                allGroups[i].ignoreParentGroups = true;
            }
        }
    }

    private void ForceCloseUI()
    {
        if (uiCanvas == null)
        {
            return;
        }

        uiCanvas.SetActive(false);

        Canvas canvas = uiCanvas.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = false;
        }

        if (_cachedCanvasGroup != null)
        {
            _cachedCanvasGroup.alpha = 0f;
            _cachedCanvasGroup.interactable = false;
            _cachedCanvasGroup.blocksRaycasts = false;
        }
    }

    private void ArayuzGuncelle()
    {
        ResolveReferencesIfNeeded(true);
        EnsureButtonListener();

        if (aktifYaraliIndex < 0 || aktifYaraliIndex >= senaryolar.Length || senaryolar[aktifYaraliIndex] == null)
        {
            if (durumMetni != null)
            {
                durumMetni.text = "Gecerli senaryo yok";
            }

            return;
        }

        Senaryo senaryo = senaryolar[aktifYaraliIndex];
        TextMeshProUGUI buttonLabel = islemButonu != null ? islemButonu.GetComponentInChildren<TextMeshProUGUI>() : null;

        if (aktifSenaryoAdimi == 0)
        {
            if (durumMetni != null)
            {
                durumMetni.text = $"[Yarali {aktifYaraliIndex + 1}] Senaryo: {senaryo.senaryoAdi}\nBekleniyor: {senaryo.adim1}";
            }

            if (buttonLabel != null)
            {
                buttonLabel.text = "BIRINCI MUDAHALE";
            }
        }
        else if (aktifSenaryoAdimi == 1)
        {
            if (durumMetni != null)
            {
                durumMetni.text = $"[Yarali {aktifYaraliIndex + 1}] Senaryo: {senaryo.senaryoAdi}\nBekleniyor: {senaryo.adim2}";
            }

            if (buttonLabel != null)
            {
                buttonLabel.text = "IKINCI MUDAHALE";
            }
        }
        else if (aktifSenaryoAdimi == 2)
        {
            if (durumMetni != null)
            {
                durumMetni.text = $"[Yarali {aktifYaraliIndex + 1}] Senaryo: {senaryo.senaryoAdi}\nBekleniyor: {senaryo.adim3}";
            }

            if (buttonLabel != null)
            {
                buttonLabel.text = "UCUNCU MUDAHALE";
            }
        }
        else
        {
            if (durumMetni != null)
            {
                durumMetni.text = "Tum islemler tamamlandi!";
            }

            if (buttonLabel != null)
            {
                buttonLabel.text = "KAPAT";
            }
        }
    }

    private void OnIslemButonuTiklandi()
    {
        if (islemDevamEdiyor)
        {
            return;
        }

        if (aktifSenaryoAdimi >= TotalScenarioSteps)
        {
            KapatUI();
            return;
        }

        string taskId = BuildStepTaskId();
        string taskName = GetCurrentStepName();
        StartCoroutine(IslemCoroutine(taskId, taskName));
    }

    private IEnumerator IslemCoroutine(string taskId, string taskName)
    {
        islemDevamEdiyor = true;

        if (islemButonu != null)
        {
            islemButonu.interactable = false;
        }

        if (durumMetni != null)
        {
            durumMetni.text = "Mudahale ediliyor, lutfen bekleyiniz...";
        }

        yield return new WaitForSeconds(3f);

        aktifSenaryoAdimi++;
        ArayuzGuncelle();

        Dictionary<string, object> parameters = BuildParameters();
        parameters[AnalyticsParams.TaskType] = "scenario_step";
        parameters[AnalyticsParams.StepIndex] = aktifSenaryoAdimi;
        parameters[AnalyticsParams.StepName] = taskName;
        parameters[AnalyticsParams.CompletedCount] = aktifSenaryoAdimi;
        parameters[AnalyticsParams.TotalCount] = TotalScenarioSteps;
        parameters[AnalyticsParams.CompletionSource] = "central_ui";

        TrainingAnalyticsFacade.OnTaskCompleted(
            TrainingAnalyticsFacade.Module2Id,
            TrainingAnalyticsFacade.Module2Name,
            taskId,
            taskName,
            parameters);

        TrainingAnalyticsFacade.OnScenarioTaskCompleted(
            TrainingAnalyticsFacade.Module2Id,
            TrainingAnalyticsFacade.Module2Name,
            TrainingAnalyticsFacade.Module2ScenarioId,
            TrainingAnalyticsFacade.Module2ScenarioName,
            taskId,
            taskName,
            parameters);

        if (aktifSenaryoAdimi >= TotalScenarioSteps)
        {
            TrainingAnalyticsFacade.RecordModule2VictimCompletion(null, aktifYaraliIndex, "central_ui", GetScenarioNameForAnalytics(), TotalVictimCount);
        }

        if (islemButonu != null)
        {
            islemButonu.interactable = true;
        }

        islemDevamEdiyor = false;
    }

    private Dictionary<string, object> BuildParameters()
    {
        return new Dictionary<string, object>
        {
            { AnalyticsParams.ScenarioId, TrainingAnalyticsFacade.Module2ScenarioId },
            { AnalyticsParams.ScenarioName, GetScenarioNameForAnalytics() },
            { AnalyticsParams.VictimId, GetVictimIdForAnalytics() },
            { AnalyticsParams.VictimName, GetVictimNameForAnalytics() },
            { AnalyticsParams.VictimIndex, aktifYaraliIndex + 1 }
        };
    }

    private string BuildStepTaskId()
    {
        return GetVictimIdForAnalytics() + "_step_" + (aktifSenaryoAdimi + 1);
    }

    private string GetScenarioNameForAnalytics()
    {
        return aktifYaraliIndex >= 0 && aktifYaraliIndex < senaryolar.Length && senaryolar[aktifYaraliIndex] != null
            ? senaryolar[aktifYaraliIndex].senaryoAdi
            : TrainingAnalyticsFacade.Module2ScenarioName;
    }

    private string GetCurrentStepName()
    {
        if (aktifYaraliIndex < 0 || aktifYaraliIndex >= senaryolar.Length || senaryolar[aktifYaraliIndex] == null)
        {
            return "Senaryo Adimi";
        }

        Senaryo senaryo = senaryolar[aktifYaraliIndex];
        return aktifSenaryoAdimi switch
        {
            0 => senaryo.adim1,
            1 => senaryo.adim2,
            2 => senaryo.adim3,
            _ => "Senaryo Tamamlandi"
        };
    }

    private string GetVictimIdForAnalytics()
    {
        return TrainingAnalyticsFacade.ResolveVictimId(null, aktifYaraliIndex);
    }

    private string GetVictimNameForAnalytics()
    {
        return "Yarali " + (aktifYaraliIndex + 1);
    }

    private int NormalizeIndex(int index)
    {
        int count = senaryolar != null && senaryolar.Length > 0 ? senaryolar.Length : TotalVictimCount;
        count = Mathf.Max(count, 1);

        if (index >= 0 && index < count)
        {
            return index;
        }

        if (index >= 1 && index <= count)
        {
            return index - 1;
        }

        return Mathf.Clamp(index, 0, count - 1);
    }

    private void InitializeScenariosOnce()
    {
        if (_scenariosInitialized)
        {
            return;
        }

        _scenariosInitialized = true;

        if (senaryolar == null || senaryolar.Length < TotalVictimCount)
        {
            senaryolar = new Senaryo[TotalVictimCount];
        }

        if (senaryolar[0] == null)
        {
            senaryolar[0] = new Senaryo { senaryoAdi = "Kalp Masaji", adim1 = "Durumu Kontrol Et", adim2 = "112'yi Ara", adim3 = "Kalp Masajina Basla" };
        }

        if (senaryolar[1] == null)
        {
            senaryolar[1] = new Senaryo { senaryoAdi = "Turnike", adim1 = "Kanama Kontrolu", adim2 = "Basi Uygula", adim3 = "Turnike Bagla" };
        }

        if (senaryolar[2] == null)
        {
            senaryolar[2] = new Senaryo { senaryoAdi = "Kirik Atel", adim1 = "Kirigi Sabitle", adim2 = "Atel Uygula", adim3 = "Hastanede Tedavi Bekle" };
        }
    }

    private void ResolveReferencesIfNeeded(bool force = false)
    {
        if (!force && uiCanvas != null && durumMetni != null && islemButonu != null)
        {
            return;
        }

        GameObject resolvedCanvas = ResolveCanvasReference();
        if (resolvedCanvas != null)
        {
            uiCanvas = resolvedCanvas;
        }

        if (uiCanvas == null)
        {
            return;
        }

        if (durumMetni == null || !IsComponentOnCanvas(durumMetni, uiCanvas))
        {
            durumMetni = FindBestStatusText(uiCanvas);
        }

        if (islemButonu == null || !IsComponentOnCanvas(islemButonu, uiCanvas))
        {
            islemButonu = FindBestActionButton(uiCanvas);
        }

        _cachedCanvasGroup = uiCanvas.GetComponent<CanvasGroup>();
    }

    private GameObject ResolveCanvasReference()
    {
        if (IsManagedCanvasCandidate(uiCanvas))
        {
            return uiCanvas;
        }

        GameObject fromButton = GetCanvasRootFromComponent(islemButonu);
        if (IsManagedCanvasCandidate(fromButton))
        {
            return fromButton;
        }

        GameObject fromText = GetCanvasRootFromComponent(durumMetni);
        if (IsManagedCanvasCandidate(fromText))
        {
            return fromText;
        }

        return FindBestCanvasCandidate();
    }

    private GameObject GetCanvasRootFromComponent(Component component)
    {
        if (component == null)
        {
            return null;
        }

        Canvas canvas = component.GetComponentInParent<Canvas>(true);
        return canvas != null ? canvas.gameObject : null;
    }

    private bool IsManagedCanvasCandidate(GameObject candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        Canvas canvas = candidate.GetComponent<Canvas>();
        if (canvas == null)
        {
            return false;
        }

        return candidate.GetComponentInParent<NPCWorldCanvas>(true) == null;
    }

    private GameObject FindBestCanvasCandidate()
    {
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
        Canvas bestCandidate = null;
        int bestScore = int.MinValue;

        for (int i = 0; i < allCanvases.Length; i++)
        {
            Canvas candidate = allCanvases[i];
            if (candidate == null)
            {
                continue;
            }

            int score = ScoreCanvasCandidate(candidate);
            if (score > bestScore)
            {
                bestScore = score;
                bestCandidate = candidate;
            }
        }

        return bestCandidate != null ? bestCandidate.gameObject : null;
    }

    private int ScoreCanvasCandidate(Canvas candidate)
    {
        int score = 0;

        if (candidate.GetComponentInParent<NPCWorldCanvas>(true) != null)
        {
            score -= 1000;
        }

        if (candidate.transform.parent == null)
        {
            score += 150;
        }

        if (candidate.renderMode == RenderMode.WorldSpace)
        {
            score += 40;
        }

        if (candidate.GetComponentInChildren<Button>(true) != null)
        {
            score += 40;
        }

        if (candidate.GetComponentInChildren<TextMeshProUGUI>(true) != null)
        {
            score += 40;
        }

        if (NameContains(candidate.name, "ilkyardim_canvas"))
        {
            score += 300;
        }
        else if (NameContains(candidate.name, "ilkyardim", "firstaid", "yardim", "modul2"))
        {
            score += 100;
        }

        return score;
    }

    private TextMeshProUGUI FindBestStatusText(GameObject canvasRoot)
    {
        if (canvasRoot == null)
        {
            return null;
        }

        TextMeshProUGUI[] texts = canvasRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
        TextMeshProUGUI bestText = null;
        int bestScore = int.MinValue;

        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI text = texts[i];
            if (text == null || text.GetComponentInParent<Button>() != null)
            {
                continue;
            }

            int score = 0;
            if (NameContains(text.name, "durum", "status", "metin", "mesaj", "info"))
            {
                score += 100;
            }

            if (NameContains(text.name, "feedback", "question", "description", "soru"))
            {
                score += 60;
            }

            if (text.rectTransform.rect.height >= 60f)
            {
                score += 10;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestText = text;
            }
        }

        return bestText;
    }

    private Button FindBestActionButton(GameObject canvasRoot)
    {
        if (canvasRoot == null)
        {
            return null;
        }

        Button[] buttons = canvasRoot.GetComponentsInChildren<Button>(true);
        Button bestButton = null;
        int bestScore = int.MinValue;

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            int score = 0;
            if (NameContains(button.name, "islem", "mudahale", "action", "islembutonu", "treatment", "button"))
            {
                score += 100;
            }

            if (button.interactable)
            {
                score += 10;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestButton = button;
            }
        }

        return bestButton;
    }

    private bool IsComponentOnCanvas(Component component, GameObject canvasRoot)
    {
        if (component == null || canvasRoot == null)
        {
            return false;
        }

        Canvas parentCanvas = component.GetComponentInParent<Canvas>(true);
        return parentCanvas != null && parentCanvas.gameObject == canvasRoot;
    }

    private bool ShouldBlockDirectOpen(bool bypassPlacementGate)
    {
        if (bypassPlacementGate)
        {
            return false;
        }

        if (requireAllCharactersPlacedToOpenUi &&
            (IlkyardimGlobalMenajer.Instance == null || !IlkyardimGlobalMenajer.Instance.allCharactersPlaced))
        {
            return true;
        }

        return openUiOnPlacement &&
               IlkyardimGlobalMenajer.Instance != null &&
               IlkyardimGlobalMenajer.Instance.IsPlacementSequenceActive;
    }

    private void EnsureButtonListener()
    {
        if (_boundIslemButonu == islemButonu)
        {
            return;
        }

        RemoveButtonListener();

        if (islemButonu == null)
        {
            return;
        }

        islemButonu.onClick.RemoveListener(OnIslemButonuTiklandi);
        islemButonu.onClick.AddListener(OnIslemButonuTiklandi);
        _boundIslemButonu = islemButonu;
    }

    private void RemoveButtonListener()
    {
        if (_boundIslemButonu == null)
        {
            return;
        }

        _boundIslemButonu.onClick.RemoveListener(OnIslemButonuTiklandi);
        _boundIslemButonu = null;
    }

    private void PrepareCanvasForManagedFlow()
    {
        if (uiCanvas == null)
        {
            return;
        }

        NPCWorldCanvas worldCanvas = uiCanvas.GetComponent<NPCWorldCanvas>();
        if (worldCanvas != null)
        {
            worldCanvas.HideCanvasImmediate();
            worldCanvas.enabled = false;
        }
    }

    private void PositionCanvasNearVictim(Transform victimTransform)
    {
        if (uiCanvas == null)
        {
            return;
        }

        Transform canvasTransform = uiCanvas.transform;
        Camera playerCamera = XRCameraHelper.GetPlayerCamera();
        Transform playerCameraTransform = playerCamera != null ? playerCamera.transform : XRCameraHelper.GetPlayerCameraTransform();
        Transform targetVictim = victimTransform != null ? victimTransform : _lastVictimTransform;

        if (playerCameraTransform != null)
        {
            Vector3 cameraForward = playerCameraTransform.forward;
            if (flattenCameraForwardOnGround)
            {
                Vector3 flattenedForward = Vector3.ProjectOnPlane(cameraForward, Vector3.up);
                if (flattenedForward.sqrMagnitude > 0.0001f)
                {
                    cameraForward = flattenedForward.normalized;
                }
            }

            float distance = Mathf.Max(0.8f, canvasDistanceFromCamera);
            Vector3 targetPosition = playerCameraTransform.position + cameraForward * distance;
            
            // Yüksekliği ayarla (kameradan biraz aşağıda veya yukarıda)
            targetPosition.y += canvasVerticalOffsetFromCamera;
            
            canvasTransform.position = targetPosition;

            // Billboarding: Her zaman oyuncuya bak (X ekseninde yatırmadan - sadece Y rotasyonu)
            Vector3 lookDir = cameraForward; // Doğrudan kameranın baktığı yöne bakmak (kamera arkasına geçerse)
            // Veya kameraya bakmak:
            Vector3 toCamera = playerCameraTransform.position - targetPosition;
            toCamera.y = 0; // Zemine paralel tut
            
            if (toCamera.sqrMagnitude > 0.001f)
            {
                canvasTransform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
            }
            else
            {
                canvasTransform.rotation = Quaternion.LookRotation(cameraForward, Vector3.up);
            }

            if (flipCanvasFacing)
            {
                canvasTransform.rotation *= Quaternion.Euler(0f, 180f, 0f);
            }
            
            // Son kontrol: Eğer kafa hizasından çok uzaklaşmışsa (beş metre kuralı gibi) veya çok yakınsa
            Debug.Log($"[IlkyardimUIMenajeri] Canvas konumlandirildi: {canvasTransform.position}, Mesafe: {Vector3.Distance(canvasTransform.position, playerCameraTransform.position)}");
        }
        else if (targetVictim != null)
        {
            Vector3 worldOffset = new Vector3(0f, canvasHeightOffset, canvasForwardOffset);
            canvasTransform.position = targetVictim.position + worldOffset;
            canvasTransform.rotation = Quaternion.identity;
        }

        Canvas canvas = uiCanvas.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = playerCamera;
        }

        if (canvasWorldScale.x <= 0f || canvasWorldScale.y <= 0f || canvasWorldScale.z <= 0f)
        {
            canvasWorldScale = new Vector3(0.002f, 0.002f, 0.002f);
        }

        canvasTransform.localScale = canvasWorldScale;
    }

    private void SyncVRHealthPanel(Transform victimTransform, int placementIndex)
    {
        if (uiCanvas == null) return;

        VRHealthPanel panel = uiCanvas.GetComponent<VRHealthPanel>();
        if (panel == null) panel = uiCanvas.GetComponentInChildren<VRHealthPanel>(true);

        if (panel != null)
        {
            WoundedNPC targetNpc = null;
            
            if (victimTransform != null)
            {
                targetNpc = victimTransform.GetComponentInParent<WoundedNPC>();
                if (targetNpc == null)
                {
                    targetNpc = victimTransform.GetComponentInChildren<WoundedNPC>();
                }
            }
            
            if (targetNpc != null)
            {
                panel.OpenForNPC(targetNpc);
                Debug.Log($"[IlkyardimUIMenajeri] VRHealthPanel guncellendi for NPC {placementIndex} via Transform {victimTransform.name}");
            }
            else
            {
                Debug.LogWarning($"[IlkyardimUIMenajeri] VRHealthPanel senkronizasyonu basarisiz: Transform uzerinde WoundedNPC bulunamadi! (Transform: {victimTransform?.name})");
            }
        }
    }

    private static bool NameContains(string source, params string[] terms)
    {
        if (string.IsNullOrEmpty(source) || terms == null)
        {
            return false;
        }

        string lower = source.ToLowerInvariant();
        for (int i = 0; i < terms.Length; i++)
        {
            string term = terms[i];
            if (!string.IsNullOrEmpty(term) && lower.Contains(term.ToLowerInvariant()))
            {
                return true;
            }
        }

        return false;
    }
}
