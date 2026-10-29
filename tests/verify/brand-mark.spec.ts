// Acceptance test: the Chatur mark beside the wordmark (REQ-UI-078, mockups/start.html `.logo`).
// Black-box: the `logo` anchor on Start, a bare page, the main window and the secondary window is
// measured in the browser: a 20 px square, amber behind, the glyph drawn in white.
import { test, expect, Page } from '@playwright/test';
import { signIn } from './support/auth';

const AMBER = '#b45f14';
const WHITE = '#ffffff';

/** Reads the logo's drawn size and the fills of its square and its glyph. */
async function readMark(aPage: Page): Promise<{ width: number; height: number; square: string; glyph: string }> {
  const vLogo = aPage.getByTestId('logo').first();
  await expect(vLogo).toBeVisible({ timeout: 20_000 });
  const vBox = await vLogo.boundingBox();
  const vSquare = (await aPage.getByTestId('logo-square').first().getAttribute('fill')) ?? '';
  const vGlyph = (await aPage.getByTestId('logo-glyph').first().getAttribute('fill')) ?? '';
  return { width: Math.round(vBox?.width ?? 0), height: Math.round(vBox?.height ?? 0), square: vSquare.toLowerCase(), glyph: vGlyph.toLowerCase() };
}

async function expectMark(aPage: Page): Promise<void> {
  const vMark = await readMark(aPage);
  expect(vMark.width).toBe(20);
  expect(vMark.height).toBe(20);
  expect(vMark.square).toBe(AMBER);
  expect(vMark.glyph).toBe(WHITE);
}

/**
 * REQ-UI-078 — When the owner opens Start, a bare page, the main or the secondary window, then the
 * logo is the white glyph on a 20 px amber square. The bare page is the sign-in page (seen before
 * sign-in); Start is reached on purpose with `?all=1`; the main window is `/`; the secondary is `/feedback`.
 */
test('REQ-UI-078 the mark is the white glyph on a 20 px amber square in every window', async ({ page }) => {
  test.setTimeout(180_000);
  await page.setViewportSize({ width: 1280, height: 800 });

  await page.goto('/sign-in', { waitUntil: 'networkidle' });
  await expectMark(page);

  await signIn(page, 'Owner');

  await page.goto('/start?all=1', { waitUntil: 'networkidle' });
  await expect(page.getByTestId('recent-side')).toBeVisible({ timeout: 20_000 });
  await expectMark(page);

  await page.goto('/', { waitUntil: 'networkidle' });
  await expectMark(page);

  await page.goto('/feedback', { waitUntil: 'networkidle' });
  await expectMark(page);
});
