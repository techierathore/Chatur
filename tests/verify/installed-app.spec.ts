// REQ-NFR-012 (owner's evening test of 21 Nov 2026, point 6): Chatur installed for the owner on Windows by
// scripts/install-windows.ps1. Checks the install folder and its Start menu entry "Chatur", then starts the installed
// app FROM THAT ENTRY, signs in as the Owner test user and quits it. That start keeps its data in a test folder under
// this test project's own bin folder, deleted when the test ends: the app's own test switch (CHATUR_TEST_DATA, the
// guard of REQ-NFR-010) and the web view's DevTools port are set for that one start only, so the owner's data, his
// remembered sign-in and his chats are never opened. Windows only; skipped elsewhere.
import { test, expect, chromium } from '@playwright/test';
import { execFileSync, spawn, ChildProcess } from 'child_process';
import { existsSync, rmSync } from 'fs';
import * as path from 'path';
import { credentials } from './support/auth';
import { scratchDir } from './support/cleanup';
import { windowsPath } from './support/run-proof';

const ON_WINDOWS_HOST = existsSync('/mnt/c/Windows') || process.platform === 'win32';
test.skip(!ON_WINDOWS_HOST, 'the installed app is a Windows install');
test.describe.configure({ mode: 'serial', timeout: 300_000 });

const DEVTOOLS_PORT = 29299;
const RELAY_PORT = 9299;

/** Runs one PowerShell command and returns what it printed; a command that finds nothing prints nothing. */
function ps(aCommand: string): string {
  try {
    return execFileSync('powershell.exe', ['-NoProfile', '-Command', aCommand], { encoding: 'utf8', timeout: 60_000 }).trim();
  } catch (aError) {
    return String((aError as { stdout?: string }).stdout ?? '').trim();
  }
}

/** The install folder and the Start menu entry, as Windows names them for this user. */
function installPaths(): { folder: string; exe: string; shortcut: string } {
  const vFolder = ps("Join-Path $env:LOCALAPPDATA 'Programs\\Chatur'");
  const vShortcut = ps("Join-Path ([Environment]::GetFolderPath('Programs')) 'Chatur.lnk'");
  return { folder: vFolder, exe: `${vFolder}\\Chatur.exe`, shortcut: vShortcut };
}

test('REQ-NFR-012 Chatur is installed under the user\'s Programs folder with a Start menu entry "Chatur" that starts it', async () => {
  const vPaths = installPaths();
  expect(ps(`Test-Path -LiteralPath '${vPaths.exe}'`)).toBe('True');
  expect(ps(`Test-Path -LiteralPath '${vPaths.shortcut}'`)).toBe('True');
  const vTarget = ps(`(New-Object -ComObject WScript.Shell).CreateShortcut('${vPaths.shortcut}').TargetPath`);
  expect(vTarget.toLowerCase()).toBe(vPaths.exe.toLowerCase());
  // A self-contained, unpackaged build: the .NET runtime and the Windows App SDK travel with it.
  expect(ps(`Test-Path -LiteralPath '${vPaths.folder}\\coreclr.dll'`)).toBe('True');
  expect(ps(`Test-Path -LiteralPath '${vPaths.folder}\\Microsoft.WindowsAppRuntime.Bootstrap.dll'`)).toBe('True');
});

test('REQ-NFR-012 the installed Chatur opens from its Start menu entry, a test user signs in, and it quits', async () => {
  const vPaths = installPaths();
  const vData = scratchDir(`installed-${Date.now()}`);
  const vRelayScript = windowsPath(path.resolve('.tfcore/utils/tf-cdp-relay.ps1'));
  let vRelay: ChildProcess | undefined;
  const vBefore = new Set(ps("Get-Process -Name Chatur -ErrorAction SilentlyContinue | ForEach-Object { $_.Id }").split(/\s+/).filter(Boolean));
  let vMine: string[] = [];
  try {
    // Started through the Start menu entry itself; the two settings apply to this start only.
    // The Start menu entry is opened as Explorer opens it (Start-Process on the .lnk), from a PowerShell whose two
    // settings the started app inherits; detached, so nothing here waits on the app.
    const vData_ = windowsPath(vData).replace(/'/g, "''");
    spawn('powershell.exe', ['-NoProfile', '-Command',
      `$env:CHATUR_TEST_DATA='${vData_}'; $env:WEBVIEW2_USER_DATA_FOLDER='${vData_}\\webview'; $env:WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS='--remote-debugging-port=${DEVTOOLS_PORT}'; Start-Process -FilePath '${vPaths.shortcut.replace(/'/g, "''")}'`],
      { stdio: 'ignore', detached: true }).unref();
    vRelay = spawn('powershell.exe', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', vRelayScript, '-ListenPort', String(RELAY_PORT), '-TargetPort', String(DEVTOOLS_PORT)], { stdio: 'ignore' });

    await expect.poll(async () => {
      try {
        return (await fetch(`http://localhost:${RELAY_PORT}/json/version`)).ok;
      } catch {
        return false;
      }
    }, { timeout: 120_000, intervals: [2_000] }).toBe(true);
    vMine = ps("Get-Process -Name Chatur -ErrorAction SilentlyContinue | ForEach-Object { $_.Id }").split(/\s+/).filter((i) => i && !vBefore.has(i));
    expect(vMine.length, 'a Chatur process started from the Start menu entry').toBeGreaterThan(0);
    const vExePath = ps(`(Get-Process -Id ${vMine[0]}).Path`);
    expect(vExePath.toLowerCase()).toBe(vPaths.exe.toLowerCase());

    const vBrowser = await chromium.connectOverCDP(`http://localhost:${RELAY_PORT}`);
    const vPage = vBrowser.contexts().flatMap((c) => c.pages())[0];
    const vEmail = vPage.getByTestId('input-email');
    // A test start remembers its own sign-in (under "test." names, never the owner's); signing out first makes this a
    // real sign-in.
    const vAccount = vPage.locator('[data-testid=account] button').first();
    await expect(vEmail.or(vAccount)).toBeVisible({ timeout: 60_000 });
    if (!(await vEmail.isVisible())) {
      await vAccount.click();
      await vPage.getByTestId('sign-out').click();
    }
    await expect.poll(() => vBrowser.contexts().flatMap((c) => c.pages()).some((p) => /\/sign-in/.test(p.url())), { timeout: 60_000 }).toBe(true);
    const vSignIn = vBrowser.contexts().flatMap((c) => c.pages()).find((p) => /\/sign-in/.test(p.url()))!;
    await expect(vSignIn.getByTestId('input-email')).toBeVisible({ timeout: 60_000 });
    const { email, password } = credentials('Owner');
    await vSignIn.getByTestId('input-email').fill(email);
    await vSignIn.getByTestId('input-password').fill(password);
    await vSignIn.locator('[data-testid=signin-form] button[type=submit]').first().click();
    // A new test data folder has no project yet: signing in lands on Start, empty, with its ways in.
    await expect.poll(() => vBrowser.contexts().flatMap((c) => c.pages()).some((p) => /\/start/.test(p.url())), { timeout: 90_000 }).toBe(true);
    const vStart = vBrowser.contexts().flatMap((c) => c.pages()).find((p) => /\/start/.test(p.url()))!;
    await expect(vStart.getByTestId('empty-start').or(vStart.getByTestId('recent-side'))).toBeVisible({ timeout: 90_000 });
    await vStart.screenshot({ path: 'tests/.artifacts/verify/shots/REQ-NFR-012-installed-start.png' });
    await vBrowser.close();

    // Quit as the window's close button does.
    for (const vId of vMine) ps(`$p = Get-Process -Id ${vId} -ErrorAction SilentlyContinue; if ($p) { [void]$p.CloseMainWindow() }`);
    await expect.poll(() => vMine.filter((i) => ps(`if (Get-Process -Id ${i} -ErrorAction SilentlyContinue) { 'up' } else { 'gone' }`) === 'up').length,
      { timeout: 60_000, intervals: [2_000] }).toBe(0);
  } finally {
    for (const vId of vMine) ps(`Stop-Process -Id ${vId} -Force -ErrorAction SilentlyContinue`);
    vRelay?.kill();
    ps(`Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -and $_.CommandLine.Contains('-ListenPort ${RELAY_PORT}') } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }`);
    rmSync(vData, { recursive: true, force: true, maxRetries: 10, retryDelay: 1_000 });
  }
});
