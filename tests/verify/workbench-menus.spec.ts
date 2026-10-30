// Acceptance tests: the Workbench menu bar and the panels it shows and hides (REQ-UI-073, REQ-UI-074),
// and the conversation drawing each step where it happened (REQ-UI-023). Black-box, web harness: every
// menu item is clicked and its effect looked at (a file on disk, a panel gone and back, a page reached).
import { Page, expect } from '@playwright/test';
import { execFileSync } from 'child_process';
import { existsSync, mkdirSync, readFileSync, readdirSync, rmSync, writeFileSync } from 'fs';
import { join } from 'path';
import { tmpdir } from 'os';
import { signIn } from './support/auth';
import { run, query } from './support/db';
import {
  test, NAME, PROJECT, ROOT, NOTES, resetFixture, openProject, openFile, tabs, editorText, projectName,
} from './support/workbench-a';

let page: Page;
const STAMP = Date.now();

test.beforeAll(async ({ browser }) => {
  test.setTimeout(120_000);
  const vContext = await browser.newContext({ baseURL: process.env.BASE_URL, permissions: ['clipboard-read', 'clipboard-write'] });
  page = await vContext.newPage();
  await signIn(page, 'Owner');
});
test.afterAll(async () => { await page.close(); });
test.beforeEach(async () => {
  test.setTimeout(120_000);
  resetFixture();
  await openProject(page);
  await expect(page.getByTestId('chat-panel')).toBeVisible({ timeout: 20_000 });
});

/** Opens one of the menu bar's menus (a click made before the circuit is interactive is retried). */
async function openMenu(aPage: Page, aMenu: string): Promise<void> {
  const vItems = aPage.getByTestId(`menu-${aMenu}-items`);
  await expect(async () => {
    if (!(await vItems.isVisible())) await aPage.getByTestId(`menu-${aMenu}`).click();
    await expect(vItems).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
}

/** Opens a menu and clicks one of its items. */
async function pick(aPage: Page, aMenu: string, aItem: string): Promise<void> {
  await openMenu(aPage, aMenu);
  await aPage.getByTestId(`menu-${aMenu}-${aItem}`).click();
}

/** MenubarItem draws Disabled as `pointer-events-none opacity-50` (it sets no aria-disabled), so that is what "off" is. */
const OFF = /pointer-events-none/;
const expectOff = (aItem: ReturnType<Page['getByTestId']>) => expect(aItem).toHaveClass(OFF);
const expectOn = (aItem: ReturnType<Page['getByTestId']>) => expect(aItem).not.toHaveClass(OFF);

const noPlaceholder = async (aPage: Page, aMenu: string) => {
  await openMenu(aPage, aMenu);
  await expect(aPage.getByTestId(`menu-${aMenu}-items`)).not.toContainText('Nothing to do yet');
  await aPage.keyboard.press('Escape');
};

/** Fills the File-menu project dialog and submits it. */
async function submitProjectDialog(aPage: Page, aFields: { path: string; name?: string; address?: string }): Promise<void> {
  const vDialog = aPage.getByTestId('open-project-dialog');
  await expect(vDialog).toBeVisible();
  if (aFields.address !== undefined) await aPage.getByTestId('open-project-address').fill(aFields.address);
  if (aFields.name !== undefined) await aPage.getByTestId('open-project-name').fill(aFields.name);
  await aPage.getByTestId('open-project-path').fill(aFields.path);
  await aPage.getByTestId('open-project-go').click();
}

test('REQ-UI-073 File, Edit, View and Help each hold their items and none says Nothing to do yet', async () => {
  const vWanted: Record<string, string[]> = {
    file: ['New project', 'Open a solution', 'Open a folder', 'Clone a repository', 'Recent projects', 'Save', 'Save all', 'Close project', 'Exit'],
    edit: ['Undo', 'Redo', 'Cut', 'Copy', 'Paste', 'Find in files', 'Go to file'],
    view: ['Files', 'Chat', 'Output', 'Prerequisites', 'Repository', 'Appearance', 'Zoom in', 'Zoom out', 'Actual size'],
    help: ['About Chatur', 'Send feedback', 'Check for updates'],
  };
  for (const [vMenu, vItems] of Object.entries(vWanted)) {
    await openMenu(page, vMenu);
    const vPop = page.getByTestId(`menu-${vMenu}-items`);
    for (const vItem of vItems) await expect(vPop).toContainText(vItem);
    await expect(vPop, 'no menu item ends in an ellipsis (owner, 2026-10-29)').not.toContainText('…');
    await page.keyboard.press('Escape');
  }
  for (const vMenu of ['file', 'edit', 'view', 'help', 'project', 'tools']) await noPlaceholder(page, vMenu);
});

test('REQ-UI-073 File ▸ Open a folder opens the folder as the project', async () => {
  const vFolder = join(ROOT, `MenuFolder${STAMP}`);
  mkdirSync(vFolder, { recursive: true });
  writeFileSync(join(vFolder, 'hello.txt'), 'hi\n');
  await pick(page, 'file', 'open-folder');
  await submitProjectDialog(page, { path: vFolder });
  await expect(projectName(page)).toHaveText(`MenuFolder${STAMP}`, { timeout: 20_000 });
  await expect(page.getByTestId('file-tree').getByText('hello.txt', { exact: true })).toBeVisible({ timeout: 20_000 });
  // a path that is not there is refused with a reason, and nothing changes
  await pick(page, 'file', 'open-folder');
  await submitProjectDialog(page, { path: join(ROOT, 'no-such-folder') });
  await expect(page.getByTestId('open-project-error')).toContainText('does not exist');
  await page.getByTestId('open-project-cancel').click();
  await expect(projectName(page)).toHaveText(`MenuFolder${STAMP}`);
});

test('REQ-UI-073 File ▸ Open a solution opens the .sln as the project', async () => {
  const vFolder = join(ROOT, `MenuSln${STAMP}`);
  mkdirSync(vFolder, { recursive: true });
  writeFileSync(join(vFolder, `MenuSln${STAMP}.sln`), '\nMicrosoft Visual Studio Solution File, Format Version 12.00\n');
  writeFileSync(join(vFolder, 'solution-notes.txt'), 'in the solution folder\n');
  await pick(page, 'file', 'open-solution');
  await submitProjectDialog(page, { path: join(vFolder, `MenuSln${STAMP}.sln`) });
  await expect(projectName(page)).toHaveText(`MenuSln${STAMP}`, { timeout: 20_000 });
  await expect(page.getByTestId('file-tree').getByText('solution-notes.txt', { exact: true })).toBeVisible({ timeout: 20_000 });
  // a folder with no solution in it is refused
  await pick(page, 'file', 'open-solution');
  await submitProjectDialog(page, { path: ROOT });
  await expect(page.getByTestId('open-project-error')).toContainText('no solution');
  await page.getByTestId('open-project-cancel').click();
});

test('REQ-UI-073 File ▸ New project makes a solution on disk and opens it', async () => {
  const vName = `MenuNew${STAMP}`;
  await pick(page, 'file', 'new');
  await submitProjectDialog(page, { path: ROOT, name: vName });
  await expect(projectName(page)).toHaveText(vName, { timeout: 60_000 });
  const vFiles = readdirSync(join(ROOT, vName));
  expect(vFiles.some((f) => /\.slnx?$/.test(f)), `a solution file in ${vFiles.join(', ')}`).toBe(true);
});

test('REQ-UI-073 File ▸ Clone a repository copies it and opens it; a bad address is refused with the reason', async () => {
  const vSource = join(tmpdir(), `chatur-clone-src-${STAMP}`);
  mkdirSync(vSource, { recursive: true });
  writeFileSync(join(vSource, 'cloned.txt'), 'from the source repository\n');
  const vGit = (...aArgs: string[]) => execFileSync('git', ['-C', vSource, '-c', 'user.name=t', '-c', 'user.email=t@t', ...aArgs], { stdio: 'ignore' });
  vGit('init', '-q');
  vGit('add', '.');
  vGit('commit', '-q', '-m', 'seed');
  const vTarget = join(ROOT, `clone-parent-${STAMP}`);
  mkdirSync(vTarget, { recursive: true });

  await pick(page, 'file', 'clone');
  await submitProjectDialog(page, { address: join(tmpdir(), 'chatur-no-such-repo'), path: vTarget });
  await expect(page.getByTestId('open-project-error')).toContainText('failed', { timeout: 30_000 });

  await submitProjectDialog(page, { address: vSource, path: vTarget });
  await expect(projectName(page)).toHaveText(`chatur-clone-src-${STAMP}`, { timeout: 60_000 });
  await expect(page.getByTestId('file-tree').getByText('cloned.txt', { exact: true })).toBeVisible({ timeout: 20_000 });
  rmSync(vSource, { recursive: true, force: true });
});

test('REQ-UI-073 File ▸ Recent projects lists what was opened and switching to one works', async () => {
  const vFolder = join(ROOT, `MenuRecent${STAMP}`);
  mkdirSync(vFolder, { recursive: true });
  await pick(page, 'file', 'open-folder');
  await submitProjectDialog(page, { path: vFolder });
  await expect(projectName(page)).toHaveText(`MenuRecent${STAMP}`, { timeout: 20_000 });
  await openMenu(page, 'file');
  const vPop = page.getByTestId('menu-file-items');
  await expect(vPop).toContainText(NAME);
  await vPop.getByText(NAME, { exact: true }).click();
  await expect(projectName(page)).toHaveText(NAME, { timeout: 20_000 });
});

test('REQ-UI-073 File ▸ Save and Save all write the open files to disk, and are off when nothing has changed', async () => {
  await openFile(page, 'Notes/notes.txt');
  await openFile(page, 'Program.cs');
  await openMenu(page, 'file');
  await expectOff(page.getByTestId('menu-file-save'));
  await expectOff(page.getByTestId('menu-file-save-all'));
  await page.keyboard.press('Escape');

  // Save: only the file in front
  const vProgram = join(PROJECT, 'Program.cs');
  await editorText(page).click();
  await editorText(page).press('Control+End');
  await page.keyboard.type('\n// saved from the menu');
  await expect(tabs(page)).toContainText('Program.cs');
  await expect.poll(async () => page.getByTestId('save-file').isEnabled(), { timeout: 10_000 }).toBe(true);
  await openMenu(page, 'file');
  await expectOn(page.getByTestId('menu-file-save'));
  await page.getByTestId('menu-file-save').click();
  await expect.poll(() => readFileSync(vProgram, 'utf8'), { timeout: 10_000 }).toContain('// saved from the menu');
  await expect(page.getByTestId('save-file')).toBeDisabled();

  // Save all: two changed files
  await editorText(page).click();
  await editorText(page).press('Control+End');
  await page.keyboard.type('\n// second change');
  await page.waitForTimeout(600); // the editor reports a change 200 ms after the last key
  await page.getByTestId('tab-notes').click();
  await editorText(page).click();
  await editorText(page).press('Control+End');
  await page.keyboard.type('beta line');
  await expect.poll(async () => page.getByTestId('save-file').isEnabled(), { timeout: 10_000 }).toBe(true);
  await pick(page, 'file', 'save-all');
  await expect.poll(() => readFileSync(vProgram, 'utf8'), { timeout: 10_000 }).toContain('// second change');
  await expect.poll(() => readFileSync(join(PROJECT, 'Notes', 'notes.txt'), 'utf8'), { timeout: 10_000 }).toContain('beta line');
});

test('REQ-UI-073 File ▸ Close project leaves the project and shows the project list', async () => {
  await pick(page, 'file', 'close');
  await expect(page).toHaveURL(/\/start/, { timeout: 20_000 });
  await expect(page.getByTestId('recent-list')).toBeVisible();
  // and nothing is reopened by itself
  await page.goto('/', { waitUntil: 'networkidle' });
  await expect(projectName(page)).toHaveText('No project', { timeout: 20_000 });
  await openProject(page);
});

test('REQ-UI-073 Start ▸ Clone, Open a solution, Open a folder and New project each open their dialog, and Open a folder opens the project', async () => {
  await pick(page, 'file', 'close');
  await expect(page).toHaveURL(/\/start/, { timeout: 20_000 });
  const vDialog = page.getByTestId('open-project-dialog');
  for (const vButton of ['clone-repo', 'open-solution', 'open-folder', 'new-project']) {
    await expect(page.getByTestId(vButton)).toBeEnabled();
    await page.getByTestId(vButton).click();
    await expect(vDialog).toBeVisible();
    await page.getByTestId('open-project-cancel').click();
    await expect(vDialog).toBeHidden();
  }
  const vFolder = join(ROOT, `StartFolder${STAMP}`);
  mkdirSync(vFolder, { recursive: true });
  writeFileSync(join(vFolder, 'from-start.txt'), 'opened from Start\n');
  await page.getByTestId('open-folder').click();
  await submitProjectDialog(page, { path: vFolder });
  await expect(projectName(page)).toHaveText(`StartFolder${STAMP}`, { timeout: 20_000 });
  await expect(page.getByTestId('file-tree').getByText('from-start.txt', { exact: true })).toBeVisible({ timeout: 20_000 });
});

test('REQ-UI-073 File ▸ Exit is there and can be pressed', async () => {
  // Pressing it stops the harness for every other spec; the quit itself is HarnessAppShell.Exit (StopApplication).
  await openMenu(page, 'file');
  await expectOn(page.getByTestId('menu-file-exit'));
});

test('REQ-UI-073 Edit ▸ Undo, Redo, Cut, Copy and Paste act on the open file', async () => {
  await openFile(page, 'Notes/notes.txt');
  const vEditor = editorText(page);
  await vEditor.click();
  await vEditor.press('Control+End');
  await page.keyboard.type('XYZ');
  await expect(vEditor).toHaveValue(/XYZ/);

  await pick(page, 'edit', 'undo');
  await expect(vEditor).not.toHaveValue(/XYZ/);
  await openMenu(page, 'edit');
  await expectOn(page.getByTestId('menu-edit-redo'));
  await page.getByTestId('menu-edit-redo').click();
  await expect(vEditor).toHaveValue(/XYZ/);

  // copy, then cut what is selected, then paste it back
  await vEditor.evaluate((el) => (el as HTMLTextAreaElement).select());
  await pick(page, 'edit', 'copy');
  expect(await page.evaluate(() => navigator.clipboard.readText())).toContain('XYZ');
  await pick(page, 'edit', 'cut');
  await expect(vEditor).toHaveValue('');
  await pick(page, 'edit', 'paste');
  await expect(vEditor).toHaveValue(/XYZ/);
});

test('REQ-UI-073 Edit ▸ the file commands are off with no file open and Redo with nothing to redo', async () => {
  await openMenu(page, 'edit');
  for (const vItem of ['undo', 'redo', 'cut', 'copy', 'paste']) await expectOff(page.getByTestId(`menu-edit-${vItem}`));
  await page.keyboard.press('Escape');
  await openFile(page, 'Notes/notes.txt');
  await openMenu(page, 'edit');
  await expectOff(page.getByTestId('menu-edit-redo'));
});

test('REQ-UI-073 Edit ▸ Find in files finds the text and opens the file at the hit', async () => {
  await pick(page, 'edit', 'find');
  const vDialog = page.getByTestId('find-dialog');
  await expect(vDialog).toBeVisible();
  await vDialog.getByTestId('search-input').fill('WBA tick');
  await vDialog.getByTestId('search-go').click();
  const vHit = vDialog.getByTestId('search-hit').filter({ hasText: 'Program.cs' }).first();
  await expect(vHit).toBeVisible({ timeout: 20_000 });
  await vHit.click();
  await expect(vDialog).toBeHidden();
  await expect(tabs(page)).toContainText('Program.cs');
  await expect(editorText(page)).toHaveValue(/WBA tick/);

  await pick(page, 'edit', 'find');
  await vDialog.getByTestId('search-input').fill('zzz-not-anywhere-zzz');
  await vDialog.getByTestId('search-go').click();
  await expect(vDialog.getByTestId('search-none')).toBeVisible({ timeout: 20_000 });
  await vDialog.getByTestId('search-close').click();
});

test('REQ-UI-073 the toolbar Find opens Find in files and the hit opens the file', async () => {
  const vDialog = page.getByTestId('find-dialog');
  await expect(page.getByTestId('find')).toBeEnabled({ timeout: 20_000 });
  await expect(async () => {
    if (!(await vDialog.isVisible())) await page.getByTestId('find').click();
    await expect(vDialog).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
  await vDialog.getByTestId('search-input').fill('WBA tick');
  await vDialog.getByTestId('search-go').click();
  const vHit = vDialog.getByTestId('search-hit').filter({ hasText: 'Program.cs' }).first();
  await expect(vHit).toBeVisible({ timeout: 20_000 });
  await vHit.click();
  await expect(vDialog).toBeHidden();
  await expect(tabs(page)).toContainText('Program.cs');
});

test('REQ-UI-073 Edit ▸ Go to file lists files by name and opens the one picked', async () => {
  await pick(page, 'edit', 'goto');
  const vDialog = page.getByTestId('goto-dialog');
  await expect(vDialog).toBeVisible();
  await vDialog.getByTestId('search-input').fill('notes');
  const vHit = vDialog.getByTestId('search-hit').filter({ hasText: 'Notes/notes.txt' });
  await expect(vHit).toHaveCount(1, { timeout: 20_000 });
  await expect(vDialog.getByTestId('search-hit').filter({ hasText: 'Program.cs' })).toHaveCount(0);
  await vHit.click();
  await expect(vDialog).toBeHidden();
  await expect(tabs(page)).toContainText('notes.txt');
  await expect(editorText(page)).toHaveValue(/alpha notes line/);
});

test('REQ-UI-073 View ▸ Files and Output hide and show their regions, and the ticks follow', async () => {
  await expect(page.getByTestId('files-panel')).toBeVisible();
  await pick(page, 'view', 'files');
  await expect(page.getByTestId('files-panel')).toHaveCount(0);
  await openMenu(page, 'view');
  await expect(page.getByTestId('menu-view-files')).toHaveAttribute('aria-checked', 'false');
  await page.getByTestId('menu-view-files').click();
  await expect(page.getByTestId('files-panel')).toBeVisible();

  await expect(page.getByTestId('output')).toBeVisible();
  await pick(page, 'view', 'output');
  await expect(page.getByTestId('output')).toHaveCount(0);
  await openMenu(page, 'view');
  await expect(page.getByTestId('menu-view-output')).toHaveAttribute('aria-checked', 'false');
  await page.getByTestId('menu-view-output').click();
  await expect(page.getByTestId('output')).toBeVisible();
});

test('REQ-UI-073 View ▸ Prerequisites, Repository and Appearance reach their pages', async () => {
  for (const [vItem, vUrl] of [['prerequisites', /\/prerequisites/], ['repository', /\/repository/], ['appearance', /\/settings\/appearance/]] as const) {
    await pick(page, 'view', vItem);
    await expect(page).toHaveURL(vUrl, { timeout: 20_000 });
    await openProject(page);
  }
});

test('REQ-UI-073 Ctrl+, opens Settings, as Tools ▸ Settings shows, even while typing in a file', async () => {
  await openMenu(page, 'tools');
  await expect(page.getByTestId('menu-tools-settings')).toContainText('Ctrl+,');
  await page.keyboard.press('Escape');
  await page.keyboard.press('Control+Comma');
  await expect(page).toHaveURL(/\/settings\/providers/, { timeout: 20_000 });
  await openProject(page);

  await openFile(page, 'Notes/notes.txt');
  await editorText(page).click();
  await page.keyboard.press('Control+Comma');
  await expect(page).toHaveURL(/\/settings\/providers/, { timeout: 20_000 });
  await openProject(page);
  await openFile(page, 'Notes/notes.txt');
  await expect(editorText(page)).not.toHaveValue(/,/); // the comma was not typed into the file
});

test('REQ-UI-073 View ▸ Zoom in, Zoom out and Actual size change the size of everything', async () => {
  const vZoom = () => page.evaluate(() => document.documentElement.style.zoom || '1');
  await pick(page, 'view', 'zoom-in');
  await expect.poll(vZoom).toBe('1.1');
  await pick(page, 'view', 'zoom-in');
  await expect.poll(vZoom).toBe('1.2');
  await pick(page, 'view', 'zoom-out');
  await expect.poll(vZoom).toBe('1.1');
  await pick(page, 'view', 'zoom-reset');
  await expect.poll(vZoom).toBe('1');
});

test('REQ-UI-073 Help ▸ About Chatur shows the version and commit Prerequisites shows; Check for updates opens Prerequisites', async () => {
  await page.goto('/prerequisites', { waitUntil: 'networkidle' });
  const vVersion = (await page.getByTestId('chatur-version').innerText()).trim();
  const vCommit = (await page.getByTestId('chatur-commit').innerText()).trim();
  await openProject(page);
  await pick(page, 'help', 'about');
  const vAbout = page.getByTestId('about-dialog');
  await expect(vAbout).toBeVisible();
  await expect(vAbout.getByTestId('about-version')).toHaveText(vVersion);
  await expect(vAbout.getByTestId('about-commit')).toHaveText(vCommit);
  await vAbout.getByTestId('about-close').click();
  await pick(page, 'help', 'updates');
  await expect(page).toHaveURL(/\/prerequisites/, { timeout: 20_000 });
  await openProject(page);
});

test('REQ-UI-074 closing the conversation gives the file view the window and View ▸ Chat brings it back', async () => {
  await openFile(page, 'Notes/notes.txt');
  const vArea = page.getByTestId('files-area');
  const vNarrow = (await vArea.boundingBox())!.width;
  await page.getByTestId('close-chat').click();
  await expect(page.getByTestId('chat-panel')).toHaveCount(0);
  await expect(vArea).toBeVisible();
  expect((await vArea.boundingBox())!.width, 'the file view took the window').toBeGreaterThan(vNarrow * 1.5);
  await expect(editorText(page)).toHaveValue(/alpha notes line/); // the open file is kept

  await openMenu(page, 'view');
  await expect(page.getByTestId('menu-view-chat')).toHaveAttribute('aria-checked', 'false');
  await page.getByTestId('menu-view-chat').click();
  await expect(page.getByTestId('chat-panel')).toBeVisible();
  await expect(page.getByTestId('ask-input')).toBeEnabled({ timeout: 15_000 });
  await openMenu(page, 'view');
  await expect(page.getByTestId('menu-view-chat')).toHaveAttribute('aria-checked', 'true');
  await page.keyboard.press('Escape');
  await page.waitForTimeout(500);

  // View ▸ Chat also hides it, and the close button and the menu stay in step
  await pick(page, 'view', 'chat');
  await expect(page.getByTestId('chat-panel')).toHaveCount(0);
  await pick(page, 'view', 'chat');
  await expect(page.getByTestId('chat-panel')).toBeVisible();
});

test('REQ-UI-074 the window is never left with neither the file view nor the conversation', async () => {
  await page.getByTestId('close-chat').click();
  await expect(page.getByTestId('chat-panel')).toHaveCount(0);
  await page.getByTestId('close-file-view').click();
  await expect(page.getByTestId('chat-panel')).toBeVisible();
  await expect(page.getByTestId('files-area')).toHaveCount(0);
  await pick(page, 'view', 'chat');
  await expect(page.getByTestId('files-area')).toBeVisible();
  await expect(page.getByTestId('chat-panel')).toHaveCount(0);
  await pick(page, 'view', 'chat');
  await expect(page.getByTestId('chat-panel')).toBeVisible();
});

test('REQ-UI-023 a stop shows only where the stop was, and each step under the reply it belongs to', async () => {
  const vProject = query('SELECT ProjectId FROM Project WHERE Name = ?', NAME)[0].ProjectId as number;
  const vBeforeSession = (query('SELECT MAX(SessionId) AS Id FROM Session WHERE ProjectId = ?', vProject)[0].Id as number) ?? 0;
  await page.getByTestId('new-chat').click();
  await expect(page.getByTestId('conversation-empty')).toBeVisible({ timeout: 15_000 });
  let vSession = vBeforeSession;
  await expect.poll(() => {
    const vId = query('SELECT MAX(SessionId) AS Id FROM Session WHERE ProjectId = ?', vProject)[0].Id as number | null;
    vSession = vId ?? vSession;
    return vId ?? 0;
  }, { timeout: 10_000 }).toBeGreaterThan(vBeforeSession);
  const vRows: [number, string, string][] = [
    [1, 'user', 'first question'],
    [2, 'tool', 'Read notes.txt'],
    [3, 'assistant', 'first reply'],
    [4, 'stopped', 'Stopped by the owner.'],
    [5, 'user', 'second question'],
    [6, 'tool', 'Read Program.cs'],
    [7, 'assistant', 'second reply'],
  ];
  for (const [vSeq, vKind, vText] of vRows) {
    run('INSERT INTO SessionEvent (SessionId, Seq, Kind, Payload, CreatedUtc) VALUES (?, ?, ?, ?, ?)', vSession, vSeq, vKind, vText, new Date().toISOString());
  }
  await page.reload({ waitUntil: 'networkidle' });
  await expect(page.getByTestId('project-switch-name')).toHaveText(NAME, { timeout: 20_000 });
  // The harness's shared database may have older sessions for this project; pick the one we seeded.
  await page.getByTestId('history').click();
  await page.getByTestId(`session-${vSession}`).click();
  await expect(page.getByTestId('conversation-empty')).toHaveCount(0, { timeout: 10_000 });
  const vThread = page.getByTestId('conversation-thread');
  await expect(vThread.getByText('second reply')).toBeVisible({ timeout: 20_000 });
  await expect(page.getByTestId('activity-step')).toHaveCount(3, { timeout: 15_000 });

  const vFirst = vThread.locator('.msg.agent').filter({ hasText: 'first reply' });
  const vSecond = vThread.locator('.msg.agent').filter({ hasText: 'second reply' });
  await expect(vFirst.getByTestId('activity-step').filter({ hasText: 'Read notes.txt' })).toHaveCount(1);
  await expect(vFirst.getByTestId('activity-step').filter({ hasText: 'Stopped by the owner' })).toHaveCount(1);
  await expect(vSecond.getByTestId('activity-step').filter({ hasText: 'Read Program.cs' })).toHaveCount(1);
  await expect(vSecond.getByTestId('activity-step').filter({ hasText: 'Stopped by the owner' })).toHaveCount(0);
  await expect(vSecond.getByTestId('activity-step')).toHaveCount(1);
});
