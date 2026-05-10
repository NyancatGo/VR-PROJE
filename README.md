# VR AFET Egitimi

Unity 2022.3.62f3 ile gelistirilen VR deprem kurtarma egitimi projesi. Proje Quest/OpenXR hedefli Android APK uretir; Firebase analytics, CSV export/audit scriptleri ve Modul 3 AI gateway altyapisi kaynak kodla birlikte gelir.

## Hazir APK

Son dogrulanan Android/Quest test APK buradan indirilebilir:

[VR_AFET_FINAL_TEST.apk](https://github.com/NyancatGo/VR-PROJE/raw/main/Releases/VR_AFET_FINAL_TEST.apk)

Dogrulama bilgisi:

- SHA256: `B5BF0BE6710943426421EC068396243BA998BBB5695055D5C18418728C88B793`
- Boyut: `541,411,351` byte
- Build tipi: Development build / final test adayi
- Hedef: Meta Quest / Android OpenXR

## Gereksinimler

- Unity `2022.3.62f3`
- Android Build Support, Android SDK/NDK ve OpenJDK Unity Hub uzerinden kurulu olmali
- Git LFS kurulu olmali (`git lfs install`)
- Node.js 20 ve npm (Firebase Functions runtime ile uyumlu)
- Firebase/Google config dosyalari repoda vardir; servis private key veya provider API secret repoya yazilmaz

## Ilk Kurulum

```powershell
git clone https://github.com/NyancatGo/VR-PROJE.git
cd VR-PROJE
git lfs pull
npm install
cd functions
npm install
cd ..
```

Unity Hub ile klasoru acarken editor surumu `2022.3.62f3` secilmeli. Unity ilk acilista asset import ve Android dependency resolve islemlerini yapabilir.

## Android / Quest Build

Unity Editor icinde:

1. `File > Build Settings`
2. Platform: `Android`
3. Scenes In Build icinde aktif sahneler:
   - `Samples/XR Interaction Toolkit/2.6.5/XR Device Simulator/Scenes/Modul1`
   - `Samples/XR Interaction Toolkit/2.6.5/XR Device Simulator/Scenes/Modul2_Guvenlik`
   - `Samples/XR Interaction Toolkit/2.6.5/XR Device Simulator/Scenes/Modul3_Triyaj`
   - `Samples/XR Interaction Toolkit/2.6.5/XR Device Simulator/Scenes/Modul4_yanginmudahale`
4. `Build` ile APK al.

Kritik Android ayarlari kaynakta sabitlenmistir:

- ARM64 + IL2CPP
- Minimum SDK 24
- OpenXR + Meta Quest Support
- XR Device Simulator sadece Editor'da aktif, APK'da kapali
- Custom Android manifest aktif
- Build sirasinda `QuestAndroidManifestPostprocessor` final manifest'i Quest icin guvenli hale getirir

## Firebase Analytics Kontrolu

```powershell
npm run audit:firebase -- --participant katilimci_key
npm run export:firebase-csv -- --participant katilimci_key
```

CSV ciktilari lokal export klasorlerine yazilir ve GitHub'a alinmaz. Firestore yazimlari deterministic dokuman ID'leri kullanir; hazir/gercek veri etiketleri CSV tarafinda gizlenir.

## Modul 3 AI Gateway

Cloud Functions kodu `functions/` altindadir. Secret degerler Firebase runtime config/secret olarak tutulmalidir, repoya yazilmamalidir.

```powershell
cd functions
npm run build
npm test
```

## Teslim Notu

Ara build APK'leri repoya commitlenmez; sadece `Releases/` altindaki dogrulanmis APK Git LFS ile saklanir. Teslim icin APK Unity'den tekrar uretilebilir. Son dogrulanan build akisi Android/Quest manifest tarafinda hardware acceleration, landscape orientation, optional eye tracking ve ARM64 native library kontrollerinden gecmistir.
