# Analytics Setup

Bu proje icin analytics katmani `Assets/Scripts/Analytics/` altinda tek bir facade uzerinden calisir. Oyun scriptleri dogrudan Firebase API cagirmaz; tum eventler `TrainingAnalyticsFacade` icinden gecerek ortak parametrelerle gonderilir.

## Mimari

- `AnalyticsRuntimeBootstrap`: ilk sahneden once analytics singleton'ini baslatir.
- `AnalyticsService`: event queue, ortak parametre birlestirme, duplicate suppression ve guvenli fallback davranisini yonetir.
- `SessionTracker`: `installation_id`, `session_id`, sure hesaplari ve quiz/task state bilgisini tutar.
- `FirebaseIntegration`: Firebase varsa reflection ile baglanir; yoksa development/no-op moda duser.
- `AnalyticsTrackers`: aktif sahne disindaki gelecekteki akislar icin takilabilir component seti sunar.

## Event Taksonomisi

- Moduller: `module_entered`, `module_transition_intent`, `module_completed`
- Icerik: `content_opened`, `infographic_opened`, `learning_content_completed`, `video_*`
- Gorev / senaryo: `task_*`, `scenario_started`, `scenario_task_completed`, `scenario_completed`, `critical_action_taken`
- Triyaj: `triage_started`, `victim_interacted`, `victim_tagged`, `triage_dialog_opened`, `help_requested`
- AI / quiz: `ai_panel_opened`, `ai_question_asked`, `quiz_started`, `quiz_answered`, `quiz_completed`, `score_recorded`

## Ortak Parametreler

Her eventte asagidaki alanlar otomatik birlesir:

- `installation_id`
- `session_id`
- `build_version`
- `scene_name`
- `runtime_platform`
- `module_id`
- `module_name`

Modul bazli ek parametreler:

- Modul 1: `content_id`, `content_name`, `target_module_id`, `target_module_name`
- Modul 2: `victim_id`, `victim_name`, `task_id`, `task_name`, `step_index`, `completion_source`
- Modul 3: `scenario_id`, `assigned_triage`, `actual_triage`, `ai_question_type`, `score_percent`

## Sahne Hook Ozeti

- `Modul1`: `ModulSecimController` ve `UI/TabContentController`
- `Modul2_Guvenlik`: `VRGrabbable`, `IlkyardimGlobalMenajer`, `NPCInteraction`, `NPCWorldCanvas`, `FirstAidUIBinder`, `IlkyardimUIMenajeri`
- `Modul3_Triyaj`: `HospitalTriageManager`, `NPCTriageInteractable`, `TriageDialogUI`, `AIManager`

## DebugView Kontrolu

1. Firebase baglandiktan sonra uygulamayi Editor veya cihazda debug modunda acin.
2. Ilgili modul akisini bir kez oynayin.
3. Firebase Analytics DebugView ekraninda event adlarini ve parametrelerini kontrol edin.
4. `ai_question_asked` eventlerinde ham kullanici metni olmadigini, sadece turetilmis `ai_question_type` geldigini dogrulayin.

## Guvenlik Notlari

- `installation_id` anonimdir ve `PlayerPrefs` icinde tutulur.
- Her uygulama acilisinda yeni bir `session_id` uretilir.
- Isim, e-posta, telefon, ham ses verisi ve ham serbest metin analytics payload'ina yazilmaz.
