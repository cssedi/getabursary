# 00 — Product Brief & Requirements

> Status: **Accepted v0.4** (2026-11-02). Product: **GetABursary** (`getabursary.co.za`).
> Owner: product owner (@cssedi). Editor: orchestrator agent.
> Every requirement has an ID (`FR-xx`, `NFR-xx`) so that later specs and task files can reference it.

---

## 1. Problem statement

South African students looking for bursaries have to search across many aggregator sites (ZA Bursaries, StudyTrust, AllBursaries, GoCareers and others). Each site lists some bursaries the others don't, so no single site is complete. Most listings don't say up front whether *the student's specific degree* is eligible, so students keep clicking into listings and back out again to find ones they actually qualify for. Closing dates are spread across sites, and students have no single place to track what they've applied for.

**We will build a free, ad-supported platform that:**
1. Gathers open bursaries from many aggregators into one deduplicated index.
2. Matches bursaries to a student's **specific university degree**, study level and marks, instead of relying on keyword search.
3. Lets students save bursaries and track their applications through to an outcome.

**One-line pitch:** *Pick your degree, see every bursary you actually qualify for, and track your applications in one place.*

## 2. Target users

| Persona | Description | Key needs |
|---|---|---|
| **Matric learner** (primary) | Grade 11–12, applying for first-year funding, often on mobile with limited data | Find bursaries for the degree they *plan* to study; know closing dates; check minimum-mark requirements |
| **Undergraduate** (primary) | Currently enrolled, needs funding for the next year | Filter by their current degree, year of study and average |
| **Postgraduate** (secondary) | Honours, Master's or PhD | Postgrad-only funding; research-area matching |
| **Admin** (internal) | You, or trusted moderators | Review ingested listings, fix errors, handle user reports, check scraper health |

## 3. Goals & non-goals

**Goals (MVP)**
- G1: Broader coverage than any single aggregator, with no duplicate listings.
- G2: Eligibility-first discovery: degree, study level and marks are structured filters, not free text.
- G3: Personal application tracking.
- G4: Data stays fresh automatically (scheduled ingestion) and can be corrected (admin screen and user reports).
- G5: Pages search engines can index, plus ad placements to fund hosting.

**Non-goals (MVP)**
- Applying for bursaries on our platform. We always link out to the funder.
- Copying the aggregators' editorial content. We store **facts plus a link** only (see §6.3).
- Native mobile apps (the website is mobile-first instead).
- Loans, NSFAS application help, internships or learnerships (possible later).

## 4. Functional requirements

### 4.1 Discovery & search
- **FR-01** Browse and search bursaries with these filters:
  - **University**, then **Faculty**, then **Qualification** (e.g. *UCT → Engineering & the Built Environment → BSc(Eng) Electrical*)
  - **Field of study** (broader category, for students who haven't chosen a university yet)
  - **Study level**: Matric → 1st year, Undergraduate (year 1–4+), Honours, Master's, PhD
  - **Minimum marks**: student enters their **overall average %** (decided: no subject marks or APS in MVP); bursaries whose stated minimum average is higher are excluded
  - **Closing date range** and a **"Closing soon"** sort
  - **Show closed bursaries** checkbox (off by default)
- **FR-02** Matching is **structured, not keyword-based**. Choosing a qualification resolves to the fields of study it belongs to, and bursaries tagged with those fields (or with that specific qualification or institution) are returned. See §7.
- **FR-03** Results are sorted by best match first, with closing date as the default tie-break. Sorting by closing date and by recently added is also available.
- **FR-04** Each result card shows: bursary name, funder, closing date (with a countdown if within 14 days), eligible study levels, minimum marks, coverage summary, and **source icon(s)** (FR-05).
- **FR-05** **Source attribution**: every bursary shows the icon/favicon and name of *each* aggregator it was found on, linking to that aggregator's listing. If it was found on 3 sites, it shows 3 icons.
- **FR-06** **Closed bursaries** stay in the index and appear greyed out with a "Closed" badge. The detail view is **not available** for them (the card isn't clickable). They are hidden unless "Show closed" is ticked.
- **FR-07** Bursary detail page (open bursaries only): structured eligibility (fields, qualifications, levels, marks, citizenship, other criteria), value and coverage, any obligation to work for the funder afterwards, closing date, **"Apply on funder's site"** button (links to the funder's own page where known, otherwise to the source listing), and source icons.
- **FR-08** **Report an inaccuracy**: a small flag icon on each card and on the detail page opens a minimal form with a reason dropdown (*wrong closing date / wrong eligibility / broken link / bursary closed / duplicate / other*), an optional comment and an optional email. Login isn't required. Submitting creates an item in the admin review queue.

### 4.2 Accounts, saved bursaries & tracker
- **FR-09 Account rule (decided)**: *if an action saves the user's own preferences or data, it requires an account; everything else doesn't.*
  - **No account needed:** search, every filter, viewing details, following "Apply" links, reporting an inaccuracy. Filters live in the URL (shareable, bookmarkable) and aren't stored on the server or in the browser.
  - **Account needed:** saving a bursary, the application tracker, the student profile, and (later) email reminders.
  - **How the prompt works:** the save/track buttons are always visible. Tapping one while signed out opens a small sign-in/sign-up dialog (Google or email). After signing in, the action the user started **completes automatically**, so they don't have to tap again. No sign-up walls, banners or nagging anywhere else.
- **FR-10** Sign-up and sign-in with email and password (with email confirmation) **and Google sign-in**.
- **FR-11** Student profile (account needed): study level, university, qualification, average mark. If set, it pre-fills the filters.
- **FR-12** **Save** a bursary (heart/bookmark) from the results list or the detail page (account needed; triggers the FR-09 dialog when signed out).
- **FR-13** **Application tracker**: each saved bursary has a status:
  `Saved → Preparing → Applied → Shortlisted/Interview → Awarded | Unsuccessful | Withdrawn`
  with an optional applied date and notes.
- **FR-14** Tracker dashboard showing counts per status, upcoming closing dates for saved-but-not-applied bursaries, and a list/board view.
- **FR-15** *(Later)* Email reminders N days before a saved bursary closes.

### 4.3 Ingestion (separate worker)
- **FR-20** A **separate ingestion worker** (its own .NET worker process, not part of the API) runs on a schedule (default: daily) and on demand from the admin screen.
- **FR-21** Each source is a **source adapter** behind a common interface (`ISourceAdapter`: list listings → fetch listing → parse into a `RawListing`). Adding a source means adding an adapter, without changing the core pipeline.
- **FR-22** Pipeline: `Fetch → Snapshot (store raw HTML/hash) → Parse → Normalise/Extract → Deduplicate/Merge → Publish or Queue for review`.
- **FR-23** **Extraction**: free-text eligibility is turned into structured fields (fields of study, qualifications, levels, minimum marks, citizenship, closing date), probably with an LLM. Each extracted field gets a confidence score, and low-confidence results go to the admin queue.
- **FR-24** **Deduplication**: the same bursary found on several sources merges into one canonical bursary that keeps every source link (this powers FR-05). Matching uses the funder plus a normalised name plus the year/closing date.
- **FR-25** Only pages whose content hash changed get re-processed, to save cost.
- **FR-26** Bursaries are marked **closed** automatically when the closing date passes. Listings that disappear from every source are flagged for review rather than deleted.
- **FR-27** Ingestion respects `robots.txt`, rate-limits each domain, sends an honest User-Agent, and records every run (per-source counts, errors, duration).

### 4.4 Reference data (degree taxonomy)
- **FR-30** Seed and maintain a taxonomy: **Institution → Faculty → Qualification**, and **Qualification ↔ Field of study** (many-to-many).
- **FR-31** Use the South African **CESM** (Classification of Educational Subject Matter, used by the Department of Higher Education and Training (DHET)) as the backbone for fields of study, so mapping is consistent and defensible.
- **FR-32** MVP coverage (decided): faculties and qualifications for **UCT, Wits, UJ and UP** (SU deferred 2026-11-02: no usable source; UP from 2021 yearbook extracts until refreshed), with the most popular degrees per faculty first. All 26 public universities are still seeded as *institutions* (so bursaries limited to, say, UKZN can be recorded), but the university picker only shows institutions that have qualifications seeded. More universities, TVET colleges and private institutions come after the MVP.

### 4.5 Admin
- **FR-40** Admin-only area (role-based, using the existing Identity + roles approach).
- **FR-41** **Review queue**: new and low-confidence listings, user reports (FR-08), suspected duplicates, and listings that vanished from all sources. Actions: approve, edit, merge, reject, mark closed.
- **FR-42** Edit any bursary field. Manual edits are **locked** so the next ingestion run doesn't overwrite them.
- **FR-43** Manage sources: enable/disable, last run status, error log, "Run now".
- **FR-44** Manage the taxonomy (add or edit institutions, faculties, qualifications and field mappings).

### 4.6 Monetisation
- **FR-50** Ad slots (e.g. Google AdSense) in the results list (e.g. every N cards) and on the detail page. Ads are **never** placed between the user and the "Apply" button, and they must be clearly labelled.
- **FR-51** Cookie/consent banner and privacy policy (needed for ads and for the Protection of Personal Information Act (POPIA)).

## 5. Non-functional requirements
- **NFR-01 Mobile-first and light on data**: results page under about 300 KB on first load (excluding ads), and usable on slow 3G.
- **NFR-02 SEO**: bursary detail pages and category pages ("Engineering bursaries 2027") are server-rendered (Angular SSR) so search engines can index them. Search traffic is the main way people will find the site and what makes ads pay.
- **NFR-03 Performance**: search API p95 under 500 ms at MVP scale (about 2–5k bursaries).
- **NFR-04 POPIA**: collect as little as possible (email, optional profile), let users delete their account, keep secrets out of the repo.
- **NFR-05 Reliability**: if one source adapter fails, the run continues for the others, and the failure shows in the admin screen.
- **NFR-06 Code quality**: Controller → Service → Repository layering with `Result<T>`, DTOs at the API boundary, automated tests, and CI on every PR (details in the upcoming `specs/conventions.md` and `AGENTS.md`).
- **NFR-07 Open source**: public repo, so **no secrets, connection strings or API keys** in the code or the history.

## 6. Data sources

### 6.1 Candidate aggregators (to verify: robots.txt, terms, structure)
| Source | Notes | Priority |
|---|---|---|
| [ZA Bursaries](https://www.zabursaries.co.za/) | Largest; organised by field and by closing month | P0 |
| [StudyTrust](https://studytrust.org.za/bursaries/) | Non-profit; curated list | P0 |
| [AllBursaries](https://allbursaries.co.za/bursaries) | Has category pages (general, government, by year) | P0 |
| [GoCareers](https://gocareers.co.za/bursaries) | "Bursary hub"; 2026/2027 listings | P1 |
| [FundiConnect](https://fundiconnect.co.za/bursaries-2027) | Yearly bursary lists | P1 |
| [CareerDrive](https://careerdrive.co.za/bursaries/page/3/) | Paginated listing | P1 |
| [BursaryZA](https://bursaryza.pages.dev/bursaries) | About 354 listings, field-of-study filters, "rebuilt daily from public sources" (it's an aggregator of aggregators, so check for overlap) | P2 |
| [Edupstairs](https://www.edupstairs.org/tag/south-africa-bursaries-2027/) | Blog-style posts | P2 |
| [Apply.org.za](https://apply.org.za/funding) | Funding lists | P2 |
| University funding pages (e.g. [Wits CCDU bursary links](https://www.wits.ac.za/ccdu/career-development-services/bursary-links)) | Primary-ish sources, often link lists | Later |

The MVP launches with the P0 sources. Each further source is a self-contained adapter task.

### 6.2 Canonical bursary record (draft)
`Bursary { id, name, funder, funderUrl, applyUrl, description(short, our own), valueSummary, coverage[], workBackObligation, studyLevels[], minAverage, minSubjectMarks[], citizenship, provinces[], fieldsOfStudy[], qualifications[], institutions[], openDate, closingDate, status(Open|ClosingSoon|Closed|Unknown), sources[{sourceId, sourceUrl, firstSeen, lastSeen}], extractionConfidence, manuallyLocked, createdAt, updatedAt }`

### 6.3 Content policy
We store **facts** (name, funder, eligibility, dates, links) and write our own short summary. We don't copy aggregators' long-form text or images. We always link to the source and, where we can, to the funder.

## 7. Search & matching approach — **decided: PostgreSQL only ([ADR-001](adr/ADR-001-postgres-only.md))**

The "intelligence" comes from two things: a **structured taxonomy** (Qualification → Field of study, using CESM) and **LLM extraction at ingestion** that turns each bursary's free text into structured eligibility. Once both exist, a search is a short traversal:

```
Student's Qualification ──belongs_to──▶ Field(s) of study ◀──eligible_for── Bursary
          │                                                                  ▲
          └──offered_by──▶ Institution ◀──restricted_to (optional)───────────┘
```
…filtered by study level, marks, closing date and status.

| Option | Pros | Cons |
|---|---|---|
| **A. PostgreSQL only** (graph-shaped tables + join tables; optional `pgvector` later for "related degrees") | One database; you already know EF Core + Npgsql; cheapest; the traversal is only 2–3 hops, which SQL handles well | Less "graph-native" querying; more complex graph features (recommendations) would take more work |
| **B. PostgreSQL + Neo4j** (Postgres for users/tracker; Neo4j for taxonomy and matching) | Natural graph queries (Cypher); good learning experience; easy to extend into recommendations | Two datastores to keep in sync; extra hosting cost; more moving parts for a first project and for small agents |
| **C. Postgres + Apache AGE** (graph extension inside Postgres) | Cypher inside one database | AGE isn't supported on AWS RDS, so you'd have to self-host Postgres; smaller ecosystem |

**Decision: A.** Matching sits behind an `IBursaryMatchingService` interface so a graph database can be added later without changing callers. See [ADR-001](adr/ADR-001-postgres-only.md).

## 8. Architecture sketch (to become `specs/02-architecture.md`)
```
[Angular SSR web app] ──HTTPS──▶ [.NET API: Controller → Service → Repository] ──▶ [PostgreSQL]
                                                                                     ▲
[.NET Ingestion Worker: Scheduler → SourceAdapters → Parser → Extractor(LLM) → Dedupe] ┘
```
- Monorepo. Full layout in [01-architecture.md](01-architecture.md).
- Scraping: AngleSharp/HtmlAgilityPack for static HTML; Playwright only for sites that need JavaScript to render.
- Testing: each adapter is tested against saved HTML fixtures, so scraper tasks can be checked without touching the network.

## 9. Hosting budget (estimates, ZAR at ~R16.64/USD, Nov 2026)

**Option 1 — Lean/learning (recommended to start): about R150–R350/month**
- 1 small VPS (2 vCPU/4 GB, e.g. Hetzner or an SA host such as Xneelo/Afrihost): ~R90–R300/month, running Docker Compose with the API, worker, Postgres and the Caddy web server (free HTTPS).
- Angular app served from the VPS or from Cloudflare Pages (free tier).
- `.co.za` domain: ~R100–R150/year.
- LLM extraction: ~R20–R150/month at MVP scale (only changed pages are processed by a small model).
- Downside: you handle backups and patching yourself.

**Option 2 — AWS, Cape Town region (af-south-1): about R750–R1,100/month**
| Item | ~USD/mo | ~ZAR/mo |
|---|---|---|
| EC2 t4g.small (API + worker in containers) | 16 | 265 |
| RDS PostgreSQL db.t4g.micro + 20 GB storage + backups | 19 | 315 |
| Application Load Balancer *(skippable: use Caddy on EC2 plus an Elastic IP instead)* | 20 | 335 |
| S3 + CloudFront (Angular), Route 53, public IPv4, CloudWatch logs | 8 | 135 |
| LLM extraction | 2–8 | 35–135 |
| **Total** | **~65** | **~R1,085** (≈ R750 without the ALB) |

Notes: Cape Town costs roughly 20–25% more than Ireland or US-East but is faster for SA users. New AWS accounts get starter credits, which should cover the first few months (check the current free-tier terms). Set an **AWS Budget alert at R500** on day one.

## 10. Open questions
- ~~Q1~~ **Decided:** PostgreSQL only (ADR-001).
- ~~Q2~~ **Decided:** sub-agents run as Claude on GitHub, picking up GitHub Issues (via the GitHub Action) and opening PRs.
- ~~Q3~~ **Decided:** task specs are markdown files in `/tasks` (source of truth); the orchestrator mirrors each one to a GitHub Issue.
- ~~Q4~~ **Decided (revised 2026-11-02):** account required only for actions that save the user's own data (FR-09); email/password + Google (FR-10).
- ~~Q5~~ **Decided:** overall average only for MVP. Subject requirements are still *extracted and stored* (shown on the detail page) but not filtered on.
- ~~Q6~~ **Decided:** big 5 universities (UCT, Wits, UP, SU, UJ) for MVP (FR-32).
- ~~Q7~~ **Decided:** no. Logged-out filters live only in the URL. Remembering defaults is the profile (FR-11, account needed).
- ~~Q8~~ **Decided:** GetABursary, repo `cssedi/getabursary` (public).

---

## Appendix A — Lessons from `ruru-api` to carry into conventions
Keep: Controller → Service → Repository with interfaces and DI registration, DTOs per feature folder, the `Result<T>` type, Identity + roles seeded from an enum, `AGENTS.md`, and the response "canary".
Fix or standardise in the new repo:
- `Result<T>` exists but isn't used yet. Make every service return `Result<T>`, and have controllers map it to HTTP codes.
- Repositories shouldn't take request DTOs. Map DTO → entity in the service.
- `result.Append(value)` (LINQ) doesn't modify the list, so `GetByUserIdAsync` always returns an empty list. Use `.Select(...).ToList()`.
- `new Guid()` is `Guid.Empty` and `new DateTime()` is year 0001. Use `Guid.NewGuid()` and `DateTime.UtcNow`.
- `app.UseCors()` comes after `MapControllers()` and has no policy name. `AllowAnyHeader()` is called twice (it should be `AllowAnyMethod()`). `ValidIssuers` reads the `ValidAudiences` config section.
- No test project yet. The new repo starts with xUnit + Testcontainers (Postgres) and CI from task #1.

## Appendix B — Name ideas
| Name | Why it fits |
|---|---|
| **Vula** / **VulaFunds** | isiZulu/isiXhosa for "open": open bursaries that open doors |
| **Funda** / **FundaFund** | "learn/study" in Nguni languages; short and memorable |
| **Phakama** | "rise up"; aspirational |
| **Thuto** | "education" in Sesotho/Setswana |
| **DegreeMatch** / **Matchd** | Says exactly what makes it different: matching on your degree |
| **Bursely** / **Bursify** | Modern, brandable, clearly about bursaries |
| **NextYear** | Students are always funding *next* year; friendly and plain |
| **Ruru Bursaries** | Builds on your existing ruru.io brand (the ruru is an owl, a symbol of wisdom) |

*Check `.co.za` domain availability, CIPC/trademark conflicts and social handles before choosing.*
