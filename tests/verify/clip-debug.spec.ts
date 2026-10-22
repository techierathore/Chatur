import { test, expect } from '@playwright/test';
test('clipboard write/read', async ({ context, page }) => {
  await context.grantPermissions(['clipboard-read', 'clipboard-write']);
  await page.goto('http://localhost:5280/sign-in');
  const vText = await page.evaluate(async () => {
    await navigator.clipboard.writeText('hello-clip');
    return navigator.clipboard.readText();
  });
  expect(vText).toContain('hello-clip');
});
