// Acceptance tests: each account has its own Chatur, and a new account starts empty (BRD-210 to BRD-216;
// the owner's 17 Nov 2026 report, docs/screenshots/2026-11-17-first-use/Home-Screan-Broken.png: an account
// that had never used Chatur opened another account's project ChaturCheck, its files, a chat it never had
// and the line "Continuing after Analyst's last session", and Switch project listed ChaturCheck and
// SmokeProj). Black-box, web harness, signed in through the real App Manager. Where a state cannot be made
// through the screens — data kept before accounts had files of their own, or an account that has opened
// nothing — it is seeded into the database file, and every check is made on the screen.
import { test, expect, Browser, Page } from '@playwright/test';
import { DatabaseSync } from 'node:sqlite';
import { mkdirSync, rmSync, writeFileSync } from 'fs';
import { join, resolve } from 'path';
import { signIn } from './support/auth';
import { testUser } from './support/settings';
import { INSTALL_DB, run, snapshot, restore, Snapshot } from './support/db';
import { projectName, openSwitcher, editorText } from './support/workbench-a';
import { testProvider } from './support/provider';

const STAMP = Date.now();
const ROOT = resolve(__dirname, `../.artifacts/verify/fixtures/accounts-${STAMP}`);
const SHOTS = resolve(__dirname, '../.artifacts/verify/shots');

test.describe.configure({ timeout: 180_000 });

async function signedIn(aBrowser: Browser, aUser: 'Owner' | 'Second' = 'Owner'): Promise<Page> {
  const vContext = await aBrowser.newContext({ baseURL: process.env.BASE_URL });
  const vPage = await vContext.newPage();
  await signIn(vPage, aUser);
  return vPage;
}

/** A folder on disk with one file in it, a solution folder with a file deep inside it, and a loose file. */
function fixtures(): { folder: string; solution: string; solutionName: string; file: string; fileText: string } {
  const vFolder = join(ROOT, 'WayFolder');
  mkdirSync(vFolder, { recursive: true });
  writeFileSync(join(vFolder, 'readme.txt'), 'opened as a folder\n');
  const vSolution = join(ROOT, 'WaySolution');
  mkdirSync(join(vSolution, 'src'), { recursive: true });
  writeFileSync(join(vSolution, 'WaySolution.sln'), '\nMicrosoft Visual Studio Solution File, Format Version 12.00\n');
  const vFile = join(vSolution, 'src', 'Greeting.cs');
  const vText = `// opened as a file ${STAMP}\n`;
  writeFileSync(vFile, vText);
  return { folder: vFolder, solution: vSolution, solutionName: 'WaySolution', file: vFile, fileText: vText };
}

/**
 * Makes the signed-in account one that has opened nothing: no named folder, no project in its list and no
 * selected project, in its own database file. Returns what was there, to put back.
 */
function openedNothing(): Snapshot {
  const vSaved = snapshot(['ProjectFolder', 'Project', 'Setting']);
  run("DELETE FROM Setting WHERE Key = 'SelectedProjectId'");
  run("UPDATE Project SET ProjectFolderId = NULL, RemovedUtc = '2026-11-17T00:00:00.0000000Z'");
  run('DELETE FROM ProjectFolder');
  return vSaved;
}

/** Opens Start after the account's own state changed underneath it. */
async function freshStart(aPage: Page): Promise<void> {
  await aPage.goto('/start', { waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('empty-start')).toBeVisible({ timeout: 30_000 });
}

/**
 * Presses a way on until its dialog opens: the harness prerenders Start, and a click made before the
 * circuit attaches reaches no handler (support/start.ts openFoldersDialog has the same retry).
 */
async function chooseWay(aPage: Page, aTestId: string): Promise<void> {
  const vDialog = aPage.getByTestId('open-project-dialog');
  await expect(async () => {
    if (!(await vDialog.isVisible())) await aPage.getByTestId(aTestId).click();
    await expect(vDialog).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
}

async function submitOpenDialog(aPage: Page, aPath: string): Promise<void> {
  const vDialog = aPage.getByTestId('open-project-dialog');
  await expect(vDialog).toBeVisible({ timeout: 15_000 });
  await aPage.getByTestId('open-project-path').fill(aPath);
  await aPage.getByTestId('open-project-go').click();
  await expect(vDialog).toBeHidden({ timeout: 30_000 });
}

test.afterAll(() => {
  rmSync(ROOT, { recursive: true, force: true });
});

test('REQ-UI-093 REQ-UI-091 what the install kept before accounts is shown to no account that signs in', async ({ browser }) => {
  // The install's own file, as the build before this change left it on the owner's machine: a named
  // folder, a project chosen in it, and a chat with the Analyst — kept once for the install.
  const vName = `LegacyCheck${STAMP}`;
  const vLegacyPath = join(ROOT, vName);
  mkdirSync(vLegacyPath, { recursive: true });
  const vDb = new DatabaseSync(INSTALL_DB);
  vDb.exec('PRAGMA busy_timeout=10000');
  vDb.exec('PRAGMA foreign_keys=OFF');
  const vNow = new Date().toISOString();
  const vProjectId = Number(vDb.prepare("INSERT INTO Project (ProjectFolderId, Name, Path, Kind, LastOpenedUtc) VALUES (NULL, ?, ?, 'Folder', ?)").run(vName, vLegacyPath, vNow).lastInsertRowid);
  const vHadSelection = vDb.prepare("SELECT Value FROM Setting WHERE Key = 'SelectedProjectId'").get() as { Value: string } | undefined;
  vDb.prepare("INSERT INTO Setting (Key, Value) VALUES ('SelectedProjectId', ?) ON CONFLICT (Key) DO UPDATE SET Value = excluded.Value").run(String(vProjectId));
  const vRole = vDb.prepare("SELECT RoleId FROM Role WHERE Code = 'analyst'").get() as { RoleId: number };
  const vSessionId = Number(vDb.prepare("INSERT INTO Session (ProjectId, RoleId, Mode, State, StartedUtc) VALUES (?, ?, 'AskFirst', 'Active', ?)").run(vProjectId, vRole.RoleId, vNow).lastInsertRowid);
  vDb.prepare("INSERT INTO SessionEvent (SessionId, Seq, Kind, Payload, CreatedUtc) VALUES (?, 1, 'user', ?, ?)").run(vSessionId, `What commands do you have ? ${STAMP}`, vNow);
  vDb.close();

  const vPage = await signedIn(browser);
  try {
    await vPage.goto('/start?all=1', { waitUntil: 'networkidle' });
    await expect(vPage.getByTestId('recent-loading')).toHaveCount(0, { timeout: 30_000 });
    await expect(vPage.getByText(vName)).toHaveCount(0);

    await vPage.goto('/', { waitUntil: 'networkidle' });
    await expect(projectName(vPage)).not.toHaveText(vName, { timeout: 20_000 });
    await expect(vPage.getByText(`What commands do you have ? ${STAMP}`)).toHaveCount(0);
    if ((await vPage.getByTestId('project-switch').count()) > 0) {
      await openSwitcher(vPage);
      await expect(vPage.getByTestId('project-switch-pop').getByText(vName, { exact: true })).toHaveCount(0);
    }
    await vPage.screenshot({ path: `${SHOTS}/REQ-UI-093-nothing-kept-before.png` });
  } finally {
    await vPage.context().close();
    // The install's file is put back as it was; nothing the owner kept is touched.
    const vClean = new DatabaseSync(INSTALL_DB);
    vClean.exec('PRAGMA foreign_keys=OFF');
    vClean.prepare('DELETE FROM SessionEvent WHERE SessionId = ?').run(vSessionId);
    vClean.prepare('DELETE FROM Session WHERE SessionId = ?').run(vSessionId);
    vClean.prepare('DELETE FROM Project WHERE ProjectId = ?').run(vProjectId);
    if (vHadSelection) vClean.prepare("UPDATE Setting SET Value = ? WHERE Key = 'SelectedProjectId'").run(vHadSelection.Value);
    else vClean.prepare("DELETE FROM Setting WHERE Key = 'SelectedProjectId'").run();
    vClean.close();
  }
});

test('REQ-UI-008 REQ-UI-091 an account that has opened nothing gets the empty start with three plainly labelled ways on', async ({ browser }) => {
  const vPage = await signedIn(browser);
  const vSaved = openedNothing();
  try {
    await freshStart(vPage);
    const vStart = vPage.getByTestId('empty-start');
    await expect(vPage.getByTestId('empty-start-title')).toHaveText('Nothing is open yet');
    await expect(vStart.getByRole('button')).toHaveCount(4); // the three ways on, and the quiet Project folders
    await expect(vPage.getByTestId('way-open-folder')).toContainText('Open a folder');
    await expect(vPage.getByTestId('way-open-solution')).toContainText('Open a solution');
    await expect(vPage.getByTestId('way-open-file')).toContainText('Open a file');
    // No project, no file and no chat: no list of another's, no Workbench, no conversation.
    await expect(vPage.getByTestId('recent-list')).toHaveCount(0);
    await expect(vPage.getByTestId('get-started')).toHaveCount(0);
    await expect(vPage.getByTestId('menubar')).toHaveCount(0);
    await expect(vPage.getByTestId('conversation')).toHaveCount(0);
    await vPage.screenshot({ path: `${SHOTS}/REQ-UI-008-empty-start.png` });
  } finally {
    restore(vSaved);
    await vPage.context().close();
  }
});

test('REQ-UI-008 REQ-UI-094 each of the three ways on opens what it says, a file with the file open', async ({ browser }) => {
  const vPage = await signedIn(browser);
  const vWays = fixtures();
  const vSaved = openedNothing();
  try {
    // Open a folder
    await freshStart(vPage);
    await chooseWay(vPage, 'way-open-folder');
    await submitOpenDialog(vPage, vWays.folder);
    await expect(projectName(vPage)).toHaveText('WayFolder', { timeout: 30_000 });
    await expect(vPage.getByTestId('file-tree').getByText('readme.txt', { exact: true })).toBeVisible({ timeout: 20_000 });

    // Open a solution
    openedNothing();
    await freshStart(vPage);
    await chooseWay(vPage, 'way-open-solution');
    await submitOpenDialog(vPage, join(vWays.solution, 'WaySolution.sln'));
    await expect(projectName(vPage)).toHaveText(vWays.solutionName, { timeout: 30_000 });

    // Open a file: its solution's folder opens, with the file open in a tab
    openedNothing();
    await freshStart(vPage);
    await chooseWay(vPage, 'way-open-file');
    await expect(vPage.getByTestId('open-project-dialog')).toContainText('Open a file');
    await submitOpenDialog(vPage, vWays.file);
    await expect(projectName(vPage)).toHaveText(vWays.solutionName, { timeout: 30_000 });
    await expect(vPage.getByTestId('tabs')).toContainText('Greeting.cs', { timeout: 20_000 });
    await expect(editorText(vPage)).toHaveValue(vWays.fileText, { timeout: 20_000 });
    await vPage.screenshot({ path: `${SHOTS}/REQ-UI-094-open-a-file.png` });
  } finally {
    restore(vSaved);
    await vPage.context().close();
  }
});

test('REQ-UI-094 Start for an account with projects offers Open a file beside Open a solution and Open a folder', async ({ browser }) => {
  const vPage = await signedIn(browser);
  try {
    await vPage.goto('/start?all=1', { waitUntil: 'networkidle' });
    const vPanel = vPage.getByTestId('get-started');
    await expect(vPanel).toBeVisible({ timeout: 30_000 });
    await expect(vPanel.getByTestId('open-folder')).toContainText('Open a folder');
    await expect(vPanel.getByTestId('open-solution')).toContainText('Open a solution');
    await expect(vPanel.getByTestId('open-file')).toContainText('Open a file');
    await chooseWay(vPage, 'open-file');
    await expect(vPage.getByTestId('open-project-dialog')).toContainText('The file');
    await vPage.getByTestId('open-project-cancel').click();
  } finally {
    await vPage.context().close();
  }
});

test('REQ-UI-091 REQ-UI-095 REQ-FN-302 a second account signing in on the same install sees none of the first account', async ({ browser }) => {
  test.skip(!testUser('Second').onAppManager, 'test user 2 has no account on App Manager yet (UsageGuide "Test users"); the owner registers it');
  const vFirst = await signedIn(browser, 'Owner');
  const vWays = fixtures();
  await vFirst.goto('/start?all=1', { waitUntil: 'networkidle' });
  await chooseWay(vFirst, 'open-folder');
  await submitOpenDialog(vFirst, vWays.folder);
  await expect(projectName(vFirst)).toHaveText('WayFolder', { timeout: 30_000 });
  // The first account leaves a settings page open (the second window of the desktop app) on its own providers.
  const vSettings = await vFirst.context().newPage();
  await vSettings.goto('/settings/providers', { waitUntil: 'networkidle' });
  const vOwnersProvider = testProvider().name;
  await expect(vSettings.getByTestId('providers-table')).toContainText(vOwnersProvider, { timeout: 30_000 });

  const vSecond = await signedIn(browser, 'Second');
  try {
    await vSecond.goto('/start', { waitUntil: 'networkidle' });
    await expect(vSecond.getByTestId('empty-start')).toBeVisible({ timeout: 30_000 });
    await expect(vSecond.getByText('WayFolder')).toHaveCount(0);
    await expect(vSecond.getByText('Continuing after')).toHaveCount(0);
    // The settings page the first account left open is closed when the account changes: it no longer lists
    // the first account's provider (18 Nov walk, MISS-Chatur-20261117-04).
    await expect(vSettings.getByTestId('providers-table')).toHaveCount(0, { timeout: 30_000 });
    // And the second account's own providers are its own: none yet.
    await vSecond.goto('/settings/providers', { waitUntil: 'networkidle' });
    await expect(vSecond.getByTestId('providers-table').getByText(vOwnersProvider, { exact: true })).toHaveCount(0);
    await vSecond.screenshot({ path: `${SHOTS}/REQ-UI-095-second-account.png` });
  } finally {
    await vSecond.context().close();
    await vFirst.context().close();
  }
});
