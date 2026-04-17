using TrainingAnalytics;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Text.RegularExpressions;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class VRGrabbable : MonoBehaviour
{
    private const int TotalVictimCount = 3;
    private XRGrabInteractable _grabInteractable;
    private XRSimpleInteractable _simpleInteractable;
    private Rigidbody _rb;
    private Animator _anim;

    private bool _placementProcessed = false;

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _simpleInteractable = GetComponent<XRSimpleInteractable>();
        _rb = GetComponent<Rigidbody>();
        _anim = GetComponent<Animator>();

        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.AddListener(OnGrabbedOrSocketed);
        }
    }

    private void Start()
    {
        if (_simpleInteractable != null)
        {
            _simpleInteractable.enabled = false;
        }
    }

    private void OnGrabbedOrSocketed(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor)
        {
            Debug.Log("<color=green><b>TEST: YARALI SOCKET'E OTURDU! FIZIK KILIDI BASLIYOR...</b></color>");
            Invoke(nameof(ProcessPlacement), 0.5f);
        }
    }

    private void ProcessPlacement()
    {
        if (_placementProcessed) return;
        _placementProcessed = true;

        if (_grabInteractable != null)
        {
            _grabInteractable.enabled = false;
        }

        Invoke(nameof(FinalizeAndNotify), 0.1f);
    }

    private void FinalizeAndNotify()
    {
        if (_rb != null)
        {
            _rb.useGravity = false;
            _rb.isKinematic = true;
            _rb.detectCollisions = false;
        }

        if (_anim != null)
        {
            _anim.enabled = false;
        }

        if (_simpleInteractable != null)
        {
            _simpleInteractable.enabled = true;
        }

        string victimId = TrainingAnalyticsFacade.ResolveVictimId(this);
        string victimName = TrainingAnalyticsFacade.ResolveVictimName(this);

        TrainingAnalyticsFacade.OnTaskCompleted(
            TrainingAnalyticsFacade.Module2Id,
            TrainingAnalyticsFacade.Module2Name,
            "victim_placement_" + victimId,
            "Yarali Yerlestirme",
            new System.Collections.Generic.Dictionary<string, object>
            {
                { AnalyticsParams.ScenarioId, TrainingAnalyticsFacade.Module2ScenarioId },
                { AnalyticsParams.ScenarioName, TrainingAnalyticsFacade.Module2ScenarioName },
                { AnalyticsParams.VictimId, victimId },
                { AnalyticsParams.VictimName, victimName },
                { AnalyticsParams.TaskType, "victim_placement" },
                { AnalyticsParams.CompletionSource, "socket_lock" }
            });

        if (IlkyardimGlobalMenajer.Instance != null)
        {
            IlkyardimGlobalMenajer.Instance.KarakterYerlestirildi(transform);
        }
        else
        {
            Debug.LogWarning("[VRGrabbable] IlkyardimGlobalMenajer.Instance null! UI acilamadi.");
        }

        enabled = false;
    }

    private int ResolveNpcIndex()
    {
        IlkyardimNPCIndex npcIndexComponent = GetComponent<IlkyardimNPCIndex>();
        if (npcIndexComponent != null)
        {
            return NormalizeVictimIndex(npcIndexComponent.index);
        }

        int parsedFromName = TryParseVictimIndexFromName(name);
        if (parsedFromName >= 0)
        {
            return parsedFromName;
        }

        if (transform.parent != null)
        {
            int parsedFromParentName = TryParseVictimIndexFromName(transform.parent.name);
            if (parsedFromParentName >= 0)
            {
                return parsedFromParentName;
            }
        }

        return 0;
    }

    private static int NormalizeVictimIndex(int rawIndex)
    {
        if (rawIndex >= 0 && rawIndex < TotalVictimCount)
        {
            return rawIndex;
        }

        if (rawIndex >= 1 && rawIndex <= TotalVictimCount)
        {
            return rawIndex - 1;
        }

        return Mathf.Clamp(rawIndex, 0, TotalVictimCount - 1);
    }

    private static int TryParseVictimIndexFromName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return -1;
        }

        Match match = Regex.Match(objectName, "(\\d+)");
        if (!match.Success)
        {
            return -1;
        }

        if (!int.TryParse(match.Value, out int number))
        {
            return -1;
        }

        return NormalizeVictimIndex(number);
    }

    private void OnDestroy()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrabbedOrSocketed);
        }
    }
}
