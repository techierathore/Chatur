// Acceptance tests: what is left on each key, on Measurements and Measurements across projects (REQ-UI-129, REQ-UI-130,
// REQ-UI-131, REQ-FN-326; BRD-287 to BRD-290; mockups/measurements.html "keys-panel" and "limit-dialog"), and the secondary
// window's nav groups that the 390 px mockup check of REQ-FN-313 found missing. Black-box, web harness, signed in as test
// user 2 ('Owner').
//
// Seeded through the database (a state the UI cannot make: a provider of each kind without a real key, and the priced calls
// Chatur made through a key), every row's name carries this run's stamp, and all of it is removed when the file ends:
//   Keys Claude <stamp>      Anthropic, key            → a limit is typed through the dialog; Chatur's own count
//   Keys Local <stamp>       Ollama, local             → "not known": a local model has no key
//   Keys OpenAI <stamp>      OpenAI, key               → "not known": OpenAI gives no balance to a normal key, no limit typed
//   Keys Router <stamp>      OpenRouter, key not filed → "not known": openrouter.ai was not asked, and says why
// A real OpenRouter or DeepSeek answer (REQ-UI-130) needs a real key: test-settings.json `balanceProvider` { name, baseUrl,
// key }; without it that one test is skipped and says so. The key is never printed.
import { test, expect, Page, Locator } from '@playwright/test';
import { mkdirSync, writeFileSync } from 'fs';
import { join } from 'path';
import { signIn } from './support/auth';
import { removeProjectsUnder, scratchDir } from './support/cleanup';
import { query, run } from './support/db';
import { testSettings } from './support/settings';
import { openByDialog } from './support/workbench-b';

test.describe.configure({ mode: 'serial', timeout: 240_000 });

const STAMP = Date.now();
const NAME = `keys-${STAMP}`;
const CLAUDE = `Keys Claude ${STAMP}`;
const LOCAL = `Keys Local ${STAMP}`;
const OPENAI = `Keys OpenAI ${STAMP}`;
const ROUTER = `Keys Router ${STAMP}`;
const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
let objRoot = '';

/** The first of this month as the panel words it, e.g. "1 Nov 2026" — the Limit dialog's day to begin with. */
function firstOfMonth(): string {
  const vNow = new Date();
  return `1 ${MONTHS[vNow.getMonth()]} ${vNow.getFullYear()}`;
}

/** A moment a minute ago, as KeyUsage.CreatedUtc is written: yyyy-MM-dd HH:mm:ss in UTC. */
function aMinuteAgoUtc(): string {
  return new Date(Date.now() - 60_000).toISOString().replace('T', ' ').substring(0, 19);
}

function providerId(aName: string): number {
  const vRows = query('SELECT ProviderId FROM Provider WHERE Name = ?', aName);
  expect(vRows.length, `provider ${aName} is seeded`).toBe(1);
  return Number(vRows[0].ProviderId);
}

/** Seeds the four providers. No key is filed for any of them, so no address is ever called with one. */
function seedProviders(): void {
  const vAdd = (aName: string, aConnector: string, aSignIn: string, aBaseUrl: string, aSecret: string | null) =>
    run("INSERT INTO Provider (Name, Connector, SignInMethod, BaseUrl, SecretName, State) VALUES (?, ?, ?, ?, ?, 'NotTestedYet')",
      aName, aConnector, aSignIn, aBaseUrl, aSecret);
  vAdd(CLAUDE, 'Anthropic', 'Key', 'https://api.anthropic.com', `keys-test.${STAMP}.claude`);
  vAdd(LOCAL, 'Ollama', 'None', 'http://127.0.0.1:11434', null);
  vAdd(OPENAI, 'OpenAI', 'Key', 'https://api.openai.com/v1', `keys-test.${STAMP}.openai`);
  vAdd(ROUTER, 'OpenAICompatible', 'Key', 'https://openrouter.ai/api/v1', `keys-test.${STAMP}.router`);
}

function removeSeed(): void {
  const vIds = query('SELECT ProviderId FROM Provider WHERE Name LIKE ?', `% ${STAMP}`).map((r) => Number(r.ProviderId));
  for (const vId of vIds) {
    run('DELETE FROM KeyUsage WHERE ProviderId = ?', vId);
    run('DELETE FROM Setting WHERE Key = ?', `KeyLimit:${vId}`);
    run('DELETE FROM Model WHERE ProviderId = ?', vId);
    run('DELETE FROM Provider WHERE ProviderId = ?', vId);
  }
}

test.beforeAll(() => {
  objRoot = scratchDir(NAME);
  const vMetrics = join(objRoot, 'docs', 'metrics');
  mkdirSync(vMetrics, { recursive: true });
  writeFileSync(join(vMetrics, 'runs.jsonl'), JSON.stringify({ kind: 'run', ts: new Date().toISOString(), cmd: 'build-phase', model: 'claude-sonnet-5-5', tokens_in: 10, tokens_out: 20 }) + '\n');
});

test.afterAll(() => {
  try {
    removeSeed();
  } finally {
    if (objRoot) removeProjectsUnder(objRoot);
  }
});

/** Signs in (which names the account's database), seeds once, and makes the scratch project the open one. */
async function ready(aPage: Page): Promise<void> {
  await expect(async () => {
    await signIn(aPage, 'Owner');
  }).toPass({ timeout: 120_000, intervals: [500] });
  if (query('SELECT 1 AS x FROM Provider WHERE Name = ?', CLAUDE).length === 0) seedProviders();
  await aPage.goto('/', { waitUntil: 'networkidle' });
  const vName = aPage.getByTestId('project-switch-name');
  const vOpen = await vName.waitFor({ timeout: 10_000 }).then(() => vName.innerText(), () => '');
  if (vOpen.trim() !== NAME) await openByDialog(aPage, 'open-folder', objRoot, NAME);
}

async function openPanel(aPage: Page, aRoute: '/measurements' | '/measurements/all'): Promise<Locator> {
  await aPage.goto(aRoute, { waitUntil: 'networkidle' });
  const vPanel = aPage.getByTestId('keys-panel');
  await expect(vPanel).toBeVisible({ timeout: 30_000 });
  await expect(aPage.getByTestId('keys-table')).toBeVisible({ timeout: 30_000 });
  return vPanel;
}

/** The 1-based number that ends a provider's row test ids (key-row-N), found by its name. */
async function rowOf(aPage: Page, aName: string): Promise<string> {
  const vRow = aPage.locator('[data-testid^="key-row-"]').filter({ has: aPage.getByText(aName, { exact: true }) });
  await expect(vRow).toHaveCount(1, { timeout: 30_000 });
  const vId = await vRow.getAttribute('data-testid');
  return (vId ?? '').replace('key-row-', '');
}

const field = (aPage: Page, aId: string) => aPage.locator(`input[data-testid=${aId}], [data-testid=${aId}] input`).first();

test('REQ-UI-129 each connected provider has one line on both Measurements screens, saying what is left and how it is known', async ({ page }) => {
  // REQ-UI-129: one line per provider, with the pill and the Limit, Used and Left columns, on Measurements and on
  // Measurements across projects; the panel sits between the tiles and the split on the first, after the projects on the second.
  await ready(page);
  const vProviders = query('SELECT COUNT(*) AS n FROM Provider').map((r) => Number(r.n))[0];
  for (const vRoute of ['/measurements', '/measurements/all'] as const) {
    await openPanel(page, vRoute);
    await expect(page.locator('[data-testid^="key-row-"]')).toHaveCount(vProviders);
    for (const vName of [CLAUDE, LOCAL, OPENAI, ROUTER]) {
      const vN = await rowOf(page, vName);
      await expect(page.getByTestId(`key-name-${vN}`)).toHaveText(vName);
      await expect(page.getByTestId(`key-source-${vN}`)).toHaveText(/^(asked of the provider|Chatur's own count|not known)$/);
      await expect(page.getByTestId(`key-note-${vN}`)).not.toHaveText('');
      await expect(page.getByTestId(`key-left-${vN}`)).not.toHaveText('');
      await expect(page.getByTestId(`key-limit-${vN}`)).toBeAttached();
      await expect(page.getByTestId(`key-used-${vN}`)).toBeAttached();
    }
    await expect(page.getByTestId('keys-note')).toContainText('never a guess');
    await expect(page.getByTestId('keys-recheck')).toBeEnabled();
  }
  // Place on the page, as the mockups have it.
  await page.goto('/measurements', { waitUntil: 'networkidle' });
  await expect(page.getByTestId('keys-panel')).toBeVisible({ timeout: 30_000 });
  const vOrder = await page.evaluate(() => ['metrics-tiles', 'keys-panel', 'metrics-split'].map((id) => {
    const vEl = document.querySelector(`[data-testid="${id}"]`);
    return vEl ? vEl.getBoundingClientRect().top : -1;
  }));
  expect(vOrder[0]).toBeLessThan(vOrder[1]);
  if (vOrder[2] >= 0) expect(vOrder[1]).toBeLessThan(vOrder[2]);
  await page.goto('/measurements/all', { waitUntil: 'networkidle' });
  await expect(page.getByTestId('keys-panel')).toBeVisible({ timeout: 30_000 });
  const vAcross = await page.evaluate(() => ['across-panel', 'keys-panel'].map((id) => document.querySelector(`[data-testid="${id}"]`)!.getBoundingClientRect().top));
  expect(vAcross[0]).toBeLessThan(vAcross[1]);
});

test('REQ-UI-130 a provider with a balance address is asked, and the line shows what is left and when it answered', async ({ page }) => {
  // REQ-UI-130: needs a real OpenRouter or DeepSeek key (test-settings.json balanceProvider); added through the real Add a
  // provider dialog, then removed again.
  const vBalance = (testSettings() as unknown as { balanceProvider?: { name: string; baseUrl: string; key: string } }).balanceProvider;
  test.skip(!vBalance?.name || !vBalance.baseUrl || !vBalance.key, 'test-settings.json has no balanceProvider (a real OpenRouter or DeepSeek key), so no provider can be asked');
  await ready(page);
  await page.goto('/settings/providers', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500);
  const vAdded = (await page.getByTestId('providers-table').getByText(vBalance!.name, { exact: true }).count()) === 0;
  if (vAdded) {
    await page.getByTestId('add-provider').click();
    await field(page, 'field-name').fill(vBalance!.name);
    await page.getByTestId('field-connector-button').click();
    await page.getByTestId('connector-compatible').click();
    await field(page, 'field-address').fill(vBalance!.baseUrl);
    await field(page, 'field-key').fill(vBalance!.key);
    await page.getByTestId('dialog-add').click();
    await expect(page.getByTestId('add-provider-dialog')).toHaveCount(0, { timeout: 60_000 });
  }
  try {
    await openPanel(page, '/measurements');
    const vN = await rowOf(page, vBalance!.name);
    await expect(page.getByTestId(`key-source-${vN}`)).toHaveText('asked of the provider', { timeout: 30_000 });
    await expect(page.getByTestId(`key-note-${vN}`)).toHaveText(/^(openrouter\.ai|api\.deepseek\.com) answered at \d\d:\d\d today/);
    await expect(page.getByTestId(`key-left-${vN}`)).toHaveText(/^-?(\$[\d,]+\.\d\d|[\d,]+\.\d\d [A-Z]{3})$/);
    await expect(page.getByTestId(`key-set-limit-${vN}`)).toHaveCount(0);
    await page.getByTestId('keys-recheck').click();
    await expect(page.getByTestId(`key-note-${vN}`)).toHaveText(/answered at \d\d:\d\d today/, { timeout: 30_000 });
  } finally {
    if (vAdded) {
      const vId = providerId(vBalance!.name);
      run('DELETE FROM KeyUsage WHERE ProviderId = ?', vId);
      await page.goto('/settings/providers', { waitUntil: 'networkidle' });
      await page.waitForTimeout(1500);
      const vRemove = page.getByTestId(`remove-${vBalance!.name.toLowerCase().replace(/ /g, '-')}`);
      if ((await vRemove.count()) > 0) await vRemove.click();
    }
  }
});

test('REQ-UI-131 a limit typed for a key shows the limit, what Chatur used since its day, and what is left', async ({ page }) => {
  // REQ-UI-131: the dialog refuses 0, three decimals and an empty amount (Save stays inert and the field says why); $50.00
  // from the first of the month is saved; two priced calls of $10.00 and $2.40 since then read Used $12.40, Left $37.60,
  // "against your limit, since 1 <month> · 2 calls", on both screens. Remove the limit puts the line back to "not known".
  await ready(page);
  const vId = providerId(CLAUDE);
  run('DELETE FROM KeyUsage WHERE ProviderId = ?', vId);
  run("INSERT INTO KeyUsage (ProviderId, ModelIdentifier, CreatedUtc, TokensIn, TokensOut, CostUsd) VALUES (?, 'claude-sonnet-5-5', ?, 1000, 500, 10.00)", vId, aMinuteAgoUtc());
  run("INSERT INTO KeyUsage (ProviderId, ModelIdentifier, CreatedUtc, TokensIn, TokensOut, CostUsd) VALUES (?, 'claude-sonnet-5-5', ?, 200, 100, 2.40)", vId, aMinuteAgoUtc());

  await openPanel(page, '/measurements');
  let vN = await rowOf(page, CLAUDE);
  await expect(page.getByTestId(`key-set-limit-${vN}`)).toHaveText('Type a limit');
  await page.getByTestId(`key-set-limit-${vN}`).click();
  const vDialog = page.getByTestId('limit-dialog');
  await expect(vDialog).toBeVisible();
  await expect(vDialog).toContainText(`Limit for ${CLAUDE}`);
  await expect(page.getByTestId('limit-from')).toContainText(/\S/);
  for (const vBad of ['0', '1.234', 'ten']) {
    await field(page, 'limit-amount').fill(vBad);
    await expect(page.getByTestId('limit-save')).toBeDisabled();
    await expect(page.getByTestId('limit-amount-error')).toBeVisible();
  }
  await field(page, 'limit-amount').fill('50.00');
  await expect(page.getByTestId('limit-amount-error')).toHaveCount(0);
  await expect(page.getByTestId('limit-save')).toBeEnabled();
  await page.getByTestId('limit-save').click();
  await expect(vDialog).toHaveCount(0, { timeout: 15_000 });

  for (const vRoute of ['/measurements', '/measurements/all'] as const) {
    await openPanel(page, vRoute);
    vN = await rowOf(page, CLAUDE);
    await expect(page.getByTestId(`key-source-${vN}`)).toHaveText("Chatur's own count");
    await expect(page.getByTestId(`key-note-${vN}`)).toHaveText(`against your limit, since ${firstOfMonth()} · 2 calls`);
    await expect(page.getByTestId(`key-limit-${vN}`)).toHaveText('$50.00');
    await expect(page.getByTestId(`key-used-${vN}`)).toHaveText('$12.40');
    await expect(page.getByTestId(`key-left-${vN}`)).toHaveText('$37.60');
    await expect(page.getByTestId(`key-set-limit-${vN}`)).toHaveText('Change limit');
  }

  await page.getByTestId(`key-set-limit-${vN}`).click();
  await expect(field(page, 'limit-amount')).toHaveValue('50.00');
  await page.getByTestId('limit-clear').click();
  await expect(page.getByTestId('limit-dialog')).toHaveCount(0, { timeout: 15_000 });
  vN = await rowOf(page, CLAUDE);
  await expect(page.getByTestId(`key-source-${vN}`)).toHaveText('not known');
  await expect(page.getByTestId(`key-left-${vN}`)).toHaveText('not known');
  expect(query('SELECT 1 AS x FROM Setting WHERE Key = ?', `KeyLimit:${vId}`)).toHaveLength(0);
});

test('REQ-FN-326 a key with no number reads "not known" and says why, never a guess', async ({ page }) => {
  // REQ-FN-326: no address and no limit (OpenAI), a local model, an address that was not asked (no key filed), and an own
  // count with a call that had no price: each reads "not known" in Left, "—" where it has no figure, and a reason.
  await ready(page);
  await openPanel(page, '/measurements');

  let vN = await rowOf(page, OPENAI);
  await expect(page.getByTestId(`key-source-${vN}`)).toHaveText('not known');
  await expect(page.getByTestId(`key-note-${vN}`)).toHaveText('OpenAI gives no balance to a normal key, and no limit is typed');
  await expect(page.getByTestId(`key-limit-${vN}`)).toHaveText('—');
  await expect(page.getByTestId(`key-used-${vN}`)).toHaveText('—');
  await expect(page.getByTestId(`key-left-${vN}`)).toHaveText('not known');

  vN = await rowOf(page, LOCAL);
  await expect(page.getByTestId(`key-source-${vN}`)).toHaveText('not known');
  await expect(page.getByTestId(`key-note-${vN}`)).toHaveText(/local model has no key/);
  await expect(page.getByTestId(`key-set-limit-${vN}`)).toHaveCount(0);

  vN = await rowOf(page, ROUTER);
  await expect(page.getByTestId(`key-source-${vN}`)).toHaveText('not known');
  await expect(page.getByTestId(`key-note-${vN}`)).toHaveText(/openrouter\.ai (was not asked|did not answer)/);
  await expect(page.getByTestId(`key-left-${vN}`)).toHaveText('not known');

  // An own count with one call that had no price.
  const vId = providerId(CLAUDE);
  run('DELETE FROM KeyUsage WHERE ProviderId = ?', vId);
  run("INSERT INTO KeyUsage (ProviderId, ModelIdentifier, CreatedUtc, TokensIn, TokensOut, CostUsd) VALUES (?, 'claude-sonnet-5-5', ?, 100, 50, 1.00)", vId, aMinuteAgoUtc());
  run("INSERT INTO KeyUsage (ProviderId, ModelIdentifier, CreatedUtc, TokensIn, TokensOut, CostUsd) VALUES (?, 'claude-sonnet-5-5', ?, 100, 50, NULL)", vId, aMinuteAgoUtc());
  const vToday = new Date();
  const vDay = `${vToday.getFullYear()}-${String(vToday.getMonth() + 1).padStart(2, '0')}-01`;
  run('INSERT INTO Setting (Key, Value) VALUES (?, ?) ON CONFLICT (Key) DO UPDATE SET Value = excluded.Value', `KeyLimit:${vId}`, JSON.stringify({ LimitUsd: 50, CountingFrom: vDay }));
  await page.getByTestId('keys-recheck').click();
  vN = await rowOf(page, CLAUDE);
  await expect(page.getByTestId(`key-source-${vN}`)).toHaveText('not known', { timeout: 30_000 });
  await expect(page.getByTestId(`key-note-${vN}`)).toHaveText(`1 of 2 calls since ${firstOfMonth()} had no price from TechieRag, so what is used is not known`);
  await expect(page.getByTestId(`key-limit-${vN}`)).toHaveText('$50.00');
  await expect(page.getByTestId(`key-used-${vN}`)).toHaveText('—');
  await expect(page.getByTestId(`key-left-${vN}`)).toHaveText('not known');
  run('DELETE FROM Setting WHERE Key = ?', `KeyLimit:${vId}`);
});

test('REQ-FN-313 the secondary nav has the mockups\' groups and order at 1280 and 390 wide', async ({ page }) => {
  // REQ-FN-313 (mockup parity at 390 px): the nav reads "This project" first (Prerequisites, Repository, Working copies,
  // Project measurements, Compare models), then "Chatur" (the settings), then "Your account", as mockups/measurements-all.html.
  await ready(page);
  const vProject = ['nav-prerequisites', 'nav-repository', 'nav-worktrees', 'nav-project-measurements', 'nav-compare-models'];
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.goto('/measurements/all', { waitUntil: 'networkidle' });
  const vNav = page.getByTestId('subnav');
  await expect(vNav.getByTestId('subnav-group-this-project')).toHaveText(/this project/i);
  await expect(vNav.getByTestId('subnav-group-chatur')).toBeVisible();
  await expect(vNav.getByTestId('subnav-group-your-account')).toBeVisible();
  const vIds = await vNav.locator('a[data-testid^="nav-"], [data-testid^="subnav-group-"]').evaluateAll((els) => els.map((e) => e.getAttribute('data-testid') ?? ''));
  expect(vIds[0]).toBe('subnav-group-this-project');
  const vFirst = vIds.slice(1, vIds.indexOf('subnav-group-chatur'));
  expect(vFirst).toEqual(vProject.filter((id) => vFirst.includes(id)));
  expect(vIds.indexOf('nav-providers')).toBeGreaterThan(vIds.indexOf('subnav-group-chatur'));
  expect(vIds.indexOf('nav-measurements-all')).toBeGreaterThan(vIds.indexOf('subnav-group-your-account'));

  await page.setViewportSize({ width: 390, height: 844 });
  await page.getByTestId('subnav-trigger').click();
  const vSheet = page.getByTestId('subnav-mobile');
  await expect(vSheet.getByTestId('subnav-group-this-project-mobile')).toBeVisible();
  await expect(vSheet.locator('a').first()).toHaveAttribute('data-testid', 'nav-prerequisites-mobile');
  await expect(vSheet.locator('a').first().locator('svg')).toHaveCount(1);
  await page.setViewportSize({ width: 1280, height: 720 });
});
