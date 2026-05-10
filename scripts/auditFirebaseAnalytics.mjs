#!/usr/bin/env node

/**
 * Firebase analytics audit — non-mutating data quality report.
 *
 * Bu script HİÇBİR Firestore dokümanını değiştirmez. Sadece okur ve
 * console'a rapor basar. Tek veri modu — kaynak etiketi (gerçek/hazır)
 * kontrolleri kaldırıldı; odak data invariant'ları:
 *
 *   • Her katılımcı için koleksiyon başına toplam doküman sayısı
 *   • Eksik metadata (katilimci, oturum_id, kurulum_id, schema_v)
 *   • PRIVACY: AI metni / prompt / cevap saklanmış olabilir mi?
 *     (suspicious-text heuristic — kelime uzunluğu + keyword)
 *
 * Critical error sayısı 0 değilse exit code 1.
 *
 * Usage:
 *   npm run audit:firebase
 *   npm run audit:firebase -- --participant baran_atici
 *   npm run audit:firebase -- --json
 */

import { createRequire } from 'node:module';
import os from 'node:os';
import path from 'node:path';
import fsSync from 'node:fs';

const require = createRequire(import.meta.url);
const admin = require('firebase-admin');

const COLLECTIONS = [
  'oturumlar',
  'moduller',
  'gorevler',
  'testler',
  'ai_etkilesim',
  'triage_sonuc',
  'detaylar',
];

// AI text leak heuristic: bu alan adlarından herhangi biri varsa privacy
// invariant ihlal edilmiş demektir. ai_etkilesim koleksiyonu sadece sayım
// ve özet tutmalı; metin içermemeli.
const SUSPICIOUS_TEXT_FIELDS = [
  'mesaj', 'message', 'icerik', 'content', 'prompt',
  'cevap', 'response', 'answer', 'yanit',
  'conversation', 'gecmis', 'history', 'transcript',
  'soru_metni', 'cevap_metni', 'kullanici_mesaji', 'doktor_cevabi',
];

// Aynı alanın (ai_soru_turu vb) kısa metin içermesi normal. Şüpheli olmak
// için minimum karakter eşiği.
const SUSPICIOUS_MIN_TEXT_LENGTH = 80;

function printHelp() {
  console.log(`
Firebase analytics audit (read-only)

Usage:
  npm run audit:firebase [-- <options>]

Options:
  --participant <key>      Audit only one participant. Repeatable.
  --project-id <id>        Firebase project id for ADC/env credentials.
  --json                   Emit machine-readable JSON instead of text.
  --help                   Show this help.

Output:
  Each participant gets a section with INFO / WARN / ERROR lines.
  Final summary lists total findings; exit code 1 if any ERROR.

Credentials (same as exportFirebaseCsv):
  FIREBASE_SERVICE_ACCOUNT_JSON
  FIREBASE_PROJECT_ID + FIREBASE_CLIENT_EMAIL + FIREBASE_PRIVATE_KEY
  GOOGLE_APPLICATION_CREDENTIALS
`);
}

function parseArgs(argv) {
  const args = { participantKeys: [], projectId: '', json: false };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--help' || a === '-h') args.help = true;
    else if (a === '--participant') args.participantKeys.push(argv[++i]);
    else if (a === '--project-id') args.projectId = argv[++i];
    else if (a === '--json') args.json = true;
    else throw new Error(`Unknown option: ${a}`);
  }
  return args;
}

function parseServiceAccountFromEnv() {
  const blob = process.env.FIREBASE_SERVICE_ACCOUNT_JSON;
  if (blob && blob.trim()) {
    let raw = blob.trim();
    if (!raw.startsWith('{')) {
      raw = Buffer.from(raw, 'base64').toString('utf8');
    }
    const parsed = JSON.parse(raw);
    if (typeof parsed.private_key === 'string') {
      parsed.private_key = parsed.private_key.replace(/\\n/g, '\n');
    }
    return parsed;
  }
  const projectId = process.env.FIREBASE_PROJECT_ID;
  const clientEmail = process.env.FIREBASE_CLIENT_EMAIL;
  let privateKey = process.env.FIREBASE_PRIVATE_KEY;
  if (projectId && clientEmail && privateKey) {
    privateKey = privateKey.replace(/\\n/g, '\n');
    return { projectId, clientEmail, privateKey };
  }
  const credFile = process.env.GOOGLE_APPLICATION_CREDENTIALS ||
    path.join(os.homedir(), '.firebase-keys', 'project-fa15e-service-account.json');
  if (credFile && fsSync.existsSync(credFile)) {
    const parsed = JSON.parse(fsSync.readFileSync(credFile, 'utf8'));
    if (typeof parsed.private_key === 'string') {
      parsed.private_key = parsed.private_key.replace(/\\n/g, '\n');
    }
    return parsed;
  }
  return null;
}

function initializeFirebase(projectId) {
  if (admin.apps.length > 0) return admin.firestore();
  const sa = parseServiceAccountFromEnv();
  if (sa) {
    admin.initializeApp({
      credential: admin.credential.cert(sa),
      projectId: sa.project_id || sa.projectId || projectId || undefined,
    });
  } else {
    admin.initializeApp({
      credential: admin.credential.applicationDefault(),
      projectId: projectId || process.env.FIREBASE_PROJECT_ID || undefined,
    });
  }
  return admin.firestore();
}

function classifyTextFieldLeak(data) {
  // Returns array of suspicious findings: { field, sample, length }.
  const findings = [];
  if (!data || typeof data !== 'object') return findings;
  for (const [key, value] of Object.entries(data)) {
    const lowerKey = key.toLowerCase();
    const isSuspectName = SUSPICIOUS_TEXT_FIELDS.some((s) => lowerKey === s || lowerKey.includes(s));
    if (!isSuspectName) continue;
    if (typeof value === 'string' && value.length >= SUSPICIOUS_MIN_TEXT_LENGTH) {
      findings.push({
        field: key,
        length: value.length,
        sample: value.slice(0, 60).replace(/\s+/g, ' ') + '…',
      });
    } else if (typeof value === 'object' && value !== null) {
      findings.push({ field: key, length: -1, sample: '[object/array]' });
    }
  }
  return findings;
}

async function auditParticipant(participantRef) {
  const result = {
    participantKey: participantRef.id,
    counts: {},
    findings: [], // { level: 'INFO'|'WARN'|'ERROR', message }
  };

  const profileSnap = await participantRef.get();
  if (!profileSnap.exists) {
    result.findings.push({ level: 'ERROR', message: `Katılımcı dokümanı yok (${participantRef.id})` });
    return result;
  }
  const profile = profileSnap.data();

  // Ana profil temel alanları
  for (const requiredField of ['kurulum_id', 'baslangic']) {
    if (!profile[requiredField]) {
      result.findings.push({
        level: 'WARN',
        message: `Katılımcı dokümanında "${requiredField}" eksik`,
      });
    }
  }

  // Her koleksiyon için detaylı denetim — tek veri modu, gerçek/hazır
  // ayrımı yok. Sadece total sayım, metadata ve privacy odaklı kontrol.
  for (const collectionName of COLLECTIONS) {
    const snap = await participantRef.collection(collectionName).get();
    const missingMetadata = { katilimci: 0, oturum_id: 0, kurulum_id: 0, schema_v: 0 };
    const leaks = [];

    for (const doc of snap.docs) {
      const data = doc.data() || {};

      // Metadata eksiklikleri
      for (const key of Object.keys(missingMetadata)) {
        const v = data[key];
        if (v === undefined || v === null || v === '' ||
            v === 'unknown_participant' || v === 'unknown_session' || v === 'unknown_installation') {
          missingMetadata[key] += 1;
        }
      }

      // Privacy: AI metin sızıntısı kontrolü (her koleksiyonda)
      const textFindings = classifyTextFieldLeak(data);
      for (const f of textFindings) {
        leaks.push({ docId: doc.id, ...f });
      }
    }

    result.counts[collectionName] = { total: snap.size };

    if (snap.size === 0) {
      result.findings.push({
        level: 'INFO',
        message: `${collectionName}: 0 doküman`,
      });
    }

    // Metadata uyarıları
    for (const [key, count] of Object.entries(missingMetadata)) {
      if (count > 0) {
        const level = (key === 'katilimci' || key === 'kurulum_id') ? 'ERROR' : 'WARN';
        result.findings.push({
          level,
          message: `${collectionName}: ${count} dokümanda "${key}" eksik veya unknown`,
        });
      }
    }

    // Privacy ihlali bulgularını ERROR olarak işaretle
    for (const leak of leaks) {
      result.findings.push({
        level: 'ERROR',
        message: `PRIVACY: ${collectionName}/${leak.docId} dokümanında "${leak.field}" alanı şüpheli metin içeriyor (${leak.length === -1 ? 'object/array' : leak.length + ' char'}; örnek: "${leak.sample}")`,
      });
    }
  }

  return result;
}

function printReport(participantResults, jsonMode) {
  if (jsonMode) {
    console.log(JSON.stringify(participantResults, null, 2));
    return;
  }

  let totalErrors = 0;
  let totalWarns = 0;

  console.log('═══════════════════════════════════════════════════════');
  console.log('  Firebase Analytics Audit — non-mutating data report');
  console.log(`  Generated: ${new Date().toISOString()}`);
  console.log('═══════════════════════════════════════════════════════\n');

  for (const r of participantResults) {
    console.log(`──────────  ${r.participantKey}  ──────────`);
    const countParts = Object.entries(r.counts).map(
      ([col, c]) => `${col}: ${c.total}`,
    );
    console.log(`Sayımlar: ${countParts.join(' | ')}`);

    const errors = r.findings.filter((f) => f.level === 'ERROR');
    const warns = r.findings.filter((f) => f.level === 'WARN');
    const infos = r.findings.filter((f) => f.level === 'INFO');

    for (const f of errors) console.log(`  [ERROR] ${f.message}`);
    for (const f of warns) console.log(`  [WARN]  ${f.message}`);
    for (const f of infos) console.log(`  [INFO]  ${f.message}`);
    if (r.findings.length === 0) console.log('  (clean — bulgu yok)');
    console.log('');

    totalErrors += errors.length;
    totalWarns += warns.length;
  }

  console.log('═══════════════════════════════════════════════════════');
  console.log(`Toplam: ${participantResults.length} katılımcı, ${totalErrors} ERROR, ${totalWarns} WARN`);
  console.log('═══════════════════════════════════════════════════════');

  return totalErrors;
}

async function main() {
  const args = parseArgs(process.argv.slice(2));
  if (args.help) {
    printHelp();
    return;
  }

  const db = initializeFirebase(args.projectId);
  const root = db.collection('katilimcilar');
  let refs;
  if (args.participantKeys.length > 0) {
    refs = args.participantKeys.map((k) => root.doc(k));
  } else {
    const snap = await root.get();
    refs = snap.docs.map((d) => d.ref);
  }

  const results = [];
  for (const ref of refs) {
    results.push(await auditParticipant(ref));
  }

  const totalErrors = printReport(results, args.json);
  if (totalErrors > 0) {
    process.exitCode = 1;
  }
}

main().catch((error) => {
  console.error(`[audit] failed: ${error.message}`);
  process.exitCode = 2;
});
