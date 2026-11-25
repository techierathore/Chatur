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

test('REQ-UI-113 the Routing page says in plain words how Automatic chooses, with one example', async ({ page }) => {
  await signIn(page, 'Yolo');
  await page.goto('/settings/routing', { waitUntil: 'networkidle' });
  await expect(page.getByTestId('auto-routing-panel')).toBeVisible({ timeout: 30_000 });
  const vExplain = page.getByTestId('auto-routing-explain');
  await expect(vExplain).toBeVisible();
  // what AutoRouter does: a small model reads the message and picks among the listed models; the person's own pick wins; the tiers decide if it fails
  await expect(vExplain).toContainText('small model');
  await expect(vExplain).toContainText('tier lists');
  await expect(vExplain).toContainText('you pick yourself');
  await expect(vExplain).toContainText('For example');
  await expect(vExplain).toContainText('which model answered and why');
});
