// Shared fixtures + flows for the Workbench "night of 18 Nov" acceptance specs (workbench-files-b.spec.ts,
// workbench-run-b.spec.ts): an unreadable folder, a git repository with a remote, a folder with hidden files,
// and a small solution. Every fixture is made fresh for the test that uses it and is opened through the real
// dialog (Start's Open a folder / Open a solution). Git is run only inside a fixture made here (its own `git init`).
import { Page, expect } from '@playwright/test';
import { execFileSync } from 'child_process';
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'fs';
import { join, resolve } from 'path';
import { lockFolder, removeProjectsUnder, scratchDir, unlockFolder } from './cleanup';

export const FIXTURES_B = resolve(__dirname, '../../.artifacts/verify/fixtures/wb-b');

/** A new empty folder under the private fixtures folder, named for the test. */
export function freshDir(aLabel: string): string {
  mkdirSync(FIXTURES_B, { recursive: true });
  return mkdtempSync(join(FIXTURES_B, `${aLabel}-`));
}

/** Writes a file, making its folders. */
export function put(aPath: string, aText: string): void {
  mkdirSync(join(aPath, '..'), { recursive: true });
  writeFileSync(aPath, aText);
}

/** Runs git in a fixture repository (the fixture's own; never anywhere else) and returns its output. */
export function git(aRepo: string, ...aArgs: string[]): string {
  return execFileSync('git', ['-C', aRepo, '-c', 'user.name=Chatur verify', '-c', 'user.email=verify@example.test', ...aArgs], {
    encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'],
  }).trim();
}

/**
 * A folder holding one subfolder the app may not read. Made in this test project's own bin folder and locked
 * with a Windows deny rule, since chmod changes nothing on the Windows drive (owner, 20 Nov 2026: no file
 * outside the repository). Returns the folder and a cleanup that also removes any project made from it.
 */
export function makeUnreadableFixture(): { root: string; locked: string; cleanup: () => void } {
  const vRoot = scratchDir(`unreadable-${Date.now()}`);
  put(join(vRoot, 'top.txt'), 'top\n');
  put(join(vRoot, 'Readable', 'inner.txt'), 'inner\n');
  put(join(vRoot, 'Zeta', 'z.txt'), 'z\n');
  const vLocked = join(vRoot, 'Locked');
  put(join(vLocked, 'secret.txt'), 'secret\n');
  lockFolder(vLocked);
  return {
    root: vRoot,
    locked: vLocked,
    cleanup: () => {
      unlockFolder(vLocked);
      removeProjectsUnder(vRoot);
    },
  };
}

/**
 * A work repository on branch main with one commit, a bare remote called origin that holds main and
 * `remote-only` (no local branch of that name), and a second local branch `local-two`.
 */
export function makeBranchFixture(): { top: string; work: string; remote: string; cleanup: () => void } {
  const vTop = freshDir('branch');
  const vRemote = join(vTop, 'remote.git');
  const vWork = join(vTop, 'work');
  mkdirSync(vRemote);
  mkdirSync(vWork);
  git(vRemote, 'init', '-q', '--bare', '-b', 'main');
  git(vWork, 'init', '-q', '-b', 'main');
  git(vWork, 'config', 'user.name', 'Chatur verify');
  git(vWork, 'config', 'user.email', 'verify@example.test');
  put(join(vWork, 'readme.txt'), 'first\n');
  put(join(vWork, 'tracked.txt'), 'tracked\n');
  git(vWork, 'add', '.');
  git(vWork, 'commit', '-q', '-m', 'first');
  git(vWork, 'remote', 'add', 'origin', vRemote);
  git(vWork, 'push', '-q', 'origin', 'main');
  git(vWork, 'branch', 'remote-only');
  git(vWork, 'push', '-q', 'origin', 'remote-only');
  git(vWork, 'branch', '-q', '-D', 'remote-only');
  git(vWork, 'branch', 'local-two');
  git(vWork, 'fetch', '-q', 'origin');
  return { top: vTop, work: vWork, remote: vRemote, cleanup: () => { removeProjectsUnder(vTop); } };
}

/** A folder with visible and hidden entries: dot-named file and folder, and .vs. */
export function makeHiddenFixture(): { root: string; cleanup: () => void } {
  const vRoot = freshDir('hidden');
  put(join(vRoot, 'visible.txt'), 'visible\n');
  put(join(vRoot, '.env'), 'SECRET=1\n');
  put(join(vRoot, '.dotdir', 'in.txt'), 'in a dot folder\n');
  put(join(vRoot, '.vs', 'state.txt'), 'vs state\n');
  put(join(vRoot, 'Docs', 'readme.txt'), 'docs\n');
  return { root: vRoot, cleanup: () => { removeProjectsUnder(vRoot); } };
}

export const SLN_NAME = 'NightSln';
export const APP_ONE = 'NightAppOne';
export const APP_TWO = 'NightAppTwo';
export const LIB = 'NightLib';
export const TESTS = 'NightTests';
export const LINE_ONE = 'NIGHT-ONE says hello';
export const LINE_TWO = 'NIGHT-TWO says hello';

const exe = (aLine: string) => ({
  csproj: '<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n    <OutputType>Exe</OutputType>\n    <TargetFramework>net10.0</TargetFramework>\n    <ImplicitUsings>enable</ImplicitUsings>\n    <Nullable>enable</Nullable>\n  </PropertyGroup>\n</Project>\n',
  program: `Console.WriteLine("${aLine}");\n`,
});

/**
 * A solution with two console apps (each prints one line and ends), a class library and a test project.
 * Built offline: Directory.Build.props and nuget.config shadow the Chatur repository's own, and the test
 * project is marked IsTestProject without a package.
 */
export function makeSolutionFixture(): { dir: string; sln: string; cleanup: () => void } {
  const vDir = freshDir('sln');
  put(join(vDir, 'Directory.Build.props'), '<Project />\n');
  put(join(vDir, 'nuget.config'), '<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear /></packageSources></configuration>\n');
  const vOne = exe(LINE_ONE);
  const vTwo = exe(LINE_TWO);
  put(join(vDir, APP_ONE, `${APP_ONE}.csproj`), vOne.csproj);
  put(join(vDir, APP_ONE, 'Program.cs'), vOne.program);
  put(join(vDir, APP_TWO, `${APP_TWO}.csproj`), vTwo.csproj);
  put(join(vDir, APP_TWO, 'Program.cs'), vTwo.program);
  put(join(vDir, LIB, `${LIB}.csproj`), '<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n  </PropertyGroup>\n</Project>\n');
  put(join(vDir, LIB, 'Greeter.cs'), 'namespace NightLib; public static class Greeter { public static string Hello() => "hello"; }\n');
  put(join(vDir, TESTS, `${TESTS}.csproj`), '<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n    <IsTestProject>true</IsTestProject>\n  </PropertyGroup>\n</Project>\n');
  put(join(vDir, TESTS, 'Tests.cs'), 'namespace NightTests; public class Tests { }\n');
  const vProject = (aName: string, aGuid: string) =>
    `Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "${aName}", "${aName}\\${aName}.csproj", "{${aGuid}}"\nEndProject\n`;
  const vSln = '\nMicrosoft Visual Studio Solution File, Format Version 12.00\n# Visual Studio Version 17\n'
    + vProject(APP_ONE, '11111111-1111-1111-1111-111111111111')
    + vProject(APP_TWO, '22222222-2222-2222-2222-222222222222')
    + vProject(LIB, '33333333-3333-3333-3333-333333333333')
    + vProject(TESTS, '44444444-4444-4444-4444-444444444444')
    + 'Global\nEndGlobal\n';
  const vSlnPath = join(vDir, `${SLN_NAME}.sln`);
  put(vSlnPath, vSln);
  return { dir: vDir, sln: vSlnPath, cleanup: () => { removeProjectsUnder(vDir); } };
}

/**
 * Opens a folder or a solution on the Workbench through Start's own buttons and its dialog, typing the path.
 * Works whatever is open now (Start is always reachable).
 */
export async function openByDialog(aPage: Page, aButton: 'open-folder' | 'open-solution', aPath: string, aName: string): Promise<void> {
  await aPage.goto('/start?all=1', { waitUntil: 'domcontentloaded' });
  await expect(aPage.getByTestId('manage-folders')).toBeVisible({ timeout: 30_000 });
  const vDialog = aPage.getByTestId('open-project-dialog');
  await expect(async () => {
    if (!(await vDialog.isVisible())) await aPage.getByTestId(aButton).click();
    await expect(vDialog).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
  await aPage.getByTestId('open-project-path').fill(aPath);
  await aPage.getByTestId('open-project-go').click();
  await expect(aPage.getByTestId('project-switch-name')).toHaveText(aName, { timeout: 30_000 });
  await expect(aPage.getByTestId('file-tree')).toBeVisible({ timeout: 20_000 });
}

/** Opens one of the menu bar's menus (a click made before the circuit is interactive is retried). */
export async function openMenu(aPage: Page, aMenu: string): Promise<void> {
  const vItems = aPage.getByTestId(`menu-${aMenu}-items`);
  await expect(async () => {
    if (!(await vItems.isVisible())) await aPage.getByTestId(`menu-${aMenu}`).click();
    await expect(vItems).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
}

/** Opens a menu and clicks one of its items. */
export async function pick(aPage: Page, aMenu: string, aItem: string): Promise<void> {
  await openMenu(aPage, aMenu);
  await aPage.getByTestId(`menu-${aMenu}-${aItem}`).click();
}

/** Makes a Files folder row open (or closed); clicks made before the circuit is interactive are retried. */
export async function setOpen(aPage: Page, aRel: string, aOpen: boolean): Promise<void> {
  const vRow = aPage.getByTestId(`tree-${aRel}`);
  await expect(vRow).toBeVisible({ timeout: 20_000 });
  await expect(async () => {
    if ((await vRow.getAttribute('data-state')) !== (aOpen ? 'open' : 'closed')) await vRow.locator('[data-slot=tree-item-row]').first().click();
    await expect(vRow).toHaveAttribute('data-state', aOpen ? 'open' : 'closed', { timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
}

/** The branch Files names at its foot. */
export const branchName = (aPage: Page) => aPage.getByTestId('branch-name');

/** Opens the branch list at the foot of Files (a click before the circuit is interactive is retried). */
export async function openBranchList(aPage: Page): Promise<void> {
  const vPop = aPage.getByTestId('branch-pop');
  await expect(aPage.getByTestId('branch')).toBeEnabled({ timeout: 20_000 });
  await expect(async () => {
    if (!(await vPop.isVisible())) await aPage.getByTestId('branch').click();
    await expect(vPop).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
}

/** Opens the target picker in the toolbar. */
export async function openTargets(aPage: Page): Promise<void> {
  const vPop = aPage.getByTestId('target-pop');
  await expect(aPage.getByTestId('target').locator('button')).toBeEnabled({ timeout: 30_000 });
  await expect(async () => {
    if (!(await vPop.isVisible())) await aPage.getByTestId('target').locator('button').click();
    await expect(vPop).toBeVisible({ timeout: 2_500 });
  }).toPass({ timeout: 30_000 });
}
