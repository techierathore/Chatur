// Acceptance tests: build-phase and fix-issues run LIVE through the Flow master on a small real .NET project, whose build
// and tests Chatur runs itself (REQ-FN-093, REQ-FN-094; BRD-171, BRD-172), and Process run shows the agent at work on a
// step the engine handed over for real (REQ-UI-070; mockups/process-run.html). Black-box, web harness.
// The model is the scripted one of support/script-model.ts: for each handed step it makes the tool calls the step's wording
// asks for (write the code, write the tests, record the rows) or leaves one thing out. The provider row, the agent loop, the
// agent's rights, the hand-over, Chatur's own `dotnet build` and `dotnet test`, the checks and the run's page are real.
import { Page, expect } from '@playwright/test';
import { join } from 'path';
import { signIn } from './support/auth';
import { ensureStubProvider, removeStubProvider, STUB_MODEL } from './support/stub-model';
import { build, put, ScriptFn, ScriptedModel, startScriptedModel, StepScript } from './support/script-model';
import { brd, checklist, cleanUp, driveRun, failedWhy, Fixture, filesWritten, startOn, stepResult, stepState, work } from './support/step-work';
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
  // Each run builds the fixture for real (the agent's own run-build, then Chatur's) and runs its tests.
  test.setTimeout(900_000);
});
test.afterEach(() => cleanUp());

const reads = (aText = 'I read what the step asks for.'): StepScript => ({ calls: [], final: aText });

async function scenario(aFixture: Fixture, aSetup: () => void, aProcess: string, aScript: ScriptFn) {
  aSetup();
  stub.setScript(aScript);
  const vRunId = await startOn(page, aFixture.name, aProcess);
  const vEnd = await driveRun(page, 600_000);
  return { runId: vRunId, end: vEnd };
}

const calc = (aBody: string) => `namespace Sw;\n\npublic static class Calc\n{\n    public static int Add(int a, int b)\n    {\n${aBody}\n    }\n}\n`;
const GOOD_CALC = calc('        return a + b;');
const WARNING_CALC = calc('        int unused = 1;\n        return a + b;');
const BUGGY_CALC = calc('        return a - b;');
const CALC_TESTS =
  'using Xunit;\n\nnamespace Sw.Tests;\n\npublic class CalcTests\n{\n    [Fact(DisplayName = "REQ-FN-001 adds two numbers")]\n    public void AddsTwoNumbers() => Assert.Equal(5, Calc.Add(2, 3));\n}\n';
const row = (aStatus: string) => [{ id: 'REQ-FN-001', title: 'Add two numbers', status: aStatus, brd: 1, acceptance: 'When two numbers are added, then their sum is returned.' }];

// ---- build-phase ---------------------------------------------------------------------------------------------------

const bp = new Fixture('SwBuild');
const setupBuild = () => bp.reset().dotnet('SwBuild').write('docs/Sw-BRD.md', brd('Sw', [1])).write('docs/Sw-Checklist.md', checklist('Sw', row('Not Started')));
const buildScript =
  (aMode: 'good' | 'warning' | 'row-left-open'): ScriptFn =>
  (c) => {
    if (c.process !== 'build-phase') return null;
    if (c.step === 4) {
      return { opening: 'Building the cluster: REQ-FN-001.', calls: [put('src/Calc.cs', aMode === 'warning' ? WARNING_CALC : GOOD_CALC), build()], final: 'I wrote src/Calc.cs and built it.' };
    }
    if (c.step === 6) return { calls: [put('tests/CalcTests.cs', CALC_TESTS)], final: 'I wrote tests/CalcTests.cs carrying REQ-FN-001.' };
    if (c.step === 10 && aMode !== 'row-left-open') return { calls: [put('docs/Sw-Checklist.md', checklist('Sw', row('Implemented')))], final: 'I recorded REQ-FN-001 as Implemented.' };
    return reads();
  };

test('REQ-FN-093 build-phase runs through the Flow master and its checks pass only when the build has no warning, the tests pass and every open row is Implemented', async () => {
  // (b1) the code builds with a warning: Chatur's own build is read, not the agent's word -> step 5.
  const vB1 = await scenario(bp, setupBuild, 'build-phase', buildScript('warning'));
  expect(vB1.end).toBe('stopped');
  expect(await failedWhy(page)).toContain('The build left 1 warning behind');
  await expect(stepState(page, 4)).toHaveText('done');
  await expect(stepState(page, 5)).toHaveText('check failed');
  expect(bp.read('src/Calc.cs')).toContain('int unused');

  // (b2) built and tested, but the agent never records the row -> step 10 names it.
  const vB2 = await scenario(bp, setupBuild, 'build-phase', buildScript('row-left-open'));
  expect(vB2.end).toBe('stopped');
  expect(await failedWhy(page)).toContain('REQ-FN-001 is still Not Started');
  await expect(stepState(page, 8)).toHaveText('check passed'); // Chatur ran the tests itself and they passed
  await expect(stepState(page, 10)).toHaveText('check failed');
  expect(bp.read('docs/Sw-Checklist.md')).toContain('Not Started');

  // (a) a whole build: no warning, tests pass, the row is Implemented and a test carries its id.
  const vA = await scenario(bp, setupBuild, 'build-phase', buildScript('good'));
  expect(vA.end).toBe('finished');
  await expect(stepState(page, 3)).toHaveText('check passed');
  await expect(stepState(page, 5)).toHaveText('check passed');
  await expect(stepResult(page, 8)).toContainText('1 tests passed');
  await expect(stepState(page, 8)).toHaveText('check passed');
  await expect(stepState(page, 10)).toHaveText('done');
  expect(bp.read('docs/Sw-Checklist.md')).toContain('Implemented');
  expect(bp.read('tests/CalcTests.cs')).toContain('REQ-FN-001');
  expect(work(vA.runId, 4)).toMatchObject({ Agent: 'Flow master', Model: STUB_MODEL, State: 'finished' });
  expect(filesWritten(vA.runId, 4)).toEqual(['src/Calc.cs']);
  expect(filesWritten(vA.runId, 6)).toEqual(['tests/CalcTests.cs']);
  await page.screenshot({ path: join(SHOTS, 'process-step-work-build-phase-finished.png'), fullPage: true });
});

// ---- fix-issues ----------------------------------------------------------------------------------------------------

const fx = new Fixture('SwFix');
const setupFix = () =>
  fx
    .reset()
    .dotnet('SwFix')
    .write('src/Calc.cs', BUGGY_CALC)
    .write('tests/CalcTests.cs', CALC_TESTS)
    .write('docs/Sw-BRD.md', brd('Sw', [1]))
    .write('docs/Sw-Checklist.md', checklist('Sw', row('FAIL')));
const fixScript =
  (aMode: 'good' | 'row-not-recorded' | 'code-not-fixed'): ScriptFn =>
  (c) => {
    if (c.process !== 'fix-issues') return null;
    if (c.step === 3) {
      const vCalls = [];
      if (aMode !== 'code-not-fixed') vCalls.push(put('src/Calc.cs', GOOD_CALC));
      if (aMode !== 'row-not-recorded') vCalls.push(put('docs/Sw-Checklist.md', checklist('Sw', row('Implemented'))));
      vCalls.push(build());
      return { calls: vCalls, final: 'I fixed REQ-FN-001 and built it.' };
    }
    return reads();
  };

test('REQ-FN-094 fix-issues runs through the Flow master and its checks pass only when the build has no warning, the failed row tests pass and it is Implemented again', async () => {
  // (b1) the agent claims the row Implemented but never fixes the code: Chatur's own test run fails it -> step 6.
  const vB1 = await scenario(fx, setupFix, 'fix-issues', fixScript('code-not-fixed'));
  expect(vB1.end).toBe('stopped');
  expect(await failedWhy(page)).toContain('REQ-FN-001 adds two numbers failed');
  await expect(stepState(page, 4)).toHaveText('check passed');
  await expect(stepState(page, 6)).toHaveText('check failed');
  expect(fx.read('src/Calc.cs')).toContain('a - b');

  // (b2) the code is fixed and the tests pass, but the row is left FAIL -> step 7 names it.
  const vB2 = await scenario(fx, setupFix, 'fix-issues', fixScript('row-not-recorded'));
  expect(vB2.end).toBe('stopped');
  expect(await failedWhy(page)).toContain('REQ-FN-001 is still FAIL');
  await expect(stepState(page, 6)).toHaveText('check passed');
  await expect(stepState(page, 7)).toHaveText('check failed');

  // (a) fixed, built without a warning, tests pass, row Implemented again.
  const vA = await scenario(fx, setupFix, 'fix-issues', fixScript('good'));
  expect(vA.end).toBe('finished');
  for (const vStep of [4, 6, 7]) await expect(stepState(page, vStep)).toHaveText('check passed');
  expect(fx.read('src/Calc.cs')).toContain('a + b');
  expect(fx.read('docs/Sw-Checklist.md')).toContain('Implemented');
  expect(filesWritten(vA.runId, 3)).toEqual(['src/Calc.cs', 'docs/Sw-Checklist.md']);
  await page.screenshot({ path: join(SHOTS, 'process-step-work-fix-issues-finished.png'), fullPage: true });
});

// ---- Process run on a step the engine handed over for real ---------------------------------------------------------

test('REQ-UI-070 Process run shows the agent, its model, the files written and the commands run of a step handed over for real', async () => {
  // build-phase with a warning stops at step 5, after step 4 was worked by the Flow master: it wrote src/Calc.cs and built.
  const vRun = await scenario(bp, setupBuild, 'build-phase', buildScript('warning'));
  expect(vRun.end).toBe('stopped');

  const vPanel = page.getByTestId('handed-step');
  await expect(vPanel).toBeVisible({ timeout: 30_000 });
  await expect(vPanel).toContainText('Step 4 was with the Flow master');
  await expect(page.getByTestId('handed-step-state')).toContainText('finished');
  await expect(page.getByTestId('handed-step-agent')).toHaveText('Flow master');
  await expect(page.getByTestId('handed-step-model')).toContainText(STUB_MODEL);
  await expect(page.getByTestId('handed-step-task')).toContainText('Build the cluster to the coding standards');
  await expect(page.getByTestId('handed-step-files')).toContainText('src/Calc.cs');
  await expect(page.getByTestId('handed-step-commands')).toContainText('Build the project.');
  await expect(page.getByTestId('handed-step-final')).toContainText('I wrote src/Calc.cs and built it.');
  await expect(page.getByTestId('handed-step-stop')).toHaveCount(0);
  // the same facts are the ones the run recorded
  expect(work(vRun.runId, 4)).toMatchObject({ Agent: 'Flow master', Model: STUB_MODEL, State: 'finished' });
  expect(filesWritten(vRun.runId, 4)).toEqual(['src/Calc.cs']);

  await vPanel.scrollIntoViewIfNeeded();
  await page.screenshot({ path: join(SHOTS, 'process-step-work-handed-step.png'), fullPage: true });
  await page.setViewportSize({ width: 390, height: 844 });
  await vPanel.scrollIntoViewIfNeeded();
  await page.screenshot({ path: join(SHOTS, 'process-step-work-handed-step-390.png'), fullPage: true });
  await page.setViewportSize({ width: 1280, height: 800 });
});
