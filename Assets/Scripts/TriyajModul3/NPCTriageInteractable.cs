using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TrainingAnalytics;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Collider))]
public class NPCTriageInteractable : MonoBehaviour
{
    private const string DefaultComplaintText = "Basim cok agriyor, nefes almakta zorlaniyorum.";
    private const string DefaultCaseTitle = "Saha vakasi";
    private const string DefaultFieldTone = "Sahada sakin ama dikkatli ilerle.";
    private static readonly Color DefaultAccentColor = new Color(0.36f, 0.84f, 1f, 0.92f);

    [Header("Triyaj Ayari")]
    [SerializeField] private TriageCategory actualCategory = TriageCategory.Red;
    [SerializeField] [TextArea(3, 6)] private string complaintText = DefaultComplaintText;
    [SerializeField] private string caseId = string.Empty;
    [SerializeField] private string patientTitle = DefaultCaseTitle;
    [SerializeField] [TextArea(2, 4)] private string fieldTone = DefaultFieldTone;
    [SerializeField] private Color accentColor = new Color(0.36f, 0.84f, 1f, 0.92f);
    [SerializeField] private TriageDialogUI triageButtonsCanvas;
    [SerializeField] private bool lockAfterTriage = true;

    [Header("Etkilesim")]
    [SerializeField] private float raycastDistance = 15f;
    [SerializeField] private LayerMask npcRaycastLayerMask;

    private XRSimpleInteractable xrInteractable;
    private Camera playerCamera;
    private bool isTriaged;
    private bool isDialogOpen;
    private TriageCaseProfile currentCaseProfile;

    public bool IsTriaged => isTriaged;
    public bool IsDialogOpen => isDialogOpen;
    public TriageCategory ActualCategory => actualCategory;
    public string ComplaintText => complaintText;
    public string CaseId => string.IsNullOrWhiteSpace(caseId) ? name : caseId.Trim();
    public string PatientTitle => string.IsNullOrWhiteSpace(patientTitle) ? DefaultCaseTitle : patientTitle.Trim();
    public string FieldTone => string.IsNullOrWhiteSpace(fieldTone) ? DefaultFieldTone : fieldTone.Trim();
    public Color AccentColor => accentColor.a <= 0f ? DefaultAccentColor : accentColor;
    public TriageCaseProfile CaseProfile => currentCaseProfile != null ? currentCaseProfile.Clone() : BuildFallbackCaseProfile();

    private void Awake()
    {
        xrInteractable = GetComponent<XRSimpleInteractable>();
        if (xrInteractable == null)
        {
            xrInteractable = gameObject.AddComponent<XRSimpleInteractable>();
        }

        EnsureCaseProfileInitialized();
    }

    private void OnEnable()
    {
        if (xrInteractable != null)
        {
            xrInteractable.selectEntered.AddListener(OnXRSelectEntered);
        }

        HospitalTriageManager.Instance?.RegisterNpc(this);
    }

    private void OnDisable()
    {
        if (xrInteractable != null)
        {
            xrInteractable.selectEntered.RemoveListener(OnXRSelectEntered);
        }

        HospitalTriageManager.Instance?.UnregisterNpc(this);
    }

    private void Update()
    {
        HandleOpenInput();
        HandleKeyboardTriageInput();
    }

    public void ConfigureScenario(string scenarioComplaintText, TriageCategory scenarioActualCategory)
    {
        complaintText = string.IsNullOrWhiteSpace(scenarioComplaintText)
            ? DefaultComplaintText
            : scenarioComplaintText.Trim();
        actualCategory = scenarioActualCategory;
        currentCaseProfile = BuildFallbackCaseProfile();

        if (isDialogOpen && triageButtonsCanvas != null)
        {
            triageButtonsCanvas.Open(this, SubmitTriage);
        }
    }

    public void ApplyCaseProfile(TriageCaseProfile caseProfile)
    {
        if (caseProfile == null)
        {
            return;
        }

        currentCaseProfile = caseProfile.Clone();
        caseId = currentCaseProfile.CaseIdOrFallback;
        patientTitle = currentCaseProfile.PatientTitleOrFallback;
        fieldTone = currentCaseProfile.ToneOrFallback;
        complaintText = currentCaseProfile.ComplaintOrFallback;
        actualCategory = currentCaseProfile.actualCategory;
        accentColor = currentCaseProfile.AccentColorOrFallback;

        if (isDialogOpen && triageButtonsCanvas != null)
        {
            triageButtonsCanvas.Open(this, SubmitTriage);
        }
    }

    public void OpenDialog()
    {
        if (isTriaged && lockAfterTriage)
        {
            return;
        }

        HospitalTriageManager hospitalManager = HospitalTriageManager.Instance;
        if (hospitalManager != null && !hospitalManager.TrySetActiveNpc(this))
        {
            return;
        }

        if (triageButtonsCanvas == null)
        {
            isDialogOpen = false;
            HospitalTriageManager.Instance?.ClearActiveNpc(this);
            Debug.LogWarning("[NPCTriageInteractable] " + name + " icin TriageDialogUI atanmamis.");
            return;
        }

        EnsureCaseProfileInitialized();
        triageButtonsCanvas.Open(this, SubmitTriage);
        isDialogOpen = true;

        TrainingAnalyticsFacade.OnVictimInteracted(
            TrainingAnalyticsFacade.Module3Id,
            TrainingAnalyticsFacade.Module3Name,
            CaseId,
            PatientTitle,
            new System.Collections.Generic.Dictionary<string, object>
            {
                { AnalyticsParams.ScenarioId, TrainingAnalyticsFacade.Module3ScenarioId },
                { AnalyticsParams.ScenarioName, TrainingAnalyticsFacade.Module3ScenarioName }
            });
    }

    public void CloseDialog()
    {
        isDialogOpen = false;

        if (triageButtonsCanvas != null)
        {
            triageButtonsCanvas.Close();
        }

        HospitalTriageManager.Instance?.ClearActiveNpc(this);
    }

    public void SubmitTriage(TriageCategory selectedCategory)
    {
        if (isTriaged && lockAfterTriage)
        {
            return;
        }

        bool triageCommitted = false;

        try
        {
            TriyajManager manager = TriyajManager.Instance;
            if (manager == null)
            {
                manager = FindObjectOfType<TriyajManager>();
            }

            if (manager != null)
            {
                manager.ApplyTriageFromHospital(this, selectedCategory, actualCategory);
                triageCommitted = true;
            }
            else
            {
                HospitalTriageManager hospitalManager = HospitalTriageManager.Instance;
                if (hospitalManager != null)
                {
                    hospitalManager.RegisterTriage(selectedCategory, actualCategory);
                    triageCommitted = true;
                    Debug.Log("[NPCTriageInteractable] Triyaj atandi: " + selectedCategory + " | Dogru: " + (selectedCategory == actualCategory));
                }
                else
                {
                    Debug.LogWarning("[NPCTriageInteractable] " + name + " icin HospitalTriageManager bulunamadi, triage commit edilemedi.");
                }
            }

            if (triageCommitted)
            {
                isTriaged = true;

                TrainingAnalyticsFacade.OnVictimTagged(
                    TrainingAnalyticsFacade.Module3Id,
                    TrainingAnalyticsFacade.Module3Name,
                    CaseId,
                    PatientTitle,
                    selectedCategory,
                    actualCategory,
                    new System.Collections.Generic.Dictionary<string, object>
                    {
                        { AnalyticsParams.ScenarioId, TrainingAnalyticsFacade.Module3ScenarioId },
                        { AnalyticsParams.ScenarioName, TrainingAnalyticsFacade.Module3ScenarioName }
                    });
            }
        }
        finally
        {
            CloseDialog();

            if (triageCommitted && lockAfterTriage && xrInteractable != null)
            {
                xrInteractable.enabled = false;
            }
        }
    }

    public void ResetTriageState()
    {
        CloseDialog();
        isTriaged = false;
        EnsureCaseProfileInitialized();

        if (xrInteractable == null)
        {
            xrInteractable = GetComponent<XRSimpleInteractable>();
            if (xrInteractable == null)
            {
                xrInteractable = gameObject.AddComponent<XRSimpleInteractable>();
            }
        }

        xrInteractable.enabled = true;
    }

    private void HandleOpenInput()
    {
        if (isTriaged || isDialogOpen)
        {
            return;
        }

        bool clicked = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            clicked = true;
        }

        if (!clicked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            clicked = true;
        }
#else
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
        {
            clicked = true;
        }
#endif

        if (!clicked || !TryInteract())
        {
            return;
        }

        OpenDialog();
    }

    private void HandleKeyboardTriageInput()
    {
        if (!isDialogOpen || isTriaged)
        {
            return;
        }

        TriageCategory? selected = null;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) selected = TriageCategory.Green;
            if (Keyboard.current.digit2Key.wasPressedThisFrame) selected = TriageCategory.Yellow;
            if (Keyboard.current.digit3Key.wasPressedThisFrame) selected = TriageCategory.Red;
            if (Keyboard.current.digit4Key.wasPressedThisFrame) selected = TriageCategory.Black;
        }
#else
        if (Input.GetKeyDown(KeyCode.Alpha1)) selected = TriageCategory.Green;
        if (Input.GetKeyDown(KeyCode.Alpha2)) selected = TriageCategory.Yellow;
        if (Input.GetKeyDown(KeyCode.Alpha3)) selected = TriageCategory.Red;
        if (Input.GetKeyDown(KeyCode.Alpha4)) selected = TriageCategory.Black;
#endif

        if (selected.HasValue)
        {
            SubmitTriage(selected.Value);
        }
    }

    private void OnXRSelectEntered(SelectEnterEventArgs args)
    {
        OpenDialog();
    }

    private bool TryInteract()
    {
        if (playerCamera == null)
        {
            playerCamera = XRCameraHelper.GetPlayerCamera();
        }

        if (playerCamera == null)
        {
            return false;
        }

        Vector2 mousePosition = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            mousePosition = Mouse.current.position.ReadValue();
        }
#else
        mousePosition = Input.mousePosition;
#endif

        Ray rayMouse = playerCamera.ScreenPointToRay(mousePosition);
        if (!Physics.Raycast(
                rayMouse,
                out RaycastHit hit,
                raycastDistance,
                ResolveNpcRaycastMask(),
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        Transform hitTransform = hit.transform;
        return hitTransform == transform || hitTransform.IsChildOf(transform);
    }

    private int ResolveNpcRaycastMask()
    {
        if (npcRaycastLayerMask.value != 0)
        {
            return npcRaycastLayerMask.value;
        }

        int npcLayer = LayerMask.NameToLayer("NPC");
        if (npcLayer >= 0)
        {
            return 1 << npcLayer;
        }

        return 1 << gameObject.layer;
    }

    private void EnsureCaseProfileInitialized()
    {
        if (currentCaseProfile != null)
        {
            return;
        }

        currentCaseProfile = BuildFallbackCaseProfile();
    }

    private TriageCaseProfile BuildFallbackCaseProfile()
    {
        return new TriageCaseProfile
        {
            caseId = CaseId,
            caseName = PatientTitle,
            patientTitle = PatientTitle,
            tone = FieldTone,
            complaintText = string.IsNullOrWhiteSpace(complaintText) ? DefaultComplaintText : complaintText.Trim(),
            criticalObservation = "Karari hizla degistirecek kritik bulguyu yeniden tara.",
            suspectedCondition = "Hayati risk tablosu veya gizli kotulesme ihtimali dislanmali.",
            initialChecks = "Hava yolu, solunum, dolasim ve bilinci birlikte kontrol et.",
            triageHint = "Karari hastanin bekleyip bekleyemeyecegine gore ver.",
            actualCategory = actualCategory,
            accentColor = AccentColor
        };
    }
}
