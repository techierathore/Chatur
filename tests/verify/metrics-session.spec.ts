// Acceptance check for REQ-FN-040, REQ-FN-041, REQ-FN-042 and REQ-UI-115 (owner's test of 25 Nov 2026, BRD-74 as changed
// that day): a real chat with a real model in an opened folder that has no docs/metrics yet makes the folder, writes the
// session and its token use there in TechieFlow's own shape (numbers as numbers, ts, harness "chatur") so TfLens reads it
// unchanged, and Project ▸ Measurements then shows that session and its tokens. A folder that is only opened writes
// nothing. The model is the owner's test provider from tests/verify/test-settings.json (a real call; its key is never printed).
import { test, expect } from '@playwright/test';
import { existsSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from 'fs';
import { join, resolve } from 'path';
import { signIn } from './support/auth';
import { removeProjectsUnder, scratchDir } from './support/cleanup';
import { ensureTestProvider, chooseTestModel } from './support/provider';
import { openByDialog } from './support/workbench-b';

test.describe.configure({ timeout: 300_000 });

const STAMP = Date.now();
let objRoot = '';

test.afterAll(() => {
  if (objRoot) removeProjectsUnder(objRoot);
});

test('REQ-FN-040 REQ-FN-041 REQ-FN-042 REQ-UI-115 a real chat turn in an opened folder makes docs/metrics, writes the session and its tokens in TechieFlow\'s shape, and Measurements shows them', async ({ page }) => {
  objRoot = scratchDir(`metrics-session-${STAMP}`);
  const vFolder = join(objRoot, 'MetricsFresh');
  mkdirSync(vFolder, { recursive: true });
  writeFileSync(join(vFolder, 'notes.txt'), 'hello = 1\n');

  try {
    await signIn(page, 'Owner');
  } catch {
    await signIn(page, 'Owner');
  }

  await ensureTestProvider(page);
  await openByDialog(page, 'open-folder', vFolder, 'MetricsFresh');
  expect(readdirSync(vFolder), 'opening the folder put nothing in it').toEqual(['notes.txt']);
  await expect(page.getByTestId('chat-panel')).toBeVisible({ timeout: 20_000 });
  await expect(async () => {
    await page.getByTestId('new-chat').click();
    await page.waitForTimeout(1_500);
    await expect(page.getByTestId('msg-text')).toHaveCount(0, { timeout: 1_000 });
  }).toPass({ timeout: 45_000 });
  await chooseTestModel(page);

  // A turn that never reaches a model (Chatur's own *help) writes nothing either.
  const vInput = page.getByTestId('ask-input');
  await expect(vInput).toBeEnabled({ timeout: 15_000 });
  await vInput.fill('*help');
  await page.getByTestId('send').click();
  await expect(page.getByTestId('msg-text').last()).toContainText('*', { timeout: 60_000 });
  await expect(vInput).toBeEnabled({ timeout: 60_000 });
  expect(existsSync(join(vFolder, 'docs')), 'a turn that never reached a model made no docs folder').toBe(false);

  // A real turn: the model answers a plain question.
  await vInput.fill(`Reply with the single word OK. ${STAMP}`);
  await page.getByTestId('send').click();
  await expect(page.getByTestId('conversation-thread').locator('.msg.agent').last().locator('.said')).not.toHaveText(/^\s*$/, { timeout: 120_000 });
  await expect(page.getByTestId('stop-agent')).toHaveCount(0, { timeout: 180_000 });
  await expect(vInput).toBeEnabled({ timeout: 60_000 });

  const vMetrics = join(vFolder, 'docs', 'metrics');
  await expect.poll(() => existsSync(join(vMetrics, 'sessions.jsonl')), { timeout: 15_000 }).toBe(true);
  expect(readdirSync(vMetrics), 'only the stream that was written to has a file').toEqual(['sessions.jsonl']);
  const vLines = readFileSync(join(vMetrics, 'sessions.jsonl'), 'utf8').split('\n').filter((l) => l.trim());
  expect(vLines.length).toBeGreaterThanOrEqual(1);
  const vRecord = JSON.parse(vLines[vLines.length - 1]);
  // TechieFlow's shape (SCHEMA.md §1, §4), which TfLens's StreamParser reads: numbers are numbers, ts is to the second.
  expect(vRecord.v).toBe(1);
  expect(vRecord.ts).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$/);
  expect(vRecord.kind).toBe('session');
  expect(vRecord.app).toBe('MetricsFresh');
  expect(vRecord.harness).toBe('chatur');
  expect(typeof vRecord.session_id).toBe('string');
  expect(typeof vRecord.model).toBe('string');
  expect(vRecord.model.length).toBeGreaterThan(0);
  expect(typeof vRecord.input_tokens).toBe('number');
  expect(typeof vRecord.output_tokens).toBe('number');
  expect(vRecord.input_tokens).toBeGreaterThan(0);
  expect(vRecord.output_tokens).toBeGreaterThan(0);
  expect(typeof vRecord.duration_s).toBe('number');
  expect(vRecord.cost_usd).toBeNull();
  console.log(`sessions.jsonl last line (model ${vRecord.model}): ${vRecord.input_tokens} in, ${vRecord.output_tokens} out`);

  // Project ▸ Measurements shows what was written, read from that file alone.
  await page.goto('/measurements', { waitUntil: 'networkidle' });
  await expect(page.getByTestId('metrics-project')).toHaveText('MetricsFresh', { timeout: 60_000 });
  await expect(page.getByTestId('tile-runs-value')).toHaveText('1', { timeout: 60_000 });
  await expect(page.getByTestId('tile-tokens-out-value')).toHaveText(Number(vRecord.output_tokens).toLocaleString('en-US'), { timeout: 30_000 });
  await expect(page.getByTestId('tile-tokens-in-value')).toHaveText(Number(vRecord.input_tokens).toLocaleString('en-US'));
  await page.screenshot({ path: resolve(__dirname, '../.artifacts/verify/shots/REQ-UI-115-chat-session.png'), fullPage: true });
});
