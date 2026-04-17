using UnityEngine;

/// <summary>
/// NPC icin yaralanma bilgilerini tutar.
/// Unity Editor uzerinden nesne bazinda ozellestirilebilir.
/// </summary>
public class NPCInjuryData : MonoBehaviour
{
    [Header("Patient Information")]
    public string patientName = "Depremzede";
    [Header("Yaralanma Bilgileri")]
    public string injuryName = "Derin Kesik";
    
    [TextArea(3, 5)]
    public string description = "Hastanın sağ ön kolunda derin bir kesik var, kanama devam ediyor.";
    
    public string requiredTreatment = "Baskı uygulayın ve turnike kullanın.";
    
    public string currentStatus = "Aktif Kanama";

    [Header("Action Panel")]
    [TextArea(2, 4)]
    public string actionPrompt = "Bu yarali icin once uygulanmasi gereken mudahaleyi secin.";

    public string correctAction = string.Empty;

    public string[] actionOptions = new string[0];

    public string successStatus = "Mudahale planlandi";

    [TextArea(2, 3)]
    public string successFeedback = "Dogru mudahale secildi. Yarali artik izlem altina alinabilir.";

    public string wrongStatus = "Mudahale yeniden degerlendirilmeli";

    [TextArea(2, 3)]
    public string wrongFeedback = "Bu yarali icin daha uygun bir mudahale secilmelidir.";

    public string ResolvedPatientName
    {
        get
        {
            return string.IsNullOrWhiteSpace(patientName) ? "Depremzede" : patientName.Trim();
        }
    }

    public string ResolvedRequiredTreatment
    {
        get
        {
            return string.IsNullOrWhiteSpace(requiredTreatment)
                ? "Uygun mudahale belirlenmeli."
                : requiredTreatment.Trim();
        }
    }

    public string ResolvedActionPrompt
    {
        get
        {
            return string.IsNullOrWhiteSpace(actionPrompt)
                ? "Bu yarali icin once uygulanmasi gereken mudahaleyi secin."
                : actionPrompt.Trim();
        }
    }

    public string ResolvedCorrectAction
    {
        get
        {
            return string.IsNullOrWhiteSpace(correctAction)
                ? ResolvedRequiredTreatment
                : correctAction.Trim();
        }
    }

    public string ResolvedSuccessStatus
    {
        get
        {
            return string.IsNullOrWhiteSpace(successStatus)
                ? "Mudahale planlandi"
                : successStatus.Trim();
        }
    }

    public string ResolvedWrongStatus
    {
        get
        {
            return string.IsNullOrWhiteSpace(wrongStatus)
                ? currentStatus
                : wrongStatus.Trim();
        }
    }

    public string ResolvedSuccessFeedback
    {
        get
        {
            return string.IsNullOrWhiteSpace(successFeedback)
                ? "Dogru mudahale secildi."
                : successFeedback.Trim();
        }
    }

    public string ResolvedWrongFeedback
    {
        get
        {
            return string.IsNullOrWhiteSpace(wrongFeedback)
                ? "Bu yarali icin daha uygun bir mudahale secilmelidir."
                : wrongFeedback.Trim();
        }
    }
}
