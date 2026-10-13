// The desktop app, used the way the owner uses it (fix-issues 2026-10-13, owner's test of the 12 Oct
// nightly): every page that lives in the secondary window is reached by clicking its menu item in the
// MAIN window, and the chat answers through a provider added on that Settings page. Runs only against
// a desktop head over CDP (tf-verify-boot.sh --head windows → CDP_URL); against the web harness every
// test here is skipped, never passed. The web harness's own specs carry each row's acceptance line;
// these carry what the browser cannot show: that the desktop app reaches the screen at all.
import { test, expect, chromium, Browser, Page } from '@playwright/test';
import { testProvider } from './support/provider';

const CDP_URL = process.env.CDP_URL ?? '';
test.skip(!CDP_URL, 'desktop head only — boot it with tf-verify-boot.sh --head windows');
test.describe.configure({ mode: 'serial', timeout: 180_000 });

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
});

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
    await vSettings.getByTestId('connector-compatible').click();
    await vField('field-address').fill(vProvider.baseUrl);
    await vField('field-key').fill(vProvider.key);
    await vSettings.getByTestId('dialog-add').click();
    await expect(vSettings.getByTestId('add-provider-dialog')).toHaveCount(0, { timeout: 90_000 });
  }
  await expect(vSettings.getByTestId('providers-table').getByText(vProvider.name, { exact: true })).toBeVisible();

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
