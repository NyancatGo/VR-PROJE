import type { VercelRequest, VercelResponse } from '@vercel/node';
import { getModul3AiConfig } from './_lib/config';

const ID_REGEX = /^[a-zA-Z0-9_\-]{1,128}$/;
const MAX_BODY = 1_200_000;
const MAX_AUDIO_BYTES = 700_000;
const RATE_WINDOW_MS = 60_000;
const RATE_LIMIT = 12;
const GROQ_TRANSCRIPTION_URL = 'https://api.groq.com/openai/v1/audio/transcriptions';

const rateBuckets = new Map<string, { count: number; resetAt: number }>();

function ipKey(req: VercelRequest): string {
  const fwd = (req.headers['x-forwarded-for'] as string) || '';
  const first = fwd.split(',')[0]?.trim();
  return first || (req.socket && (req.socket as any).remoteAddress) || 'unknown';
}

function rateAllow(ip: string): boolean {
  const now = Date.now();
  const bucket = rateBuckets.get(ip);
  if (!bucket || bucket.resetAt <= now) {
    rateBuckets.set(ip, { count: 1, resetAt: now + RATE_WINDOW_MS });
    return true;
  }
  if (bucket.count >= RATE_LIMIT) return false;
  bucket.count += 1;
  return true;
}

function jsonError(res: VercelResponse, status: number, error: string) {
  res.status(status).json({ ok: false, error });
}

function cleanProviderName(value: string | undefined): string {
  return (value || '').trim().toLowerCase();
}

export default async function handler(req: VercelRequest, res: VercelResponse) {
  if (req.method !== 'POST') {
    jsonError(res, 405, 'METHOD_NOT_ALLOWED');
    return;
  }

  const rawSize = JSON.stringify(req.body ?? {}).length;
  if (rawSize > MAX_BODY) {
    jsonError(res, 413, 'BODY_TOO_LARGE');
    return;
  }

  const ip = ipKey(req);
  if (!rateAllow(ip)) {
    jsonError(res, 429, 'RATE_LIMITED');
    return;
  }

  const body = (req.body || {}) as Record<string, unknown>;
  const participantKey = String(body.participantKey || 'unknown_participant');
  const sessionId = String(body.sessionId || 'unknown_session');
  const moduleId = String(body.moduleId || '');
  const audioBase64 = typeof body.audioBase64 === 'string' ? body.audioBase64 : '';

  if (!ID_REGEX.test(participantKey) || !ID_REGEX.test(sessionId)) {
    jsonError(res, 400, 'BAD_REQUEST');
    return;
  }
  if (moduleId && moduleId.length > 64) {
    jsonError(res, 400, 'BAD_REQUEST');
    return;
  }
  if (!audioBase64) {
    jsonError(res, 400, 'AUDIO_MISSING');
    return;
  }

  const audio = Buffer.from(audioBase64, 'base64');
  if (!audio.length || audio.length > MAX_AUDIO_BYTES) {
    jsonError(res, 413, 'AUDIO_INVALID_OR_TOO_LARGE');
    return;
  }

  const config = await getModul3AiConfig();
  if (!config.stt_enabled) {
    jsonError(res, 200, 'STT_DISABLED');
    return;
  }

  const provider = cleanProviderName(config.stt_provider);
  if (provider !== 'groq') {
    jsonError(res, 501, 'STT_PROVIDER_UNSUPPORTED');
    return;
  }

  const apiKey = process.env.GROQ_API_KEY;
  if (!apiKey || !apiKey.trim()) {
    console.error('[modul3Stt] GROQ_API_KEY missing');
    jsonError(res, 500, 'STT_SECRET_MISSING');
    return;
  }

  const requestedModel = typeof body.model === 'string' ? body.model.trim() : '';
  const requestedLanguage = typeof body.language === 'string' ? body.language.trim() : '';
  const model = requestedModel || config.stt_model || 'whisper-large-v3-turbo';
  const language = requestedLanguage || config.stt_language || 'tr';

  const form = new FormData();
  const wavBlob = new Blob([new Uint8Array(audio)], { type: 'audio/wav' });
  form.append('file', wavBlob, 'doctor_chat_mic.wav');
  form.append('model', model);
  if (language) {
    form.append('language', language);
  }

  const groqRes = await fetch(GROQ_TRANSCRIPTION_URL, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${apiKey}`,
      Accept: 'application/json',
    },
    body: form,
  });

  const responseText = await groqRes.text().catch(() => '');
  if (!groqRes.ok) {
    console.warn(`[modul3Stt] Groq failed status=${groqRes.status} detailLen=${responseText.length}`);
    jsonError(res, 502, 'STT_PROVIDER_FAILED');
    return;
  }

  let parsed: { text?: string } = {};
  try {
    parsed = responseText ? JSON.parse(responseText) : {};
  } catch {
    parsed = {};
  }

  const text = typeof parsed.text === 'string' ? parsed.text.trim() : '';
  if (!text) {
    jsonError(res, 502, 'STT_EMPTY_TRANSCRIPT');
    return;
  }

  res.setHeader('Cache-Control', 'no-store');
  res.status(200).json({
    ok: true,
    provider: 'groq',
    model,
    text,
  });
}
