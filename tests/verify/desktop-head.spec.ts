// The desktop app, used the way the owner uses it (fix-issues 2026-10-13, owner's test of the 12 Oct
// nightly): every page that lives in the secondary window is reached by clicking its menu item in the
// MAIN window, and the chat answers through a provider added on that Settings page. Runs only against
// a desktop head over CDP (tf-verify-boot.sh --head windows → CDP_URL); against the web harness every
// test here is skipped, never passed. The web harness's own specs carry each row's acceptance line;
// these carry what the browser cannot show: that the desktop app reaches the screen at all.
import { test, expect, chromium, Browser, Page } from '@playwright/test';
import { existsSync, readFileSync, readdirSync, statSync } from 'fs';
import * as path from 'path';
import { testProvider, connectorItem } from './support/provider';
import { credentials } from './support/auth';
import { ROOT as FIXTURE_ROOT, NAME as FIXTURE_PROJECT, resetFixture } from './support/workbench-a';
import { removeProjectsUnder } from './support/cleanup';
import {
  CONSOLE_BROKEN, CONSOLE_PROGRAM, ProofSolution, deleteProofSolution, makeProofSolution, webLaunchAddress, writeConsole,
} from './support/run-proof';

// A desktop head started for the tests keeps its own database under tests/.artifacts/app-data/desktop, never the
// owner's application-data database (REQ-NFR-010): it starts signed out with no project, so the run signs in and
// opens a fixture project itself.
const TEST_DB = path.resolve('tests/.artifacts/app-data/desktop/chatur.db');

/** A WSL path under /mnt/<drive>/ as Windows writes it, for the desktop app's own fields. */
const windowsPath = (aPath: string): string =>
  aPath.replace(/^\/mnt\/([a-z])\//, (_m, aDrive: string) => `${aDrive.toUpperCase()}:\\`).replace(/\//g, '\\');

// When this run began: the account's own database written by the chat test must be newer (REQ-NFR-010).
const RUN_STARTED = Date.now();

const CDP_URL = process.env.CDP_URL ?? '';
test.skip(!CDP_URL, 'desktop head only — boot it with tf-verify-boot.sh --head windows');
// Run in file order on one worker (playwright.config.ts) but not serial: a test that fails on the provider's key
// must not skip the checks after it, REQ-NFR-010's among them.
test.describe.configure({ timeout: 180_000 });

let browser: Browser;

/** Every web view of the running app: the main window's and, once open, the secondary window's. */
const views = (): Page[] => browser.contexts().flatMap((aContext) => aContext.pages());

/** The main window: the web view showing the Workbench. */
function mainWindow(): Page {
  const vMain = views().find((aPage) => new URL(aPage.url()).pathname === '/');
  if (!vMain) throw new Error(`No main window on the Workbench; open views: ${views().map((p) => p.url()).join(', ')}`);
  return vMain;
}

/** Clicks a menu item in the main window's menu bar. */
async function choose(aMain: Page, aMenu: string, aItem: string): Promise<void> {
  await aMain.getByTestId(aMenu).click();
  await aMain.getByTestId(aItem).click();
}

/** Waits until a web view other than the main window shows the route, and returns it. */
async function secondaryOn(aMain: Page, aRoute: string): Promise<Page> {
  let vFound: Page | undefined;
  await expect.poll(() => {
    vFound = views().find((aPage) => aPage !== aMain && new URL(aPage.url()).pathname === aRoute);
    return vFound ? 'open' : views().map((p) => new URL(p.url()).pathname).join(',');
  }, { timeout: 30_000 }).toBe('open');
  await expect(vFound!.getByTestId('window-title')).toBeVisible({ timeout: 30_000 });
  return vFound!;
}

test.beforeAll(async () => {
  browser = await chromium.connectOverCDP(CDP_URL);
  await ensureMainWindow();
});

/** Signs in on the start window and opens the fixture project, unless the main window is already up. */
async function ensureMainWindow(): Promise<void> {
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
  resetFixture();
  const vList = vStart.getByTestId('recent-list');
  // With a project already chosen, signing in goes straight to the main window (REQ-FN-006); otherwise to Start.
  const vFolders = vStart.getByTestId('manage-folders').or(vStart.getByTestId('name-a-folder')).first();
  await expect.poll(async () => onMain() || (await vFolders.isVisible().catch(() => false)), { timeout: 60_000 }).toBe(true);
  if (onMain()) {
    await expect(mainWindow().getByTestId('menubar')).toBeVisible({ timeout: 30_000 });
    return;
  }
  if ((await vList.getByText(FIXTURE_PROJECT, { exact: true }).count()) === 0) {
    const vDialog = vStart.getByTestId('folders-dialog');
    await expect(async () => {
      if (!(await vDialog.isVisible())) await vStart.getByTestId('manage-folders').or(vStart.getByTestId('name-a-folder')).first().click();
      await expect(vDialog).toBeVisible({ timeout: 2_500 });
    }).toPass({ timeout: 30_000 });
    await vStart.getByTestId('new-folder-path').fill(windowsPath(FIXTURE_ROOT));
    await vStart.getByTestId('add-folder').click();
    await vStart.getByTestId('folders-dialog-close').click();
  }
  await vList.getByText(FIXTURE_PROJECT, { exact: true }).click({ timeout: 30_000 });
  await expect.poll(onMain, { timeout: 60_000 }).toBe(true);
  await expect(mainWindow().getByTestId('menubar')).toBeVisible({ timeout: 30_000 });
}

const PAGES: { ids: string; menu: string; item: string; route: string; title: string }[] = [
  { ids: 'REQ-FN-011 REQ-UI-017 REQ-UI-018 REQ-FN-012 REQ-FN-013', menu: 'menu-project', item: 'menu-project-prerequisites', route: '/prerequisites', title: 'Prerequisites' },
  { ids: 'REQ-UI-042 REQ-UI-043 REQ-UI-044 REQ-FN-047 REQ-FN-048', menu: 'menu-project', item: 'menu-project-repository', route: '/repository', title: 'Repository' },
  { ids: 'REQ-FN-014 REQ-FN-015 REQ-FN-016 REQ-FN-017 REQ-UI-019 REQ-UI-020', menu: 'menu-tools', item: 'menu-tools-settings', route: '/settings/providers', title: 'Model providers' },
  { ids: 'REQ-FN-018 REQ-FN-019 REQ-FN-020 REQ-FN-021 REQ-FN-022 REQ-FN-023', menu: 'menu-tools', item: 'menu-tools-nav-routing', route: '/settings/routing', title: 'Routing' },
  { ids: 'REQ-UI-025 REQ-UI-026 REQ-FN-028 REQ-FN-029 REQ-UI-027 REQ-UI-028 REQ-UI-029 REQ-UI-030 REQ-FN-030', menu: 'menu-tools', item: 'menu-tools-nav-agents', route: '/settings/agents', title: 'Agents' },
  { ids: 'REQ-UI-034 REQ-FN-037 REQ-FN-038 REQ-FN-039', menu: 'menu-tools', item: 'menu-tools-nav-corrections', route: '/settings/corrections', title: 'Corrections' },
  { ids: 'REQ-FN-040 REQ-FN-041 REQ-FN-042 REQ-FN-043 REQ-UI-035 REQ-UI-036', menu: 'menu-tools', item: 'menu-tools-nav-measurements', route: '/settings/measurements', title: 'Measurements' },
  { ids: 'REQ-UI-037 REQ-UI-038', menu: 'menu-tools', item: 'menu-tools-nav-appearance', route: '/settings/appearance', title: 'Appearance' },
];

for (const vPage of PAGES) {
  test(`REQ-FN-106 ${vPage.ids} ${vPage.title} opens in the secondary window from the main window's menu`, async () => {
    const vMain = mainWindow();
    await choose(vMain, vPage.menu, vPage.item);
    const vSecondary = await secondaryOn(vMain, vPage.route);
    await expect(vSecondary.getByTestId('window-title')).toHaveText(vPage.title);
    // The main window stays on the Workbench: nothing navigated it away.
    expect(new URL(vMain.url()).pathname).toBe('/');
    await expect(vMain.getByTestId('menubar')).toBeVisible();
  });
}

test('REQ-UI-073 REQ-FN-106 Ctrl+, in the main window opens Settings in the secondary window', async () => {
  const vMain = mainWindow();
  await vMain.getByTestId('wordmark').click(); // the key goes to the main window's own view; the name opens no menu
  await vMain.keyboard.press('Control+Comma');
  const vSecondary = await secondaryOn(vMain, '/settings/providers');
  await vSecondary.getByTestId('close-window').click();
  await expect.poll(() => views().filter((aPage) => aPage !== vMain).length, { timeout: 30_000 }).toBe(0);
});

test('REQ-FN-106 the secondary window closes with its Close button and the main window carries on', async () => {
  const vMain = mainWindow();
  await choose(vMain, 'menu-tools', 'menu-tools-settings');
  const vSecondary = await secondaryOn(vMain, '/settings/providers');
  await vSecondary.getByTestId('close-window').click();
  await expect.poll(() => views().filter((aPage) => aPage !== vMain).length, { timeout: 30_000 }).toBe(0);
  await expect(vMain.getByTestId('menubar')).toBeVisible();
});

test('REQ-FN-025 REQ-FN-026 REQ-FN-027 REQ-UI-021 a provider added through Tools ▸ Settings in the desktop app makes the chat answer', async () => {
  const vProvider = testProvider();
  const vMain = mainWindow();
  await choose(vMain, 'menu-tools', 'menu-tools-settings');
  const vSettings = await secondaryOn(vMain, '/settings/providers');
  const vField = (aId: string) => vSettings.locator(`input[data-testid=${aId}], [data-testid=${aId}] input`).first();
  await vSettings.waitForTimeout(1500);
  if (await vSettings.getByTestId('providers-table').getByText(vProvider.name, { exact: true }).count() === 0) {
    await vSettings.getByTestId('add-provider').click();
    await vField('field-name').fill(vProvider.name);
    await vSettings.getByTestId('field-connector-button').click();
    await vSettings.getByTestId(connectorItem(vProvider.connector)).click();
    if (vProvider.baseUrl) await vField('field-address').fill(vProvider.baseUrl);
    await vField('field-key').fill(vProvider.key);
    await vSettings.getByTestId('dialog-add').click();
    await expect(vSettings.getByTestId('add-provider-dialog')).toHaveCount(0, { timeout: 90_000 });
  }
  await expect(vSettings.getByTestId('providers-table').getByText(vProvider.name, { exact: true }).first()).toBeVisible();

  // Back in the main window: choose the model and ask.
  await vMain.getByTestId('model-pick').click();
  await vMain.getByTestId('model-pop').getByText(vProvider.model, { exact: true }).click();
  const vWord = `READY${Date.now() % 100000}`;
  await vMain.locator('textarea').first().fill(`Reply with the single word ${vWord} and nothing else.`);
  await vMain.getByTestId('send').click();
  const vConversation = vMain.getByTestId('conversation');
  await expect(vConversation).toContainText(vWord, { timeout: 120_000 });
  // The reply names the model that produced it, and the tokens it used are shown.
  await expect(vConversation).toContainText(vProvider.model);
  await expect(vMain.getByTestId('cost')).toContainText('tokens');
});

test('REQ-UI-073 REQ-FN-007 REQ-FN-044 no menu of the desktop app shows "Nothing to do yet"', async () => {
  const vMain = mainWindow();
  for (const vMenu of ['menu-file', 'menu-edit', 'menu-view', 'menu-project', 'menu-tools', 'menu-help']) {
    await vMain.getByTestId(vMenu).click();
    const vItems = vMain.getByTestId(`${vMenu}-items`);
    await expect(vItems).toBeVisible();
    await expect(vItems).not.toContainText('Nothing to do yet');
    expect((await vItems.innerText()).trim().length).toBeGreaterThan(0);
    await vMain.keyboard.press('Escape');
  }
});

// ---- The owner's evening test of 21 Nov 2026, in the desktop app itself -------------------------------------------
// Run (points 1.1 to 1.4) on a real Blazor web app, a console app and a class library with tests made with the Windows
// dotnet under this test project's bin folder; the project switcher (point 2); and a closed window that leaves no
// "Unobserved task exception" behind (point 5). The proof solution is removed from the test account and deleted after.
const PROOF_STAMP = Date.now();
let proof: ProofSolution | undefined;
const LOGS = path.resolve('tests/.artifacts/app-data/desktop/logs');

/** How many "Unobserved task exception" lines the desktop head's own logs hold. */
function unobservedCount(): number {
  if (!existsSync(LOGS)) return 0;
  return readdirSync(LOGS).filter((f) => f.endsWith('.log'))
    .reduce((aSum, f) => aSum + (readFileSync(path.join(LOGS, f), 'utf8').match(/Unobserved task exception/g)?.length ?? 0), 0);
}

/** Opens the proof solution in the main window through File > Open a solution. */
async function openProof(aMain: Page): Promise<void> {
  if (!proof) proof = makeProofSolution('windows', PROOF_STAMP);
  // The open project must be this run's proof solution, not one of the same name an earlier run made and removed.
  if (((await aMain.getByTestId('project-switch').getAttribute('title')) ?? '').includes(proof.solutionForApp)) return;
  await choose(aMain, 'menu-file', 'menu-file-open-solution');
  await aMain.locator('input[data-testid=open-project-path], [data-testid=open-project-path] input').first().fill(proof.solutionForApp);
  await aMain.getByTestId('open-project-go').click();
  await expect(aMain.getByTestId('project-switch-name')).toHaveText('ProofApps', { timeout: 60_000 });
}

/** Chooses a run target by its name. */
async function chooseRunTarget(aMain: Page, aName: string): Promise<void> {
  await expect(async () => {
    if (!(await aMain.getByTestId('target-pop').isVisible())) await aMain.locator('[data-testid=target] button').first().click();
    await expect(aMain.getByTestId('target-pop')).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
  await aMain.getByTestId('target-pop').getByText(aName, { exact: true }).click();
  await expect(aMain.locator('[data-testid=target] button').first()).toContainText(aName);
}

test.afterAll(async () => {
  if (!proof) return;
  // The app lets go of the proof folder first (it watches the open project's folder), and every program the runs
  // started is ended, so Windows lets the folder be deleted.
  try {
    const vMain = mainWindow();
    if (((await vMain.getByTestId('project-switch').getAttribute('title')) ?? '').includes(proof.solutionForApp)) {
      await vMain.getByTestId('project-switch').click();
      await vMain.getByTestId('project-switch-pop').locator('button[data-testid^=project-switch-]').filter({ has: vMain.getByTestId('project-switch-item-name').getByText(FIXTURE_PROJECT, { exact: true }) }).first().click();
      await expect(vMain.getByTestId('project-switch-name')).toHaveText(FIXTURE_PROJECT, { timeout: 30_000 });
    }
  } catch {
    /* the window is gone: nothing holds the folder */
  }
  deleteProofSolution({ ...proof, root: proof.root }, 'windows');
  removeProjectsUnder(proof.root);
});

test('REQ-UI-107 REQ-UI-108 in the desktop app, running the console app opens Output by itself and shows the build, the program live and its exit code', async () => {
  test.setTimeout(600_000);
  const vMain = mainWindow();
  await openProof(vMain);
  writeConsole(proof!, CONSOLE_PROGRAM);
  await chooseRunTarget(vMain, 'ProofConsole');
  if (await vMain.getByTestId('output').isVisible()) {
    await choose(vMain, 'menu-view', 'menu-view-output');
    await vMain.keyboard.press('Escape');
    await expect(vMain.getByTestId('output')).toBeHidden({ timeout: 10_000 });
  }
  await vMain.getByTestId('run').click();
  const vOutput = vMain.getByTestId('output');
  await expect(vOutput, 'Output opened by itself').toBeVisible({ timeout: 10_000 });
  await expect(vOutput).toContainText('> dotnet build', { timeout: 30_000 });
  await expect(vOutput).toContainText(/Built in \d+(\.\d+)?s/, { timeout: 300_000 });
  await expect(vOutput).toContainText('Proof console starting', { timeout: 120_000 });
  await expect(vOutput).toContainText('Process exited with code 3.', { timeout: 60_000 });
  await expect(vMain.getByTestId('output-status')).toHaveText('failed, exit code 3');
  await vMain.screenshot({ path: 'tests/.artifacts/verify/shots/desktop-REQ-UI-107-console.png' });
});

test('REQ-UI-108 in the desktop app, a run whose build fails says so with its errors and starts nothing', async () => {
  test.setTimeout(400_000);
  const vMain = mainWindow();
  await openProof(vMain);
  writeConsole(proof!, CONSOLE_BROKEN);
  try {
    await chooseRunTarget(vMain, 'ProofConsole');
    await vMain.getByTestId('run').click();
    const vOutput = vMain.getByTestId('output');
    await expect(vOutput).toContainText('The program was not started.', { timeout: 300_000 });
    await expect(vOutput).toContainText('error CS1002');
    await expect(vMain.getByTestId('output-status')).toHaveText('build failed');
    await expect(vMain.getByTestId('output-error').first()).toContainText('ProofConsole/Program.cs:1');
  } finally {
    writeConsole(proof!, CONSOLE_PROGRAM);
  }
});

test('REQ-FN-306 in the desktop app, running the Blazor web app opens the browser at its launch-settings address once it is up', async () => {
  test.setTimeout(600_000);
  const vMain = mainWindow();
  await openProof(vMain);
  const vAddress = webLaunchAddress(proof!);
  await chooseRunTarget(vMain, 'ProofWeb');
  await vMain.getByTestId('run').click();
  const vOutput = vMain.getByTestId('output');
  await expect(vOutput).toContainText('--launch-profile', { timeout: 300_000 });
  await expect(vOutput).toContainText('Now listening on:', { timeout: 180_000 });
  await expect(vOutput).toContainText(`Opening ${vAddress} in the browser`, { timeout: 30_000 });
  await expect(vMain.getByTestId('stop')).toBeVisible();
  const vResponse = await vMain.request.get(vAddress, { ignoreHTTPSErrors: true, timeout: 30_000 });
  expect(vResponse.status()).toBe(200);
  await vMain.screenshot({ path: 'tests/.artifacts/verify/shots/desktop-REQ-FN-306-web.png' });
  await vMain.getByTestId('stop').click();
  await expect(vOutput).toContainText('Run stopped.', { timeout: 60_000 });
});

test('REQ-UI-107 in the desktop app, running the class library\'s test project runs its tests and shows the exit code', async () => {
  test.setTimeout(600_000);
  const vMain = mainWindow();
  await openProof(vMain);
  await chooseRunTarget(vMain, 'ProofLib.Tests');
  await vMain.getByTestId('run').click();
  const vOutput = vMain.getByTestId('output');
  await expect(vOutput).toContainText('> dotnet test', { timeout: 300_000 });
  await expect(vOutput).toContainText('Process exited with code 0.', { timeout: 300_000 });
  await expect(vMain.getByTestId('output-status')).toHaveText('ended, exit code 0');
});

test('REQ-FN-307 REQ-UI-109 REQ-UI-110 in the desktop app, the switcher names the open project, lists every project and switches on a pick', async () => {
  const vMain = mainWindow();
  await openProof(vMain);
  await expect(vMain.getByTestId('project-switch-label')).toHaveText('Project:');
  await vMain.getByTestId('project-switch').click();
  const vPop = vMain.getByTestId('project-switch-pop');
  await expect(vPop).toBeVisible({ timeout: 10_000 });
  await expect(vPop).not.toContainText('All projects');
  const vOther = vPop.locator('button[data-testid^=project-switch-]').filter({ has: vMain.getByTestId('project-switch-item-name').getByText(FIXTURE_PROJECT, { exact: true }) }).first();
  await vOther.click();
  await expect(vMain.getByTestId('project-switch-name')).toHaveText(FIXTURE_PROJECT, { timeout: 30_000 });
  await expect(vMain.getByTestId('file-tree')).toBeVisible();
  await vMain.getByTestId('project-switch').click();
  await expect(vMain.getByTestId('project-switch-pop').locator('[aria-current=true]')).toContainText(FIXTURE_PROJECT);
  await vMain.getByTestId('project-switch-pop').locator('button[data-testid^=project-switch-]').filter({ has: vMain.getByTestId('project-switch-item-name').getByText('ProofApps', { exact: true }) }).first().click();
  await expect(vMain.getByTestId('project-switch-name')).toHaveText('ProofApps', { timeout: 30_000 });
});

test('REQ-NFR-011 in the desktop app, closing the secondary window and switching project after leaves no unobserved task exception', async () => {
  test.setTimeout(300_000);
  const vMain = mainWindow();
  await openProof(vMain);
  const vBefore = unobservedCount();
  for (let vRound = 0; vRound < 2; vRound++) {
    await choose(vMain, 'menu-tools', 'menu-tools-settings');
    const vSettings = await secondaryOn(vMain, '/settings/providers');
    await vSettings.getByTestId('close-window').click();
    await expect.poll(() => views().filter((aPage) => aPage !== vMain).length, { timeout: 30_000 }).toBe(0);
  }
  // Every app-wide change a closed window's pages used to draw for: two project switches, then time for the
  // finalizer that reports unobserved task exceptions to run.
  for (const vName of [FIXTURE_PROJECT, 'ProofApps', FIXTURE_PROJECT, 'ProofApps']) {
    await vMain.getByTestId('project-switch').click();
    await vMain.getByTestId('project-switch-pop').locator('button[data-testid^=project-switch-]').filter({ has: vMain.getByTestId('project-switch-item-name').getByText(vName, { exact: true }) }).first().click();
    await expect(vMain.getByTestId('project-switch-name')).toHaveText(vName, { timeout: 30_000 });
  }
  for (let vI = 0; vI < 6; vI++) {
    await choose(vMain, 'menu-view', 'menu-view-files');
    await vMain.keyboard.press('Escape');
    await vMain.waitForTimeout(5_000);
  }
  expect(unobservedCount() - vBefore, 'new "Unobserved task exception" lines in the desktop log').toBe(0);
});

test('REQ-FN-009 Sign out chosen in the main window of the desktop app shows Sign in, and signing in again works', async () => {
  // MISS of 2026-11-16: with a project open, Sign out left the window on "Loading Chatur" until the app was
  // closed and opened again. The page reloads on sign-out, and the menu bar's clean-up waited for an answer from
  // the old page's script, which never came, so the new page was never attached.
  const vMain = mainWindow();
  await vMain.locator('[data-testid=account] button').first().click();
  await vMain.getByTestId('sign-out').click();
  await expect.poll(() => views().map((aPage) => new URL(aPage.url()).pathname).join(','), { timeout: 30_000 }).toContain('/sign-in');
  const vSignIn = views().find((aPage) => new URL(aPage.url()).pathname === '/sign-in')!;
  await expect(vSignIn.getByTestId('input-email'), 'Sign in is drawn, not "Loading Chatur"').toBeVisible({ timeout: 30_000 });
  await expect(vSignIn.locator('body')).not.toContainText('Loading Chatur');

  // Sign in again and go back to the project, so the app is left the way the other tests found it.
  const { email, password } = credentials('Owner');
  await vSignIn.getByTestId('input-email').fill(email);
  await vSignIn.getByTestId('input-password').fill(password);
  await vSignIn.locator('[data-testid=signin-form] button[type=submit]').first().click();
  const vPaths = () => views().map((aPage) => new URL(aPage.url()).pathname);
  await expect.poll(() => vPaths().some((aPath) => aPath === '/' || aPath === '/start'), { timeout: 60_000 }).toBe(true);
  if (!vPaths().includes('/')) await ensureMainWindow();
  await expect(mainWindow().getByTestId('menubar')).toBeVisible({ timeout: 30_000 });
});

test('REQ-NFR-010 the desktop head under test keeps its database under tests/.artifacts and leaves the owner database alone', async () => {
  // Runs last: the chat test above has written a message and chosen a model by now. The proof is read from
  // the test's own data folder only. The owner's application-data folder is never opened, not even to read
  // a timestamp (owner, 18 Nov 2026; MISS-Chatur-20261117-03): where the app writes is shown by where the
  // chat it just had was written. TestDataGuardTests prove the guard refuses the owner's folder.
  expect(existsSync(TEST_DB), `the install's test database ${TEST_DB} exists`).toBe(true);
  expect(statSync(TEST_DB).size).toBeGreaterThan(0);
  const vAccounts = path.join(path.dirname(TEST_DB), 'accounts');
  const vWritten = existsSync(vAccounts)
    ? readdirSync(vAccounts).map((aFolder) => path.join(vAccounts, aFolder, 'chatur.db')).filter((aDb) => existsSync(aDb))
    : [];
  expect(vWritten.length, `an account database under ${vAccounts}`).toBeGreaterThan(0);
  const vNewest = Math.max(...vWritten.map((aDb) => statSync(aDb).mtimeMs));
  expect(vNewest, 'the signed-in account wrote its chat to the test folder during this run').toBeGreaterThanOrEqual(RUN_STARTED - 1_000);
});

