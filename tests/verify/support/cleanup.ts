// Every test removes the projects it creates, and each run removes those an earlier run left behind (owner,
// 20 Nov 2026: about 80 left-over projects in the test account made the File and Edit menus slow to open).
// Only the test accounts are touched: the databases of the web harness copies under tests/.artifacts/verify/run-*/,
// which the tests boot and sign in to as the UsageGuide test users. The owner's own Chatur data is never opened here.
// Scratch folders for a test live under this test project's own bin folder (tests/verify/bin), never the system's
// temp folder, and are deleted when the test ends.
import { DatabaseSync } from 'node:sqlite';
import { existsSync, mkdirSync, readdirSync, rmSync, statSync, writeFileSync } from 'fs';
import { execFileSync } from 'child_process';
import * as path from 'path';
import { dbPath } from './db';

const REPO = path.resolve(__dirname, '..', '..', '..');
const TESTS = path.join(REPO, 'tests') + path.sep;
const VERIFY_ARTIFACTS = path.join(REPO, 'tests', '.artifacts', 'verify');

/** This test project's own bin folder: where a test's scratch data lives. */
export const SCRATCH = path.resolve(__dirname, '..', 'bin');

/** A fresh scratch folder under tests/verify/bin; the caller deletes it when its test ends. */
export function scratchDir(aName: string): string {
  const vDir = path.join(SCRATCH, aName);
  mkdirSync(vDir, { recursive: true });
  return vDir;
}

/** A dated name: ten or more digits from Date.now(), the mark every test-made project carries. */
const STAMPED = /\d{10,}/;

function openDb(aFile: string): DatabaseSync {
  const vDb = new DatabaseSync(aFile);
  vDb.exec('PRAGMA busy_timeout=10000');
  vDb.exec('PRAGMA foreign_keys=OFF');
  return vDb;
}

/** Tables that point at a project, read from the file itself so a table added later is cleaned too. */
function projectTables(aDb: DatabaseSync): string[] {
  const vTables = (aDb.prepare("SELECT name FROM sqlite_master WHERE type='table'").all() as { name: string }[]).map((r) => r.name);
  return vTables.filter((t) => t !== 'Project'
    && (aDb.prepare(`PRAGMA table_info("${t}")`).all() as { name: string }[]).some((c) => c.name === 'ProjectId'));
}

/** Deletes these projects and everything recorded against them from one account's database. */
function deleteProjects(aDb: DatabaseSync, aIds: number[]): void {
  if (aIds.length === 0) return;
  const vList = aIds.join(',');
  aDb.exec('BEGIN');
  try {
    // Children of a project's own rows have no ProjectId of their own: left behind, they are inherited by the next row
    // that reuses the id (SQLite reuses rowids). An end-to-end run failed on AutoRunProcess's key that way, and a new
    // conversation showed the messages of a removed project's conversation (SessionEvent, verify of 23 Nov 2026). So
    // every table with a ProjectId has the children of its primary key removed first, not a hand-kept list.
    const vTables = (aDb.prepare("SELECT name FROM sqlite_master WHERE type='table'").all() as { name: string }[]).map((r) => r.name);
    const vParents: [string, string][] = [];
    for (const vTable of vTables) {
      const vCols = aDb.prepare(`PRAGMA table_info("${vTable}")`).all() as { name: string; pk: number }[];
      const vKeys = vCols.filter((c) => c.pk > 0);
      if (vCols.some((c) => c.name === 'ProjectId') && vKeys.length === 1 && vKeys[0].name !== 'ProjectId') vParents.push([vTable, vKeys[0].name]);
    }
    for (const [vParent, vKey] of vParents) {
      for (const vChild of vTables) {
        const vCols = (aDb.prepare(`PRAGMA table_info("${vChild}")`).all() as { name: string }[]).map((c) => c.name);
        if (vChild === vParent || !vCols.includes(vKey) || vCols.includes('ProjectId')) continue;
        aDb.exec(`DELETE FROM "${vChild}" WHERE ${vKey} IN (SELECT ${vKey} FROM "${vParent}" WHERE ProjectId IN (${vList}))`);
      }
    }
    for (const vTable of projectTables(aDb)) aDb.exec(`DELETE FROM "${vTable}" WHERE ProjectId IN (${vList})`);
    aDb.exec(`DELETE FROM Setting WHERE Key = 'SelectedProjectId' AND Value IN (${aIds.map((i) => `'${i}'`).join(',')})`);
    aDb.exec(`DELETE FROM Project WHERE ProjectId IN (${vList})`);
    aDb.exec('COMMIT');
  } catch (aError) {
    try {
      aDb.exec('ROLLBACK');
    } catch {
      /* the failed statement already ended the transaction; the first error is the one that matters */
    }
    throw aError;
  }
}

/**
 * Removes every project at or under this folder from the test accounts (every one the harness copies hold, or only
 * the database named), the folder entry naming it, and the folder on disk. Refuses a folder outside tests/.
 */
export function removeProjectsUnder(aFolder: string, aDbFile?: string): number {
  const vFolder = path.resolve(aFolder);
  if (!(vFolder + path.sep).startsWith(TESTS)) throw new Error(`refusing to remove projects outside tests/: ${vFolder}`);
  let vCount = 0;
  const vFiles = aDbFile ? [aDbFile] : [...new Set([...testAccountDatabases(), dbPath()])];
  for (const vFile of vFiles) {
    if (!existsSync(vFile)) continue;
    const vDb = openDb(vFile);
    try {
      const vLike = vFolder + path.sep + '%';
      // The desktop app on Windows stores the same folder as C:\…; both spellings are removed.
      const vWindows = vFolder.replace(/^\/mnt\/([a-z])\//, (_m, aDrive: string) => `${aDrive.toUpperCase()}:\\`).replace(/\//g, '\\');
      const vWindowsLike = vWindows + '\\%';
      const vIds = (vDb.prepare('SELECT ProjectId FROM Project WHERE Path = ? OR Path LIKE ? OR Path = ? OR Path LIKE ?')
        .all(vFolder, vLike, vWindows, vWindowsLike) as { ProjectId: number }[]).map((r) => r.ProjectId);
      // The desktop app on Windows may hold its database for a moment (a run just ended writes to it), and a lock
      // taken on the Windows side is not waited for from WSL: the removal is tried again a few times.
      for (let vTry = 1; ; vTry++) {
        try {
          deleteProjects(vDb, vIds);
          vDb.prepare('DELETE FROM ProjectFolder WHERE Path = ? OR Path LIKE ? OR Path = ? OR Path LIKE ?').run(vFolder, vLike, vWindows, vWindowsLike);
          break;
        } catch (aError) {
          if (vTry >= 6 || !/locked|busy/i.test(String((aError as Error).message))) throw aError;
          Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, 2_000);
        }
      }
      vCount += vIds.length;
    } finally {
      vDb.close();
    }
  }
  rmSync(vFolder, { recursive: true, force: true, maxRetries: 10, retryDelay: 1_000 });
  return vCount;
}

/** Removes the projects a test made inside a shared fixture folder: those whose folder name carries its stamp. */
export function removeStampedProjects(aParent: string, aStamp: number | string, aDbFile?: string): number {
  const vParent = path.resolve(aParent);
  if (!existsSync(vParent)) return 0;
  let vCount = 0;
  for (const vName of readdirSync(vParent)) {
    if (vName.includes(String(aStamp))) vCount += removeProjectsUnder(path.join(vParent, vName), aDbFile);
  }
  return vCount;
}

/** Whether a folder has gone untouched for more than an hour. */
function idleOverAnHour(aFolder: string): boolean {
  try {
    return Date.now() - statSync(aFolder).mtimeMs > 3_600_000;
  } catch {
    return true;
  }
}

/** Every test account database the harness copies hold (run-<port>/harness-data/accounts/<id>/chatur.db). */
export function testAccountDatabases(): string[] {
  const vFiles: string[] = [];
  // A desktop head started by a test keeps its data under tests/.artifacts/app-data/<head> (REQ-NFR-010), never
  // in the owner's own application-data folder, which is never opened here.
  const vAppData = path.join(REPO, 'tests', '.artifacts', 'app-data');
  if (existsSync(vAppData)) {
    for (const vHead of readdirSync(vAppData)) {
      const vInstall = path.join(vAppData, vHead, 'chatur.db');
      if (existsSync(vInstall)) vFiles.push(vInstall);
      const vAccounts = path.join(vAppData, vHead, 'accounts');
      if (!existsSync(vAccounts)) continue;
      for (const vId of readdirSync(vAccounts)) {
        const vFile = path.join(vAccounts, vId, 'chatur.db');
        if (existsSync(vFile)) vFiles.push(vFile);
      }
    }
  }
  if (!existsSync(VERIFY_ARTIFACTS)) return vFiles;
  for (const vRun of readdirSync(VERIFY_ARTIFACTS).filter((n) => n.startsWith('run-'))) {
    const vInstall = path.join(VERIFY_ARTIFACTS, vRun, 'harness-data', 'chatur.db'); // the harness copy's own file
    if (existsSync(vInstall)) vFiles.push(vInstall);
    const vAccounts = path.join(VERIFY_ARTIFACTS, vRun, 'harness-data', 'accounts');
    if (!existsSync(vAccounts)) continue;
    for (const vId of readdirSync(vAccounts)) {
      const vFile = path.join(vAccounts, vId, 'chatur.db');
      if (existsSync(vFile)) vFiles.push(vFile);
    }
  }
  return vFiles;
}

/**
 * Removes, from every test account, the projects earlier runs left behind: a project inside the repository's
 * tests folder whose name carries a test's date stamp, which lies in a test's scratch folder, or whose folder is
 * gone. Their folders on disk go too. Returns how many projects were removed.
 */
export function sweepLeftoverProjects(): number {
  let vRemoved = 0;
  const vFolders = new Set<string>();
  for (const vFile of testAccountDatabases()) {
    const vDb = openDb(vFile);
    try {
      const vRows = vDb.prepare('SELECT ProjectId, Path FROM Project').all() as { ProjectId: number; Path: string }[];
      const vIds: number[] = [];
      for (const vRow of vRows) {
        const vPath = path.resolve(vRow.Path);
        if (!(vPath + path.sep).startsWith(TESTS)) continue; // never anything outside the tests folder
        const vFolder = /\.sln$/i.test(vPath) ? path.dirname(vPath) : vPath;
        // a stamped or scratch project counts as left behind only when nothing has touched it for an hour: a run
        // going on beside this one (a second test window) keeps its live fixtures
        const vLeft = !existsSync(vPath)
          || ((STAMPED.test(path.basename(vFolder)) || (vFolder + path.sep).startsWith(SCRATCH + path.sep)) && idleOverAnHour(vFolder));
        if (!vLeft) continue;
        vIds.push(vRow.ProjectId);
        vFolders.add(vFolder);
      }
      deleteProjects(vDb, vIds);
      vRemoved += vIds.length;
      // a folder entry left pointing at a stamped or missing fixture root goes as well
      for (const vRow of vDb.prepare('SELECT ProjectFolderId, Path FROM ProjectFolder').all() as { ProjectFolderId: number; Path: string }[]) {
        const vPath = path.resolve(vRow.Path);
        if (!(vPath + path.sep).startsWith(TESTS)) continue;
        if (!existsSync(vPath) || (STAMPED.test(vPath.slice(TESTS.length)) && idleOverAnHour(vPath))) {
          vDb.prepare('UPDATE Project SET ProjectFolderId = NULL WHERE ProjectFolderId = ?').run(vRow.ProjectFolderId);
          vDb.prepare('DELETE FROM ProjectFolder WHERE ProjectFolderId = ?').run(vRow.ProjectFolderId);
          if (existsSync(vPath)) vFolders.add(vPath);
        }
      }
    } finally {
      vDb.close();
    }
  }
  for (const vFolder of vFolders) {
    if ((vFolder + path.sep).startsWith(TESTS) && vFolder !== TESTS.slice(0, -1)) rmSync(vFolder, { recursive: true, force: true });
  }
  return vRemoved;
}

/**
 * Makes a folder under tests/ unreadable to the app, and back. chmod changes nothing on the Windows drive the
 * repository sits on, so a Windows deny rule does it (it reaches the WSL side through the drive mount).
 */
export function lockFolder(aFolder: string): void {
  windowsAcl(aFolder, ['/deny', '%USERNAME%:(OI)(CI)(RD,X)']);
}

/** Takes the deny rule of {@link lockFolder} off again, so the folder can be deleted. */
export function unlockFolder(aFolder: string): void {
  windowsAcl(aFolder, ['/remove:d', '%USERNAME%']);
}

function windowsAcl(aFolder: string, aArgs: string[]): void {
  const vFolder = path.resolve(aFolder);
  if (!(vFolder + path.sep).startsWith(TESTS) || !existsSync(vFolder)) return;
  const vWin = execFileSync('wslpath', ['-w', vFolder], { encoding: 'utf8' }).trim();
  execFileSync('cmd.exe', ['/c', 'icacls', vWin, ...aArgs], { stdio: 'ignore', cwd: REPO });
}

/**
 * Makes a scratch folder count as outside every repository although it sits inside Chatur's own checkout: a
 * `.git` file whose pointer leads nowhere stops the search for a repository going further up, so the folder
 * reads as "not a git repository".
 */
export function outsideAnyRepository(aFolder: string): void {
  mkdirSync(aFolder, { recursive: true });
  writeFileSync(path.join(aFolder, '.git'), 'gitdir: ./no-repository-here\n');
}
