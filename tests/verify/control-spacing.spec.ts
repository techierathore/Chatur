// Acceptance tests: the shared spacing every control keeps (REQ-UI-080, the owner's 29 Oct report
// docs/screenshots/2026-10-29-first-use/New-Project-Incorrect-Paddings.png). Black-box, web harness: the
// values are read from the rendered controls, the way the owner sees them, on the New project dialog and
// the File menu, which load the same shared stylesheet as every other screen.
import { test, expect, Page, Locator } from '@playwright/test';
import { signIn } from './support/auth';

const SHOTS = 'tests/.artifacts/verify/shots';

const px = (aLocator: Locator, aProperty: string): Promise<number> =>
  aLocator.evaluate((e, p) => parseFloat(getComputedStyle(e).getPropertyValue(p)) || 0, aProperty);

/** The width of the focus ring's offset band, read from the box-shadow the ring draws. */
const ringOffset = (aLocator: Locator): Promise<string> =>
  aLocator.evaluate((e) => getComputedStyle(e).getPropertyValue('--tw-ring-offset-width').trim());

async function openNewProject(aPage: Page): Promise<Locator> {
  await aPage.goto('/start?all=1', { waitUntil: 'networkidle' });
  const vDialog = aPage.getByTestId('open-project-dialog');
  await expect(async () => {
    if (!(await vDialog.isVisible())) await aPage.getByTestId('new-project').click();
    await expect(vDialog).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
  return vDialog;
}

test.beforeEach(async ({ page }) => {
  await signIn(page, 'Owner');
});

test('REQ-UI-080 a focused field on New project keeps its text inside, its ring on its border and clear of its label', async ({ page }) => {
  const vDialog = await openNewProject(page);
  const vName = page.getByTestId('open-project-name').locator('xpath=descendant-or-self::input').first();
  await vName.focus();
  // The text sits inside the field, never against its border.
  expect(await px(vName, 'padding-left')).toBeGreaterThanOrEqual(10);
  expect(await px(vName, 'padding-right')).toBeGreaterThanOrEqual(10);
  // The ring hugs the border instead of standing off it (the band the owner saw run into the label).
  expect(await ringOffset(vName)).toMatch(/^0(px)?$/);
  // The label above has room: the field starts at least 6px below it.
  const vLabel = vDialog.locator('label').first();
  const vLabelBox = (await vLabel.boundingBox())!;
  const vFieldBox = (await vName.boundingBox())!;
  expect(vFieldBox.y - (vLabelBox.y + vLabelBox.height)).toBeGreaterThanOrEqual(6);
  // The field keeps a gutter to the dialog's own edge on both sides.
  const vDialogBox = (await vDialog.boundingBox())!;
  expect(vFieldBox.x - vDialogBox.x).toBeGreaterThanOrEqual(14);
  expect(vDialogBox.x + vDialogBox.width - (vFieldBox.x + vFieldBox.width)).toBeGreaterThanOrEqual(14);
  // The buttons keep their text inside too.
  for (const vId of ['open-project-cancel', 'open-project-go']) {
    expect(await px(page.getByTestId(vId), 'padding-left'), vId).toBeGreaterThanOrEqual(10);
  }
  await page.screenshot({ path: `${SHOTS}/REQ-UI-080-new-project.png` });
});

test('REQ-UI-080 menu items and pop-up lists keep the mockup spacing of 10px across and 5px around', async ({ page }) => {
  await page.goto('/', { waitUntil: 'networkidle' });
  await expect(async () => {
    if (!(await page.getByTestId('menu-file-items').isVisible())) await page.getByTestId('menu-file').click();
    await expect(page.getByTestId('menu-file-items')).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
  const vItem = page.getByTestId('menu-file-items').getByRole('menuitem').first();
  expect(await px(vItem, 'padding-left')).toBeGreaterThanOrEqual(10);
  expect(await px(vItem, 'padding-right')).toBeGreaterThanOrEqual(10);
  expect(await px(page.getByTestId('menu-file-items'), 'padding-top')).toBeGreaterThanOrEqual(5);
  await page.screenshot({ path: `${SHOTS}/REQ-UI-080-menu.png` });
  await page.keyboard.press('Escape');
});
