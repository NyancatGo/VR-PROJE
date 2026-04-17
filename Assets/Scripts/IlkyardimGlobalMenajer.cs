using System.Collections.Generic;
using TrainingAnalytics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IlkyardimGlobalMenajer : MonoBehaviour
{
    public static IlkyardimGlobalMenajer Instance { get; private set; }

    public int yerlestirilenYaraliSayisi = 0;
    public bool allCharactersPlaced = false;
    public bool LastPlacementUiOpened { get; private set; }

    private const int TotalVictimCount = 3;
    private const string DontDestroySceneName = "DontDestroyOnLoad";
    private readonly HashSet<int> _placedVictimIds = new HashSet<int>();

    private int _activePlacementIndex = -1;
    private bool _uiOpenForCurrentPlacement = false;
    private IlkyardimUIMenajeri _activeManager;

    public bool IsPlacementSequenceActive
    {
        get
        {
            return yerlestirilenYaraliSayisi > 0 && yerlestirilenYaraliSayisi < TotalVictimCount;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            ResetRuntimeState();
            return;
        }

        if (Instance == this)
        {
            ResetRuntimeState();
            return;
        }

        int currentPriority = GetSingletonPriority();
        int existingPriority = Instance != null ? Instance.GetSingletonPriority() : int.MinValue;
        if (currentPriority > existingPriority)
        {
            IlkyardimGlobalMenajer previous = Instance;
            Instance = this;
            ResetRuntimeState();

            if (previous != null)
            {
                Debug.LogWarning($"[GlobalMenajer] Daha uygun singleton secildi, onceki component kaldiriliyor: {previous.name}", previous);
                Destroy(previous);
            }

            return;
        }

        Debug.LogWarning($"[GlobalMenajer] Fazla instance tespit edildi, bu component devre disi birakiliyor: {name}", this);
        Destroy(this);
    }

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        IlkyardimGlobalMenajer[] all = FindObjectsOfType<IlkyardimGlobalMenajer>(true);
        if (all.Length > 1)
        {
            int destroyed = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i] != this)
                {
                    Debug.LogWarning($"[GlobalMenajer] Start suresinde fazla instance temizleniyor: {all[i].gameObject.name} (scene: {all[i].gameObject.scene.name})", all[i]);
                    Destroy(all[i]);
                    destroyed++;
                }
            }

            if (destroyed > 0)
            {
                Debug.LogWarning($"[GlobalMenajer] {destroyed} fazla instance temizlendi. Aktif singleton: {gameObject.name}");
            }
        }

        Debug.Log($"[GlobalMenajer] Singleton aktif: {gameObject.name}, scene={gameObject.scene.name}, priority={GetSingletonPriority()}, hasUIMenajeri={GetComponent<IlkyardimUIMenajeri>() != null}");
    }

    public void KarakterYerlestirildi(Transform victimTransform = null)
    {
        if (victimTransform != null)
        {
            int victimInstanceId = victimTransform.GetInstanceID();
            if (!_placedVictimIds.Add(victimInstanceId))
            {
                Debug.LogWarning("[GlobalMenajer] Ayni yarali icin tekrar yerlestirme bildirimi geldi; sayac artirilmadi.", victimTransform);
                LastPlacementUiOpened = _activeManager != null && _activeManager.IsCanvasCurrentlyVisible;
                return;
            }
        }

        if (yerlestirilenYaraliSayisi >= TotalVictimCount)
        {
            Debug.LogWarning("[GlobalMenajer] Tum yaralilar zaten sayilmis durumda; ek yerlestirme yok sayildi.");
            LastPlacementUiOpened = _activeManager != null && _activeManager.IsCanvasCurrentlyVisible;
            return;
        }

        yerlestirilenYaraliSayisi++;
        int placementIndex = Mathf.Clamp(yerlestirilenYaraliSayisi - 1, 0, TotalVictimCount - 1);

        Debug.Log($"[GlobalMenajer] Yarali yerlestirildi: {yerlestirilenYaraliSayisi}/{TotalVictimCount} (index={placementIndex})");

        TrainingAnalyticsFacade.OnTaskProgress(
            TrainingAnalyticsFacade.Module2Id,
            TrainingAnalyticsFacade.Module2Name,
            "victim_placement_progress",
            "Yarali Yerlestirme Ilerlemesi",
            Mathf.Clamp01((float)yerlestirilenYaraliSayisi / TotalVictimCount),
            new System.Collections.Generic.Dictionary<string, object>
            {
                { AnalyticsParams.ScenarioId, TrainingAnalyticsFacade.Module2ScenarioId },
                { AnalyticsParams.ScenarioName, TrainingAnalyticsFacade.Module2ScenarioName },
                { AnalyticsParams.CompletedCount, yerlestirilenYaraliSayisi },
                { AnalyticsParams.TotalCount, TotalVictimCount },
                { AnalyticsParams.PlacementCount, yerlestirilenYaraliSayisi },
                { AnalyticsParams.TotalPlacements, TotalVictimCount }
            });

        bool uiOpened = TryOpenFirstAidUiForPlacement(placementIndex, victimTransform);
        LastPlacementUiOpened = uiOpened;

        if (yerlestirilenYaraliSayisi >= TotalVictimCount)
        {
            allCharactersPlaced = true;
            Debug.Log("Tum yaralilar yerlesti! Ilk Yardim arayuzu kullanilabilir.");

            TrainingAnalyticsFacade.OnCriticalActionTaken(
                TrainingAnalyticsFacade.Module2Id,
                TrainingAnalyticsFacade.Module2Name,
                "all_victims_placed",
                "Tum Yaralilar Yerlestirildi",
                new System.Collections.Generic.Dictionary<string, object>
            {
                { AnalyticsParams.CompletedCount, yerlestirilenYaraliSayisi },
                { AnalyticsParams.TotalCount, TotalVictimCount }
            });
        }
    }

    private bool TryOpenFirstAidUiForPlacement(int placementIndex, Transform victimTransform)
    {
        Debug.Log($"[GlobalMenajer] TryOpenFirstAidUiForPlacement - placementIndex={placementIndex}, victim={victimTransform?.name}");

        if (_uiOpenForCurrentPlacement && _activePlacementIndex == placementIndex)
        {
            Debug.Log($"[GlobalMenajer] UI zaten acik for placement {placementIndex}, yeniden acilmiyor.");
            return false;
        }

        if (_activeManager != null)
        {
            _activeManager.KapatUI();
        }
        NPCWorldCanvas.HideAllCanvases();

        IlkyardimUIMenajeri[] managers = FindObjectsOfType<IlkyardimUIMenajeri>(true);
        if (managers == null || managers.Length == 0)
        {
            Debug.LogWarning("[GlobalMenajer] IlkyardimUIMenajeri bulunamadi!");
            TryOpenFallbackWorldCanvas(placementIndex);
            return false;
        }

        IlkyardimUIMenajeri bestManager = SelectBestUiManager(managers);

        if (bestManager == null)
        {
            Debug.LogWarning("[GlobalMenajer] Gecerli IlkyardimUIMenajeri bulunamadi!");
            TryOpenFallbackWorldCanvas(placementIndex);
            return false;
        }

        _activePlacementIndex = placementIndex;
        _uiOpenForCurrentPlacement = true;
        _activeManager = bestManager;

        bestManager.OpenForPlacement(placementIndex, victimTransform);

        bool canvasActive = bestManager.uiCanvas != null && bestManager.uiCanvas.activeInHierarchy;
        bool uiVisible = bestManager.IsCanvasCurrentlyVisible;
        Debug.Log($"[GlobalMenajer] After OpenForPlacement - canvasActive={canvasActive}, uiVisible={uiVisible}");

        if (!canvasActive || !uiVisible)
        {
            IlkyardimUIMenajeri retryManager = SelectBestUiManager(managers, bestManager);
            if (retryManager != null)
            {
                Debug.LogWarning($"[GlobalMenajer] Ilk managerde UI gorunmedi, alternatif manager deneniyor: {retryManager.name}");
                bestManager.KapatUI();

                _activeManager = retryManager;
                retryManager.OpenForPlacement(placementIndex, victimTransform);

                canvasActive = retryManager.uiCanvas != null && retryManager.uiCanvas.activeInHierarchy;
                uiVisible = retryManager.IsCanvasCurrentlyVisible;
                Debug.Log($"[GlobalMenajer] Retry OpenForPlacement - canvasActive={canvasActive}, uiVisible={uiVisible}");
            }
        }

        if (!canvasActive || !uiVisible)
        {
            Debug.LogWarning($"[GlobalMenajer] Merkez UI canvas active DEGIL; fallback deneniyor. Placement {placementIndex}");
            TryOpenFallbackWorldCanvas(placementIndex);
            _uiOpenForCurrentPlacement = false;
            _activeManager = null;
            return false;
        }

        Debug.Log($"[GlobalMenajer] Merkez UI acildi - Placement {placementIndex}");
        return true;
    }

    private void TryOpenFallbackWorldCanvas(int placementIndex)
    {
        Debug.Log("[GlobalMenajer] Merkez UI acilamadi, fallback world canvas deneniyor...");
        NPCWorldCanvas[] allCanvases = FindObjectsOfType<NPCWorldCanvas>(true);
        for (int i = 0; i < allCanvases.Length; i++)
        {
            NPCWorldCanvas canvas = allCanvases[i];
            if (canvas == null) continue;

            WoundedNPC npc = canvas.GetComponentInParent<WoundedNPC>();
            if (npc != null && npc.healthData != null)
            {
                IlkyardimNPCIndex npcIndex = npc.GetComponent<IlkyardimNPCIndex>();
                int npcIdx = npcIndex != null ? npcIndex.index : i;

                if (npcIdx == placementIndex || (npcIdx == placementIndex + 1))
                {
                    canvas.ShowCanvas(true);
                    Debug.Log($"[GlobalMenajer] Fallback canvas acildi for NPC {npcIdx}");
                    return;
                }
            }
        }

        if (allCanvases.Length > 0)
        {
            allCanvases[0].ShowCanvas(true);
            Debug.Log($"[GlobalMenajer] Ilk siradaki fallback canvas acildi");
        }
    }

    public void CloseAllWorldCanvases()
    {
        NPCWorldCanvas.HideAllCanvases();
    }

    public void CloseActiveUi()
    {
        if (_activeManager != null)
        {
            _activeManager.KapatUI();
        }
        _uiOpenForCurrentPlacement = false;
        _activePlacementIndex = -1;
        _activeManager = null;
    }

    private int GetSingletonPriority()
    {
        int score = 0;

        if (GetComponent<IlkyardimUIMenajeri>() != null)
        {
            score += 100;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (gameObject.scene == activeScene)
        {
            score += 50;
        }

        if (gameObject.scene.name == DontDestroySceneName)
        {
            score -= 100;
        }

        return score;
    }

    private void ResetRuntimeState()
    {
        yerlestirilenYaraliSayisi = 0;
        allCharactersPlaced = false;
        LastPlacementUiOpened = false;

        _placedVictimIds.Clear();
        _activePlacementIndex = -1;
        _uiOpenForCurrentPlacement = false;
        _activeManager = null;
    }

    private IlkyardimUIMenajeri SelectBestUiManager(IlkyardimUIMenajeri[] managers, IlkyardimUIMenajeri exclude = null, int placementIndex = -1)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        IlkyardimUIMenajeri bestManager = null;
        int bestScore = int.MinValue;

        for (int i = 0; i < managers.Length; i++)
        {
            IlkyardimUIMenajeri manager = managers[i];
            if (manager == null || manager == exclude)
            {
                continue;
            }

            int score = 0;
            if (manager.isActiveAndEnabled)
            {
                score += 10;
            }

            if (manager.gameObject.scene == activeScene)
            {
                score += 40;
            }

            if (manager.gameObject.scene.name == DontDestroySceneName)
            {
                score -= 40;
            }

            if (manager.uiCanvas != null)
            {
                score += 60;

                if (manager.uiCanvas.scene == activeScene)
                {
                    score += 40;
                }

                if (manager.uiCanvas.scene.name == DontDestroySceneName)
                {
                    score -= 40;
                }

                if (manager.uiCanvas.activeInHierarchy)
                {
                    score += 5;
                }

                if (placementIndex >= 0)
                {
                    // Yarali ismine/indexine gore ozel eslesme (Yarali 1 -> index 0 vs)
                    string searchName1 = (placementIndex + 1).ToString();
                    if (manager.uiCanvas.name.Contains(searchName1))
                    {
                        score += 500;
                    }
                }
            }
            else
            {
                score -= 20;
            }

            if (placementIndex >= 0)
            {
                WoundedNPC npc = manager.GetComponentInParent<WoundedNPC>();
                if (npc == null && manager.uiCanvas != null)
                {
                    npc = manager.uiCanvas.GetComponentInParent<WoundedNPC>();
                }
                
                if (npc != null)
                {
                    IlkyardimNPCIndex npcIndex = npc.GetComponent<IlkyardimNPCIndex>();
                    int idx = npcIndex != null ? npcIndex.index : -1;
                    if (idx == placementIndex)
                    {
                        score += 1000;
                    }
                    else if (idx != -1)
                    {
                        score -= 500; // EGER BASKA BIR YARALIYA AIT CANVAS ISE KESINLIKLE KULLANMA
                    }
                }
                else
                {
                    // isimden cikarim yap (Orn: Canvas_Yarali1)
                    if (manager.gameObject.name.Contains("Yarali") && !manager.gameObject.name.Contains((placementIndex + 1).ToString()))
                    {
                         score -= 500;
                    }
                }
            }

            Debug.Log($"[GlobalMenajer] Manager aday skor: {manager.name}, score={score}, scene={manager.gameObject.scene.name}, uiCanvas={manager.uiCanvas?.name}");

            if (score > bestScore)
            {
                bestScore = score;
                bestManager = manager;
            }
        }

        return bestManager;
    }

    public bool IsUiOpenForVictim(int victimIndex)
    {
        return _uiOpenForCurrentPlacement && _activePlacementIndex == victimIndex;
    }

    public int GetActivePlacementIndex()
    {
        return _activePlacementIndex;
    }
}
