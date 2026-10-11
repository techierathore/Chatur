// Acceptance tests: each document process runs LIVE through its own agent and its checks pass only when the documents are
// whole (REQ-FN-092, 095, 097, 098, 099, 100, 101, 102; BRD-169..179; mockups/process-run.html). Black-box, web harness.
// The model is the scripted one of support/script-model.ts: for every handed step it makes the tool calls the step's wording
// asks for (a good agent) or leaves something out (a careless one). Everything else - the provider row, the agent loop, the
// agent's rights, the hand-over, the checks reading the disk, the run's page - is real. Each row's test starts the process
// from Processes on a fixture project under tests/.artifacts and proves both outcomes: the scripted agent that does what
// "done" means is passed and the run reaches its end (the owner's stops answered yes); an agent that leaves something out
// stops the run at the named check with its reason, read back from the disk and the page.
//
// Analyst (day1-greenfield, amend-docs) and Verifier (triage-issues, metrics-report) hold "writes documents" and no
// edit-file right (owner decision 2026-10-09), so their documents under docs/ are written for real and both outcomes
// are proven live like the other processes.
import { Page, expect } from '@playwright/test';
import { join } from 'path';
import { signIn } from './support/auth';
import { query } from './support/db';
import { ensureStubProvider, removeStubProvider, STUB_MODEL } from './support/stub-model';
import { put, ScriptFn, ScriptedModel, startScriptedModel, StepScript } from './support/script-model';
import { brd, checklist, cleanUp, driveRun, Fixture, failedWhy, filesWritten, refusals, startOn, stepResult, stepState, today, work } from './support/step-work';
import { test, SHOTS } from './support/workbench-a';

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
  test.setTimeout(420_000);
});
test.afterEach(() => cleanUp());

/** The step the scripted agent does nothing in: it reads, and says so. */
const reads = (aText = 'I read what the step asks for.'): StepScript => ({ calls: [], final: aText });

/** Runs one scenario: the fixture is set up, the agent scripted, the process started from Processes and driven to its end. */
async function scenario(aFixture: Fixture, aSetup: () => void, aProcess: string, aScript: ScriptFn) {
  aSetup();
  stub.setScript(aScript);
  const vRunId = await startOn(page, aFixture.name, aProcess);
  const vEnd = await driveRun(page);
  return { runId: vRunId, end: vEnd };
}

async function shot(aName: string) {
  await page.screenshot({ path: join(SHOTS, `process-step-work-${aName}.png`), fullPage: true });
}

const rows = (aIds: number[], aStatus = 'Not Started', aMockup?: string) =>
  aIds.map((n) => ({ id: `REQ-FN-${String(n).padStart(3, '0')}`, title: `Requirement ${n}`, status: aStatus, brd: n, acceptance: `When the owner does ${n}, then ${n} happens.`, mockup: aMockup }));

// ---- split-brd (Architect) -----------------------------------------------------------------------------------------

const split = new Fixture('SwSplit');
const setupSplit = () =>
  split.reset().write('docs/Sw-BRD.md', brd('Sw', [1, 2, 3, 4])).write('docs/Sw-Checklist.md', checklist('Sw', rows([1, 2, 3, 4])));
const phases = (aSecond: string) =>
  `# Sw — Phases\n\n| Phase | Scope | BRD range | Status |\n|---|---|---|---|\n| 1 | Sign in | BRD-1 to BRD-2 | Not started |\n| 2 | Reports | ${aSecond} | Not started |\n`;
const splitScript =
  (aMode: 'good' | 'missing-id' | 'uncut'): ScriptFn =>
  (c) => {
    if (c.process !== 'split-brd') return null;
    if (c.step === 2) {
      return { opening: 'Grouping the requirements into two phases.', calls: [put('docs/Sw-Phases.md', phases(aMode === 'missing-id' ? 'BRD-3' : 'BRD-3 to BRD-4'))], final: 'I wrote docs/Sw-Phases.md: phase 1 holds BRD-1 to BRD-2 and phase 2 the rest.' };
    }
    if (c.step === 4) {
      const vCalls = [
        put('docs/Sw-P2-BRD.md', brd('Sw P2', [3, 4])),
        put('docs/Sw-Checklist.md', checklist('Sw', rows([1, 2]))),
        put('docs/Sw-P2-Checklist.md', checklist('Sw P2', rows([3, 4]))),
      ];
      if (aMode !== 'uncut') vCalls.unshift(put('docs/Sw-BRD.md', brd('Sw', [1, 2])));
      return { calls: vCalls, final: 'I wrote one BRD and one checklist per phase.' };
    }
    return reads();
  };

test('REQ-FN-092 split-brd runs through the Architect and its checks pass only when every id is in exactly one phase', async () => {
  // (b1) the Architect leaves BRD-4 out of the phases file -> step 3 names it.
  const vB1 = await scenario(split, setupSplit, 'split-brd', splitScript('missing-id'));
  expect(vB1.end).toBe('stopped');
  expect(await failedWhy(page)).toContain('BRD-4 is in no phase');
  await expect(stepState(page, 3)).toHaveText('check failed');
  await expect(stepState(page, 2)).toHaveText('done'); // a step with its check: the agent's work was handed and done
  expect(split.read('docs/Sw-Phases.md')).not.toContain('BRD-4');
  await shot('split-brd-stopped');

  // (b2) the phases are right but the BRD is not cut: phase 1's file still holds every id -> step 5 names them.
  const vB2 = await scenario(split, setupSplit, 'split-brd', splitScript('uncut'));
  expect(vB2.end).toBe('stopped');
  const vWhy = await failedWhy(page);
  expect(vWhy).toContain("BRD-3 is in docs/Sw-BRD.md but not in phase 1's range");
  await expect(stepState(page, 5)).toHaveText('check failed');
  expect(split.read('docs/Sw-BRD.md')).toContain('BRD-4');

  // (a) a whole split: the checks pass and the run reaches its end after the owner's stop.
  const vA = await scenario(split, setupSplit, 'split-brd', splitScript('good'));
  expect(vA.end).toBe('finished');
  await expect(stepState(page, 3)).toHaveText('check passed');
  await expect(stepState(page, 5)).toHaveText('check passed');
  expect(split.read('docs/Sw-P2-BRD.md')).toContain('BRD-4');
  expect(split.read('docs/Sw-BRD.md')).not.toContain('BRD-3');
  expect(work(vA.runId, 4)).toMatchObject({ Agent: 'Architect', Model: STUB_MODEL, State: 'finished' });
  expect(filesWritten(vA.runId, 4)).toEqual(['docs/Sw-BRD.md', 'docs/Sw-P2-BRD.md', 'docs/Sw-Checklist.md', 'docs/Sw-P2-Checklist.md']);
  await expect(stepResult(page, 4)).toContainText('The Architect (' + STUB_MODEL + ') wrote 4 files');
  await shot('split-brd-finished');
});

// ---- deploy-checklist (Architect) ----------------------------------------------------------------------------------

const deploy = new Fixture('SwDeploy');
const setupDeploy = () => deploy.reset().write('README.md', '# Sw\n\nReleased by a pipeline in .github/workflows/release.yml, version 1.2.0.\n').write('docs/Sw-Brief.md', 'A brief.\n');
const section = (aTitle: string, aWho: string | null) => `## ${aTitle}\n\n- [ ] ${aTitle} the release — ${aWho ? 'Who: ' + aWho : 'someone'} (pipeline, secrets, version 1.2.0)\n`;
const deployList = (aMode: 'good' | 'no-rollback' | 'no-owner') =>
  '# Release list\n\n' +
  ['Before', 'Deploy', 'After', ...(aMode === 'no-rollback' ? [] : ['Rollback']), 'Secrets', 'Proven'].map((s) => section(s, aMode === 'no-owner' && s === 'After' ? null : 'Dana')).join('\n');
const deployScript =
  (aMode: 'good' | 'no-rollback' | 'no-owner'): ScriptFn =>
  (c) => {
    if (c.process !== 'deploy-checklist') return null;
    if (c.step === 2) return { calls: [put('docs/Sw-Deploy-Checklist.md', deployList(aMode))], final: 'I wrote docs/Sw-Deploy-Checklist.md.' };
    return reads();
  };

test('REQ-FN-101 deploy-checklist runs through the Architect and its checks pass only when the list is whole and every item names who', async () => {
  // (b1) no rollback section -> step 2.
  const vB1 = await scenario(deploy, setupDeploy, 'deploy-checklist', deployScript('no-rollback'));
  expect(vB1.end).toBe('stopped');
  expect(await failedWhy(page)).toContain('has no "rollback" section');
  await expect(stepState(page, 2)).toHaveText('check failed');
  expect(deploy.read('docs/Sw-Deploy-Checklist.md')).not.toContain('## Rollback');

  // (b2) every section, but one item names no one -> step 3.
  const vB2 = await scenario(deploy, setupDeploy, 'deploy-checklist', deployScript('no-owner'));
  expect(vB2.end).toBe('stopped');
  expect(await failedWhy(page)).toContain('names no one');
  await expect(stepState(page, 3)).toHaveText('check failed');

  // (a) a whole list.
  const vA = await scenario(deploy, setupDeploy, 'deploy-checklist', deployScript('good'));
  expect(vA.end).toBe('finished');
  await expect(stepState(page, 2)).toHaveText('done');
  for (const vStep of [3, 5]) await expect(stepState(page, vStep)).toHaveText('check passed');
  expect(filesWritten(vA.runId, 2)).toEqual(['docs/Sw-Deploy-Checklist.md']);
  expect(work(vA.runId, 2)).toMatchObject({ Agent: 'Architect', Model: STUB_MODEL });
  await shot('deploy-checklist-finished');
});

// ---- mockups (Architect) -------------------------------------------------------------------------------------------

const mockups = new Fixture('SwMockups');
const uiRow = (aMockup?: string) => [{ id: 'REQ-UI-001', title: 'Sign in', status: 'Not Started', brd: 1, acceptance: 'When the owner signs in, then the start window opens.', mockup: aMockup }];
const setupMockups = () =>
  mockups
    .reset()
    .write('docs/Sw-BRD.md', brd('Sw', [1]))
    .write('docs/Sw-UIDesign.md', '# Sw — UI design\n\n## Screens\n\n### Screen: Sign in (`/signin`)\n\n**Mockup:** [mockups/sign-in.html](mockups/sign-in.html)\n')
    .write('docs/Sw-Checklist.md', checklist('Sw', uiRow()));
const design = (aStates: string) =>
  `# Sw — UI design\n\n## Screens\n\n### Screen: Sign in (\`/signin\`)\n\n**Mockup:** [mockups/sign-in.html](mockups/sign-in.html)\n\n| Region | Control | Shows |\n|---|---|---|\n| form | Card | the fields |\n\n**States:** ${aStates}\n`;
const mockupsScript =
  (aMode: 'good' | 'no-anchor' | 'no-error-state' | 'unlinked'): ScriptFn =>
  (c) => {
    if (c.process !== 'mockups') return null;
    if (c.step === 2) {
      return {
        calls: [
          put('docs/mockups/sign-in.html', aMode === 'no-anchor' ? '<html><body><form></form></body></html>' : '<html><body><form data-testid="signin-form"></form></body></html>'),
          put('docs/Sw-UIDesign.md', design(aMode === 'no-error-state' ? 'empty: nothing yet · loading: a spinner' : 'empty: nothing yet · loading: a spinner · error: the message')),
        ],
        final: 'I drew docs/mockups/sign-in.html and described the screen in docs/Sw-UIDesign.md.',
      };
    }
    if (c.step === 4 && aMode !== 'unlinked') return { calls: [put('docs/Sw-Checklist.md', checklist('Sw', uiRow('mockups/sign-in.html')))], final: 'I linked REQ-UI-001 to its mockup.' };
    return reads();
  };

test('REQ-FN-102 mockups runs through the Architect and its checks pass only when every screen is drawn, described and linked', async () => {
  // (b1) the mockup carries no data-testid anchor -> step 2.
  expect((await scenario(mockups, setupMockups, 'mockups', mockupsScript('no-anchor'))).end).toBe('stopped');
  expect(await failedWhy(page)).toContain('docs/mockups/sign-in.html has no data-testid anchor');
  await expect(stepState(page, 2)).toHaveText('check failed');
  expect(mockups.read('docs/mockups/sign-in.html')).not.toContain('data-testid');

  // (b2) the design leaves out the error state -> step 3.
  expect((await scenario(mockups, setupMockups, 'mockups', mockupsScript('no-error-state'))).end).toBe('stopped');
  expect(await failedWhy(page)).toContain('does not describe its error state');
  await expect(stepState(page, 3)).toHaveText('check failed');

  // (b3) screens drawn and described but the UI row is not linked to its mockup -> step 4.
  expect((await scenario(mockups, setupMockups, 'mockups', mockupsScript('unlinked'))).end).toBe('stopped');
  expect(await failedWhy(page)).toContain('REQ-UI-001 names no mockup');
  await expect(stepState(page, 4)).toHaveText('check failed');

  // (a) drawn, described and linked.
  const vA = await scenario(mockups, setupMockups, 'mockups', mockupsScript('good'));
  expect(vA.end).toBe('finished');
  for (const vStep of [2, 4]) await expect(stepState(page, vStep)).toHaveText('done');
  await expect(stepState(page, 3)).toHaveText('check passed');
  expect(mockups.read('docs/Sw-Checklist.md')).toContain('*Mockup:* mockups/sign-in.html');
  expect(filesWritten(vA.runId, 2)).toEqual(['docs/mockups/sign-in.html', 'docs/Sw-UIDesign.md']);
  await shot('mockups-finished');
});

// ---- handoff-phase (Flow master) -----------------------------------------------------------------------------------

const handoff = new Fixture('SwHandoff');
const phasesFile = (aStatus: string) => `# Sw — Phases\n\n| Phase | Scope | BRD range | Status |\n|---|---|---|---|\n| 1 | Everything | BRD-1 to BRD-2 | ${aStatus} |\n`;
const setupHandoff = (aSecondStatus: string) => () =>
  handoff
    .reset()
    .write('docs/Sw-BRD.md', brd('Sw', [1, 2]))
    .write('docs/Sw-Checklist.md', checklist('Sw', [...rows([1], 'Verified'), ...rows([2], aSecondStatus)]))
    .write('docs/Sw-Phases.md', phasesFile('In progress'))
    .write('PROJECT-STATUS.md', '# Status\n\nPhase 1 is in progress.\n');
const handoffScript =
  (aMarkPhase: boolean): ScriptFn =>
  (c) => {
    if (c.process !== 'handoff-phase') return null;
    if (c.step === 4) {
      const vCalls = [put('PROJECT-STATUS.md', `# Status\n\nHand-off ${today()}: phase 1 is closed and phase 2 opens.\n`)];
      if (aMarkPhase) vCalls.push(put('docs/Sw-Phases.md', phasesFile('Done')));
      return { calls: vCalls, final: 'I wrote the hand-off into PROJECT-STATUS.md.' };
    }
    return reads();
  };

test('REQ-FN-095 handoff-phase runs through the Flow master and its checks pass only when no row is open and the hand-off is recorded', async () => {
  // (b1) a row of the phase is still In Progress -> step 2 names it.
  expect((await scenario(handoff, setupHandoff('In Progress'), 'handoff-phase', handoffScript(true))).end).toBe('stopped');
  expect(await failedWhy(page)).toContain('REQ-FN-002 is In Progress');
  await expect(stepState(page, 2)).toHaveText('check failed');

  // (b2) the hand-off is written but the phases file does not mark the phase done -> step 4.
  expect((await scenario(handoff, setupHandoff('Verified'), 'handoff-phase', handoffScript(false))).end).toBe('stopped');
  expect(await failedWhy(page)).toContain('does not mark phase 1 done');
  await expect(stepState(page, 4)).toHaveText('check failed');
  expect(handoff.read('PROJECT-STATUS.md')).toContain('Hand-off ' + today());

  // (a) closed in both documents.
  const vA = await scenario(handoff, setupHandoff('Verified'), 'handoff-phase', handoffScript(true));
  expect(vA.end).toBe('finished');
  await expect(stepState(page, 2)).toHaveText('check passed');
  await expect(stepState(page, 4)).toHaveText('done');
  expect(handoff.read('docs/Sw-Phases.md')).toContain('Done');
  expect(filesWritten(vA.runId, 4)).toEqual(['PROJECT-STATUS.md', 'docs/Sw-Phases.md']);
  expect(work(vA.runId, 4)).toMatchObject({ Agent: 'Flow master', Model: STUB_MODEL });
  await shot('handoff-phase-finished');
});

// ---- the four processes whose agents hold "writes documents" (owner decision 2026-10-09) --------------------------------

/** What the roles' seeded rights say: the document right is held, the source right is not, so every write below is a docs/ write. */
const right = (aRole: string, aAction: string) =>
  Number(query('SELECT rr.Allowed AS Allowed FROM RoleRight rr JOIN Role r ON r.RoleId = rr.RoleId WHERE r.Name = ? AND rr.Action = ? ORDER BY r.Version DESC LIMIT 1', aRole, aAction)[0].Allowed);
const holdsOnlyDocuments = (aRole: string) => {
  expect(right(aRole, 'write-docs')).toBe(1);
  expect(right(aRole, 'edit-file')).toBe(0);
};

const BRIEF = '# Brief\n\nThe audience is small shops. The problem is stock counting. The measure of success is a count in five minutes.\n';
const BRIEF_NO_MEASURE = '# Brief\n\nThe audience is small shops. The problem is stock counting.\n';

// ---- day1-greenfield (Analyst) ----

const day1 = new Fixture('SwDay1');
const setupDay1 = () => day1.reset().write('README.md', '# Sw\n\nAn app that counts stock for small shops.\n');
const day1Script =
  (aMode: 'good' | 'no-measure'): ScriptFn =>
  (c) => {
    if (c.process !== 'day1-greenfield') return null;
    if (c.step === 3) return { calls: [put('docs/Sw-Brief.md', aMode === 'good' ? BRIEF : BRIEF_NO_MEASURE)], final: 'I drafted docs/Sw-Brief.md.' };
    if (c.step === 5) return { calls: [put('docs/Sw-BRD.md', brd('Sw', [1, 2]))], final: 'I wrote docs/Sw-BRD.md.' };
    if (c.step === 7) {
      return {
        calls: [
          put('docs/Sw-Architecture.md', '# Sw — Architecture\n\nOne app, one database.\n'),
          put('docs/Sw-UIDesign.md', design('empty: nothing yet · loading: a spinner · error: the message')),
          put('docs/mockups/sign-in.html', '<html><body><form data-testid="signin-form"></form></body></html>'),
          put('docs/Sw-Checklist.md', checklist('Sw', rows([1, 2]))),
        ],
        final: 'I wrote the architecture, the UI design with its mockup, and the checklist.',
      };
    }
    return reads();
  };

test('REQ-FN-100 day1-greenfield runs through the Analyst, which writes its documents under docs/; the checks pass when they are whole and name what is missing when not', async () => {
  holdsOnlyDocuments('Analyst');

  // (b) the Analyst's brief names no measure of success -> step 4 names it, and what the Analyst wrote is on disk.
  const vB = await scenario(day1, setupDay1, 'day1-greenfield', day1Script('no-measure'));
  expect(vB.end).toBe('stopped');
  expect(await failedWhy(page)).toContain('docs/Sw-Brief.md has no measure of success');
  await expect(stepState(page, 3)).toHaveText('done');
  await expect(stepState(page, 4)).toHaveText('check failed');
  expect(day1.read('docs/Sw-Brief.md')).not.toContain('measure');
  expect(refusals(vB.runId, 3)).toEqual([]);

  // (a) every document written: the checks pass and the run reaches its end after the owner's three stops.
  const vA = await scenario(day1, setupDay1, 'day1-greenfield', day1Script('good'));
  expect(vA.end).toBe('finished');
  await expect(stepState(page, 4)).toHaveText('check passed');
  await expect(stepState(page, 5)).toHaveText('done');
  await expect(stepState(page, 8)).toHaveText('check passed');
  expect(filesWritten(vA.runId, 3)).toEqual(['docs/Sw-Brief.md']);
  expect(filesWritten(vA.runId, 7)).toEqual(['docs/Sw-Architecture.md', 'docs/Sw-UIDesign.md', 'docs/mockups/sign-in.html', 'docs/Sw-Checklist.md']);
  for (const vStep of [3, 5, 7]) expect(refusals(vA.runId, vStep)).toEqual([]);
  expect(work(vA.runId, 3)).toMatchObject({ Agent: 'Analyst', Model: STUB_MODEL, State: 'finished' });
  await shot('day1-greenfield-finished');
});

// ---- amend-docs (Analyst) ----

const amend = new Fixture('SwAmend');
const setupAmend = () => amend.reset().write('docs/Sw-BRD.md', brd('Sw', [1, 2])).write('docs/Sw-Checklist.md', checklist('Sw', rows([1, 2])));
const amendScript =
  (aMode: 'good' | 'no-row'): ScriptFn =>
  (c) => {
    if (c.process !== 'amend-docs') return null;
    if (c.step === 2) return reads('The late change touches docs/Sw-BRD.md and docs/Sw-Checklist.md.');
    if (c.step === 5) {
      const vCalls = [put('docs/Sw-BRD.md', brd('Sw', [1, 2, 3])), put('docs/Sw-Checklist.md', checklist('Sw', rows(aMode === 'good' ? [1, 2, 3] : [1, 2])))];
      return { calls: vCalls, final: 'I amended docs/Sw-BRD.md and docs/Sw-Checklist.md.' };
    }
    return reads();
  };

test('REQ-FN-097 amend-docs runs through the Analyst, which amends the documents under docs/; the check passes when they agree and names the missing row when not', async () => {
  holdsOnlyDocuments('Analyst');

  // (b) the BRD gets BRD-3 but the checklist gets no row for it -> step 5 names it.
  const vB = await scenario(amend, setupAmend, 'amend-docs', amendScript('no-row'));
  expect(vB.end).toBe('stopped');
  expect(await failedWhy(page)).toContain('new BRD-3 has no checklist row');
  await expect(stepState(page, 3)).toHaveText('check passed');
  await expect(stepState(page, 5)).toHaveText('check failed');
  expect(amend.read('docs/Sw-BRD.md')).toContain('BRD-3');
  expect(refusals(vB.runId, 5)).toEqual([]);

  // (a) the late change is in every document that has to carry it.
  const vA = await scenario(amend, setupAmend, 'amend-docs', amendScript('good'));
  expect(vA.end).toBe('finished');
  await expect(stepState(page, 5)).toHaveText('done');
  expect(filesWritten(vA.runId, 5)).toEqual(['docs/Sw-BRD.md', 'docs/Sw-Checklist.md']);
  expect(amend.read('docs/Sw-Checklist.md')).toContain('REQ-FN-003');
  expect(work(vA.runId, 5)).toMatchObject({ Agent: 'Analyst', Model: STUB_MODEL, State: 'finished' });
  await shot('amend-docs-finished');
});

// ---- triage-issues (Verifier) ----

const triage = new Fixture('SwTriage');
const setupTriage = () =>
  triage
    .reset()
    .write('docs/Sw-BRD.md', brd('Sw', [1]))
    .write('docs/Sw-Checklist.md', checklist('Sw', rows([1], 'Verified')))
    .write('docs/issues/login-crash.md', '# Login crashes on an empty password\n\nPress Sign in with no password.\n')
    .write('docs/issues/export-date.md', '# Export loses the date\n\nExport a report and the date column is empty.\n');
const reportRow = (aId: string, aTitle: string, aBrd: number) => ({ id: aId, title: aTitle, status: 'Not Started', brd: aBrd, acceptance: `When ${aTitle.toLowerCase()}, then it is fixed.` });
const triageScript =
  (aMode: 'good' | 'one-missing'): ScriptFn =>
  (c) => {
    if (c.process !== 'triage-issues') return null;
    if (c.step === 4) {
      const vRows = [...rows([1], 'Verified'), reportRow('REQ-FN-002', 'Login crashes on an empty password', 1), ...(aMode === 'good' ? [reportRow('REQ-FN-003', 'Export loses the date', 1)] : [])];
      return { calls: [put('docs/Sw-Checklist.md', checklist('Sw', vRows))], final: 'I added the reports to docs/Sw-Checklist.md.' };
    }
    return reads();
  };

test('REQ-FN-098 triage-issues runs through the Verifier, which writes the checklist rows under docs/; the check passes when every report has a row and names the one without', async () => {
  holdsOnlyDocuments('Verifier');

  // (b) only one of the two reports becomes a row -> step 4 names the other.
  const vB = await scenario(triage, setupTriage, 'triage-issues', triageScript('one-missing'));
  expect(vB.end).toBe('stopped');
  expect(await failedWhy(page)).toContain('docs/issues/export-date.md ("Export loses the date") has no new Not Started row');
  await expect(stepState(page, 3)).toHaveText('check passed');
  await expect(stepState(page, 4)).toHaveText('check failed');
  expect(triage.read('docs/Sw-Checklist.md')).toContain('Login crashes');
  expect(refusals(vB.runId, 4)).toEqual([]);

  // (a) both reports are rows.
  const vA = await scenario(triage, setupTriage, 'triage-issues', triageScript('good'));
  expect(vA.end).toBe('finished');
  await expect(stepState(page, 4)).toHaveText('done');
  expect(filesWritten(vA.runId, 4)).toEqual(['docs/Sw-Checklist.md']);
  expect(work(vA.runId, 4)).toMatchObject({ Agent: 'Verifier', Model: STUB_MODEL, State: 'finished' });
  await shot('triage-issues-finished');
});

// ---- metrics-report (Verifier) ----

const metrics = new Fixture('SwMetrics');
const setupMetrics = () =>
  metrics
    .reset()
    .write('docs/metrics/runs.jsonl', '{"tokens_in":100,"tokens_out":50,"build_result":"pass"}\n{"tokens_in":100,"tokens_out":50,"build_result":"fail"}\n')
    .write('docs/metrics/gates.jsonl', '{"verdict":"FAIL"}\n');
const metricsScript =
  (aTokens: number): ScriptFn =>
  (c) =>
    c.process === 'metrics-report' && c.step === 2
      ? { calls: [put('docs/Sw-Metrics-Report.md', `# Metrics\n\nRuns: 2\nTokens: ${aTokens}\nFailures: 2\n`)], final: 'I wrote docs/Sw-Metrics-Report.md.' }
      : reads();

test('REQ-FN-099 metrics-report runs through the Verifier, which writes the report under docs/; the check passes when it matches the records and names the figure that does not', async () => {
  holdsOnlyDocuments('Verifier');

  // (b) the report gives 250 tokens where the records add up to 300 -> step 4 names the figure.
  const vB = await scenario(metrics, setupMetrics, 'metrics-report', metricsScript(250));
  expect(vB.end).toBe('stopped');
  await expect(stepState(page, 3)).toHaveText('check passed');
  expect(await failedWhy(page)).toContain('says 250 tokens but the records add up to 300');
  await expect(stepState(page, 4)).toHaveText('check failed');
  expect(refusals(vB.runId, 2)).toEqual([]);
  expect(metrics.exists('docs/Sw-Metrics-Report.md')).toBe(true);

  // (a) the report is the records' totals.
  const vA = await scenario(metrics, setupMetrics, 'metrics-report', metricsScript(300));
  expect(vA.end).toBe('finished');
  await expect(stepState(page, 3)).toHaveText('check passed');
  await expect(stepState(page, 4)).toHaveText('done');
  expect(filesWritten(vA.runId, 2)).toEqual(['docs/Sw-Metrics-Report.md']);
  expect(work(vA.runId, 2)).toMatchObject({ Agent: 'Verifier', Model: STUB_MODEL, State: 'finished' });
  await shot('metrics-report-finished');
});

// ---- the conversation while a step handed over for real is being worked --------------------------------------------

test('REQ-UI-071 the agent\'s messages and tool calls appear in the conversation, under its name, while the engine\'s step is worked', async () => {
  setupSplit();
  stub.setScript((c) =>
    c.process === 'split-brd' && c.step === 2
      ? { opening: 'Grouping the requirements into two phases.', calls: [put('docs/Sw-Phases.md', phases('BRD-3 to BRD-4'))], final: 'I wrote docs/Sw-Phases.md.', delayBeforeFinalMs: 60_000 }
      : reads(),
  );
  const vRunId = await startOn(page, split.name, 'split-brd');

  // while the step is still being worked (the model holds its closing message back), the conversation already follows the session
  await expect(page.getByTestId('handed-step-state')).toContainText('working', { timeout: 60_000 });
  // the thread is the agent's session: it shows the step it was handed and the tool call it made, as it happened
  const vConversation = page.getByTestId('conversation');
  await expect(vConversation).toContainText('You have been handed one step of a Chatur process run', { timeout: 30_000 });
  await expect(vConversation).toContainText('Edit "docs/Sw-Phases.md"', { timeout: 30_000 });
  await expect(page.getByTestId('handed-step-state')).toContainText('working'); // and the step had not ended yet
  await shot('conversation-live');

  // while step 2 is the handed step (its closing message is held back, so the run cannot have moved on) the thread is under the
  // agent's name: the task it was handed is "Task for Architect" and names the step, and its tool call is listed beneath
  const vTask = page.getByTestId('step-task').last();
  await expect(vTask).toBeVisible({ timeout: 30_000 });
  await expect(vConversation).toContainText('Task for Architect');
  await expect(vConversation).toContainText(`split-brd run ${vRunId}, step 2`);
  await expect(page.getByTestId('handed-step-state')).toContainText('working');
  // then the run goes on
  expect(await driveRun(page)).toBe('stopped');
});
