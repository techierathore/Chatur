// Acceptance tests: Process run while it goes, while it waits, and after a restart (REQ-FN-052, REQ-FN-053,
// REQ-FN-055; mockups/process-run.html). Black-box, web harness. A run is started from Project ▸ Processes,
// and the restart stops the harness process and starts the same copy again on the same database.
import { Page, expect } from '@playwright/test';
import { join } from 'path';
import { signIn } from './support/auth';
import { query } from './support/db';
import { openProcessesTab, startProcess, tab } from './support/phase2-tabs';
import { restartHarness } from './support/restart';
import { ensureStubProvider, removeStubProvider, startStubModel, StubModel } from './support/stub-model';
import { test, resetFixture, openProject, SHOTS } from './support/workbench-a';

let page: Page;
let stub: StubModel;

test.beforeAll(async ({ browser }) => {
  page = await browser.newPage({ baseURL: process.env.BASE_URL });
  await signIn(page, 'Owner');
  stub = await startStubModel('read-file', { path: 'docs/WbAFilesApp-P2-Checklist.md' });
  await ensureStubProvider(page, stub);
});
test.afterAll(async () => {
  await removeStubProvider(page);
  await stub.close();
  await page.close();
});
test.beforeEach(async () => {
  // verify-phase builds and tests the fixture project for real, about a minute; the restart adds one more
  test.setTimeout(480_000);
  resetFixture();
});

type StepRow = { Position: number; State: string; Result: string | null; FinishedUtc: string | null };
const steps = (aRunId: number) =>
  query('SELECT Position, State, Result, FinishedUtc FROM ProcessRunStep WHERE RunId = ? ORDER BY Position', aRunId) as StepRow[];
const runRow = (aRunId: number) =>
  query('SELECT State, CurrentStep, EndedUtc FROM ProcessRun WHERE RunId = ?', aRunId)[0] as { State: string; CurrentStep: number; EndedUtc: string | null };

function slowBuildFixture() {
  // Step 2 of verify-phase builds the fixture project. On fast machines the build finishes before the
  // test can observe it running, so give the compiler some work to do.
  const vClasses = Array.from({ length: 3000 }, (_, i) => `public class SlowBuildClass${i} { public int Value { get; set; } = ${i}; }`).join('\n');
  // eslint-disable-next-line @typescript-eslint/no-var-requires
  require('./support/workbench-a').write(join(require('./support/workbench-a').PROJECT, 'SlowBuild.cs'), vClasses);
}

async function startVerifyPhase(): Promise<number> {
  slowBuildFixture();
  await openProject(page);
  await openProcessesTab(page);
  return startProcess(page, 'verify-phase');
}

test('REQ-FN-052 while the run goes, the step it is on is marked and the finished steps show their result', async () => {
  await startVerifyPhase();
  // step 2 builds the fixture project, about ten seconds: long enough to see the run while it is on it
  await expect(page.getByTestId('run-step-2-state')).toHaveText('running', { timeout: 30_000 });
  await expect(page.getByTestId('run-state')).toContainText('running');
  await expect(page.getByTestId('run-step-1-state')).toHaveText(/done|check passed/);
  await expect(page.getByTestId('run-step-1-result')).toContainText('P2-Checklist.md');
  await expect(page.getByTestId('run-step-3-state')).toHaveText('to come');
  await expect(page.getByTestId('run-step-3-result')).toHaveText('Not started');
  await page.screenshot({ path: join(SHOTS, 'process-run-going.png'), fullPage: true });

  // once past the build, the build step shows what it did
  await expect(page.getByTestId('run-step-2-state')).toHaveText(/done|check passed/, { timeout: 180_000 });
  await expect(page.getByTestId('run-step-2-result')).toContainText('Build passed');
});

test('REQ-FN-053 at the owner stop the run waits and does nothing further until answered', async () => {
  const vId = await startVerifyPhase();
  await expect(page.getByTestId('run-state')).toContainText('waiting for you', { timeout: 180_000 });
  const vBefore = steps(vId);
  const vWaitingAt = runRow(vId).CurrentStep;
  await expect(page.getByTestId(`run-step-${vWaitingAt}-state`)).toHaveText('waiting for you');

  // left alone for half a minute, nothing moves: no step finishes, no later step starts, the run does not end
  await page.waitForTimeout(30_000);
  expect(steps(vId), 'no step changed while the run waited').toEqual(vBefore);
  expect(runRow(vId)).toMatchObject({ State: 'waiting', CurrentStep: vWaitingAt, EndedUtc: null });
  await expect(page.getByTestId('run-state')).toContainText('waiting for you');
  await expect(page.getByTestId(`run-step-${vWaitingAt + 1}-state`)).toHaveText('to come');

  // the answer is what moves it on
  await page.getByTestId('answer-yes').click();
  await expect(page.getByTestId('run-state')).toContainText('finished', { timeout: 180_000 });
});

test('REQ-FN-055 a run waiting before a restart is answered after it and carries on from that step', async () => {
  const vId = await startVerifyPhase();
  await expect(page.getByTestId('run-state')).toContainText('waiting for you', { timeout: 180_000 });
  const vWaitingAt = runRow(vId).CurrentStep;
  const vDoneBefore = steps(vId).filter((s) => s.Position < vWaitingAt);
  expect(vDoneBefore.length).toBeGreaterThan(0);

  await restartHarness();
  await signIn(page, 'Owner');
  await openProject(page);
  await openProcessesTab(page);
  await expect(page.getByTestId('open-last-run')).toBeEnabled({ timeout: 30_000 });
  await page.getByTestId('open-last-run').click();
  await expect(tab(page, `verify-phase · run ${vId}`).first()).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('run-state')).toContainText('waiting for you', { timeout: 30_000 });
  await expect(page.getByTestId(`run-step-${vWaitingAt}-state`)).toHaveText('waiting for you');
  await page.screenshot({ path: join(SHOTS, 'process-run-after-restart.png'), fullPage: true });

  await page.getByTestId('answer-yes').click();
  await expect(page.getByTestId('run-state')).toContainText('finished', { timeout: 180_000 });
  // the steps before the stop were not run again: each kept the result and the time it had before the restart
  expect(steps(vId).filter((s) => s.Position < vWaitingAt), 'the earlier steps were not run again').toEqual(vDoneBefore);
  expect(steps(vId).find((s) => s.Position === vWaitingAt)?.State).toBe('done');
});
