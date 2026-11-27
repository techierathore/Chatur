// Acceptance tests for the owner's test of 25 Nov 2026 (REQ-UI-125, REQ-FN-324, REQ-FN-325, REQ-UI-041, REQ-UI-128,
// REQ-FN-045, REQ-FN-046): the solution is the root of FILES with its projects below it; several projects start together
// from a choice kept for the solution; a folder with a project and no solution is said so before it opens; closing the
// whole file view asks about unsaved files like closing one; the chat moves to the newest message; and Chatur is the
// editor, so no button hands a file to another program. Black-box, web harness, signed in as the 'Owner' test user.
// Every fixture is made fresh under tests/.artifacts and deleted with its projects when the test ends.
import { Page, expect } from '@playwright/test';
import { execFileSync } from 'child_process';
import { join, resolve } from 'path';
import { mkdirSync } from 'fs';
import { signIn } from './support/auth';
import { removeProjectsUnder } from './support/cleanup';
import { test, outputText, pill, stopIfRunning, tabs, editorText, openFile, openProject, NAME, resetFixture } from './support/workbench-a';
import {
  makeSolutionFixture, openByDialog, openTargets, freshDir, put, SLN_NAME, APP_ONE, APP_TWO, LIB, TESTS, LINE_ONE, LINE_TWO,
} from './support/workbench-b';

let page: Page;
const SHOTS = resolve(__dirname, '../.artifacts/verify/shots');

test.describe.configure({ mode: 'serial' });

test.beforeAll(async ({ browser }) => {
  test.setTimeout(120_000);
  mkdirSync(SHOTS, { recursive: true });
  const vContext = await browser.newContext({ baseURL: process.env.BASE_URL });
  page = await vContext.newPage();
  await signIn(page, 'Owner');
});
test.afterAll(async () => { await page.close(); });
test.beforeEach(() => { test.setTimeout(240_000); });
test.afterEach(async () => { await stopIfRunning(page); });

const targetButton = () => page.getByTestId('target').locator('button');

/** What FILES shows: the root rows that stand for a solution, the project rows, and every name directly under the root. */
async function filesTree(aPage: Page): Promise<{ roots: string[]; projects: string[]; all: string[] }> {
  const vRows = await aPage.getByTestId('file-tree').locator('[data-testid^="tree-"]').evaluateAll((aEls) => aEls.map((e) => ({ id: e.getAttribute('data-testid') ?? '', kind: e.getAttribute('data-kind') ?? '' })));
  const vOwn = vRows.map((r) => ({ name: r.id.slice('tree-'.length), kind: r.kind })).filter((r) => r.name.length > 0 && !r.name.includes('/') && !/-(menu|run|unreadable)$/.test(r.name));
  return {
    roots: vOwn.filter((r) => r.kind === 'solution').map((r) => r.name).sort(),
    projects: vOwn.filter((r) => r.kind === 'project').map((r) => r.name).sort(),
    all: vOwn.filter((r) => r.kind !== 'solution').map((r) => r.name).sort(),
  };
}

test('REQ-UI-125 the solution is the root of FILES, its projects are below it, and the solution file is not beside them', async () => {
  const vFix = makeSolutionFixture();
  try {
    await openByDialog(page, 'open-solution', vFix.sln, SLN_NAME);
    await expect.poll(async () => { const t = await filesTree(page); return { roots: t.roots, projects: t.projects }; }, { timeout: 30_000 }).toEqual({
      roots: [`${SLN_NAME}.sln`],
      projects: [APP_ONE, APP_TWO, LIB, TESTS].sort(),
    });
    // Projects come first, then the other entries; the solution file itself is not listed beside them.
    const vAll = (await filesTree(page)).all;
    expect(vAll.some((n) => /\.slnx?$/.test(n)), 'the solution file is not a row beside the projects').toBe(false);
    await expect(page.getByTestId(`tree-${SLN_NAME}.sln`)).toHaveAttribute('data-kind', 'solution');
    await expect(page.getByTestId(`tree-${SLN_NAME}.sln`)).toHaveAttribute('data-state', 'open');
    // The tree has one row at its top, the solution; everything else sits inside it.
    expect(await page.getByTestId('file-tree').locator('[role=tree] > [role=treeitem], [role=tree] > li').count()).toBeLessThanOrEqual(1);
    await page.screenshot({ path: join(SHOTS, 'REQ-UI-125-solution-root.png'), fullPage: true });
  } finally {
    vFix.cleanup();
  }
});

test('REQ-UI-125 a folder opened before its solution was made shows the solution as the root once it exists', async () => {
  const vDir = freshDir('later');
  try {
    put(join(vDir, 'Directory.Build.props'), '<Project />\n');
    put(join(vDir, 'nuget.config'), '<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear /></packageSources></configuration>\n');
    execFileSync(`${process.env.HOME}/.dotnet/dotnet`, ['new', 'console', '-n', 'Early', '-o', join(vDir, 'Early')], { cwd: vDir, stdio: 'pipe', timeout: 180_000 });
    await openByDialog(page, 'open-folder', vDir, vDir.split('/').pop()!);
    // No solution yet: the open asked, and the helper went on (REQ-FN-325); FILES is flat.
    await expect.poll(async () => { const t = await filesTree(page); return { roots: t.roots, early: t.all.includes('Early') }; }, { timeout: 30_000 }).toEqual({ roots: [], early: true });

    execFileSync(`${process.env.HOME}/.dotnet/dotnet`, ['new', 'sln', '--name', 'LaterSln', '-o', vDir], { cwd: vDir, stdio: 'pipe', timeout: 180_000 });
    execFileSync(`${process.env.HOME}/.dotnet/dotnet`, ['sln', join(vDir, 'LaterSln.slnx'), 'add', join(vDir, 'Early', 'Early.csproj')], { cwd: vDir, stdio: 'pipe', timeout: 180_000 });
    await page.getByTestId('files-refresh').click();
    await expect.poll(async () => { const t = await filesTree(page); return { roots: t.roots, projects: t.projects }; }, { timeout: 30_000 }).toEqual({ roots: ['LaterSln.slnx'], projects: ['Early'] });
  } finally {
    removeProjectsUnder(vDir);
  }
});

test('REQ-FN-324 several startup projects are chosen once, kept for the solution, and Run builds and starts them together with each line named', async () => {
  const vFix = makeSolutionFixture();
  try {
    await openByDialog(page, 'open-solution', vFix.sln, SLN_NAME);
    await openTargets(page);
    await expect(page.getByTestId('target-multiple')).toBeVisible();
    await page.getByTestId('target-multiple').click();
    const vDialog = page.getByTestId('startup-dialog');
    await expect(vDialog).toBeVisible({ timeout: 15_000 });
    // Projects that can run are listed: the two programs; never the library or the test project.
    await expect(vDialog.locator('[data-testid^="startup-row-"]')).toHaveCount(2);
    await expect(vDialog).toContainText(APP_ONE);
    await expect(vDialog).toContainText(APP_TWO);
    await expect(vDialog).not.toContainText(LIB);
    await expect(vDialog).not.toContainText(TESTS);
    await page.getByTestId('startup-hint').waitFor();
    for (const vRow of await vDialog.locator('[data-testid^="startup-tick-"]').all()) await vRow.click();
    await expect(page.getByTestId('startup-hint')).toContainText('2 ticked');
    await page.screenshot({ path: join(SHOTS, 'REQ-FN-324-startup-dialog.png') });
    await page.getByTestId('startup-save').click();
    await expect(vDialog).toBeHidden({ timeout: 15_000 });
    await expect(targetButton()).toContainText('2 startup projects');
    // FILES marks every startup project as running.
    await expect(page.getByTestId(`tree-${APP_ONE}-run`)).toBeVisible();
    await expect(page.getByTestId(`tree-${APP_TWO}-run`)).toBeVisible();

    await page.getByTestId('run').click();
    const vOutput = outputText(page);
    await expect(vOutput).toContainText(`[${APP_ONE}] ${LINE_ONE}`, { timeout: 240_000 });
    await expect(vOutput).toContainText(`[${APP_TWO}] ${LINE_TWO}`, { timeout: 60_000 });
    await expect(vOutput).toContainText('All 2 projects ended.', { timeout: 60_000 });
    await expect(pill(page)).toContainText('ended');
    await page.screenshot({ path: join(SHOTS, 'REQ-FN-324-run-together.png'), fullPage: true });

    // The choice is kept for the solution: after reopening it, it is still there.
    await page.goto('/', { waitUntil: 'domcontentloaded' });
    await expect(targetButton()).toContainText('2 startup projects', { timeout: 30_000 });

    // Choosing one project to run ends the choice.
    await openTargets(page);
    await page.getByTestId('target-pop').getByText(APP_TWO, { exact: true }).first().click();
    await expect(targetButton()).toContainText(APP_TWO);
    await page.getByTestId('run').click();
    await expect(outputText(page)).toContainText(LINE_TWO, { timeout: 240_000 });
    await expect(outputText(page)).toContainText('Process exited with code 0.', { timeout: 60_000 });
    await expect(outputText(page)).not.toContainText(`[${APP_TWO}]`);
  } finally {
    await stopIfRunning(page);
    vFix.cleanup();
  }
});

test('REQ-FN-325 opening a folder that has a project and no solution says so first and asks; Go back opens nothing, Open as it is opens it', async () => {
  const vDir = freshDir('nosln');
  try {
    put(join(vDir, 'Directory.Build.props'), '<Project />\n');
    execFileSync(`${process.env.HOME}/.dotnet/dotnet`, ['new', 'console', '-n', 'Loose', '-o', join(vDir, 'Loose')], { cwd: vDir, stdio: 'pipe', timeout: 180_000 });
    await page.goto('/start?all=1', { waitUntil: 'domcontentloaded' });
    await expect(page.getByTestId('manage-folders')).toBeVisible({ timeout: 30_000 });
    const vDialog = page.getByTestId('open-project-dialog');
    await expect(async () => {
      if (!(await vDialog.isVisible())) await page.getByTestId('open-folder').click();
      await expect(vDialog).toBeVisible({ timeout: 2_500 });
    }).toPass({ timeout: 30_000 });
    await page.getByTestId('open-project-path').fill(vDir);
    await page.getByTestId('open-project-go').click();

    const vAsk = page.getByTestId('open-project-no-solution');
    await expect(vAsk).toBeVisible({ timeout: 30_000 });
    await expect(vAsk).toContainText('This folder has no solution');
    await expect(vAsk).toContainText('Loose');
    await expect(page).toHaveURL(/\/start/);
    await page.screenshot({ path: join(SHOTS, 'REQ-FN-325-no-solution.png') });

    await page.getByTestId('open-project-no-solution-back').click();
    await expect(vAsk).toHaveCount(0);
    await expect(page.getByTestId('open-project-path')).toHaveValue(vDir);
    await expect(page).toHaveURL(/\/start/);

    await page.getByTestId('open-project-go').click();
    await expect(vAsk).toBeVisible({ timeout: 30_000 });
    await page.getByTestId('open-project-no-solution-go').click();
    await expect(page.getByTestId('project-switch-name')).toHaveText(vDir.split('/').pop()!, { timeout: 30_000 });
  } finally {
    removeProjectsUnder(vDir);
  }
});

test('REQ-UI-041 closing the whole file view with an unsaved file asks as closing one file does, and Keep editing leaves the view and the text as they were', async () => {
  resetFixture();
  await openProject(page, NAME);
  await openFile(page, 'Program.cs');
  await openFile(page, 'Notes/notes.txt');
  await editorText(page).fill('typed but never saved in the view test\n');
  await expect(tabs(page).locator('[data-dirty="true"]').first()).toBeAttached({ timeout: 20_000 });

  await page.getByTestId('close-file-view').click();
  const vDialog = page.getByTestId('close-unsaved-dialog');
  await expect(vDialog).toBeVisible({ timeout: 20_000 });
  await expect(vDialog).toContainText('Close without saving?');
  await page.screenshot({ path: join(SHOTS, 'REQ-UI-041-close-view.png') });
  await page.getByTestId('close-unsaved-cancel').click();
  await expect(vDialog).toBeHidden();
  await expect(page.getByTestId('files-area')).toBeVisible();
  await expect(editorText(page)).toHaveValue('typed but never saved in the view test\n');

  // Saved, nothing is unsaved: the view closes at once.
  await page.getByTestId('save-file').click();
  await expect(tabs(page).locator('[data-dirty="true"]')).toHaveCount(0, { timeout: 20_000 });
  await page.getByTestId('close-file-view').click();
  await expect(page.getByTestId('files-area')).toHaveCount(0, { timeout: 20_000 });
  await expect(vDialog).toHaveCount(0);
  // The chat has the window; View > Files brings the file view back is unchanged. Closing with a change asks again, and discarding closes.
  await page.goto('/', { waitUntil: 'domcontentloaded' });
  await openFile(page, 'Notes/notes.txt');
  await editorText(page).fill('typed again\n');
  await expect(tabs(page).locator('[data-dirty="true"]').first()).toBeAttached({ timeout: 20_000 });
  await page.getByTestId('close-file-view').click();
  await expect(vDialog).toBeVisible({ timeout: 20_000 });
  await page.getByTestId('close-unsaved-discard').click();
  await expect(page.getByTestId('files-area')).toHaveCount(0, { timeout: 20_000 });
});

test('REQ-FN-045 REQ-FN-046 the file view has no button that hands a file to another program', async () => {
  resetFixture();
  await openProject(page, NAME);
  await openFile(page, 'Program.cs');
  await expect(page.getByTestId('save-file')).toBeVisible();
  await expect(page.getByTestId('open-in-editor')).toHaveCount(0);
  await expect(page.getByTestId('open-with-default')).toHaveCount(0);
  await page.screenshot({ path: join(SHOTS, 'REQ-FN-045-no-editor-buttons.png') });
});

test('REQ-UI-128 after a long reply the conversation is at its newest message', async () => {
  resetFixture();
  await openProject(page, NAME);
  const vInput = page.getByTestId('ask-input');
  await expect(vInput).toBeEnabled({ timeout: 30_000 });
  // The Analyst's own *help is a long list, answered by Chatur itself with no model: longer than the window.
  await page.getByTestId('agent-pick').click();
  await page.getByTestId('agent-analyst').click();
  await vInput.fill('*help');
  await page.getByTestId('send').click();
  await expect(page.getByTestId('msg-text').last()).toContainText('brainstorm', { timeout: 60_000 });
  await expect(page.getByTestId('stop-agent')).toHaveCount(0, { timeout: 60_000 });
  const vGap = () => page.getByTestId('conversation').evaluate((aRoot) => {
    const vEls = [aRoot, ...Array.from(aRoot.querySelectorAll('*'))] as HTMLElement[];
    const vViewport = vEls.find((e) => e.scrollHeight > e.clientHeight + 5 && getComputedStyle(e).overflowY !== 'visible');
    return vViewport ? vViewport.scrollHeight - vViewport.clientHeight - vViewport.scrollTop : -1;
  });
  // -1 would mean it never overflowed, which would make this test prove nothing.
  await expect.poll(vGap, { timeout: 20_000 }).toBeGreaterThanOrEqual(0);
  await expect.poll(vGap, { timeout: 20_000 }).toBeLessThanOrEqual(4);
  await vInput.fill('*yolo');
  await page.getByTestId('send').click();
  await expect(page.getByTestId('msg-text').last()).toContainText('YOLO', { timeout: 60_000 });
  await expect.poll(vGap, { timeout: 20_000 }).toBeLessThanOrEqual(4);
  await page.screenshot({ path: join(SHOTS, 'REQ-UI-128-newest.png') });
});
