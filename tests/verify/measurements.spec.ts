// Acceptance tests: Measurements and Measurements across projects (REQ-UI-115 to REQ-UI-121, REQ-FN-313, REQ-FN-314; BRD-253
// to BRD-262; mockups/measurements.html and measurements-all.html). Black-box, web harness, signed in as test user 2 ('Owner').
// The project is a scratch folder under tests/verify/bin holding docs/metrics/runs.jsonl, gates.jsonl and misses.jsonl that
// this file writes, opened through Start's own "Open a folder" dialog; it is removed with its project when the file ends.
//
// The seed (dates are relative to now, in this machine's time zone, so every period has a known answer):
//   line 1  build-phase   claude-sonnet-4-5  today          in 1,000  out 2,000  cache 5,000 / 300  $0.50   10 min  pass
//   line 2  verify-phase  claude-opus-4-1    3 days ago     in   400  out   800  cache   100 /  50  $1.25    2 min  pass
//   line 3  build-phase   claude-sonnet-4-5  20 days ago    in 3,000  out 4,000  cache     0 /   0  no cost 5 min  fail
//   line 4  triage-issues gpt-5-mini         100 days ago   no tokens at all, no cost, no time
// So: All history 4 runs · Today 1 · Last 7 days 2 · Last 30 days 3. Misses: four (gate, self-smoke, owner, and one that
// names no one), one of them fixed and Verified. Gates: three rows graded, two of them passed the first time.
import { test, expect, Page } from '@playwright/test';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'fs';
import { inflateRawSync } from 'zlib';
import { join } from 'path';
import { signIn } from './support/auth';
import { removeProjectsUnder, scratchDir } from './support/cleanup';
import { openByDialog, pick } from './support/workbench-b';

test.describe.configure({ timeout: 240_000 });

const STAMP = Date.now();
const NAME = `meas-${STAMP}`;
const SLUG = NAME.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
const SHOTS = 'docs/screenshots';
let objRoot = '';

/** A moment `aDaysAgo` days back at noon, in this machine's time zone, as the ISO text a record carries. */
function at(aDaysAgo: number): string {
  const vDate = new Date();
  vDate.setDate(vDate.getDate() - aDaysAgo);
  vDate.setHours(12, 0, 0, 0);
  return vDate.toISOString();
}

/**
 * Shows the page in the light theme for a screenshot and waits for the colour fade to end: the table rows and buttons fade
 * their colours, and a screenshot taken at once caught the dark theme's light text on the light background.
 */
async function lightForShot(aPage: Page): Promise<void> {
  await aPage.evaluate(() => document.documentElement.classList.remove('dark'));
  await aPage.waitForTimeout(800);
}

const lines = (aRecords: object[]): string => aRecords.map((r) => JSON.stringify(r)).join('\n') + '\n';

function seed(): void {
  const vMetrics = join(objRoot, 'docs', 'metrics');
  mkdirSync(vMetrics, { recursive: true });
  writeFileSync(join(vMetrics, 'runs.jsonl'), lines([
    { kind: 'run', ts: at(0), cmd: 'build-phase', mode: 'yolo', model: 'claude-sonnet-4-5', tokens_in: 1000, tokens_out: 2000, tokens_cache_read: 5000, tokens_cache_write: 300, cost_usd: 0.5, duration_s: 600, build_result: 'pass', reqs_touched: ['REQ-UI-001'] },
    { kind: 'run', ts: at(3), cmd: 'verify-phase', model: 'claude-opus-4-1', tokens_in: 400, tokens_out: 800, tokens_cache_read: 100, tokens_cache_write: 50, cost_usd: 1.25, duration_s: 120, build_result: 'pass', reqs_touched: ['REQ-UI-001', 'REQ-FN-002'] },
    { kind: 'run', ts: at(20), cmd: 'build-phase', model: 'claude-sonnet-4-5', tokens_in: 3000, tokens_out: 4000, tokens_cache_read: 0, tokens_cache_write: 0, duration_s: 300, build_result: 'fail', reqs_touched: [] },
    { kind: 'run', ts: at(100), cmd: 'triage-issues', model: 'gpt-5-mini', tokens_scope: 'none' },
  ]));
  writeFileSync(join(vMetrics, 'gates.jsonl'), lines([
    { kind: 'gate', ts: at(0), app: 'Meas', req_id: 'REQ-UI-001', verdict: 'Verified', attempt: 1 },
    { kind: 'gate', ts: at(1), app: 'Meas', req_id: 'REQ-FN-002', verdict: 'Failed', attempt: 1 },
    { kind: 'gate', ts: at(0), app: 'Meas', req_id: 'REQ-FN-002', verdict: 'Verified', attempt: 2 },
    { kind: 'gate', ts: at(2), app: 'Meas', req_id: 'REQ-NFR-001', verdict: 'Verified', attempt: 1 },
  ]));
  writeFileSync(join(vMetrics, 'misses.jsonl'), lines([
    { kind: 'miss', ts: at(0), miss_id: 'miss-a1', req_id: 'REQ-UI-001', found_by: 'gate', found_gate: 'verify', miss_class: 'logic', severity: 'major', what: 'The form lost its input' },
    { kind: 'miss', ts: at(1), miss_id: 'miss-a2', req_id: 'REQ-UI-001', found_by: 'self-smoke', miss_class: 'ui', severity: 'minor', what: 'A label was cut off' },
    { kind: 'miss', ts: at(2), miss_id: 'miss-a3', req_id: 'REQ-FN-002', found_by: 'owner', miss_class: 'spec', severity: 'major' },
    { kind: 'miss', ts: at(2), miss_id: 'miss-a4', miss_class: 'copy', severity: 'minor' },
    { kind: 'miss-fix', ts: at(0), miss_id: 'miss-a1', fix_attempt: 1, verdict_after: 'Verified' },
  ]));
}

test.beforeAll(() => {
  objRoot = scratchDir(NAME);
  seed();
});

test.afterAll(() => {
  if (objRoot) removeProjectsUnder(objRoot);
});

/** Signs in and makes the scratch project the open one (opening it again when another is open). */
async function ready(aPage: Page): Promise<void> {
  await expect(async () => {
    await signIn(aPage, 'Owner');
  }).toPass({ timeout: 120_000, intervals: [500] });
  await aPage.goto('/', { waitUntil: 'networkidle' });
  const vName = aPage.getByTestId('project-switch-name');
  const vOpen = await vName.waitFor({ timeout: 10_000 }).then(() => vName.innerText(), () => '');
  if (vOpen.trim() !== NAME) await openByDialog(aPage, 'open-folder', objRoot, NAME);
}

async function openMeasurements(aPage: Page): Promise<void> {
  await aPage.goto('/measurements', { waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('metrics-heading')).toBeVisible();
  await expect(aPage.getByTestId('tile-runs-value')).not.toHaveText('—', { timeout: 30_000 });
}

async function choosePeriod(aPage: Page, aPrefix: string, aItem: string): Promise<void> {
  await aPage.getByTestId(`${aPrefix}-button`).click();
  await aPage.getByTestId(`${aPrefix}-${aItem}`).click();
}

const text = (aPage: Page, aId: string) => aPage.getByTestId(aId);

/** The seeded zip's files, read without a library: the central directory, then each entry inflated. */
function readZip(aFile: string): Map<string, string> {
  const vBuf = readFileSync(aFile);
  let vEnd = vBuf.length - 22;
  while (vEnd >= 0 && vBuf.readUInt32LE(vEnd) !== 0x06054b50) vEnd--;
  expect(vEnd, 'the zip has an end-of-directory record').toBeGreaterThanOrEqual(0);
  const vCount = vBuf.readUInt16LE(vEnd + 10);
  let vAt = vBuf.readUInt32LE(vEnd + 16);
  const vFiles = new Map<string, string>();
  for (let i = 0; i < vCount; i++) {
    expect(vBuf.readUInt32LE(vAt)).toBe(0x02014b50);
    const vMethod = vBuf.readUInt16LE(vAt + 10);
    const vCompressed = vBuf.readUInt32LE(vAt + 20);
    const vNameLen = vBuf.readUInt16LE(vAt + 28);
    const vExtraLen = vBuf.readUInt16LE(vAt + 30);
    const vCommentLen = vBuf.readUInt16LE(vAt + 32);
    const vLocal = vBuf.readUInt32LE(vAt + 42);
    const vName = vBuf.toString('utf8', vAt + 46, vAt + 46 + vNameLen);
    const vDataAt = vLocal + 30 + vBuf.readUInt16LE(vLocal + 26) + vBuf.readUInt16LE(vLocal + 28);
    const vData = vBuf.subarray(vDataAt, vDataAt + vCompressed);
    vFiles.set(vName, (vMethod === 0 ? vData : inflateRawSync(vData)).toString('utf8'));
    vAt += 46 + vNameLen + vExtraLen + vCommentLen;
  }
  return vFiles;
}

test('REQ-UI-115 Measurements opens from Project in the main menu for the open project and reads only its own files', async ({ page }) => {
  // REQ-UI-115: Project ▸ Measurements opens the screen in the secondary window and names the open project and its
  // docs/metrics folder; no request leaves for anything but the app's own address while it loads.
  await ready(page);
  const vForeign: string[] = [];
  const vOrigin = new URL(page.url()).origin;
  page.on('request', (r) => {
    const vUrl = r.url();
    if (!vUrl.startsWith(vOrigin) && !vUrl.startsWith('data:') && !vUrl.startsWith('blob:') && !vUrl.startsWith('about:')) vForeign.push(vUrl);
  });
  await pick(page, 'project', 'measurements');
  await expect(page).toHaveURL(/\/measurements$/);
  await expect(text(page, 'window-title')).toHaveText('Measurements');
  await expect(text(page, 'metrics-project')).toHaveText(NAME);
  await expect(text(page, 'metrics-folder')).toContainText(join(objRoot, 'docs', 'metrics').replace(/\\/g, '/'));
  await expect(text(page, 'tile-runs-value')).toHaveText('4', { timeout: 30_000 });
  await expect(text(page, 'metrics-offline-note')).toContainText('nothing is sent anywhere');
  await expect(text(page, 'nav-project-measurements')).toBeVisible();
  await expect(text(page, 'nav-measurements-all')).toBeVisible();
  expect(vForeign, 'no request left the app\'s own origin').toEqual([]);
});

test('REQ-UI-116 the period is picked and the figures follow it', async ({ page }) => {
  // REQ-UI-116: All history to begin with (4 runs); Today 1; Last 7 days 2; Last 30 days 3; Between two dates shows two
  // dates and, with To before From, keeps the figures and says why.
  await ready(page);
  await openMeasurements(page);
  await expect(text(page, 'tile-runs-value')).toHaveText('4');
  await expect(text(page, 'period-button')).toContainText('All history');
  await choosePeriod(page, 'period', 'today');
  await expect(text(page, 'tile-runs-value')).toHaveText('1', { timeout: 15_000 });
  await expect(text(page, 'tile-tokens-out-value')).toHaveText('2,000');
  await choosePeriod(page, 'period', '7');
  await expect(text(page, 'tile-runs-value')).toHaveText('2', { timeout: 15_000 });
  await expect(text(page, 'tile-tokens-out-value')).toHaveText('2,800');
  await choosePeriod(page, 'period', '30');
  await expect(text(page, 'tile-runs-value')).toHaveText('3', { timeout: 15_000 });
  await expect(text(page, 'tile-tokens-out-value')).toHaveText('6,800');
  await choosePeriod(page, 'period', 'between');
  await expect(text(page, 'period-from')).toBeVisible();
  await expect(text(page, 'period-to')).toBeVisible();
  await expect(text(page, 'tile-runs-value')).toHaveText('3'); // nothing picked yet: the figures stay
  await choosePeriod(page, 'period', 'all');
  await expect(text(page, 'tile-runs-value')).toHaveText('4', { timeout: 15_000 });
  await expect(text(page, 'period-from')).toHaveCount(0);
});

test('REQ-UI-117 the tiles and the three splits show the seeded sums, with not measured and no price where due', async ({ page }) => {
  // REQ-UI-117: all history, tiles: 4 runs; tokens in 4,400 (one run not measured); out 6,800; cache read 5,100; cache
  // write 350; Measured USD $1.75 with "2 runs no price"; total wall clock 17 m. By model, by agent and by command each
  // line carries its own sums; the run with no tokens reads "not measured" and its cost "no price".
  await ready(page);
  await openMeasurements(page);
  await expect(text(page, 'tile-runs-value')).toHaveText('4');
  await expect(text(page, 'tile-tokens-in-value')).toHaveText('4,400');
  await expect(text(page, 'tile-tokens-in-note')).toHaveText('1 not measured');
  await expect(text(page, 'tile-tokens-out-value')).toHaveText('6,800');
  await expect(text(page, 'tile-cache-read-value')).toHaveText('5,100');
  await expect(text(page, 'tile-cache-write-value')).toHaveText('350');
  await expect(text(page, 'tile-usd-value')).toHaveText('$1.75');
  await expect(text(page, 'tile-usd-note')).toHaveText('2 runs no price');
  await expect(text(page, 'tile-wall-value')).toHaveText('17 m');

  const vSonnet = text(page, 'split-model-claude-sonnet-4-5');
  await expect(vSonnet).toContainText('4,000');
  await expect(vSonnet).toContainText('6,000');
  await expect(vSonnet).toContainText('$0.50');
  await expect(vSonnet).toContainText('15 m');
  await expect(text(page, 'split-model-claude-opus-4-1')).toContainText('$1.25');
  const vMini = text(page, 'split-model-gpt-5-mini');
  await expect(vMini).toContainText('not measured');
  await expect(vMini).toContainText('no price');

  await text(page, 'split-tab-agent').click();
  await expect(text(page, 'split-agent-verifier')).toContainText('800');
  await expect(text(page, 'split-agent-flow-master')).toContainText('6,000');
  await text(page, 'split-tab-command').click();
  await expect(text(page, 'split-command-build-phase')).toContainText('6,000');
  await expect(text(page, 'split-command-verify-phase')).toContainText('$1.25');
  await expect(text(page, 'split-command-triage-issues')).toContainText('not measured');

  // The page scrolls inside its own panel, so a full-page shot stops at the window: a tall window shows every region.
  await page.setViewportSize({ width: 1280, height: 1900 });
  await lightForShot(page);
  mkdirSync(SHOTS, { recursive: true });
  await page.screenshot({ path: join(SHOTS, 'measurements.png'), fullPage: false });
  await page.setViewportSize({ width: 1280, height: 720 });
});

test('REQ-UI-118 misses show how many, who found each, the rows and the first-pass rate', async ({ page }) => {
  // REQ-UI-118: four misses; found by gate 1, self-smoke 1, owner 1 and "not measured" 1; rows REQ-UI-001 twice and
  // REQ-FN-002 once; three rows graded, two of them first time, so 67% and "2 of 3 rows passed the first time"; each miss
  // is a line with its status (miss-a1 was fixed and is Verified, the others are open).
  await ready(page);
  await openMeasurements(page);
  await expect(text(page, 'misses-count')).toHaveText('4 misses');
  for (const vKey of ['gate', 'self-smoke', 'owner']) await expect(text(page, `foundby-${vKey}-count`)).toHaveText('1');
  await expect(text(page, 'foundby-gate')).toContainText('a check');
  await expect(text(page, 'foundby-self-smoke')).toContainText("the builder's own check");
  await expect(text(page, 'foundby-owner')).toContainText('you');
  await expect(text(page, 'foundby-not-measured-count')).toHaveText('1');
  await expect(text(page, 'byrow-req-ui-001-count')).toHaveText('2');
  await expect(text(page, 'byrow-req-fn-002-count')).toHaveText('1');
  await expect(text(page, 'firstpass-rate')).toHaveText('67%');
  await expect(text(page, 'firstpass-text')).toHaveText('2 of 3 rows passed the first time');
  await expect(page.locator('[data-testid^="miss-row-"]')).toHaveCount(4);
  await expect(text(page, 'miss-row-miss-a1')).toContainText('Verified');
  await expect(text(page, 'miss-row-miss-a1')).toContainText('gate');
  await expect(text(page, 'miss-row-miss-a1')).toContainText('major');
  await expect(text(page, 'miss-row-miss-a2')).toContainText('open');
  await expect(text(page, 'miss-row-miss-a3')).toContainText('owner');
});

test('REQ-UI-119 the estimate region shows the shared estimate table or says there is none', async ({ page }) => {
  // REQ-UI-119: the scratch project has no phase estimate, so the region carries its one empty line and never a second,
  // hand-made table; when a phase estimate exists the same EstimateActualTable (panel metrics-estimate-panel) takes its place.
  await ready(page);
  await openMeasurements(page);
  const vRegion = text(page, 'metrics-estimate');
  await expect(vRegion).toBeVisible();
  if (await text(page, 'metrics-estimate-panel').count()) {
    await expect(text(page, 'metrics-estimate-table')).toBeVisible();
    await expect(text(page, 'metrics-estimate-note')).toContainText('Tokens and time are judged apart');
  } else {
    await expect(text(page, 'metrics-estimate-empty')).toContainText('No estimate has been written for this phase');
    await expect(vRegion.locator('table')).toHaveCount(0);
  }
});

test('REQ-UI-120 one line per run, which opens to its command, model, tokens, time, result and rows', async ({ page }) => {
  // REQ-UI-120: four run lines (newest first); opening line 2 (the verify run) shows its command, model, four token kinds,
  // Measured USD, wall clock, result, both rows it touched and the record as written; the run with nothing measured says so.
  await ready(page);
  await openMeasurements(page);
  await expect(page.locator('[data-testid^="run-row-"]')).toHaveCount(4);
  await expect(text(page, 'run-row-1')).toContainText('build-phase');
  await text(page, 'run-row-2').click();
  const vSheet = text(page, 'run-detail');
  await expect(vSheet).toBeVisible();
  await expect(text(page, 'detail-command')).toHaveText('verify-phase');
  await expect(text(page, 'detail-model')).toHaveText('claude-opus-4-1');
  await expect(text(page, 'detail-tokens-in')).toHaveText('400');
  await expect(text(page, 'detail-tokens-out')).toHaveText('800');
  await expect(text(page, 'detail-cache-read')).toHaveText('100');
  await expect(text(page, 'detail-cache-write')).toHaveText('50');
  await expect(text(page, 'detail-usd')).toHaveText('$1.25');
  await expect(text(page, 'detail-wall')).toHaveText('2 m');
  await expect(text(page, 'detail-result')).toHaveText('pass');
  await expect(text(page, 'detail-rows')).toContainText('REQ-UI-001');
  await expect(text(page, 'detail-rows')).toContainText('REQ-FN-002');
  await expect(text(page, 'detail-raw')).toContainText('verify-phase');
  await page.keyboard.press('Escape');
  await expect(vSheet).toBeHidden();
  await text(page, 'run-row-4').click();
  await expect(text(page, 'detail-tokens-out')).toHaveText('not measured');
  await expect(text(page, 'detail-usd')).toHaveText('no price');
});

test('REQ-FN-313 Measurements across projects lists every project with its totals for the period', async ({ page }) => {
  // REQ-FN-313: last 7 days to begin with, so the scratch project reads 2 runs, tokens out 2,800 and $1.75, 4 misses; All
  // history gives 4 runs and "no price" is not shown for it (it has a priced run); Open selects it and shows its Measurements.
  await ready(page);
  await page.goto('/measurements/all', { waitUntil: 'networkidle' });
  await expect(text(page, 'across-heading')).toBeVisible();
  await expect(text(page, 'across-button')).toContainText('Last 7 days');
  const vRow = text(page, `across-row-${SLUG}`);
  await expect(vRow).toBeVisible({ timeout: 30_000 });
  await expect(text(page, `across-${SLUG}-runs`)).toHaveText('2');
  await expect(text(page, `across-${SLUG}-tokens-in`)).toHaveText('1,400');
  await expect(text(page, `across-${SLUG}-tokens-out`)).toHaveText('2,800');
  await expect(text(page, `across-${SLUG}-usd`)).toHaveText('$1.75');
  await expect(text(page, `across-${SLUG}-misses`)).toHaveText('4');
  await expect(text(page, 'across-tile-runs-value')).toHaveText(/^[\d,]+$/);
  await expect(text(page, 'across-tile-projects-value')).toHaveText(/^[1-9]\d*$/);
  await choosePeriod(page, 'across', 'all');
  await expect(text(page, `across-${SLUG}-runs`)).toHaveText('4', { timeout: 15_000 });
  await expect(text(page, `across-${SLUG}-tokens-out`)).toHaveText('6,800');
  await choosePeriod(page, 'across', '7');
  await expect(text(page, `across-${SLUG}-runs`)).toHaveText('2', { timeout: 15_000 });

  await page.setViewportSize({ width: 1280, height: 900 });
  await lightForShot(page);
  mkdirSync(SHOTS, { recursive: true });
  await page.screenshot({ path: join(SHOTS, 'measurements-all.png'), fullPage: false });
  await page.setViewportSize({ width: 1280, height: 720 });

  await text(page, `across-open-${SLUG}`).click();
  await expect(page).toHaveURL(/\/measurements$/);
  await expect(text(page, 'metrics-project')).toHaveText(NAME);
});

test('REQ-FN-314 Save as zip writes one zip whose entries are docs/metrics/<file>', async ({ page }) => {
  // REQ-FN-314: Save as zip offers the project's own folder, Save writes the zip there and shows its path; the zip holds
  // docs/metrics/runs.jsonl, gates.jsonl and misses.jsonl exactly as on disk, and nothing else.
  await ready(page);
  await openMeasurements(page);
  await text(page, 'save-zip').click();
  await expect(text(page, 'zip-dialog')).toBeVisible();
  await expect(text(page, 'zip-folder')).toHaveValue(objRoot);
  await text(page, 'zip-save').click();
  await expect(text(page, 'zip-saved')).toBeVisible({ timeout: 30_000 });
  const vZip = (await text(page, 'zip-saved').innerText()).trim();
  expect(vZip.startsWith(objRoot), `the zip is in ${objRoot}`).toBe(true);
  expect(vZip.endsWith('.zip')).toBe(true);
  expect(existsSync(vZip)).toBe(true);
  const vFiles = readZip(vZip);
  expect([...vFiles.keys()].sort()).toEqual(['docs/metrics/gates.jsonl', 'docs/metrics/misses.jsonl', 'docs/metrics/runs.jsonl']);
  for (const vName of ['runs', 'gates', 'misses']) {
    expect(vFiles.get(`docs/metrics/${vName}.jsonl`)).toBe(readFileSync(join(objRoot, 'docs', 'metrics', `${vName}.jsonl`), 'utf8'));
  }
  // a folder that cannot be written says so
  await text(page, 'zip-folder').fill(join(objRoot, 'docs', 'metrics', 'runs.jsonl', 'nowhere'));
  await text(page, 'zip-save').click();
  await expect(text(page, 'zip-error')).toBeVisible({ timeout: 15_000 });
});

test('REQ-UI-121 Settings Measurements keeps the stream tiles and links to the Measurements screen', async ({ page }) => {
  // REQ-UI-121: the page still shows its six-stream tiles and table; its link "See the runs, tokens, cost and misses"
  // opens /measurements, and the two entries in the navigation are different pages.
  await ready(page);
  await page.goto('/settings/measurements', { waitUntil: 'networkidle' });
  await expect(text(page, 'stream-tiles')).toBeVisible({ timeout: 30_000 });
  await expect(text(page, 'tile-runs-count')).toHaveText('4');
  await expect(text(page, 'streams-table')).toBeVisible();
  const vLink = text(page, 'open-measurements-screen');
  await expect(vLink).toContainText('See the runs, tokens, cost and misses');
  await vLink.click();
  await expect(page).toHaveURL(/\/measurements$/);
  await expect(text(page, 'metrics-heading')).toBeVisible();
  await expect(text(page, 'nav-measurements')).toBeVisible();
  await expect(text(page, 'nav-project-measurements')).toBeVisible();
});
