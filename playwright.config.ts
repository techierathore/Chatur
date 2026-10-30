import { defineConfig } from '@playwright/test';

// CHATUR_WSL_HARNESS_PORT=<port>: Playwright itself runs the harness copy that tf-verify-boot.sh published to
// tests/.artifacts/verify/run-<port>/, under WSL dotnet, for the length of the test run (TF-018: the Windows-side
// harness cannot read the WSL fixture paths the tests type). Its output is added to app-<port>.log, which the
// specs that check a logged launch read. Unset, the tests use the app already booted at BASE_URL.
const WSL_PORT = process.env.CHATUR_WSL_HARNESS_PORT;
const RUN = `${process.cwd()}/tests/.artifacts/verify/run-${WSL_PORT}`;

export default defineConfig({
  testDir: './tests/verify',
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
