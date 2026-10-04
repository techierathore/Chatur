// Acceptance tests: Repository (REQ-UI-042..044, REQ-FN-047, REQ-FN-048). Black-box, web harness.
// A throwaway git repository (plus a bare remote and a second clone) is built under
// tests/.artifacts/verify/fixtures/ and opened as the project through the app's own Start flow.
import { test, expect as baseExpect, Page } from '@playwright/test';
import { execFileSync } from 'child_process';
import { existsSync, mkdirSync, rmSync, writeFileSync, unlinkSync, readFileSync } from 'fs';
import * as path from 'path';
import { signIn } from './support/auth';
import { openFoldersDialog } from './support/start';

test.describe.configure({ mode: 'serial' });
test.setTimeout(120_000); // git on the Windows drive is slow

const expect = baseExpect.configure({ timeout: 30_000 }); // git on the Windows drive is slow

const FIXTURES = path.resolve('tests/.artifacts/verify/fixtures');
const ROOT = path.join(FIXTURES, 'repository-spec', 'root'); // the folder named on Start (scanned one level deep)
const REPO = path.join(ROOT, 'repospec-proj'); // the project under test
const REMOTE = path.join(FIXTURES, 'repository-spec', 'remote.git');
const OTHER = path.join(FIXTURES, 'repository-spec', 'other'); // a second clone that pushes "from elsewhere"
const SHOTS = 'tests/.artifacts/verify';

function git(aCwd: string, ...aArgs: string[]): string {
  return execFileSync('git', aArgs, { cwd: aCwd, encoding: 'utf8' }).trim();
}

function identify(aCwd: string): void {
  git(aCwd, 'config', 'user.name', 'Verify Fixture');
  git(aCwd, 'config', 'user.email', 'verify-fixture@example.com');
  git(aCwd, 'config', 'commit.gpgsign', 'false');
}

function buildFixture(): void {
  rmSync(ROOT, { recursive: true, force: true });
  rmSync(REMOTE, { recursive: true, force: true });
  rmSync(OTHER, { recursive: true, force: true });
  mkdirSync(REPO, { recursive: true });
  mkdirSync(path.dirname(REMOTE), { recursive: true });

  execFileSync('git', ['init', '--bare', '--initial-branch=main', REMOTE]);
  git(REPO, 'init', '--initial-branch=main');
  identify(REPO);
  writeFileSync(path.join(REPO, 'notes.txt'), 'first line\n');
  writeFileSync(path.join(REPO, 'obsolete.txt'), 'to be removed\n');
  git(REPO, 'add', '.');
  git(REPO, 'commit', '-m', 'initial');
  git(REPO, 'remote', 'add', 'origin', REMOTE);
  git(REPO, 'push', '-u', 'origin', 'main');

  git(REPO, 'checkout', '-b', 'feature/board');
  writeFileSync(path.join(REPO, 'board.txt'), 'board\n');
  git(REPO, 'add', '.');
  git(REPO, 'commit', '-m', 'board work');
  git(REPO, 'checkout', 'main');

  // Pending changes on main: one modified (+1/-0), one added (+2/-0), one deleted.
  writeFileSync(path.join(REPO, 'notes.txt'), 'first line\nsecond line\n');
  writeFileSync(path.join(REPO, 'new-feature.txt'), 'alpha\nbeta\n');
  unlinkSync(path.join(REPO, 'obsolete.txt'));
}

async function openFixtureAsProject(aPage: Page): Promise<void> {
  // Start is reached on purpose with ?all=1 even while a project is selected (bare /start forwards, REQ-FN-006).
  await aPage.goto('/start?all=1', { waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('recent-side')).toBeVisible();
  const vDialog = await openFoldersDialog(aPage);
  await expect(vDialog).toBeVisible();
  if ((await vDialog.getByText(ROOT, { exact: true }).count()) === 0) {
    await vDialog.getByTestId('new-folder-path').fill(ROOT);
    await vDialog.getByTestId('add-folder').click();
    await expect(vDialog.getByText(ROOT, { exact: true })).toBeVisible();
  }
  await vDialog.getByTestId('folders-dialog-close').click();
  await expect(vDialog).toBeHidden();
  const vRow = aPage.getByTestId(/^project-\d+-name$/).filter({ hasText: /^repospec-proj$/ });
  await expect(vRow).toBeVisible();
  await vRow.click();
  await expect(aPage).not.toHaveURL(/\/start/);
}

// The selected project is shared app state (other specs change it), so before each screen visit open the
// fixture through the real Start flow unless it is already the selected project.
async function selectFixtureProject(aPage: Page): Promise<void> {
  await aPage.goto('/', { waitUntil: 'networkidle' });
  const vName = aPage.getByTestId('project-switch-name');
  if (await vName.waitFor({ timeout: 10_000 }).then(() => true, () => false)) {
    if ((await vName.innerText()).trim() === 'repospec-proj') return;
  }
  await openFixtureAsProject(aPage);
}


async function openRepository(aPage: Page): Promise<void> {
  await selectFixtureProject(aPage);
  await aPage.goto('/repository', { waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('branch-row')).toBeVisible({ timeout: 30_000 });
  await expect(aPage.getByTestId('page-heading')).toContainText(REPO); // the selected project is the fixture
  await expect(aPage.getByTestId('page-heading')).not.toContainText('…', { timeout: 60_000 }); // git has answered
}

test.beforeAll(() => {
  buildFixture();
});

test.beforeEach(async ({ page }) => {
  // The Sign in form can drop typed text while the page is still connecting; try again rather than fail on that.
  await expect(async () => {
    await signIn(page, 'Owner');
  }).toPass({ timeout: 120_000, intervals: [500] });
});

test('REQ-UI-042 every changed file is listed with its state and choosing one shows its text', async ({ page }) => {
  await openFixtureAsProject(page);
  await openRepository(page);

  await expect(page.getByTestId('page-heading')).toContainText('Changes on main');
  await expect(page.getByTestId('changes-table')).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('change-state-notes.txt')).toHaveText('modified');
  await expect(page.getByTestId('change-added-notes.txt')).toHaveText('+1');
  await expect(page.getByTestId('change-state-new-feature.txt')).toHaveText('added');
  await expect(page.getByTestId('change-state-obsolete.txt')).toHaveText('deleted');

  await page.getByTestId('change-path-notes.txt').click();
  await expect(page.getByTestId('diff-file')).toHaveText('notes.txt');
  const vPanel = page.getByTestId('diff-panel');
  await expect(vPanel).toContainText('second line');
  await page.screenshot({ path: `${SHOTS}/repository-ui042.png` });

  await page.getByTestId('change-path-new-feature.txt').click();
  await expect(page.getByTestId('diff-file')).toHaveText('new-feature.txt');
  await expect(vPanel).toContainText('alpha');
  await expect(vPanel).toContainText('beta');
});

test('REQ-UI-043 checking in chosen files with a message commits them and empties the list of them', async ({ page }) => {
  await openFixtureAsProject(page);
  await openRepository(page);
  await expect(page.getByTestId('changes-table')).toBeVisible({ timeout: 30_000 });

  await expect(page.getByTestId('checkin-submit')).toBeDisabled();
  await page.getByTestId('change-path-notes.txt').click();
  await expect(page.getByTestId('chosen-count')).toContainText('1 of 3 chosen');
  await page.getByTestId('checkin-message').fill('[REQ-UI-043] verify check-in');
  await expect(page.getByTestId('checkin-submit')).toBeEnabled();
  await page.getByTestId('checkin-submit').click();

  await expect(page.getByTestId('change-path-notes.txt')).toHaveCount(0, { timeout: 30_000 });
  await expect(page.getByTestId('change-path-new-feature.txt')).toBeVisible();
  await expect(page.getByTestId('change-path-obsolete.txt')).toBeVisible();
  await page.screenshot({ path: `${SHOTS}/repository-ui043.png` });

  expect(git(REPO, 'log', '-1', '--format=%s')).toBe('[REQ-UI-043] verify check-in');
  expect(git(REPO, 'show', '--name-only', '--format=', 'HEAD')).toBe('notes.txt');
});

test('REQ-UI-044 push and pull show the result and how far ahead or behind the branch is', async ({ page }) => {
  await openFixtureAsProject(page);
  await openRepository(page);
  await expect(page.getByTestId('ahead')).toHaveText('1 to push', { timeout: 30_000 });
  await expect(page.getByTestId('behind')).toHaveText('0 to pull');

  await page.getByTestId('push').click();
  await expect(page.getByTestId('ahead')).toHaveText('0 to push', { timeout: 30_000 });
  await expect(page.getByTestId('behind')).toHaveText('0 to pull');
  expect(git(REMOTE, 'log', '-1', '--format=%s', 'main')).toBe('[REQ-UI-043] verify check-in');

  // Somebody else pushes to the server.
  execFileSync('git', ['clone', REMOTE, OTHER]);
  identify(OTHER);
  writeFileSync(path.join(OTHER, 'from-elsewhere.txt'), 'hello\n');
  git(OTHER, 'add', '.');
  git(OTHER, 'commit', '-m', 'from elsewhere');
  git(OTHER, 'push', 'origin', 'main');

  await openRepository(page);
  await expect(page.getByTestId('behind')).toHaveText('1 to pull', { timeout: 30_000 });
  await page.screenshot({ path: `${SHOTS}/repository-ui044-behind.png` });
  await page.getByTestId('pull').click();
  await expect(page.getByTestId('behind')).toHaveText('0 to pull', { timeout: 30_000 });
  await expect(page.getByTestId('ahead')).toHaveText('0 to push');
  expect(existsSync(path.join(REPO, 'from-elsewhere.txt'))).toBe(true);
});

test('REQ-FN-047 switching branch names it in the header and shows that branch\'s changed files', async ({ page }) => {
  await openFixtureAsProject(page);
  await openRepository(page);
  await expect(page.getByTestId('page-heading')).toContainText('Changes on main');

  await page.getByTestId('branch-current').click();
  const vPop = page.getByTestId('branch-pop');
  await expect(vPop.getByTestId('branch-option-main')).toBeVisible();
  await expect(vPop.getByTestId('branch-option-feature/board')).toBeVisible();
  await vPop.getByTestId('branch-option-feature/board').click();

  await expect(page.getByTestId('page-heading')).toContainText('Changes on feature/board', { timeout: 30_000 });
  await expect(page.getByTestId('branch-current')).toContainText('feature/board');
  expect(git(REPO, 'rev-parse', '--abbrev-ref', 'HEAD')).toBe('feature/board');
  expect(existsSync(path.join(REPO, 'board.txt'))).toBe(true);
  // board.txt is committed on this branch, so it is not a pending change here.
  await expect(page.getByTestId('change-path-board.txt')).toHaveCount(0);
  await expect(page.getByTestId('change-path-new-feature.txt')).toBeVisible();
  await page.screenshot({ path: `${SHOTS}/repository-fn047.png` });

  await page.getByTestId('branch-current').click();
  await page.getByTestId('branch-pop').getByTestId('branch-option-main').click();
  await expect(page.getByTestId('page-heading')).toContainText('Changes on main', { timeout: 30_000 });
  expect(git(REPO, 'rev-parse', '--abbrev-ref', 'HEAD')).toBe('main');
  expect(existsSync(path.join(REPO, 'board.txt'))).toBe(false);
});

test('REQ-UI-042 history marks a check-in only a process branch holds with the process icon', async ({ page }) => {
  // The mockup's history carries the "a process wrote it" pill with its icon; the screen draws it for any
  // check-in that only a run branch holds, which is visible once the owner stands on that branch.
  git(REPO, 'branch', 'run/REQ-FN-048-history');
  git(REPO, 'stash', '--include-untracked');
  git(REPO, 'checkout', 'run/REQ-FN-048-history');
  writeFileSync(path.join(REPO, 'run-work.txt'), 'by a process\n');
  git(REPO, 'add', '.');
  git(REPO, 'commit', '-m', '[REQ-FN-048] by a process');
  git(REPO, 'checkout', 'main');
  git(REPO, 'stash', 'pop');

  await openFixtureAsProject(page);
  await openRepository(page);
  await page.getByTestId('branch-current').click();
  await page.getByTestId('branch-pop').getByTestId('branch-option-run/REQ-FN-048-history').click();
  await expect(page.getByTestId('page-heading')).toContainText('run/REQ-FN-048-history', { timeout: 30_000 });

  const vMade = page.getByTestId('commit-message-1').locator('xpath=ancestor::tr').getByTestId('commit-made-1');
  await expect(vMade).toContainText('a process wrote it', { timeout: 30_000 });
  await expect(vMade.locator('svg')).toHaveCount(1);
  await expect(page.getByTestId('history-table').locator('svg').first()).toBeVisible();
  // The branches a process made carry the mockup's "Merge into main…" button with its icon (REQ-FN-048).
  const vMerge = page.getByTestId('process-branch-merge-1');
  await expect(vMerge).toContainText('Merge into main');
  await expect(vMerge.locator('svg')).toHaveCount(1);
  await page.getByTestId('history-panel').screenshot({ path: `${SHOTS}/repository-history-icon.png` });
  await page.getByTestId('process-branches-panel').screenshot({ path: `${SHOTS}/repository-process-branches.png` });
  await page.setViewportSize({ width: 390, height: 900 });
  await page.getByTestId('process-branches-panel').screenshot({ path: `${SHOTS}/repository-process-branches-390.png` });
  await page.setViewportSize({ width: 1280, height: 900 });

  await page.getByTestId('branch-current').click();
  await page.getByTestId('branch-pop').getByTestId('branch-option-main').click();
  await expect(page.getByTestId('page-heading')).toContainText('Changes on main', { timeout: 30_000 });
});

test('REQ-FN-048 an automatic check-in goes to its own branch', async () => {
  test.skip(
    true,
    'No screen shows this: the commit happens by itself when an agent run finishes, not from a control on ' +
      'Repository, so a browser cannot trigger or observe it. It is covered by the unit test ' +
      'RunBranchCheckInCommitsOnlyToTheRunsOwnBranch.',
  );
});
