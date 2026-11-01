---
id: T-010
title: Research and seed faculties + popular qualifications for 5 universities
phase: 1
area: data
model: sonnet
status: todo
depends_on: [T-008]
requirements: [FR-01, FR-30, FR-32]
issue: null
---

## Goal
Students at the five largest contact universities can pick their actual qualification. Faculties and their most popular undergraduate qualifications are seeded and mapped to CESM fields of study.

## Context
- `AGENTS.md`
- `specs/00-product-brief.md` §4.4
- `specs/02-data-model.md` §2.1, §3.1
- `data/seed/README.md`, `data/seed/fields-of-study.json`, `data/seed/institutions.json`

## Scope
You may create or modify **only**:
- `data/seed/faculties/uct.json`, `wits.json`, `up.json`, `su.json`, `uj.json`
- `data/seed/schemas/faculties.schema.json`
- `data/seed/README.md` (add a section)

## Requirements
1. One file per institution in this format:
```json
{
  "institutionSlug": "uct",
  "sourceUrls": ["https://..."],
  "faculties": [
    {
      "name": "Engineering & the Built Environment",
      "slug": "engineering-built-environment",
      "qualifications": [
        { "name": "BSc (Eng) Electrical Engineering", "abbreviation": "BSc(Eng)", "studyLevel": "Undergraduate",
          "nqfLevel": 8, "durationYears": 4, "slug": "bsc-eng-electrical", "fieldCodes": ["08"] }
      ]
    }
  ]
}
```
2. Use the universities' **official** faculty and programme pages. Put every URL you used in `sourceUrls`. Don't invent programmes. If you can't verify something, leave it out.
3. For each university: all faculties, and **up to 10** of the most common undergraduate qualifications per faculty.
4. `fieldCodes` must be codes that exist in `fields-of-study.json` (first order only for now). A qualification may have several.
5. Add a schema and validate against it.
6. In the PR description, flag that this data **needs human review** before merge.

## Acceptance criteria
- [ ] 5 files, each valid against the schema
- [ ] Every `fieldCodes` entry exists in `fields-of-study.json` (check with the script below)
- [ ] Every file lists at least one official `sourceUrls` entry on the university's domain
- [ ] PR description says "needs human review" and lists any doubtful entries
- [ ] Task file `status: done`

## How to verify
```bash
for f in data/seed/faculties/*.json; do npx --yes ajv-cli@5 validate --spec=draft2020 -s data/seed/schemas/faculties.schema.json -d $f; done
node -e "const codes=new Set(require('./data/seed/fields-of-study.json').map(f=>f.code));for(const u of ['uct','wits','up','su','uj']){const d=require('./data/seed/faculties/'+u+'.json');for(const f of d.faculties)for(const q of f.qualifications)for(const c of q.fieldCodes)if(!codes.has(c))throw new Error(u+': '+q.name+' bad code '+c)};console.log('ok')"
```
