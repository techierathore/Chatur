// Acceptance tests: Integrations (REQ-FN-081, REQ-FN-082, REQ-FN-083, REQ-UI-069). Black-box, web harness.
// Anchors come from docs/mockups/integrations.html (data-testid), never from the page source.
// The page has two parts, each addressed on its own: the published-actions part (actions-panel, example-call,
// outside-runs) and the verifier and downloads part (verifier-windows, downloads-panel).
// Fixture rows the screen cannot make on this machine (a Mac/Windows window check, a signed release) are
// written into the signed-in account's own database and put back at the end of each test (support/db.ts).
import { test, expect, Page } from '@playwright/test';
import { signIn } from './support/auth';
import { query, run, snapshot, restore } from './support/db';

test.setTimeout(120_000);

async function openIntegrations(aPage: Page) {
  await signIn(aPage, 'Owner');
  await aPage.goto('/integrations', { waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('page')).toBeVisible({ timeout: 30_000 });
}

test('REQ-FN-081 Integrations lists every action Chatur offers with what starts it and its address', async ({ page }) => {
  // REQ-FN-081: the actions part lists every action: its name, what starts it, its address (METHOD /v1/chatur/...)
  // and a state; "Start a process" is on.
  await openIntegrations(page);
  await expect(page.getByTestId('actions-panel'), 'the published-actions part is drawn').toBeVisible({ timeout: 15_000 });
  const vTable = page.getByTestId('actions-table');
  await expect(vTable, 'Integrations shows an actions table').toBeVisible({ timeout: 15_000 });
  const vRows = vTable.locator('tbody tr');
  expect(await vRows.count(), 'all six actions Chatur offers are listed').toBe(6);
  for (let i = 0; i < (await vRows.count()); i++) {
    const vCells = vRows.nth(i).locator('td');
    const vText = (await vRows.nth(i).innerText()).trim();
    expect(await vCells.count(), `row ${i + 1} has name, what it does, what starts it, address, state: ${vText}`).toBeGreaterThanOrEqual(5);
    await expect(vCells.nth(0)).not.toBeEmpty();
    await expect(vCells.nth(2), 'what starts it').not.toBeEmpty();
    await expect(vCells.nth(3), 'its address').toHaveText(/^(GET|POST|PUT|DELETE)\s+\/v1\/chatur\/\S+/);
  }
  await expect(page.getByTestId('action-state-start')).toHaveText('on');
  await expect(page.getByTestId('actions-count')).toContainText(/\d+ of 6 on/);
  await page.screenshot({ path: 'tests/.artifacts/verify/integrations.png', fullPage: true });
});

test('REQ-FN-082 an outside tool calling a published action starts the matching process and the run is listed', async ({ page, playwright, baseURL }) => {
  // REQ-FN-082: make a token on the screen (shown once, never printed), read the published address of "Start a process",
  // call it from outside with the request API as another tool would, expect a run number back, and see that run listed.
  // Without a token the call is refused. The run and token the test made are removed at the end.
  await openIntegrations(page);
  const vRunsBefore = new Set(query('SELECT RunId FROM ProcessRun').map((aRow) => Number(aRow.RunId)));
  const vSnap = snapshot(['IntegrationToken', 'IntegrationCall']);
  const vApi = await playwright.request.newContext({ baseURL: baseURL! });
  try {
    const vRow = page.getByTestId('action-row-start');
    await expect(vRow, 'Integrations publishes a "Start a process" action').toBeVisible({ timeout: 15_000 });
    const vAddress = (await vRow.locator('td').nth(3).innerText()).trim();
    const vMatch = /^(POST)\s+(\/\S+)$/.exec(vAddress);
    expect(vMatch, `the published address reads METHOD /path; got "${vAddress}"`).not.toBeNull();

    const vRefused = await vApi.fetch(vMatch![2], { method: 'POST', data: { process: 'verify-phase' }, headers: { 'Content-Type': 'application/json' } });
    expect(vRefused.status(), 'a call with no token is refused').toBe(401);

    await page.getByTestId('new-token').click();
    const vToken = (await page.getByTestId('token-value').innerText()).trim();
    expect(vToken.startsWith('chatur_pat_'), 'a token is shown once').toBe(true);

    // The call names a project that exists: earlier tests remove the projects they open, so "the open project" may be
    // none, and Chatur then rightly answers 400 "No project is open in Chatur, so name one." (verify of 22 Nov 2026).
    const vProject = String(query('SELECT Name FROM Project WHERE RemovedUtc IS NULL ORDER BY ProjectId LIMIT 1')[0]?.Name ?? '');
    expect(vProject, 'a project exists to start the process on').not.toBe('');
    const vResponse = await vApi.fetch(vMatch![2], {
      method: vMatch![1],
      data: { project: vProject, process: 'verify-phase' },
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${vToken}` },
    });
    expect(vResponse.status(), 'the call is accepted').toBeLessThan(300);
    const vBody = await vResponse.json();
    expect(vBody.run, 'the answer gives back a run number').toBeGreaterThan(0);

    await page.goto('/integrations', { waitUntil: 'networkidle' });
    await expect(page.getByTestId(`outside-run-${vBody.run}`), 'the new run is listed').toBeVisible({ timeout: 30_000 });
    await expect(page.getByTestId(`outside-run-${vBody.run}`)).toContainText('verify-phase');
    await expect(page.getByTestId('token-value'), 'the token is not shown a second time').toHaveCount(0);
  } finally {
    await vApi.dispose();
    restore(vSnap);
    for (const vRow of query('SELECT RunId FROM ProcessRun')) {
      if (!vRunsBefore.has(Number(vRow.RunId))) run('DELETE FROM ProcessRun WHERE RunId = ?', Number(vRow.RunId));
    }
  }
});

test('REQ-FN-083 the verifier drives a built Mac or Windows application\'s own window and keeps the screenshots', async ({ page }) => {
  // REQ-FN-083: the verifier part lists checks of built applications: application and platform, machine, screenshots
  // kept above zero, when and result. NOT MEASURED HERE: driving a real Mac or Windows head needs a Mac or a Windows
  // desktop; this WSL web harness has neither, so the test writes the record the verifier writes after such a check
  // (a fixture row, removed at the end) and measures what the screen then shows of it.
  await openIntegrations(page);
  const vSnap = snapshot(['VerifierWindowCheck']);
  try {
    run(`INSERT INTO VerifierWindowCheck (Application, Platform, Machine, ScreenshotsKept, CheckedUtc, Result) VALUES (?, ?, ?, ?, ?, ?)`,
      'FixtureApp · Mac Catalyst', 'Mac Catalyst', 'Fixture Mac', 7, '2026-11-20T10:00:00.0000000Z', 'passed');
    await page.goto('/integrations', { waitUntil: 'networkidle' });
    await expect(page.getByTestId('verifier-windows'), 'the verifier part is drawn').toBeVisible({ timeout: 15_000 });
    const vTable = page.getByTestId('checks-table');
    await expect(vTable, 'Integrations lists the verifier window checks').toBeVisible({ timeout: 15_000 });
    const vRows = vTable.locator('tbody tr');
    expect(await vRows.count()).toBeGreaterThanOrEqual(1);
    const vCells = vRows.first().locator('td');
    await expect(vCells.nth(0)).toHaveText(/Mac|Windows/);
    expect(parseInt((await vCells.nth(2).innerText()).trim(), 10), 'screenshots kept').toBeGreaterThan(0);
    await expect(page.getByTestId('check-state-1')).toHaveText('passed');
  } finally {
    restore(vSnap);
  }
});

test('REQ-UI-069 a published release shows signed Mac and Windows downloads and the signature state', async ({ page }) => {
  // REQ-UI-069: the Signed downloads part shows a Mac and a Windows download, each saying who signed it, and the
  // signature's state. NOT MEASURED HERE: a real signed release needs an Apple Developer ID certificate and a
  // Windows code-signing certificate, which this machine does not have; the test writes the release record
  // (fixture rows, removed at the end) and measures what the screen shows of it.
  await openIntegrations(page);
  const vSnap = snapshot(['ReleaseDownload']);
  try {
    run(`INSERT INTO ReleaseDownload (Version, Platform, FileName, SignedBy, Notarisation, PublishedUtc) VALUES (?, ?, ?, ?, ?, ?)`,
      '9.9.9', 'Mac', 'Fixture-9.9.9.dmg', 'Developer ID Application — Fixture Signer', 'Notarised', '2026-11-20T10:00:00.0000000Z');
    run(`INSERT INTO ReleaseDownload (Version, Platform, FileName, SignedBy, Notarisation, PublishedUtc) VALUES (?, ?, ?, ?, ?, ?)`,
      '9.9.9', 'Windows', 'Fixture-9.9.9.msix', 'a code-signing certificate — Fixture Signer', 'NotApplicable', '2026-11-20T10:01:00.0000000Z');
    await page.goto('/integrations', { waitUntil: 'networkidle' });
    await expect(page.getByTestId('downloads-panel'), 'the downloads part is drawn').toBeVisible({ timeout: 15_000 });
    const vMac = page.getByTestId('download-mac');
    const vWin = page.getByTestId('download-win');
    await expect(vMac).toBeVisible();
    await expect(vWin).toBeVisible();
    await expect(vMac).toContainText(/Signed by/);
    await expect(vWin).toContainText(/Signed by/);
    await expect(page.getByTestId('download-mac-notarised')).toHaveText('Notarised');
    await expect(page.getByTestId('download-win-notarised')).toHaveText('Notarising does not apply');
  } finally {
    restore(vSnap);
  }
});
