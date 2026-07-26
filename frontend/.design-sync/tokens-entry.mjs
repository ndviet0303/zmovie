// Bundle entry for the tokens-only sync.
//
// zmovie is a Vue/Nuxt application, not a React component library, so there is
// nothing here for the claude.ai/design agent to import and render — see
// .design-sync/NOTES.md. This module is intentionally empty: it gives the
// converter a valid entry to resolve, which makes it emit the documented
// tokens-only output (an empty-bodied _ds_bundle.js plus the real stylesheet).
//
// The deliverable is the CSS: cfg.cssEntry points at the compiled zmovie
// token sheet. Do not add exports here expecting them to become components —
// component discovery reads a shipped .d.ts tree, which this package has none of.

export {};
