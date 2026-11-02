---
id: T-032
title: Design foundation - tokens, font, base styles, shell, formatting helpers
phase: 4
area: web
model: sonnet
status: todo
depends_on: [T-029]
requirements: [NFR-01, NFR-02]
issue: 55
---

## Goal
The web app uses the spec-06 design system everywhere: jacaranda/highlighter tokens, the self-hosted Atkinson Hyperlegible Next font, base styles that satisfy the §7 interface rules, a restyled header/footer, and shared Intl-based date/number formatting that is hydration-safe.

## Context
- `AGENTS.md` (incl. "Frontend work"), **`specs/06-design.md` (governs; read all of it)**, `specs/05-web.md`
- Existing code: `src/web/src/styles.css`, Tailwind config, `src/web/src/app/layout/**` (or wherever header/footer live), `shared/ui/closing-date-badge/**`

## Scope
You may create or modify **only**:
- `src/web/src/styles.css`, Tailwind config, `src/web/src/index.html`, `src/web/angular.json` (assets/fonts only), `src/web/package.json` + lockfile (font package only)
- Header/footer components and `app.component.*` (shell only)
- `src/web/src/app/shared/format/**` (new), `src/web/src/app/shared/ui/closing-date-badge/**`
- Replace any remaining old tokens (`#0B6E4F`, `#F2A900`, `primary`/`accent` classes) across `src/web/src/**` with the new token classes (mechanical find/replace only)

## Requirements
1. Tokens from 06 §3 as CSS variables, mapped into the Tailwind theme as `ink`, `paper`, `jacaranda`, `bloom`, `highlighter`, `muted`, `line`, `closed`. Remove the old green/gold tokens entirely.
2. Font per 06 §4: Fontsource variable package, Latin subset only, `font-display: swap`, a `<link rel="preload">` for the woff2, and the 1.25 type scale as Tailwind font sizes. Headings `text-wrap: balance`.
3. Base styles per 06 §7: `:focus-visible` 2 px jacaranda ring with 2 px offset (no bare `outline: none`); `touch-action: manipulation` on buttons, links and inputs; `-webkit-tap-highlight-color: transparent` plus a visible `:active` state; `color-scheme: light`; native `select` explicit bg/text colours; body `background: var(--paper)`; no `transition: all` anywhere.
4. `index.html`: `<meta name="theme-color" content="#FAFAFC">`, a viewport that **allows zoom**, `lang="en-ZA"`.
5. Shell: a "Skip to content" link as the first focusable element, targeting `<main id="content" tabindex="-1">`; header = wordmark "GetABursary" (weight 800, ink) + nothing else; footer per 05 with sentence-case copy.
6. `shared/format`: `formatDate(iso, style)`, `formatNumber` and `formatRand` using `Intl.DateTimeFormat('en-ZA')`/`Intl.NumberFormat('en-ZA', { currency: 'ZAR' })` (exposed as pipes too); a `TODAY` injection token whose value is set on the server and passed to the client through `TransferState`, so "closes in N days" renders identically on both (06 §7 Hydration).
7. Rework `ClosingDateBadgeComponent` to the ledger date block (06 §4–§5): day number 31 px/800 tabular-nums, month 13 px/600, highlighter swipe behind the day when ≤ 14 days (static here; the animation comes in T-034), "Closed" treatment. Use the format helpers.

## Acceptance criteria
- [ ] No old tokens left: `grep -rn "0B6E4F\|F2A900" src/web/src` returns nothing
- [ ] Tests: the format helpers (en-ZA output, ZAR currency); `TODAY` comes from TransferState on the client; skip link present and focuses `#content`; the date block thresholds (0, 1, 14, 15 days, closed)
- [ ] Viewport doesn't disable zoom; theme-color present; build within budgets (the font counts toward assets, not the JS budget)
- [ ] PR has the "Design checklist" section (AGENTS.md)
- [ ] `npm test -- --watch=false`, `npm run lint` and `npm run build` pass
- [ ] Task file `status: done`

## How to verify
```bash
cd src/web && npm ci && npm test -- --watch=false && npm run lint && npm run build
grep -rn "0B6E4F\|F2A900\|transition: all\|outline: none" src || echo clean
```
