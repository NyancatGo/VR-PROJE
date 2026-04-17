using System;
using UnityEngine;

[Serializable]
public class TriageCaseProfile
{
    private static readonly Color DefaultAccentColor = new Color(0.36f, 0.84f, 1f, 0.92f);

    public string caseId = string.Empty;
    public string caseName = string.Empty;
    public string patientTitle = string.Empty;
    public string tone = string.Empty;
    [TextArea(3, 6)] public string complaintText = string.Empty;
    [TextArea(2, 5)] public string criticalObservation = string.Empty;
    [TextArea(2, 5)] public string suspectedCondition = string.Empty;
    [TextArea(2, 5)] public string initialChecks = string.Empty;
    [TextArea(2, 5)] public string triageHint = string.Empty;
    public TriageCategory actualCategory = TriageCategory.Unassigned;
    public Color accentColor = new Color(0.36f, 0.84f, 1f, 0.92f);

    public string CaseIdOrFallback => string.IsNullOrWhiteSpace(caseId) ? "vaka" : caseId.Trim();
    public string CaseNameOrFallback => string.IsNullOrWhiteSpace(caseName) ? "Saha vakasi" : caseName.Trim();
    public string PatientTitleOrFallback => string.IsNullOrWhiteSpace(patientTitle) ? CaseNameOrFallback : patientTitle.Trim();
    public string ToneOrFallback => string.IsNullOrWhiteSpace(tone) ? "Sahada sakin ama dikkatli ilerle." : tone.Trim();
    public string ComplaintOrFallback => string.IsNullOrWhiteSpace(complaintText) ? "Sikayet bilgisi bulunamadi." : complaintText.Trim();
    public string CriticalObservationOrFallback => string.IsNullOrWhiteSpace(criticalObservation) ? "Hayati riski belirleyen bulguyu hizla yeniden tara." : criticalObservation.Trim();
    public string SuspectedConditionOrFallback => string.IsNullOrWhiteSpace(suspectedCondition) ? "ABC bozulmasi veya gizli kotulesme ihtimali var." : suspectedCondition.Trim();
    public string InitialChecksOrFallback => string.IsNullOrWhiteSpace(initialChecks) ? "Hava yolu, solunum, dolasim ve bilinci birlikte kontrol et." : initialChecks.Trim();
    public string TriageHintOrFallback => string.IsNullOrWhiteSpace(triageHint) ? "Karari hastanin bekleyip bekleyemeyecegini belirleyen bulguya gore ver." : triageHint.Trim();
    public Color AccentColorOrFallback => accentColor.a <= 0f ? DefaultAccentColor : accentColor;

    public TriageCaseProfile Clone()
    {
        return new TriageCaseProfile
        {
            caseId = caseId,
            caseName = caseName,
            patientTitle = patientTitle,
            tone = tone,
            complaintText = complaintText,
            criticalObservation = criticalObservation,
            suspectedCondition = suspectedCondition,
            initialChecks = initialChecks,
            triageHint = triageHint,
            actualCategory = actualCategory,
            accentColor = accentColor
        };
    }
}
