// Acceptance check for REQ-FN-040: running an agent session in a project writes the five
// docs/metrics files, and the sessions file carries Chatur's signature. With no provider configured
// (TechieRag gap TR-RAG-003) the honest "no model provider" reply is the turn, and it still counts
// as a session that ran.
import { test, expect } from '@playwright/test';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'fs';
import { resolve } from 'path';
import { signIn } from './support/auth';
import { openFoldersDialog } from './support/start';

test.describe.configure({ timeout: 180_000 });

const FIXTURE_ROOT = resolve(__dirname, '../.artifacts/verify/fixtures/metrics/root');
const FIXTURE_PROJECT = 'metrics-a';
const METRICS = resolve(FIXTURE_ROOT, FIXTURE_PROJECT, 'docs', 'metrics');

test('REQ-FN-040 a session turn writes the five metrics files and a chatur-signed sessions line', async ({ page }) => {
  mkdirSync(resolve(FIXTURE_ROOT, FIXTURE_PROJECT), { recursive: true });
  writeFileSync(resolve(FIXTURE_ROOT, FIXTURE_PROJECT, 'notes.txt'), 'hello = 1\n');

  await signIn(page, 'Second');
  await page.goto('/start?all=1', { waitUntil: 'networkidle' });
  const vList = page.getByTestId('recent-list');
  await expect(vList).toBeVisible();
  if (!(await vList.getByText(FIXTURE_PROJECT, { exact: true }).count())) {
    await openFoldersDialog(page);
    await page.getByTestId('new-folder-path').fill(FIXTURE_ROOT);
    await page.getByTestId('add-folder').click();
    await page.getByTestId('folders-dialog-close').click();
  }
  await vList.getByText(FIXTURE_PROJECT, { exact: true }).click();
  await expect(page.getByTestId('chat-panel')).toBeVisible({ timeout: 20_000 });
  await expect(page.getByTestId('project-switch-name')).toHaveText(FIXTURE_PROJECT);

  const vInput = page.getByTestId('ask-input');
  await expect(vInput).toBeEnabled({ timeout: 15_000 });
  await vInput.fill(`Hello ${Date.now()}`);
  await page.getByTestId('send').click();
  // The turn is over when an agent reply has text and the box takes input again — a real model's
  // answer or, with no provider, the honest "No model provider is configured" line.
  await expect(page.getByTestId('conversation-thread').locator('.msg.agent').last().locator('.said'))
    .not.toHaveText(/^\s*$/, { timeout: 120_000 });
  await expect(vInput).toBeEnabled({ timeout: 180_000 });

  await expect.poll(() => existsSync(resolve(METRICS, 'sessions.jsonl')), { timeout: 10_000 }).toBe(true);
  for (const vName of ['runs', 'gates', 'sessions', 'commits', 'misses']) {
    expect(existsSync(resolve(METRICS, `${vName}.jsonl`)), `${vName}.jsonl exists`).toBe(true);
  }
  const vLines = readFileSync(resolve(METRICS, 'sessions.jsonl'), 'utf8').split('\n').filter((l) => l.trim());
  expect(vLines.length).toBeGreaterThanOrEqual(1);
  const vRecord = JSON.parse(vLines[vLines.length - 1]);
  expect(vRecord.tool).toBe('chatur');
  expect(vRecord.kind).toBe('session');
  expect(vRecord.app).toBe(FIXTURE_PROJECT);
  console.log('sessions.jsonl last line: ' + vLines[vLines.length - 1]);
});
