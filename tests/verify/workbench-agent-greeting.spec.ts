// Acceptance tests: an agent greets the person in the Workbench conversation with what it is for and every command it
// has, from Chatur's own list (REQ-FN-308, BRD-241, changed 22 Nov 2026), and carries those commands out itself
// (REQ-FN-311, BRD-249), its replies drawn as formatted text (REQ-UI-114, BRD-250). Black-box, web harness. The project
// is a scratch folder under tests/verify/bin holding two persona files in the shapes the framework uses, whose own
// command lists are fixtures: the greeting must show Chatur's whole list, never the narrower file's. The folder and the
// project go when the test ends. Commands Chatur answers itself need no model; the one test that asks a model uses the
// local stand-in of support/stub-model.ts and checks only what the agent's model is TOLD.
import { test, expect, Page } from '@playwright/test';
import { join } from 'path';
import { signIn } from './support/auth';
import { removeProjectsUnder, scratchDir } from './support/cleanup';
import { put, openByDialog } from './support/workbench-b';
import { query } from './support/db';
import { existsSync, readFileSync } from 'fs';
import { startStubModel, ensureStubProvider, chooseStubModel, removeStubProvider } from './support/stub-model';

test.describe.configure({ timeout: 180_000, mode: 'serial' });

const ANALYST = `# analyst

\`\`\`yaml
agent:
  name: Testa
  id: analyst
  title: Fixture Analyst
  whenToUse: Use for fixture briefs and fixture research
commands:
  - help: Show the fixture commands
  - fixture-brief {App}: Write the fixture brief
  - exit: Say goodbye
\`\`\`
`;

const FLOW_MASTER = `# Flow Master

Fixmaster, the fixture builder (icon X): the one persona for building fixture rows.

## Commands

| Command | What it does | Task file |
|---|---|---|
| \`*fixture-build {App}\` | builds every open fixture row | \`build.md\` |
| \`*fixture-fix {App}\` | fixes what the fixture verify found | \`fix.md\` |
`;

let objRoot = '';
let objName = '';

test.beforeAll(() => {
  objRoot = scratchDir(`agent-greeting-${Date.now()}`);
  objName = objRoot.split('/').pop()!;
  put(join(objRoot, 'notes.txt'), 'hello = 1\n');
  put(join(objRoot, '.tfcore', 'agents', 'analyst.md'), ANALYST);
  put(join(objRoot, '.tfcore', 'agents', 'flow-master.md'), FLOW_MASTER);
});

test.afterAll(() => {
  if (objRoot) removeProjectsUnder(objRoot);
});

async function openFixture(aPage: Page): Promise<void> {
  await signIn(aPage, 'Owner');
  await openByDialog(aPage, 'open-folder', objRoot, objName);
  await expect(aPage.getByTestId('chat-panel')).toBeVisible({ timeout: 30_000 });
  await expect(aPage.getByTestId('ask-input')).toBeEnabled({ timeout: 15_000 });
}

async function pickAgent(aPage: Page, aCode: string): Promise<void> {
  await aPage.getByTestId('agent-pick').click();
  await aPage.getByTestId(`agent-${aCode}`).click();
}

const ANALYST_COMMANDS = ['help', 'day1-brownfield', 'day1-greenfield', 'mockups', 'split-brd', 'brainstorm', 'amend-docs', 'create-project-brief', 'doc-out', 'research-prompt', 'yolo', 'exit'];

/** Starts a new conversation and waits until the old replies are gone from the panel. */
async function newChat(aPage: Page): Promise<void> {
  // The panel may still be loading the agent's last conversation when it is first shown; a new conversation started
  // before that load ends is replaced by it. So: start one, wait, and start again until the panel stays empty.
  await expect(async () => {
    await aPage.getByTestId('new-chat').click();
    await aPage.waitForTimeout(1_500);
    await expect(aPage.getByTestId('msg-text')).toHaveCount(0, { timeout: 1_000 });
  }).toPass({ timeout: 45_000 });
}

/** Sends one message in the composer and waits for the reply to finish; returns the reply's text box. */
async function say(aPage: Page, aText: string) {
  const vInput = aPage.getByTestId('ask-input');
  await expect(vInput).toBeEnabled({ timeout: 30_000 });
  const vBefore = await aPage.getByTestId('msg-text').count();
  await vInput.fill(aText);
  await aPage.getByTestId('send').click();
  await expect(aPage.getByTestId('msg-text')).toHaveCount(vBefore + 1, { timeout: 60_000 });
  await expect(vInput).toBeEnabled({ timeout: 60_000 });
  return aPage.getByTestId('msg-text').last();
}

test('REQ-FN-308 choosing the analyst greets with what it is for and every command it has', async ({ page }) => {
  await openFixture(page);
  await pickAgent(page, 'analyst');

  const vGreeting = page.getByTestId('agent-greeting');
  await expect(vGreeting).toHaveCount(1, { timeout: 15_000 });
  await expect(vGreeting).toContainText('Testa');
  await expect(vGreeting).toContainText('Fixture Analyst');
  await expect(page.getByTestId('agent-greeting-purpose')).toContainText('fixture briefs and fixture research');
  // Chatur's own list, never the persona file's three: every command of TechieFlow's analyst.
  const vCommands = page.getByTestId('agent-greeting-command');
  await expect(vCommands).toHaveCount(ANALYST_COMMANDS.length);
  for (const vCode of ANALYST_COMMANDS) await expect(vGreeting).toContainText(`*${vCode}`);
  await expect(vGreeting).toContainText('*brainstorm {topic}');
  await expect(vGreeting).not.toContainText('*fixture-brief');
});

test('REQ-FN-308 changing the agent to the flow master greets again with its own commands', async ({ page }) => {
  await openFixture(page);
  await pickAgent(page, 'analyst');
  await expect(page.getByTestId('agent-greeting-command')).toHaveCount(ANALYST_COMMANDS.length, { timeout: 15_000 });

  await pickAgent(page, 'flow-master');

  // one greeting at a time: the flow master's, named from its file, with every command of TechieFlow's flow master
  await expect(page.getByTestId('agent-greeting')).toHaveCount(1);
  await expect(page.getByTestId('agent-greeting')).toContainText('Fixmaster');
  const vCommands = page.getByTestId('agent-greeting-command');
  await expect(vCommands).toHaveCount(20);
  await expect(vCommands.first()).toContainText('*develop-end-to-end');
  await expect(page.getByTestId('agent-greeting')).toContainText('*render-workflow-docs {App}');
  await expect(page.getByTestId('agent-greeting')).not.toContainText('*fixture-build');
});

test('REQ-FN-308 asked whether it has a brainstorm command, the analyst says yes from its list, with no model', async ({ page }) => {
  await openFixture(page);
  await pickAgent(page, 'analyst');
  await newChat(page);
  const vReply = await say(page, "Don't you have brain-storm command ?");
  await expect(vReply).toContainText('Yes.');
  await expect(vReply).toContainText('*brainstorm {topic}');
  await expect(page.getByTestId('msg-model').last()).toHaveText('Chatur (no model)');

  const vAll = await say(page, 'What commands do you have?');
  for (const vCode of ANALYST_COMMANDS) await expect(vAll).toContainText(`*${vCode}`);
});

test('REQ-FN-311 REQ-UI-114 *help lists every command as a numbered list with code, drawn as formatted text', async ({ page }) => {
  await openFixture(page);
  await pickAgent(page, 'analyst');
  await newChat(page);
  const vReply = await say(page, '*help');
  await expect(vReply.locator('ol > li')).toHaveCount(ANALYST_COMMANDS.length);
  await expect(vReply.locator('code').first()).toBeVisible();
  const vText = await vReply.innerText();
  expect(vText).not.toContain('**');
  expect(vText).not.toContain('`');
  await page.screenshot({ path: 'tests/.artifacts/verify/shots/REQ-UI-114-help.png' });
});

test('REQ-FN-311 REQ-UI-114 *yolo turns Go ahead on and back off, its words drawn bold', async ({ page }) => {
  await openFixture(page);
  await pickAgent(page, 'architect');
  await newChat(page);
  const vOn = await say(page, '*yolo');
  await expect(vOn.locator('strong')).toHaveText('Go ahead');
  const vSession = () => query("SELECT Mode FROM Session WHERE ProjectId = (SELECT ProjectId FROM Project WHERE Path = ?) ORDER BY SessionId DESC LIMIT 1", objRoot)[0]?.Mode;
  await expect.poll(vSession, { timeout: 10_000 }).toBe('GoAhead');
  const vOff = await say(page, '*yolo');
  await expect(vOff.locator('strong')).toHaveText('Ask first');
  await expect.poll(vSession, { timeout: 10_000 }).toBe('AskFirst');
});

test('REQ-FN-311 REQ-UI-114 the verifier\'s *report shows the rows in scope as a table', async ({ page }) => {
  put(join(objRoot, 'docs', 'Fixture-Checklist.md'), '| ID | Requirement | Status | % | Remarks | Details |\n|---|---|---|---|---|---|\n| REQ-UI-001 | A list | Verified | 100% | fixture passed | x |\n| REQ-FN-001 | A save | Not Started | 0% | fixture open | x |\n');
  await openFixture(page);
  await pickAgent(page, 'verifier');
  await newChat(page);
  const vReply = await say(page, '*report ui');
  await expect(vReply.locator('table')).toBeVisible();
  await expect(vReply.locator('table')).toContainText('REQ-UI-001');
  await expect(vReply.locator('table')).not.toContainText('REQ-FN-001');
  expect(await vReply.innerText()).not.toContain('|');
});

test('REQ-FN-311 the flow master\'s *log-miss, *generate-html and *build-phase are done by Chatur', async ({ page }) => {
  put(join(objRoot, 'docs', 'Fixture-BRD.md'), '# Fixture — BRD\n\n**BRD-1** — one thing.\n');
  await openFixture(page);
  await pickAgent(page, 'flow-master');
  await newChat(page);

  const vMiss = await say(page, '*log-miss "the fixture button stayed grey"');
  await expect(vMiss).toContainText('Logged');
  expect(readFileSync(join(objRoot, 'docs', 'metrics', 'misses.jsonl'), 'utf8')).toContain('the fixture button stayed grey');

  const vHtml = await say(page, '*generate-html docs/Fixture-BRD.md');
  await expect(vHtml).toContainText('docs/Fixture-BRD.html');
  expect(existsSync(join(objRoot, 'docs', 'Fixture-BRD.html'))).toBe(true);

  const vRun = await say(page, '*build-phase');
  await expect(vRun).toContainText('build-phase');
  await expect(vRun.locator('a[href*="/processes/"]')).toBeVisible();
});

test('REQ-FN-308 the agent persona and its whole command list reach the model as the system prompt', async ({ page }) => {
  const vStub = await startStubModel('read-file', { path: 'notes.txt' });
  try {
    await ensureStubProvider(page, vStub);
    await openFixture(page);
    await pickAgent(page, 'analyst');
    await page.getByTestId('new-chat').click();
    await chooseStubModel(page);
    const vInput = page.getByTestId('ask-input');
    await expect(vInput).toBeEnabled({ timeout: 15_000 });
    await vInput.fill('Read notes.txt and say what it holds.');
    await page.getByTestId('send').click();
    await expect(page.getByTestId('msg-model').last()).toHaveText('stub-edit-model', { timeout: 60_000 });

    const vPrompts = vStub.systemPrompts ?? [];
    expect(vPrompts.length, 'the model was asked').toBeGreaterThan(0);
    for (const vPrompt of vPrompts) {
      expect(vPrompt).toContain('Testa');
      expect(vPrompt).toContain('*brainstorm {topic}');
      expect(vPrompt).toContain('never say you lack a command that is on it');
    }
  } finally {
    await removeStubProvider(page);
    await vStub.close();
  }
});
