// Acceptance tests: Processes (REQ-FN-049, REQ-FN-050, REQ-FN-051; mockups/processes.html). Black-box, web harness.
// The project is the private workbench fixture, opened through Start's own flow.
import { Page, expect } from '@playwright/test';
import { readdirSync } from 'fs';
import { join } from 'path';
import { signIn } from './support/auth';
import { query, run } from './support/db';
import { openProcessesTab, startProcess, tab } from './support/phase2-tabs';
import { ensureStubProvider, removeStubProvider, startStubModel, StubModel } from './support/stub-model';
import { test, NAME, PROJECT, resetFixture, openProject, write, SHOTS } from './support/workbench-a';

const COMMANDS = [
  'day1-greenfield', 'split-brd', 'build-phase', 'verify-phase', 'fix-issues', 'handoff-phase',
  'refresh-status', 'deploy-checklist', 'mockups', 'amend-docs', 'triage-issues', 'metrics-report',
];

let page: Page;
let stub: StubModel;

test.beforeAll(async ({ browser }) => {
  page = await browser.newPage({ baseURL: process.env.BASE_URL });
  await signIn(page, 'Owner');
  stub = await startStubModel('read-file', { path: `docs/${NAME}-P2-Checklist.md` });
  await ensureStubProvider(page, stub);
  // A run left behind by an earlier aborted run would show as "running now" and hide the last-run cell.
  run("UPDATE ProcessRun SET State='stopped', EndedUtc=datetime('now') WHERE State IN ('running','waiting') AND ProjectId=(SELECT ProjectId FROM Project WHERE Name=?)", NAME);
});
test.afterAll(async () => {
  await removeStubProvider(page);
  await stub.close();
  await page.close();
});
test.beforeEach(async () => {
  test.setTimeout(120_000);
  resetFixture();
  write(join(PROJECT, 'docs', `${NAME}-P2-BRD.md`), '# brd\n');
  write(join(PROJECT, 'docs', `${NAME}-P2-Checklist.md`), '# checklist\n');
});

async function openProcesses() {
  await openProject(page);
  await openProcessesTab(page);
}

function listing(aDir: string): string[] {
  return readdirSync(aDir, { recursive: true, encoding: 'utf8' }).map(String).sort();
}

test('REQ-FN-049 every command is listed with what it does and its shape', async () => {
  await openProcesses();
  for (const vCode of COMMANDS) {
    await expect(page.getByTestId(`row-${vCode}`)).toBeVisible();
    await expect(page.getByTestId(`row-${vCode}`)).not.toHaveText(new RegExp(`^\\s*${vCode}\\s*$`));
    await expect(page.getByTestId(`shape-${vCode}`)).toHaveText(/^\d+ · \d+ · \d+$/);
    await expect(page.getByTestId(`last-${vCode}`)).toBeVisible();
  }
  await expect(page.getByTestId('shape-verify-phase')).toHaveText('7 · 4 · 1');
  await page.screenshot({ path: join(SHOTS, 'processes-p2a.png'), fullPage: true });
});

test('REQ-FN-050 start opens Process run for the selected project on its first step', async () => {
  await openProcesses();
  const vId = await startProcess(page, 'build-phase');
  // The run opens as its own tab in the same window, the conversation still on the right.
  await expect(page.getByTestId('process-run')).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('chat-panel')).toBeVisible();
  const vRows = query('SELECT ProjectId, ProcessCode, StartedUtc FROM ProcessRun WHERE RunId=?', vId);
  expect(vRows).toHaveLength(1);
  expect(vRows[0].ProcessCode).toBe('build-phase');
  const vProject = query('SELECT ProjectId FROM Project WHERE Name=?', NAME);
  expect(vRows[0].ProjectId).toBe(vProject[0].ProjectId);
  // The start is remembered: Processes now shows when build-phase last ran.
  await expect(page.getByTestId('run-state')).toContainText(/stopped|finished/, { timeout: 180_000 });
  await tab(page, 'Processes').click();
  await expect(page.getByTestId('last-build-phase')).not.toHaveText(/^\s*[-–—]?\s*$/, { timeout: 30_000 });
});

test('REQ-FN-051 a process names the project own documents and leaves the folder as it was', async () => {
  const vBefore = listing(PROJECT);
  await openProcesses();
  await expect(page.getByTestId('what-a-process-touches')).toBeVisible();
  await expect(page.getByTestId('touch-brd')).toContainText(`docs/${NAME}-P2-BRD.md`);
  await expect(page.getByTestId('touch-checklist')).toContainText(`docs/${NAME}-P2-Checklist.md`);
  await expect(page.getByTestId('touch-status')).toContainText('PROJECT-STATUS.md');
  expect(listing(PROJECT)).toEqual(vBefore);
});
