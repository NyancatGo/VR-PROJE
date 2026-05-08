# Vercel AI Gateway — Modul3 Doctor Chat

Bu dosya Unity Quest APK'sının Modul3 doktor chat sistemini bir Vercel serverless backend üzerinden çalıştırma kurulumudur. Firebase Cloud Functions / Blaze planı **kullanılmaz**.

```
Quest APK / Unity (AIManager)
   POST  →  https://<vercel-project>.vercel.app/api/modul3Chat
                 │
                 ├─ Firestore'dan ai_config/modul3 okur (sadece konfig — API key YOK)
                 ├─ DeepSeek (primary)  → fail ise fallback
                 └─ Groq    (fallback)
```

API key'ler **sadece** Vercel Environment Variables içinde tutulur. APK içinde, Firestore içinde, repo içinde key yoktur.

---

## 1) Dosya Yapısı

| Dosya | Görev |
|---|---|
| `api/modul3Chat.ts` | Vercel default export handler (POST endpoint) |
| `api/_lib/config.ts` | Firestore'dan `ai_config/modul3` okuyan + 60sn cache yapan modül |
| `api/_lib/normalize.ts` | `<think>…</think>` ve "Reasoning:" bloklarını cevaptan temizler |
| `api/_lib/providers/types.ts` | Provider arayüzü |
| `api/_lib/providers/deepseek.ts` | DeepSeek provider |
| `api/_lib/providers/groq.ts` | Groq provider (fallback) |
| `package.json` | Vercel build için (firebase-admin, @vercel/node) |
| `tsconfig.json` | TS ayarları (Unity dosyalarını exclude eder) |
| `vercel.json` | Function memory/timeout |
| `.vercelignore` | Unity Assets/Library/Temp deploy edilmesin |

> `functions/` klasörü **legacy / not used** — silinmedi ama Vercel akışı bunu kullanmıyor. İleride istersen kaldırabilirsin.

---

## 2) Vercel Environment Variables

Vercel dashboard → Project Settings → Environment Variables (Production + Preview):

| Key | Açıklama |
|---|---|
| `DEEPSEEK_API_KEY` | DeepSeek API key (sk-…) |
| `GROQ_API_KEY` | Groq API key |
| `FIREBASE_SERVICE_ACCOUNT_JSON` | Firebase service account JSON. Tek satır JSON yapıştır **veya** base64 encode et. Kod ikisini de algılar. |

**Alternatif** (yukarıdaki tek değişken yerine):

| Key | Açıklama |
|---|---|
| `FIREBASE_PROJECT_ID` | Firebase project id |
| `FIREBASE_CLIENT_EMAIL` | Service account email |
| `FIREBASE_PRIVATE_KEY` | Private key (newline'lar `\n` literal olarak kalsın — kod `\n`'i gerçek newline'a çevirir) |

### Service Account Hazırlama
1. Firebase Console → Project Settings → Service accounts → Generate new private key → JSON indir.
2. JSON içeriğini **tek satır** yap veya `base64`'le:
   ```powershell
   [Convert]::ToBase64String([IO.File]::ReadAllBytes("service-account.json"))
   ```
3. Çıkan string'i `FIREBASE_SERVICE_ACCOUNT_JSON`'a yapıştır.

> Service account dosyasını **repo'ya commit etme.** `.gitignore`'da olduğundan emin ol.

---

## 3) Firestore Document

Firestore'da `ai_config/modul3` adında **tek bir doküman** oluştur:

```json
{
  "enabled": true,
  "provider": "deepseek",
  "model": "deepseek-chat",
  "fallback_order": ["groq"],
  "temperature": 0.4,
  "max_tokens": 500,
  "system_prompt": "Sen bir triyaj eğitim simülasyonunda görev yapan uzman doktorsun. Kısa, net ve öğrenciye rehber olacak şekilde cevap ver."
}
```

API key bu dokümanda **olmaz**. APK uzaktan değiştirmek istediğin her şey burada: provider, model, prompt, temperature, max_tokens, fallback_order, enabled.

Backend bu dokümanı 60 saniye cache'ler. Değişiklik en geç 60sn sonra etkili olur.

---

## 4) Deploy Adımları

```bash
# 1. Vercel CLI (bir kerelik)
npm i -g vercel

# 2. Repo kökünde login + link
vercel login
vercel link

# 3. Env var'ları yukarıdaki listeden Vercel dashboard'da gir.
#    (CLI ile: vercel env add DEEPSEEK_API_KEY production)

# 4. Deploy
vercel --prod
```

Deploy sonrası Vercel sana `https://<project>.vercel.app/api/modul3Chat` URL'ini verir.

---

## 5) Unity Tarafında Yapılacaklar

`Assets/Scripts/TriyajModul3/AIManager.cs` zaten gateway POST destekliyor. Sadece Inspector'dan:

- **Use Gateway For Doctor Chat**: ✅ true (default zaten true)
- **Gateway Endpoint**: `https://<project>.vercel.app/api/modul3Chat`
- **API Key alanı**: boş bırak.

APK'yı bu ayarla bir kez al. Sonrasında prompt/model/provider değişiklikleri için APK rebuild gerekmez.

---

## 6) Smoke Test Örnekleri

Hepsi `https://<project>.vercel.app/api/modul3Chat`'e POST. PowerShell örneği:

```powershell
$body = @{
  participantKey = "P001"
  sessionId      = "S20260508a"
  moduleId       = "module_3"
  message        = "Hasta bilinçsiz, nabız var, solunum yüzeyel. Triyaj?"
  conversation   = @()
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri "https://<project>.vercel.app/api/modul3Chat" -ContentType "application/json" -Body $body
```

| Senaryo | Beklenen |
|---|---|
| Geçerli body | `200 { ok: true, provider: "deepseek", answer: "…", source: "primary" }` |
| `enabled=false` Firestore'da | `200 { ok: false, error: "AI_DISABLED", answer: "Doktor bu turda…" }` |
| DeepSeek down (örn. yanlış key) | `200 { ok: true, provider: "groq", source: "fallback" }` |
| Tüm key'ler eksik | `200 { ok: false, error: "AI_TEMPORARILY_UNAVAILABLE", answer: "Doktor şu anda…" }` |
| `message: ""` | `400 { ok: false, error: "BAD_REQUEST" }` |
| `participantKey` formatı bozuk | `400 { ok: false, error: "BAD_REQUEST" }` |
| GET request | `405 { ok: false, error: "METHOD_NOT_ALLOWED" }` |
| 60sn'de >10 istek (aynı IP) | `429 { ok: false, error: "RATE_LIMITED" }` |

---

## 7) Rapor

### 1. Ne değişti?
- Yeni Vercel API route eklendi: `api/modul3Chat.ts` + `api/_lib/*`.
- Root'a `package.json`, `tsconfig.json`, `vercel.json`, `.vercelignore` eklendi.
- Bu README eklendi.
- Unity tarafında **tek bir tooltip stringi** Vercel'i yansıtacak şekilde güncellendi (davranış değişmedi). Logic'e dokunulmadı.
- `functions/` klasörü silinmedi (destructive olmaması için), Vercel kullanmıyor.

### 2. Neden değişti?
- Firebase Spark planı + Cloud Functions Blaze gerektiriyordu. Vercel free tier ile bu mimari çalışır.
- API key'ler artık APK'da değil Vercel env var'da. Anahtar bozulursa APK rebuild gerekmez.
- Prompt/model/provider Firestore `ai_config/modul3`'te. APK'da hardcoded değil.

### 3. Unity tarafında ne yapmam gerekiyor?
- `AIManager` Inspector → `Gateway Endpoint` = `https://<project>.vercel.app/api/modul3Chat`
- `Use Gateway For Doctor Chat` = true
- API Key alanı boş kalmalı.
- Build → APK → gözlüğe yükle.

### 4. Vercel'de hangi env var'lar?
- `DEEPSEEK_API_KEY`
- `GROQ_API_KEY`
- `FIREBASE_SERVICE_ACCOUNT_JSON` (tek değişkenli yöntem) **veya** `FIREBASE_PROJECT_ID` + `FIREBASE_CLIENT_EMAIL` + `FIREBASE_PRIVATE_KEY`.

### 5. Firebase'de hangi doküman?
- Collection: `ai_config`
- Document id: `modul3`
- Şema yukarıdaki "Firestore Document" başlığında. **API key kesinlikle koyma.**

### 6. Deploy adımları
1. Service account JSON'u indir, base64'le, Vercel env var olarak ekle.
2. DeepSeek & Groq key'leri Vercel env var olarak ekle.
3. `vercel link` + `vercel --prod`.
4. Firestore'da `ai_config/modul3` dokümanını oluştur.
5. Smoke test (yukarıdaki tablo).
6. Unity Inspector'da endpoint'i gir, APK'yı bir kere build et.

### 7. Kalan riskler
- **In-memory rate limit**: Vercel cold start sonrası bucket sıfırlanır. Gerçek koruma değil — sadece self-DoS koruması. Üretimde gerçek kullanım var ise Upstash/Redis tabanlı rate limit eklenmeli.
- **`ai_config/modul3` cache 60sn**: değişiklik anında etkili olmaz, en geç 60sn sonra. Kabul edilebilir bir trade-off.
- **Firestore okuma maliyeti**: Spark plan günlük 50K okuma içerir; cache TTL bu sınıra çarpmamak için var. Cold start başına 1 okuma.
- **Service account scope**: indirdiğin SA Firestore admin yetkili. İstersen sadece `ai_config/modul3` okuyacak custom IAM rolü tanımla — **şart değil**, küçük proje için fazla.
- **Groq model adı**: `llama-3.1-70b-versatile` — Groq tarafında deprecate olabilir. Firestore'dan `model` override ederek anında değiştirebilirsin.
- **functions/ klasörü hâlâ duruyor**: kullanılmıyor ama dependencies (firebase-functions) `npm install` zamanı çatışmaz çünkü ayrı `functions/package.json` içinde. Yine de istersen ileride sil.
- **MiniMax provider yok**: chat için stabil endpoint kontratı doğrulanmadığı için Vercel tarafına taşınmadı (legacy `functions/` içinde stub kaldı). Modül 3 için DeepSeek+Groq yeterli.
- **TypeScript build**: Vercel Node 20 runtime built-in `fetch` kullanır, `node-fetch` bağımlılığı yok. `engines.node >= 20` zorunlu.

### 8. APK yeniden build almadan neleri değiştirebilirim?
Firestore `ai_config/modul3` üzerinden:
- `enabled` (AI sistemini tamamen kapat)
- `provider` (deepseek ↔ groq)
- `model` (örn. `deepseek-chat` → `deepseek-reasoner`)
- `fallback_order` (sıralama, ekleme/çıkarma)
- `temperature`
- `max_tokens`
- `system_prompt` (doktorun kişiliği/talimatları)

Vercel env var'ından:
- `DEEPSEEK_API_KEY` rotation
- `GROQ_API_KEY` rotation
- (env değişikliği sonrası Vercel "Redeploy" — kod değişmiyor, ~30sn)

APK'da değişen tek şey **endpoint URL'i**. Bu Inspector'da bir kez girildiğinde sabit kalır.
