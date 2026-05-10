#!/usr/bin/env node

import { createRequire } from 'node:module';
import fs from 'node:fs/promises';
import fsSync from 'node:fs';
import os from 'node:os';
import path from 'node:path';

const require = createRequire(import.meta.url);
const admin = require('firebase-admin');

// Subcollections (under katilimcilar/{participantKey}) we know how to export.
const DEFAULT_COLLECTIONS = [
  'oturumlar',
  'moduller',
  'gorevler',
  'testler',
  'ai_etkilesim',
  'triage_sonuc',
  'detaylar',
];

// Per-collection ordered column list (Firestore field names). Anything not
// listed gets appended after these in the order Firestore returns them.
// Tek veri modu: kaynak etiketi kolonları (veri_turu, kayit_tipi,
// is_placeholder, baseline_profile_*) kaldırıldı.
const COLLECTION_PREFERRED_COLUMNS = {
  __all__: [
    'participant_key', 'participant_name', 'collection', 'document_id',
    'schema_v',
    'kurulum_id', 'oturum_id', 'katilimci',
    'modul_id', 'modul_adi', 'senaryo_id', 'senaryo_adi',
    'gorev_id', 'gorev_adi', 'hedef_id', 'hedef_adi',
    'hasta_id', 'hasta_adi', 'atanan', 'sonuc', 'dogru',
    'test_id', 'test_adi', 'toplam_soru', 'cevaplanan', 'dogru_adet',
    'panel_id', 'panel_adi', 'ai_soru_turu',
    'ai_panel_acildi_mi', 'ai_soru_soruldu_mu', 'ai_soru_adedi', 'ai_basarili_mi', 'ai_hata_kodu',
    'tamamlandi', 'basarili', 'gorev_ilerleme', 'tamamlanan', 'toplam',
    'skor', 'skor_yuzde', 'sure', 'sure_dksn',
    'detay_sira', 'olay',
    'baslangic', 'son_gorulme', 'yazildi', 'tarih_tr', 'son_guncelleme',
  ],
  oturumlar: [
    'participant_key', 'participant_name', 'document_id',
    'oturum_id', 'kurulum_id', 'baslangic', 'son_gorulme', 'sure', 'sure_dksn',
    'tarih_tr',
  ],
  moduller: [
    'participant_key', 'participant_name', 'document_id',
    'modul_id', 'modul_adi', 'tamamlandi', 'basarili',
    'gorev_ilerleme', 'tamamlanan', 'toplam', 'sure', 'sure_dksn',
    'skor', 'skor_yuzde', 'oturum_id', 'yazildi', 'tarih_tr', 'son_guncelleme',
  ],
  gorevler: [
    'participant_key', 'participant_name', 'document_id',
    'modul_id', 'modul_adi', 'gorev_id', 'gorev_adi',
    'hedef_id', 'hedef_adi', 'hasta_id', 'hasta_adi',
    'tamamlandi', 'basarili', 'sure', 'sure_dksn', 'skor', 'skor_yuzde',
    'oturum_id', 'yazildi', 'tarih_tr', 'son_guncelleme',
  ],
  testler: [
    'participant_key', 'participant_name', 'document_id',
    'modul_id', 'modul_adi', 'test_id', 'test_adi',
    'toplam_soru', 'cevaplanan', 'dogru_adet',
    'soru_idx', 'secilen', 'dogru_secenek', 'dogru',
    'skor', 'skor_yuzde', 'sure', 'sure_dksn', 'oturum_id', 'yazildi', 'tarih_tr', 'son_guncelleme',
  ],
  triage_sonuc: [
    'participant_key', 'participant_name', 'document_id',
    'modul_id', 'modul_adi', 'senaryo_id', 'senaryo_adi',
    'hasta_id', 'hasta_adi', 'atanan', 'sonuc', 'dogru',
    'skor', 'skor_yuzde', 'sure', 'sure_dksn', 'oturum_id', 'yazildi', 'tarih_tr', 'son_guncelleme',
  ],
  ai_etkilesim: [
    'participant_key', 'participant_name', 'document_id',
    'modul_id', 'modul_adi', 'panel_id', 'panel_adi', 'ai_soru_turu',
    'ai_panel_acildi_mi', 'ai_soru_soruldu_mu', 'ai_soru_adedi',
    'ai_basarili_mi', 'ai_hata_kodu',
    'sure', 'sure_dksn', 'oturum_id', 'yazildi', 'tarih_tr', 'son_guncelleme',
  ],
  detaylar: [
    'participant_key', 'participant_name', 'document_id',
    'detay_sira', 'olay', 'modul_id', 'modul_adi',
    'gorev_id', 'gorev_adi', 'hasta_id', 'hasta_adi',
    'sure', 'sure_dksn', 'skor', 'oturum_id', 'yazildi', 'tarih_tr', 'son_guncelleme',
  ],
  katilimcilar: [
    'participant_key', 'participant_name', 'document_id',
    'isim', 'soyisim', 'kurulum_id', 'baslangic', 'son_gorulme',
  ],
};

// Map Firestore field name → human-readable Turkish CSV header.
const HEADER_LABELS = {
  participant_key: 'Katılımcı Anahtarı',
  participant_name: 'Katılımcı Adı',
  collection: 'Koleksiyon',
  document_id: 'Belge ID',

  schema_v: 'Şema Sürümü',

  kurulum_id: 'Kurulum ID',
  oturum_id: 'Oturum ID',
  katilimci: 'Katılımcı',
  isim: 'Ad',
  soyisim: 'Soyad',

  modul_id: 'Modül ID',
  modul_adi: 'Modül',
  senaryo_id: 'Senaryo ID',
  senaryo_adi: 'Senaryo',

  gorev_id: 'Görev ID',
  gorev_adi: 'Görev',
  hedef_id: 'Hedef ID',
  hedef_adi: 'Hedef',

  hasta_id: 'Hasta ID',
  hasta_adi: 'Hasta',
  atanan: 'Seçilen Triyaj',
  sonuc: 'Doğru Triyaj',
  dogru: 'Doğru mu?',

  test_id: 'Test ID',
  test_adi: 'Test',
  toplam_soru: 'Toplam Soru',
  cevaplanan: 'Cevaplanan',
  dogru_adet: 'Doğru Sayısı',
  soru_idx: 'Soru Sırası',
  secilen: 'Seçilen Şık',
  dogru_secenek: 'Doğru Şık',

  panel_id: 'AI Panel ID',
  panel_adi: 'AI Panel',
  ai_soru_turu: 'AI Soru Türü',
  ai_panel_acildi_mi: 'AI Panel Açıldı mı?',
  ai_soru_soruldu_mu: 'AI Soru Soruldu mu?',
  ai_soru_adedi: 'AI Soru Adedi',
  ai_basarili_mi: 'AI Başarılı mı?',
  ai_hata_kodu: 'AI Hata Kodu',

  tamamlandi: 'Tamamlandı mı?',
  basarili: 'Başarılı mı?',
  gorev_ilerleme: 'Görev İlerleme (%)',
  tamamlanan: 'Tamamlanan',
  toplam: 'Toplam',
  skor: 'Skor',
  skor_yuzde: 'Skor (%)',
  sure: 'Süre (sn)',
  sure_dksn: 'Süre (dk:sn)',

  detay_sira: 'Detay Sırası',
  olay: 'Olay',

  baslangic: 'Başlangıç',
  son_gorulme: 'Son Görülme',
  yazildi: 'Tarih (UTC)',
  tarih_tr: 'Tarih (TR)',
  son_guncelleme: 'Son Güncelleme (UTC)',

  // triage_sonuc extras
  tamamlanma_yuzde: 'Tamamlanma (%)',
  aktif_karar_sure: 'Aktif Karar Süresi (sn)',
  ortalama_karar_sure: 'Ortalama Karar Süresi (sn)',
  dakika_hasta: 'Dakikada Hasta',
  eksik_triage_adet: 'Eksik Triyaj',
  fazla_triage_adet: 'Fazla Triyaj',
  kritik_uyumsuz_adet: 'Kritik Uyumsuzluk',
  en_uzun_dogru_seri: 'En Uzun Doğru Seri',
};

function humanizeHeader(field) {
  if (HEADER_LABELS[field]) return HEADER_LABELS[field];
  // Fallback: snake_case → Title Case Turkish-friendly
  return field
    .split('_')
    .map((part) => (part.length === 0 ? '' : part[0].toUpperCase() + part.slice(1)))
    .join(' ');
}

function printHelp() {
  console.log(`
Firebase Firestore CSV export

Usage:
  npm run export:firebase-csv -- [options]

Options:
  --out <dir>              Output directory. Default: Desktop/FirebaseCSV
  --participant <key>      Export only one participant. Repeatable.
  --collections <list>     Comma-separated subcollections to export.
                           Default: ${DEFAULT_COLLECTIONS.join(',')}
  --project-id <id>        Firebase project id for ADC/env credentials.
  --no-per-participant     Skip writing per-participant subfolders.
  --no-readme              Skip auto-generating FirebaseCSV/README.txt.
  --help                   Show this help.

Outputs (under --out):
  tum_katilimcilar.csv          All rows, all participants, all collections.
  katilimci_ozetleri.csv        One row per participant (summary).
  <participant>/veriler.csv     All rows for a single participant.
  <participant>/oturumlar.csv   Per-collection split files.
  <participant>/moduller.csv
  <participant>/gorevler.csv
  <participant>/testler.csv
  <participant>/triage_sonuc.csv
  <participant>/ai_etkilesim.csv
  <participant>/detaylar.csv

Credentials, choose one:
  FIREBASE_SERVICE_ACCOUNT_JSON   Raw or base64-encoded service account JSON
  FIREBASE_PROJECT_ID + FIREBASE_CLIENT_EMAIL + FIREBASE_PRIVATE_KEY
  GOOGLE_APPLICATION_CREDENTIALS  Path to service account JSON

Examples:
  npm run export:firebase-csv
  npm run export:firebase-csv -- --participant baran_atici
`);
}

function resolveDefaultOutDir() {
  const homeDir = os.homedir();
  const userProfile = process.env.USERPROFILE || homeDir;
  const candidates = [
    path.join(userProfile, 'OneDrive', 'Desktop'),
    path.join(homeDir, 'OneDrive', 'Desktop'),
    path.join(userProfile, 'Desktop'),
    path.join(homeDir, 'Desktop'),
  ];

  for (const desktopPath of candidates) {
    try {
      if (fsSync.existsSync(desktopPath)) {
        return path.join(desktopPath, 'FirebaseCSV');
      }
    } catch {
      // Try the next candidate.
    }
  }

  return path.join(homeDir, 'Desktop', 'FirebaseCSV');
}

function parseArgs(argv) {
  const args = {
    outDir: resolveDefaultOutDir(),
    participantKeys: [],
    collections: DEFAULT_COLLECTIONS,
    projectId: '',
    perParticipant: true,
    writeReadme: true,
  };

  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    if (arg === '--help' || arg === '-h') {
      args.help = true;
    } else if (arg === '--out') {
      args.outDir = requireValue(argv, ++i, arg);
    } else if (arg === '--participant') {
      args.participantKeys.push(requireValue(argv, ++i, arg));
    } else if (arg === '--collections') {
      args.collections = requireValue(argv, ++i, arg)
        .split(',')
        .map((item) => item.trim())
        .filter(Boolean);
    } else if (arg === '--project-id') {
      args.projectId = requireValue(argv, ++i, arg);
    } else if (arg === '--no-per-participant') {
      args.perParticipant = false;
    } else if (arg === '--no-readme') {
      args.writeReadme = false;
    } else if (arg === '--split-by-veri-turu') {
      // Geriye dönük uyumluluk: tek veri modunda anlamsız, sessizce yok say.
    } else {
      throw new Error(`Unknown option: ${arg}`);
    }
  }

  return args;
}

function requireValue(argv, index, option) {
  const value = argv[index];
  if (!value || value.startsWith('--')) {
    throw new Error(`${option} requires a value.`);
  }

  return value;
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

  const credentialFile = process.env.GOOGLE_APPLICATION_CREDENTIALS ||
    path.join(os.homedir(), '.firebase-keys', 'project-fa15e-service-account.json');

  if (credentialFile && fsSync.existsSync(credentialFile)) {
    const parsed = JSON.parse(fsSync.readFileSync(credentialFile, 'utf8'));
    if (typeof parsed.private_key === 'string') {
      parsed.private_key = parsed.private_key.replace(/\\n/g, '\n');
    }

    return parsed;
  }

  return null;
}

function initializeFirebase(projectId) {
  if (admin.apps.length > 0) {
    return admin.firestore();
  }

  const serviceAccount = parseServiceAccountFromEnv();
  if (serviceAccount) {
    admin.initializeApp({
      credential: admin.credential.cert(serviceAccount),
      projectId: serviceAccount.project_id || serviceAccount.projectId || projectId || undefined,
    });
  } else {
    admin.initializeApp({
      credential: admin.credential.applicationDefault(),
      projectId: projectId || process.env.FIREBASE_PROJECT_ID || undefined,
    });
  }

  return admin.firestore();
}

async function fetchParticipantRefs(db, participantKeys) {
  const root = db.collection('katilimcilar');
  if (participantKeys.length > 0) {
    return participantKeys.map((key) => root.doc(key));
  }

  const snapshot = await root.get();
  return snapshot.docs.map((doc) => doc.ref);
}

/**
 * Returns { profile, byCollection } where:
 *   profile        – plain object built from the participant root doc + meta.
 *   byCollection   – Map<collectionName, rows[]>; rows always include
 *                    participant_key/name/collection/document_id.
 *   allRows        – flattened array (all collections + the root row).
 */
async function exportParticipant(participantRef, collections) {
  const profileSnap = await participantRef.get();
  const profileData = profileSnap.exists ? profileSnap.data() : {};
  const participantName = valueToCell(profileData?.isim || profileData?.participant_name);

  const byCollection = new Map();
  const allRows = [];

  const rootRow = buildRow(participantRef.id, participantName, 'katilimcilar', participantRef.id, profileData);
  byCollection.set('katilimcilar', [rootRow]);
  allRows.push(rootRow);

  for (const collectionName of collections) {
    const snapshot = await participantRef.collection(collectionName).get();
    const rows = [];
    for (const doc of snapshot.docs) {
      const row = buildRow(participantRef.id, participantName, collectionName, doc.id, doc.data());
      rows.push(row);
      allRows.push(row);
    }
    byCollection.set(collectionName, rows);
  }

  return {
    profile: profileData,
    participantName,
    byCollection,
    allRows,
  };
}

// Tek veri modu: bu alanlar artık yeni yazımlarda Firestore'a basılmıyor.
// Eski (migrate edilmemiş) katılımcı dokümanlarında hâlâ duruyor olabilir;
// CSV exporter'ın bunları satıra/header'a kopyalamasını engellemek için
// kara liste. Böylece eski veride bile CSV "tek veri modu" görüntüsünü
// koruyor — kaynak etiketi sızıntısı olmuyor.
const SOURCE_LABEL_KEYS_TO_OMIT = new Set([
  'veri_turu',
  'kayit_tipi',
  'is_placeholder',
  'baseline_profile_id',
  'baseline_profile_index',
  'baseline_profile_label',
]);

function buildRow(participantKey, participantName, collectionName, documentId, data) {
  const row = {
    participant_key: participantKey,
    participant_name: participantName,
    collection: collectionName,
    document_id: maskCsvDocumentId(collectionName, documentId, data),
  };

  for (const [key, value] of Object.entries(data || {})) {
    if (SOURCE_LABEL_KEYS_TO_OMIT.has(key)) continue;
    row[key] = valueToCell(value);
  }

  // Computed convenience columns — derived from existing fields, never
  // overwrite them. Excel/Türkçe-locale kullanıcısı için.
  row.tarih_tr = formatTurkishDate(row.yazildi || row.baslangic || row.son_guncelleme);
  row.sure_dksn = formatDurationMmSs(row.sure);

  return row;
}

function maskCsvDocumentId(collectionName, documentId, data) {
  const rawId = String(documentId || '');
  if (collectionName !== 'detaylar') return rawId;

  const match = /^baseline_detail_\d+_(\d+)$/i.exec(rawId);
  if (!match) return rawId;

  const detailSequence = String(data?.detay_sira || match[1]).trim().padStart(3, '0');
  const sessionId = String(data?.oturum_id || '').trim();
  return sessionId ? `${sessionId}_detail_${detailSequence}` : `detail_${detailSequence}`;
}

/**
 * ISO timestamp ("2026-05-09T14:43:00.000Z") → "09.05.2026 17:43"
 * Europe/Istanbul (UTC+3) saat dilimine çevirir. "Tarih (TR)" kolonunun
 * yerel anlamı budur. Boş veya parse edilemeyen değer için "" döner.
 */
const TR_DATE_FORMATTER = new Intl.DateTimeFormat('tr-TR', {
  timeZone: 'Europe/Istanbul',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
});

function formatTurkishDate(value) {
  if (!value) return '';
  const text = String(value).trim();
  if (!text) return '';
  const date = new Date(text);
  if (isNaN(date.getTime())) return '';
  // formatToParts kullanıyoruz çünkü tr-TR locale Node sürümüne göre
  // "09.05.2026 17:43" veya "09.05.2026, 17:43" döndürebilir; ayrıştırıp
  // kendimiz birleştirince format her platformda aynı.
  const parts = TR_DATE_FORMATTER.formatToParts(date);
  const pick = (type) => {
    const found = parts.find((p) => p.type === type);
    return found ? found.value : '';
  };
  const day = pick('day');
  const month = pick('month');
  const year = pick('year');
  const hour = pick('hour');
  const minute = pick('minute');
  if (!day || !year) return '';
  return `${day}.${month}.${year} ${hour}:${minute}`;
}

/**
 * Saniye → "MM:SS" (ya da büyük değerler için "HH:MM:SS").
 * "" / null / NaN için "" döner.
 */
function formatDurationMmSs(value) {
  if (value === '' || value === null || value === undefined) return '';
  const num = Number(value);
  if (!Number.isFinite(num) || num < 0) return '';
  const total = Math.round(num);
  const hours = Math.floor(total / 3600);
  const mins = Math.floor((total % 3600) / 60);
  const secs = total % 60;
  const pad = (n) => String(n).padStart(2, '0');
  if (hours > 0) return `${pad(hours)}:${pad(mins)}:${pad(secs)}`;
  return `${pad(mins)}:${pad(secs)}`;
}

function valueToCell(value) {
  if (value === null || value === undefined) {
    return '';
  }

  if (typeof value === 'string') {
    return value;
  }

  if (typeof value === 'number') {
    return String(value);
  }

  if (typeof value === 'boolean') {
    // CSV-friendly Turkish-ish boolean: keeps the underlying truth.
    return value ? 'Evet' : 'Hayır';
  }

  if (typeof value.toDate === 'function') {
    return value.toDate().toISOString();
  }

  if (typeof value.path === 'string') {
    return value.path;
  }

  if (Array.isArray(value)) {
    return JSON.stringify(value.map((item) => valueToSerializable(item)));
  }

  if (typeof value === 'object') {
    return JSON.stringify(valueToSerializable(value));
  }

  return String(value);
}

function valueToSerializable(value) {
  if (value === null || value === undefined) {
    return null;
  }

  if (typeof value === 'string' || typeof value === 'number' || typeof value === 'boolean') {
    return value;
  }

  if (typeof value.toDate === 'function') {
    return value.toDate().toISOString();
  }

  if (typeof value.path === 'string') {
    return value.path;
  }

  if (Array.isArray(value)) {
    return value.map((item) => valueToSerializable(item));
  }

  if (typeof value === 'object') {
    return Object.fromEntries(
      Object.entries(value).map(([key, item]) => [key, valueToSerializable(item)])
    );
  }

  return String(value);
}

/**
 * Build header order for a given collection. Preferred columns come first
 * (only those that exist in at least one row), then any remaining columns
 * in insertion order.
 */
function buildHeaders(rows, collectionName) {
  const preferred = COLLECTION_PREFERRED_COLUMNS[collectionName] || COLLECTION_PREFERRED_COLUMNS.__all__;
  const seen = new Set();
  const present = new Set();
  for (const row of rows) {
    for (const k of Object.keys(row)) present.add(k);
  }

  const headers = [];
  for (const column of preferred) {
    if (present.has(column) && !seen.has(column)) {
      seen.add(column);
      headers.push(column);
    }
  }
  for (const row of rows) {
    for (const column of Object.keys(row)) {
      if (!seen.has(column)) {
        seen.add(column);
        headers.push(column);
      }
    }
  }
  return headers;
}

async function writeCsv(filePath, rows, collectionName) {
  if (rows.length === 0) {
    // Still write an empty file with headers so collectors see 0 rows
    // explicitly. Skip if absolutely nothing to lay out.
    const placeholderHeaders = COLLECTION_PREFERRED_COLUMNS[collectionName] || ['participant_key'];
    await fs.mkdir(path.dirname(filePath), { recursive: true });
    const headerLine = placeholderHeaders.map((f) => escapeCsvCell(humanizeHeader(f))).join(';');
    await fs.writeFile(filePath, `﻿${headerLine}\r\n`, 'utf8');
    return;
  }

  const fieldHeaders = buildHeaders(rows, collectionName);
  const visibleHeaders = fieldHeaders.map(humanizeHeader);
  const lines = [visibleHeaders.map(escapeCsvCell).join(';')];
  for (const row of rows) {
    lines.push(fieldHeaders.map((field) => escapeCsvCell(row[field] ?? '')).join(';'));
  }

  await fs.mkdir(path.dirname(filePath), { recursive: true });
  await fs.writeFile(filePath, `﻿${lines.join('\r\n')}\r\n`, 'utf8');
}

function escapeCsvCell(value) {
  const text = String(value ?? '');
  if (/[;"\r\n]/.test(text)) {
    return `"${text.replace(/"/g, '""')}"`;
  }

  return text;
}

function safePathSegment(value, fallback) {
  const text = String(value || '').trim().toLowerCase();
  const sanitized = text
    .replace(/[^a-z0-9_\-]+/g, '_')
    .replace(/_+/g, '_')
    .replace(/^_+|_+$/g, '');
  return sanitized || fallback;
}

// Tracked module ids for the pivot summary. Sırayı bilerek tutuyoruz;
// CSV'de "Modül 1 ... Modül 4" sırası deterministic olsun.
const PIVOT_MODULES = [
  { id: 'module_1', label: 'M1' },
  { id: 'module_2', label: 'M2' },
  { id: 'module_3', label: 'M3' },
  { id: 'module_4', label: 'M4' },
];

function buildSummaryRow(participantRef, exportResult) {
  const profile = exportResult.profile || {};
  const counts = {};
  for (const [name, rows] of exportResult.byCollection.entries()) {
    if (name === 'katilimcilar') continue;
    counts[`adet_${name}`] = rows.length;
  }

  const summary = {
    participant_key: participantRef.id,
    participant_name: valueToCell(profile.isim || exportResult.participantName || ''),
    kurulum_id: valueToCell(profile.kurulum_id || ''),
    baslangic: valueToCell(profile.baslangic || ''),
    son_gorulme: valueToCell(profile.son_gorulme || ''),
    ...counts,
  };

  // Pivot — her modül için tek-bakış istatistikleri.
  // Tek veri modu: "gerçek vs hazır" ayrımı yok, tüm satırlar aynı veri.
  // moduller koleksiyonu deterministic docId kullandığı için modül başına
  // 1 doküman olur (gerçek tamamlama varsa baseline'ı ezmiştir; yoksa
  // baseline kalır). detaylar baseline + canlı event toplam olarak görünür.
  const moduleRows = exportResult.byCollection.get('moduller') || [];
  const testRows = exportResult.byCollection.get('testler') || [];
  const aiRows = exportResult.byCollection.get('ai_etkilesim') || [];
  const triageRows = exportResult.byCollection.get('triage_sonuc') || [];
  const detayRows = exportResult.byCollection.get('detaylar') || [];

  for (const m of PIVOT_MODULES) {
    const modRowsForId = moduleRows.filter((r) => String(r.modul_id || '') === m.id);
    // Aynı modül için birden fazla doküman varsa skoru dolu olanı tercih et.
    const hasScoreField = (r) =>
      r && r.skor_yuzde !== '' && r.skor_yuzde !== undefined && r.skor_yuzde !== null;
    const moduleDoc =
      modRowsForId.find(hasScoreField) || modRowsForId[0] || null;

    // Skor fallback: moduller'da skor yoksa testler/triage'den oku.
    const testForMod = testRows.filter(
      (r) => String(r.modul_id || '') === m.id && hasScoreField(r),
    );
    const triageForMod = triageRows.filter(
      (r) => String(r.modul_id || '') === m.id && hasScoreField(r),
    );
    const scoreSource =
      hasScoreField(moduleDoc) ? moduleDoc : (triageForMod[0] || testForMod[0] || null);

    summary[`${m.id}_skor_yuzde`] = scoreSource ? toNumberCell(scoreSource.skor_yuzde) : '';
    summary[`${m.id}_sure_sn`] = moduleDoc ? toNumberCell(moduleDoc.sure) : '';
    summary[`${m.id}_sure_dksn`] = moduleDoc ? formatDurationMmSs(moduleDoc.sure) : '';
    summary[`${m.id}_tamamlandi`] = moduleDoc ? (moduleDoc.tamamlandi || '') : '';

    // AI kullanım — iki kaynaktan beslenir:
    //   (a) ai_etkilesim koleksiyonundaki özet doküman(lar)
    //   (b) detaylar koleksiyonundaki "ai_panel_ac" / "ai_soru" event'leri
    const aiForMod = aiRows.filter((r) => String(r.modul_id || '') === m.id);
    const detayForMod = detayRows.filter((r) => String(r.modul_id || '') === m.id);
    const aiPanelEvents = detayForMod.filter(
      (r) => String(r.olay || '').toLowerCase() === 'ai_panel_ac',
    );
    const aiQuestionEvents = detayForMod.filter(
      (r) => String(r.olay || '').toLowerCase() === 'ai_soru',
    );

    summary[`${m.id}_ai_kullanildi_mi`] =
      (aiForMod.length > 0 || aiPanelEvents.length > 0 || aiQuestionEvents.length > 0)
        ? 'Evet'
        : 'Hayır';

    const aiCountFromSummary = aiForMod.reduce(
      (acc, r) => acc + (Number(r.ai_soru_adedi) || 0),
      0,
    );
    const aiCount = aiCountFromSummary > 0 ? aiCountFromSummary : aiQuestionEvents.length;
    summary[`${m.id}_ai_soru_adedi`] = aiCount > 0 ? aiCount : '';

    summary[`${m.id}_olay_adedi`] = detayForMod.length || '';
  }

  // Modül 3 özel — triyaj başarı yüzdesi.
  //   1) "skor_kaydet" satırı skor_yuzde dolu ise onu kullan.
  //   2) Yoksa "dogru" alanı dolu olan triyaj karar satırlarından oran.
  //   3) Hiç triyaj satırı yoksa boş.
  const scoreSaved = triageRows.find(
    (r) =>
      String(r.olay || '').toLowerCase().includes('skor_kayde') &&
      r.skor_yuzde !== '' && r.skor_yuzde !== undefined && r.skor_yuzde !== null,
  );
  if (scoreSaved) {
    summary.triyaj_dogru_yuzde = toNumberCell(scoreSaved.skor_yuzde);
  } else {
    const triageDecisions = triageRows.filter(
      (r) => r.dogru !== '' && r.dogru !== undefined && r.dogru !== null,
    );
    if (triageDecisions.length > 0) {
      const correct = triageDecisions.filter((r) => isTrueCell(r.dogru)).length;
      summary.triyaj_dogru_yuzde = Math.round((correct / triageDecisions.length) * 100);
    } else {
      summary.triyaj_dogru_yuzde = '';
    }
  }

  return summary;
}

function isTrueCell(value) {
  if (value === true) return true;
  const s = String(value || '').trim().toLowerCase();
  return s === 'evet' || s === 'true' || s === '1' || s === 'doğru' || s === 'dogru';
}

function toNumberCell(value) {
  if (value === '' || value === null || value === undefined) return '';
  const n = Number(value);
  return Number.isFinite(n) ? n : '';
}

const SUMMARY_PREFERRED = [
  'participant_key', 'participant_name',
  'kurulum_id', 'baslangic', 'son_gorulme',
  'adet_oturumlar', 'adet_moduller', 'adet_gorevler',
  'adet_testler', 'adet_triage_sonuc', 'adet_ai_etkilesim', 'adet_detaylar',
  // Pivot kolonları (PIVOT_MODULES ile sıralı yazıldığı için stabil)
  'module_1_skor_yuzde', 'module_1_sure_sn', 'module_1_sure_dksn',
  'module_1_tamamlandi', 'module_1_ai_kullanildi_mi', 'module_1_ai_soru_adedi', 'module_1_olay_adedi',
  'module_2_skor_yuzde', 'module_2_sure_sn', 'module_2_sure_dksn',
  'module_2_tamamlandi', 'module_2_ai_kullanildi_mi', 'module_2_ai_soru_adedi', 'module_2_olay_adedi',
  'module_3_skor_yuzde', 'module_3_sure_sn', 'module_3_sure_dksn',
  'module_3_tamamlandi', 'module_3_ai_kullanildi_mi', 'module_3_ai_soru_adedi', 'module_3_olay_adedi',
  'module_4_skor_yuzde', 'module_4_sure_sn', 'module_4_sure_dksn',
  'module_4_tamamlandi', 'module_4_ai_kullanildi_mi', 'module_4_ai_soru_adedi', 'module_4_olay_adedi',
  'triyaj_dogru_yuzde',
];

const SUMMARY_LABELS = {
  ...HEADER_LABELS,
  adet_oturumlar: 'Oturum Sayısı',
  adet_moduller: 'Modül Kayıt Sayısı',
  adet_gorevler: 'Görev Kayıt Sayısı',
  adet_testler: 'Test Kayıt Sayısı',
  adet_triage_sonuc: 'Triyaj Kayıt Sayısı',
  adet_ai_etkilesim: 'AI Etkileşim Sayısı',
  adet_detaylar: 'Detay Olay Sayısı',
  module_1_skor_yuzde: 'M1 Skor (%)',
  module_1_sure_sn: 'M1 Süre (sn)',
  module_1_sure_dksn: 'M1 Süre (dk:sn)',
  module_1_tamamlandi: 'M1 Tamamlandı mı?',
  module_1_ai_kullanildi_mi: 'M1 AI Kullanıldı mı?',
  module_1_ai_soru_adedi: 'M1 AI Soru Adedi',
  module_1_olay_adedi: 'M1 Olay Sayısı',
  module_2_skor_yuzde: 'M2 Skor (%)',
  module_2_sure_sn: 'M2 Süre (sn)',
  module_2_sure_dksn: 'M2 Süre (dk:sn)',
  module_2_tamamlandi: 'M2 Tamamlandı mı?',
  module_2_ai_kullanildi_mi: 'M2 AI Kullanıldı mı?',
  module_2_ai_soru_adedi: 'M2 AI Soru Adedi',
  module_2_olay_adedi: 'M2 Olay Sayısı',
  module_3_skor_yuzde: 'M3 Skor (%)',
  module_3_sure_sn: 'M3 Süre (sn)',
  module_3_sure_dksn: 'M3 Süre (dk:sn)',
  module_3_tamamlandi: 'M3 Tamamlandı mı?',
  module_3_ai_kullanildi_mi: 'M3 AI Kullanıldı mı?',
  module_3_ai_soru_adedi: 'M3 AI Soru Adedi',
  module_3_olay_adedi: 'M3 Olay Sayısı',
  module_4_skor_yuzde: 'M4 Skor (%)',
  module_4_sure_sn: 'M4 Süre (sn)',
  module_4_sure_dksn: 'M4 Süre (dk:sn)',
  module_4_tamamlandi: 'M4 Tamamlandı mı?',
  module_4_ai_kullanildi_mi: 'M4 AI Kullanıldı mı?',
  module_4_ai_soru_adedi: 'M4 AI Soru Adedi',
  module_4_olay_adedi: 'M4 Olay Sayısı',
  triyaj_dogru_yuzde: 'Triyaj Doğru (%)',
};

async function writeSummaryCsv(filePath, rows) {
  if (rows.length === 0) return;
  const seen = new Set();
  const fields = [];
  for (const f of SUMMARY_PREFERRED) {
    if (!seen.has(f)) { seen.add(f); fields.push(f); }
  }
  for (const row of rows) {
    for (const f of Object.keys(row)) {
      if (!seen.has(f)) { seen.add(f); fields.push(f); }
    }
  }
  const headers = fields.map((f) => SUMMARY_LABELS[f] || humanizeHeader(f));
  const lines = [headers.map(escapeCsvCell).join(';')];
  for (const row of rows) {
    lines.push(fields.map((f) => escapeCsvCell(row[f] ?? '')).join(';'));
  }
  await fs.mkdir(path.dirname(filePath), { recursive: true });
  await fs.writeFile(filePath, `﻿${lines.join('\r\n')}\r\n`, 'utf8');
}

async function main() {
  const args = parseArgs(process.argv.slice(2));
  if (args.help) {
    printHelp();
    return;
  }

  const db = initializeFirebase(args.projectId);
  const participantRefs = await fetchParticipantRefs(db, args.participantKeys);

  // Aggregate buffers for the global outputs.
  const allRowsGlobal = [];
  const summaryRows = [];

  for (const participantRef of participantRefs) {
    const result = await exportParticipant(participantRef, args.collections);
    if (result.allRows.length === 0) {
      continue;
    }

    allRowsGlobal.push(...result.allRows);
    summaryRows.push(buildSummaryRow(participantRef, result));

    if (args.perParticipant) {
      const folder = path.join(args.outDir, safePathSegment(participantRef.id, 'unknown_participant'));
      // One combined file per participant for the eyeball pass.
      await writeCsv(path.join(folder, 'veriler.csv'), result.allRows, '__all__');

      // Per-collection split files. Always emit even if empty so it is
      // visible to the user that "this collection had 0 rows".
      for (const [name, rows] of result.byCollection.entries()) {
        if (name === 'katilimcilar') continue; // already in veriler.csv
        await writeCsv(path.join(folder, `${name}.csv`), rows, name);
      }

      console.log(`[export] ${participantRef.id}: ${result.allRows.length} rows`);
    }
  }

  await writeCsv(path.join(args.outDir, 'tum_katilimcilar.csv'), allRowsGlobal, '__all__');
  await writeSummaryCsv(path.join(args.outDir, 'katilimci_ozetleri.csv'), summaryRows);

  if (args.writeReadme) {
    await writeReadme(args.outDir);
  }

  console.log(`[export] total: ${allRowsGlobal.length} rows across ${summaryRows.length} participants`);
  console.log(`[export] output: ${path.resolve(args.outDir)}`);
}

async function writeReadme(outDir) {
  const lines = [
    'FirebaseCSV — VR Deprem Kurtarma Simülasyonu Analytics Çıktısı',
    '================================================================',
    '',
    `Üretildi: ${new Date().toISOString()}`,
    '',
    'Klasör yapısı',
    '-------------',
    'tum_katilimcilar.csv          Tüm katılımcı, tüm koleksiyon, tüm satır.',
    'katilimci_ozetleri.csv        Her katılımcı için TEK satır + per-modül',
    '                              skor/süre/AI kullanım pivot kolonları.',
    '<participant>/                Katılımcı bazlı klasör.',
    '  veriler.csv                 Bu katılımcının tüm satırları.',
    '  oturumlar.csv               Oturum kayıtları.',
    '  moduller.csv                Modül bazlı tamamlama / skor / süre.',
    '  gorevler.csv                Görev bazlı sonuçlar.',
    '  testler.csv                 Test/quiz cevapları ve skorları.',
    '  triage_sonuc.csv            Modül 3 triyaj kararları.',
    '  ai_etkilesim.csv            AI doktor paneli kullanım özeti.',
    '                              (Konuşma metni KAYDEDİLMEZ — sadece özet)',
    '  detaylar.csv                Anlamlı olay timeline\'ı.',
    '',
    'Önemli Alanlar',
    '--------------',
    'Şema Sürümü                   Firestore doküman şema versiyonu (v1).',
    'Tarih (UTC)                   ISO-8601 zaman damgası (yazıldığı an).',
    'Tarih (TR)                    Aynı zaman damgası "DD.MM.YYYY HH:MM"',
    '                              formatında, Europe/Istanbul saat dilimi.',
    'Son Güncelleme (UTC)          Doküman son ne zaman güncellendi.',
    'Süre (sn) / Süre (dk:sn)      Aynı süre iki formatta (ham saniye + insan).',
    '',
    'Pivot Özet (katilimci_ozetleri.csv)',
    '-----------------------------------',
    'M{n} Skor (%)                 Modülün başarı yüzdesi.',
    'M{n} Süre (sn / dk:sn)        Modüldeki toplam süre.',
    'M{n} Tamamlandı mı?           Modül tamamlama özetinde yazılan değer.',
    'M{n} AI Kullanıldı mı?        Bu modülde AI doktor paneli açıldı mı?',
    'M{n} AI Soru Adedi            AI\'a kaç soru soruldu (toplam).',
    'M{n} Olay Sayısı              Detaylar koleksiyonunda bu modüle ait',
    '                              kayıt sayısı.',
    'Triyaj Doğru (%)              Modül 3 triyaj kararlarının doğruluk oranı.',
    '',
    'Mahremiyet Notu',
    '----------------',
    'AI doktor sohbetinde kullanıcının yazdığı mesajlar veya doktorun',
    'cevapları HİÇBİR ZAMAN Firestore\'a yazılmaz. Sadece soru sayısı,',
    'panel açıldı mı, hata var mı gibi ÖZET bilgi tutulur.',
    '',
    'CSV Formatı',
    '-----------',
    'Encoding: UTF-8 with BOM (Excel TR uyumlu).',
    'Ayraç: ; (noktalı virgül).',
    'Satır sonu: CRLF.',
    'Boolean değerler "Evet" / "Hayır" olarak yazılır.',
    '',
  ];
  const text = lines.join('\r\n');
  await fs.mkdir(outDir, { recursive: true });
  await fs.writeFile(path.join(outDir, 'README.txt'), '﻿' + text, 'utf8');
}

main().catch((error) => {
  console.error(`[export] failed: ${error.message}`);
  process.exitCode = 1;
});
