// Acceptance tests: Licence screen (REQ-UI-064..066, REQ-FN-075). Black-box, web harness.
// Rule: no account on the shared App Manager is created, changed or has a device forgotten. The harness holds
// a test licence in memory (the testLicence block of test-settings.json) and these tests drive that one: its
// name, state and days, its device cap and over-cap warning, its three machines (one forgettable) and its
// feature switches. /test/licence/reset puts it back before and after every test.
import { test, expect, Page } from '@playwright/test';
import { signIn } from './support/auth';
import { testSettings } from './support/settings';

test.setTimeout(120_000);

const licence = () => {
  const vBlock = testSettings().testLicence;
  if (!vBlock?.enabled) {
    throw new Error('not measured: testLicence is not enabled in tests/verify/test-settings.json, so the harness would ask the shared App Manager');
  }
  return vBlock;
};

async function resetLicence(aPage: Page): Promise<void> {
  const vResponse = await aPage.request.post('/test/licence/reset');
  expect(vResponse.status(), 'the harness holds the test licence (Development, testLicence enabled)').toBe(200);
}

test.beforeEach(async ({ page }) => {
  licence();
  await expect(async () => {
    await signIn(page, 'Owner');
  }).toPass({ timeout: 120_000, intervals: [500] });
  await resetLicence(page);
});

test.afterEach(async ({ page }) => {
  await resetLicence(page);
});

async function openLicence(aPage: Page): Promise<void> {
  await aPage.goto('/licence', { waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('licence-panel')).toBeVisible();
  await expect(aPage.getByTestId('licence-name')).toBeVisible();
}

test('REQ-UI-064 Licence shows the licence name, state and days remaining', async ({ page }) => {
  // REQ-UI-064: when Licence opens, the licence's name, state and days remaining are shown.
  // Scenario: sign in as Owner, open /licence, read the name, the state pill and the days-left line against
  // the test licence's own values.
  const vLicence = licence();
  await openLicence(page);
  await expect(page.getByTestId('licence-name')).toHaveText(vLicence.name);
  await expect(page.getByTestId('licence-state')).toHaveText(new RegExp(`^${vLicence.state}$`, 'i'));
  await expect(page.getByTestId('licence-days')).toHaveText(`${vLicence.daysRemaining} days left`);
});

test('REQ-UI-065 Licence shows the over-cap warning the server sent', async ({ page }) => {
  // REQ-UI-065: when the account is over the licence's device cap, Licence shows the server's warning.
  // Scenario: the test licence has a cap of 1 and two machines signed in (the third is blocked). The warning
  // text must be the configured text, word for word; once one machine is forgotten the account is within the
  // cap and the warning is gone.
  const vLicence = licence();
  await openLicence(page);
  await expect(page.getByTestId('licence-warning')).toBeVisible();
  await expect(page.getByTestId('licence-warning-message')).toHaveText(vLicence.overCapWarning);
  await expect(page.getByTestId('licence-warning-title')).toContainText(`2 devices against a cap of ${vLicence.deviceCap}`);

  await page.getByTestId('forget-win').click();
  await expect(page.getByTestId('dev-row-win')).toHaveCount(0);
  await expect(page.getByTestId('licence-warning')).toHaveCount(0);
});

test('REQ-UI-066 Forgetting a device removes it from the list and ends its sign-in', async ({ page }) => {
  // REQ-UI-066: when the owner forgets a device, it leaves the list and its sign-in no longer works.
  // Scenario: the test licence's machine "win" can sign in; press its Forget; its row is gone, the count drops,
  // the other machine is untouched, and the harness's sign-in check for "win" is refused while "mac" is not.
  const vLicence = licence();
  await openLicence(page);
  const vKnown = vLicence.devices.length;
  await expect(page.getByTestId('devices-count')).toHaveText(`${vKnown} known`);
  expect((await page.request.get('/test/licence/sign-in/win')).status(), 'win signs in before it is forgotten').toBe(200);

  await page.getByTestId('forget-win').click();

  await expect(page.getByTestId('dev-row-win')).toHaveCount(0);
  await expect(page.getByTestId('devices-count')).toHaveText(`${vKnown - 1} known`);
  await expect(page.getByTestId('dev-row-mac')).toBeVisible();
  expect((await page.request.get('/test/licence/sign-in/win')).status(), 'win no longer signs in').toBe(401);
  expect((await page.request.get('/test/licence/sign-in/mac')).status(), 'mac still signs in').toBe(200);
});

test('REQ-FN-075 A feature switched off for the licence is not offered anywhere', async ({ page }) => {
  // REQ-FN-075: when a feature is off for this licence, it is not offered anywhere in Chatur.
  // Scenario: switch Measurements off on Licence; its entry leaves the window's list on Licence and on another
  // secondary page after a full reload; switch it on and every
  // one comes back. Other features' entries stay throughout.
  await openLicence(page);
  await expect(page.getByTestId('nav-measurements')).toBeVisible();

  await page.getByTestId('feature-measurements-switch').click();
  await expect(page.getByTestId('feature-measurements-state')).toHaveText('off');
  await expect(page.getByTestId('nav-measurements')).toHaveCount(0);
  await expect(page.getByTestId('nav-integrations')).toBeVisible();

  await page.goto('/prerequisites', { waitUntil: 'networkidle' });
  await expect(page.getByTestId('nav-prerequisites')).toBeVisible();
  await expect(page.getByTestId('nav-measurements')).toHaveCount(0);
  await expect(page.getByTestId('nav-integrations')).toBeVisible();

  await openLicence(page);
  await page.getByTestId('feature-measurements-switch').click();
  await expect(page.getByTestId('feature-measurements-state')).toHaveText('on');
  await expect(page.getByTestId('nav-measurements')).toBeVisible();
});

test('REQ-FN-075 A feature switched off is not offered in the main window either', async ({ page }) => {
  // REQ-FN-075, the main window: with the board and auto run off on Licence, Project ▸ Board and the three
  // end-to-end entries are not offered; switched on again, they come back.
  await openLicence(page);
  await page.getByTestId('feature-board-switch').click();
  await expect(page.getByTestId('feature-board-state')).toHaveText('off');
  await page.getByTestId('feature-auto-run-switch').click();
  await expect(page.getByTestId('feature-auto-run-state')).toHaveText('off');

  const projectMenu = async () => {
    await page.goto('/', { waitUntil: 'networkidle' });
    const vItems = page.getByTestId('menu-project-items');
    await expect(async () => {
      if (!(await vItems.isVisible())) await page.getByTestId('menu-project').click();
      await expect(vItems).toBeVisible({ timeout: 2_500 });
    }).toPass({ timeout: 30_000 });
    return vItems;
  };
  let vItems = await projectMenu();
  await expect(vItems.getByTestId('menu-project-processes')).toBeVisible();
  for (const vId of ['menu-project-board', 'menu-project-auto', 'menu-project-queue', 'menu-project-report']) {
    await expect(page.getByTestId(vId)).toHaveCount(0);
  }
  await page.keyboard.press('Escape');

  await openLicence(page);
  await page.getByTestId('feature-board-switch').click();
  await expect(page.getByTestId('feature-board-state')).toHaveText('on');
  await page.getByTestId('feature-auto-run-switch').click();
  await expect(page.getByTestId('feature-auto-run-state')).toHaveText('on');
  vItems = await projectMenu();
  for (const vId of ['menu-project-board', 'menu-project-auto', 'menu-project-queue', 'menu-project-report']) {
    await expect(page.getByTestId(vId)).toBeVisible();
  }
  await page.keyboard.press('Escape');
});
