// Shared Start helpers for the acceptance tests.
import { Page, Locator, expect } from '@playwright/test';

/**
 * Opens the Project folders dialog from a Start button (default `manage-folders`; the empty state's
 * `name-a-folder` also works) and waits until the dialog is visible.
 *
 * Why it retries: the web harness prerenders Start, so the whole page (button, loaded list) is in
 * the DOM before the Blazor circuit attaches. A click made in that window reaches no handler and is
 * lost (the button only shows its pressed style); Playwright cannot tell, because the prerendered
 * page looks finished. Opening is idempotent (it only sets "open"), so the click is repeated until
 * the dialog shows. The assertion itself is unchanged: `folders-dialog` must become visible.
 */
export async function openFoldersDialog(aPage: Page, aButtonTestId = 'manage-folders'): Promise<Locator> {
  const vDialog = aPage.getByTestId('folders-dialog');
  await expect(async () => {
    if (!(await vDialog.isVisible())) await aPage.getByTestId(aButtonTestId).click();
    await expect(vDialog).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
  return vDialog;
}
