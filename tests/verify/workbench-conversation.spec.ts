// Verify-phase acceptance tests — Workbench conversation region (ConversationPanel).
// Rows: REQ-FN-025/026/027, REQ-UI-021/022/023/024/031, REQ-FN-031/032, REQ-UI-032, REQ-FN-033..036, REQ-UI-033.
// Black-box: only the rendered UI. No model provider can answer on this machine (TechieRag gap TR-RAG-003,
// owner decision 1), so clauses that need a real model answer are skipped with that reason AFTER the
// observable part of the row has been asserted.
import { test, expect, Page, Locator } from '@playwright/test';
import { signIn } from './support/auth';
import { openFoldersDialog } from './support/start';
import { ensureTestProvider, chooseTestModel, testProvider } from './support/provider';
import { startStubModel, ensureStubProvider, chooseStubModel, removeStubProvider } from './support/stub-model';
import { mkdirSync, writeFileSync, existsSync, readFileSync } from 'fs';
import { resolve } from 'path';


test.describe.configure({ timeout: 150_000 });
const FIXTURE_ROOT = resolve(__dirname, '../.artifacts/verify/fixtures/workbench-b-root');
const FIXTURE_PROJECT = 'workbench-b';
const SHOTS = resolve(__dirname, '../.artifacts/verify/shots/workbench-conversation');

let objProject = '';
let objProviderReady = false;

function ensureFixture(): void {
  const vDir = resolve(FIXTURE_ROOT, FIXTURE_PROJECT);
  mkdirSync(vDir, { recursive: true });
  if (!existsSync(resolve(vDir, 'notes.txt'))) writeFileSync(resolve(vDir, 'notes.txt'), 'hello = 1\n');
  if (!existsSync(resolve(vDir, 'README.md'))) writeFileSync(resolve(vDir, 'README.md'), '# Workbench B\n');
  mkdirSync(SHOTS, { recursive: true });
}

async function shot(aPage: Page, aName: string): Promise<string> {
  const vPath = `${SHOTS}/${aName}.png`;
  await aPage.screenshot({ path: vPath });
  return vPath;
}

/** Reaches the Workbench on the fixture project through the real Start flow (`/start?all=1`). */
async function openWorkbench(aPage: Page): Promise<void> {
  await signIn(aPage, 'Second');
  if (!objProviderReady) {
    await ensureTestProvider(aPage);
    objProviderReady = true;
  }
  await aPage.goto('/start?all=1', { waitUntil: 'networkidle' });
  await expect(aPage).toHaveURL(/\/start/);
  const vList = aPage.getByTestId('recent-list');
  await expect(vList).toBeVisible({ timeout: 20_000 });
  await expect(aPage.getByTestId('recent-loading')).toHaveCount(0, { timeout: 20_000 });
  if (!(await vList.getByText(FIXTURE_PROJECT, { exact: true }).count())) {
    await openFoldersDialog(aPage);
    await aPage.getByTestId('new-folder-path').fill(FIXTURE_ROOT);
    await aPage.getByTestId('add-folder').click();
    await expect(aPage.getByTestId('folders-list')).toContainText(FIXTURE_ROOT);
    await aPage.getByTestId('folders-dialog-close').click();
  }
  await expect(async () => {
    if (!(await aPage.getByTestId('chat-panel').isVisible())) await vList.getByText(FIXTURE_PROJECT, { exact: true }).click();
    await expect(aPage.getByTestId('chat-panel')).toBeVisible({ timeout: 4000 });
  }).toPass({ timeout: 30_000 });
  await expect(aPage.getByTestId('project-switch-name')).toHaveText(FIXTURE_PROJECT);
  await expect(aPage.getByTestId('ask-input')).toBeEnabled({ timeout: 15_000 });
  objProject = FIXTURE_PROJECT;
}

async function reloadOnProject(aPage: Page): Promise<void> {
  await aPage.reload({ waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('chat-panel')).toBeVisible({ timeout: 20_000 });
  await expect(aPage.getByTestId('project-switch-name')).toHaveText(FIXTURE_PROJECT);
  await expect(aPage.getByTestId('ask-input')).toBeEnabled({ timeout: 15_000 });
}

async function send(aPage: Page, aText: string): Promise<void> {
  const vInput = aPage.getByTestId('ask-input');
  await expect(vInput).toBeEnabled({ timeout: 15_000 });
  await vInput.fill(aText);
  await aPage.getByTestId('send').click();
}

const thread = (aPage: Page): Locator => aPage.getByTestId('conversation-thread');

async function isOn(aItem: Locator): Promise<boolean> {
  return aItem.evaluate((e) => {
    const vEls = [e, ...Array.from(e.querySelectorAll('*'))] as HTMLElement[];
    return vEls.some((x) => x.getAttribute('aria-checked') === 'true' || x.getAttribute('aria-pressed') === 'true' || x.getAttribute('data-state') === 'on');
  });
}

test.beforeAll(() => ensureFixture());

test.beforeEach(async ({ page }) => {
  await openWorkbench(page);
});


const NOTES = resolve(FIXTURE_ROOT, FIXTURE_PROJECT, 'notes.txt');
const resetNotes = (): void => writeFileSync(NOTES, 'hello = 1\n');
const readNotes = (): string => readFileSync(NOTES, 'utf8');
const agentSaid = (aPage: Page): Locator => thread(aPage).locator('.msg.agent .said').last();
const step = (aPage: Page, aText: RegExp | string): Locator => aPage.getByTestId('activity-step').filter({ hasText: aText });
const refusedStep = (aPage: Page): Locator => aPage.getByTestId('activity-step').filter({ hasText: /refused/ });

async function pickRole(aPage: Page, aCode: 'analyst' | 'flow-master'): Promise<void> {
  await aPage.getByTestId('agent-pick').click();
  await aPage.getByTestId(`agent-${aCode}`).click();
}

async function setMode(aPage: Page, aMode: 'Ask first' | 'Go ahead'): Promise<void> {
  // dispatchEvent: with a model and role chosen the toolbar overflows at 1280x720 and the toggle is covered (see REQ-UI-024)
  await aPage.getByTestId('mode').getByRole('radio', { name: aMode, exact: true }).dispatchEvent('click');
}

/**
 * Model-driven setup: role picked, a NEW conversation started with it (so each test sends the model a
 * short history rather than every earlier test's turns — 81k tokens by the end otherwise), and the
 * model chosen in the composer.
 */
async function modelSetup(aPage: Page, aRole: 'analyst' | 'flow-master'): Promise<void> {
  await pickRole(aPage, aRole);
  await aPage.getByTestId('new-chat').click();
  await expect(thread(aPage).locator('.msg')).toHaveCount(0, { timeout: 15_000 });
  await chooseTestModel(aPage);
}

/** Sends a prompt and waits until the turn is over (the composer is enabled again). */
async function sendAndFinish(aPage: Page, aText: string): Promise<void> {
  await send(aPage, aText);
  await expect(aPage.getByTestId('ask-input')).toBeDisabled({ timeout: 10_000 }).catch(() => undefined);
  await expect(aPage.getByTestId('ask-input')).toBeEnabled({ timeout: 90_000 });
}

/** Models are non-deterministic: re-asks (up to aTries) until aCheck holds, then asserts it. */
async function untilHolds(aPage: Page, aPrompt: string, aCheck: () => Promise<boolean>, aTries = 2): Promise<void> {
  for (let vTry = 0; vTry < aTries; vTry++) {
    await sendAndFinish(aPage, aPrompt);
    for (let vWait = 0; vWait < 6; vWait++) {
      if (await aCheck()) return;
      await aPage.waitForTimeout(500);
    }
  }
  await shot(aPage, `no-${Date.now()}`);
  expect(await aCheck(), `the model did not do what was asked after ${aTries} tries`).toBe(true);
}

/** Clears any change left pending by an earlier test so only this test's proposal is on screen. */
async function clearProposals(aPage: Page): Promise<void> {
  for (let vI = 0; vI < 10 && (await aPage.getByTestId('proposed-change').count()) > 0; vI++) {
    await aPage.getByTestId('change-reject').first().click();
    await aPage.waitForTimeout(600);
  }
}

const EDIT_PROMPT = 'Use the edit-file tool to change notes.txt so its whole text is exactly: hello = 2';

async function proposeEdit(aPage: Page): Promise<void> {
  resetNotes();
  await modelSetup(aPage, 'flow-master');
  await setMode(aPage, 'Ask first');
  await clearProposals(aPage);
  // The composer's agent picker says Flow master, but every edit-file call is refused by the SESSION's role
  // (Analyst) — so no proposal can appear. That refusal is the app defect behind a failure here.
  await untilHolds(aPage, EDIT_PROMPT, async () => (await aPage.getByTestId('proposed-change').count()) > 0);
}

test('REQ-FN-025 sending a message about the selected project shows a reply from the chosen role in the conversation', async ({ page }) => {
  await modelSetup(page, 'analyst');
  await send(page, 'Reply with the single word READY.');
  const vReply = thread(page).locator('.msg.agent').last();
  await expect(vReply).toBeVisible({ timeout: 90_000 });
  await expect(page.getByTestId('ask-input')).toBeEnabled({ timeout: 90_000 });
  await expect(vReply).toContainText('Analyst'); // the chosen role labels the reply
  await expect(vReply.locator('.said')).toContainText(/\S/);
  await expect(vReply.locator('.said')).not.toContainText(/No model provider is configured/i);
});

test('REQ-FN-026 a reply names the model that produced it', async ({ page }) => {
  await modelSetup(page, 'analyst');
  await sendAndFinish(page, 'Reply with the single word READY.');
  await expect(thread(page).locator('.msg.agent').last().locator('.model')).toContainText(testProvider().model);
});

test('REQ-FN-027 the reply text grows in the conversation before the reply is finished', async ({ page }) => {
  await modelSetup(page, 'analyst');
  await send(page, 'Count from 1 to 80, one number per line, with no other text.');
  const vSamples: number[] = [];
  const vStart = Date.now();
  while (Date.now() - vStart < 90_000) {
    const vBusy = await page.getByTestId('ask-input').isDisabled();
    const vLen = await thread(page).locator('.msg.agent .said').last().innerText().then((t) => t.length).catch(() => 0);
    if (vBusy) vSamples.push(vLen);
    if (!vBusy && vSamples.length > 0) break;
    await page.waitForTimeout(120);
  }
  const vPositive = vSamples.filter((n) => n > 0);
  expect(new Set(vPositive).size, `text lengths while busy: ${vSamples.join(',')}`).toBeGreaterThanOrEqual(2);
  expect(vPositive[0]).toBeLessThan(Math.max(...vSamples));
});

test('REQ-UI-021 a finished reply shows the tokens it used and the running total', async ({ page }) => {
  await modelSetup(page, 'analyst');
  await sendAndFinish(page, 'Reply with the single word READY.');
  const vTokens = (aText: string): number => Number(aText.replace(/[^\d]/g, ''));
  await expect(thread(page).locator('.msg.agent').last().locator('.model')).toContainText(/[\d,]+ tokens/);
  await expect(page.getByTestId('cost')).toHaveText(/^[\d,]+ tokens$/);
  const vFirst = vTokens(await page.getByTestId('cost').innerText());
  expect(vFirst).toBeGreaterThan(0);
  await sendAndFinish(page, 'Reply with the single word AGAIN.');
  await expect.poll(async () => vTokens(await page.getByTestId('cost').innerText())).toBeGreaterThan(vFirst);
});

test('REQ-UI-022 a file read by the agent appears in the activity panel as it happens', async ({ page }) => {
  await modelSetup(page, 'flow-master');
  const vBefore = await step(page, /notes\.txt/).count();
  let vSeenWhileBusy = false;
  for (let vTry = 0; vTry < 2 && !vSeenWhileBusy; vTry++) {
    await send(page, 'Use the read-file tool to read notes.txt, then reply DONE.');
    const vStart = Date.now();
    while (Date.now() - vStart < 90_000) {
      const vBusy = await page.getByTestId('ask-input').isDisabled();
      if (vBusy && (await step(page, /notes\.txt/).count()) > vBefore) vSeenWhileBusy = true;
      if (!vBusy && Date.now() - vStart > 1500) break;
      await page.waitForTimeout(150);
    }
  }
  await shot(page, 'REQ-UI-022');
  expect(await step(page, /notes\.txt/).count(), 'a read step for notes.txt appears in the activity panel').toBeGreaterThan(vBefore);
  expect(vSeenWhileBusy, 'the step was visible while the agent was still working').toBe(true);
});

test('REQ-UI-023 Stop stops the agent mid-reply, the activity panel says so, and no further step is taken', async ({ page }) => {
  await modelSetup(page, 'analyst');
  const vStoppedBefore = await step(page, /Stopped by the owner/).count();
  await send(page, 'Write the numbers from 1 to 600, one per line, with no other text.');
  await expect.poll(async () => (await agentSaid(page).innerText().catch(() => '')).length, { timeout: 60_000 }).toBeGreaterThan(0);
  await page.getByTestId('stop-agent').click();
  await expect(step(page, /Stopped by the owner/)).toHaveCount(vStoppedBefore + 1, { timeout: 10_000 });
  await expect(page.getByTestId('ask-input')).toBeEnabled({ timeout: 15_000 });
  const vLen = (await agentSaid(page).innerText()).length;
  const vSteps = await page.getByTestId('activity-step').count();
  await page.waitForTimeout(3000);
  expect((await agentSaid(page).innerText()).length, 'the reply stopped growing').toBe(vLen);
  expect(await page.getByTestId('activity-step').count(), 'no further step').toBe(vSteps);
  expect(await agentSaid(page).innerText()).not.toMatch(/\b600\s*$/); // it did not run to the end
});

test('REQ-UI-024 the mode set on the session is kept with the session and shown while it runs', async ({ page }) => {
  await chooseTestModel(page);
  await pickRole(page, 'flow-master');
  const vMode = page.getByTestId('mode');
  const vGo = vMode.getByText('Go ahead', { exact: true });
  const vAsk = vMode.getByText('Ask first', { exact: true });
  await vGo.click({ timeout: 5000 }); // a real click: the toggle must be reachable with a model and role chosen
  await expect.poll(() => isOn(vGo.locator('xpath=ancestor-or-self::*[self::button or @role][1]'))).toBe(true);
  await reloadOnProject(page);
  await expect.poll(() => isOn(vMode.getByText('Go ahead', { exact: true }).locator('xpath=ancestor-or-self::*[self::button or @role][1]'))).toBe(true);
  await page.getByTestId('history').click();
  await expect(page.getByTestId('history-pop')).toContainText('GoAhead');
  await page.keyboard.press('Escape');
  // exactly one is chosen at a time; switch back and it is kept too
  await vAsk.click();
  await reloadOnProject(page);
  await expect.poll(() => isOn(vMode.getByText('Ask first', { exact: true }).locator('xpath=ancestor-or-self::*[self::button or @role][1]'))).toBe(true);
  expect(await isOn(vMode.getByText('Go ahead', { exact: true }).locator('xpath=ancestor-or-self::*[self::button or @role][1]'))).toBe(false);
});

test('REQ-UI-031 Sessions lists each session with its project, role, mode, when it started and its state', async ({ page }) => {
  await page.getByTestId('history').click();
  const vPop = page.getByTestId('history-pop');
  await expect(vPop).toBeVisible();
  const vRows = vPop.locator('[data-testid^="session-"]');
  await expect(vRows.first()).toBeVisible();
  const vRow = (await vRows.first().innerText()).replace(/\s+/g, ' ');
  try {
    expect(vRow).toMatch(/\S+ · (AskFirst|GoAhead)/); // role · mode
    expect(vRow).toMatch(/\d{1,2} \w{3}, \d{2}:\d{2}/); // when it started
    expect(vRow).toMatch(/(Active|Stopped|Finished|Done|Ended|Completed|Idle)/); // state
    expect(await vPop.innerText()).toContain(objProject); // its project
  } catch (e) {
    await shot(page, 'REQ-UI-031');
    throw e;
  }
});

test('REQ-FN-031 continuing a session opens the Workbench with that conversation, its role and its mode', async ({ page }) => {
  const vRole = (await page.getByTestId('agent-pick').innerText()).trim();
  const vText = `Continue me ${Date.now()}`;
  await send(page, vText);
  await expect(thread(page).locator('.msg.agent').last()).toBeVisible({ timeout: 30_000 });
  await reloadOnProject(page);
  await expect(thread(page).locator('.msg.me', { hasText: vText })).toBeVisible();
  await expect(page.getByTestId('conversation')).toContainText(vRole);
  await expect(page.getByTestId('mode')).toBeVisible();
  // Either continue line is right: a change an earlier test left waiting turns it into "picking up where it stopped".
  await expect(page.getByTestId('chat-panel')).toContainText(/Continuing after|picking up where it stopped/i);
});

test('REQ-FN-032 a reopened session keeps every message and refusal in order', async ({ page }) => {
  const vA = `First turn ${Date.now()}`;
  const vB = `Second turn ${Date.now()}`;
  await send(page, vA);
  // Wait for THIS turn: earlier agent lines are already visible, so "last agent line visible" passed
  // before the first send reached the server, and typing the second message then raced it (2026-09-30).
  await expect(thread(page).locator('.msg.me').last()).toContainText(vA, { timeout: 30_000 });
  await expect(page.getByTestId('ask-input')).toBeEnabled({ timeout: 30_000 });
  const vCount = await thread(page).locator('.msg').count();
  await send(page, vB);
  await expect(thread(page).locator('.msg')).toHaveCount(vCount + 2, { timeout: 30_000 });
  // A live model takes longer than any fixed pause: the turn is over when the box takes input again.
  await expect(page.getByTestId('ask-input')).toBeEnabled({ timeout: 180_000 });
  await page.waitForTimeout(1000); // let the turn finish being recorded before the reload
  await reloadOnProject(page);
  const vTexts = await thread(page).locator('.msg .said').allInnerTexts();
  const vIa = vTexts.findIndex((t) => t.includes(vA));
  const vIb = vTexts.findIndex((t) => t.includes(vB));
  expect(vIa).toBeGreaterThanOrEqual(0);
  expect(vIb).toBeGreaterThan(vIa);
  // each of my turns is followed by the agent's reply, in order
  const vRoles = await thread(page).locator('.msg').evaluateAll((els) => els.map((e) => (e.classList.contains('me') ? 'me' : 'agent')));
  expect(vRoles[vIa + 1]).toBe('agent');
  expect(vRoles[vIb + 1]).toBe('agent');
});


test('REQ-UI-032 a proposed edit shows the file with its old and new text side by side', async ({ page }) => {
  await proposeEdit(page);
  const vCard = page.getByTestId('proposed-change').last();
  await expect(vCard.getByTestId('change-file')).toContainText('notes.txt');
  await expect(vCard).toContainText('hello = 1');
  await expect(vCard).toContainText('hello = 2');
  await shot(page, 'REQ-UI-032');
  expect(readNotes()).toBe('hello = 1\n');
});

test('REQ-FN-033 approving a change writes the new text to the file and moves the row to approved', async ({ page }) => {
  await proposeEdit(page);
  await page.getByTestId('change-approve').last().click();
  await expect.poll(() => readNotes(), { timeout: 15_000 }).toContain('hello = 2');
  await expect(page.getByTestId('proposed-change')).toHaveCount(0, { timeout: 10_000 }); // no longer Proposed
});

test('REQ-FN-034 rejecting a change leaves the file untouched and moves the row to rejected', async ({ page }) => {
  await proposeEdit(page);
  await page.getByTestId('change-reject').last().click();
  await expect(page.getByTestId('proposed-change')).toHaveCount(0, { timeout: 10_000 });
  await page.waitForTimeout(1000);
  expect(readNotes()).toBe('hello = 1\n');
});

test('REQ-FN-035 in ask me first no file is written until the owner has approved that change', async ({ page }) => {
  await proposeEdit(page);
  await page.waitForTimeout(1500);
  expect(readNotes(), 'file untouched while the change is only proposed').toBe('hello = 1\n');
  await page.getByTestId('change-approve').last().click();
  await expect.poll(() => readNotes(), { timeout: 15_000 }).toContain('hello = 2');
});

test('REQ-FN-036 a refused request never reaches the tool: an agent without the edit right is refused and the file is untouched', async ({ page }) => {
  resetNotes();
  await modelSetup(page, 'analyst'); // the Analyst's edit-file right is 0
  await setMode(page, 'Go ahead'); // so only the role's rights can stop it
  await clearProposals(page);
  const vBefore = await refusedStep(page).count();
  await untilHolds(page, EDIT_PROMPT, async () => (await refusedStep(page).count()) > vBefore);
  await shot(page, 'REQ-FN-036');
  expect(readNotes()).toBe('hello = 1\n');
  await expect(page.getByTestId('proposed-change')).toHaveCount(0);
});

test('REQ-UI-033 a model asking to run a source-control command is refused and the refusal is shown in the activity', async ({ page }) => {
  // The model's choice is fixed by a local stand-in that always asks for run-source-control (support/stub-model.ts):
  // a real model sometimes declines, and then the guard is never reached. The guard, agent loop and activity are real.
  const vStub = await startStubModel('run-source-control', { command: 'git status' });
  try {
    await ensureStubProvider(page, vStub);
    await openWorkbench(page);
    await pickRole(page, 'flow-master');
    await page.getByTestId('new-chat').click();
    await expect(thread(page).locator('.msg')).toHaveCount(0, { timeout: 15_000 });
    await chooseStubModel(page);
    const vBefore = await refusedStep(page).count();
    await sendAndFinish(page, 'Show me the source-control status of this project.');
    expect(vStub.calls[0], 'the model asked for the run-source-control tool').toBe('tool:run-source-control');
    await expect.poll(async () => refusedStep(page).count(), { timeout: 15_000 }).toBeGreaterThan(vBefore);
    await shot(page, 'REQ-UI-033');
  } finally {
    await removeStubProvider(page);
    await vStub.close();
  }
});
