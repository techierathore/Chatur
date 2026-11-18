// Regression test (2026-11-15): End-to-end run was blank on the first screen check after a test run.
// Black-box, web harness. A test run leaves its pages closed without a goodbye, and the harness kept each
// closed page alive on the server for three minutes. The app's state is one object for the whole harness,
// so when the selected project then changed (the screen check's seed does that), every closed page re-read
// its files, its run targets, its branch and its conversation at once, and the page that was really open
// waited behind them with an empty work area: 5.8 to 6.9 s with 30 closed pages, against 0.3 to 0.8 s with
// none. The screen check looks about two seconds after it opens a screen.
// This test leaves closed pages behind, changes the project the way the seed does, and opens End-to-end run
// by its address: the screen must be drawn within the time the screen check gives it.
import { expect } from '@playwright/test';
import { signIn } from './support/auth';
import { query, run } from './support/db';
import { tab } from './support/phase2-tabs';
import { test, NAME, resetFixture, openProject } from './support/workbench-a';

const CLOSED_PAGES = 12;
// The screen check opens the address, waits for the network to go quiet and then 1.5 s more.
const SCREEN_CHECK_WAIT_MS = 2_500;

test('REQ-UI-055 End-to-end run opens by its address in time when closed pages are left and the project changed', async ({ browser }) => {
  test.setTimeout(300_000);
  resetFixture();

  // The pages a test run leaves: each one a signed-in main window, closed without telling the server.
  for (let vIndex = 0; vIndex < CLOSED_PAGES; vIndex++) {
    const vContext = await browser.newContext({ baseURL: process.env.BASE_URL });
    const vLeft = await vContext.newPage();
    await signIn(vLeft, 'Owner');
    if (vIndex === 0) await openProject(vLeft, NAME);
    else await vLeft.goto('/', { waitUntil: 'domcontentloaded' });
    await expect(vLeft.getByTestId('file-tree')).toBeVisible({ timeout: 30_000 });
    await vContext.close();
  }

  const vContext = await browser.newContext({ baseURL: process.env.BASE_URL, viewport: { width: 1280, height: 800 } });
  const vPage = await vContext.newPage();
  try {
    await signIn(vPage, 'Owner');

    // The project changes behind the app, as tests/verify/seed/_seedlib.py open_project does it.
    const vNow = Number(query("SELECT Value FROM Setting WHERE Key = 'SelectedProjectId'")[0].Value);
    const vOther = query('SELECT ProjectId FROM Project WHERE ProjectId <> ? AND RemovedUtc IS NULL ORDER BY ProjectId LIMIT 1', vNow);
    expect(vOther.length, 'the harness holds a second project to change to').toBe(1);
    run("UPDATE Setting SET Value = ? WHERE Key = 'SelectedProjectId'", String(vOther[0].ProjectId));

    const vStarted = Date.now();
    await vPage.goto('/auto', { waitUntil: 'domcontentloaded' });
    await expect(tab(vPage, 'Auto run').first(), 'the Auto run tab is open').toBeVisible({ timeout: 30_000 });
    await expect(vPage.getByTestId('auto-run'), 'End-to-end run is drawn').toBeVisible({ timeout: 30_000 });
    const vTook = Date.now() - vStarted;
    expect(vTook, `End-to-end run took ${vTook} ms to draw; the screen check looks after about ${SCREEN_CHECK_WAIT_MS} ms`).toBeLessThan(SCREEN_CHECK_WAIT_MS);
  } finally {
    await vContext.close();
  }
});
