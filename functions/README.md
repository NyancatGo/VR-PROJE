# VR-PROJE — AI Gateway (`modul3Chat`)

Firebase Cloud Functions backend that proxies doctor-chat requests from the
Unity VR client to one or more LLM providers. The Unity APK ships with **no
provider API keys**; all secrets live in Cloud Functions runtime
configuration.

## 1. Overview

Endpoint: `POST https://us-central1-<project>.cloudfunctions.net/modul3Chat`

### Request body

```json
{
  "participantKey": "baran_atici",
  "sessionId": "sess_2026_05_07_001",
  "moduleId": "module_3",
  "message": "Hasta nasıl?",
  "conversation": [
    { "role": "user", "content": "Selam doktor." },
    { "role": "assistant", "content": "Merhaba, hastayı dinliyorum." }
  ]
}
```

`participantKey` and `sessionId` must match `^[a-zA-Z0-9_\-]{1,128}$`.
`message` length ≤ 4000. `conversation` is sliced to the last 10 entries
server-side.

### Response — success

```json
{
  "ok": true,
  "provider": "deepseek",
  "model": "deepseek-chat",
  "answer": "Hasta stabil; nabız 88, SpO2 %96.",
  "source": "primary"
}
```

### Response — failure

```json
{ "ok": false, "error": "AI_TEMPORARILY_UNAVAILABLE", "answer": "Doktor şu anda yanıt veremiyor." }
```

Possible `error` codes: `BAD_REQUEST` (400), `BODY_TOO_LARGE` (413),
`METHOD_NOT_ALLOWED` (405), `RATE_LIMITED` (429), `AI_DISABLED` (200),
`AI_TEMPORARILY_UNAVAILABLE` (200). The function never returns raw provider
error text or stack traces in the response body.

## 2. Firestore config doc

Path: `ai_config/modul3` (single document).

Example:

```json
{
  "enabled": true,
  "provider": "deepseek",
  "fallback_order": ["groq", "minimax"],
  "model": "deepseek-chat",
  "temperature": 0.4,
  "max_tokens": 500,
  "system_prompt": "You are a calm Turkish-speaking emergency-room doctor coaching a paramedic trainee."
}
```

Field reference:

| Field            | Type     | Default                  | Notes                                              |
|------------------|----------|--------------------------|----------------------------------------------------|
| `enabled`        | boolean  | `true`                   | `false` returns `AI_DISABLED` immediately.         |
| `provider`       | string   | `"deepseek"`             | Primary provider key.                              |
| `fallback_order` | string[] | `[]`                     | Tried in order when primary fails.                 |
| `model`          | string   | provider-specific        | Passed to provider's `model` parameter.            |
| `temperature`    | number   | `0.4`                    | 0.0–1.0.                                           |
| `max_tokens`     | number   | `500`                    | Per-response cap.                                  |
| `system_prompt`  | string   | `undefined`              | Prepended as the first system message.             |

Cached in-memory in each function instance for 60 seconds.

### Manual creation (Firebase Console)

1. Open Firebase Console → Firestore Database.
2. **Start collection** → Collection ID: `ai_config`.
3. Document ID: `modul3`.
4. Add the fields above and **Save**.

## 3. Secrets

Set per-provider secrets (Firebase project must be on the **Blaze** plan):

```
firebase functions:secrets:set DEEPSEEK_API_KEY
firebase functions:secrets:set GROQ_API_KEY
firebase functions:secrets:set MINIMAX_API_KEY
```

The CLI prompts you for each value; nothing is committed to the repo.

> **Important:** rotate the previously-leaked DeepSeek key in the DeepSeek
> dashboard *before* setting it as a Cloud Functions secret. The old value
> has been removed from the repo but should be considered burned.

## 4. Local development

```
cd functions
npm install
npm run build
firebase emulators:start --only functions,firestore
```

Sample curl against the local emulator:

```
curl -X POST http://localhost:5001/<project>/us-central1/modul3Chat \
  -H "Content-Type: application/json" \
  -d '{"participantKey":"baran_atici","sessionId":"sess1","moduleId":"module_3","message":"Hasta nasıl?"}'
```

For the emulator you can stub secrets via a local `.env.local` file inside
`functions/` (gitignored) or by exporting `DEEPSEEK_API_KEY` etc. in the
shell before running `emulators:start`.

## 5. Deploy

```
firebase deploy --only functions:modul3Chat
```

Not run as part of the migration commit — run manually after secrets and the
Firestore config doc are in place.

## 6. Tests

```
npm test
```

Authored test files:

- `src/__tests__/normalize.test.ts` — `<think>`/`<thinking>`/`Reasoning:`
  stripping plus empty + plain pass-through.
- `src/__tests__/fallback.test.ts` — primary-fail → secondary, all-fail,
  and `enabled:false` paths against a mocked provider chain.

## 7. Production hardening (TODO)

- Enable **Firebase App Check** with the **Play Integrity** provider so only
  signed Quest builds can reach the endpoint.
- Enforce **Firebase Auth** (Anonymous is fine for kiosk use) and verify
  `req.headers.authorization` Bearer ID token inside the function.
- Move the rate limit from in-process `Map` to a Firestore counter doc (or
  Memorystore/Redis) — the current implementation is per-instance and
  doesn't hold under autoscaling.
- Convert `modul3Chat` to a callable function (`functions.https.onCall`) to
  get App Check verification for free.
- Add a **Cloud Logging alert** for spikes in
  `[modul3Chat] provider=… failed` warnings or `AI_TEMPORARILY_UNAVAILABLE`
  responses.
- Wire `ResolveSessionIdForGateway()` on the Unity side once
  `TrainingAnalyticsFacade` exposes a session-id accessor.

## 8. Risks / caveats

- **In-memory rate limit** — see hardening note above. Best-effort only.
- **MiniMax provider is a stub** — listing `minimax` in `fallback_order` is
  safe (the dispatcher catches the throw and continues), but it never
  succeeds until `src/providers/minimax.ts` is implemented.
- **`firebase.json` at repo root** — created by this migration. If you
  later add other Firebase products (hosting, firestore rules deploy, etc.)
  merge their config into this file by hand rather than overwriting it.
- **`firestore.rules`** — new minimal file covers only `ai_config/{doc}`.
  If you already had a rules file in another location, merge manually; do
  not blindly deploy this one over an existing ruleset (would lock down
  `katilimcilar/...` analytics writes).
- **No App Check on Unity side** — listed in plan as future work; the
  endpoint is currently reachable by anyone who learns the URL until App
  Check is wired.

## 9. APK rebuild matrix

| Change                                                           | Rebuild APK? |
|------------------------------------------------------------------|--------------|
| Provider swap (`provider` field in `ai_config/modul3`)           | No           |
| Model name change (`model` field)                                | No           |
| `system_prompt` text update                                      | No           |
| `temperature`, `max_tokens` tweaks                               | No           |
| `enabled: true ↔ false`                                          | No           |
| Reordering / extending `fallback_order`                          | No           |
| Rotating any provider API key (`firebase functions:secrets:set`) | No           |
| Gateway URL change (different Cloud Function name or region)     | **Yes**      |
| Response-parser/DTO contract change in `index.ts`                | **Yes**      |
| Switching to a different Firebase project                        | **Yes**      |
| Toggling `useGatewayForDoctorChat` default in Inspector          | **Yes**      |

Anything that changes the Unity client's serialized `gatewayEndpoint` or
the request/response JSON shape requires a rebuild. Pure backend behavior
changes (provider, model, prompt, temperature, fallback) do not.
