import { defineConfig } from '@playwright/test';
export default defineConfig({
  testDir: './tests/verify',
  outputDir: './tests/.artifacts/test-results',
  reporter: 'line',
  // One worker: the web harness keeps one server-side selected project and session store, so specs
  // running side by side flip each other's state (verify-phase 2026-09-30).
  workers: 1,
  use: { baseURL: process.env.BASE_URL, headless: true, screenshot: 'only-on-failure', trace: 'retain-on-failure' },
});
