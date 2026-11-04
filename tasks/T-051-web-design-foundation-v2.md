---
id: T-051
title: Design v2 foundation - tokens, fonts, header and nav, base controls, hydration fixes
phase: 7
area: web
model: sonnet
status: todo
depends_on: [T-050]
requirements: [NFR-01, NFR-03]
issue: 101
---

## Goal
The whole site moves to the v2 look (warm paper, ink, jacaranda, highlighter; Bricolage Grotesque + Newsreader) with a proper header/nav and footer, base controls that work on phones, and hydration that never drops a tap.

## Context
- `AGENTS.md`, **`specs/06-design.md` v2 (governs; read all of it, including §7 checklist and §9 visual check)**, `specs/05-web.md`
- Skills: frontend-design and web-design-guidelines principles are already distilled into spec 06; follow the spec (§2–§4, §5 Header, §6, §7)

## Scope
- `src/web/src/styles.css`, Tailwind theme config, `src/web/src/index.html` (preload, theme-color), `src/web/package.json` + lock (add the two `@fontsource-variable` packages, remove Atkinson)
- `src/web/src/app/app.config.ts` (`provideClientHydration(withEventReplay(), withIncrementalHydration())`), the shell/header/footer components, `src/web/src/app/shared/ui/**` (restyle only; keep APIs)
- Existing spec files only where markup changes break them (standing allowance), `docs/screenshots/T-051/**`

## Requirements
1. Tokens and type exactly as spec 06 §3–§4 (CSS variables mapped into Tailwind; remove v1-only tokens).
2. Header per §5: wordmark, nav, account menu; < 640 px a menu button opening a sheet (CDK dialog, focus trapped, Esc closes, `overscroll-behavior: contain`). Sticky with a hairline only when scrolled (IntersectionObserver sentinel, no scroll listeners).
3. Base controls (global styles or small shared classes): primary/secondary buttons, text inputs, native `<select>` (custom chevron via background SVG, explicit colours, 48 px min height, full-width on mobile), checkboxes, focus ring. Disabled controls look disabled **and** have a visible reason where used.
4. Footer: About, Privacy, "Listings come from public bursary sites. Always confirm on the funder's site."
5. No horizontal scroll at 320 px on any existing page; nothing overlaps.

## Acceptance criteria
- [ ] Screenshots (spec 06 §9) of `/`, `/search`, one bursary detail and `/about`, mobile + desktop, committed and reviewed by you; the PR lists them
- [ ] Tests: header nav sheet opens/closes with keyboard; event replay is configured (app.config test or a hydration test)
- [ ] Bundle within the 05 budgets (`npm run build` output in the PR)
- [ ] "Design checklist" (06 §7) in the PR; `npm test -- --watch=false`, `npm run lint`, `npm run build` pass; Task file `status: done`

## How to verify
```bash
cd src/web && npm ci && npm test -- --watch=false && npm run lint && npm run build && cd ../..
node scripts/visual-check.mjs T-051 / /search /about
```
