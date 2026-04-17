# Manual Steps For User

## Firebase Paketleri

Unity Package import adimlarinda asagidaki Firebase paketlerini ekleyin:

- Firebase App
- Firebase Analytics
- Opsiyonel: Firebase Crashlytics
- Opsiyonel: Firebase Firestore

Bu kod tabani SDK olmadan da derlenir; SDK geldiginde reflection tabanli entegrasyon otomatik denenir.

## Config Dosyalari

- Android icin `google-services.json`
- iOS icin `GoogleService-Info.plist`

Bu dosyalari Firebase Console projenizden indirip Unity Firebase kurulum dokumanindaki standart konumlara yerlestirin.

## Unity Tarafinda Dogrulama

1. Projeyi Unity 2022.3.62f3 ile acin.
2. Console'da analytics bootstrap warning/error var mi kontrol edin.
3. `Modul1`, `Modul2_Guvenlik`, `Modul3_Triyaj` sahnelerini tek tek Play edin.
4. Oyun akisi bozulmadan eventlerin debug log veya Firebase DebugView uzerine dustugunu teyit edin.

## Sahneye Sonradan Takilabilecek Tracker'lar

Asagidaki component'ler daha sonra inspector uzerinden sahnelere eklenebilir:

- `VideoTracker`
- `InfographicTracker`
- `ContentOpenTracker`
- `TaskTracker`
- `ScenarioTracker`
- `QuizTracker`
- `AIInteractionTracker`
- `TriageTracker`

## Beklenen Smoke Testler

- Firebase yokken oyun akisi devam etmeli.
- Firebase varken queue edilen eventler init sonrasi flush olmali.
- Modul 1 tab secimleri ve modul gecis niyeti gorulmeli.
- Modul 2 yarali yerlestirme ve ilk yardim tamamlama eventleri akmali.
- Modul 3 triyaj etiketleme, ipucu alma, AI soru sorma ve mini test eventleri gorulmeli.
