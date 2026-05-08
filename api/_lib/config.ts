import admin from 'firebase-admin';

export interface Modul3AiConfig {
  enabled: boolean;
  provider: string;
  fallback_order: string[];
  model?: string;
  temperature: number;
  max_tokens: number;
  system_prompt?: string;
  tts_enabled: boolean;
  tts_provider: string;
  tts_voice_id: string;
  tts_model: string;
  tts_stability: number;
  tts_similarity_boost: number;
  tts_style: number;
  tts_use_speaker_boost: boolean;
}

const DEFAULTS = {
  fallback_order: [] as string[],
  temperature: 0.4,
  max_tokens: 500,
  tts_provider: 'elevenlabs',
  tts_voice_id: 'pNInz6obpgDQGcFmaJgB',
  tts_model: 'eleven_multilingual_v2',
  tts_stability: 0.5,
  tts_similarity_boost: 0.75,
  tts_style: 0,
};

const TTL_MS = 60_000;
let cached: { value: Modul3AiConfig; ts: number } | null = null;

function loadServiceAccount(): admin.ServiceAccount | null {
  const blob = process.env.FIREBASE_SERVICE_ACCOUNT_JSON;
  if (blob && blob.trim().length > 0) {
    let raw = blob.trim();
    // Allow base64 encoded JSON to bypass dashboard escaping issues.
    if (!raw.startsWith('{')) {
      try {
        raw = Buffer.from(raw, 'base64').toString('utf8');
      } catch {
        // fall through
      }
    }
    const parsed = JSON.parse(raw);
    if (typeof parsed.private_key === 'string') {
      parsed.private_key = parsed.private_key.replace(/\\n/g, '\n');
    }
    return parsed as admin.ServiceAccount;
  }

  const projectId = process.env.FIREBASE_PROJECT_ID;
  const clientEmail = process.env.FIREBASE_CLIENT_EMAIL;
  let privateKey = process.env.FIREBASE_PRIVATE_KEY;
  if (projectId && clientEmail && privateKey) {
    privateKey = privateKey.replace(/\\n/g, '\n');
    return { projectId, clientEmail, privateKey } as admin.ServiceAccount;
  }
  return null;
}

function ensureFirestore(): FirebaseFirestore.Firestore {
  if (!admin.apps.length) {
    const sa = loadServiceAccount();
    if (sa) {
      admin.initializeApp({ credential: admin.credential.cert(sa) });
    } else {
      // Last resort: ADC. Will throw on Vercel without env vars set.
      admin.initializeApp();
    }
  }
  return admin.firestore();
}

export async function getModul3AiConfig(force = false): Promise<Modul3AiConfig> {
  const now = Date.now();
  if (!force && cached && now - cached.ts < TTL_MS) {
    return cached.value;
  }

  const db = ensureFirestore();
  const snap = await db.doc('ai_config/modul3').get();
  const raw = (snap.exists ? snap.data() : {}) as Partial<Modul3AiConfig>;

  const value: Modul3AiConfig = {
    enabled: raw.enabled !== false,
    provider: raw.provider || 'deepseek',
    fallback_order: Array.isArray(raw.fallback_order) ? raw.fallback_order : DEFAULTS.fallback_order,
    model: raw.model,
    temperature: typeof raw.temperature === 'number' ? raw.temperature : DEFAULTS.temperature,
    max_tokens: typeof raw.max_tokens === 'number' ? raw.max_tokens : DEFAULTS.max_tokens,
    system_prompt: raw.system_prompt,
    tts_enabled: raw.tts_enabled !== false,
    tts_provider: raw.tts_provider || DEFAULTS.tts_provider,
    tts_voice_id: raw.tts_voice_id || DEFAULTS.tts_voice_id,
    tts_model: raw.tts_model || DEFAULTS.tts_model,
    tts_stability: typeof raw.tts_stability === 'number' ? raw.tts_stability : DEFAULTS.tts_stability,
    tts_similarity_boost: typeof raw.tts_similarity_boost === 'number'
      ? raw.tts_similarity_boost
      : DEFAULTS.tts_similarity_boost,
    tts_style: typeof raw.tts_style === 'number' ? raw.tts_style : DEFAULTS.tts_style,
    tts_use_speaker_boost: raw.tts_use_speaker_boost !== false,
  };

  cached = { value, ts: now };
  return value;
}

export function _resetConfigCache(): void {
  cached = null;
}
