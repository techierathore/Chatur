// Acceptance tests for New project from every starting point a person meets (owner's tests of 22, 23 and 25 Nov 2026;
// BRD-244, BRD-247, BRD-248, BRD-252, BRD-276 to BRD-280): nothing open; an empty folder open; a folder that is open
// and not empty; a folder with projects and no solution; a folder with a solution. Each test compares the EXACT
// files on disk after Create with the ones wanted. Since 25 Nov, as in Visual Studio: a new solution gets a folder of
// its own named after it inside the location, the solution file in that folder and each project in a folder of its
// own inside that, also when the two names are the same; nothing lands directly in the location. The one exception is
// the open folder that holds projects and no solution, whose solution is made in that folder and takes them in.
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
// New project is tall (search, list, names, where, the paths and what it will take in); the owner's window is larger than the default 720.
test.use({ viewport: { width: 1280, height: 1000 } });

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

  // The search box narrows the long list to the one wanted, so it is in reach however tall the dialog is (REQ-UI-127).
  await aPage.getByTestId('template-search').fill(aShortName);
  await aPage.getByTestId(`template-${aShortName}`).dispatchEvent('click');
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

/**
 * What FILES shows (REQ-UI-125): the one root row that stands for the solution, and the names directly under it. A row
 * carries ids of its own parts too (its right-click menu, its run label): only the rows themselves count.
 */
async function filesTree(aPage: Page): Promise<{ roots: string[]; children: string[] }> {
  const vRows = await aPage.getByTestId('file-tree').locator('[data-testid^="tree-"]').evaluateAll((aEls) => aEls.map((e) => ({ id: e.getAttribute('data-testid') ?? '', kind: e.getAttribute('data-kind') ?? '' })));
  const vOwn = vRows.map((r) => ({ name: r.id.slice('tree-'.length), kind: r.kind })).filter((r) => r.name.length > 0 && !r.name.includes('/'));
  const vRowsOnly = vOwn.filter((r) => !/-(menu|run)$/.test(r.name) || !vOwn.some((o) => o.name === r.name.replace(/-(menu|run)$/, '')));
  return {
    roots: vRowsOnly.filter((r) => r.kind === 'solution').map((r) => r.name).sort(),
    children: vRowsOnly.filter((r) => r.kind !== 'solution').map((r) => r.name).sort(),
  };
}

test('REQ-NFR-018 REQ-FN-310 REQ-FN-309 REQ-UI-112 REQ-UI-127 with nothing open, New project on Nothing is open yet has one Location field, searches the project types, and makes the solution its own folder in the picked folder, the project in its own folder inside it', async ({ page }) => {
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
    // REQ-UI-127: the search box narrows the long list as the person types, and says so when nothing matches.
    await expect(page.getByTestId('template-list')).toBeVisible({ timeout: 120_000 });
    const vAll = await page.locator('[data-testid^="template-"][data-testid$="-x"], [data-testid=template-list] > button').count();
    await page.getByTestId('template-search').fill('class lib');
    await expect(page.getByTestId('template-classlib')).toBeVisible();
    await expect(page.getByTestId('template-console')).toHaveCount(0);
    expect(await page.locator('[data-testid=template-list] > button').count(), 'the search narrowed the list').toBeLessThan(vAll);
    await page.getByTestId('template-search').fill('zzzz-not-a-type');
    await expect(page.getByTestId('template-none')).toBeVisible();
    await page.getByTestId('template-search').fill('');
    await expect(page.getByTestId('template-none')).toHaveCount(0);
    await pickTemplate(page, 'console');

    // One Location field, empty; nothing is suggested outside what the person picks. Create waits for it.
    await expect(page.getByTestId('new-project-location')).toHaveValue('');
    await expect(page.getByTestId('new-project-folder')).toHaveCount(0);
    await expect(page.getByTestId('new-project-create')).toBeDisabled();
    await page.getByTestId('new-project-solution').fill(vSolution);
    await expect(page.getByTestId('new-project-create')).toBeDisabled();
    await page.getByTestId('new-project-location').fill(vPicked);
    // The two full paths that will exist, before anything is made.
    const vSolutionFolder = join(vPicked, vSolution);
    await expect(page.getByTestId('new-project-solution-folder')).toHaveText(vSolutionFolder);
    await expect(page.getByTestId('new-project-solution-path')).toHaveText(join(vSolutionFolder, `${vSolution}.slnx`));
    await expect(page.getByTestId('new-project-project-path')).toHaveText(join(vSolutionFolder, vSolution));
    expect(existsSync(vPicked), 'nothing is made before Create').toBe(false);
    await page.screenshot({ path: `${SHOTS}/REQ-FN-310-dialog.png` });
    await create(page);

    await expect(page).toHaveURL(/\/$/, { timeout: 30_000 });
    await expect(page.getByTestId('project-switch-name')).toHaveText(vSolution, { timeout: 30_000 });
    // Nothing lands directly in the picked folder: the solution's folder, the solution in it, the project in its own folder inside.
    expect(readdirSync(vPicked), 'what the picked folder holds').toEqual([vSolution]);
    expect(disk(vPicked), 'the files in the picked folder').toEqual([`${vSolution}/${vSolution}.slnx`, `${vSolution}/${vSolution}/Program.cs`, `${vSolution}/${vSolution}/${vSolution}.csproj`].sort());
    expect(readFileSync(join(vPicked, vSolution, `${vSolution}.slnx`), 'utf8')).toContain(`${vSolution}/${vSolution}.csproj`);
    await runHasTarget(page, vSolution);
    await page.screenshot({ path: `${SHOTS}/REQ-FN-309-run-target.png` });
  } finally {
    removeProjectsUnder(vPicked);
    restore(vSaved);
  }
});

test('REQ-NFR-018 REQ-FN-310 REQ-UI-126 REQ-UI-125 REQ-FN-309 with an empty folder open, New project shows the paths as the names are typed, makes the solution its own folder inside it and the project one inside that, and FILES shows the solution as its root', async ({ page }) => {
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

  // The paths follow the names as they are typed.
  const vSolution = `Todo${STAMP}`;
  const vSolutionFolder = join(vFolder, vSolution);
  await page.getByTestId('new-project-solution').fill(vSolution);
  await expect(page.getByTestId('new-project-solution-folder')).toHaveText(vSolutionFolder);
  await expect(page.getByTestId('new-project-solution-path')).toHaveText(join(vSolutionFolder, `${vSolution}.slnx`));
  await expect(page.getByTestId('new-project-project-path')).toHaveText(join(vSolutionFolder, vSolution));
  const vProject = `TodoApp${STAMP}`;
  await page.getByTestId('new-project-name').fill(vProject);
  await expect(page.getByTestId('new-project-project-path')).toHaveText(join(vSolutionFolder, vProject));
  await expect(page.getByTestId('new-project-solution-path')).toHaveText(join(vSolutionFolder, `${vSolution}.slnx`));
  await page.screenshot({ path: `${SHOTS}/REQ-UI-126-empty-folder-dialog.png` });
  await create(page);

  await expect(page.getByTestId('project-switch-name')).toHaveText(vSolution, { timeout: 30_000 });
  expect(readdirSync(vFolder), 'nothing lands directly in the folder that was open').toEqual([vSolution]);
  expect(disk(vFolder), 'the files in the folder that was open').toEqual([`${vSolution}/${vProject}/Program.cs`, `${vSolution}/${vProject}/${vProject}.csproj`, `${vSolution}/${vSolution}.slnx`].sort());
  await runHasTarget(page, vProject);
  // FILES: the solution is the root and its project is below it, not beside it (REQ-UI-125).
  await expect.poll(() => filesTree(page), { timeout: 30_000 }).toEqual({ roots: [`${vSolution}.slnx`], children: [vProject] });
  await expect(page.getByTestId(`tree-${vSolution}.slnx`)).toHaveAttribute('data-kind', 'solution');
  await expect(page.getByTestId(`tree-${vProject}`)).toHaveAttribute('data-kind', 'project');
  await page.screenshot({ path: `${SHOTS}/REQ-UI-125-files-root.png` });

  // With the solution now open, File > New project is the same dialog, adding to it (REQ-UI-112): the folder shown is
  // the solution's folder, the solution file is the one already there, and the project goes one folder under it.
  await newProject(page);
  const vDialog = page.getByTestId('new-project-dialog');
  await expect(vDialog).toContainText(`New project in ${vSolution}`);
  await expect(page.getByTestId('template-list')).toBeVisible({ timeout: 120_000 });
  await expect(page.getByTestId('new-project-solution')).toHaveCount(0);
  await expect(page.getByTestId('new-project-folder')).toHaveText(vSolutionFolder);
  await expect(page.getByTestId('new-project-paths')).toContainText('already there');
  await expect(page.getByTestId('new-project-solution-path')).toHaveText(join(vSolutionFolder, `${vSolution}.slnx`));
  await page.getByTestId('new-project-name').fill('Second');
  await expect(page.getByTestId('new-project-project-path')).toHaveText(join(vSolutionFolder, 'Second'));
  // "Change location" turns the folder into a field that starts on the same folder.
  await page.getByTestId('new-project-change-location').click();
  await expect(page.getByTestId('new-project-location')).toHaveValue(vSolutionFolder);
  await page.getByTestId('new-project-cancel').click();
  expect(disk(vFolder), 'Cancel made nothing').toEqual([`${vSolution}/${vProject}/Program.cs`, `${vSolution}/${vProject}/${vProject}.csproj`, `${vSolution}/${vSolution}.slnx`].sort());
});

test('REQ-NFR-018 REQ-FN-310 REQ-UI-125 REQ-FN-309 the owner\'s case of 25 Nov: a solution and a project of the same name, in a folder that holds only a docs folder, each get a folder of their own, one inside the other, and the new project is the one that runs', async ({ page }) => {
  await signedIn(page);
  const vName = `OwnerCase${STAMP}`;
  const vFolder = join(objScratch, vName);
  mkdirSync(join(vFolder, 'docs', 'metrics'), { recursive: true });
  writeFileSync(join(vFolder, 'docs', 'metrics', 'sessions.jsonl'), '{"v":1,"harness":"chatur","kind":"session"}\n');
  await openByDialog(page, 'open-folder', vFolder, vName);

  await newProject(page);
  await pickTemplate(page, 'console');
  const vSolution = `Checking-Chatur${STAMP}`;
  const vSolutionFolder = join(vFolder, vSolution);
  await page.getByTestId('new-project-solution').fill(vSolution);
  await expect(page.getByTestId('new-project-folder')).toHaveText(vFolder);
  await expect(page.getByTestId('new-project-solution-folder')).toHaveText(vSolutionFolder);
  await expect(page.getByTestId('new-project-solution-path')).toHaveText(join(vSolutionFolder, `${vSolution}.slnx`));
  await expect(page.getByTestId('new-project-project-path')).toHaveText(join(vSolutionFolder, vSolution));
  await create(page);

  await expect(page.getByTestId('project-switch-name')).toHaveText(vSolution, { timeout: 30_000 });
  expect(disk(vFolder), 'the files in the folder that was open').toEqual([
    `${vSolution}/${vSolution}.slnx`, `${vSolution}/${vSolution}/${vSolution}.csproj`, `${vSolution}/${vSolution}/Program.cs`, 'docs/metrics/sessions.jsonl',
  ].sort());
  await expect.poll(() => filesTree(page), { timeout: 30_000 }).toEqual({ roots: [`${vSolution}.slnx`], children: [vSolution] });
  await runHasTarget(page, vSolution);

  // A second console project is added from File > New project: it becomes the project that runs (REQ-FN-309, owner's step 3).
  await newProject(page);
  await pickTemplate(page, 'console');
  await page.getByTestId('new-project-name').fill('TryConsole');
  await create(page);
  await runHasTarget(page, 'TryConsole');
  await expect(page.getByTestId(`tree-TryConsole-run`)).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId(`tree-${vSolution}-run`)).toHaveCount(0);
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
  // The solution is the root of FILES with both projects below it, though the folder was opened before the solution existed (REQ-UI-125).
  await expect.poll(() => filesTree(page), { timeout: 30_000 }).toEqual({ roots: [`${vSolution}.slnx`], children: ['First', 'Second'] });
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
  await expect.poll(() => filesTree(page), { timeout: 30_000 }).toEqual({ roots: [vSln], children: ['Added', 'First'] });
});
