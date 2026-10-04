// Verify-phase acceptance tests for Start: REQ-UI-005..009 and REQ-FN-006 (mockups/start.html).
// Black-box: only data-testid selectors from the mockup / real Start + ProjectFoldersDialog markup.
import { test, expect, Page, Browser } from '@playwright/test';
import { mkdirSync, writeFileSync, existsSync } from 'fs';
import { resolve } from 'path';
import { signIn as sharedSignIn } from './support/auth';
import { openFoldersDialog } from './support/start';

/** The shared sign-in, retried once: App Manager sign-in is occasionally slower than its 30 s wait. */
async function signIn(aPage: Page, aUser: 'Owner'): Promise<void> {
  try {
    await sharedSignIn(aPage, aUser);
  } catch {
    await sharedSignIn(aPage, aUser);
  }
}

test.describe.configure({ timeout: 300_000 }); // one worker runs the file top to bottom; order matters (see below)

const FIXTURES = resolve(__dirname, '../.artifacts/verify/fixtures/start');
const ROOT = resolve(FIXTURES, 'root');       // AlphaApp (.sln), BetaTool (.csproj), ScratchNotes (plain folder)
const ROOT2 = resolve(FIXTURES, 'root2');     // GammaLib (plain folder)

test.beforeAll(() => {
  mkdirSync(resolve(ROOT, 'AlphaApp'), { recursive: true });
  mkdirSync(resolve(ROOT, 'BetaTool'), { recursive: true });
  mkdirSync(resolve(ROOT, 'ScratchNotes'), { recursive: true });
  mkdirSync(resolve(ROOT2, 'GammaLib'), { recursive: true });
  const put = (p: string, c: string) => { if (!existsSync(p)) writeFileSync(p, c); };
  put(resolve(ROOT, 'AlphaApp/AlphaApp.sln'), 'Microsoft Visual Studio Solution File, Format Version 12.00\n');
  put(resolve(ROOT, 'BetaTool/BetaTool.csproj'), '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>\n');
  put(resolve(ROOT, 'ScratchNotes/notes.txt'), 'hi\n');
  put(resolve(ROOT2, 'GammaLib/readme.txt'), 'x\n');
});

/**
 * Opens Start. When a project is already selected Start reopens straight into the Workbench
 * (REQ-FN-006), so neither Start nor the folders dialog can be reached: that state is NOT-TESTED
 * here, never a fake pass. (The tests that themselves select a project run last, so a clean
 * database is consumed in one run; re-runs need a fresh harness database.)
 */
async function openStart(aPage: Page): Promise<void> {
  // `?all=1` is how the owner reaches Start on purpose ("All projects…", fix 2026-09-30); a bare
  // `/start` still forwards to the Workbench when a project is selected (REQ-FN-006).
  await aPage.goto('/start?all=1', { waitUntil: 'networkidle' });
  try {
    await expect(aPage.getByTestId('recent-side')).toBeVisible({ timeout: 15_000 });
  } catch {
    const vRedirected = !/\/start/.test(aPage.url());
    test.skip(vRedirected, 'A project is already selected in the shared app database, so Start reopens straight into the Workbench (REQ-FN-006) and the Start screen cannot be reached. Needs a fresh harness database.');
    throw new Error('Start did not render (url ' + aPage.url() + ')');
  }
  await expect(aPage.getByTestId('recent-loading')).toHaveCount(0, { timeout: 30_000 });
}

async function openDialog(aPage: Page): Promise<void> {
  await openFoldersDialog(aPage);
}

async function addFolder(aPage: Page, aPath: string): Promise<void> {
  await aPage.getByTestId('new-folder-path').fill(aPath);
  await aPage.getByTestId('add-folder').click();
  await expect(aPage.getByTestId('folders-list').getByText(aPath, { exact: true })).toBeVisible();
}

async function closeDialog(aPage: Page): Promise<void> {
  await aPage.getByTestId('folders-dialog-close').click();
  await expect(aPage.getByTestId('folders-dialog')).toHaveCount(0);
}

/** Removes the fixture folders (only ours; other groups' folders are left alone) so each test starts without them. */
async function clearOwnFolders(aPage: Page): Promise<void> {
  await openStart(aPage);
  await openDialog(aPage);
  for (const vPath of [ROOT, ROOT2]) {
    const vRow = aPage.locator('[data-testid^="folder-row-"]').filter({ hasText: vPath });
    if ((await vRow.count()) > 0) {
      await vRow.locator('[data-testid^="remove-folder-"]').click();
      await expect(vRow).toHaveCount(0);
    }
  }
  await closeDialog(aPage);
}

async function freshSignedIn(aPage: Page): Promise<void> {
  await signIn(aPage, 'Owner');
  await clearOwnFolders(aPage);
}

async function nameRoot(aPage: Page, aPath = ROOT): Promise<void> {
  await openDialog(aPage);
  await addFolder(aPage, aPath);
  await closeDialog(aPage);
  await expect(aPage.getByTestId('recent-loading')).toHaveCount(0, { timeout: 30_000 });
}

async function signedInContext(aBrowser: Browser) {
  const vContext = await aBrowser.newContext({ baseURL: process.env.BASE_URL });
  const vPage = await vContext.newPage();
  await signIn(vPage, 'Owner');
  return { vContext, vPage };
}

/** The project's name cell (by visible name) inside the project list. */
function projectName(aPage: Page, aName: string) {
  return aPage.getByTestId('project-list').locator('[data-testid^="project-"][data-testid$="-name"]', { hasText: new RegExp(`^${aName}$`) });
}

test('REQ-UI-005 adding a folder in the Project folders dialog lists it on Projects', async ({ page }) => {
  await freshSignedIn(page);
  await openStart(page);
  await openDialog(page);
  await addFolder(page, ROOT);
  await closeDialog(page);
  // The folder now appears on Projects: the header counts it and the projects found in it are listed.
  await expect(page.getByTestId('recent-subtitle')).toContainText('named folder');
  await expect(page.getByTestId('recent-subtitle')).not.toContainText('No folders have been named yet');
  await expect(projectName(page, 'AlphaApp')).toBeVisible();
  // and it stays in the folder list when the dialog is reopened.
  await openDialog(page);
  await expect(page.getByTestId('folders-list').getByText(ROOT, { exact: true })).toBeVisible();
  await page.screenshot({ path: resolve(FIXTURES, 'ui-005.png') });
  await closeDialog(page);
  await clearOwnFolders(page);
});

test('REQ-UI-006 a named folder with projects lists each with name, path and last-opened', async ({ page }) => {
  await freshSignedIn(page);
  await nameRoot(page);
  await expect(page.getByTestId('project-list')).toBeVisible();
  for (const vName of ['AlphaApp', 'BetaTool', 'ScratchNotes']) {
    const vNameCell = projectName(page, vName);
    await expect(vNameCell, `${vName} listed once`).toHaveCount(1);
    const vTestId = (await vNameCell.getAttribute('data-testid'))!.replace(/-name$/, '');
    await expect(page.getByTestId(`${vTestId}-path`), `${vName} path`).toContainText(`${ROOT}/${vName}`);
    await expect(page.getByTestId(`${vTestId}-when`), `${vName} last opened`).toHaveText(/\S/);
  }
  await page.screenshot({ path: resolve(FIXTURES, 'ui-006.png') });
  await clearOwnFolders(page);
});

test('REQ-UI-008 with no folder named Projects shows an empty state whose button opens the dialog', async ({ page }) => {
  await freshSignedIn(page);
  await openStart(page);
  await openDialog(page);
  const vOthers = await page.locator('[data-testid^="folder-row-"]').count();
  await closeDialog(page);
  test.skip(vOthers > 0, 'Other test groups have named folders in the shared app database, so "no folder named" cannot be produced without deleting their data.');
  await expect(page.getByTestId('recent-subtitle')).toContainText('No folders have been named yet');
  await expect(page.getByTestId('project-list')).toHaveCount(0);
  const vButton = page.getByTestId('name-a-folder');
  await expect(vButton).toBeVisible();
  await openFoldersDialog(page, 'name-a-folder');
  await page.screenshot({ path: resolve(FIXTURES, 'ui-008.png') });
});

test('REQ-UI-009 removing a folder drops its projects and the folder stays gone after a restart', async ({ browser }) => {
  const { vContext, vPage } = await signedInContext(browser);
  try {
    await clearOwnFolders(vPage);
    await openDialog(vPage);
    await addFolder(vPage, ROOT);
    await addFolder(vPage, ROOT2);
    await closeDialog(vPage);
    await expect(projectName(vPage, 'GammaLib')).toBeVisible();
    await expect(projectName(vPage, 'AlphaApp')).toBeVisible();

    await openDialog(vPage);
    const vRow = vPage.locator('[data-testid^="folder-row-"]').filter({ hasText: ROOT2 });
    await vRow.locator('[data-testid^="remove-folder-"]').click();
    await expect(vPage.getByTestId('folders-list').getByText(ROOT2, { exact: true })).toHaveCount(0);
    await closeDialog(vPage);
    await expect(projectName(vPage, 'GammaLib')).toHaveCount(0);
    await expect(projectName(vPage, 'AlphaApp')).toBeVisible();
    await vPage.screenshot({ path: resolve(FIXTURES, 'ui-009.png') });
  } finally {
    await vContext.close();
  }
  // restart: brand-new browser session
  const { vContext: vContext2, vPage: vPage2 } = await signedInContext(browser);
  try {
    await openStart(vPage2);
    await expect(projectName(vPage2, 'GammaLib')).toHaveCount(0);
    await openDialog(vPage2);
    await expect(vPage2.getByTestId('folders-list').getByText(ROOT2, { exact: true })).toHaveCount(0);
    await expect(vPage2.getByTestId('folders-list').getByText(ROOT, { exact: true })).toBeVisible();
    await closeDialog(vPage2);
    await clearOwnFolders(vPage2);
  } finally {
    await vContext2.close();
  }
});

// The two tests below select a project, which makes Start reopen into the Workbench from then on,
// so they run last.
test('REQ-UI-007 opening a project from the list opens the Workbench with it selected', async ({ page }) => {
  await freshSignedIn(page);
  await nameRoot(page);
  await projectName(page, 'AlphaApp').click();
  await expect(page).toHaveURL(/\/$/, { timeout: 30_000 });
  await expect(page.getByTestId('conversation')).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('project-switch-name')).toHaveText('AlphaApp');
  await page.screenshot({ path: resolve(FIXTURES, 'ui-007.png') });
});

test('REQ-FN-006 the selected project is still selected after closing and opening again', async ({ browser }) => {
  const { vContext, vPage } = await signedInContext(browser);
  try {
    await openStart(vPage);
    await nameRoot(vPage);
    await projectName(vPage, 'BetaTool').click();
    await expect(vPage.getByTestId('project-switch-name')).toHaveText('BetaTool', { timeout: 30_000 });
  } finally {
    await vContext.close(); // "closed": every cookie and page gone
  }
  const { vContext: vContext2, vPage: vPage2 } = await signedInContext(browser); // "opened again"
  try {
    await vPage2.goto('/start', { waitUntil: 'networkidle' });
    await expect(vPage2.getByTestId('project-switch-name')).toHaveText('BetaTool', { timeout: 30_000 });
    await vPage2.screenshot({ path: resolve(FIXTURES, 'fn-006.png') });
  } finally {
    await vContext2.close();
  }
});
