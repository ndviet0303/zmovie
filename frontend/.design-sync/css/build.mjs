// Builds the tokens-only stylesheet that design-sync ships as cfg.cssEntry.
//
// Step 1 derives a safelist from the @theme block in assets/css/main.css, so the
// class vocabulary tracks the real token source instead of a hand-maintained
// list that rots the moment someone adds a colour.
// Step 2 runs the Tailwind v4 CLI over .design-sync/css/entry.css.
//
// Run from the frontend/ package root:  node .design-sync/css/build.mjs

import { execFileSync } from 'node:child_process';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = dirname(fileURLToPath(import.meta.url));
const PKG = resolve(HERE, '../..');
const MAIN_CSS = resolve(PKG, 'assets/css/main.css');
const CACHE = resolve(PKG, '.design-sync/.cache');
const SAFELIST = resolve(CACHE, 'safelist.txt');
const ENTRY = resolve(HERE, 'entry.css');
const OUT = resolve(CACHE, 'zmovie-tokens.css');

// Utility families worth emitting for every theme colour. Kept narrow on
// purpose — every entry here multiplies into the shipped sheet.
const COLOR_FAMILIES = ['bg', 'text', 'border', 'ring', 'outline', 'from', 'to', 'fill', 'stroke'];
const ALPHA_FAMILIES = ['bg', 'text', 'border'];
const ALPHAS = [5, 10, 20, 30, 40, 50, 60, 70, 80, 90];
const VARIANTS = ['hover', 'focus', 'focus-visible', 'active', 'disabled', 'group-hover'];
const VARIANT_FAMILIES = ['bg', 'text', 'border', 'ring'];
const SPACING_FAMILIES = ['w', 'max-w', 'min-w', 'h', 'p', 'px', 'py', 'm', 'mx', 'gap'];

// ── Standard-scale backfill ─────────────────────────────────────────────────
// The app scan above only emits what zmovie itself uses, which leaves arbitrary
// holes: p-7/p-10/p-16 ship but p-8 doesn't; mb-5/mb-8 ship but mb-6 doesn't.
// A design agent writing p-8 would silently get no padding. These lists close
// the scale so the vocabulary is complete instead of incidental. Values are
// Tailwind's own defaults — nothing invented.
const STEPS = [
  0, 0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 16, 20, 24, 28, 32, 36, 40, 44,
  48, 52, 56, 60, 64, 72, 80, 96,
];
const STEP_FAMILIES = [
  'p', 'px', 'py', 'pt', 'pr', 'pb', 'pl',
  'm', 'mx', 'my', 'mt', 'mr', 'mb', 'ml',
  'gap', 'gap-x', 'gap-y', 'space-x', 'space-y',
  'w', 'h', 'min-w', 'min-h', 'max-w', 'max-h', 'size',
  'top', 'right', 'bottom', 'left', 'inset',
];
const KEYWORD_SIZES = {
  w: ['full', 'screen', 'auto', 'fit', 'min', 'max', '1/2', '1/3', '2/3', '1/4', '3/4', '1/5', '4/5'],
  h: ['full', 'screen', 'auto', 'fit', 'min', 'max', '1/2', '1/3', '2/3'],
  'min-h': ['full', 'screen', 'dvh', '0'],
  'max-w': ['xs', 'sm', 'md', 'lg', 'xl', '2xl', '3xl', '4xl', '5xl', '6xl', '7xl', 'full', 'prose', 'none', 'fit'],
  'max-h': ['full', 'screen', 'none'],
  inset: ['0', 'auto', 'x-0', 'y-0'],
};
const MISC = [
  // display / box
  'block', 'inline-block', 'inline', 'flex', 'inline-flex', 'grid', 'inline-grid', 'hidden', 'contents',
  'relative', 'absolute', 'fixed', 'sticky', 'static',
  'isolate', 'overflow-hidden', 'overflow-auto', 'overflow-x-auto', 'overflow-y-auto', 'overflow-scroll',
  'overflow-visible', 'box-border', 'box-content',
  // flex / grid
  'flex-row', 'flex-row-reverse', 'flex-col', 'flex-col-reverse', 'flex-wrap', 'flex-nowrap',
  'flex-1', 'flex-auto', 'flex-initial', 'flex-none', 'grow', 'grow-0', 'shrink', 'shrink-0',
  'items-start', 'items-center', 'items-end', 'items-baseline', 'items-stretch',
  'justify-start', 'justify-center', 'justify-end', 'justify-between', 'justify-around', 'justify-evenly',
  'self-start', 'self-center', 'self-end', 'self-stretch', 'self-auto',
  'content-center', 'content-between', 'place-items-center', 'place-content-center',
  'order-first', 'order-last', 'order-none',
  // typography
  'text-left', 'text-center', 'text-right', 'text-justify',
  'font-thin', 'font-extralight', 'font-light', 'font-normal', 'font-medium', 'font-semibold',
  'font-bold', 'font-extrabold', 'font-black',
  'italic', 'not-italic', 'uppercase', 'lowercase', 'capitalize', 'normal-case',
  'underline', 'line-through', 'no-underline', 'truncate', 'text-nowrap', 'text-wrap', 'text-balance',
  'break-words', 'break-all', 'whitespace-nowrap', 'whitespace-normal', 'whitespace-pre-line',
  'tabular-nums', 'antialiased',
  // effects / misc
  'border', 'border-0', 'border-2', 'border-4', 'border-8',
  'border-t', 'border-r', 'border-b', 'border-l', 'border-y', 'border-x',
  'border-solid', 'border-dashed', 'border-none',
  'rounded-none', 'rounded-sm', 'rounded', 'rounded-md', 'rounded-lg', 'rounded-xl', 'rounded-2xl',
  'rounded-3xl', 'rounded-full', 'rounded-t-lg', 'rounded-b-lg', 'rounded-t-xl', 'rounded-b-xl',
  'shadow-none', 'shadow-sm', 'shadow', 'shadow-md', 'shadow-lg', 'shadow-xl', 'shadow-2xl', 'shadow-inner',
  'opacity-0', 'opacity-10', 'opacity-20', 'opacity-30', 'opacity-40', 'opacity-50', 'opacity-60',
  'opacity-70', 'opacity-80', 'opacity-90', 'opacity-100',
  'cursor-pointer', 'cursor-default', 'cursor-not-allowed', 'select-none', 'pointer-events-none',
  'transition', 'transition-all', 'transition-colors', 'transition-opacity', 'transition-transform',
  'duration-150', 'duration-200', 'duration-300', 'duration-500', 'ease-in', 'ease-out', 'ease-in-out',
  'object-cover', 'object-contain', 'object-center', 'aspect-square', 'aspect-video', 'aspect-auto',
  'z-0', 'z-10', 'z-20', 'z-30', 'z-40', 'z-50', 'z-auto',
  'w-px', 'h-px', 'mx-auto', 'my-auto', 'ml-auto', 'mr-auto',
  'sr-only', 'not-sr-only', 'backdrop-blur', 'backdrop-blur-sm', 'backdrop-blur-md', 'blur-sm',
  'ring', 'ring-0', 'ring-1', 'ring-2', 'ring-4', 'ring-inset', 'ring-offset-2',
  'outline-none', 'outline', 'outline-2', 'outline-offset-2',
  'grid-flow-row', 'grid-flow-col', 'col-auto', 'row-auto',
];
const SCALE_FAMILIES = {
  'text': ['xs', 'sm', 'base', 'lg', 'xl', '2xl', '3xl', '4xl', '5xl', '6xl', '7xl', '8xl', '9xl'],
  'leading': ['none', 'tight', 'snug', 'normal', 'relaxed', 'loose', '3', '4', '5', '6', '7', '8', '9', '10'],
  'tracking': ['tighter', 'tight', 'normal', 'wide', 'wider', 'widest'],
  'line-clamp': ['1', '2', '3', '4', '5', '6', 'none'],
  'grid-cols': ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10', '11', '12', 'none'],
  'grid-rows': ['1', '2', '3', '4', '5', '6', 'none'],
  'col-span': ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10', '11', '12', 'full'],
  'row-span': ['1', '2', '3', '4', '5', '6', 'full'],
  'divide-x': [''],
  'divide-y': [''],
};
// Responsive prefixes for the utilities a real screen layout needs at breakpoints.
const BREAKPOINTS = ['sm', 'md', 'lg', 'xl'];
const RESPONSIVE = [
  'flex', 'grid', 'hidden', 'block', 'flex-row', 'flex-col',
  'grid-cols-1', 'grid-cols-2', 'grid-cols-3', 'grid-cols-4', 'grid-cols-5', 'grid-cols-6',
  'text-sm', 'text-base', 'text-lg', 'text-xl', 'text-2xl', 'text-3xl', 'text-4xl', 'text-5xl',
  'p-4', 'p-6', 'p-8', 'px-4', 'px-6', 'px-8', 'py-4', 'py-6', 'py-8',
  'gap-2', 'gap-4', 'gap-6', 'gap-8', 'w-auto', 'w-full', 'w-1/2', 'w-1/3',
  'items-center', 'justify-between', 'text-left', 'text-center', 'mx-auto', 'max-w-md', 'max-w-lg',
  'max-w-2xl', 'max-w-4xl', 'max-w-6xl', 'max-w-7xl',
];

// Parse the `@theme inline { … }` block: it is the authoritative list of what
// the app actually exposes to Tailwind's namespace.
function themeBlock(css) {
  const start = css.indexOf('@theme');
  if (start === -1) throw new Error(`no @theme block in ${MAIN_CSS}`);
  const open = css.indexOf('{', start);
  let depth = 0;
  for (let i = open; i < css.length; i++) {
    if (css[i] === '{') depth++;
    else if (css[i] === '}' && --depth === 0) return css.slice(open + 1, i);
  }
  throw new Error(`unterminated @theme block in ${MAIN_CSS}`);
}

function namespace(block, prefix) {
  const names = new Set();
  const rx = new RegExp(`--${prefix}-([A-Za-z0-9_.\\\\-]+)\\s*:`, 'g');
  for (const m of block.matchAll(rx)) names.add(m[1].replace(/\\/g, ''));
  return [...names];
}

const block = themeBlock(readFileSync(MAIN_CSS, 'utf8'));
const colors = namespace(block, 'color');
const fonts = namespace(block, 'font');
const spacing = namespace(block, 'spacing');

if (!colors.length) throw new Error('parsed @theme but found no --color-* entries');

const out = new Set();
for (const c of colors) {
  for (const f of COLOR_FAMILIES) out.add(`${f}-${c}`);
  for (const f of ALPHA_FAMILIES) for (const a of ALPHAS) out.add(`${f}-${c}/${a}`);
  for (const v of VARIANTS) for (const f of VARIANT_FAMILIES) out.add(`${v}:${f}-${c}`);
}
for (const f of fonts) out.add(`font-${f}`);
for (const s of spacing) for (const f of SPACING_FAMILIES) out.add(`${f}-${s}`);

// Standard-scale backfill (see the lists above).
for (const f of STEP_FAMILIES) for (const s of STEPS) out.add(`${f}-${s}`);
for (const [f, vals] of Object.entries(KEYWORD_SIZES)) for (const v of vals) out.add(`${f}-${v}`);
for (const [f, vals] of Object.entries(SCALE_FAMILIES)) {
  for (const v of vals) out.add(v === '' ? f : `${f}-${v}`);
}
for (const c of MISC) out.add(c);
for (const bp of BREAKPOINTS) for (const c of RESPONSIVE) out.add(`${bp}:${c}`);

mkdirSync(CACHE, { recursive: true });
writeFileSync(SAFELIST, [...out].sort().join('\n') + '\n');
console.error(
  `safelist: ${out.size} candidates from ${colors.length} colours, ${fonts.length} fonts, ${spacing.length} spacing steps`,
);

// The CLI lives in the staged converter deps (.ds-sync), which re-sync recreates.
const cli = resolve(PKG, '.ds-sync/node_modules/.bin/tailwindcss');
execFileSync(cli, ['-i', ENTRY, '-o', OUT], { cwd: PKG, stdio: 'inherit' });

// Drop main.css's Google Fonts @import: the family is vendored locally instead
// (.design-sync/fonts, wired via cfg.extraFonts) so designs don't depend on the
// CDN being reachable. Both must never ship — two @font-face sets for one family
// means the LATER one wins, and a blocked src does not fall back to the earlier
// set, so leaving the remote import in would silently defeat the vendored copy.
// NB: the Google Fonts URL itself contains semicolons (wght@400;500;600;…), so
// matching up to the first `;` truncates mid-URL and leaves the remainder as
// garbage at the top of the sheet — which invalidates the @layer order
// declarations that follow and silently kills every utility. Match the quoted
// string to its closing quote instead.
let css = readFileSync(OUT, 'utf8');
const before = css;
css = css.replace(/@import\s+(?:url\(\s*)?(["'])https:\/\/fonts\.googleapis\.com.*?\1\s*\)?\s*;\s*/g, '');
if (css === before) {
  console.error('! expected a fonts.googleapis.com @import to strip and found none — check main.css');
} else if (/fonts\.googleapis\.com|display=swap/.test(css)) {
  throw new Error('remote font @import only partially stripped — leftover fragment would corrupt the sheet');
} else {
  console.error('stripped remote font @import (family vendored via cfg.extraFonts)');
}
writeFileSync(OUT, css);
console.error(`wrote ${OUT}`);
