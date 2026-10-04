// Connects the owner's model (decision 1, option A: OpenCode Go) through the real Add a provider
// dialog, once per app database. The key is read from Chatur's user secrets and never printed.
import { Page, expect } from '@playwright/test';
import { readFileSync } from 'fs';

const SECRETS_PATH =
  process.env.CHATUR_SECRETS ??
  '/mnt/c/Users/srkra/AppData/Roaming/Microsoft/UserSecrets/chatur-3f1c9a52-0d4e-4b7a-9c1e-5a8e2b7d6f10/secrets.json';

/** The test provider's name, address, model and key from user secrets (`Chatur:TestProvider:*`). */
export function testProvider(): { name: string; baseUrl: string; model: string; key: string } {
  const vS = JSON.parse(readFileSync(SECRETS_PATH, 'utf8').replace(/^﻿/, ''));
  return {
    name: vS['Chatur:TestProvider:Name'] ?? 'OpenCode Go',
    baseUrl: vS['Chatur:TestProvider:BaseUrl'],
    model: vS['Chatur:TestProvider:Model'],
    key: vS['Chatur:TestProvider:Key'],
  };
}

const field = (aPage: Page, aId: string) => aPage.locator(`input[data-testid=${aId}], [data-testid=${aId}] input`).first();

/** Adds the test provider through Settings ▸ Model providers unless a provider of that name is already listed. */
export async function ensureTestProvider(aPage: Page): Promise<void> {
  const vP = testProvider();
  await aPage.goto('/settings/providers', { waitUntil: 'networkidle' });
  await aPage.waitForTimeout(1500);
  if (await aPage.getByTestId('providers-table').getByText(vP.name, { exact: true }).count() > 0) return;
  await aPage.getByTestId('add-provider').click();
  await field(aPage, 'field-name').fill(vP.name);
  await aPage.getByTestId('field-connector-button').click();
  await aPage.getByTestId('connector-compatible').click();
  await field(aPage, 'field-address').fill(vP.baseUrl);
  await field(aPage, 'field-key').fill(vP.key);
  await aPage.getByTestId('dialog-add').click();
  await expect(aPage.getByTestId('add-provider-dialog')).toHaveCount(0, { timeout: 60_000 });
  await expect(aPage.getByTestId('providers-table').getByText(vP.name, { exact: true })).toBeVisible();
}

/** On the Workbench, chooses the test provider's model in the composer's model picker. */
export async function chooseTestModel(aPage: Page): Promise<void> {
  const vP = testProvider();
  await aPage.getByTestId('model-pick').click();
  await aPage.getByTestId('model-pop').getByText(vP.model, { exact: true }).click();
  await expect(aPage.getByTestId('model-pick')).toContainText(vP.model);
}
