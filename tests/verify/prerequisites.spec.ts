// Verify-phase acceptance tests for Prerequisites (Doctor): REQ-FN-011, REQ-UI-017, REQ-UI-018,
// REQ-FN-012, REQ-FN-013. Black-box: the page is driven through the real signed-in UI only.
import { test, expect, Page } from '@playwright/test';
import { readFileSync, mkdirSync, rmSync, writeFileSync } from 'fs';
import { createServer, Server } from 'http';
import * as path from 'path';
import { credentials } from './support/auth';
import { openFoldersDialog } from './support/start';

const REPO = '/mnt/c/1MyCode/Chatur';

// REQ-UI-017: a project whose files make Doctor require a tool this machine really lacks. A .csproj with
// <UseMaui>true</UseMaui> makes Doctor probe the MAUI workload; `dotnet workload list` here lists only
// maui-android, which cannot build Chatur's own desktop heads, so the row is honestly "not found".
const FIXTURE_ROOT = path.resolve('tests/.artifacts/verify/fixtures/prerequisites/root');
const FIXTURE_PROJECT = path.join(FIXTURE_ROOT, 'MauiFixture');

// REQ-FN-013: a local stand-in for GitHub's releases API. The app under test must have been started with
// Chatur__Releases__ApiBaseAddress=http://localhost:<STUB_PORT>/ (the app's own configuration setting).
const RELEASE_STUB_PORT = Number(process.env.CHATUR_RELEASE_STUB_PORT ?? 18952);

// The sign-in form can be submitted natively before Blazor has made it interactive (empty fields,
// URL "/sign-in?"), so wait for the circuit and retry once instead of trusting one click.
async function signInWhenReady(aPage: Page): Promise<void> {
  const { email, password } = credentials('Owner');
  for (let vTry = 0; vTry < 3; vTry++) {
    await aPage.goto('/sign-in', { waitUntil: 'networkidle' });
    await aPage.waitForTimeout(2000);
    await aPage.getByTestId('input-email').fill(email);
    await aPage.getByTestId('input-password').fill(password);
    await aPage.locator('[data-testid=signin-form] button[type=submit]').first().click();
    try {
      await expect(aPage).not.toHaveURL(/\/sign-in/, { timeout: 12_000 });
      return;
    } catch { /* retry */ }
  }
  throw new Error('Could not sign in as Owner after 3 attempts');
}

async function openPrerequisites(aPage: Page): Promise<void> {
  await signInWhenReady(aPage);
  await aPage.goto('/prerequisites', { waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('page-heading')).toBeVisible({ timeout: 30_000 });
  // The probe runs on open; wait until it has either listed tools or shown its honest empty state.
  await expect(aPage.getByText('Checking…')).toHaveCount(0, { timeout: 30_000 });
}

async function openFixtureAsProject(aPage: Page): Promise<void> {
  rmSync(FIXTURE_ROOT, { recursive: true, force: true });
  mkdirSync(FIXTURE_PROJECT, { recursive: true });
  writeFileSync(path.join(FIXTURE_PROJECT, 'MauiFixture.csproj'),
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><UseMaui>true</UseMaui></PropertyGroup></Project>\n');
  // Start is reached on purpose with ?all=1 even while a project is selected (bare /start forwards, REQ-FN-006).
  await aPage.goto('/start?all=1', { waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('recent-side')).toBeVisible();
  const vDialog = await openFoldersDialog(aPage);
  if ((await vDialog.getByText(FIXTURE_ROOT, { exact: true }).count()) === 0) {
    await vDialog.getByTestId('new-folder-path').fill(FIXTURE_ROOT);
    await vDialog.getByTestId('add-folder').click();
    await expect(vDialog.getByText(FIXTURE_ROOT, { exact: true })).toBeVisible();
  }
  await vDialog.getByTestId('folders-dialog-close').click();
  await expect(vDialog).toBeHidden();
  const vRow = aPage.getByTestId(/^project-\d+-name$/).filter({ hasText: /^MauiFixture$/ });
  await expect(vRow).toBeVisible();
  await vRow.click();
  await expect(aPage).not.toHaveURL(/\/start/);
}

type ReleaseStub = { hits: () => number; setRelease: (aTag: string | null) => void; close: () => Promise<void> };

async function startReleaseStub(): Promise<ReleaseStub> {
  let vTag: string | null = null;
  let vHits = 0;
  const vBase = `http://localhost:${RELEASE_STUB_PORT}`;
  const vServer: Server = createServer((aReq, aRes) => {
    if (!/\/releases(\?.*)?$/.test(aReq.url ?? '')) { aRes.writeHead(404); aRes.end(); return; }
    vHits++;
    aRes.writeHead(200, { 'content-type': 'application/json' });
    aRes.end(JSON.stringify(vTag === null ? [] : [{
      tag_name: vTag,
      target_commitish: 'abcdef0123456789abcdef0123456789abcdef01',
      published_at: new Date().toISOString(),
      assets: [
        { name: 'Chatur-mac-arm64.zip', browser_download_url: `${vBase}/download/Chatur-mac-arm64.zip` },
        { name: 'Chatur-win-x64.zip', browser_download_url: `${vBase}/download/Chatur-win-x64.zip` },
      ],
    }]));
  });
  await new Promise<void>((resolve) => vServer.listen(RELEASE_STUB_PORT, resolve));
  return {
    hits: () => vHits,
    setRelease: (aTag) => { vTag = aTag; },
    close: () => new Promise<void>((resolve) => vServer.close(() => resolve())),
  };
}

function toolRows(aPage: Page) {
  return aPage.getByTestId('tools-table').locator('tbody tr');
}

test.describe('Prerequisites', () => {
  test.setTimeout(120_000);
  test('REQ-FN-011 every tool listed with the version found or not found', async ({ page }) => {
    await openPrerequisites(page);
    const vRows = toolRows(page);
    await expect(vRows.first()).toBeVisible({ timeout: 30_000 });
    const vCount = await vRows.count();
    expect(vCount).toBeGreaterThan(0);
    const vNames: string[] = [];
    for (let i = 0; i < vCount; i++) {
      const vCells = vRows.nth(i).locator('td');
      const vName = (await vCells.nth(0).innerText()).trim();
      const vFound = (await vCells.nth(2).innerText()).trim();
      vNames.push(vName);
      expect(vName, `row ${i} names its tool`).not.toBe('');
      expect(vFound, `${vName} shows a version or "not found"`).toMatch(/^(not found|\d+(\.\d+)+\S*)$/);
    }
    // The source tool is always probed (REQ-FN-011 remarks); the stat tile counts every listed tool.
    expect(vNames.map(n => n.toLowerCase())).toContain('git');
    await expect(page.getByTestId('req-tiles')).toContainText(String(vCount));
    await page.screenshot({ path: 'tests/.artifacts/verify/screens/prerequisites-fn011.png' });
  });

  test('REQ-UI-017 a missing tool shows the command that installs it, ready to copy', async ({ page, context }) => {
    await context.grantPermissions(['clipboard-read', 'clipboard-write']).catch(() => undefined);
    await signInWhenReady(page);
    await openFixtureAsProject(page);
    await page.goto('/prerequisites', { waitUntil: 'networkidle' });
    await expect(page.getByTestId('page-heading')).toContainText('MauiFixture');
    await expect(page.getByText('Checking…')).toHaveCount(0, { timeout: 30_000 });

    const vRow = toolRows(page).filter({ has: page.getByText('MAUI workload', { exact: true }) });
    await expect(vRow, 'the MAUI project makes Doctor list the MAUI workload').toHaveCount(1, { timeout: 30_000 });
    const vCells = vRow.locator('td');
    expect((await vCells.nth(2).innerText()).trim()).toBe('not found');
    const vFix = page.getByTestId('copy-fix-maui-workload');
    await expect(vFix).toBeVisible();
    await expect(vFix).toContainText('dotnet workload install maui');
    const vCopy = vFix.locator('button');
    await expect(vCopy).toBeVisible();
    await vCopy.click();
    await expect.poll(() => page.evaluate(() => navigator.clipboard.readText()).catch(() => 'clipboard unreadable'), { timeout: 10_000 })
      .toContain('dotnet workload install maui');
    // Tools the machine does have show no fix command.
    const vRows = toolRows(page);
    for (let i = 0; i < (await vRows.count()); i++) {
      const vC = vRows.nth(i).locator('td');
      if ((await vC.nth(2).innerText()).trim() !== 'not found') {
        expect((await vC.nth(4).innerText()).trim(), 'a ready tool has no fix command').toBe('');
      }
    }
    await page.screenshot({ path: 'tests/.artifacts/verify/screens/prerequisites-ui017.png' });
  });

  test('REQ-UI-018 Doctor shows Chatur own version and the commit the build came from', async ({ page }) => {
    await openPrerequisites(page);
    await expect(page.getByTestId('this-chatur')).toBeVisible();
    const vVersion = (await page.getByTestId('chatur-version').innerText()).trim();
    const vCommit = (await page.getByTestId('chatur-commit').innerText()).trim();

    // Version must match what the build stamps: VersionPrefix in Directory.Build.props.
    const vProps = readFileSync(`${REPO}/Directory.Build.props`, 'utf8');
    const vPrefix = /<VersionPrefix[^>]*>([^<]+)</.exec(vProps)?.[1]?.trim();
    expect(vPrefix, 'Directory.Build.props defines a VersionPrefix').toBeTruthy();
    expect(vVersion).toMatch(/^\d+\.\d+\.\d+/);
    expect(vVersion.startsWith(vPrefix!), `version "${vVersion}" starts with stamped ${vPrefix}`).toBe(true);

    // Commit: short hex sha that is the start of the repository's current commit (read from the ref files).
    expect(vCommit).toMatch(/^[0-9a-f]{7}$/);
    const vHead = readFileSync(`${REPO}/.git/HEAD`, 'utf8').trim();
    const vSha = vHead.startsWith('ref: ')
      ? readFileSync(`${REPO}/.git/${vHead.slice(5).trim()}`, 'utf8').trim()
      : vHead;
    expect(vSha.startsWith(vCommit), `commit ${vCommit} is the build's HEAD ${vSha.slice(0, 7)}`).toBe(true);
    await page.screenshot({ path: 'tests/.artifacts/verify/screens/prerequisites-ui018.png' });
  });

  test('REQ-FN-012 Homebrew tools are found when started from Finder on a Mac', async () => {
    test.skip(true, 'Needs a Mac: Chatur started from Finder, where Homebrew is not on the default PATH');
  });

  test('REQ-FN-013 a newer nightly is offered with a download link for this machine', async ({ page }) => {
    const vStub = await startReleaseStub();
    try {
      // An older release: no offer. Doubles as the check that the app reads the stub.
      vStub.setRelease('v0.0.1-nightly');
      await openPrerequisites(page);
      await expect(page.getByTestId('chatur-version')).toBeVisible();
      test.skip(vStub.hits() === 0,
        `The app was not started with Chatur__Releases__ApiBaseAddress=http://localhost:${RELEASE_STUB_PORT}/, so the stub feed was never asked`);
      await expect(page.getByTestId('newer-build')).toHaveCount(0);
      await expect(page.getByTestId('download-build')).toHaveCount(0);

      // A newer release with an asset per machine: Doctor says so and links the asset for this machine.
      vStub.setRelease('v99.0.0-nightly');
      await page.getByTestId('check-again').click();
      const vBanner = page.getByTestId('newer-build');
      await expect(vBanner).toBeVisible({ timeout: 30_000 });
      await expect(vBanner).toContainText('99.0.0');
      await expect(vBanner).toContainText('abcdef0');
      const vOnMac = process.platform === 'darwin'; // the app under test runs on this machine
      await expect(page.getByTestId('download-build'))
        .toHaveAttribute('href', `http://localhost:${RELEASE_STUB_PORT}/download/${vOnMac ? 'Chatur-mac-arm64.zip' : 'Chatur-win-x64.zip'}`);
      await page.screenshot({ path: 'tests/.artifacts/verify/screens/prerequisites-fn013.png' });
    } finally {
      await vStub.close();
    }
  });
});
