// Shared flows for the process-step-work specs: a private fixture project per process under
// tests/.artifacts/verify/fixtures/step-work/, opened through Start's own "Project folders" dialog, a process started
// from Processes, and a run driven to its end (the owner stops answered yes) or to the check that stops it.
// Fixture documents are the shapes the TechieFlow commands write, so the checks read what they would read in a real project.
import { Locator, Page, expect } from '@playwright/test';
import { existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'fs';
import { dirname, join, resolve } from 'path';
import { run, query } from './db';
import { openProcessesTab, startProcess } from './phase2-tabs';
import { projectName } from './workbench-a';

export const STEP_ROOT = resolve(__dirname, '../../.artifacts/verify/fixtures/step-work');

/** One private fixture project: a folder the test fills with the files a project of that process would hold. */
export class Fixture {
  readonly dir: string;
  constructor(readonly name: string) {
    this.dir = join(STEP_ROOT, name);
  }
  /** Empties the folder and gives it the shadows of the repo's own build props and package sources. */
  reset(): this {
    rmSync(this.dir, { recursive: true, force: true });
    mkdirSync(this.dir, { recursive: true });
    return this;
  }
  write(aRel: string, aText: string): this {
    const vFull = join(this.dir, aRel);
    mkdirSync(dirname(vFull), { recursive: true });
    writeFileSync(vFull, aText);
    return this;
  }
  read(aRel: string): string {
    return existsSync(join(this.dir, aRel)) ? readFileSync(join(this.dir, aRel), 'utf8') : '';
  }
  exists(aRel: string): boolean {
    return existsSync(join(this.dir, aRel));
  }
  /** A real, tiny .NET test project (xunit from the machine's own package cache, so it builds without a network). */
  dotnet(aName: string): this {
    this.write('Directory.Build.props', '<Project />\n');
    const vCache = process.env.NUGET_PACKAGES ?? join(process.env.HOME ?? '', '.nuget', 'packages');
    this.write(
      'nuget.config',
      `<?xml version="1.0" encoding="utf-8"?>\n<configuration>\n  <packageSources>\n    <clear />\n    <add key="cache" value="${vCache}" />\n  </packageSources>\n</configuration>\n`,
    );
    this.write(
      `${aName}.csproj`,
      '<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n    <ImplicitUsings>enable</ImplicitUsings>\n    <Nullable>enable</Nullable>\n    <IsPackable>false</IsPackable>\n    <NuGetAudit>false</NuGetAudit>\n  </PropertyGroup>\n  <ItemGroup>\n    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />\n    <PackageReference Include="xunit" Version="2.*" />\n    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />\n  </ItemGroup>\n</Project>\n',
    );
    return this;
  }
}

// ---- documents a project holds -------------------------------------------------------------------------------------

export type Row = { id: string; title: string; status: string; brd: number; acceptance: string; mockup?: string };

/** A requirement checklist in the shape the framework writes: the status table, then one detail block per row. */
export function checklist(aTitle: string, aRows: Row[]): string {
  const vTable = aRows.map((r) => `| ${r.id} | ${r.title} | ${r.status} | ${r.status === 'Verified' || r.status === 'Implemented' ? '100%' : '0%'} | | |`).join('\n');
  const vDetails = aRows
    .map((r) => `- **${r.id}** — ${r.title}. *BRD:* BRD-${r.brd}${r.mockup ? ` · *Mockup:* ${r.mockup}` : ''}\n  - *Acceptance:* ${r.acceptance}`)
    .join('\n');
  return `# ${aTitle} — Checklist\n\n## Requirements Status\n\n| ID | Requirement | Status | % | Remarks | Details |\n|----|-------------|--------|---|---------|---------|\n${vTable}\n\n## Page: Main (\`/main\`)\n\n${vDetails}\n`;
}

/** A BRD of items, each with an acceptance line. */
export function brd(aTitle: string, aNumbers: number[]): string {
  const vItems = aNumbers
    .map((n) => `- **BRD-${n}** — Requirement ${n}. *Screen:* Main\n  - *Acceptance:* When the owner does ${n}, then ${n} happens.`)
    .join('\n');
  return `# ${aTitle} — BRD\n\n${vItems}\n`;
}

// ---- the owner's session -------------------------------------------------------------------------------------------

/**
 * A run started from Processes keeps the owner's session mode on the project (BRD "Step work"). The UI can set that mode
 * only in a session on the conversation; this writes the row such a session leaves - the owner on Go ahead - so the
 * agent's writes land on disk instead of waiting for an approval no one is there to give.
 */
export function seedGoAheadSession(aProject: string): number {
  const vProject = Number(query('SELECT ProjectId FROM Project WHERE Name = ? AND RemovedUtc IS NULL', aProject)[0].ProjectId);
  const vRole = Number(query("SELECT RoleId FROM Role WHERE Name='Flow master' ORDER BY Version DESC LIMIT 1")[0].RoleId);
  return run('INSERT INTO Session (ProjectId, RoleId, Mode, State, StartedUtc) VALUES (?,?,?,?,?)', vProject, vRole, 'GoAhead', 'Active', new Date().toISOString());
}

export function dropSession(aSessionId: number): void {
  if (aSessionId > 0) run('DELETE FROM Session WHERE SessionId=?', aSessionId);
}

// ---- flows ---------------------------------------------------------------------------------------------------------

/** Opens `aName` (a folder under STEP_ROOT) on the Workbench through Start's real flow, naming STEP_ROOT once. */
export async function openStepProject(aPage: Page, aName: string): Promise<void> {
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
    if ((await vDialog.getByText(STEP_ROOT, { exact: true }).count()) === 0) {
      await aPage.getByTestId('new-folder-path').fill(STEP_ROOT);
      await aPage.getByTestId('add-folder').click();
    }
    await expect(vDialog.getByText(STEP_ROOT, { exact: true })).toBeVisible({ timeout: 20_000 });
    await aPage.getByTestId('folders-dialog-close').click();
    await expect(vDialog).toBeHidden();
    await expect(vList.getByText(aName, { exact: true })).toBeVisible({ timeout: 20_000 });
  }
  await expect(async () => {
    if (/\/start/.test(aPage.url())) await vList.getByText(aName, { exact: true }).click();
    await expect(aPage).not.toHaveURL(/\/start/, { timeout: 3_000 });
  }).toPass({ timeout: 30_000 });
  await expect(projectName(aPage)).toHaveText(aName, { timeout: 30_000 });
}

let objSeeded = 0;

/** Removes the owner's seeded session of the last startOn, so the next test starts from its own. */
export function cleanUp(): void {
  dropSession(objSeeded);
  objSeeded = 0;
}

/** Opens the project, puts the owner on Go ahead, starts the process from Processes, and returns the run's id. */
export async function startOn(aPage: Page, aProject: string, aProcess: string): Promise<number> {
  await openStepProject(aPage, aProject);
  objSeeded = seedGoAheadSession(aProject);
  await openProcessesTab(aPage);
  const vId = await startProcess(aPage, aProcess);
  await expect(aPage.getByTestId('run-heading')).toBeVisible({ timeout: 30_000 });
  return vId;
}

/**
 * Drives the run until it ends: every owner stop it comes to is answered yes (once). Returns how it ended - `finished`
 * when it went through every step, `stopped` when a check stopped it.
 */
export async function driveRun(aPage: Page, aTimeoutMs = 360_000): Promise<'finished' | 'stopped'> {
  const vEnd = Date.now() + aTimeoutMs;
  let vAnswered = '';
  while (Date.now() < vEnd) {
    const vState = ((await aPage.getByTestId('run-state').innerText().catch(() => '')) || '').toLowerCase();
    if (vState.includes('finished')) return 'finished';
    if (vState.includes('stopped')) return 'stopped';
    if (vState.includes('waiting')) {
      const vStep = (await aPage.getByTestId('question-step').innerText().catch(() => '')).trim();
      const vYes = aPage.getByTestId('answer-yes');
      if (vStep !== vAnswered && (await vYes.isEnabled().catch(() => false))) {
        vAnswered = vStep;
        await vYes.click();
      }
    }
    await aPage.waitForTimeout(1_000);
  }
  throw new Error('The run neither finished nor stopped within ' + aTimeoutMs + ' ms.');
}

/** The run's own account of why it stopped, as the page shows it. */
export async function failedWhy(aPage: Page): Promise<string> {
  await expect(aPage.getByTestId('failed-check')).toBeVisible({ timeout: 30_000 });
  return (await aPage.getByTestId('failed-check-why').innerText()).trim();
}

/** What a step reads on the run's step list. */
export const stepState = (aPage: Page, aPosition: number): Locator => aPage.getByTestId(`run-step-${aPosition}-state`);
export const stepResult = (aPage: Page, aPosition: number): Locator => aPage.getByTestId(`run-step-${aPosition}-result`);

/** The hand-over row of one step, read from Chatur's database (what the step recorded, not what the page says of it). */
export function work(aRunId: number, aPosition: number): Record<string, string | number | null> | undefined {
  return query('SELECT w.SessionId AS SessionId, w.AgentName AS Agent, w.ModelName AS Model, w.State AS State, w.FinalMessage AS Final FROM ProcessRunStepWork w WHERE w.RunId = ? AND w.Position = ?', aRunId, aPosition)[0];
}

/** The tool refusals recorded on the session of one step. */
export function refusals(aRunId: number, aPosition: number): string[] {
  return query(
    "SELECT e.Payload AS Payload FROM SessionEvent e JOIN ProcessRunStepWork w ON w.SessionId = e.SessionId WHERE w.RunId = ? AND w.Position = ? AND e.Kind = 'refusal' ORDER BY e.Seq",
    aRunId,
    aPosition,
  ).map((r) => String(r.Payload));
}

/** The files a step's agent wrote (changes applied), from the session's own change rows. */
export function filesWritten(aRunId: number, aPosition: number): string[] {
  return query(
    "SELECT c.FilePath AS FilePath FROM Change c JOIN ProcessRunStepWork w ON w.SessionId = c.SessionId WHERE w.RunId = ? AND w.Position = ? AND c.Status = 'Approved' ORDER BY c.ChangeId",
    aRunId,
    aPosition,
  ).map((r) => String(r.FilePath));
}

export const today = () => new Date().toISOString().slice(0, 10);
