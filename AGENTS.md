# AGENTS.md - Unity VR Deprem Kurtarma Simülasyonu

## Proje Özeti
Bu proje, Unity 2022.3.62f3, XR Interaction Toolkit, TextMeshPro ve Universal Render Pipeline kullanılarak geliştirilen bir Unity VR deprem kurtarma simülasyonudur.

## Derleme Komutları

### Projeyi Derleme
```bash
# Unity Editor'de projeyi açın ve şunu kullanın:
# File > Build Settings > Build

# Komut satırı derleme (Unity Hub kurulu olmalı):
"<unity-editor-yolu>" -quit -batchmode -buildTarget StandaloneWindows64 -buildPath "Build/Windows"
```

### Testleri Çalıştırma
```bash
# Unity Test Framework testleri (NUnit tabanlı)
# Unity Editor'de: Window > General > Test Runner
# Veya Rider/VS Code ile Unity Test Framework eklentisi kullanın

# Tüm play mode testlerini çalıştır:
"<unity-editor-yolu>" -quit -batchmode -runPlayModeTests -testResults "Temp/test-results.xml"

# Tüm edit mode testlerini çalıştır:
"<unity-editor-yolu>" -quit -batchmode -runEditorTests -testResults "Temp/test-results.xml"

# Tek bir test sınıfı çalıştır (test adına göre filtrele):
"<unity-editor-yolu>" -quit -batchmode -runPlayModeTests -testFilter "TestClassName" -testResults "Temp/test-results.xml"
```

### Kod Analizi
```bash
# Roslyn analizörleri desteklenen IDE'lerde otomatik çalışır (Rider, Unity eklentili VS)
# Ayrı linting komutu yoktur; IDE kod incelemesi kullanın
```

---

## Kod Stili Kuralları

### Genel İlkeler
- Unity varsayılan kuralları + C# standart pratikleri
- Hedef: .NET 4.x (Assembly-CSharp.csproj'de yapılandırılmış)
- LangVersion 9.0

### Dosya Yapısı
```
Assets/
  Scripts/           # Çalışma zamanı C# scriptleri (MonoBehaviour'lar, vb.)
    Editor/          # Yalnızca Editor scriptleri
  Editor/            # Üst düzey Editor scriptleri (custom inspector'lar, menü öğeleri)
  Scenes/            # Unity sahne dosyaları
  Prefabs/           # Prefab varlıkları
  Materials/         # Materyal varlıkları
  TextMesh Pro/      # TMPro kaynakları ve örnekleri
```

### Adlandırma Kuralları
| Öğe | Kural | Örnek |
|-----|-------|-------|
| Sınıflar | PascalCase | `YaraliController`, `TriyajManager` |
| Metotlar | PascalCase | `CycleTriage()`, `ApplyTriage()` |
| Alanlar (public) | PascalCase | `totalVictims`, `dogruTriyajSayisi` |
| Alanlar (private) | _camelCase veya camelCase | `_interactable`, `carriedCount` |
| Serileştirilmiş alanlar | camelCase | `spawnRadius`, `enkazPrefabs` |
| Sabitler | PascalCase | `MaxCarryCount` |
| Enum'lar | PascalCase | `TriageCategory.Green` |
| Namespace'ler | PascalCase | `TriyajModul3` |
| Unity API çağrıları | Orijinal | `GameObject`, `Transform`, `Debug.Log` |

### Kod Biçimlendirme
- ** Girinti**: 4 boşluk (Unity varsayılanı)
- ** Süslü Parantezler**: Allman stili (açan süslü parantez yeni satırda)
- ** Satır uzunluğu**: Katı sınır yok, ancak ~120 karakter civarında sarılabilir
- ** Attribute'lar**: Sınıfın/metodun üzerine satır olarak yazılır:
  ```csharp
  [RequireComponent(typeof(XRSimpleInteractable))]
  public class YaraliController : MonoBehaviour
  {
      [SerializeField] private Transform backAnchor;
      
      [Header("Triyaj Ayarları")]
      public TriageCategory ActualTriageState = TriageCategory.Unassigned;
  }
  ```

### İçe Aktarımlar (Imports)
```csharp
using UnityEngine;                      // UnityEngine her zaman ilk sırada
using UnityEngine.XR.Interaction.Toolkit; // XR toolkit ikinci sırada
using System.Collections.Generic;         // System namespace'leri sonra
using TMPro;                             // Üçüncü parti (TextMeshPro)
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;            // Koşullu derleme en son
#endif
```
- Kullanılmayan içe aktarımları commitlemeden önce kaldırın
- Gruplama sırası: Unity, XR Toolkit, System, Üçüncü parti, Koşullu

### Türler
- Belirli türler için `var` kullanın: `var hit = new RaycastHit();`
- Tür belirsiz olduğunda `var` kullanmaktan kaçının: `var result = Calculate();` (olmaz)
- Dinamik koleksiyonlar için ham diziler yerine `List<T>` kullanın
- Küçük veri container'ları için `struct` tercih edin (Unity vektör türleri)
- Serileştirilebilir sınıflar için `Serializable` kullanın:
  ```csharp
  [Serializable]
  public class ModuleData
  {
      public string moduleName;
      public int moduleId;
  }
  ```

### Null Kontrolleri
- Null-conditional kullanın: `TriyajManager.Instance?.ApplyTriage(...)`
- Null-coalescing kullanın: `scoreText ?? GetComponent<TextMeshProUGUI>()`
- Singleton kullanmadan önce null kontrolü yapın:
  ```csharp
  if (RescueManager.Instance != null)
      RescueManager.Instance.AddCarried();
  ```

### Hata Yönetimi
- `try/catch` kullanımını minimal tutun; savunma amaçlı null kontrollerini tercih edin
- Hataları `Debug.LogError` veya `Debug.LogWarning` ile loglayın
- Geliştirme zamanı kontrolleri için `Debug.Assert` kullanın:
  ```csharp
  Debug.Assert(yaraliPrefab != null, "Yarali prefab atanmadı!");
  ```

### Unity'ye Özgü Kalıplar

#### Singleton Kalıbı
```csharp
public class RescueManager : MonoBehaviour
{
    public static RescueManager Instance;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}
```

#### Serileştirilmiş Referanslar
```csharp
[SerializeField] private XRSimpleInteractable interactable;
[Header("Yerleştirme Ayarları")]
public GameObject yaraliPrefab;
public List<Transform> spawnPoints = new List<Transform>();
```

#### Coroutine Kalıbı
```csharp
IEnumerator SpawnWithDelay()
{
    yield return new WaitForSeconds(1f);
    Instantiate(prefab);
}
```

#### Yalnızca Editor Kodu
```csharp
#if UNITY_EDITOR
[ContextMenu("Enkaz Temizle")]
public void EnkaziTemizle() { ... }
#endif
```

#### Koşullu Input Sistemi
```csharp
#if ENABLE_INPUT_SYSTEM
if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
    ToggleWallhack();
#else
if (Input.GetKeyDown(KeyCode.H))
    ToggleWallhack();
#endif
```

### Dokümantasyon
- Public API için XML doc yorumları kullanın:
  ```csharp
  /// <summary>
  /// Yaralıya bir triyaj rengi atanması
  /// </summary>
  public void AssignTriage(TriageCategory assignedCategory) { ... }
  ```
- Serileştirilmiş alanlar için Inspector'da dokümantasyon amacıyla `[Tooltip]` kullanın
- Inspector'da serileştirilmiş alanları gruplamak için `[Header]` kullanın

### Test Kuralları
- Testler `Assets/Tests/` veya `Assets/PlayModeTests/` dizinlerine konur (keşif için isimlendirme önemli)
- Unity Test Framework (NUnit tabanlı) kullanın:
  ```csharp
  using NUnit.Framework;
  
  public class TriyajTests
  {
      [Test]
      public void TestTriyajDongusu()
      {
          var controller = new GameObject().AddComponent<YaraliController>();
          Assert.AreEqual(TriageCategory.Unassigned, controller.AssignedTriageState);
      }
  }
  ```
- Yield gerektiren coroutine testleri için `[UnityTest]` kullanın

### Yaygın Hatalar
1. **Update içinde `FindObjectsOfType` kullanmayın** (pahalıdır)
2. **Yolları hardcode etmeyin**; `Application.dataPath` veya Resources kullanın
3. **GameObject'leri doğrudan yok etmeyin**; Editor'de değilse `Destroy()` kullanın
4. **Script başka bir bileşene bağımlıysa `[RequireComponent]` kullanın**
5. **Hafıza sızıntılarını önlemek için olayları `OnDisable()` içinde çözer**

---

## IDE Yapılandırması

### Rider (Önerilen)
- Unity eklentisini kurun: `com.unity.ide.rider` (manifest'te zaten var)
- Kod stili: Unity varsayılanını kullanın (veya ReSharper > Manage Options > Unity)
- Analyzer severity: Varsayılan (uyarılar gösterilir)

### VS Code
- Kurun: `com.unity.ide.vscode` paketi
- IntelliSense için C# eklentisini kullanın

### VS 2022
- Kurun: `com.unity.ide.visualstudio` paketi
- Etkinleştirin: `Tools > Unity > General`

---

## Sürüm Bilgileri
- **Unity**: 2022.3.62f3
- **XR Toolkit**: 2.6.5
- **Test Framework**: 1.1.33
- **Hedef Platform**: StandaloneWindows64

---

## Modül 3 / XR Geliştirme Notları (Kritik Uyarılar ve Referanslar)
Bu projede XR Interaction Toolkit yapılandırmalarında belirli kısıtlamalar ve buglar tespit edilmiştir. İlerleyen geliştirmelerde bu hataların tekrarlanmaması için aşağıdaki referanslar **kesinlikle uyulması gereken kurallardır**.

### 1. VR Canvas Boyutlandırma ve Render Modu
VR ortamında oluşturulan tüm Canvas objelerinde yapılan en büyük hata varsayılan ayarları kullanmaktır.
* **Render Mode:** Kesinlikle `World Space` olmalıdır.
* **Boyut (Rect Transform):** Width/Height genelde `1920x1080` veya `800x600` tutulur ancak Scale kesinlikle `(0.002, 0.002, 0.002)` veya `(0.0015, 0.0015, 0.0015)` yapılmalıdır.
* **Neden?** Unity'de 1 birim 1 metreye eşittir. Scale 1 kalırsa Canvas 1.9 kilometre genişliğinde bir dev gibi haritaya kaplanır.

### 2. VR'da UI Butonlarına Tıklayabilme (Raycaster & Event System)
Standart Unity UI butonları (Graphic Raycaster) VR lazerlerini (XR Ray Interactor) algılamaz.
* **Canvas Modifier:** Canvas obje hiyerarşisine mutlaka `Tracked Device Graphic Raycaster` eklenmelidir. (Varsayılan `Graphic Raycaster` silinebilir veya pasif bırakılabilir).
* **Event System:** Sahnede aktif olan Event System üzerinde `Standalone Input Module` YERİNE `XR UI Input Module` bulunmalıdır. 
* **Layer:** Canvas ve Canvas içindeki butonların Layer'ı `UI` olarak kalmalıdır.

### 3. XR UI Input Bug'ı ve Özel "VRUIClickHelper" Çözümü
Projeler arası geçiş ve farklı VR cihazları (Oculus/Pico/Simulator) sebebiyle `XR UI Input Module` bazen tetikleyicileri (Trigger/Grip) algılamamaktadır.
* **Çözüm:** Bu projede buton tıklamaları garantilemek için `VRUIClickHelper` scripti geliştirilmiştir. Tıklanmayan Canvaslara bu bileşen eklenmelidir.
* **Nasıl Çalışır?** `VRUIClickHelper`, `XRRayInteractor` üzerinden doğrudan "lazerin şu an UI'da neye değdiğini" alır (`TryGetCurrentUIRaycastResult`). Ardından hem PC simülatöründe tetiklenen klavye girişleri (G tuşu) hem de VR kollarından okunan Grip (`CommonUsages.gripButton`) durumuna göre hedeflenen butondaki `IPointerClickHandler` fonksiyonunu manuel olarak çalıştırır.

### 4. Dinamik Canvas Konumlandırma (Kamera Yanılgısı)
VR projelerinde NPC ile etkileşime girildiğinde Canvas'ın kameranın etrafında rastgele veya yanlış yerlerde spawn olması yaygın bir sorundur.
* **Hata:** `Camera.main` kullanımı VR sahnesinde genelde patlar çünkü sahnedeki başka pasif kameralar (örn. UI kamerası) ana etiketine sahip olabilir.
* **Doğru Kullanım:** Oyuncunun gerçek kafa konumunu bulmak için **`FindObjectOfType<Unity.XR.CoreUtils.XROrigin>().Camera.transform`** referans alınmalıdır.
* **Pozisyonlama Formülü:** 
```csharp
Transform playerCam = xrOrigin.Camera.transform;
// Canvas'ı kameranın hizasında 1.5 metre ileri koy
canvasTransform.position = playerCam.position + (playerCam.forward * 1.5f);
// Canvas'ın yüzeyini oyuncuya döndür (LookAt)
canvasTransform.LookAt(canvasTransform.position + playerCam.rotation * Vector3.forward, playerCam.rotation * Vector3.up);
```

### 5. Işınlanma Sonrası Haritadan Düşme Sorunu
`XROrigin` içindeki `CharacterController`, ışınlanma(Teleportation) yapıldığı anda eğer ayak hizasını tam bir zemin üzerinde bulamazsa oyuncuyu sonsuzluğa düşürmektedir.
* **Zemin (Collider) Kuralı:** Oyuncunun ışınlanacağı hedef alandaki yer objelerinde `Mesh Collider` veya `Box Collider` **ŞARTTIR**. Ayrıca `Is Trigger` seçeneği kesinlikle **KAPALI** olmalıdır.
* **Safeback (Görünmez Zemin):** Hastane gibi yükseltili/kompleks modelleden zeminlerin sorunlu Mesh yapısına karşı en güvenli çözüm: Yere `Y` scale değeri `0.1` olan koca bir Cube koyup, `Mesh Renderer`'ını kapatarak onu devasa bir "Görünmez Taban" (Box Collider) olarak sabitlemektir.
* **Raycast Teleport Adaptasyonu:** Işınlanma anında kod üzerinden manuel zemin bulma işlemi kullanılmalıdır:
```csharp
if (Physics.Raycast(hedefPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore)) {
    hedefPos.y = hit.point.y; // Yüksekliği zemine yapıştır
}
```
