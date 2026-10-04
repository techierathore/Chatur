// Verification of the Workbench project/files/editor/conversation-start rows (verify-phase step 4).
// Black-box: drives the running web harness only; one signed-in page is shared by the tests in this file.
import { Page, expect } from '@playwright/test';
import { readFileSync } from 'fs';
import { join } from 'path';
import { signIn } from './support/auth';
import {
  test, NAME, MULTI, PROJECT, NOTES, resetFixture, openProject, switchTo, openTargetMenu, openFile, setFolder,
  projectName, tree, tabs, editorText,
} from './support/workbench-a';

let page: Page;

test.beforeAll(async ({ browser }) => {
  page = await browser.newPage({ baseURL: process.env.BASE_URL });
  await signIn(page, 'Yolo');
});
test.afterAll(async () => { await page.close(); });
test.beforeEach(async () => {
  test.setTimeout(90_000);
  resetFixture();
});

test('REQ-FN-007 selected project named in the top bar with its branch, files listed on the left', async () => {
  await openProject(page);
  await expect(projectName(page)).toHaveText(NAME);
  const vTree = page.getByTestId('file-tree');
  await expect(vTree.getByText('Program.cs', { exact: true })).toBeVisible({ timeout: 20_000 });
  await expect(vTree.getByText(`${NAME}.csproj`, { exact: true })).toBeVisible();
  const vBranch = page.getByTestId('branch');
  await expect(vBranch).toBeVisible();
  await expect(vBranch).not.toHaveText(/^\s*—\s*$/, { timeout: 20_000 });
  await expect(vBranch).toHaveText(/\S/);
});

test('REQ-FN-008 a project opens straight onto a conversation with a role already chosen', async () => {
  await openProject(page);
  await expect(page.getByTestId('conversation-thread')).toBeVisible({ timeout: 20_000 });
  const vPick = page.getByTestId('agent-pick');
  await expect(vPick).toBeEnabled();
  await expect(vPick).not.toHaveText(/No agent/);
  await expect(vPick).toHaveText(/\S/);
  await expect(page.getByTestId('ask-input')).toBeEnabled();
});

test('REQ-UI-010 picking another project in the top bar makes the Workbench and other screens show it', async () => {
  await openProject(page);
  await openProject(page, MULTI); // make sure the second project is known, then come back to the first
  await openProject(page, NAME);
  await switchTo(page, MULTI);
  const vTree = page.getByTestId('file-tree');
  await expect(vTree.getByText('Multi.cs', { exact: true })).toBeVisible({ timeout: 20_000 });
  await expect(vTree.getByText('Program.cs', { exact: true })).toHaveCount(0);
  await page.goto('/prerequisites');
  await expect(page.getByText(MULTI).first()).toBeVisible({ timeout: 20_000 });
  await page.goto('/repository');
  await expect(page.getByText(MULTI).first()).toBeVisible({ timeout: 20_000 });
  await page.goto('/');
  await expect(projectName(page)).toHaveText(MULTI, { timeout: 20_000 });
});

test('REQ-UI-011 only targets this machine can build are choosable; one needing another machine is shown unavailable', async () => {
  await openProject(page, MULTI);
  await openTargetMenu(page);
  const vPop = page.getByTestId('target-pop');
  const vIos = vPop.getByRole('menuitem').filter({ hasText: 'iOS' });
  const vBuildable = vPop.getByRole('menuitem').filter({ hasText: 'net10.0 ·' });
  await expect(vIos.first()).toBeVisible();
  await expect(vIos.first()).toContainText('needs another machine');
  await expect(vIos.first()).toHaveAttribute('data-disabled', /.*/);
  await expect(vBuildable.first()).toBeVisible();
  await expect(vBuildable.first()).not.toHaveAttribute('data-disabled', /.*/);
  const vBefore = (await page.getByTestId('target').innerText()).trim();
  await vIos.first().click({ force: true });
  await page.keyboard.press('Escape');
  await expect(page.getByTestId('target')).toHaveText(vBefore);
  await expect(page.getByTestId('target')).not.toContainText('iOS');
});

test('REQ-UI-039 the files card shows the solution and folders, and a folder opens and closes', async () => {
  await openProject(page);
  const vTree = page.getByTestId('file-tree');
  await expect(vTree.getByText(`${NAME}.csproj`, { exact: true })).toBeVisible({ timeout: 20_000 });
  await expect(tree(page, 'Notes')).toBeVisible();
  await setFolder(page, 'Notes', false);
  await expect(tree(page, 'Notes/notes.txt')).toBeHidden();
  await setFolder(page, 'Notes', true);
  await expect(tree(page, 'Notes/notes.txt')).toBeVisible();
  await setFolder(page, 'Notes', false);
  await expect(tree(page, 'Notes/notes.txt')).toBeHidden();
});

test('REQ-UI-040 opening a file from the tree shows its text in a new tab named after the file', async () => {
  await openProject(page);
  await openFile(page, 'Program.cs');
  await expect(tabs(page).getByText('Program.cs', { exact: true })).toBeVisible({ timeout: 20_000 });
  await expect(editorText(page)).toHaveValue(/WBA starting/);
  await expect(editorText(page)).toHaveValue(/WBA tick/);
});

test('REQ-FN-044 changing the text and saving writes the new text to the file on disk', async () => {
  const vFile = join(PROJECT, 'Notes', 'notes.txt');
  await openProject(page);
  await openFile(page, 'Notes/notes.txt');
  await expect(editorText(page)).toHaveValue(NOTES);
  const vNew = 'beta replaced by the test\n';
  await editorText(page).fill(vNew);
  await expect(page.getByTestId('save-file')).toBeEnabled({ timeout: 20_000 });
  await page.getByTestId('save-file').click();
  await expect.poll(() => readFileSync(vFile, 'utf8'), { timeout: 20_000 }).toBe(vNew);
});

test('REQ-UI-041 an unsaved tab is marked, and closing it asks before the change is lost', async () => {
  const vFile = join(PROJECT, 'Notes', 'notes.txt');
  await openProject(page);
  await openFile(page, 'Notes/notes.txt');
  await expect(editorText(page)).toHaveValue(NOTES);
  await expect(tabs(page).locator('[data-dirty="true"]')).toHaveCount(0);
  await editorText(page).fill('typed but never saved\n');
  await expect(tabs(page).locator('[data-dirty="true"]').first()).toBeAttached({ timeout: 20_000 });
  const vClose = tabs(page).getByRole('button', { name: /^Close notes\.txt/ });
  await vClose.click();
  await expect(page.getByTestId('close-unsaved-dialog')).toBeVisible({ timeout: 20_000 });
  await page.getByTestId('close-unsaved-cancel').click();
  await expect(page.getByTestId('close-unsaved-dialog')).toBeHidden();
  await expect(tabs(page).getByText('notes.txt').first()).toBeVisible();
  await expect(editorText(page)).toHaveValue('typed but never saved\n');
  await vClose.click();
  await page.getByTestId('close-unsaved-discard').click();
  await expect(tabs(page).getByText('notes.txt')).toHaveCount(0);
  expect(readFileSync(vFile, 'utf8')).toBe(NOTES);
});

test('REQ-FN-024 opening a project shows the agent Chatur chose and says beside the box why', async () => {
  await openProject(page);
  const vPick = page.getByTestId('agent-pick');
  await expect(vPick).toBeEnabled({ timeout: 20_000 });
  await expect(vPick).not.toHaveText(/No agent/);
  await expect(vPick).toHaveText(/\S/);
  const vWhy = page.getByTestId('agent-why');
  await expect(vWhy).toBeVisible();
  await expect(vWhy).toHaveText(/\S{3,}/);
});
