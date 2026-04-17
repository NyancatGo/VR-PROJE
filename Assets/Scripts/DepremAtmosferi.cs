using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Deprem sahnesi için görsel efektler ve atmosfer ayarları
/// </summary>
public class DepremAtmosferi : MonoBehaviour
{
    [Header("Toz ve Duman Efektleri")]
    [Tooltip("Havada uçuşan toz partikülleri")]
    public ParticleSystem tozPartikuller;
    
    [Tooltip("Enkaz üzerinden yükselen duman")]
    public ParticleSystem dumanPartikuller;
    
    [Header("Atmosfer Ayarları")]
    [Tooltip("Sis yoğunluğu")]
    [Range(0, 0.1f)]
    public float sisYogunlugu = 0.01f;
    
    [Tooltip("Sis rengi (grimsi-kahverengi)")]
    public Color sisRengi = new Color(0.7f, 0.65f, 0.6f, 1f);
    
    [Header("Işık Ayarları")]
    [Tooltip("Güneş ışığı yoğunluğu (düşük olmalı)")]
    [Range(0, 2)]
    public float isikYogunlugu = 0.6f;
    
    [Tooltip("Ambient ışık rengi")]
    public Color ambientRenk = new Color(0.4f, 0.4f, 0.4f, 1f);

    [Tooltip("Bu obje devre disi kaldiginda deprem atmosfer ayarlarini varsayilana geri al.")]
    public bool restoreDefaultsOnDisable = true;
    
    [Header("Ses Efektleri")]
    [Tooltip("Ortam ses kaynağı")]
    public AudioSource ortamSesi;
    
    [Tooltip("Rüzgar ve enkaz sesleri")]
    public AudioClip ruzgarSesi;
    
    void Start()
    {
        AtmosferiAyarla();
        
        if (ortamSesi != null && ruzgarSesi != null)
        {
            ortamSesi.clip = ruzgarSesi;
            ortamSesi.loop = true;
            ortamSesi.volume = 0.3f;
            ortamSesi.Play();
        }
    }
    
    void AtmosferiAyarla()
    {
        // Sis ayarları
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = sisYogunlugu;
        RenderSettings.fogColor = sisRengi;
        
        // Ambient ışık
        RenderSettings.ambientLight = ambientRenk;
        RenderSettings.ambientIntensity = 0.7f;
        
        // Directional light'ı bul ve ayarla
        Light[] lights = FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.intensity = isikYogunlugu;
                light.color = new Color(0.93f, 0.89f, 0.8f); // Hafif sarımsı beyaz
                break;
            }
        }
    }
    
    /// <summary>
    /// Runtime'da toz partikül efekti oluştur
    /// </summary>
    public void TozEfektiOlustur(Vector3 konum)
    {
        if (tozPartikuller != null)
        {
            ParticleSystem toz = Instantiate(tozPartikuller, konum, Quaternion.identity);
            Destroy(toz.gameObject, 5f);
        }
    }
    
    /// <summary>
    /// Runtime'da duman efekti oluştur
    /// </summary>
    public void DumanEfektiOlustur(Vector3 konum)
    {
        if (dumanPartikuller != null)
        {
            ParticleSystem duman = Instantiate(dumanPartikuller, konum, Quaternion.identity);
            Destroy(duman.gameObject, 10f);
        }
    }
    
    void OnValidate()
    {
        // Editor'da değişiklik yapıldığında atmosferi güncelle
        if (Application.isPlaying)
        {
            AtmosferiAyarla();
        }
    }

    void OnDisable()
    {
        if (!Application.isPlaying || !restoreDefaultsOnDisable)
        {
            return;
        }

        VarsayilanAtmosfereDon();
    }

    private static void VarsayilanAtmosfereDon()
    {
        RenderSettings.fog = false;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.01f;
        RenderSettings.fogColor = Color.gray;

        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientLight = Color.white;
        RenderSettings.ambientIntensity = 1f;

        // Sonraki sahneye dusuk deprem aydinlatmasi tasinmasin.
        Light[] lights = FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            if (light == null || light.type != LightType.Directional)
            {
                continue;
            }

            light.color = Color.white;
            light.intensity = Mathf.Max(light.intensity, 1f);
            light.shadowStrength = Mathf.Min(light.shadowStrength, 0.85f);
        }
    }
}
