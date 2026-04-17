using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;

/// <summary>
/// Sahne yüklendiğinde fazla AudioListener bileşenlerini devre dışı bırakır.
/// Sadece XR Origin kamerasındaki AudioListener aktif kalır.
/// </summary>
public class AudioListenerManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        // Her sahne yüklendiğinde çalışacak
        SceneManager.sceneLoaded += OnSceneLoaded;
        // İlk sahne için de çalıştır
        CleanupAudioListeners();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CleanupAudioListeners();
    }

    private static void CleanupAudioListeners()
    {
        var xrOrigin = XRCameraHelper.GetXROrigin();
        AudioListener primaryListener = null;

        // XR Origin'in kamerasındaki AudioListener'ı bul
        if (xrOrigin != null && xrOrigin.Camera != null)
        {
            primaryListener = xrOrigin.Camera.GetComponent<AudioListener>();
            if (primaryListener != null)
                primaryListener.enabled = true;
        }

        // Tüm AudioListener'ları bul
        var allListeners = FindObjectsOfType<AudioListener>(true);
        int disabledCount = 0;

        foreach (var listener in allListeners)
        {
            if (listener == primaryListener)
                continue;

            listener.enabled = false;
            disabledCount++;
        }

        if (disabledCount > 0)
        {
            Debug.Log($"[AudioListenerManager] {disabledCount} fazla AudioListener devre dışı bırakıldı. " +
                      $"Aktif listener: {(primaryListener != null ? primaryListener.gameObject.name : "YOK")}");
        }
    }
}
