# Deprem Sonrası Şehir Sahnesi

Bu proje, deprem sonrası yıkılmış bir şehir sahnesi oluşturmak için gerekli araçları içerir.

## Klasör Yapısı

- **Assets/Enkaz/**: Enkaz ve yıkık bina prefabları
  - EnkazBuyuk.prefab - Büyük enkaz yığınları
  - EnkazKucuk.prefab - Küçük enkaz parçaları
  - YikikDuvar.prefab - Yıkık duvarlar
  - YikikZemin.prefab - Yıkık zemin parçaları
  - YikikKolon.prefab - Yıkık kolonlar

- **Assets/Scenes/DepremSonrasiSehir.unity**: Ana deprem sahnesi

- **Assets/Scripts/DepremSahnesiYoneticisi.cs**: Sahne yönetim scripti

## Kullanım

### 1. Sahneyi Açma
- `Assets/Scenes/DepremSonrasiSehir.unity` sahnesini açın

### 2. Enkaz Yerleştirme
- Hierarchy'de `DepremYoneticisi` objesini seçin
- Inspector'da prefab listelerini kontrol edin
- "Sahneyi Oluştur" butonuna tıklayın

### 3. Ayarlar

**Enkaz Prefabları**: 
- Assets/Enkaz klasöründen prefabları sürükleyip bırakın

**Yerleştirme Ayarları**:
- `Spawn Radius`: Enkaz yerleştirme alanının yarıçapı
- `Enkaz Sayisi`: Kaç adet enkaz yerleştirilecek
- `Yıkık Bina Sayisi`: Kaç adet yıkık bina parçası yerleştirilecek

**Rastgele Dağılım**:
- `Gruplar Halinde`: Enkaz gruplar halinde mi yerleştirilsin?
- `Grup Başına Enkaz`: Her grupta kaç enkaz olacak

### 4. Manuel Enkaz Ekleme

**Window > Ruin Helper** menüsünden:
- Spawn Large Rubble Pile
- Spawn Small Rubble Pieces
- Spawn Broken Wall (End)
- Spawn Corner Broken Wall
- Spawn Broken Floor
- Spawn Pillar Frame

## Mevcut Asset'ler

Projede kullanılabilir enkaz asset'leri:

1. **DBK Modüler Bina Parçaları**:
   - Yıkık duvarlar (Wall_End_A, Wall_90_B)
   - Yıkık zeminler (Floor_End_B, Floor_90_A)
   - Beton kolonlar (Pillar_A, Pillar_B)

2. **Rubble/Debris Modelleri**:
   - Büyük enkaz yığınları (DBK_Concrete_Rubble_Big)
   - Küçük enkaz parçaları (DBK_Concrete_Rubble_Small_C)
   - Rubble_Big_Sample

## İpuçları

- Terrain üzerinde çalışıyorsanız, script otomatik olarak enkaz yüksekliklerini ayarlayacaktır
- Farklı enkaz dağılımları denemek için "Enkaz Temizle" ve"Sahneyi Oluştur" butonlarını kullanın
- Manuel yerleştirme için Ruin Helper penceresini kullanabilirsiniz
- Enkaz prefablarına kendi modellerinizi ekleyebilirsiniz

## Sahne Özellikleri

- Puslu atmosfer (Fog) deprem sonrası havayı simüle eder
- Düşük ambient ışık yıkık şehir atmosferi verir
- Terrain 100x100 boyutunda, ihtiyaç halinde genişletilebilir

## Geliştirme

Yeni özellikler eklemek için:
1. `DepremSahnesiYoneticisi.cs` scriptini düzenleyin
2. Enkaz klasörüne yeni prefablar ekleyin
3. Inspector'dan yeni prefabları listeye ekleyin
