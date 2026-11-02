# 05 — Web App (Angular)

> Status: **Accepted v1.0** (2026-11-02). Implements brief FR-01–FR-08, NFR-01, NFR-02. Accounts, saving and the tracker (FR-09–FR-14) come in Phase 5.
> Conventions: [03-conventions §8](03-conventions.md). API contract: [01-architecture §6](01-architecture.md#6-api-surface-v1-mvp), [02-data-model §5](02-data-model.md#5-matching-algorithm), task T-022 (detail response).

## 1. Stack decisions
| Concern | Choice | Why |
|---|---|---|
| Framework | Angular, latest stable, standalone components, signals, new control flow (`@if`, `@for`) | Agreed stack |
| Rendering | Angular SSR (`@angular/ssr`, Express server) | SEO for bursary pages (NFR-02) |
| Styling | **Tailwind CSS** (build-time, no runtime cost) + CSS variables for design tokens | Small CSS output for mobile/data budgets (NFR-01) |
| Accessible primitives | **Angular CDK** only (dialog, a11y, overlay). No component library | Light, accessible |
| HTTP | `HttpClient` with `withFetch()` and an error interceptor that maps ProblemDetails to `ApiError` | SSR-friendly |
| Tests | The Angular CLI's default unit test runner; `npm test -- --watch=false` must work (CI runs it) | |
| Lint/format | Angular ESLint + Prettier | |

## 2. Hosting shape (for dev parity)
In production one origin serves everything: Caddy routes `/api/*` to the .NET API and everything else to the Angular SSR server. So **no CORS** is needed. In development, `proxy.conf.json` forwards `/api` to `http://localhost:5266`. The client always calls relative URLs (`/api/v1/...`). During SSR, the server uses the `API_INTERNAL_URL` env var as the base URL.

## 3. Routes
| Path | Page | Rendered | Notes |
|---|---|---|---|
| `/` | Home: one-line pitch + the **degree picker** (institution → faculty → qualification) + "Search" | SSR | The picker submits to `/search?...` |
| `/search` | Search results + filters | SSR (first page), then client | All state in the query string (§4) |
| `/bursaries/:slug` | Bursary detail | SSR | Closed (API `410`) → "This bursary has closed" view with summary only |
| `/bursaries/field/:fieldSlug` | SEO landing: open bursaries for a field of study | SSR | Same results component, field pre-applied |
| `/privacy`, `/about` | Static pages | SSR/prerender | Content placeholders in Phase 4 |
| `**` | 404 | SSR | Sets HTTP 404 status during SSR |

## 4. Search URL schema (FR-01–FR-03; the URL *is* the state, brief FR-09)
`/search?institutionId=&facultyId=&qualificationId=&fieldIds=&studyLevel=&average=&closingFrom=&closingTo=&includeClosed=&sort=&page=`
- Names match the API's search query parameters. `facultyId` is UI-only (used to restore the picker); it isn't sent to the API.
- `fieldIds` is comma-separated in the URL and repeated in the API call.
- Changing any filter resets `page` to 1 and **replaces** the URL (`replaceUrl`), so typing in "average" doesn't flood the browser history. Submitting from the home page **pushes**.
- Unknown or invalid params are dropped silently (e.g. `average=150`).

## 5. Components (feature folders under `src/web/src/app/`)
| Folder | Contents |
|---|---|
| `core/api/` | `ApiClient` services per feature (`TaxonomyApi`, `BursaryApi`, `ReportApi`), DTO interfaces mirroring the API exactly, `ApiError`, error interceptor, `API_BASE_URL` token |
| `shared/ui/` | `SourceIconsComponent`, `ClosingDateBadgeComponent`, `EmptyStateComponent`, `ErrorStateComponent`, `SkeletonCardComponent`, `ReportButtonComponent` |
| `features/home/` | `HomePage`, `DegreePickerComponent` (cascading selects, reusable on the search page) |
| `features/search/` | `SearchPage`, `FilterPanelComponent`, `SearchStateService` (URL ⇄ signals), `ResultListComponent`, `BursaryCardComponent`, `PaginationComponent` |
| `features/bursary/` | `BursaryDetailPage`, `ClosedBursaryComponent` |
| `features/report/` | `ReportDialogComponent` (CDK dialog) |
| `features/seo/` | `FieldLandingPage`, `SeoService` (title, meta description, canonical, Open Graph, JSON-LD) |

## 6. Behaviour rules
- **Result card (FR-04):** name, funder, closing date (+ "Closes in N days" badge when ≤ 14 days), study levels, min average, a coverage summary (first 3 items + "+N"), source icons (FR-05), and a report flag (FR-08). The whole card links to `/bursaries/:slug`.
- **Closed (FR-06):** the card is greyed out (reduced opacity plus a "Closed" badge), has no link (`aria-disabled="true"`, not focusable as a link), and still shows source icons. Closed results only appear when "Show closed" is ticked.
- **Source icons (FR-05):** one 16×16 favicon per source (`iconUrl`, lazy-loaded, `alt` = source name), each linking to that source's listing URL (`target="_blank" rel="noopener nofollow"`). If an icon fails to load, show the source's initials in a circle.
- **Apply button (FR-07):** "Apply on funder's site" → `applyUrl`, opens in a new tab with `rel="noopener nofollow"`.
- **States:** every data view has loading (skeletons), empty ("No bursaries match. Try removing the average filter", with a one-tap "Clear filters"), and error (retry button) states.
- **Report (FR-08):** the dialog has a reason (required radio list), comment (≤ 1000, with a counter) and an optional email. Submit → `POST /api/v1/bursaries/{id}/reports`. `201` → "Thanks, we'll check it" toast. `429` → "Too many reports from your connection, try later". `400` → inline field errors.

## 7. Non-functional
- **Performance budget (NFR-01):** Angular budgets measure **raw (uncompressed)** size. Initial bundle: warning at 300 KB raw, error at 350 KB raw (≈ 90–100 KB over the network). Any component style: error at 6 KB. Set in `angular.json`. *(Revised 2026-11-02: the bare scaffold alone is ~270 KB raw.)* No web fonts: use the system font stack. Images are lazy-loaded.
- **Accessibility:** WCAG 2.1 AA. Form controls have labels, focus is visible, the dialog traps focus and closes on Esc, colour contrast ≥ 4.5:1, and the layout works at 320 px wide.
- **SEO (NFR-02):** unique `<title>` and meta description per page, canonical URLs, `robots.txt`, `sitemap.xml` (an SSR server route that lists open bursaries and field pages via the API), and JSON-LD on the detail page (`Grant`-like `MonetaryGrant` schema.org type where fields allow).
- **Ads:** not in Phase 4. Leave a `<div class="ad-slot" data-slot="results-inline">` placeholder every 6 cards and one on the detail page, hidden for now (Phase 6 wires up AdSense and consent).

## 8. Design tokens (CSS variables in `styles.css`, used through Tailwind's theme)
| Token | Value | Use |
|---|---|---|
| `--color-primary` | `#0B6E4F` | Buttons, links (contrast-checked on white) |
| `--color-primary-ink` | `#FFFFFF` | Text on primary |
| `--color-accent` | `#F2A900` | Closing-soon badge, highlights (never text on white) |
| `--color-ink` | `#1A1A1A` | Body text |
| `--color-muted` | `#5C5C5C` | Secondary text |
| `--color-surface` | `#FFFFFF` / `--color-surface-alt` `#F5F7F6` | Cards / page background |
| `--radius` | `12px` | Cards, inputs |
| Font | `system-ui, -apple-system, "Segoe UI", Roboto, sans-serif` | |
