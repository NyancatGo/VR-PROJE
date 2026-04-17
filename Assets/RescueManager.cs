using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class RescueManager : MonoBehaviour
{
    public static RescueManager Instance;
    public TextMeshProUGUI scoreText;

    [Header("Spawn Settings")]
    public GameObject yaraliPrefab;
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("Victim Settings")]
    public int totalVictimsToSpawn = 3;

    [Header("Bırakma Noktaları (YaralıYeri sıralı)")]
    [Tooltip("Sahne hiyerarşisindeki YaraliYeri, YaraliYeri2, YaraliYeri3 objeleri (Inspector'dan da atanabilir)")]
    public List<Transform> dropSpots = new List<Transform>();

    private int totalVictims = 0;
    private int rescuedCount = 0;
    private int carriedCount = 0;

    // Bir sonraki kullanılacak bırakma noktası indeksi
    private int nextDropSpotIndex = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // dropSpots atanmamışsa sahnede YaralıYeri/2/3 adlı objeleri bul
        if (dropSpots == null || dropSpots.Count == 0 || HasNullSpots())
            AutoFindDropSpots();

        SpawnVictims();
        UpdateUI();
    }

    bool HasNullSpots()
    {
        foreach (var s in dropSpots)
            if (s == null) return true;
        return false;
    }

    void AutoFindDropSpots()
    {
        // Resources.FindObjectsOfTypeAll ile inactive dahil tüm objeleri tara
        var allGOs = Resources.FindObjectsOfTypeAll<GameObject>();
        var found = new Dictionary<int, Transform>();

        string[] targetNames = { "YaralıYeri", "YaraliYeri" };
        string[] targetNames2 = { "YaralıYeri2", "YaraliYeri2" };
        string[] targetNames3 = { "YaralıYeri3", "YaraliYeri3" };

        foreach (var go in allGOs)
        {
            if (!go.scene.isLoaded) continue;

            string n = go.name;
            foreach (var tn in targetNames)
                if (n == tn && !found.ContainsKey(1)) { found[1] = go.transform; break; }

            foreach (var tn in targetNames2)
                if (n == tn && !found.ContainsKey(2)) { found[2] = go.transform; break; }

            foreach (var tn in targetNames3)
                if (n == tn && !found.ContainsKey(3)) { found[3] = go.transform; break; }
        }

        dropSpots = new List<Transform>();
        if (found.ContainsKey(1)) dropSpots.Add(found[1]);
        if (found.ContainsKey(2)) dropSpots.Add(found[2]);
        if (found.ContainsKey(3)) dropSpots.Add(found[3]);

        // Fallback: adında "yarali" + "yeri" geçen objeleri alfabetik sırala
        if (dropSpots.Count == 0)
        {
            var candidates = new List<GameObject>();
            foreach (var go in allGOs)
            {
                if (!go.scene.isLoaded) continue;
                string lower = go.name.ToLower();
                if (lower.Contains("yarali") && lower.Contains("yeri"))
                    candidates.Add(go);
            }
            candidates.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
            foreach (var go in candidates)
                dropSpots.Add(go.transform);
        }

        Debug.Log($"[RescueManager] AutoFindDropSpots: {dropSpots.Count} nokta bulundu.");
        for (int i = 0; i < dropSpots.Count; i++)
        {
            if (dropSpots[i] != null)
                Debug.Log($"  [{i}] -> {dropSpots[i].name} at {dropSpots[i].position}");
        }
    }

    /// <summary>
    /// Sıradaki bırakma noktasını döndürür ve indeksi ilerletir.
    /// </summary>
    public Transform GetNextDropSpot()
    {
        if (dropSpots == null || dropSpots.Count == 0)
        {
            Debug.LogWarning("[RescueManager] Hiç dropSpot tanımlı değil!");
            return null;
        }

        // Geçersiz (null) noktaları atla
        while (nextDropSpotIndex < dropSpots.Count && dropSpots[nextDropSpotIndex] == null)
            nextDropSpotIndex++;

        if (nextDropSpotIndex >= dropSpots.Count)
        {
            Debug.LogWarning("[RescueManager] Tüm bırakma noktaları tükendi, son geçerli nokta kullanıldı.");
            // Son geçerli noktayı döndür
            for (int i = dropSpots.Count - 1; i >= 0; i--)
                if (dropSpots[i] != null) return dropSpots[i];
            return null;
        }

        Transform spot = dropSpots[nextDropSpotIndex];
        nextDropSpotIndex++;
        return spot;
    }

    void SpawnVictims()
    {
        if (yaraliPrefab == null)
        {
            totalVictims = FindObjectsOfType<YaraliController>().Length;
            return;
        }

        if (spawnPoints.Count == 0)
        {
            totalVictims = FindObjectsOfType<YaraliController>().Length;
            return;
        }

        List<Transform> availablePoints = new List<Transform>(spawnPoints);
        for (int i = 0; i < availablePoints.Count; i++)
        {
            Transform temp = availablePoints[i];
            int randomIndex = Random.Range(i, availablePoints.Count);
            availablePoints[i] = availablePoints[randomIndex];
            availablePoints[randomIndex] = temp;
        }

        int spawnCount = Mathf.Min(totalVictimsToSpawn, availablePoints.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            Instantiate(yaraliPrefab, availablePoints[i].position, availablePoints[i].rotation);
        }

        totalVictims = FindObjectsOfType<YaraliController>().Length;
    }

    public bool CanCarry()
    {
        return carriedCount < 1;
    }

    public void AddCarried()
    {
        carriedCount++;
        UpdateUI();
    }

    public void RemoveCarriedAndAddRescue()
    {
        if (carriedCount > 0)
        {
            carriedCount--;
            rescuedCount++;
            UpdateUI();

            if (rescuedCount >= totalVictims && totalVictims > 0)
            {
                Debug.Log("Gorev Tamamlandi: Tum yaralilar kurtarildi!");
                if (scoreText != null)
                {
                    scoreText.text = "<color=#00FF00>GÖREV BAŞARILI!</color>\nTüm Yaralılar Kurtarıldı.";
                }
            }
        }
    }

    void UpdateUI()
    {
        if (scoreText != null && rescuedCount < Mathf.Max(totalVictims, totalVictimsToSpawn))
        {
            int maxTotal = Mathf.Max(totalVictims, totalVictimsToSpawn);
            scoreText.text = $"<color=#FFFFFF>GÖREV DURUMU</color>\n-------------\nKURTARILAN:\n<size=120%><color=#00FF00>{rescuedCount} / {maxTotal}</color></size>\n\nSIRTINIZDAKİ:\n<color=#FFA500>{carriedCount}</color>";
        }
    }
}
