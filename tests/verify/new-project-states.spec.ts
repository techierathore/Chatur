// Acceptance tests for New project from every starting point a person meets (owner's tests of 22 and 23 Nov 2026;
// BRD-244, BRD-247, BRD-248, BRD-252, BRD-276 to BRD-280): nothing open; an empty folder open; a folder that is open
// and not empty; a folder with projects and no solution; a folder with a solution. Each test compares the EXACT
// files on disk after Create with the ones wanted: the solution file in the folder itself, the project in one folder
// directly under it, never a folder named after the solution inside the folder. On 23 Nov the owner got exactly that
// extra folder while the earlier tests passed, because they only asked whether some solution file was in a folder
// that was truly empty; his held one file of Chatur's own.
//
// Never from a solution written by hand (REQ-NFR-013): a starting point that needs a project or a solution has it
// made by this machine's own dotnet first. Black-box, web harness, signed in as test user 2 ('Owner'). The "nothing
// open" state is made in the test account's own database and put back exactly as it was; every folder a test makes
// lives under tests/verify/bin and is deleted, with its projects in the test account, when the test ends.
import { test, expect, Page } from '@playwright/test';
import { execFileSync } from 'child_process';
import { existsSync, mkdirSync, readFileSync, readdirSync, statSync, writeFileSync } from 'fs';
import { join, resolve } from 'path';
import { signIn } from './support/auth';
import { removeProjectsUnder, scratchDir } from './support/cleanup';
import { run, snapshot, restore } from './support/db';
import { openByDialog, pick } from './support/workbench-b';

test.describe.configure({ mode: 'serial', timeout: 420_000 });

const STAMP = Date.now();
const SHOTS = resolve(__dirname, '../.artifacts/verify/shots');
let objScratch = '';

test.beforeAll(() => {
  mkdirSync(SHOTS, { recursive: true });
  objScratch = scratchDir(`new-project-states-${STAMP}`);
});

test.afterAll(() => {
  if (objScratch) removeProjectsUnder(objScratch);
});

/** Signs in as the Owner test user; a first attempt made while the circuit starts is retried once. */
async function signedIn(aPage: Page): Promise<void> {
  try {
    await signIn(aPage, 'Owner');
  } catch {
    await signIn(aPage, 'Owner');
  }
}

/**
 * Every file under a folder, relative to it, in name order: the disk as the person sees it. Build output (bin, obj)
 * is left out, since dotnet writes it on restore and on Run and its file names change with the SDK.
 */
function disk(aFolder: string): string[] {
  const vFound: string[] = [];
  const vWalk = (aDir: string, aRel: string) => {
    for (const vName of readdirSync(aDir)) {
      const vRel = aRel ? `${aRel}/${vName}` : vName;
      if (statSync(join(aDir, vName)).isDirectory()) {
        if (vName !== 'bin' && vName !== 'obj') vWalk(join(aDir, vName), vRel);
      } else {
        vFound.push(vRel);
      }
    }
  };
  vWalk(aFolder, '');
  return vFound.sort();
}

/** Runs this machine's own dotnet in a folder, as a person would from a terminal, to make a starting point. */
function dotnet(aFolder: string, ...aArgs: string[]): void {
  execFileSync(`${process.env.HOME}/.dotnet/dotnet`, aArgs, { cwd: aFolder, stdio: 'pipe', timeout: 180_000 });
}

/** Opens File > New project on the Workbench. */
async function newProject(aPage: Page): Promise<void> {
  const vDialog = aPage.getByTestId('new-project-dialog');
  await expect(async () => {
    if (!(await vDialog.isVisible())) await pick(aPage, 'file', 'new');
    await expect(vDialog).toBeVisible({ timeout: 3_000 });
  }).toPass({ timeout: 30_000 });
}

/** Waits for the machine's templates in the open New project dialog and picks one. */
async function pickTemplate(aPage: Page, aShortName: string): Promise<void> {
  await expect(aPage.getByTestId('template-loading')).toHaveCount(0, { timeout: 120_000 });
  for (const vShort of ['console', 'classlib', 'blazor', 'xunit']) {
    await expect(aPage.getByTestId(`template-${vShort}`), `template ${vShort}`).toBeVisible();
  }

  await aPage.getByTestId(`template-${aShortName}`).click();
  await expect(aPage.getByTestId('new-project-framework-button')).toContainText('net10.0', { timeout: 60_000 });
}

/** Presses Create and waits for the dialog to close, failing with dotnet's own words when it could not. */
async function create(aPage: Page): Promise<void> {
  const vDialog = aPage.getByTestId('new-project-dialog');
  await expect(aPage.getByTestId('new-project-create')).toBeEnabled();
  await aPage.getByTestId('new-project-create').click();
  const vState = async () => {
    if (await aPage.getByTestId('new-project-error').isVisible()) return 'error';
    return (await vDialog.isVisible()) ? 'open' : 'closed';
  };
  await expect.poll(vState, { timeout: 240_000, intervals: [500, 1_000, 2_000] }).not.toBe('open');
  if ((await vState()) === 'error') throw new Error(`Create failed: ${await aPage.getByTestId('new-project-error').innerText()}`);
}

/** The Run target picker names this project, and never stays on "No target found". */
async function runHasTarget(aPage: Page, aProject: string): Promise<void> {
  const vPicker = aPage.getByTestId('target').locator('button');
  await expect(vPicker).toContainText(aProject, { timeout: 30_000 });
  await expect(vPicker).not.toContainText('No target found');
  await expect(aPage.getByTestId('run')).toBeEnabled();
}

/** The names at the top of FILES: what sits directly in the folder that is its root. */
async function filesRoot(aPage: Page): Promise<string[]> {
  const vIds = await aPage.getByTestId('file-tree').locator('[data-testid^="tree-"]').evaluateAll((aRows) => aRows.map((r) => r.getAttribute('data-testid') ?? ''));
  const vNames = vIds.map((i) => i.slice('tree-'.length)).filter((n) => n.length > 0 && !n.includes('/'));
  // A row carries ids of its own parts too (its right-click menu, its run label): only the rows themselves count.
  return vNames.filter((n) => !/-(menu|run)$/.test(n) || !vNames.includes(n.replace(/-(menu|run)$/, ''))).sort();
}

test('REQ-NFR-018 REQ-FN-310 REQ-FN-309 REQ-UI-112 with nothing open, New project on Nothing is open yet has one Location field, and the picked folder holds the solution file and the project one folder under it', async ({ page }) => {
  await signedIn(page);
  const vSaved = snapshot(['ProjectFolder', 'Project', 'Setting']);
  const vSolution = `Fresh${STAMP}`;
  const vPicked = join(objScratch, `Picked${STAMP}`);
  try {
    // The first start after install: no project, no folder, nothing selected.
    run("DELETE FROM Setting WHERE Key = 'SelectedProjectId'");
    run("UPDATE Project SET ProjectFolderId = NULL, RemovedUtc = '2026-11-22T00:00:00.0000000Z' WHERE RemovedUtc IS NULL");
    run('DELETE FROM ProjectFolder');
    await page.goto('/start', { waitUntil: 'networkidle' });
    await expect(page.getByTestId('empty-start-title')).toHaveText('Nothing is open yet', { timeout: 30_000 });
    await expect(page.getByTestId('way-new-project')).toContainText('New project');
    await page.screenshot({ path: `${SHOTS}/REQ-FN-310-nothing-open.png` });

    const vDialog = page.getByTestId('new-project-dialog');
    await expect(async () => {
      if (!(await vDialog.isVisible())) await page.getByTestId('way-new-project').click();
      await expect(vDialog).toBeVisible({ timeout: 2_500 });
    }).toPass({ timeout: 30_000 });
    await expect(vDialog).toContainText('New project');
    await expect(page.getByTestId('open-project-dialog')).toHaveCount(0);
    await pickTemplate(page, 'console');

    // One Location field, empty; nothing is suggested outside what the person picks. Create waits for it.
    await expect(page.getByTestId('new-project-location')).toHaveValue('');
    await expect(page.getByTestId('new-project-folder')).toHaveCount(0);
    await expect(page.getByTestId('new-project-create')).toBeDisabled();
    await page.getByTestId('new-project-solution').fill(vSolution);
    await expect(page.getByTestId('new-project-create')).toBeDisabled();
    await page.getByTestId('new-project-location').fill(vPicked);
    // The two full paths that will exist, before anything is made.
    await expect(page.getByTestId('new-project-solution-path')).toHaveText(join(vPicked, `${vSolution}.slnx`));
    await expect(page.getByTestId('new-project-project-path')).toHaveText(join(vPicked, vSolution));
    expect(existsSync(vPicked), 'nothing is made before Create').toBe(false);
    await page.screenshot({ path: `${SHOTS}/REQ-FN-310-dialog.png` });
    await create(page);

    await expect(page).toHaveURL(/\/$/, { timeout: 30_000 });
    await expect(page.getByTestId('project-switch-name')).toHaveText(vSolution, { timeout: 30_000 });
    expect(disk(vPicked), 'the files in the picked folder').toEqual([`${vSolution}.slnx`, `${vSolution}/Program.cs`, `${vSolution}/${vSolution}.csproj`].sort());
    expect(readFileSync(join(vPicked, `${vSolution}.slnx`), 'utf8')).toContain(`${vSolution}/${vSolution}.csproj`);
    await runHasTarget(page, vSolution);
    await page.screenshot({ path: `${SHOTS}/REQ-FN-309-run-target.png` });
  } finally {
    removeProjectsUnder(vPicked);
    restore(vSaved);
  }
});

test('REQ-NFR-018 REQ-FN-310 REQ-UI-126 REQ-UI-125 REQ-FN-309 with an empty folder open, New project shows the folder and the two paths as the names are typed, makes the solution file in that folder and the project one folder under it, and FILES keeps the folder as its root', async ({ page }) => {
  await signedIn(page);
  const vName = `Empty${STAMP}`;
  const vFolder = join(objScratch, vName);
  mkdirSync(vFolder, { recursive: true });
  await openByDialog(page, 'open-folder', vFolder, vName);
  await expect(page.getByTestId('target').locator('button')).toContainText('No target found');

  await newProject(page);
  await pickTemplate(page, 'console');
  // The folder that is open is shown, not typed; "Change location" is there for the rare case.
  await expect(page.getByTestId('new-project-folder')).toHaveText(vFolder);
  await expect(page.getByTestId('new-project-location')).toHaveCount(0);
  await expect(page.getByTestId('new-project-change-location')).toBeVisible();
  await expect(page.getByTestId('new-project-create')).toBeDisabled();

  // The two paths follow the names as they are typed.
  const vSolution = `Todo${STAMP}`;
  await page.getByTestId('new-project-solution').fill(vSolution);
  await expect(page.getByTestId('new-project-solution-path')).toHaveText(join(vFolder, `${vSolution}.slnx`));
  await expect(page.getByTestId('new-project-project-path')).toHaveText(join(vFolder, vSolution));
  const vProject = `TodoApp${STAMP}`;
  await page.getByTestId('new-project-name').fill(vProject);
  await expect(page.getByTestId('new-project-project-path')).toHaveText(join(vFolder, vProject));
  await expect(page.getByTestId('new-project-solution-path')).toHaveText(join(vFolder, `${vSolution}.slnx`));
  await page.screenshot({ path: `${SHOTS}/REQ-UI-126-empty-folder-dialog.png` });
  await create(page);

  await expect(page.getByTestId('project-switch-name')).toHaveText(vSolution, { timeout: 30_000 });
  expect(disk(vFolder), 'the files in the folder that was open').toEqual([`${vProject}/Program.cs`, `${vProject}/${vProject}.csproj`, `${vSolution}.slnx`].sort());
  await runHasTarget(page, vProject);
  // FILES: the folder that was open is still the root, holding the solution file and the project folder.
  await expect(page.getByTestId(`tree-${vSolution}.slnx`)).toBeVisible({ timeout: 30_000 });
  await expect.poll(() => filesRoot(page), { timeout: 30_000 }).toEqual([vProject, `${vSolution}.slnx`].sort());
  await page.screenshot({ path: `${SHOTS}/REQ-UI-125-files-root.png` });

  // With the solution now open, File > New project is the same dialog, adding to it (REQ-UI-112): the folder shown is
  // the same folder, the solution file is the one already there, and the project goes one folder under it.
  await newProject(page);
  const vDialog = page.getByTestId('new-project-dialog');
  await expect(vDialog).toContainText(`New project in ${vSolution}`);
  await expect(page.getByTestId('template-list')).toBeVisible({ timeout: 120_000 });
  await expect(page.getByTestId('new-project-solution')).toHaveCount(0);
  await expect(page.getByTestId('new-project-folder')).toHaveText(vFolder);
  await expect(page.getByTestId('new-project-paths')).toContainText('already there');
  await expect(page.getByTestId('new-project-solution-path')).toHaveText(join(vFolder, `${vSolution}.slnx`));
  await page.getByTestId('new-project-name').fill('Second');
  await expect(page.getByTestId('new-project-project-path')).toHaveText(join(vFolder, 'Second'));
  // "Change location" turns the folder into a field that starts on the same folder.
  await page.getByTestId('new-project-change-location').click();
  await expect(page.getByTestId('new-project-location')).toHaveValue(vFolder);
  await page.getByTestId('new-project-cancel').click();
  expect(disk(vFolder), 'Cancel made nothing').toEqual([`${vProject}/Program.cs`, `${vProject}/${vProject}.csproj`, `${vSolution}.slnx`].sort());
});

test('REQ-NFR-018 REQ-FN-310 the owner\'s case: a folder that is open and holds only a docs folder gets the solution file in the folder itself, never a folder named after the solution inside it', async ({ page }) => {
  await signedIn(page);
  const vName = `OwnerCase${STAMP}`;
  const vFolder = join(objScratch, vName);
  mkdirSync(join(vFolder, 'docs', 'metrics'), { recursive: true });
  writeFileSync(join(vFolder, 'docs', 'metrics', 'sessions.jsonl'), '{"v":"1","tool":"chatur","kind":"session"}\n');
  await openByDialog(page, 'open-folder', vFolder, vName);

  await newProject(page);
  await pickTemplate(page, 'console');
  const vSolution = `Checking-Chatur${STAMP}`;
  await page.getByTestId('new-project-solution').fill(vSolution);
  await expect(page.getByTestId('new-project-folder')).toHaveText(vFolder);
  await expect(page.getByTestId('new-project-solution-path')).toHaveText(join(vFolder, `${vSolution}.slnx`));
  await expect(page.getByTestId('new-project-project-path')).toHaveText(join(vFolder, vSolution));
  await create(page);

  await expect(page.getByTestId('project-switch-name')).toHaveText(vSolution, { timeout: 30_000 });
  expect(disk(vFolder), 'the files in the folder that was open').toEqual([
    `${vSolution}.slnx`, `${vSolution}/${vSolution}.csproj`, `${vSolution}/Program.cs`, 'docs/metrics/sessions.jsonl',
  ].sort());
  expect(existsSync(join(vFolder, vSolution, vSolution)), 'no folder of the same name inside itself').toBe(false);
  await expect.poll(() => filesRoot(page), { timeout: 30_000 }).toEqual([vSolution, `${vSolution}.slnx`, 'docs'].sort());
  await runHasTarget(page, vSolution);
});

test('REQ-NFR-018 REQ-UI-124 REQ-FN-309 with a folder open that has a project and no solution, New project says so first, makes the solution in that folder, and the solution holds that project and the new one', async ({ page }) => {
  await signedIn(page);
  const vName = `Loose${STAMP}`;
  const vFolder = join(objScratch, vName);
  mkdirSync(vFolder, { recursive: true });
  // The starting point, made the way a person makes it: dotnet new, and no solution.
  dotnet(vFolder, 'new', 'console', '-n', 'First', '-o', join(vFolder, 'First'));
  expect(disk(vFolder)).toEqual(['First/First.csproj', 'First/Program.cs'].sort());
  await openByDialog(page, 'open-folder', vFolder, vName);

  await newProject(page);
  await pickTemplate(page, 'classlib');
  // Told first: the folder has a project and no solution, and Create will take it in.
  await expect(page.getByTestId('new-project-found')).toContainText('has a project and no solution');
  await expect(page.getByTestId('new-project-found')).toContainText(join('First', 'First.csproj'));
  const vSolution = `Work${STAMP}`;
  await page.getByTestId('new-project-solution').fill(vSolution);
  await page.getByTestId('new-project-name').fill('Second');
  await expect(page.getByTestId('new-project-solution-path')).toHaveText(join(vFolder, `${vSolution}.slnx`));
  await expect(page.getByTestId('new-project-project-path')).toHaveText(join(vFolder, 'Second'));
  await page.screenshot({ path: `${SHOTS}/REQ-UI-124-projects-no-solution.png` });
  await create(page);

  await expect(page.getByTestId('project-switch-name')).toHaveText(vSolution, { timeout: 30_000 });
  expect(disk(vFolder), 'the files in the folder that was open').toEqual([
    'First/First.csproj', 'First/Program.cs', 'Second/Class1.cs', 'Second/Second.csproj', `${vSolution}.slnx`,
  ].sort());
  const vText = readFileSync(join(vFolder, `${vSolution}.slnx`), 'utf8');
  expect(vText, 'the project that had no solution is in it').toContain('First/First.csproj');
  expect(vText, 'the new project is in it').toContain('Second/Second.csproj');
  await runHasTarget(page, 'First');
});

test('REQ-NFR-018 REQ-FN-323 with a folder open that has a solution, New project adds the project to that solution, one folder under it, and makes no second solution', async ({ page }) => {
  await signedIn(page);
  const vName = `HasSln${STAMP}`;
  const vFolder = join(objScratch, vName);
  mkdirSync(vFolder, { recursive: true });
  // The starting point, made by this machine's dotnet: a solution with one project, opened as a folder.
  dotnet(vFolder, 'new', 'sln', '--name', 'Kept', '-o', vFolder);
  dotnet(vFolder, 'new', 'console', '-n', 'First', '-o', join(vFolder, 'First'));
  const vSln = readdirSync(vFolder).find((f) => /\.slnx?$/.test(f))!;
  dotnet(vFolder, 'sln', join(vFolder, vSln), 'add', join(vFolder, 'First', 'First.csproj'));
  await openByDialog(page, 'open-folder', vFolder, vName);

  await newProject(page);
  await pickTemplate(page, 'classlib');
  // The folder has a solution: no solution name is asked for, and its file is named as already there.
  await expect(page.getByTestId('new-project-solution')).toHaveCount(0);
  await expect(page.getByTestId('new-project-paths')).toContainText('already there');
  await expect(page.getByTestId('new-project-solution-path')).toHaveText(join(vFolder, vSln));
  await page.getByTestId('new-project-name').fill('Added');
  await expect(page.getByTestId('new-project-project-path')).toHaveText(join(vFolder, 'Added'));
  await create(page);

  await expect(page.getByTestId('project-switch-name')).toHaveText('Kept', { timeout: 30_000 });
  expect(disk(vFolder), 'the files in the folder that was open').toEqual([
    'Added/Added.csproj', 'Added/Class1.cs', 'First/First.csproj', 'First/Program.cs', vSln,
  ].sort());
  const vText = readFileSync(join(vFolder, vSln), 'utf8');
  expect(vText).toContain('Added/Added.csproj');
  expect(vText).toContain('First/First.csproj');
  await runHasTarget(page, 'First');
});
