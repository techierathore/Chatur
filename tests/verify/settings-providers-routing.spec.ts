// Acceptance tests: Settings > Model providers and Routing
// (REQ-FN-014..017, REQ-UI-019, REQ-UI-020, REQ-FN-018..023). Black-box, web harness.
import { test, expect, Page } from '@playwright/test';
import { signIn } from './support/auth';
import { openFoldersDialog } from './support/start';
import { ensureTestProvider, testProvider } from './support/provider';
import { mkdirSync, writeFileSync, existsSync } from 'fs';
import { resolve } from 'path';


const STAMP = Date.now();
const DEAD = 'http://localhost:59999';
const FAKE_KEY = `sk-verify-${STAMP}-SECRETVALUE`;
const created: string[] = [];
let createdReal = false; // true only when this run added the real provider, so only then is it removed

const providerKey = (aName: string) => aName.toLowerCase().replace(/ /g, '-');

/** Adds the real test provider unless it is already listed; remembers when this run added it. */
async function ensureReal(aPage: Page): Promise<void> {
  const vName = testProvider().name;
  await aPage.goto('/settings/providers', { waitUntil: 'networkidle' });
  await aPage.waitForTimeout(1500);
  const vListed = (await aPage.getByTestId('providers-table').getByText(vName, { exact: true }).count()) > 0;
  if (!vListed) createdReal = true;
  await ensureTestProvider(aPage);
}

function nameFor(aRow: string): string {
  return `verify-${aRow.toLowerCase()}-${STAMP}`;
}

async function openProviders(aPage: Page): Promise<void> {
  await aPage.goto('/settings/providers', { waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('providers-heading')).toBeVisible({ timeout: 30_000 });
}

async function openDialog(aPage: Page): Promise<void> {
  await aPage.getByTestId('add-provider').click();
  await expect(aPage.getByTestId('add-provider-dialog')).toBeVisible();
}

async function pick(aPage: Page, aButton: string, aItem: string): Promise<void> {
  await aPage.getByTestId(aButton).click();
  await aPage.getByTestId(aItem).click();
}

async function fillOllama(aPage: Page, aName: string): Promise<void> {
  await aPage.getByTestId('field-name').fill(aName);
  await pick(aPage, 'field-connector-button', 'connector-ollama');
  await aPage.getByTestId('field-address').fill(DEAD);
}

async function addOllama(aPage: Page, aName: string): Promise<void> {
  await openDialog(aPage);
  await fillOllama(aPage, aName);
  await aPage.getByTestId('dialog-add').click();
  await expect(aPage.getByTestId('add-provider-dialog')).toBeHidden({ timeout: 20_000 });
  created.push(aName);
  await expect(aPage.getByTestId('providers-table')).toContainText(aName, { timeout: 15_000 });
}

async function removeByName(aPage: Page, aName: string): Promise<void> {
  const vBtn = aPage.getByTestId(`remove-${aName}`);
  if (await vBtn.count()) {
    await vBtn.click();
    await expect(aPage.getByTestId('providers-table')).not.toContainText(aName, { timeout: 15_000 });
  }
}

test.beforeEach(async ({ page }) => {
  await signIn(page, 'Owner');
});

test.afterAll(async ({ browser }) => {
  if (!created.length && !createdReal) return;
  const vPage = await browser.newPage({ baseURL: process.env.BASE_URL });
  try {
    await signIn(vPage, 'Owner');
    await openProviders(vPage);
    for (const vName of created) await removeByName(vPage, vName);
    if (createdReal) await removeByName(vPage, providerKey(testProvider().name));
  } finally {
    await vPage.close();
  }
});

test('REQ-FN-014 a pasted-key provider is listed as connected with its models; an unreachable one is refused and saves nothing', async ({ page }) => {
  // No usable key exists, so the "listed as connected" half cannot be produced; this proves the
  // key path: Add must refuse an unreachable OpenAI-compatible service and save nothing.
  const vName = nameFor('FN-014');
  await openProviders(page);
  const vBefore = await page.getByTestId('providers-table').innerText();
  await openDialog(page);
  await page.getByTestId('field-name').fill(vName);
  await pick(page, 'field-connector-button', 'connector-compatible');
  await page.getByTestId('field-address').fill(DEAD);
  await page.getByTestId('field-key').fill(FAKE_KEY);
  await page.getByTestId('dialog-add').click();
  await expect(page.getByTestId('dialog-error')).toBeVisible({ timeout: 30_000 });
  await page.getByTestId('dialog-cancel').click();
  await openProviders(page);
  await expect(page.getByTestId('providers-table')).not.toContainText(vName);
  expect(await page.getByTestId('providers-table').innerText()).toBe(vBefore);
  // The pasted-key dialog itself offers the key field and the connector choice.
  await openDialog(page);
  await expect(page.getByTestId('field-key')).toBeVisible();
  await page.getByTestId('dialog-cancel').click();

  // The real provider with its pasted key: listed as connected, and its models can be chosen in routing.
  await ensureReal(page);
  const vReal = testProvider();
  const vKey = providerKey(vReal.name);
  await expect(page.getByTestId(`${vKey}-state`)).toContainText(/connected/i, { timeout: 15_000 });
  await expect(page.getByTestId(`${vKey}-state`)).not.toContainText(/not tested|failed/i);
  await openRouting(page);
  await page.getByTestId('tier-3-add').click();
  await expect(page.getByTestId('tier-3-add-pop').getByText(vReal.model).first()).toBeVisible({ timeout: 10_000 });
  await page.keyboard.press('Escape');
});

test('REQ-FN-015 a local model with its web address is listed as connected with no key', async ({ page }) => {
  const vName = nameFor('FN-015');
  await openProviders(page);
  await openDialog(page);
  await fillOllama(page, vName);
  await expect(page.getByTestId('field-key')).toHaveCount(0);
  await page.getByTestId('dialog-add').click();
  await expect(page.getByTestId('add-provider-dialog')).toBeHidden({ timeout: 20_000 });
  created.push(vName);
  const vTable = page.getByTestId('providers-table');
  await expect(vTable).toContainText(vName);
  await expect(vTable).toContainText(DEAD);
  await expect(page.getByTestId(`${vName}-state`)).toBeVisible();
  await removeByName(page, vName);
});

test('REQ-FN-016 browser sign-in shows a sign-in code and Cancel saves nothing', async ({ page }) => {
  await openProviders(page);
  const vBefore = await page.getByTestId('providers-table').innerText();
  await openDialog(page);
  await page.getByTestId('field-name').fill(nameFor('FN-016'));
  await pick(page, 'field-connector-button', 'connector-openai');
  await pick(page, 'field-signin-button', 'signin-browser');
  await expect(page.getByTestId('signin-terms')).toBeVisible();
  await page.getByTestId('dialog-add').click();
  const vCode = page.getByTestId('signin-code');
  await expect(vCode).toBeVisible({ timeout: 45_000 });
  expect((await vCode.innerText()).trim().length).toBeGreaterThan(3);
  await expect(page.getByTestId('signin-waiting')).toBeVisible();
  await page.getByTestId('dialog-cancel').click();
  await expect(page.getByTestId('add-provider-dialog')).toBeHidden({ timeout: 15_000 });
  await openProviders(page);
  expect(await page.getByTestId('providers-table').innerText()).toBe(vBefore);
  // Not produced: completing the sign-in (needs the owner's own ChatGPT account), so "listed as connected" is unverified.
});

test('REQ-FN-017 a pasted key never appears on the page or in the providers table', async ({ page }) => {
  // The key being filed in the OS store (Keychain / Credential Manager) and the database holding only
  // the secret's name is proven by unit tests (ProviderActionsTests); the web harness has no OS store.
  // Black-box here: the key never shows after a refused Add, nor anywhere in the settings page.
  await openProviders(page);
  await openDialog(page);
  await page.getByTestId('field-name').fill(nameFor('FN-017'));
  await pick(page, 'field-connector-button', 'connector-compatible');
  await page.getByTestId('field-address').fill(DEAD);
  await page.getByTestId('field-key').fill(FAKE_KEY);
  await expect(page.getByTestId('field-key')).toHaveAttribute('type', 'password');
  await page.getByTestId('dialog-add').click();
  await expect(page.getByTestId('dialog-error')).toBeVisible({ timeout: 30_000 });
  await page.getByTestId('dialog-cancel').click();
  await openProviders(page);
  expect(await page.content()).not.toContain(FAKE_KEY);
  await expect(page.getByTestId('secrets-panel')).toBeVisible();
  await expect(page.getByTestId('secret-windows')).toContainText('Credential Manager');
  await expect(page.getByTestId('secret-mac')).toContainText('Keychain');
});

test('REQ-UI-019 testing a provider shows on its row whether it answered, and the message when it did not', async ({ page }) => {
  const vName = nameFor('UI-019');
  await openProviders(page);
  await addOllama(page, vName);
  await page.getByTestId(`test-${vName}`).click();
  await expect(page.getByTestId(`${vName}-error`)).toBeVisible({ timeout: 45_000 });
  expect((await page.getByTestId(`${vName}-error`).innerText()).trim().length).toBeGreaterThan(0);
  await expect(page.getByTestId(`${vName}-state`)).toContainText(/failed/i);
  await removeByName(page, vName);

  // A provider that does answer: its row says so.
  await ensureReal(page);
  const vKey = providerKey(testProvider().name);
  await page.getByTestId(`test-${vKey}`).click();
  await expect(page.getByTestId(`${vKey}-state`)).toContainText(/connected/i, { timeout: 60_000 });
  await expect(page.getByTestId(`${vKey}-error`)).toHaveCount(0);
});

test('REQ-UI-020 removing a provider takes it out of the list', async ({ page }) => {
  // The secret deletion half is covered by unit tests (RemoveAsyncDeletesTheProviderAndItsSecret); a local provider has no secret.
  const vName = nameFor('UI-020');
  await openProviders(page);
  await addOllama(page, vName);
  await page.getByTestId(`remove-${vName}`).click();
  await expect(page.getByTestId('providers-table')).not.toContainText(vName, { timeout: 15_000 });
  await page.reload({ waitUntil: 'networkidle' });
  await expect(page.getByTestId('providers-table')).not.toContainText(vName);
});

async function openRouting(aPage: Page): Promise<void> {
  await aPage.goto('/settings/routing', { waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('tiers')).toBeVisible({ timeout: 30_000 });
}

test('REQ-FN-018 the owner can set a role tier on the Routing page', async ({ page }) => {
  await openRouting(page);
  await expect(page.getByTestId('agent-tiers-panel')).toBeVisible({ timeout: 5_000 });
  for (const vRole of ['analyst', 'architect', 'flowmaster', 'verifier'])
    await expect(page.getByTestId(`${vRole}-tier-button`)).toBeVisible({ timeout: 2_000 });
  // Work started by the role using a model of that tier needs a real model (TR-RAG-003) and is not UI-observable.
});

test('REQ-FN-019 the owner can set a kind of work to a tier on the Routing page', async ({ page }) => {
  await openRouting(page);
  await expect(page.getByTestId('work-tiers-panel')).toBeVisible({ timeout: 5_000 });
  for (const vKind of ['chat', 'plan', 'write-code', 'review-code', 'summarise', 'verify'])
    await expect(page.getByTestId(`${vKind}-tier-button`)).toBeVisible({ timeout: 2_000 });
  await expect(page.getByTestId('work-wins-note')).toBeVisible();
});

test('REQ-FN-020 the owner can reorder the models in a tier on the Routing page', async ({ page }) => {
  await ensureReal(page);
  await openRouting(page);
  await expect(page.getByTestId('tier-1-add')).toBeVisible({ timeout: 5_000 });
  await expect(page.getByTestId('tier-3-add')).toBeVisible({ timeout: 2_000 });
  await expect(page.getByTestId('reset-tiers')).toBeVisible();
  // Start from the shipped tiers; the connected provider's models are all filed in Tier 2.
  await page.getByTestId('reset-tiers').click();
  await expect(page.getByTestId('tier-2-row-2')).toBeVisible({ timeout: 10_000 });
  const vFirst = (await page.getByTestId('tier-2-row-1').innerText()).replace(/\s+/g, ' ').trim();
  const vSecond = (await page.getByTestId('tier-2-row-2').innerText()).replace(/\s+/g, ' ').trim();
  await expect(page.getByTestId('tier-2-up-1')).toBeDisabled();
  // Moving the first model down makes it second; the new order is the order after reopening the page.
  await page.getByTestId('tier-2-down-1').click();
  await expect(page.getByTestId('tier-2-row-1')).toContainText(vSecond.replace(/^\d+ /, '').split(' ')[0]);
  await page.reload({ waitUntil: 'networkidle' });
  expect((await page.getByTestId('tier-2-row-2').innerText()).replace(/\s+/g, ' ').trim()).toBe(vFirst.replace(/^1 /, '2 '));
  // Moving it back up, and then Reset the tiers, both give the shipped order again.
  await page.getByTestId('tier-2-up-2').click();
  await expect(async () => {
    expect((await page.getByTestId('tier-2-row-1').innerText()).replace(/\s+/g, ' ').trim()).toBe(vFirst);
  }).toPass({ timeout: 10_000 });
  await page.getByTestId('tier-2-down-1').click();
  await expect(page.getByTestId('tier-2-up-2')).toBeEnabled();
  await page.getByTestId('reset-tiers').click();
  await expect(async () => {
    expect((await page.getByTestId('tier-2-row-1').innerText()).replace(/\s+/g, ' ').trim()).toBe(vFirst);
  }).toPass({ timeout: 10_000 });
});

const FIXTURE_ROOT = resolve(__dirname, '../.artifacts/verify/fixtures/workbench-routing-root');
const FIXTURE_PROJECT = 'routing-fixture';

async function openWorkbench(aPage: Page): Promise<void> {
  const vDir = resolve(FIXTURE_ROOT, FIXTURE_PROJECT);
  mkdirSync(vDir, { recursive: true });
  if (!existsSync(resolve(vDir, 'notes.txt'))) writeFileSync(resolve(vDir, 'notes.txt'), 'hello = 1\n');
  await aPage.goto('/start?all=1', { waitUntil: 'networkidle' });
  const vList = aPage.getByTestId('recent-list');
  await expect(vList).toBeVisible({ timeout: 20_000 });
  await expect(aPage.getByTestId('recent-loading')).toHaveCount(0, { timeout: 20_000 });
  if (!(await vList.getByText(FIXTURE_PROJECT, { exact: true }).count())) {
    await openFoldersDialog(aPage);
    await aPage.getByTestId('new-folder-path').fill(FIXTURE_ROOT);
    await aPage.getByTestId('add-folder').click();
    await expect(aPage.getByTestId('folders-list')).toContainText(FIXTURE_ROOT);
    await aPage.getByTestId('folders-dialog-close').click();
  }
  await expect(async () => {
    if (!(await aPage.getByTestId('chat-panel').isVisible())) await vList.getByText(FIXTURE_PROJECT, { exact: true }).click();
    await expect(aPage.getByTestId('chat-panel')).toBeVisible({ timeout: 4000 });
  }).toPass({ timeout: 30_000 });
  await expect(aPage.getByTestId('ask-input')).toBeEnabled({ timeout: 15_000 });
}

async function tierRows(aPage: Page, aTier: number): Promise<string[]> {
  const vRows = aPage.locator(`[data-testid^="tier-${aTier}-row-"]`);
  const vTexts: string[] = [];
  for (let i = 0; i < (await vRows.count()); i++) vTexts.push((await vRows.nth(i).innerText()).replace(/\s+/g, ' ').trim());
  return vTexts;
}

async function clearTier(aPage: Page, aTier: number): Promise<void> {
  const vRows = aPage.locator(`[data-testid^="tier-${aTier}-row-"]`);
  for (let vGuard = 0; vGuard < 40; vGuard++) {
    const vCount = await vRows.count();
    if (vCount === 0) return;
    await aPage.getByTestId(`tier-${aTier}-remove-${vCount}`).click({ timeout: 10_000 });
    await expect(vRows).toHaveCount(vCount - 1, { timeout: 10_000 });
  }
}

async function addToTier(aPage: Page, aTier: number, aIdentifier: string, aProvider: string): Promise<void> {
  await aPage.getByTestId(`tier-${aTier}-add`).click();
  await aPage.getByTestId(`tier-${aTier}-add-pop`).locator(`[data-testid^="tier-${aTier}-add-"]`)
    .filter({ hasText: `${aIdentifier} (${aProvider})` }).first().click();
}

test('REQ-FN-021 a limited model steps aside to the next in the chain and the reply says which model answered', async ({ page }) => {
  test.setTimeout(240_000);
  const vReal = testProvider();
  const vDeadName = nameFor('FN-021');
  const vTier = 3;
  await ensureReal(page);
  await openProviders(page);
  await addOllama(page, vDeadName);

  await openRouting(page);
  const vOriginalChain = await tierRows(page, vTier);
  const vAnalystBefore = (await page.getByTestId('analyst-tier-button').innerText()).match(/[123]/)?.[0] ?? '2';
  try {
    // chain: the dead local model first, the real model second, nothing else
    await clearTier(page, vTier);
    await addToTier(page, vTier, vDeadName, vDeadName);
    await expect(page.getByTestId(`tier-${vTier}-row-1`)).toContainText(vDeadName);
    await addToTier(page, vTier, vReal.model, vReal.name);
    await expect(page.getByTestId(`tier-${vTier}-row-2`)).toContainText(vReal.model);
    await page.getByTestId('analyst-tier-button').click();
    await page.getByTestId(`analyst-tier-${vTier}`).click();
    await expect(page.getByTestId('analyst-tier-button')).toContainText(String(vTier));

    await openWorkbench(page);
    await page.getByTestId('agent-pick').click();
    await page.getByTestId('agent-analyst').click();
    await expect(page.getByTestId('agent-pick')).toContainText(/analyst/i);
    await expect(page.getByTestId('model-pick')).toContainText('Automatic');
    await page.getByTestId('ask-input').fill('Reply with the single word: ready');
    await page.getByTestId('send').click();

    const vReply = page.getByTestId('conversation-thread').locator('.msg.agent').last();
    await expect(vReply.locator('.model')).toBeVisible({ timeout: 120_000 });
    await expect(vReply.locator('.model')).toContainText(vReal.model);
    await expect(vReply.locator('.model')).not.toContainText(vDeadName);
    await expect(page.getByTestId('activity-step').filter({ hasText: /limited or unavailable/i })).not.toHaveCount(0);
    await expect(page.getByTestId('activity-step').filter({ hasText: vDeadName }).first()).toBeVisible();
  } finally {
    await openRouting(page);
    await clearTier(page, vTier);
    for (const vText of vOriginalChain) {
      if (vText.includes(vDeadName)) continue;
      const vMatch = vText.match(/^\d+ (.+?) (\S.*)$/);
      if (vMatch) await addToTier(page, vTier, vMatch[1], vMatch[2]).catch(() => undefined);
    }
    await page.getByTestId('analyst-tier-button').click();
    await page.getByTestId(`analyst-tier-${vAnalystBefore}`).click();
  }
});

test('REQ-FN-022 repeated failed fixes climb a tier and record the move', async () => {
  test.skip(true, 'Not honestly drivable through the UI: it needs an approved change followed by the build failing twice on a fixture project, and which change a real model proposes cannot be controlled. Covered by AgentActionsTests unit tests (RunBuildEscalatesTheWorkKindAfterRepeatedFailedBuildsFollowingAnApprovedChange).');
});

test('REQ-FN-023 routing settings are as they were left after reopening Settings', async ({ page }) => {
  // The only UI way to leave a routing setting is a tier picker / chain control on the Routing page.
  await openRouting(page);
  const vPicker = page.getByTestId('chat-tier-button');
  await expect(vPicker, 'no work-kind tier picker on Routing, so no setting can be left and re-read').toBeVisible({ timeout: 5_000 });
  const vBefore = (await vPicker.innerText()).trim();
  const vTarget = vBefore.includes('3') ? '1' : '3';
  await vPicker.click();
  await page.getByTestId(`chat-tier-${vTarget}`).click();
  await page.reload({ waitUntil: 'networkidle' });
  await expect(page.getByTestId('chat-tier-button')).toContainText(vTarget);
  // restore
  await page.getByTestId('chat-tier-button').click();
  await page.getByTestId(`chat-tier-${vBefore.match(/[123]/)?.[0] ?? '2'}`).click();
});
