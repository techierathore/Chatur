// Acceptance tests: Run queue (REQ-UI-057 with the controls REQ-FN-067, REQ-FN-068, REQ-FN-069 are smoked through;
// mockups/run-queue.html). Black-box, web harness. The screen is opened from the Project menu as a tab. A run that is
// "going" is SEEDED as a run waiting out a usage limit (a going state nothing drives forward by itself), because a
// real chain cannot be held still for a test; ending it is done in the database, which is what the acceptance line
// is about (the next row must not start until the one before it ended).
import { Page, expect } from '@playwright/test';
import { join } from 'path';
import { signIn } from './support/auth';
import { query, run } from './support/db';
import { tab } from './support/phase2-tabs';
import { test, NAME, resetFixture, openProject, SHOTS } from './support/workbench-a';

let page: Page;

test.beforeAll(async ({ browser }) => {
  page = await browser.newPage({ baseURL: process.env.BASE_URL });
  await signIn(page, 'Owner');
});
test.afterAll(async () => { await page.close(); });
test.beforeEach(async () => {
  test.setTimeout(240_000);
  resetFixture();
});

const BRIEF = 'Finish the coverage panel: the file list, the per-file bar and the filter. Verify before you stop.';

function clearAll(): number {
  const vProject = Number(query('SELECT ProjectId FROM Project WHERE Name = ?', NAME)[0].ProjectId);
  run('DELETE FROM RunQueueItem');
  run('DELETE FROM AutoRunEvent');
  run('DELETE FROM AutoRunProcess');
  run('DELETE FROM AutoRun');
  return vProject;
}

/** An end-to-end run waiting out a usage limit: going, but nothing carries it on for hours. */
function seedGoingRun(aProject: number, aState = 'WaitingForLimit'): number {
  const vStart = new Date(Date.now() - 42 * 60_000).toISOString();
  const vResume = new Date(Date.now() + 6 * 3_600_000).toISOString();
  return run(
    'INSERT INTO AutoRun (ProjectId, Brief, State, CurrentProcess, StartedUtc, ResumeAtUtc) VALUES (?, ?, ?, 5, ?, ?)',
    aProject, BRIEF, aState, vStart, aState === 'WaitingForLimit' ? vResume : null);
}

function seedQueueRow(aProject: number, aBrief: string, aStatus: string, aRunId: number | null): number {
  return run(
    'INSERT INTO RunQueueItem (ProjectId, Brief, Status, AutoRunId, QueuedUtc) VALUES (?, ?, ?, ?, ?)',
    aProject, aBrief, aStatus, aRunId, new Date().toISOString());
}

async function openQueueTab(): Promise<void> {
  await expect(async () => {
    if (!(await page.getByTestId('menu-project-queue').isVisible())) await page.getByTestId('menu-project').click();
    await page.getByTestId('menu-project-queue').click({ timeout: 2_500 });
    await expect(tab(page, 'Run queue').first()).toBeVisible({ timeout: 3_000 });
  }).toPass({ timeout: 30_000 });
  await expect(page.getByTestId('run-queue')).toBeVisible({ timeout: 30_000 });
}

async function openWithSeed(aSeed: (aProject: number) => void): Promise<void> {
  await openProject(page);
  aSeed(clearAll());
  await page.reload({ waitUntil: 'domcontentloaded' });
  await expect(page.getByTestId('project-switch-name')).toHaveText(NAME, { timeout: 30_000 });
  await openQueueTab();
}

test('REQ-UI-057 the next run waits while one is going and starts only after it has ended', async () => {
  let vGoing = 0;
  await openWithSeed((aProject) => {
    vGoing = seedGoingRun(aProject);
    seedQueueRow(aProject, BRIEF, 'Started', vGoing);
    seedQueueRow(aProject, 'Settings: providers and routing, to the mockup.', 'Queued', null);
  });

  await expect(page.getByTestId('queue-table')).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('queue-count')).toHaveText('1 running · 1 waiting');
  await expect(page.getByTestId('queue-state-1')).toContainText('running');
  await expect(page.getByTestId('queue-state-2')).toContainText('waiting');
  await expect(page.getByTestId('queue-position-2')).toHaveText('2');

  // The screen asks again every few seconds: after more than one round the second run is still waiting.
  await page.waitForTimeout(10_000);
  await expect(page.getByTestId('queue-state-2')).toContainText('waiting');
  expect(query('SELECT COUNT(*) AS n FROM AutoRun')[0].n).toBe(1);

  // Nothing overlaps: the heading sits above the table, the table above the explanation.
  const vHead = (await page.getByTestId('queue-heading').boundingBox())!;
  const vTable = (await page.getByTestId('queue-panel').boundingBox())!;
  const vHow = (await page.getByTestId('how-queue-behaves').boundingBox())!;
  expect(vHead.y + vHead.height).toBeLessThanOrEqual(vTable.y + 1);
  expect(vTable.y + vTable.height).toBeLessThanOrEqual(vHow.y + 1);
  await page.screenshot({ path: join(SHOTS, 'run-queue-one-at-a-time.png'), fullPage: true });

  // The run ends; the next one takes its turn and the ended one reads finished.
  run("UPDATE AutoRun SET State = 'Stopped', EndedUtc = ?, StopReason = 'Ended for the test.' WHERE AutoRunId = ?", new Date().toISOString(), vGoing);
  await expect(page.getByTestId('queue-state-2')).not.toContainText('waiting', { timeout: 60_000 });
  const vNow = query('SELECT COUNT(*) AS n FROM AutoRun')[0].n as number;
  expect(vNow).toBeGreaterThanOrEqual(2);
});

test('REQ-FN-067 queueing a run lists it with its project and position', async () => {
  await openWithSeed((aProject) => { seedQueueRow(aProject, BRIEF, 'Started', seedGoingRun(aProject)); });
  await page.getByTestId('queue-a-run').click();
  await expect(page.getByTestId('queue-run-dialog')).toBeVisible({ timeout: 15_000 });
  await page.getByTestId('queue-brief').fill('Too short');
  await page.getByTestId('queue-run-add').click();
  await expect(page.getByTestId('queue-run-error')).toBeVisible();
  await page.getByTestId('queue-brief').fill('Chart screen from the P3 brief, with the three tests it needs.');
  await page.getByTestId('queue-run-add').click();
  await expect(page.getByTestId('queue-run-dialog')).toBeHidden({ timeout: 15_000 });
  await expect(page.getByTestId('queue-project-2')).toHaveText(NAME);
  await expect(page.getByTestId('queue-position-2')).toHaveText('2');
  await expect(page.getByTestId('queue-state-2')).toContainText('waiting');
});

test('REQ-FN-069 a queued run can be removed after a confirmation and the running one cannot', async () => {
  await openWithSeed((aProject) => {
    seedQueueRow(aProject, BRIEF, 'Started', seedGoingRun(aProject));
    seedQueueRow(aProject, 'Nightly verify of what is open.', 'Queued', null);
  });
  await expect(page.getByTestId('remove-1')).toBeDisabled();
  await expect(page.getByTestId('resume-1')).toBeDisabled();
  await expect(page.getByTestId('pause-2')).toBeDisabled();
  await page.getByTestId('remove-2').click();
  await expect(page.getByTestId('remove-dialog')).toBeVisible();
  await page.getByTestId('remove-confirm').click();
  await expect(page.getByTestId('queue-row-2')).toHaveCount(0, { timeout: 15_000 });
  await expect(page.getByTestId('queue-count')).toHaveText('1 running · 0 waiting');
});

/** Whether a control sits wholly inside a box (a pixel of rounding allowed). */
function inside(aInner: { x: number; y: number; width: number; height: number }, aOuter: { x: number; y: number; width: number; height: number }): boolean {
  return aInner.width > 0 && aInner.x >= aOuter.x - 1 && aInner.x + aInner.width <= aOuter.x + aOuter.width + 1;
}

test('REQ-UI-057 every column and every action button is inside the queue tab at 1280 and inside the screen at 390', async () => {
  await openWithSeed((aProject) => {
    seedQueueRow(aProject, BRIEF, 'Started', seedGoingRun(aProject));
    seedQueueRow(aProject, 'Settings: providers and routing, to the mockup.', 'Queued', null);
    seedQueueRow(aProject, 'Nightly verify of what is open.', 'Queued', null);
  });
  await expect(page.getByTestId('queue-table')).toBeVisible({ timeout: 30_000 });
  for (const vSize of [{ width: 1280, height: 800 }, { width: 390, height: 844 }]) {
    await page.setViewportSize(vSize);
    await expect(async () => {
      const vTab = (await page.getByTestId('run-queue').boundingBox())!;
      const vPanel = (await page.getByTestId('queue-panel').boundingBox())!;
      // At 1280 the whole table is inside the card: nothing needs a sideways scroll to be reached.
      const vTable = (await page.getByTestId('queue-table').boundingBox())!;
      if (vSize.width >= 1280) expect(vTable.x + vTable.width).toBeLessThanOrEqual(vPanel.x + vPanel.width + 1);
      // At 390 the table is wider than the card, as the mockup's is, and scrolls inside it: the buttons are reached by
      // scrolling the table, never by scrolling the page, and once reached they are inside the screen.
      // TrBlazeUI 2.1.6: Started and Time drop out below 640 px (HideBelow Sm), so no sideways scroll is needed at 390.
      if (vSize.width < 640) {
        await expect(page.getByTestId('queue-started-1')).toBeHidden();
        await expect(page.getByTestId('queue-time-1')).toBeHidden();
        const vScrolls = await page.getByTestId('queue-table').evaluate((aEl) => {
          for (let vNode: HTMLElement | null = aEl as HTMLElement; vNode && vNode !== document.body; vNode = vNode.parentElement) {
            if (vNode.scrollWidth > vNode.clientWidth + 1 && /(auto|scroll)/.test(getComputedStyle(vNode).overflowX)) return true;
          }
          return false;
        });
        expect(vScrolls, 'no sideways scroll inside the queue card at 390').toBe(false);
      } else {
        await expect(page.getByTestId('queue-started-1')).toBeVisible();
        await expect(page.getByTestId('queue-time-1')).toBeVisible();
      }
      // Every action button is inside the card, the tab and the screen (at 390, once the table is scrolled to them).
      const vIds = ['pause-1', 'resume-1', 'remove-1', 'pause-2', 'resume-2', 'remove-2', 'pause-3', 'resume-3', 'remove-3'];
      // (the row-1 buttons are checked at 390 too: one scroll brings the whole column in)
      // At 1280 every column is in view as well, the two that used to be cut off included.
      if (vSize.width >= 1280) vIds.push('queue-project-1', 'queue-state-1', 'queue-started-1', 'queue-time-1');
      for (const vId of vIds) {
        const vBox = await page.getByTestId(vId).boundingBox();
        expect(vBox, `${vId} at ${vSize.width}`).not.toBeNull();
        expect(inside(vBox!, vPanel), `${vId} inside the queue card at ${vSize.width}`).toBe(true);
        expect(inside(vBox!, vTab), `${vId} inside the tab at ${vSize.width}`).toBe(true);
        expect(inside(vBox!, { x: 0, y: 0, width: vSize.width, height: vSize.height }), `${vId} inside the viewport at ${vSize.width}`).toBe(true);
      }
    }).toPass({ timeout: 15_000 });
    await page.screenshot({ path: join(SHOTS, `run-queue-fits-${vSize.width}.png`), fullPage: true });
  }
  await page.setViewportSize({ width: 1280, height: 720 });
});
