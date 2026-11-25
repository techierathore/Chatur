// Acceptance test for REQ-UI-112 (BRD-244, the owner's evening test of 21 Nov): File > New project with a solution
// open on the Workbench lists this machine's dotnet new templates, asks only for a name, and Create makes the
// project, adds it to the solution, and shows it in the files panel and the run targets. Black-box, web harness,
// signed in as test user 2 ('Owner'). The solution lives under tests/verify/bin and is deleted, with its project
// in the test account, when the test ends (owner, 20 Nov 2026).
import { test, expect, Page } from '@playwright/test';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'fs';
import { join, resolve } from 'path';
import { removeProjectsUnder, scratchDir } from './support/cleanup';
import { signIn } from './support/auth';
import { openTargetMenu, setFolder, tree } from './support/workbench-a';

const STAMP = Date.now();
const SHOTS = resolve(__dirname, '../.artifacts/verify/shots');

test.describe.configure({ timeout: 300_000 });

let objScratch = '';

test.afterAll(() => {
  if (objScratch) removeProjectsUnder(objScratch);
});

/** Opens a menu and clicks one of its items; clicks made before the circuit is interactive are retried. */
async function pick(aPage: Page, aMenu: string, aItem: string): Promise<void> {
  const vItems = aPage.getByTestId(`menu-${aMenu}-items`);
  await expect(async () => {
    if (!(await vItems.isVisible())) await aPage.getByTestId(`menu-${aMenu}`).click();
    await expect(vItems).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
  await aPage.getByTestId(`menu-${aMenu}-${aItem}`).click();
}

test('REQ-UI-112 New project with a solution open lists the machine templates, asks only for a name, and Create adds the project to the solution, the files and the run targets', async ({ page }) => {
  mkdirSync(SHOTS, { recursive: true });
  try {
    await signIn(page, 'Owner');
  } catch {
    await signIn(page, 'Owner');
  }

  // A solution of its own with no project in it. Chatur's Directory.Build.props and nuget.config sit above the
  // scratch folder, so each is shadowed here (as support/workbench-a.ts does for its fixture).
  objScratch = scratchDir(`new-project-${STAMP}`);
  const vSolutionName = `NpSln${STAMP}`;
  const vFolder = join(objScratch, vSolutionName);
  mkdirSync(vFolder, { recursive: true });
  writeFileSync(join(vFolder, 'Directory.Build.props'), '<Project />\n');
  writeFileSync(join(vFolder, 'nuget.config'), '<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear /></packageSources></configuration>\n');
  const vSolution = join(vFolder, `${vSolutionName}.sln`);
  writeFileSync(vSolution, '\nMicrosoft Visual Studio Solution File, Format Version 12.00\n# Visual Studio Version 17\nVisualStudioVersion = 17.0.31903.59\nMinimumVisualStudioVersion = 10.0.40219.1\n');

  // Open it as a solution, from Start.
  await page.goto('/start?all=1', { waitUntil: 'networkidle' });
  await expect(page.getByTestId('empty-start').or(page.getByTestId('recent-side'))).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('recent-loading')).toHaveCount(0, { timeout: 30_000 });
  const vWay = (await page.getByTestId('empty-start').isVisible()) ? 'way-open-solution' : 'open-solution';
  const vOpenDialog = page.getByTestId('open-project-dialog');
  await expect(async () => {
    if (!(await vOpenDialog.isVisible())) await page.getByTestId(vWay).click();
    await expect(vOpenDialog).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
  await page.getByTestId('open-project-path').fill(vSolution);
  await page.getByTestId('open-project-go').click();
  await expect(vOpenDialog).toBeHidden({ timeout: 30_000 });
  await expect(page.getByTestId('project-switch-name')).toHaveText(vSolutionName, { timeout: 30_000 });
  await expect(page).toHaveURL(/\/$/);

  // File > New project: the dialog is the template one, not the empty-solution one.
  const vDialog = page.getByTestId('new-project-dialog');
  await expect(async () => {
    if (!(await vDialog.isVisible())) await pick(page, 'file', 'new');
    await expect(vDialog).toBeVisible({ timeout: 3_000 });
  }).toPass({ timeout: 30_000 });
  await expect(vDialog).toContainText(`New project in ${vSolutionName}`);
  await expect(page.getByTestId('open-project-dialog')).toHaveCount(0);

  // The templates of this machine arrive (a few seconds the first time), with Console App among them.
  await expect(page.getByTestId('template-loading')).toHaveCount(0, { timeout: 120_000 });
  await expect(page.getByTestId('template-list')).toBeVisible();
  for (const vShort of ['console', 'classlib', 'blazor', 'xunit']) {
    await expect(page.getByTestId(`template-${vShort}`), `template ${vShort}`).toBeVisible();
  }
  await expect(page.getByTestId('template-console')).toContainText('Console App');
  await expect(page.getByTestId('template-console')).toContainText('console');
  await page.getByTestId('template-console').click();

  // Only the name must be typed; the framework has a default and the folder is the solution's own.
  const vProject = `NpTools${STAMP}`;
  await expect(page.getByTestId('new-project-create')).toBeDisabled();
  await page.getByTestId('new-project-name').fill(vProject);
  await page.getByTestId('new-project-name').press('Tab');
  await expect(page.getByTestId('new-project-framework')).toBeVisible({ timeout: 60_000 });
  await expect(page.getByTestId('new-project-framework-button')).toContainText('net10.0', { timeout: 60_000 });
  // The folder is shown, not typed, and the two paths that will exist are named (REQ-UI-126).
  await expect(page.getByTestId('new-project-folder')).toHaveText(vFolder);
  await expect(page.getByTestId('new-project-solution-path')).toHaveText(vSolution);
  await expect(page.getByTestId('new-project-project-path')).toHaveText(join(vFolder, vProject));
  await expect(page.getByTestId('new-project-create')).toBeEnabled();
  await page.screenshot({ path: `${SHOTS}/REQ-UI-112-dialog.png` });
  await page.getByTestId('new-project-create').click();

  // The dialog closes unless dotnet failed, in which case its words are shown.
  const vState = async () => {
    if (await page.getByTestId('new-project-error').isVisible()) return 'error';
    return (await vDialog.isVisible()) ? 'open' : 'closed';
  };
  await expect.poll(vState, { timeout: 240_000, intervals: [500, 1_000, 2_000] }).not.toBe('open');
  if ((await vState()) === 'error') {
    throw new Error(`Create failed: ${await page.getByTestId('new-project-error').innerText()}`);
  }
  await expect(vDialog).toBeHidden({ timeout: 10_000 });

  // The project is on disk, in the solution file, in the files panel and among the run targets.
  const vCsproj = join(vFolder, vProject, `${vProject}.csproj`);
  expect(existsSync(vCsproj), 'the .csproj was made').toBe(true);
  expect(readFileSync(vSolution, 'utf8')).toContain(`${vProject}.csproj`);
  await setFolder(page, vProject, true);
  await expect(tree(page, `${vProject}/${vProject}.csproj`)).toBeVisible({ timeout: 30_000 });
  await openTargetMenu(page);
  await expect(page.getByTestId('target-pop')).toContainText(vProject, { timeout: 30_000 });
  await page.screenshot({ path: `${SHOTS}/REQ-UI-112-done.png` });
});
