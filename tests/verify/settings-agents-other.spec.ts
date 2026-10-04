// Acceptance tests: Settings ▸ Agents, Corrections, Measurements, Appearance
// (REQ-UI-025..030, REQ-FN-028..030, REQ-UI-034..038, REQ-FN-037..043). Black-box, web harness.
//
// Fixture notes (verify-phase step 4):
//  - Roles/corrections/refused-writes are seeded and RESTORED through the harness's own SQLite file (support/db.ts)
//    only where the acceptance line itself is about the database (REQ-UI-025) or where nothing in the UI can
//    create the state (a Correction row, a RefusedWrite row). Every verdict is read back through the UI.
//  - Shared state other writers also use (the selected project, the Appearance settings) is put back at the end.
import { test, expect, Page, Locator } from '@playwright/test';
import { existsSync, mkdirSync, readFileSync, writeFileSync, rmSync } from 'fs';
import { signIn } from './support/auth';
import { openFoldersDialog } from './support/start';
import { ensureTestProvider, chooseTestModel } from './support/provider';
import { startStubModel, ensureStubProvider, chooseStubModel, removeStubProvider } from './support/stub-model';
import { query, run, snapshot, restore, ROLE_TABLES, Snapshot } from './support/db';

// Tests are independent (each restores what it changed); run with --workers=1 because the harness has one session store.

const REPO = '/mnt/c/1MyCode/Chatur';
const FIXTURES = `${REPO}/tests/.artifacts/verify/fixtures`;
const SETTINGS_B = `${FIXTURES}/settings-b`;
const SHOTS = `${REPO}/tests/.artifacts/verify/settings-b-shots`;
mkdirSync(SETTINGS_B, { recursive: true });
mkdirSync(SHOTS, { recursive: true });

const RIGHTS: Record<string, string> = {
  'read-file': 'Reads code',
  'edit-file': 'Changes code',
  'run-build': 'Runs commands',
  'run-source-control': 'Runs source control',
  'mark-verified': 'Marks work verified',
  'correct-wording': 'Corrects its own wording',
};

test.beforeEach(async ({ page }) => {
  test.setTimeout(240_000);
  // another verifier signing in at the same moment can knock this sign-in back to the form; retry a few times
  for (let vTry = 1; ; vTry++) {
    try {
      await signIn(page, 'Yolo');
      break;
    } catch (aError) {
      if (vTry >= 4) throw aError;
      await page.waitForTimeout(3000);
    }
  }
});

// --- helpers -----------------------------------------------------------------------------------------------------

async function open(aPage: Page, aPath: string, aReady: string): Promise<void> {
  await aPage.goto(aPath, { waitUntil: 'domcontentloaded' });
  await expect(aPage.getByTestId(aReady)).toBeVisible({ timeout: 90_000 });
  await aPage.waitForTimeout(2000); // let the Blazor circuit attach before typing into the page
}

function field(aPage: Page, aTestId: string): Locator {
  return aPage
    .locator(`textarea[data-testid="${aTestId}"], input[data-testid="${aTestId}"]`)
    .or(aPage.locator(`[data-testid="${aTestId}"] textarea, [data-testid="${aTestId}"] input`))
    .first();
}

async function openAgent(aPage: Page, aCode: string): Promise<void> {
  await open(aPage, '/settings/agents', 'agent-list');
  await aPage.getByTestId(`agent-${aCode}`).click();
  await expect(aPage.getByTestId('agent-detail-head')).toBeVisible();
  await expect(aPage.getByTestId('agent-detail-head')).toContainText(roleName(aCode), { timeout: 20_000 });
}

function roleName(aCode: string): string {
  return String(query('SELECT Name FROM Role WHERE Code = ?', aCode)[0].Name);
}

async function tab(aPage: Page, aName: string): Promise<void> {
  // the "Showing" picker (mockup subtab-pick): open its list, then choose the view
  await aPage.getByTestId('subtab-button').click();
  await aPage.getByTestId(`subtab-${aName}`).click();
  await aPage.waitForTimeout(400);
}

async function save(aPage: Page): Promise<void> {
  await aPage.getByTestId('save-agent').click();
  await expect(aPage.getByText(/saved as v\d+/i).first()).toBeVisible({ timeout: 30_000 });
}

function roleRow(aCode: string): Record<string, string | number | null> {
  return query('SELECT * FROM Role WHERE Code = ?', aCode)[0];
}

/** The export uses PascalCase keys; compare on lower-cased keys so only the content is judged. */
function lowerKeys(aValue: unknown): unknown {
  if (Array.isArray(aValue)) return aValue.map(lowerKeys);
  if (aValue && typeof aValue === 'object') {
    return Object.fromEntries(Object.entries(aValue as Record<string, unknown>).map(([k, v]) => [k.toLowerCase(), lowerKeys(v)]));
  }
  return aValue;
}

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
/** Today as the pages print it ("30 Sep 2026"), for both the local and the UTC calendar day (server and browser may differ). */
function todayRe(): RegExp {
  const vNow = new Date();
  const vDays = [`${vNow.getDate()} ${MONTHS[vNow.getMonth()]} ${vNow.getFullYear()}`, `${vNow.getUTCDate()} ${MONTHS[vNow.getUTCMonth()]} ${vNow.getUTCFullYear()}`];
  return new RegExp(`(?<!\\d)(${vDays.join('|')})(?!\\d)`);
}

function shot(aPage: Page, aName: string): Promise<Buffer> {
  return aPage.screenshot({ path: `${SHOTS}/${aName}.png`, fullPage: true });
}


// --- model-driven helpers (a real model answers; see support/provider.ts) ----------------------------------------------
const FIXTURE_ROOT = `${SETTINGS_B}/root`;

/** Opens a fixture project through the real Start flow (names the root folder first when needed). */
async function startFixtureProject(aPage: Page, aProject: string): Promise<void> {
  await open(aPage, '/start?all=1', 'recent-side');
  const vItem = aPage.locator('[data-testid^="project-"][data-testid$="-name"]', { hasText: new RegExp(`^${aProject}$`) });
  if ((await vItem.count()) === 0) {
    await openFoldersDialog(aPage);
    if (!(await aPage.getByTestId('folders-dialog').innerText()).includes(FIXTURE_ROOT)) {
      await field(aPage, 'new-folder-path').fill(FIXTURE_ROOT);
      await aPage.getByTestId('add-folder').click();
    }
    await expect(aPage.getByTestId('folders-dialog')).toContainText(FIXTURE_ROOT, { timeout: 20_000 });
    await aPage.getByTestId('folders-dialog-close').click();
    await aPage.waitForTimeout(1500);
    await aPage.reload({ waitUntil: 'domcontentloaded' });
    await expect(aPage.getByTestId('recent-side')).toBeVisible({ timeout: 60_000 });
    await aPage.waitForTimeout(2000);
  }
  await vItem.first().click();
  await expect(aPage.getByTestId('ask-input')).toBeVisible({ timeout: 60_000 });
  await aPage.waitForTimeout(2500);
}

/** On the Workbench: picks the agent and the test model, sends one message and waits until the agent has finished the turn. */
async function askAgent(aPage: Page, aRoleCode: string, aPrompt: string, aNewChat = false, aChooseModel: (aPage: Page) => Promise<void> = chooseTestModel): Promise<void> {
  await aPage.getByTestId('agent-pick').click();
  await aPage.getByTestId('agent-pop').getByTestId(`agent-${aRoleCode}`).click();
  await expect(aPage.getByTestId('agent-pick')).toContainText(roleName(aRoleCode));
  // A new conversation per ask: the model otherwise re-reads every earlier attempt (refused edits
  // included) and keeps repeating them instead of the tool it is asked for.
  if (aNewChat) {
    await aPage.getByTestId('new-chat').click();
    await expect(aPage.getByTestId('conversation-thread').locator('.msg')).toHaveCount(0, { timeout: 15_000 });
  }
  await aChooseModel(aPage);
  await field(aPage, 'ask-input').fill(aPrompt);
  await aPage.getByTestId('send').click();
  await expect(aPage.getByTestId('ask-input')).toBeEnabled({ timeout: 180_000 }); // disabled while the turn runs
  await aPage.waitForTimeout(1500);
}

// --- Agents ------------------------------------------------------------------------------------------------------

test('REQ-UI-025 roles tab shows a role\'s new wording after it is changed in the database, with no file edited', async ({ page }) => {
  const vSnap = snapshot(ROLE_TABLES);
  try {
    const vStamp = new Date().toISOString();
    const vNew = `Verify UI-025 wording written straight into the database at ${vStamp} and nowhere else.`;
    run('UPDATE Role SET Wording = ? WHERE Code = ?', vNew, 'analyst');
    await openAgent(page, 'analyst');
    await expect(field(page, 'wording-text')).toHaveValue(vNew);
    // every seeded role is listed, read from rows
    await expect(page.getByTestId('agent-count')).toHaveText(String(query('SELECT COUNT(*) AS n FROM Role')[0].n));
  } finally {
    restore(vSnap);
  }
});

test('REQ-UI-026 each role shows what it may do, its commands and the tier it uses', async ({ page }) => {
  const vRoles = query('SELECT RoleId, Code, Tier FROM Role ORDER BY RoleId');
  expect(vRoles.length).toBeGreaterThan(0);
  await open(page, '/settings/agents', 'agent-list');
  for (const vRole of vRoles) {
    const vCode = String(vRole.Code);
    await page.getByTestId(`agent-${vCode}`).click();
    await expect(page.getByTestId('agent-detail-head')).toContainText(roleName(vCode), { timeout: 20_000 });

    // tier, in the list and on the wording tab
    await expect(page.getByTestId(`${vCode}-tier`)).toHaveText(`Tier ${vRole.Tier}`);
    await expect(page.getByTestId('wording-tier-button')).toContainText(`Tier ${vRole.Tier}`);

    // rights: one on/off line per right, matching the stored answer
    await tab(page, 'rights');
    const vRights = query('SELECT Action, Allowed FROM RoleRight WHERE RoleId = ?', Number(vRole.RoleId));
    expect(vRights.length).toBeGreaterThan(0);
    for (const vRight of vRights) {
      const vLine = page.getByTestId(`right-${vRight.Action}`);
      await expect(vLine, `${vCode} ${vRight.Action}`).toBeVisible();
      await expect(vLine).toContainText(RIGHTS[String(vRight.Action)]);
      await expect(vLine).toContainText(vRight.Allowed ? /\bon\b/ : /\boff\b/);
    }

    // commands: every stored command is on the commands tab
    await tab(page, 'commands');
    const vCommands = query('SELECT Command FROM RoleCommand WHERE RoleId = ?', Number(vRole.RoleId));
    for (const vCommand of vCommands) {
      await expect(field(page, 'commands-text')).toHaveValue(new RegExp(vCommand.Command!.toString().replace(/[.*+?^${}()|[\]\\]/g, '\\$&')));
    }
    await tab(page, 'wording');
  }
});

test('REQ-FN-028 export writes one file holding every role, right, command and rule with its version', async ({ page }) => {
  await open(page, '/settings/agents', 'agent-list');
  const [vDownload] = await Promise.all([page.waitForEvent('download', { timeout: 30_000 }), page.getByTestId('export-agents').click()]);
  const vFile = `${SETTINGS_B}/roles-export-fn028.json`;
  await vDownload.saveAs(vFile);
  const vJson = lowerKeys(JSON.parse(readFileSync(vFile, 'utf8'))) as any;

  expect(vJson.schema).toBe('chatur-roles-export');
  const vDbRoles = query('SELECT * FROM Role ORDER BY RoleId');
  expect(vJson.roles).toHaveLength(vDbRoles.length);
  for (const vDb of vDbRoles) {
    const vRole = vJson.roles.find((r: { code: string }) => r.code === vDb.Code);
    expect(vRole, `role ${vDb.Code} is in the file`).toBeTruthy();
    expect(vRole.wording).toBe(vDb.Wording);
    expect(vRole.version).toBe(Number(vDb.Version));
    const vDbRights = query('SELECT Action, Allowed FROM RoleRight WHERE RoleId = ?', Number(vDb.RoleId));
    expect(vRole.rights).toHaveLength(vDbRights.length);
    for (const vRight of vDbRights) {
      const vFound = vRole.rights.find((r: { action: string }) => r.action === vRight.Action);
      expect(vFound, `${vDb.Code}/${vRight.Action}`).toBeTruthy();
      expect(vFound.allowed).toBe(Boolean(vRight.Allowed));
    }
    const vDbCommands = query('SELECT Command FROM RoleCommand WHERE RoleId = ?', Number(vDb.RoleId));
    expect(vRole.commands.map((c: { command: string }) => c.command).sort()).toEqual(vDbCommands.map((c) => String(c.Command)).sort());
  }
  const vDbRules = query('SELECT Scope, Text, Version FROM Rule');
  expect(vJson.rules).toHaveLength(vDbRules.length);
  for (const vRule of vDbRules) {
    const vFound = vJson.rules.find((r: { scope: string; text: string }) => r.scope === vRule.Scope && r.text === vRule.Text);
    expect(vFound, `rule ${vRule.Scope}: ${String(vRule.Text).slice(0, 30)}`).toBeTruthy();
    expect(vFound.version).toBe(Number(vRule.Version));
  }
});

test('REQ-FN-029 importing the exported file brings back the same roles with their rights, commands and rules', async ({ page }) => {
  const vSnap = snapshot(ROLE_TABLES);
  try {
    await open(page, '/settings/agents', 'agent-list');
    const [vDownload] = await Promise.all([page.waitForEvent('download', { timeout: 30_000 }), page.getByTestId('export-agents').click()]);
    const vFile = `${SETTINGS_B}/roles-export-fn029.json`;
    await vDownload.saveAs(vFile);

    // "the other machine": roles that differ from the file (wording, a right, a command, a role missing altogether)
    const vBefore = snapshot(ROLE_TABLES);
    const vAnalyst = roleRow('analyst');
    run('UPDATE Role SET Wording = ? WHERE Code = ?', 'The other machine has a different wording for this role altogether.', 'analyst');
    run("UPDATE RoleRight SET Allowed = 1 - Allowed WHERE RoleId = ? AND Action = 'edit-file'", Number(vAnalyst.RoleId));
    run('DELETE FROM RoleCommand WHERE RoleId = ?', Number(vAnalyst.RoleId));
    const vFlow = roleRow('flow-master');
    run('DELETE FROM RoleRight WHERE RoleId = ?', Number(vFlow.RoleId));
    run('DELETE FROM RoleCommand WHERE RoleId = ?', Number(vFlow.RoleId));
    run('DELETE FROM Role WHERE Code = ?', 'flow-master');

    await open(page, '/settings/agents', 'agent-list');
    await page.getByTestId('import-agents').click();
    await expect(page.getByTestId('import-picker')).toBeVisible();
    await page.locator('[data-testid="import-dropzone"] input[type="file"], input[type="file"]').first().setInputFiles(vFile);
    await expect(page.getByText(/were imported/i).first()).toBeVisible({ timeout: 30_000 });

    // the roles there are the same as in the file
    const vJson = lowerKeys(JSON.parse(readFileSync(vFile, 'utf8'))) as any;
    expect(query('SELECT COUNT(*) AS n FROM Role')[0].n).toBe(vJson.roles.length);
    for (const vRole of vJson.roles) {
      const vDb = roleRow(vRole.code);
      expect(vDb, `role ${vRole.code} exists after import`).toBeTruthy();
      expect(vDb.Wording).toBe(vRole.wording);
      expect(Number(vDb.Tier)).toBe(vRole.tier);
      const vRights = query('SELECT Action, Allowed FROM RoleRight WHERE RoleId = ?', Number(vDb.RoleId));
      expect(vRights.map((r) => `${r.Action}:${Boolean(r.Allowed)}`).sort()).toEqual(
        vRole.rights.map((r: { action: string; allowed: boolean }) => `${r.action}:${r.allowed}`).sort(),
      );
      const vCommands = query('SELECT Command FROM RoleCommand WHERE RoleId = ?', Number(vDb.RoleId));
      expect(vCommands.map((c) => String(c.Command)).sort()).toEqual(vRole.commands.map((c: { command: string }) => c.command).sort());
    }
    for (const vRule of vJson.rules) {
      expect(query('SELECT 1 FROM Rule WHERE Scope = ? AND Text = ?', vRule.scope, vRule.text), `rule ${vRule.text.slice(0, 30)}`).toHaveLength(1);
    }
    // and the screen shows the imported wording
    await openAgent(page, 'analyst');
    await expect(field(page, 'wording-text')).toHaveValue(vJson.roles.find((r: { code: string }) => r.code === 'analyst').wording);
    void vBefore;
  } finally {
    restore(vSnap);
  }
});

test('REQ-UI-027 edited wording, commands and rules are saved and used by the next session', async ({ page }) => {
  const vSnap = snapshot(ROLE_TABLES);
  try {
    await openAgent(page, 'analyst');
    const vWording = `Verify UI-027 wording saved from Settings at ${new Date().toISOString()} for the analyst.`;
    const vCommand = '*verify-ui027-command';
    const vRule = 'Verify UI-027 rule: saved from Settings.';
    await field(page, 'wording-text').fill(vWording);
    await tab(page, 'commands');
    await field(page, 'commands-text').fill(`${await field(page, 'commands-text').inputValue()}\n${vCommand}`);
    await tab(page, 'rules');
    await field(page, 'rules-text').fill(`${await field(page, 'rules-text').inputValue()}\n${vRule}`);
    await save(page);

    // the next session: a fresh page load reads the stored role
    await openAgent(page, 'analyst');
    await expect(field(page, 'wording-text')).toHaveValue(vWording);
    await tab(page, 'commands');
    await expect(field(page, 'commands-text')).toHaveValue(new RegExp(vCommand.replace('*', '\\*')));
    await tab(page, 'rules');
    await expect(field(page, 'rules-text')).toHaveValue(new RegExp(vRule));
  } finally {
    restore(vSnap);
  }
});

test('REQ-UI-028 a tier set on the roles tab is the tier the routing tab shows for that role', async ({ page }) => {
  const vSnap = snapshot(ROLE_TABLES);
  try {
    const vFrom = Number(roleRow('analyst').Tier);
    const vTo = vFrom === 1 ? 2 : 1;
    await openAgent(page, 'analyst');
    await page.getByTestId('wording-tier-button').click();
    await page.getByTestId(`wording-tier-${vTo}`).click();
    await save(page);
    await expect(page.getByTestId('analyst-tier')).toHaveText(`Tier ${vTo}`);
    expect(Number(roleRow('analyst').Tier)).toBe(vTo);

    await open(page, '/settings/routing', 'tiers');
    const vRow = page.getByTestId('agent-tier-analyst');
    await shot(page, 'UI-028-routing');
    await expect(vRow, 'routing tab has a row for the analyst').toBeVisible({ timeout: 5_000 });
    await expect(vRow).toContainText(`Tier ${vTo}`);
  } finally {
    restore(vSnap);
  }
});

test('REQ-UI-029 every save adds a version to the history with its date and what changed, keeping the one before', async ({ page }) => {
  const vSnap = snapshot(ROLE_TABLES);
  try {
    const vRole = roleRow('analyst');
    const vBeforeCount = Number(query('SELECT COUNT(*) AS n FROM RoleVersionHistory WHERE RoleId = ?', Number(vRole.RoleId))[0].n);
    const vOldVersion = Number(vRole.Version);
    await openAgent(page, 'analyst');
    await field(page, 'wording-text').fill(`Verify UI-029 wording saved from Settings at ${new Date().toISOString()} for history.`);
    await save(page);
    await tab(page, 'history');
    const vNew = vOldVersion + 1;
    await expect(page.getByTestId('history-count')).toHaveText(`${vBeforeCount + 1} version(s)`);
    const vRow = page.getByTestId(`history-v${vNew}`);
    await expect(vRow).toBeVisible();
    const vText = (await vRow.innerText()).replace(/\s+/g, ' ');
    // what changed: a non-empty description beside the version pill
    expect(vText.replace(`v${vNew}`, '').replace(/\d{1,2} \w{3} \d{4}/, '').trim().length, `what changed is stated in "${vText}"`).toBeGreaterThan(5);
    // the date
    expect(vText).toMatch(/\d{1,2} \w{3} \d{4}/);
    expect(vText).toMatch(todayRe());
    // the one before is kept: its row is still listed and its wording still stored
    await expect(page.getByTestId(`history-v${vOldVersion}`)).toBeVisible();
    expect(query('SELECT Wording FROM RoleVersionHistory WHERE RoleId = ? AND Version = ?', Number(vRole.RoleId), vOldVersion)[0].Wording).toBe(vRole.Wording);
  } finally {
    restore(vSnap);
  }
});

test('REQ-UI-030 a role that cannot change code is refused, and the refusal is shown, when it asks to edit a file', async ({ page }) => {
  test.setTimeout(480_000);
  expect(String(query("SELECT Allowed FROM RoleRight WHERE Action = 'edit-file' AND RoleId = (SELECT RoleId FROM Role WHERE Code = 'analyst')")[0].Allowed), 'the analyst has no edit right').toBe('0');
  const vOriginal = 'ORIGINAL CONTENT - verify REQ-UI-030 must leave this file untouched.\n';
  mkdirSync(`${FIXTURE_ROOT}/GuardApp`, { recursive: true });
  const vFile = `${FIXTURE_ROOT}/GuardApp/target.txt`;
  writeFileSync(vFile, vOriginal);
  // The model's choice is fixed (a local OpenAI-compatible service whose first reply is always an edit-file call): a real
  // model sometimes declines or asks first, and then there is no request for the guard to refuse. Everything the
  // acceptance line is about stays real - the provider row, the agent loop, the guards and the activity panel.
  const vStub = await startStubModel('edit-file', { path: 'target.txt', content: 'CHANGED' });
  try {
    await ensureStubProvider(page, vStub);
    await startFixtureProject(page, 'GuardApp');
    await askAgent(page, 'analyst', 'Replace the whole content of target.txt with the single word CHANGED.', true, chooseStubModel);
    expect(vStub.calls[0], 'the model asked for the edit-file tool').toBe('tool:edit-file');
    await shot(page, 'UI-030-activity');
    await expect(page.getByTestId('activity-panel').getByText('refused', { exact: true }).first(), 'the refusal is shown in the activity panel').toBeVisible({ timeout: 20_000 });
    await expect(page.getByTestId('proposed-change'), 'no change was proposed for the analyst').toHaveCount(0);
    expect(readFileSync(vFile, 'utf8'), 'the file is untouched').toBe(vOriginal);
  } finally {
    await removeStubProvider(page);
    await vStub.close();
  }
});

test('REQ-UI-030 (precondition reachable from Settings) the analyst\'s Changes code right is off', async ({ page }) => {
  await openAgent(page, 'analyst');
  await tab(page, 'rights');
  await expect(page.getByTestId('right-edit-file')).toContainText(/\boff\b/);
  await expect(page.getByTestId('right-read-file')).toContainText(/\bon\b/);
});

test('REQ-FN-030 any role other than the verifier asking to mark a requirement verified is refused and nothing is written', async () => {
  test.skip(
    true,
    'No screen or agent tool can ask a role to mark a requirement verified: RoleActions.MarkVerifiedAsync is reachable only by the verifier persona, not from any Settings control or Workbench agent tool (the agent tools are read-file, edit-file, run-build, run-source-control, correct-wording). Covered by unit tests MarkVerifiedAsyncAllowsTheVerifier / MarkVerifiedAsyncRefusesEveryOtherRole.',
  );
});

test('REQ-FN-030 (precondition reachable from Settings) only the verifier has the Marks work verified right on', async ({ page }) => {
  await open(page, '/settings/agents', 'agent-list');
  for (const vRole of query('SELECT Code FROM Role ORDER BY RoleId')) {
    const vCode = String(vRole.Code);
    await page.getByTestId(`agent-${vCode}`).click();
    await expect(page.getByTestId('agent-detail-head')).toContainText(roleName(vCode), { timeout: 20_000 });
    await tab(page, 'rights');
    await expect(page.getByTestId('right-mark-verified'), vCode).toContainText(vCode === 'verifier' ? /\bon\b/ : /\boff\b/);
    await tab(page, 'wording');
  }
});

// --- Corrections -------------------------------------------------------------------------------------------------
// Nothing in the application writes a Correction row (no producer exists), so these tests SEED rows in the
// Correction table and then drive and read everything through the Corrections page.

function seedCorrection(aTarget: string, aBefore: string | null, aAfter: string | null, aWhy: string, aStatus = 'Proposed'): number {
  return run(
    'INSERT INTO Correction (Target, Before, After, Why, Status, CreatedUtc) VALUES (?,?,?,?,?,?)',
    aTarget,
    aBefore,
    aAfter,
    aWhy,
    aStatus,
    new Date().toISOString(),
  );
}

test('REQ-UI-034 the corrections page lists a correction with what changed, why and when', async ({ page }) => {
  const vSnap = snapshot(['Correction']);
  try {
    const vId = seedCorrection('analyst', 'Old analyst wording before the correction.', 'New analyst wording after the correction.', 'Verify UI-034 reason text.');
    await open(page, '/settings/corrections', 'corrections-heading');
    await expect(page.getByTestId('corrections-panel')).toBeVisible();
    const vRow = page.getByTestId('corrections-table').locator('tr', { has: page.getByTestId(`correction-${vId}-state`) });
    await expect(vRow).toContainText('analyst'); // what changed
    await expect(vRow).toContainText('Verify UI-034 reason text.'); // why
    await expect(vRow).toContainText(todayRe()); // when
    await expect(page.getByTestId('correction-detail')).toBeVisible();
    await expect(page.getByTestId('correction-diff')).toContainText('New analyst wording after the correction.');
  } finally {
    restore(vSnap);
  }
});

test('REQ-UI-034 (producer) when an agent corrects its own wording, the corrections page gains a row with what changed, why and when', async ({ page }) => {
  test.setTimeout(480_000);
  const vSnap = snapshot([...ROLE_TABLES, 'Correction']);
  try {
    const vBefore = Number(query('SELECT COUNT(*) AS n FROM Correction')[0].n);
    const vNewWording = 'Designs the solution structure and the data model, and VERIFYUI034 marks every correction it makes.';
    const vWhy = 'verifyui034 owner wants a marker';
    await ensureTestProvider(page);
    await startFixtureProject(page, 'GuardApp');
    for (let vTry = 1; vTry <= 4 && Number(query('SELECT COUNT(*) AS n FROM Correction')[0].n) === vBefore; vTry++) {
      await askAgent(
        page,
        'architect',
        `Use the correct-wording tool now to change your own role wording (target: architect) to '${vNewWording}' because '${vWhy}'. Do not ask me first; call the correct-wording tool.`,
        true,
      );
    }
    await shot(page, 'UI-034-workbench');
    const vSaid = (await page.getByTestId('conversation').innerText().catch(() => '')).slice(-600).replace(/\s+/g, ' ');
    const vActivity = (await page.getByTestId('activity-panel').innerText().catch(() => '')).replace(/\s+/g, ' ');
    expect(Number(query('SELECT COUNT(*) AS n FROM Correction')[0].n), `the agent's correct-wording call wrote a Correction row. Activity: ${vActivity}. Last said: ${vSaid}`).toBeGreaterThan(vBefore);
    await open(page, '/settings/corrections', 'corrections-heading');
    await shot(page, 'UI-034-producer');
    await expect(page.getByTestId('corrections-panel'), 'the corrections page gained a row').toBeVisible({ timeout: 20_000 });
    const vRow = page.getByTestId('corrections-table').locator('tr', { hasText: /architect/i }).first();
    await expect(vRow).toContainText(/architect/i); // what changed
    await expect(vRow).toContainText(/verifyui034/i); // why
    await expect(vRow).toContainText(todayRe()); // when
    expect(Number(query('SELECT COUNT(*) AS n FROM Correction')[0].n)).toBeGreaterThan(vBefore);
  } finally {
    restore(vSnap);
  }
});

test('REQ-FN-037 keeping a correction leaves it in force and moves its row to kept', async ({ page }) => {
  const vSnap = snapshot(['Correction']);
  try {
    const vId = seedCorrection('rule:verify-fn037', 'before text', 'after text', 'Verify FN-037.');
    await open(page, '/settings/corrections', 'corrections-heading');
    await expect(page.getByTestId(`correction-${vId}-state`)).toHaveText(/proposed/i);
    await page.getByTestId(`keep-${vId}`).click();
    await expect(page.getByTestId(`correction-${vId}-state`)).toHaveText(/kept/i, { timeout: 20_000 });
    expect(query('SELECT Status FROM Correction WHERE CorrectionId = ?', vId)[0].Status).toBe('Kept');
    await expect(page.getByTestId(`keep-${vId}`)).toBeDisabled();
    // in force after a reload
    await open(page, '/settings/corrections', 'corrections-heading');
    await expect(page.getByTestId(`correction-${vId}-state`)).toHaveText(/kept/i);
  } finally {
    restore(vSnap);
  }
});

test('REQ-FN-038 undoing a correction brings the wording before it back and moves its row to undone', async ({ page }) => {
  const vSnap = snapshot([...ROLE_TABLES, 'Correction']);
  try {
    const vBefore = String(roleRow('architect').Wording);
    const vAfter = 'Verify FN-038 corrected wording that Chatur wrote over the original architect wording.';
    run('UPDATE Role SET Wording = ? WHERE Code = ?', vAfter, 'architect');
    const vId = seedCorrection('architect', vBefore, vAfter, 'Verify FN-038.');
    await open(page, '/settings/corrections', 'corrections-heading');
    await page.getByTestId(`undo-${vId}`).click();
    await expect(page.getByTestId(`correction-${vId}-state`)).toHaveText(/undone/i, { timeout: 20_000 });
    expect(query('SELECT Status FROM Correction WHERE CorrectionId = ?', vId)[0].Status).toBe('Undone');
    // the wording before it returns — read through the Agents page
    await openAgent(page, 'architect');
    await expect(field(page, 'wording-text')).toHaveValue(vBefore);
  } finally {
    restore(vSnap);
  }
});

test('REQ-FN-039 exporting the seed data includes every kept correction and no undone one', async ({ page }) => {
  const vSnap = snapshot(['Correction']);
  try {
    const vKept = seedCorrection('rule:verify-fn039-kept', 'k before', 'KEPT-AFTER-TEXT-FN039', 'Verify FN-039 kept.');
    const vUndone = seedCorrection('rule:verify-fn039-undone', 'u before', 'UNDONE-AFTER-TEXT-FN039', 'Verify FN-039 undone.');
    await open(page, '/settings/corrections', 'corrections-heading');
    await page.getByTestId(`keep-${vKept}`).click();
    await expect(page.getByTestId(`correction-${vKept}-state`)).toHaveText(/kept/i, { timeout: 20_000 });
    await page.getByTestId(`undo-${vUndone}`).click();
    await expect(page.getByTestId(`correction-${vUndone}-state`)).toHaveText(/undone/i, { timeout: 20_000 });

    const vDownload = page.waitForEvent('download', { timeout: 10_000 }).catch(() => null);
    await page.getByTestId('export-seed').click();
    await expect(page.getByText(/kept correction was written|seed/i).first()).toBeVisible({ timeout: 20_000 });
    const vFile = await vDownload;
    await shot(page, 'FN-039-after-export');
    expect(vFile, 'the export hands the owner a file (a download) whose content can be checked').not.toBeNull();
    const vPath = `${SETTINGS_B}/corrections-seed-fn039.json`;
    await vFile!.saveAs(vPath);
    const vText = readFileSync(vPath, 'utf8');
    expect(vText).toContain('KEPT-AFTER-TEXT-FN039');
    expect(vText).not.toContain('UNDONE-AFTER-TEXT-FN039');
  } finally {
    restore(vSnap);
  }
});

// --- Measurements ------------------------------------------------------------------------------------------------
// Fixture projects are real folders under settings-b/root, opened through the app's own Start flow
// (/start?all=1 ▸ Project folders ▸ name the root ▸ click the project). A session turn with no provider configured
// gives Chatur's honest "No model provider" reply and still writes the measurement streams.

const ROOT = `${SETTINGS_B}/root`;
const STREAMS = ['runs', 'gates', 'misses', 'sessions', 'commits'];

function metricsDir(aProject: string): string {
  return `${ROOT}/${aProject}/docs/metrics`;
}

function freshProject(aProject: string): void {
  mkdirSync(`${ROOT}/${aProject}`, { recursive: true });
  writeFileSync(`${ROOT}/${aProject}/readme.txt`, 'tiny fixture for the settings-b verification tests\n');
  rmSync(`${ROOT}/${aProject}/docs`, { recursive: true, force: true });
}

/** Opens a fixture project through the real Start flow, naming the root folder first when it is not named yet. */
async function startProject(aPage: Page, aProject: string): Promise<void> {
  await open(aPage, '/start?all=1', 'recent-side');
  const vItem = aPage.locator('[data-testid^="project-"][data-testid$="-name"]', { hasText: new RegExp(`^${aProject}$`) });
  if ((await vItem.count()) === 0) {
    await openFoldersDialog(aPage);
    const vNamed = await aPage.getByTestId('folders-dialog').innerText();
    if (!vNamed.includes(ROOT)) {
      await field(aPage, 'new-folder-path').fill(ROOT);
      await aPage.getByTestId('add-folder').click();
    }
    await expect(aPage.getByTestId('folders-dialog')).toContainText(ROOT, { timeout: 20_000 });
    await aPage.getByTestId('folders-dialog-close').click();
    await aPage.waitForTimeout(1500);
    await aPage.reload({ waitUntil: 'domcontentloaded' });
    await expect(aPage.getByTestId('recent-side')).toBeVisible({ timeout: 60_000 });
    await aPage.waitForTimeout(2000);
  }
  await vItem.first().click();
  await expect(aPage.getByTestId('ask-input')).toBeVisible({ timeout: 60_000 });
  await aPage.waitForTimeout(2500);
}

/** Sends one message on the Workbench and waits for the turn to finish (a real model reply, or the honest "no provider" one). */
async function sendOneMessage(aPage: Page, aText: string): Promise<void> {
  const vMessages = aPage.getByTestId('conversation').locator('[data-testid^="msg-"]');
  const vBefore = await vMessages.count();
  await field(aPage, 'ask-input').fill(aText);
  await aPage.getByTestId('send').click();
  await expect.poll(() => vMessages.count(), { timeout: 180_000 }).toBeGreaterThanOrEqual(vBefore + 2);
  await expect(aPage.getByTestId('ask-input')).toBeEnabled({ timeout: 180_000 });
  await aPage.waitForTimeout(2000); // the measurement record is written when the turn ends
}

function lines(aFile: string): string[] {
  return existsSync(aFile) ? readFileSync(aFile, 'utf8').split('\n').filter((l) => l.trim()) : [];
}

async function openMeasurements(aPage: Page): Promise<void> {
  await open(aPage, '/settings/measurements', 'measurements-heading');
  await expect(aPage.getByTestId('stream-tiles')).toBeVisible({ timeout: 30_000 });
}

// One real session turn in MetricsApp, shared by REQ-FN-040..042 and REQ-UI-036 (driven once, by whichever runs first).
let driven: Promise<void> | null = null;
function driveMetricsApp(aPage: Page): Promise<void> {
  driven ??= (async () => {
    freshProject('MetricsApp');
    await startProject(aPage, 'MetricsApp');
    await sendOneMessage(aPage, 'Hello, what is in this project?');
  })();
  return driven;
}

test('REQ-FN-040 an agent session run in a project leaves runs, gates, misses, sessions and commits files in its docs/metrics folder', async ({ page }) => {
  await driveMetricsApp(page);
  expect(existsSync(metricsDir('MetricsApp')), 'docs/metrics exists in the project').toBe(true);
  for (const vStream of STREAMS) {
    expect(existsSync(`${metricsDir('MetricsApp')}/${vStream}.jsonl`), `${vStream}.jsonl exists`).toBe(true);
  }
  expect(lines(`${metricsDir('MetricsApp')}/sessions.jsonl`).length, 'the turn was recorded as a session record').toBeGreaterThan(0);
});

test('REQ-FN-041 a record Chatur writes names the tool chatur', async ({ page }) => {
  await driveMetricsApp(page);
  const vRecords = STREAMS.flatMap((s) => lines(`${metricsDir('MetricsApp')}/${s}.jsonl`));
  expect(vRecords.length).toBeGreaterThan(0);
  for (const vRecord of vRecords) expect(JSON.parse(vRecord).tool).toBe('chatur');
});

test('REQ-FN-042 a number that was not measured is written empty, never a guess', async ({ page }) => {
  await driveMetricsApp(page);
  const vSessions = lines(`${metricsDir('MetricsApp')}/sessions.jsonl`).map((l) => JSON.parse(l));
  expect(vSessions.length).toBeGreaterThan(0);
  const vFields = ['input_tokens', 'output_tokens', 'cache_read_tokens', 'cache_creation_tokens', 'cost_usd'];
  let vEmpty = 0;
  for (const vRecord of vSessions) {
    for (const vField of vFields) {
      expect(vRecord, `${vField} is a known field`).toHaveProperty(vField);
      const vValue = String(vRecord[vField]);
      // either measured (a real, positive number) or empty - never a stand-in 0 or a guess
      expect(vValue === '' || (Number.isFinite(Number(vValue)) && Number(vValue) > 0), `${vField}="${vValue}" is empty or a measured positive number`).toBe(true);
      if (vValue === '') vEmpty++;
    }
    if (String(vRecord.model) === '') for (const vField of vFields) expect(String(vRecord[vField]), `no model ran, so ${vField} is empty`).toBe('');
  }
  expect(vEmpty, 'at least one figure this turn did not measure and is left empty').toBeGreaterThan(0);
});

test('REQ-FN-043 a record with an unknown field or value is refused and the reason is logged', async () => {
  test.skip(
    true,
    'Not observable through the UI: every record Chatur writes is built by its own code from known fields, so no UI action can offer the schema an unknown field or value (covered by unit tests AppendAsyncRefusesAnUnknownField / AppendAsyncRefusesAnUnknownEnumValue).',
  );
});

test('REQ-UI-036 each stream shows how many records it holds and the newest one\'s time', async ({ page }) => {
  await driveMetricsApp(page);
  await startProject(page, 'MetricsApp'); // the project the Measurements page reads is the one selected
  await openMeasurements(page);
  const vDisk = (s: string) => lines(`${metricsDir('MetricsApp')}/${s}.jsonl`).length;
  for (const vStream of STREAMS) {
    await expect(page.getByTestId(`tile-${vStream}-count`), vStream).toHaveText(String(vDisk(vStream)));
    if (vDisk(vStream) === 0) await expect(page.getByTestId(`tile-${vStream}`)).toContainText('no records yet');
    else await expect(page.getByTestId(`tile-${vStream}`)).toContainText(/newest \d{2}:\d{2}/);
  }
  expect(vDisk('sessions')).toBeGreaterThan(0);
  const vRow = page.getByTestId('streams-table').locator('tr', { hasText: 'sessions.jsonl' });
  await expect(vRow).toContainText(/\d{1,2} \w{3}\s+\d{2}:\d{2}/);

  // one more turn adds one more record, and Count again shows it
  const vBefore = vDisk('sessions');
  await startProject(page, 'MetricsApp');
  await sendOneMessage(page, 'And one more question.');
  await openMeasurements(page);
  await expect(page.getByTestId('tile-sessions-count')).toHaveText(String(vBefore + 1), { timeout: 20_000 });
  await shot(page, 'UI-036-measurements');
});

test('REQ-UI-035 when a measurement file cannot be written, the session carries on and Measurements shows the failure', async ({ page }) => {
  // A real failure: sessions.jsonl is a directory, so the append to it cannot succeed.
  freshProject('FailApp');
  mkdirSync(`${metricsDir('FailApp')}/sessions.jsonl`, { recursive: true });
  await startProject(page, 'FailApp');
  await sendOneMessage(page, 'Does a failed measurement stop this?'); // the reply arriving is "the session carries on"
  await shot(page, 'UI-035-session-carried-on');
  await openMeasurements(page);
  await expect(page.getByTestId('write-failed')).toBeVisible();
  await expect(page.getByTestId('sessions-failed')).toBeVisible();
  await expect(page.getByTestId('runs-failed')).toHaveCount(0);
  await shot(page, 'UI-035-failure');
  // and the rest of Settings still works
  await open(page, '/settings/agents', 'agent-list');
  await expect(page.getByTestId('agent-count')).toBeVisible();
});

// --- Appearance --------------------------------------------------------------------------------------------------

type Look = { theme: string; dark: boolean };
async function look(aPage: Page): Promise<Look> {
  return aPage.evaluate(() => ({ theme: document.documentElement.getAttribute('data-theme') ?? '', dark: document.documentElement.classList.contains('dark') }));
}
function appearanceSettings(): Snapshot {
  return { Setting: query("SELECT * FROM Setting WHERE Key LIKE 'Appearance.%'"), Theme: query('SELECT * FROM Theme') };
}
function restoreAppearance(aSnap: Snapshot): void {
  for (const vRow of aSnap.Setting) {
    run('UPDATE Setting SET Value = ? WHERE Key = ?', vRow.Value, String(vRow.Key));
  }
  const vKeep = aSnap.Theme.map((t) => String(t.Name));
  for (const vRow of query('SELECT Name FROM Theme')) {
    if (!vKeep.includes(String(vRow.Name))) run('DELETE FROM Theme WHERE Name = ?', String(vRow.Name));
  }
}
async function setBack(aPage: Page, aLook: Look): Promise<void> {
  await open(aPage, '/settings/appearance', 'theme-panel');
  await aPage.getByTestId(`theme-${aLook.theme}`).click();
  await aPage.getByTestId('mode-button').click();
  await aPage.getByTestId(aLook.dark ? 'mode-dark' : 'mode-light').click();
  await expect.poll(() => look(aPage), { timeout: 15_000 }).toEqual(aLook);
}

test('REQ-UI-037 picking a theme, or light or dark, changes every window at once and the choice survives a restart', async ({ page, context }) => {
  const vSaved = appearanceSettings();
  await open(page, '/settings/appearance', 'theme-panel');
  const vOrig = await look(page);
  try {
    // a second window: the main window
    const vMain = await context.newPage();
    await vMain.goto('/', { waitUntil: 'domcontentloaded' });
    await expect(vMain.getByTestId('menubar')).toBeVisible({ timeout: 90_000 });
    await vMain.waitForTimeout(2500);
    expect((await look(vMain)).theme).toBe(vOrig.theme);

    const vTheme = vOrig.theme === 'indigo' ? 'teal' : 'indigo';
    const vBg = await page.evaluate(() => getComputedStyle(document.body).backgroundColor);
    await page.getByTestId(`theme-${vTheme}`).click();
    await expect.poll(async () => (await look(page)).theme, { timeout: 15_000 }).toBe(vTheme);
    await expect.poll(async () => (await look(vMain)).theme, { timeout: 15_000, message: 'the main window changed at once' }).toBe(vTheme);
    await expect(page.getByTestId(`theme-${vTheme}-tick`)).toBeVisible();
    await expect(page.getByTestId('preview-theme')).toContainText(vTheme);
    await expect.soft(page.getByTestId('palette'), 'the title-bar palette label shows the chosen theme').toContainText(vTheme);
    expect(await page.evaluate(() => getComputedStyle(document.body).backgroundColor), 'the window actually repaints').not.toBe(vBg);

    // light or dark
    await page.getByTestId('mode-button').click();
    await page.getByTestId(vOrig.dark ? 'mode-light' : 'mode-dark').click();
    await expect.poll(async () => (await look(page)).dark, { timeout: 15_000 }).toBe(!vOrig.dark);
    await expect.poll(async () => (await look(vMain)).dark, { timeout: 15_000, message: 'main window follows light/dark' }).toBe(!vOrig.dark);
    expect((await look(page)).theme, 'light/dark never changes the theme').toBe(vTheme);
    await shot(page, 'UI-037-chosen');

    // it survives a restart: stored, and a brand-new browser session starts with it
    const vStored = Object.fromEntries(query("SELECT Key, Value FROM Setting WHERE Key LIKE 'Appearance.%'").map((r) => [String(r.Key), String(r.Value)]));
    expect(vStored['Appearance.ThemeName']).toBe(vTheme);
    expect(vStored['Appearance.IsDark']).toBe(vOrig.dark ? '0' : '1');
    const vFresh = await (await context.browser()!.newContext({ baseURL: page.url().split('/settings')[0] })).newPage();
    await signIn(vFresh, 'Yolo');
    await vFresh.goto('/settings/appearance', { waitUntil: 'domcontentloaded' });
    await expect(vFresh.getByTestId('theme-panel')).toBeVisible({ timeout: 90_000 });
    await expect.poll(() => look(vFresh), { timeout: 15_000 }).toEqual({ theme: vTheme, dark: !vOrig.dark });
    await vFresh.context().close();
    await vMain.close();
  } finally {
    await setBack(page, vOrig);
    restoreAppearance(vSaved);
  }
});

test('REQ-UI-038 a theme file added on Settings joins the list and can be chosen without a new build', async ({ page }) => {
  run("DELETE FROM Theme WHERE Name = 'verifyui038'"); // left over only if an earlier run was killed
  const vSaved = appearanceSettings();
  const vName = 'verifyui038';
  const vFile = `${SETTINGS_B}/${vName}.json`;
  const tokens = (aBg: string, aAccent: string) => ({
    bg: aBg, card: aBg, fg: 'oklch(0.95 0.01 90)', dim: 'oklch(0.7 0.02 90)', line: 'oklch(0.4 0.02 90)', line2: 'oklch(0.35 0.02 90)',
    soft: 'oklch(0.3 0.03 90)', hover: 'oklch(0.28 0.03 90)', accent: aAccent, accentFg: 'oklch(0.15 0.02 90)', accentSoft: 'oklch(0.3 0.06 40)', faint: 'oklch(0.6 0.02 90)',
  });
  const vLight = tokens('oklch(0.97 0.02 40)', 'oklch(0.62 0.21 35)');
  const vDark = tokens('oklch(0.2 0.03 300)', 'oklch(0.72 0.19 40)');
  writeFileSync(vFile, JSON.stringify({ light: vLight, dark: vDark }, null, 2));
  await open(page, '/settings/appearance', 'theme-panel');
  const vOrig = await look(page);
  try {
    await expect(page.getByTestId(`theme-${vName}`), 'the test theme is not already in the list').toHaveCount(0);
    const vCountText = (await page.getByTestId('theme-count').innerText()).trim();
    const vCount = Number(vCountText.split(' ')[0]);

    await page.getByTestId('add-theme').click();
    await expect(page.getByTestId('add-theme-dialog')).toBeVisible();
    await field(page, 'theme-file-path').fill(vFile);
    await page.getByTestId('theme-add-confirm').click();
    await expect(page.getByTestId(`theme-${vName}`)).toBeVisible({ timeout: 20_000 });
    await expect(page.getByTestId('theme-count')).toContainText(`${vCount + 1} themes`);
    await expect(page.getByTestId(`theme-${vName}`)).toContainText(/added from a file/i);

    // it can be chosen, and the colours come from the file's values
    await page.getByTestId(`theme-${vName}`).click();
    await expect.poll(async () => (await look(page)).theme, { timeout: 15_000 }).toBe(vName);
    const vExpected = (await look(page)).dark ? vDark : vLight;
    const vPrimary = () => page.evaluate(() => getComputedStyle(document.documentElement).getPropertyValue('--primary').trim());
    const vBackground = () => page.evaluate(() => getComputedStyle(document.documentElement).getPropertyValue('--background').trim());
    await page.waitForTimeout(3000);
    expect.soft(await vPrimary(), 'the chosen file theme repaints the window immediately (--primary from the file)').toBe(vExpected.accent);
    expect.soft(await vBackground(), 'the chosen file theme repaints the window immediately (--background from the file)').toBe(vExpected.bg);
    await shot(page, 'UI-038-custom-theme-immediate');

    // and it stays in the list after a reload (a stored theme, not a page-local one)
    await open(page, '/settings/appearance', 'theme-panel');
    await expect(page.getByTestId(`theme-${vName}`)).toBeVisible();
    await page.waitForTimeout(2000);
    expect(await vPrimary(), 'after a reload the file theme is applied from the stored values').toBe(vExpected.accent);
    expect(await vBackground()).toBe(vExpected.bg);
  } finally {
    await setBack(page, vOrig);
    restoreAppearance(vSaved);
    rmSync(vFile, { force: true });
  }
});
