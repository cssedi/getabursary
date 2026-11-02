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

## Needs verification

These values were recorded without a live check. A reviewer should confirm each one before the seeder is run against production data.

**Institution websites (`websiteUrl`).** Every university URL in `institutions.json` is unverified. Each was recorded from memory of the institution's official domain and has not been opened or checked. Confirm each one loads and is the official homepage:

- UCT, Wits, UP, SU, UKZN, UFS, NWU, RU, UWC, UFH, UL, SMU
- UJ, NMU (`mandela.ac.za`, which is not the same as the short name), UNISA, Univen, UNIZULU, WSU, SPU, UMP
- CPUT, DUT, TUT, VUT, CUT, MUT

**Institution type grouping.** The spec (§3.2) says to double-check the Traditional, Comprehensive and University of Technology grouping against DHET's list.

**Source icons.** `iconUrl` assumes `/favicon.ico` on each site. Confirm each path returns an image, or update the value.

**Source robots.txt and terms.** Brief §6.1 says each candidate aggregator must be checked for `robots.txt`, terms and structure before its adapter is enabled. That work belongs to the adapter tasks, not this seed.
