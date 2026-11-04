// Usage: node scripts/visual-check.mjs <task-id> <path> [<path>…] [--sign-in-admin]
// Starts db + API + web, loads sample data, saves mobile/desktop screenshots, stops API + web (db stays up).
import { spawn, spawnSync } from 'node:child_process';
import { mkdirSync, readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const web = join(root, 'src', 'web');
const isWin = process.platform === 'win32';
const args = process.argv.slice(2);
const signIn = args.includes('--sign-in-admin');
const [taskId, ...paths] = args.filter((a) => !a.startsWith('--'));
if (!taskId || paths.length === 0) {
  console.error('Usage: node scripts/visual-check.mjs <task-id> <path> [<path>…] [--sign-in-admin]');
  process.exit(2);
}

// Dev-only admin, created by the API's AuthSeeder in Development. Never use on a real database.
const ADMIN_EMAIL = 'admin@getabursary.local';
const ADMIN_PASSWORD = 'DevOnly-Passw0rd!';

const children = [];
const stop = () => {
  for (const c of children) {
    if (c.exitCode !== null) continue;
    // shortcut: process-group kill on POSIX, taskkill tree on Windows; revisit if orphans show up.
    if (isWin) spawnSync('taskkill', ['/pid', String(c.pid), '/t', '/f']);
    else {
      try { process.kill(-c.pid, 'SIGTERM'); } catch { /* already gone */ }
    }
  }
};
process.on('SIGINT', () => { stop(); process.exit(130); });

function run(cmd, cmdArgs, opts = {}) {
  const r = spawnSync(cmd, cmdArgs, { cwd: root, encoding: 'utf8', shell: isWin, ...opts });
  if (r.status !== 0) throw new Error(`${cmd} ${cmdArgs.join(' ')} failed (${r.status}): ${r.stderr || r.stdout || r.error}`);
  return r;
}

function start(name, cmd, cmdArgs, cwd, env = {}) {
  const c = spawn(cmd, cmdArgs, { cwd, env: { ...process.env, ...env }, shell: isWin, detached: !isWin, stdio: ['ignore', 'pipe', 'pipe'] });
  let tail = '';
  const keep = (d) => { tail = (tail + d).slice(-2000); };
  c.stdout.on('data', keep);
  c.stderr.on('data', keep);
  c.exited = new Promise((res) => c.on('exit', (code) => res(code)));
  c.tail = () => tail;
  c.name = name;
  children.push(c);
  return c;
}

async function waitFor(what, child, ok, timeoutMs) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    if (child.exitCode !== null) throw new Error(`${child.name} exited early (${child.exitCode}):\n${child.tail()}`);
    if (await ok().catch(() => false)) return;
    await new Promise((r) => setTimeout(r, 1000));
  }
  throw new Error(`Timed out waiting for ${what}.\n${child.tail()}`);
}

const slugOf = (p) => p.replace(/^\/+|\/+$/g, '').split('?')[0].replace(/[^a-zA-Z0-9]+/g, '-').replace(/^-|-$/g, '') || 'home';

try {
  console.log('Starting the database…');
  run('docker', ['compose', 'up', '-d', '--wait', 'db']);

  console.log('Starting the API…');
  const api = start('API', 'dotnet', ['run', '--project', 'src/GetABursary.Api', '--urls', 'http://localhost:5266'], root, {
    ASPNETCORE_ENVIRONMENT: 'Development',
    Database__MigrateOnStartup: 'true',
    Admin__Emails: ADMIN_EMAIL,
    Admin__InitialPassword: ADMIN_PASSWORD,
  });
  await waitFor('the API health check', api, async () => (await fetch('http://localhost:5266/api/v1/health')).ok, 240_000);

  console.log('Loading sample bursaries…');
  const sql = readFileSync(join(root, 'data', 'dev', 'sample-bursaries.sql'), 'utf8');
  run('docker', ['compose', 'exec', '-T', 'db', 'sh', '-c', 'psql -v ON_ERROR_STOP=1 -q -U "$POSTGRES_USER" -d "$POSTGRES_DB"'], { input: sql });

  console.log('Starting the web dev server…');
  const ng = start('web dev server', 'npm', ['start'], web);
  await waitFor('http://localhost:4200', ng, async () => (await fetch('http://localhost:4200/')).ok, 240_000);

  let chromium;
  try {
    ({ chromium } = createRequire(join(web, 'package.json'))('playwright'));
  } catch {
    throw new Error('Playwright is not installed. Run: cd src/web && npm ci && npx playwright install chromium');
  }
  const browser = await chromium.launch();
  const outDir = join(root, 'docs', 'screenshots', taskId);
  mkdirSync(outDir, { recursive: true });
  const saved = [];
  try {
    for (const [mode, viewport] of [['mobile', { width: 375, height: 812 }], ['desktop', { width: 1440, height: 900 }]]) {
      const context = await browser.newContext({ viewport, reducedMotion: 'reduce' });
      if (signIn) {
        const res = await context.request.post('http://localhost:4200/api/v1/auth/login?useCookies=true', { data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD }, headers: { 'X-Requested-With': 'getabursary' } });
        if (!res.ok()) throw new Error(`Admin sign-in failed (${res.status()})`);
      }
      const page = await context.newPage();
      for (const p of paths) {
        await page.goto(`http://localhost:4200${p.startsWith('/') ? p : '/' + p}`, { waitUntil: 'networkidle' });
        const file = join(outDir, `${slugOf(p)}-${mode}.png`);
        await page.screenshot({ path: file, fullPage: true });
        saved.push(file);
      }
      await context.close();
    }
  } finally {
    await browser.close();
  }
  console.log('Saved:\n' + saved.map((f) => '  ' + f).join('\n'));
} catch (e) {
  console.error(`visual-check failed: ${e.message}`);
  process.exitCode = 1;
} finally {
  stop();
}
