// Acceptance tests for the agents' commands typed in the chat and carried out to their end (REQ-FN-315..322,
// REQ-UI-122, REQ-UI-123; BRD-265..BRD-274). Black-box, web harness, signed in as test user 2 ('Owner'), with the owner's
// test provider from tests/verify/test-settings.json (the key is read there and never printed): a real model, because
// the point is that the agent carries each command out. Every test starts from a real start state: an empty folder
// opened through Start's Open a folder, and, where the command needs code, a small project the test makes in it with
// `dotnet new`. The conversation is in Go ahead (`*yolo`) so the agent's writes land without an approval click. Each
// folder lives under tests/verify/bin and is deleted, with its project, when its test ends.
import { test, expect, Page } from '@playwright/test';
import { execFileSync } from 'child_process';
import { existsSync, mkdirSync, readFileSync, statSync, writeFileSync } from 'fs';
import { homedir } from 'os';
import { join } from 'path';
import { signIn } from './support/auth';
import { removeProjectsUnder, scratchDir } from './support/cleanup';
import { openByDialog } from './support/workbench-b';
import { ensureTestProvider, chooseTestModel } from './support/provider';

test.describe.configure({ timeout: 1_200_000 });

const STAMP = Date.now();
/** While one row is being worked on, the row id here runs only its test; empty runs all of them. */
const ONLY = '';
const row: typeof test = ((aTitle: string, aBody: never) => (ONLY && !aTitle.startsWith(ONLY) ? test.skip(aTitle, aBody) : test(aTitle, aBody))) as never;
const objRoots: string[] = [];

test.afterAll(() => {
  for (const vRoot of objRoots) removeProjectsUnder(vRoot);
});

/** Sends one message and waits for the whole reply; returns the reply's text. */
async function say(aPage: Page, aText: string): Promise<string> {
  const vInput = aPage.getByTestId('ask-input');
  await expect(vInput).toBeEnabled({ timeout: 60_000 });
  const vBefore = await aPage.getByTestId('msg-text').count();
  await vInput.fill(aText);
  await aPage.getByTestId('send').click();
  await expect(aPage.getByTestId('msg-text')).toHaveCount(vBefore + 1, { timeout: 60_000 });
  await expect(vInput).toBeEnabled({ timeout: 400_000 });
  await expect(aPage.getByTestId('stop-agent')).toHaveCount(0, { timeout: 400_000 });
  const vReply = await aPage.getByTestId('msg-text').last().innerText();
  // The reply's opening, in the test's own output: when a file is not written, this says what the agent did instead.
  console.log(`[reply to "${aText.slice(0, 40)}"] ${vReply.replace(/\s+/g, ' ').slice(0, 400)}`);
  return vReply;
}

/** Keeps answering until every file is on disk, at most a few turns: a real model may ask once more before writing. */
async function untilWritten(aPage: Page, aFiles: string[], aNudges: string[]): Promise<void> {
  const vDone = () => aFiles.every((f) => existsSync(f));
  for (const vNudge of aNudges) {
    if (vDone()) return;
    await say(aPage, vNudge);
  }

  for (const vFile of aFiles) expect(existsSync(vFile), `${vFile} was written`).toBe(true);
}

/** A project's code: the .NET template run in the folder, offline (no restore), by the test itself. */
function dotnetNew(aFolder: string, aTemplate: string, aName: string): void {
  execFileSync(join(homedir(), '.dotnet', 'dotnet'), ['new', aTemplate, '--no-restore', '-n', aName, '-o', aFolder, '--force'], {
    stdio: 'ignore', timeout: 120_000,
  });
}

interface Started { page: Page; root: string; app: string }

/** Signs in, makes a folder (with its code when asked), opens it, picks the agent, starts a new chat on the test model. */
async function begin(aPage: Page, aTag: string, aAgent: string, aSeed?: (aRoot: string, aApp: string) => void, aYolo = true): Promise<Started> {
  try {
    await signIn(aPage, 'Owner');
  } catch {
    await signIn(aPage, 'Owner');
  }

  await ensureTestProvider(aPage);
  const vRoot = scratchDir(`cmd-${aTag}-${STAMP}`);
  objRoots.push(vRoot);
  mkdirSync(vRoot, { recursive: true });
  const vName = vRoot.split('/').pop()!;
  const vApp = vName.replace(/[^A-Za-z0-9]/g, '');
  aSeed?.(vRoot, vApp);
  await openByDialog(aPage, 'open-folder', vRoot, vName);
  await aPage.getByTestId('agent-pick').click();
  await aPage.getByTestId(`agent-${aAgent}`).click();
  await expect(aPage.getByTestId('agent-greeting')).toBeVisible({ timeout: 30_000 });
  await expect(async () => {
    await aPage.getByTestId('new-chat').click();
    await aPage.waitForTimeout(1_500);
    await expect(aPage.getByTestId('msg-text')).toHaveCount(0, { timeout: 1_000 });
  }).toPass({ timeout: 45_000 });
  await chooseTestModel(aPage);
  if (aYolo) expect(await say(aPage, '*yolo')).toContain('Go ahead');
  return { page: aPage, root: vRoot, app: vApp };
}

/** A small checklist with open rows, as a project that has been through day 1 has. */
function writeChecklist(aRoot: string, aApp: string): void {
  mkdirSync(join(aRoot, 'docs'), { recursive: true });
  writeFileSync(
    join(aRoot, 'docs', `${aApp}-Checklist.md`),
    `# ${aApp} — Checklist\n\n## Requirements Status\n\n| ID | Requirement | Status | % | Remarks | Details |\n|----|-------------|--------|---|---------|---------|\n`
    + '| REQ-FN-001 | A task can be added | Not Started | 0% | | |\n| REQ-FN-002 | A task can be ticked off | Not Started | 0% | | |\n| REQ-UI-001 | The list is shown on the main screen | Not Started | 0% | | |\n\n'
    + '## Page: Main\n\n- **REQ-FN-001** — A task can be added. *BRD:* BRD-1\n  - *Acceptance:* When the person types a task and presses Enter, then it is in the list.\n'
    + '- **REQ-FN-002** — A task can be ticked off. *BRD:* BRD-2\n  - *Acceptance:* When the person ticks a task, then it shows as done.\n'
    + '- **REQ-UI-001** — The list is shown. *BRD:* BRD-3\n  - *Acceptance:* When the person opens the app, then the list is on the main screen.\n',
  );
}

/** A console project with a little real logic in it, for the commands that read existing code. */
function writeConsoleProject(aRoot: string, aApp: string): void {
  dotnetNew(aRoot, 'console', aApp);
  mkdirSync(join(aRoot, 'Tasks'), { recursive: true });
  writeFileSync(join(aRoot, 'Tasks', 'TaskItem.cs'), `namespace ${aApp}.Tasks;\n\n/// <summary>One to-do item.</summary>\npublic sealed record TaskItem(int Id, string Title, DateOnly? Due, bool Done);\n`);
  writeFileSync(
    join(aRoot, 'Tasks', 'TaskList.cs'),
    `namespace ${aApp}.Tasks;\n\n/// <summary>The person's list of tasks, in memory.</summary>\npublic sealed class TaskList\n{\n    private readonly List<TaskItem> objItems = [];\n\n    /// <summary>Adds a task.</summary>\n    public TaskItem Add(string aTitle, DateOnly? aDue = null)\n    {\n        var vItem = new TaskItem(objItems.Count + 1, aTitle, aDue, false);\n        objItems.Add(vItem);\n        return vItem;\n    }\n\n    /// <summary>Ticks a task off.</summary>\n    public void Complete(int aId) => objItems[objItems.FindIndex(i => i.Id == aId)] = objItems.First(i => i.Id == aId) with { Done = true };\n\n    /// <summary>Tasks due today or earlier that are not done.</summary>\n    public IEnumerable<TaskItem> Today(DateOnly aToday) => objItems.Where(i => !i.Done && i.Due <= aToday);\n}\n`,
  );
  writeFileSync(join(aRoot, 'Program.cs'), `using ${aApp}.Tasks;\n\nvar vList = new TaskList();\nvar vLine = Console.ReadLine();\nwhile (!string.IsNullOrWhiteSpace(vLine))\n{\n    vList.Add(vLine);\n    vLine = Console.ReadLine();\n}\n\nforeach (var vTask in vList.Today(DateOnly.FromDateTime(DateTime.Today)))\n{\n    Console.WriteLine(vTask.Title);\n}\n`);
}

/** A small web project with routed pages, for the guides that map or explain screens. */
function writeWebProject(aRoot: string, aApp: string): void {
  dotnetNew(aRoot, 'razor', aApp);
}

/** A markdown file's text, once it is there and not empty. */
function read(aFile: string, aMin = 400): string {
  expect(existsSync(aFile), `${aFile} exists`).toBe(true);
  expect(statSync(aFile).size, `${aFile} is more than a stub`).toBeGreaterThan(aMin);
  return readFileSync(aFile, 'utf8');
}

row('REQ-FN-315 *day1-brownfield in a project with code writes the BRD and the Architecture', async ({ page }) => {
  const { app, root } = await begin(page, 'brown', 'analyst', writeConsoleProject);
  const vBrd = join(root, 'docs', `${app}-BRD.md`);
  const vArch = join(root, 'docs', `${app}-Architecture.md`);
  const vReply = await say(page, `*day1-brownfield ${app}`);
  expect(vReply.length).toBeGreaterThan(20);
  await untilWritten(page, [vBrd, vArch], [
    'Use the code and any documents as they are, take the sensible reading for anything open (Small size, stack as in the project files) and write all the documents now.',
    `Write docs/${app}-Architecture.md and docs/${app}-BRD.md now.`,
    'Please write the files now, without more questions.',
  ]);
  expect(read(vBrd)).toMatch(/BRD-\d+/);
  expect(read(vArch)).toMatch(/TaskList|net\d|Console|stack/i);
  await expect(page.getByTestId('conversation')).toContainText('is written', { timeout: 60_000 });
});

row('REQ-FN-316 *doc-out writes the whole document drafted in the conversation to its file', async ({ page }) => {
  const { root } = await begin(page, 'docout', 'analyst');
  const vDraft = await say(page, 'Draft here in the chat, do not write any file yet, a short document called "Glossary" for a small to-do app for one person. It must have a heading "# Glossary" and a table of at least five terms (Task, Due date, Today view, Quick add, Done item) each with a one-line meaning. It belongs in docs/Glossary.md.');
  expect(vDraft).toContain('Glossary');
  const vFile = join(root, 'docs', 'Glossary.md');
  expect(existsSync(vFile), 'nothing is written before *doc-out').toBe(false);
  await say(page, '*doc-out');
  await untilWritten(page, [vFile], ['Write docs/Glossary.md now, the whole document as drafted above.', 'Please write the file now.']);
  const vText = read(vFile, 200);
  expect(vText).toContain('# Glossary');
  for (const vTerm of ['Task', 'Due date', 'Today view', 'Quick add', 'Done item']) expect(vText).toContain(vTerm);
});

row('REQ-FN-317 the Analyst *research-prompt writes docs/{App}-Research-Prompt.md', async ({ page }) => {
  const { app, root } = await begin(page, 'resa', 'analyst');
  const vFile = join(root, 'docs', `${app}-Research-Prompt.md`);
  await say(page, '*research-prompt note-taking apps for one person that work offline');
  await untilWritten(page, [vFile], [
    'Focus 4, competitors. It should inform whether to build it at all; constraints: one developer, no budget; known: Notion, Obsidian and Apple Notes exist. Write the prompt file now.',
    `Write docs/${app}-Research-Prompt.md now, no more questions.`,
    'Please write the file now.',
  ]);
  const vText = read(vFile);
  expect(vText).toMatch(/objective|question/i);
});

row('REQ-FN-318 *devguide writes docs/{App}-DevGuide.md', async ({ page }) => {
  const { app, root } = await begin(page, 'devg', 'flow-master', writeWebProject);
  const vFile = join(root, 'docs', `${app}-DevGuide.md`);
  await say(page, `*devguide ${app}`);
  await untilWritten(page, [vFile], [
    'Go on with the sensible reading and write the guide file now; mark anything you cannot confirm as unresolved.',
    `Write docs/${app}-DevGuide.md now.`,
    'Please write the file now.',
  ]);
  const vText = read(vFile);
  expect(vText).toMatch(/Index|Privacy/);
  expect(vText).toMatch(/Call chain|break/i);
});

row('REQ-FN-319 *productguide writes docs/{App}-ProductGuide.md', async ({ page }) => {
  const { app, root } = await begin(page, 'prodg', 'flow-master', writeWebProject);
  const vFile = join(root, 'docs', `${app}-ProductGuide.md`);
  await say(page, `*productguide ${app}`);
  await untilWritten(page, [vFile], [
    'Go on with the sensible reading and write the manual file now.',
    `Write docs/${app}-ProductGuide.md now.`,
    'Please write the file now.',
  ]);
  const vText = read(vFile);
  expect(vText).toMatch(/^#{1,3} .*(getting started|start)/im);
});

/** Stops the project's end-to-end run, whatever state it is in, so no model is left working after the test. */
async function stopRun(aPage: Page, aApp: string): Promise<void> {
  try {
    await say(aPage, `*develop-end-to-end ${aApp} --stop`);
  } catch {
    // the run was already over; nothing to stop
  }
}

row('REQ-UI-122 *develop-end-to-end typed in the chat starts the run, links to its page, and the link opens that run', async ({ page }) => {
  const { app } = await begin(page, 'e2e', 'flow-master', undefined, false);
  try {
    const vReply = await say(page, `*develop-end-to-end ${app} a small to-do app for one person: add a task, tick it off, see today's tasks`);
    expect(vReply).toMatch(/Started end-to-end run \d+/);
    const vLink = page.getByTestId('msg-text').last().locator('a', { hasText: /End-to-end run/ });
    await expect(vLink).toHaveCount(1);
    await vLink.click();
    await expect(page.getByTestId('auto-run')).toBeVisible({ timeout: 30_000 });
    await expect(page.getByTestId('running-now')).toBeVisible({ timeout: 30_000 });
    await expect(page.getByTestId('running-process')).not.toBeEmpty();
    // still in the same window, with the conversation beside it
    await expect(page.getByTestId('ask-input')).toBeVisible();
    await page.screenshot({ path: 'tests/.artifacts/verify/shots/REQ-UI-122-run.png' });
  } finally {
    await stopRun(page, app);
  }
});

row('REQ-FN-320 *develop-status says whether the run is going, its step, its open rows and its tokens', async ({ page }) => {
  const { app } = await begin(page, 'status', 'flow-master', undefined, false);
  try {
    const vNone = await say(page, `*develop-status ${app}`);
    expect(vNone).toMatch(/No end-to-end run/);
    await say(page, `*develop-end-to-end ${app} a small to-do app for one person: add a task, tick it off, see today's tasks`);
    const vGoing = await say(page, `*develop-status ${app}`);
    expect(vGoing).toMatch(/Going: Run \d+/);
    expect(vGoing).toMatch(/Now: .+ on .+ step \d+/s);
    expect(vGoing).toMatch(/Open requirements: \d+/);
    expect(vGoing).toMatch(/Tokens so far: [\d,.]+/);
  } finally {
    await stopRun(page, app);
  }
});

row('REQ-FN-321 *estimate-phase in a project with a checklist gives the phase estimate or says why there is none', async ({ page }) => {
  const { app } = await begin(page, 'est', 'flow-master', writeChecklist, false);
  const vReply = await say(page, `*estimate-phase ${app}`);
  expect(vReply).toMatch(/Estimate for phase|No estimate yet for phase/);
  if (/Estimate for phase/.test(vReply)) {
    expect(vReply).toMatch(/low \/ likely \/ high/);
    expect(vReply).toMatch(/\d[\d,]* \/ \d[\d,]* \/ \d[\d,]*/);
  }
});

row('REQ-FN-322 the Architect *research writes docs/{App}-Research-Prompt.md', async ({ page }) => {
  const { app, root } = await begin(page, 'resb', 'architect');
  const vFile = join(root, 'docs', `${app}-Research-Prompt.md`);
  await say(page, '*research which local database suits a desktop to-do app for one person');
  await untilWritten(page, [vFile], [
    'Focus 5, technology. It should inform the choice of data store; constraints: .NET, works offline, no server; known: SQLite and LiteDB exist. Write the prompt file now.',
    `Write docs/${app}-Research-Prompt.md now, no more questions.`,
    'Please write the file now.',
  ]);
  expect(read(vFile)).toMatch(/objective|question/i);
});

row('REQ-UI-123 a command that starts a process run says so and its link opens that run page', async ({ page }) => {
  const { app } = await begin(page, 'proc', 'flow-master', writeChecklist, false);
  const vReply = await say(page, `*refresh-status ${app}`);
  const vMatch = /process run (\d+)/.exec(vReply);
  expect(vMatch, 'the reply names the process run').not.toBeNull();
  expect(vReply).toMatch(/Started|Already running/);
  const vLink = page.getByTestId('msg-text').last().locator('a', { hasText: /Processes/ });
  await expect(vLink).toHaveCount(1);
  await vLink.click();
  await expect(page.getByTestId('run-heading')).toBeVisible({ timeout: 30_000 });
  await expect(page.getByTestId('run-process')).toHaveText('refresh-status');
  await expect(page.getByTestId('run-project')).toBeVisible();
  await expect(page.getByTestId('tabs')).toContainText(`refresh-status · run ${vMatch![1]}`);
  await page.screenshot({ path: 'tests/.artifacts/verify/shots/REQ-UI-123-run.png' });
});
