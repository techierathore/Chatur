// Acceptance tests: the end-to-end run engine through the End-to-end run screen (REQ-FN-061, REQ-FN-062,
// REQ-FN-063, REQ-FN-066; mockups/auto-run.html). Black-box, web harness, a private tiny .NET project with a
// checklist that has open requirements. The chain really runs through the process engine (verify-phase really
// builds the fixture), so the budgets are honest. A work step is handed to its process's agent (REQ-FN-084), so a
// scripted model service (tests/verify/support/stub-model.ts) answers every agent turn the same way each time.
import { Page, expect } from '@playwright/test';
import { join } from 'path';
import { signIn } from './support/auth';
import { query, run } from './support/db';
import { openProcessesTab, tab } from './support/phase2-tabs';
import { test, ROOT, write, openProject, SHOTS } from './support/workbench-a';
import { ensureStubProvider, removeStubProvider, startStubModel, StubModel } from './support/stub-model';
import { restartHarness } from './support/restart';

const NAME = 'AutoEngineApp';
const DIR = join(ROOT, NAME);
const CODES = ['day1-greenfield', 'split-brd', 'mockups', 'deploy-checklist', 'build-phase', 'verify-phase', 'fix-issues', 'refresh-status'];
const ALL_TWELVE = [
  'day1-greenfield', 'split-brd', 'build-phase', 'verify-phase', 'fix-issues', 'handoff-phase',
  'refresh-status', 'deploy-checklist', 'mockups', 'amend-docs', 'triage-issues', 'metrics-report',
];

let page: Page;
let stub: StubModel;

test.beforeAll(async ({ browser }) => {
  page = await browser.newPage({ baseURL: process.env.BASE_URL });
  await signIn(page, 'Owner');
  stub = await startStubModel('read-file', { path: 'Program.cs' });
  await ensureStubProvider(page, stub);
});
test.afterAll(async () => {
  await removeStubProvider(page);
  await stub.close();
  await page.close();
});

function resetEngineFixture(): void {
  write(join(DIR, 'Directory.Build.props'), '<Project />\n');
  write(join(DIR, 'nuget.config'), '<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear /></packageSources></configuration>\n');
  write(join(DIR, `${NAME}.csproj`),
    '<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n    <OutputType>Exe</OutputType>\n    <TargetFramework>net10.0</TargetFramework>\n    <ImplicitUsings>enable</ImplicitUsings>\n    <Nullable>enable</Nullable>\n  </PropertyGroup>\n</Project>\n');
  write(join(DIR, 'Program.cs'), 'Console.WriteLine("engine fixture");\n');
  write(join(DIR, 'docs', `${NAME}-Checklist.md`),
    '# AutoEngineApp - Checklist (phase 1)\n\n## Requirements Status\n\n| ID | Requirement | Status | % | Remarks | Details |\n|----|---|---|---|---|---|\n'
    + '| REQ-FN-001 | Not yet begun | Not Started | 0% | - | [view](#d-req-fn-001) |\n'
    + '| REQ-FN-002 | Sent back | Blocked | 25% | - | [view](#d-req-fn-002) |\n'
    + '| REQ-FN-003 | Proven | Verified | 100% | - | [view](#d-req-fn-003) |\n\n'
    + '## Page: Home (`/`)\n\n<a id="d-req-fn-001"></a>\n- **REQ-FN-001** - Not yet begun. *BRD:* BRD-1\n  - *Acceptance:* When it is begun, then it works.\n');
}

function projectId(): number {
  return Number(query('SELECT ProjectId FROM Project WHERE Name = ?', NAME)[0].ProjectId);
}

function clearRuns(aProject: number): void {
  run('DELETE FROM AutoRunEvent WHERE AutoRunId IN (SELECT AutoRunId FROM AutoRun WHERE ProjectId = ?)', aProject);
  run('DELETE FROM AutoRunProcess WHERE AutoRunId IN (SELECT AutoRunId FROM AutoRun WHERE ProjectId = ?)', aProject);
  run('DELETE FROM AutoRun WHERE ProjectId = ?', aProject);
}

async function openAutoRunTab(): Promise<void> {
  await expect(async () => {
    if (!(await page.getByTestId('menu-project-auto').isVisible())) await page.getByTestId('menu-project').click();
    await page.getByTestId('menu-project-auto').click({ timeout: 2_500 });
    await expect(tab(page, 'Auto run').first()).toBeVisible({ timeout: 3_000 });
  }).toPass({ timeout: 30_000 });
  await expect(page.getByTestId('auto-run')).toBeVisible({ timeout: 30_000 });
}

test('REQ-FN-061 REQ-FN-065 a brief starts the chain by itself, hands the work to the role and stops with the reason when a check fails', async () => {
  test.setTimeout(240_000);
  resetEngineFixture();
  await openProject(page, NAME);
  clearRuns(projectId());
  await page.reload({ waitUntil: 'domcontentloaded' });
  await expect(page.getByTestId('project-switch-name')).toHaveText(NAME, { timeout: 30_000 });
  await openAutoRunTab();

  await page.getByTestId('brief-text').fill('Begin the not-yet-begun requirement of the fixture and verify it before you stop, tagging every REQ id.');
  await expect(async () => {
    if ((await page.getByTestId('running-now').count()) === 0 && (await page.getByTestId('stopped-note').count()) === 0) await page.getByTestId('start-run').click({ timeout: 2_500 });
    await expect(page.getByTestId('running-now').or(page.getByTestId('stopped-note')).first()).toBeVisible({ timeout: 4_000 });
  }).toPass({ timeout: 60_000 });

  // Nobody starts the first process: the run does, hands its work steps to the Analyst, and stops at the check the
  // scripted model cannot satisfy (it never writes the brief), saying which check and why.
  await expect(page.getByTestId('run-state')).toHaveText('stopped', { timeout: 180_000 });
  await expect(page.getByTestId('stopped-note')).toContainText('the project brief exists');
  await expect(page.getByTestId('chain-row-1')).toContainText('day1-greenfield');
  const vOwn = 'SELECT ProcessRunId FROM AutoRunProcess WHERE ProcessRunId IS NOT NULL AND AutoRunId = (SELECT MAX(AutoRunId) FROM AutoRun WHERE ProjectId = ?)';
  const vRuns = query(`SELECT ProcessCode, State FROM ProcessRun WHERE RunId IN (${vOwn}) ORDER BY RunId`, projectId());
  expect(vRuns.map((r) => r.ProcessCode)).toEqual(['day1-greenfield']);
  expect(vRuns[0].State).toBe('stopped');
  const vWork = query(`SELECT AgentName, State FROM ProcessRunStepWork WHERE RunId IN (${vOwn})`, projectId());
  expect(vWork.length).toBeGreaterThan(0);
  expect(vWork.every((w) => w.AgentName === 'Analyst')).toBe(true);
  expect(stub.calls.length).toBeGreaterThan(0);
  await page.screenshot({ path: join(SHOTS, 'auto-run-engine-stopped.png'), fullPage: true });
});

test('REQ-FN-061 a run going when Chatur closes is the same run after it opens again, and goes on to the same end', async () => {
  test.setTimeout(420_000);
  resetEngineFixture();
  await openProject(page, NAME);
  clearRuns(projectId());
  await page.reload({ waitUntil: 'domcontentloaded' });
  await expect(page.getByTestId('project-switch-name')).toHaveText(NAME, { timeout: 30_000 });
  await openAutoRunTab();

  // a slow model keeps the Analyst's step going, so Chatur is closed while the run is under way
  stub.setDelay(25_000);
  let vId = 0;
  try {
    await page.getByTestId('brief-text').fill('Begin the not-yet-begun requirement of the fixture and verify it before you stop, tagging every REQ id.');
    await expect(async () => {
      if ((await page.getByTestId('running-now').count()) === 0) await page.getByTestId('start-run').click({ timeout: 2_500 });
      await expect(page.getByTestId('running-now')).toBeVisible({ timeout: 4_000 });
    }).toPass({ timeout: 60_000 });
    const vRun = query('SELECT AutoRunId, State FROM AutoRun WHERE ProjectId = ?', projectId());
    expect(vRun).toHaveLength(1);
    expect(vRun[0].State).toBe('Running');
    vId = Number(vRun[0].AutoRunId);

    await restartHarness();
  } finally {
    stub.setDelay(0);
  }

  await signIn(page, 'Owner');
  await openProject(page, NAME);
  await openAutoRunTab();
  // the same run, not a new one, and it goes on to the end the uninterrupted run reaches
  await expect(page.getByTestId('run-state')).toHaveText('stopped', { timeout: 240_000 });
  await expect(page.getByTestId('stopped-note')).toContainText('the project brief exists');
  const vAfter = query('SELECT AutoRunId FROM AutoRun WHERE ProjectId = ?', projectId());
  expect(vAfter.map((r) => Number(r.AutoRunId)), 'still the one run, carried on').toEqual([vId]);
  const vOwn = 'SELECT ProcessRunId FROM AutoRunProcess WHERE ProcessRunId IS NOT NULL AND AutoRunId = ?';
  const vRuns = query(`SELECT ProcessCode FROM ProcessRun WHERE RunId IN (${vOwn})`, vId);
  expect(vRuns.map((r) => r.ProcessCode), 'day1-greenfield was carried on, not started a second time').toEqual(['day1-greenfield']);
  // the interruption is in the run's record (REQ-FN-065: the retry is recorded)
  const vPickedUp = query("SELECT Kind, ProcessCode, Message FROM AutoRunEvent WHERE AutoRunId = ? AND Message LIKE '%picked up again%'", vId);
  expect(vPickedUp, 'one record line says the run was picked up after Chatur closed').toHaveLength(1);
  expect(vPickedUp[0]).toMatchObject({ Kind: 'Retry', ProcessCode: 'day1-greenfield' });
  await page.screenshot({ path: join(SHOTS, 'auto-run-after-restart.png'), fullPage: true });
});

test('REQ-FN-062 REQ-FN-066 a chain that has finished reads ready for acceptance, reports the open work and cannot be complete', async () => {
  test.setTimeout(120_000);
  await openProject(page, NAME);
  const vProject = projectId();
  clearRuns(vProject);
  const vNow = new Date().toISOString().replace('Z', '0Z');
  run("INSERT INTO AutoRun (ProjectId, Brief, State, CurrentProcess, StartedUtc, EndedUtc) VALUES (?, ?, 'ReadyForAcceptance', 8, ?, ?)", vProject, '[engine] Finish the fixture and stop for the acceptance test.', vNow, vNow);
  const vId = Number(query('SELECT AutoRunId FROM AutoRun WHERE ProjectId = ?', vProject)[0].AutoRunId);
  CODES.forEach((code, i) => run("INSERT INTO AutoRunProcess (AutoRunId, Position, ProcessCode, State, ProcessRunId, Retries) VALUES (?, ?, ?, 'Finished', NULL, 0)", vId, i + 1, code));
  await page.reload({ waitUntil: 'domcontentloaded' });
  await expect(page.getByTestId('project-switch-name')).toHaveText(NAME, { timeout: 30_000 });
  await openAutoRunTab();

  await expect(page.getByTestId('run-state')).toHaveText('ready for your acceptance test', { timeout: 30_000 });
  await expect(page.getByTestId('open-count')).not.toHaveText('0');
  await expect(page.getByTestId('complete-rule')).toContainText('never reports complete while a single requirement is open');
  expect(query("SELECT State FROM AutoRun WHERE ProjectId = ? AND State = 'Complete'", vProject)).toHaveLength(0);
  await page.screenshot({ path: join(SHOTS, 'auto-run-engine-ready.png'), fullPage: true });
});

test('REQ-FN-063 with no run going every process can still be started on its own from Processes', async () => {
  test.setTimeout(120_000);
  await openProject(page, NAME);
  await page.reload({ waitUntil: 'domcontentloaded' });
  await expect(page.getByTestId('project-switch-name')).toHaveText(NAME, { timeout: 30_000 });
  expect(query("SELECT AutoRunId FROM AutoRun WHERE ProjectId = ? AND State IN ('Running','WaitingForLimit','Paused')", projectId())).toHaveLength(0);
  await openProcessesTab(page);
  for (const vCode of ALL_TWELVE) {
    await expect(page.getByTestId(`start-${vCode}`)).toBeEnabled();
  }
});
