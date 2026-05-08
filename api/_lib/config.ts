import admin from 'firebase-admin';

export interface Modul3AiConfig {
  enabled: boolean;
  provider: string;
  fallback_order: string[];
  model?: string;
  temperature: number;
  max_tokens: number;
  system_prompt?: string;
}

const DEFAULTS = {
  fallback_order: [] as string[],
  temperature: 0.4,
  max_tokens: 500,
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
  };

  cached = { value, ts: now };
  return value;
}

export function _resetConfigCache(): void {
  cached = null;
}
