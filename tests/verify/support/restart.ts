// Restarts the running web harness the way closing and reopening Chatur restarts the app: the process is
// stopped and the same published copy is started again on the same port, so it opens the same
// harness-data/chatur.db. tf-verify-boot.sh start cannot be used for this: it publishes a fresh copy and
// with it a fresh database. The new process is written into boot-<port>.json, so the boot script's own
// stop still stops it.
import { execFileSync, spawn } from 'child_process';
import { openSync, readFileSync, writeFileSync } from 'fs';

const ROOT = process.env.REPO || process.cwd();

function port(): number {
  const vBase = process.env.BASE_URL;
  if (!vBase) throw new Error('BASE_URL is not set: start the harness with tf-verify-boot.sh first.');
  return Number(new URL(vBase).port);
}

async function answers(aUrl: string): Promise<boolean> {
  try {
    await fetch(aUrl, { signal: AbortSignal.timeout(2_000) });
    return true;
  } catch {
    return false;
  }
}

function commandLine(aPid: number): string[] {
  if (process.platform === 'darwin') {
    const vCmd = execFileSync('ps', ['-p', String(aPid), '-o', 'command='], { encoding: 'utf8' }).trim();
    // ps returns the executable and its arguments as one string. The harness command lines here
    // contain no spaces in individual arguments, so a plain split is safe.
    return vCmd.split(/\s+/).filter((a) => a.length > 0);
  }
  return readFileSync(`/proc/${aPid}/cmdline`, 'utf8').split('\0').filter((a) => a.length > 0);
}

/** Stops the harness on BASE_URL's port and starts the same copy again; resolves once it answers. */
export async function restartHarness(): Promise<void> {
  const vPort = port();
  const vUrl = `http://localhost:${vPort}/`;
  const vStatePath = `${ROOT}/tests/.artifacts/verify/boot-${vPort}.json`;
  const vState = JSON.parse(readFileSync(vStatePath, 'utf8'));
  const vPid: number = vState.pids[0];
  const vArgs = commandLine(vPid);
  const vEnv = process.platform === 'darwin'
    ? { ...process.env, ASPNETCORE_ENVIRONMENT: 'Development' }
    : Object.fromEntries(
        readFileSync(`/proc/${vPid}/environ`, 'utf8').split('\0').filter((a) => a.includes('=')).map((a) => {
          const i = a.indexOf('=');
          return [a.slice(0, i), a.slice(i + 1)];
        }),
      );

  process.kill(vPid, 'SIGTERM');
  for (let i = 0; i < 60 && (await answers(vUrl)); i++) await new Promise((r) => setTimeout(r, 500));
  if (await answers(vUrl)) throw new Error(`the harness on port ${vPort} did not stop`);

  const vLog = openSync(`${ROOT}/${vState.log}`, 'a');
  const vChild = spawn(vArgs[0], vArgs.slice(1), { cwd: ROOT, env: vEnv, detached: true, stdio: ['ignore', vLog, vLog] });
  vChild.unref();
  vState.pids = [vChild.pid];
  writeFileSync(vStatePath, JSON.stringify(vState, null, 1));

  for (let i = 0; i < 120 && !(await answers(vUrl)); i++) await new Promise((r) => setTimeout(r, 500));
  if (!(await answers(vUrl))) throw new Error(`the harness on port ${vPort} did not come back`);
}
