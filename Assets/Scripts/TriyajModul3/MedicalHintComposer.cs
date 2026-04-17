using System;
using System.Text;

public static class MedicalHintComposer
{
    private const int MaxClauseLength = 68;

    public static string Compose(TriageCaseProfile profile, int requestIndex = 0)
    {
        if (profile == null)
        {
            return ComposeFallback(string.Empty, TriageCategory.Unassigned);
        }

        int variant = Math.Abs(requestIndex) % 3;
        string likely = TakeLeadClause(profile.SuspectedConditionOrFallback, MaxClauseLength);
        string threshold = SelectThreshold(profile, variant);
        string check = SelectCheck(profile, variant);
        string priority = SelectPriority(profile, variant);

        if (string.IsNullOrWhiteSpace(likely))
        {
            likely = DefaultLikely(profile.actualCategory);
        }

        return BuildStructuredHint(likely, threshold, check, priority);
    }

    public static string ComposeFallback(string complaint, TriageCategory category)
    {
        switch (category)
        {
            case TriageCategory.Red:
                return BuildStructuredHint(
                    "Hayati risk olabilir",
                    "Morarma, solunum zorlugu veya hizli kotulesme",
                    "Hava yolu, solunum ve nabiz",
                    "Kirmizi dusun; bekletme");

            case TriageCategory.Yellow:
                return BuildStructuredHint(
                    "Ciddi travma olabilir",
                    "ABC su an korunuyor ama tablo hafife alinmaz",
                    "Dolasim, ciddi kirik ve gizli kanama",
                    "Sari dusun; yakindan izle");

            case TriageCategory.Green:
                return BuildStructuredHint(
                    "Hafif travma veya stres yaniti olabilir",
                    "Yuruyor, konusuyor ve genel durumu korunuyor",
                    "Gizli kanama, yeni bas donmesi ve nefes sikintisi",
                    "Yesil dusun; dusuk oncelik");

            case TriageCategory.Black:
                return BuildStructuredHint(
                    "Kardiyorespiratuvar arrest olabilir",
                    "Hava yolu sonrasi da solunum yok",
                    "Yasam belirtisi ve buyuk kanama",
                    "Siyah dusun; START'ta beklenti dusuk");

            default:
                return string.IsNullOrWhiteSpace(complaint)
                    ? BuildStructuredHint(
                        "Tablo net degil",
                        "Karari degistiren kritik bulgu henuz ayiklanmadi",
                        "Hava yolu, solunum, dolasim ve bilinc",
                        "En kritik bulguya gore karar ver")
                    : BuildStructuredHint(
                        "Tabloyu netlestirmek gerekiyor",
                        "Sikayeti belirleyen esas hayati bulguyu ayikla",
                        "Kisa ABC taramasi ve ana yakinma",
                        "Tek karar verdiren bulguya odaklan");
        }
    }

    private static string SelectThreshold(TriageCaseProfile profile, int variant)
    {
        if (profile == null)
        {
            return string.Empty;
        }

        if (profile.actualCategory == TriageCategory.Black)
        {
            return "Hava yolu sonrasi da solunum yok";
        }

        string observation = TakeLeadClause(profile.CriticalObservationOrFallback, MaxClauseLength);
        string triage = TakeLeadClause(profile.TriageHintOrFallback, MaxClauseLength);

        if (variant == 1 && !LooksEquivalent(triage, observation))
        {
            return triage;
        }

        return string.IsNullOrWhiteSpace(observation) ? triage : observation;
    }

    private static string SelectCheck(TriageCaseProfile profile, int variant)
    {
        if (profile == null)
        {
            return "Hava yolu, solunum ve dolasim";
        }

        if (profile.actualCategory == TriageCategory.Black)
        {
            return variant == 2
                ? "Tek son ABC kontrolu ve buyuk kanama"
                : "Hava yolu sonrasi spontan solunum ve yasam belirtisi";
        }

        string initialChecks = TakeLeadClause(profile.InitialChecksOrFallback, MaxClauseLength);
        if (!string.IsNullOrWhiteSpace(initialChecks))
        {
            return initialChecks;
        }

        switch (profile.actualCategory)
        {
            case TriageCategory.Red:
                return "Hava yolu, solunum ve nabiz";

            case TriageCategory.Yellow:
                return "Dolasim, kanama ve major travma";

            case TriageCategory.Green:
                return "Gizli kotulesme ve yurutulebilme";

            default:
                return "Hava yolu, solunum ve dolasim";
        }
    }

    private static string SelectPriority(TriageCaseProfile profile, int variant)
    {
        if (profile == null)
        {
            return "Karari en kritik bulguya gore ver";
        }

        switch (profile.actualCategory)
        {
            case TriageCategory.Red:
                return variant == 1 ? "Kirmizi dusun; dakikalar onemli" : "Kirmizi dusun; bekletme";

            case TriageCategory.Yellow:
                return variant == 2 ? "Sari dusun; bozulursa yukselt" : "Sari dusun; orta-yuksek oncelik";

            case TriageCategory.Green:
                return variant == 1 ? "Yesil dusun; gizli risk yoksa bekleyebilir" : "Yesil dusun; dusuk oncelik";

            case TriageCategory.Black:
                return variant == 1 ? "Siyah dusun; START'ta beklenti dusuk" : "Siyah dusun; geri donus bulgusu yok";

            default:
                return TakeLeadClause(profile.TriageHintOrFallback, MaxClauseLength);
        }
    }

    private static string BuildStructuredHint(string likely, string threshold, string check, string priority)
    {
        StringBuilder builder = new StringBuilder(256);
        AppendLine(builder, "Olasi durum", likely);
        AppendLine(builder, "Esik bulgu", threshold);
        AppendLine(builder, "Simdi bak", check);
        AppendLine(builder, "Oncelik", priority);
        return builder.ToString().Trim();
    }

    private static void AppendLine(StringBuilder builder, string label, string value)
    {
        if (builder == null || string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append('\n');
        }

        builder.Append(label);
        builder.Append(": ");
        builder.Append(NormalizeSentence(value));
    }

    private static string DefaultLikely(TriageCategory category)
    {
        switch (category)
        {
            case TriageCategory.Red:
                return "Acil bozulma riski olabilir";

            case TriageCategory.Yellow:
                return "Ciddi ama gecikmeye kismen dayanan travma olabilir";

            case TriageCategory.Green:
                return "Hafif travma veya stres yaniti olabilir";

            case TriageCategory.Black:
                return "Kardiyorespiratuvar arrest olabilir";

            default:
                return "Tabloyu netlestirmek gerekiyor";
        }
    }

    private static string TakeLeadClause(string text, int maxLength)
    {
        string normalized = NormalizeSentence(text);
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length <= maxLength)
        {
            return normalized;
        }

        int separatorIndex = normalized.IndexOf("; ", StringComparison.Ordinal);
        if (separatorIndex > 0 && separatorIndex < maxLength)
        {
            return normalized.Substring(0, separatorIndex).TrimEnd('.', ',', ';', ':');
        }

        int cutIndex = normalized.LastIndexOf(' ', Math.Min(maxLength, normalized.Length - 1));
        if (cutIndex < 24)
        {
            cutIndex = Math.Min(maxLength, normalized.Length);
        }

        return normalized.Substring(0, cutIndex).TrimEnd('.', ',', ';', ':');
    }

    private static bool LooksEquivalent(string left, string right)
    {
        return string.Equals(NormalizeSentence(left), NormalizeSentence(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSentence(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string normalized = text
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();

        while (normalized.Contains("  "))
        {
            normalized = normalized.Replace("  ", " ");
        }

        return normalized.Trim().TrimEnd('.', ';', ':', ',');
    }
}
