---
id: T-025
title: Scaffold Angular SSR app with Tailwind, app shell and dev proxy
phase: 4
area: web
model: sonnet
status: todo
depends_on: []
requirements: [NFR-01, NFR-02]
issue: 34
---

## Goal
A running Angular SSR app in `src/web` with the app shell (header, footer, routes stubbed), design tokens, Tailwind, a dev proxy to the API, size budgets and passing tests, so CI's `web` job builds and tests it.

## Context
- `AGENTS.md`, `specs/03-conventions.md` §8
- **`specs/05-web.md`** §1, §2, §3, §7, §8 (all of it)

## Scope
You may create or modify **only**:
- `src/web/**`
- `.github/workflows/ci.yml` only if the web job's commands must change to match the CLI defaults (explain in the PR)

## Requirements
1. Create the app with the Angular CLI (latest stable): standalone, routing, SSR (`--ssr`), CSS. Project name `getabursary-web`, root `src/web`. Commit `package-lock.json`.
2. Install and configure Tailwind CSS (official Angular guide for the version you install). Define the design tokens from spec §8 as CSS variables in `styles.css` and map them into Tailwind's theme (`primary`, `accent`, `ink`, `muted`, `surface`, `surface-alt`). Use the system font stack.
3. App shell: `HeaderComponent` (text logo "GetABursary" linking to `/`), `FooterComponent` (links to `/about` and `/privacy`, and "Bursary info is gathered from public sources. Always confirm details on the funder's site.").
4. Routes from spec §3 as **lazy-loaded placeholder components**, each showing its page name: `HomePage`, `SearchPage`, `BursaryDetailPage`, `FieldLandingPage`, `AboutPage`, `PrivacyPage`, `NotFoundPage`. `NotFoundPage` sets HTTP 404 during SSR (use the `RESPONSE_INIT`/response token provided by `@angular/ssr` for your version).
5. `provideHttpClient(withFetch())` and an `API_BASE_URL` injection token: `''` in the browser, `process.env['API_INTERNAL_URL'] ?? 'http://localhost:5266'` on the server.
6. `proxy.conf.json` forwarding `/api` to `http://localhost:5266`, wired into `ng serve`.
7. `angular.json` budgets: initial ≤ 250 KB (error), any component style ≤ 6 KB (error).
8. Install `@angular/cdk` (used by later tasks).
9. npm scripts: `start`, `build`, `test` (must work with `-- --watch=false`), `lint` (Angular ESLint), `serve:ssr`.
10. `src/web/README.md`: how to run (`npm ci`, `npm start`, run the API on 5266), test and build.

## Acceptance criteria
- [ ] `cd src/web && npm ci && npm test -- --watch=false && npm run build` passes
- [ ] Tests: the app shell renders the header logo text and footer links; each route resolves to its placeholder; `NotFoundPage` exists for unknown routes
- [ ] SSR build output exists and `npm run serve:ssr` serves `/` with the header in the server-rendered HTML (state the command you used in the PR)
- [ ] Budgets configured as above, and the build is within them
- [ ] CI `web` job is green
- [ ] Task file `status: done`

## How to verify
```bash
cd src/web && npm ci && npm test -- --watch=false && npm run build && npm run lint
```
