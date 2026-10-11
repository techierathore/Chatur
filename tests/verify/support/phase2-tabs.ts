// Opens the phase-2 screens the way a user does: from the main window's Project menu, each one a tab in
// the same strip files open in (docs/Chatur-P2-UIDesign.md: "Each of these opens as a tab in the one
// window, with the conversation still pinned to its right"). Clicks made before the circuit is
// interactive are retried.
import { Locator, Page, expect } from '@playwright/test';

export const tabStrip = (aPage: Page): Locator => aPage.getByTestId('tabs');

/** A tab in the strip, by its label. */
export const tab = (aPage: Page, aLabel: string | RegExp): Locator =>
  tabStrip(aPage).getByText(aLabel, typeof aLabel === 'string' ? { exact: true } : undefined);

async function fromProjectMenu(aPage: Page, aItemId: string, aTab: string | RegExp): Promise<void> {
  await expect(async () => {
    if (!(await aPage.getByTestId(aItemId).isVisible())) await aPage.getByTestId('menu-project').click();
    await aPage.getByTestId(aItemId).click({ timeout: 2_500 });
    await expect(tab(aPage, aTab).first()).toBeVisible({ timeout: 3_000 });
  }).toPass({ timeout: 30_000 });
}

/** Project ▸ Processes… */
export async function openProcessesTab(aPage: Page): Promise<void> {
  await fromProjectMenu(aPage, 'menu-project-processes', 'Processes');
  await expect(aPage.getByTestId('process-table')).toBeVisible({ timeout: 30_000 });
}

/** Project ▸ Board… */
export async function openBoardTab(aPage: Page): Promise<void> {
  await fromProjectMenu(aPage, 'menu-project-board', 'Board');
  await expect(aPage.getByTestId('board-table')).toBeVisible({ timeout: 30_000 });
}

/** Project ▸ Agent workspace… */
export async function openAgentsTab(aPage: Page): Promise<void> {
  await fromProjectMenu(aPage, 'menu-project-agents', 'Agents');
  await expect(aPage.getByTestId('workspace-heading')).toBeVisible({ timeout: 30_000 });
}

/** Presses Start on a process in the open Processes tab and returns the id of the run tab it opens. */
export async function startProcess(aPage: Page, aCode: string): Promise<number> {
  const vRunTab = tab(aPage, new RegExp(`^${aCode} · run \\d+$`));
  await expect(async () => {
    if (!(await vRunTab.first().isVisible())) await aPage.getByTestId(`start-${aCode}`).click({ timeout: 2_500 });
    await expect(vRunTab.first()).toBeVisible({ timeout: 3_000 });
  }).toPass({ timeout: 30_000 });
  const vText = (await vRunTab.first().innerText()).trim();
  return Number(/run (\d+)$/.exec(vText)![1]);
}

/** Closes a tab by its label with its own close button. */
export async function closeTab(aPage: Page, aLabel: string): Promise<void> {
  await tabStrip(aPage).getByRole('button', { name: new RegExp(`^Close ${aLabel.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}`) }).click();
  await expect(tab(aPage, aLabel)).toHaveCount(0);
}
