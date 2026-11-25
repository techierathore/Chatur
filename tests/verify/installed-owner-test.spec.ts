// The owner's test, run on the INSTALLED Chatur the way the owner runs it (owner's tests of 22 and 23 Nov 2026;
// REQ-NFR-013, REQ-NFR-019): started from its Start menu entry, signed in, and worked from the states a person really
// meets. Part 1 is the owner's own case of 23 Nov: open an empty folder, New project, and look at the disk: the
// solution file is in that folder itself and the project in one folder under it. Then New project from nothing,
// projects from this machine's templates, Run, a folder with a project and no solution, the Analyst's brainstorm and
// brief in another empty folder, the measurements screens and one command to each agent. Never from a solution made
// by hand.
//
// The start keeps its data in a test data folder under this test project's own bin folder (CHATUR_TEST_DATA, the guard
// of REQ-NFR-010; the web view's DevTools port is set for that one start only), so the owner's own data, his
// remembered sign-in, his projects and his chats are never opened. The work folders live beside it, under a folder
// whose Directory.Build.props and nuget.config shadow Chatur's own, exactly as scripts/install-windows.ps1 writes them
// into this repository's bin folder for the owner's own run of the same steps (C:\...\Chatur\bin\OwnerTest and so on).
// Nothing is made outside this repository, and everything is deleted when the run ends. The model is the owner's test
// provider from tests/verify/test-settings.json; its key is read there and never printed. Windows only; skipped elsewhere.
import { test, expect, chromium, Browser, Page } from '@playwright/test';
import { execFileSync, spawn, ChildProcess } from 'child_process';
import { copyFileSync, existsSync, mkdirSync, readFileSync, readdirSync, rmSync, statSync, writeFileSync } from 'fs';
import * as path from 'path';
import { credentials } from './support/auth';
import { scratchDir } from './support/cleanup';
import { windowsPath } from './support/run-proof';
import { testProvider, connectorItem } from './support/provider';

const ON_WINDOWS_HOST = existsSync('/mnt/c/Windows') || process.platform === 'win32';
test.skip(!ON_WINDOWS_HOST, 'the installed app is a Windows install');
test.describe.configure({ mode: 'serial', timeout: 900_000 });

const DEVTOOLS_PORT = 29298;
const RELAY_PORT = 9298;
const STAMP = Date.now();
const SHOTS = 'tests/.artifacts/verify/shots';

let objRoot = '';
let objData = '';
let objWork = '';
let objBrowser: Browser;
let objRelay: ChildProcess | undefined;
let objMine: string[] = [];

/** Runs one PowerShell command and returns what it printed; a command that finds nothing prints nothing. */
function ps(aCommand: string): string {
  try {
    return execFileSync('powershell.exe', ['-NoProfile', '-Command', aCommand], { encoding: 'utf8', timeout: 60_000 }).trim();
  } catch (aError) {
    return String((aError as { stdout?: string }).stdout ?? '').trim();
  }
}

/** Every web view of the running app. */
const views = (): Page[] => objBrowser.contexts().flatMap((aContext) => aContext.pages());

/** The web view whose path matches, once it is there. */
async function viewOn(aPath: RegExp, aTimeout = 90_000): Promise<Page> {
  let vFound: Page | undefined;
  await expect.poll(() => {
    vFound = views().find((aPage) => aPath.test(new URL(aPage.url()).pathname));
    return vFound ? 'there' : views().map((p) => new URL(p.url()).pathname).join(',');
  }, { timeout: aTimeout }).toBe('there');
  return vFound!;
}

/** The main window, on the Workbench. */
const mainWindow = () => viewOn(/^\/$/);

/** Clicks a menu item of the main window's menu bar; a click before the window is ready is retried. */
async function choose(aMain: Page, aMenu: string, aItem: string): Promise<void> {
  const vItems = aMain.getByTestId(`menu-${aMenu}-items`);
  await expect(async () => {
    if (!(await vItems.isVisible())) await aMain.getByTestId(`menu-${aMenu}`).click();
    await expect(vItems).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
  await aMain.getByTestId(`menu-${aMenu}-${aItem}`).click();
}

/** Waits for the templates in the open New project dialog and picks one. */
async function pickTemplate(aPage: Page, aShortName: string): Promise<void> {
  await expect(aPage.getByTestId('template-loading')).toHaveCount(0, { timeout: 180_000 });
  await aPage.getByTestId(`template-${aShortName}`).click();
  await expect(aPage.getByTestId('new-project-framework-button')).toContainText('net10.0', { timeout: 90_000 });
}

/** Presses Create and waits for the dialog to close, failing with dotnet's own words when it could not. */
async function create(aPage: Page): Promise<void> {
  const vDialog = aPage.getByTestId('new-project-dialog');
  await aPage.getByTestId('new-project-create').click();
  const vState = async () => {
    if (await aPage.getByTestId('new-project-error').isVisible().catch(() => false)) return 'error';
    return (await vDialog.isVisible().catch(() => false)) ? 'open' : 'closed';
  };
  await expect.poll(vState, { timeout: 300_000, intervals: [1_000, 2_000] }).not.toBe('open');
  if ((await vState()) === 'error') throw new Error(`Create failed: ${await aPage.getByTestId('new-project-error').innerText()}`);
}

/** Opens File ▸ New project on the main window. */
async function newProject(aMain: Page): Promise<void> {
  const vDialog = aMain.getByTestId('new-project-dialog');
  await expect(async () => {
    if (!(await vDialog.isVisible())) await choose(aMain, 'file', 'new');
    await expect(vDialog).toBeVisible({ timeout: 3_000 });
  }).toPass({ timeout: 60_000 });
}

/** Chooses a run target by name in the toolbar. */
async function chooseTarget(aMain: Page, aName: string): Promise<void> {
  const vPop = aMain.getByTestId('target-pop');
  await expect(aMain.getByTestId('target').locator('button')).toBeEnabled({ timeout: 60_000 });
  await expect(async () => {
    if (!(await vPop.isVisible())) await aMain.getByTestId('target').locator('button').click();
    await expect(vPop).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
  await vPop.getByText(aName, { exact: true }).click();
  await expect(aMain.getByTestId('target').locator('button')).toContainText(aName);
}

/** Opens a folder from Start's own Open a folder, after closing what is open. */
async function openEmptyFolder(aMain: Page, aFolder: string): Promise<Page> {
  await choose(aMain, 'file', 'close');
  const vStart = await viewOn(/^\/start/);
  const vWay = vStart.getByTestId('open-folder').or(vStart.getByTestId('way-open-folder')).first();
  const vDialog = vStart.getByTestId('open-project-dialog');
  await expect(async () => {
    if (!(await vDialog.isVisible())) await vWay.click();
    await expect(vDialog).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 60_000 });
  await vStart.getByTestId('open-project-path').fill(windowsPath(aFolder));
  await vStart.getByTestId('open-project-go').click();
  const vMain = await mainWindow();
  await expect(vMain.getByTestId('project-switch-name')).toHaveText(path.basename(aFolder), { timeout: 60_000 });
  return vMain;
}

/** Sends one message in the chat and waits for the whole reply; returns its text box. */
async function say(aMain: Page, aText: string) {
  const vInput = aMain.getByTestId('ask-input');
  await expect(vInput).toBeEnabled({ timeout: 60_000 });
  const vBefore = await aMain.getByTestId('msg-text').count();
  await vInput.fill(aText);
  await aMain.getByTestId('send').click();
  await expect(aMain.getByTestId('msg-text')).toHaveCount(vBefore + 1, { timeout: 120_000 });
  await expect(aMain.getByTestId('stop-agent')).toHaveCount(0, { timeout: 400_000 });
  await expect(vInput).toBeEnabled({ timeout: 60_000 });
  return aMain.getByTestId('msg-text').last();
}

/** Keeps answering until the file is on disk, at most a few turns: a real model may ask once more before writing. */
async function untilWritten(aMain: Page, aFile: string, aNudges: string[]): Promise<void> {
  for (const vNudge of aNudges) {
    if (existsSync(aFile)) return;
    await say(aMain, vNudge);
  }

  expect(existsSync(aFile), `${aFile} was written`).toBe(true);
}

/** Every file under a folder, relative to it, in name order, leaving out build output (bin, obj): the disk as the person sees it. */
function disk(aFolder: string): string[] {
  const vFound: string[] = [];
  const vWalk = (aDir: string, aRel: string) => {
    for (const vName of readdirSync(aDir)) {
      const vRel = aRel ? `${aRel}/${vName}` : vName;
      if (statSync(path.join(aDir, vName)).isDirectory()) {
        if (vName !== 'bin' && vName !== 'obj') vWalk(path.join(aDir, vName), vRel);
      } else {
        vFound.push(vRel);
      }
    }
  };
  vWalk(aFolder, '');
  return vFound.sort();
}

/** The names at the top of FILES: what sits directly in the folder that is its root. */
async function filesRoot(aMain: Page): Promise<string[]> {
  const vIds = await aMain.getByTestId('file-tree').locator('[data-testid^="tree-"]').evaluateAll((aRows) => aRows.map((r) => r.getAttribute('data-testid') ?? ''));
  const vNames = vIds.map((i) => i.slice('tree-'.length)).filter((n) => n.length > 0 && !/[\\/]/.test(n));
  // A row carries ids of its own parts too (its right-click menu, its run label): only the rows themselves count.
  return vNames.filter((n) => !/-(menu|run)$/.test(n) || !vNames.includes(n.replace(/-(menu|run)$/, ''))).sort();
}

/**
 * The model: the owner's test provider, added through Tools > Settings in the secondary window when this test data
 * folder does not have it yet (owner's test 8, and 11 when it runs on its own). Its key is typed, never printed.
 */
async function connectProvider(aMain: Page) {
  const vProvider = testProvider();
  await choose(aMain, 'tools', 'settings');
  const vSettings = await viewOn(/^\/settings\/providers/);
  await vSettings.waitForTimeout(1_500);
  if (await vSettings.getByTestId('providers-table').getByText(vProvider.name, { exact: true }).count() === 0) {
    const vField = (aId: string) => vSettings.locator(`input[data-testid=${aId}], [data-testid=${aId}] input`).first();
    await vSettings.getByTestId('add-provider').click();
    await vField('field-name').fill(vProvider.name);
    await vSettings.getByTestId('field-connector-button').click();
    await vSettings.getByTestId(connectorItem(vProvider.connector)).click();
    if (vProvider.baseUrl) await vField('field-address').fill(vProvider.baseUrl);
    await vField('field-key').fill(vProvider.key);
    await vSettings.getByTestId('dialog-add').click();
    await expect(vSettings.getByTestId('add-provider-dialog')).toHaveCount(0, { timeout: 90_000 });
  }
  return vProvider;
}

test.beforeAll(async () => {
  test.setTimeout(300_000);
  mkdirSync(SHOTS, { recursive: true });
  objRoot = scratchDir(`installed-owner-${STAMP}`);
  objData = path.join(objRoot, 'data');
  objWork = path.join(objRoot, 'work');
  mkdirSync(objData, { recursive: true });
  mkdirSync(objWork, { recursive: true });
  // The owner's folders sit outside this repository; these shadow Chatur's own build settings above the work folder.
  writeFileSync(path.join(objWork, 'Directory.Build.props'), '<Project />\n');
  writeFileSync(path.join(objWork, 'Directory.Build.targets'), '<Project />\n');
  // As the install script does for the owner's own run: the work folder reads as "not a repository", so nothing done
  // in it can reach Chatur's own repository, which sits above it.
  writeFileSync(path.join(objWork, '.git'), 'gitdir: ./no-repository-here\n');
  writeFileSync(path.join(objWork, 'nuget.config'), '<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>\n');

  const vFolder = ps("Join-Path $env:LOCALAPPDATA 'Programs\\Chatur'");
  const vShortcut = ps("Join-Path ([Environment]::GetFolderPath('Programs')) 'Chatur.lnk'");
  expect(ps(`Test-Path -LiteralPath '${vShortcut}'`), 'the Start menu entry').toBe('True');
  const vBefore = new Set(ps('Get-Process -Name Chatur -ErrorAction SilentlyContinue | ForEach-Object { $_.Id }').split(/\s+/).filter(Boolean));
  const vDataWin = windowsPath(objData).replace(/'/g, "''");
  spawn('powershell.exe', ['-NoProfile', '-Command',
    `$env:CHATUR_TEST_DATA='${vDataWin}'; $env:WEBVIEW2_USER_DATA_FOLDER='${vDataWin}\\webview'; $env:WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS='--remote-debugging-port=${DEVTOOLS_PORT}'; Start-Process -FilePath '${vShortcut.replace(/'/g, "''")}'`],
    { stdio: 'ignore', detached: true }).unref();
  objRelay = spawn('powershell.exe', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', windowsPath(path.resolve('.tfcore/utils/tf-cdp-relay.ps1')), '-ListenPort', String(RELAY_PORT), '-TargetPort', String(DEVTOOLS_PORT)], { stdio: 'ignore' });
  await expect.poll(async () => {
    try {
      return (await fetch(`http://localhost:${RELAY_PORT}/json/version`)).ok;
    } catch {
      return false;
    }
  }, { timeout: 150_000, intervals: [2_000] }).toBe(true);
  objMine = ps('Get-Process -Name Chatur -ErrorAction SilentlyContinue | ForEach-Object { $_.Id }').split(/\s+/).filter((i) => i && !vBefore.has(i));
  expect(objMine.length, 'Chatur started from its Start menu entry').toBeGreaterThan(0);
  expect(ps(`(Get-Process -Id ${objMine[0]}).Path`).toLowerCase()).toBe(`${vFolder}\\Chatur.exe`.toLowerCase());
  objBrowser = await chromium.connectOverCDP(`http://localhost:${RELAY_PORT}`);
});

test.afterAll(async () => {
  try {
    await objBrowser?.close();
  } catch {
    /* already gone */
  }

  for (const vId of objMine) ps(`$p = Get-Process -Id ${vId} -ErrorAction SilentlyContinue; if ($p) { [void]$p.CloseMainWindow() }`);
  await new Promise((r) => setTimeout(r, 5_000));
  for (const vId of objMine) ps(`Stop-Process -Id ${vId} -Force -ErrorAction SilentlyContinue`);
  // The programs the runs started (the web app, the tests) end with Chatur's Stop or with the app; any left are ended
  // by their folder, which is this run's own.
  const vWorkWin = windowsPath(objWork).replace(/'/g, "''");
  ps(`Get-CimInstance Win32_Process | Where-Object { $_.ExecutablePath -and $_.ExecutablePath.StartsWith('${vWorkWin}') } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }`);
  objRelay?.kill();
  ps(`Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -and $_.CommandLine.Contains('-ListenPort ${RELAY_PORT}') } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }`);
  if (objRoot) rmSync(objRoot, { recursive: true, force: true, maxRetries: 10, retryDelay: 1_000 });
});

test('REQ-NFR-019 REQ-NFR-013 REQ-FN-310 REQ-UI-126 REQ-UI-125 REQ-UI-008 owner\'s test 1: the installed Chatur starts on Nothing is open yet; with an empty folder open, New project makes the solution file in that folder and the project one folder under it', async () => {
  // Sign in as a test user on the test data folder's own first start. A test start may remember its own earlier
  // sign-in (kept under "test." names, never the owner's); then it opens on Start straight away.
  const vFirst = await viewOn(/^\/(sign-in|start)/, 120_000);
  const vEmail = vFirst.getByTestId('input-email');
  if (await vEmail.isVisible({ timeout: 20_000 }).catch(() => false)) {
    const { email, password } = credentials('Owner');
    await vEmail.fill(email);
    await vFirst.getByTestId('input-password').fill(password);
    await vFirst.locator('[data-testid=signin-form] button[type=submit]').first().click();
  }

  const vStart = await viewOn(/^\/start/, 120_000);
  await expect(vStart.getByTestId('empty-start-title')).toHaveText('Nothing is open yet', { timeout: 60_000 });
  for (const vWay of ['way-new-project', 'way-open-folder', 'way-open-solution', 'way-open-file']) await expect(vStart.getByTestId(vWay)).toBeVisible();
  await vStart.screenshot({ path: `${SHOTS}/owner-test-1-nothing-open.png` });

  // The owner's case of 23 Nov: an empty folder, opened with "Open a folder".
  const vFolder = path.join(objWork, 'OwnerTest');
  mkdirSync(vFolder, { recursive: true });
  const vOpen = vStart.getByTestId('open-project-dialog');
  await expect(async () => {
    if (!(await vOpen.isVisible())) await vStart.getByTestId('way-open-folder').click();
    await expect(vOpen).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 60_000 });
  await vStart.getByTestId('open-project-path').fill(windowsPath(vFolder));
  await vStart.getByTestId('open-project-go').click();
  const vMain = await mainWindow();
  await expect(vMain.getByTestId('project-switch-name')).toHaveText('OwnerTest', { timeout: 60_000 });
  await expect(vMain.getByTestId('target').locator('button')).toContainText('No target found');
  expect(readdirSync(vFolder), 'opening the folder put nothing in it').toEqual([]);

  // File > New project, Blazor Web App, name Checking-Chatur: the dialog says where things go before it makes them.
  await newProject(vMain);
  await pickTemplate(vMain, 'blazor');
  const vWin = windowsPath(vFolder);
  await expect(vMain.getByTestId('new-project-folder')).toHaveText(vWin);
  await expect(vMain.getByTestId('new-project-location')).toHaveCount(0);
  await expect(vMain.getByTestId('new-project-change-location')).toBeVisible();
  await vMain.getByTestId('new-project-solution').fill('Checking-Chatur');
  await expect(vMain.getByTestId('new-project-solution-path')).toHaveText(`${vWin}\\Checking-Chatur.slnx`);
  await expect(vMain.getByTestId('new-project-project-path')).toHaveText(`${vWin}\\Checking-Chatur`);
  await vMain.screenshot({ path: `${SHOTS}/owner-test-1-new-project.png` });
  await create(vMain);

  // The disk: the solution file in the folder that was open, the project in one folder under it, nothing else.
  const vAfter = await mainWindow();
  await expect(vAfter.getByTestId('project-switch-name')).toHaveText('Checking-Chatur', { timeout: 60_000 });
  expect(readdirSync(vFolder).sort(), 'what the folder that was open holds').toEqual(['Checking-Chatur', 'Checking-Chatur.slnx']);
  expect(existsSync(path.join(vFolder, 'Checking-Chatur', 'Checking-Chatur.csproj')), 'the project file, one folder under').toBe(true);
  expect(existsSync(path.join(vFolder, 'Checking-Chatur', 'Checking-Chatur')), 'no folder of the same name inside itself').toBe(false);
  expect(readFileSync(path.join(vFolder, 'Checking-Chatur.slnx'), 'utf8')).toContain('Checking-Chatur/Checking-Chatur.csproj');
  // FILES: the folder that was open is the root, holding the solution file and the project folder.
  await expect.poll(() => filesRoot(vAfter), { timeout: 60_000 }).toEqual(['Checking-Chatur', 'Checking-Chatur.slnx']);
  await expect(vAfter.getByTestId('target').locator('button')).toContainText('Checking-Chatur', { timeout: 60_000 });
  await vAfter.screenshot({ path: `${SHOTS}/owner-test-1-workbench.png` });
});

test('REQ-NFR-013 REQ-FN-310 REQ-FN-309 owner\'s test 2: with nothing open, New project has one Location field and makes the solution file in the picked folder and the project one folder under it', async () => {
  await signedInStart();
  const vOpen = views().find((p) => new URL(p.url()).pathname === '/');
  if (vOpen) await choose(vOpen, 'file', 'close');
  const vStart = await viewOn(/^\/start/, 120_000);
  const vWay = vStart.getByTestId('new-project').or(vStart.getByTestId('way-new-project')).first();
  const vDialog = vStart.getByTestId('new-project-dialog');
  await expect(async () => {
    if (!(await vDialog.isVisible())) await vWay.click();
    await expect(vDialog).toBeVisible({ timeout: 3_000 });
  }).toPass({ timeout: 60_000 });
  await pickTemplate(vStart, 'console');
  const vFolder = path.join(objWork, 'FromNothing');
  const vWin = windowsPath(vFolder);
  await expect(vStart.getByTestId('new-project-folder')).toHaveCount(0);
  await expect(vStart.getByTestId('new-project-location')).toHaveValue('');
  await vStart.getByTestId('new-project-solution').fill('FromNothing');
  await vStart.getByTestId('new-project-name').fill('TryConsole');
  await vStart.getByTestId('new-project-location').fill(vWin);
  await expect(vStart.getByTestId('new-project-solution-path')).toHaveText(`${vWin}\\FromNothing.slnx`);
  await expect(vStart.getByTestId('new-project-project-path')).toHaveText(`${vWin}\\TryConsole`);
  await vStart.screenshot({ path: `${SHOTS}/owner-test-2-new-project.png` });
  await create(vStart);

  const vMain = await mainWindow();
  await expect(vMain.getByTestId('project-switch-name')).toHaveText('FromNothing', { timeout: 60_000 });
  await expect(vMain.getByTestId('target').locator('button')).toContainText('TryConsole', { timeout: 60_000 });
  expect(disk(vFolder), 'the files in the picked folder').toEqual(['FromNothing.slnx', 'TryConsole/Program.cs', 'TryConsole/TryConsole.csproj'].sort());
  await vMain.screenshot({ path: `${SHOTS}/owner-test-2-workbench.png` });
});

test('REQ-UI-112 owner\'s test 3: File New project adds projects to the solution from this machine\'s templates', async () => {
  const vMain = await mainWindow();
  const vWin = windowsPath(path.join(objWork, 'FromNothing'));
  for (const [vShort, vName] of [['blazor', 'TryWeb'], ['classlib', 'TryLib'], ['xunit', 'TryLib.Tests']]) {
    await newProject(vMain);
    await expect(vMain.getByTestId('new-project-dialog')).toContainText('New project in FromNothing');
    await pickTemplate(vMain, vShort);
    await vMain.getByTestId('new-project-name').fill(vName);
    await expect(vMain.getByTestId('new-project-folder')).toHaveText(vWin);
    await expect(vMain.getByTestId('new-project-project-path')).toHaveText(`${vWin}\\${vName}`);
    await create(vMain);
    expect(existsSync(path.join(objWork, 'FromNothing', vName, `${vName}.csproj`)), vName).toBe(true);
  }

  expect(readdirSync(path.join(objWork, 'FromNothing')).sort(), 'the solution folder').toEqual(['FromNothing.slnx', 'TryConsole', 'TryLib', 'TryLib.Tests', 'TryWeb']);
  await chooseTarget(vMain, 'TryWeb');
  await expect(vMain.getByTestId('target-pop')).toHaveCount(0);
  await vMain.getByTestId('target').locator('button').click();
  await expect(vMain.getByTestId('target-pop')).toContainText('TryLib.Tests');
  await vMain.keyboard.press('Escape');
  await vMain.screenshot({ path: `${SHOTS}/owner-test-3-projects.png` });
});

test('REQ-UI-107 REQ-FN-306 owner\'s test 4: running the web app opens Output and the browser, and Stop ends it', async () => {
  const vMain = await mainWindow();
  await chooseTarget(vMain, 'TryWeb');
  await vMain.getByTestId('run').click();
  const vOutput = vMain.getByTestId('output');
  await expect(vOutput).toBeVisible({ timeout: 10_000 });
  await expect(vOutput).toContainText('> dotnet build', { timeout: 30_000 });
  await expect(vOutput).toContainText('Now listening on:', { timeout: 400_000 });
  await expect(vOutput).toContainText('in the browser', { timeout: 30_000 });
  await vMain.screenshot({ path: `${SHOTS}/owner-test-4-web.png` });
  await vMain.getByTestId('stop').click();
  await expect(vOutput).toContainText('Run stopped.', { timeout: 60_000 });
});

test('REQ-UI-107 REQ-UI-108 owner\'s test 5: the console app runs to its exit code, and a broken build says so', async () => {
  const vMain = await mainWindow();
  await chooseTarget(vMain, 'TryConsole');
  await vMain.getByTestId('run').click();
  const vOutput = vMain.getByTestId('output');
  await expect(vOutput).toContainText('Hello, World!', { timeout: 400_000 });
  await expect(vOutput).toContainText('Process exited with code 0.', { timeout: 60_000 });

  const vProgram = path.join(objWork, 'FromNothing', 'TryConsole', 'Program.cs');
  const vText = readFileSync(vProgram, 'utf8');
  writeFileSync(vProgram, vText.replace(');', ')'));
  try {
    await vMain.getByTestId('run').click();
    await expect(vOutput).toContainText('The program was not started.', { timeout: 300_000 });
    await expect(vMain.getByTestId('output-status')).toHaveText('build failed');
  } finally {
    writeFileSync(vProgram, vText);
  }

  await vMain.screenshot({ path: `${SHOTS}/owner-test-5-console.png` });
});

test('REQ-UI-107 owner\'s test 6: the test project runs its tests', async () => {
  const vMain = await mainWindow();
  await chooseTarget(vMain, 'TryLib.Tests');
  await vMain.getByTestId('run').click();
  const vOutput = vMain.getByTestId('output');
  await expect(vOutput).toContainText('> dotnet test', { timeout: 400_000 });
  await expect(vOutput).toContainText(/Passed!|Test summary: total: \d+, failed: 0/, { timeout: 400_000 });
  await expect(vOutput).toContainText('Process exited with code 0.', { timeout: 60_000 });
});

test('REQ-UI-124 REQ-FN-309 owner\'s test 7: in a folder with a project and no solution, New project says so first, makes the solution there and puts both projects in it', async () => {
  // The starting point, made as a person makes it: dotnet new in a terminal, and no solution.
  const vFolder = path.join(objWork, 'LooseOne');
  mkdirSync(vFolder, { recursive: true });
  const vWin = windowsPath(vFolder);
  execFileSync('powershell.exe', ['-NoProfile', '-Command', `Set-Location -LiteralPath '${vWin.replace(/'/g, "''")}'; dotnet new console -n Loose -o '${vWin.replace(/'/g, "''")}\\Loose' | Out-Null`], { timeout: 180_000 });
  expect(disk(vFolder)).toEqual(['Loose/Loose.csproj', 'Loose/Program.cs'].sort());
  await signedInStart();
  const vMain = await openFolderFromAnywhere(vFolder);

  await newProject(vMain);
  await pickTemplate(vMain, 'classlib');
  await expect(vMain.getByTestId('new-project-found')).toContainText('has a project and no solution');
  await expect(vMain.getByTestId('new-project-found')).toContainText('Loose\\Loose.csproj');
  await vMain.getByTestId('new-project-solution').fill('Gathered');
  await vMain.getByTestId('new-project-name').fill('Second');
  await expect(vMain.getByTestId('new-project-solution-path')).toHaveText(`${vWin}\\Gathered.slnx`);
  await expect(vMain.getByTestId('new-project-project-path')).toHaveText(`${vWin}\\Second`);
  await vMain.screenshot({ path: `${SHOTS}/owner-test-7-loose-project.png` });
  await create(vMain);

  const vAfter = await mainWindow();
  await expect(vAfter.getByTestId('project-switch-name')).toHaveText('Gathered', { timeout: 60_000 });
  expect(disk(vFolder), 'the files in the folder that was open').toEqual(['Gathered.slnx', 'Loose/Loose.csproj', 'Loose/Program.cs', 'Second/Class1.cs', 'Second/Second.csproj'].sort());
  const vText = readFileSync(path.join(vFolder, 'Gathered.slnx'), 'utf8');
  expect(vText).toContain('Loose/Loose.csproj');
  expect(vText).toContain('Second/Second.csproj');
  await expect(vAfter.getByTestId('target').locator('button')).toContainText('Loose', { timeout: 60_000 });
});

test('REQ-FN-308 REQ-FN-311 REQ-UI-114 REQ-FN-312 REQ-FN-040 owner\'s test 8: in an empty folder the Analyst has its commands and chatting leaves no file there, then it brainstorms with the person and writes the brief', async () => {
  const vFolder = path.join(objWork, 'BrainTest');
  mkdirSync(vFolder, { recursive: true });
  await signedInStart();
  const vMain = await openFolderFromAnywhere(vFolder);

  const vProvider = await connectProvider(vMain);

  await vMain.getByTestId('agent-pick').click();
  await vMain.getByTestId('agent-analyst').click();
  await vMain.getByTestId('model-pick').click();
  await vMain.getByTestId('model-pop').getByText(vProvider.model, { exact: true }).click();
  const vGreeting = vMain.getByTestId('agent-greeting').last();
  await expect(vGreeting).toContainText('*brainstorm {topic}', { timeout: 30_000 });
  await expect(vMain.getByTestId('agent-greeting-command')).toHaveCount(12);

  const vYes = await say(vMain, "Don't you have brain-storm command ?");
  await expect(vYes).toContainText('Yes.');
  const vHelp = await say(vMain, '*help');
  await expect(vHelp.locator('ol > li')).toHaveCount(12);
  expect(await vHelp.innerText()).not.toContain('**');
  const vYolo = await say(vMain, '*yolo');
  await expect(vYolo.locator('strong')).toHaveText('Go ahead');
  // Three turns of chat, and nothing asked for a file yet: the folder holds no file from Chatur (owner, 23 Nov 2026).
  expect(readdirSync(vFolder), 'the folder after three chat turns').toEqual([]);

  await say(vMain, '*brainstorm a small to-do app for one person');
  await say(vMain, '1) A to-do list app for one person on a desktop. 2) Keep it small: no accounts, no sync. 3) Focused ideas. 4) Yes, save it. Recommend one technique and use it now; my ideas: due dates, a today view, quick add with a keyboard shortcut, and done items fade out.');
  const vResults = path.join(vFolder, 'docs', 'brainstorming-session-results.md');
  await untilWritten(vMain, vResults, [
    'That is enough ideas. Please end the session now and save the session document.',
    'Write docs/brainstorming-session-results.md now with what we have.',
    'Save it now, please.',
  ]);

  const vBrief = path.join(vFolder, 'docs', 'BrainTest-Brief.md');
  await say(vMain, '*create-project-brief BrainTest');
  await untilWritten(vMain, vBrief, [
    'Use the brainstorm. Take the sensible reading for anything open, put real unknowns under Open questions, and write the brief now.',
    'Write docs/BrainTest-Brief.md now.',
    'Write the brief file now, please.',
  ]);
  expect(readFileSync(vBrief, 'utf8')).toContain('## Must do');
  await expect(vMain.getByTestId('conversation')).toContainText('docs/BrainTest-Brief.md is written');
  await expect(vMain.getByTestId('tree-docs')).toBeVisible({ timeout: 30_000 });
  await vMain.screenshot({ path: `${SHOTS}/owner-test-9-brief.png` });
});

/** Signs in when this start shows the sign-in page (a part run on its own begins on a fresh test data folder). */
async function signedInStart(): Promise<void> {
  const vFirst = await viewOn(/^\/(sign-in|start|)$/, 120_000);
  const vEmail = vFirst.getByTestId('input-email');
  if (await vEmail.isVisible({ timeout: 10_000 }).catch(() => false)) {
    const { email, password } = credentials('Owner');
    await vEmail.fill(email);
    await vFirst.getByTestId('input-password').fill(password);
    await vFirst.locator('[data-testid=signin-form] button[type=submit]').first().click();
  }
}

/** Opens a folder from Start's Open a folder, from the main window (closing what is open) or from Start itself. */
async function openFolderFromAnywhere(aFolder: string): Promise<Page> {
  await expect.poll(() => views().some((p) => /^\/(start|)$/.test(new URL(p.url()).pathname)), { timeout: 120_000 }).toBe(true);
  const vMain = views().find((p) => new URL(p.url()).pathname === '/');
  if (vMain) return openEmptyFolder(vMain, aFolder);
  const vStart = await viewOn(/^\/start/);
  const vWay = vStart.getByTestId('open-folder').or(vStart.getByTestId('way-open-folder')).first();
  const vDialog = vStart.getByTestId('open-project-dialog');
  await expect(async () => {
    if (!(await vDialog.isVisible())) await vWay.click();
    await expect(vDialog).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 60_000 });
  await vStart.getByTestId('open-project-path').fill(windowsPath(aFolder));
  await vStart.getByTestId('open-project-go').click();
  const vOpened = await mainWindow();
  await expect(vOpened.getByTestId('project-switch-name')).toHaveText(path.basename(aFolder), { timeout: 60_000 });
  return vOpened;
}

/** Opens an agent's conversation afresh on the test model, with Go ahead on, so a command that writes a file runs to its end. */
async function talkTo(aMain: Page, aAgent: string): Promise<void> {
  const vProvider = testProvider();
  await aMain.getByTestId('agent-pick').click();
  await aMain.getByTestId(`agent-${aAgent}`).click();
  await expect(aMain.getByTestId('agent-greeting').last()).toBeVisible({ timeout: 30_000 });
  await expect(async () => {
    await aMain.getByTestId('new-chat').click();
    await aMain.waitForTimeout(1_500);
    await expect(aMain.getByTestId('msg-text')).toHaveCount(0, { timeout: 1_000 });
  }).toPass({ timeout: 45_000 });
  await aMain.getByTestId('model-pick').click();
  await aMain.getByTestId('model-pop').getByText(vProvider.model, { exact: true }).click();
  // The Verifier has no *yolo (its *report writes nothing); the other three turn Go ahead on so their files land.
  if (aAgent !== 'verifier') await expect(await say(aMain, '*yolo')).toContainText('Go ahead');
}

const METRICS_FOLDER = () => path.join(objWork, 'MetricsTest');

test('REQ-NFR-017 REQ-UI-115 REQ-UI-120 REQ-FN-313 REQ-FN-314 REQ-UI-121 owner\'s test 10: Project Measurements shows the project\'s own figures, a run opens, the zip is saved, and every project is listed', async () => {
  // The project is a folder holding a COPY of Chatur's own measurement files and its phase-4 checklist: real records,
  // read only from the copy, so Chatur's own repository is never opened by the test.
  await signedInStart();
  const vFolder = METRICS_FOLDER();
  const vMetrics = path.join(vFolder, 'docs', 'metrics');
  mkdirSync(vMetrics, { recursive: true });
  for (const vName of readdirSync('docs/metrics').filter((f) => f.endsWith('.jsonl'))) copyFileSync(path.join('docs/metrics', vName), path.join(vMetrics, vName));
  copyFileSync('docs/Chatur-P4-Checklist.md', path.join(vFolder, 'docs', 'MetricsTest-Checklist.md'));
  const vMain = await openFolderFromAnywhere(vFolder);

  await choose(vMain, 'project', 'measurements');
  const vPage = await viewOn(/^\/measurements$/);
  await expect(vPage.getByTestId('metrics-project')).toHaveText('MetricsTest', { timeout: 60_000 });
  await expect(vPage.getByTestId('tile-runs-value')).not.toHaveText('—', { timeout: 60_000 });
  const vAll = Number((await vPage.getByTestId('tile-runs-value').innerText()).replace(/,/g, ''));
  expect(vAll, 'runs recorded in all history').toBeGreaterThan(0);
  for (const vTile of ['tokens-in', 'tokens-out', 'cache-read', 'cache-write', 'usd', 'wall']) await expect(vPage.getByTestId(`tile-${vTile}`)).toBeVisible();
  await expect(vPage.getByTestId('metrics-misses')).toBeVisible();
  await expect(vPage.getByTestId('firstpass-rate')).not.toHaveText('');
  await expect(vPage.getByTestId('metrics-offline-note')).toContainText('nothing is sent anywhere');
  await vPage.screenshot({ path: `${SHOTS}/owner-test-10-measurements.png`, fullPage: true });

  await vPage.getByTestId('period-button').click();
  await vPage.getByTestId('period-7').click();
  await expect(vPage.getByTestId('metrics-period-note')).toContainText('Last 7 days', { timeout: 30_000 });
  const vWeek = Number((await vPage.getByTestId('tile-runs-value').innerText()).replace(/,/g, ''));
  expect(vWeek, 'a week holds no more runs than all history').toBeLessThanOrEqual(vAll);
  await vPage.getByTestId('period-button').click();
  await vPage.getByTestId('period-all').click();

  await vPage.locator('[data-testid^="run-row-"]').first().click();
  await expect(vPage.getByTestId('run-detail')).toBeVisible({ timeout: 15_000 });
  await expect(vPage.getByTestId('detail-command')).not.toHaveText('');
  await expect(vPage.getByTestId('detail-raw')).toBeVisible();
  await vPage.keyboard.press('Escape');
  await expect(vPage.getByTestId('run-detail')).toBeHidden();

  await vPage.getByTestId('save-zip').click();
  await expect(vPage.getByTestId('zip-dialog')).toBeVisible();
  await vPage.getByTestId('zip-save').click();
  await expect(vPage.getByTestId('zip-saved')).toBeVisible({ timeout: 60_000 });
  expect(readdirSync(vFolder).some((f) => f.endsWith('.zip')), 'the zip is in the project folder').toBe(true);
  await vPage.getByTestId('zip-close').click();

  await vPage.getByTestId('nav-measurements-all').click();
  await expect(vPage.getByTestId('across-heading')).toBeVisible({ timeout: 30_000 });
  await expect(vPage.getByTestId('across-row-metricstest')).toBeVisible({ timeout: 60_000 });
  await vPage.screenshot({ path: `${SHOTS}/owner-test-10-across.png` });

  await vPage.getByTestId('nav-measurements').click();
  await vPage.getByTestId('open-measurements-screen').click();
  await expect(vPage.getByTestId('metrics-heading')).toBeVisible({ timeout: 30_000 });
});

test('REQ-NFR-017 REQ-FN-317 REQ-FN-322 REQ-FN-320 owner\'s test 11: one command typed to each agent runs to its end', async () => {
  const vFolder = METRICS_FOLDER();
  const vMain = await mainWindow();
  await expect(vMain.getByTestId('project-switch-name')).toHaveText('MetricsTest', { timeout: 60_000 });
  const vPrompt = path.join(vFolder, 'docs', 'MetricsTest-Research-Prompt.md');

  await connectProvider(vMain);
  await talkTo(vMain, 'analyst');
  await say(vMain, '*research-prompt note-taking apps for one person that work offline');
  await untilWritten(vMain, vPrompt, [
    'Focus 4, competitors. It should inform whether to build it at all; constraints: one developer, no budget; known: Notion, Obsidian and Apple Notes exist. Write the prompt file now.',
    'Write docs/MetricsTest-Research-Prompt.md now, no more questions.',
    'Please write the file now.',
  ]);
  const vFirst = statSync(vPrompt).mtimeMs;

  await talkTo(vMain, 'architect');
  await say(vMain, '*research which local database suits a desktop to-do app for one person');
  const vRewritten = () => existsSync(vPrompt) && statSync(vPrompt).mtimeMs > vFirst;
  for (const vNudge of [
    'Focus 5, technology. It should inform the choice of data store; constraints: .NET, works offline, no server; known: SQLite and LiteDB exist. Write the prompt file now.',
    'Write docs/MetricsTest-Research-Prompt.md now, no more questions.',
    'Please write the file now.',
  ]) {
    if (vRewritten()) break;
    await say(vMain, vNudge);
  }
  expect(vRewritten(), 'the Architect wrote the research prompt again').toBe(true);

  await talkTo(vMain, 'flow-master');
  await expect(await say(vMain, '*develop-status MetricsTest')).toContainText('No end-to-end run');

  await talkTo(vMain, 'verifier');
  const vReport = await say(vMain, '*report all MetricsTest');
  await expect(vReport).toContainText('row(s) in scope');
  await expect(vReport.locator('table')).toBeVisible();
  await vMain.screenshot({ path: `${SHOTS}/owner-test-11-verifier.png` });
});
