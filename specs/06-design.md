# 06 — Design System & Interface Rules

> Status: **Accepted v2.0** (2026-11-04). **Governs every frontend task.** Supersedes v1.0 (jacaranda "deadline ledger" on cool white) and the tokens in [05-web §8](05-web.md).
> Sources: the frontend-design and web-design-guidelines skills (owner, 2026-11-02), and the owner's v2 brief (2026-11-04): *"a UI inspired by a minimalist Anthropic look, be creative with the font but fitting for the context; a real home page welcoming the user; fix responsiveness, broken dropdowns, choppiness and overlapping elements."* Reference pages the owner shared: anthropic.com/constitution (warm paper, generous space, serif reading text, one hand-drawn spot illustration) and jozue.fr (huge, tight, confident grotesque headline).

## 1. Brief
- **Subject:** South African bursaries, matched to a student's actual degree and closing dates.
- **Audience:** matric learners and university students aged 17–25, mostly on mid-range Android phones with expensive data, often with a parent beside them.
- **Primary job:** "which bursaries can *I* get for *my* degree, and when do they close?"
- **Feeling:** calm and trustworthy (it's about money and their future), not corporate. Quiet pages, one confident voice.

## 2. Design plan (reviewed against generic defaults)

**Concept: "a calm desk in bursary season."** Warm paper, lots of air, a confident headline, and one thing that's loud: the closing date, marked with a highlighter the way students mark the dates they can't miss.

| Axis | Generic default | What we do instead, and why |
|---|---|---|
| Colour | Cream + serif + terracotta (the stock "Anthropic-like" output) | Warm paper and ink give the Anthropic calm the owner asked for, but the accent stays **jacaranda** (our v1 identity: Gauteng in bursary season), not clay. The **highlighter** yellow is the only loud colour, and it only ever means "closes soon" |
| Type | One sans everywhere, or a high-contrast serif display | **Bricolage Grotesque** (variable: weight, width, optical size) for headlines, UI and the big date numerals: friendly, a little hand-made, young without being childish, and it gets confident at display sizes like the jozue.fr headline. **Newsreader** (variable serif) only for reading text: hero intro, bursary descriptions, About/Privacy. The pairing is clearly distinct: grotesque for doing, serif for reading |
| Hero | Headline + stat + gradient | A very large, tightly set headline in Bricolage (condensed width), a short serif intro, then the degree search right there. Beside it on desktop (below it on mobile), **one** hand-drawn line illustration: a calendar page with a highlighter swipe (inline SVG, ink lines, the swipe in `--highlighter`) |
| Results | Grid of identical rounded cards with shadows | **Ledger rows**: big date column left, details right, hairlines between, no shadows (kept from v1, restyled) |
| Structure | Eyebrows, numbered sections, middle-dot meta | No eyebrows. Numbers only on "How it works" (a real sequence). Meta as short separate lines |

## 3. Tokens (CSS variables in `styles.css`, mapped into the Tailwind theme; v1 names kept where the role is the same)

| Token | Hex | Role | Contrast |
|---|---|---|---|
| `--paper` | `#F7F5EF` | Page background (warm paper) | — |
| `--paper-raised` | `#FFFFFF` | Inputs, dialogs, the search panel | — |
| `--ink` | `#1B1A17` | Text, headings, primary button fill | 16.6:1 on paper |
| `--muted` | `#5E5B53` | Secondary text, metadata | 6.4:1 on paper |
| `--line` | `#E2DED3` | Hairlines, input borders | — |
| `--jacaranda` | `#4F3A9E` | Links, focus ring, selected state, the saved icon | 8.1:1 on paper |
| `--bloom` | `#ECE7F8` | Selected chips, hover fill on rows (never text) | — |
| `--highlighter` | `#FFE14D` | **Only** closing-soon dates (≤ 14 days) and the hero illustration's swipe | ink on it: 13.4:1 |
| `--closed` | `#8C887E` | Closed-bursary text (always next to the word "Closed") | large text only |

- **Primary button:** ink fill, paper text, 10 px radius, 48 px tall. Secondary: paper-raised with a `--line` border. Links: jacaranda, underline offset 3 px.
- **Radius:** 10 px controls and the search panel, 16 px dialogs/sheets, **0** on ledger rows. **No shadows** except dialogs (one soft shadow) and the sticky header's hairline.
- **Spacing:** 4 px base (`4 8 12 16 24 32 48 64 96`). Gutter 16 px mobile, 24 px ≥ 768, 32 px ≥ 1280. Sections on home are separated by 96 px (64 px on mobile), not by rules.
- **Widths:** 1200 px max page; 680 px reading column; search uses a 288 px filter column + fluid results.
- **Dark mode:** not in this iteration. `color-scheme: light`, `theme-color` = `--paper`.

## 4. Typography
- **Families:** `@fontsource-variable/bricolage-grotesque` and `@fontsource-variable/newsreader`, self-hosted, **Latin subset only**, `font-display: swap`, the Bricolage file preloaded. Fallbacks: `ui-sans-serif, system-ui` / `ui-serif, Georgia`. Remove Atkinson Hyperlegible.
- **Scale** (ratio 1.25, 16 px base): 13 / 16 / 20 / 25 / 31 / 39 / 49 px, plus the hero display size.
- **Hero headline:** Bricolage, `font-size: clamp(2.75rem, 8vw, 6.5rem)`, weight 700, `font-stretch: 80%` (condensed), letter-spacing −0.03em, line-height 0.95, `text-wrap: balance`. Copy: **"Find the bursaries that fit your degree."** No single accented word.
- **Headings:** Bricolage 700, line-height 1.1, `text-wrap: balance`. **UI text** (labels, buttons, rows): Bricolage 400–600 at 16 px / 1.5.
- **Reading text:** Newsreader 400, 18 px / 1.65 (serif gets more leading), `max-width: 65ch`.
- **Date numerals** (ledger + closing-soon list): Bricolage 800, 32 px, `tabular-nums`, month under it at 13 px 600.
- **Sentence case everywhere.** No all-caps labels, no letter-spaced eyebrows, no middle-dot meta strings, no `→` on links or buttons.

## 5. Screens (mobile first, left-aligned)

**Header** (sticky, paper background, hairline bottom when scrolled): wordmark "getabursary" (Bricolage 700, lowercase, the "a" in "a bursary" not styled differently) left; right: "Search", "My bursaries" (if signed in), account menu or "Sign in". On < 640 px the nav collapses into a menu button that opens a sheet.

**Home** (`/`):
```
[header]
Find the bursaries                      ┌ hand-drawn calendar page,
that fit your degree.                   │ one date highlighted ┐
We read South Africa's bursary sites so you don't have to. Pick your
degree and see what you qualify for, sorted by closing date.   (serif)
┌ search panel (paper-raised, 1px line, radius 10) ─────────────────┐
│ University ▾        Qualification ▾ (grouped by faculty)          │
│ Year of study ▾     Your average [72] %        [ Show bursaries ] │
└────────────────────────────────────────────────────────────────────┘
Closing soon                                              See all
 14 Nov ▓  Sasol Bursary            Sasol                 (ledger rows, 5)
 21 Nov    Exxaro Bursary Scheme    Exxaro
How it works   1 Pick your degree  2 See what you qualify for  3 Track your applications
Browse by field   Engineering   Health sciences   Commerce   Law   … (large Bricolage links, 2–3 columns)
Where the listings come from: ZA Bursaries, StudyTrust, AllBursaries (links). Always apply on the funder's own site.
[footer]
```
- The search panel order is **University → Qualification → Year of study → Average**. Qualification is one native `<select>` with `<optgroup>` per faculty, loaded for the chosen university (no separate faculty step). Before a university is picked, it shows "Pick a university first" and is disabled with a visible reason. Desktop: 2×2 grid; mobile: stacked, full-width controls ≥ 48 px tall. Everything is optional except nothing: "Show bursaries" always works.
- "Closing soon" uses the real search (`sort=ClosingSoonest&pageSize=5`), rendered on the server.

**Search** (`/search`): filters in a 288 px left column at ≥ 1024 px (sticky, scrolls independently, **never overlaps results**); below 1024 px a "Filters (3)" button opens a bottom sheet. Field of study is a list of collapsible groups (`<details>`) with a filter-as-you-type box at the top; only the groups with a selection are open. Results are ledger rows:
```
 14   Sasol Bursary                                   [save] [report]
 Nov  Sasol
 ▓▓   Undergraduate, min 65% average
      Found on ZA Bursaries, StudyTrust       (each name links to that site's page for this bursary)
```
**"Found on"**: list **every** source the bursary was found on, by name, each linking (`target="_blank" rel="noopener nofollow"`) to the exact aggregator page (`sources[].url`), with its 16 px favicon before the name. Never "found on 1 site".

**Detail** (`/bursaries/:slug`): 680 px column; date block; title; funder; "Found on" (same as above); sections in serif reading text; sticky "Apply on {funder}'s site" bar on mobile.

**Everything else** (account dialogs, tracker, admin): same tokens and type; admin may use plain tables with hairlines.

## 6. Principles
1. **The date is the loudest thing on result screens; the headline is the loudest thing on home.** Nothing else gets display-size type.
2. **One bold device: the highlighter.** It means "closes soon" (and draws the swipe in the hero illustration). Nothing else uses it.
3. **Calm by default.** Warm paper, whitespace, hairlines. No gradients, no card grids, no shadows on content.
4. **Fast and steady.** No layout shift (reserve space for loading content, explicit image sizes), no element ever overlaps another at any width from 320 px to 1920 px, interactions respond within one frame (transitions ≤ 150 ms, `transform`/`opacity` only). Hydration uses event replay so taps made before the app wakes up still work, and below-the-fold sections hydrate on viewport.
5. **Motion: one moment.** On home, the hero illustration's highlighter swipe draws in once (`scaleX` 0→1, 500 ms). On results, closing-soon swipes appear without animation. Respect `prefers-reduced-motion`.
6. **Cheap on data.** Two variable font files (Latin), one inline SVG illustration, favicons only. Initial JS+CSS within the 05 budgets.
7. **Check it with your eyes.** Every web task commits screenshots (§9) at 375×812 and 1440×900 of each page it changes, and the author reviews them before opening the PR.

## 7. Interface rules checklist (from the Web Interface Guidelines; reviewers check every web PR against this)
**Accessibility:** icon-only buttons have an accessible name · every control has a label · actions are `<button>`, navigation is `<a>`, never clickable divs · images have `alt` (empty if decorative), decorative SVGs `aria-hidden="true"` · toasts and async validation use a polite live region · native elements before ARIA · logical heading order + a "Skip to results/content" link · `scroll-margin-top` on anchor targets.
**Focus:** visible focus on everything interactive, using `:focus-visible` (a 2 px `--jacaranda` ring, 2 px offset) · never remove outlines without a replacement · sticky header/footer/apply bar never covers the focused element.
**Forms:** correct `type`/`inputmode` (`average` → `inputmode="numeric"`), `autocomplete` and `name` on every input (`email` for report email; `off` for non-personal fields) · never block paste · labels clickable · `spellcheck="false"` on email · checkbox/radio and label share one hit target · submit stays enabled until the request starts, then shows a spinner · errors inline beside the field, focus moves to the first error · placeholders show an example and end with "…".
**Animation:** respect `prefers-reduced-motion` · animate only `transform`/`opacity` · no `transition: all`, list properties · interruptible.
**Typography & copy:** `…` not `...` · curly quotes · non-breaking space between number and unit (`72 %` is wrong; write `72%`; `R 50 000` uses NBSPs) · loading text ends with "…" · `tabular-nums` for dates and counts · `text-wrap: balance` on headings · active voice · numerals for counts · specific button labels ("Show bursaries", "Send report", not "Submit") · errors say how to fix it · address the reader as "you", never "I/we" in UI copy.
**Content handling:** long titles/funder names truncate or wrap without breaking layout (`min-width: 0` on flex children, `overflow-wrap: anywhere` for URLs) · every list has an empty state · test with short, typical and very long names.
**Images:** favicons have explicit `width`/`height` (16) · lazy-load below the fold.
**Performance:** results paginate at ≤ 50 (no virtualization needed) · no layout reads during render · preload the font, `font-display: swap` · `preconnect` only if a third-party origin is used.
**Navigation & state:** filters, sort, page and an open filter sheet are reflected in the URL (05 §4) · links support Cmd/Ctrl/middle-click (real `<a href>`) · nothing destructive in the MVP.
**Touch:** `touch-action: manipulation` on controls · deliberate `-webkit-tap-highlight-color` (transparent + our own pressed state) · `overscroll-behavior: contain` in dialogs/sheets · no autofocus on mobile.
**Layout:** safe-area insets on the sticky apply bar · no horizontal scroll at 320 px · flex/grid, no JS measuring.
**Theming:** `color-scheme: light`, `theme-color` matches `--paper`, native `<select>` gets explicit background and text colours.
**Locale:** dates and numbers via `Intl.DateTimeFormat('en-ZA', …)` / `Intl.NumberFormat('en-ZA', …)` (currency `ZAR`) · never hard-coded date patterns.
**Hydration:** dates that depend on "today" (closing-soon) are computed from an injected clock and rendered identically on server and client (pass the server's date via transfer state) · no hydration-warning suppression.
**Hover/active:** every button and link has hover and active states, more prominent than resting.
**Anti-patterns (always blocking):** zoom-disabling viewport · paste-blocking · `transition: all` · outline removed without replacement · `(click)` navigation instead of links · clickable `div`/`span` · images without dimensions · unlabelled inputs/icon buttons · hard-coded date/number formats · unjustified autofocus.

> **Conflict resolved:** the guidelines say Title Case for headings and buttons; the frontend-design guidance says sentence case. **We use sentence case** (product-owner instruction for both skills to govern; sentence case is easier to read and is the South African English norm). The rest of §7 applies as written.

## 8. Copy rules (from frontend-design)
- Words exist to help someone use the page. One job per string.
- Name things from the student's side: "closing date", "your average", "what's covered", "found on".
- Buttons say what happens: "Show bursaries", "Send report", "Apply on Sasol's site". The resulting toast uses the same verb: "Report sent".
- Errors say what happened and what to do, without apology: "Bursaries didn't load. Check your connection and try again." Empty states invite an action: "No bursaries match a 75% average. Lower it to see more."
- No exclamation marks, no "Oops", no "Uh oh".

## 9. Visual check (required for every web task)
- `scripts/dev-stack.sh` starts Postgres (docker compose), the API with `Database:MigrateOnStartup=true` plus **sample data** (`data/dev/sample-bursaries.sql`: about 15 realistic bursaries, some closing within 14 days, some closed, several found on 2–3 sources), and `ng serve`.
- `node scripts/screenshots.mjs <task-id> <path> [<path>…]` uses Playwright (Chromium) to save `docs/screenshots/<task-id>/<page>-{mobile,desktop}.png` at 375×812 and 1440×900, full page.
- The author **opens every screenshot** (Read tool), fixes overlaps, clipping, horizontal scroll and misalignment, re-shoots, and commits the final PNGs. The PR lists them. Reviewers open them too.
