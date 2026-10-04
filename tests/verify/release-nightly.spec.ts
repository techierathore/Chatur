// Acceptance test: REQ-NFR-006 — the published nightly, not a source build. Black-box over CDP.
// The verifier downloads the `v<version>-nightly` Windows zip from the GitHub release, starts its
// Chatur.exe with WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS=--remote-debugging-port=<n>, and passes:
//   NIGHTLY_CDP     the DevTools address of that running Chatur.exe (through tf-cdp-relay.ps1)
//   NIGHTLY_VERSION the release's version (e.g. 0.1.0-nightly)
//   NIGHTLY_COMMIT  the commit the release was built from (full sha)
// Without NIGHTLY_CDP there is no published build to drive, and the test skips with that reason.
import { test, expect, chromium } from '@playwright/test';
import { readFileSync } from 'fs';

const CDP = process.env.NIGHTLY_CDP ?? '';
const VERSION = process.env.NIGHTLY_VERSION ?? '';
const COMMIT = process.env.NIGHTLY_COMMIT ?? '';

test.setTimeout(180_000);

test('REQ-NFR-006 the published nightly opens and Prerequisites shows the release version and commit', async () => {
  test.skip(!CDP, 'No published nightly is running: set NIGHTLY_CDP to the DevTools address of the downloaded Chatur.exe.');
  const vBrowser = await chromium.connectOverCDP(CDP);
  try {
    const vPage = vBrowser.contexts()[0].pages().find((p) => !p.url().startsWith('devtools')) ?? vBrowser.contexts()[0].pages()[0];
    // The app must get past its loading screen to either Sign in or a signed-in screen.
    await expect(vPage.locator('body'), 'the published build leaves "Loading Chatur…"').not.toHaveText(/^\s*Loading Chatur…\s*$/, { timeout: 60_000 });
    if ((await vPage.locator('input[type=password]').count()) > 0) {
      const vSecrets = JSON.parse(readFileSync(process.env.CHATUR_SECRETS_FILE ?? '', 'utf8'));
      await vPage.locator('input[type=email], input[name*=mail i]').first().fill('chatur-owner@techierathore.com');
      await vPage.locator('input[type=password]').first().fill(vSecrets['Chatur:TestUsers:Owner']);
      await vPage.locator('button[type=submit]').first().click();
    }
    await vPage.evaluate(() => (window as unknown as { Blazor: { navigateTo: (u: string) => void } }).Blazor.navigateTo('/prerequisites'));
    await expect(vPage.getByTestId('chatur-version')).toContainText(VERSION, { timeout: 30_000 });
    await expect(vPage.getByTestId('chatur-commit')).toContainText(COMMIT.slice(0, 7), { timeout: 30_000 });
  } finally {
    await vBrowser.close();
  }
});
