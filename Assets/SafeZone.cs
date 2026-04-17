using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// Güvenli bölge — oyuncu içeri girince sırtındaki yaralıyı YaralıYeri noktalarına yatırır.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SafeZone : MonoBehaviour
{
    [Header("Güvenli Bölge Eşiği (metre)")]
    [SerializeField] private float rescueDistance = 3f;

    private Collider col;
    private float    timer        = 0f;
    private const float INTERVAL  = 0.5f;

    private Transform playerXROrigin = null;

    void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true;
        
        // Rigidbody yoksa ekle (OnTrigger olayları için gereklidir)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= INTERVAL)
        {
            timer = 0f;
            DistanceCheck();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Tetiklenen objenin kökünde (veya kendisinde) oyuncu ile ilgili bir şey var mı diye kontrol edelim.
        if (other.CompareTag("Player") || other.GetComponentInParent<XROrigin>() != null || other.GetComponentInChildren<Camera>() != null)
        {
            Debug.Log("[SafeZone] OnTriggerEnter tetiklendi!");
            TryRescue();
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<XROrigin>() != null || other.GetComponentInChildren<Camera>() != null)
        {
            TryRescue(); 
        }
    }

    void DistanceCheck()
    {
        Transform playerTr = GetPlayerTransform();
        if (playerTr == null) return;

        // Player merkezi ile SafeZone'un en yakın yüzeyi (veya merkezi) arasındaki uzaklık
        Vector3 closest = col.ClosestPoint(playerTr.position);
        float dist = Vector3.Distance(playerTr.position, closest);

        if (dist <= rescueDistance)
        {
            Debug.Log($"[SafeZone] Mesafe limiti içinde ({dist}m). Kurtarma deneniyor...");
            TryRescue();
        }
    }

    void TryRescue()
    {
        var allYaralilar = FindObjectsOfType<YaraliController>();
        bool rescued = false;

        foreach (var yarali in allYaralilar)
        {
            // Sadece taşınmakta olan (isCarried) yaralıyı kurtar!
            if (yarali != null && yarali.isCarried)
            {
                Debug.Log($"[SafeZone] Taşınan yaralı ({yarali.gameObject.name}) tespit edildi, kurtarılıyor!");
                
                Transform spot = RescueManager.Instance?.GetNextDropSpot();
                if (spot != null)
                {
                    yarali.DropAtSpot(spot);
                }
                else
                {
                    Debug.LogWarning("[SafeZone] DropSpot bulunamadı, varsayılan bölgeye bırakılıyor.");
                    yarali.DropAtSafeZone(transform);
                }

                rescued = true;
            }
        }

        if (rescued && RescueManager.Instance != null)
        {
            Debug.Log("[SafeZone] Yaralı kurtarıldı. Skorbord güncelleniyor.");
            RescueManager.Instance.RemoveCarriedAndAddRescue();
        }
    }

    Transform GetPlayerTransform()
    {
        if (playerXROrigin != null) return playerXROrigin;

        var origin = FindObjectOfType<XROrigin>();
        if (origin != null) 
        {
            playerXROrigin = origin.transform;
            return playerXROrigin;
        }

        if (Camera.main != null)
        {
            playerXROrigin = Camera.main.transform;
            return playerXROrigin;
        }

        return null;
    }
}