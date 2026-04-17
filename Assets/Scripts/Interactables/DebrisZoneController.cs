using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AfetSimulasyon.Modul2
{
    /// <summary>
    /// Enkaz kaldırma alanı için Trigger. Tanımlanan objeler (debris) bu alandan çıkınca (OnTriggerExit) kalan sayısı azalır, sıfırlandığında event tetiklenir.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DebrisZoneController : MonoBehaviour
    {
        [Header("Debris Settings")]
        [Tooltip("Yolu kapatan ve dışarı taşınması gereken enkaz objelerinin listesi. (Tag de kullanılabilir ancak liste daha garantilidir).")]
        public List<GameObject> debrisObjectsInZone = new List<GameObject>();

        [Header("Events")]
        public UnityEvent OnZoneCleared;

        private int initialDebrisCount;

        void Start()
        {
            initialDebrisCount = debrisObjectsInZone.Count;
            
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true; // Mutlaka Trigger olmalı
            }

            if (initialDebrisCount == 0)
            {
                Debug.LogWarning("DebrisZoneController: No debris objects assigned to the zone " + gameObject.name);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (debrisObjectsInZone.Contains(other.gameObject))
            {
                // Obje listede varsa ve alandan çıktıysa
                debrisObjectsInZone.Remove(other.gameObject);
                CheckZoneStatus();
            }
            // Objenin parent'ını kontrol etme (Grab Interactable objelerde bazen root/child Collider ayrımı olabilir)
            else if (other.attachedRigidbody != null && debrisObjectsInZone.Contains(other.attachedRigidbody.gameObject))
            {
                debrisObjectsInZone.Remove(other.attachedRigidbody.gameObject);
                CheckZoneStatus();
            }
        }

        private void CheckZoneStatus()
        {
            Debug.Log("Debris removed. Remaining in zone: " + debrisObjectsInZone.Count);

            if (debrisObjectsInZone.Count == 0)
            {
                ZoneCleared();
            }
        }

        private void ZoneCleared()
        {
            Debug.Log("<color=green>Debris Zone completely cleared!</color> Path is open.");
            OnZoneCleared?.Invoke();

            // TODO: GameManager entegrasyonu
            // Example: GameManager.Instance.CompleteTask("ClearDebris");
        }
    }
}
