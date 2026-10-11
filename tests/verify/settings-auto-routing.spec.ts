// Acceptance tests: Settings > Routing > Automatic routing (REQ-FN-071, REQ-FN-072). Black-box, web harness.
import { test, expect } from '@playwright/test';
import { signIn } from './support/auth';

test.describe.configure({ mode: 'serial' });

test('REQ-FN-071 the owner turns automatic routing on and it is remembered', async ({ page }) => {
  await signIn(page, 'Yolo');
  await page.goto('/settings/routing', { waitUntil: 'networkidle' });
  await expect(page.getByTestId('auto-routing-panel')).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('auto-routing-later')).toHaveCount(0);
  const vSwitch = page.getByTestId('auto-routing-switch');
  await expect(vSwitch).toBeEnabled();
  if ((await page.getByTestId('auto-routing-state').innerText()) === 'on') await vSwitch.click();
  await expect(page.getByTestId('auto-routing-state')).toHaveText('off');
  await vSwitch.click();
  await expect(page.getByTestId('auto-routing-state')).toHaveText('on');
  await page.goto('/settings/routing', { waitUntil: 'networkidle' });
  await expect(page.getByTestId('auto-routing-state')).toHaveText('on', { timeout: 30_000 });
});

test('REQ-FN-072 the owner turns automatic routing off and the tiers stay as they were', async ({ page }) => {
  await signIn(page, 'Yolo');
  await page.goto('/settings/routing', { waitUntil: 'networkidle' });
  await expect(page.getByTestId('auto-routing-panel')).toBeVisible({ timeout: 30_000 });
  const vBefore = await page.getByTestId('tier-1-count').innerText();
  if ((await page.getByTestId('auto-routing-state').innerText()) === 'off') await page.getByTestId('auto-routing-switch').click();
  await expect(page.getByTestId('auto-routing-state')).toHaveText('on');
  await page.getByTestId('auto-routing-switch').click();
  await expect(page.getByTestId('auto-routing-state')).toHaveText('off');
  await page.goto('/settings/routing', { waitUntil: 'networkidle' });
  await expect(page.getByTestId('auto-routing-state')).toHaveText('off', { timeout: 30_000 });
  await expect(page.getByTestId('tier-1-count')).toHaveText(vBefore);
});
