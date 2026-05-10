import * as admin from 'firebase-admin';

export interface Modul3AiConfig {
  enabled: boolean;
  provider: string;
  fallback_order: string[];
  model?: string;
  temperature: number;
  max_tokens: number;
  system_prompt?: string;
}

const DEFAULTS: Omit<Modul3AiConfig, 'enabled' | 'provider'> = {
  fallback_order: [],
  temperature: 0.4,
  max_tokens: 500,
};

const TTL_MS = 60_000;
let cached: { value: Modul3AiConfig; ts: number } | null = null;

function ensureFirestore(): FirebaseFirestore.Firestore {
  if (!admin.apps.length) {
    admin.initializeApp();
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
    enabled: raw.enabled !== false, // default true unless explicitly disabled
    provider: raw.provider || 'deepseek',
    fallback_order: Array.isArray(raw.fallback_order) ? raw.fallback_order : DEFAULTS.fallback_order,
    model: raw.model,
    temperature: typeof raw.temperature === 'number' ? raw.temperature : DEFAULTS.temperature,
    max_tokens: typeof raw.max_tokens === 'number' ? raw.max_tokens : DEFAULTS.max_tokens,
    system_prompt: raw.system_prompt,
  };

  cached = { value, ts: now };
  return value;
}

/** Test-only: clear the in-memory cache. */
export function _resetConfigCache(): void {
  cached = null;
}
