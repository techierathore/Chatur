import { defineConfig } from '@playwright/test';

// The Mac screen walk only (tests/verify/support/mac-walk.sh starts the harness on 9244 against a local App Manager
// stand-in). Its own config: no global cleanup (that sweeps the real test accounts), no test-settings.json.
export default defineConfig({
  testDir: './',
  testMatch: 'mac-screen-walk.spec.ts',
  outputDir: '../.artifacts/mac-walk/test-results',
  reporter: 'line',
  workers: 1,
  timeout: 1_800_000,
  use: { baseURL: 'http://localhost:9244', headless: true, screenshot: 'off', trace: 'off' },
});
