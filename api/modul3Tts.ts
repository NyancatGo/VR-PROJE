import type { VercelRequest, VercelResponse } from '@vercel/node';
import { getModul3AiConfig } from './_lib/config';

const ID_REGEX = /^[a-zA-Z0-9_\-]{1,128}$/;
const MAX_BODY = 8_000;
const MAX_TTS_TEXT = 1_200;

const RATE_WINDOW_MS = 60_000;
const RATE_LIMIT = 20;
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

function clamp01(value: number): number {
  if (!Number.isFinite(value)) return 0;
  return Math.max(0, Math.min(1, value));
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
  const text = typeof body.text === 'string' ? body.text.trim() : '';

  if (!ID_REGEX.test(participantKey) || !ID_REGEX.test(sessionId)) {
    jsonError(res, 400, 'BAD_REQUEST');
    return;
  }
  if (moduleId && moduleId.length > 64) {
    jsonError(res, 400, 'BAD_REQUEST');
    return;
  }
  if (!text || text.length > MAX_TTS_TEXT) {
    jsonError(res, 400, 'BAD_REQUEST');
    return;
  }

  const config = await getModul3AiConfig();
  if (!config.tts_enabled) {
    jsonError(res, 200, 'TTS_DISABLED');
    return;
  }

  const provider = (config.tts_provider || '').trim().toLowerCase();
  if (provider !== 'elevenlabs') {
    jsonError(res, 501, 'TTS_PROVIDER_UNSUPPORTED');
    return;
  }

  const apiKey = process.env.ELEVENLABS_API_KEY;
  if (!apiKey || !apiKey.trim()) {
    console.error('[modul3Tts] ELEVENLABS_API_KEY missing');
    jsonError(res, 500, 'TTS_SECRET_MISSING');
    return;
  }

  const voiceId = (config.tts_voice_id || '').trim();
  if (!voiceId) {
    jsonError(res, 500, 'TTS_VOICE_MISSING');
    return;
  }

  const url = `https://api.elevenlabs.io/v1/text-to-speech/${encodeURIComponent(voiceId)}`;
  const payload = {
    text,
    model_id: config.tts_model || 'eleven_multilingual_v2',
    voice_settings: {
      stability: clamp01(config.tts_stability),
      similarity_boost: clamp01(config.tts_similarity_boost),
      style: clamp01(config.tts_style),
      use_speaker_boost: config.tts_use_speaker_boost,
    },
  };

  const elevenRes = await fetch(url, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'audio/mpeg',
      'xi-api-key': apiKey,
    },
    body: JSON.stringify(payload),
  });

  if (!elevenRes.ok) {
    const detail = await elevenRes.text().catch(() => '');
    console.warn(`[modul3Tts] ElevenLabs failed status=${elevenRes.status} detailLen=${detail.length}`);
    jsonError(res, 502, 'TTS_PROVIDER_FAILED');
    return;
  }

  const audio = Buffer.from(await elevenRes.arrayBuffer());
  if (!audio.length) {
    jsonError(res, 502, 'TTS_EMPTY_AUDIO');
    return;
  }

  res.setHeader('Content-Type', 'audio/mpeg');
  res.setHeader('Cache-Control', 'no-store');
  res.setHeader('X-TTS-Provider', 'elevenlabs');
  res.setHeader('X-TTS-Model', payload.model_id);
  res.setHeader('X-TTS-Voice', voiceId);
  res.status(200).send(audio);
}
