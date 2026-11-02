---
id: T-039
title: Google sign-in (external login) for the API
phase: 5
area: api
model: sonnet
status: done
depends_on: [T-038]
requirements: [FR-10]
issue: 76
---

## Goal
"Continue with Google" signs a user in (creating the account on first use) and returns them to the page they started on.

## Context
- `AGENTS.md` (incl. "smallest complete change"), `specs/03-conventions.md`, **`specs/07-accounts-and-admin.md`**, `specs/adr/ADR-003-cookie-auth.md` (§1)

## Scope
- `src/GetABursary.Api/Extensions/AuthServiceCollectionExtensions.cs`, `src/GetABursary.Api/Controllers/ExternalAuthController.cs`, `src/GetABursary.Api/Services/Auth/**`, `src/GetABursary.Api/appsettings*.json` (keys only, empty values), `Directory.Packages.props`, `src/GetABursary.Api/GetABursary.Api.csproj` (`Microsoft.AspNetCore.Authentication.Google`)
- `tests/GetABursary.Api.Tests/Auth/**`, `docs/DEVELOPMENT.md` (how to create Google OAuth credentials)

## Requirements
1. `AddGoogle` only when `Auth:Google:ClientId` and `ClientSecret` are set (the app still starts without them).
2. `GET /api/v1/auth/external/google?returnUrl=/search?...` → challenge. The callback signs in an existing user by Google login or by verified email, or creates one (email confirmed, role `Student`, plus `Admin` if listed), sets the Identity cookie, then redirects to `returnUrl`. **`returnUrl` must be a local path** (reject absolute or `//` URLs: open-redirect protection).
3. `docs/DEVELOPMENT.md`: steps to create the Google OAuth client and the redirect URI (`{App:PublicBaseUrl}/signin-google`), stored with `dotnet user-secrets`.

## Acceptance criteria
- [ ] Tests: the challenge endpoint returns a redirect when configured and 404 when not; the callback handler (unit-tested with a fake external principal) creates or links the user and assigns roles; `returnUrl=https://evil.com` or `//evil.com` → redirect to `/`
- [ ] `dotnet build` and `dotnet test` pass; Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Auth"
```
