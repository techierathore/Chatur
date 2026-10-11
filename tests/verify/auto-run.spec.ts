// Acceptance tests: End-to-end run (REQ-UI-055, REQ-UI-056; mockups/auto-run.html). Black-box, web harness.
// The screen is opened from the Project menu as a tab. A run that is going is SEEDED as a paused run (the
// only going state nothing carries on by itself), because the chain cannot be made to reach a given role on
// demand; the start button is exercised for real in the last test.
import { Page, expect } from '@playwright/test';
import { join } from 'path';
import { signIn } from './support/auth';
import { query, run } from './support/db';
import { tab } from './support/phase2-tabs';
import { test, NAME, resetFixture, openProject, SHOTS } from './support/workbench-a';

let page: Page;

test.beforeAll(async ({ browser }) => {
  page = await browser.newPage({ baseURL: process.env.BASE_URL });
  await signIn(page, 'Owner');
});
test.afterAll(async () => { await page.close(); });
test.beforeEach(async () => {
  test.setTimeout(180_000);
  resetFixture();
});

const CHAIN = [
  ['day1-greenfield', 'Analyst'],
  ['split-brd', 'Architect'],
  ['mockups', 'Architect'],
  ['deploy-checklist', 'Architect'],
  ['build-phase', 'Flow master'],
  ['verify-phase', 'Verifier'],
  ['fix-issues', 'Flow master'],
  ['refresh-status', 'Flow master'],
];

async function openAutoRunTab(): Promise<void> {
  await expect(async () => {
    if (!(await page.getByTestId('menu-project-auto').isVisible())) await page.getByTestId('menu-project').click();
    await page.getByTestId('menu-project-auto').click({ timeout: 2_500 });
    await expect(tab(page, 'Auto run').first()).toBeVisible({ timeout: 3_000 });
  }).toPass({ timeout: 30_000 });
  await expect(page.getByTestId('auto-run')).toBeVisible({ timeout: 30_000 });
}

function clearRuns(): number {
  const vProject = Number(query('SELECT ProjectId FROM Project WHERE Name = ?', NAME)[0].ProjectId);
  const vTables = query("SELECT name FROM sqlite_master WHERE type='table' AND name IN ('AutoRunEvent','AutoRunProcess','AutoRun')").map((r) => r.name);
  if (vTables.includes('AutoRunEvent')) run('DELETE FROM AutoRunEvent WHERE AutoRunId IN (SELECT AutoRunId FROM AutoRun WHERE ProjectId = ?)', vProject);
  if (vTables.includes('AutoRunProcess')) run('DELETE FROM AutoRunProcess WHERE AutoRunId IN (SELECT AutoRunId FROM AutoRun WHERE ProjectId = ?)', vProject);
  run('DELETE FROM AutoRun WHERE ProjectId = ?', vProject);
  return vProject;
}

/** A run paused on process 5 (build-phase, the Flow master): four finished, one retried, a limit waited out. */
function seedGoingRun(aProject: number): number {
  const vStart = new Date(Date.now() - 102 * 60_000).toISOString();
  const vId = run(
    "INSERT INTO AutoRun (ProjectId, Brief, State, CurrentProcess, StartedUtc) VALUES (?, ?, 'Paused', 5, ?)",
    aProject, 'Finish the coverage panel: the file list, the per-file bar and the filter. Verify before you stop.', vStart);
  CHAIN.forEach(([aCode], aIndex) => {
    const vPos = aIndex + 1;
    const vState = vPos < 5 ? 'Finished' : vPos === 5 ? 'Running' : 'ToCome';
    run('INSERT INTO AutoRunProcess (AutoRunId, Position, ProcessCode, State, ProcessRunId, Retries) VALUES (?, ?, ?, ?, NULL, ?)',
      vId, vPos, aCode, vState, vPos === 4 ? 1 : 0);
  });
  const vAt = new Date(Date.now() - 60 * 60_000).toISOString();
  run("INSERT INTO AutoRunEvent (AutoRunId, AtUtc, Kind, ProcessPosition, ProcessCode, Message) VALUES (?, ?, 'Retry', 4, 'deploy-checklist', ?)",
    vId, vAt, 'Step 4, deploy-checklist, stalled once and was retried.');
  run("INSERT INTO AutoRunEvent (AutoRunId, AtUtc, Kind, ProcessPosition, ProcessCode, Message) VALUES (?, ?, 'ProcessStarted', 5, 'build-phase', ?)",
    vId, vAt, 'Started the Flow master for build-phase.');
  return vId;
}

test('REQ-UI-056 a run that is going shows the agent, the process, the step and the requirement', async () => {
  await openProject(page);
  const vProject = clearRuns();
  seedGoingRun(vProject);
  await page.reload({ waitUntil: 'domcontentloaded' });
  await expect(page.getByTestId('project-switch-name')).toHaveText(NAME, { timeout: 30_000 });
  await openAutoRunTab();

  await expect(page.getByTestId('running-now')).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('running-agent')).toHaveText('Flow master');
  await expect(page.getByTestId('running-process')).toHaveText('build-phase');
  await expect(page.getByTestId('running-position')).toHaveText('5 of 8');
  await expect(page.getByTestId('running-step')).not.toBeEmpty();
  await expect(page.getByTestId('running-req')).not.toBeEmpty();
  await expect(page.getByTestId('run-state')).toHaveText('paused');
  await expect(page.getByTestId('progress-value')).toContainText('%');
  await expect(page.getByTestId('retry-note')).toContainText('deploy-checklist');

  // Nothing overlaps: the running panel sits wholly above the chain panel.
  const vNow = (await page.getByTestId('running-now').boundingBox())!;
  const vChain = (await page.getByTestId('chain-panel').boundingBox())!;
  expect(vNow.y + vNow.height).toBeLessThanOrEqual(vChain.y + 1);
  await page.setViewportSize({ width: 1280, height: 2300 });
  await page.screenshot({ path: join(SHOTS, 'auto-run-going.png') });
  await page.setViewportSize({ width: 1280, height: 720 });
});

test('REQ-UI-055 the chain names the role each process starts and marks the one running', async () => {
  await openProject(page);
  const vProject = clearRuns();
  seedGoingRun(vProject);
  await page.reload({ waitUntil: 'domcontentloaded' });
  await expect(page.getByTestId('project-switch-name')).toHaveText(NAME, { timeout: 30_000 });
  await openAutoRunTab();

  await expect(page.getByTestId('chain-table')).toBeVisible({ timeout: 30_000 });
  for (let i = 0; i < CHAIN.length; i++) {
    await expect(page.getByTestId(`chain-row-${i + 1}`)).toContainText(CHAIN[i][0]);
    await expect(page.getByTestId(`chain-agent-${i + 1}`)).toHaveText(CHAIN[i][1]);
  }
  await expect(page.getByTestId('chain-state-1')).toHaveText(/finished/);
  await expect(page.getByTestId('chain-state-5')).toHaveText(/running/);
  await expect(page.getByTestId('chain-state-6')).toHaveText(/to come/);
  await expect(page.getByTestId('chain-retried-4')).toBeVisible();
  // The role working now is named at the top as well as in the chain row.
  await expect(page.getByTestId('running-agent')).toHaveText(CHAIN[4][1]);
});

test('REQ-UI-056 with no run going the screen offers the brief and shows nothing running', async () => {
  await openProject(page);
  clearRuns();
  await page.reload({ waitUntil: 'domcontentloaded' });
  await expect(page.getByTestId('project-switch-name')).toHaveText(NAME, { timeout: 30_000 });
  await openAutoRunTab();
  await expect(page.getByTestId('brief-panel')).toBeVisible();
  await expect(page.getByTestId('auto-empty')).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('running-now')).toHaveCount(0);
  await expect(page.getByTestId('step-by-step-note')).toBeVisible();
  // The bar and the two buttons are hidden, and the chain is shown as eight processes still to come.
  await expect(page.getByTestId('progress-bar')).toHaveCount(0);
  await expect(page.getByTestId('pause-run')).toHaveCount(0);
  await expect(page.getByTestId('stop-run')).toHaveCount(0);
  for (let i = 1; i <= CHAIN.length; i++) {
    await expect(page.getByTestId(`chain-row-${i}`)).toContainText(CHAIN[i - 1][0]);
    await expect(page.getByTestId(`chain-agent-${i}`)).toHaveText(CHAIN[i - 1][1]);
    await expect(page.getByTestId(`chain-state-${i}`)).toHaveText('to come');
  }

  // A short brief is refused and stays in the box.
  await page.getByTestId('brief-text').fill('Too short');
  await page.getByTestId('start-run').click();
  await expect(page.getByTestId('brief-rule')).toContainText('Say what should be true when the run finishes');
  await expect(page.getByTestId('brief-text')).toHaveValue('Too short');
  await page.screenshot({ path: join(SHOTS, 'auto-run-empty.png'), fullPage: true });
});

test('REQ-UI-056 starting a run from a brief shows the run at once', async () => {
  await openProject(page);
  clearRuns();
  await page.reload({ waitUntil: 'domcontentloaded' });
  await expect(page.getByTestId('project-switch-name')).toHaveText(NAME, { timeout: 30_000 });
  await openAutoRunTab();
  await page.getByTestId('brief-text').fill('Add a notes summary to the fixture program. Follow the BRD, tag every change with its REQ id, and verify before you stop.');
  await expect(async () => {
    if ((await page.getByTestId('running-now').count()) === 0) await page.getByTestId('start-run').click({ timeout: 2_500 });
    await expect(page.getByTestId('running-now')).toBeVisible({ timeout: 4_000 });
  }).toPass({ timeout: 60_000 });
  await expect(page.getByTestId('chain-table')).toBeVisible();
  await expect(page.getByTestId('chain-row-1')).toContainText('day1-greenfield');
  await page.screenshot({ path: join(SHOTS, 'auto-run-started.png'), fullPage: true });
  // Leave nothing running behind.
  const vStop = page.getByTestId('stop-run');
  if (await vStop.isVisible()) {
    await vStop.click();
    await page.getByTestId('stop-run-confirm').click();
  }
});
