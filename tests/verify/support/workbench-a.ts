// Shared fixture + flows for the Workbench files / build acceptance specs (workbench-files.spec.ts,
// workbench-build.spec.ts). The fixture is private: two tiny .NET projects under
// tests/.artifacts/verify/fixtures/workbench-a/, opened through Start's own "Project folders" dialog.
import { test, expect, Page } from '@playwright/test';
import { mkdirSync, writeFileSync, existsSync } from 'fs';
import { join, resolve } from 'path';
import { scratchDir } from './cleanup';
import { execFileSync } from 'child_process';

export const ROOT = resolve(__dirname, '../../.artifacts/verify/fixtures/workbench-a');
export const NAME = 'WbAFilesApp';
export const MULTI = 'WbAMultiHead';
export const PROJECT = join(ROOT, NAME);
// The fixture program's process id, in this test project's own bin folder (owner, 20 Nov 2026: nothing outside the repository).
export const PID_FILE = join(scratchDir('workbench-a'), 'wba-run.pid');
// The booted app's own log follows the port of BASE_URL (5280 when none is set), as db.ts does for the database.
export const APP_LOG = resolve(__dirname, `../../.artifacts/verify/app-${new URL(process.env.BASE_URL ?? 'http://localhost:5280').port || '5280'}.log`);
export const SHOTS = resolve(__dirname, '../../.artifacts/verify/shots');

export const PROGRAM_OK = `Console.WriteLine("WBA starting");
File.WriteAllText(@"${PID_FILE}", Environment.ProcessId.ToString());
for (var i = 1; i <= 40; i++)
{
    Console.WriteLine($"WBA tick {i}");
    Thread.Sleep(1000);
}
Console.WriteLine("WBA done");
`;
export const PROGRAM_BROKEN = `Console.WriteLine("WBA starting")
Console.WriteLine("WBA second");
Console.WriteLine("WBA third")
`;
export const NOTES = 'alpha notes line\n';

/** A platform TFM this host cannot build, so the target-pop shows it as needing another machine. */
export function remoteTfm(): string {
  switch (process.platform) {
    case 'win32': return 'net10.0-ios';
    case 'darwin': return 'net10.0-windows10.0.19041.0';
    default: return 'net10.0-ios';
  }
}

/** Human-readable platform name shown in the target menu for the {@link remoteTfm} row. */
export function remoteLabel(): string {
  switch (process.platform) {
    case 'win32': return 'iOS';
    case 'darwin': return 'Windows';
    default: return 'iOS';
  }
}

export function write(aPath: string, aText: string) {
  mkdirSync(join(aPath, '..'), { recursive: true });
  writeFileSync(aPath, aText);
}

/** (Re)creates the private fixture files. Safe to call before every test. */
export function resetFixture(aProgram = PROGRAM_OK) {
  // The Chatur repo's Directory.Build.props/nuget.config sit above the fixture; shadow them inside each project.
  for (const vDir of [PROJECT, join(ROOT, MULTI)]) {
    write(join(vDir, 'Directory.Build.props'), '<Project />\n');
    write(join(vDir, 'nuget.config'), '<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear /></packageSources></configuration>\n');
  }
  write(join(PROJECT, `${NAME}.csproj`),
    '<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n    <OutputType>Exe</OutputType>\n    <TargetFramework>net10.0</TargetFramework>\n    <ImplicitUsings>enable</ImplicitUsings>\n    <Nullable>enable</Nullable>\n  </PropertyGroup>\n</Project>\n');
  write(join(PROJECT, 'Program.cs'), aProgram);
  write(join(PROJECT, 'Notes', 'notes.txt'), NOTES);
  // A second project that is only discovered, never built: a multi-head csproj with a head this machine cannot build.
  write(join(ROOT, MULTI, `${MULTI}.csproj`),
    `<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n    <OutputType>Exe</OutputType>\n    <TargetFrameworks>net10.0;${remoteTfm()}</TargetFrameworks>\n  </PropertyGroup>\n</Project>\n`);
  write(join(ROOT, MULTI, 'Multi.cs'), 'class Multi { }\n');
  write(join(ROOT, MULTI, 'Docs', 'readme.txt'), 'multi readme\n');
  ownRepository(PROJECT);
}

/**
 * Makes the fixture project a git repository of its own, on branch main with one empty check-in, unless it
 * already is one. Before 18 Nov it had none: inside Chatur's working copy git walked up and the top bar showed
 * Chatur's own branch, so REQ-FN-007 passed only by reading the repository the tests sit in, and failed in a
 * separate copy with no repository above it. Only the fixture's own repository is ever written (TF-037).
 */
function ownRepository(aPath: string): void {
  const vGit = (aArgs: string[]) => execFileSync('git', ['-C', aPath, ...aArgs], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] }).trim();
  let vTop = '';
  try {
    vTop = vGit(['rev-parse', '--show-toplevel']);
  } catch {
    vTop = '';
  }
  if (vTop && resolve(vTop) === resolve(aPath)) return;
  vGit(['init', '-q', '-b', 'main']);
  vGit(['-c', 'user.name=Chatur verify', '-c', 'user.email=verify@example.test', 'commit', '-q', '--allow-empty', '-m', 'fixture']);
}

export const projectName = (aPage: Page) => aPage.getByTestId('project-switch-name');
export const tree = (aPage: Page, aRel: string) => aPage.getByTestId(`tree-${aRel}`);
export const tabs = (aPage: Page) => aPage.getByTestId('tabs');
export const outputText = (aPage: Page) => aPage.getByTestId('output');
export const pill = (aPage: Page) => aPage.getByTestId('output-status');
export const editorText = (aPage: Page) => aPage.getByTestId('editor').locator('textarea').first();

/**
 * Opens `aName` on the Workbench through the real flow: Start's "all projects" view
 * (`/start?all=1`), the Project folders dialog (naming the fixture root once), then the project's row.
 */
export async function openProject(aPage: Page, aName = NAME) {
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
  await expect(projectName(aPage)).toHaveText(aName, { timeout: 30_000 });
}

export async function openSwitcher(aPage: Page) {
  await expect(async () => {
    if (!(await aPage.getByTestId('project-switch-pop').isVisible())) await aPage.getByTestId('project-switch').click();
    await expect(aPage.getByTestId('project-switch-pop')).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
}

export async function switchTo(aPage: Page, aName: string) {
  await openSwitcher(aPage);
  await aPage.getByTestId('project-switch-pop').getByText(aName, { exact: true }).click();
  await expect(projectName(aPage)).toHaveText(aName, { timeout: 20_000 });
}

export async function openTargetMenu(aPage: Page) {
  await expect(async () => {
    if (!(await aPage.getByTestId('target-pop').isVisible())) await aPage.getByTestId('target').click();
    await expect(aPage.getByTestId('target-pop')).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
}

/** Makes a folder row open (or closed); clicks made before the circuit is interactive are retried. */
export async function setFolder(aPage: Page, aRel: string, aOpen: boolean) {
  const vRow = tree(aPage, aRel);
  await expect(vRow).toBeVisible({ timeout: 20_000 });
  await expect(async () => {
    if ((await vRow.getAttribute('data-state')) !== (aOpen ? 'open' : 'closed')) await vRow.locator('[data-slot=tree-item-row]').first().click();
    await expect(vRow).toHaveAttribute('data-state', aOpen ? 'open' : 'closed', { timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
}

/** Opens a file from the tree into a tab (its folders are opened first). */
export async function openFile(aPage: Page, aRel: string) {
  const vParts = aRel.split('/');
  for (let vI = 1; vI < vParts.length; vI++) await setFolder(aPage, vParts.slice(0, vI).join('/'), true);
  const vRow = tree(aPage, aRel);
  const vName = vParts[vParts.length - 1];
  await expect(vRow).toBeVisible({ timeout: 20_000 });
  await expect(async () => {
    if (!(await tabs(aPage).getByText(vName, { exact: true }).isVisible())) await vRow.locator('[data-slot=tree-item-row]').first().click();
    await expect(tabs(aPage).getByText(vName, { exact: true })).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
}

export async function shot(aPage: Page, aName: string) {
  mkdirSync(SHOTS, { recursive: true });
  await aPage.screenshot({ path: join(SHOTS, `workbench-a-${aName}.png`), fullPage: true });
}

export async function stopIfRunning(aPage: Page) {
  try {
    if (await aPage.getByTestId('stop').isEnabled({ timeout: 1_000 })) await aPage.getByTestId('stop').click();
  } catch { /* page already gone */ }
}

export { test, existsSync };
