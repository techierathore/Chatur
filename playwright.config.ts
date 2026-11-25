import { defineConfig } from '@playwright/test';
import { existsSync, readFileSync } from 'fs';

// The app's address comes from the JSON file beside the tests (owner rule, 2026-11-04 and 2026-11-20: never an
// environment variable): tests/verify/testsettings.Local.json, Verify:BaseUrl, which tf-verify-tests.sh writes for
// the run. The specs read it as process.env.BASE_URL, so it is handed to them from here.
const LOCAL_SETTINGS = `${process.cwd()}/tests/verify/testsettings.Local.json`;
let objWslPort: string | undefined;
if (existsSync(LOCAL_SETTINGS)) {
  try {
    const vVerify = JSON.parse(readFileSync(LOCAL_SETTINGS, 'utf8'))?.Verify;
    if (typeof vVerify?.BaseUrl === 'string' && vVerify.BaseUrl.length > 0) process.env.BASE_URL = vVerify.BaseUrl;
    // a desktop head is reached over its DevTools address (desktop-head.spec.ts)
    if (typeof vVerify?.CdpUrl === 'string' && vVerify.CdpUrl.length > 0) process.env.CDP_URL = vVerify.CdpUrl;
    if (vVerify?.WslHarnessPort) objWslPort = String(vVerify.WslHarnessPort);
  } catch {
    // a half-written settings file leaves the address as tf-verify-tests.sh passed it
  }
}

// Verify:WslHarnessPort in the same JSON file (owner, 22 Nov 2026: test settings come from the JSON file beside the
// tests, never an environment variable): Playwright itself runs the harness copy that tf-verify-boot.sh published to
// tests/.artifacts/verify/run-<port>/, under WSL dotnet, for the length of the test run (TF-018: the Windows-side
// harness cannot read the WSL fixture paths the tests type). Its output is added to app-<port>.log, which the
// specs that check a logged launch read. Absent, the tests use the app already booted at BASE_URL.
const WSL_PORT = objWslPort;
const RUN = `${process.cwd()}/tests/.artifacts/verify/run-${WSL_PORT}`;

export default defineConfig({
  testDir: './tests/verify',
  // Every run starts and ends with the test accounts holding no project an earlier test left behind (owner, 20 Nov 2026).
  globalSetup: './tests/verify/support/global-cleanup.ts',
  globalTeardown: './tests/verify/support/global-cleanup.ts',
  outputDir: './tests/.artifacts/test-results',
  reporter: 'line',
  // One worker: the web harness keeps one server-side selected project and session store, so specs
  // running side by side flip each other's state (verify-phase 2026-09-30).
  workers: 1,
  use: { baseURL: process.env.BASE_URL, headless: true, screenshot: 'only-on-failure', trace: 'retain-on-failure' },
  webServer: WSL_PORT
    ? {
        command: `cd ${RUN} && ASPNETCORE_ENVIRONMENT=Development ${process.env.HOME}/.dotnet/dotnet Chatur.WebHarness.dll --urls http://localhost:${WSL_PORT} --contentRoot ${RUN} 2>&1 | tee -a ${process.cwd()}/tests/.artifacts/verify/app-${WSL_PORT}.log`,
        url: `http://localhost:${WSL_PORT}/healthz`,
        reuseExistingServer: false,
        timeout: 120_000,
      }
    : undefined,
});
