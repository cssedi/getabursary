---
id: T-024
title: Seed faculties + popular qualifications for UP, SU and UJ from prospectus PDFs
phase: 1
area: data
model: sonnet
status: blocked
depends_on: [T-010]
requirements: [FR-01, FR-30, FR-32]
issue: 31
---

> **Blocked until the product owner adds the PDFs.** Set `status: todo` once `data/sources/prospectuses/up.pdf`, `su.pdf` and `uj.pdf` are on `main`.

## Goal
Students at **UP, SU and UJ** can pick their actual qualification, completing the big-5 MVP coverage (brief FR-32). These sites block automated access, so the source is each university's official undergraduate prospectus PDF, committed to the repo.

## Context
- `AGENTS.md`
- `tasks/T-010-faculties-and-qualifications-seed.md` (same file format, rules and schema; copy its approach)
- `specs/00-product-brief.md` §4.4, `specs/02-data-model.md` §2.1, §3.1
- `data/seed/README.md`, `data/seed/fields-of-study.json`, `data/seed/schemas/faculties.schema.json`, `data/seed/faculties/uct.json` (worked example)
- Sources: `data/sources/prospectuses/up.pdf`, `su.pdf`, `uj.pdf` (read them with the Read tool; read page ranges, they are long)

## Scope
You may create or modify **only**:
- `data/seed/faculties/up.json`, `su.json`, `uj.json`
- `data/seed/README.md` (add the three universities to the existing section)

## Requirements
1. Same JSON format and schema as T-010. Do not change the schema.
2. Source of truth is the PDF. Set `sourceUrls` to the university's official prospectus page URL (as printed in the PDF or `data/sources/prospectuses/README.md`) and also record the PDF path and page numbers per faculty in `data/seed/README.md`.
3. For each university: all faculties (UJ: treat the College of Business and Economics as a faculty), and **up to 10** of the most common undergraduate qualifications per faculty, exactly as named in the prospectus.
4. Only first-order CESM `fieldCodes` that exist in `fields-of-study.json`.
5. Don't invent anything that isn't in the PDF. In the PR description, flag **needs human review** and list doubtful entries.

## Acceptance criteria
- [ ] 3 files (`up.json`, `su.json`, `uj.json`), each valid against the schema
- [ ] Every `fieldCodes` entry exists in `fields-of-study.json`
- [ ] README lists PDF page references per faculty
- [ ] PR description says "needs human review"
- [ ] Task file `status: done`

## How to verify
```bash
for f in up su uj; do npx --yes ajv-cli@5 validate --spec=draft2020 -s data/seed/schemas/faculties.schema.json -d data/seed/faculties/$f.json; done
node -e "const codes=new Set(require('./data/seed/fields-of-study.json').map(f=>f.code));for(const u of ['up','su','uj']){const d=require('./data/seed/faculties/'+u+'.json');for(const f of d.faculties)for(const q of f.qualifications)for(const c of q.fieldCodes)if(!codes.has(c))throw new Error(u+': '+q.name+' bad code '+c)};console.log('ok')"
```
