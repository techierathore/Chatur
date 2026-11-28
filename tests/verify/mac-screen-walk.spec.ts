// The Mac screen walk (27 Nov 2026, the owner's first night on a Mac). Every screen of the web harness, light and
// dark, at desktop (1440x900) and phone (390x844) width, signed in through the local App Manager stand-in
// (support/appmanager-standin.py) as a made-up user. Run it with: bash tests/verify/support/mac-walk.sh
// Each shot lands in tests/.artifacts/mac-walk/<screen>-<theme>-<width>.png; every finding in results.json.
// Black-box: it reads only what a person would see, plus the browser console and network.
import { test, expect, Page, ConsoleMessage, Request, Response } from '@playwright/test';
import { mkdirSync, writeFileSync } from 'fs';
import * as path from 'path';

const OUT = path.resolve(__dirname, '..', '.artifacts', 'mac-walk');
const EMAIL = 'mac-walk@example.invalid';
// Any password: the stand-in accepts every one, and this one is never sent anywhere else.
const PASSWORD = 'Stand-in-only-1!';

type Theme = 'light' | 'dark';
interface Width { name: 'desktop' | 'phone'; width: number; height: number }
const WIDTHS: Width[] = [
  { name: 'desktop', width: 1440, height: 900 },
  { name: 'phone', width: 390, height: 844 },
];
const THEMES: Theme[] = ['light', 'dark'];

interface Screen {
  name: string;
  route: string;
  /** Something done after the route settles (open a menu, a dialog). */
  act?: (aPage: Page) => Promise<void>;
  desktopOnly?: boolean;
}

async function openMenu(aPage: Page, aMenu: string): Promise<void> {
  await expect(async () => {
    if (!(await aPage.getByTestId(`menu-${aMenu}-items`).isVisible())) await aPage.getByTestId(`menu-${aMenu}`).click();
    await expect(aPage.getByTestId(`menu-${aMenu}-items`)).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 20_000 });
}

const SIGNED_OUT: Screen[] = [
  { name: 'sign-in', route: '/sign-in' },
  { name: 'register', route: '/register' },
  {
    name: 'forgot-password',
    route: '/sign-in',
    act: async (aPage) => {
      await aPage.getByTestId('input-email').fill(EMAIL);
      await aPage.getByTestId('forgot-password').click();
      await aPage.waitForTimeout(800);
    },
  },
];

const SIGNED_IN: Screen[] = [
  { name: 'start', route: '/start' },
  { name: 'start-all', route: '/start?all=1' },
  {
    name: 'start-folders-dialog',
    route: '/start?all=1',
    act: async (aPage) => {
      await expect(async () => {
        if (!(await aPage.getByTestId('folders-dialog').isVisible())) await aPage.getByTestId('manage-folders').click();
        await expect(aPage.getByTestId('folders-dialog')).toBeVisible({ timeout: 2_500 });
      }).toPass({ timeout: 20_000 });
    },
  },
  { name: 'main', route: '/' },
  ...['file', 'edit', 'view', 'project', 'tools', 'help'].map<Screen>((aMenu) => ({
    name: `main-menu-${aMenu}`,
    route: '/',
    desktopOnly: true,
    act: (aPage) => openMenu(aPage, aMenu),
  })),
  {
    name: 'main-account-menu',
    route: '/',
    desktopOnly: true,
    act: async (aPage) => {
      await aPage.locator('[data-testid=account] button').first().click();
      await expect(aPage.getByTestId('account-pop')).toBeVisible({ timeout: 10_000 });
    },
  },
  { name: 'board', route: '/board' },
  { name: 'auto', route: '/auto' },
  { name: 'auto-queue', route: '/auto/queue' },
  { name: 'auto-report', route: '/auto/report' },
  { name: 'processes', route: '/processes' },
  { name: 'process-run', route: '/processes/1/run' },
  { name: 'workspace-analyst', route: '/workspace/analyst' },
  { name: 'prerequisites', route: '/prerequisites' },
  { name: 'repository', route: '/repository' },
  { name: 'worktrees', route: '/worktrees' },
  { name: 'integrations', route: '/integrations' },
  { name: 'all-projects', route: '/all-projects' },
  { name: 'compare-models', route: '/compare-models' },
  { name: 'measurements', route: '/measurements' },
  { name: 'measurements-all', route: '/measurements/all' },
  { name: 'feedback', route: '/feedback' },
  { name: 'licence', route: '/licence' },
  { name: 'settings-account', route: '/settings/account' },
  { name: 'settings-providers', route: '/settings/providers' },
  { name: 'settings-routing', route: '/settings/routing' },
  { name: 'settings-agents', route: '/settings/agents' },
  { name: 'settings-corrections', route: '/settings/corrections' },
  { name: 'settings-measurements', route: '/settings/measurements' },
  { name: 'settings-appearance', route: '/settings/appearance' },
];

interface Finding { screen: string; theme: Theme; width: string; url: string; shot: string; problems: string[]; notes: string[] }
const RESULTS: Finding[] = [];

let objConsole: string[] = [];
let objFailed: string[] = [];

function watch(aPage: Page): void {
  aPage.on('console', (aMsg: ConsoleMessage) => {
    if (aMsg.type() === 'error') objConsole.push(aMsg.text().slice(0, 300));
  });
  aPage.on('pageerror', (aError) => objConsole.push(`pageerror: ${aError.message.slice(0, 300)}`));
  aPage.on('requestfailed', (aRequest: Request) => {
    const vReason = aRequest.failure()?.errorText ?? '';
    // A navigation away cancels the page's open requests; that is not a fault.
    if (!/ERR_ABORTED/.test(vReason)) objFailed.push(`${aRequest.method()} ${aRequest.url()} ${vReason}`);
  });
  aPage.on('response', (aResponse: Response) => {
    if (aResponse.status() >= 400) objFailed.push(`${aResponse.status()} ${aResponse.request().method()} ${aResponse.url()}`);
  });
}

/** Sets light or dark with the screen's own toggle (the harness keeps the choice), and the browser's preference to match. */
async function setTheme(aPage: Page, aTheme: Theme): Promise<void> {
  await aPage.emulateMedia({ colorScheme: aTheme });
  const vIsDark = () => aPage.evaluate(() => document.documentElement.classList.contains('dark'));
  for (let vTry = 0; vTry < 4 && (await vIsDark()) !== (aTheme === 'dark'); vTry++) {
    const vToggle = aPage.getByTestId('theme-toggle').first();
    if (await vToggle.isVisible().catch(() => false)) {
      await vToggle.click();
    }
    await aPage.waitForTimeout(700);
  }
}

async function settle(aPage: Page): Promise<void> {
  await aPage.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => undefined);
  await aPage.waitForTimeout(1_200);
}

/** What the page shows that can go wrong on a Mac, read in the page. */
async function inspect(aPage: Page, aTheme: Theme) {
  return aPage.evaluate((aDark) => {
    const vVisible = (aEl: Element) => {
      const vRect = (aEl as HTMLElement).getBoundingClientRect();
      const vStyle = getComputedStyle(aEl);
      return vRect.width > 0 && vRect.height > 0 && vStyle.visibility !== 'hidden' && vStyle.display !== 'none' && Number(vStyle.opacity) > 0;
    };
    const vText = document.body.innerText ?? '';
    const vSheets = Array.from(document.styleSheets).length;
    const vBodyFont = getComputedStyle(document.body).fontFamily;
    const vHtmlDark = document.documentElement.classList.contains('dark');

    // Platform words: Windows paths and Windows-only keys where a Mac person reads them.
    const vPlatform: string[] = [];
    const vWinPath = /[A-Za-z]:\\[^\s]*|\\Users\\|\\AppData\\|%LOCALAPPDATA%|%APPDATA%|\.exe\b/g;
    for (const vMatch of vText.match(vWinPath) ?? []) vPlatform.push(`windows path: ${vMatch}`);
    const vMacBackslash = /\/(Users|Volumes)\/[^\s]*\\[^\s]*/g;
    for (const vMatch of vText.match(vMacBackslash) ?? []) vPlatform.push(`mac path with backslash: ${vMatch}`);
    for (const vMatch of vText.match(/\b(Ctrl|Control)\s*\+\s*[\w,.]+|\bAlt\s*\+\s*\w+|\bWin\s*\+\s*\w+|Windows key|File Explorer|Explorer\b|PowerShell|cmd\.exe|winget|Program Files/g) ?? [])
      vPlatform.push(`windows word: ${vMatch}`);
    for (const vEl of Array.from(document.querySelectorAll('input[placeholder], textarea[placeholder]'))) {
      const vHolder = vEl.getAttribute('placeholder') ?? '';
      if (/[A-Za-z]:\\|\\Users\\|Ctrl\+/.test(vHolder)) vPlatform.push(`placeholder: ${vHolder}`);
    }

    // Clipped text: a visible element with its own text whose content is wider than its box, cut without ellipsis.
    const vClipped: string[] = [];
    // Contrast: a visible element with its own text against the first opaque background behind it.
    const vLow: string[] = [];
    // The theme colours are oklch(); a 1x1 canvas turns any CSS colour into sRGB bytes.
    const vCanvas = document.createElement('canvas');
    vCanvas.width = 1;
    vCanvas.height = 1;
    const vPaint = vCanvas.getContext('2d', { willReadFrequently: true })!;
    const vRgb = (aColour: string) => {
      if (!aColour || aColour === 'transparent') return null;
      if (!/^rgba?\(/.test(aColour)) {
        const vAlpha = aColour.match(/\/\s*([\d.]+%?)\s*\)$/);
        vPaint.clearRect(0, 0, 1, 1);
        vPaint.fillStyle = '#000';
        vPaint.fillStyle = aColour;
        vPaint.fillRect(0, 0, 1, 1);
        const [r, g, b] = Array.from(vPaint.getImageData(0, 0, 1, 1).data);
        const vA = vAlpha ? (vAlpha[1].endsWith('%') ? parseFloat(vAlpha[1]) / 100 : parseFloat(vAlpha[1])) : 1;
        return { r, g, b, a: vA };
      }
      const vM = aColour.match(/rgba?\(([^)]+)\)/);
      if (!vM) return null;
      const vP = vM[1].split(/[ ,/]+/).filter(Boolean).map(Number);
      return { r: vP[0], g: vP[1], b: vP[2], a: vP.length > 3 ? vP[3] : 1 };
    };
    const vLum = (c: { r: number; g: number; b: number }) => {
      const f = (v: number) => { v /= 255; return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4); };
      return 0.2126 * f(c.r) + 0.7152 * f(c.g) + 0.0722 * f(c.b);
    };
    const vBackground = (aEl: Element | null) => {
      for (let vEl = aEl; vEl; vEl = vEl.parentElement) {
        const vC = vRgb(getComputedStyle(vEl).backgroundColor);
        if (vC && vC.a > 0.85) return vC;
      }
      return vRgb(getComputedStyle(document.body).backgroundColor) ?? { r: 255, g: 255, b: 255, a: 1 };
    };
    for (const vEl of Array.from(document.body.querySelectorAll('*'))) {
      const vOwn = Array.from(vEl.childNodes).filter((n) => n.nodeType === 3).map((n) => n.textContent ?? '').join('').trim();
      if (!vOwn || !vVisible(vEl) || vEl.closest('[aria-hidden=true]')) continue;
      const vStyle = getComputedStyle(vEl);
      const vHtml = vEl as HTMLElement;
      // A 1px box is a screen-reader-only label, not clipped text.
      if (vHtml.clientWidth > 2 && vHtml.scrollWidth > vHtml.clientWidth + 2 &&vStyle.overflowX !== 'visible' && vStyle.textOverflow !== 'ellipsis' && vClipped.length < 12)
        vClipped.push(`${vEl.tagName.toLowerCase()}${vHtml.dataset.testid ? `[${vHtml.dataset.testid}]` : ''} "${vOwn.slice(0, 50)}" ${vHtml.scrollWidth}>${vHtml.clientWidth}`);
      const vFg = vRgb(vStyle.color);
      if (!vFg || vFg.a < 0.3 || (vEl as HTMLButtonElement).disabled || vEl.closest('[disabled],[aria-disabled=true],[data-disabled]')) continue;
      const vBg = vBackground(vEl);
      const vL1 = vLum(vFg), vL2 = vLum(vBg);
      const vRatio = (Math.max(vL1, vL2) + 0.05) / (Math.min(vL1, vL2) + 0.05);
      const vLarge = parseFloat(vStyle.fontSize) >= 18.5 || (parseFloat(vStyle.fontSize) >= 14 && Number(vStyle.fontWeight) >= 700);
      if (vRatio < (vLarge ? 3 : 4.5) && vLow.length < 12)
        vLow.push(`${vRatio.toFixed(2)} "${vOwn.slice(0, 40)}" ${vStyle.color} on rgb(${vBg.r},${vBg.g},${vBg.b})`);
    }
    return {
      scrollWidth: document.documentElement.scrollWidth,
      innerWidth: window.innerWidth,
      textLength: vText.trim().length,
      sheets: vSheets,
      bodyFont: vBodyFont,
      bodyBg: getComputedStyle(document.body).backgroundColor,
      htmlDark: vHtmlDark,
      wantDark: aDark,
      platform: vPlatform,
      clipped: vClipped,
      lowContrast: vLow,
    };
  }, aTheme === 'dark');
}

async function walk(aPage: Page, aScreen: Screen, aTheme: Theme, aWidth: Width): Promise<void> {
  objConsole = [];
  objFailed = [];
  await aPage.goto(aScreen.route, { waitUntil: 'domcontentloaded' });
  await settle(aPage);
  await setTheme(aPage, aTheme);
  if (aScreen.act) await aScreen.act(aPage).catch((aError) => objConsole.push(`walk could not act: ${String(aError).slice(0, 200)}`));
  await aPage.waitForTimeout(500);
  const vShot = path.join(OUT, `${aScreen.name}-${aTheme}-${aWidth.name}.png`);
  await aPage.screenshot({ path: vShot, fullPage: false });
  const vSeen = await inspect(aPage, aTheme);
  const vProblems: string[] = [];
  const vNotes: string[] = [];
  if (objConsole.length) vProblems.push(...objConsole.map((c) => `console: ${c}`));
  if (objFailed.length) vProblems.push(...objFailed.map((f) => `request: ${f}`));
  if (vSeen.scrollWidth > vSeen.innerWidth) (aWidth.name === 'desktop' ? vProblems : vNotes).push(`horizontal overflow: scrollWidth ${vSeen.scrollWidth} > ${vSeen.innerWidth}`);
  if (vSeen.textLength < 20) vProblems.push(`almost no visible text (${vSeen.textLength} chars)`);
  if (vSeen.sheets < 3 || /Times/.test(vSeen.bodyFont)) vProblems.push(`unstyled: ${vSeen.sheets} sheets, font ${vSeen.bodyFont}`);
  if (vSeen.htmlDark !== vSeen.wantDark) vProblems.push(`theme did not switch to ${aTheme}`);
  vProblems.push(...vSeen.platform);
  // 27 Nov 2026: on a Mac a MAUI project needs Xcode, and Prerequisites must say whether it is there.
  if (aScreen.name === 'prerequisites' && process.platform === 'darwin' && !(await aPage.getByText('Xcode', { exact: true }).count()))
    vProblems.push('prerequisites: no Xcode row for the MAUI fixture project on a Mac');
  vNotes.push(...vSeen.clipped.map((c) => `clipped: ${c}`));
  vNotes.push(...vSeen.lowContrast.map((c) => `low contrast: ${c}`));
  RESULTS.push({ screen: aScreen.name, theme: aTheme, width: aWidth.name, url: aPage.url(), shot: path.relative(OUT, vShot), problems: vProblems, notes: vNotes });
  writeFileSync(path.join(OUT, 'results.json'), JSON.stringify(RESULTS, null, 2));
  expect.soft(vProblems, `${aScreen.name} ${aTheme} ${aWidth.name}`).toEqual([]);
}

async function signIn(aPage: Page): Promise<void> {
  for (let vAttempt = 1; vAttempt <= 3; vAttempt++) {
    await aPage.goto('/sign-in', { waitUntil: 'networkidle' });
    if (!/\/sign-in/.test(aPage.url())) return;
    await aPage.waitForTimeout(1_500 * vAttempt); // let the circuit attach before typing
    await aPage.getByTestId('input-email').fill(EMAIL);
    await aPage.getByTestId('input-password').fill(PASSWORD);
    const vYes = aPage.getByTestId('remember-yes');
    if (await vYes.isVisible().catch(() => false)) await vYes.click();
    await aPage.getByTestId('signin-submit').click();
    try {
      await expect(aPage).not.toHaveURL(/\/sign-in/, { timeout: 30_000 });
      return;
    } catch {
      // the form posted before Blazor was interactive; try again
    }
  }
  throw new Error('Could not sign in against the App Manager stand-in.');
}

async function signOut(aPage: Page): Promise<void> {
  await aPage.goto('/', { waitUntil: 'networkidle' });
  if (/\/sign-in/.test(aPage.url())) return;
  await aPage.locator('[data-testid=account] button').first().click();
  await aPage.getByTestId('sign-out').click();
  await expect(aPage).toHaveURL(/\/sign-in/, { timeout: 30_000 });
}

test.describe.configure({ mode: 'serial' });

test('Mac screen walk: every screen, light and dark, desktop and phone', async ({ browser }) => {
  mkdirSync(OUT, { recursive: true });
  const vContext = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const vPage = await vContext.newPage();
  watch(vPage);

  await vPage.goto('/sign-in', { waitUntil: 'networkidle' });
  await signOut(vPage);
  for (const vWidth of WIDTHS) {
    await vPage.setViewportSize({ width: vWidth.width, height: vWidth.height });
    for (const vTheme of THEMES) {
      for (const vScreen of SIGNED_OUT) {
        if (vScreen.desktopOnly && vWidth.name !== 'desktop') continue;
        await walk(vPage, vScreen, vTheme, vWidth);
      }
    }
  }

  await vPage.setViewportSize({ width: 1440, height: 900 });
  await signIn(vPage);
  for (const vWidth of WIDTHS) {
    await vPage.setViewportSize({ width: vWidth.width, height: vWidth.height });
    for (const vTheme of THEMES) {
      for (const vScreen of SIGNED_IN) {
        if (vScreen.desktopOnly && vWidth.name !== 'desktop') continue;
        await walk(vPage, vScreen, vTheme, vWidth);
      }
    }
  }
  await vContext.close();
});
