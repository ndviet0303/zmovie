# design-sync notes — zmovie

Synced to claude.ai/design project `544c926a-0300-4b68-84b9-3be7684112fd`
("ZMovie Design System"). Config home is **`frontend/`**, so every
package-relative path in `config.json` resolves from there. Run all commands
from `frontend/`, not the repo root.

## Why this is a tokens-only sync

- zmovie is a **Vue/Nuxt app**, and design-sync's scope is React design systems —
  `_ds_bundle.js` and the preview cards both render through React. There is nothing
  in 37 `.vue` SFCs the claude.ai/design agent can import.
- It is also **not a library**: `package.json` declares no `main`/`module`/`exports`,
  and `frontend/dist` is a symlink to `.output/public` (the built Nuxt _site_).
- So the deliverable is the brand layer: tokens, the utility-class vocabulary, and
  Nunito Sans. `components/` ships empty and `window.ZMovie` is an empty object —
  **that is intended, not a broken build.** `.design-sync/conventions.md` tells the
  design agent this up front so it doesn't try to import components.
- If a React component library is ever added, re-scope: point `cfg.entry` at its built
  entry, drop `cfg.entry`'s tokens stub, and the normal component flow applies.

## Build specifics (don't rediscover these)

- **`--node-modules` must be `./.ds-sync/node_modules`, not `./node_modules`.**
  The converter vendors React into `_vendor/` unconditionally — before it reaches the
  tokens-only branch — and zmovie has no React dependency. React lives in the staged
  converter deps instead, so the app's `package.json` stays clean. Passing the app's
  node_modules fails with `react not found under --node-modules`.
- **`cfg.entry` is `.design-sync/tokens-entry.mjs`, an intentionally empty module.**
  The entry is resolved _before_ the tokens-only check, so with no entry and no
  `src/`/`lib/`/`components/` directory under `frontend/`, the build exits `[NO_DIST]`.
  The stub gives it something to resolve; `PKG_DIR` is then found by walking up to
  `frontend/package.json`.
- **`cfg.buildCmd` (`node .design-sync/css/build.mjs`) must run before the converter.**
  It writes `cfg.cssEntry` (`.design-sync/.cache/zmovie-tokens.css`), which is gitignored —
  so on a fresh clone the converter has no stylesheet until it runs. The driver does not
  run it for you.
- Converter deps: `npm i esbuild ts-morph @types/react @tailwindcss/cli react react-dom playwright`
  inside `.ds-sync/`, plus `npx playwright install chromium`.
- `[DTS_REACT] @types/react not found` may appear — harmless here (zero components means
  no prop extraction).

## The stylesheet: why it is built, not copied

`.design-sync/css/build.mjs` compiles `assets/css/main.css` (the real token source) with
the Tailwind v4 CLI. Two `@source` scans, both deliberate:

- `app/**` reproduces the utilities zmovie itself uses, from source — so the sheet does
  not depend on a Nuxt build artifact existing.
- a generated safelist emits the **full** vocabulary. This matters: scanning alone leaves
  arbitrary holes (`p-7`/`p-10`/`p-16` shipped but `p-8` did not; `mb-5`/`mb-8` but not
  `mb-6`), and a design agent writing `p-8` would silently get no padding. The safelist is
  derived from main.css's `@theme` block, so adding a colour there flows through
  automatically — do not hand-maintain a class list.
- Result: ~436 KB, ~3800 selectors. Chunky for a stylesheet but it gzips well, and the
  alternative is silently missing utilities. Trim via the family/step lists at the top of
  `build.mjs` if it ever needs to shrink.
- **No Tailwind runs at render time**, so arbitrary values (`p-[13px]`, `bg-[#abc]`) can
  never work in a design. The conventions header states this explicitly.

## Fonts: vendored on purpose

`main.css` pulls Nunito Sans from the Google Fonts CDN. That is left intact in the app, but
the _exported_ sheet vendors the family locally (`.design-sync/fonts/`, wired through
`cfg.extraFonts`) and `build.mjs` **strips the remote `@import`**. Reasons:

- A design rendered somewhere that blocks external hosts would silently fall back to system
  sans, and nothing downstream catches that.
- Shipping both is actively worse than either: two `@font-face` sets for one family means the
  later one wins, and a failed `src` does **not** fall back to the earlier set.
- Google serves Nunito Sans as a **variable** woff2 — every requested weight in a subset is
  the same file. `fetch.mjs` collapses them to one face per subset with `font-weight: 400 900`;
  not doing so shipped the same bytes 6× (444 KB → 80 KB). `fetch.mjs` throws if a subset ever
  serves distinct files per weight, which would mean per-weight faces are needed again.
- Subsets kept: latin, latin-ext, **vietnamese** (zmovie ships Vietnamese copy — never drop
  this one or substitute a font lacking the diacritics). Cyrillic dropped.
- Licensing: Nunito Sans is OFL 1.1, so redistribution is fine.

### Trap: stripping that `@import`

The Google Fonts URL contains semicolons (`wght@400;500;600;…`). A regex matching to the
first `;` truncates mid-URL and leaves `500;600;700;800;900&display=swap");` at the top of the
sheet, which invalidates the `@layer` order declarations that follow and **kills every
utility** while colours and fonts keep working — so it looks fine at a glance. The fix in
`build.mjs` matches the quoted string to its closing quote and hard-fails if any
`fonts.googleapis.com`/`display=swap` fragment survives. Don't loosen that check.

## Verification done (and how to redo it)

There are no preview cards, so the converter's render check is vacuous (`0/0`). The real
verification was a hand-built probe page linking **only `styles.css`** (all a rendered design
receives), screenshotted with every external request blocked:

- tokens resolve (`#181a20` page, `#1f222a` surfaces, `#f89300` primary), alpha compositing works
- layout works: `p-8`→32px, `md:grid-cols-4`, `rounded-xl`→12px, `aspect-video`→16/9, `text-4xl`→36px
- 3 `@font-face` entries report `loaded`, Vietnamese diacritics render, **0 external requests**

To redo: write a probe to `ds-bundle/.probe.html` (dot-prefixed = never uploaded, and wiped by
every rebuild — keep the source elsewhere) and drive it with playwright, aborting any non-`file://`
request. Checking computed styles beats eyeballing: the corrupted-CSS bug above still _looked_
plausible until `main`'s padding was measured.

## Known render warns

None. `[FONT_REMOTE]` and `[FONT_MISSING]` are both expected to stay absent now that the family
is vendored and the remote import stripped — if either reappears, the font wiring regressed.

## Re-sync risks

- **`cfg.cssEntry` is gitignored output.** Forget `buildCmd` and the converter either fails or
  ships a stale sheet. Always run it first.
- **The safelist tracks `@theme` only.** A colour added to `:root` in main.css but not mapped in
  `@theme inline` gets no utilities. Check the `colours declared in @theme` count in build output.
- **Standard-scale lists in `build.mjs` are hand-picked.** They cover Tailwind's defaults as of
  v4.3.3; a Tailwind major bump could rename or add steps. Re-run the coverage probe after upgrading.
- **`conventions.md` enumerates real class and token names** — it was validated against the built
  CSS at sync time. If tokens are renamed in main.css, re-validate it (grep each name against
  `ds-bundle/_ds_bundle.css`) rather than assuming it is still true; the design agent trusts it
  and will emit vocabulary that silently doesn't resolve.
- **Fonts are committed, so builds need no network** — but `fetch.mjs` does. Only re-run it to
  refresh, and re-check the variable-font dedupe assumption if Google's response shape changes.
- `_vendor/react*.js` (1.1 MB) ships unused, since there are no preview cards to render. Harmless;
  the app's self-check expects the layout.
