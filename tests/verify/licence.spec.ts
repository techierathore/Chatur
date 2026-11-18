// Acceptance tests: Licence screen (REQ-UI-064..066, REQ-FN-075). Black-box, web harness.
// Rules: no account on the shared App Manager is created, changed or has a device forgotten. A row that
// needs a licence source the app does not offer a test seam for fails with "not measured:".
import { test, expect, Page } from '@playwright/test';
import { signIn } from './support/auth';

test.setTimeout(120_000);

test.beforeEach(async ({ page }) => {
  await expect(async () => {
    await signIn(page, 'Owner');
  }).toPass({ timeout: 120_000, intervals: [500] });
});

async function openLicence(aPage: Page): Promise<void> {
  await aPage.goto('/licence', { waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('licence-panel')).toBeVisible();
}

test('REQ-UI-064 Licence shows the licence name, state and days remaining', async ({ page }) => {
  // REQ-UI-064: when Licence opens, the licence's name, state and days remaining are shown.
  // Scenario: sign in as Owner, open /licence, read the licence panel: a non-empty title, a state badge and
  // a "<n> days remaining" line must be visible.
  await openLicence(page);
  const vPanel = page.getByTestId('licence-panel');
  const vName = (await vPanel.locator('h1,h2,h3,[data-slot=card-title]').first().innerText()).trim();
  expect(vName.length, 'licence name is shown').toBeGreaterThan(0);
  const vState = page.getByTestId('licence-state');
  await expect(vState).toBeVisible();
  await expect(vState).toContainText(/\b\d+\s+days?\s+remaining\b/i);
  const vStateText = (await vState.innerText()).replace(/\d+\s+days?\s+remaining/i, '').trim();
  expect(vStateText.length, 'licence state word is shown').toBeGreaterThan(0);
});

test('REQ-UI-065 Licence shows the over-cap warning the server sent', async ({ page }) => {
  // REQ-UI-065: when the account is over the licence's device cap, Licence shows the server's warning.
  // Scenario: needs a licence source returning an over-cap warning with known text. The app offers no
  // stub/fake licence source, and pushing the live test account over its cap would change the account
  // on the shared App Manager, which is forbidden.
  await openLicence(page);
  throw new Error('not measured: the app offers no stub licence source to make the server send an over-cap warning, and forcing the live test account over its cap would change the shared App Manager account');
});

test('REQ-UI-066 Forgetting a device removes it from the list and ends its sign-in', async ({ page }) => {
  // REQ-UI-066: when the owner forgets a device, it leaves the list and its sign-in no longer works.
  // Scenario: needs a device in the list, a Forget action, then a check that the device's own session fails.
  // Forgetting a real device on the live App Manager changes the account, which is forbidden, and the app
  // offers no fake licence source.
  await openLicence(page);
  const vForget = await page.getByRole('button', { name: /forget/i }).count();
  throw new Error(`not measured: no stub licence source exists to hold a device safely (Forget controls on page: ${vForget}); forgetting a real device on the shared App Manager would change the account`);
});

test('REQ-FN-075 A feature switched off for the licence is not offered anywhere', async ({ page }) => {
  // REQ-FN-075: when a feature is off for this licence, it is not offered anywhere in Chatur.
  // Scenario: needs a licence with one feature off, then a sweep of menus, tabs and screens for that feature.
  // The app offers no stub licence source, and switching a feature off needs a change to the live licence.
  await openLicence(page);
  throw new Error('not measured: no stub licence source to switch a feature off, and changing the live licence on the shared App Manager is forbidden');
});
