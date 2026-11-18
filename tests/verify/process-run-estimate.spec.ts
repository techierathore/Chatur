// Acceptance tests: Process run sets what a run has spent so far beside the estimate it was started with (REQ-UI-087,
// REQ-UI-089 Process run part) and, for a handoff-phase run, the phase against its estimate (REQ-FN-113); mockups/process-run.html,
// `run-against-estimate` and `handoff-estimate`. Black-box, web harness.
// Fixtures F1 and F6 of docs/Chatur-P3-BRD.md ("Estimate before, actual after") are written as the project's own
// docs/metrics/runs.jsonl, with a phase checklist of REQ-FN-301..303. The estimate is the real one: Processes > Start > confirm.
// The model is a scripted OpenAI-compatible service that answers every handed step with a sentence and REPORTS USAGE, so what
// Chatur counts after each model call is controlled: 1,500 output tokens on the first call, then 3,000 on the second (held
// back so the 1,500 can be read while the run is still going), then none - 4,500 in all.
import { Page, expect } from '@playwright/test';
import { createServer, IncomingMessage, Server } from 'http';
import { join } from 'path';
import { signIn } from './support/auth';
import { ensureStubProvider, removeStubProvider, STUB_MODEL, STUB_PORT, StubModel } from './support/stub-model';
import { stepOf } from './support/script-model';
import { run } from './support/db';
import { brd, checklist, cleanUp, driveRun, Fixture, openStepProject, seedGoAheadSession, dropSession } from './support/step-work';
import { openProcessesTab, tab } from './support/phase2-tabs';
import { test } from './support/workbench-a';

const SCREENS = join(__dirname, '../.artifacts/verify/screens');
let page: Page;
let model: UsageModel;
let seeded = 0;

// ---- a scripted model that reports usage -------------------------------------------------------------------------------

type UsageModel = StubModel & {
  /** Output tokens reported by each handed-step call so far, in order. */
  reported: number[];
  /** Sets the output tokens the n-th (0-based) handed-step call reports, and how long that reply is held back. */
  plan: (aTokens: (aCall: number) => number, aHoldMs?: (aCall: number) => number) => void;
};

const readBody = (aReq: IncomingMessage): Promise<string> =>
  new Promise((resolve) => {
    const vChunks: Buffer[] = [];
    aReq.on('data', (c) => vChunks.push(c));
    aReq.on('end', () => resolve(Buffer.concat(vChunks).toString('utf8')));
  });

const sleep = (aMs: number) => new Promise((resolve) => setTimeout(resolve, aMs));

async function startUsageModel(): Promise<UsageModel> {
  let vTokens = (_: number) => 0;
  let vHold = (_: number) => 0;
  const vReported: number[] = [];
  const vServer: Server = createServer(async (aReq, aRes) => {
    const vUrl = aReq.url ?? '';
    if (aReq.method === 'GET' && /\/models\/?(\?.*)?$/.test(vUrl)) {
      aRes.writeHead(200, { 'content-type': 'application/json' });
      aRes.end(JSON.stringify({ object: 'list', data: [{ id: STUB_MODEL, object: 'model' }] }));
      return;
    }
    if (aReq.method === 'POST' && /\/chat\/completions\/?(\?.*)?$/.test(vUrl)) {
      const vBody = JSON.parse((await readBody(aReq)) || '{}');
      const vHanded = stepOf(vBody.messages ?? []) !== null;
      let vOut = 0;
      if (vHanded) {
        const vCall = vReported.length;
        vOut = vTokens(vCall);
        vReported.push(vOut);
        const vWait = vHold(vCall);
        if (vWait > 0) await sleep(vWait);
      }
      const vUsage = { prompt_tokens: 120, completion_tokens: vOut, total_tokens: 120 + vOut };
      const vText = 'I read what the step asks for.';
      if (vBody.stream === false) {
        aRes.writeHead(200, { 'content-type': 'application/json' });
        aRes.end(JSON.stringify({ id: 'usage-1', object: 'chat.completion', created: 0, model: STUB_MODEL, choices: [{ index: 0, finish_reason: 'stop', message: { role: 'assistant', content: vText } }], usage: vUsage }));
        return;
      }
      const vChunk = (aChoices: object[], aExtra: object = {}) =>
        `data: ${JSON.stringify({ id: 'usage-1', object: 'chat.completion.chunk', created: 0, model: STUB_MODEL, choices: aChoices, ...aExtra })}\n\n`;
      aRes.writeHead(200, { 'content-type': 'text/event-stream', 'cache-control': 'no-cache' });
      aRes.write(vChunk([{ index: 0, delta: { role: 'assistant', content: vText }, finish_reason: null }]));
      aRes.write(vChunk([{ index: 0, delta: {}, finish_reason: 'stop' }], { usage: vUsage }));
      aRes.write(vChunk([], { usage: vUsage }));
      aRes.write('data: [DONE]\n\n');
      aRes.end();
      return;
    }
    aRes.writeHead(404, { 'content-type': 'application/json' });
    aRes.end(JSON.stringify({ error: { message: `usage model: no ${aReq.method} ${vUrl}` } }));
  });
  await new Promise<void>((resolve, reject) => {
    vServer.once('error', reject);
    vServer.listen(STUB_PORT, '127.0.0.1', () => resolve());
  });
  return {
    baseUrl: `http://127.0.0.1:${STUB_PORT}/v1`,
    calls: [],
    reported: vReported,
    plan: (aTokens, aHoldMs) => {
      vTokens = aTokens;
      vHold = aHoldMs ?? (() => 0);
      vReported.length = 0;
    },
    setDelay: () => undefined,
    close: () => new Promise<void>((resolve) => vServer.close(() => resolve())),
  };
}

test.beforeAll(async ({ browser }) => {
  page = await browser.newPage({ baseURL: process.env.BASE_URL });
  await signIn(page, 'Owner');
  model = await startUsageModel();
  await ensureStubProvider(page, model);
});
test.afterAll(async () => {
  dropSession(seeded);
  cleanUp();
  await removeStubProvider(page);
  await model.close();
  await page.close();
});
test.beforeEach(() => {
  test.setTimeout(600_000);
});
test.afterEach(() => {
  dropSession(seeded);
  seeded = 0;
});

// ---- fixtures F1 and F6 ------------------------------------------------------------------------------------------------

const OPUS = 'claude-opus-5-5';
let objCount = 0;

/** One run record as tf-metrics writes it; each call starts three hours after the one before, so start times never collide. */
function runLine(aCmd: string, aReqs: string[], aTokensOut: number, aSeconds: number): string {
  objCount++;
  const vStart = new Date(Date.UTC(2026, 9, 1, 0, 0, 0) + objCount * 3 * 3600_000);
  const vEnd = new Date(vStart.getTime() + aSeconds * 1000);
  const vStamp = (d: Date) => d.toISOString().replace(/\.\d{3}Z$/, 'Z');
  return JSON.stringify({
    v: 1, ts: vStamp(vEnd), kind: 'run', app: 'Fixture', cmd: aCmd, mode: 'build', started: vStamp(vStart), ended: vStamp(vEnd),
    duration_s: aSeconds, reqs_touched: aReqs, reqs_count: aReqs.length, model: OPUS, tokens_out: aTokensOut, tokens_scope: 'tree',
  });
}

/** F1: five build-phase runs on REQ-FN rows outside the phase. */
function f1(): string[] {
  objCount = 0;
  return [
    runLine('build-phase', ['REQ-FN-201', 'REQ-FN-202'], 4000, 600),
    runLine('build-phase', ['REQ-FN-203'], 3000, 240),
    runLine('build-phase', ['REQ-FN-204', 'REQ-FN-205'], 10000, 900),
    runLine('build-phase', ['REQ-FN-206', 'REQ-FN-207', 'REQ-FN-208', 'REQ-FN-209'], 4000, 480),
    runLine('build-phase', ['REQ-FN-210', 'REQ-FN-211', 'REQ-FN-212', 'REQ-FN-213'], 16000, 1440),
  ];
}

/** F6's actuals after F1: R1 to R3 build-phase runs on the phase's rows, R4 a verify-phase run that named no row. */
function f6Actuals(): string[] {
  return [
    runLine('build-phase', ['REQ-FN-301', 'REQ-FN-302', 'REQ-FN-999'], 9000, 900),
    runLine('build-phase', ['REQ-FN-303'], 3000, 300),
    runLine('build-phase', ['REQ-FN-302'], 1500, 150),
    runLine('verify-phase', [], 2000, 120),
  ];
}

const rows = ['REQ-FN-301', 'REQ-FN-302', 'REQ-FN-303'].map((id, i) => ({ id, title: `Fixture row ${i + 1}`, status: 'Not Started', brd: 1, acceptance: 'When it is used, then it works.' }));

function setup(aFixture: Fixture, aRunLines: string[]): void {
  // A run a failed earlier attempt left 'running' (its page was driving it and is gone) would turn Start into Watch: end it.
  run("UPDATE ProcessRun SET State='stopped', EndedUtc=? WHERE State IN ('running','waiting') AND ProjectId IN (SELECT ProjectId FROM Project WHERE Name=?)", new Date().toISOString(), aFixture.name);
  aFixture.reset().write('docs/Est-BRD.md', brd('Est', [1])).write('docs/Est-Checklist.md', checklist('Est', rows)).write('docs/metrics/runs.jsonl', aRunLines.join('\n') + '\n');
}

/** F1's records, then F6's actuals: the file as it stands once R1 to R4 have run. */
function setupActuals(aFixture: Fixture): void {
  aFixture.write('docs/metrics/runs.jsonl', [...f1(), ...f6Actuals()].join('\n') + '\n');
}

/** Opens the project, puts the owner on Go ahead, presses Start on a process in Processes and confirms the estimate it shows. */
async function startWithEstimate(aFixture: Fixture, aProcess: string): Promise<void> {
  await openStepProject(page, aFixture.name);
  seeded = seedGoAheadSession(aFixture.name);
  await openProcessesTab(page);
  await page.getByTestId(`start-${aProcess}`).click();
  await expect(page.getByTestId('estimate-panel')).toBeVisible({ timeout: 30_000 });
  await page.getByTestId('estimate-confirm').click();
  await expect(tab(page, new RegExp(`^${aProcess} · run \\d+$`)).first()).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('run-heading')).toBeVisible({ timeout: 30_000 });
}

/** Starts a process that has no estimate panel of its own (handoff-phase) and opens its run. */
async function startPlain(aProcess: string): Promise<void> {
  await openProcessesTab(page);
  await page.getByTestId(`start-${aProcess}`).click();
  await expect(tab(page, new RegExp(`^${aProcess} · run \\d+$`)).first()).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('run-heading')).toBeVisible({ timeout: 30_000 });
}

// ---- REQ-UI-087 and REQ-UI-089 (Process run) ---------------------------------------------------------------------------

const est = new Fixture('EstRun');
const working = new Fixture('EstWorking');

test('REQ-UI-087 a build-phase run on F1 shows its output tokens and time so far beside 6,000/9,000/12,000 and 12 m/15 m/18 m, uncombined; REQ-UI-089 your own testing reads not measured', async () => {
  setup(est, f1());
  // 1,500 output tokens on the first handed call, 3,000 on the second, none after: 4,500 in all (the acceptance's figure).
  model.plan((c) => (c === 0 ? 1500 : c === 1 ? 3000 : 0));
  await startWithEstimate(est, 'build-phase');
  expect(await driveRun(page, 480_000)).toMatch(/finished|stopped/);

  const vCard = page.getByTestId('run-against-estimate');
  await expect(vCard).toBeVisible({ timeout: 30_000 });
  // The estimate kept at start: F1's phase total (BRD-195), tokens and time each in their own cell.
  await expect(page.getByTestId('estimate-tokens')).toHaveText('6,000 · 9,000 · 12,000');
  await expect(page.getByTestId('estimate-time')).toHaveText('12 m · 15 m · 18 m');
  // (a) the output tokens the run spent, counted from the usage each call returned; never "not measured yet".
  expect(model.reported.slice(0, 2)).toEqual([1500, 3000]);
  await expect(page.getByTestId('so-far-tokens')).toHaveText('4,500');
  await expect(vCard).not.toContainText('not measured yet');
  // (b) time so far is a duration, beside the time estimate and not combined with it.
  const vTime = (await page.getByTestId('so-far-time').innerText()).trim();
  expect(vTime).toMatch(/^\d+ s$|^\d+ m$|^\d+ h \d{2} m$/);
  expect(vTime).not.toContain('·');
  expect(vTime).not.toBe((await page.getByTestId('estimate-time').innerText()).trim());
  await expect(page.getByTestId('so-far-tokens')).not.toContainText(/\d\s*[·/]\s*\d|of |%/);
  // The owner's own testing is never a number (REQ-UI-089, Process run part).
  await expect(page.getByTestId('run-against-owner-value')).toHaveText('not measured');
  await page.getByTestId('run-against-note').scrollIntoViewIfNeeded();
  await page.screenshot({ path: join(SCREENS, 'process-run-estimate.png'), fullPage: true });
});

test('REQ-UI-087 while the run is still working the card already shows the 1,500 output tokens its first model call returned, beside the estimate kept at start', async () => {
  setup(working, f1());
  // The second handed call is held 45 s, so the run is still going (step 2 working) while the first call's 1,500 is on record.
  model.plan((c) => (c === 0 ? 1500 : 0), (c) => (c === 1 ? 45_000 : 0));
  await startWithEstimate(working, 'build-phase');

  await expect(page.getByTestId('handed-step-state')).toContainText('working', { timeout: 60_000 });
  await expect(page.getByTestId('run-state')).not.toContainText(/finished|stopped/i);
  await expect(page.getByTestId('run-against-estimate')).toBeVisible({ timeout: 20_000 });
  await expect(page.getByTestId('estimate-tokens')).toHaveText('6,000 · 9,000 · 12,000');
  await expect(page.getByTestId('so-far-tokens')).toHaveText('1,500', { timeout: 20_000 });
  await expect(page.getByTestId('so-far-tokens')).not.toContainText('not measured');
  await expect(page.getByTestId('run-against-owner-value')).toHaveText('not measured');
  await driveRun(page, 300_000).catch(() => undefined);
});

// ---- REQ-FN-113 ---------------------------------------------------------------------------------------------------------

const hand = new Fixture('EstHandoff');

test('REQ-FN-113 a handoff-phase run on F6 shows build 6,000/9,000/12,000 beside 10,500 tokens apportioned within, and 1 run that named no row apart', async () => {
  // The estimate is made by the real flow (Start > confirm on build-phase, which writes the phase's estimate batch from F1) ...
  setup(hand, f1());
  model.plan(() => 0);
  await startWithEstimate(hand, 'build-phase');
  await driveRun(page, 480_000);
  // ... then F6's actual runs R1 to R4 are on record (they touch the phase's rows, so the batch above is untouched).
  setupActuals(hand);
  await startPlain('handoff-phase');

  const vPanel = page.getByTestId('handoff-panel');
  await expect(vPanel).toBeVisible({ timeout: 60_000 });
  await expect(page.getByTestId('handoff-build-estimate')).toContainText('6,000 · 9,000 · 12,000 tokens');
  await expect(page.getByTestId('handoff-build-estimate')).toContainText('12 m · 15 m · 18 m');
  await expect(page.getByTestId('handoff-build-actual-tokens')).toHaveText('10,500 tokens');
  await expect(page.getByTestId('handoff-build-actual-time')).toHaveText('18 m');
  await expect(page.getByTestId('handoff-build-apportioned')).toHaveText('apportioned');
  await expect(page.getByTestId('handoff-build-where')).toContainText('within');
  await expect(page.getByTestId('handoff-build')).not.toContainText('insufficient data');
  // The verify run that named no row is in no figure and stands apart.
  await expect(page.getByTestId('handoff-no-row')).toHaveCount(1);
  await expect(page.getByTestId('handoff-no-row')).toContainText('1 run named no row — 2,000 tokens out, 2 m');
  await expect(page.getByTestId('handoff-qa-actual')).not.toContainText('2,000');
  // The owner's testing is not a number.
  await expect(page.getByTestId('handoff-owner-actual')).toHaveText('not measured');
  await vPanel.scrollIntoViewIfNeeded();
  await page.screenshot({ path: join(SCREENS, 'process-run-handoff.png'), fullPage: true });
  await driveRun(page, 300_000).catch(() => undefined);
});
