using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

namespace UI
{
    public enum ContentType
    {
        Infografik,
        Sunum,
        Video
    }

    public class ContentCard : MonoBehaviour
    {
        [Header("Content Settings")]
        public ContentType contentType;
        public string contentTitle;
        public Sprite thumbnail;
        public Sprite fullImage;
        public VideoClip videoClip;
        
        [Header("UI References")]
        public Button openButton;
        public Image thumbnailImage;
        public TextMeshProUGUI titleText;

        private ContentViewer _contentViewer;

        private void Awake()
        {
            // Auto-find ContentViewer if not assigned
            if (_contentViewer == null)
            {
                _contentViewer = FindObjectOfType<ContentViewer>();
            }

            // Setup open button
            if (openButton != null)
            {
                openButton.onClick.RemoveAllListeners();
                openButton.onClick.AddListener(OnOpenClicked);
            }

            // Update thumbnail and title
            UpdateCardDisplay();
        }

        private void UpdateCardDisplay()
        {
            if (thumbnailImage != null && thumbnail != null)
            {
                thumbnailImage.sprite = thumbnail;
            }

            if (titleText != null)
            {
                titleText.text = contentTitle;
            }
        }

        private void OnOpenClicked()
        {
            if (_contentViewer != null)
            {
                _contentViewer.ShowContent(contentType, fullImage, videoClip, contentTitle);
            }
        }

        // Public methods for setting content data
        public void SetContentData(string title, Sprite thumb, Sprite fullImg)
        {
            contentTitle = title;
            thumbnail = thumb;
            fullImage = fullImg;
            UpdateCardDisplay();
        }

        public void SetContentData(string title, Sprite thumb, VideoClip video)
        {
            contentTitle = title;
            thumbnail = thumb;
            videoClip = video;
            contentType = ContentType.Video;
            UpdateCardDisplay();
        }
    }
}
