// Acceptance test for REQ-FN-312 (BRD-251, owner's test of 22 Nov 2026, point 2.4): in an empty folder, the Analyst
// brainstorms a small app with the person and then writes the brief, and the brief's file is there. Black-box, web
// harness, signed in as test user 2 ('Owner'), with the owner's test provider from tests/verify/test-settings.json (the
// key is read there and never printed): a real model, because the point is that the agent carries the commands out.
// The conversation is in Go ahead, so the agent's writes land without an approval click. The folder lives under
// tests/verify/bin and is deleted, with its project, when the test ends.
import { test, expect, Page } from '@playwright/test';
import { existsSync, mkdirSync, readFileSync } from 'fs';
import { join } from 'path';
import { signIn } from './support/auth';
import { removeProjectsUnder, scratchDir } from './support/cleanup';
import { openByDialog } from './support/workbench-b';
import { ensureTestProvider, chooseTestModel } from './support/provider';

test.describe.configure({ timeout: 900_000 });

const STAMP = Date.now();
let objRoot = '';

test.afterAll(() => {
  if (objRoot) removeProjectsUnder(objRoot);
});

/** Sends one message and waits for the whole reply; returns the reply's text. */
async function say(aPage: Page, aText: string): Promise<string> {
  const vInput = aPage.getByTestId('ask-input');
  await expect(vInput).toBeEnabled({ timeout: 60_000 });
  const vBefore = await aPage.getByTestId('msg-text').count();
  await vInput.fill(aText);
  await aPage.getByTestId('send').click();
  await expect(aPage.getByTestId('msg-text')).toHaveCount(vBefore + 1, { timeout: 60_000 });
  await expect(vInput).toBeEnabled({ timeout: 300_000 });
  await expect(aPage.getByTestId('stop-agent')).toHaveCount(0, { timeout: 300_000 });
  return aPage.getByTestId('msg-text').last().innerText();
}

/** Keeps answering until the file is on disk, at most a few turns: a real model may ask once more before writing. */
async function untilWritten(aPage: Page, aFile: string, aNudges: string[]): Promise<void> {
  for (const vNudge of aNudges) {
    if (existsSync(aFile)) return;
    await say(aPage, vNudge);
  }

  expect(existsSync(aFile), `${aFile} was written`).toBe(true);
}

test('REQ-FN-312 in an empty folder the Analyst brainstorms a small app with the person and then writes the brief', async ({ page }) => {
  try {
    await signIn(page, 'Owner');
  } catch {
    await signIn(page, 'Owner');
  }

  await ensureTestProvider(page);
  objRoot = scratchDir(`brain-${STAMP}`);
  mkdirSync(objRoot, { recursive: true });
  const vName = objRoot.split('/').pop()!;
  const vApp = vName.replace(/[^A-Za-z0-9]/g, '');
  await openByDialog(page, 'open-folder', objRoot, vName);
  await page.getByTestId('agent-pick').click();
  await page.getByTestId('agent-analyst').click();
  await expect(page.getByTestId('agent-greeting')).toContainText('*brainstorm {topic}', { timeout: 30_000 });
  // A new conversation, once the panel has finished loading the agent's last one.
  await expect(async () => {
    await page.getByTestId('new-chat').click();
    await page.waitForTimeout(1_500);
    await expect(page.getByTestId('msg-text')).toHaveCount(0, { timeout: 1_000 });
  }).toPass({ timeout: 45_000 });
  await chooseTestModel(page);

  // Go ahead, through the agent's own command.
  expect(await say(page, '*yolo')).toContain('Go ahead');

  // The brainstorm: the agent facilitates; the person answers its setup questions.
  const vOpening = await say(page, '*brainstorm a small to-do app for one person');
  expect(vOpening.length).toBeGreaterThan(40);
  await say(page, '1) A to-do list app for one person on a desktop. 2) Keep it small: no accounts, no sync. 3) Focused ideas. 4) Yes, save it. Recommend one technique and use it now; my ideas: due dates, a today view, quick add with a keyboard shortcut, and done items fade out.');
  const vResults = join(objRoot, 'docs', 'brainstorming-session-results.md');
  await untilWritten(page, vResults, [
    'That is enough ideas. Please end the session now and save the session document.',
    'Write docs/brainstorming-session-results.md now with what we have.',
    'Save it now, please.',
  ]);
  expect(readFileSync(vResults, 'utf8')).toMatch(/today view|due date/i);

  // The brief, from the brainstorm.
  const vBrief = join(objRoot, 'docs', `${vApp}-Brief.md`);
  await say(page, `*create-project-brief ${vApp}`);
  await untilWritten(page, vBrief, [
    'Use the brainstorm. Take the sensible reading for anything open, put real unknowns under Open questions, and write the brief now.',
    `Write docs/${vApp}-Brief.md now.`,
    'Write the brief file now, please.',
  ]);
  const vText = readFileSync(vBrief, 'utf8');
  expect(vText).toContain('## Must do');
  expect(vText).toContain('## Who it is for');
  await expect(page.getByTestId('conversation')).toContainText(`docs/${vApp}-Brief.md is written`);
  // FILES shows what the agent wrote, without a refresh by hand; the brief is dated today.
  await expect(page.getByTestId('tree-docs')).toBeVisible({ timeout: 30_000 });
  // The brief is dated in this machine's own day; the UTC day differs for hours after local midnight (verify of 23 Nov 2026, IST).
  const vNow = new Date();
  const vLocalDay = `${vNow.getFullYear()}-${String(vNow.getMonth() + 1).padStart(2, '0')}-${String(vNow.getDate()).padStart(2, '0')}`;
  expect(vText).toContain(vLocalDay);
  await page.screenshot({ path: 'tests/.artifacts/verify/shots/REQ-FN-312-brief.png' });
});
