// Acceptance tests: a process run whose work steps are handed to the process's agent (REQ-FN-084, REQ-FN-085, REQ-FN-086,
// REQ-FN-087, REQ-FN-088; mockups/process-run.html). Black-box, web harness. The model is the stub of support/stub-model.ts: its
// choice (one edit-file call, then a sentence) is fixed so the hand-over, the agent's rights and the check that judges the
// disk run the same way every time; everything else - the provider row, the agent loop, the guards, the engine - is real.
import { Page, expect } from '@playwright/test';
import { join } from 'path';
import { signIn } from './support/auth';
import { query } from './support/db';
import { openProcessesTab, startProcess } from './support/phase2-tabs';
import { ensureStubProvider, removeStubProvider, startStubModel, StubModel, STUB_MODEL } from './support/stub-model';
import { test, NAME, resetFixture, openProject, SHOTS } from './support/workbench-a';

let page: Page;
let stub: StubModel;

test.beforeAll(async ({ browser }) => {
  page = await browser.newPage({ baseURL: process.env.BASE_URL });
  await signIn(page, 'Owner');
  stub = await startStubModel('edit-file', { path: 'src/stub-note.md', content: 'A note the agent wrote.' });
  await ensureStubProvider(page, stub);
});
test.afterAll(async () => {
  await removeStubProvider(page);
  await stub.close();
  await page.close();
});
test.beforeEach(async () => {
  test.setTimeout(240_000);
  resetFixture();
});

test('REQ-FN-084 a work step goes to the process agent and the run shows who has it and with which model', async () => {
  await openProject(page);
  await openProcessesTab(page);
  const vRunId = await startProcess(page, 'refresh-status');
  await expect(page.getByTestId('run-heading')).toBeVisible({ timeout: 30_000 });

  // The agent only proposes a status document (the session asks first), so the check at the last step finds none on the disk and the run stops there with that reason.
  await expect(page.getByTestId('run-state')).toContainText('stopped', { timeout: 120_000 });
  await expect(page.getByTestId('run-step-1-result')).toContainText('The Flow master (' + STUB_MODEL + ')', { timeout: 30_000 });
  await expect(page.getByTestId('failed-check')).toBeVisible();
  await expect(page.getByTestId('failed-check-why')).toContainText('the status document does not exist', { timeout: 30_000 });
  await expect(page.getByTestId('run-step-4-state')).toHaveText('check failed');

  // The step the agent worked shows the agent, the model, and what it proposed (the session asks first, so nothing was written).
  const vSessions = query(
    "SELECT r.Name AS Agent, w.ModelName AS Model, w.State AS State FROM ProcessRunStepWork w JOIN Session s ON s.SessionId = w.SessionId JOIN Role r ON r.RoleId = s.RoleId WHERE w.RunId = ? ORDER BY w.Position",
    vRunId,
  );
  expect(vSessions.length).toBeGreaterThanOrEqual(2);
  expect(vSessions[0].Agent).toBe('Flow master');
  expect(String(vSessions[0].Model)).toBe(STUB_MODEL);
  expect(vSessions[0].State).toBe('finished');
  await page.screenshot({ path: join(SHOTS, 'process-step-work-stopped.png'), fullPage: true });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.screenshot({ path: join(SHOTS, 'process-step-work-stopped-390.png'), fullPage: true });
  await page.setViewportSize({ width: 1280, height: 800 });
});

test('REQ-FN-085 the agent that asks for a tool its rights withhold is refused and the refusal is recorded on the step', async () => {
  // The analyst owns day1-greenfield and holds writes documents only (docs/), not edit-file; the stub's first reply asks for edit-file on a source path (src/).
  await openProject(page);
  await openProcessesTab(page);
  const vRunId = await startProcess(page, 'day1-greenfield');
  await expect(page.getByTestId('run-heading')).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('run-state')).toContainText('waiting for you', { timeout: 120_000 });

  const vRefusals = query(
    "SELECT e.Payload AS Payload FROM SessionEvent e JOIN ProcessRunStepWork w ON w.SessionId = e.SessionId WHERE w.RunId = ? AND e.Kind = 'refusal'",
    vRunId,
  );
  expect(vRefusals.length).toBeGreaterThanOrEqual(1);
  expect(String(vRefusals[0].Payload)).toContain('edit-file');
  await expect(page.getByTestId('run-step-1-result')).toContainText('was refused 1 time');
  await page.screenshot({ path: join(SHOTS, 'process-step-work-refused.png'), fullPage: true });
});
