// Acceptance tests: Requirements board (REQ-UI-049..052). Black-box, web harness.
// A throwaway project folder holding two checklist files is built under tests/.artifacts/verify/fixtures/
// and opened as the project through the app's own Start flow.
import { test, expect, Page } from '@playwright/test';
import { mkdirSync, readFileSync, rmSync, writeFileSync } from 'fs';
import * as path from 'path';
import { signIn } from './support/auth';
import { openFoldersDialog } from './support/start';
import { openBoardTab, openBoardTabAnyState, openAgentsTab, closeTab } from './support/phase2-tabs';

test.describe.configure({ mode: 'serial' });
test.setTimeout(120_000);

const ROOT = path.resolve('tests/.artifacts/verify/fixtures/board-spec/root');
const PROJECT = path.join(ROOT, 'boardspec-proj');
const DOCS = path.join(PROJECT, 'docs');
const P1 = path.join(DOCS, 'BoardSpec-Checklist.md');
const P2 = path.join(DOCS, 'BoardSpec-P2-Checklist.md');
const SHOTS = 'tests/.artifacts/verify';

function checklist(aRows: string[][], aDetails: string): string {
  const vRows = aRows.map((r) => `| ${r[0]} | ${r[1]} | ${r[2]} | ${r[3]} | ${r[4]} | [view](#d-${r[0].toLowerCase()}) |`).join('\n');
  return `# BoardSpec — Checklist\n\n## Requirements Status\n\n| ID | Requirement | Status | % | Remarks | Details |\n|----|-------------|--------|---|---------|---------|\n${vRows}\n\n${aDetails}\n`;
}

function buildFixture(): void {
  rmSync(ROOT, { recursive: true, force: true });
  mkdirSync(DOCS, { recursive: true });
  writeFileSync(P1, checklist(
    [['REQ-NFR-001', 'Opens in two seconds', 'Verified', '100%', 'measured 1.4 s']],
    '## Page: Whole app (`/`)\n\n- **REQ-NFR-001** — Opens in two seconds. *BRD:* BRD-1\n  - *Acceptance:* The window opens within two seconds.\n'));
  writeFileSync(P2, checklist(
    [
      ['REQ-UI-001', 'Settings form keeps what you typed', 'Implemented', '100%', '[REQ-UI-001] validation added'],
      ['REQ-FN-001', 'A run resumes at its step', 'Not Started', '0%', '—'],
      ['REQ-RAG-001', 'Routing picks the model', 'Blocked', '25%', 'waits on the rules'],
    ],
    '## Page: Settings (`/settings/providers`)\n\n- **REQ-UI-001** — Settings form keeps what you typed. *BRD:* BRD-2 · *Mockup:* mockups/settings-providers.html\n  - *Acceptance:* Typing in the providers form and coming back leaves every field as it was.\n\n- **REQ-FN-001** — A run resumes at its step. *BRD:* BRD-3\n  - *Acceptance:* A stopped run carries on at the step it stopped at.\n\n- **REQ-RAG-001** — Routing picks the model. *BRD:* BRD-4\n  - *Acceptance:* The tier picks the model.\n'));
}

async function selectFixtureProject(aPage: Page, aProject = 'boardspec-proj'): Promise<void> {
  await aPage.goto('/', { waitUntil: 'networkidle' });
  const vName = aPage.getByTestId('project-switch-name');
  if (await vName.waitFor({ timeout: 10_000 }).then(() => true, () => false)) {
    if ((await vName.innerText()).trim() === aProject) return;
  }
  await aPage.goto('/start?all=1', { waitUntil: 'networkidle' });
  const vDialog = await openFoldersDialog(aPage);
  if ((await vDialog.getByText(ROOT, { exact: true }).count()) === 0) {
    await vDialog.getByTestId('new-folder-path').fill(ROOT);
    await vDialog.getByTestId('add-folder').click();
    await expect(vDialog.getByText(ROOT, { exact: true })).toBeVisible();
  }
  await vDialog.getByTestId('folders-dialog-close').click();
  await expect(vDialog).toBeHidden();
  const vRow = aPage.getByTestId(/^project-\d+-name$/).filter({ hasText: new RegExp(`^${aProject}$`) });
  await expect(vRow).toBeVisible();
  await vRow.click();
  await expect(aPage).not.toHaveURL(/\/start/);
}

async function openBoard(aPage: Page): Promise<void> {
  await selectFixtureProject(aPage);
  // Project > Board... opens it as a tab in the main window, the conversation still on the right.
  await openBoardTab(aPage);
  await expect(aPage.getByTestId('chat-panel')).toBeVisible();
}

test.beforeAll(() => buildFixture());

test.beforeEach(async ({ page }) => {
  await expect(async () => {
    await signIn(page, 'Owner');
  }).toPass({ timeout: 120_000, intervals: [500] });
});

test('REQ-UI-049 the board shows every requirement with its id, title and status', async ({ page }) => {
  // REQ-UI-049: every requirement of the project shows with its id, title and status.
  await openBoard(page);
  await expect(page.getByTestId('count-all')).toContainText('4');
  await expect(page.getByTestId('showing-count')).toContainText('showing 4 of 4');
  for (const vId of ['REQ-NFR-001', 'REQ-UI-001', 'REQ-FN-001', 'REQ-RAG-001']) {
    await expect(page.getByTestId(`req-row-${vId}`)).toBeVisible();
  }
  await expect(page.getByTestId('req-row-REQ-UI-001')).toContainText('Settings form keeps what you typed');
  await expect(page.getByTestId('status-REQ-UI-001-button')).toContainText('In progress');
  await expect(page.getByTestId('status-REQ-NFR-001-button')).toContainText('Verified');
  await expect(page.getByTestId('status-REQ-FN-001-button')).toContainText('Planned');
  await expect(page.getByTestId('status-REQ-RAG-001-button')).toContainText('Blocked');
  await page.screenshot({ path: `${SHOTS}/board.png`, fullPage: true });
});

test('REQ-UI-050 the board shows the agent that owns each requirement', async ({ page }) => {
  // REQ-UI-050: the type says who owns it, and the board shows that agent against it.
  await openBoard(page);
  await expect(page.getByTestId('type-REQ-UI-001')).toHaveText('REQ-UI');
  await expect(page.getByTestId('agent-REQ-UI-001')).toHaveText('Flow master');
  await expect(page.getByTestId('agent-REQ-FN-001')).toHaveText('Flow master');
  await expect(page.getByTestId('agent-REQ-RAG-001')).toHaveText('Flow master');
  await expect(page.getByTestId('agent-REQ-NFR-001')).toHaveText('Verifier');
  await expect(page.getByTestId('phase-REQ-UI-001')).toHaveText('Phase 2');
  await expect(page.getByTestId('phase-REQ-NFR-001')).toHaveText('Phase 1');
});

test('REQ-UI-051 an opened requirement shows its acceptance line, remarks and screen', async ({ page }) => {
  // REQ-UI-051: opening a requirement shows its acceptance line, remarks and screen.
  await openBoard(page);
  await page.getByTestId('open-REQ-UI-001').click();
  await expect(page.getByTestId('detail-id')).toHaveText('REQ-UI-001');
  await expect(page.getByTestId('detail-acceptance')).toContainText('leaves every field as it was');
  await expect(page.getByTestId('detail-remarks')).toContainText('[REQ-UI-001] validation added');
  await expect(page.getByTestId('detail-screen')).toContainText('Settings');
  await expect(page.getByTestId('detail-screen-link')).toHaveAttribute('href', /\/settings\/providers$/);
  await page.getByTestId('open-REQ-FN-001').click();
  await expect(page.getByTestId('detail-acceptance')).toContainText('carries on at the step');
  await page.screenshot({ path: `${SHOTS}/board-detail.png`, fullPage: true });
});

test('REQ-UI-052 a checklist edited outside Chatur shows its new statuses when the board reopens', async ({ page }) => {
  // REQ-UI-052: a checklist file changed outside Chatur shows its new status when the board is opened again.
  await openBoard(page);
  await expect(page.getByTestId('status-REQ-FN-001-button')).toContainText('Planned');
  const vOriginal = readFileSync(P2, 'utf8');
  try {
    writeFileSync(P2, vOriginal.replace('| REQ-FN-001 | A run resumes at its step | Not Started | 0% |', '| REQ-FN-001 | A run resumes at its step | Verified | 100% |'));
    await closeTab(page, 'Board');
    await openBoardTab(page);
    await expect(page.getByTestId('status-REQ-FN-001-button')).toContainText('Verified', { timeout: 15_000 });
    await expect(page.getByTestId('count-verified')).toContainText('2');
  } finally {
    writeFileSync(P2, vOriginal);
  }
});

/** Chooses one option of a board filter or status select, by the select's own testid. */
async function choose(aPage: Page, aSelect: string, aOption: string): Promise<void> {
  await expect(async () => {
    if (!(await aPage.getByTestId(`${aSelect}-pop`).isVisible())) await aPage.getByTestId(`${aSelect}-button`).click();
    await aPage.getByTestId(`${aSelect}-pop`).getByText(aOption, { exact: true }).click({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
}

test('REQ-FN-056 the board narrows by phase, status, agent and search, and Reset shows everything', async ({ page }) => {
  // Acceptance: filtering by phase, status, agent or search shows only the matching requirements,
  // while the totals still count the whole project.
  await openBoard(page);
  await expect(page.getByTestId('showing-count')).toContainText('showing 4 of 4');

  await choose(page, 'filter-status', 'Blocked');
  await expect(page.getByTestId('showing-count')).toContainText('showing 1 of 4');
  await expect(page.getByTestId('req-row-REQ-RAG-001')).toBeVisible();
  await expect(page.getByTestId('req-row-REQ-UI-001')).toHaveCount(0);
  await expect(page.getByTestId('count-all')).toContainText('4');
  await page.screenshot({ path: `${SHOTS}/board-filtered-status.png`, fullPage: true });

  await page.getByTestId('filter-reset').click();
  await expect(page.getByTestId('showing-count')).toContainText('showing 4 of 4');

  await choose(page, 'filter-phase', 'Phase 1');
  await expect(page.getByTestId('showing-count')).toContainText('showing 1 of 4');
  await expect(page.getByTestId('req-row-REQ-NFR-001')).toBeVisible();
  await page.getByTestId('filter-reset').click();

  await choose(page, 'filter-agent', 'Verifier');
  await expect(page.getByTestId('showing-count')).toContainText('showing 1 of 4');
  await expect(page.getByTestId('req-row-REQ-NFR-001')).toBeVisible();
  await page.getByTestId('filter-reset').click();

  await page.getByTestId('filter-search').fill('REQ-UI');
  await expect(page.getByTestId('showing-count')).toContainText('showing 1 of 4');
  await expect(page.getByTestId('req-row-REQ-UI-001')).toBeVisible();
  await page.getByTestId('filter-search').fill('resumes');
  await expect(page.getByTestId('req-row-REQ-FN-001')).toBeVisible();
  await expect(page.getByTestId('showing-count')).toContainText('showing 1 of 4');

  await page.getByTestId('filter-search').fill('zzzz-nothing');
  await expect(page.getByTestId('board-no-match')).toBeVisible();
  await expect(page.getByTestId('count-all')).toContainText('4');
  await page.getByTestId('filter-reset').click();
  await expect(page.getByTestId('showing-count')).toContainText('showing 4 of 4');
  await expect(page.getByTestId('filter-search')).toHaveValue('');
});

test('REQ-FN-057 a status changed on the board is a change in the checklist file', async ({ page }) => {
  // Acceptance: choosing a status on a row rewrites that row in the project's own checklist file and
  // nothing else in it.
  await openBoard(page);
  const vBefore = readFileSync(P2, 'utf8');
  try {
    await choose(page, 'status-REQ-FN-001', 'In progress');
    await expect(page.getByTestId('status-REQ-FN-001-button')).toContainText('In progress', { timeout: 15_000 });
    await expect(page.getByTestId('count-in-progress')).toContainText('2');
    await expect(async () => {
      const vAfter = readFileSync(P2, 'utf8');
      expect(vAfter).toContain('| REQ-FN-001 | A run resumes at its step | In Progress | 50% |');
      expect(vAfter.replace(/\| REQ-FN-001 \|[^\n]*\n/, '')).toBe(vBefore.replace(/\| REQ-FN-001 \|[^\n]*\n/, ''));
    }).toPass({ timeout: 15_000 });
    await page.screenshot({ path: `${SHOTS}/board-status-changed.png`, fullPage: true });

    // reopen the tab: the board reads the file again and shows what the file now says
    await closeTab(page, 'Board');
    await openBoardTab(page);
    await expect(page.getByTestId('status-REQ-FN-001-button')).toContainText('In progress');
  } finally {
    writeFileSync(P2, vBefore);
  }
});

test('REQ-UI-049 a project whose folder is gone says so and names the folder instead of "No project is open"', async ({ page }) => {
  // REQ-UI-049: the selected project's folder was swept away; Board must not claim that no project is open.
  const vGone = path.join(ROOT, 'goneproj');
  mkdirSync(path.join(vGone, 'docs'), { recursive: true });
  writeFileSync(path.join(vGone, 'docs', 'Gone-Checklist.md'), readFileSync(P1, 'utf8'));
  try {
    await selectFixtureProject(page, 'goneproj');
    await openBoardTab(page);
    await expect(page.getByTestId('count-all')).toContainText('1');
    await closeTab(page, 'Board');
    rmSync(vGone, { recursive: true, force: true });
    await openBoardTabAnyState(page);
    await expect(page.getByTestId('folder-missing')).toContainText("goneproj's folder cannot be found");
    await expect(page.getByTestId('folder-missing-path')).toContainText('goneproj');
    await expect(page.getByTestId('no-project')).toHaveCount(0);
    await expect(page.getByTestId('pick-another-project')).toHaveAttribute('href', /\/start\?all=1$/);
    await page.screenshot({ path: `${SHOTS}/board-folder-missing.png`, fullPage: true });
    // The Agent workspace tells the same truth instead of "0 requirements".
    await openAgentsTab(page);
    await expect(page.getByTestId('open-work-panel').getByTestId('folder-missing')).toContainText("goneproj's folder cannot be found");
    await expect(page.getByTestId('open-work-count')).toHaveText('folder missing');
    await page.screenshot({ path: `${SHOTS}/agent-workspace-folder-missing.png`, fullPage: true });
    // The way out works: Pick another project opens the project list, and a project with a folder shows its rows again.
    await page.getByTestId('pick-another-project').click();
    await expect(page).toHaveURL(/\/start/);
    await selectFixtureProject(page);
    await openBoardTab(page);
    await expect(page.getByTestId('count-all')).toContainText('4');
  } finally {
    rmSync(vGone, { recursive: true, force: true });
  }
});
