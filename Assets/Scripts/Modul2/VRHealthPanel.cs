using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VRHealthPanel : MonoBehaviour
{
    [Header("Panel Referansları")]
    public GameObject panelRoot;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI heartRateText;
    public TextMeshProUGUI respirationText;
    public TextMeshProUGUI bloodPressureText;
    public TextMeshProUGUI consciousnessText;
    public TextMeshProUGUI questionText;
    public Button[] optionButtons;
    public TextMeshProUGUI[] optionTexts;
    public TextMeshProUGUI feedbackText;
    public Image feedbackBackground;
    public Color correctColor = new Color(0.22f, 0.55f, 0.09f);
    public Color wrongColor   = new Color(0.64f, 0.18f, 0.18f);

    private NPCHealthData _currentData;
    private bool _answered;

    public void OpenForNPC(WoundedNPC npc)
    {
        _currentData = npc.GetHealthData();
        _answered = false;

        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        if (feedbackBackground != null) feedbackBackground.gameObject.SetActive(false);

        if (npcNameText != null)       npcNameText.text       = _currentData.npcName;
        if (statusText != null)        statusText.text        = _currentData.statusLabel + "\n" + _currentData.statusSummary;
        if (heartRateText != null)     heartRateText.text     = "Nabız: " + _currentData.heartRate;
        if (respirationText != null)   respirationText.text   = "Solunum: " + _currentData.respirationRate;
        if (bloodPressureText != null) bloodPressureText.text = "Tansiyon: " + _currentData.bloodPressure;
        if (consciousnessText != null) consciousnessText.text = "Bilinç: " + _currentData.consciousnessLevel;
        if (questionText != null)      questionText.text      = _currentData.question;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int idx = i;
            if (i < _currentData.options.Length && optionTexts[i] != null)
                optionTexts[i].text = _currentData.options[i].optionText;

            if (optionButtons[i] != null)
            {
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(idx));
                optionButtons[i].image.color = Color.white;
            }
        }

        if (panelRoot != null) 
        {
            panelRoot.SetActive(true);
            
            // --- VR ETKİLEŞİM GÜVENCESİ ---
            Canvas canvas = panelRoot.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                // World Space olduğundan emin ol
                canvas.renderMode = RenderMode.WorldSpace;
                
                // Kamera ata
                canvas.worldCamera = XRCameraHelper.GetPlayerCamera();

                // Raycaster kontrolü
                var raycaster = canvas.GetComponent("TrackedDeviceGraphicRaycaster");
                if (raycaster == null)
                {
                    System.Type raycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
                    if (raycasterType != null)
                    {
                        canvas.gameObject.AddComponent(raycasterType);
                    }
                }
            }
        }
    }

    private void OnOptionSelected(int idx)
    {
        if (_answered) return;
        _answered = true;

        bool correct = idx < _currentData.options.Length && _currentData.options[idx].isCorrect;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] == null) continue;
            if (i < _currentData.options.Length && _currentData.options[i].isCorrect)
                optionButtons[i].image.color = correctColor;
            else if (i == idx)
                optionButtons[i].image.color = wrongColor;
        }

        if (feedbackText != null)
        {
            feedbackText.text = correct ? _currentData.feedbackCorrect : _currentData.feedbackWrong;
            feedbackText.gameObject.SetActive(true);
        }
        if (feedbackBackground != null)
        {
            feedbackBackground.color = correct
                ? new Color(correctColor.r, correctColor.g, correctColor.b, 0.15f)
                : new Color(wrongColor.r,   wrongColor.g,   wrongColor.b,   0.15f);
            feedbackBackground.gameObject.SetActive(true);
        }
    }

    public void ClosePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }
}
