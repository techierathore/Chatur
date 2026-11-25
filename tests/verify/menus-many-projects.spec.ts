// Acceptance test: the menu bar opens at once however many projects the account holds (REQ-UI-073, REQ-UI-010).
// Owner, 20 Nov 2026: with about 80 projects in the test account, File and Edit opened only some of the time.
// Cause: the File trigger re-read every project on pointer-down (ProjectActions.ScanAsync walks every named
// folder on disk and runs a statement per subfolder, all synchronously on the page's circuit), so the click
// that opens the menu queued behind it, and a second click toggled it shut again. This test gives the account
// 200 projects, then opens File, Edit and the project switcher with ONE click each, five times each, and
// requires every one to open within a second — no retry, no second click.
// Scratch data (the 200 project folders) lives under this test project's own bin folder and is deleted, with
// every project row it added, when the test ends.
import { test, expect, Page } from '@playwright/test';
import { mkdirSync, writeFileSync } from 'fs';
import { SCRATCH, removeProjectsUnder } from './support/cleanup';
import { join } from 'path';
import { signIn } from './support/auth';
import { run, query } from './support/db';
import { NAME, openProject, openFile, resetFixture } from './support/workbench-a';

const COUNT = 200;
const ROOT = join(SCRATCH, `many-projects-${Date.now()}`);
const OPEN_WITHIN_MS = 1_000;
const TIMES = 5;

let page: Page;

/** Makes 200 project folders, a few holding a solution, and names their parent folder for this account. */
function seedProjects(): void {
  for (let vI = 1; vI <= COUNT; vI++) {
    const vDir = join(ROOT, `many-${String(vI).padStart(3, '0')}`);
    mkdirSync(vDir, { recursive: true });
    if (vI % 10 === 0) writeFileSync(join(vDir, `Many${vI}.sln`), '\n');
  }
  run('INSERT OR IGNORE INTO ProjectFolder (Path, CreatedUtc) VALUES (?, ?)', ROOT, new Date().toISOString());
}

/** Removes every project row and the folder row this test added, then the folders on disk. */
function removeSeeded(): void {
  removeProjectsUnder(ROOT);
}

/** One click on the trigger, then the time until its list is on screen; fails if it takes longer than allowed. */
async function openOnce(aTrigger: string, aList: string): Promise<number> {
  const vList = page.getByTestId(aList);
  await expect(vList).toBeHidden();
  const vStart = Date.now();
  await page.getByTestId(aTrigger).click();
  await expect(vList, `${aTrigger} opened with one click`).toBeVisible({ timeout: OPEN_WITHIN_MS });
  const vTook = Date.now() - vStart;
  await page.keyboard.press('Escape');
  // Escape closes it a moment later; only a menu still open after that (a TrBlazeUI menu bar can stay open on
  // Escape) is closed by its trigger, so the close click never reopens one that was already closing.
  const vClosed = await vList.waitFor({ state: 'hidden', timeout: 1_500 }).then(() => true, () => false);
  if (!vClosed) await page.getByTestId(aTrigger).click();
  await expect(vList).toBeHidden({ timeout: 5_000 });
  return vTook;
}

test.describe.configure({ mode: 'serial' });

test.beforeAll(async ({ browser }) => {
  test.setTimeout(240_000);
  page = await (await browser.newContext({ baseURL: process.env.BASE_URL })).newPage();
  await signIn(page, 'Owner');
  seedProjects();
  resetFixture();
  await openProject(page); // the Start window scans the folders, so the 200 projects are now the account's
  const vKnown = query("SELECT COUNT(*) AS n FROM Project WHERE RemovedUtc IS NULL AND Path LIKE ? || '%'", ROOT)[0].n;
  expect(Number(vKnown), 'the account holds the 200 seeded projects').toBe(COUNT);
  // five of them opened lately, so File ▸ Recent projects has a list to show
  run("UPDATE Project SET LastOpenedUtc = ? WHERE Path LIKE ? || '%' AND Name IN ('many-001','many-002','many-003','many-004','many-005')", new Date(Date.now() - 60_000).toISOString(), ROOT);
  await expect(page.getByTestId('chat-panel')).toBeVisible({ timeout: 20_000 });
  await page.waitForTimeout(1_500); // the circuit is interactive before the first measured click
});

test.afterAll(async () => {
  removeSeeded();
  await page?.close();
});

test('REQ-UI-073 File opens with one click every time on an account with 200 projects', async () => {
  const vTimes: number[] = [];
  for (let vI = 0; vI < TIMES; vI++) vTimes.push(await openOnce('menu-file', 'menu-file-items'));
  console.log(`File opened in ${vTimes.join(', ')} ms`);
});

test('REQ-UI-073 Edit opens with one click every time on an account with 200 projects, a file open', async () => {
  await openFile(page, 'Notes/notes.txt');
  const vTimes: number[] = [];
  for (let vI = 0; vI < TIMES; vI++) vTimes.push(await openOnce('menu-edit', 'menu-edit-items'));
  console.log(`Edit opened in ${vTimes.join(', ')} ms`);
});

test('REQ-UI-073 File ▸ Recent projects lists the projects opened last, not held back by the 200', async () => {
  await page.getByTestId('menu-file').click();
  await expect(page.getByTestId('menu-file-items')).toBeVisible({ timeout: OPEN_WITHIN_MS });
  await page.getByTestId('menu-file-recent').click();
  const vList = page.getByTestId('menu-file-recent-list');
  await expect(vList).toBeVisible({ timeout: OPEN_WITHIN_MS });
  await expect(vList.getByText(NAME, { exact: true })).toBeVisible();
  await page.keyboard.press('Escape');
  await page.keyboard.press('Escape');
  if (await page.getByTestId('menu-file-items').isVisible()) await page.getByTestId('menu-file').click();
});

test('REQ-UI-010 the project switcher opens with one click every time on an account with 200 projects', async () => {
  const vTimes: number[] = [];
  for (let vI = 0; vI < TIMES; vI++) vTimes.push(await openOnce('project-switch', 'project-switch-pop'));
  console.log(`Project switcher opened in ${vTimes.join(', ')} ms`);
});

test('REQ-UI-109 the project switcher lists every project of the account in a list that scrolls, with a filter box and no "All projects"', async () => {
  // Decided by the owner on 21 Nov 2026, replacing the 20 Nov "20 last opened, then All projects".
  await page.getByTestId('project-switch').click();
  const vPop = page.getByTestId('project-switch-pop');
  await expect(vPop).toBeVisible({ timeout: OPEN_WITHIN_MS });
  const vList = page.getByTestId('project-switch-list');
  // Only the rows in view are drawn; the list says how many it holds, and scrolling to its end shows the last ones.
  await expect.poll(async () => Number(await vList.getAttribute('data-count')), { timeout: 20_000 }).toBeGreaterThanOrEqual(200);
  await expect(page.getByTestId('project-switch-all')).toHaveCount(0);
  await expect(vPop).not.toContainText('All projects');
  const vScrolls = await vList.evaluate((aEl) => aEl.scrollHeight > aEl.clientHeight && getComputedStyle(aEl).overflowY !== 'visible');
  expect(vScrolls, 'the list scrolls').toBe(true);
  const vFirstShown = await vPop.getByTestId('project-switch-item-name').first().innerText();
  await vList.evaluate((aEl) => { aEl.scrollTop = aEl.scrollHeight; });
  await expect.poll(() => vPop.getByTestId('project-switch-item-name').first().innerText(), { timeout: 10_000 }).not.toBe(vFirstShown);
  const vFilter = page.getByTestId('project-switch-filter').locator('input').or(page.locator('input[data-testid=project-switch-filter]')).first();
  await expect(vFilter).toBeVisible();
  await vFilter.fill(NAME);
  await expect.poll(() => vPop.getByTestId('project-switch-item-name').count(), { timeout: 10_000 }).toBeLessThan(200);
  await expect(vPop.getByTestId('project-switch-item-name').getByText(NAME, { exact: true }).first()).toBeVisible();
  await page.keyboard.press('Escape');
});

test('REQ-UI-010 a long project name takes one line in the switcher, cut with an ellipsis, the whole name on hover', async () => {
  const vLong = 'A-really-long-project-name-for-the-switcher-list';
  const vDir = join(ROOT, vLong);
  mkdirSync(vDir, { recursive: true });
  run("INSERT INTO Project (ProjectFolderId, Name, Path, Kind, LastOpenedUtc) VALUES (NULL, ?, ?, 'Folder', ?)", vLong, vDir, new Date().toISOString());
  await page.reload({ waitUntil: 'networkidle' });
  await page.waitForTimeout(1_500);
  await page.getByTestId('project-switch').click();
  const vItem = page.getByTestId('project-switch-pop').locator(`[title="${vLong}"]`);
  await expect(vItem).toBeVisible({ timeout: 5_000 });
  const vName = vItem.getByTestId('project-switch-item-name');
  const vBox = await vName.evaluate((e) => ({
    oneLine: e.getBoundingClientRect().height <= parseFloat(getComputedStyle(e).fontSize) * 1.9,
    cut: e.scrollWidth > e.clientWidth,
    overflow: getComputedStyle(e).textOverflow,
  }));
  expect(vBox.oneLine, 'one line').toBe(true);
  expect(vBox.cut, 'the name is cut').toBe(true);
  expect(vBox.overflow).toBe('ellipsis');
  await page.keyboard.press('Escape');
});
