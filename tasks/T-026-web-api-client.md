---
id: T-026
title: Typed API client, DTO models and error interceptor for the web app
phase: 4
area: web
model: haiku
status: done
depends_on: [T-025]
requirements: [FR-01, FR-04, FR-07, FR-08]
issue: 35
---

## Goal
The web app has typed services for every MVP API endpoint, with DTO interfaces that mirror the API exactly and one consistent error type, so page tasks never touch `HttpClient` directly.

## Context
- `AGENTS.md`, `specs/05-web.md` §1, §2, §5 (`core/api/`)
- API contract: `specs/01-architecture.md` §6; `specs/02-data-model.md` §5 (Output: `BursarySummaryResponse`); `tasks/T-020-taxonomy-read-endpoints.md` (taxonomy responses); `tasks/T-021-bursary-search-matching.md` (query params, `PagedResponse`); `tasks/T-022-bursary-detail-endpoint.md` (`BursaryDetailResponse`, 410 shape); `tasks/T-023-report-inaccuracy-endpoint.md` (report request)

## Scope
You may create or modify **only**:
- `src/web/src/app/core/api/**`
- `src/web/src/app/app.config.ts` (register the interceptor)
- `src/web/src/testing/**` (shared test fixtures)

## Requirements
1. `models.ts`: TypeScript interfaces for `InstitutionResponse`, `FacultyResponse`, `QualificationResponse`, `FieldOfStudyResponse`, `BursarySearchParams`, `BursarySummaryResponse`, `SourceLink`, `PagedResponse<T>`, `BursaryDetailResponse`, `CreateReportRequest`, plus string-union types for every enum (`StudyLevel`, `CoverageItem`, `Citizenship`, `Province`, `ReportReason`, `SearchSort`). Field names are camelCase, exactly as the specs/tasks list them. Dates are ISO strings.
2. `ApiError` class: `status`, `code` (first error code from ProblemDetails `extensions.errors`, or `'Network'`), `message`, `errors[]`, and `extensions` (raw). `errorInterceptor` converts every non-2xx `HttpErrorResponse` into an `ApiError`.
3. Services (`providedIn: 'root'`, using `API_BASE_URL`):
   - `TaxonomyApi`: `institutions(includeAll?)`, `faculties(institutionId)`, `qualifications(facultyId)`, `fields()`
   - `BursaryApi`: `search(params)` (repeats `fieldIds`, omits empty params), `bySlug(slug)` returns `Observable<{ kind: 'open', bursary } | { kind: 'closed', summary }>` (maps a `410` `ApiError` with `extensions.bursary` to `closed`)
   - `ReportApi`: `create(bursaryId, request)`
4. `src/web/src/testing/fixtures.ts`: realistic sample objects for each DTO (2 institutions, 2 faculties, 3 qualifications, 3 bursaries including one closed and one closing soon) for later page tests.

## Acceptance criteria
- [ ] Unit tests with `HttpTestingController`: correct URLs and query strings for each method (including repeated `fieldIds` and omitted empty params); `bySlug` maps `200` → open and `410` → closed; the interceptor turns a ProblemDetails `400` into `ApiError` with the right `code`; a network error gives `code: 'Network'`
- [ ] `npm test -- --watch=false`, `npm run build` and `npm run lint` pass
- [ ] Task file `status: done`

## How to verify
```bash
cd src/web && npm ci && npm test -- --watch=false && npm run lint && npm run build
```
