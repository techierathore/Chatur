// Acceptance tests: the "writes documents" right (REQ-FN-103, REQ-FN-104, REQ-FN-105, REQ-UI-072; BRD-180..183;
// mockups/settings-agents.html, mockups/agent-workspace.html, mockups/process-run.html). Black-box, web harness.
// The right is read where the owner reads rights (Settings > Agents, Agent workspace) and proven where it acts: a
// handed step on Process run, with the scripted model of support/script-model.ts making the tool calls. Everything else
// - the provider row, the agent loop, the guard, the session record, the hand-over, the page - is real.
import { Page, expect } from '@playwright/test';
import { join } from 'path';
import { signIn } from './support/auth';
import { query } from './support/db';
import { openAgentsTab } from './support/phase2-tabs';
import { ensureStubProvider, removeStubProvider, STUB_MODEL } from './support/stub-model';
import { put, ScriptFn, ScriptedModel, startScriptedModel } from './support/script-model';
import { cleanUp, driveRun, Fixture, filesWritten, refusals, startOn, stepState, work } from './support/step-work';
import { test, openProject, resetFixture, SHOTS } from './support/workbench-a';

let page: Page;
let stub: ScriptedModel;

test.beforeAll(async ({ browser }) => {
  page = await browser.newPage({ baseURL: process.env.BASE_URL });
  await signIn(page, 'Owner');
  stub = await startScriptedModel(() => null);
  await ensureStubProvider(page, stub);
});
test.afterAll(async () => {
  cleanUp();
  await removeStubProvider(page);
  await stub.close();
  await page.close();
});
test.beforeEach(() => {
  test.setTimeout(300_000);
});
test.afterEach(() => cleanUp());

async function panelShot(aName: string) {
  await page.getByTestId('rights-panel').scrollIntoViewIfNeeded();
  await page.getByTestId('rights-panel').screenshot({ path: join(SHOTS, `document-rights-${aName}.png`) });
}

async function shot(aName: string) {
  await page.screenshot({ path: join(SHOTS, `document-rights-${aName}.png`), fullPage: true });
}

/** What the roles hold, as the decision words it: writes documents for the three that check and plan; edit-file as before. */
const ROLES: { code: string; name: string; writesDocs: boolean; editFile: boolean }[] = [
  { code: 'analyst', name: 'Analyst', writesDocs: true, editFile: false },
  { code: 'architect', name: 'Architect', writesDocs: true, editFile: true },
  { code: 'verifier', name: 'Verifier', writesDocs: true, editFile: false },
  { code: 'flow-master', name: 'Flow master', writesDocs: false, editFile: true },
];

async function openRoleOnSettings(aCode: string, aName: string): Promise<void> {
  await page.goto('/settings/agents', { waitUntil: 'domcontentloaded' });
  await expect(page.getByTestId('agent-list')).toBeVisible({ timeout: 90_000 });
  await page.waitForTimeout(2000); // let the Blazor circuit attach before clicking
  await expect(async () => {
    await page.getByTestId(`agent-${aCode}`).click({ timeout: 2_500 });
    await expect(page.getByTestId('agent-detail-head')).toContainText(aName, { timeout: 3_000 });
  }).toPass({ timeout: 30_000 });
  await page.getByTestId('subtab-button').click();
  await page.getByTestId('subtab-rights').click();
  await expect(page.getByTestId('rights-panel')).toBeVisible();
}

test('REQ-FN-104 each role on Settings: the Analyst, Architect and Verifier hold writes documents, the Flow master does not, source-file rights as before', async () => {
  for (const vRole of ROLES) {
    // the stored answer (what the migration seeded) ...
    const vStored = (aAction: string) =>
      Number(query('SELECT rr.Allowed AS Allowed FROM RoleRight rr JOIN Role r ON r.RoleId = rr.RoleId WHERE r.Code = ? AND rr.Action = ?', vRole.code, aAction)[0].Allowed);
    expect(vStored('write-docs') === 1, `${vRole.code} stored write-docs`).toBe(vRole.writesDocs);
    expect(vStored('edit-file') === 1, `${vRole.code} stored edit-file`).toBe(vRole.editFile);

    // ... and what the owner reads when the role is opened
    await openRoleOnSettings(vRole.code, vRole.name);
    const vDocs = page.getByTestId('right-write-docs');
    await expect(vDocs).toContainText('Writes documents');
    await expect(vDocs).toContainText(vRole.writesDocs ? /\bon\b/ : /\boff\b/);
    await expect(page.getByTestId('right-edit-file')).toContainText(vRole.editFile ? /\bon\b/ : /\boff\b/);
  }
});

test('REQ-UI-072 Settings > Agents lists "Writes documents" among the rights with the mockup\'s anchors, held or withheld', async () => {
  for (const vRole of ROLES) {
    await openRoleOnSettings(vRole.code, vRole.name);
    const vRow = page.getByTestId('right-writes-docs');
    await expect(vRow, `${vRole.code} row`).toBeVisible();
    await expect(vRow).toContainText('Writes documents');
    await expect(page.getByTestId('switch-writes-docs')).toBeVisible();
    await expect(vRow).toContainText(vRole.writesDocs ? 'on' : 'off');
    // it sits with the other rights, in the one panel, and the other rights are still all there
    for (const vOther of ['right-reads-code', 'right-changes-code', 'right-runs-commands', 'right-marks-verified']) {
      await expect(page.getByTestId('rights-panel').getByTestId(vOther)).toBeVisible();
    }
    await expect(page.getByTestId('rights-panel').getByTestId('right-writes-docs')).toBeVisible();
  }
  await openRoleOnSettings('analyst', 'Analyst');
  await page.setViewportSize({ width: 1280, height: 900 });
  await panelShot('settings-analyst-1280');
  await page.setViewportSize({ width: 390, height: 900 });
  await panelShot('settings-analyst-390');
  await openRoleOnSettings('flow-master', 'Flow master');
  await panelShot('settings-flowmaster-390');
  await page.setViewportSize({ width: 1280, height: 720 });
});

test('REQ-UI-072 the Agent workspace lists "write a document under docs/" as held, or says where it went for the Flow master', async () => {
  resetFixture();
  await openProject(page);
  await openAgentsTab(page);
  const vPick = async (aCode: string, aName: string) => {
    const vHeading = page.getByTestId('workspace-heading');
    if (!(await vHeading.innerText()).includes(`${aName}'s view`)) {
      await expect(async () => {
        await page.getByTestId('agent-picker-button').click();
        await page.getByTestId(`agent-${aCode}`).click({ timeout: 2_500 });
        await expect(vHeading).toContainText(`${aName}'s view`, { timeout: 3_000 });
      }).toPass({ timeout: 30_000 });
    }
  };

  for (const vRole of ROLES.filter((r) => r.writesDocs)) {
    await vPick(vRole.code, vRole.name);
    await expect(page.getByTestId('rights-allowed').getByTestId('right-docs'), vRole.code).toContainText('write a document under docs/');
    await expect(page.getByTestId('denied-docs')).toHaveCount(0);
  }
  await vPick('verifier', 'Verifier');
  await page.setViewportSize({ width: 1280, height: 1500 });
  await shot('workspace-verifier-1280');
  await page.setViewportSize({ width: 390, height: 1900 });
  await shot('workspace-verifier-390');

  // the Flow master does not hold it, and its withheld line says it is not a restriction
  await page.setViewportSize({ width: 1280, height: 720 });
  await vPick('flow-master', 'Flow master');
  await expect(page.getByTestId('right-docs')).toHaveCount(0);
  const vDenied = page.getByTestId('rights-denied').getByTestId('denied-docs');
  await expect(vDenied).toContainText('write a document under docs/');
  await expect(vDenied).toContainText('already covered by writing to a source file');
  await expect(vDenied).not.toContainText('belongs to');
  await page.setViewportSize({ width: 390, height: 1900 });
  await shot('workspace-flowmaster-390');
  await page.setViewportSize({ width: 1280, height: 1500 });
  await shot('workspace-flowmaster-1280');
  await page.setViewportSize({ width: 1280, height: 720 });
});

// ---- the right at work: a handed step on Process run ---------------------------------------------------------------

// A brief with no measure of success: step 4's check then stops the run, so the page is still on the step that was handed.
const BRIEF_UNFINISHED = 'The audience is small shops. The problem is stock counting.\n';

const day1 = new Fixture('SwDocRights');
const setupDay1 = () => day1.reset().write('README.md', '# Sw\n\nAn app that counts stock for small shops.\n').write('src/Existing.cs', 'class Existing {}\n');

async function runDay1(aScript: ScriptFn) {
  setupDay1();
  stub.setScript(aScript);
  const vRunId = await startOn(page, day1.name, 'day1-greenfield');
  const vEnd = await driveRun(page);
  return { runId: vRunId, end: vEnd };
}

test('REQ-FN-103 an agent with only writes documents is refused a path that resolves outside docs/, even through "..", with the reason', async () => {
  const vAbsolute = join(day1.dir, 'src', 'Absolute.cs');
  const vRun = await runDay1((c) =>
    c.process === 'day1-greenfield' && c.step === 3
      ? { calls: [put('docs/../src/Escape.cs', 'class Escape {}'), put(vAbsolute, 'class Absolute {}'), put('docs/Sw-Brief.md', BRIEF_UNFINISHED)], final: 'I tried three writes.' }
      : { calls: [], final: 'I read what the step asks for.' },
  );
  expect(vRun.end).toBe('stopped');

  // the Analyst holds writes documents and not edit-file, so these two escapes are refused and say why
  const vRefused = refusals(vRun.runId, 3);
  expect(vRefused).toHaveLength(2);
  expect(vRefused[0]).toContain('docs/../src/Escape.cs');
  expect(vRefused[0]).toContain('edit-file');
  expect(vRefused[1]).toContain(vAbsolute);
  expect(day1.exists('src/Escape.cs')).toBe(false);
  expect(day1.exists('src/Absolute.cs')).toBe(false);
  expect(day1.read('src/Existing.cs')).toBe('class Existing {}\n');
  // while the document inside docs/ went through
  expect(day1.exists('docs/Sw-Brief.md')).toBe(true);
  expect(work(vRun.runId, 3)).toMatchObject({ Agent: 'Analyst', Model: STUB_MODEL });
  await expect(page.getByTestId('handed-step-refusal').first()).toContainText('docs/');
  await shot('refused-outside-docs');
});

test('REQ-FN-105 a handed step with only writes documents writes its docs/ files, and a source write is refused and recorded on the step', async () => {
  const vRun = await runDay1((c) =>
    c.process === 'day1-greenfield' && c.step === 3
      ? { calls: [put('docs/Sw-Brief.md', BRIEF_UNFINISHED), put('src/Feature.cs', 'class Feature {}')], final: 'I wrote the brief; the source file was not mine to write.' }
      : { calls: [], final: 'I read what the step asks for.' },
  );
  expect(vRun.end).toBe('stopped');

  expect(filesWritten(vRun.runId, 3)).toEqual(['docs/Sw-Brief.md']);
  expect(day1.exists('docs/Sw-Brief.md')).toBe(true);
  expect(day1.exists('src/Feature.cs')).toBe(false);
  const vRefused = refusals(vRun.runId, 3);
  expect(vRefused).toHaveLength(1);
  expect(vRefused[0]).toContain('src/Feature.cs');
  expect(vRefused[0]).toContain('edit-file');
  await expect(stepState(page, 3)).toHaveText('done'); // the brief is there, so the step's own check passes
  await expect(page.getByTestId('handed-step-refusal').first()).toContainText('src/Feature.cs');
  await shot('docs-written-source-refused');
});
