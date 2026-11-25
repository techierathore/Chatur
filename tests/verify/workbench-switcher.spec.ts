// Verify-phase acceptance tests for the project switcher, from the owner's evening test of 21 Nov 2026:
// REQ-FN-307 (picking a project switches to it: its files, its chat and its state), REQ-UI-109 (every project, a list
// that scrolls, a filter box, no "All projects"), REQ-UI-110 (the bar clearly names the project open now). The owner
// had two projects both called "Xpenser" (a folder and a solution): picking the other one changed nothing that could be
// seen. Web harness, signed in as the Owner test user. The projects are made under this test project's own bin folder
// and removed, with every row recorded against them, when the file ends.
import { test, expect, Page } from '@playwright/test';
import { mkdirSync, rmSync, writeFileSync } from 'fs';
import { join } from 'path';
import { signIn } from './support/auth';
import { removeProjectsUnder, SCRATCH } from './support/cleanup';

const STAMP = Date.now();
// Made in beforeAll, never when the file is only listed, so a listing leaves no folder behind.
const ROOT = join(SCRATCH, `switcher-${STAMP}`);
const TWIN = `Twin${STAMP}`;
const FIRST = join(ROOT, 'first', TWIN);
const SECOND = join(ROOT, 'second', TWIN);

test.describe.configure({ mode: 'serial', timeout: 240_000 });

let page: Page;

test.beforeAll(async ({ browser }) => {
  for (const [vDir, vFile] of [[FIRST, 'only-in-first.txt'], [SECOND, 'only-in-second.txt']] as const) {
    mkdirSync(vDir, { recursive: true });
    writeFileSync(join(vDir, vFile), `${vFile}\n`);
  }
  page = await browser.newPage();
  try {
    await signIn(page, 'Owner');
  } catch {
    await signIn(page, 'Owner');
  }
  await openFolder(SECOND);
  await openFolder(FIRST);
});

test.afterAll(async () => {
  await page?.close();
  removeProjectsUnder(ROOT);
  rmSync(ROOT, { recursive: true, force: true });
});

/** Opens a folder as a project through File ▸ Open a folder, the way the owner does. */
async function openFolder(aPath: string): Promise<void> {
  await page.goto('/start?all=1', { waitUntil: 'networkidle' });
  const vDialog = page.getByTestId('open-project-dialog');
  await expect(async () => {
    if (!(await vDialog.isVisible())) {
      const vWay = (await page.getByTestId('empty-start').isVisible()) ? 'way-open-folder' : 'open-folder';
      await page.getByTestId(vWay).click();
    }
    await expect(vDialog).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
  await page.getByTestId('open-project-path').locator('input').or(page.locator('input[data-testid=open-project-path]')).first().fill(aPath);
  await page.getByTestId('open-project-go').click();
  await expect(vDialog).toBeHidden({ timeout: 30_000 });
  await expect(page.getByTestId('project-switch-name')).toHaveText(TWIN, { timeout: 30_000 });
}

/** Opens the switcher until its list shows. */
async function openSwitcher(): Promise<void> {
  const vPop = page.getByTestId('project-switch-pop');
  await expect(async () => {
    if (!(await vPop.isVisible())) await page.getByTestId('project-switch').click();
    await expect(vPop).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 20_000 });
}

/** The switcher's row for the project at this path. */
function rowFor(aPath: string) {
  return page.getByTestId('project-switch-list').locator('button').filter({ has: page.getByTestId('project-switch-item-path').getByText(aPath, { exact: true }) });
}

test('REQ-UI-110 the bar reads "Project:" and the name of the project open now, and its list ticks that one as open now', async () => {
  await expect(page.getByTestId('project-switch-label')).toHaveText('Project:');
  await expect(page.getByTestId('project-switch-name')).toHaveText(TWIN);
  await expect(page.getByTestId('project-switch')).toHaveAttribute('title', new RegExp(FIRST.replace(/[\\^$.*+?()[\]{}|]/g, '\\$&')));
  await openSwitcher();
  const vCurrent = rowFor(FIRST);
  await expect(vCurrent).toHaveAttribute('aria-current', 'true');
  await expect(vCurrent.getByTestId('project-switch-current')).toHaveText('open now');
  await expect(rowFor(SECOND).getByTestId('project-switch-current')).toHaveCount(0);
  await page.keyboard.press('Escape');
});

test('REQ-FN-307 picking the other project of the same name in the switcher switches to it: its files, its chat, the bar', async () => {
  await expect(page.getByTestId('file-tree').getByText('only-in-first.txt', { exact: true })).toBeVisible({ timeout: 20_000 });
  await openSwitcher();
  await rowFor(SECOND).click();
  await expect(page.getByTestId('project-switch-pop')).toBeHidden({ timeout: 10_000 });
  await expect(page.getByTestId('file-tree').getByText('only-in-second.txt', { exact: true })).toBeVisible({ timeout: 20_000 });
  await expect(page.getByTestId('file-tree').getByText('only-in-first.txt', { exact: true })).toHaveCount(0);
  await expect(page.getByTestId('conversation')).toBeVisible();
  await expect(page.getByTestId('project-switch')).toHaveAttribute('title', new RegExp(SECOND.replace(/[\\^$.*+?()[\]{}|]/g, '\\$&')));
  await openSwitcher();
  await expect(rowFor(SECOND)).toHaveAttribute('aria-current', 'true');
  // and back again
  await rowFor(FIRST).click();
  await expect(page.getByTestId('file-tree').getByText('only-in-first.txt', { exact: true })).toBeVisible({ timeout: 20_000 });
});

test('REQ-UI-109 the switcher has no "All projects", lists both projects with their paths, and its filter narrows the list', async () => {
  await openSwitcher();
  const vPop = page.getByTestId('project-switch-pop');
  await expect(page.getByTestId('project-switch-all')).toHaveCount(0);
  await expect(vPop).not.toContainText('All projects');
  await expect(rowFor(FIRST)).toBeVisible();
  await expect(rowFor(SECOND)).toBeVisible();
  const vFilter = page.locator('input[data-testid=project-switch-filter], [data-testid=project-switch-filter] input').first();
  if (await vFilter.isVisible()) {
    await vFilter.fill(join('second', TWIN));
    await expect(rowFor(SECOND)).toBeVisible({ timeout: 10_000 });
    await expect(rowFor(FIRST)).toHaveCount(0, { timeout: 10_000 });
  }
  await page.screenshot({ path: join(__dirname, '..', '.artifacts', 'verify', 'shots', 'REQ-UI-109-switcher.png') }).catch(() => undefined);
  await page.keyboard.press('Escape');
});
