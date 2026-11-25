// The owner's proof for Run (21 Nov 2026, point 1.4): a real Blazor web app, a console app and a class library with
// tests, made from this machine's own `dotnet new` templates in one solution under this test project's bin folder, and
// deleted when the test ends. The web harness runs on the WSL side, so its copy is made with the WSL dotnet; the
// desktop app runs on Windows, so its copy is made with the Windows dotnet. No setting here comes from the environment.
import { execFileSync } from 'child_process';
import { existsSync, readFileSync, rmSync, writeFileSync } from 'fs';
import { join } from 'path';
import { homedir } from 'os';
import { scratchDir } from './cleanup';

export type ProofSide = 'wsl' | 'windows';

export interface ProofSolution {
  /** The folder holding the solution, as this test process sees it. */
  root: string;
  /** The solution file, as this test process sees it. */
  solution: string;
  /** The solution file, as the app under test sees it (a Windows path for the desktop app). */
  solutionForApp: string;
}

/** A WSL path under /mnt/<drive>/ as Windows writes it. */
export const windowsPath = (aPath: string): string =>
  aPath.replace(/^\/mnt\/([a-z])\//, (_m, aDrive: string) => `${aDrive.toUpperCase()}:\\`).replace(/\//g, '\\');

/** The console program: three lines, then exit code 3, so the exit code shows. */
export const CONSOLE_PROGRAM = 'Console.WriteLine("Proof console starting");\nConsole.WriteLine("Proof console working");\nConsole.Error.WriteLine("Proof console warning on stderr");\nreturn 3;\n';

/** The console program with a compile error, for the failed build. */
export const CONSOLE_BROKEN = 'Console.WriteLine("Proof console starting")\nreturn 0;\n';

function dotnet(aSide: ProofSide, aCwd: string, aArgs: string[]): void {
  if (aSide === 'windows') {
    execFileSync('cmd.exe', ['/c', 'dotnet', ...aArgs], { cwd: aCwd, stdio: 'ignore', timeout: 300_000 });
  } else {
    const vDotnet = existsSync(join(homedir(), '.dotnet', 'dotnet')) ? join(homedir(), '.dotnet', 'dotnet') : 'dotnet';
    execFileSync(vDotnet, aArgs, { cwd: aCwd, stdio: 'ignore', timeout: 300_000 });
  }
}

/** Makes the proof solution from the machine's templates: ProofWeb (blazor), ProofConsole, ProofLib, ProofLib.Tests (xunit). */
export function makeProofSolution(aSide: ProofSide, aStamp: number | string): ProofSolution {
  const vRoot = scratchDir(`run-proof-${aSide}-${aStamp}`);
  // Chatur's own Directory.Build.props and nuget.config sit above this folder; the proof projects must not inherit them.
  writeFileSync(join(vRoot, 'Directory.Build.props'), '<Project />\n');
  writeFileSync(join(vRoot, 'Directory.Build.targets'), '<Project />\n');
  writeFileSync(join(vRoot, 'nuget.config'), '<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>\n');
  dotnet(aSide, vRoot, ['new', 'sln', '-n', 'ProofApps', '--format', 'sln']);
  dotnet(aSide, vRoot, ['new', 'blazor', '-n', 'ProofWeb', '-f', 'net10.0']);
  dotnet(aSide, vRoot, ['new', 'console', '-n', 'ProofConsole', '-f', 'net10.0']);
  dotnet(aSide, vRoot, ['new', 'classlib', '-n', 'ProofLib', '-f', 'net10.0']);
  dotnet(aSide, vRoot, ['new', 'xunit', '-n', 'ProofLib.Tests', '-f', 'net10.0']);
  dotnet(aSide, vRoot, ['sln', 'ProofApps.sln', 'add', 'ProofWeb', 'ProofConsole', 'ProofLib', 'ProofLib.Tests']);
  dotnet(aSide, vRoot, ['add', 'ProofLib.Tests', 'reference', 'ProofLib']);
  writeFileSync(join(vRoot, 'ProofLib', 'Class1.cs'), 'namespace ProofLib;\n\npublic static class Adder\n{\n    public static int Add(int a, int b) => a + b;\n}\n');
  writeFileSync(join(vRoot, 'ProofLib.Tests', 'UnitTest1.cs'), 'namespace ProofLib.Tests;\n\npublic class AdderTests\n{\n    [Fact]\n    public void AddsTwoNumbers() => Assert.Equal(5, ProofLib.Adder.Add(2, 3));\n}\n');
  writeFileSync(join(vRoot, 'ProofConsole', 'Program.cs'), CONSOLE_PROGRAM);
  // Restored once here, so the runs under test spend their time building and running, not downloading.
  dotnet(aSide, vRoot, ['restore', 'ProofApps.sln']);
  const vSolution = join(vRoot, 'ProofApps.sln');
  return { root: vRoot, solution: vSolution, solutionForApp: aSide === 'windows' ? windowsPath(vSolution) : vSolution };
}

/** Writes the console program's text. */
export function writeConsole(aProof: ProofSolution, aText: string): void {
  writeFileSync(join(aProof.root, 'ProofConsole', 'Program.cs'), aText);
}

/** The address the web app's own launch settings name first, read from the file the template wrote. */
export function webLaunchAddress(aProof: ProofSolution): string {
  const vText = readFileSync(join(aProof.root, 'ProofWeb', 'Properties', 'launchSettings.json'), 'utf8');
  const vSettings = JSON.parse(vText) as { profiles: Record<string, { commandName?: string; applicationUrl?: string }> };
  const vProfile = Object.values(vSettings.profiles).find((p) => p.commandName === 'Project');
  return (vProfile?.applicationUrl ?? '').split(';')[0];
}

/**
 * Ends every program still running from the proof solution's folder (a run a failed test never stopped), on the side
 * it runs on, then deletes the folder. The caller removes the project from the test account first.
 */
export function deleteProofSolution(aProof: ProofSolution | undefined, aSide: ProofSide = 'wsl'): void {
  if (!aProof) return;
  try {
    if (aSide === 'windows') {
      const vFolder = windowsPath(aProof.root).replace(/'/g, "''");
      execFileSync('powershell.exe', ['-NoProfile', '-Command',
        `Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -and $_.CommandLine.Contains('${vFolder}') } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }`],
        { stdio: 'ignore', timeout: 60_000 });
    } else {
      execFileSync('pkill', ['-f', aProof.root], { stdio: 'ignore' });
    }
  } catch {
    /* nothing was running */
  }
  rmSync(aProof.root, { recursive: true, force: true, maxRetries: 10, retryDelay: 1_000 });
}
