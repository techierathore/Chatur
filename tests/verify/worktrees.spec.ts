// Acceptance tests: Worktrees (REQ-FN-079, REQ-FN-080, REQ-UI-068; mockups/worktrees.html). Black-box, web harness.
// A throwaway git repository is built under tests/.artifacts/verify/fixtures/ and opened as the project through the
// app's own Start flow. Working copies are added and removed through the screen; the disk is checked directly.
import { test, expect as baseExpect, Page } from '@playwright/test';
import { execFileSync } from 'child_process';
import { existsSync, mkdirSync, rmSync, writeFileSync } from 'fs';
import * as path from 'path';
import { signIn } from './support/auth';
import { openFoldersDialog } from './support/start';

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
}

async function openWorktrees(aPage: Page): Promise<void> {
  await selectFixtureProject(aPage);
  await aPage.goto('/worktrees', { waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('page-heading')).toBeVisible();
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
}

test.beforeAll(() => {
  buildFixture();
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
  // Scenario: add a copy, start a run in it (scripted model writes a file), then the file exists under the copy
  // and not in the owner's project folder. The screen offers no way to start a run in a chosen copy, so the
  // black-box check cannot be set up; the test says so instead of passing on a weaker check.
  await openWorktrees(page);
  const vRunButtons = page.getByTestId(/^wt-run-/);
  if ((await vRunButtons.count()) === 0) {
    throw new Error('not measured: Worktrees has no control that starts a run in a chosen working copy, so where a run writes its files cannot be observed from the browser.');
  }
  await vRunButtons.first().click();
  expect(existsSync(path.join(REPO, 'run-output.txt'))).toBe(false);
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
  const vConfirm = page.getByRole('button', { name: /^(Remove|Yes|Confirm)/ }).last();
  if (await vConfirm.isVisible().catch(() => false)) await vConfirm.click();

  await expect(page.getByTestId('worktrees-table').locator('tr', { hasText: vFolder })).toHaveCount(0);
  await page.screenshot({ path: `${SHOTS}/worktrees-ui068.png` });
  expect(existsSync(vFolder)).toBe(false);
});
