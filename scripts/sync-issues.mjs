#!/usr/bin/env node
// Mirrors tasks/T-*.md to GitHub Issues (task files are the source of truth).
//
// Usage:
//   node scripts/sync-issues.mjs --check     validate front-matter + dependency graph (used in CI, no network)
//   node scripts/sync-issues.mjs --ready     list tasks whose dependencies are all done, critical path first
//   node scripts/sync-issues.mjs --ready --json   same, as JSON (used by the autopilot workflow)
//   node scripts/sync-issues.mjs             create/update issues and labels (needs `gh` authenticated)
//   node scripts/sync-issues.mjs --dry-run   show what would be created/updated
//
// Env: GITHUB_REPOSITORY (default cssedi/getabursary)

import { readFileSync, writeFileSync, readdirSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { execFileSync } from 'node:child_process';

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..');
const TASKS_DIR = join(ROOT, 'tasks');
const REPO = process.env.GITHUB_REPOSITORY || 'cssedi/getabursary';
const args = new Set(process.argv.slice(2));

const STATUSES = ['todo', 'ready', 'in-progress', 'done', 'blocked'];
const MODELS = ['haiku', 'sonnet'];
const AREAS = ['infra', 'core', 'api', 'worker', 'web', 'data', 'docs'];
const REQUIRED = ['id', 'title', 'phase', 'area', 'model', 'status', 'depends_on', 'requirements'];

const LABELS = {
  task: { color: '1d76db', description: 'Agent task mirrored from tasks/' },
  'agent:ready': { color: '0e8a16', description: 'Dependencies done; implementer agent may start' },
  'agent:blocked': { color: 'b60205', description: 'Agent needs a human decision' },
  'agent:in-progress': { color: 'fbca04', description: 'Autopilot has started an implementer on this task' },
  'model:haiku': { color: 'c5def5', description: 'Implement with the small model' },
  'model:sonnet': { color: '5319e7', description: 'Implement with the medium model' },
  ...Object.fromEntries(AREAS.map((a) => [`area:${a}`, { color: 'fbca04', description: `Area: ${a}` }])),
  ...Object.fromEntries([0, 1, 2, 3, 4, 5, 6].map((p) => [`phase:${p}`, { color: 'bfdadc', description: `Phase ${p}` }])),
};

// ---------- parsing ----------
function parseValue(raw) {
  const v = raw.trim();
  if (v === 'null' || v === '') return null;
  if (/^\[.*\]$/.test(v)) {
    const inner = v.slice(1, -1).trim();
    return inner ? inner.split(',').map((s) => s.trim().replace(/^['"]|['"]$/g, '')) : [];
  }
  if (/^-?\d+$/.test(v)) return Number(v);
  return v.replace(/^['"]|['"]$/g, '');
}

function readTask(file) {
  const text = readFileSync(join(TASKS_DIR, file), 'utf8');
  const m = text.match(/^---\n([\s\S]*?)\n---\n([\s\S]*)$/);
  if (!m) throw new Error(`${file}: missing front-matter block`);
  const meta = {};
  for (const line of m[1].split('\n')) {
    if (!line.trim() || line.trim().startsWith('#')) continue;
    const idx = line.indexOf(':');
    if (idx < 0) throw new Error(`${file}: bad front-matter line "${line}"`);
    meta[line.slice(0, idx).trim()] = parseValue(line.slice(idx + 1));
  }
  return { file, meta, body: m[2], text, rawMeta: m[1] };
}

function loadTasks() {
  return readdirSync(TASKS_DIR)
    .filter((f) => /^T-\d{3}-.*\.md$/.test(f))
    .sort()
    .map(readTask);
}

// ---------- validation ----------
function validate(tasks) {
  const errors = [];
  const byId = new Map();
  for (const t of tasks) {
    for (const k of REQUIRED) if (!(k in t.meta)) errors.push(`${t.file}: missing "${k}"`);
    const { id, status, model, area, depends_on, requirements } = t.meta;
    if (id && !t.file.startsWith(`${id}-`)) errors.push(`${t.file}: file name must start with "${id}-"`);
    if (byId.has(id)) errors.push(`${t.file}: duplicate id ${id}`);
    byId.set(id, t);
    if (status && !STATUSES.includes(status)) errors.push(`${t.file}: status must be one of ${STATUSES}`);
    if (model && !MODELS.includes(model)) errors.push(`${t.file}: model must be one of ${MODELS}`);
    if (area && !AREAS.includes(area)) errors.push(`${t.file}: area must be one of ${AREAS}`);
    if (!Array.isArray(depends_on)) errors.push(`${t.file}: depends_on must be a list`);
    if (!Array.isArray(requirements)) errors.push(`${t.file}: requirements must be a list`);
    for (const section of ['## Goal', '## Context', '## Scope', '## Acceptance criteria', '## How to verify']) {
      if (!t.body.includes(section)) errors.push(`${t.file}: missing section "${section}"`);
    }
  }
  for (const t of tasks) {
    for (const d of t.meta.depends_on || []) if (!byId.has(d)) errors.push(`${t.file}: unknown dependency ${d}`);
  }
  // cycle detection
  const state = new Map();
  const visit = (id, path) => {
    if (state.get(id) === 'done') return;
    if (state.get(id) === 'visiting') { errors.push(`dependency cycle: ${[...path, id].join(' -> ')}`); return; }
    state.set(id, 'visiting');
    for (const d of byId.get(id)?.meta.depends_on || []) if (byId.has(d)) visit(d, [...path, id]);
    state.set(id, 'done');
  };
  for (const id of byId.keys()) visit(id, []);
  return { errors, byId };
}

// Priority = length of the longest chain of tasks that waits on this one (critical path first),
// then number of tasks that transitively depend on it, then task id.
function priorities(tasks) {
  const dependents = new Map(tasks.map((t) => [t.meta.id, []]));
  for (const t of tasks) for (const d of t.meta.depends_on || []) dependents.get(d)?.push(t.meta.id);
  const depth = new Map();
  const chain = (id) => {
    if (!depth.has(id)) depth.set(id, 1 + Math.max(0, ...dependents.get(id).map(chain)));
    return depth.get(id);
  };
  const reach = (id, seen = new Set()) => {
    for (const c of dependents.get(id)) if (!seen.has(c)) { seen.add(c); reach(c, seen); }
    return seen;
  };
  return new Map(tasks.map((t) => [t.meta.id, { chain: chain(t.meta.id), unlocks: reach(t.meta.id).size }]));
}

function readyTasks(tasks, byId) {
  const prio = priorities(tasks);
  return tasks
    .filter(
      (t) => ['todo', 'ready'].includes(t.meta.status) &&
        (t.meta.depends_on || []).every((d) => byId.get(d)?.meta.status === 'done'),
    )
    .map((t) => Object.assign(t, { priority: prio.get(t.meta.id) }))
    .sort((a, b) => b.priority.chain - a.priority.chain || b.priority.unlocks - a.priority.unlocks ||
      a.meta.id.localeCompare(b.meta.id));
}

// ---------- GitHub ----------
function gh(method, path, payload) {
  const a = ['api', '-X', method, `repos/${REPO}${path}`, '-H', 'Accept: application/vnd.github+json'];
  if (payload) a.push('--input', '-');
  const out = execFileSync('gh', a, { input: payload ? JSON.stringify(payload) : undefined, encoding: 'utf8' });
  return out ? JSON.parse(out) : null;
}

function ensureLabels(dry) {
  const existing = new Set(gh('GET', '/labels?per_page=100').map((l) => l.name));
  for (const [name, { color, description }] of Object.entries(LABELS)) {
    if (existing.has(name)) continue;
    console.log(`label + ${name}`);
    if (!dry) gh('POST', '/labels', { name, color, description });
  }
}

function issueBody(t, byId) {
  const deps = (t.meta.depends_on || []).map((d) => {
    const dep = byId.get(d);
    return dep?.meta.issue ? `#${dep.meta.issue} (${d})` : d;
  });
  const header = [
    `> 📄 Source of truth: [\`tasks/${t.file}\`](https://github.com/${REPO}/blob/main/tasks/${t.file}). Edit the file, not this issue.`,
    `> **Depends on:** ${deps.length ? deps.join(', ') : 'nothing'} · **Requirements:** ${(t.meta.requirements || []).join(', ') || '–'} · **Model:** ${t.meta.model}`,
    '',
  ].join('\n');
  return `${header}\n${t.body.trim()}\n`;
}

function setIssueNumber(t, number) {
  const newMeta = /^issue:.*$/m.test(t.rawMeta)
    ? t.rawMeta.replace(/^issue:.*$/m, `issue: ${number}`)
    : `${t.rawMeta}\nissue: ${number}`;
  writeFileSync(join(TASKS_DIR, t.file), t.text.replace(t.rawMeta, newMeta));
  t.meta.issue = number;
}

// ---------- main ----------
const tasks = loadTasks();
const { errors, byId } = validate(tasks);
if (errors.length) {
  console.error(errors.map((e) => `✗ ${e}`).join('\n'));
  process.exit(1);
}
if (args.has('--check')) {
  console.log(`✓ ${tasks.length} task files valid; dependency graph is acyclic.`);
  process.exit(0);
}
if (args.has('--ready')) {
  const ready = readyTasks(tasks, byId);
  if (args.has('--json')) {
    console.log(JSON.stringify(ready.map((t) => ({
      id: t.meta.id, issue: t.meta.issue, model: t.meta.model, title: t.meta.title, file: t.file, ...t.priority,
    }))));
  } else {
    for (const t of ready) {
      console.log(`${t.meta.id}  #${t.meta.issue ?? '?'}  [${t.meta.model}]  chain=${t.priority.chain} unlocks=${t.priority.unlocks}  ${t.meta.title}`);
    }
  }
  process.exit(0);
}

const dry = args.has('--dry-run');
ensureLabels(dry);
// Pass 1: create missing issues (so dependency references can use issue numbers).
for (const t of tasks) {
  if (t.meta.issue) continue;
  console.log(`issue + ${t.meta.id} ${t.meta.title}`);
  if (dry) continue;
  const created = gh('POST', '/issues', { title: `[${t.meta.id}] ${t.meta.title}`, body: 'Syncing…' });
  setIssueNumber(t, created.number);
}
// Pass 2: update titles, bodies, labels and state.
for (const t of tasks) {
  if (!t.meta.issue) continue;
  const labels = ['task', `phase:${t.meta.phase}`, `area:${t.meta.area}`, `model:${t.meta.model}`];
  if (t.meta.status === 'blocked') labels.push('agent:blocked');
  const current = dry ? null : gh('GET', `/issues/${t.meta.issue}`);
  if (current?.labels?.some((l) => l.name === 'agent:ready')) labels.push('agent:ready');
  const payload = {
    title: `[${t.meta.id}] ${t.meta.title}`,
    body: issueBody(t, byId),
    labels,
    state: t.meta.status === 'done' ? 'closed' : 'open',
  };
  console.log(`issue ~ #${t.meta.issue} ${t.meta.id} (${payload.state})`);
  if (!dry) gh('PATCH', `/issues/${t.meta.issue}`, payload);
}
