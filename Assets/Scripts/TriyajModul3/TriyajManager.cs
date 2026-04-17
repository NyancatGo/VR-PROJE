using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TriyajManager : MonoBehaviour
{
    public static TriyajManager Instance;

    [Header("Triyaj İstatistikleri")]
    public int dogruTriyajSayisi = 0;
    public int yanlisTriyajSayisi = 0;

    [Header("UI (Opsiyonel)")]
    public TextMeshProUGUI triyajSkorText;

    [Header("UI Feedback")]
    public TextMeshProUGUI feedbackText;

    [Header("Runtime Guard")]
    [SerializeField] private bool autoResolveUiReferences = true;

    [HideInInspector]
    public YaraliController hoveredYarali_VR;

    private Camera _playerCamera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttach()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        string sceneName = activeScene.name.ToLowerInvariant();
        bool isTriyajScene = sceneName.Contains("triyaj") || sceneName.Contains("modul3");
        if (!isTriyajScene)
        {
            return;
        }

        if (FindObjectOfType<TriyajManager>() == null)
        {
            var obj = new GameObject("TriyajManager_AutoAttach");
            obj.AddComponent<TriyajManager>();
            Debug.Log("TriyajManager sahneye otomatik eklendi.");
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            ResolveUiReferencesIfNeeded();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ResolveUiReferencesIfNeeded();
        UpdateUI();
    }

    private void Update()
    {
        CheckTriageInput();
    }

    private void CheckTriageInput()
    {
        TriageCategory? selectedCat = null;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) selectedCat = TriageCategory.Green;
            if (Keyboard.current.digit2Key.wasPressedThisFrame) selectedCat = TriageCategory.Yellow;
            if (Keyboard.current.digit3Key.wasPressedThisFrame) selectedCat = TriageCategory.Red;
            if (Keyboard.current.digit4Key.wasPressedThisFrame) selectedCat = TriageCategory.Black;
        }
#else
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedCat = TriageCategory.Green;
        if (Input.GetKeyDown(KeyCode.Alpha2)) selectedCat = TriageCategory.Yellow;
        if (Input.GetKeyDown(KeyCode.Alpha3)) selectedCat = TriageCategory.Red;
        if (Input.GetKeyDown(KeyCode.Alpha4)) selectedCat = TriageCategory.Black;
#endif

        if (selectedCat.HasValue)
        {
            var hedefYarali = GetYaraliFromCrosshair();
            if (hedefYarali != null)
            {
                ApplyTriage(hedefYarali, selectedCat.Value);
            }
            else
            {
                ShowFeedback("Triyaj atamak icin yaraliya bakmalisiniz!", Color.white);
            }
        }
    }

    public void SetHoveredYarali(YaraliController y)
    {
        hoveredYarali_VR = y;
    }

    public void ClearHoveredYarali(YaraliController y)
    {
        if (hoveredYarali_VR == y) hoveredYarali_VR = null;
    }

    private YaraliController GetYaraliFromCrosshair()
    {
        if (hoveredYarali_VR != null)
            return hoveredYarali_VR;

        if (_playerCamera == null)
            _playerCamera = XRCameraHelper.GetPlayerCamera();

        if (_playerCamera == null)
            return null;

        Vector2 mousePos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            mousePos = Mouse.current.position.ReadValue();
#else
        mousePos = Input.mousePosition;
#endif

        Ray ray = _playerCamera.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, 10f))
        {
            return hit.collider.GetComponentInParent<YaraliController>();
        }

        return null;
    }

    public void ApplyTriage(YaraliController yarali, TriageCategory category)
    {
        if (yarali.isTriaged)
        {
            ShowFeedback("Bu yaraliya zaten " + yarali.AssignedTriageState + " dendi!", Color.yellow);
            return;
        }

        yarali.AssignTriage(category);

        if (yarali.ActualTriageState == category)
        {
            dogruTriyajSayisi++;
            ShowFeedback("Dogru Triyaj (" + category + ")", Color.green);
        }
        else
        {
            yanlisTriyajSayisi++;
            ShowFeedback("Yanlis Triyaj! (Gercekte: " + yarali.ActualTriageState + ", Senin Secimin: " + category + ")", Color.red);
        }

        UpdateUI();
    }

    public void ApplyTriageFromHospital(MonoBehaviour npc, TriageCategory verilen, TriageCategory dogru)
    {
        if (npc == null)
        {
            return;
        }

        bool isCorrect = verilen == dogru;
        if (isCorrect)
        {
            dogruTriyajSayisi++;
            ShowFeedback("Dogru Triyaj (" + verilen + ")", Color.green);
        }
        else
        {
            yanlisTriyajSayisi++;
            ShowFeedback("Yanlis Triyaj! (Gercekte: " + dogru + ", Senin Secimin: " + verilen + ")", Color.red);
        }

        HospitalTriageManager.Instance?.RegisterTriage(npc, verilen, dogru);
        UpdateUI();
    }

    public void ResetHospitalStats()
    {
        dogruTriyajSayisi = 0;
        yanlisTriyajSayisi = 0;
        hoveredYarali_VR = null;
        CancelInvoke(nameof(ClearFeedback));
        ClearFeedback();
        UpdateUI();
    }

    private void UpdateUI()
    {
        ResolveUiReferencesIfNeeded();
        if (triyajSkorText != null)
        {
            triyajSkorText.text = $"Dogru: <color=#00FF00>{dogruTriyajSayisi}</color> | Yanlis: <color=#FF0000>{yanlisTriyajSayisi}</color>";
        }
    }

    private void ShowFeedback(string msg, Color clr)
    {
        ResolveUiReferencesIfNeeded();
        Debug.Log("[Triyaj Feedback] " + msg);
        if (feedbackText != null)
        {
            feedbackText.color = clr;
            feedbackText.text = msg;
            CancelInvoke(nameof(ClearFeedback));
            Invoke(nameof(ClearFeedback), 3f);
        }
    }

    private void ClearFeedback()
    {
        if (feedbackText != null) feedbackText.text = "";
    }

    private void ResolveUiReferencesIfNeeded()
    {
        if (!autoResolveUiReferences)
        {
            return;
        }

        if (triyajSkorText != null && feedbackText != null)
        {
            return;
        }

        TextMeshProUGUI[] candidates = FindObjectsOfType<TextMeshProUGUI>(true);
        for (int i = 0; i < candidates.Length; i++)
        {
            TextMeshProUGUI text = candidates[i];
            if (text == null)
            {
                continue;
            }

            string nameLower = text.name.ToLowerInvariant();
            if (triyajSkorText == null &&
                (nameLower.Contains("skor") || nameLower.Contains("score") || nameLower.Contains("puan")))
            {
                triyajSkorText = text;
                continue;
            }

            if (feedbackText == null &&
                (nameLower.Contains("feedback") || nameLower.Contains("geri") || nameLower.Contains("mesaj")))
            {
                feedbackText = text;
            }
        }
    }

}
