# Reference seed data

Version-controlled reference data that the `DataSeeder` (T-009) loads idempotently into the database. Specs: [`specs/02-data-model.md`](../../specs/02-data-model.md) §1 (enums) and §3 (lists).

## Schemas

Each data file is a bare JSON array, so it cannot carry `$schema` itself. Validate against the schema below, using draft 2020-12:

| Data file | Schema |
|---|---|
| `fields-of-study.json` | `schemas/fields-of-study.schema.json` |
| `institutions.json` | `schemas/institutions.schema.json` |
| `sources.json` | `schemas/sources.schema.json` |

```json
{ "$schema": "./schemas/fields-of-study.schema.json" }
{ "$schema": "./schemas/institutions.schema.json" }
{ "$schema": "./schemas/sources.schema.json" }
```

Validate:

```bash
for f in fields-of-study institutions sources; do npx --yes ajv-cli@5 validate --spec=draft2020 -s data/seed/schemas/$f.schema.json -d data/seed/$f.json; done
```

JSON Schema cannot express "unique across items" for `code`, `slug`, `shortName` or `key`. Uniqueness and counts are checked by the node command in the T-008 "How to verify" section, and should also be covered by seeder tests in T-009.

## Files

### `fields-of-study.json`
The 20 first-order fields of the **CESM** (Classification of Educational Subject Matter), used to tag qualifications and bursaries. Each entry is `{ code, name, slug, parentCode }`. `parentCode` is `null` for first-order codes. Second-order codes (for example `0801` Chemical Engineering) are added by a later task and must cite the DHET source.

- **Source:** Department of Higher Education and Training (DHET), *Classification of Educational Subject Matter (CESM)*, first-order fields. Names are copied exactly from spec §3.1.
- **Slugs:** lowercase kebab-case of the full name, with punctuation removed (for example `business-economics-and-management-studies`).

### `institutions.json`
The 26 public universities: 12 traditional, 8 comprehensive and 6 universities of technology (grouping as in spec §3.2). Each entry is `{ shortName, name, slug, type, province, websiteUrl }`.

- **Source:** the institutions listed in spec §3.2, with the grouping in that table. The spec says the type grouping should be double-checked against DHET's current classification.
- `type` and `province` use the enum names from spec §1 (`TraditionalUniversity`, `WesternCape`, and so on).
- `websiteUrl` is the official homepage. An empty string means "not yet verified" (see below).

### `sources.json`
The 9 aggregator sites from spec §3.3 and brief §6.1. Each entry is `{ key, name, baseUrl, iconUrl, enabled, crawlDelayMs }`.

- All sources are `enabled: false` until their adapter is merged. Enabling a source is a deliberate change, made with its adapter task.
- `crawlDelayMs` is 3000 for every source. Scrapers must keep at least this gap between requests (conventions §7).
- `iconUrl` is `<baseUrl>/favicon.ico`, which is the convention from the task. Some sites may not serve it at that path. See "Needs verification".

## How to change these files

1. Edit the JSON in the same PR as the change. Keep the array order stable, so the diff stays readable.
2. Keep the schema in sync with any new field or enum value. Enum values must match spec §1.
3. Run the validation commands above and check the counts. The expected counts are 20 fields, 26 institutions and 9 sources. If a count changes, update the spec first.
4. Seed data is loaded by the `DataSeeder`, never by `HasData` (conventions §4). How the seeder treats rows removed from these files is defined in T-009.

## Faculties and qualifications (`faculties/*.json`)

One file per institution, validated against `schemas/faculties.schema.json` (draft 2020-12). Each file has `institutionSlug`, `sourceUrls` and `faculties`. Each faculty has `name`, `slug` and `qualifications`. A qualification has `name`, `abbreviation?`, `studyLevel`, `nqfLevel?`, `durationYears?`, `slug` and `fieldCodes`. `fieldCodes` are first-order CESM codes from `fields-of-study.json`.

- **Covered:** UCT and Wits (T-010). UP and UJ (T-024), from the prospectus PDFs below. SU is excluded by product-owner decision.
- **Sources:** official faculty and programme pages, listed in each file's `sourceUrls`. Nothing is included that could not be read there.
- **Optional fields:** `abbreviation`, `nqfLevel` and `durationYears` are left out when the source page did not state them. No `nqfLevel` is set anywhere yet.
- **Scope:** at most 10 undergraduate qualifications per faculty. Where a degree has many majors (for example the UCT BSc, BA and BCom), the degree is one qualification.
- **Field mapping:** `fieldCodes` are the seeder author's judgement and need human review.

### UJ and UP sources (T-024)

Both sites block automated access, so the data comes from PDFs under `data/sources/prospectuses/`. `sourceUrls` are the official sites printed in the PDFs. Page numbers are PDF page numbers (they match the printed numbers in the UJ prospectus).

**UJ** (`uj.json`): `uj/uj-undergraduate-prospectus-2027.pdf` is the only source used. The College of Business and Economics is treated as a faculty. Where a degree has many majors, the degree is one qualification. Qualifications whose heading and programme are printed separately are written as `Bachelor of Commerce (Accounting)`.

| Faculty | Pages |
|---|---|
| Art, Design and Architecture | 32-34 (names in the admission table on 37) |
| College of Business and Economics | 40-45 |
| Education | 53-54 |
| Engineering and the Built Environment | 60-62 |
| Health Sciences | 66-69 |
| Humanities | 74-76 |
| Law | 82 |
| Science | 86-90 |

**UP** (`up.json`): `"sourceYear": 2021`. The schema has no field for this, so it is recorded here. All UP data comes from the 2021 yearbook extracts, and programme names may have changed since. UP names are the printed yearbook names (often abbreviations such as `BNurs`). `ENGAGE` variants and extended programmes are not listed separately. Pages are from the "Undergraduate Degree" list in each file.

| Faculty | File | Pages |
|---|---|---|
| Engineering, Built Environment and Information Technology | `up-yearbook-2021-ebit.pdf` | 9-10 |
| Education | `up-yearbook-2021-edu.pdf` | 5 |
| Economic and Management Sciences | `up-yearbook-2021-ems.pdf` | 7 |
| Humanities | `up-yearbook-2021-hum.pdf` | 11 |
| Law | `up-yearbook-2021-law.pdf` | 6 |
| Health Sciences | `up-yearbook-2021-med.pdf` | 8 |
| Natural and Agricultural Sciences | `up-yearbook-2021-sci.pdf` | 7-8 |
| Theology and Religion | `up-yearbook-2021-theo.pdf` | 6-7 |

The PDFs have no Veterinary Science faculty, so UP has none here yet.

Validate:

```bash
for f in data/seed/faculties/*.json; do npx --yes ajv-cli@5 validate --spec=draft2020 -s data/seed/schemas/faculties.schema.json -d $f; done
```

## Needs verification

These values were recorded without a live check. A reviewer should confirm each one before the seeder is run against production data.

**Institution websites (`websiteUrl`).** Every university URL in `institutions.json` is unverified. Each was recorded from memory of the institution's official domain and has not been opened or checked. Confirm each one loads and is the official homepage:

- UCT, Wits, UP, SU, UKZN, UFS, NWU, RU, UWC, UFH, UL, SMU
- UJ, NMU (`mandela.ac.za`, which is not the same as the short name), UNISA, Univen, UNIZULU, WSU, SPU, UMP
- CPUT, DUT, TUT, VUT, CUT, MUT

**Institution type grouping.** The spec (§3.2) says to double-check the Traditional, Comprehensive and University of Technology grouping against DHET's list.

**Source icons.** `iconUrl` assumes `/favicon.ico` on each site. Confirm each path returns an image, or update the value.

**Source robots.txt and terms.** Brief §6.1 says each candidate aggregator must be checked for `robots.txt`, terms and structure before its adapter is enabled. That work belongs to the adapter tasks, not this seed.
