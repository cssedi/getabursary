# Web design audit (T-036)

Every `.ts`, `.html` and `.css` file under `src/web/src/` (non-spec) checked against specs/06-design.md §7, §4 and §8. Paths are relative to `src/web/src/`. Line numbers are from before this PR's edits.

## index.html
✓ pass (viewport allows zoom, `theme-color` matches `--paper`, font preloaded, `lang="en-ZA"`).

## styles.css
- `styles.css:123` no `scroll-padding-bottom`, so the sticky apply bar can cover a focused element → fix: `html { scroll-padding-bottom: 96px }`.
- `styles.css:78` links have no stronger hover state when underlined → fix: `a:hover { text-decoration-thickness: 2px }`.

## main.ts, main.server.ts, server.ts
✓ pass (no UI).

## testing/fixtures.ts
✓ pass (test data only).

## app/app.ts
✓ pass (skip link, `#content` has `scroll-margin-top`).

## app/app.config.ts, app/app.config.server.ts, app/app.routes.ts, app/app.routes.server.ts
✓ pass (no UI).

## app/layout/header.component.ts
✓ pass.

## app/layout/footer.component.ts
- `footer.component.ts:14` straight apostrophe in "funder's" → fix: curly ’.
- `footer.component.ts:11-12` links have hover but no pressed state → fix: `active:text-ink`.

## app/core/api/* (api-base-url, api-error, bursary-api, error.interceptor, models, query-params, report-api, taxonomy-api)
✓ pass (no UI).

## app/shared/format/format.ts, today.ts
✓ pass (`Intl` with `en-ZA`, no hard-coded patterns).

## app/shared/ui/closing-date-badge/closing-date-badge.component.ts
✓ pass.

## app/shared/ui/empty-state/empty-state.component.ts
- `empty-state.component.ts:12` button has no hover state and is under 44 px → fix: `min-h-11 hover:bg-ink active:bg-ink`.

## app/shared/ui/error-state/error-state.component.ts
- `error-state.component.ts:11` same button problem → fix: same.
- `error-state.component.ts:20` straight apostrophe in "didn't" → fix: curly ’.

## app/shared/ui/report-button/report-button.component.ts
✓ pass (icon-only button has `aria-label`, SVG is `aria-hidden`).

## app/shared/ui/skeleton-card/skeleton-card.component.ts
✓ pass (static, `aria-hidden`; the loading message lives in the result list).

## app/shared/ui/source-icons/source-icons.component.ts
- `source-icons.component.ts:13` `bg-gray-300` / `text-gray-900` are outside the §3 tokens → fix: `bg-bloom text-ink`.

## app/shared/ui/toast/toast.component.ts, toast.service.ts
✓ pass (polite live region).

## app/features/home/home.page.ts
✓ pass (labelled controls, `inputmode="numeric"`, focus moves to the first error, placeholder ends with "…").

## app/features/home/degree-picker.component.ts
- `degree-picker.component.ts:35,53,71` `rounded-[var(--radius)]` is an undefined variable and `border-gray-400` is outside the tokens → fix: `rounded-control border-line bg-paper text-ink min-h-11`.
- `degree-picker.component.ts:34,52,70` selects have no `name` or `autocomplete` → fix: added `name` and `autocomplete="off"`.
- `degree-picker.component.ts:54,72` `||faculties()` missing space → fix.

## app/features/search/search.page.ts
- `search.page.ts:38` Filters button has no hover or pressed state → fix: `hover:bg-bloom active:bg-bloom`.

## app/features/search/search-state.service.ts
✓ pass (no UI).

## app/features/search/bursary-row/bursary-row.component.ts
- `bursary-row.component.ts:164` row title is an `h3` directly under the page `h1` (heading order skips h2) → fix: `h2`.
- `bursary-row.component.ts:168` title link has no hover state → fix: `hover:underline`.

## app/features/search/filter-panel/filter-panel.component.ts
- `filter-panel.component.ts:25,60,78,89,99` undefined `--radius` variable and `border-gray-400` → fix: `rounded-control border-line bg-paper text-ink min-h-11`.
- `filter-panel.component.ts:58-99` no `name` or `autocomplete` on inputs → fix: added.
- `filter-panel.component.ts:72` average has no example placeholder → fix: `e.g. 72…`.
- `filter-panel.component.ts:26` summary has no hover state → fix: `hover:text-jacaranda`.
- `filter-panel.component.ts:116` Clear filters button has no hover or pressed state and is under 44 px → fix.

## app/features/search/filter-sheet/filter-sheet.component.ts
- `filter-sheet.component.ts:32` sticky "Show N bursaries" button has no hover state and can cover the focused field → fix: `hover:bg-ink active:bg-ink`, `scroll-padding-bottom: 80px` on the sheet.

## app/features/search/result-list/result-list.component.ts
- `result-list.component.ts:488` loading state has no text for assistive tech → fix: `Loading bursaries…` in a visually hidden status.

## app/features/search/pagination/pagination.component.ts
- `pagination.component.ts:11-27` Previous and Next are `<button (click)>` that change the URL (anti-pattern: click navigation instead of links) → fix: real `<a routerLink [queryParams]>`; the unavailable end is a `span` with `aria-disabled`.
- `pagination.component.ts:13` `rounded border` outside the tokens, no hover or pressed state → fix: control radius, `--line` border, `--bloom` hover.
- `pagination.component.ts:19` page count not tabular → fix: `tabular-nums`.

## app/features/bursary/bursary-detail.page.ts
- `bursary-detail.page.ts:459` "We couldn't load this bursary." uses "we" and a straight apostrophe, and does not say what to do → fix: "This bursary didn’t load. Check your connection and try again."
- `bursary-detail.page.ts:482` `color: #fff` is outside the tokens → fix: `var(--paper)`.

## app/features/bursary/closed-bursary.component.ts, bursary-labels.ts, bursary-detail.resolver.ts
✓ pass.

## app/features/report/report-dialog.component.ts
- `report-dialog.component.ts:130` submit hover is only a 10% opacity change, not more prominent than rest → fix: `hover:bg-ink active:bg-ink`.

## app/features/seo/field-landing.page.ts
- `field-landing.page.ts:36` "We could not find that field of study." uses "we" → fix: "That field of study isn’t listed. Check the address or search for it."

## app/features/seo/seo.service.ts, sitemap.ts
✓ pass (no UI).

## app/features/static/not-found.page.ts
- `not-found.page.ts:5` heading was the placeholder text "NotFoundPage" → fix: "Page not found" plus a line telling the reader what to do.

## app/features/static/about.page.ts, privacy.page.ts
- Placeholder headings ("AboutPage", "PrivacyPage") → not fixed, see below.

## Fixed in this PR
- Pagination is now real links (Cmd/Ctrl/middle-click work); disabled end is not a button.
- Hover and pressed states added to the empty/error state buttons, Filters button, Clear filters, Show N bursaries, report Send button, pagination, footer links, row title.
- Colours and radii moved to §3 tokens in the degree picker, filter panel, source-icon fallback, detail apply button and pagination.
- `name`, `autocomplete="off"` and an example placeholder added on filter and degree-picker controls.
- Row titles are `h2` so heading order is h1 → h2.
- Sticky apply bar and filter-sheet button no longer cover a focused element (`scroll-padding-bottom`).
- Copy: curly apostrophes, no "we", errors say how to fix them, 404 page has real copy, loading text for the skeleton.
- Specs updated for the changes above (pagination links, `h2` rows, 404 copy).

## Not fixed (with reason)
- `about.page.ts`, `privacy.page.ts` are placeholders. Writing their content is a new feature, outside "fixes only". They belong to the task that owns the static pages.
- The initial bundle is over the 300 KB raw warning budget (`ng build` warns at 319 KB). Not a §7 rule and not caused by these edits; I did not measure the baseline.
- The submit button shows "Sending…" text without a spinner (§7 forms). Adding an icon is new UI; the disabled state and text cover the need for now.
- `rgb(30 26 46 / 0.5)` dialog backdrops are the `--ink` token at 50% opacity, so left as is.
