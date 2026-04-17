using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;

public class VideoContentManager : MonoBehaviour
{
    [Header("Video Ayarları")]
    public VideoPlayer videoPlayer;
    public RawImage videoRawImage;
    public GameObject playButton;
    public GameObject pauseButton;
    public TextMeshProUGUI videoTitle;
    public TextMeshProUGUI videoDescription;

    [Header("Video Listesi")]
    public VideoClip[] videoClips;
    public string[] videoTitles;
    public string[] videoDescriptions;

    private int currentVideoIndex = -1;
    private bool isPlaying = false;

    void Start()
    {
        // Başlangıçta video panelini gizle
        if (videoRawImage != null)
        {
            videoRawImage.enabled = false;
        }
    }

    public void PlayVideo(int index)
    {
        if (index < 0 || videoClips == null || index >= videoClips.Length)
            return;

        currentVideoIndex = index;

        // Video listesini güncelle
        if (videoPlayer != null && videoClips[index] != null)
        {
            videoPlayer.clip = videoClips[index];
            videoPlayer.Play();
            isPlaying = true;

            if (videoRawImage != null)
            {
                videoRawImage.enabled = true;
            }

            UpdateButtons();
        }

        // Başlık ve açıklamayı güncelle
        if (videoTitle != null && videoTitles != null && index < videoTitles.Length)
        {
            videoTitle.text = videoTitles[index];
        }

        if (videoDescription != null && videoDescriptions != null && index < videoDescriptions.Length)
        {
            videoDescription.text = videoDescriptions[index];
        }
    }

    public void TogglePlayPause()
    {
        if (videoPlayer == null)
            return;

        if (isPlaying)
        {
            videoPlayer.Pause();
            isPlaying = false;
        }
        else
        {
            videoPlayer.Play();
            isPlaying = true;
        }

        UpdateButtons();
    }

    public void StopVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            isPlaying = false;

            if (videoRawImage != null)
            {
                videoRawImage.enabled = false;
            }

            UpdateButtons();
        }
    }

    void UpdateButtons()
    {
        if (playButton != null)
        {
            playButton.SetActive(!isPlaying);
        }

        if (pauseButton != null)
        {
            pauseButton.SetActive(isPlaying);
        }
    }

    public int GetCurrentVideoIndex()
    {
        return currentVideoIndex;
    }

    public bool IsVideoPlaying()
    {
        return isPlaying;
    }
}
