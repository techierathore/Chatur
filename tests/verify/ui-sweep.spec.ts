// Acceptance tests: two sweeps over every screen from the owner's 29 Oct first use (docs/screenshots/2026-10-29-first-use).
// REQ-UI-081: no label, menu item, button or placeholder ends in three dots. REQ-UI-082: the buttons the button
// walk found doing nothing (tests/.artifacts/triage/button-walk.md) now do their job, or are gone. Black-box, web harness.
import { Page, expect } from '@playwright/test';
import { writeFileSync, rmSync } from 'fs';
import { join } from 'path';
import { signIn } from './support/auth';
import { test, PROJECT, resetFixture, openProject } from './support/workbench-a';

const ROUTES = [
  '/', '/start?all=1', '/prerequisites', '/repository', '/processes', '/board', '/worktrees', '/integrations',
  '/feedback', '/licence', '/settings/providers', '/settings/routing', '/settings/agents', '/settings/corrections',
  '/settings/measurements', '/settings/appearance',
];
const MENUS = ['file', 'edit', 'view', 'project', 'tools', 'help'];
const DOTS = /(…|\.\.\.)\s*$/;

let page: Page;

test.describe.configure({ mode: 'serial', timeout: 300_000 });

test.beforeAll(async ({ browser }) => {
  page = await browser.newPage();
  resetFixture();
  await signIn(page, 'Owner');
  await openProject(page);
});

test.afterAll(async () => {
  await page.close();
});

/** Every visible label, button, menu item and link text, and every placeholder, that ends in three dots. */
async function dottedTexts(aPage: Page): Promise<string[]> {
  return aPage.evaluate((aDots) => {
    const vDots = new RegExp(aDots);
    const vSeen: string[] = [];
    const vVisible = (e: Element) => (e as HTMLElement).offsetParent !== null || getComputedStyle(e).position === 'fixed';
    for (const vEl of Array.from(document.querySelectorAll('button, a, label, [role=menuitem], [role=option], h1, h2, h3, th'))) {
      const vText = (vEl as HTMLElement).innerText?.trim() ?? '';
      if (vVisible(vEl) && vDots.test(vText)) vSeen.push(vText);
    }
    for (const vEl of Array.from(document.querySelectorAll('input[placeholder], textarea[placeholder]'))) {
      const vText = vEl.getAttribute('placeholder') ?? '';
      if (vVisible(vEl) && vDots.test(vText)) vSeen.push(`placeholder: ${vText}`);
    }
    return vSeen;
  }, DOTS.source);
}

test('REQ-UI-081 no menu, button, label or placeholder on any screen ends in three dots', async () => {
  const vFound: string[] = [];
  for (const vRoute of ROUTES) {
    await page.goto(vRoute, { waitUntil: 'networkidle' });
    await page.waitForTimeout(1500);
    vFound.push(...(await dottedTexts(page)).map((t) => `${vRoute}: ${t}`));
  }
  await page.goto('/', { waitUntil: 'networkidle' });
  for (const vMenu of MENUS) {
    await expect(async () => {
      if (!(await page.getByTestId(`menu-${vMenu}-items`).isVisible())) await page.getByTestId(`menu-${vMenu}`).click();
      await expect(page.getByTestId(`menu-${vMenu}-items`)).toBeVisible({ timeout: 2_500 });
    }).toPass({ timeout: 30_000 });
    vFound.push(...(await dottedTexts(page)).map((t) => `${vMenu} menu: ${t}`));
    await page.keyboard.press('Escape');
  }
  expect(vFound, 'texts ending in three dots').toEqual([]);
});

test('REQ-UI-082 the toolbar Find, once always greyed out, opens Find in files', async () => {
  await page.goto('/', { waitUntil: 'networkidle' });
  await expect(page.getByTestId('find')).toBeEnabled({ timeout: 20_000 });
  await page.getByTestId('find').click();
  await expect(page.getByRole('dialog')).toBeVisible({ timeout: 10_000 });
  await page.keyboard.press('Escape');
});

test('REQ-UI-082 the Files panel Refresh re-reads the folder from disk', async () => {
  const vFile = join(PROJECT, 'sweep-made-outside.txt');
  rmSync(vFile, { force: true });
  await page.goto('/', { waitUntil: 'networkidle' });
  writeFileSync(vFile, 'made outside Chatur\n');
  await page.getByTestId('files-refresh').click();
  await expect(page.getByTestId('file-tree').getByText('sweep-made-outside.txt', { exact: true })).toBeVisible({ timeout: 20_000 });
  rmSync(vFile, { force: true });
});

test('REQ-UI-082 buttons with no job are gone: Repository New branch, Process run Pause', async () => {
  await page.goto('/repository', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);
  await expect(page.getByTestId('new-branch')).toHaveCount(0);
  await page.goto('/processes', { waitUntil: 'networkidle' });
  await expect(page.getByTestId('pause-run')).toHaveCount(0);
});
