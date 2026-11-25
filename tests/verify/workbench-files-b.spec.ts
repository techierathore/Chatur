// Acceptance tests for the Workbench files / branch / menu rows made on 18 Nov (REQ-UI-096, REQ-UI-101,
// REQ-FN-304, REQ-UI-102, REQ-UI-103, REQ-FN-305). Black-box, web harness: each test makes its own fixture,
// opens it through Start's own dialog and looks only at the screen (and at the fixture on disk).
import { Page, expect } from '@playwright/test';
import { existsSync } from 'fs';
import { join } from 'path';
import { signIn } from './support/auth';
import { test, NAME, openProject, resetFixture } from './support/workbench-a';
import {
  makeUnreadableFixture, makeBranchFixture, makeHiddenFixture, makeSolutionFixture, git, put, openByDialog, openMenu, pick,
  setOpen, branchName, openBranchList, SLN_NAME,
} from './support/workbench-b';

let page: Page;

test.beforeAll(async ({ browser }) => {
  test.setTimeout(120_000);
  const vContext = await browser.newContext({ baseURL: process.env.BASE_URL });
  page = await vContext.newPage();
  await signIn(page, 'Owner');
});
test.afterAll(async () => { await page.close(); });
test.beforeEach(() => { test.setTimeout(150_000); });

test('REQ-UI-096 a folder Windows will not list shows as not readable and every other entry still shows', async () => {
  const vFix = makeUnreadableFixture();
  try {
    await openByDialog(page, 'open-folder', vFix.root, vFix.root.split('/').pop()!);
    const vTree = page.getByTestId('file-tree');
    await expect(page.getByTestId('tree-Locked')).toBeVisible({ timeout: 20_000 });
    await expect(page.getByTestId('tree-Locked')).toHaveAttribute('data-readable', 'false');
    await expect(page.getByTestId('tree-Locked-unreadable')).toHaveText(/not readable/i);
    // every other entry still shows, and none of them is marked
    for (const vRel of ['Readable', 'Zeta', 'top.txt']) {
      await expect(page.getByTestId(`tree-${vRel}`)).toBeVisible();
      await expect(page.getByTestId(`tree-${vRel}`)).not.toHaveAttribute('data-readable', 'false');
      await expect(page.getByTestId(`tree-${vRel}-unreadable`)).toHaveCount(0);
    }
    await expect(vTree.getByText('not readable')).toHaveCount(1);
    // a folder's contents appear only after it is opened
    await expect(page.getByTestId('tree-Readable/inner.txt')).toHaveCount(0);
    await expect(page.getByTestId('tree-Zeta/z.txt')).toHaveCount(0);
    await setOpen(page, 'Readable', true);
    await expect(page.getByTestId('tree-Readable/inner.txt')).toBeVisible({ timeout: 20_000 });
    await expect(page.getByTestId('tree-Zeta/z.txt')).toHaveCount(0); // a sibling stays unread
    // the locked folder does not open and nothing inside it is listed
    await page.getByTestId('tree-Locked').click({ force: true });
    await expect(page.getByTestId('tree-Locked/secret.txt')).toHaveCount(0);
    await expect(page.getByTestId('tree-Locked')).toHaveAttribute('data-readable', 'false');
    await expect(page.getByTestId('tree-top.txt')).toBeVisible(); // the app carries on
    await page.screenshot({ path: join(__dirname, '../.artifacts/verify/shots/REQ-UI-096-unreadable.png'), fullPage: true });
  } finally {
    vFix.cleanup();
  }
});

test('REQ-UI-101 the branch at the foot of Files lists local and remote branches and choosing one switches to it', async () => {
  const vFix = makeBranchFixture();
  try {
    await openByDialog(page, 'open-folder', vFix.work, 'work');
    // the toolbar has no branch picker; the branch is named once, under the files
    const vToolbar = page.getByTestId('toolbar');
    await expect(vToolbar).toBeVisible();
    await expect(vToolbar.locator('[data-testid=branch-pick]')).toHaveCount(0);
    await expect(vToolbar.locator('[data-testid=repo]')).toHaveCount(0);
    await expect(page.locator('[data-testid^=toolbar-branch-]')).toHaveCount(0);
    await expect(page.getByTestId('files-panel').getByTestId('branch')).toBeVisible();
    await expect(branchName(page)).toHaveText('main', { timeout: 20_000 });

    await openBranchList(page);
    const vPop = page.getByTestId('branch-pop');
    await expect(vPop.getByTestId('branch-local-main')).toBeVisible();
    await expect(vPop.getByTestId('branch-local-local-two')).toBeVisible();
    await expect(vPop.getByTestId('branch-remote-origin/remote-only')).toBeVisible();
    await expect(vPop.getByTestId('branch-remote-origin/main')).toBeVisible();
    // the current one is ticked, the others are not
    await expect(vPop.getByTestId('branch-local-main').locator('svg')).toHaveCount(1);
    await expect(vPop.getByTestId('branch-local-local-two').locator('svg')).toHaveCount(0);
    await page.screenshot({ path: join(__dirname, '../.artifacts/verify/shots/REQ-UI-101-branch-list.png'), fullPage: true });

    // choosing a local branch switches to it (the screen and the repository agree)
    await vPop.getByTestId('branch-local-local-two').click();
    await expect(branchName(page)).toHaveText('local-two', { timeout: 30_000 });
    expect(git(vFix.work, 'rev-parse', '--abbrev-ref', 'HEAD')).toBe('local-two');
    await openBranchList(page);
    await expect(page.getByTestId('branch-local-local-two').locator('svg')).toHaveCount(1);
    await expect(page.getByTestId('branch-local-main').locator('svg')).toHaveCount(0);

    // choosing a remote branch switches to it as well
    await page.getByTestId('branch-remote-origin/remote-only').click();
    await expect(branchName(page)).toHaveText(/remote-only/, { timeout: 30_000 });
    expect(git(vFix.work, 'rev-parse', '--abbrev-ref', 'HEAD')).toMatch(/remote-only/);
  } finally {
    vFix.cleanup();
  }
});

test('REQ-FN-304 picking another branch with uncommitted changes first offers to bring them along, put them aside or stay', async () => {
  const vFix = makeBranchFixture();
  try {
    await openByDialog(page, 'open-folder', vFix.work, 'work');
    await expect(branchName(page)).toHaveText('main', { timeout: 20_000 });
    // uncommitted: a changed tracked file and a new untracked one
    put(join(vFix.work, 'tracked.txt'), 'changed after the check-in\n');
    put(join(vFix.work, 'brand-new.txt'), 'untracked\n');

    // Stay: nothing changes
    await openBranchList(page);
    await page.getByTestId('branch-local-local-two').click();
    const vDialog = page.getByTestId('branch-dirty');
    await expect(vDialog).toBeVisible({ timeout: 20_000 });
    await expect(vDialog.getByTestId('branch-dirty-bring')).toBeVisible();
    await expect(vDialog.getByTestId('branch-dirty-aside')).toBeVisible();
    await expect(vDialog.getByTestId('branch-dirty-stay')).toBeVisible();
    expect(git(vFix.work, 'rev-parse', '--abbrev-ref', 'HEAD')).toBe('main'); // asked first: not switched yet
    await page.screenshot({ path: join(__dirname, '../.artifacts/verify/shots/REQ-FN-304-dirty-dialog.png'), fullPage: true });
    await vDialog.getByTestId('branch-dirty-stay').click();
    await expect(vDialog).toBeHidden();
    await expect(branchName(page)).toHaveText('main');
    expect(git(vFix.work, 'rev-parse', '--abbrev-ref', 'HEAD')).toBe('main');
    expect(git(vFix.work, 'status', '--porcelain')).toContain('brand-new.txt');
    expect(git(vFix.work, 'stash', 'list')).toBe('');

    // Put them aside: the branch switches clean and the stash holds the changes
    await openBranchList(page);
    await page.getByTestId('branch-local-local-two').click();
    await expect(vDialog).toBeVisible({ timeout: 20_000 });
    await vDialog.getByTestId('branch-dirty-aside').click();
    await expect(vDialog).toBeHidden({ timeout: 30_000 });
    await expect(branchName(page)).toHaveText('local-two', { timeout: 30_000 });
    expect(git(vFix.work, 'rev-parse', '--abbrev-ref', 'HEAD')).toBe('local-two');
    expect(git(vFix.work, 'stash', 'list')).toMatch(/stash@\{0\}/);
    expect(git(vFix.work, 'status', '--porcelain')).toBe('');
    expect(existsSync(join(vFix.work, 'brand-new.txt'))).toBe(false); // put aside with the untracked file (-u)
  } finally {
    vFix.cleanup();
  }
});

test('REQ-UI-102 the agent the closed picker names carries the tick in the list, and no entry says suggested', async () => {
  resetFixture();
  await openProject(page, NAME);
  const vButton = page.getByTestId('agent-pick').locator('button');
  await expect(vButton).toBeEnabled({ timeout: 30_000 });
  const vPop = page.getByTestId('agent-pop');
  const openPick = async () => {
    await expect(async () => {
      if (!(await vPop.isVisible())) await vButton.click();
      await expect(vPop).toBeVisible({ timeout: 2_500 });
    }).toPass({ timeout: 30_000 });
  };
  const check = async (aNamed: string) => {
    await openPick();
    const vChecked = vPop.locator('[role=menuitem][aria-checked=true]');
    await expect(vChecked).toHaveCount(1);
    await expect(vPop.locator('[role=menuitem][aria-checked=false]').first()).toBeVisible();
    await expect(vChecked.getByTestId('agent-tick')).toHaveCount(1);
    await expect(vPop.getByTestId('agent-tick')).toHaveCount(1); // the one tick, on that row
    await expect(vChecked).toContainText(aNamed === 'No agent' ? 'No agent' : aNamed);
    await expect(vPop).not.toContainText(/suggest/i);
  };
  // the agent Chatur chose when the project opened
  const vFirst = (await vButton.innerText()).trim();
  expect(vFirst).not.toBe('');
  await check(vFirst);
  await page.screenshot({ path: join(__dirname, '../.artifacts/verify/shots/REQ-UI-102-agent-pop.png'), fullPage: true });
  // choose a different agent: the closed picker follows and the tick moves with it
  const vOthers = vPop.locator('[role=menuitem][aria-checked=false][data-testid^="agent-"]:not([data-testid=agent-none]):not([data-testid=agent-settings-link])');
  await vOthers.first().click();
  await expect(vPop).toBeHidden();
  const vSecond = (await vButton.innerText()).trim();
  expect(vSecond).not.toBe(vFirst);
  await check(vSecond);
  // and No agent carries the tick when it is chosen
  await vPop.getByTestId('agent-none').click();
  await expect(vPop).toBeHidden();
  await expect(vButton).toContainText('No agent');
  await openPick();
  await expect(vPop.locator('[role=menuitem][aria-checked=true]')).toHaveCount(1);
  await expect(vPop.getByTestId('agent-none')).toHaveAttribute('aria-checked', 'true');
  await expect(vPop.getByTestId('agent-none').getByTestId('agent-tick')).toHaveCount(1);
  await expect(vPop).not.toContainText(/suggest/i);
  await page.keyboard.press('Escape');
});

test('REQ-UI-103 dot-named files and .vs stay hidden until View > Show hidden files, which the account keeps', async ({ browser }) => {
  const vFix = makeHiddenFixture();
  const vName = vFix.root.split('/').pop()!;
  const setHidden = async (aOn: boolean) => {
    await openMenu(page, 'view');
    const vItem = page.getByTestId('menu-view-hidden');
    await expect(vItem).toContainText('Show hidden files');
    if ((await vItem.getAttribute('aria-checked')) !== String(aOn)) await vItem.click();
    else await page.keyboard.press('Escape');
    await openMenu(page, 'view');
    await expect(vItem).toHaveAttribute('aria-checked', String(aOn));
    await page.keyboard.press('Escape');
  };
  try {
    await openByDialog(page, 'open-folder', vFix.root, vName);
    await setHidden(false); // an earlier run may have left it on
    await expect(page.getByTestId('tree-visible.txt')).toBeVisible({ timeout: 20_000 });
    await expect(page.getByTestId('tree-Docs')).toBeVisible();
    for (const vHidden of ['.env', '.dotdir', '.vs', '.git']) await expect(page.getByTestId(`tree-${vHidden}`)).toHaveCount(0);

    await setHidden(true);
    for (const vShown of ['.env', '.dotdir', '.vs']) await expect(page.getByTestId(`tree-${vShown}`)).toBeVisible({ timeout: 20_000 });
    await expect(page.getByTestId('tree-visible.txt')).toBeVisible();
    await setOpen(page, '.vs', true);
    await expect(page.getByTestId('tree-.vs/state.txt')).toBeVisible({ timeout: 20_000 });
    await page.screenshot({ path: join(__dirname, '../.artifacts/verify/shots/REQ-UI-103-hidden-shown.png'), fullPage: true });

    // the account keeps it: still ticked after a reload, and in a fresh browser signed in as the same account
    await page.reload({ waitUntil: 'networkidle' });
    await expect(page.getByTestId('tree-.env')).toBeVisible({ timeout: 30_000 });
    await openMenu(page, 'view');
    await expect(page.getByTestId('menu-view-hidden')).toHaveAttribute('aria-checked', 'true');
    await page.keyboard.press('Escape');
    const vContext = await browser.newContext({ baseURL: process.env.BASE_URL });
    try {
      const vOther = await vContext.newPage();
      await signIn(vOther, 'Owner');
      await vOther.goto('/', { waitUntil: 'networkidle' });
      await expect(vOther.getByTestId('project-switch-name')).toBeVisible({ timeout: 30_000 });
      await openMenu(vOther, 'view');
      await expect(vOther.getByTestId('menu-view-hidden')).toHaveAttribute('aria-checked', 'true');
    } finally {
      await vContext.close();
    }
  } finally {
    await setHidden(false);
    await expect(page.getByTestId('tree-.env')).toHaveCount(0, { timeout: 20_000 });
    await expect(page.getByTestId('tree-.vs')).toHaveCount(0);
    vFix.cleanup();
  }
});

test('REQ-FN-305 File says Close solution with a solution open and Close folder with a folder open, never Close project', async () => {
  const vSln = makeSolutionFixture();
  const vFolder = makeHiddenFixture();
  try {
    await openByDialog(page, 'open-solution', vSln.sln, SLN_NAME);
    await openMenu(page, 'file');
    await expect(page.getByTestId('menu-file-close')).toHaveText('Close solution');
    await expect(page.getByTestId('menu-file-items')).not.toContainText('Close project');
    await expect(page.getByTestId('menu-file-items')).not.toContainText('Close folder');
    await page.screenshot({ path: join(__dirname, '../.artifacts/verify/shots/REQ-FN-305-close-solution.png'), fullPage: true });
    await page.keyboard.press('Escape');

    await openByDialog(page, 'open-folder', vFolder.root, vFolder.root.split('/').pop()!);
    await openMenu(page, 'file');
    await expect(page.getByTestId('menu-file-close')).toHaveText('Close folder');
    await expect(page.getByTestId('menu-file-items')).not.toContainText('Close project');
    await expect(page.getByTestId('menu-file-items')).not.toContainText('Close solution');
    await page.keyboard.press('Escape');

    // the item closes what is open
    await pick(page, 'file', 'close');
    await expect(page).toHaveURL(/\/start/, { timeout: 20_000 });
  } finally {
    vSln.cleanup();
    vFolder.cleanup();
  }
});
