---
id: T-040
title: Web sign-in/sign-up dialog, auth state, header account menu, sign-in prompt replay
phase: 5
area: web
model: sonnet
status: done
depends_on: [T-038]
requirements: [FR-09, FR-10]
issue: 77
---

## Goal
Students can sign up, sign in (email or Google) and sign out from any page. Any "save" action taken while signed out opens the sign-in dialog and completes itself after sign-in (FR-09).

## Context
- `AGENTS.md` (incl. "smallest complete change"), `specs/03-conventions.md`, **`specs/07-accounts-and-admin.md`**, `specs/adr/ADR-003-cookie-auth.md` (§1)
- Frontend: **`specs/06-design.md` governs** (tokens, type, §7 checklist, §8 copy); `specs/05-web.md`
- `specs/01-architecture.md` §3.3 (requireAuth replay)

## Scope
- `src/web/src/app/core/auth/**` (new), `src/web/src/app/core/api/**` (auth calls, the `X-Requested-With: getabursary` header on every non-GET request)
- `src/web/src/app/features/account/**` (new: dialog + pages), the header/shell components, `app.routes.ts` (account routes only)

## Requirements
1. `AuthService` (signals): `user()` from `GET /api/v1/me` (loaded once at start; on SSR, forward the incoming cookie to the API), `signIn`, `signUp`, `signOut`, `isAdmin()`.
2. `requireAuth(action)`: if signed in, run now; else store the pending action in `sessionStorage` (try/catch), open the dialog, and replay it after sign-in, including after the Google redirect round trip.
3. Sign-in dialog (CDK dialog, 06 design): tabs "Sign in" / "Create account"; email (`type=email autocomplete=email spellcheck=false`), password (`autocomplete=current-password` / `new-password`); "Continue with Google" (link to `/api/v1/auth/external/google?returnUrl=<current path>`); "Forgot password?" link. After sign-up: "Check your email to confirm your account." Errors inline per 06 §7/§8.
4. Pages: `/account/confirm?userId&code` (calls confirmEmail, then shows "Email confirmed. You can save bursaries now." + sign-in), `/account/forgot`, `/account/reset?email&code`.
5. Header: signed out → "Sign in" button; signed in → account menu (email, "My bursaries" link (placeholder route until T-043), "Admin" link only if `isAdmin()`, "Sign out").

## Acceptance criteria
- [ ] Tests: AuthService loads `/me` and handles 401; `requireAuth` runs immediately when signed in and replays after sign-in when not; the dialog validates and shows API errors inline; the header shows the right controls per state; non-GET requests carry the header
- [ ] "Design checklist" in the PR; `npm test -- --watch=false`, `npm run lint`, `npm run build` pass; Task file `status: done`

## How to verify
```bash
cd src/web && npm ci && npm test -- --watch=false && npm run lint && npm run build
```
