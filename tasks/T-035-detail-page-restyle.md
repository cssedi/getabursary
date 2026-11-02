---
id: T-035
title: Restyle bursary detail page with date block and sticky apply bar
phase: 4
area: web
model: haiku
status: done
depends_on: [T-032]
requirements: [FR-06, FR-07]
issue: 58
---

## Goal
The detail page follows 06 §5 Detail: one 720 px column, the closing date block at the top, plain sections, and a full-width "Apply on {funder}'s site" button that sticks to the bottom on mobile.

## Context
- `AGENTS.md` (incl. "Frontend work"), **`specs/06-design.md` (governs; read all of it)**, `specs/05-web.md` (§5 Detail, §7 Layout/Focus, §8 Copy)
- Existing: `src/web/src/app/features/bursary/**` (from T-029)

## Scope
You may create or modify **only**:
- `src/web/src/app/features/bursary/**`

## Requirements
1. Max width 720 px, left-aligned. Top: the T-032 date block + name (h1) + funder + source icons.
2. Section headings in sentence case: "Who can apply", "Fields of study and qualifications", "What's covered", "Work-back obligation", "About this bursary". Hairline separators, no cards. Values formatted with the T-032 helpers (dates, ZAR amounts).
3. Apply button: label "Apply on {funder}'s site" (fallback "Apply on the funder's site"), jacaranda background, white text, min height 48 px. On < 768 px it's a sticky bottom bar with `padding-bottom: max(12px, env(safe-area-inset-bottom))`; the page gets matching bottom padding so the bar never covers focused content.
4. Closed view (`ClosedBursaryComponent`): date block in the closed treatment, "This bursary closed on {date}." and the similar-fields link, per 06 §8 copy. No apply bar.
5. "Last updated {date}" uses the format helper.

## Acceptance criteria
- [ ] Tests: headings present in sentence case; the apply label uses the funder name; the sticky bar class applies below 768 px (test the class/structure); closed view has no apply bar
- [ ] PR has the "Design checklist" section
- [ ] `npm test -- --watch=false`, `npm run lint` and `npm run build` pass
- [ ] Task file `status: done`

## How to verify
```bash
cd src/web && npm ci && npm test -- --watch=false && npm run lint && npm run build
```
