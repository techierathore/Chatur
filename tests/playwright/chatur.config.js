// Minimal Playwright config for Chatur's browser checks (Architecture §1 Q6). Points at
// Chatur.WebHarness, started separately by bash .tfcore/utils/tf-verify-boot.sh — this config never
// starts the server itself, so the same run can attach to a harness already booted for a screenshot.
// @ts-check
const { defineConfig } = require('@playwright/test');

module.exports = defineConfig({
  testDir: '.',
  timeout: 30_000,
  use: {
    baseURL: 'http://localhost:5280',
    trace: 'retain-on-failure'
  }
});
