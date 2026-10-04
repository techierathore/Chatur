// Verification of the Workbench build / run / output / open-externally / sign-out rows (verify-phase step 4).
// Black-box: drives the running web harness only; one signed-in page is shared except for the sign-out test.
import { Page, expect } from '@playwright/test';
import { readFileSync, rmSync } from 'fs';
import { join } from 'path';
import { signIn } from './support/auth';
import {
  test, existsSync, NAME, PROJECT, PID_FILE, APP_LOG, PROGRAM_BROKEN, resetFixture, write, openProject,
  openTargetMenu, openFile, projectName, tabs, outputText, pill, editorText, shot, stopIfRunning,
} from './support/workbench-a';

let page: Page;

test.beforeAll(async ({ browser }) => {
  page = await browser.newPage({ baseURL: process.env.BASE_URL });
  await signIn(page, 'Yolo');
});
test.afterAll(async () => { await page.close(); });
test.beforeEach(async () => {
  test.setTimeout(150_000);
  resetFixture();
  rmSync(PID_FILE, { force: true });
});
test.afterEach(async () => { await stopIfRunning(page); });

test('REQ-UI-012 building shows the result as succeeded or failed with the time it took', async () => {
  await openProject(page);
  await page.getByTestId('build').click();
  await expect(pill(page)).toContainText('build passed', { timeout: 90_000 });
  await expect(outputText(page)).toContainText(/Build succeeded in \d+(\.\d+)?s/);
  await shot(page, 'ui-012-succeeded');
  write(join(PROJECT, 'Program.cs'), PROGRAM_BROKEN);
  await expect(page.getByTestId('build')).toBeEnabled({ timeout: 30_000 });
  await page.getByTestId('build').click();
  await expect(pill(page)).toContainText('build failed', { timeout: 90_000 });
  await expect(outputText(page)).toContainText(/Build FAILED in \d+(\.\d+)?s/);
  await shot(page, 'ui-012-failed');
});

test('REQ-UI-016 a failed build lists each error with its file and line, and opening one opens that file', async () => {
  resetFixture(PROGRAM_BROKEN);
  await openProject(page);
  await page.getByTestId('build').click();
  await expect(pill(page)).toContainText('build failed', { timeout: 90_000 });
  const vErrors = page.getByTestId('output-error');
  await expect(vErrors).toHaveCount(2, { timeout: 30_000 });
  await expect(vErrors.nth(0)).toContainText('Program.cs:1');
  await expect(vErrors.nth(1)).toContainText('Program.cs:3');
  await shot(page, 'ui-016-errors');
  await vErrors.nth(1).click();
  await expect(tabs(page).getByText('Program.cs', { exact: true })).toBeVisible({ timeout: 20_000 });
  await expect(editorText(page)).toHaveValue(/WBA third/);
});

test('REQ-FN-010 returning to Run offers the target used last time already chosen', async () => {
  await openProject(page);
  const vCurrent = (await page.getByTestId('target').innerText()).trim();
  const vWant = vCurrent.includes('Release') ? 'Debug' : 'Release';
  await openTargetMenu(page);
  await page.getByTestId('target-pop').getByRole('menuitem').filter({ hasText: `· ${vWant}` }).click();
  await expect(page.getByTestId('target')).toContainText(`· ${vWant}`);
  await page.getByTestId('build').click();
  await expect(pill(page)).toContainText('build passed', { timeout: 90_000 });
  await page.goto('/');
  await expect(projectName(page)).toHaveText(NAME, { timeout: 20_000 });
  await expect(page.getByTestId('target')).toContainText(`· ${vWant}`, { timeout: 20_000 });
});

test('REQ-UI-013 running the chosen target starts the program and Run shows it as running', async () => {
  await openProject(page);
  await page.getByTestId('run').click();
  await expect(pill(page)).toContainText('running', { timeout: 90_000 });
  await expect(outputText(page)).toContainText('WBA starting', { timeout: 60_000 });
  await expect(page.getByTestId('stop')).toBeEnabled();
  await shot(page, 'ui-013-running');
});

test('REQ-UI-014 a line the program writes appears in the output panel while the program is still running', async () => {
  await openProject(page);
  await page.getByTestId('run').click();
  await expect(outputText(page)).toContainText('WBA tick 2', { timeout: 90_000 });
  await expect(outputText(page)).not.toContainText('WBA done');
  await expect(pill(page)).toContainText('running');
  await expect(outputText(page)).toContainText('WBA tick 4', { timeout: 30_000 });
});

test('REQ-UI-015 stopping ends every program the run started and Run shows it as stopped', async () => {
  await openProject(page);
  await page.getByTestId('run').click();
  await expect(outputText(page)).toContainText('WBA tick 1', { timeout: 90_000 });
  await expect.poll(() => existsSync(PID_FILE), { timeout: 10_000 }).toBe(true);
  const vPid = Number(readFileSync(PID_FILE, 'utf8').trim());
  expect(existsSync(`/proc/${vPid}`)).toBe(true);
  await page.getByTestId('stop').click();
  await expect.poll(() => existsSync(`/proc/${vPid}`), { timeout: 20_000 }).toBe(false);
  await expect(outputText(page)).toContainText('Run stopped.', { timeout: 20_000 });
  await expect(page.getByTestId('run')).toBeEnabled({ timeout: 20_000 });
  await shot(page, 'ui-015-stopped');
  await expect(pill(page)).toContainText('stopped', { timeout: 5_000 });
  await expect(pill(page)).not.toContainText('running');
});

/** Text of the app's own log, for the launcher's "Would open ..." lines (the web harness only logs them). */
const appLog = () => (existsSync(APP_LOG) ? readFileSync(APP_LOG, 'utf8') : '');
const countIn = (aNeedle: string) => appLog().split(aNeedle).length - 1;

test('REQ-FN-045 choosing the owner\'s editor for a file reaches the app without error', async () => {
  test.info().annotations.push({ type: 'skipped-clause', description: 'The program opening with the file is a native launch; the web harness launcher only logs it by design, so only the request reaching the app is verified.' });
  await openProject(page);
  await openFile(page, 'Program.cs');
  const vNeedle = `Would open ${join(PROJECT, 'Program.cs')} in the owner's editor`;
  const vBefore = countIn(vNeedle);
  await page.getByTestId('open-in-editor').click();
  await expect.poll(() => countIn(vNeedle), { timeout: 15_000 }).toBeGreaterThan(vBefore);
  await expect(page.getByTestId('editor')).toBeVisible();
  await expect(page.getByText(/unhandled|error has occurred/i)).toHaveCount(0);
});

test('REQ-FN-046 opening a file with the system default application reaches the app without error', async () => {
  test.info().annotations.push({ type: 'skipped-clause', description: 'The machine opening the file in its own program is a native launch; the web harness launcher only logs it by design, so only the request reaching the app is verified.' });
  await openProject(page);
  await openFile(page, 'Program.cs');
  const vNeedle = `Would open ${join(PROJECT, 'Program.cs')} with the machine's default application`;
  const vBefore = countIn(vNeedle);
  await page.getByTestId('open-with-default').click();
  await expect.poll(() => countIn(vNeedle), { timeout: 15_000 }).toBeGreaterThan(vBefore);
  await expect(page.getByTestId('editor')).toBeVisible();
  await expect(page.getByText(/unhandled|error has occurred/i)).toHaveCount(0);
});

// Signs out for every window of the shared app, so it is the last test in the file and uses its own context.
test('REQ-FN-009 signing out returns to Sign in and the stored token no longer opens the app', async ({ browser }) => {
  const vContext = await browser.newContext({ baseURL: process.env.BASE_URL });
  const vPage = await vContext.newPage();
  try {
    await signIn(vPage, 'Yolo');
    await vPage.goto('/');
    await expect(projectName(vPage)).toBeVisible({ timeout: 30_000 });
    await expect(async () => {
      if (!(await vPage.getByTestId('account-pop').isVisible())) await vPage.getByTestId('account').click();
      await expect(vPage.getByTestId('sign-out')).toBeVisible({ timeout: 2_500 });
    }).toPass({ timeout: 30_000 });
    await vPage.getByTestId('sign-out').click();
    await expect(vPage).toHaveURL(/\/sign-in/, { timeout: 30_000 });
    const vFresh = await browser.newContext({ baseURL: process.env.BASE_URL });
    try {
      const vFreshPage = await vFresh.newPage();
      await vFreshPage.goto('/');
      await expect(vFreshPage).toHaveURL(/\/sign-in/, { timeout: 30_000 });
      await vFreshPage.goto('/start');
      await expect(vFreshPage).toHaveURL(/\/sign-in/, { timeout: 30_000 });
    } finally {
      await vFresh.close();
    }
    await vPage.reload();
    await expect(vPage).toHaveURL(/\/sign-in/, { timeout: 30_000 });
  } finally {
    await vContext.close();
  }
});
