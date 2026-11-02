---
id: T-033
title: Home page hero as a fill-in-the-blanks search sentence
phase: 4
area: web
model: sonnet
status: done
depends_on: [T-032]
requirements: [FR-01, FR-32]
issue: 56
---

## Goal
The home page opens with the search itself: "Find bursaries for [degree] at [university] in [level] with a [__]% average. [Show bursaries]", built from real, accessible form controls (06 §5 Home).

## Context
- `AGENTS.md` (incl. "Frontend work"), **`specs/06-design.md` (governs; read all of it)**, `specs/05-web.md` (§5 Home wireframe, §7 Forms, §8 Copy)
- Existing: `src/web/src/app/features/home/**` (HomePage, DegreePickerComponent from T-027)

## Scope
You may create or modify **only**:
- `src/web/src/app/features/home/**`

## Requirements
1. Sentence layout per the 06 §5 wireframe, left-aligned, wrapping naturally at 320 px. The order reads as a sentence, but the *dependency* order still holds: choosing the university filters the degree list (degree select is disabled with the placeholder "Choose a university first…" until one is picked). Faculty is chosen implicitly: group degree options by faculty with `<optgroup>`, so no separate faculty select is needed here.
2. Each control has a visually-hidden `<label>` ("Degree", "University", "Study level", "Your average"). Average: `type="text" inputmode="numeric" autocomplete="off" name="average"`, placeholder "e.g. 72…", validated 0–100 with an inline error that says how to fix it.
3. Button "Show bursaries" (sentence case) → `/search` with query params (push), same params as before. It stays enabled; if required info is missing (university), it shows an inline error and moves focus there.
4. Under the sentence: "Browse by field:" followed by links to the 6 field landing pages (comma-separated real `<a>` links, no middle dots).
5. No hero image, gradient, stats block or eyebrow label.

## Acceptance criteria
- [ ] Tests: degree select disabled until a university is chosen; optgroups per faculty; the average validation message; submit navigates with the right params; missing university → inline error + focus; labels exist for every control
- [ ] Keyboard-only walk-through works (describe it in the PR); at 320 px nothing scrolls horizontally
- [ ] PR has the "Design checklist" section
- [ ] `npm test -- --watch=false`, `npm run lint` and `npm run build` pass
- [ ] Task file `status: done`

## How to verify
```bash
cd src/web && npm ci && npm test -- --watch=false && npm run lint && npm run build
```
