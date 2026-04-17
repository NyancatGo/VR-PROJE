using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Yarali NPC'lere wallhack (duvar arkasi gorunurluk) efekti ekler.
/// Bu scripti her yarali NPC prefab'ina veya sahne objesine ekle.
/// Onemli: Wallhack'i sadece WallhackToggleManager kontrol eder,
/// bu script kendi basina acmaz.
/// </summary>
public class YaraliWallhack : MonoBehaviour
{
    [Header("Wallhack Ayarlari")]
    [Tooltip("Silhouette rengi (kirmizi = acil yarali)")]
    public Color silhouetteColor = new Color(1f, 0.2f, 0.2f, 0.6f);

    [Tooltip("Nabiz hizi — gorunurluk titresimi")]
    [Range(0f, 5f)]
    public float pulseSpeed = 2f;

    [Tooltip("Minimum alpha (en soluk an)")]
    [Range(0f, 1f)]
    public float pulseMin = 0.3f;

    [Tooltip("Maksimum alpha (en parlak an)")]
    [Range(0f, 1f)]
    public float pulseMax = 0.8f;

    [HideInInspector]
    public bool wallhackAktif = false;

    private Material wallhackMat;
    private List<RendererData> originalRenderers = new List<RendererData>();
    private bool isSetup = false;

    private struct RendererData
    {
        public Renderer renderer;
        public Material[] originalMaterials;
    }

    void Start()
    {
        // Sadece setup yap — wallhack'i ACMA
        // Wallhack acma/kapama tamamen WallhackToggleManager tarafindan yonetilir
        Setup();
    }

    public void Setup()
    {
        if (isSetup) return;

        // Shader'i bul ve materyal olustur
        Shader wallhackShader = Shader.Find("Custom/WallhackSilhouette");
        if (wallhackShader == null)
        {
            Debug.LogError("[YaraliWallhack] 'Custom/WallhackSilhouette' shader bulunamadi!");
            return;
        }

        wallhackMat = new Material(wallhackShader);
        wallhackMat.SetColor("_Color", silhouetteColor);
        wallhackMat.SetFloat("_PulseSpeed", pulseSpeed);
        wallhackMat.SetFloat("_PulseMin", pulseMin);
        wallhackMat.SetFloat("_PulseMax", pulseMax);

        // Tum rendererlari kaydet
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r is ParticleSystemRenderer) continue;

            originalRenderers.Add(new RendererData
            {
                renderer = r,
                originalMaterials = r.sharedMaterials
            });
        }

        isSetup = true;
    }

    /// <summary>
    /// Wallhack efektini aktif eder — NPC duvar arkasinda gorunur
    /// </summary>
    public void EnableWallhack()
    {
        if (!isSetup) Setup();
        if (wallhackMat == null) return;

        wallhackAktif = true;

        foreach (RendererData rd in originalRenderers)
        {
            if (rd.renderer == null) continue;

            // Orijinal materyallerin uzerine wallhack materyalini ekle
            Material[] baseMats = rd.originalMaterials;
            Material[] newMats = new Material[baseMats.Length + 1];
            baseMats.CopyTo(newMats, 0);
            newMats[newMats.Length - 1] = wallhackMat;
            rd.renderer.materials = newMats;
        }

        Debug.Log($"[YaraliWallhack] {gameObject.name} icin wallhack AKTIF");
    }

    /// <summary>
    /// Wallhack efektini kapatir — normal gorunum
    /// </summary>
    public void DisableWallhack()
    {
        wallhackAktif = false;

        foreach (RendererData rd in originalRenderers)
        {
            if (rd.renderer == null) continue;
            // Orijinal materyallere geri don
            rd.renderer.materials = rd.originalMaterials;
        }

        Debug.Log($"[YaraliWallhack] {gameObject.name} icin wallhack KAPALI");
    }

    /// <summary>
    /// Rengi runtime'da degistir (ornegin triyaj onceligi icin)
    /// </summary>
    public void SetColor(Color newColor)
    {
        silhouetteColor = newColor;
        if (wallhackMat != null)
        {
            wallhackMat.SetColor("_Color", silhouetteColor);
        }
    }

    void OnDestroy()
    {
        if (wallhackMat != null)
        {
            Destroy(wallhackMat);
        }
    }

    /// <summary>
    /// Triyaj durumuna gore wallhack siluetinin rengini ayarlar
    /// </summary>
    public void UpdateWallhackColorByTriage(TriageCategory category)
    {
        if (!isSetup) Setup();

        Color newColor = silhouetteColor; // default/existing
        switch (category)
        {
            case TriageCategory.Green: newColor = Color.green; break;
            case TriageCategory.Yellow: newColor = Color.yellow; break;
            case TriageCategory.Red: newColor = Color.red; break;
            case TriageCategory.Black: newColor = Color.black; break;
        }

        // Görünürlüğü de biraz artırabiliriz triyajdan sonra
        pulseMin = 0.5f;
        pulseMax = 1.0f;
        pulseSpeed = category == TriageCategory.Red ? 3f : (category == TriageCategory.Black ? 0f : 1.5f);

        SetColor(newColor);

        if (wallhackMat != null)
        {
            wallhackMat.SetFloat("_PulseMin", pulseMin);
            wallhackMat.SetFloat("_PulseMax", pulseMax);
            wallhackMat.SetFloat("_PulseSpeed", pulseSpeed);
        }

        // Cok Onemli: Eger Wallhack kapalıysa (H'ye basilmamissa) renk atamasi mesh uzerine gecmez!
        // Oyuncu triyaj atadiginda bu rengi GORMELI, bu yuzden aktif ediyoruz:
        if (!wallhackAktif)
        {
            EnableWallhack();
        }
    }
}
