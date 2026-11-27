// Verify-phase acceptance tests for Run, from the owner's evening test of 21 Nov 2026 (points 1.1 to 1.4):
// REQ-UI-107 (Run shows the build and the program's output live, in an Output panel that opens by itself, errors
// marked), REQ-FN-306 (a web project opens in the browser at its address once it is up), REQ-UI-108 (a failed build
// says so with its errors; an ended program shows its exit code). Proven on a real Blazor web app, a console app and a
// class library with tests, made from this machine's own templates under this test project's bin folder, and removed
// with every row recorded against them when the file ends. Web harness; the Windows app's own proof is in
// desktop-head.spec.ts. Signed in as the Owner test user.
import { test, expect, Page } from '@playwright/test';
import { signIn } from './support/auth';
import { removeProjectsUnder } from './support/cleanup';
import {
  CONSOLE_BROKEN, CONSOLE_PROGRAM, ProofSolution, deleteProofSolution, makeProofSolution, webLaunchAddress, writeConsole,
} from './support/run-proof';

const STAMP = Date.now();

test.describe.configure({ timeout: 420_000 });

let page: Page;
let proof: ProofSolution | undefined;

const output = () => page.getByTestId('output');
const pill = () => page.getByTestId('output-status');

test.beforeAll(async ({ browser }) => {
  test.setTimeout(600_000);
  proof = makeProofSolution('wsl', STAMP);
  page = await browser.newPage();
  try {
    await signIn(page, 'Owner');
  } catch {
    await signIn(page, 'Owner');
  }
  await page.goto('/start?all=1', { waitUntil: 'networkidle' });
  const vDialog = page.getByTestId('open-project-dialog');
  await expect(async () => {
    if (!(await vDialog.isVisible())) {
      await page.getByTestId((await page.getByTestId('empty-start').isVisible()) ? 'way-open-solution' : 'open-solution').click();
    }
    await expect(vDialog).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
  await page.locator('input[data-testid=open-project-path], [data-testid=open-project-path] input').first().fill(proof.solutionForApp);
  await page.getByTestId('open-project-go').click();
  await expect(vDialog).toBeHidden({ timeout: 60_000 });
  await expect(page.getByTestId('project-switch-name')).toHaveText('ProofApps', { timeout: 60_000 });
});

test.afterEach(async () => {
  try {
    if (await page.getByTestId('stop').isVisible({ timeout: 1_000 })) {
      await page.getByTestId('stop').click();
      await expect(page.getByTestId('stop')).toBeHidden({ timeout: 30_000 });
    }
  } catch {
    /* nothing running */
  }
});

test.afterAll(async () => {
  await page?.close();
  if (proof) removeProjectsUnder(proof.root);
  deleteProofSolution(proof);
});

/** Chooses a target by its name in the target picker. */
async function chooseTarget(aName: string): Promise<void> {
  await expect(async () => {
    if (!(await page.getByTestId('target-pop').isVisible())) await page.locator('[data-testid=target] button').first().click();
    await expect(page.getByTestId('target-pop')).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
  await page.getByTestId('target-pop').getByText(aName, { exact: true }).click();
  await expect(page.locator('[data-testid=target] button').first()).toContainText(aName);
}

/** Hides Output with View ▸ Output, so a run has to open it by itself. */
async function hideOutput(): Promise<void> {
  if (!(await output().isVisible())) return;
  await page.getByTestId('menu-view').click();
  await page.getByTestId('menu-view-output').click();
  await expect(output()).toBeHidden({ timeout: 10_000 });
  // A checkbox item leaves its menu open; Escape closes it so the next click reaches the toolbar.
  await page.keyboard.press('Escape');
  await expect(page.getByTestId('menu-view-items')).toBeHidden({ timeout: 10_000 });
}

test('REQ-UI-107 REQ-UI-108 running the console app opens Output by itself, shows the build then the program live, and its exit code', async () => {
  writeConsole(proof!, CONSOLE_PROGRAM);
  await chooseTarget('ProofConsole');
  await hideOutput();
  await page.getByTestId('run').click();
  await expect(output(), 'Output opened by itself').toBeVisible({ timeout: 10_000 });
  await expect(output()).toContainText('> dotnet build', { timeout: 30_000 });
  await expect(output()).toContainText(/Built in \d+(\.\d+)?s/, { timeout: 240_000 });
  await expect(output()).toContainText('> dotnet run');
  await expect(output()).toContainText('Proof console starting', { timeout: 60_000 });
  await expect(output()).toContainText('Proof console warning on stderr');
  await expect(output()).toContainText('Process exited with code 3.', { timeout: 60_000 });
  await expect(pill()).toHaveText('failed, exit code 3');
  await page.screenshot({ path: 'tests/.artifacts/verify/shots/REQ-UI-107-console-run.png' });
});

test('REQ-UI-108 REQ-UI-107 a run whose build fails says the build failed, lists each error with file and line, and starts nothing', async () => {
  writeConsole(proof!, CONSOLE_BROKEN);
  try {
    await chooseTarget('ProofConsole');
    await page.getByTestId('run').click();
    await expect(output()).toContainText('Build FAILED', { timeout: 240_000 });
    await expect(output()).toContainText('The program was not started.');
    await expect(output()).not.toContainText('> dotnet run');
    await expect(pill()).toHaveText('build failed');
    const vErrors = page.getByTestId('output-error');
    await expect(vErrors.first()).toContainText('ProofConsole/Program.cs:1');
    await expect(vErrors.first()).toContainText('; expected');
    await expect(output()).toContainText('error CS1002');
    // An error line in the log itself is marked.
    await expect(output()).toContainText(/Failure: \S+Program\.cs\(1,\d+\): error CS1002/);
    await expect(output()).toContainText(/[1-9]\d* failures?\./);
    await page.screenshot({ path: 'tests/.artifacts/verify/shots/REQ-UI-108-failed-build.png' });
  } finally {
    writeConsole(proof!, CONSOLE_PROGRAM);
  }
});

test('REQ-FN-306 REQ-UI-107 running the Blazor web app opens the browser at its launch-settings address once it is listening', async () => {
  const vAddress = webLaunchAddress(proof!);
  expect(vAddress).toMatch(/^https?:\/\/localhost:\d+$/);
  await chooseTarget('ProofWeb');
  await page.getByTestId('run').click();
  await expect(output()).toContainText('--launch-profile', { timeout: 240_000 });
  await expect(output()).toContainText('Now listening on:', { timeout: 120_000 });
  await expect(output()).toContainText(`Opening ${vAddress} in the browser`, { timeout: 30_000 });
  await expect(pill()).toHaveText('running');
  // The address answers: the app really is up where the browser was sent.
  const vResponse = await page.request.get(vAddress, { ignoreHTTPSErrors: true, timeout: 30_000 });
  expect(vResponse.status()).toBe(200);
  // That first request makes the template's https redirection log its "warn:" with the indented line "Failed to
  // determine the https port for redirect.": a warning by its own level, never marked as an error (BRD-235, 27 Nov).
  const vHttpsLine = output().getByText('Failed to determine the https port for redirect.');
  await expect(vHttpsLine.first()).toBeVisible({ timeout: 30_000 });
  await expect(output()).not.toContainText(/Failure:\s*\S*\s*Failed to determine the https port/);
  expect(await vHttpsLine.first().evaluate((e) => e.closest('[data-slot="log-view-line"]')?.getAttribute('data-log-kind'))).toBe('warning');
  await page.screenshot({ path: 'tests/.artifacts/verify/shots/REQ-FN-306-web-run.png' });
  await page.getByTestId('stop').click();
  await expect(output()).toContainText('Run stopped.', { timeout: 30_000 });
});

test('REQ-UI-107 REQ-UI-108 running the class library\'s test project builds it and runs its tests, with the exit code', async () => {
  await chooseTarget('ProofLib.Tests');
  await page.getByTestId('run').click();
  await expect(output()).toContainText('> dotnet build', { timeout: 30_000 });
  await expect(output()).toContainText('> dotnet test', { timeout: 240_000 });
  await expect(output()).toContainText(/Passed!|Total tests: 1/, { timeout: 180_000 });
  await expect(output()).toContainText('Process exited with code 0.', { timeout: 60_000 });
  await expect(pill()).toHaveText('ended, exit code 0');
});
