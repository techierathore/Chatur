// Acceptance tests: All projects (REQ-FN-076, REQ-UI-067, REQ-FN-077, REQ-FN-078). Black-box, web harness.
// Two throwaway project folders are built under tests/.artifacts/verify/fixtures/ and opened through the app's
// own Start flow so Chatur knows them: "allprojspec-a" holds two checklist files (2 verified, 3 not verified),
// "allprojspec-b" holds one checklist with a single open requirement.
// Row anchors follow the mockup: project-row-<slug>, open-<slug>; cells in column order Project, Folder, Phase, Reqs,
// Open, Verified, Last worked, State, (open link). The previously selected project is restored at the end.
import { test, expect, Page } from '@playwright/test';
import { mkdirSync, rmSync, writeFileSync } from 'fs';
import * as path from 'path';
import { signIn } from './support/auth';
import { openFoldersDialog } from './support/start';
import { query, run, snapshot, restore, ROLE_TABLES } from './support/db';

test.setTimeout(180_000);

const ROOT = path.resolve('tests/.artifacts/verify/fixtures/all-projects-spec/root');
const NAME_A = 'allprojspec-a';
const NAME_B = 'allprojspec-b';
const SHOTS = 'tests/.artifacts/verify';
let objPrevious = '';

function checklist(aRows: string[][]): string {
  const vRows = aRows.map((r) => `| ${r[0]} | ${r[1]} | ${r[2]} | ${r[3]} | ${r[4]} | [view](#d-${r[0].toLowerCase()}) |`).join('\n');
  const vDetails = aRows
    .map((r) => `- **${r[0]}** — ${r[1]}. *BRD:* BRD-1\n  - *Acceptance:* ${r[1]} works.\n`)
    .join('\n');
  return `# Spec — Checklist\n\n## Requirements Status\n\n| ID | Requirement | Status | % | Remarks | Details |\n|----|-------------|--------|---|---------|---------|\n${vRows}\n\n## Page: Whole app (\`/\`)\n\n${vDetails}\n`;
}

function buildFixture(): void {
  rmSync(ROOT, { recursive: true, force: true });
  const vDocsA = path.join(ROOT, NAME_A, 'docs');
  const vDocsB = path.join(ROOT, NAME_B, 'docs');
  mkdirSync(vDocsA, { recursive: true });
  mkdirSync(vDocsB, { recursive: true });
  writeFileSync(path.join(vDocsA, 'AllSpecA-Checklist.md'), checklist([
    ['REQ-NFR-001', 'Opens fast', 'Verified', '100%', 'ok'],
    ['REQ-NFR-002', 'Stays small', 'Verified', '100%', 'ok'],
  ]));
  writeFileSync(path.join(vDocsA, 'AllSpecA-P2-Checklist.md'), checklist([
    ['REQ-UI-001', 'Form keeps input', 'Implemented', '100%', 'done'],
    ['REQ-FN-001', 'Run resumes', 'Not Started', '0%', '—'],
    ['REQ-RAG-001', 'Routing picks model', 'Blocked', '25%', 'waits'],
  ]));
  writeFileSync(path.join(vDocsB, 'AllSpecB-Checklist.md'), checklist([
    ['REQ-FN-001', 'Single open one', 'Not Started', '0%', '—'],
  ]));
}

async function addRootFolder(aPage: Page): Promise<void> {
  await aPage.goto('/start?all=1', { waitUntil: 'networkidle' });
  const vDialog = await openFoldersDialog(aPage);
  if ((await vDialog.getByText(ROOT, { exact: true }).count()) === 0) {
    await vDialog.getByTestId('new-folder-path').fill(ROOT);
    await vDialog.getByTestId('add-folder').click();
    await expect(vDialog.getByText(ROOT, { exact: true })).toBeVisible({ timeout: 30_000 });
  }
  await vDialog.getByTestId('folders-dialog-close').click();
  await expect(vDialog).toBeHidden();
}

/** Picks a project by name through Start (the app's own flow) unless it is already the selected one. */
async function selectProject(aPage: Page, aName: string): Promise<void> {
  await aPage.goto('/', { waitUntil: 'networkidle' });
  const vName = aPage.getByTestId('project-switch-name');
  if (await vName.waitFor({ timeout: 10_000 }).then(() => true, () => false)) {
    if ((await vName.innerText()).trim() === aName) return;
  }
  await addRootFolder(aPage);
  const vRow = aPage.getByTestId(/^project-\d+-name$/).filter({ hasText: new RegExp(`^${aName}$`) });
  await expect(vRow).toBeVisible({ timeout: 60_000 });
  await vRow.click();
  await expect(aPage).not.toHaveURL(/\/start/);
}

async function openAllProjects(aPage: Page): Promise<void> {
  await aPage.goto('/all-projects', { waitUntil: 'networkidle' });
  await expect(aPage.getByTestId('projects-heading')).toBeVisible();
}

function projectRow(aPage: Page, aName: string) {
  return aPage.locator('[data-testid^="project-row-"]').filter({ hasText: aName });
}

test.beforeAll(async ({ browser }) => {
  test.setTimeout(180_000);
  buildFixture();
  const vPage = await browser.newPage({ baseURL: process.env.BASE_URL ?? 'http://localhost:5290' });
  try {
    await signIn(vPage, 'Owner');
    await vPage.goto('/', { waitUntil: 'networkidle' });
    const vName = vPage.getByTestId('project-switch-name');
    if (await vName.waitFor({ timeout: 8_000 }).then(() => true, () => false)) objPrevious = (await vName.innerText()).trim();
  } catch {
    objPrevious = '';
  } finally {
    await vPage.close();
  }
});

test.beforeEach(async ({ page }) => {
  await expect(async () => {
    await signIn(page, 'Owner');
  }).toPass({ timeout: 120_000, intervals: [500] });
});

test('REQ-FN-076 every project Chatur knows is listed with its phase and when it was last worked', async ({ page }) => {
  // REQ-FN-076: make Chatur know two projects (open both through Start so each has been worked on today), note the
  // project selected before, then open All projects. Both fixture projects must be listed, each row must show a
  // phase ("Phase N") and a last-worked value; the table must have a row for every project Start lists.
  await selectProject(page, NAME_A);
  await selectProject(page, NAME_B);
  await page.goto('/start?all=1', { waitUntil: 'networkidle' });
  await expect(page.getByTestId('recent-loading')).toHaveCount(0, { timeout: 30_000 });
  const vKnown = (await page.getByTestId(/^project-\d+-name$/).allInnerTexts()).map((t) => t.trim()).filter(Boolean);
  expect(vKnown).toContain(NAME_A);
  expect(vKnown).toContain(NAME_B);

  await openAllProjects(page);
  const vRows = page.locator('[data-testid^="project-row-"]');
  await expect(vRows.first()).toBeVisible({ timeout: 15_000 });
  for (const vProject of vKnown) {
    await expect(projectRow(page, vProject).first(), `project ${vProject} listed`).toBeVisible();
  }
  for (const vProject of [NAME_A, NAME_B]) {
    const vCells = projectRow(page, vProject).first().locator('td');
    await expect(vCells.nth(2), `${vProject} phase`).toHaveText(/Phase\s*\d+/);
    const vWhen = (await vCells.nth(6).innerText()).trim();
    expect(vWhen.length, `${vProject} last worked shown`).toBeGreaterThan(0);
    expect(vWhen, `${vProject} last worked is a real time, not a dash`).not.toMatch(/^[—-]$/);
  }
  await page.screenshot({ path: `${SHOTS}/all-projects.png`, fullPage: true });
});

test('REQ-UI-067 a project row shows how many requirements are open and how many are verified', async ({ page }) => {
  // REQ-UI-067: project A has 5 requirements, 2 Verified and 3 not verified; project B has 1 open and none verified.
  // The row of each shows the Open and Verified counts (columns 5 and 6) matching the checklists on disk.
  await selectProject(page, NAME_A);
  await selectProject(page, NAME_B);
  await openAllProjects(page);
  const vA = projectRow(page, NAME_A).first();
  await expect(vA).toBeVisible({ timeout: 15_000 });
  await expect(vA.locator('td').nth(4), 'A open count').toHaveText(/^\s*3\s*$/);
  await expect(vA.locator('td').nth(5), 'A verified count').toHaveText(/^\s*2\s*$/);
  const vB = projectRow(page, NAME_B).first();
  await expect(vB.locator('td').nth(4), 'B open count').toHaveText(/^\s*1\s*$/);
  await expect(vB.locator('td').nth(5), 'B verified count').toHaveText(/^\s*0\s*$/);
});

test('REQ-FN-077 opening a project from the view selects it and opens Project home', async ({ page }) => {
  // REQ-FN-077: select project B, open All projects, open project A from its row. Then A is the selected project
  // (project-switch-name) and Project home opens (the workbench at "/", not the All projects route).
  await selectProject(page, NAME_A);
  await selectProject(page, NAME_B);
  const vWas = (await page.getByTestId('project-switch-name').innerText()).trim();
  expect(vWas).toBe(NAME_B);
  await openAllProjects(page);
  const vRow = projectRow(page, NAME_A).first();
  await expect(vRow).toBeVisible({ timeout: 15_000 });
  // The page is prerendered; a click before the Blazor circuit attaches reaches no handler, so it is repeated until the page leaves.
  await expect(async () => {
    if (/\/all-projects/.test(page.url())) await vRow.locator('[data-testid^="open-"]').first().click();
    await expect(page).not.toHaveURL(/\/all-projects/, { timeout: 3_000 });
  }).toPass({ timeout: 60_000 });
  await expect(page.getByTestId('project-switch-name')).toHaveText(NAME_A, { timeout: 30_000 });
  await page.screenshot({ path: `${SHOTS}/all-projects-opened.png`, fullPage: true });
});

/** The pretend App Manager's store, as the other machine would read it (test-only route of the web harness). */
async function storeRecords(aPage: Page): Promise<Array<{ deviceId: string; deviceName: string; json: string }>> {
  const vResponse = await aPage.request.get('/test/role-sync');
  expect(vResponse.ok()).toBeTruthy();
  const vAll = (await vResponse.json()) as Record<string, Array<{ deviceId: string; deviceName: string; json: string }>>;
  return Object.values(vAll).flat();
}

async function openAnalystRules(aPage: Page): Promise<void> {
  await aPage.goto('/settings/agents', { waitUntil: 'domcontentloaded' });
  await expect(aPage.getByTestId('agent-list')).toBeVisible({ timeout: 90_000 });
  await aPage.waitForTimeout(2000);
  await aPage.getByTestId('agent-analyst').click();
  await expect(aPage.getByTestId('agent-detail-head')).toBeVisible();
  await aPage.getByTestId('subtab-button').click();
  await aPage.getByTestId('subtab-rules').click();
  await aPage.waitForTimeout(400);
}

async function syncNow(aPage: Page): Promise<void> {
  await openAllProjects(aPage);
  await aPage.waitForTimeout(1500); // let the Blazor circuit attach before clicking
  await aPage.getByTestId('sync-now').click();
  await expect(aPage.getByTestId('sync-outcome')).toBeVisible({ timeout: 30_000 });
}

test('REQ-FN-078 roles and rules changed on one machine are on the other after syncing through App Manager', async ({ page }) => {
  // REQ-FN-078: the web harness stands a file in for App Manager (appsettings RoleSync:StoreFile) and acts as a second
  // machine, TR-WIN-DESK, under the same account. Machine 1 (the app) changes a rule on Settings > Agents and syncs: the
  // store then holds it for the other machine. Machine 2 changes a rule and syncs; machine 1 syncs and then shows it on
  // Settings > Agents. Decided: the second machine is driven through the harness's test-only routes, never App Manager.
  const vSnap = snapshot(ROLE_TABLES);
  const vFromHere = `Verify FN-078 rule from this machine ${Date.now()}.`;
  const vFromThere = `Verify FN-078 rule from the other machine ${Date.now()}.`;
  try {
    expect((await page.request.delete('/test/role-sync')).ok()).toBeTruthy();
    await syncNow(page); // baseline: this machine is in the store
    await expect(page.getByTestId('sync-machine-this')).toBeVisible();
    await expect(page.getByTestId('sync-state-this')).toHaveText(/up to date/);

    // machine 1 changes a rule and syncs
    await openAnalystRules(page);
    const vRules = page.locator('textarea[data-testid="rules-text"], [data-testid="rules-text"] textarea').first();
    await vRules.fill(`${await vRules.inputValue()}\n${vFromHere}`);
    await page.getByTestId('save-agent').click();
    await expect(page.getByText(/saved as v\d+/i).first()).toBeVisible({ timeout: 30_000 });
    await syncNow(page);
    await expect(page.getByTestId('sync-outcome')).toContainText(/Sent/);
    const vHeld = await storeRecords(page);
    expect(vHeld.some((r) => r.json.includes(vFromHere)), 'the store holds the rule changed here, for the other machine').toBeTruthy();

    // machine 2 changes a rule and syncs; machine 1 syncs and has it
    const vPeer = await page.request.post(`/test/role-sync/peer?addRule=${encodeURIComponent(vFromThere)}`);
    expect(vPeer.ok()).toBeTruthy();
    await syncNow(page);
    await expect(page.getByTestId('sync-outcome')).toContainText(/Took/);
    await expect(page.getByTestId('sync-machine-tr-win-desk')).toContainText('TR-WIN-DESK');
    await expect(page.getByTestId('sync-state-tr-win-desk')).toHaveText(/up to date/);
    await openAnalystRules(page);
    await expect(page.locator('textarea[data-testid="rules-text"], [data-testid="rules-text"] textarea').first()).toHaveValue(new RegExp(vFromThere.replace(/\./g, '\\.')));
    await openAllProjects(page);
    await expect(page.getByTestId('sync-panel')).toBeVisible();
    await page.getByTestId('projects-panel').screenshot({ path: `${SHOTS}/all-projects-table.png` });
    await page.getByTestId('sync-panel').screenshot({ path: `${SHOTS}/all-projects-sync.png` });
  } finally {
    restore(vSnap);
    await page.request.delete('/test/role-sync');
  }
});

test.afterAll(async ({ browser }) => {
  test.setTimeout(180_000);
  // Put the previously selected project back (the selection is one shared server-side value), then remove what this
  // spec made: the fixture projects' rows, the named root folder's row, and the folders on disk.
  const vPage = await browser.newPage({ baseURL: process.env.BASE_URL ?? 'http://localhost:5290' });
  try {
    await signIn(vPage, 'Owner');
    if (objPrevious && objPrevious !== NAME_A && objPrevious !== NAME_B) await selectProject(vPage, objPrevious);
  } catch {
    // best effort; the cleanup below still runs
  } finally {
    await vPage.close();
  }
  try {
    const vLike = `${ROOT}%`;
    run('DELETE FROM Setting WHERE Key = ? AND Value IN (SELECT CAST(ProjectId AS TEXT) FROM Project WHERE Path LIKE ?)', 'SelectedProjectId', vLike);
    run('DELETE FROM Project WHERE Path LIKE ?', vLike);
    run('DELETE FROM ProjectFolder WHERE Path = ?', ROOT);
    expect(query('SELECT 1 FROM Project WHERE Path LIKE ?', vLike)).toHaveLength(0);
  } catch {
    // a project with history cannot be deleted; the app's own folder removal hides it
  }
  rmSync(ROOT, { recursive: true, force: true });
});
