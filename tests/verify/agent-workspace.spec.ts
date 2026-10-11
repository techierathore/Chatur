// Verification of the Agent workspace tab (REQ-UI-053, REQ-UI-054, REQ-FN-058, REQ-FN-059, REQ-FN-060;
// mockups/agent-workspace.html). It opens from Project > Agent workspace... as a tab in the main window,
// the conversation still on the right. Black-box: drives the running web harness only. The fixture project gets its own checklist so the
// open-work table has known rows.
import { Page, expect } from '@playwright/test';
import { join } from 'path';
import { signIn } from './support/auth';
import { query, run } from './support/db';
import { openAgentsTab, tab } from './support/phase2-tabs';
import { startStubModel, ensureStubProvider, chooseStubModel, removeStubProvider, StubModel } from './support/stub-model';
import { test, NAME, PROJECT, resetFixture, openProject, write, SHOTS } from './support/workbench-a';

let page: Page;

const CHECKLIST = `# WbA Checklist

## Requirements Status

| ID | Title | Status | % | Remarks | Detail |
|---|---|---|---|---|---|
| REQ-NFR-001 | Cold start is quick | Blocked | 25% | — | [view](#d-req-nfr-001) |
| REQ-NFR-002 | Settings keep what you typed | In Progress | 50% | — | [view](#d-req-nfr-002) |
| REQ-NFR-003 | Already checked | Verified | 100% | — | [view](#d-req-nfr-003) |
| REQ-FN-001 | A flow master row | Not Started | 0% | — | [view](#d-req-fn-001) |
`;

test.beforeAll(async ({ browser }) => {
  page = await browser.newPage({ baseURL: process.env.BASE_URL });
  await signIn(page, 'Yolo');
});
test.afterAll(async () => { await page.close(); });
test.beforeEach(async () => {
  test.setTimeout(120_000);
  resetFixture();
  write(join(PROJECT, 'docs', 'WbA-Checklist.md'), CHECKLIST);
});

/** Opens the project, then Project > Agent workspace..., then picks an agent in the tab's own picker. */
async function openWorkspace(aAgent: string, aName: string): Promise<void> {
  await openProject(page);
  await openAgentsTab(page);
  await pickAgent(aAgent, aName);
}

async function pickAgent(aAgent: string, aName: string): Promise<void> {
  const vHeading = page.getByTestId('workspace-heading');
  if (!(await vHeading.innerText()).includes(`${aName}'s view`)) {
    await expect(async () => {
      await page.getByTestId('agent-picker-button').click();
      await page.getByTestId(`agent-${aAgent}`).click({ timeout: 2_500 });
      await expect(vHeading).toContainText(`${aName}'s view`, { timeout: 3_000 });
    }).toPass({ timeout: 30_000 });
  }
}

test('REQ-UI-053 an agent opens on a view of its own for the selected project', async () => {
  await openProject(page);
  await openAgentsTab(page);
  await expect(page.getByTestId('tool-area')).toBeVisible();
  await expect(page.getByTestId('chat-panel')).toBeVisible();
  await pickAgent('verifier', 'Verifier');
  const vHead = page.getByTestId('workspace-heading');
  await expect(vHead).toContainText("Verifier's view of " + NAME, { timeout: 30_000 });
  await expect(page.getByTestId('rights-panel')).toBeVisible();
  await expect(page.getByTestId('right-read')).toBeVisible();
  await expect(page.getByTestId('denied-source')).toBeVisible();
  await expect(page.getByTestId('denied-git')).toBeVisible();
  await expect(page.getByTestId('commands-panel')).toBeVisible();
  await expect(page.getByTestId('sessions-panel')).toBeVisible();
  await page.setViewportSize({ width: 1280, height: 1900 });
  await page.screenshot({ path: join(SHOTS, 'agent-workspace-verifier.png') });
  await page.setViewportSize({ width: 1280, height: 720 });

  // another agent: a different workspace, and the Verifier's work is not in it
  await pickAgent('flow-master', 'Flow master');
  await expect(page.getByTestId('workspace-heading')).toContainText("Flow master's view of " + NAME, { timeout: 20_000 });
  await expect(page.getByTestId('work-REQ-NFR-001')).toHaveCount(0);
  await page.screenshot({ path: join(SHOTS, 'agent-workspace-flowmaster.png'), fullPage: true });
});

test('REQ-UI-054 an agent lists the open requirements it owns with their ids and statuses', async () => {
  await openWorkspace('verifier', 'Verifier');
  await expect(page.getByTestId('work-REQ-NFR-001')).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('work-status-REQ-NFR-001')).toHaveText('blocked');
  await expect(page.getByTestId('work-status-REQ-NFR-002')).toHaveText('in progress');
  await expect(page.getByTestId('work-REQ-NFR-003')).toHaveCount(0);
  await expect(page.getByTestId('work-REQ-FN-001')).toHaveCount(0);
  await expect(page.getByTestId('open-work-count')).toHaveText('2 requirements');
  await expect(page.getByTestId('start-REQ-NFR-001')).toBeVisible();
});

test('REQ-UI-054 the Start button of a row is inside the panel, not clipped', async () => {
  await openWorkspace('verifier', 'Verifier');
  await expect(async () => {
    const vButton = await page.getByTestId('start-REQ-NFR-001').boundingBox();
    const vPanel = await page.getByTestId('open-work-panel').boundingBox();
    expect(vButton).not.toBeNull();
    expect(vPanel).not.toBeNull();
    expect(vButton!.x + vButton!.width).toBeLessThanOrEqual(vPanel!.x + vPanel!.width);
  }).toPass({ timeout: 30_000 });
});

// ---- rows that need the screen: rights decide what is offered, a command starts in the conversation,
// sessions left open stay open. The model's answer is fixed by a local stand-in (support/stub-model.ts);
// the provider row, the agent loop, the guards and the conversation are the real thing.

const thread = () => page.getByTestId('conversation-thread');

test('REQ-FN-058 an action the agent has no right to is not offered, and its right is listed once as withheld', async () => {
  await openWorkspace('verifier', 'Verifier');
  await expect(page.getByTestId('command-verify-phase')).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('run-verify-phase')).toBeEnabled();
  // The Verifier may not edit source, so the commands that need it are not on the screen at all.
  await expect(page.getByTestId('denied-source')).toBeVisible();
  for (const vCommand of ['build-phase', 'fix-issues', 'create-doc']) {
    await expect(page.getByTestId(`command-${vCommand}`)).toHaveCount(0);
    await expect(page.getByTestId(`run-${vCommand}`)).toHaveCount(0);
  }
  await expect(page.getByTestId('rights-note')).toContainText('not offered on this screen at all');
  await page.setViewportSize({ width: 1280, height: 1900 });
  await page.screenshot({ path: join(SHOTS, 'agent-workspace-rights-verifier.png') });
  await page.setViewportSize({ width: 1280, height: 720 });

  // The Flow master may edit source, so build-phase is offered there.
  await pickAgent('flow-master', 'Flow master');
  await expect(page.getByTestId('command-build-phase')).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('right-source')).toBeVisible();

  // Take the right away in the database and the command goes with it, on a fresh read of the workspace.
  const vWhere = "Action='edit-file' AND RoleId IN (SELECT RoleId FROM Role WHERE Code='flow-master')";
  const vOriginal = query(`SELECT RoleRightId, Allowed FROM RoleRight WHERE ${vWhere}`);
  try {
    run(`UPDATE RoleRight SET Allowed=0 WHERE ${vWhere}`);
    await pickAgent('verifier', 'Verifier');
    await pickAgent('flow-master', 'Flow master');
    await expect(page.getByTestId('denied-source')).toBeVisible({ timeout: 30_000 });
    await expect(page.getByTestId('command-build-phase')).toHaveCount(0);
    await expect(page.getByTestId('run-build-phase')).toHaveCount(0);
  } finally {
    for (const vRow of vOriginal) run('UPDATE RoleRight SET Allowed=? WHERE RoleRightId=?', vRow.Allowed, vRow.RoleRightId);
  }
});

test.describe('in the conversation', () => {
  let stub: StubModel;

  test.beforeAll(async () => {
    stub = await startStubModel('run-source-control', { command: 'status' });
    await ensureStubProvider(page, stub);
  });
  test.afterAll(async () => {
    await removeStubProvider(page);
    await stub.close();
  });

  /** Opens the workspace for an agent with the stub model chosen in the pinned conversation. */
  async function openWithStub(aAgent: string, aName: string): Promise<void> {
    await openProject(page);
    await expect(page.getByTestId('ask-input')).toBeEnabled({ timeout: 20_000 });
    await chooseStubModel(page);
    await openAgentsTab(page);
    await pickAgent(aAgent, aName);
  }

  test('REQ-FN-059 a command starts its agent on that command in the conversation', async () => {
    await openWithStub('verifier', 'Verifier');
    const vCalls = stub.calls.length;
    await page.getByTestId('run-verify-phase').click();
    // The command is the first message in the pinned conversation, and the reply streams in under it.
    await expect(thread().getByTestId('msg-1')).toContainText('*verify-phase', { timeout: 30_000 });
    await expect(thread().getByTestId('msg-1')).toContainText('You');
    await expect(thread().getByTestId('msg-2')).toContainText('Verifier', { timeout: 30_000 });
    await expect(thread().getByTestId('msg-2')).toContainText('Understood.', { timeout: 60_000 });
    expect(stub.calls.length).toBeGreaterThan(vCalls);
    // the main window around it did not move: tabs, conversation and the tab's own view are all still there
    await expect(page.getByTestId('tool-area')).toBeVisible();
    await expect(tab(page, 'Agents')).toBeVisible();
    await page.screenshot({ path: join(SHOTS, 'agent-workspace-command-started.png'), fullPage: true });
  });

  test('REQ-UI-054 Start on a requirement opens the agent in the conversation and sends the first message there', async () => {
    await openWithStub('verifier', 'Verifier');
    await expect(page.getByTestId('start-REQ-NFR-001')).toBeVisible({ timeout: 30_000 });
    await page.getByTestId('start-REQ-NFR-001').click();
    await expect(thread().getByTestId('msg-1')).toContainText('REQ-NFR-001', { timeout: 30_000 });
    await expect(thread().getByTestId('msg-2')).toContainText('Verifier', { timeout: 30_000 });
    await expect(thread().getByTestId('msg-2')).toContainText('Understood.', { timeout: 60_000 });
  });

  test('REQ-FN-060 changing agent leaves its sessions open, and one is opened again where it was left', async () => {
    await openWithStub('verifier', 'Verifier');
    const vCount = async () => Number(await page.getByTestId('sessions-count').innerText());
    const vOpen = () =>
      query("SELECT COUNT(*) AS n FROM Session WHERE State<>'Stopped' AND RoleId IN (SELECT RoleId FROM Role WHERE Code='verifier')")[0].n as number;
    const vBefore = await vCount();
    await page.getByTestId('run-verify-phase').click();
    await expect(thread().getByTestId('msg-2')).toContainText('Understood.', { timeout: 60_000 });
    await expect.poll(vCount, { timeout: 30_000 }).toBe(vBefore + 1);
    const vVerifierSessions = vOpen();

    // another agent: its own view, and the Verifier's sessions are still open when we come back
    await pickAgent('flow-master', 'Flow master');
    await expect(page.getByTestId('sessions-note')).toContainText('leaves these open');
    await pickAgent('verifier', 'Verifier');
    await expect.poll(vCount, { timeout: 30_000 }).toBe(vBefore + 1);
    expect(vOpen()).toBe(vVerifierSessions);
    await page.getByTestId('agent-picker-button').click();
    await expect(page.getByTestId('agent-verifier')).toContainText(`${vBefore + 1} open`);

    // open it again from the sessions panel: the conversation shows it as it was left
    await page.getByTestId('new-chat').click();
    await expect(thread().locator('.msg')).toHaveCount(0, { timeout: 15_000 });
    await page.getByTestId('sessions-panel').locator('a', { hasText: '*verify-phase' }).first().click();
    await expect(thread().getByTestId('msg-1')).toContainText('*verify-phase', { timeout: 20_000 });
  });
});
