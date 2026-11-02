---
id: T-038
title: Identity with cookie auth, email confirmation and admin seeding (API)
phase: 5
area: api
model: sonnet
status: todo
depends_on: []
requirements: [FR-09, FR-10, FR-40]
issue: 75
---

## Goal
Users can register, confirm their email, sign in and out with a secure cookie. Admins are set by config. Everything comes from ASP.NET Identity's built-in endpoints (ADR-003), not hand-written auth.

## Context
- `AGENTS.md` (incl. "smallest complete change"), `specs/03-conventions.md`, **`specs/07-accounts-and-admin.md`**, `specs/adr/ADR-003-cookie-auth.md` (§1)
- `specs/02-data-model.md` §2.5 (Identity tables, Guid keys)

## Scope
- `src/GetABursary.Core/Domain/Users/**`, `src/GetABursary.Core/Data/**` (Identity in the DbContext + **one** migration `AddIdentity`), `src/GetABursary.Core/GetABursary.Core.csproj`, `Directory.Packages.props`
- `src/GetABursary.Api/Program.cs`, `src/GetABursary.Api/Extensions/AuthServiceCollectionExtensions.cs`, `src/GetABursary.Api/Services/Auth/**` (email senders, admin seeding), `src/GetABursary.Api/Controllers/MeController.cs`, `src/GetABursary.Api/DTOs/Auth/**`, `src/GetABursary.Api/appsettings*.json` (non-secret defaults)
- `tests/GetABursary.Api.Tests/Auth/**`

## Requirements
1. `User : IdentityUser<Guid>`; `GetABursaryDbContext` derives from `IdentityDbContext<User, IdentityRole<Guid>, Guid>` (keep snake_case naming). One migration `AddIdentity`.
2. `AddIdentityApiEndpoints<User>()` + roles; `app.MapGroup("/api/v1/auth").MapIdentityApi<User>()`. Cookie: `HttpOnly`, `SecurePolicy=Always` outside Development, `SameSite=Lax`; API returns 401/403 (no redirects to login pages). `RequireConfirmedEmail = true`; password min 8.
3. `IEmailSender<User>`: `LogEmailSender` (Development, logs the link) and `SmtpEmailSender` (`Email:Smtp:Host/Port/User/Password/From`) using `System.Net.Mail` or MailKit only if already present. Links point to the web app (`App:PublicBaseUrl` + `/account/confirm` and `/account/reset` with query params).
4. Roles `Student` and `Admin` are ensured at startup. New users get `Student`. Users whose email is in `Admin:Emails` get `Admin` (at startup and right after their email is confirmed).
5. `GET /api/v1/me` (authorized) → `{ id, email, roles[] }`; 401 when signed out.
6. Cheap CSRF insurance: reject state-changing `/api/v1/*` requests (POST/PUT/PATCH/DELETE) without the header `X-Requested-With: getabursary` (the web client will send it). Existing anonymous POST (reports) must keep working with the header; update its tests via the standing allowance.

## Acceptance criteria
- [ ] Integration tests: register → confirmation link logged → confirm → login sets a cookie → `/me` returns the email + `Student`; login before confirming fails; wrong password 5× locks out; logout clears; `Admin:Emails` user gets `Admin`; POST without the header → 400
- [ ] No secrets in appsettings; `dotnet build` and `dotnet test` pass
- [ ] Task file `status: done`

## How to verify
```bash
dotnet test --filter "FullyQualifiedName~Auth"
```
