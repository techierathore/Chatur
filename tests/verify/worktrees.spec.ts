// Acceptance tests: Worktrees (REQ-FN-079, REQ-FN-080, REQ-UI-068; mockups/worktrees.html). Black-box, web harness.
// A throwaway git repository is built under tests/.artifacts/verify/fixtures/ and opened as the project through the
// app's own Start flow. Working copies are added and removed through the screen; the disk is checked directly.
import { test, expect as baseExpect, Page } from '@playwright/test';
import { execFileSync } from 'child_process';
import { existsSync, mkdirSync, rmSync, writeFileSync } from 'fs';
import * as path from 'path';
import { signIn } from './support/auth';
import { openFoldersDialog } from './support/start';
import { query, run } from './support/db';
import { ensureStubProvider, removeStubProvider } from './support/stub-model';
import { put, startScriptedModel } from './support/script-model';
import { dropSession } from './support/step-work';

test.setTimeout(120_000);
const expect = baseExpect.configure({ timeout: 30_000 });

const FIXTURES = path.resolve('tests/.artifacts/verify/fixtures');
const ROOT = path.join(FIXTURES, 'worktrees-spec', 'root');
const REPO = path.join(ROOT, 'wtspec-proj');
const COPIES = path.join(FIXTURES, 'worktrees-spec', 'copies'); // outside the project, as the dialog requires
const SHOTS = 'tests/.artifacts/verify';

function git(aCwd: string, ...aArgs: string[]): string {
  return execFileSync('git', aArgs, { cwd: aCwd, encoding: 'utf8' }).trim();
}

function buildFixture(): void {
  rmSync(path.join(FIXTURES, 'worktrees-spec'), { recursive: true, force: true });
  mkdirSync(REPO, { recursive: true });
  mkdirSync(COPIES, { recursive: true });
  git(REPO, 'init', '--initial-branch=main');
  git(REPO, 'config', 'user.name', 'Verify Fixture');
  git(REPO, 'config', 'user.email', 'verify-fixture@example.com');
  git(REPO, 'config', 'commit.gpgsign', 'false');
  writeFileSync(path.join(REPO, 'notes.txt'), 'first line\n');
  git(REPO, 'add', '.');
  git(REPO, 'commit', '-m', 'initial');
  git(REPO, 'branch', 'feat/copy-one');
  git(REPO, 'branch', 'feat/copy-two');
  git(REPO, 'branch', 'feat/copy-three');
}

async function selectFixtureProject(aPage: Page): Promise<void> {
  await aPage.goto('/', { waitUntil: 'networkidle' });
  const vName = aPage.getByTestId('project-switch-name');
  if (await vName.waitFor({ timeout: 10_000 }).then(() => true, () => false)) {
    if ((await vName.innerText()).trim() === 'wtspec-proj') return;
  }
  await aPage.goto('/start?all=1', { waitUntil: 'networkidle' });
  const vDialog = await openFoldersDialog(aPage);
  if ((await vDialog.getByText(ROOT, { exact: true }).count()) === 0) {
    await vDialog.getByTestId('new-folder-path').fill(ROOT);
    await vDialog.getByTestId('add-folder').click();
    await expect(vDialog.getByText(ROOT, { exact: true })).toBeVisible();
  }
  await vDialog.getByTestId('folders-dialog-close').click();
  await aPage.getByTestId(/^project-\d+-name$/).filter({ hasText: /^wtspec-proj$/ }).click();
  await expect(aPage).not.toHaveURL(/\/start/);
  // The choice is kept in the database; wait for the window to show it before another page load reads it back.
  await expect(aPage.getByTestId('project-switch-name')).toHaveText('wtspec-proj');
}

async function openWorktrees(aPage: Page): Promise<void> {
  await selectFixtureProject(aPage);
  await aPage.goto('/worktrees', { waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('page-heading')).toBeVisible();
  await expect(aPage.getByTestId('worktrees-lead')).toContainText('wtspec-proj');
}

// Adds a working copy through the dialog (branch pick, folder, Add). Fails with "not measured:" when the screen has no way to.
async function addCopy(aPage: Page, aBranch: string, aFolder: string): Promise<void> {
  const vAdd = aPage.getByTestId('add-worktree');
  if ((await vAdd.count()) === 0) {
    throw new Error('not measured: Worktrees draws no "Add a working copy" control (add-worktree), so a copy cannot be added from the screen.');
  }
  await vAdd.click();
  await expect(aPage.getByTestId('add-worktree-dialog')).toBeVisible();
  await aPage.getByTestId('new-wt-branch-button').click();
  await aPage.getByTestId('new-wt-branch-pop').getByText(aBranch, { exact: true }).click();
  await aPage.getByTestId('new-wt-path').fill(aFolder);
  await aPage.getByTestId('new-wt-add').click();
  await expect(aPage.getByTestId('add-worktree-dialog')).toBeHidden();
}

// The fixture project, its hidden working-copy projects, their runs, sessions and everything a run wrote about itself are
// removed here, and the named folder with them: the spec leaves no project row, no run row and no folder behind. A run's
// child rows go too, or the next run, which gets the same run id back, would find a hand-over that is already done.
function removeFixtureProjects(): void {
  const vProjects = query('SELECT ProjectId FROM Project WHERE Path = ? OR Path LIKE ? OR Path LIKE ?', REPO, `${COPIES}%`, `${REPO}%`).map((r) => Number(r.ProjectId));
  for (const vId of vProjects) {
    const vRuns = `SELECT RunId FROM ProcessRun WHERE ProjectId = ${vId}`;
    const vSessions = `SELECT SessionId FROM Session WHERE ProjectId = ${vId}`;
    for (const vTable of ['ProcessRunStep', 'ProcessRunVerdict', 'ProcessRunCommand', 'ProcessRunTest', 'ProcessRunStepWork', 'ProcessRunFile']) {
      run(`DELETE FROM ${vTable} WHERE RunId IN (${vRuns})`);
    }
    run(`DELETE FROM RunEstimate WHERE RunKind = 'process' AND RunId IN (${vRuns})`);
    run(`DELETE FROM SessionEvent WHERE SessionId IN (${vSessions})`);
    run(`DELETE FROM Change WHERE SessionId IN (${vSessions})`);
    run('DELETE FROM ProcessRun WHERE ProjectId = ?', vId);
    run('DELETE FROM Session WHERE ProjectId = ?', vId);
    run('DELETE FROM Project WHERE ProjectId = ?', vId);
  }
  // What an earlier, interrupted run of this spec left behind, which would be mistaken for the new run's own.
  for (const vTable of ['ProcessRunStep', 'ProcessRunVerdict', 'ProcessRunCommand', 'ProcessRunTest', 'ProcessRunStepWork', 'ProcessRunFile']) {
    run(`DELETE FROM ${vTable} WHERE RunId NOT IN (SELECT RunId FROM ProcessRun)`);
  }
  run('DELETE FROM ProjectFolder WHERE Path = ?', ROOT);
}

test.beforeAll(() => {
  try {
    removeFixtureProjects();
  } catch {
    // no account database yet on a fresh harness: there is nothing of an earlier run to remove
  }
  buildFixture();
});

test.afterAll(() => {
  try {
    removeFixtureProjects();
  } catch {
    // the database is not reachable any more; the folder is still removed below
  }
  rmSync(path.join(FIXTURES, 'worktrees-spec'), { recursive: true, force: true });
});

test.beforeEach(async ({ page }) => {
  await expect(async () => {
    await signIn(page, 'Owner');
  }).toPass({ timeout: 120_000, intervals: [500] });
});

test('REQ-FN-079 a working copy added for a branch is listed with its path and its branch', async ({ page }) => {
  // Scenario: open Worktrees on the fixture project, add a copy of branch feat/copy-one in a new empty folder,
  // then the table has a row showing that folder path and branch, and git lists the copy on disk.
  const vFolder = path.join(COPIES, 'one');
  await openWorktrees(page);
  await addCopy(page, 'feat/copy-one', vFolder);

  const vTable = page.getByTestId('worktrees-table');
  await expect(vTable).toBeVisible();
  const vRow = vTable.locator('tr', { hasText: vFolder });
  await expect(vRow).toBeVisible();
  await expect(vRow).toContainText('feat/copy-one');
  await page.screenshot({ path: `${SHOTS}/worktrees-fn079.png` });

  expect(existsSync(path.join(vFolder, 'notes.txt'))).toBe(true);
  expect(git(vFolder, 'rev-parse', '--abbrev-ref', 'HEAD')).toBe('feat/copy-one');
});

test('REQ-FN-080 a run started in a working copy changes files only in that copy, not in the owner\'s', async ({ page }) => {
  // Scenario: add a copy of feat/copy-three, press "Run here" on its row and pick refresh-status; the scripted model's agent
  // writes run-output.txt. The file is under the copy and not in the owner's project folder.
  test.setTimeout(300_000); // a run goes through a hand-over to the agent, which is slow on a busy machine
  const vFolder = path.join(COPIES, 'run');
  const vStub = await startScriptedModel((c) => (c.process === 'refresh-status' && c.step === 1 ? { calls: [put('run-output.txt', 'written by the run\n')], final: 'I wrote run-output.txt.' } : null));
  let vSession = 0;
  try {
    await ensureStubProvider(page, vStub);
    await openWorktrees(page);
    await addCopy(page, 'feat/copy-three', vFolder);
    const vRow = page.getByTestId('worktrees-table').locator('tr', { hasText: vFolder });
    await expect(vRow).toBeVisible();

    // The owner is on Go ahead in the copy, so the agent's write lands on disk rather than waiting for an approval.
    const vProject = Number(query('SELECT ProjectId FROM Project WHERE Path = ?', vFolder)[0].ProjectId);
    const vRole = Number(query("SELECT RoleId FROM Role WHERE Name='Flow master' ORDER BY Version DESC LIMIT 1")[0].RoleId);
    vSession = run('INSERT INTO Session (ProjectId, RoleId, Mode, State, StartedUtc) VALUES (?,?,?,?,?)', vProject, vRole, 'GoAhead', 'Active', new Date().toISOString());

    await vRow.getByRole('button', { name: 'Run here' }).click();
    await page.getByTestId(/^wt-start-.*-refresh-status$/).click();
    await expect(page.getByTestId('run-heading')).toBeVisible({ timeout: 60_000 });
    await expect.poll(() => existsSync(path.join(vFolder, 'run-output.txt')), { timeout: 200_000 }).toBe(true);
    await page.screenshot({ path: `${SHOTS}/worktrees-fn080.png` });

    expect(existsSync(path.join(REPO, 'run-output.txt'))).toBe(false);
    expect(git(REPO, 'status', '--porcelain')).toBe('');
    expect(git(vFolder, 'status', '--porcelain')).toContain('run-output.txt');
  } finally {
    dropSession(vSession);
    await removeStubProvider(page);
    await vStub.close();
  }
});

test('REQ-UI-068 removing a working copy takes it off the list and deletes its folder', async ({ page }) => {
  // Scenario: add a copy of feat/copy-two, remove it with its Remove button, then its row is gone from the list
  // and its folder no longer exists on disk.
  const vFolder = path.join(COPIES, 'two');
  await openWorktrees(page);
  await addCopy(page, 'feat/copy-two', vFolder);

  const vRow = page.getByTestId('worktrees-table').locator('tr', { hasText: vFolder });
  await expect(vRow).toBeVisible();
  expect(existsSync(vFolder)).toBe(true);
  await vRow.getByTestId(/^wt-remove-/).click();
  await page.getByTestId('remove-worktree-confirm').click();
  await page.waitForTimeout(1_500);
  await page.screenshot({ path: `${SHOTS}/worktrees-ui068-after-confirm.png` });

  await expect(page.getByTestId('worktrees-table').locator('tr', { hasText: vFolder })).toHaveCount(0);
  await page.screenshot({ path: `${SHOTS}/worktrees-ui068.png` });
  expect(existsSync(vFolder)).toBe(false);
});
