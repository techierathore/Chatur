// The owner's test of 22 Nov 2026 in the Windows app itself (the desktop head under test, over CDP; tf-verify-boot.sh
// --head windows): New project from an empty folder (REQ-FN-310, REQ-FN-309, REQ-UI-112) and the agents' own commands
// drawn as formatted text (REQ-FN-308, REQ-FN-311, REQ-UI-114). Against the web harness every test here is skipped,
// never passed. The desktop head keeps its own database under tests/.artifacts/app-data/desktop (REQ-NFR-010); the
// folder these tests make lives under this test project's bin folder and goes, with its projects, when they end.
import { test, expect, chromium, Browser, Page } from '@playwright/test';
import { existsSync, mkdirSync, readdirSync, writeFileSync } from 'fs';
import * as path from 'path';
import { credentials } from './support/auth';
import { removeProjectsUnder } from './support/cleanup';

const windowsPath = (aPath: string): string =>
  aPath.replace(/^\/mnt\/([a-z])\//, (_m, aDrive: string) => `${aDrive.toUpperCase()}:\\`).replace(/\//g, '\\');

const CDP_URL = process.env.CDP_URL ?? '';
test.skip(!CDP_URL, 'desktop head only — boot it with tf-verify-boot.sh --head windows');
test.describe.configure({ mode: 'serial', timeout: 300_000 });

let browser: Browser;
const NEW_STAMP = Date.now();
const NEW_ROOT = path.resolve('tests/verify/bin', `desktop-new-${NEW_STAMP}`);
const views = (): Page[] => browser.contexts().flatMap((aContext) => aContext.pages());

function mainWindow(): Page {
  const vMain = views().find((aPage) => new URL(aPage.url()).pathname === '/');
  if (!vMain) throw new Error(`No main window on the Workbench; open views: ${views().map((p) => p.url()).join(', ')}`);
  return vMain;
}

/** Clicks a menu item in the main window's menu bar; a click before the window is ready is retried. */
async function choose(aMain: Page, aMenu: string, aItem: string): Promise<void> {
  const vItems = aMain.getByTestId(`${aMenu}-items`);
  await expect(async () => {
    if (!(await vItems.isVisible())) await aMain.getByTestId(aMenu).click();
    await expect(vItems).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
  await aMain.getByTestId(aItem).click();
}

test.beforeAll(async () => {
  browser = await chromium.connectOverCDP(CDP_URL);
  const onMain = () => views().some((aPage) => new URL(aPage.url()).pathname === '/');
  if (onMain()) return;
  const vStart = views()[0];
  const vEmail = vStart.getByTestId('input-email');
  if (await vEmail.isVisible({ timeout: 15_000 }).catch(() => false)) {
    const { email, password } = credentials('Owner');
    await vEmail.fill(email);
    await vStart.getByTestId('input-password').fill(password);
    await vStart.locator('[data-testid=signin-form] button[type=submit]').first().click();
  }
  // Signed in with nothing open: the empty start offers New project and the three Open ways; a scratch
  // folder is opened through Open a folder so the tests start on the Workbench.
  await expect.poll(async () => onMain() || (await vStart.getByTestId('way-open-folder').or(vStart.getByTestId('open-folder')).first().isVisible().catch(() => false)), { timeout: 90_000 }).toBe(true);
  if (onMain()) return;
  const vDialog = vStart.getByTestId('open-project-dialog');
  await expect(async () => {
    if (!(await vDialog.isVisible())) await vStart.getByTestId('way-open-folder').or(vStart.getByTestId('open-folder')).first().click();
    await expect(vDialog).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
  const vFirst = path.join(NEW_ROOT, 'Opened');
  mkdirSync(vFirst, { recursive: true });
  await vStart.getByTestId('open-project-path').fill(windowsPath(vFirst));
  await vStart.getByTestId('open-project-go').click();
  await expect.poll(onMain, { timeout: 60_000 }).toBe(true);
});

// ---- The owner's test of 22 Nov 2026, in the desktop app itself ---------------------------------------------------
// New project from an empty folder (BRD-247, BRD-248, BRD-244) and the agents' own commands drawn as formatted text
// (BRD-249, BRD-241, BRD-250). The folder lives under this test project's bin folder, beside shadows of Chatur's own
// build settings, and goes with its projects when the run ends.

test.afterAll(() => {
  removeProjectsUnder(NEW_ROOT);
});

/** Opens a folder in the main window through File > Open a folder. */
async function openFolderInMain(aMain: Page, aFolder: string): Promise<void> {
  await choose(aMain, 'menu-file', 'menu-file-open-folder');
  await aMain.locator('input[data-testid=open-project-path], [data-testid=open-project-path] input').first().fill(windowsPath(aFolder));
  await aMain.getByTestId('open-project-go').click();
  await expect(aMain.getByTestId('project-switch-name')).toHaveText(path.basename(aFolder), { timeout: 60_000 });
}

test('REQ-FN-310 REQ-FN-309 REQ-UI-112 in the desktop app, New project from an empty folder lists the templates, makes the solution and its first project there, and Run has a target', async () => {
  test.setTimeout(600_000);
  const vFolder = path.join(NEW_ROOT, 'Empty');
  mkdirSync(vFolder, { recursive: true });
  writeFileSync(path.join(NEW_ROOT, 'Directory.Build.props'), '<Project />\n');
  writeFileSync(path.join(NEW_ROOT, 'nuget.config'), '<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>\n');
  const vMain = mainWindow();
  await openFolderInMain(vMain, vFolder);
  await expect(vMain.locator('[data-testid=target] button').first()).toContainText('No target found');

  const vDialog = vMain.getByTestId('new-project-dialog');
  await expect(async () => {
    if (!(await vDialog.isVisible())) await choose(vMain, 'menu-file', 'menu-file-new');
    await expect(vDialog).toBeVisible({ timeout: 3_000 });
  }).toPass({ timeout: 60_000 });
  await expect(vMain.getByTestId('template-loading')).toHaveCount(0, { timeout: 180_000 });
  for (const vShort of ['console', 'classlib', 'blazor', 'xunit']) await expect(vMain.getByTestId(`template-${vShort}`)).toBeVisible();
  await vMain.getByTestId('template-console').click();
  await vMain.getByTestId('new-project-solution').fill(`DeskTodo${NEW_STAMP}`);
  await expect(vMain.getByTestId('new-project-folder')).toHaveText(windowsPath(vFolder));
  await expect(vMain.getByTestId('new-project-solution-path')).toHaveText(`${windowsPath(vFolder)}\\DeskTodo${NEW_STAMP}.slnx`);
  await expect(vMain.getByTestId('new-project-project-path')).toHaveText(`${windowsPath(vFolder)}\\DeskTodo${NEW_STAMP}`);
  await vMain.getByTestId('new-project-create').click();
  await expect(vDialog).toBeHidden({ timeout: 300_000 });

  const vAfter = mainWindow();
  await expect(vAfter.getByTestId('project-switch-name')).toHaveText(`DeskTodo${NEW_STAMP}`, { timeout: 60_000 });
  await expect(vAfter.locator('[data-testid=target] button').first()).toContainText(`DeskTodo${NEW_STAMP}`, { timeout: 60_000 });
  // The exact paths: the solution file in the folder that was open, the project in one folder under it.
  expect(readdirSync(vFolder).sort()).toEqual([`DeskTodo${NEW_STAMP}`, `DeskTodo${NEW_STAMP}.slnx`]);
  expect(existsSync(path.join(vFolder, `DeskTodo${NEW_STAMP}`, `DeskTodo${NEW_STAMP}.csproj`))).toBe(true);
  await vAfter.screenshot({ path: 'tests/.artifacts/verify/shots/desktop-REQ-FN-310-new-project.png' });
});

test('REQ-FN-308 REQ-FN-311 REQ-UI-114 in the desktop app, the analyst greets with every command, answers brainstorm yes, and *help is drawn as a formatted list', async () => {
  const vMain = mainWindow();
  await vMain.getByTestId('agent-pick').click();
  await vMain.getByTestId('agent-analyst').click();
  await expect(vMain.getByTestId('agent-greeting').last()).toContainText('*brainstorm {topic}', { timeout: 30_000 });
  await vMain.getByTestId('new-chat').click();
  const say = async (aText: string) => {
    const vBefore = await vMain.getByTestId('msg-text').count();
    await vMain.getByTestId('ask-input').fill(aText);
    await vMain.getByTestId('send').click();
    await expect(vMain.getByTestId('msg-text')).toHaveCount(vBefore + 1, { timeout: 60_000 });
    return vMain.getByTestId('msg-text').last();
  };
  await expect(await say("Don't you have brain-storm command ?")).toContainText('Yes.');
  const vHelp = await say('*help');
  await expect(vHelp.locator('ol > li')).toHaveCount(12);
  await expect(vHelp.locator('code').first()).toBeVisible();
  expect(await vHelp.innerText()).not.toContain('**');
  await vMain.screenshot({ path: 'tests/.artifacts/verify/shots/desktop-REQ-UI-114-help.png' });
});
