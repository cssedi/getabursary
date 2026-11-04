---
id: T-050
title: Visual check tooling - sample data and one-command screenshots of the running app
phase: 7
area: infra
model: sonnet
status: done
depends_on: []
requirements: [NFR-01]
issue: 100
---

## Goal
Any agent can run one command that starts the whole app locally with realistic sample bursaries and saves phone and desktop screenshots of chosen pages, so UI work is checked by eye before review (spec 06 §9).

## Context
- `AGENTS.md`, **`specs/06-design.md` v2 (governs; read all of it, including §7 checklist and §9 visual check)**, `specs/05-web.md`
- Skills: frontend-design and web-design-guidelines principles are already distilled into spec 06; follow the spec
- `docs/DEVELOPMENT.md`, `docker-compose.yml`, `specs/02-data-model.md` (tables), the EF migrations in `src/GetABursary.Core/Data/Migrations/` (exact column names)

## Scope
- `scripts/visual-check.mjs` (new), `data/dev/sample-bursaries.sql` (new), `src/web/package.json` + `package-lock.json` (add `playwright` as a devDependency and a `"visual-check"` npm script), `docs/DEVELOPMENT.md` (a "Visual check" section), `.gitignore` (if needed)

## Requirements
1. `node scripts/visual-check.mjs <task-id> <path> [<path>…]` (Node only, no bash needed): `docker compose up -d db` and wait for healthy → start the API (`dotnet run --project src/GetABursary.Api`, Development, `Database__MigrateOnStartup=true`, port 5266) and wait for `/api/v1/health` → load `data/dev/sample-bursaries.sql` with `docker compose exec -T db psql` → start the web dev server (`npm start` in `src/web`) and wait for port 4200 → for each path, a full-page Playwright screenshot at 375×812 and 1440×900 to `docs/screenshots/<task-id>/<slug>-mobile.png` / `-desktop.png` (slug from the path, e.g. `home`, `search`, `bursary-sasol-bursary`) → stop the API and web server (leave the db running). Exits non-zero with a clear message if any step fails. Prints the saved file paths.
2. Optional `--sign-in-admin`: signs in as a dev admin before shooting (create a confirmed admin in the SQL, password documented in DEVELOPMENT.md as dev-only), so tracker/admin pages can be shot.
3. `data/dev/sample-bursaries.sql`: idempotent (`ON CONFLICT DO NOTHING` / fixed UUIDs), about 15 published bursaries with real-looking SA funders (e.g. Sasol, Exxaro, City of Cape Town, Investec, Nedbank, Transnet, Eskom, SAICA Thuthuka), fields and institutions referenced by seeded slugs, closing dates **relative to now()** (3 within 14 days, 8 later, 3 closed, 1 unknown), mixed study levels and minimum averages, and **6 bursaries with 2–3 `bursary_source` rows** whose URLs look like real aggregator pages (e.g. `https://www.zabursaries.co.za/government-bursaries-south-africa/city-of-cape-town-bursary/`, `https://studytrust.org.za/bursaries/...`). Plus one very long bursary/funder name to test wrapping.
4. Playwright: use the npm package and `npx playwright install chromium` (document it); disable animations for stable shots (`reducedMotion: 'reduce'`).

## Acceptance criteria
- [ ] Running `node scripts/visual-check.mjs T-050 / /search` on a clean checkout produces 4 PNGs; commit them under `docs/screenshots/T-050/` and open them yourself (Read tool) to confirm they show the app with sample data
- [ ] Running the sample SQL twice gives no errors and no duplicates
- [ ] `docs/DEVELOPMENT.md` explains the command in 5 lines or fewer
- [ ] `dotnet build`, `npm test -- --watch=false` and `npm run build` (in `src/web`) still pass; Task file `status: done`

## How to verify
```bash
npx --yes playwright install chromium
node scripts/visual-check.mjs T-050 / /search
```
