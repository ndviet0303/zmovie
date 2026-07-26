# zmovie — brand & styling conventions

**There are no importable components in this design system.** `window.ZMovie` is
intentionally empty and `components/` is empty. zmovie's own UI is Vue/Nuxt, which
cannot be rendered here, so what ships is the **brand layer**: tokens, typography and
the exact utility-class vocabulary. Build screens as ordinary React + JSX markup and
style them with the classes below — never try to import a zmovie component.

## Dark-only, single theme

There is one theme and it is dark. `--background: #181a20`, `--foreground: #fafafa`.
There is no light mode and no `dark:` variant setup — do not write `dark:` classes and
do not assume a light surface anywhere. Page background and body text colour are already
applied to `<body>`; surfaces stack **upward** from the page, not downward:

`bg-surface-container-lowest` (#121419, below page) → `bg-background` (#181a20, page) →
`bg-surface-container` (#1f222a, cards/panels) — with `border-border` (#35383f) for edges.

## The styling idiom: precompiled Tailwind utilities

Style with utility classes. Two hard constraints, because the stylesheet is **precompiled
and there is no Tailwind running at render time**:

1. **Arbitrary values do not work.** `p-[13px]`, `bg-[#ff0000]`, `w-[42%]` produce nothing.
2. **Only shipped classes resolve.** The standard scale is complete — spacing steps
   `0 0.5 1 1.5 2 2.5 3 3.5 4 5 6 7 8 9 10 11 12 14 16 20 24 28 32 36 40 44 48 52 56 60 64 72 80 96`
   across `p/px/py/pt/pr/pb/pl`, `m/mx/my/mt/mr/mb/ml`, `gap`/`gap-x`/`gap-y`,
   `space-x`/`space-y`, `w/h/min-w/min-h/max-w/max-h/size`, `top/right/bottom/left/inset`;
   `text-xs`…`text-9xl`; `rounded-*` through `rounded-3xl` + `rounded-full`;
   `grid-cols-1`…`12`; `sm: md: lg: xl:` on common layout/type/spacing classes.
   For anything exotic, use an inline `style` with a `var(--token)` instead of inventing a class.

Every colour token below exists as `bg-*`, `text-*`, `border-*` and `ring-*`, each also with
`/5 /10 /20 /30 /40 /50 /60 /70 /80 /90` alpha steps and `hover: focus: focus-visible: active:
disabled: group-hover:` variants. The same names are available as CSS custom properties
(`var(--primary)`) for inline styles.

| Family   | Class names                                                                                                                                                               |
| -------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Brand    | `primary` (#f89300 amber — the accent for CTAs), `primary-foreground`, `primary-container`, `primary-container-foreground`, `secondary` (#ffc107), `secondary-foreground` |
| Surfaces | `background`, `surface-container`, `surface-container-lowest`, `input`, `border`, `ring`                                                                                  |
| Text     | `foreground` (primary copy), `muted-foreground` (#bdbdbd secondary copy), `tertiary` (#9e9e9e de-emphasised/meta)                                                         |
| Status   | `success` (#12d18e), `warning` (#facc15), `destructive` (#f75555), `disabled`                                                                                             |
| Neutrals | `gray-50` … `gray-900`                                                                                                                                                    |

## Typography

Nunito Sans, vendored locally (weights 400–900, incl. Vietnamese diacritics — zmovie ships
Vietnamese and English copy, so never substitute a font lacking them). `font-sans` and
`font-display` are both Nunito Sans; use `font-display` on headings to keep intent explicit.
Body text is weight 500, not 400. zmovie leans heavy for headings: `font-bold` /
`font-extrabold`. Tailwind preflight resets heading sizes, so always set size and weight
explicitly on an `<h1>`/`<h2>`.

## Where the truth lives

Read `_ds/<folder>/styles.css` and its two imports before styling: `fonts/fonts.css`
(the `@font-face` set) and `_ds_bundle.css` (all tokens under `:root` plus every utility).
Grepping `_ds_bundle.css` for a class is the definitive check on whether it will resolve.

## An idiomatic snippet

```jsx
<section className="bg-surface-container border-border rounded-xl border p-8">
  <div className="mb-6 flex items-center justify-between">
    <div>
      <h2 className="font-display mb-1 text-2xl font-bold">Tiếp tục xem</h2>
      <p className="text-muted-foreground text-sm">Tập 4 · còn 22 phút</p>
    </div>
    <button className="bg-primary text-primary-foreground hover:bg-primary/90 rounded-lg px-6 py-3 font-bold">
      Xem tiếp
    </button>
  </div>
  <div className="grid grid-cols-2 gap-6 md:grid-cols-4">
    <article className="bg-surface-container-lowest border-border overflow-hidden rounded-xl border">
      <div className="bg-primary/20 aspect-video" />
      <div className="p-4">
        <h3 className="mb-1 truncate text-sm font-bold">Dune: Part Two</h3>
        <p className="text-tertiary text-xs">2024 · Khoa học</p>
      </div>
    </article>
  </div>
</section>
```
