// Acceptance tests: Process run (REQ-UI-047, REQ-UI-048; mockups/process-run.html). Black-box, web harness.
// A run is started through Processes' own Start link; the failed-check case seeds the run's own rows,
// because the UI cannot make a check fail by itself.
import { Page, expect } from '@playwright/test';
import { join } from 'path';
import { signIn } from './support/auth';
import { query, run } from './support/db';
import { openProcessesTab, startProcess, closeTab, tab } from './support/phase2-tabs';
import { test, NAME, resetFixture, openProject, SHOTS } from './support/workbench-a';

let page: Page;

test.beforeAll(async ({ browser }) => {
  page = await browser.newPage({ baseURL: process.env.BASE_URL });
  await signIn(page, 'Owner');
});
test.afterAll(async () => { await page.close(); });
test.beforeEach(async () => {
  // A verify-phase run builds and tests the fixture project for real (owner decision 1, option A), about a minute.
  test.setTimeout(420_000);
  resetFixture();
});

async function startVerifyPhase(): Promise<number> {
  await openProject(page);
  await openProcessesTab(page);
  return startProcess(page, 'verify-phase');
}

test('REQ-UI-047 the run shows its steps and stops where it must wait', async () => {
  await startVerifyPhase();
  await expect(page.getByTestId('run-heading')).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('run-process')).toHaveText('verify-phase');
  await expect(page.getByTestId('run-project')).toHaveText(NAME);
  await expect(page.getByTestId('run-steps-panel')).toBeVisible();
  await expect(page.getByTestId('run-step-1')).toBeVisible();
  await expect(page.getByTestId('run-step-1-state')).toHaveText(/done|check passed/);
  await expect(page.getByTestId('run-state')).toContainText('waiting for you', { timeout: 180_000 });
  await expect(page.getByTestId('question-panel')).toBeVisible();
  await expect(page.getByTestId('answer-yes')).toBeVisible();
  await page.screenshot({ path: join(SHOTS, 'process-run-waiting.png'), fullPage: true });
});

test('REQ-UI-047 a failed check stops the run and says which check and why', async () => {
  const vId = await startVerifyPhase();
  await expect(page.getByTestId('run-state')).toContainText('waiting for you', { timeout: 180_000 });
  // Make step 3 (a check) the one that failed and the run stopped on it; keep the casing the database already uses.
  const vRunKey = query("SELECT name FROM pragma_table_info('ProcessRun')").some((r) => r.name === 'ProcessRunId') ? 'ProcessRunId' : 'RunId';
  const vCols = query("SELECT name FROM pragma_table_info('ProcessRunStep')").map((r) => r.name as string);
  const vStepKey = vCols.includes('ProcessRunId') ? 'ProcessRunId' : 'RunId';
  const vPos = vCols.includes('Seq') ? 'Seq' : 'Position';
  const vSample = String(query('SELECT State FROM ProcessRun WHERE ' + vRunKey + '=?', vId)[0].State);
  const vCase = (aWord: string) => (vSample[0] === vSample[0].toLowerCase() ? aWord.toLowerCase() : aWord);
  run(`DELETE FROM ProcessRunStep WHERE ${vStepKey}=? AND ${vPos}>3`, vId);
  if (query(`SELECT 1 AS x FROM ProcessRunStep WHERE ${vStepKey}=? AND ${vPos}=3`, vId).length === 0) {
    run(`INSERT INTO ProcessRunStep (${vStepKey}, ${vPos}, State, Result, FinishedUtc) VALUES (?, 3, ?, ?, ?)`, vId, vCase('Failed'), 'x', new Date().toISOString());
  }
  run(`UPDATE ProcessRunStep SET State=?, Result=? WHERE ${vStepKey}=? AND ${vPos}=3`, vCase('Failed'), 'The build failed with 2 errors in src/Query.cs', vId);
  run('UPDATE ProcessRun SET State=?, EndedUtc=? WHERE ' + vRunKey + '=?', vCase('Stopped'), new Date().toISOString(), vId);
  // Close the run's tab and open the run again from Processes > Last run, as the owner would.
  await closeTab(page, `verify-phase · run ${vId}`);
  await tab(page, 'Processes').click();
  await expect(page.getByTestId('open-last-run')).toBeEnabled({ timeout: 30_000 });
  await page.getByTestId('open-last-run').click();
  await expect(page.getByTestId('run-state')).toContainText('stopped', { timeout: 30_000 });
  await expect(page.getByTestId('failed-check')).toBeVisible();
  await expect(page.getByTestId('failed-check-detail')).toContainText('Step 3');
  await expect(page.getByTestId('failed-check-why')).toContainText('The build failed with 2 errors in src/Query.cs');
  await expect(page.getByTestId('run-step-3-state')).toHaveText('check failed');
  await expect(page.getByTestId('question-panel')).toHaveCount(0);
  await expect(page.getByTestId('wrote-verdicts')).toContainText('Nothing was written');
  await page.screenshot({ path: join(SHOTS, 'process-run-failed.png'), fullPage: true });
});

test('REQ-UI-048 a finished run says the verdicts are in the checklist table', async () => {
  const vId = await startVerifyPhase();
  await expect(page.getByTestId('run-state')).toContainText('waiting for you', { timeout: 180_000 });
  // verify-phase has one owner stop (step 6); after the answer the button stays drawn but disabled
  // while the run goes on, so it is clicked once rather than in a loop.
  await page.getByTestId('answer-yes').click();
  await expect(page.getByTestId('run-state')).toContainText('finished', { timeout: 180_000 });
  await expect(page.getByTestId('question-panel')).toHaveCount(0);
  // The verdicts panel names the checklist the verdicts were written into (Fixture-P2-Checklist.md).
  await expect(page.getByTestId('wrote-verdicts')).toContainText(/Wrote \d+ verdicts? into docs\/Fixture-P2-Checklist\.md/);
  await page.screenshot({ path: join(SHOTS, 'process-run-finished.png'), fullPage: true });
  expect(vId).toBeGreaterThan(0);
});
