# 07 — Accounts, Saved Bursaries & Admin

> Status: **Accepted v1.0** (2026-11-02). Implements brief FR-09–FR-14 and FR-40–FR-43. Auth approach: [ADR-003](adr/ADR-003-cookie-auth.md). UI follows [06-design](06-design.md).

## 1. Accounts (Epic A)
- **Sign-up/sign-in:** email + password (Identity defaults: 8+ characters, lockout after 5 failures) and **Google**. Email must be confirmed before saving anything (Identity `RequireConfirmedEmail`).
- **Email sending:** `IEmailSender` with two implementations: `LogEmailSender` (Development: writes the link to the console) and `SmtpEmailSender` (config `Email:Smtp:*`, used in production; any SMTP provider's free tier works).
- **Roles:** `Student` (default on sign-up) and `Admin`. At startup, users whose email is in config `Admin:Emails` (comma-separated) get the `Admin` role. No role-management UI.
- **The FR-09 rule:** saving a bursary, the tracker and the profile need an account. Tapping save while signed out opens the sign-in dialog, and the save completes automatically after sign-in (01-architecture §3.3).
- **API (all under `/api/v1`):** `auth/*` from `MapIdentityApi` (register, login with `useCookies=true`, logout, confirmEmail, resendConfirmationEmail, forgotPassword, resetPassword, manage/info); `auth/external/google` (challenge) + callback; `me` (id, email, roles).
- **Saved bursaries & tracker:** table `tracker_entry` (02-data-model §2.5): `user_id`, `bursary_id`, `status` (TrackerStatus), `applied_on?`, `notes ≤ 2000`, unique (`user_id`, `bursary_id`). "Saved" is simply an entry with status `Saved`. Endpoints: `GET /me/tracker`, `PUT /me/tracker/{bursaryId}` (upsert status/notes), `DELETE /me/tracker/{bursaryId}`.
- **Profile (FR-11) is deferred:** filters already live in the URL. Add it only if students ask.

## 2. Admin tab (Epic B)
- **Route:** `/admin`, visible in the header only for admins; the API enforces `[Authorize(Roles = "Admin")]` on every `/api/v1/admin/*` endpoint (the UI guard is convenience, not security).
- **Review queue (FR-41):** a list of open `review_item`s, newest first, filterable by type (New listing, Low confidence, Possible duplicate, Vanished, User report). Each item shows the bursary summary and the reason. Actions: **Publish** (sets `is_published`, resolves the item), **Edit** (opens the editor), **Dismiss** (resolves, with an optional note), **Mark closed**.
- **Bursary editor (FR-42):** edit name, funder, closing date, open date, study levels, min average, coverage, fields of study, apply URL, summary. Every field the admin changes is added to `locked_fields`, so ingestion never overwrites it. Shows "locked" next to locked fields with an unlock control.
- **Sources (FR-43):** a table of sources: enabled toggle, last run time, last run counts (discovered/created/updated/failed), last error. A **Run now** button (all sources, or one source) inserts an `ingestion_run_request`; the worker picks it up within a minute (T-011).
- **Bursary list:** search all bursaries by name (published or not) to edit any of them.

## 3. Out of scope for this MVP
Role management UI, user management, audit log, bulk actions, taxonomy editing in the UI (seed JSON stays the way to change it), email reminders (FR-15).
