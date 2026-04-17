using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Modul2EnkazPlasiyeri : MonoBehaviour
{
    /// <summary>
    /// Bu script Modul2_Guvenlik sahnesine enkazları yerleştirir.
    /// Inspector'dan çalıştırılır.
    /// </summary>
    
    [ContextMenu("Enkazları Modul2'ye Yerleştir")]
    public void YuklemeYap()
    {
        Debug.LogError("Lütfen Editor penceresiyle: Tools > Modul2 - Enkaz Yükle kullanın");
    }
}

#if UNITY_EDITOR

public static class Modul2EnkazOtomatikYukleme
{
    /// <summary>
    /// Oyun oynatıldığında otomatik enkazları yerleştir
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void OyunAcildikd()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            // Oyun modunda ise enkazları yapılandır
            var yonetici = Object.FindFirstObjectByType<DepremSahnesiYoneticisi>();
            if (yonetici != null)
            {
                Debug.Log("Deprem Enkaz Sistemi aktif!");
            }
        }
    }
}

#endif
