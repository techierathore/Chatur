// Acceptance tests: Run report (REQ-UI-058..061). Black-box, web harness.
// Four tests read hand-written metrics files; the last one (REQ-UI-058 real run) reads a run Chatur really carried out.
// A throwaway project folder holding a checklist and three measurement streams with two runs is built under
// tests/.artifacts/verify/fixtures/ and opened as the project through the app's own Start flow.
import { test, expect, Page } from '@playwright/test';
import { mkdirSync, rmSync, writeFileSync } from 'fs';
import * as path from 'path';
import { signIn } from './support/auth';
import { openFoldersDialog } from './support/start';
import { tab } from './support/phase2-tabs';
import { query, run } from './support/db';
import { ROOT as WB_ROOT, write, openProject } from './support/workbench-a';

test.describe.configure({ mode: 'serial' });
test.setTimeout(120_000);

const ROOT = path.resolve('tests/.artifacts/verify/fixtures/run-report-spec/root');
const PROJECT = path.join(ROOT, 'reportspec-proj');
const DOCS = path.join(PROJECT, 'docs');
const METRICS = path.join(DOCS, 'metrics');
const SHOTS = 'tests/.artifacts/verify';

const NEWER = '2026-09-22T02:00:00Z';
const OLDER = '2026-09-20T09:05:00Z';

const lines = (aRecords: object[]): string => aRecords.map((r) => JSON.stringify(r)).join('\n') + '\n';

function buildFixture(): void {
  rmSync(ROOT, { recursive: true, force: true });
  mkdirSync(METRICS, { recursive: true });
  writeFileSync(path.join(DOCS, 'ReportSpec-Checklist.md'),
    '# ReportSpec — Checklist\n\n## Requirements Status\n\n| ID | Requirement | Status | % | Remarks | Details |\n|----|-------------|--------|---|---------|---------|\n' +
    '| REQ-UI-014 | Coverage panel file list | Verified | 100% | ok | [view](#d-req-ui-014) |\n' +
    '| REQ-FN-021 | The filter keeps its choice | Implemented | 75% | — | [view](#d-req-fn-021) |\n' +
    '| REQ-NFR-004 | The panel opens within 400 ms | Blocked | 25% | — | [view](#d-req-nfr-004) |\n' +
    '| REQ-UI-001 | Older requirement | Verified | 100% | — | [view](#d-req-ui-001) |\n\n' +
    '## Page: Coverage (`/coverage`)\n\n- **REQ-UI-014** — Coverage panel file list. *BRD:* BRD-1\n  - *Acceptance:* The panel lists files.\n');
  writeFileSync(path.join(METRICS, 'runs.jsonl'), lines([
    { kind: 'run', cmd: 'auto', started: OLDER, ended: '2026-09-20T10:05:00Z', reqs_touched: ['REQ-UI-001'], model: 'old-model', tokens_in: 100, tokens_out: 50, duration_s: 3600 },
    { kind: 'run', cmd: 'auto', started: NEWER, ended: '2026-09-22T05:14:00Z', reqs_touched: ['REQ-UI-014', 'REQ-FN-021', 'REQ-NFR-004'], model: 'big-model', tier: 'deep', model_tokens_out: { 'big-model': 900, 'small-model': 100 }, tokens_in: 1000000, tokens_out: 240000, duration_s: 11640, waited_s: 1320 },
  ]));
  writeFileSync(path.join(METRICS, 'gates.jsonl'), lines([
    { kind: 'gate', run_id: NEWER, req_id: 'REQ-UI-014', verdict: 'Verified', ts: '2026-09-22T02:41:00Z' },
    { kind: 'gate', run_id: NEWER, req_id: 'REQ-FN-021', verdict: 'Needs re-verify', ts: '2026-09-22T03:58:00Z' },
    { kind: 'gate', run_id: NEWER, req_id: 'REQ-NFR-004', verdict: 'Blocked', ts: '2026-09-22T04:52:00Z' },
    { kind: 'gate', run_id: OLDER, req_id: 'REQ-UI-001', verdict: 'Verified', ts: '2026-09-20T09:30:00Z' },
  ]));
  writeFileSync(path.join(METRICS, 'misses.jsonl'), lines([
    { kind: 'miss', miss_id: 'M1', req_id: 'REQ-FN-021', miss_class: 'unspecified-gap', artifact: 'brd', what: 'The brief never said which projects the filter spans', found_run_id: NEWER },
    { kind: 'miss', miss_id: 'M2', req_id: 'REQ-UI-017', miss_class: 'missed-requirement', artifact: 'src', found_by: 'library-feedback', req_class: 'UI', what: 'The chip control has no disabled state', found_run_id: NEWER },
    { kind: 'miss', miss_id: 'M3', req_id: 'REQ-FN-022', miss_class: 'wrong-behaviour', artifact: 'src', what: 'The run retried step 4 twice', found_run_id: NEWER },
    { kind: 'miss-fix', miss_id: 'M3', verdict_after: 'Verified' },
  ]));
}

async function selectFixtureProject(aPage: Page): Promise<void> {
  await aPage.goto('/', { waitUntil: 'networkidle' });
  const vName = aPage.getByTestId('project-switch-name');
  if (await vName.waitFor({ timeout: 10_000 }).then(() => true, () => false)) {
    if ((await vName.innerText()).trim() === 'reportspec-proj') return;
  }
  await aPage.goto('/start?all=1', { waitUntil: 'networkidle' });
  const vDialog = await openFoldersDialog(aPage);
  if ((await vDialog.getByText(ROOT, { exact: true }).count()) === 0) {
    await vDialog.getByTestId('new-folder-path').fill(ROOT);
    await vDialog.getByTestId('add-folder').click();
    await expect(vDialog.getByText(ROOT, { exact: true })).toBeVisible();
  }
  await vDialog.getByTestId('folders-dialog-close').click();
  await expect(vDialog).toBeHidden();
  const vRow = aPage.getByTestId(/^project-\d+-name$/).filter({ hasText: /^reportspec-proj$/ });
  await expect(vRow).toBeVisible();
  await vRow.click();
  await expect(aPage).not.toHaveURL(/\/start/);
}

async function openReport(aPage: Page): Promise<void> {
  await selectFixtureProject(aPage);
  await expect(async () => {
    if (!(await aPage.getByTestId('menu-project-report').isVisible())) await aPage.getByTestId('menu-project').click();
    await aPage.getByTestId('menu-project-report').click({ timeout: 2_500 });
    await expect(tab(aPage, 'Run record').first()).toBeVisible({ timeout: 3_000 });
  }).toPass({ timeout: 30_000 });
  await expect(aPage.getByTestId('requirements-table')).toBeVisible({ timeout: 30_000 });
  await expect(aPage.getByTestId('chat-panel')).toBeVisible();
}

test.beforeAll(() => buildFixture());

test.beforeEach(async ({ page }) => {
  await expect(async () => {
    await signIn(page, 'Owner');
  }).toPass({ timeout: 120_000, intervals: [500] });
});

test('REQ-UI-058 the report lists every requirement the run touched with the verdict it was left at', async ({ page }) => {
  await openReport(page);
  await expect(page.getByTestId('tab-run-report')).toBeVisible();
  await expect(page.getByTestId('tile-touched-value')).toHaveText('3');
  await expect(page.getByTestId('tile-verified-value')).toHaveText('1');
  await expect(page.getByTestId('req-row-1')).toContainText('REQ-UI-014');
  await expect(page.getByTestId('req-row-1')).toContainText('Coverage panel file list');
  await expect(page.getByTestId('verdict-1')).toContainText('Verified');
  await expect(page.getByTestId('req-row-2')).toContainText('REQ-FN-021');
  await expect(page.getByTestId('verdict-2')).toContainText('Partial');
  await expect(page.getByTestId('req-row-3')).toContainText('REQ-NFR-004');
  await expect(page.getByTestId('verdict-3')).toContainText('Blocked');
  await expect(page.getByTestId('req-row-3')).toContainText('Verifier');
  await expect(page.getByTestId('verdict-counts')).toContainText('1 Verified');
  await page.setViewportSize({ width: 1500, height: 2400 });
  await page.screenshot({ path: `${SHOTS}/run-report.png`, fullPage: true });
});

test('REQ-UI-059 the report lists each miss with what it was and whose gap it was', async ({ page }) => {
  await openReport(page);
  await expect(page.getByTestId('tile-misses-value')).toHaveText('3');
  await expect(page.getByTestId('miss-row-1')).toContainText('The brief never said which projects the filter spans');
  await expect(page.getByTestId('miss-owner-1')).toHaveText("the owner's brief");
  await expect(page.getByTestId('miss-fixed-1')).toContainText('no');
  await expect(page.getByTestId('miss-owner-2')).toHaveText('TrBlazeUI');
  await expect(page.getByTestId('miss-row-2')).toContainText('REQ-UI-017');
  await expect(page.getByTestId('miss-owner-3')).toHaveText('Chatur');
  await expect(page.getByTestId('miss-fixed-3')).toContainText('yes');
});

test('REQ-UI-060 the report shows the tokens, the models used and the time the run took', async ({ page }) => {
  await openReport(page);
  await expect(page.getByTestId('tile-tokens-value')).toHaveText('1.24 M');
  await expect(page.getByTestId('tile-time-value')).toHaveText('3 h 14 m');
  await expect(page.getByTestId('cost-row-1')).toContainText('big-model');
  await expect(page.getByTestId('cost-row-1')).toContainText('deep');
  await expect(page.getByTestId('cost-row-2')).toContainText('small-model');
  await expect(page.getByTestId('cost-row-total')).toContainText('2 models');
  await expect(page.getByTestId('cost-row-total')).toContainText('3 h 14 m');
  await expect(page.getByTestId('waiting-note')).toContainText('22 minutes');
});

test('REQ-UI-061 picking an earlier run shows that run instead of the newest', async ({ page }) => {
  await openReport(page);
  await expect(page.getByTestId('req-row-1')).toContainText('REQ-UI-014');
  await page.getByTestId('run-select-button').click();
  const vPop = page.getByTestId('run-select-pop');
  await expect(vPop).toBeVisible();
  await vPop.getByText(/20 Sep 09:05/).click();
  await expect(page.getByTestId('req-row-1')).toContainText('REQ-UI-001');
  await expect(page.getByTestId('tile-touched-value')).toHaveText('1');
  await expect(page.getByTestId('tile-tokens-value')).toHaveText('150');
  await expect(page.getByTestId('tile-misses-value')).toHaveText('0');
  await expect(page.getByTestId('waiting-note')).toHaveCount(0);
  await page.screenshot({ path: `${SHOTS}/run-report-earlier.png`, fullPage: true });
});

// ---- A real end-to-end run -------------------------------------------------------------------------------------
// The chain of a private tiny .NET project is really run through the process engine (as auto-run-engine.spec.ts
// does; no model is called). What a verifier writes while it works is then added the way it would be: verdict rows
// on the verify-phase process run, and the project's own measurement files (tokens and misses stamped inside the
// run's window). The page must agree with itself: every tile with the table it totals.

const REAL = 'ReportRealApp';
const REAL_DIR = path.join(WB_ROOT, REAL);

function buildRealFixture(): void {
  write(path.join(REAL_DIR, 'Directory.Build.props'), '<Project />\n');
  write(path.join(REAL_DIR, 'nuget.config'), '<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear /></packageSources></configuration>\n');
  write(path.join(REAL_DIR, `${REAL}.csproj`),
    '<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n    <OutputType>Exe</OutputType>\n    <TargetFramework>net10.0</TargetFramework>\n    <ImplicitUsings>enable</ImplicitUsings>\n    <Nullable>enable</Nullable>\n  </PropertyGroup>\n</Project>\n');
  write(path.join(REAL_DIR, 'Program.cs'), 'Console.WriteLine("report fixture");\n');
  write(path.join(REAL_DIR, 'docs', `${REAL}-Checklist.md`),
    '# ReportRealApp - Checklist (phase 1)\n\n## Requirements Status\n\n| ID | Requirement | Status | % | Remarks | Details |\n|----|---|---|---|---|---|\n'
    + '| REQ-FN-001 | Not yet begun | Not Started | 0% | - | [view](#d-req-fn-001) |\n'
    + '| REQ-FN-002 | Sent back | Blocked | 25% | - | [view](#d-req-fn-002) |\n'
    + '| REQ-FN-003 | Proven | Verified | 100% | - | [view](#d-req-fn-003) |\n\n'
    + '## Page: Home (`/`)\n\n<a id="d-req-fn-001"></a>\n- **REQ-FN-001** - Not yet begun. *BRD:* BRD-1\n  - *Acceptance:* When it is begun, then it works.\n');
  rmSync(path.join(REAL_DIR, 'docs', 'metrics'), { recursive: true, force: true });
}

async function openReportTab(aPage: Page): Promise<void> {
  await expect(async () => {
    if (!(await aPage.getByTestId('menu-project-report').isVisible())) await aPage.getByTestId('menu-project').click();
    await aPage.getByTestId('menu-project-report').click({ timeout: 2_500 });
    await expect(tab(aPage, 'Run record').first()).toBeVisible({ timeout: 3_000 });
  }).toPass({ timeout: 30_000 });
  await expect(aPage.getByTestId('requirements-table')).toBeVisible({ timeout: 30_000 });
}

const figure = (aText: string): number => Number(aText.replace(/[^0-9]/g, ''));

test('REQ-UI-058 REQ-UI-059 REQ-UI-060 the report of a run Chatur really carried out agrees with itself, and says what it was not given', async ({ page }) => {
  test.setTimeout(420_000);
  buildRealFixture();
  await openProject(page, REAL);
  const vProject = Number(query('SELECT ProjectId FROM Project WHERE Name = ?', REAL)[0].ProjectId);
  run('DELETE FROM ProcessRunVerdict WHERE RunId IN (SELECT RunId FROM ProcessRun WHERE ProjectId = ?)', vProject);
  run('DELETE FROM AutoRunEvent WHERE AutoRunId IN (SELECT AutoRunId FROM AutoRun WHERE ProjectId = ?)', vProject);
  run('DELETE FROM AutoRunProcess WHERE AutoRunId IN (SELECT AutoRunId FROM AutoRun WHERE ProjectId = ?)', vProject);
  run('DELETE FROM AutoRun WHERE ProjectId = ?', vProject);
  await page.reload({ waitUntil: 'domcontentloaded' });
  await expect(page.getByTestId('project-switch-name')).toHaveText(REAL, { timeout: 30_000 });

  // Start the run from its screen and let it end on its own. Since steps are handed to their agents
  // (owner decision 2026-10-08), the chain may stop at a process its agent cannot finish; a stopped run
  // has ended too, which is all REQ-UI-058 asks of a report.
  await expect(async () => {
    if (!(await page.getByTestId('menu-project-auto').isVisible())) await page.getByTestId('menu-project').click();
    await page.getByTestId('menu-project-auto').click({ timeout: 2_500 });
    await expect(page.getByTestId('auto-run')).toBeVisible({ timeout: 3_000 });
  }).toPass({ timeout: 30_000 });
  await page.getByTestId('brief-text').fill('Begin the not-yet-begun requirement of the fixture and verify it before you stop, tagging every REQ id.');
  await expect(async () => {
    if ((await page.getByTestId('running-now').count()) === 0) await page.getByTestId('start-run').click({ timeout: 2_500 });
    await expect(page.getByTestId('running-now')).toBeVisible({ timeout: 4_000 });
  }).toPass({ timeout: 60_000 });
  await expect(page.getByTestId('run-state')).toHaveText(/ready for your acceptance test|stopped/, { timeout: 360_000 });

  // 1. Nothing but the run itself: it is listed and closed, takes time, and what it was not given reads "not recorded".
  await openReportTab(page);
  await expect(page.getByTestId('report-subtitle')).toContainText(REAL);
  await expect(page.getByTestId('report-subtitle')).toContainText(/finished|stopped/);
  await expect(page.getByTestId('tile-touched-value')).toHaveText('0');
  await expect(page.getByTestId('tile-tokens-value')).toHaveText('not recorded');
  await expect(page.getByTestId('tile-time-value')).not.toHaveText('not recorded');
  await expect(page.getByTestId('requirements-empty')).toBeVisible();
  await expect(page.getByTestId('cost-empty')).toBeVisible();
  await expect(page.getByTestId('cost-row-total')).toHaveCount(0);

  // 2. What a verifier writes while it works, as it would write it.
  const vAuto = query('SELECT StartedUtc, EndedUtc FROM AutoRun WHERE ProjectId = ?', vProject)[0];
  const vStart = new Date(String(vAuto.StartedUtc)).getTime();
  const vEnd = new Date(String(vAuto.EndedUtc)).getTime();
  // Every record the test writes falls inside the run's own window (the report reads misses and costs
  // by time); the run may end within seconds when it stops, so the moments are tenths of that window.
  const vAt = (aTenths: number): string => new Date(vStart + ((vEnd - vStart) * aTenths) / 10).toISOString();
  // The verdicts go on a process run that belongs to this end-to-end run (the report reads the run's own process runs).
  const vVerify = Number(query('SELECT p.ProcessRunId AS RunId FROM AutoRunProcess p JOIN AutoRun a ON a.AutoRunId = p.AutoRunId WHERE a.ProjectId = ? AND p.ProcessRunId IS NOT NULL ORDER BY p.ProcessRunId DESC', vProject)[0].RunId);
  const vVerdicts: [string, string, number][] = [['REQ-FN-003', 'Verified', 100], ['REQ-FN-002', 'Blocked', 25], ['REQ-FN-001', 'Needs re-verify', 75]];
  for (const [vId, vStatus, vPercent] of vVerdicts) {
    run('INSERT INTO ProcessRunVerdict (RunId, RequirementId, ChecklistFile, Status, Percent, Remark, WrittenUtc) VALUES (?, ?, ?, ?, ?, ?, ?)',
      vVerify, vId, `docs/${REAL}-Checklist.md`, vStatus, vPercent, 'written by the test', vAt(3));
  }
  const vMetrics = path.join(REAL_DIR, 'docs', 'metrics');
  mkdirSync(vMetrics, { recursive: true });
  writeFileSync(path.join(vMetrics, 'runs.jsonl'), lines([
    { kind: 'run', cmd: 'build-phase', started: vAt(2), ended: vAt(6), model: 'big-model', tier: 'deep', tokens_in: 30000, tokens_out: 9000, model_tokens_out: { 'big-model': 6000, 'small-model': 3000 } },
    { kind: 'run', cmd: 'verify-phase', started: vAt(4), ended: vAt(8), model: 'big-model', tier: 'deep', tokens_in: 12000, tokens_out: 3000 },
  ]));
  writeFileSync(path.join(vMetrics, 'misses.jsonl'), lines([
    { kind: 'miss', miss_id: 'R1', req_id: 'REQ-FN-001', miss_class: 'unspecified-gap', artifact: 'brd', what: 'The brief never said what begun means', ts: vAt(5) },
    { kind: 'miss', miss_id: 'R2', req_id: 'REQ-FN-002', miss_class: 'wrong-behaviour', artifact: 'src', what: 'The sent-back requirement was not reopened', ts: vAt(5) },
  ]));

  // 3. Read again: the requirements, the misses, the tiles and the cost table agree.
  await page.getByTestId('open-run').click();
  await expect(page.getByTestId('req-row-3')).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('tile-touched-value')).toHaveText('3');
  await expect(page.getByTestId('tile-verified-value')).toHaveText('1');
  await expect(page.getByTestId('verdict-1')).toContainText('Verified');
  await expect(page.getByTestId('verdict-2')).toContainText('Blocked');
  await expect(page.getByTestId('verdict-3')).toContainText('Partial');
  await expect(page.getByTestId('verdict-counts')).toContainText('1 Verified');
  await expect(page.getByTestId('requirements-shown')).toHaveText('3 of 3 shown');
  await expect(page.getByTestId('tile-misses-value')).toHaveText('2');
  await expect(page.getByTestId('misses-table').locator('[data-testid^="miss-row-"]')).toHaveCount(2);

  const vIn = figure(await page.getByTestId('cost-row-total').locator('td').nth(3).innerText());
  const vOut = figure(await page.getByTestId('cost-row-total').locator('td').nth(4).innerText());
  expect(vIn).toBe(42000);
  expect(vOut).toBe(12000);
  await expect(page.getByTestId('tile-tokens-value')).toHaveText('54.0 k');
  expect(54_000).toBe(vIn + vOut);
  // The rows above the total add up to the total, column by column.
  const vRows = page.getByTestId('cost-table').locator('[data-testid^="cost-row-"]:not([data-testid="cost-row-total"])');
  const vCount = await vRows.count();
  expect(vCount).toBeGreaterThan(0);
  let vSumIn = 0, vSumOut = 0;
  for (let i = 0; i < vCount; i++) {
    const vCells = vRows.nth(i).locator('td');
    vSumIn += figure(await vCells.nth(3).innerText());
    vSumOut += figure(await vCells.nth(4).innerText());
  }
  expect(vSumIn).toBe(vIn);
  expect(vSumOut).toBe(vOut);
  // The time tile and the total row's time are the one figure.
  await expect(page.getByTestId('cost-row-total')).toContainText(await page.getByTestId('tile-time-value').innerText());
  await page.setViewportSize({ width: 1280, height: 2400 });
  await page.screenshot({ path: `${SHOTS}/run-report-real-run.png`, fullPage: true });
});
