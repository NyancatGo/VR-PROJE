using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Doktor NPC'nin sorduğu AI test mekaniğini simüle eder.
/// Gerçek AI kodları eklenene kadar Placeholder (Sahte) 10 soruluk bir yapıyla UI besler.
/// </summary>
public class MockAIQuizManager : MonoBehaviour
{
    public static MockAIQuizManager Instance;

    [Header("UI Elemanları (Test/Sınav Ekranı)")]
    public GameObject quizCanvas;
    public TextMeshProUGUI soruText;
    
    // Basit bir senaryo için tuş ataması
    [Tooltip("Onay İçin Tuş Örn: Evet/Hayır gibi simülasyonlar için kullanılabilir.")]
    public TextMeshProUGUI bilgiText;

    private int mevcutSoruIndex = 0;
    private int score = 0;
    private bool sinavAktif = false;
    
    private List<string> mockSorular = new List<string>()
    {
        "1. Triyaj alanında SİYAH kodlu hasta öncelikli midir? [Evet(Y)/Hayır(N)]",
        "2. Nefes darlığı çeken hasta KIRMIZI alana mı alınır? [Evet(Y)/Hayır(N)]",
        "3. Açık kırığı olan hasta YEŞİL midir? [Evet(Y)/Hayır(N)]",
        "4. Kendi başına yürüyebilen hasta YEŞİL kategoride midir? [Evet(Y)/Hayır(N)]"
        // Burası daha da çoğaltılabilir. AI bağlandığında bu liste dinamik üretilecek!
    };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        if (quizCanvas != null)
            quizCanvas.SetActive(false);
    }

    public void StartQuiz()
    {
        sinavAktif = true;
        mevcutSoruIndex = 0;
        score = 0;

        if (quizCanvas != null)
        {
            quizCanvas.SetActive(true);
            SoruGoster();
        }
        else
        {
            Debug.LogWarning("[Geliştirme] Sınav UI Canvası eksik, sınav arkaplanda devam ediyor.");
            SoruGosterConsole();
        }
    }

    void Update()
    {
        if (!sinavAktif) return;

        // Kaba taslak evet/hayır tuşlama klavye takibi (Y / N tuşları kullanarak simüle ettim)
        if (Input.GetKeyDown(KeyCode.Y))
        {
            CevapVer(true);
        }
        else if (Input.GetKeyDown(KeyCode.N))
        {
            CevapVer(false);
        }
    }

    public void SoruGoster()
    {
        if (mevcutSoruIndex < mockSorular.Count)
        {
            soruText.text = mockSorular[mevcutSoruIndex];
            bilgiText.text = "Cevaplamak için Y (Evet) veya N (Hayır) tuşlarına basın.";
        }
        else
        {
            SinaviBitir();
        }
    }

    public void SoruGosterConsole()
    {
        if (mevcutSoruIndex < mockSorular.Count)
            Debug.Log("[DOKTOR AI]: " + mockSorular[mevcutSoruIndex]);
        else
            SinaviBitir();
    }

    public void CevapVer(bool evetMi)
    {
        // Burada gerçekte AI bağlantısı ile doğruluğu kontrol edeceğiz.
        // Şimdilik sadece basit bir puan simülasyonu:
        score += 10; 
        
        mevcutSoruIndex++;

        if (quizCanvas != null && quizCanvas.activeSelf)
        {
            SoruGoster();
        }
        else
        {
            SoruGosterConsole();
        }
    }

    public void SinaviBitir()
    {
        sinavAktif = false;
        if (soruText != null) 
            soruText.text = $"Sınav Bitti! Puanınız: {score}";
            
        if (bilgiText != null) 
            bilgiText.text = "Çıkmak için boşluğa (Escape/Panel Geri Tuşu) basın.";
            
        Debug.Log($"[DOKTOR AI] Değerlendirme Tamamlandı. Oyuncu Puanı: {score}");
    }
}
