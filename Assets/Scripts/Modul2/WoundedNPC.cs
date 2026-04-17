using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[System.Serializable]
public class QuizOption
{
    public string optionText;
    public bool isCorrect;
}

[System.Serializable]
public class NPCHealthData
{
    [Header("Kimlik")]
    public string npcName;
    public string statusLabel;
    [TextArea] public string statusSummary;

    [Header("Vital Bulgular")]
    public string heartRate;
    public string respirationRate;
    public string bloodPressure;
    public string consciousnessLevel;

    [Header("Quiz")]
    [TextArea] public string question;
    public QuizOption[] options = new QuizOption[4];
    [TextArea] public string feedbackCorrect;
    [TextArea] public string feedbackWrong;
}

public class WoundedNPC : MonoBehaviour
{
    public NPCHealthData healthData;

    private XRSimpleInteractable _simpleInteractable;

    private void Awake()
    {
        _simpleInteractable = GetComponent<XRSimpleInteractable>();
        if (_simpleInteractable != null)
        {
            _simpleInteractable.selectEntered.AddListener(OnVRSelect);
        }
    }

    private void OnDestroy()
    {
        if (_simpleInteractable != null)
            _simpleInteractable.selectEntered.RemoveListener(OnVRSelect);
    }

    public void OnVRSelect(SelectEnterEventArgs args)
    {
        var panel = FindObjectOfType<VRHealthPanel>(true);
        if (panel != null)
        {
            panel.OpenForNPC(this);
        }
        else
        {
            Debug.LogWarning("[WoundedNPC] VRHealthPanel bulunamadi!");
        }
    }

    public NPCHealthData GetHealthData() => healthData;
}
