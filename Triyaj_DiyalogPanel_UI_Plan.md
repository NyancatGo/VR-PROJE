# Triyaj Diyalog Paneli - Detaylı UI Analiz ve Revizyon Planı

## 1) Mevcut Durum Analizi (Kod ve Görsel Üzerinden)

### Ana kontrol noktası
- Diyalog panelinin yerleşimi runtime'da büyük ölçüde `TriageDialogUI.cs` içinde yeniden kuruluyor.
- Yani sadece prefab üzerinde elle yaptığın birçok değişiklik açılışta geri ezilebilir.

### Kritik dosyalar
- Runtime UI düzeni: `Assets/Scripts/TriyajModul3/TriageDialogUI.cs`
- Prefab kaynağı: `Assets/Prefabs/Triyaj/TriageButtonsCanvas.prefab`
- Editor üretici araç: `Assets/Scripts/Editor/TriyajHospitalSetupTool.cs`

### Gözlenen sorunlar (ekrandaki duruma uyumlu)
1. **Chat/içerik alanı küçük görünüyor**
   - `HintSection` yüksekliği ve `HintScrollView` alanı dar kalıyor.
   - Şikayet bloğu yüksekliği fazla, karar buton alanı çok baskın.
2. **Yarı siyah çerçeve fazla yer kaplıyor**
   - Ana panel + gölge (`PanelShadow`) + koyu alt paneller üst üste binip görsel ağırlığı artırıyor.
   - Kontrast oranı iyi ama negatif boşluk kullanımı zayıf.
3. **Alt renkli butonlar fazla büyük**
   - Runtime preset: `triageButtonCellSize = (248,102)` (hala büyük algılanıyor).
   - 2x2 grid yüksekliği, içerik alanından çalıyor.
4. **Hiyerarşi dengesi (visual hierarchy) net değil**
   - Kullanıcının öncelikle okuyacağı bölüm (şikayet + ipucu) ile eylem butonları yarışıyor.
   - “Triyaj Kararı” başlığı karar butonlarına göre görsel olarak zayıf.

### Kök neden
- `ApplyResolvedLayoutPreset()` metodu her açılışta sabit değerlerle düzen kuruyor.
- Bu yüzden hedef estetik için doğrudan bu metot ve onu kullanan layout fonksiyonları revize edilmeli.

---

## 2) Altın Oran ve Okunabilirlik Temelli Yeni Yerleşim Stratejisi

Amaç: Panelde görsel ağırlığı **içerik (şikayet + ipucu) %62** / **eylem alanı %38** civarına getirmek.

### Önerilen alan dağılımı
- Üst şikayet bloğu: ~%20
- Orta ipucu/chat bloğu: ~%33
- Alt karar buton bölgesi: ~%31
- Kalan: başlık/boşluk/geçiş payları ~%16

### Boyut stratejisi
- Canvas'ı yatay biraz büyüt: metin nefes alsın.
- Butonları küçült ama tıklanabilirlik sınırını koru (VR için minimum rahat hedef alan korunmalı).
- Padding/margin değerlerini düşürüp “gerçek içerik alanı” artır.

### Renk/katman stratejisi
- Ana panel opacity'yi biraz azalt.
- `PanelShadow` offset ve alpha’yı düşür.
- Chat iç arka planını çok koyu siyahdan bir ton aç.
- Renkli butonların doygunluğu 1 kademe azaltılsın (daha premium/az bağıran görünüm).

---

## 3) Ölçülebilir Revizyon Hedefleri (Target Değerler)

> Bu değerler doğrudan `TriageDialogUI.cs` içindeki runtime layout için hedeflenmeli.

### A) Global preset (`ApplyResolvedLayoutPreset`)
- `canvasSize`: **(980,620) -> (1080,680)**
- `triageButtonCellSize`: **(248,102) -> (214,82)**
- `triageButtonSpacing`: **(18,16) -> (14,12)**
- `triageButtonAnchorY`: **0.115 -> 0.095**
- `complaintPadding`: **(18,14) -> (20,12)**

### B) Panel ve gölge
- `ApplyPanelLayout`: panel inset'i çok hafif azalt
  - offsetMin: **(18,16) -> (16,14)**
  - offsetMax: **(-18,-16) -> (-16,-14)**
- `EnsurePanelShadow`:
  - offsetMin: **(30,-28) -> (18,-16)**
  - offsetMax: **(12,-10) -> (8,-6)**
  - shadow alpha düşürülmeli

### C) Şikayet bloğu (üst)
- `EnsureComplaintBlock`:
  - anchorMin.y: **0.74 -> 0.76**
  - anchorMax.y: **0.92 -> 0.93**
- `complaintText.fontSize`: **27 -> 25** (auto-size ile)
- satır aralığı biraz düşürülmeli

### D) İpucu/chat bloğu (orta)
- `EnsureHintSection`:
  - anchorMin.y: **0.35 -> 0.40**
  - anchorMax.y: **0.70 -> 0.74**
- `EnsureHintScrollView`:
  - anchorMin.y: **0.19 -> 0.17**
  - anchorMax.y: **0.64 -> 0.70**
- `HintButton`:
  - sizeDelta: **(208,44) -> (188,38)**
  - fontSize: **18 -> 16**

### E) Karar başlığı + buton grid (alt)
- `DecisionTitle`:
  - anchorY: **0.31 -> 0.33**
  - fontSize: **17 -> 16**
- `buttonContainer`:
  - boyut otomatik hesap aynı kalsın ama hücre küçüldüğü için toplam alan daralacak
  - alt blok daha kompakt görünecek
- buton label:
  - font max: **24 -> 21**
  - padding düşürülsün

---

## 4) Codex'e Vereceğin En Doğru Prompt (Kopyala-Yapıştır)

```text
Unity projemde triage diyalog UI’sini estetik ve okunabilirlik açısından yeniden dengele.

ÖNEMLİ:
1) Sadece şu dosyayı düzenle: Assets/Scripts/TriyajModul3/TriageDialogUI.cs
2) Prefab dosyasını elle değiştirme; runtime layout zaten bu script tarafından kuruluyor.
3) Kod stili mevcut dosyayla aynı kalsın.
4) Yeni yorum (comment) ekleme.

HEDEF:
- Chat/ipucu alanı daha büyük ve okunabilir olsun.
- Yarı siyah dış çerçevenin kapladığı alan/yoğunluk azalsın.
- Alt renkli triyaj butonları daha kompakt olsun.
- Görsel hiyerarşi: önce şikayet + ipucu okunmalı, sonra karar butonları.
- VR tıklanabilirlik korunmalı.

UYGULANACAK DEĞERLER:
- ApplyResolvedLayoutPreset:
  - canvasSize = new Vector2(1080f, 680f)
  - triageButtonCellSize = new Vector2(214f, 82f)
  - triageButtonSpacing = new Vector2(14f, 12f)
  - triageButtonAnchorY = 0.095f
  - complaintPadding = new Vector2(20f, 12f)
- ApplyPanelLayout:
  - offsetMin = new Vector2(16f, 14f)
  - offsetMax = new Vector2(-16f, -14f)
- EnsurePanelShadow:
  - offsetMin = new Vector2(18f, -16f)
  - offsetMax = new Vector2(8f, -6f)
  - shadow renginin alpha’sını düşür
- EnsureComplaintBlock:
  - anchorMin = new Vector2(0.075f, 0.76f)
  - anchorMax = new Vector2(0.925f, 0.93f)
- EnsureHintSection:
  - anchorMin = new Vector2(0.08f, 0.40f)
  - anchorMax = new Vector2(0.92f, 0.74f)
- EnsureHintScrollView:
  - anchorMin = new Vector2(0.04f, 0.17f)
  - anchorMax = new Vector2(0.96f, 0.70f)
- EnsureHintButton:
  - sizeDelta = new Vector2(188f, 38f)
- ConfigureHintButton:
  - label fontSize = 16f
  - fontSizeMax = 16f
- ApplyDecisionTitleLayout:
  - anchor y = 0.33f
  - fontSize = 16f
- ConfigureButtonLabel / StyleButton:
  - triage label fontSize max = 21f, min = 17f
  - label paddingi bir miktar azalt

EK KURAL:
- Renkleri tamamen değiştirme, mevcut temayı koru.
- Sadece ton ve yoğunluk iyileştirmesi yap.

ÇIKTI:
- Değişiklikleri tamamla.
- Sonrasında dosyada yapılan değişiklikleri kısa maddeler halinde özetle.
```

---

## 5) Revizyon Sonrası Hızlı Kontrol Checklist

- Panel oyuncu önünde aynı mesafede açılıyor mu?
- Şikayet metni tek bakışta okunuyor mu?
- İpucu/chat bloğu eskisine göre belirgin şekilde büyüdü mü?
- Butonlar küçük ama VR lazerle rahat tıklanıyor mu?
- Siyah/yarı saydam dış görünüm sahneyi boğmadan okunabilir kontrast veriyor mu?
- Farklı şikayet uzunluklarında metin taşması var mı?

---

## 6) Not (Önemli)

Eğer ileride `Tools/Triyaj Hastane/Olustur/Triyaj Buton Canvas` ile prefabı yeniden üretirsen, editor aracındaki varsayılanlar farklı olabilir. Bu yüzden kalıcı estetik standardı için ikinci adımda `TriyajHospitalSetupTool.cs` içindeki triage buton grid sabitlerini de `TriageDialogUI.cs` ile uyumlu hale getirmek gerekir.
