// Acceptance test: REQ-UI-048 (BRD-105) — a finished verify-phase run writes a verdict into the
// project's own checklist table for each requirement waiting for one, and writes Verified ONLY when a
// test carrying the requirement's id passed (owner decision 1 = option A, 2026-10-07). Black-box, web
// harness, against a private fixture project that is a real .NET test project: Chatur builds it and runs
// its tests for real, so one run proves both outcomes (a passing id, a failing id, an id no test carries).
import { Page, expect } from '@playwright/test';
import { existsSync, readFileSync } from 'fs';
import { homedir } from 'os';
import { join } from 'path';
import { signIn } from './support/auth';
import { openProcessesTab, startProcess } from './support/phase2-tabs';
import { test, openProject, write, ROOT, SHOTS } from './support/workbench-a';

const NAME = 'WbAVerdictApp';
const PROJECT = join(ROOT, NAME);
const CHECKLIST = join(PROJECT, 'docs', 'Fixture-P2-Checklist.md');
const BEFORE =
  '# Fixture P2 checklist\n\n## Requirements Status\n\n| Req ID | Title | Status | % | Remarks | Detail |\n|---|---|---|---|---|---|\n' +
  '| REQ-FN-001 | First | Implemented | 75% | [REQ-FN-001] built. | [view](#d-1) |\n' +
  '| REQ-FN-002 | Second | Implemented | 75% | [REQ-FN-002] built. | [view](#d-2) |\n' +
  '| REQ-FN-003 | Third | Implemented | 75% | ⚠ visual: clipped. | [view](#d-3) |\n' +
  '| REQ-FN-004 | Fourth | Not Started | 0% | | [view](#d-4) |\n';

const TESTS = `using Xunit;

namespace Fixture;

public class RequirementTests
{
    [Fact(DisplayName = "REQ-FN-001 first requirement passes")]
    public void FirstPasses() => Assert.Equal(4, 2 + 2);

    [Fact(DisplayName = "REQ-FN-002 second requirement fails")]
    public void SecondFails() => Assert.Fail("deliberate failure for the verdict spec");
}
`;

/** Writes the fixture: a tiny real test project restored from this machine's package cache, and its checklist. */
function resetVerdictProject() {
  const vCache = process.env.NUGET_PACKAGES ?? join(homedir(), '.nuget', 'packages');
  write(join(PROJECT, 'Directory.Build.props'), '<Project />\n');
  write(join(PROJECT, 'nuget.config'),
    '<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear />' +
    (existsSync(vCache) ? `<add key="cache" value="${vCache}" />` : '<add key="nuget" value="https://api.nuget.org/v3/index.json" />') +
    '</packageSources></configuration>\n');
  write(join(PROJECT, 'Fixture.Tests.csproj'),
    '<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n    <ImplicitUsings>enable</ImplicitUsings>\n    <Nullable>enable</Nullable>\n    <IsPackable>false</IsPackable>\n    <NuGetAudit>false</NuGetAudit>\n  </PropertyGroup>\n  <ItemGroup>\n' +
    '    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />\n    <PackageReference Include="xunit" Version="2.*" />\n    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />\n  </ItemGroup>\n</Project>\n');
  write(join(PROJECT, 'RequirementTests.cs'), TESTS);
  write(CHECKLIST, BEFORE);
}

let page: Page;
test.beforeAll(async ({ browser }) => {
  page = await browser.newPage({ baseURL: process.env.BASE_URL });
  await signIn(page, 'Owner');
});
test.afterAll(async () => { await page.close(); });

async function startVerifyRun() {
  resetVerdictProject();
  await openProject(page, NAME);
  await openProcessesTab(page);
  await startProcess(page, 'verify-phase');
  // The build and the tests really run, so this takes as long as the project's own build and tests do.
  await expect(page.getByTestId('run-state')).toContainText('waiting for you', { timeout: 180_000 });
}

test('REQ-UI-048 an approved verify run writes Verified only where a passing test carries the id', async () => {
  test.setTimeout(300_000);
  await startVerifyRun();
  await page.getByTestId('answer-yes').click();
  await expect(page.getByTestId('run-state')).toContainText('finished', { timeout: 60_000 });
  await page.screenshot({ path: join(SHOTS, 'process-verdicts-finished.png'), fullPage: true });
  const vLines = readFileSync(CHECKLIST, 'utf8').split('\n');
  const vBefore = BEFORE.split('\n');
  expect(vLines.length).toBe(vBefore.length);
  // A passing test carries REQ-FN-001: Verified, 100%.
  expect(vLines[6]).toMatch(/^\| REQ-FN-001 \| First \| Verified \| 100% \| \[REQ-FN-001\] built\. \[run \d+\] verify-phase \d{4}-\d\d-\d\d: Verified — 1 of 1 test carrying REQ-FN-001 passed\. \| /);
  // A failing test carries REQ-FN-002: Needs re-verify, naming the failing test.
  expect(vLines[7]).toMatch(/^\| REQ-FN-002 \| Second \| Needs re-verify \| 75% \| \[REQ-FN-002\] built\. \[run \d+\] verify-phase \d{4}-\d\d-\d\d: Needs re-verify — 1 of 1 test failed: REQ-FN-002 second requirement fails\. \| /);
  // No test carries REQ-FN-003 (it also has a recorded defect, which is graded, not skipped).
  expect(vLines[8]).toMatch(/Needs re-verify \| 75% \| ⚠ visual: clipped\. \[run \d+\] verify-phase \d{4}-\d\d-\d\d: Needs re-verify — no test carries REQ-FN-003\. \| /);
  // A row that was not waiting for a verdict is exactly as it was.
  expect(vLines[9]).toBe(vBefore[9]);
});

test('REQ-UI-048 refusing the approval leaves the checklist exactly as it was', async () => {
  test.setTimeout(300_000);
  await startVerifyRun();
  await page.getByTestId('answer-no').click();
  await expect(page.getByTestId('run-state')).toContainText('finished', { timeout: 60_000 });
  expect(readFileSync(CHECKLIST, 'utf8')).toBe(BEFORE);
});
