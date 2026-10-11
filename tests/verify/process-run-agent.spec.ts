// Acceptance tests: Process run shows the agent at work (REQ-UI-070) and the conversation shows its output (REQ-UI-071);
// mockups/process-run.html, `handed-step` and `msg-handed-step`. Black-box, web harness.
// The UI cannot make an agent work by itself, so the handed session's own rows are seeded (as the failed-check case in
// process-run.spec.ts seeds a failed check): the session, its events and the run step's hand-over record, in the shapes
// Chatur.Core writes. Everything the tests assert goes through the screen.
import { Page, expect } from '@playwright/test';
import { join } from 'path';
import { signIn } from './support/auth';
import { query, run } from './support/db';
import { openProcessesTab, startProcess, closeTab, tab } from './support/phase2-tabs';
import { test, resetFixture, openProject, SHOTS } from './support/workbench-a';

let page: Page;
let runId = 0;
let sessionId = 0;

test.beforeAll(async ({ browser }) => {
  page = await browser.newPage({ baseURL: process.env.BASE_URL });
  await signIn(page, 'Owner');
});
test.afterAll(async () => { await page.close(); });
test.beforeEach(async () => {
  test.setTimeout(420_000);
  resetFixture();
});
test.afterEach(() => {
  if (sessionId > 0) {
    run('DELETE FROM ProcessRunStepWork WHERE SessionId=?', sessionId);
    run('DELETE FROM SessionEvent WHERE SessionId=?', sessionId);
    run('DELETE FROM Change WHERE SessionId=?', sessionId);
    run('DELETE FROM Session WHERE SessionId=?', sessionId);
    sessionId = 0;
  }
});

const now = () => new Date().toISOString();

function addEvent(aSeq: number, aKind: string, aPayload: string, aModel: string | null = null, aTokens: number | null = null) {
  run('INSERT INTO SessionEvent (SessionId, Seq, Kind, Payload, Model, Tokens, CreatedUtc) VALUES (?,?,?,?,?,?,?)', sessionId, aSeq, aKind, aPayload, aModel, aTokens, now());
}

/** Starts verify-phase, waits for its owner stop, then hands step 4 to a Flow master session that has written a file, built and been refused. */
async function startWithHandedStep(aState: 'working' | 'finished'): Promise<number> {
  await openProject(page);
  await openProcessesTab(page);
  const vId = await startProcess(page, 'verify-phase');
  await expect(page.getByTestId('run-state')).toContainText('waiting for you', { timeout: 180_000 });
  runId = vId;
  const vProject = Number(query('SELECT ProjectId FROM ProcessRun WHERE RunId=?', vId)[0].ProjectId);
  const vRole = Number(query("SELECT RoleId FROM Role WHERE Name='Flow master' ORDER BY Version DESC LIMIT 1")[0].RoleId);
  sessionId = run('INSERT INTO Session (ProjectId, RoleId, Mode, State, StartedUtc) VALUES (?,?,?,?,?)', vProject, vRole, 'GoAhead', 'Active', now());
  addEvent(1, 'user', `Step 4 of verify-phase run ${vId}: build the cluster.`);
  addEvent(2, 'assistant', 'Building the coverage filter and its tests.', 'stub-edit-model', 120);
  addEvent(3, 'tool', 'Build the project.');
  addEvent(4, 'refusal', 'git commit - no agent runs source control.');
  run('INSERT INTO Change (SessionId, FilePath, Before, After, Status) VALUES (?,?,?,?,?)', sessionId, 'src/Coverage/CoverageFilter.cs', '', 'class CoverageFilter {}', 'Approved');
  run(
    'INSERT INTO ProcessRunStepWork (RunId, Position, SessionId, AgentName, ModelName, Tier, State, Task, FinalMessage, StartedUtc) VALUES (?,?,?,?,?,?,?,?,?,?)',
    vId, 4, sessionId, 'Flow master', 'stub-edit-model', 1, aState, `verify-phase run ${vId}, step 4`, aState === 'finished' ? 'The cluster is built.' : null, now(),
  );
  // Close the run's tab and open it again, as the owner would, so the page reads the handed step.
  await closeTab(page, `verify-phase · run ${vId}`);
  await tab(page, 'Processes').click();
  await expect(page.getByTestId('open-last-run')).toBeEnabled({ timeout: 30_000 });
  await page.getByTestId('open-last-run').click();
  return vId;
}

test('REQ-UI-070 Process run shows the agent, its model, and the files, commands and refusals so far', async () => {
  await startWithHandedStep('working');
  const vPanel = page.getByTestId('handed-step');
  await expect(vPanel).toBeVisible({ timeout: 30_000 });
  await expect(vPanel).toContainText('Step 4 is with the Flow master');
  await expect(page.getByTestId('handed-step-state')).toContainText('working');
  await expect(page.getByTestId('handed-step-agent')).toHaveText('Flow master');
  await expect(page.getByTestId('handed-step-model')).toContainText('stub-edit-model');
  await expect(page.getByTestId('handed-step-model')).toContainText('tier 1');
  await expect(page.getByTestId('handed-step-task')).toContainText('step 4');
  await expect(page.getByTestId('handed-step-files')).toContainText('src/Coverage/CoverageFilter.cs');
  await expect(page.getByTestId('handed-step-commands')).toContainText('Build the project.');
  await expect(page.getByTestId('handed-step-refusal')).toContainText('git commit');
  await expect(page.getByTestId('handed-step-stop')).toBeVisible();
  await vPanel.scrollIntoViewIfNeeded();
  await page.screenshot({ path: join(SHOTS, 'process-run-handed-step.png'), fullPage: true });
});

test('REQ-UI-070 a finished handed step shows the agent\'s final message and no Stop', async () => {
  await startWithHandedStep('finished');
  await expect(page.getByTestId('handed-step')).toContainText('Step 4 was with the Flow master', { timeout: 30_000 });
  await expect(page.getByTestId('handed-step-state')).toContainText('finished');
  await expect(page.getByTestId('handed-step-final')).toContainText('The cluster is built.');
  await expect(page.getByTestId('handed-step-stop')).toHaveCount(0);
});

test('REQ-UI-071 the agent\'s messages and tool calls appear in the conversation as they happen, under its name', async () => {
  await startWithHandedStep('working');
  const vMessage = page.getByTestId('msg-handed-step');
  await expect(vMessage).toBeVisible({ timeout: 30_000 });
  await expect(vMessage).toContainText('Flow master');
  await expect(vMessage).toContainText('stub-edit-model');
  await expect(vMessage).toContainText('verify-phase run');
  await expect(vMessage).toContainText('Building the coverage filter and its tests.');
  await expect(page.getByTestId('conversation')).toContainText('refused');
  // The agent goes on while the owner watches: a new tool call and a new message land without any reload.
  addEvent(5, 'tool', 'Build the project.');
  addEvent(6, 'assistant', 'The build passed; writing the tests next.', 'stub-edit-model', 90);
  await expect(page.getByTestId('conversation')).toContainText('The build passed; writing the tests next.', { timeout: 15_000 });
  await expect(page.getByTestId('activity-step').filter({ hasText: 'Build the project.' })).toHaveCount(2, { timeout: 15_000 });
  await page.screenshot({ path: join(SHOTS, 'process-run-handed-conversation.png'), fullPage: true });
});

/** The owner's own newest session on the open project: not one a process step started. */
function ownersSessionId(): number {
  const vProject = Number(query("SELECT ProjectId FROM Project WHERE Name='WbAFilesApp'")[0].ProjectId);
  return Number(query('SELECT SessionId FROM Session WHERE ProjectId=? AND SessionId NOT IN (SELECT SessionId FROM ProcessRunStepWork) ORDER BY StartedUtc DESC, SessionId DESC LIMIT 1', vProject)[0].SessionId);
}

test('REQ-UI-071 the conversation follows the handed step under the agent\'s name and the owner\'s own session is back after leaving Process run', async () => {
  await startWithHandedStep('working');
  // While Process run shows the step: the task the run gave the agent is the step's task, never "You".
  const vTask = page.getByTestId('step-task');
  await expect(vTask).toContainText('Step 4 of verify-phase', { timeout: 30_000 });
  await expect(page.getByTestId('msg-1')).toContainText('Task for Flow master');
  await expect(page.getByTestId('msg-1')).not.toContainText('You');
  await expect(page.getByTestId('msg-handed-step')).toContainText('Building the coverage filter and its tests.');
  // Leaving the tab ends the view: the step is no longer followed and the owner's conversation is back.
  await tab(page, 'Processes').click();
  await expect(page.getByTestId('msg-handed-step')).toHaveCount(0, { timeout: 15_000 });
  await expect(page.getByTestId('step-task')).toHaveCount(0);
  await expect(page.getByTestId('conversation')).not.toContainText('Building the coverage filter');
  await expect(page.getByTestId('conversation')).not.toContainText('Step 4 of verify-phase');
  await page.getByTestId('history').click();
  await expect(page.getByTestId(`session-${sessionId}`)).toHaveCount(0);
  await expect(page.getByTestId(`session-${ownersSessionId()}`)).toBeVisible();
  await page.keyboard.press('Escape');
});

test('REQ-FN-084 a project whose newest session is a step session opens the owner\'s conversation, not the step\'s', async () => {
  await startWithHandedStep('finished');
  expect(sessionId).toBeGreaterThan(ownersSessionId());
  await page.reload({ waitUntil: 'domcontentloaded' });
  await openProject(page);
  await expect(page.getByTestId('chat-panel')).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('conversation-thread')).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('conversation')).not.toContainText('Building the coverage filter');
  await expect(page.getByTestId('conversation')).not.toContainText('Step 4 of verify-phase');
  await expect(page.getByTestId('agent-why')).not.toContainText('Flow master');
});

test('REQ-UI-071 a long unbroken path in a conversation message wraps inside the panel at 390', async () => {
  await openProject(page);
  await expect(page.getByTestId('conversation-thread')).toBeVisible({ timeout: 30_000 });
  const vOwn = ownersSessionId();
  const vPath = `C:\\Users\\owner\\source\\repos\\ProjectWithAVeryLongName\\src\\Component.Feature.Area\\Sub.Folder.Name\\${'VeryLongFileNameSegment'.repeat(6)}.cs`;
  run('INSERT INTO SessionEvent (SessionId, Seq, Kind, Payload, Model, Tokens, CreatedUtc) VALUES (?,?,?,?,?,?,?)', vOwn, 1, 'user', `Open ${vPath}`, null, null, now());
  run('INSERT INTO SessionEvent (SessionId, Seq, Kind, Payload, Model, Tokens, CreatedUtc) VALUES (?,?,?,?,?,?,?)', vOwn, 2, 'assistant', `Reading ${vPath}`, 'stub-edit-model', 5, now());
  try {
    await page.setViewportSize({ width: 390, height: 900 });
    await page.reload({ waitUntil: 'domcontentloaded' });
    await openProject(page);
    for (const vId of ['msg-1', 'msg-2']) {
      await expect(page.getByTestId(vId)).toContainText('VeryLongFileNameSegment', { timeout: 30_000 });
    }
    const vPanel = (await page.getByTestId('chat-panel').boundingBox())!;
    for (const vId of ['msg-1', 'msg-2']) {
      const vSaid = page.getByTestId(vId).locator('.said');
      const vBox = (await vSaid.boundingBox())!;
      expect(vBox.x + vBox.width, `${vId} stays inside the panel`).toBeLessThanOrEqual(vPanel.x + vPanel.width + 1);
      expect(await vSaid.evaluate((e) => e.scrollWidth <= e.clientWidth + 1), `${vId} is not clipped`).toBe(true);
    }
  } finally {
    run('DELETE FROM SessionEvent WHERE SessionId=?', vOwn);
    await page.setViewportSize({ width: 1280, height: 720 });
  }
});
