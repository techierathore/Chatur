// Acceptance tests: the estimate before a process starts, on Processes (REQ-UI-083, REQ-UI-089, REQ-UI-090, REQ-FN-107..111,
// REQ-FN-114, REQ-FN-116; mockups/processes.html "estimate-panel"). Black-box, web harness.
//
// The fixture project is "Fixture" (the name the BRD's fixtures F1..F6 and the Metrics Schema example use), a folder under
// tests/.artifacts/verify/fixtures/step-work/ opened through Start's own Project folders dialog. Each test rewrites its
// docs/ (phase-2 checklist) and docs/metrics/runs.jsonl from the BRD's fixture table, so no test depends on another.
//
// Seeding through the database (and why): the Model providers screen cannot add a model without a real provider and a real
// key, so a Model row "claude-opus-5-5" on tier 1 filed under a fixture provider is inserted straight into the harness's
// database (support/db.ts) and every other provider is set not-connected for the test; both are put back afterwards from a
// snapshot. The fixture provider's address is a local counting server (or a closed port), so a run that is started after a
// confirm cannot reach a real model, and the count of requests proves no model is asked for an estimate (REQ-FN-110).
import { Page, expect } from '@playwright/test';
import { createServer, Server } from 'http';
import { existsSync, mkdirSync, readdirSync, readFileSync, rmSync, writeFileSync } from 'fs';
import { dirname, join, resolve } from 'path';
import { signIn } from './support/auth';
import { query, restore, run, snapshot, Snapshot } from './support/db';
import { openProcessesTab, tab } from './support/phase2-tabs';
import { openStepProject, STEP_ROOT } from './support/step-work';
import { removeProjectsUnder } from './support/cleanup';
import { test, APP_LOG } from './support/workbench-a';

const NAME = 'Fixture';
const OPUS = 'claude-opus-5-5';
const SONNET = 'claude-sonnet-5-5';
const PROJECT = join(STEP_ROOT, NAME);
const SISTER_ROOT = resolve(__dirname, '../.artifacts/verify/fixtures/estimate-sisters');
const SHOT = resolve(__dirname, '../.artifacts/verify/screens/processes-estimate.png');
const COUNT_PORT = Number(process.env.CHATUR_ESTIMATE_COUNT_PORT ?? 18991);
const PROVIDER = 'Estimate fixture provider';

let page: Page;
let counter: Server;
let modelRequests = 0;
let saved: Snapshot | undefined;

// ---- fixtures F1..F6 as files --------------------------------------------------------------------------------------

type Rec = { cmd: string; reqs: string[]; tokens: number | null; seconds: number; model?: string; backfilled?: boolean };
const stamp = (aMs: number) => new Date(aMs).toISOString().replace(/\.\d+Z$/, 'Z');

/** Run records, three hours apart from 2026-10-01 as the unit tests' fixtures are; the same JSON shape (native types). */
function runLines(aRuns: Rec[], aVoid: number[] = []): string {
  const vBase = Date.UTC(2026, 9, 1);
  const vLines: string[] = [];
  const vStarts: string[] = [];
  aRuns.forEach((r, i) => {
    const vStart = vBase + (i + 1) * 3 * 3600_000;
    const vEnd = vStart + r.seconds * 1000;
    vStarts.push(stamp(vStart));
    const vRec: Record<string, unknown> = {
      v: 1, ts: stamp(vEnd), kind: 'run', app: NAME, cmd: r.cmd, mode: 'build', started: stamp(vStart), ended: stamp(vEnd),
      duration_s: r.seconds, reqs_touched: r.reqs, reqs_count: r.reqs.length, model: r.model ?? OPUS,
    };
    if (r.tokens !== null) vRec.tokens_out = r.tokens;
    vRec.tokens_scope = r.tokens === null ? 'none' : 'tree';
    if (r.backfilled) vRec.backfilled = true;
    vLines.push(JSON.stringify(vRec));
  });
  for (const i of aVoid) vLines.push(JSON.stringify({ v: 1, kind: 'run-void', cmd: aRuns[i].cmd, started: vStarts[i], reason: 'fixture' }));
  return vLines.join('\n') + '\n';
}

const b = (aReqs: string[], aTokens: number | null, aSeconds: number, aModel?: string, aBackfilled = false): Rec =>
  ({ cmd: 'build-phase', reqs: aReqs, tokens: aTokens, seconds: aSeconds, model: aModel, backfilled: aBackfilled });

/** F1: five build-phase runs on REQ-FN rows outside phase 2. */
function f1(aModels: (string | undefined)[] = []): Rec[] {
  return [
    b(['REQ-FN-201', 'REQ-FN-202'], 4000, 600, aModels[0]),
    b(['REQ-FN-203'], 3000, 240, aModels[1]),
    b(['REQ-FN-204', 'REQ-FN-205'], 10000, 900, aModels[2]),
    b(['REQ-FN-206', 'REQ-FN-207', 'REQ-FN-208', 'REQ-FN-209'], 4000, 480, aModels[3]),
    b(['REQ-FN-210', 'REQ-FN-211', 'REQ-FN-212', 'REQ-FN-213'], 16000, 1440, aModels[4]),
  ];
}

const checklist = (aRows: string[]) =>
  '# Fixture — Checklist (phase 2)\n\n## Requirements Status\n\n| ID | Requirement | Status | % | Remarks | Details |\n|----|-------------|--------|---|---------|---------|\n' +
  aRows.map((r) => `| ${r} | A row | Not Started | 0% | | |\n`).join('') + '\n## Page: Fixture\n';

function put(aPath: string, aText: string) {
  mkdirSync(dirname(aPath), { recursive: true });
  writeFileSync(aPath, aText);
}

/** Empties a folder file by file; a file the app still holds open on this file system is left, and written over later. */
function clean(aDir: string) {
  if (!existsSync(aDir)) return;
  for (const vEntry of readdirSync(aDir, { recursive: true, encoding: 'utf8' }).map(String).sort().reverse()) {
    try { rmSync(join(aDir, vEntry), { recursive: true, force: true }); } catch { /* best effort */ }
  }
}

/** Rewrites the Fixture project: a phase-2 checklist and (when given) its run records; nothing else is left over. */
function fixture(aRuns: Rec[] | null, aRows = ['REQ-FN-301', 'REQ-FN-302', 'REQ-FN-303'], aVoid: number[] = []) {
  clean(PROJECT);
  put(join(PROJECT, 'docs', `${NAME}-P2-Checklist.md`), checklist(aRows));
  put(join(PROJECT, 'docs', `${NAME}-P2-BRD.md`), '# brd\n');
  if (aRuns) put(join(PROJECT, 'docs', 'metrics', 'runs.jsonl'), runLines(aRuns, aVoid));
}

/** A sister project S1 holding the given run records. */
function sister(aRuns: Rec[]): string {
  const vDir = join(SISTER_ROOT, 'S1');
  clean(vDir);
  put(join(vDir, 'docs', 'metrics', 'runs.jsonl'), runLines(aRuns));
  return vDir;
}

const estimatesPath = () => join(PROJECT, 'docs', 'metrics', 'estimates.jsonl');
const estimateRecords = (): Record<string, any>[] =>
  existsSync(estimatesPath()) ? readFileSync(estimatesPath(), 'utf8').split('\n').filter((l) => l.trim()).map((l) => JSON.parse(l)) : [];
const runCount = () => Number(query('SELECT COUNT(*) AS n FROM ProcessRun WHERE ProjectId=(SELECT ProjectId FROM Project WHERE Name=?)', NAME)[0].n);

// ---- database: a connected tier-1 opus model that no real service stands behind ------------------------------------------

function seedRouting(aBaseUrl: string) {
  saved = snapshot(['Provider', 'Model']);
  run("UPDATE Provider SET State='NotTestedYet' WHERE State='Connected'");
  const vProvider = run("INSERT INTO Provider (Name, Connector, SignInMethod, BaseUrl, SecretName, State) VALUES (?,?,?,?,?,?)",
    PROVIDER, 'OpenAICompatible', 'key', aBaseUrl, 'Chatur:EstimateFixture:Key', 'Connected');
  run('INSERT INTO Model (ProviderId, Tier, Identifier) VALUES (?,?,?)', vProvider, 1, OPUS);
}

const COUNTING_URL = () => `http://127.0.0.1:${COUNT_PORT}/v1`;
const CLOSED_URL = 'http://127.0.0.1:9/v1';

async function openProcesses() {
  await openStepProject(page, NAME);
  await openProcessesTab(page);
}

/** Presses Start on a process and waits for the estimate panel naming it. */
async function pressStart(aCode: string) {
  // Known app defect (see the test "Start can be pressed again after Cancel"): after a Cancel the Start buttons stay
  // disabled until Reload re-reads the list, so a test that presses Start twice reloads in between, as an owner would.
  if (await page.getByTestId(`start-${aCode}`).isDisabled().catch(() => false)) {
    await text('reload-processes').click();
    await expect(page.getByTestId(`start-${aCode}`)).toBeEnabled({ timeout: 15_000 });
  }
  await expect(async () => {
    if (!(await page.getByTestId('estimate-panel').isVisible())) await page.getByTestId(`start-${aCode}`).click({ timeout: 2_500 });
    await expect(page.getByTestId('estimate-panel')).toContainText(`Before ${aCode} starts`, { timeout: 3_000 });
  }).toPass({ timeout: 30_000 });
}

const text = (aId: string) => page.getByTestId(aId);

async function openPerRow() {
  const vToggle = text('estimate-per-row-toggle');
  if (!(await text('estimate-row-table').isVisible().catch(() => false))) await vToggle.click();
  await expect(text('estimate-row-table')).toBeVisible();
}

/** Confirms the open estimate and waits for the run it starts. */
async function confirm(aCode: string): Promise<number> {
  await text('estimate-confirm').click();
  const vRunTab = tab(page, new RegExp(`^${aCode} · run \\d+$`));
  await expect(vRunTab.first()).toBeVisible({ timeout: 30_000 });
  return Number(/run (\d+)$/.exec((await vRunTab.first().innerText()).trim())![1]);
}

async function addSisterOnSettings(aFolder: string) {
  await page.goto('/settings/measurements', { waitUntil: 'networkidle' });
  await expect(text('sister-panel')).toBeVisible({ timeout: 30_000 });
  await text('sister-path').locator('input').or(text('sister-path')).first().fill(aFolder);
  await text('sister-add').click();
  await expect(text('sister-panel')).toContainText('S1', { timeout: 20_000 });
  await expect(text('sister-panel')).toContainText(/\d+ runs?/);
}

test.beforeAll(async ({ browser }) => {
  counter = createServer((aReq, aRes) => {
    modelRequests++;
    aRes.writeHead(404, { 'content-type': 'application/json' });
    aRes.end(JSON.stringify({ error: { message: 'estimate fixture: no model here' } }));
  });
  await new Promise<void>((ok, bad) => { counter.once('error', bad); counter.listen(COUNT_PORT, '127.0.0.1', () => ok()); });
  page = await browser.newPage({ baseURL: process.env.BASE_URL });
  await signIn(page, 'Owner');
});
test.afterAll(async () => {
  await page.close();
  await new Promise<void>((ok) => counter.close(() => ok()));
  // The project this file made goes, with its folder and its rows (owner, 20 Nov 2026): settings-estimate's
  // project carried the same name "Fixture" once, and two of them in Start's list broke every click on the name.
  removeProjectsUnder(PROJECT);
});
test.beforeEach(() => {
  test.setTimeout(180_000);
  modelRequests = 0;
  run('DELETE FROM SisterProject');
  seedRouting(COUNTING_URL());
});
test.afterEach(async () => {
  // A run a test confirmed ends on its own (the fixture provider has no model); wait for it so it does not write into the
  // next test's fixture, then make sure none is left running. The providers and models go back as they were.
  await expect.poll(() => Number(query("SELECT COUNT(*) AS n FROM ProcessRun WHERE State IN ('running','waiting') AND ProjectId=(SELECT ProjectId FROM Project WHERE Name=?)", NAME)[0].n), { timeout: 60_000 }).toBe(0).catch(() => undefined);
  run("UPDATE ProcessRun SET State='stopped', EndedUtc=datetime('now') WHERE State IN ('running','waiting') AND ProjectId=(SELECT ProjectId FROM Project WHERE Name=?)", NAME);
  run('DELETE FROM SisterProject');
  if (saved) restore(saved);
  saved = undefined;
});

// ---- tests ---------------------------------------------------------------------------------------------------------

test('REQ-UI-083 REQ-UI-089 Start on build-phase, verify-phase and fix-issues shows every row and the total and runs nothing until confirmed', async () => {
  fixture(f1());
  await openProcesses();
  const vBefore = runCount();

  for (const vCode of ['build-phase', 'verify-phase', 'fix-issues']) {
    await pressStart(vCode);
    await expect(text('estimate-table')).toBeVisible();
    await expect(text('estimate-rows')).toHaveText('3 rows · phase 2');
    // The four lines of work, the phase total on each, and the per-row table with all three rows.
    for (const vLine of ['build', 'qa', 'triage', 'owner']) await expect(text(`estimate-row-${vLine}`)).toBeVisible();
    await expect(text('estimate-tokens-build')).toHaveText('6,000 · 9,000 · 12,000');
    if (vCode === 'build-phase') {
      await text('estimate-panel').evaluate((e) => e.scrollIntoView({ block: 'start' }));
      await page.screenshot({ path: SHOT });
    }
    await openPerRow();
    for (const vN of [1, 2, 3]) await expect(text(`estimate-req-${vN}`)).toContainText(`REQ-FN-30${vN}`);
    // The owner's own testing is never a number.
    await expect(text('estimate-tokens-owner')).toHaveText('not measured');
    await expect(text('estimate-time-owner')).toHaveText('not measured');
    // Nothing runs, nothing is written, until the owner confirms.
    expect(runCount()).toBe(vBefore);
    expect(existsSync(estimatesPath())).toBe(false);
    await expect(tab(page, new RegExp(`^${vCode} · run \\d+$`))).toHaveCount(0);
    await text('estimate-cancel').click();
    await expect(text('estimate-panel')).toHaveCount(0);
    expect(runCount()).toBe(vBefore);
    expect(existsSync(estimatesPath())).toBe(false);
  }

  // Confirm starts the run, and keeps the estimate.
  await pressStart('build-phase');
  const vRun = await confirm('build-phase');
  expect(runCount()).toBe(vBefore + 1);
  expect(query('SELECT ProcessCode FROM ProcessRun WHERE RunId=?', vRun)[0].ProcessCode).toBe('build-phase');
  await expect.poll(() => estimateRecords().length, { timeout: 20_000 }).toBeGreaterThan(0);
});

test('REQ-FN-107 REQ-UI-089 fixture F1 reads 2,000/3,000/4,000 tokens and 240/300/360 s per row over 5 runs, total 6,000/9,000/12,000', async () => {
  fixture(f1());
  await openProcesses();
  await pressStart('build-phase');
  await expect(text('estimate-tokens-build')).toHaveText('6,000 · 9,000 · 12,000');
  await expect(text('estimate-time-build')).toHaveText('12 m · 15 m · 18 m'); // 720 / 900 / 1,080 s
  await expect(text('estimate-basis-build')).toContainText('this project · 5 runs');
  await openPerRow();
  for (const vN of [1, 2, 3]) {
    const vRow = text(`estimate-req-${vN}`);
    await expect(vRow).toContainText('2,000 · 3,000 · 4,000');
    await expect(vRow).toContainText('4 m · 5 m · 6 m'); // 240 / 300 / 360 s, written in minutes
  }
  // The seconds themselves, as kept on a confirmed start.
  await confirm('build-phase');
  await expect.poll(() => estimateRecords().length, { timeout: 20_000 }).toBeGreaterThan(0);
  const vRows = estimateRecords().filter((r) => r.cmd === 'build-phase' && r.req_id);
  expect(vRows.map((r) => r.req_id)).toEqual(['REQ-FN-301', 'REQ-FN-302', 'REQ-FN-303']);
  for (const r of vRows) {
    expect(r.tokens_out).toEqual({ low: 2000, mid: 3000, high: 4000 });
    expect(r.duration_s).toEqual({ low: 240, mid: 300, high: 360 });
    expect(r.basis).toBe('repo');
    expect(r.basis_runs).toBe(5);
  }
  const vTotal = estimateRecords().find((r) => r.cmd === 'build-phase' && r.req_id === null)!;
  expect(vTotal.tokens_out).toEqual({ low: 6000, mid: 9000, high: 12000 });
  expect(vTotal.duration_s).toEqual({ low: 720, mid: 900, high: 1080 });
});

test('REQ-FN-108 fixture F2 reads 1,750/2,500/3,750 tokens and 175/250/350 s, basis family over 4 runs, naming S1', async () => {
  // Two REQ-UI runs here (1,000 tokens/100 s per row; 3,000/300) and two in sister project S1 (2,000/200; 6,000/500).
  fixture([b(['REQ-UI-201'], 1000, 100), b(['REQ-UI-202'], 3000, 300)], ['REQ-UI-301']);
  const vS1 = sister([b(['REQ-UI-801'], 2000, 200), b(['REQ-UI-802'], 6000, 500)]);
  await addSisterOnSettings(vS1);
  await openProcesses();
  await pressStart('build-phase');
  await expect(text('estimate-tokens-build')).toHaveText('1,750 · 2,500 · 3,750');
  await expect(text('estimate-time-build')).toHaveText('3 m · 4 m · 6 m'); // 175 / 250 / 350 s
  await expect(text('estimate-basis-build')).toContainText('from sister projects S1 · 4 runs');
  await openPerRow();
  await expect(text('estimate-req-1')).toContainText('1,750 · 2,500 · 3,750');
  await confirm('build-phase');
  await expect.poll(() => estimateRecords().length, { timeout: 20_000 }).toBeGreaterThan(0);
  const vRow = estimateRecords().find((r) => r.cmd === 'build-phase' && r.req_id === 'REQ-UI-301')!;
  expect(vRow.tokens_out).toEqual({ low: 1750, mid: 2500, high: 3750 });
  expect(vRow.duration_s).toEqual({ low: 175, mid: 250, high: 350 });
  expect(vRow.basis).toBe('family');
  expect(vRow.basis_runs).toBe(4);
});

test('REQ-FN-109 fixture F3 after handoff reads no precedent, basis none over 2 runs, with no figure shown or written', async () => {
  // One triage-issues run here on a REQ-FN row, one more in S1. (Build runs of F1 are added so the panel has a figure
  // somewhere: with none at all the panel reads "No estimate yet" instead of its table, which REQ-UI-090 proves.)
  fixture([...f1(), { cmd: 'triage-issues', reqs: ['REQ-FN-201'], tokens: 1000, seconds: 100 }]);
  const vS1 = sister([{ cmd: 'triage-issues', reqs: ['REQ-FN-801'], tokens: 2000, seconds: 200 }]);
  await addSisterOnSettings(vS1);
  await openProcesses();
  await pressStart('build-phase');
  await expect(text('estimate-tokens-triage')).toHaveText('no precedent');
  await expect(text('estimate-time-triage')).toHaveText('no precedent');
  await expect(text('estimate-basis-triage')).toContainText('fewer than three, so no figure');
  await expect(text('estimate-row-triage')).not.toContainText(/\d[\d,]* · \d/);
  await confirm('build-phase');
  await expect.poll(() => estimateRecords().length, { timeout: 20_000 }).toBeGreaterThan(0);
  const vTriage = estimateRecords().filter((r) => r.cmd === 'triage');
  expect(vTriage.length).toBeGreaterThan(0);
  for (const r of vTriage) {
    expect(r.tokens_out).toBeNull();
    expect(r.duration_s).toBeNull();
    expect(r.basis).toBe('none');
  }
  // Each row rests on the 2 runs found (here and in S1).
  for (const r of vTriage.filter((x) => x.req_id)) expect(r.basis_runs).toBe(2);
});

test('REQ-FN-109 the screen names the 2 runs behind no precedent', async () => {
  fixture([...f1(), { cmd: 'triage-issues', reqs: ['REQ-FN-201'], tokens: 1000, seconds: 100 }]);
  const vS1 = sister([{ cmd: 'triage-issues', reqs: ['REQ-FN-801'], tokens: 2000, seconds: 200 }]);
  await addSisterOnSettings(vS1);
  await openProcesses();
  await pressStart('build-phase');
  await expect(text('estimate-tokens-triage')).toHaveText('no precedent');
  await expect(text('estimate-basis-triage')).toContainText('2 runs');
});

test('REQ-UI-083 Start can be pressed again after Cancel without reloading', async () => {
  fixture(f1());
  await openProcesses();
  await pressStart('build-phase');
  await text('estimate-cancel').click();
  await expect(text('estimate-panel')).toHaveCount(0);
  await expect(page.getByTestId('start-verify-phase')).toBeEnabled({ timeout: 10_000 });
  await page.getByTestId('start-verify-phase').click({ timeout: 5_000 });
  await expect(text('estimate-panel')).toContainText('Before verify-phase starts');
});

test('REQ-FN-110 fixture F1 estimated twice with no model provider reachable gives the same figures to the last digit and asks no model', async () => {
  // The only connected provider is the fixture's, at a closed port: nothing a model could answer from.
  run("DELETE FROM Model WHERE Identifier=?", OPUS);
  run("DELETE FROM Provider WHERE Name=?", PROVIDER);
  run("UPDATE Provider SET State='NotTestedYet'");
  const vProvider = run("INSERT INTO Provider (Name, Connector, SignInMethod, BaseUrl, SecretName, State) VALUES (?,?,?,?,?,?)",
    PROVIDER, 'OpenAICompatible', 'key', CLOSED_URL, 'Chatur:EstimateFixture:Key', 'Connected');
  run('INSERT INTO Model (ProviderId, Tier, Identifier) VALUES (?,?,?)', vProvider, 1, OPUS);
  fixture(f1());
  await openProcesses();
  const vLogBefore = existsSync(APP_LOG) ? readFileSync(APP_LOG, 'utf8').length : 0;

  const vRead = async () => {
    await pressStart('build-phase');
    await openPerRow();
    const vPanel = (await text('estimate-table').innerText()) + '\n' + (await text('estimate-row-table').innerText());
    await text('estimate-cancel').click();
    await expect(text('estimate-panel')).toHaveCount(0);
    return vPanel;
  };
  const vFirst = await vRead();
  const vSecond = await vRead();
  expect(vFirst).toContain('6,000 · 9,000 · 12,000');
  expect(vSecond).toBe(vFirst);
  expect(modelRequests).toBe(0);
  const vLog = existsSync(APP_LOG) ? readFileSync(APP_LOG, 'utf8').slice(vLogBefore) : '';
  expect(vLog).not.toMatch(/chat\/completions|127\.0\.0\.1:9\b/);
  expect(existsSync(estimatesPath())).toBe(false);
});

test('REQ-FN-111 fixture F5 with build on tier 1 reads 3,000/4,000/4,500 tokens and 330/360/405 s over 3 runs, naming claude-opus-5-5 and tier 1', async () => {
  // F1 with runs B and D on claude-sonnet-5-5: three runs (A, C, E) are on tier 1's claude-opus-5-5.
  // Seeding: the fixture provider's tier-1 model is claude-opus-5-5 (a Model row inserted through the database).
  fixture(f1([undefined, SONNET, undefined, SONNET, undefined]));
  await openProcesses();
  await pressStart('build-phase');
  await expect(text('estimate-model')).toHaveText(`${OPUS} · tier 1`);
  await expect(text('estimate-tokens-build')).toHaveText('9,000 · 12,000 · 13,500'); // three rows
  await openPerRow();
  await expect(text('estimate-req-1')).toContainText('3,000 · 4,000 · 4,500');
  await expect(text('estimate-req-1')).toContainText('6 m · 6 m · 7 m'); // 330 / 360 / 405 s
  await expect(text('estimate-basis-build')).toContainText('this project · 3 runs');
  await expect(text('estimate-basis-build')).not.toContainText('all models');
  await confirm('build-phase');
  await expect.poll(() => estimateRecords().length, { timeout: 20_000 }).toBeGreaterThan(0);
  const vRow = estimateRecords().find((r) => r.cmd === 'build-phase' && r.req_id === 'REQ-FN-301')!;
  expect(vRow.tokens_out).toEqual({ low: 3000, mid: 4000, high: 4500 });
  expect(vRow.duration_s).toEqual({ low: 330, mid: 360, high: 405 });
  expect(vRow).toMatchObject({ basis: 'repo', basis_runs: 3, model: OPUS, tier: 1 });
});

test('REQ-FN-114 a confirmed start for fixture F1 keeps REQ-FN-301 build record equal to the Metrics Schema example, ts aside', async () => {
  fixture(f1());
  await openProcesses();
  await pressStart('build-phase');
  await confirm('build-phase');
  await expect.poll(() => estimateRecords().length, { timeout: 20_000 }).toBeGreaterThan(0);
  const vRecord = estimateRecords().find((r) => r.cmd === 'build-phase' && r.req_id === 'REQ-FN-301')!;
  expect(vRecord).toBeDefined();
  expect(typeof vRecord.ts).toBe('string');
  const { ts: _ts, ...vRest } = vRecord;
  expect(vRest).toEqual({
    v: 1, kind: 'estimate', app: 'Fixture', phase: 2, req_id: 'REQ-FN-301', cmd: 'build-phase',
    tokens_out: { low: 2000, mid: 3000, high: 4000 }, duration_s: { low: 240, mid: 300, high: 360 },
    basis: 'repo', basis_runs: 5, model: OPUS, tier: 1, harness: 'chatur',
  });
  // One batch: every record of it carries the same ts.
  expect(new Set(estimateRecords().map((r) => r.ts)).size).toBe(1);
  // Never written to the run records.
  expect(readFileSync(join(PROJECT, 'docs', 'metrics', 'runs.jsonl'), 'utf8')).not.toContain('"kind":"estimate"');
});

test('REQ-UI-090 a project with no usable run anywhere reads No estimate yet, Start works and no error is logged', async () => {
  fixture(null);
  await openProcesses();
  const vLogBefore = existsSync(APP_LOG) ? readFileSync(APP_LOG, 'utf8').length : 0;
  await pressStart('build-phase');
  await expect(text('estimate-none-yet')).toContainText('No estimate yet');
  await expect(text('estimate-table')).toHaveCount(0);
  await expect(page.getByText('Could not work out the estimate')).toHaveCount(0);
  await expect(text('estimate-confirm')).toBeEnabled();
  const vBefore = runCount();
  const vRun = await confirm('build-phase');
  expect(runCount()).toBe(vBefore + 1);
  expect(Number(query('SELECT COUNT(*) AS n FROM ProcessRun WHERE RunId=?', vRun)[0].n)).toBe(1);
  await expect(page.getByText(/Could not work out the estimate|Could not start the process/)).toHaveCount(0);
  const vLog = existsSync(APP_LOG) ? readFileSync(APP_LOG, 'utf8').slice(vLogBefore) : '';
  const vErrors = vLog.split('\n').filter((l) => /\[ERR\]|\[FTL\]/.test(l) && /estimat/i.test(l));
  expect(vErrors).toEqual([]);
});

test('REQ-FN-116 fixture F4 reads the same figures as F1 over the same 5 runs and its batch carries basis_note retro', async () => {
  // F1, plus a run naming no row, one with no measured tokens, one backfilled, one voided and one that touched REQ-FN-301.
  const vRuns = [
    ...f1(),
    b([], 5000, 500),
    b(['REQ-FN-220'], null, 500),
    b(['REQ-FN-221'], 5000, 500, undefined, true),
    b(['REQ-FN-222'], 5000, 500),
    b(['REQ-FN-301'], 5000, 500),
  ];
  fixture(vRuns, undefined, [8]);
  await openProcesses();
  await pressStart('build-phase');
  await expect(text('estimate-retro')).toHaveText('estimated after the fact');
  await expect(text('estimate-tokens-build')).toHaveText('6,000 · 9,000 · 12,000');
  await expect(text('estimate-time-build')).toHaveText('12 m · 15 m · 18 m');
  await expect(text('estimate-basis-build')).toContainText('this project · 5 runs');
  await openPerRow();
  await expect(text('estimate-req-1')).toContainText('2,000 · 3,000 · 4,000');
  await confirm('build-phase');
  await expect.poll(() => estimateRecords().length, { timeout: 20_000 }).toBeGreaterThan(0);
  const vAll = estimateRecords();
  expect(vAll.length).toBeGreaterThan(0);
  for (const r of vAll) expect(r.basis_note).toBe('retro');
  const vRow = vAll.find((r) => r.cmd === 'build-phase' && r.req_id === 'REQ-FN-301')!;
  expect(vRow.tokens_out).toEqual({ low: 2000, mid: 3000, high: 4000 });
  expect(vRow.duration_s).toEqual({ low: 240, mid: 300, high: 360 });
  expect(vRow.basis_runs).toBe(5);
});
