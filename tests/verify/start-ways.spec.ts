// Verify-phase acceptance tests for tonight's Start changes (phase 3): REQ-UI-097 (the last project cannot
// open), REQ-UI-098 (centred icons on the ways in), REQ-UI-099 (the project list closes back to the Workbench),
// REQ-UI-100 (Open a solution's Browse asks for a solution file). Black-box, web harness, signed in as test
// user 2 ('Owner'), who has a Chatur of their own (BRD-210). Everything is checked on the screen, except
// what the harness only writes to its log (the picker request) and the restart, which is the harness process
// stopped and started again (support/restart.ts).
import { test, expect, Page } from '@playwright/test';
import { chmodSync, existsSync, mkdirSync, readFileSync, renameSync, writeFileSync } from 'fs';
import { lockFolder, removeProjectsUnder, scratchDir, unlockFolder } from './support/cleanup';
import { join, resolve } from 'path';
import { signIn } from './support/auth';
import { restartHarness } from './support/restart';

const STAMP = Date.now();
const ROOT = resolve(__dirname, `../.artifacts/verify/fixtures/start-ways-${STAMP}`);
const SHOTS = resolve(__dirname, '../.artifacts/verify/shots');
const PORT = new URL(process.env.BASE_URL ?? 'http://localhost:9263').port;
const APP_LOG = resolve(process.env.REPO || process.cwd(), `tests/.artifacts/verify/app-${PORT}.log`);

test.describe.configure({ timeout: 240_000 });

test.beforeAll(() => mkdirSync(SHOTS, { recursive: true }));

let objTemp = '';

// Every project this file made goes, with its folder and every row recorded against it (owner, 20 Nov 2026).
test.afterAll(() => {
  if (objTemp) {
    unlockFolder(join(objTemp, `Unreadable${STAMP}`));
    removeProjectsUnder(objTemp);
  }
  try {
    chmodSync(ROOT, 0o755);
  } catch {
    /* already gone */
  }
  removeProjectsUnder(ROOT);
});

/** A plain folder with one file in it, made fresh for each test. */
function folder(aName: string): string {
  const vPath = join(ROOT, `${aName}${STAMP}`);
  mkdirSync(vPath, { recursive: true });
  writeFileSync(join(vPath, 'readme.txt'), 'start ways fixture\n');
  return vPath;
}

async function signedIn(aPage: Page): Promise<void> {
  try {
    await signIn(aPage, 'Owner');
  } catch {
    await signIn(aPage, 'Owner');
  }
}

/** Opens the project list on purpose (`?all=1`), which shows even while a project is selected. */
async function openList(aPage: Page): Promise<void> {
  await aPage.goto('/start?all=1', { waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('empty-start').or(aPage.getByTestId('recent-side'))).toBeVisible({ timeout: 30_000 });
  await expect(aPage.getByTestId('recent-loading')).toHaveCount(0, { timeout: 30_000 });
}

/**
 * Presses a way on until its dialog opens: the harness prerenders Start, and a click made before the circuit
 * attaches reaches no handler (the same retry as support/start.ts openFoldersDialog).
 */
async function chooseWay(aPage: Page, aTestId: string): Promise<void> {
  const vDialog = aPage.getByTestId('open-project-dialog');
  await expect(async () => {
    if (!(await vDialog.isVisible())) await aPage.getByTestId(aTestId).click();
    await expect(vDialog).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
}

/** Opens a folder as a project through Open a folder, whichever Start (empty or listed) the account has. */
async function openFolderProject(aPage: Page, aPath: string, aName: string): Promise<void> {
  await openList(aPage);
  await chooseWay(aPage, (await aPage.getByTestId('empty-start').isVisible()) ? 'way-open-folder' : 'open-folder');
  await aPage.getByTestId('open-project-path').fill(aPath);
  await aPage.getByTestId('open-project-go').click();
  await expect(aPage.getByTestId('open-project-dialog')).toBeHidden({ timeout: 30_000 });
  await expect(aPage.getByTestId('project-switch-name')).toHaveText(aName, { timeout: 30_000 });
}

/** Closes and opens Chatur again: the harness is stopped and started on the same data, and the account signs in again. */
async function restartAndSignIn(aPage: Page): Promise<void> {
  await aPage.goto('about:blank'); // the old circuit is left, so it cannot reconnect over the sign-in
  await restartHarness();
  await aPage.context().clearCookies();
  await signedIn(aPage);
}

test('REQ-UI-097 when the last project cannot open as Chatur starts, Start says which project and why and nothing stops', async ({ page }) => {
  await signedIn(page);
  // The unreadable folder lives in this test project's own bin folder, made unreadable by a Windows deny rule:
  // chmod has no effect on the /mnt/c drive the repository sits on (owner, 20 Nov 2026: no file outside the repository).
  const vTemp = scratchDir(`start-ways-${STAMP}`);
  objTemp = vTemp;
  const vPath = join(vTemp, `Unreadable${STAMP}`);
  const vName = `Unreadable${STAMP}`;
  mkdirSync(vPath);
  writeFileSync(join(vPath, 'readme.txt'), 'start ways fixture\n');
  await openFolderProject(page, vPath, vName);

  // 1. The folder cannot be read: Chatur starts again.
  lockFolder(vPath);
  try {
    await restartAndSignIn(page);
    await page.goto('/start', { waitUntil: 'networkidle' });
    const vLine = page.getByTestId('reopen-failed');
    await expect(vLine).toBeVisible({ timeout: 30_000 });
    await expect(vLine).toContainText(`Chatur could not open ${vName} last time`);
    await expect(vLine).toContainText(vPath);
    await expect(page).toHaveURL(/\/start/);
    await expect(page.getByTestId('conversation')).toHaveCount(0);
    await expect(page.getByTestId('menubar')).toHaveCount(0);
    await page.screenshot({ path: `${SHOTS}/REQ-UI-097-unreadable.png` });
  } finally {
    unlockFolder(vPath);
  }

  // The stored selection is kept, so once the folder can be read again Chatur opens it by itself.
  await page.goto('/start', { waitUntil: 'networkidle' });
  await expect(page).toHaveURL(/\/$/, { timeout: 20_000 });
  await expect(page.getByTestId('project-switch-name')).toHaveText(vName, { timeout: 20_000 });
  await openFolderProject(page, vPath, vName);

  // 2. The folder is gone.
  const vMoved = `${vPath}-moved`;
  renameSync(vPath, vMoved);
  try {
    await restartAndSignIn(page);
    await page.goto('/start', { waitUntil: 'networkidle' });
    const vLine = page.getByTestId('reopen-failed');
    await expect(vLine).toBeVisible({ timeout: 30_000 });
    await expect(vLine).toContainText(`Chatur could not open ${vName} last time`);
    await expect(vLine).toContainText('no longer there');
    await expect(page).toHaveURL(/\/start/);
    await expect(page.getByTestId('conversation')).toHaveCount(0);
    await page.screenshot({ path: `${SHOTS}/REQ-UI-097-gone.png` });
  } finally {
    renameSync(vMoved, vPath);
    removeProjectsUnder(vTemp);
  }
});

test('REQ-UI-098 each way in on Start shows a centred icon for its action: branch, solution, folder, file, plus', async ({ page }) => {
  await signedIn(page);
  await openList(page);

  const centred = async (aTile: string, aIcon: string) => {
    const vTile = page.getByTestId(aTile);
    const vSlot = vTile.locator(`[data-icon="${aIcon}"]`);
    await expect(vSlot, `${aTile} icon slot`).toHaveCount(1);
    const vSvg = vSlot.locator('svg');
    await expect(vSvg, `${aTile} svg`).toBeVisible();
    const vBox = (await vSlot.boundingBox())!;
    const vGlyph = (await vSvg.boundingBox())!;
    expect(Math.abs(vBox.x + vBox.width / 2 - (vGlyph.x + vGlyph.width / 2)), `${aTile} horizontal centre`).toBeLessThanOrEqual(1.5);
    expect(Math.abs(vBox.y + vBox.height / 2 - (vGlyph.y + vGlyph.height / 2)), `${aTile} vertical centre`).toBeLessThanOrEqual(1.5);
  };

  // The empty start has the four ways on of an account that has opened nothing (New project first since 22 Nov 2026).
  if (await page.getByTestId('empty-start').isVisible()) {
    await centred('way-new-project', 'plus');
    await centred('way-open-solution', 'layers');
    await centred('way-open-folder', 'folder');
    await centred('way-open-file', 'file');
    await page.screenshot({ path: `${SHOTS}/REQ-UI-098-empty-start.png` });
    await openFolderProject(page, folder('Icons'), `Icons${STAMP}`);
    await openList(page);
  }

  // Start for an account with projects has all five.
  await expect(page.getByTestId('get-started')).toBeVisible();
  await centred('clone-repo', 'git-branch');
  await centred('open-solution', 'layers');
  await centred('open-folder', 'folder');
  await centred('open-file', 'file');
  await centred('new-project', 'plus');
  await page.screenshot({ path: `${SHOTS}/REQ-UI-098-ways-in.png` });
});

test('REQ-UI-099 closing the project list with its close button or Escape shows the Workbench it came from, unchanged', async ({ page }) => {
  await signedIn(page);
  const vName = `Closes${STAMP}`;
  await openFolderProject(page, folder('Closes'), vName);
  await expect(page.getByTestId('file-tree').getByText('readme.txt', { exact: true })).toBeVisible({ timeout: 20_000 });

  // Since 21 Nov 2026 the switcher has no "All projects" (BRD-239): the project list opens from File ▸ Recent projects.
  const fromSwitcher = async () => {
    await expect(async () => {
      await page.getByTestId('menu-file').click();
      await page.getByTestId('menu-file-recent').click();
      await expect(page.getByTestId('menu-file-project-list')).toBeVisible({ timeout: 2_500 });
    }).toPass({ timeout: 20_000 });
    await page.getByTestId('menu-file-project-list').click();
    await expect(page).toHaveURL(/\/start\?all=1/, { timeout: 20_000 });
    await expect(page.getByTestId('start-close')).toBeVisible({ timeout: 30_000 });
  };
  const backAgain = async () => {
    await expect(page).toHaveURL(/\/$/, { timeout: 20_000 });
    await expect(page.getByTestId('project-switch-name')).toHaveText(vName, { timeout: 20_000 });
    await expect(page.getByTestId('conversation')).toBeVisible({ timeout: 20_000 });
    await expect(page.getByTestId('file-tree').getByText('readme.txt', { exact: true })).toBeVisible({ timeout: 20_000 });
  };

  // The close button
  await fromSwitcher();
  await page.screenshot({ path: `${SHOTS}/REQ-UI-099-list.png` });
  await page.getByTestId('start-close').click();
  await backAgain();

  // Escape
  await fromSwitcher();
  await expect(async () => {
    await page.keyboard.press('Escape');
    await expect(page).toHaveURL(/\/$/, { timeout: 2_000 });
  }).toPass({ timeout: 20_000 });
  await backAgain();
  await page.screenshot({ path: `${SHOTS}/REQ-UI-099-back.png` });
});

test('REQ-UI-100 Browse in Open a solution asks for a solution file, Open a folder still asks for a folder, and the path stays typeable', async ({ page }) => {
  await signedIn(page);
  await openList(page);
  const vSolutionWay = (await page.getByTestId('empty-start').isVisible()) ? 'way-open-solution' : 'open-solution';
  const vFolderWay = (await page.getByTestId('empty-start').isVisible()) ? 'way-open-folder' : 'open-folder';

  const logText = () => (existsSync(APP_LOG) ? readFileSync(APP_LOG, 'utf8') : '');
  const pressBrowse = async (aLine: string) => {
    const vBefore = logText().split(aLine).length - 1;
    const vBrowse = page.getByTestId('open-project-browse');
    await expect(vBrowse).toBeVisible();
    await expect(async () => {
      await vBrowse.click();
      await expect.poll(() => logText().split(aLine).length - 1, { timeout: 3_000 }).toBeGreaterThan(vBefore);
    }).toPass({ timeout: 20_000 });
  };
  const typeable = async (aPath: string) => {
    const vPathField = page.getByTestId('open-project-path');
    await vPathField.fill(aPath);
    await expect(vPathField).toHaveValue(aPath);
    await expect(page.getByTestId('open-project-go')).toBeEnabled();
  };

  await chooseWay(page, vSolutionWay);
  await expect(page.getByTestId('open-project-dialog')).toContainText('Open a solution');
  await pressBrowse('File picker requested (Open a solution) for .sln, .slnx');
  await typeable(join(ROOT, 'Typed.sln'));
  await page.screenshot({ path: `${SHOTS}/REQ-UI-100-solution-browse.png` });
  await page.getByTestId('open-project-cancel').click();
  await expect(page.getByTestId('open-project-dialog')).toBeHidden();

  await chooseWay(page, vFolderWay);
  await expect(page.getByTestId('open-project-dialog')).toContainText('Open a folder');
  await pressBrowse('Folder picker requested (Open a folder)');
  await typeable(ROOT);
  await page.getByTestId('open-project-cancel').click();
});
