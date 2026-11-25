// Acceptance tests: every agent reply names the model that answered, and on Automatic says why (REQ-UI-111, BRD-242).
// Black-box, web harness. The model is the local stand-in of support/stub-model.ts and it is the only connected
// provider while the test runs, so Automatic has one model to choose from. Automatic routing is switched on for the
// test and put back as it was; the scratch project goes when the test ends.
import { test, expect, Page } from '@playwright/test';
import { join } from 'path';
import { signIn } from './support/auth';
import { removeProjectsUnder, scratchDir } from './support/cleanup';
import { put, openByDialog } from './support/workbench-b';
import { startStubModel, ensureStubProvider, chooseStubModel, removeStubProvider, STUB_MODEL } from './support/stub-model';

test.describe.configure({ timeout: 240_000, mode: 'serial' });

let objRoot = '';
let objName = '';

test.beforeAll(() => {
  objRoot = scratchDir(`reply-model-${Date.now()}`);
  objName = objRoot.split('/').pop()!;
  put(join(objRoot, 'notes.txt'), 'hello = 1\n');
});

test.afterAll(() => {
  if (objRoot) removeProjectsUnder(objRoot);
});

async function setAutoRouting(aPage: Page, aOn: boolean): Promise<boolean> {
  await aPage.goto('/settings/routing', { waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('auto-routing-panel')).toBeVisible({ timeout: 30_000 });
  const vWasOn = (await aPage.getByTestId('auto-routing-state').innerText()) === 'on';
  if (vWasOn !== aOn) await aPage.getByTestId('auto-routing-switch').click();
  await expect(aPage.getByTestId('auto-routing-state')).toHaveText(aOn ? 'on' : 'off');
  return vWasOn;
}

async function newConversation(aPage: Page): Promise<void> {
  await openByDialog(aPage, 'open-folder', objRoot, objName);
  await expect(aPage.getByTestId('chat-panel')).toBeVisible({ timeout: 30_000 });
  await aPage.getByTestId('new-chat').click();
  await expect(aPage.getByTestId('ask-input')).toBeEnabled({ timeout: 15_000 });
}

async function ask(aPage: Page, aText: string): Promise<void> {
  const vInput = aPage.getByTestId('ask-input');
  await expect(vInput).toBeEnabled({ timeout: 15_000 });
  await vInput.fill(aText);
  await aPage.getByTestId('send').click();
}

test('REQ-UI-111 a reply names its model, and on Automatic says why, and both are still there after a reload', async ({ page }) => {
  const vStub = await startStubModel('read-file', { path: 'notes.txt' });
  let vWasOn = false;
  try {
    await signIn(page, 'Owner');
    await ensureStubProvider(page, vStub);
    vWasOn = await setAutoRouting(page, true);
    await newConversation(page);

    // Automatic is the composer's default: no model has been picked
    await expect(page.getByTestId('model-pick')).toContainText('Automatic');
    await ask(page, 'Say hello.');
    await expect(page.getByTestId('msg-model').last()).toHaveText(STUB_MODEL, { timeout: 60_000 });
    const vWhy = page.getByTestId('msg-model-why').last();
    await expect(vWhy).toBeVisible();
    await expect(vWhy).toContainText('Automatic:');

    // persisted with the message: still shown when the project is opened again
    await page.reload({ waitUntil: 'networkidle' });
    await expect(page.getByTestId('chat-panel')).toBeVisible({ timeout: 30_000 });
    await expect(page.getByTestId('msg-model').last()).toHaveText(STUB_MODEL, { timeout: 30_000 });
    await expect(page.getByTestId('msg-model-why').last()).toContainText('Automatic:');
  } finally {
    await setAutoRouting(page, vWasOn);
    await removeStubProvider(page);
    await vStub.close();
  }
});

test('REQ-UI-111 a reply from a model the person picked names it and gives no routing reason', async ({ page }) => {
  const vStub = await startStubModel('read-file', { path: 'notes.txt' });
  try {
    await signIn(page, 'Owner');
    await ensureStubProvider(page, vStub);
    await newConversation(page);
    await chooseStubModel(page);
    await ask(page, 'Say hello.');

    await expect(page.getByTestId('msg-model').last()).toHaveText(STUB_MODEL, { timeout: 60_000 });
    await expect(page.getByTestId('msg-model-why')).toHaveCount(0);
  } finally {
    await removeStubProvider(page);
    await vStub.close();
  }
});
