// Acceptance tests: the estimate before an end-to-end run or a queued run (REQ-UI-084, REQ-UI-085, and the
// End-to-end run and Run queue parts of REQ-UI-089; mockups/auto-run.html, mockups/run-queue.html). Black-box, web
// harness. The fixture is fixture F1 of docs/Chatur-P3-BRD.md (five build-phase runs on REQ-FN rows outside the phase),
// written on disk as a private project, EstF1App, beside the Workbench fixtures: its docs/metrics/runs.jsonl and a
// phase-2 checklist of REQ-FN-301..303. Nothing here starts real model work; the one queued run is removed at the end.
import { Page, expect } from '@playwright/test';
import { mkdirSync } from 'fs';
import { join, resolve } from 'path';
import { signIn } from './support/auth';
import { query, run } from './support/db';
import { tab } from './support/phase2-tabs';
import { test, ROOT, write, openProject } from './support/workbench-a';
import { ensureTestProvider, testProvider } from './support/provider';

const NAME = 'EstF1App';
const SCREENS = resolve(__dirname, '../.artifacts/verify/screens');
const BRIEF = 'Add a notes summary to the fixture program. Follow the BRD, tag every change with its REQ id, and verify before you stop.';

let page: Page;

test.beforeAll(async ({ browser }) => {
  page = await browser.newPage({ baseURL: process.env.BASE_URL });
  await signIn(page, 'Owner');
});
test.afterAll(async () => {
  try { await removeAddedModels(); } catch { /* the page may be gone */ }
  await page.close();
});
test.beforeEach(async () => {
  test.setTimeout(240_000);
  writeFixture();
});

/** F1: A 2 rows 4,000 tokens 600 s; B 1, 3,000, 240; C 2, 10,000, 900; D 4, 4,000, 480; E 4, 16,000, 1,440; all build-phase on opus. */
function f1Lines(): string[] {
  const vRuns: [string[], number, number][] = [
    [['REQ-FN-201', 'REQ-FN-202'], 4000, 600],
    [['REQ-FN-203'], 3000, 240],
    [['REQ-FN-204', 'REQ-FN-205'], 10000, 900],
    [['REQ-FN-206', 'REQ-FN-207', 'REQ-FN-208', 'REQ-FN-209'], 4000, 480],
    [['REQ-FN-210', 'REQ-FN-211', 'REQ-FN-212', 'REQ-FN-213'], 16000, 1440],
  ];
  const vStamp = (aMs: number) => new Date(aMs).toISOString().replace(/\.\d{3}Z$/, 'Z');
  const vBase = Date.UTC(2026, 9, 1);
  return vRuns.map(([aReqs, aTokens, aSeconds], aIndex) => {
    const vStart = vBase + (aIndex + 1) * 3 * 3_600_000;
    const vEnd = vStart + aSeconds * 1000;
    return JSON.stringify({
      v: 1, ts: vStamp(vEnd), kind: 'run', app: NAME, cmd: 'build-phase', mode: 'build', started: vStamp(vStart), ended: vStamp(vEnd),
      duration_s: aSeconds, reqs_touched: aReqs, reqs_count: aReqs.length, model: 'claude-opus-5-5', tokens_out: aTokens, tokens_scope: 'tree',
    });
  });
}

function writeFixture(): void {
  const vDir = join(ROOT, NAME);
  write(join(vDir, 'Directory.Build.props'), '<Project />\n');
  write(join(vDir, 'nuget.config'), '<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear /></packageSources></configuration>\n');
  write(join(vDir, `${NAME}.csproj`), '<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n    <OutputType>Exe</OutputType>\n    <TargetFramework>net10.0</TargetFramework>\n  </PropertyGroup>\n</Project>\n');
  write(join(vDir, 'Program.cs'), 'Console.WriteLine("est");\n');
  const vRows = ['REQ-FN-301', 'REQ-FN-302', 'REQ-FN-303'].map((aId) => `| ${aId} | A row | Not Started | 0% | | |\n`).join('');
  write(join(vDir, 'docs', `${NAME}-P2-Checklist.md`),
    `# ${NAME} — Checklist (phase 2)\n\n## Requirements Status\n\n| ID | Requirement | Status | % | Remarks | Details |\n|----|-------------|--------|---|---------|---------|\n${vRows}\n## Page: Fixture\n`);
  write(join(vDir, 'docs', 'metrics', 'runs.jsonl'), f1Lines().join('\n') + '\n');
}

function projectId(): number {
  return Number(query('SELECT ProjectId FROM Project WHERE Name = ?', NAME)[0].ProjectId);
}

/** Takes every run and queue row of the fixture project out, ending any run first so nothing is left going. */
function clearRuns(): void {
  const vProject = projectId();
  run("UPDATE AutoRun SET State = 'Stopped', EndedUtc = ? WHERE ProjectId = ? AND State NOT IN ('Stopped','Finished','Failed')", new Date().toISOString(), vProject);
  // The queue lists every project's runs and the cells are numbered over the whole queue, so the queue is emptied
  // (as run-queue.spec.ts does) for this project's queued run to be row 2; the queue's kept estimates go with it, so a
  // reused queue-item id never finds an estimate kept for an earlier item.
  run('DELETE FROM RunQueueItem');
  run("DELETE FROM RunEstimate WHERE RunKind = 'queue'");
  run('DELETE FROM AutoRunEvent WHERE AutoRunId IN (SELECT AutoRunId FROM AutoRun WHERE ProjectId = ?)', vProject);
  run('DELETE FROM AutoRunProcess WHERE AutoRunId IN (SELECT AutoRunId FROM AutoRun WHERE ProjectId = ?)', vProject);
  run('DELETE FROM AutoRun WHERE ProjectId = ?', vProject);
}

/** A run waiting out a usage limit, with its queue row: going, so a run queued behind it waits and no real work starts. */
function seedGoingRun(): void {
  const vProject = projectId();
  const vId = run(
    "INSERT INTO AutoRun (ProjectId, Brief, State, CurrentProcess, StartedUtc, ResumeAtUtc) VALUES (?, ?, 'WaitingForLimit', 5, ?, ?)",
    vProject, BRIEF, new Date(Date.now() - 42 * 60_000).toISOString(), new Date(Date.now() + 6 * 3_600_000).toISOString());
  run("INSERT INTO RunQueueItem (ProjectId, Brief, Status, AutoRunId, QueuedUtc) VALUES (?, ?, 'Started', ?, ?)", vProject, BRIEF, vId, new Date().toISOString());
}

/** Switches Settings ▸ Routing automatic routing to the wanted state. */
async function setAutoRouting(aOn: boolean): Promise<void> {
  await page.goto('/settings/routing', { waitUntil: 'networkidle' });
  await expect(page.getByTestId('auto-routing-panel')).toBeVisible({ timeout: 30_000 });
  const vState = page.getByTestId('auto-routing-state');
  await expect(vState).toHaveText(/^(on|off)$/, { timeout: 15_000 });
  if ((await vState.innerText()) !== (aOn ? 'on' : 'off')) await page.getByTestId('auto-routing-switch').click();
  await expect(vState).toHaveText(aOn ? 'on' : 'off', { timeout: 15_000 });
}

/** The tiers this spec filled with the test provider's model, because they named none; taken out again afterwards. */
const addedTiers: number[] = [];

/** The estimate names a model only when its tier's chain holds one: fills each empty tier with the test provider's model. */
async function ensureTierModels(): Promise<void> {
  const vP = testProvider();
  await ensureTestProvider(page);
  await page.goto('/settings/routing', { waitUntil: 'networkidle' });
  await expect(page.getByTestId('auto-routing-panel')).toBeVisible({ timeout: 30_000 });
  for (const vTier of [1, 2, 3]) {
    const vRows = page.locator(`[data-testid^="tier-${vTier}-row-"]`);
    if ((await vRows.count()) > 0) continue;
    await page.getByTestId(`tier-${vTier}-add`).click();
    await page.getByTestId(`tier-${vTier}-add-pop`).locator(`[data-testid^="tier-${vTier}-add-"]`)
      .filter({ hasText: `${vP.model} (${vP.name})` }).first().click();
    await expect(vRows).toHaveCount(1, { timeout: 15_000 });
    if (!addedTiers.includes(vTier)) addedTiers.push(vTier);
  }
}

async function removeAddedModels(): Promise<void> {
  if (addedTiers.length === 0) return;
  await page.goto('/settings/routing', { waitUntil: 'networkidle' });
  for (const vTier of addedTiers.splice(0)) {
    const vRows = page.locator(`[data-testid^="tier-${vTier}-row-"]`);
    if ((await vRows.count()) === 1) {
      await page.getByTestId(`tier-${vTier}-remove-1`).click();
      await expect(vRows).toHaveCount(0, { timeout: 15_000 });
    }
  }
}

async function openTab(aMenuId: string, aTitle: string, aScreenId: string): Promise<void> {
  await expect(async () => {
    if (!(await page.getByTestId(aMenuId).isVisible())) await page.getByTestId('menu-project').click();
    await page.getByTestId(aMenuId).click({ timeout: 2_500 });
    await expect(tab(page, aTitle).first()).toBeVisible({ timeout: 3_000 });
  }).toPass({ timeout: 30_000 });
  await expect(page.getByTestId(aScreenId)).toBeVisible({ timeout: 30_000 });
}

/** Opens the fixture project on the Workbench, then the End-to-end run tab with the brief filled in. */
async function openAutoRunWithBrief(): Promise<void> {
  await openProject(page, NAME);
  clearRuns();
  await page.reload({ waitUntil: 'domcontentloaded' });
  await expect(page.getByTestId('project-switch-name')).toHaveText(NAME, { timeout: 30_000 });
  await openTab('menu-project-auto', 'Auto run', 'auto-run');
  await expect(page.getByTestId('brief-text')).toBeVisible({ timeout: 30_000 });
  await page.getByTestId('brief-text').fill(BRIEF);
}

test('REQ-UI-084 the estimate shows before Start on End-to-end run, each line naming its model and tier', async () => {
  await ensureTierModels();
  await setAutoRouting(false);
  await openAutoRunWithBrief();

  // The estimate is up before anything is started.
  await expect(page.getByTestId('estimate-panel')).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('estimate-table')).toBeVisible();
  await expect(page.getByTestId('estimate-tokens-build')).not.toHaveText('');
  await expect(page.getByTestId('estimate-tokens-build')).not.toHaveText('no precedent');
  await expect(page.getByTestId('running-now')).toHaveCount(0);
  expect(query('SELECT COUNT(*) AS n FROM AutoRun WHERE ProjectId = ?', projectId())[0].n).toBe(0);

  // Each line of work names its model and tier.
  await expect(page.getByTestId('estimate-models')).toBeVisible();
  for (const vKey of ['build', 'verify', 'fix', 'triage']) {
    await expect(page.getByTestId(`estimate-model-${vKey}`)).toHaveText(/\S+.*tier \d+/);
  }
  mkdirSync(SCREENS, { recursive: true });
  await page.setViewportSize({ width: 1280, height: 1500 });
  await page.screenshot({ path: join(SCREENS, 'auto-run-estimate.png'), fullPage: true });
  await page.setViewportSize({ width: 1280, height: 720 });
});

test('REQ-UI-084 with automatic routing on the estimate says the run may use another model, and with it off it does not', async () => {
  try {
    await setAutoRouting(true);
    await openAutoRunWithBrief();
    await expect(page.getByTestId('estimate-table')).toBeVisible({ timeout: 30_000 });
    await expect(page.getByTestId('estimate-auto-routing')).toHaveText('Automatic routing is on, so the run may use another model than the one named.');
  } finally {
    await setAutoRouting(false);
  }
  await openAutoRunWithBrief();
  await expect(page.getByTestId('estimate-table')).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('estimate-auto-routing')).toHaveCount(0);
});

test('REQ-UI-089 End-to-end run reads the owner\'s own testing as not measured, never a number', async () => {
  await setAutoRouting(false);
  await openAutoRunWithBrief();
  await expect(page.getByTestId('estimate-table')).toBeVisible({ timeout: 30_000 });
  const vOwner = page.getByTestId('estimate-row-owner');
  await expect(vOwner).toBeVisible();
  await expect(vOwner).toContainText(/not measured/i);
  await expect(page.getByTestId('estimate-tokens-owner')).toHaveText(/not measured/i);
  await expect(page.getByTestId('estimate-time-owner')).toHaveText(/not measured/i);
  expect(await vOwner.innerText()).not.toMatch(/\d/);
});

test('REQ-UI-085 and REQ-UI-089 a run queued for F1 shows its estimate before Queue it is pressed and keeps it on the waiting run', async () => {
  await setAutoRouting(false);
  await openProject(page, NAME);
  const vProject = projectId();
  clearRuns();
  seedGoingRun();
  await page.reload({ waitUntil: 'domcontentloaded' });
  await expect(page.getByTestId('project-switch-name')).toHaveText(NAME, { timeout: 30_000 });
  await openTab('menu-project-queue', 'Run queue', 'run-queue');

  await page.getByTestId('queue-a-run').click();
  await expect(page.getByTestId('queue-run-dialog')).toBeVisible({ timeout: 15_000 });
  // The dialog's project select defaults to the open project when it is not already chosen.
  if ((await page.getByTestId('queue-estimate-panel').count()) === 0) {
    await page.getByTestId('queue-project').click();
    await page.getByRole('option', { name: NAME, exact: true }).click();
  }
  await page.getByTestId('queue-brief').fill(BRIEF);

  // Before Queue it is pressed the estimate is up, and nothing is queued yet.
  const vPanel = page.getByTestId('queue-estimate-panel');
  await expect(vPanel).toBeVisible({ timeout: 30_000 });
  await expect(vPanel.getByTestId('estimate-table')).toBeVisible();
  await expect(vPanel.getByTestId('estimate-tokens-build')).not.toHaveText('no precedent');
  const vBuildTokens = (await vPanel.getByTestId('estimate-tokens-build').innerText()).trim();
  expect(query('SELECT COUNT(*) AS n FROM RunQueueItem WHERE ProjectId = ?', vProject)[0].n).toBe(1);
  // REQ-UI-089: the owner's own testing is not measured here too.
  await expect(vPanel.getByTestId('estimate-tokens-owner')).toHaveText(/not measured/i);
  await expect(vPanel.getByTestId('estimate-time-owner')).toHaveText(/not measured/i);
  mkdirSync(SCREENS, { recursive: true });
  await page.screenshot({ path: join(SCREENS, 'run-queue-estimate-dialog.png'), fullPage: true });

  await page.getByTestId('queue-run-add').click();
  await expect(page.getByTestId('queue-run-dialog')).toBeHidden({ timeout: 15_000 });

  // The waiting run keeps the estimate it was queued with (find the row of this project).
  const vRows = query('SELECT RunQueueItemId FROM RunQueueItem WHERE ProjectId = ?', vProject);
  expect(vRows.length).toBe(2);
  const vCell = page.getByTestId('queue-estimate-2');
  await expect(vCell).toBeVisible({ timeout: 30_000 });
  await expect(vCell).toHaveText(/\d/);
  await expect(vCell).not.toHaveText(/no precedent|no estimate yet/);
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.screenshot({ path: join(SCREENS, 'run-queue-estimate.png'), fullPage: true });

  // Kept in the database, too, so a later read does not work it out again.
  expect(query("SELECT COUNT(*) AS n FROM RunEstimate WHERE RunKind = 'queue' AND ProjectId = ?", vProject)[0].n).toBeGreaterThanOrEqual(1);

  // Remove the queued run.
  await page.getByTestId('remove-2').click();
  await page.getByTestId('remove-confirm').click();
  await expect(page.getByTestId('queue-row-2')).toHaveCount(0, { timeout: 15_000 });
  expect(query("SELECT COUNT(*) AS n FROM RunQueueItem WHERE ProjectId = ? AND Status = 'Queued'", vProject)[0].n).toBe(0);
  clearRuns();
  void vBuildTokens;
});
