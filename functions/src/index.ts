import * as functions from 'firebase-functions';
import { getModul3AiConfig, Modul3AiConfig } from './config';
import { stripReasoning } from './normalize';
import { ChatProvider, ProviderInput } from './providers/types';
import { deepseekProvider } from './providers/deepseek';
import { groqProvider } from './providers/groq';
import { minimaxProvider } from './providers/minimax';

const PROVIDERS: Record<string, ChatProvider> = {
  deepseek: deepseekProvider,
  groq: groqProvider,
  minimax: minimaxProvider,
};

const ID_REGEX = /^[a-zA-Z0-9_\-]{1,128}$/;
const MAX_BODY = 16_000;
const MAX_MESSAGE = 4000;
const MAX_CONVERSATION = 10;

interface RateLimitBucket {
  count: number;
  resetAt: number;
}
const RATE_WINDOW_MS = 60_000;
const RATE_LIMIT = 10;
const rateBuckets = new Map<string, RateLimitBucket>();

function ipKey(req: functions.https.Request): string {
  const fwd = (req.headers['x-forwarded-for'] as string) || '';
  const first = fwd.split(',')[0]?.trim();
  return first || req.ip || 'unknown';
}

function rateAllow(ip: string): boolean {
  const now = Date.now();
  const b = rateBuckets.get(ip);
  if (!b || b.resetAt <= now) {
    rateBuckets.set(ip, { count: 1, resetAt: now + RATE_WINDOW_MS });
    return true;
  }
  if (b.count >= RATE_LIMIT) {
    return false;
  }
  b.count += 1;
  return true;
}

function buildResponse(ok: boolean, body: Record<string, unknown>) {
  return { ok, ...body };
}

export interface DispatchDeps {
  providers?: Record<string, ChatProvider>;
  loadConfig?: () => Promise<Modul3AiConfig>;
}

export async function dispatchChat(
  payload: { message: string; conversation: Array<{ role: string; content: string }> },
  deps: DispatchDeps = {}
): Promise<{ ok: boolean; provider?: string; model?: string; answer?: string; source?: string; error?: string }> {
  const providers = deps.providers || PROVIDERS;
  const loadConfig = deps.loadConfig || getModul3AiConfig;

  const config = await loadConfig();
  if (!config.enabled) {
    return {
      ok: false,
      error: 'AI_DISABLED',
      answer: 'Doktor bu turda net bir yanıt üretemedi.',
    };
  }

  const chain: string[] = [];
  const seen = new Set<string>();
  for (const name of [config.provider, ...config.fallback_order]) {
    if (!name) continue;
    const k = name.toLowerCase();
    if (seen.has(k)) continue;
    seen.add(k);
    chain.push(k);
  }

  const baseInput: ProviderInput = {
    systemPrompt: config.system_prompt,
    message: payload.message,
    conversation: payload.conversation,
    model: config.model,
    temperature: config.temperature,
    maxTokens: config.max_tokens,
  };

  for (let i = 0; i < chain.length; i++) {
    const name = chain[i];
    const provider = providers[name];
    if (!provider) {
      console.warn(`[modul3Chat] unknown provider in chain: ${name}`);
      continue;
    }
    try {
      const res = await provider.call(baseInput);
      const cleaned = stripReasoning(res.answer);
      if (!cleaned) {
        throw new Error(`${name} produced empty answer after strip`);
      }
      return {
        ok: true,
        provider: name,
        model: res.model,
        answer: cleaned,
        source: i === 0 ? 'primary' : 'fallback',
      };
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'unknown';
      console.warn(`[modul3Chat] provider=${name} failed: ${msg}`);
    }
  }

  return {
    ok: false,
    error: 'AI_TEMPORARILY_UNAVAILABLE',
    answer: 'Doktor şu anda yanıt veremiyor.',
  };
}

export const modul3Chat = functions.https.onRequest(async (req, res): Promise<void> => {
  if (req.method !== 'POST') {
    res.status(405).json(buildResponse(false, { error: 'METHOD_NOT_ALLOWED' }));
    return;
  }

  const rawSize = JSON.stringify(req.body ?? {}).length;
  if (rawSize > MAX_BODY) {
    res.status(413).json(buildResponse(false, { error: 'BODY_TOO_LARGE' }));
    return;
  }

  const ip = ipKey(req);
  if (!rateAllow(ip)) {
    res.status(429).json(buildResponse(false, { error: 'RATE_LIMITED' }));
    return;
  }

  const body = (req.body || {}) as Record<string, unknown>;
  const participantKey = String(body.participantKey || '');
  const sessionId = String(body.sessionId || '');
  const moduleId = String(body.moduleId || '');
  const message = typeof body.message === 'string' ? body.message : '';
  const conversationRaw = Array.isArray(body.conversation) ? body.conversation : [];

  if (!ID_REGEX.test(participantKey) || !ID_REGEX.test(sessionId)) {
    res.status(400).json(buildResponse(false, { error: 'BAD_REQUEST' }));
    return;
  }
  if (!message || message.length > MAX_MESSAGE) {
    res.status(400).json(buildResponse(false, { error: 'BAD_REQUEST' }));
    return;
  }
  if (moduleId && moduleId.length > 64) {
    res.status(400).json(buildResponse(false, { error: 'BAD_REQUEST' }));
    return;
  }

  const conversation = conversationRaw
    .slice(-MAX_CONVERSATION)
    .map((m: any) => ({
      role: typeof m?.role === 'string' ? m.role : '',
      content: typeof m?.content === 'string' ? m.content : '',
    }))
    .filter((m) => m.role && m.content);

  try {
    const result = await dispatchChat({ message, conversation });
    res.status(200).json(result);
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : 'unknown';
    console.error(`[modul3Chat] unexpected error: ${msg}`);
    res.status(200).json({
      ok: false,
      error: 'AI_TEMPORARILY_UNAVAILABLE',
      answer: 'Doktor şu anda yanıt veremiyor.',
    });
  }
});
