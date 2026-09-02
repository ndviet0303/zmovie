// Vendors Nunito Sans locally so exported designs never depend on
// fonts.googleapis.com being reachable from the rendering environment.
//
// Why self-host at all: assets/css/main.css pulls the family from the Google
// Fonts CDN. That works in the app, but a design rendered somewhere that blocks
// external hosts silently falls back to system sans — and nothing downstream
// catches it. Note the shipped stylesheet's remote @import is stripped by
// .design-sync/css/build.mjs: two @font-face sets for one family means the
// later one shadows, and a failed src does NOT fall back to the earlier set.
//
// Nunito Sans is licensed OFL 1.1, which permits redistribution.
// Cyrillic subsets are dropped (zmovie ships Vietnamese + English); latin,
// latin-ext and vietnamese are kept. Files are committed, so a build needs no
// network. Re-run only to refresh: node .design-sync/fonts/fetch.mjs

import { writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = dirname(fileURLToPath(import.meta.url));
const KEEP = new Set(['latin', 'latin-ext', 'vietnamese']);
const UA =
  'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36';
const API =
  'https://fonts.googleapis.com/css2?family=Nunito+Sans:wght@400;500;600;700;800;900&display=swap';

const css = await fetch(API, { headers: { 'User-Agent': UA } }).then((r) => {
  if (!r.ok) throw new Error(`google fonts API ${r.status}`);
  return r.text();
});

// Each face is preceded by a `/* <subset> */` comment. Google serves Nunito Sans
// as a VARIABLE woff2, so every requested weight within one subset points at the
// same file — collapse them to one face per subset declaring the whole 400..900
// range, or the same bytes ship six times over.
const chunks = css.split('/*').slice(1);
const bySubset = new Map();
let skipped = 0;

for (const chunk of chunks) {
  const subset = chunk.slice(0, chunk.indexOf('*/')).trim();
  const face = chunk.slice(chunk.indexOf('*/') + 2);
  if (!/@font-face/.test(face)) continue;
  if (!KEEP.has(subset)) { skipped++; continue; }

  const url = face.match(/url\((https:\/\/[^)]+\.woff2)\)/)?.[1];
  const range = face.match(/unicode-range:\s*([^;]+);/)?.[1]?.trim();
  if (!url) throw new Error(`unparsable @font-face for subset ${subset}`);

  const seen = bySubset.get(subset);
  if (seen) {
    // Guard the variable-font assumption: if a subset ever serves distinct files
    // per weight, collapsing would silently drop weights.
    if (seen.url !== url) throw new Error(`subset ${subset} serves >1 distinct woff2 — per-weight faces needed`);
    continue;
  }
  bySubset.set(subset, { url, range });
}

const out = [];
for (const [subset, { url, range }] of bySubset) {
  const name = `nunito-sans-${subset}.woff2`;
  const bytes = await fetch(url, { headers: { 'User-Agent': UA } }).then(async (r) => {
    if (!r.ok) throw new Error(`${name}: ${r.status}`);
    return Buffer.from(await r.arrayBuffer());
  });
  writeFileSync(join(HERE, name), bytes);
  out.push(
    `/* ${subset} */\n@font-face {\n  font-family: 'Nunito Sans';\n  font-style: normal;\n` +
      `  font-weight: 400 900;\n  font-stretch: 100%;\n  font-display: swap;\n` +
      `  src: url(./${name}) format('woff2');\n` +
      (range ? `  unicode-range: ${range};\n` : '') +
      `}`,
  );
}
const kept = out.length;

writeFileSync(
  join(HERE, 'nunito-sans.css'),
  `/* Nunito Sans (OFL 1.1), vendored from Google Fonts by fetch.mjs.\n` +
    ` * Subsets: ${[...KEEP].join(', ')}. Do not edit by hand — re-run fetch.mjs. */\n\n` +
    out.join('\n\n') +
    '\n',
);

console.error(`wrote ${kept} woff2 files (+ nunito-sans.css), skipped ${skipped} out-of-scope subset faces`);
