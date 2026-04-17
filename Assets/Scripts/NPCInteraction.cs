using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TrainingAnalytics;

/// <summary>
/// VR'da NPC ile etkilesime girildiginde UI acar.
/// YENI MIMARI: UI acma yetkisi IlkyardimGlobalMenajer'dedir.
/// Bu script sadece GlobalMenajer'e talep gonderir, kendisi UI acmaz.
/// </summary>
public class NPCInteraction : MonoBehaviour
{
    [Tooltip("NPC'in World Canvas'i - GlobalMenajer yetkilendirmesi ile kullanilir")]
    public NPCWorldCanvas myWorldCanvas;

    private XRBaseInteractable _interactable;

    private void Awake()
    {
        _interactable = GetComponent<XRBaseInteractable>();
        if (_interactable == null)
        {
            Debug.LogError("NPCInteraction requires an XRBaseInteractable on the object.", this);
        }
    }

    private void OnEnable()
    {
        if (_interactable != null)
        {
            _interactable.activated.AddListener(OnNPCActivated);
        }
    }

    private void OnDisable()
    {
        if (_interactable != null)
        {
            _interactable.activated.RemoveListener(OnNPCActivated);
        }
    }

    private void OnNPCActivated(ActivateEventArgs args)
    {
        YaraliController yc = GetComponent<YaraliController>();
        if (yc != null && !yc.isPlacedAtYaraliYeri)
        {
            return;
        }

        TrainingAnalyticsFacade.OnCriticalActionTaken(
            TrainingAnalyticsFacade.Module2Id,
            TrainingAnalyticsFacade.Module2Name,
            "npc_activated",
            "NPC Aktivasyonu",
            new System.Collections.Generic.Dictionary<string, object>
            {
                { AnalyticsParams.VictimId, TrainingAnalyticsFacade.ResolveVictimId(this) },
                { AnalyticsParams.VictimName, TrainingAnalyticsFacade.ResolveVictimName(this) },
                { AnalyticsParams.EntrySource, "xr_activate" }
            });

        if (IlkyardimGlobalMenajer.Instance != null)
        {
            int victimIndex = ResolveVictimIndex();
            if (IlkyardimGlobalMenajer.Instance.IsUiOpenForVictim(victimIndex))
            {
                Debug.Log($"[NPCInteraction] UI zaten acik for victim {victimIndex}");
                return;
            }

            Debug.Log($"[NPCInteraction] Placement tabanli UI akisi aktif; victim {victimIndex} icin yeni bir UI acma tetiklenmedi.");
            return;
        }
        else
        {
            Debug.LogWarning("[NPCInteraction] GlobalMenajer yok, fallback canvas aciliyor.");
            FallbackOpenWorldCanvas();
        }
    }

    private int ResolveVictimIndex()
    {
        IlkyardimNPCIndex npcIndex = GetComponent<IlkyardimNPCIndex>();
        if (npcIndex != null)
        {
            return npcIndex.index > 0 ? npcIndex.index - 1 : npcIndex.index;
        }

        return 0;
    }

    private void FallbackOpenWorldCanvas()
    {
        if (myWorldCanvas != null)
        {
            myWorldCanvas.ShowCanvas();
        }
    }
}
