using System.Collections.Generic;
using UnityEngine;

public static class TriageCaseCatalog
{
    private static readonly TriageCaseProfile[] Profiles =
    {
        new TriageCaseProfile
        {
            caseId = "gogus-basisi-morarma",
            caseName = "Moraran Solunum Acili",
            patientTitle = "Hasta 1 | Gogus basisi ve morarma",
            tone = "Panikli konusuyor, her cumlesinde nefesi yarim kaliyor ve yardim bekliyor.",
            complaintText = "Gogsume sanki agir bir sey oturdu. Nefes alirken gogsum kilitleniyor, dudaklarim morariyor gibi ve cumlemi tamamlayamiyorum.",
            criticalObservation = "Artan solunum eforu, konusurken zorlanma ve belirgin morarma tabloyu hizla kotulestiren isaretler.",
            suspectedCondition = "Agir solunum sikintisi, gogus travmasi veya hipoksiyle giden bir yasamsal tehdit dusundurur.",
            initialChecks = "Konusma tamamlaniyor mu, gogus hareketi esit mi, SpO2 dusuk mu ve yardimci solunum kaslari devrede mi diye bak.",
            triageHint = "Morarma, tek kelimelik konusma veya belirgin solunum yuku varsa bekletme; bu hasta en ust acil hatta yakindir.",
            actualCategory = TriageCategory.Red,
            accentColor = new Color(0.18f, 0.86f, 0.93f, 0.95f)
        },
        new TriageCaseProfile
        {
            caseId = "ezilme-ic-kanama",
            caseName = "Ezilme ve Ic Kanama Suphesi",
            patientTitle = "Hasta 2 | Ezilme ve ic kanama supesi",
            tone = "Disaridan sakin gorunuyor ama agri nedeniyle icine kapanmis; her harekette yuzunu sikiyor.",
            complaintText = "Enkaz bacaklarimi ve kalcami uzun sure sikistirdi. Karnimin ici de baski yapiyor, basim donuyor ve otururken bile fenalasiyorum.",
            criticalObservation = "Ezilme oykusu, iceri coken agri, solukluk ve bas donmesi gizli dolasim bozulmasi lehine kuvvetli uyaridir.",
            suspectedCondition = "Pelvik travma, ezilme sendromu veya ic kanama nedeniyle sok gelisiyor olabilir.",
            initialChecks = "Cilt rengi, nabiz hizi, kapiller dolum, karinda sertlik ve pelviste instabilite var mi diye hizla kontrol et.",
            triageHint = "Dis kanama az olsa bile solukluk ve dolasim bozuluyorsa bu hasta bekleyebilecek grupta degil; ust oncelige cikarmayi dusun.",
            actualCategory = TriageCategory.Red,
            accentColor = new Color(0.82f, 0.39f, 0.32f, 0.95f)
        },
        new TriageCaseProfile
        {
            caseId = "acik-kirik-bilinc-acik",
            caseName = "Acik Kirik Ama Bilinci Acik",
            patientTitle = "Hasta 3 | Acik kirik ama bilinci acik",
            tone = "Acisi yogun ama sorulara tutarli cevap veriyor; kontrolu kaybetmemek icin kendini zor tutuyor.",
            complaintText = "Bacagimda kemik disari cikti, cok aciyor ama nefesim normal. Bilincim acik, konusabiliyorum ama ayagimin ustune basamiyorum.",
            criticalObservation = "Agir ortopedik travma var ancak hasta iletisim kuruyor, hava yolu acik ve solunumu korunuyor.",
            suspectedCondition = "ABC gorece stabil olsa da acik kirik ciddi doku hasari ve kan kaybi riski tasiyor.",
            initialChecks = "Distal nabiz, aktif kanama, duyu-kaybi, deformite ve agriya ragmen genel stabiliteyi tekrar degerlendir.",
            triageHint = "Hayati bulgular stabil kalirken ciddi ortopedik travma varsa geciktirme ama en ust acil ile de karistirma; orta-yuksek oncelik mantigi uygundur.",
            actualCategory = TriageCategory.Yellow,
            accentColor = new Color(0.94f, 0.73f, 0.24f, 0.95f)
        },
        new TriageCaseProfile
        {
            caseId = "duman-soluma-ses-kisikligi",
            caseName = "Duman Soluma ve Ses Kisikligi",
            patientTitle = "Hasta 4 | Duman soluma ve ses kisikligi",
            tone = "Konusurken sesi catallaniyor; yuzunde isi ve dumanin yarattigi huzursuzluk cok belirgin.",
            complaintText = "Koridorda dumana maruz kaldim. Sesim gidiyor, bogazim daraliyor gibi ve oksurdukce gogsum yaniyor.",
            criticalObservation = "Ses kisikligi, bogazda daralma hissi ve duman oykusu ust hava yolu odemine gidebilecek tabloyu dusundurur.",
            suspectedCondition = "Inhalasyon yaralanmasi veya ust hava yolu etkilenmesi nedeniyle hasta ani kotulesebilir.",
            initialChecks = "Agiiz-burun icinde kurum, stridor, yuz-boyun yanigi, solunum sesi ve oksijenlenmeyi hemen kontrol et.",
            triageHint = "Ses belirgin kisik, stridor var veya hava yolu daraliyor hissi kuvvetliyse bekletme; bu tablo hizla en acile kayabilir.",
            actualCategory = TriageCategory.Red,
            accentColor = new Color(0.95f, 0.52f, 0.22f, 0.95f)
        },
        new TriageCaseProfile
        {
            caseId = "yuruyebilen-sarsilmis",
            caseName = "Yuruyebilen Ama Sarsilmis",
            patientTitle = "Hasta 5 | Yuruyebilen ama sarsilmis",
            tone = "Ayakta durabiliyor ama sesi titresimli; daha cok korku, sersemlik ve hafif agrilardan soz ediyor.",
            complaintText = "Kendi basima yuruyebiliyorum. Dizimde ve kolumda ufak agrilar var, biraz sersemledim ama nefesimde ya da bilincimde sorun hissetmiyorum.",
            criticalObservation = "Yuruyebilmesi, tutarli konusmasi ve hayati risk belirten belirgin bulgu olmamasi dusuk aciliyeti destekler.",
            suspectedCondition = "Hafif travma, yumusak doku yaralanmasi veya stres yaniti on planda olabilir.",
            initialChecks = "Yuruyus dengesi, bas donmesi artiyor mu, gizli kanama bulgusu var mi ve bilinci net mi diye kisa tarama yap.",
            triageHint = "Hayati risk bulgusu yok, yurutulebiliyor ve genel durumu korunuyorsa bu hasta daha alt oncelikte tutulabilir.",
            actualCategory = TriageCategory.Green,
            accentColor = new Color(0.26f, 0.72f, 0.42f, 0.95f)
        },
        new TriageCaseProfile
        {
            caseId = "yanitsiz-solunumsuz",
            caseName = "Yanitsiz ve Solunumsuz",
            patientTitle = "Hasta 6 | Yanit yok, solunum yok",
            tone = "Sahadaki ekip icin agir bir an; tablo sogukkanli ama net bir START degerlendirmesi istiyor.",
            complaintText = "Hasta yanit vermiyor. Spontan solunum yok, hareket yok ve ilk bakida geri donus gosteren bir bulgu secilemiyor.",
            criticalObservation = "Yanitsizlik ve solunumsuzluk, basit hava yolu duzeltmesine ragmen degismiyorsa beklentisiz gorunum olusturur.",
            suspectedCondition = "Kardiyorespiratuvar arrest veya geri donussuz travmatik kayip dusunulur.",
            initialChecks = "Hava yolunu kisa duzelt, spontan solunum geri geliyor mu, buyuk kanama var mi ve temel yasam belirtisi seciliyor mu diye bak.",
            triageHint = "Hava yolu acildiginda da solunum yoksa START mantiginda beklenti dusuktur; renk secimi bu sert gercege gore yapilmalidir.",
            actualCategory = TriageCategory.Black,
            accentColor = new Color(0.38f, 0.45f, 0.56f, 0.95f)
        }
    };

    public static int Count => Profiles.Length;

    public static TriageCaseProfile GetProfileForIndex(int index)
    {
        if (Profiles.Length == 0)
        {
            return new TriageCaseProfile();
        }

        int wrappedIndex = Mathf.Abs(index) % Profiles.Length;
        return Profiles[wrappedIndex].Clone();
    }

    public static void ApplyProfiles(IList<NPCTriageInteractable> npcs)
    {
        if (npcs == null)
        {
            return;
        }

        for (int i = 0; i < npcs.Count; i++)
        {
            NPCTriageInteractable npc = npcs[i];
            if (npc == null)
            {
                continue;
            }

            npc.ApplyCaseProfile(GetProfileForIndex(i));
        }
    }
}
