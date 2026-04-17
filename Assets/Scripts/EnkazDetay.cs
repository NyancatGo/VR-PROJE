using UnityEngine;

/// <summary>
/// Enkaz objelerine eklenebilecek basit bir detay scripti
/// Enkaz parçalarına renk varyasyonu ve rastgele rotasyon ekler
/// </summary>
public class EnkazDetay : MonoBehaviour
{
    [Header("Renk Varyasyonu")]
    [Tooltip("Enkaz rengine hafif varyasyon ekle")]
    public bool renkVaryasyonuEkle = true;
    
    [Range(0, 0.3f)]
    public float renkVaryasyonMiktari = 0.15f;
    
    [Header("Rastgele Rotasyon")]
    [Tooltip("Başlangıçta rastgele rotasyon uygula")]
    public bool rastgeleRotasyon = true;
    
    void Start()
    {
        if (renkVaryasyonuEkle)
        {
            RenkVaryasyonu();
        }
        
        if (rastgeleRotasyon)
        {
            RastgeleRotasyonUygula();
        }
    }
    
    void RenkVaryasyonu()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        
        foreach (Renderer rend in renderers)
        {
            if (rend.material != null)
            {
                // Materyalin rengine hafif varyasyon ekle
                Color baseColor = rend.material.color;
                float varyasyon = Random.Range(-renkVaryasyonMiktari, renkVaryasyonMiktari);
                
                Color yeniRenk = new Color(
                    Mathf.Clamp01(baseColor.r + varyasyon),
                    Mathf.Clamp01(baseColor.g + varyasyon),
                    Mathf.Clamp01(baseColor.b + varyasyon),
                    baseColor.a
                );
                
                rend.material.color = yeniRenk;
            }
        }
    }
    
    void RastgeleRotasyonUygula()
    {
        // Y ekseninde tamamen rastgele, X ve Z'de hafif rotasyon
        Vector3 rastgeleEuler = new Vector3(
            Random.Range(-10f, 10f),
            Random.Range(0f, 360f),
            Random.Range(-10f, 10f)
        );
        
        transform.rotation = Quaternion.Euler(rastgeleEuler);
    }
}
