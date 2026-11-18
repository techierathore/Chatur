// Connects the owner's test provider (the testProvider block of test-settings.json) through the real Add a provider
// dialog, once per app database. The key is read from tests/verify/test-settings.json and never printed.
import { Page, expect } from '@playwright/test';
import { run } from './db';
import { testSettings } from './settings';

/** The test provider's name, address, model and key from tests/verify/test-settings.json (`testProvider`). */
/** The owner's test provider, exactly as the testProvider block of test-settings.json names it; nothing defaulted here. */
export function testProvider(): { name: string; connector: string; baseUrl: string; model: string; key: string } {
  const vP = testSettings().testProvider;
  if (!vP?.name || !vP.connector || !vP.model) throw new Error('test-settings.json: testProvider needs name, connector and model');
  return { name: vP.name, connector: vP.connector, baseUrl: vP.baseUrl ?? '', model: vP.model, key: vP.key };
}

/** The Add provider dialog's choice for a connector name as the provider row stores it. */
const CONNECTOR_ITEM: Record<string, string> = {
  anthropic: 'connector-anthropic', openai: 'connector-openai', openaicompatible: 'connector-compatible',
  google: 'connector-google', ollama: 'connector-ollama',
};

/** The data-testid of the Add provider dialog's connector choice for the test provider's connector. */
export function connectorItem(aConnector: string): string {
  const vItem = CONNECTOR_ITEM[aConnector.toLowerCase()];
  if (!vItem) throw new Error(`test-settings.json: the Add provider dialog has no connector "${aConnector}"`);
  return vItem;
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
  await aPage.getByTestId(connectorItem(vP.connector)).click();
  if (vP.baseUrl) await field(aPage, 'field-address').fill(vP.baseUrl);
  await field(aPage, 'field-key').fill(vP.key);
  await aPage.getByTestId('dialog-add').click();
  await expect(aPage.getByTestId('add-provider-dialog')).toHaveCount(0, { timeout: 60_000 });
  await expect(aPage.getByTestId('providers-table').getByText(vP.name, { exact: true }).first()).toBeVisible();
  // A provider left behind in a non-Connected state (e.g. by stub-test isolation) still needs its
  // models to appear on the Routing page.
  run("UPDATE Provider SET State = 'Connected' WHERE Name = ?", vP.name);
}

/** On the Workbench, chooses the test provider's model in the composer's model picker. */
export async function chooseTestModel(aPage: Page): Promise<void> {
  const vP = testProvider();
  await aPage.getByTestId('model-pick').click();
  await aPage.getByTestId('model-pop').getByText(vP.model, { exact: true }).click();
  await expect(aPage.getByTestId('model-pick')).toContainText(vP.model);
}
