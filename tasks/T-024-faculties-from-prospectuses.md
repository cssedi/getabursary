---
id: T-024
title: Seed faculties + popular qualifications for UJ and UP from university PDFs
phase: 1
area: data
model: sonnet
status: done
depends_on: [T-010]
requirements: [FR-01, FR-30, FR-32]
issue: 31
---

> PDFs added 2026-11-02 (see `data/sources/prospectuses/README.md`). **SU is excluded** by product-owner decision.

## Goal
Students at **UJ and UP** can pick their actual qualification (brief FR-32; SU is deferred). These sites block automated access, so the source is official university PDFs committed to the repo.

## Context
- `AGENTS.md`
- `tasks/T-010-faculties-and-qualifications-seed.md` (same file format, rules and schema; copy its approach)
- `specs/00-product-brief.md` §4.4, `specs/02-data-model.md` §2.1, §3.1
- `data/seed/README.md`, `data/seed/fields-of-study.json`, `data/seed/schemas/faculties.schema.json`, `data/seed/faculties/uct.json` (worked example)
- **`data/sources/prospectuses/README.md`** (which file to use for what; read it first)
- Sources: `data/sources/prospectuses/uj/*.pdf`, `data/sources/prospectuses/up/*.pdf`. Read them with the Read tool, using page ranges. Don't read the 200+ page yearbooks end to end: find the programme-list pages first (e.g. `pdftotext file.pdf - | grep -n` for programme names) and read only those.

## Scope
You may create or modify **only**:
- `data/seed/faculties/up.json`, `uj.json`
- `data/seed/README.md` (add the three universities to the existing section)

## Requirements
1. Same JSON format and schema as T-010. Do not change the schema.
2. Source of truth is the PDFs. Set `sourceUrls` to the university's official website URL as printed in the PDF (e.g. `https://www.uj.ac.za`, `https://www.up.ac.za`), and record the PDF path and page numbers per faculty in `data/seed/README.md`. For UJ, the 2027 prospectus wins when documents disagree.
   **UP data comes from 2021 yearbook extracts:** add a top-level `"sourceYear": 2021` note in `data/seed/README.md` for UP, and in the PR list any UP programme you suspect may have been renamed or discontinued. (Don't change the schema.)
3. For each university: all faculties (UJ: treat the College of Business and Economics as a faculty), and **up to 10** of the most common undergraduate qualifications per faculty, exactly as named in the prospectus.
4. Only first-order CESM `fieldCodes` that exist in `fields-of-study.json`.
5. Don't invent anything that isn't in the PDF. In the PR description, flag **needs human review** and list doubtful entries.

## Acceptance criteria
- [ ] 2 files (`uj.json`, `up.json`), each valid against the schema
- [ ] Every `fieldCodes` entry exists in `fields-of-study.json`
- [ ] README lists PDF page references per faculty
- [ ] PR description says "needs human review"
- [ ] Task file `status: done`

## How to verify
```bash
for f in up uj; do npx --yes ajv-cli@5 validate --spec=draft2020 -s data/seed/schemas/faculties.schema.json -d data/seed/faculties/$f.json; done
node -e "const codes=new Set(require('./data/seed/fields-of-study.json').map(f=>f.code));for(const u of ['up','uj']){const d=require('./data/seed/faculties/'+u+'.json');for(const f of d.faculties)for(const q of f.qualifications)for(const c of q.fieldCodes)if(!codes.has(c))throw new Error(u+': '+q.name+' bad code '+c)};console.log('ok')"
```
