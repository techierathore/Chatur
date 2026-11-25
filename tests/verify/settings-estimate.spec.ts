// Acceptance tests: Settings > Measurements > Sister projects (REQ-UI-086) and Settings > Routing > estimate note and tier
// change (REQ-FN-112); BRD-197 and BRD-201, fixture F5 of docs/Chatur-P3-BRD.md. Black-box, web harness.
// Fixture projects are real folders under tests/.artifacts/verify/fixtures/settings-estimate*, written from the run
// records of F1/F5 (tests/unit/Chatur.Tests/Estimates/EstimateFixtures.cs). Each test sets up and restores its own state.
import { test, expect, Page } from '@playwright/test';
import { mkdirSync, writeFileSync, rmSync } from 'fs';
import { join, resolve } from 'path';
import { signIn } from './support/auth';
import { query, run, snapshot, restore } from './support/db';
import { removeProjectsUnder } from './support/cleanup';

test.describe.configure({ mode: 'serial' });

const BASE = resolve(__dirname, '../.artifacts/verify/fixtures');
const ROOT = join(BASE, 'settings-estimate-root'); // a named project-folder root holding the project "SetEstFixture"
const PROJECT = 'SetEstFixture'; // not "Fixture": processes-estimate.spec.ts names its own project that, and projects are picked by name
const SISTERS = join(BASE, 'settings-estimate-sisters'); // never named as a project root
const SHOTS = resolve(__dirname, '../.artifacts/verify/screens');
const OPUS = 'claude-opus-5-5';
const SONNET = 'claude-sonnet-5-5';

const stamp = (aDate: Date) => aDate.toISOString().replace(/\.\d+Z$/, 'Z');

// Every project this file made goes when it ends, with its folder root and its rows (owner, 20 Nov 2026).
test.afterAll(() => removeProjectsUnder(ROOT));

/** One run record per call, three hours after the one before, as EstimateFixtures.Runs.Add writes them. */
function runLines(aRuns: { reqs: string[]; tokens: number; seconds: number; model: string }[]): string {
  const vBase = Date.UTC(2026, 9, 1, 0, 0, 0);
  return aRuns
    .map((aRun, aIndex) => {
      const vStarted = new Date(vBase + (aIndex + 1) * 3 * 3600_000);
      const vEnded = new Date(vStarted.getTime() + aRun.seconds * 1000);
      return JSON.stringify({
        v: 1, ts: stamp(vEnded), kind: 'run', app: PROJECT, cmd: 'build-phase', mode: 'build',
        started: stamp(vStarted), ended: stamp(vEnded), duration_s: aRun.seconds,
        reqs_touched: aRun.reqs, reqs_count: aRun.reqs.length, model: aRun.model, tokens_out: aRun.tokens, tokens_scope: 'tree',
      });
    })
    .join('\n') + '\n';
}

/** F5: F1's five build-phase runs with B and D on claude-sonnet-5-5, the rest on claude-opus-5-5. */
function f5(): string {
  return runLines([
    { reqs: ['REQ-FN-201', 'REQ-FN-202'], tokens: 4000, seconds: 600, model: OPUS },
    { reqs: ['REQ-FN-203'], tokens: 3000, seconds: 240, model: SONNET },
    { reqs: ['REQ-FN-204', 'REQ-FN-205'], tokens: 10000, seconds: 900, model: OPUS },
    { reqs: ['REQ-FN-206', 'REQ-FN-207', 'REQ-FN-208', 'REQ-FN-209'], tokens: 4000, seconds: 480, model: SONNET },
    { reqs: ['REQ-FN-210', 'REQ-FN-211', 'REQ-FN-212', 'REQ-FN-213'], tokens: 16000, seconds: 1440, model: OPUS },
  ]);
}

function write(aPath: string, aText: string): void {
  mkdirSync(join(aPath, '..'), { recursive: true });
  writeFileSync(aPath, aText);
}

/** The project "Fixture": a phase-2 checklist of REQ-FN-301..303 and the F5 run records. */
function writeFixtureProject(): void {
  const vDir = join(ROOT, PROJECT);
  rmSync(vDir, { recursive: true, force: true });
  write(join(vDir, 'readme.txt'), 'fixture F5 for the settings estimate verification tests\n');
  write(join(vDir, 'docs', `${PROJECT}-P2-Checklist.md`),
    '# Fixture - Checklist (phase 2)\n\n## Requirements Status\n\n| ID | Requirement | Status | % | Remarks | Details |\n|----|-------------|--------|---|---------|---------|\n'
    + ['REQ-FN-301', 'REQ-FN-302', 'REQ-FN-303'].map((aRow) => `| ${aRow} | A row | Not Started | 0% | | |\n`).join('')
    + '\n## Page: Fixture\n');
  write(join(vDir, 'docs', 'metrics', 'runs.jsonl'), f5());
}

/** Selects the project through Start's own flow: all projects, name the root once, click the project. */
async function selectProject(aPage: Page, aName: string): Promise<void> {
  await aPage.goto('/start?all=1', { waitUntil: 'domcontentloaded' });
  const vList = aPage.getByTestId('recent-list');
  await expect(aPage.getByTestId('manage-folders')).toBeVisible({ timeout: 30_000 });
  await expect(aPage.getByTestId('recent-loading')).toHaveCount(0, { timeout: 30_000 });
  if ((await vList.getByText(aName, { exact: true }).count()) === 0) {
    const vDialog = aPage.getByTestId('folders-dialog');
    await expect(async () => {
      if (!(await vDialog.isVisible())) await aPage.getByTestId('manage-folders').click();
      await expect(vDialog).toBeVisible({ timeout: 2_500 });
    }).toPass({ timeout: 30_000 });
    if ((await vDialog.getByText(ROOT, { exact: true }).count()) === 0) {
      await aPage.getByTestId('new-folder-path').fill(ROOT);
      await aPage.getByTestId('add-folder').click();
    }
    await expect(vDialog.getByText(ROOT, { exact: true })).toBeVisible({ timeout: 20_000 });
    await aPage.getByTestId('folders-dialog-close').click();
    await expect(vDialog).toBeHidden();
    await expect(vList.getByText(aName, { exact: true })).toBeVisible({ timeout: 20_000 });
  }
  await expect(async () => {
    if (/\/start/.test(aPage.url())) await vList.getByText(aName, { exact: true }).click();
    await expect(aPage).not.toHaveURL(/\/start/, { timeout: 3_000 });
  }).toPass({ timeout: 30_000 });
  await expect(aPage.getByTestId('project-switch-name')).toHaveText(aName, { timeout: 30_000 });
}

async function openSettings(aPage: Page, aPath: string, aReady: string): Promise<void> {
  await aPage.goto(aPath, { waitUntil: 'domcontentloaded' });
  await expect(aPage.getByTestId(aReady)).toBeVisible({ timeout: 60_000 });
  await aPage.waitForTimeout(2000); // let the Blazor circuit attach before clicking
}

test.beforeEach(async ({ page }) => {
  test.setTimeout(180_000);
  await signIn(page, 'Owner');
});

test('REQ-UI-086 the owner adds a sister project on Settings and it is listed with its folder and run count; a folder without a run record is refused with the reason', async ({ page }) => {
  const vSnap = snapshot(['SisterProject']);
  const vGood = join(SISTERS, 'SisterGood');
  const vBare = join(SISTERS, 'SisterNoRuns');
  write(join(vGood, 'docs', 'metrics', 'runs.jsonl'), runLines([
    { reqs: ['REQ-UI-001'], tokens: 1000, seconds: 100, model: OPUS },
    { reqs: ['REQ-UI-002'], tokens: 2000, seconds: 200, model: OPUS },
    { reqs: ['REQ-UI-003'], tokens: 3000, seconds: 300, model: OPUS },
  ]));
  mkdirSync(vBare, { recursive: true });
  rmSync(join(vBare, 'docs'), { recursive: true, force: true });
  writeFixtureProject();
  run('DELETE FROM SisterProject WHERE Path IN (?, ?)', vGood, vBare);
  try {
    await selectProject(page, PROJECT);
    await openSettings(page, '/settings/measurements', 'measurements-heading');
    await expect(page.getByTestId('sister-panel')).toBeVisible({ timeout: 30_000 });
    await expect(page.getByTestId('sister-rule')).toContainText('never writes');
    const vCountBefore = (await page.getByTestId('sister-count').innerText()).trim();

    // A folder with no docs/metrics/runs.jsonl is refused, the reason is shown, and nothing is listed.
    await page.getByTestId('sister-path').fill(vBare);
    await expect(page.getByTestId('sister-add')).toBeEnabled();
    await page.getByTestId('sister-add').click();
    await expect(page.getByTestId('sister-error')).toBeVisible({ timeout: 20_000 });
    await expect(page.getByTestId('sister-error')).toContainText('has no docs/metrics/runs.jsonl');
    await expect(page.getByTestId('sister-error')).toContainText('SisterNoRuns');
    await expect(page.getByTestId('sister-count')).toHaveText(vCountBefore);
    expect(query('SELECT 1 FROM SisterProject WHERE Path = ?', vBare)).toHaveLength(0);

    // A folder holding the run record is listed with its folder and run count.
    await page.getByTestId('sister-path').fill(vGood);
    await page.getByTestId('sister-add').click();
    const vRow = page.locator('[data-testid^="sister-"]').filter({ has: page.getByText(vGood, { exact: true }) }).first();
    await expect(vRow).toBeVisible({ timeout: 20_000 });
    await expect(vRow).toContainText('SisterGood');
    await expect(vRow).toContainText(vGood);
    await expect(vRow).toContainText('3 runs');
    await expect(page.getByTestId('sister-error')).toHaveCount(0);
    await expect(page.getByTestId('sister-path')).toHaveValue('');

    // It is still listed after reopening the page.
    await openSettings(page, '/settings/measurements', 'sister-panel');
    await expect(page.getByTestId('sister-panel')).toContainText(vGood);
    await expect(page.getByTestId('sister-panel')).toContainText('3 runs');

    mkdirSync(SHOTS, { recursive: true });
    await page.getByTestId('sister-panel').scrollIntoViewIfNeeded();
    await page.screenshot({ path: join(SHOTS, 'settings-measurements-sisters.png'), fullPage: true });

    // Remove the added sister again.
    const vId = query('SELECT SisterProjectId AS Id FROM SisterProject WHERE Path = ?', vGood)[0].Id;
    await page.getByTestId(`sister-remove-${vId}`).click();
    await expect(page.getByTestId('sister-panel')).not.toContainText(vGood, { timeout: 20_000 });
    expect(query('SELECT 1 FROM SisterProject WHERE Path = ?', vGood)).toHaveLength(0);
  } finally {
    restore(vSnap);
  }
});

test('REQ-FN-112 moving build to tier 2 on Settings Routing changes fixture F5 build estimate to 2,000/3,000/4,000 tokens from all models for claude-sonnet-5-5 with no restart, and a started run keeps its own estimate', async ({ page }) => {
  // Seeding (support/db.ts): the Routing page can only file models a connected provider lists, and claude-opus-5-5 is not
  // listed by any provider this harness can reach, so Model rows are written directly: a local provider at a dead address
  // ("verify-dead-estimate", so the run this test starts spends no tokens) holding exactly claude-opus-5-5 in tier 1 and
  // claude-sonnet-5-5 in tier 2. Provider, Model and the routing tables are restored afterwards.
  const vSnap = snapshot(['Provider', 'Model', 'RoutingSetting', 'RoutingEntry', 'RoutingChoice', 'SisterProject']);
  writeFixtureProject();
  // A run of this project left behind by an earlier aborted run would show as running and refuse a new Start.
  run("UPDATE ProcessRun SET State = 'stopped', EndedUtc = datetime('now') WHERE State IN ('running', 'waiting') AND ProjectId = (SELECT ProjectId FROM Project WHERE Name = ?)", PROJECT);
  let vOriginalWorkTier = '2';
  let vRouting = false;
  try {
    run('DELETE FROM RoutingEntry');
    run('DELETE FROM Model');
    run("DELETE FROM Provider WHERE Name = 'verify-dead-estimate'");
    const vProvider = run("INSERT INTO Provider (Name, Connector, SignInMethod, BaseUrl, SecretName, State) VALUES ('verify-dead-estimate', 'Ollama', 'None', 'http://127.0.0.2:59999', NULL, 'Connected')");
    run('INSERT INTO Model (ModelId, ProviderId, Tier, Identifier) VALUES (?, ?, ?, ?)', 9001, vProvider, 1, OPUS);
    run('INSERT INTO Model (ModelId, ProviderId, Tier, Identifier) VALUES (?, ?, ?, ?)', 9002, vProvider, 2, SONNET);
    run("UPDATE RoutingSetting SET Value = '[9001]' WHERE Key = 'Tier1Chain'");
    run("UPDATE RoutingSetting SET Value = '[9002]' WHERE Key = 'Tier2Chain'");
    run("UPDATE RoutingSetting SET Value = '[]' WHERE Key = 'Tier3Chain'");
    run('DELETE FROM SisterProject');

    // The Routing note about estimates.
    await openSettings(page, '/settings/routing', 'tiers');
    vRouting = true;
    const vNote = page.getByTestId('routing-estimate-note');
    await expect(vNote).toBeVisible();
    await expect(vNote).toContainText('Changing a tier changes the estimate shown before the next run starts');
    await expect(vNote).toContainText('A run already started keeps the estimate it started with');
    await expect(page.getByTestId('tier-1-row-1')).toContainText(OPUS);
    await expect(page.getByTestId('tier-2-row-1')).toContainText(SONNET);
    mkdirSync(SHOTS, { recursive: true });
    await vNote.scrollIntoViewIfNeeded();
    await page.screenshot({ path: join(SHOTS, 'settings-routing-note.png'), fullPage: true });

    const pickWorkTier = async (aTier: string) =>
      expect(async () => {
        if (!(await page.getByTestId(`write-code-tier-${aTier}`).isVisible())) await page.getByTestId('write-code-tier-button').click();
        await page.getByTestId(`write-code-tier-${aTier}`).click({ timeout: 3_000 });
        await expect(page.getByTestId('write-code-tier-button')).toContainText(`Tier ${aTier}`, { timeout: 3_000 });
      }).toPass({ timeout: 30_000 });
    vOriginalWorkTier = (await page.getByTestId('write-code-tier-button').innerText()).match(/[123]/)?.[0] ?? '2';

    // Build on tier 1: the estimate on Processes is for claude-opus-5-5, 3 runs.
    await pickWorkTier('1');
    await selectProject(page, PROJECT);
    const vOpenEstimate = async () => {
      // The Project menu lives in the main window; picking a project from Start can land back on the secondary window's page.
      if (!(await page.getByTestId('menu-project').isVisible().catch(() => false))) {
        await page.goto('/', { waitUntil: 'domcontentloaded' });
        await expect(page.getByTestId('menu-project')).toBeVisible({ timeout: 30_000 });
      }
      await page.getByTestId('menu-project').click();
      await page.getByTestId('menu-project-processes').click();
      await expect(page.getByTestId('process-table')).toBeVisible({ timeout: 30_000 });
      await expect(async () => {
        if (!(await page.getByTestId('estimate-panel').isVisible())) await page.getByTestId('start-build-phase').click({ timeout: 2_500 });
        await expect(page.getByTestId('estimate-panel')).toBeVisible({ timeout: 3_000 });
      }).toPass({ timeout: 30_000 });
    };
    await vOpenEstimate();
    await expect(page.getByTestId('estimate-model')).toContainText(OPUS);
    await expect(page.getByTestId('estimate-model')).toContainText('tier 1', { ignoreCase: true });
    await expect(page.getByTestId('estimate-basis-build')).not.toContainText('all models');
    await expect(page.getByTestId('estimate-tokens-build')).toContainText('13,500'); // tier 1 phase total over 3 rows of 3,000 · 4,000 · 4,500: 9,000 · 12,000 · 13,500
    await page.getByTestId('estimate-per-row-toggle').click();
    await expect(page.getByTestId('estimate-req-1')).toContainText('3,000');
    await expect(page.getByTestId('estimate-req-1')).toContainText('4,000');
    await expect(page.getByTestId('estimate-req-1')).toContainText('4,500');

    // Start the run on tier 1: it keeps this estimate. Then stop it at once (nothing is built).
    await page.getByTestId('estimate-confirm').click();
    await expect.poll(() => query("SELECT 1 FROM RunEstimate WHERE RunKind = 'process'").length, { timeout: 60_000 }).toBeGreaterThan(0);
    await expect(page.getByTestId('process-run')).toBeVisible({ timeout: 30_000 });
    const vRunId = Number(query("SELECT RunId AS Id FROM RunEstimate WHERE RunKind = 'process' ORDER BY KeptUtc DESC LIMIT 1")[0].Id);
    const vKeptBefore = String(query('SELECT Snapshot AS S FROM RunEstimate WHERE RunId = ?', vRunId)[0].S);
    expect(vKeptBefore).toContain(OPUS);
    if (await page.getByTestId('stop-run').isEnabled().catch(() => false)) {
      await page.getByTestId('stop-run').click();
      await page.getByTestId('stop-run-confirm').click();
    }

    // Move build to tier 2 on Routing; no restart.
    await openSettings(page, '/settings/routing', 'tiers');
    await pickWorkTier('2');
    await expect(page.getByTestId('write-code-tier-button')).toContainText('Tier 2');
    await page.reload({ waitUntil: 'domcontentloaded' });
    await expect(page.getByTestId('write-code-tier-button')).toContainText('Tier 2', { timeout: 30_000 });

    await page.goto('/start?all=1', { waitUntil: 'domcontentloaded' });
    await selectProject(page, PROJECT);
    await vOpenEstimate();
    await expect(page.getByTestId('estimate-model')).toContainText(SONNET);
    await expect(page.getByTestId('estimate-model')).toContainText('tier 2', { ignoreCase: true });
    await expect(page.getByTestId('estimate-basis-build')).toContainText('from all models');
    await page.getByTestId('estimate-per-row-toggle').click();
    await expect(page.getByTestId('estimate-req-1')).toContainText('2,000');
    await expect(page.getByTestId('estimate-req-1')).toContainText('3,000');
    await expect(page.getByTestId('estimate-req-1')).toContainText('4,000');
    await expect(page.getByTestId('estimate-tokens-build')).toContainText('6,000');
    await expect(page.getByTestId('estimate-tokens-build')).toContainText('12,000');
    await page.getByTestId('estimate-cancel').click();

    // The run started before the change keeps its own estimate, for claude-opus-5-5.
    const vKeptAfter = String(query('SELECT Snapshot AS S FROM RunEstimate WHERE RunId = ?', vRunId)[0].S);
    expect(vKeptAfter).toBe(vKeptBefore);
    const vBuild = (JSON.parse(vKeptAfter).Totals as { Step: string; Model: string; Tier: number; BasisRuns: number; FromAllModels: boolean }[])
      .find((aTotal) => aTotal.Step === 'build-phase')!;
    expect(vBuild).toMatchObject({ Model: OPUS, Tier: 1, BasisRuns: 3, FromAllModels: false });
  } finally {
    restore(vSnap);
    if (vRouting) {
      try {
        await openSettings(page, '/settings/routing', 'tiers');
        await expect(async () => {
          if (!(await page.getByTestId(`write-code-tier-${vOriginalWorkTier}`).isVisible())) await page.getByTestId('write-code-tier-button').click();
          await page.getByTestId(`write-code-tier-${vOriginalWorkTier}`).click({ timeout: 3_000 });
          await expect(page.getByTestId('write-code-tier-button')).toContainText(`Tier ${vOriginalWorkTier}`, { timeout: 3_000 });
        }).toPass({ timeout: 30_000 });
      } catch { /* the page is gone; the original tier could not be restored through it */ }
    }
  }
});
