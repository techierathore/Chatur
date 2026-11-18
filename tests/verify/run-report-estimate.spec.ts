// Acceptance tests: the estimate beside the actual on Run report (REQ-UI-088, REQ-FN-115, and the Run report part of
// REQ-UI-089; mockups/run-report.html "estimate-actual-panel"). Black-box, web harness.
//
// SEEDED (the UI cannot produce a finished multi-hour end-to-end run), in a private project EstRepApp under the
// Workbench fixtures root, and only in this app copy's database and that folder:
//   - docs/EstRepApp-P2-Checklist.md: rows REQ-FN-301..303 (the phase of fixture F1, docs/Chatur-P3-BRD.md);
//   - docs/metrics/runs.jsonl: the five F1 build-phase runs on REQ-FN rows outside the phase, plus three build-phase runs
//     that touched REQ-FN-301 only (the "actual"; their tokens add up to the case under test);
//   - docs/metrics/estimates.jsonl: the batch F1 gives, written in the framework's shape (build 6,000 / 9,000 / 12,000
//     tokens, 720 / 900 / 1,080 s; the other lines have no precedent);
//   - the database: one finished AutoRun with its eight AutoRunProcess / ProcessRun rows, and the RunEstimate row the app
//     keeps for it (RunKind auto, the batch's ts, the snapshot), exactly as ConfirmAsync would have left it.
import { Page, expect } from '@playwright/test';
import { mkdirSync, readFileSync, writeFileSync } from 'fs';
import { join, resolve } from 'path';
import { signIn } from './support/auth';
import { query, run } from './support/db';
import { tab, openProcessesTab } from './support/phase2-tabs';
import { test, ROOT, write, openProject } from './support/workbench-a';

const NAME = 'EstRepApp';
const DIR = join(ROOT, NAME);
const METRICS = join(DIR, 'docs', 'metrics');
const CHECKLIST = join(DIR, 'docs', `${NAME}-P2-Checklist.md`);
const STATUS = join(DIR, 'PROJECT-STATUS.md');
const SCREENS = resolve(__dirname, '../.artifacts/verify/screens');
const BATCH = '2026-11-13T08:00:00Z';
const MODEL = 'claude-opus-5-5';
const BRIEF = 'Add a notes summary to the fixture program. Follow the BRD, tag every change with its REQ id, and verify before you stop.';
const CHAIN = ['day1-greenfield', 'split-brd', 'mockups', 'deploy-checklist', 'build-phase', 'verify-phase', 'fix-issues', 'refresh-status'];

let page: Page;

test.beforeAll(async ({ browser }) => {
  page = await browser.newPage({ baseURL: process.env.BASE_URL });
  await signIn(page, 'Owner');
});
// Another Playwright run on the same artifacts folder can remove a trace file under this page; closing is best effort.
test.afterAll(async () => { await page.close().catch(() => undefined); });
test.beforeEach(() => { test.setTimeout(240_000); });

const stamp = (aMs: number): string => new Date(aMs).toISOString().replace(/\.\d{3}Z$/, 'Z');
const stampDb = (aMs: number): string => new Date(aMs).toISOString().replace('Z', '0000Z');

/** F1: A 2 rows 4,000 tokens 600 s; B 1, 3,000, 240; C 2, 10,000, 900; D 4, 4,000, 480; E 4, 16,000, 1,440; all build-phase on opus. */
function f1Lines(): string[] {
  const vRuns: [string[], number, number][] = [
    [['REQ-FN-201', 'REQ-FN-202'], 4000, 600],
    [['REQ-FN-203'], 3000, 240],
    [['REQ-FN-204', 'REQ-FN-205'], 10000, 900],
    [['REQ-FN-206', 'REQ-FN-207', 'REQ-FN-208', 'REQ-FN-209'], 4000, 480],
    [['REQ-FN-210', 'REQ-FN-211', 'REQ-FN-212', 'REQ-FN-213'], 16000, 1440],
  ];
  const vBase = Date.UTC(2026, 9, 1);
  return vRuns.map(([aReqs, aTokens, aSeconds], aIndex) => runLine(aReqs, aTokens, aSeconds, vBase + (aIndex + 1) * 3 * 3_600_000));
}

function runLine(aReqs: string[], aTokens: number, aSeconds: number, aStartMs: number): string {
  return JSON.stringify({
    v: 1, ts: stamp(aStartMs + aSeconds * 1000), kind: 'run', app: NAME, cmd: 'build-phase', mode: 'build', started: stamp(aStartMs), ended: stamp(aStartMs + aSeconds * 1000),
    duration_s: aSeconds, reqs_touched: aReqs, reqs_count: aReqs.length, model: MODEL, tokens_out: aTokens, tokens_scope: 'tree',
  });
}

/** The three runs of this end-to-end run that touched REQ-FN-301, their output tokens adding up to aTotal. */
function actualLines(aTotal: number): string[] {
  const vFirst = Math.floor(aTotal / 3);
  const vParts = [vFirst, vFirst, aTotal - 2 * vFirst];
  const vBase = Date.UTC(2026, 10, 12, 9, 0, 0);
  return vParts.map((aTokens, aIndex) => runLine(['REQ-FN-301'], aTokens, 300, vBase + aIndex * 3_600_000));
}

/** One estimates.jsonl record in the framework's shape plus Chatur's tier. */
function estimateLine(aReq: string | null, aStep: string, aTokens: number[] | null, aSeconds: number[] | null, aTotal: boolean): string {
  const vFigure = (aValues: number[] | null) => (aValues ? { low: aValues[0], mid: aValues[1], high: aValues[2] } : null);
  const vRecord: Record<string, unknown> = {
    v: 1, ts: BATCH, kind: 'estimate', app: NAME, phase: 2, req_id: aReq, cmd: aStep, tokens_out: vFigure(aTokens), duration_s: vFigure(aSeconds),
    basis: aTokens ? 'repo' : 'none', basis_runs: aTokens ? 5 : 0, model: MODEL, tier: 1, harness: 'chatur',
  };
  if (aTotal) { vRecord.rows_n = 3; vRecord.rows_no_precedent_n = aTokens ? 0 : 3; }
  return JSON.stringify(vRecord);
}

function estimateLines(): string[] {
  const vRows = ['REQ-FN-301', 'REQ-FN-302', 'REQ-FN-303'];
  const vBuildTokens = [[2000, 2000, 2000], [3000, 3000, 3000], [4000, 4000, 4000]];
  const vBuildSeconds = [[240, 240, 240], [300, 300, 300], [360, 360, 360]];
  const vLines: string[] = [];
  vRows.forEach((aRow, aIndex) => {
    vLines.push(estimateLine(aRow, 'build-phase', vBuildTokens[aIndex], vBuildSeconds[aIndex], false));
    for (const vStep of ['verify-phase', 'fix-issues', 'triage']) vLines.push(estimateLine(aRow, vStep, null, null, false));
  });
  vLines.push(estimateLine(null, 'build-phase', [6000, 9000, 12000], [720, 900, 1080], true));
  for (const vStep of ['verify-phase', 'fix-issues', 'triage']) vLines.push(estimateLine(null, vStep, null, null, true));
  return vLines;
}

/** The snapshot RunEstimate keeps (a serialized PhaseEstimate). */
function snapshot(): string {
  const vRows = ['REQ-FN-301', 'REQ-FN-302', 'REQ-FN-303'];
  const vFigure = (aLow: number, aMid: number, aHigh: number) => ({ Low: aLow, Mid: aMid, High: aHigh });
  const vRowFigures = vRows.flatMap((aRow, aIndex) => [
    { ReqId: aRow, Step: 'build-phase', TokensOut: vFigure(2000 + aIndex * 1000 + (aIndex === 2 ? 0 : 0), 2000 + aIndex * 1000, 2000 + aIndex * 1000), DurationS: vFigure(240 + aIndex * 60, 240 + aIndex * 60, 240 + aIndex * 60), Basis: 'repo', BasisRuns: 5, FromAllModels: false, SisterProjects: [] },
    ...['verify-phase', 'fix-issues', 'triage'].map((aStep) => ({ ReqId: aRow, Step: aStep, TokensOut: null, DurationS: null, Basis: 'none', BasisRuns: 0, FromAllModels: false, SisterProjects: [] })),
  ]);
  const vTotals = [
    { Step: 'build-phase', Model: MODEL, Tier: 1, TokensOut: vFigure(6000, 9000, 12000), DurationS: vFigure(720, 900, 1080), Basis: 'repo', BasisRuns: 5, RowsN: 3, RowsNoPrecedentN: 0, FromAllModels: false, SisterProjects: [] },
    ...['verify-phase', 'fix-issues', 'triage'].map((aStep) => ({ Step: aStep, Model: MODEL, Tier: 1, TokensOut: null, DurationS: null, Basis: 'none', BasisRuns: 0, RowsN: 3, RowsNoPrecedentN: 3, FromAllModels: false, SisterProjects: [] })),
  ];
  return JSON.stringify({ App: NAME, Phase: 2, Rows: vRows, RowFigures: vRowFigures, Totals: vTotals, Retro: false, AutoRouting: false, Skipped: { Backfilled: 0, Voided: 0, NoReqs: 0, ThisPhase: 0, NoTokens: 0 } });
}

function writeProjectFiles(aActualTokens: number): void {
  write(join(DIR, 'Directory.Build.props'), '<Project />\n');
  write(join(DIR, 'nuget.config'), '<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear /></packageSources></configuration>\n');
  write(join(DIR, `${NAME}.csproj`), '<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n    <OutputType>Exe</OutputType>\n    <TargetFramework>net10.0</TargetFramework>\n  </PropertyGroup>\n</Project>\n');
  write(join(DIR, 'Program.cs'), 'Console.WriteLine("est");\n');
  const vRows = ['REQ-FN-301', 'REQ-FN-302', 'REQ-FN-303'].map((aId) => `| ${aId} | A row | Not Started | 0% | | |\n`).join('');
  write(CHECKLIST, `# ${NAME} — Checklist (phase 2)\n\n## Requirements Status\n\n| ID | Requirement | Status | % | Remarks | Details |\n|----|-------------|--------|---|---------|---------|\n${vRows}\n## Page: Fixture\n`);
  write(STATUS, `# ${NAME} — Project status\n\nPhase 2 in progress. Three requirements not started.\n`);
  write(join(METRICS, 'runs.jsonl'), [...f1Lines(), ...actualLines(aActualTokens)].join('\n') + '\n');
  write(join(METRICS, 'estimates.jsonl'), estimateLines().join('\n') + '\n');
  for (const vName of ['gates', 'misses', 'sessions', 'commits']) write(join(METRICS, `${vName}.jsonl`), '');
}

function projectId(): number {
  return Number(query('SELECT ProjectId FROM Project WHERE Name = ?', NAME)[0].ProjectId);
}

/** Puts one finished end-to-end run, with the estimate it kept, in place on the project (the project must be known to the app). */
function seedFinishedRun(): number {
  const vProject = projectId();
  run('DELETE FROM RunEstimate WHERE ProjectId = ?', vProject);
  run('DELETE FROM AutoRunEvent WHERE AutoRunId IN (SELECT AutoRunId FROM AutoRun WHERE ProjectId = ?)', vProject);
  run('DELETE FROM AutoRunProcess WHERE AutoRunId IN (SELECT AutoRunId FROM AutoRun WHERE ProjectId = ?)', vProject);
  run('DELETE FROM AutoRun WHERE ProjectId = ?', vProject);
  run("UPDATE ProcessRun SET State='stopped', EndedUtc=? WHERE State IN ('running','waiting') AND ProjectId = ?", stampDb(Date.now()), vProject);
  const vEnd = Date.now() - 60_000;
  const vStart = vEnd - 3.5 * 3_600_000;
  const vAuto = run('INSERT INTO AutoRun (ProjectId, Brief, State, CurrentProcess, StartedUtc, EndedUtc) VALUES (?,?,?,?,?,?)',
    vProject, `[seed:estimate-report] ${BRIEF}`, 'Complete', CHAIN.length, stampDb(vStart), stampDb(vEnd));
  const vSpan = (vEnd - vStart) / CHAIN.length;
  CHAIN.forEach((aCode, aIndex) => {
    const vFrom = vStart + vSpan * aIndex;
    const vRun = run("INSERT INTO ProcessRun (ProjectId, ProcessCode, State, CurrentStep, StartedUtc, EndedUtc) VALUES (?,?,'Finished',1,?,?)",
      vProject, aCode, stampDb(vFrom), stampDb(vFrom + vSpan));
    run("INSERT INTO AutoRunProcess (AutoRunId, Position, ProcessCode, State, ProcessRunId, Retries) VALUES (?,?,?,'Finished',?,0)", vAuto, aIndex + 1, aCode, vRun);
  });
  run('INSERT OR REPLACE INTO RunEstimate (RunKind, RunId, ProjectId, BatchTs, Snapshot, KeptUtc) VALUES (?,?,?,?,?,?)', 'auto', vAuto, vProject, BATCH, snapshot(), stampDb(vStart));
  return vAuto;
}

async function openReport(): Promise<void> {
  await expect(async () => {
    if (!(await page.getByTestId('menu-project-report').isVisible())) await page.getByTestId('menu-project').click();
    await page.getByTestId('menu-project-report').click({ timeout: 2_500 });
    await expect(tab(page, 'Run record').first()).toBeVisible({ timeout: 3_000 });
  }).toPass({ timeout: 30_000 });
  await expect(page.getByTestId('requirements-table')).toBeVisible({ timeout: 30_000 });
}

/** Writes the project, opens it, seeds the run, and opens Run report on it. */
async function prepareReport(aActualTokens: number): Promise<void> {
  writeProjectFiles(aActualTokens);
  await openProject(page, NAME);
  seedFinishedRun();
  await page.reload({ waitUntil: 'domcontentloaded' });
  await expect(page.getByTestId('project-switch-name')).toHaveText(NAME, { timeout: 30_000 });
  await openReport();
}

const CASES: [number, string][] = [[6000, 'within'], [12000, 'within'], [12001, 'above'], [5999, 'below']];

for (const [vTokens, vWhere] of CASES) {
  test(`REQ-UI-088 a run estimated at 6,000 / 9,000 / 12,000 tokens that ends with ${vTokens.toLocaleString('en-US')} reads ${vWhere} on Run report`, async () => {
    await prepareReport(vTokens);
    await expect(page.getByTestId('estimate-actual-panel')).toBeVisible({ timeout: 30_000 });
    // The estimate is the kept batch, both ends of it belong to the range.
    await expect(page.getByTestId('estimate-actual-build-estimate')).toContainText('6,000 · 9,000 · 12,000 tokens');
    await expect(page.getByTestId('estimate-actual-build-actual-tokens')).toHaveText(`${vTokens.toLocaleString('en-US')} tokens`);
    await expect(page.getByTestId('estimate-actual-build-where')).toHaveText(vWhere);
    if (vTokens === 12001) {
      mkdirSync(SCREENS, { recursive: true });
      await page.setViewportSize({ width: 1280, height: 2400 });
      await page.getByTestId('estimate-actual-panel').scrollIntoViewIfNeeded();
      await page.screenshot({ path: join(SCREENS, 'run-report-estimate.png'), fullPage: true });
      await page.setViewportSize({ width: 1280, height: 720 });
    }
  });
}

test('REQ-UI-089 Run report reads the owner\'s own testing as not measured, never a number', async () => {
  await prepareReport(9000);
  await expect(page.getByTestId('estimate-actual-panel')).toBeVisible({ timeout: 30_000 });
  const vOwner = page.getByTestId('estimate-actual-owner');
  await expect(vOwner).toBeVisible();
  await expect(page.getByTestId('estimate-actual-owner-estimate')).toHaveText('not measured');
  await expect(page.getByTestId('estimate-actual-owner-actual')).toHaveText('not measured');
  expect(await vOwner.innerText()).not.toMatch(/\d/);
});

// ---- REQ-FN-115 -------------------------------------------------------------------------------------------------

/** What would be a ratio of an actual to its estimate, in any of the usual spellings. */
const RATIO = [/\d(?:[.,]\d+)?\s*(?:×|x)(?=\s|$|[^a-z])/i, /\d(?:[.,]\d+)?\s*%/, /\/\s*estimate/i, /(?:of|vs\.?|per|to)\s+(?:the\s+)?estimate/i, /ratio/i, /times\s+(?:the\s+)?estimate/i];

function expectNoRatio(aWhere: string, aText: string): void {
  for (const vPattern of RATIO) expect(aText, `${aWhere} shows a ratio (${vPattern})`).not.toMatch(vPattern);
}

const read = (aPath: string): string => readFileSync(aPath, 'utf8');

test('REQ-FN-115 with F1 estimated and the run ended, runs.jsonl, the checklist and PROJECT-STATUS hold no estimate figure and no screen shows a ratio', async () => {
  await prepareReport(9000);
  const vRunsBefore = read(join(METRICS, 'runs.jsonl'));
  const vChecklistBefore = read(CHECKLIST);
  const vStatusBefore = read(STATUS);
  const vProject = projectId();

  // Run report.
  await expect(page.getByTestId('estimate-actual-panel')).toBeVisible({ timeout: 30_000 });
  expectNoRatio('Run report', await page.getByTestId('estimate-actual-panel').innerText());

  // Processes: the estimate panel before build-phase starts (confirmed, so the batch is written where the app writes it).
  await openProcessesTab(page);
  await page.getByTestId('start-build-phase').click();
  await expect(page.getByTestId('estimate-panel')).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('estimate-tokens-build')).toHaveText(/6,000 · 9,000 · 12,000|6,000/);
  expectNoRatio('Processes estimate panel', await page.getByTestId('estimate-panel').innerText());
  await page.getByTestId('estimate-cancel').click();
  await expect(page.getByTestId('estimate-panel')).toHaveCount(0);

  // End-to-end run: the estimate before Start.
  await expect(async () => {
    if (!(await page.getByTestId('menu-project-auto').isVisible())) await page.getByTestId('menu-project').click();
    await page.getByTestId('menu-project-auto').click({ timeout: 2_500 });
    await expect(page.getByTestId('auto-run')).toBeVisible({ timeout: 3_000 });
  }).toPass({ timeout: 30_000 });
  await page.getByTestId('brief-text').fill(BRIEF);
  await expect(page.getByTestId('estimate-table')).toBeVisible({ timeout: 30_000 });
  expectNoRatio('End-to-end run', await page.getByTestId('estimate-panel').innerText());

  // Run queue: the dialog's estimate before Queue it is pressed, then closed without queueing.
  await expect(async () => {
    if (!(await page.getByTestId('menu-project-queue').isVisible())) await page.getByTestId('menu-project').click();
    await page.getByTestId('menu-project-queue').click({ timeout: 2_500 });
    await expect(page.getByTestId('run-queue')).toBeVisible({ timeout: 3_000 });
  }).toPass({ timeout: 30_000 });
  await page.getByTestId('queue-a-run').click();
  await expect(page.getByTestId('queue-run-dialog')).toBeVisible({ timeout: 15_000 });
  if ((await page.getByTestId('queue-estimate-panel').count()) === 0) {
    await page.getByTestId('queue-project').click();
    await page.getByRole('option', { name: NAME, exact: true }).click();
  }
  await page.getByTestId('queue-brief').fill(BRIEF);
  await expect(page.getByTestId('queue-estimate-panel')).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('queue-estimate-panel').getByTestId('estimate-table')).toBeVisible();
  expectNoRatio('Run queue estimate', await page.getByTestId('queue-estimate-panel').innerText());
  await page.keyboard.press('Escape');
  await expect(page.getByTestId('queue-run-dialog')).toBeHidden({ timeout: 15_000 });
  expectNoRatio('Run queue page', await page.getByTestId('run-queue').innerText());
  expect(query('SELECT COUNT(*) AS n FROM RunQueueItem WHERE ProjectId = ?', vProject)[0].n).toBe(0);

  // Process run: a build-phase run of this project that kept the estimate it was started with.
  const vBuildRun = Number(query("SELECT MAX(RunId) AS RunId FROM ProcessRun WHERE ProjectId = ? AND ProcessCode = 'build-phase'", vProject)[0].RunId);
  run('INSERT OR REPLACE INTO RunEstimate (RunKind, RunId, ProjectId, BatchTs, Snapshot, KeptUtc) VALUES (?,?,?,?,?,?)', 'process', vBuildRun, vProject, BATCH, snapshot(), stampDb(Date.now()));
  await openProcessesTab(page);
  await page.getByTestId('open-last-run').click();
  await expect(page.getByTestId('process-run')).toBeVisible({ timeout: 30_000 });
  await page.waitForTimeout(1500);
  const vProcessRunText = await page.getByTestId('process-run').innerText();
  for (const vPanel of ['run-against-estimate', 'handoff-panel']) {
    if ((await page.getByTestId(vPanel).count()) > 0) expectNoRatio(`Process run ${vPanel}`, await page.getByTestId(vPanel).innerText());
  }
  expect(vProcessRunText).not.toMatch(/\/\s*estimate|(?:of|vs\.?|times)\s+(?:the\s+)?estimate|ratio/i);

  // The files of the project: the estimate stayed in estimates.jsonl; the measurement stream, the checklist and the status hold none.
  const vRunsAfter = read(join(METRICS, 'runs.jsonl'));
  for (const vLine of vRunsAfter.split('\n').filter((aLine) => aLine.trim().length > 0)) {
    const vRecord = JSON.parse(vLine) as Record<string, unknown>;
    expect(vRecord.kind, 'runs.jsonl holds only measurements').not.toBe('estimate');
    for (const vKey of ['tokens_out', 'duration_s']) {
      expect(typeof vRecord[vKey] === 'object' && vRecord[vKey] !== null, `runs.jsonl ${vKey} is a single measured number`).toBe(false);
    }
    expect(Object.keys(vRecord).some((aKey) => /^(?:.*estimate.*|low|mid|high|ratio|.*_ratio|ratio_.*)$/i.test(aKey)), 'runs.jsonl has no estimate field').toBe(false);
  }
  // What was there is still there, byte for byte (the app may only have added measurements of its own).
  expect(vRunsAfter.startsWith(vRunsBefore)).toBe(true);
  expect(read(CHECKLIST)).toBe(vChecklistBefore);
  expect(read(STATUS)).toBe(vStatusBefore);
  expect(read(CHECKLIST)).not.toMatch(/estimate/i);
  expect(read(STATUS)).not.toMatch(/estimate|6,000|9,000|12,000/i);
  void writeFileSync;
});
