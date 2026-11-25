// Run before and after every Playwright run (playwright.config.ts globalSetup / globalTeardown): removes from the
// test accounts the projects a test made and an earlier run left behind, and the scratch folders an interrupted
// test left in tests/verify/bin (only those older than an hour, so a run going on beside this one keeps its own).
// Owner, 20 Nov 2026. Prints counts only.
import { existsSync, readdirSync, rmSync, statSync } from 'fs';
import * as path from 'path';
import { SCRATCH, sweepLeftoverProjects } from './cleanup';

export default async function globalCleanup(): Promise<void> {
  const vRemoved = sweepLeftoverProjects();
  let vScratch = 0;
  if (existsSync(SCRATCH)) {
    for (const vName of readdirSync(SCRATCH)) {
      const vPath = path.join(SCRATCH, vName);
      if (Date.now() - statSync(vPath).mtimeMs > 3_600_000) {
        rmSync(vPath, { recursive: true, force: true });
        vScratch++;
      }
    }
  }
  if (vRemoved > 0 || vScratch > 0) console.log(`test accounts: removed ${vRemoved} left-over project(s); ${vScratch} old scratch folder(s)`);
}
