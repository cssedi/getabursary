---
id: T-008
title: Create reference seed JSON (fields of study, universities, sources)
phase: 1
area: data
model: haiku
status: todo
depends_on: []
requirements: [FR-31, FR-32, FR-05]
issue: null
---

## Goal
Reviewed, version-controlled reference data files that the seeder (T-009) will load: the 20 CESM first-order fields of study, the 26 public universities, and the aggregator sources.

## Context
- `AGENTS.md`
- `specs/02-data-model.md` §3 (all of it: the lists to transcribe) and §1 (enum values)

## Scope
You may create or modify **only**:
- `data/seed/fields-of-study.json`
- `data/seed/institutions.json`
- `data/seed/sources.json`
- `data/seed/README.md`
- `data/seed/schemas/*.schema.json`

## Requirements
1. `fields-of-study.json`: an array of `{ "code": "08", "name": "Engineering", "slug": "engineering", "parentCode": null }` for all 20 first-order codes in spec §3.1, copied **exactly**.
2. `institutions.json`: an array of `{ "shortName": "UCT", "name": "University of Cape Town", "slug": "uct", "type": "TraditionalUniversity", "province": "WesternCape", "websiteUrl": "https://www.uct.ac.za" }` for all 26 universities in spec §3.2. `type` and `province` must use the enum names from spec §1. Use each university's official homepage URL. If you aren't sure of a URL, leave `""` and list it in the README under "Needs verification".
3. `sources.json`: an array of `{ "key": "zabursaries", "name": "ZA Bursaries", "baseUrl": "https://www.zabursaries.co.za", "iconUrl": "https://www.zabursaries.co.za/favicon.ico", "enabled": false, "crawlDelayMs": 3000 }` for all 9 sources in spec §3.3 (find each site's base URL; the brief §6.1 has links).
4. JSON Schema (draft 2020-12) for each file in `schemas/`, with `$schema` references at the top of each data file's companion README (JSON arrays can't hold `$schema` themselves).
5. `README.md`: what each file is, its source (cite the DHET CESM document for fields), how to change it, and the "Needs verification" list.

## Acceptance criteria
- [ ] Exactly 20 fields, 26 institutions, 9 sources. Slugs are unique and kebab-case
- [ ] Each file validates against its schema (`npx --yes ajv-cli@5 validate -s data/seed/schemas/x.schema.json -d data/seed/x.json --spec=draft2020`)
- [ ] Enum values match `specs/02-data-model.md` §1 exactly
- [ ] Task file `status: done`

## How to verify
```bash
for f in fields-of-study institutions sources; do npx --yes ajv-cli@5 validate --spec=draft2020 -s data/seed/schemas/$f.schema.json -d data/seed/$f.json; done
node -e "for (const f of ['fields-of-study','institutions','sources']) console.log(f, require('./data/seed/'+f+'.json').length)"
```
