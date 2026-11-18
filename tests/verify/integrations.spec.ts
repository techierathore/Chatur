// Acceptance tests: Integrations (REQ-FN-081, REQ-FN-082, REQ-FN-083, REQ-UI-069). Black-box, web harness.
// Anchors come from docs/mockups/integrations.html (data-testid), never from the page source.
import { test, expect } from '@playwright/test';
import { signIn } from './support/auth';

test.setTimeout(120_000);

async function openIntegrations(aPage: import('@playwright/test').Page) {
  await signIn(aPage, 'Owner');
  await aPage.goto('/integrations', { waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('page')).toBeVisible({ timeout: 30_000 });
}

test('REQ-FN-081 Integrations lists every action Chatur offers with what starts it and its address', async ({ page }) => {
  // REQ-FN-081: open Integrations; the actions table lists at least one action, and each row names the
  // action, what starts it (a trigger) and its address (METHOD /v1/chatur/...), with an on/off state.
  await openIntegrations(page);
  const vTable = page.getByTestId('actions-table');
  await expect(vTable, 'not measured: Integrations shows no actions table (the screen lists no published action)').toBeVisible({ timeout: 15_000 });
  const vRows = vTable.locator('tbody tr');
  expect(await vRows.count()).toBeGreaterThanOrEqual(1);
  for (let i = 0; i < (await vRows.count()); i++) {
    const vCells = vRows.nth(i).locator('td');
    const vText = (await vRows.nth(i).innerText()).trim();
    expect(await vCells.count(), `row ${i + 1} has name, what it does, what starts it, address, state: ${vText}`).toBeGreaterThanOrEqual(5);
    await expect(vCells.nth(0)).not.toBeEmpty();
    await expect(vCells.nth(2), 'what starts it').not.toBeEmpty();
    await expect(vCells.nth(3), 'its address').toHaveText(/^(GET|POST|PUT|DELETE)\s+\/\S+/);
  }
  await page.screenshot({ path: 'tests/.artifacts/verify/integrations.png', fullPage: true });
});

test('REQ-FN-082 an outside tool calling a published action starts the matching process and the run is listed', async ({ page, playwright, baseURL }) => {
  // REQ-FN-082: read the published address of "Start a process" from Integrations, call it from outside
  // with the request API as another tool would, expect a run number back, then see that run listed
  // (Processes / run list) after the call.
  await openIntegrations(page);
  const vRow = page.getByTestId('action-row-start');
  await expect(vRow, 'not measured: Integrations publishes no "Start a process" action to call').toBeVisible({ timeout: 15_000 });
  const vAddress = (await vRow.locator('td.mono, td').nth(3).innerText()).trim();
  const vMatch = /^(GET|POST)\s+(\/\S+)$/.exec(vAddress);
  expect(vMatch, `the published address reads METHOD /path; got "${vAddress}"`).not.toBeNull();
  const vApi = await playwright.request.newContext({ baseURL: baseURL! });
  try {
    const vResponse = await vApi.fetch(vMatch![2], {
      method: vMatch![1],
      data: { project: 'Chatur', process: 'verify-phase' },
      headers: { 'Content-Type': 'application/json' },
    });
    expect(vResponse.status(), `the call to ${vAddress} is accepted`).toBeLessThan(300);
    const vBody = await vResponse.json();
    expect(vBody.run, 'the answer gives back a run number').toBeTruthy();
    // The run is listed: its number shows on the screen that lists runs.
    await page.goto('/integrations', { waitUntil: 'networkidle' });
    await expect(page.getByText(String(vBody.run)).first(), 'the new run is listed').toBeVisible({ timeout: 30_000 });
  } finally {
    await vApi.dispose();
  }
});

test('REQ-FN-083 the verifier drives a built Mac or Windows application\'s own window and keeps the screenshots', async ({ page }) => {
  // REQ-FN-083: "The verifier on real windows" lists checks of built applications: each row names the
  // application and platform, the machine, a screenshots-kept count above zero, when, and the result.
  // Driving a real Mac or Windows head is not possible on the WSL web harness; the kept record is what
  // can be observed here, and it must exist.
  await openIntegrations(page);
  const vTable = page.getByTestId('checks-table');
  await expect(vTable, 'not measured: Integrations lists no verifier window checks (no Mac/Windows head has been driven and recorded here)').toBeVisible({ timeout: 15_000 });
  const vRows = vTable.locator('tbody tr');
  expect(await vRows.count()).toBeGreaterThanOrEqual(1);
  const vCells = vRows.first().locator('td');
  await expect(vCells.nth(0)).toHaveText(/Mac|Windows/);
  const vKept = parseInt((await vCells.nth(2).innerText()).trim(), 10);
  expect(vKept, 'screenshots kept').toBeGreaterThan(0);
});

test('REQ-UI-069 a published release shows signed Mac and Windows downloads and the signature state', async ({ page }) => {
  // REQ-UI-069: the Signed downloads panel shows a Mac and a Windows download, each saying who signed it,
  // and the screen shows the signature's state (Notarised for Mac). No signing certificate exists on this
  // machine, so a real signed release cannot be produced; the panel must at least show a published release.
  await openIntegrations(page);
  await expect(page.getByTestId('downloads-panel'), 'not measured: no published release with signed downloads is shown (no signing certificate on this machine)').toBeVisible({ timeout: 15_000 });
  const vMac = page.getByTestId('download-mac');
  const vWin = page.getByTestId('download-win');
  await expect(vMac).toBeVisible();
  await expect(vWin).toBeVisible();
  await expect(vMac).toContainText(/Signed by/);
  await expect(vWin).toContainText(/Signed by/);
  await expect(page.getByTestId('download-mac-notarised')).toHaveText(/Notarised|Not notarised|Notarising/);
});
