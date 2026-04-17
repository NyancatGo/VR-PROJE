using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

namespace UI
{
    public class ContentViewer : MonoBehaviour
    {
        [Header("Viewer Panel")]
        public GameObject viewerPanel;
        public Image contentImage;
        public RawImage videoRawImage;
        public VideoPlayer videoPlayer;
        
        [Header("UI Elements")]
        public Button closeButton;
        public TextMeshProUGUI titleText;
        public GameObject loadingIndicator;

        [Header("Navigation")]
        public Button prevButton;
        public Button nextButton;
        public int currentIndex;
        public ContentCard[] contentCards;


        private void Awake()
        {
            // Setup close button
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseViewer);
            }

            // Setup navigation buttons
            if (prevButton != null)
            {
                prevButton.onClick.RemoveAllListeners();
                prevButton.onClick.AddListener(ShowPrevious);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(ShowNext);
            }

            // Hide viewer initially
            if (viewerPanel != null)
            {
                viewerPanel.SetActive(false);
            }

            // Find all content cards
            contentCards = FindObjectsOfType<ContentCard>();
        }

        public void ShowContent(ContentType contentType, Sprite image, VideoClip videoClip, string title)
        {
            if (viewerPanel == null) return;

            viewerPanel.SetActive(true);

            // Update title
            if (titleText != null)
            {
                titleText.text = title;
            }

            // Handle content based on type
            if (contentType == ContentType.Video && videoClip != null)
            {
                ShowVideo(videoClip);
            }
            else if (image != null)
            {
                ShowImage(image);
            }

            // Update navigation state
            UpdateNavigationButtons();
        }

        private void ShowImage(Sprite image)
        {

            // Show image, hide video
            if (contentImage != null)
            {
                contentImage.gameObject.SetActive(true);
                contentImage.sprite = image;
            }

            if (videoRawImage != null)
            {
                videoRawImage.gameObject.SetActive(false);
            }

            if (videoPlayer != null)
            {
                videoPlayer.Stop();
                videoPlayer.gameObject.SetActive(false);
            }
        }

        private void ShowVideo(VideoClip videoClip)
        {

            // Hide image, show video
            if (contentImage != null)
            {
                contentImage.gameObject.SetActive(false);
            }

            if (videoRawImage != null)
            {
                videoRawImage.gameObject.SetActive(true);
            }

            if (videoPlayer != null)
            {
                videoPlayer.gameObject.SetActive(true);
                videoPlayer.clip = videoClip;
                videoPlayer.Play();
            }
        }

        public void CloseViewer()
        {
            if (viewerPanel != null)
            {
                viewerPanel.SetActive(false);
            }

            // Stop video if playing
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }
        }

        private void ShowPrevious()
        {
            if (contentCards == null || contentCards.Length == 0) return;

            currentIndex--;
            if (currentIndex < 0)
            {
                currentIndex = contentCards.Length - 1;
            }

            ShowContentAtIndex(currentIndex);
        }

        private void ShowNext()
        {
            if (contentCards == null || contentCards.Length == 0) return;

            currentIndex++;
            if (currentIndex >= contentCards.Length)
            {
                currentIndex = 0;
            }

            ShowContentAtIndex(currentIndex);
        }

        private void ShowContentAtIndex(int index)
        {
            if (contentCards == null || index < 0 || index >= contentCards.Length) return;

            var card = contentCards[index];
            if (card != null)
            {
                ShowContent(card.contentType, card.fullImage, card.videoClip, card.contentTitle);
            }
        }

        private void UpdateNavigationButtons()
        {
            // Enable/disable navigation based on content count
            bool hasMultipleContent = contentCards != null && contentCards.Length > 1;

            if (prevButton != null)
            {
                prevButton.gameObject.SetActive(hasMultipleContent);
            }

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(hasMultipleContent);
            }
        }

        private void OnDestroy()
        {
            // Clean up video player
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }
        }
    }
}
