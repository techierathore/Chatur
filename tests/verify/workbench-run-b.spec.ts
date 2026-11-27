// Acceptance tests for the Workbench Stop / run-target rows made on 18 Nov (REQ-UI-104, REQ-UI-105,
// REQ-UI-106). Black-box, web harness: the solution fixture is made fresh, opened through Start's Open a
// solution dialog (typing the .sln path), and what runs is read from the Output panel.
import { Page, expect } from '@playwright/test';
import { join } from 'path';
import { rmSync } from 'fs';
import { signIn } from './support/auth';
import { test, NAME, PID_FILE, resetFixture, openProject, outputText, pill, stopIfRunning } from './support/workbench-a';
import {
  makeSolutionFixture, openByDialog, openMenu, pick, openTargets, SLN_NAME, APP_ONE, APP_TWO, LIB, TESTS, LINE_ONE, LINE_TWO,
} from './support/workbench-b';

let page: Page;
const SHOTS = join(__dirname, '../.artifacts/verify/shots');

test.beforeAll(async ({ browser }) => {
  test.setTimeout(120_000);
  const vContext = await browser.newContext({ baseURL: process.env.BASE_URL });
  page = await vContext.newPage();
  await signIn(page, 'Owner');
});
test.afterAll(async () => { await page.close(); });
test.beforeEach(() => { test.setTimeout(180_000); });
test.afterEach(async () => { await stopIfRunning(page); });

const toolbarStop = () => page.getByTestId('toolbar').getByTestId('stop');
const targetButton = () => page.getByTestId('target').locator('button');

test('REQ-UI-104 the toolbar shows no Stop while nothing builds or runs, and shows one that ends a build or run that goes', async () => {
  resetFixture();
  rmSync(PID_FILE, { force: true });
  await openProject(page, NAME);
  await expect(page.getByTestId('run')).toBeEnabled({ timeout: 30_000 });
  await expect(page.locator('[data-testid=stop]')).toHaveCount(0); // nothing going: no Stop in the toolbar
  await expect(page.getByTestId('toolbar').locator('[data-testid=stop]')).toHaveCount(0);

  // a build shows it, and it is gone again when the build ends
  await page.getByTestId('build').click();
  await expect(pill(page)).toContainText('build passed', { timeout: 90_000 });
  await expect(toolbarStop()).toHaveCount(0, { timeout: 20_000 });

  // a run shows it, pressing it ends the run, and it goes away again
  await page.getByTestId('run').click();
  await expect(toolbarStop()).toBeVisible({ timeout: 30_000 });
  await expect(toolbarStop()).toBeEnabled();
  await expect(outputText(page)).toContainText('WBA tick 1', { timeout: 90_000 });
  await expect(toolbarStop()).toBeVisible();
  await page.screenshot({ path: join(SHOTS, 'REQ-UI-104-stop-while-running.png'), fullPage: true });
  await toolbarStop().click();
  await expect(outputText(page)).toContainText('Run stopped.', { timeout: 30_000 });
  await expect(pill(page)).toContainText('stopped', { timeout: 20_000 });
  await expect(toolbarStop()).toHaveCount(0, { timeout: 20_000 });
  await expect(page.getByTestId('run')).toBeEnabled({ timeout: 20_000 });
});

test('REQ-UI-105 the target picker lists the solution\'s runnable projects, and choosing one and pressing Run runs that project', async () => {
  const vFix = makeSolutionFixture();
  try {
    await openByDialog(page, 'open-solution', vFix.sln, SLN_NAME);
    await openTargets(page);
    const vPop = page.getByTestId('target-pop');
    const vItems = vPop.getByRole('menuitemradio');
    await expect(vItems.filter({ hasText: APP_ONE })).toHaveCount(1);
    await expect(vItems.filter({ hasText: APP_TWO })).toHaveCount(1);
    await expect(vItems.filter({ hasText: LIB })).toHaveCount(0);
    // Since 21 Nov 2026 a solution's test project is a target too: running it runs its tests (owner, point 1.4).
    await expect(vItems.filter({ hasText: TESTS })).toHaveCount(1);
    await expect(vItems).toHaveCount(3);
    await page.screenshot({ path: join(SHOTS, 'REQ-UI-105-target-list.png'), fullPage: true });

    // choose the second app and run it: its line, not the first app's, reaches the output
    await vItems.filter({ hasText: APP_TWO }).click();
    await expect(targetButton()).toContainText(APP_TWO);
    await page.getByTestId('run').click();
    await expect(outputText(page)).toContainText(LINE_TWO, { timeout: 120_000 });
    await expect(outputText(page)).not.toContainText(LINE_ONE);
    await expect(page.getByTestId('run')).toBeEnabled({ timeout: 60_000 });

    // and choose the first app: now its line shows
    await openTargets(page);
    await vPop.getByRole('menuitemradio').filter({ hasText: APP_ONE }).click();
    await expect(targetButton()).toContainText(APP_ONE);
    await page.getByTestId('run').click();
    await expect(outputText(page)).toContainText(LINE_ONE, { timeout: 120_000 });
    await expect(outputText(page)).not.toContainText(LINE_TWO);
  } finally {
    vFix.cleanup();
  }
});

test('REQ-UI-106 setting the project to run from the picker, the Project menu or Files makes that account offered it next time', async () => {
  const vFix = makeSolutionFixture();
  /** Closes the solution and opens it again the way a person comes back to it. */
  const reopen = async () => {
    await pick(page, 'file', 'close');
    await expect(page).toHaveURL(/\/start/, { timeout: 20_000 });
    await openByDialog(page, 'open-solution', vFix.sln, SLN_NAME);
    await expect(targetButton()).toBeEnabled({ timeout: 30_000 });
  };
  /** After coming back, the button already names the project and it is the first the picker offers, ticked. */
  const expectOffered = async (aName: string) => {
    await expect(targetButton()).toContainText(aName, { timeout: 30_000 });
    await openTargets(page);
    const vFirst = page.getByTestId('target-pop').getByRole('menuitemradio').first();
    await expect(vFirst).toContainText(aName);
    await expect(vFirst.locator('svg')).toHaveCount(1);
    await page.keyboard.press('Escape');
    await expect(page.getByTestId('target-pop')).toBeHidden();
  };
  try {
    await openByDialog(page, 'open-solution', vFix.sln, SLN_NAME);
    await expect(targetButton()).toBeEnabled({ timeout: 30_000 });

    // 1. the picker in the toolbar
    await openTargets(page);
    await page.getByTestId('target-pop').getByRole('menuitemradio').filter({ hasText: APP_TWO }).click();
    await expect(targetButton()).toContainText(APP_TWO);
    await reopen();
    await expectOffered(APP_TWO);

    // 2. Project > Project to run
    await openMenu(page, 'project');
    await expect(page.getByTestId('menu-project-items')).toContainText('Project to run');
    await page.getByTestId('menu-project-items').getByRole('menuitem').filter({ hasText: APP_ONE }).click();
    await expect(targetButton()).toContainText(APP_ONE);
    await reopen();
    await expectOffered(APP_ONE);

    // 3. right-click on the project's folder (and its .csproj) in Files > Set as the project to run
    const vSet = async (aRow: string, aExpect: string) => {
      await page.getByTestId(`tree-${aRow}-menu`).first().click({ button: 'right' });
      await expect(page.getByTestId('tree-set-run')).toBeVisible({ timeout: 10_000 });
      await page.screenshot({ path: join(SHOTS, `REQ-UI-106-files-menu-${aExpect}.png`), fullPage: true });
      await page.getByTestId('tree-set-run').click();
      await expect(targetButton()).toContainText(aExpect, { timeout: 20_000 });
    };
    await expect(page.getByTestId(`tree-${APP_TWO}`)).toBeVisible({ timeout: 20_000 });
    await vSet(APP_TWO, APP_TWO);
    await reopen();
    await expectOffered(APP_TWO);
    await page.getByTestId(`tree-${APP_ONE}`).locator('[data-slot=tree-item-row]').first().click(); // open the folder
    await expect(page.getByTestId(`tree-${APP_ONE}/${APP_ONE}.csproj`)).toBeVisible({ timeout: 20_000 });
    await vSet(`${APP_ONE}/${APP_ONE}.csproj`, APP_ONE);
    await reopen();
    await expectOffered(APP_ONE);
  } finally {
    vFix.cleanup();
  }
});
