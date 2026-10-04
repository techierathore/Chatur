// Test-fixture access to the harness's own SQLite file (verify-phase step 4). Used only to SEED a state the
// UI cannot produce (a correction row, a refused write) and to RESTORE what a test changed — never to decide a verdict
// on its own; every assertion goes through the UI except where the acceptance line itself is about the database.
import { DatabaseSync } from 'node:sqlite';

// Follows the booted instance: CHATUR_DB wins, otherwise run-<port of BASE_URL>/harness-data/chatur.db.
const PORT = (() => {
  try {
    return new URL(process.env.BASE_URL ?? 'http://localhost:5280').port || '5280';
  } catch {
    return '5280';
  }
})();
const DB_PATH =
  process.env.CHATUR_DB ?? `/mnt/c/1MyCode/Chatur/tests/.artifacts/verify/run-${PORT}/harness-data/chatur.db`;

export type Row = Record<string, string | number | null>;

export function open(): DatabaseSync {
  const vDb = new DatabaseSync(DB_PATH);
  vDb.exec('PRAGMA busy_timeout=10000');
  vDb.exec('PRAGMA foreign_keys=OFF');
  return vDb;
}

export function query(aSql: string, ...aArgs: (string | number | null)[]): Row[] {
  const vDb = open();
  try {
    return vDb.prepare(aSql).all(...aArgs) as Row[];
  } finally {
    vDb.close();
  }
}

export function run(aSql: string, ...aArgs: (string | number | null)[]): number {
  const vDb = open();
  try {
    const vResult = vDb.prepare(aSql).run(...aArgs);
    return Number(vResult.lastInsertRowid);
  } finally {
    vDb.close();
  }
}

export type Snapshot = Record<string, Row[]>;

/** Copies whole tables so a test can put them back exactly as they were. */
export function snapshot(aTables: string[]): Snapshot {
  const vSnap: Snapshot = {};
  for (const vTable of aTables) vSnap[vTable] = query(`SELECT * FROM "${vTable}"`);
  return vSnap;
}

export function restore(aSnap: Snapshot): void {
  const vDb = open();
  try {
    vDb.exec('BEGIN');
    for (const [vTable, vRows] of Object.entries(aSnap)) {
      vDb.exec(`DELETE FROM "${vTable}"`);
      for (const vRow of vRows) {
        const vCols = Object.keys(vRow);
        vDb
          .prepare(`INSERT INTO "${vTable}" (${vCols.map((c) => `"${c}"`).join(',')}) VALUES (${vCols.map(() => '?').join(',')})`)
          .run(...(vCols.map((c) => vRow[c]) as (string | number | null)[]));
      }
    }
    vDb.exec('COMMIT');
  } finally {
    vDb.close();
  }
}

export const ROLE_TABLES = ['Role', 'RoleCommand', 'RoleRight', 'Rule', 'RoleVersionHistory'];
