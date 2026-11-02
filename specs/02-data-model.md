# 02 — Data Model & Matching

> Status: **Accepted v1.0**. Database: PostgreSQL 17 ([ADR-001](adr/ADR-001-postgres-only.md)). Table and column names are `snake_case` (via `EFCore.NamingConventions`). C# entities are PascalCase.
> Primary keys are `uuid` (generated in code with `Guid.CreateVersion7()`) unless noted. Every entity has `created_at` and `updated_at` (`timestamptz`, UTC), set in `SaveChangesAsync`.

## 1. Enums (stored as `text` via `HasConversion<string>()` for readability)

| Enum | Values |
|---|---|
| `StudyLevel` | `Matric` (applying for 1st year), `Undergraduate`, `Honours`, `Masters`, `Doctoral` |
| `BursaryStatus` | `Open`, `Closed`, `Unknown` (no closing date found). *ClosingSoon is computed on read: Open and closing within 14 days* |
| `Citizenship` | `SouthAfrican`, `SouthAfricanOrPermanentResident`, `Any`, `Unknown` |
| `CoverageItem` | `Tuition`, `Accommodation`, `Books`, `Meals`, `Laptop`, `Stipend`, `Transport`, `Other` |
| `InstitutionType` | `TraditionalUniversity`, `ComprehensiveUniversity`, `UniversityOfTechnology`, `Tvet`, `Private` |
| `Province` | `EasternCape`, `FreeState`, `Gauteng`, `KwaZuluNatal`, `Limpopo`, `Mpumalanga`, `NorthWest`, `NorthernCape`, `WesternCape` |
| `ReportReason` | `WrongClosingDate`, `WrongEligibility`, `BrokenLink`, `BursaryClosed`, `Duplicate`, `Other` |
| `ReviewItemType` | `NewListing`, `LowConfidence`, `PossibleDuplicate`, `Vanished`, `UserReport` |
| `ReviewStatus` | `Open`, `Resolved`, `Dismissed` |
| `TrackerStatus` | `Saved`, `Preparing`, `Applied`, `Shortlisted`, `Awarded`, `Unsuccessful`, `Withdrawn` |
| `IngestionTrigger` | `Scheduled`, `Manual` |
| `RunStatus` | `Running`, `Succeeded`, `PartiallyFailed`, `Failed` |

## 2. Tables

### 2.1 Taxonomy (FR-30–FR-32)
| Table | Columns | Notes |
|---|---|---|
| `field_of_study` | `id`, `code` (CESM, e.g. `08`, `0801`) **unique**, `name`, `slug` **unique**, `parent_id` → field_of_study (null for first-order) | Two levels: CESM first order and second order |
| `institution` | `id`, `name`, `short_name`, `slug` **unique**, `type`, `province`, `website_url` | |
| `faculty` | `id`, `institution_id` →, `name`, `slug` | unique (`institution_id`, `slug`) |
| `qualification` | `id`, `faculty_id` →, `name` (e.g. "BSc (Eng) Electrical Engineering"), `abbreviation`, `study_level`, `nqf_level` int?, `duration_years` int?, `slug` | unique (`faculty_id`, `slug`) |
| `qualification_field` | `qualification_id`, `field_of_study_id` | composite PK; many-to-many |

### 2.2 Bursaries (FR-04–FR-07, FR-24)
| Table | Columns | Notes |
|---|---|---|
| `funder` | `id`, `name`, `normalized_name` **unique**, `website_url` | |
| `bursary` | `id`, `slug` **unique**, `name`, `normalized_name`, `funder_id` → (nullable), `summary` (our own text, ≤ 600 chars), `value_summary`, `coverage` `text[]`, `work_back_required` bool?, `work_back_details`, `study_levels` `text[]` (empty = unknown), `min_average` `numeric(5,2)`?, `min_subject_requirements` `jsonb` (`[{ "subject": "Mathematics", "minPercent": 60 }]`), `citizenship`, `provinces` `text[]` (empty = national), `all_fields_eligible` bool, `open_date` date?, `closing_date` date?, `status`, `apply_url`, `extraction_confidence` `numeric(3,2)`, `locked_fields` `text[]`, `is_published` bool, `last_seen_at` | GIN index on `normalized_name gin_trgm_ops`; index on (`status`, `closing_date`) |
| `bursary_field` | `bursary_id`, `field_of_study_id` | eligible fields |
| `bursary_qualification` | `bursary_id`, `qualification_id` | specific qualifications named by the funder |
| `bursary_institution` | `bursary_id`, `institution_id` | **restriction**: if any rows exist, only these institutions are eligible |
| `source` | `id`, `key` **unique** (e.g. `zabursaries`), `name`, `base_url`, `icon_url`, `enabled`, `crawl_delay_ms` (default 3000) | one row per aggregator; seeded |
| `bursary_source` | `bursary_id`, `source_id`, `source_url`, `first_seen_at`, `last_seen_at` | composite PK (`bursary_id`, `source_id`, `source_url`); powers the source icons (FR-05) |

### 2.3 Ingestion (FR-20–FR-27)
| Table | Columns |
|---|---|
| `raw_listing` | `id`, `source_id`, `url` (unique with `source_id`), `content_hash` (SHA-256 hex), `html` (text, latest only), `parsed` jsonb, `extracted` jsonb, `bursary_id`? →, `first_fetched_at`, `last_fetched_at`, `last_changed_at`, `missed_runs` int |
| `ingestion_run` | `id`, `trigger`, `status`, `started_at`, `finished_at` |
| `ingestion_run_source` | `id`, `run_id` →, `source_id` →, `discovered`, `fetched`, `unchanged`, `created`, `updated`, `failed` (ints), `error_summary` text |
| `ingestion_run_request` | `id`, `source_id`? (null = all), `requested_by_user_id`, `requested_at`, `picked_up_at`? |

### 2.4 Reports & review (FR-08, FR-41)
| Table | Columns |
|---|---|
| `listing_report` | `id`, `bursary_id` →, `reason`, `comment` (≤ 1000), `contact_email`?, `ip_hash`, `status` |
| `review_item` | `id`, `type`, `status`, `bursary_id`?, `raw_listing_id`?, `listing_report_id`?, `details` jsonb, `resolved_by_user_id`?, `resolved_at`?, `notes` |

### 2.5 Users & tracker (FR-09–FR-14)
| Table | Columns |
|---|---|
| ASP.NET Identity tables | `Guid` keys; roles `Student`, `Admin` |
| `user_profile` | `user_id` PK →, `study_level`?, `institution_id`?, `qualification_id`?, `average`? |
| `tracker_entry` | `id`, `user_id` →, `bursary_id` →, `status`, `applied_on` date?, `notes` (≤ 2000); unique (`user_id`, `bursary_id`) |

## 3. Reference seed data (`data/seed/*.json`)

### 3.1 Fields of study: CESM first order (DHET Classification of Educational Subject Matter)
`01` Agriculture, Agricultural Operations and Related Sciences · `02` Architecture and the Built Environment · `03` Visual and Performing Arts · `04` Business, Economics and Management Studies · `05` Communication, Journalism and Related Studies · `06` Computer and Information Sciences · `07` Education · `08` Engineering · `09` Health Professions and Related Clinical Sciences · `10` Family Ecology and Consumer Sciences · `11` Languages, Linguistics and Literature · `12` Law · `13` Life Sciences · `14` Physical Sciences · `15` Mathematics and Statistics · `16` Military Sciences · `17` Philosophy, Religion and Theology · `18` Psychology · `19` Public Management and Services · `20` Social Sciences

Second-order codes (e.g. `0801` Chemical Engineering) are added by a dedicated task and must cite the DHET source document.

### 3.2 Institutions: the 26 public universities
| Short | Name | Type | Province |
|---|---|---|---|
| UCT | University of Cape Town | Traditional | WC |
| Wits | University of the Witwatersrand | Traditional | GP |
| UP | University of Pretoria | Traditional | GP |
| SU | Stellenbosch University | Traditional | WC |
| UKZN | University of KwaZulu-Natal | Traditional | KZN |
| UFS | University of the Free State | Traditional | FS |
| NWU | North-West University | Traditional | NW |
| RU | Rhodes University | Traditional | EC |
| UWC | University of the Western Cape | Traditional | WC |
| UFH | University of Fort Hare | Traditional | EC |
| UL | University of Limpopo | Traditional | LP |
| SMU | Sefako Makgatho Health Sciences University | Traditional | GP |
| UJ | University of Johannesburg | Comprehensive | GP |
| NMU | Nelson Mandela University | Comprehensive | EC |
| UNISA | University of South Africa | Comprehensive | GP |
| Univen | University of Venda | Comprehensive | LP |
| UNIZULU | University of Zululand | Comprehensive | KZN |
| WSU | Walter Sisulu University | Comprehensive | EC |
| SPU | Sol Plaatje University | Comprehensive | NC |
| UMP | University of Mpumalanga | Comprehensive | MP |
| CPUT | Cape Peninsula University of Technology | UoT | WC |
| DUT | Durban University of Technology | UoT | KZN |
| TUT | Tshwane University of Technology | UoT | GP |
| VUT | Vaal University of Technology | UoT | GP |
| CUT | Central University of Technology | UoT | FS |
| MUT | Mangosuthu University of Technology | UoT | KZN |

*Classification by type follows DHET's grouping and should be double-checked in the seeding task.*

**MVP:** all 26 are seeded as institutions, but faculties and qualifications exist only for the big 5 (UCT, Wits, UP, SU, UJ). See brief FR-32.

### 3.3 Sources (seeded, `enabled=false` until their adapter is merged)
`zabursaries` (https://www.zabursaries.co.za), `studytrust` (https://studytrust.org.za), `allbursaries` (https://allbursaries.co.za), `gocareers`, `fundiconnect`, `careerdrive`, `bursaryza`, `edupstairs`, `applyorgza`.

## 4. Deduplication rules (FR-24)
1. `normalized_name` = lowercase, remove the year tokens `20\d\d`, the words `bursary|bursaries|programme|program|scheme|scholarship`, punctuation, and repeated whitespace.
2. Candidates are bursaries where `similarity(normalized_name, @n) >= 0.4` (pg_trgm).
3. **Auto-merge** if the funders match (same `funder_id`) **and** similarity ≥ 0.6 **and** (closing dates are within 45 days of each other, or either is null).
4. **Possible duplicate** (`ReviewItem.PossibleDuplicate`, no merge) if similarity is 0.4–0.6 with the same funder, or ≥ 0.8 with no funder on either side.
5. Otherwise create a new bursary with `is_published = false` and a `ReviewItem.NewListing` (MVP: an admin publishes it). *A later setting may auto-publish when `extraction_confidence ≥ 0.85`.*

## 5. Matching algorithm (FR-01–FR-03) {#5-matching-algorithm}

**Input** (`BursarySearchCriteria`): `qualificationId?`, `fieldIds[]?`, `institutionId?`, `studyLevel?`, `average?`, `closingFrom?`, `closingTo?`, `includeClosed` (default false), `sort` (`BestMatch` | `ClosingSoonest` | `Newest`), `page`, `pageSize` (≤ 50).

**Resolution:** if `qualificationId` is given, the target fields are its `qualification_field` rows **plus their CESM parents**, and `institutionId` defaults to the qualification's institution.

**Eligibility filter** (every condition must hold):
- `is_published = true`
- `includeClosed` **or** `status <> 'Closed'`
- Field match: `all_fields_eligible` **or** the bursary has a field in the target fields **or** the bursary lists `qualificationId`. *This filter is skipped when no qualification or field is given.*
- Institution: the bursary has no `bursary_institution` rows **or** has one for `institutionId`
- Level: `study_levels` is empty (unknown) **or** contains `studyLevel`
- Marks: `min_average IS NULL` **or** `min_average <= average`
- Closing-date range, if given

**Score** (for `BestMatch`): `+3` qualification listed explicitly · `+2` second-order field match · `+1` first-order field match · `+0.5` all fields eligible · `-0.5` study levels unknown. Ties are broken by `closing_date ASC NULLS LAST`. Closed bursaries (when included) always sort last.

**Output:** `BursarySummaryResponse { id, slug, name, funderName, closingDate, isClosingSoon, status, studyLevels, minAverage, coverage, sources: [{ key, name, iconUrl, url }], matchScore }`.
