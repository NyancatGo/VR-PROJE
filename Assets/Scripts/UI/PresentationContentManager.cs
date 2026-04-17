using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PresentationContentManager : MonoBehaviour
{
    [Header("Sunum Ayarları")]
    public GameObject presentationPanel;
    public RawImage slideImage;
    public TextMeshProUGUI slideTitle;
    public TextMeshProUGUI slideDescription;
    public TextMeshProUGUI slideCounter;

    [Header("Sunum Slaytları")]
    public Sprite[] slides;
    public string[] slideTitles;
    public string[] slideDescriptions;

    [Header("Navigasyon Butonları")]
    public Button previousButton;
    public Button nextButton;
    public Button closeButton;

    private int currentSlideIndex = -1;

    void Start()
    {
        // Başlangıçta sunum panelini gizle
        if (presentationPanel != null)
        {
            presentationPanel.SetActive(false);
        }

        // Buton listener'larını ekle
        if (previousButton != null)
        {
            previousButton.onClick.AddListener(PreviousSlide);
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(NextSlide);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePresentation);
        }

        // Başlangıçta navigasyon butonlarını ayarla
        UpdateNavigationButtons();
    }

    public void OpenPresentation()
    {
        if (slides == null || slides.Length == 0)
            return;

        if (presentationPanel != null)
        {
            presentationPanel.SetActive(true);
        }

        // İlk slaytı göster
        ShowSlide(0);
    }

    public void OpenPresentation(int startIndex)
    {
        if (slides == null || slides.Length == 0)
            return;

        if (presentationPanel != null)
        {
            presentationPanel.SetActive(true);
        }

        ShowSlide(startIndex);
    }

    public void ClosePresentation()
    {
        if (presentationPanel != null)
        {
            presentationPanel.SetActive(false);
        }

        currentSlideIndex = -1;
    }

    public void NextSlide()
    {
        if (slides == null || slides.Length == 0)
            return;

        int nextIndex = currentSlideIndex + 1;
        if (nextIndex < slides.Length)
        {
            ShowSlide(nextIndex);
        }
    }

    public void PreviousSlide()
    {
        if (slides == null || slides.Length == 0)
            return;

        int prevIndex = currentSlideIndex - 1;
        if (prevIndex >= 0)
        {
            ShowSlide(prevIndex);
        }
    }

    void ShowSlide(int index)
    {
        if (index < 0 || slides == null || index >= slides.Length)
            return;

        currentSlideIndex = index;

        // Slayt görselini güncelle
        if (slideImage != null && slides[index] != null)
        {
            slideImage.texture = slides[index].texture;
        }

        // Başlığı güncelle
        if (slideTitle != null && slideTitles != null && index < slideTitles.Length)
        {
            slideTitle.text = slideTitles[index];
        }

        // Açıklamayı güncelle
        if (slideDescription != null && slideDescriptions != null && index < slideDescriptions.Length)
        {
            slideDescription.text = slideDescriptions[index];
        }

        // Sayaç güncelle
        if (slideCounter != null)
        {
            slideCounter.text = $"{index + 1} / {slides.Length}";
        }

        UpdateNavigationButtons();
    }

    void UpdateNavigationButtons()
    {
        if (previousButton != null)
        {
            previousButton.interactable = currentSlideIndex > 0;
        }

        if (nextButton != null)
        {
            nextButton.interactable = slides != null && currentSlideIndex < slides.Length - 1;
        }
    }

    public int GetCurrentSlideIndex()
    {
        return currentSlideIndex;
    }

    public int GetTotalSlides()
    {
        return slides != null ? slides.Length : 0;
    }
}
