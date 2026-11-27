// Acceptance checks for Compare models (UI Design phase 4 "Screen: Compare models", mockups/compare-models.html):
// REQ-FN-327 (two to four models; one or five refused with the reason), REQ-FN-328 (a question changes nothing),
// REQ-FN-329 (a task that changes files works in its own copy under the project's bin folder, removed afterwards),
// REQ-UI-132 (time, tokens, cost or "no price", answer or changed files, side by side), REQ-UI-133 (a right or wrong mark
// is kept and shows again), REQ-FN-330 (runs.jsonl and gates.jsonl in TfLens's shape; a comparison opens again) and
// REQ-NFR-021 (a REAL read-only question on two models of the owner's test provider from tests/verify/test-settings.json;
// its key is read by support/provider.ts and never printed).
// The other rows use the stub model of support/stub-model.ts, which always asks for write-file first: on a question that
// write is refused (no write tool exists), on a task it lands in the model's own copy.
import { test, expect, Page } from '@playwright/test';
import { createHash } from 'crypto';
import { existsSync, mkdirSync, readdirSync, readFileSync, statSync, writeFileSync } from 'fs';
import { join, relative, resolve } from 'path';
import { signIn } from './support/auth';
import { removeProjectsUnder, scratchDir } from './support/cleanup';
import { query, run } from './support/db';
import { ensureTestProvider, testProvider } from './support/provider';
import { STUB_MODEL, STUB_PROVIDER_NAME, StubModel, ensureStubProvider, removeStubProvider, startStubModel } from './support/stub-model';
import { openByDialog } from './support/workbench-b';

test.describe.configure({ mode: 'serial', timeout: 360_000 });

const STAMP = Date.now();
const PROJECT = `Compare${STAMP}`;
const SHOTS = resolve(__dirname, '../.artifacts/verify/shots');
let objRoot = '';
let objFolder = '';
let objStub: StubModel | null = null;

/** The stub's extra model names: the stub answers any model name, so one provider gives five models. */
const STUB_EXTRA = ['stub-edit-model-b', 'stub-edit-model-c', 'stub-edit-model-d', 'stub-edit-model-e'];

/** A model's name as the page writes it into a test id (CompareWorkspace.SafeName). */
function safeName(aName: string): string {
  const vSafe = aName.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '');
  return vSafe.length === 0 ? 'model' : vSafe;
}

/** Every file of the project with a hash of its bytes; bin, obj, .git and docs/metrics (Chatur's own records) left out. */
function snapshot(aRoot: string): Record<string, string> {
  const vResult: Record<string, string> = {};
  const walk = (aDir: string) => {
    for (const vName of readdirSync(aDir)) {
      const vFull = join(aDir, vName);
      const vRel = relative(aRoot, vFull).replace(/\\/g, '/');
      if (statSync(vFull).isDirectory()) {
        if (['bin', 'obj', '.git', 'node_modules', '.vs'].includes(vName) || vRel === 'docs/metrics') continue;
        walk(vFull);
      } else {
        vResult[vRel] = createHash('sha256').update(readFileSync(vFull)).digest('hex');
      }
    }
  };
  walk(aRoot);
  return vResult;
}

/** The records of one docs/metrics stream written by Compare models since the test began. */
function records(aStream: string): Record<string, any>[] {
  const vFile = join(objFolder, 'docs', 'metrics', aStream);
  if (!existsSync(vFile)) return [];
  return readFileSync(vFile, 'utf8').split('\n').filter((l) => l.trim()).map((l) => JSON.parse(l));
}

async function openCompare(aPage: Page): Promise<void> {
  await openByDialog(aPage, 'open-folder', objFolder, PROJECT);
  await aPage.goto('/compare-models', { waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('compare-heading')).toBeVisible({ timeout: 30_000 });
  await expect(aPage.getByTestId('compare-project')).toHaveText(PROJECT, { timeout: 30_000 });
}

async function tick(aPage: Page, aModel: string): Promise<void> {
  const vBox = aPage.getByTestId(`model-check-${safeName(aModel)}`);
  await expect(vBox).toBeVisible({ timeout: 20_000 });
  await vBox.click();
}

async function chooseKind(aPage: Page, aKind: 'kind-question' | 'kind-change'): Promise<void> {
  await aPage.getByTestId('compare-kind-button').click();
  await aPage.getByTestId(aKind).click();
  await expect(aPage.getByTestId('compare-kind-button')).toContainText(aKind === 'kind-question' ? 'Question, changes nothing' : 'Task that changes files');
}

/** Checks one result card shows its time, tokens in and out, and cost or "no price" (REQ-UI-132). */
async function expectFigures(aPage: Page, aN: number, aTokensReported: boolean): Promise<void> {
  await expect(aPage.getByTestId(`result-time-${aN}`)).toHaveText(/^(\d+(\.\d)?s|\d+m \d{2}s)$/);
  if (aTokensReported) await expect(aPage.getByTestId(`result-tokens-${aN}`)).toHaveText(/^[\d,]+ \/ [\d,]+$/);
  else await expect(aPage.getByTestId(`result-tokens-${aN}`)).toHaveText(/^([\d,]+ \/ [\d,]+|not reported)$/);
  await expect(aPage.getByTestId(`result-cost-${aN}`)).toHaveText(/^(\$\d+\.\d{2,4}|no price)$/);
}

test.beforeAll(() => {
  objRoot = scratchDir(`compare-models-${STAMP}`);
  objFolder = join(objRoot, PROJECT);
  mkdirSync(join(objFolder, 'src'), { recursive: true });
  writeFileSync(join(objFolder, 'readme.md'), '# Compare demo\nThis project keeps its measurements in docs/metrics/runs.jsonl.\n');
  writeFileSync(join(objFolder, 'src', 'app.txt'), 'hello = 1\n');
});

test.afterAll(async () => {
  if (objStub) await objStub.close();
  if (objRoot) removeProjectsUnder(objRoot);
});

test('REQ-NFR-021 REQ-FN-328 REQ-UI-132 REQ-FN-330 two real models answer a read-only question with time, tokens and cost, and runs.jsonl holds both', async ({ page }) => {
  const vProvider = testProvider();
  await signIn(page, 'Owner');
  await ensureTestProvider(page);
  const vModelsOf = () => query(
    "SELECT COUNT(*) AS n FROM Model m JOIN Provider p ON p.ProviderId = m.ProviderId WHERE p.Name = ?",
    vProvider.name,
  )[0].n as number;
  if (vModelsOf() < 2) {
    // The web harness seeds the test provider with its one test model; added through Model providers, as a person
    // adds it, the provider lists every model the vendor serves.
    run('DELETE FROM Model WHERE ProviderId IN (SELECT ProviderId FROM Provider WHERE Name = ?)', vProvider.name);
    run('DELETE FROM Provider WHERE Name = ?', vProvider.name);
    await ensureTestProvider(page);
  }
  const vModels = (query(
    "SELECT m.Identifier AS Identifier FROM Model m JOIN Provider p ON p.ProviderId = m.ProviderId WHERE p.Name = ? AND p.State = 'Connected' ORDER BY m.Tier DESC, m.ModelId",
    vProvider.name,
  ) as { Identifier: string }[]).map((r) => r.Identifier);
  expect(vModels, `the test provider "${vProvider.name}" needs two models to compare`).toContain(vProvider.model);
  const vOther = vModels.find((m) => m !== vProvider.model && /haiku|mini|flash|small/i.test(m)) ?? vModels.find((m) => m !== vProvider.model);
  expect(vOther, `the test provider "${vProvider.name}" has only one model`).toBeTruthy();
  const vPicked = [vProvider.model, vOther!];

  await openCompare(page);
  const vBefore = snapshot(objFolder);
  const vStarted = new Date().toISOString();
  for (const vModel of vPicked) await tick(page, vModel);
  await expect(page.getByTestId('compare-pick-count')).toHaveText('2 picked · two to four');
  await expect(page.getByTestId('compare-kind-button')).toContainText('Question, changes nothing');
  await page.getByTestId('compare-task').fill(`In one sentence: where does this project keep its measurements? Read readme.md. (${STAMP})`);
  await page.getByTestId('compare-run').click();

  await expect(page.getByTestId('compare-state')).toHaveText('done', { timeout: 300_000 });
  await expect(page.getByTestId('compare-status')).toContainText('nothing in the project changed');
  for (const vN of [1, 2]) {
    await expect(page.getByTestId(`result-model-${vN}`)).toHaveText(vPicked[vN - 1]);
    await expect(page.getByTestId(`result-error-${vN}`)).toHaveCount(0);
    await expect(page.getByTestId(`result-answer-${vN}`)).not.toHaveText(/^\s*$/);
    await expectFigures(page, vN, true);
    // TechieRag prices every current Claude model, so a real call costs a number of dollars, never "no price".
    await expect(page.getByTestId(`result-cost-${vN}`)).toHaveText(/^\$\d+\.\d{2,4}$/);
    console.log(`model ${vN}: time ${await page.getByTestId(`result-time-${vN}`).textContent()}, tokens ${await page.getByTestId(`result-tokens-${vN}`).textContent()}, cost ${await page.getByTestId(`result-cost-${vN}`).textContent()}`);
  }
  expect(snapshot(objFolder), 'a question changed no file of the project').toEqual(vBefore);
  expect(existsSync(join(objFolder, 'bin', 'chatur-compare')), 'a question made no copy').toBe(false);
  await page.screenshot({ path: join(SHOTS, 'REQ-NFR-021-compare-models.png'), fullPage: true });

  const vRuns = records('runs.jsonl').filter((r) => r.cmd === 'compare-models' && r.started >= vStarted);
  expect(vRuns.map((r) => r.model).sort()).toEqual([...vPicked].sort());
  for (const vRun of vRuns) {
    expect(vRun.kind).toBe('run');
    expect(vRun.harness).toBe('chatur');
    expect(vRun.mode).toBe('question');
    expect(vRun.started).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$/);
    expect(typeof vRun.duration_s).toBe('number');
    expect(typeof vRun.tokens_in).toBe('number');
    expect(typeof vRun.tokens_out).toBe('number');
    expect(vRun.tokens_in).toBeGreaterThan(0);
    expect(typeof vRun.cost_usd).toBe('number');
    expect(vRun.cost_usd).toBeGreaterThan(0);
  }
  expect(new Set(vRuns.map((r) => r.started)).size, 'each model has its own started').toBe(2);
});

test.describe('with the stub model', () => {
  test.beforeAll(async () => {
    objStub = await startStubModel('write-file', { path: 'notes.md', content: 'compare notes\n' });
  });

  test.afterEach(async ({ page }) => {
    await removeStubProvider(page);
  });

  async function stubReady(aPage: Page): Promise<void> {
    await signIn(aPage, 'Owner');
    await ensureStubProvider(aPage, objStub!);
    for (const vModel of STUB_EXTRA) {
      run(
        `INSERT INTO Model (ProviderId, Tier, Identifier)
         SELECT ProviderId, 2, ? FROM Provider WHERE Name = ?
           AND NOT EXISTS (SELECT 1 FROM Model m JOIN Provider p ON p.ProviderId = m.ProviderId WHERE p.Name = ? AND m.Identifier = ?)`,
        vModel, STUB_PROVIDER_NAME, STUB_PROVIDER_NAME, vModel,
      );
    }
    await openCompare(aPage);
  }

  test('REQ-FN-327 one model or a fifth is refused with the reason, two to four run', async ({ page }) => {
    await stubReady(page);
    const vCount = () => Number((query('SELECT COUNT(*) AS N FROM Comparison') as { N: number }[])[0].N);
    const vBefore = vCount();
    await page.getByTestId('compare-task').fill('Which files are here?');

    await tick(page, STUB_MODEL);
    await expect(page.getByTestId('compare-pick-count')).toHaveText('1 picked · two to four');
    await page.getByTestId('compare-run').click();
    await expect(page.getByTestId('compare-pick-count')).toContainText('pick at least two');
    await expect(page.getByTestId('compare-results')).toHaveCount(0);

    for (const vModel of STUB_EXTRA) await tick(page, vModel);
    await expect(page.getByTestId('compare-pick-count')).toContainText('four at most');
    await page.getByTestId('compare-run').click();
    await expect(page.getByTestId('compare-pick-count')).toContainText('four at most');
    await expect(page.getByTestId('compare-results')).toHaveCount(0);
    expect(vCount(), 'a refused pick started nothing').toBe(vBefore);

    for (const vModel of STUB_EXTRA.slice(1)) await tick(page, vModel);
    await expect(page.getByTestId('compare-pick-count')).toHaveText('2 picked · two to four');
    await page.getByTestId('compare-run').click();
    await expect(page.getByTestId('compare-state')).toHaveText('done', { timeout: 120_000 });
    await expect(page.getByTestId('result-model-1')).toHaveText(STUB_MODEL);
    await expect(page.getByTestId('result-model-2')).toHaveText(STUB_EXTRA[0]);
    expect(vCount()).toBe(vBefore + 1);
  });

  test('REQ-FN-328 REQ-UI-132 REQ-UI-133 REQ-FN-330 a question that tries to write changes nothing; marks are kept and the comparison opens again', async ({ page }) => {
    await stubReady(page);
    const vBefore = snapshot(objFolder);
    const vStarted = new Date().toISOString();
    await tick(page, STUB_MODEL);
    await tick(page, STUB_EXTRA[0]);
    await chooseKind(page, 'kind-question');
    await page.getByTestId('compare-task').fill(`Overwrite notes.md. (${STAMP})`);
    await page.getByTestId('compare-run').click();

    await expect(page.getByTestId('compare-state')).toHaveText('done', { timeout: 120_000 });
    await expect(page.getByTestId('compare-status')).toContainText('nothing in the project changed');
    expect(snapshot(objFolder), 'the stub asked to write and nothing changed').toEqual(vBefore);
    expect(existsSync(join(objFolder, 'notes.md'))).toBe(false);
    for (const vN of [1, 2]) {
      await expect(page.getByTestId(`result-answer-${vN}`)).toContainText('Understood.');
      await expect(page.getByTestId(`result-files-${vN}`)).toHaveCount(0);
      await expect(page.getByTestId(`result-mark-${vN}`)).toHaveText('not marked');
      await expectFigures(page, vN, false);
    }

    // REQ-UI-133: marks, one changed; REQ-FN-330: each mark is a new gates record on that model's run.
    await page.getByTestId('mark-right-1').click();
    await expect(page.getByTestId('result-mark-1')).toHaveText('right');
    await page.getByTestId('mark-wrong-2').click();
    await expect(page.getByTestId('result-mark-2')).toHaveText('wrong');
    await page.getByTestId('mark-wrong-1').click();
    await expect(page.getByTestId('result-mark-1')).toHaveText('wrong');
    await page.screenshot({ path: join(SHOTS, 'REQ-UI-133-compare-marks.png'), fullPage: true });

    const vRuns = records('runs.jsonl').filter((r) => r.cmd === 'compare-models' && r.started >= vStarted);
    expect(vRuns.map((r) => r.model)).toEqual(expect.arrayContaining([STUB_MODEL, STUB_EXTRA[0]]));
    expect(vRuns).toHaveLength(2);
    expect(vRuns.every((r) => r.mode === 'question' && r.files_written === null)).toBe(true);
    const vFirstRun = vRuns.find((r) => r.model === STUB_MODEL)!;
    const vSecondRun = vRuns.find((r) => r.model === STUB_EXTRA[0])!;
    const vGates = records('gates.jsonl').filter((g) => g.run_id === vFirstRun.started || g.run_id === vSecondRun.started);
    expect(vGates.map((g) => [g.run_id, g.verdict, g.gate])).toEqual([
      [vFirstRun.started, 'Verified', null],
      [vSecondRun.started, 'FAIL', 'owner-mark'],
      [vFirstRun.started, 'FAIL', 'owner-mark'],
    ]);
    expect(vGates.every((g) => g.kind === 'gate' && JSON.stringify(g.gates_run) === '["owner-mark"]')).toBe(true);

    // The comparison opens again from Past comparisons, with its marks.
    await page.goto('/compare-models', { waitUntil: 'networkidle' });
    await expect(page.getByTestId('compare-results')).toHaveCount(0);
    const vRow = page.getByTestId('history-row-1');
    await expect(vRow).toContainText('question');
    await expect(vRow).toContainText('2 wrong');
    await vRow.click();
    await expect(page.getByTestId('result-mark-1')).toHaveText('wrong', { timeout: 20_000 });
    await expect(page.getByTestId('result-mark-2')).toHaveText('wrong');
    await expect(page.getByTestId('result-answer-1')).toContainText('Understood.');
  });

  test('REQ-FN-329 REQ-UI-132 REQ-FN-330 a task that changes files works in its own copy under bin, listed and then removed', async ({ page }) => {
    await stubReady(page);
    const vBefore = snapshot(objFolder);
    const vStarted = new Date().toISOString();
    await tick(page, STUB_MODEL);
    await tick(page, STUB_EXTRA[0]);
    await chooseKind(page, 'kind-change');
    await expect(page.getByTestId('compare-kind-note')).toContainText('bin/chatur-compare');
    await page.getByTestId('compare-task').fill(`Add notes.md. (${STAMP})`);
    objStub!.setDelay(3_000);
    try {
      await page.getByTestId('compare-run').click();
      // While the models run, each has its own copy under the project's bin folder, and its write lands there.
      const vCopies = join(objFolder, 'bin', 'chatur-compare');
      await expect.poll(() => {
        if (!existsSync(vCopies)) return 0;
        let vFound = 0;
        for (const vId of readdirSync(vCopies)) {
          for (const vModel of readdirSync(join(vCopies, vId))) if (existsSync(join(vCopies, vId, vModel, 'notes.md'))) vFound++;
        }
        return vFound;
      }, { timeout: 60_000, intervals: [250] }).toBe(2);
      expect(existsSync(join(objFolder, 'notes.md')), 'the real project is untouched while the copies change').toBe(false);
      await expect(page.getByTestId('compare-state')).toHaveText('done', { timeout: 120_000 });
    } finally {
      objStub!.setDelay(0);
    }

    await expect(page.getByTestId('compare-status')).toContainText('each copy removed');
    expect(existsSync(join(objFolder, 'bin', 'chatur-compare')), 'the copies are removed when the run ends').toBe(false);
    expect(snapshot(objFolder), 'the real project is unchanged').toEqual(vBefore);
    for (const vN of [1, 2]) {
      await expect(page.getByTestId(`result-files-${vN}`)).toHaveText('A notes.md (+1)');
      await expect(page.getByTestId(`result-answer-${vN}`)).toContainText('Understood.');
      await expectFigures(page, vN, false);
    }
    await page.screenshot({ path: join(SHOTS, 'REQ-FN-329-compare-change.png'), fullPage: true });

    const vRuns = records('runs.jsonl').filter((r) => r.cmd === 'compare-models' && r.started >= vStarted);
    expect(vRuns).toHaveLength(2);
    expect(vRuns.every((r) => r.mode === 'change' && r.files_written === 1)).toBe(true);
  });
});
