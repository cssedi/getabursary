# ADR-003: Cookie auth with ASP.NET Identity's built-in endpoints (not JWT)

- **Status:** Accepted, 2026-11-02 (supersedes the "JWT bearer" line in 01-architecture §4)
- **Deciders:** @cssedi, orchestrator

## Context
The web app and API share one origin (05-web §2), and pages are server-rendered. We need email/password and Google sign-in, plus `Student` and `Admin` roles.

## Decision
Use **ASP.NET Core Identity with cookie authentication** and the built-in **`MapIdentityApi<User>()`** endpoints (register, login, logout, confirm email, forgot/reset password), mounted at `/api/v1/auth`. Add Google as an external login. No JWT, no token storage in the browser.

## Consequences
- ✅ Far less code: Identity already ships the endpoints, password hashing, lockout and email-token flows.
- ✅ Safer: an `HttpOnly`, `Secure`, `SameSite=Lax` cookie can't be read by scripts. Nothing to keep in `localStorage`.
- ✅ Works with SSR: the server render forwards the cookie to the API.
- ❌ Only works same-origin, which is our hosting model. A future native app would need tokens (Identity can issue bearer tokens from the same endpoints if that happens).
- Anti-forgery: state-changing requests come from our own origin with `SameSite=Lax`. Add an `X-Requested-With`/anti-forgery header check on POST/PUT/DELETE as cheap insurance.
