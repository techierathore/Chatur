// The owner's test, run on the INSTALLED Chatur the way the owner runs it (owner's tests of 22, 23 and 25 Nov 2026;
// REQ-NFR-013, REQ-NFR-019): started from its Start menu entry, signed in, and worked from the states a person really
// meets. Part 1 is the owner's own case: open an empty folder, New project, and look at the disk: the solution has a
// folder of its own, the solution file in it and the project in a folder of its own inside that, and FILES shows the
// solution as its root. Then New project from nothing, projects from this machine's templates (the newest runnable one
// is the one that runs), several projects started together, Run, the file window, a folder with a project and no
// solution, the Analyst's brainstorm and brief in another empty folder with its sessions and tokens written to docs/metrics,
// the measurements screens chosen inside the app, and one command to each agent. Never from a solution made by hand.
// Every Chatur head here is started on DevTools port 29243 behind relay port 9243 (the owner's setting of 26 Nov 2026,
// because another tool is being checked on this machine at 9223); the first test shows that the pages driven are Chatur's.
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
// A click that cannot land fails in a minute, not at the end of a test's quarter of an hour.
test.use({ actionTimeout: 60_000 });

const DEVTOOLS_PORT = 29243;
const RELAY_PORT = 9243;
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
  // The search box narrows the long list to the one wanted (REQ-UI-127).
  await aPage.getByTestId('template-search').fill(aShortName);
  // A click event, not a mouse move: the dialog scrolls inside a short window, and pointer hit-testing over two scroll areas is what failed.
  await aPage.getByTestId(`template-${aShortName}`).dispatchEvent('click');
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
  await goOnIfAsked(vStart);
  const vMain = await mainWindow();
  await expect(vMain.getByTestId('project-switch-name')).toHaveText(path.basename(aFolder), { timeout: 60_000 });
  return vMain;
}

/** A folder with a project and no solution is asked about first (REQ-FN-325); a step that only needs it open goes on. */
async function goOnIfAsked(aPage: Page): Promise<void> {
  const vAsk = aPage.getByTestId('open-project-no-solution');
  await vAsk.waitFor({ state: 'visible', timeout: 6_000 }).catch(() => undefined);
  if (await vAsk.isVisible().catch(() => false)) await aPage.getByTestId('open-project-no-solution-go').click();
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

/** How far the conversation's view is from its end, in pixels: 0 is the newest message; -1 means it does not scroll at all. */
async function chatGap(aMain: Page): Promise<number> {
  return aMain.getByTestId('conversation').evaluate((aRoot) => {
    const vEls = [aRoot, ...Array.from(aRoot.querySelectorAll('*'))] as HTMLElement[];
    const vViewport = vEls.find((e) => e.scrollHeight > e.clientHeight + 5 && getComputedStyle(e).overflowY !== 'visible');
    return vViewport ? vViewport.scrollHeight - vViewport.clientHeight - vViewport.scrollTop : -1;
  });
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

/**
 * What FILES shows (REQ-UI-125): the root rows that stand for a solution, and the names directly under them. A row
 * carries ids of its own parts too (its right-click menu, its run label): only the rows themselves count.
 */
async function filesTree(aMain: Page): Promise<{ roots: string[]; projects: string[]; others: string[] }> {
  const vRows = await aMain.getByTestId('file-tree').locator('[data-testid^="tree-"]').evaluateAll((aEls) => aEls.map((e) => ({ id: e.getAttribute('data-testid') ?? '', kind: e.getAttribute('data-kind') ?? '' })));
  const vOwn = vRows.map((r) => ({ name: r.id.slice('tree-'.length), kind: r.kind })).filter((r) => r.name.length > 0 && !/[\\/]/.test(r.name) && !/-(menu|run|unreadable)$/.test(r.name));
  return {
    roots: vOwn.filter((r) => r.kind === 'solution').map((r) => r.name).sort(),
    projects: vOwn.filter((r) => r.kind === 'project').map((r) => r.name).sort(),
    others: vOwn.filter((r) => r.kind === '').map((r) => r.name).sort(),
  };
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

test('the pages driven here are Chatur\'s: the DevTools port is 9243, its pages are Chatur\'s windows, and the process is the installed Chatur.exe', async () => {
  // Shown once, so a check on this laptop never drives another tool's pages (another tool is checked on port 9223).
  const vList = (await (await fetch(`http://localhost:${RELAY_PORT}/json/list`)).json()) as { title: string; url: string; type: string }[];
  const vPages = vList.filter((p) => p.type === 'page');
  expect(vPages.length, 'pages on the relay').toBeGreaterThan(0);
  for (const vPage of vPages) expect(vPage.url, 'a page of the app\'s own web view').toMatch(/^https:\/\/0\.0\.0\.1\//);  // the origin a MAUI Blazor Hybrid window serves its own pages from
  const vVersion = (await (await fetch(`http://localhost:${RELAY_PORT}/json/version`)).json()) as { Browser: string };
  const vOwner = ps(`(Get-Process -Id ${objMine[0]}).Path`);
  const vTitles = vPages.map((p) => `${p.title || '(no title)'} ${new URL(p.url).pathname}`).join(' | ');
  console.log(`Chatur proof: relay port ${RELAY_PORT} -> web view DevTools port ${DEVTOOLS_PORT}; ${vVersion.Browser}; process ${vOwner}; pages: ${vTitles}`);
  expect(vOwner.toLowerCase()).toMatch(/\\programs\\chatur\\chatur\.exe$/);
  const vBrand = await viewOn(/^\/(sign-in|start|)$/, 120_000);
  await expect(vBrand.getByTestId('brand-mark').or(vBrand.locator('text=Chatur').first()).first()).toBeVisible({ timeout: 60_000 });
  await expect(vBrand).toHaveTitle(/Chatur/i, { timeout: 30_000 }).catch(() => undefined);
});

test('REQ-NFR-019 REQ-NFR-013 REQ-FN-310 REQ-UI-126 REQ-UI-125 REQ-UI-008 owner\'s test 1: the installed Chatur starts on Nothing is open yet; with an empty folder open, New project makes the solution its own folder and the project one inside it, and FILES shows the solution as its root', async () => {
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
  const vSolutionFolder = `${vWin}\\Checking-Chatur`;
  await expect(vMain.getByTestId('new-project-folder')).toHaveText(vWin);
  await expect(vMain.getByTestId('new-project-location')).toHaveCount(0);
  await expect(vMain.getByTestId('new-project-change-location')).toBeVisible();
  await vMain.getByTestId('new-project-solution').fill('Checking-Chatur');
  await expect(vMain.getByTestId('new-project-solution-folder')).toHaveText(vSolutionFolder);
  await expect(vMain.getByTestId('new-project-solution-path')).toHaveText(`${vSolutionFolder}\\Checking-Chatur.slnx`);
  await expect(vMain.getByTestId('new-project-project-path')).toHaveText(`${vSolutionFolder}\\Checking-Chatur`);
  await vMain.screenshot({ path: `${SHOTS}/owner-test-1-new-project.png` });
  await create(vMain);

  // The disk: nothing directly in the folder that was open; the solution's own folder, the solution file in it, the project in its own folder inside.
  const vAfter = await mainWindow();
  await expect(vAfter.getByTestId('project-switch-name')).toHaveText('Checking-Chatur', { timeout: 60_000 });
  expect(readdirSync(vFolder), 'what the folder that was open holds').toEqual(['Checking-Chatur']);
  expect(readdirSync(path.join(vFolder, 'Checking-Chatur')).sort(), 'what the solution\'s folder holds').toEqual(['Checking-Chatur', 'Checking-Chatur.slnx']);
  expect(existsSync(path.join(vFolder, 'Checking-Chatur', 'Checking-Chatur', 'Checking-Chatur.csproj')), 'the project file, in the project\'s own folder').toBe(true);
  expect(readFileSync(path.join(vFolder, 'Checking-Chatur', 'Checking-Chatur.slnx'), 'utf8')).toContain('Checking-Chatur/Checking-Chatur.csproj');
  // FILES: the solution is the root and its project is below it, not beside it (the owner's Solution-Tree-Issue).
  await expect.poll(async () => { const t = await filesTree(vAfter); return { roots: t.roots, projects: t.projects }; }, { timeout: 60_000 }).toEqual({ roots: ['Checking-Chatur.slnx'], projects: ['Checking-Chatur'] });
  await expect(vAfter.getByTestId('target').locator('button')).toContainText('Checking-Chatur', { timeout: 60_000 });
  await vAfter.screenshot({ path: `${SHOTS}/owner-test-1-workbench.png` });
});

test('REQ-NFR-013 REQ-FN-310 REQ-FN-309 owner\'s test 3: with nothing open, New project has one Location field and makes the solution\'s own folder in the picked folder, the project one inside it', async () => {
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
  const vWin = windowsPath(objWork);
  await expect(vStart.getByTestId('new-project-folder')).toHaveCount(0);
  await expect(vStart.getByTestId('new-project-location')).toHaveValue('');
  await vStart.getByTestId('new-project-solution').fill('FromNothing');
  await vStart.getByTestId('new-project-name').fill('TryConsole');
  await vStart.getByTestId('new-project-location').fill(vWin);
  await expect(vStart.getByTestId('new-project-solution-folder')).toHaveText(`${vWin}\\FromNothing`);
  await expect(vStart.getByTestId('new-project-solution-path')).toHaveText(`${vWin}\\FromNothing\\FromNothing.slnx`);
  await expect(vStart.getByTestId('new-project-project-path')).toHaveText(`${vWin}\\FromNothing\\TryConsole`);
  await vStart.screenshot({ path: `${SHOTS}/owner-test-2-new-project.png` });
  await create(vStart);

  const vMain = await mainWindow();
  await expect(vMain.getByTestId('project-switch-name')).toHaveText('FromNothing', { timeout: 60_000 });
  await expect(vMain.getByTestId('target').locator('button')).toContainText('TryConsole', { timeout: 60_000 });
  expect(disk(vFolder), 'the files in the solution\'s folder').toEqual(['FromNothing.slnx', 'TryConsole/Program.cs', 'TryConsole/TryConsole.csproj'].sort());
  await vMain.screenshot({ path: `${SHOTS}/owner-test-2-workbench.png` });
});

test('REQ-UI-112 owner\'s test 4: File New project adds projects to the solution from this machine\'s templates', async () => {
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
    // The project just added is the one that runs, when it can run: the web app is; a library and a test project are not (the owner's Step-3-Issues).
    await expect(vMain.getByTestId('target').locator('button')).toContainText('TryWeb', { timeout: 60_000 });
  }
  await expect.poll(async () => (await filesTree(vMain)).roots, { timeout: 30_000 }).toEqual(['FromNothing.slnx']);
  await expect(vMain.getByTestId('tree-TryWeb-run')).toBeVisible();
  await expect(vMain.getByTestId('tree-TryConsole-run')).toHaveCount(0);

  expect(readdirSync(path.join(objWork, 'FromNothing')).sort(), 'the solution folder').toEqual(['FromNothing.slnx', 'TryConsole', 'TryLib', 'TryLib.Tests', 'TryWeb']);
  await expect(vMain.getByTestId('target').locator('button')).toContainText('TryWeb');
  await vMain.getByTestId('target').locator('button').click();
  await expect(vMain.getByTestId('target-multiple')).toBeVisible();
  await vMain.keyboard.press('Escape');
  await expect(vMain.getByTestId('target-pop')).toHaveCount(0);
  await vMain.getByTestId('target').locator('button').click();
  await expect(vMain.getByTestId('target-pop')).toContainText('TryLib.Tests');
  await vMain.keyboard.press('Escape');
  await vMain.screenshot({ path: `${SHOTS}/owner-test-3-projects.png` });
});

test('REQ-UI-107 REQ-FN-306 owner\'s test 6: running the web app opens Output and the browser, and Stop ends it', async () => {
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

test('REQ-UI-107 REQ-UI-108 owner\'s test 7: the console app runs to its exit code, and a broken build says so', async () => {
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

test('REQ-UI-107 owner\'s test 8: the test project runs its tests', async () => {
  const vMain = await mainWindow();
  await chooseTarget(vMain, 'TryLib.Tests');
  await vMain.getByTestId('run').click();
  const vOutput = vMain.getByTestId('output');
  await expect(vOutput).toContainText('> dotnet test', { timeout: 400_000 });
  await expect(vOutput).toContainText(/Passed!|Test summary: total: \d+, failed: 0/, { timeout: 400_000 });
  await expect(vOutput).toContainText('Process exited with code 0.', { timeout: 60_000 });
});

test('REQ-FN-324 owner\'s test 9: several projects start together from a choice kept for the solution, each line named, and Stop ends them all', async () => {
  const vMain = await mainWindow();
  await expect(vMain.getByTestId('project-switch-name')).toHaveText('FromNothing', { timeout: 60_000 });
  const vPicker = vMain.getByTestId('target').locator('button');
  await expect(vPicker).toBeEnabled({ timeout: 60_000 });
  await vPicker.click();
  await vMain.getByTestId('target-multiple').click();
  const vDialog = vMain.getByTestId('startup-dialog');
  await expect(vDialog).toBeVisible({ timeout: 15_000 });
  // The projects that can run, each once: the console app and the web app; never the library or the test project.
  await expect(vDialog.locator('[data-testid^="startup-row-"]')).toHaveCount(2);
  await expect(vDialog).not.toContainText('TryLib');
  await vDialog.getByTestId('startup-tick-tryconsole-tryconsole-csproj').click();
  await vDialog.getByTestId('startup-tick-tryweb-tryweb-csproj').click();
  await expect(vMain.getByTestId('startup-hint')).toContainText('2 ticked');
  await vMain.screenshot({ path: `${SHOTS}/owner-test-6b-startup-dialog.png` });
  await vMain.getByTestId('startup-save').click();
  await expect(vDialog).toBeHidden({ timeout: 15_000 });
  await expect(vPicker).toContainText('2 startup projects');
  await expect(vMain.getByTestId('tree-TryConsole-run')).toBeVisible();
  await expect(vMain.getByTestId('tree-TryWeb-run')).toBeVisible();

  await vMain.getByTestId('run').click();
  const vOutput = vMain.getByTestId('output');
  await expect(vOutput).toContainText('[TryConsole] Hello, World!', { timeout: 400_000 });
  await expect(vOutput).toContainText('[TryWeb] ', { timeout: 60_000 });
  await expect(vOutput).toContainText('Now listening on:', { timeout: 120_000 });
  await vMain.screenshot({ path: `${SHOTS}/owner-test-6b-run-together.png` });
  await vMain.getByTestId('stop').click();
  await expect(vOutput).toContainText('All 2 projects ended.', { timeout: 90_000 });

  // The choice is kept for the solution; picking one project ends it.
  await chooseTarget(vMain, 'TryWeb');
  await expect(vPicker).toContainText('TryWeb');
  await expect(vPicker).not.toContainText('startup projects');
});

test('REQ-FN-045 REQ-FN-046 REQ-UI-041 owner\'s test 11: the file window has Save and no button that hands the file to another program, and closing the whole view asks about an unsaved file', async () => {
  const vMain = await mainWindow();
  const vProgram = path.join(objWork, 'FromNothing', 'TryConsole', 'Program.cs');
  const vText = readFileSync(vProgram, 'utf8');
  try {
    await vMain.getByTestId('tree-TryConsole').locator('[data-slot=tree-item-row]').first().click();
    await vMain.getByTestId('tree-TryConsole/Program.cs').locator('[data-slot=tree-item-row]').first().click();
    await expect(vMain.getByTestId('tabs').getByText('Program.cs', { exact: true })).toBeVisible({ timeout: 30_000 });
    await expect(vMain.getByTestId('save-file')).toBeVisible();
    await expect(vMain.getByTestId('open-in-editor')).toHaveCount(0);
    await expect(vMain.getByTestId('open-with-default')).toHaveCount(0);
    await vMain.screenshot({ path: `${SHOTS}/owner-test-6c-file-window.png` });
    await vMain.getByTestId('editor').locator('textarea').first().fill('// changed and not saved\n' + vText);
    await expect(vMain.getByTestId('tabs').locator('[data-dirty="true"]').first()).toBeAttached({ timeout: 20_000 });
    await vMain.getByTestId('close-file-view').click();
    const vAsk = vMain.getByTestId('close-unsaved-dialog');
    await expect(vAsk).toBeVisible({ timeout: 20_000 });
    await vMain.screenshot({ path: `${SHOTS}/owner-test-6c-close-view.png` });
    await vMain.getByTestId('close-unsaved-cancel').click();
    await expect(vAsk).toBeHidden();
    await expect(vMain.getByTestId('files-area')).toBeVisible();
    expect(readFileSync(vProgram, 'utf8'), 'nothing was saved by asking').toBe(vText);
    await vMain.getByTestId('close-file-view').click();
    await vMain.getByTestId('close-unsaved-discard').click();
    await expect(vMain.getByTestId('files-area')).toHaveCount(0, { timeout: 20_000 });
    expect(readFileSync(vProgram, 'utf8'), 'discarding left the file as it was').toBe(vText);
  } finally {
    writeFileSync(vProgram, vText);
  }
});

test('REQ-UI-124 REQ-FN-325 REQ-FN-309 REQ-UI-125 owner\'s test 12: a folder with a project and no solution is said so and asked about; New project then makes the solution there, and it is the root of FILES with the new project the one that runs', async () => {
  // The starting point, made as a person makes it: dotnet new in a terminal, and no solution.
  const vFolder = path.join(objWork, 'LooseOne');
  mkdirSync(vFolder, { recursive: true });
  const vWin = windowsPath(vFolder);
  execFileSync('powershell.exe', ['-NoProfile', '-Command', `Set-Location -LiteralPath '${vWin.replace(/'/g, "''")}'; dotnet new console -n Loose -o '${vWin.replace(/'/g, "''")}\\Loose' | Out-Null`], { timeout: 180_000 });
  expect(disk(vFolder)).toEqual(['Loose/Loose.csproj', 'Loose/Program.cs'].sort());
  await signedInStart();
  await expect.poll(() => views().some((p) => /^\/(start|)$/.test(new URL(p.url()).pathname)), { timeout: 120_000 }).toBe(true);
  const vOpenMain = views().find((p) => new URL(p.url()).pathname === '/');
  if (vOpenMain) await choose(vOpenMain, 'file', 'close');
  const vStart = await viewOn(/^\/start/);
  const vWayFolder = vStart.getByTestId('open-folder').or(vStart.getByTestId('way-open-folder')).first();
  const vDialog = vStart.getByTestId('open-project-dialog');
  await expect(async () => {
    if (!(await vDialog.isVisible())) await vWayFolder.click();
    await expect(vDialog).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 60_000 });
  await vStart.getByTestId('open-project-path').fill(vWin);
  await vStart.getByTestId('open-project-go').click();
  // Said so, and asked, before it opens.
  const vAsk = vStart.getByTestId('open-project-no-solution');
  await expect(vAsk).toBeVisible({ timeout: 30_000 });
  await expect(vAsk).toContainText('This folder has no solution');
  await expect(vAsk).toContainText('Loose\\Loose.csproj');
  await vStart.screenshot({ path: `${SHOTS}/owner-test-7-no-solution.png` });
  await vStart.getByTestId('open-project-no-solution-back').click();
  await expect(vAsk).toHaveCount(0);
  await expect(vStart.getByTestId('open-project-path')).toHaveValue(vWin);
  await vStart.getByTestId('open-project-go').click();
  await expect(vAsk).toBeVisible({ timeout: 30_000 });
  await vStart.getByTestId('open-project-no-solution-go').click();
  const vMain = await mainWindow();
  await expect(vMain.getByTestId('project-switch-name')).toHaveText('LooseOne', { timeout: 60_000 });

  await newProject(vMain);
  await pickTemplate(vMain, 'console');
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
  expect(disk(vFolder), 'the files in the folder that was open').toEqual(['Gathered.slnx', 'Loose/Loose.csproj', 'Loose/Program.cs', 'Second/Program.cs', 'Second/Second.csproj'].sort());
  const vSlnText = readFileSync(path.join(vFolder, 'Gathered.slnx'), 'utf8');
  expect(vSlnText).toContain('Loose/Loose.csproj');
  expect(vSlnText).toContain('Second/Second.csproj');
  // The solution is the root and both projects are below it; the project just added is the one that runs.
  await expect.poll(async () => { const t = await filesTree(vAfter); return { roots: t.roots, projects: t.projects }; }, { timeout: 60_000 }).toEqual({ roots: ['Gathered.slnx'], projects: ['Loose', 'Second'] });
  await expect(vAfter.getByTestId('target').locator('button')).toContainText('Second', { timeout: 60_000 });
  await expect(vAfter.getByTestId('tree-Second-run')).toBeVisible();
  await expect(vAfter.getByTestId('tree-Loose-run')).toHaveCount(0);
  await vAfter.screenshot({ path: `${SHOTS}/owner-test-7-workbench.png` });
});

test('REQ-FN-308 REQ-FN-311 REQ-UI-114 REQ-FN-312 REQ-FN-040 owner\'s test 15: in an empty folder the Analyst has its commands and chatting leaves no file there, then it brainstorms with the person and writes the brief', async () => {
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
  // After *yolo, as after a long reply, the conversation is at the newest message (the owner's step 8, REQ-UI-128).
  await expect.poll(() => chatGap(vMain), { timeout: 20_000 }).toBeLessThanOrEqual(4);
  await vMain.screenshot({ path: `${SHOTS}/owner-test-8-newest.png` });
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
  // The sessions and their tokens are written under the opened project's docs/metrics, in TechieFlow's shape (owner's step 10).
  const vSessions = path.join(vFolder, 'docs', 'metrics', 'sessions.jsonl');
  expect(existsSync(vSessions), 'docs/metrics/sessions.jsonl was written by the real chat').toBe(true);
  const vRecords = readFileSync(vSessions, 'utf8').split('\n').filter((l) => l.trim()).map((l) => JSON.parse(l));
  expect(vRecords.length).toBeGreaterThan(0);
  const vLast = vRecords[vRecords.length - 1];
  expect(vLast.v).toBe(1);
  expect(vLast.ts).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$/);
  expect(vLast.harness).toBe('chatur');
  expect(vLast.app).toBe('BrainTest');
  expect(typeof vLast.input_tokens).toBe('number');
  expect(typeof vLast.output_tokens).toBe('number');
  expect(vLast.output_tokens).toBeGreaterThan(0);
  console.log(`BrainTest sessions.jsonl: ${vRecords.length} record(s); the last session used ${vLast.input_tokens} in, ${vLast.output_tokens} out on ${vLast.model}`);
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
  await goOnIfAsked(vStart);
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

const BRAIN_FOLDER = () => path.join(objWork, 'BrainTest');

/** Picks a project in the project switcher of the main window, as the person does: no path is typed. */
async function switchProject(aMain: Page, aName: string): Promise<void> {
  if ((await aMain.getByTestId('project-switch-name').innerText()).trim() === aName) return;
  const vPop = aMain.getByTestId('project-switch-pop');
  await expect(async () => {
    if (!(await vPop.isVisible())) await aMain.getByTestId('project-switch').click();
    await expect(vPop).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
  await vPop.getByTestId('project-switch-item-name').filter({ hasText: new RegExp(`^${aName}$`) }).first().click();
  await expect(aMain.getByTestId('project-switch-name')).toHaveText(aName, { timeout: 30_000 });
}

test('REQ-NFR-017 REQ-UI-115 REQ-FN-040 REQ-UI-120 REQ-FN-313 REQ-FN-314 REQ-UI-121 owner\'s test 19: pick the project in the app, then Project Measurements shows the chat sessions and their tokens, a line opens, the zip is saved, and every project is listed', async () => {
  // The project is BrainTest, the one chatted in at test 8 and 9: chosen in the app, nothing typed. What it shows is read
  // only from the docs/metrics that chat wrote.
  await signedInStart();
  const vFolder = BRAIN_FOLDER();
  const vMain = await mainWindow();
  await switchProject(vMain, 'BrainTest');

  await choose(vMain, 'project', 'measurements');
  const vPage = await viewOn(/^\/measurements$/);
  await expect(vPage.getByTestId('metrics-project')).toHaveText('BrainTest', { timeout: 60_000 });
  await expect(vPage.getByTestId('tile-runs-value')).not.toHaveText('—', { timeout: 60_000 });
  const vAll = Number((await vPage.getByTestId('tile-runs-value').innerText()).replace(/,/g, ''));
  expect(vAll, 'chat sessions recorded in all history').toBeGreaterThan(0);
  // The tokens the real chat used are on the screen, not "not measured".
  await expect(vPage.getByTestId('tile-tokens-out-value')).not.toHaveText('not measured');
  expect(Number((await vPage.getByTestId('tile-tokens-out-value').innerText()).replace(/,/g, '')), 'tokens out').toBeGreaterThan(0);
  expect(Number((await vPage.getByTestId('tile-tokens-in-value').innerText()).replace(/,/g, '')), 'tokens in').toBeGreaterThan(0);
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
  await expect(vPage.getByTestId('across-row-braintest')).toBeVisible({ timeout: 60_000 });
  await vPage.screenshot({ path: `${SHOTS}/owner-test-10-across.png` });

  await vPage.getByTestId('nav-measurements').click();
  await vPage.getByTestId('open-measurements-screen').click();
  await expect(vPage.getByTestId('metrics-heading')).toBeVisible({ timeout: 30_000 });
});

/** The keys panel's line for the test provider, found by its name. */
async function keyRowOf(aPage: Page, aName: string) {
  const vRow = aPage.locator('[data-testid^="key-row-"]').filter({ has: aPage.getByText(aName, { exact: true }) }).first();
  await expect(vRow).toBeVisible({ timeout: 60_000 });
  const vId = (await vRow.getAttribute('data-testid'))!.replace('key-row-', '');
  return { row: vRow, n: vId };
}

test('REQ-UI-129 REQ-UI-131 REQ-FN-326 REQ-NFR-022 owner\'s test 21: what is left on each key reads not known, then counts against a typed limit, on both measurements screens', async () => {
  await signedInStart();
  const vMain = await mainWindow();
  await switchProject(vMain, 'BrainTest');
  const vProvider = await connectProvider(vMain);
  await choose(vMain, 'project', 'measurements');
  const vPage = await viewOn(/^\/measurements$/);
  await expect(vPage.getByTestId('keys-panel')).toBeVisible({ timeout: 60_000 });
  const { n } = await keyRowOf(vPage, vProvider.name);

  // 2) An Anthropic key gives no balance and no limit is typed yet: "not known", never a number.
  await expect(vPage.getByTestId(`key-source-${n}`)).toHaveText('not known');
  await expect(vPage.getByTestId(`key-left-${n}`)).toHaveText('not known');

  // 3) A limit of $5.00 from the first of this month: Chatur's own count of the priced calls made since.
  await vPage.getByTestId(`key-set-limit-${n}`).click();
  await expect(vPage.getByTestId('limit-dialog')).toBeVisible();
  const vAmount = vPage.locator('input[data-testid=limit-amount], [data-testid=limit-amount] input').first();
  await vAmount.fill('5.00');
  await vPage.getByTestId('limit-save').click();
  await expect(vPage.getByTestId(`key-source-${n}`)).toHaveText("Chatur's own count", { timeout: 30_000 });
  await expect(vPage.getByTestId(`key-limit-${n}`)).toHaveText('$5.00');
  await expect(vPage.getByTestId(`key-used-${n}`)).toHaveText(/^\$\d+\.\d{2,4}$/);
  const vUsed = Number((await vPage.getByTestId(`key-used-${n}`).innerText()).replace('$', ''));
  expect(vUsed, 'the chats of the parts above were priced and counted').toBeGreaterThan(0);
  const vLeft = Number((await vPage.getByTestId(`key-left-${n}`).innerText()).replace('$', ''));
  expect(Math.abs(vLeft - (5 - vUsed)), 'Left is the limit less what was used').toBeLessThan(0.0101);
  await vPage.screenshot({ path: `${SHOTS}/owner-test-21-keys.png`, fullPage: true });

  // 4) The same line under every project.
  await vPage.getByTestId('nav-measurements-all').click();
  await expect(vPage.getByTestId('across-heading')).toBeVisible({ timeout: 30_000 });
  const vAcross = await keyRowOf(vPage, vProvider.name);
  await expect(vPage.getByTestId(`key-limit-${vAcross.n}`)).toHaveText('$5.00');

  // 5) Remove the limit: "not known" again.
  await vPage.getByTestId('nav-project-measurements').click();
  const vBack = await keyRowOf(vPage, vProvider.name);
  await vPage.getByTestId(`key-set-limit-${vBack.n}`).click();
  await vPage.getByTestId('limit-clear').click();
  await expect(vPage.getByTestId(`key-source-${vBack.n}`)).toHaveText('not known', { timeout: 30_000 });
});

/** On Compare models, ticks exactly the named models. */
async function pickModels(aPage: Page, aModels: string[]): Promise<void> {
  for (const vBox of await aPage.locator('[data-testid^="model-check-"]').all()) {
    const vId = (await vBox.getAttribute('data-testid'))!.replace('model-check-', '');
    const vOn = (await vBox.getAttribute('aria-checked')) === 'true' || (await vBox.getAttribute('data-state')) === 'checked';
    if (vOn !== aModels.includes(vId)) await vBox.click();
  }
}

test('REQ-FN-327 REQ-FN-328 REQ-UI-132 REQ-UI-133 REQ-FN-330 REQ-NFR-021 REQ-NFR-022 owner\'s test 22: one question to two models, side by side with time, tokens and cost, marked and opened again', async () => {
  const vFolder = BRAIN_FOLDER();
  await signedInStart();
  const vMain = await mainWindow();
  await switchProject(vMain, 'BrainTest');
  await connectProvider(vMain);
  await choose(vMain, 'project', 'measurements');
  const vPage = await viewOn(/^\/measurements$/);
  await vPage.getByTestId('nav-compare-models').click();
  await expect(vPage.getByTestId('compare-project')).toHaveText('BrainTest', { timeout: 30_000 });

  // 2) One model is refused.
  await pickModels(vPage, ['claude-haiku-5-5']);
  await vPage.getByTestId('compare-run').click();
  await expect(vPage.getByTestId('compare-pick-count')).toContainText('One model is not a comparison');

  // 3)–6) Two models, a read-only question.
  await pickModels(vPage, ['claude-haiku-5-5', 'claude-sonnet-5-5']);
  await vPage.getByTestId('compare-kind-button').click();
  await vPage.getByTestId('kind-question').click();
  await vPage.getByTestId('compare-task').fill('Which files are in the docs folder of this project? Answer in one sentence.');
  const vRunsBefore = existsSync(path.join(vFolder, 'docs', 'metrics', 'runs.jsonl'))
    ? readFileSync(path.join(vFolder, 'docs', 'metrics', 'runs.jsonl'), 'utf8').split('\n').filter((l) => l.includes('"compare-models"')).length
    : 0;
  await vPage.getByTestId('compare-run').click();
  await expect(vPage.getByTestId('compare-state')).toHaveText('done', { timeout: 300_000 });
  await expect(vPage.getByTestId('compare-status')).toContainText('nothing in the project changed');
  for (const vN of [1, 2]) {
    await expect(vPage.getByTestId(`result-answer-${vN}`)).not.toHaveText(/^\s*$/);
    await expect(vPage.getByTestId(`result-time-${vN}`)).toHaveText(/\d/);
    await expect(vPage.getByTestId(`result-tokens-${vN}`)).toHaveText(/^[\d,]+ \/ [\d,]+$/);
    await expect(vPage.getByTestId(`result-cost-${vN}`)).toHaveText(/^\$\d+\.\d{2,4}$/);
  }

  // 7) Marks.
  await vPage.getByTestId('mark-right-1').click();
  await vPage.getByTestId('mark-wrong-2').click();
  await expect(vPage.getByTestId('result-mark-1')).toHaveText('right');
  await expect(vPage.getByTestId('result-mark-2')).toHaveText('wrong');
  await vPage.screenshot({ path: `${SHOTS}/owner-test-22-compare.png`, fullPage: true });
  const vRuns = readFileSync(path.join(vFolder, 'docs', 'metrics', 'runs.jsonl'), 'utf8').split('\n').filter((l) => l.includes('"compare-models"'));
  expect(vRuns.length - vRunsBefore, 'two new compare-models lines in runs.jsonl').toBe(2);
  expect(readFileSync(path.join(vFolder, 'docs', 'metrics', 'gates.jsonl'), 'utf8')).toContain('owner-mark');

  // 8) Opened again from Past comparisons.
  await vPage.getByTestId('nav-project-measurements').click();
  await vPage.getByTestId('nav-compare-models').click();
  await vPage.getByTestId('history-row-1').click();
  await expect(vPage.getByTestId('result-mark-1')).toHaveText('right', { timeout: 30_000 });
  await expect(vPage.getByTestId('result-mark-2')).toHaveText('wrong');
});

test('REQ-FN-329 REQ-UI-132 REQ-NFR-022 owner\'s test 23: a task that changes files works in each model\'s own copy under the project\'s bin folder, and the copies are gone', async () => {
  const vFolder = BRAIN_FOLDER();
  await signedInStart();
  const vMain = await mainWindow();
  await switchProject(vMain, 'BrainTest');
  await choose(vMain, 'project', 'measurements');
  const vPage = await viewOn(/^\/measurements$/);
  await vPage.getByTestId('nav-compare-models').click();
  await expect(vPage.getByTestId('compare-project')).toHaveText('BrainTest', { timeout: 30_000 });
  await pickModels(vPage, ['claude-haiku-5-5', 'claude-sonnet-5-5']);
  await vPage.getByTestId('compare-kind-button').click();
  await vPage.getByTestId('kind-change').click();
  await vPage.getByTestId('compare-task').fill('Add a file docs/notes.md with the one line: Made in Compare models.');
  await vPage.getByTestId('compare-run').click();
  await expect(vPage.getByTestId('compare-state')).toHaveText('done', { timeout: 600_000 });
  for (const vN of [1, 2]) await expect(vPage.getByTestId(`result-files-${vN}`)).toContainText('docs/notes.md');
  await vPage.screenshot({ path: `${SHOTS}/owner-test-23-compare-change.png`, fullPage: true });
  expect(existsSync(path.join(vFolder, 'docs', 'notes.md')), 'each model worked in its own copy').toBe(false);
  expect(existsSync(path.join(vFolder, 'bin', 'chatur-compare')), 'the copies are gone').toBe(false);
});

test('REQ-NFR-017 REQ-FN-317 REQ-FN-322 REQ-FN-320 owner\'s test 24: one command typed to each agent runs to its end', async () => {
  const vFolder = BRAIN_FOLDER();
  await signedInStart();
  const vMain = await mainWindow();
  await switchProject(vMain, 'BrainTest');
  const vPrompt = path.join(vFolder, 'docs', 'BrainTest-Research-Prompt.md');

  await connectProvider(vMain);
  await talkTo(vMain, 'analyst');
  await say(vMain, '*research-prompt note-taking apps for one person that work offline');
  await untilWritten(vMain, vPrompt, [
    'Focus 4, competitors. It should inform whether to build it at all; constraints: one developer, no budget; known: Notion, Obsidian and Apple Notes exist. Write the prompt file now.',
    'Write docs/BrainTest-Research-Prompt.md now, no more questions.',
    'Please write the file now.',
  ]);
  const vFirst = statSync(vPrompt).mtimeMs;

  await talkTo(vMain, 'architect');
  await say(vMain, '*research which local database suits a desktop to-do app for one person');
  const vRewritten = () => existsSync(vPrompt) && statSync(vPrompt).mtimeMs > vFirst;
  for (const vNudge of [
    'Focus 5, technology. It should inform the choice of data store; constraints: .NET, works offline, no server; known: SQLite and LiteDB exist. Write the prompt file now.',
    'Write docs/BrainTest-Research-Prompt.md now, no more questions.',
    'Please write the file now.',
  ]) {
    if (vRewritten()) break;
    await say(vMain, vNudge);
  }
  expect(vRewritten(), 'the Architect wrote the research prompt again').toBe(true);

  await talkTo(vMain, 'flow-master');
  await expect(await say(vMain, '*develop-status BrainTest')).toContainText('No end-to-end run');

  await talkTo(vMain, 'verifier');
  // BrainTest has no checklist, and the Verifier says so plainly instead of an empty table.
  const vReport = await say(vMain, '*report all BrainTest');
  await expect(vReport).toContainText('no checklist yet');
  await vMain.screenshot({ path: `${SHOTS}/owner-test-11-verifier.png` });
});
