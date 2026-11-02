# 06 — Design System & Interface Rules

> Status: **Accepted v1.0** (2026-11-02). **Governs every frontend task.** Supersedes the tokens in [05-web §8](05-web.md).
> Sources: the product owner asked that two skills govern the frontend: *frontend-design* (visual identity, typography, copy) and *web-design-guidelines* (the [Vercel Web Interface Guidelines](https://github.com/vercel-labs/web-interface-guidelines)). §2–§6 apply the first; §7 is our checklist of the second.

## 1. Brief
- **Subject:** South African bursaries, matched to a student's actual degree.
- **Audience:** matric learners and university students aged 17–25, mostly on mid-range Android phones with expensive data, often filling this in between classes or at home with a parent.
- **Primary job:** answer "which bursaries can *I* get for *my* degree, and when do they close?" in one screen.
- **The thing that matters most in this world is the closing date.** Missing it means losing a year of funding. The design is built around that.

## 2. Design plan (after review against generic defaults)

**Concept: "the deadline ledger."** Bursary season in Gauteng runs through jacaranda season (Oct–Nov), when students are writing exams and chasing applications. The design borrows two things from that world: the **jacaranda purple** that covers Pretoria and Joburg at exactly that time of year, and the **highlighter** students drag across the dates they can't miss.

| Axis | First instinct (rejected) | Why rejected | Choice |
|---|---|---|---|
| Colour | Forest green + gold | Reads as a generic finance/"trust" template | Jacaranda purple + highlighter yellow on cool white |
| Type | System font stack | No personality; looks like a default | **Atkinson Hyperlegible Next** (one family). It was designed for legibility for low-vision readers, which suits small, cheap screens. Personality comes from its distinctive letterforms and big date numerals |
| Results | Grid of identical rounded cards with soft shadows | The "SaaS card kit" | **Ledger rows**: a big date column on the left, details on the right, separated by space and a single hairline. No shadows |
| Hero | Headline + stat + gradient | The default hero | **The search *is* the hero**: a fill-in-the-blanks sentence (§5) |

## 3. Tokens (CSS variables in `styles.css`, mapped into the Tailwind theme)

| Token | Hex | Role | Contrast |
|---|---|---|---|
| `--ink` | `#1E1A2E` | Body text, headings (deep aubergine, not a tinted near-black) | 16.2:1 on paper |
| `--paper` | `#FAFAFC` | Page background (cool white, **not cream**) | — |
| `--jacaranda` | `#5E43B0` | Primary actions, links, focus ring, selected state | 6.9:1 on paper; white text on it 7.2:1 |
| `--bloom` | `#E6DEFA` | Selected chips, hover background, soft fills (never text) | — |
| `--highlighter` | `#FFE14D` | **Only** closing-soon dates (≤ 14 days), drawn as a highlighter swipe behind the date. Never text, never decoration | ink on it: 13.0:1 |
| `--muted` | `#5B566B` | Secondary text, metadata | 6.7:1 on paper |
| `--line` | `#E3E0EA` | Hairlines between rows, input borders | — |
| `--closed` | `#8A8697` | Closed-bursary text (paired with the word "Closed", never colour alone) | 3.4:1: only for large text (≥ 18 px) and always next to the word "Closed" |

- **Radius:** `--radius-control: 8px` for inputs, buttons and chips. Rows and sections have **no radius and no shadow**. Dialogs: 16 px.
- **Spacing:** 4 px base (`4, 8, 12, 16, 24, 32, 48, 64`). Page gutter 16 px on mobile, 24 px from 768 px.
- **Max width:** 720 px for reading/detail pages, 1040 px for search (filters + results).
- **Dark mode:** not in the MVP. Set `color-scheme: light` and `<meta name="theme-color" content="#FAFAFC">`.

## 4. Typography
- **Family:** Atkinson Hyperlegible Next, variable (weights 200–800, OFL-1.1), **self-hosted** via Fontsource (`@fontsource-variable/atkinson-hyperlegible-next`; verify the exact package name), **Latin subset only**, `font-display: swap`, preloaded. Fallback: `system-ui, sans-serif`.
- **Scale** (ratio 1.25, 16 px base): 13 / 16 / 20 / 25 / 31 / 39 px. Body 16 px / 1.55. Headings line-height 1.15, weight 700, `text-wrap: balance`.
- **Date numerals** are the display type: the day number in result rows is 31 px weight 800 with `font-variant-numeric: tabular-nums`; the month sits under it at 13 px weight 600. This is the one place the type gets loud.
- Line length ≤ 70 characters (`max-width: 65ch` on prose).
- **Sentence case everywhere**, headings and buttons included (decision: this overrides the guidelines' Title Case rule; see §7 note). **No all-caps labels**, no letter-spaced eyebrows, no single-word accent colours in headlines, no middle-dot meta strings (`A · B · C`), no `→` appended to links or buttons.

## 5. Layout & key screens (mobile first, left-aligned)

**Home: the search is the hero**
```
GetABursary
────────────────────────────────────
Find bursaries for
[ BCom Accounting ▾ ] at [ UJ ▾ ]
in [ first year ▾ ] with a [ 72 ]% average.
                          [ Show bursaries ]
────────────────────────────────────
Browse by field: Engineering, Commerce, Health …   (links)
```
The sentence is built from real `<label>`/`<select>`/`<input>` elements (each select has a visually-hidden label). On mobile it wraps naturally; each control is ≥ 44 px tall.

**Search: the deadline ledger**
```
23 bursaries match                 Sort: closing soonest ▾
┌──────┬───────────────────────────────────────────┐
│  14  │ Sasol Engineering Bursary          ⚑     │
│ Nov  │ Sasol                                     │
│ ▓▓▓▓ │ Undergraduate, min 65% average             │
│      │ Covers tuition and books                   │
│      │ [ZA][ST]  found on 2 sites                 │
├──────┼───────────────────────────────────────────┤
│  31  │ Investec Tertiary Bursary           ⚑     │
│ Jan  │ …                                          │
```
- The date column is fixed-width (64 px). Closing soon (≤ 14 days) gets the highlighter swipe behind the day number plus the text "closes in 5 days" in the row. **Closed** rows render the date in `--closed` and replace it with the word "Closed", the whole row at reduced emphasis, not clickable (05 §6).
- The row's title is the link (the whole row is a hit target via a stretched link). The flag (report) button sits outside the link.
- Filters: a left column ≥ 1024 px; otherwise a "Filters (3)" button that opens a bottom sheet (CDK dialog) with overscroll contained.

**Detail:** single 720 px column. The closing date block at the top (same treatment as the ledger), then "Who can apply", "What's covered", "Work-back obligation", "About", and a full-width **Apply on {funder}'s site** button that sticks to the bottom on mobile.

## 6. Principles
1. **The date is the loudest thing on every screen.** Nothing else gets display-size type.
2. **One bold device: the highlighter.** It means "closes soon" and nothing else. Don't reuse it for decoration, badges or marketing.
3. **Plain, specific copy** (§8). Name things the way students do: "closing date", "average", "what's covered", not "deadline window", "aggregate", "benefits package".
4. **Quiet structure.** Hairlines and whitespace, not boxes and shadows. No numbered markers unless the content is a real sequence. No gradients.
5. **Motion: one moment only.** When results first render, the highlighter swipes in behind closing-soon dates (`transform: scaleX` 0→1, 300 ms, staggered ≤ 3 rows). Respond to user actions (dialog open, filter sheet). Nothing else animates; `prefers-reduced-motion: reduce` disables the swipe (it just appears).
6. **Cheap on data.** One font file, no images except source favicons, no icon font (inline SVG only).

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
