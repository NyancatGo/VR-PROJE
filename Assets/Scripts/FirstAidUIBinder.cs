using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Extracts data from NPCInjuryData and binds it to the UI Canvas text fields.
/// Handles the world-space first-aid panel interactions for Module 2.
/// </summary>
public class FirstAidUIBinder : MonoBehaviour
{
    private static readonly string[] FallbackActionPool =
    {
        "Turnike uygula",
        "Basincli pansuman uygula",
        "Kirigi sabitle",
        "Hastayi yan pozisyona al",
        "Solunumu yeniden degerlendir",
        "Battaniye ile isi kaybini azalt"
    };

    private static readonly Color DefaultStatusColor = new Color(1f, 0.82f, 0.24f, 1f);
    private static readonly Color SuccessStatusColor = new Color(0.35f, 0.9f, 0.54f, 1f);
    private static readonly Color ErrorStatusColor = new Color(1f, 0.45f, 0.45f, 1f);
    private static readonly Color ActionButtonColor = new Color(0.13f, 0.53f, 0.74f, 0.95f);
    private static readonly Color SuccessButtonColor = new Color(0.16f, 0.67f, 0.38f, 0.98f);
    private static readonly Color WrongButtonColor = new Color(0.75f, 0.25f, 0.25f, 0.98f);
    private static readonly Color CloseButtonColor = new Color(0.22f, 0.25f, 0.31f, 0.98f);

    [Header("UI Text References")]
    public TextMeshProUGUI injuryNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI treatmentText;
    public TextMeshProUGUI statusText;

    [Header("Buttons")]
    public Button treatmentButton;
    public Transform actionButtonRoot;
    public Button closeButton;

    [Header("Data Source")]
    public NPCInjuryData npcData;

    private readonly List<Button> runtimeActionButtons = new List<Button>();
    private readonly Dictionary<Button, string> buttonLabels = new Dictionary<Button, string>();

    private bool isTreatmentCompleted;
    private string selectedAction = string.Empty;
    private string feedbackMessage = string.Empty;
    private bool lastSelectionWasCorrect;

    private void Awake()
    {
        ResolveButtonReferences();
        EnsureCloseButton();
    }

    /// <summary>
    /// Updates the text elements with the NPC's specific injury data.
    /// Called right before the canvas becomes visible.
    /// </summary>
    public void UpdateUI()
    {
        if (npcData == null)
        {
            Debug.LogWarning("FirstAidUIBinder: NPCInjuryData is not assigned.");
            return;
        }

        ResolveButtonReferences();
        EnsureCloseButton();
        RebuildActionButtons();
        RefreshTexts();
        RefreshActionButtons();
    }

    private void ResolveButtonReferences()
    {
        if (actionButtonRoot == null && treatmentButton != null)
        {
            actionButtonRoot = treatmentButton.transform.parent;
        }

        if (treatmentButton != null)
        {
            treatmentButton.gameObject.SetActive(false);
        }
    }

    private void EnsureCloseButton()
    {
        if (closeButton == null)
        {
            closeButton = CreateButton("Paneli Kapat", CloseButtonColor);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
            closeButton.transform.SetAsLastSibling();
        }
    }

    private void RebuildActionButtons()
    {
        for (int i = 0; i < runtimeActionButtons.Count; i++)
        {
            if (runtimeActionButtons[i] != null)
            {
                Destroy(runtimeActionButtons[i].gameObject);
            }
        }

        runtimeActionButtons.Clear();
        buttonLabels.Clear();

        string[] options = BuildActionOptions();
        for (int i = 0; i < options.Length; i++)
        {
            Button actionButton = CreateButton(options[i], ActionButtonColor);
            if (actionButton == null)
            {
                continue;
            }

            string cachedLabel = options[i];
            actionButton.onClick.AddListener(() => OnActionSelected(cachedLabel));

            runtimeActionButtons.Add(actionButton);
            buttonLabels[actionButton] = cachedLabel;
        }

        if (closeButton != null)
        {
            closeButton.transform.SetAsLastSibling();
        }
    }

    private string[] BuildActionOptions()
    {
        List<string> options = new List<string>();
        string correctAction = npcData != null ? npcData.ResolvedCorrectAction : string.Empty;

        if (npcData != null && npcData.actionOptions != null)
        {
            for (int i = 0; i < npcData.actionOptions.Length; i++)
            {
                AddUniqueOption(options, npcData.actionOptions[i]);
            }
        }

        AddUniqueOption(options, correctAction);

        for (int i = 0; i < FallbackActionPool.Length && options.Count < 4; i++)
        {
            AddUniqueOption(options, FallbackActionPool[i]);
        }

        if (options.Count == 0)
        {
            AddUniqueOption(options, "Durumu degerlendir");
            AddUniqueOption(options, "Profesyonel destek cagir");
        }

        int stableSeed = (npcData != null ? npcData.ResolvedPatientName : gameObject.name).GetHashCode() & int.MaxValue;
        int rotation = options.Count > 0 ? stableSeed % options.Count : 0;

        if (rotation > 0)
        {
            List<string> rotated = new List<string>(options.Count);
            for (int i = 0; i < options.Count; i++)
            {
                rotated.Add(options[(i + rotation) % options.Count]);
            }

            options = rotated;
        }

        return options.ToArray();
    }

    private static void AddUniqueOption(List<string> options, string candidate)
    {
        if (options == null || string.IsNullOrWhiteSpace(candidate))
        {
            return;
        }

        string trimmedCandidate = candidate.Trim();
        for (int i = 0; i < options.Count; i++)
        {
            if (string.Equals(options[i], trimmedCandidate, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        options.Add(trimmedCandidate);
    }

    private Button CreateButton(string label, Color backgroundColor)
    {
        Transform parent = actionButtonRoot != null ? actionButtonRoot : transform;
        if (parent == null)
        {
            return null;
        }

        Button button;
        if (treatmentButton != null)
        {
            GameObject clone = Instantiate(treatmentButton.gameObject, parent);
            clone.name = $"{label}_Button";
            clone.SetActive(true);
            button = clone.GetComponent<Button>();
        }
        else
        {
            GameObject buttonObject = new GameObject(
                $"{label}_Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));

            buttonObject.transform.SetParent(parent, false);
            button = buttonObject.GetComponent<Button>();

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.minHeight = 58f;
            layout.preferredHeight = 58f;
        }

        if (button == null)
        {
            return null;
        }

        button.onClick.RemoveAllListeners();
        button.interactable = true;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = backgroundColor;
            image.raycastTarget = true;
        }

        TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (labelText == null)
        {
            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(button.transform, false);
            labelText = textObject.GetComponent<TextMeshProUGUI>();

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 8f);
            textRect.offsetMax = new Vector2(-14f, -8f);
        }

        labelText.text = label;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.enableWordWrapping = true;
        labelText.raycastTarget = false;

        return button;
    }

    private void OnActionSelected(string actionLabel)
    {
        if (npcData == null || isTreatmentCompleted)
        {
            return;
        }

        selectedAction = actionLabel;
        lastSelectionWasCorrect = string.Equals(
            actionLabel.Trim(),
            npcData.ResolvedCorrectAction,
            StringComparison.OrdinalIgnoreCase);

        if (lastSelectionWasCorrect)
        {
            isTreatmentCompleted = true;
            npcData.currentStatus = npcData.ResolvedSuccessStatus;
            feedbackMessage = npcData.ResolvedSuccessFeedback;
        }
        else
        {
            feedbackMessage = npcData.ResolvedWrongFeedback;
        }

        RefreshTexts();
        RefreshActionButtons();
    }

    private void RefreshTexts()
    {
        if (npcData == null)
        {
            return;
        }

        if (injuryNameText != null)
        {
            injuryNameText.text = $"{npcData.ResolvedPatientName}\n<size=82%>{npcData.injuryName}</size>";
        }

        if (descriptionText != null)
        {
            descriptionText.text = npcData.description;
        }

        if (treatmentText != null)
        {
            if (string.IsNullOrWhiteSpace(selectedAction))
            {
                treatmentText.text = $"Mudahale Secimi: {npcData.ResolvedActionPrompt}";
            }
            else
            {
                treatmentText.text = $"Secilen Islem: {selectedAction}\nGeri Bildirim: {feedbackMessage}";
            }
        }

        if (statusText != null)
        {
            string displayedStatus = npcData.currentStatus;
            Color displayedColor = DefaultStatusColor;

            if (isTreatmentCompleted)
            {
                displayedStatus = npcData.ResolvedSuccessStatus;
                displayedColor = SuccessStatusColor;
            }
            else if (!string.IsNullOrWhiteSpace(selectedAction))
            {
                displayedStatus = npcData.ResolvedWrongStatus;
                displayedColor = ErrorStatusColor;
            }

            statusText.text = "Durum: " + displayedStatus;
            statusText.color = displayedColor;
        }
    }

    private void RefreshActionButtons()
    {
        for (int i = 0; i < runtimeActionButtons.Count; i++)
        {
            Button button = runtimeActionButtons[i];
            if (button == null)
            {
                continue;
            }

            string label;
            if (!buttonLabels.TryGetValue(button, out label))
            {
                label = string.Empty;
            }

            bool isThisSelected = !string.IsNullOrWhiteSpace(selectedAction)
                && string.Equals(label, selectedAction, StringComparison.OrdinalIgnoreCase);

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                if (isTreatmentCompleted && string.Equals(label, npcData.ResolvedCorrectAction, StringComparison.OrdinalIgnoreCase))
                {
                    image.color = SuccessButtonColor;
                }
                else if (isThisSelected && !lastSelectionWasCorrect)
                {
                    image.color = WrongButtonColor;
                }
                else
                {
                    image.color = ActionButtonColor;
                }
            }

            button.interactable = !isTreatmentCompleted;
        }
    }

    private void ClosePanel()
    {
        NPCWorldCanvas worldCanvas = GetComponent<NPCWorldCanvas>();
        if (worldCanvas != null)
        {
            worldCanvas.HideCanvas();
        }
    }
}
