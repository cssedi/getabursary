---
id: T-010
title: Research and seed faculties + popular qualifications for UCT and Wits
phase: 1
area: data
model: sonnet
status: done
depends_on: [T-008]
requirements: [FR-01, FR-30, FR-32]
issue: 10
---

## Goal
Students at **UCT and Wits** can pick their actual qualification. (Scope narrowed on 2026-11-02: UP, SU and UJ block automated access, so they move to T-024, which works from prospectus PDFs. This task still adds the shared schema.) Faculties and their most popular undergraduate qualifications are seeded and mapped to CESM fields of study.

## Context
- `AGENTS.md`
- `specs/00-product-brief.md` §4.4
- `specs/02-data-model.md` §2.1, §3.1
- `data/seed/README.md`, `data/seed/fields-of-study.json`, `data/seed/institutions.json`

## Scope
You may create or modify **only**:
- `data/seed/faculties/uct.json`, `wits.json`
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
2. Use the universities' **official** faculty and programme pages, read with **WebFetch** (plain `curl` gets bot-challenged; if Wits' course finder loads programmes dynamically, use its faculty/school pages instead). Put every URL you used in `sourceUrls`. Don't invent programmes. If you can't verify something, leave it out.
3. For each university: all faculties, and **up to 10** of the most common undergraduate qualifications per faculty.
4. `fieldCodes` must be codes that exist in `fields-of-study.json` (first order only for now). A qualification may have several.
5. Add a schema and validate against it.
6. In the PR description, flag that this data **needs human review** before merge.

## Acceptance criteria
- [ ] 2 files (`uct.json`, `wits.json`), each valid against the schema
- [ ] Every `fieldCodes` entry exists in `fields-of-study.json` (check with the script below)
- [ ] Every file lists at least one official `sourceUrls` entry on the university's domain
- [ ] PR description says "needs human review" and lists any doubtful entries
- [ ] Task file `status: done`

## How to verify
```bash
for f in data/seed/faculties/*.json; do npx --yes ajv-cli@5 validate --spec=draft2020 -s data/seed/schemas/faculties.schema.json -d $f; done
node -e "const codes=new Set(require('./data/seed/fields-of-study.json').map(f=>f.code));for(const u of ['uct','wits']){const d=require('./data/seed/faculties/'+u+'.json');for(const f of d.faculties)for(const q of f.qualifications)for(const c of q.fieldCodes)if(!codes.has(c))throw new Error(u+': '+q.name+' bad code '+c)};console.log('ok')"
```
