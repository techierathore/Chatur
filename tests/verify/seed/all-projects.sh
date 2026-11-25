#!/usr/bin/env bash
# Seed for All projects (docs/mockups/all-projects.html), run by tf-verify-screens.sh and tf-mockup-parity.sh
# before they open /all-projects, with TF_BASE set to the running app's address (verify of 21 Nov 2026).
#
# The mockup draws six projects (TfLens, AstroLyfe, AppManager, MyDiary, Xpenser, TechieRag) and two machines
# sharing agents and rules: this one and one named WIN. This seed makes the six as plain project folders in the
# tests' own scratch folder (tests/verify/bin/all-projects-seed, removed by the run-end sweep once idle) with their
# rows in the signed-in test account's database, and leaves a WIN machine's record in the harness's role-sync
# store file. Idempotent. Writes only under tests/.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
BASE="${TF_BASE:?TF_BASE is not set: run this from tf-verify-screens.sh or set it to the app address}"
PORT="$(sed -E 's#.*:([0-9]+)/?.*#\1#' <<<"$BASE")"
DATA="$ROOT/tests/.artifacts/verify/run-$PORT/harness-data"
DB="$DATA/chatur.db"
ACCOUNT=""
POINTER="$DATA/accounts/current.txt"
if [ -s "$POINTER" ]; then ACCOUNT="$(tr -d '\r\n' < "$POINTER")"; DB="$DATA/accounts/$ACCOUNT/chatur.db"; fi
[[ -f "$DB" && -n "$ACCOUNT" ]] || { echo "no signed-in account database for the app at $BASE"; exit 1; }
case "$DB" in
  "$ROOT/tests/.artifacts/"*) ;;
  *) echo "refusing to write to $DB: the seed only writes under tests/.artifacts/"; exit 1 ;;
esac

python3 - "$DB" "$ROOT" "$DATA/role-sync-store.json" "$ACCOUNT" <<'PY'
import datetime, json, os, sqlite3, sys
db, root, store, account = sys.argv[1:5]
folder = os.path.join(root, "tests", "verify", "bin", "all-projects-seed")
c = sqlite3.connect(db, timeout=20)
now = datetime.datetime.now(datetime.timezone.utc)
iso = lambda d: d.strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
# name, phase, requirements, open, verified: the mockup's own numbers, each project's checklist holding them
projects = [("TfLens", 4, 186, 22, 148), ("AstroLyfe", 3, 142, 31, 96), ("AppManager", 4, 98, 9, 84),
            ("MyDiary", 2, 64, 18, 40), ("Xpenser", 2, 51, 14, 33), ("TechieRag", 5, 120, 0, 107)]
for i, (name, phase, total, open_, verified) in enumerate(projects):
    path = os.path.join(folder, name)
    os.makedirs(os.path.join(path, "docs"), exist_ok=True)
    rows = ["Verified"] * verified + ["Not Started"] * open_ + ["N/A"] * (total - verified - open_)
    table = "\n".join(f"| REQ-FN-{n + 1:03d} | A requirement | {status} | 0% | | |" for n, status in enumerate(rows))
    with open(os.path.join(path, "docs", f"{name}-P{phase}-Checklist.md"), "w") as f:
        f.write(f"# {name} — Checklist (phase {phase})\n\n## Requirements Status\n\n"
                "| ID | Requirement | Status | % | Remarks | Details |\n|----|-------------|--------|---|---------|---------|\n" + table + "\n")
    # the mockup's states: three active (worked on within 14 days), two paused, and TechieRag "handed over",
    # which Chatur draws as the neutral "not opened" (it keeps no hand-over state of its own)
    ago = [0, 1, 8, 16, 20, None][i]
    when = None if ago is None else iso(now - datetime.timedelta(days=ago, hours=i))
    if c.execute("SELECT 1 FROM Project WHERE Path = ?", (path,)).fetchone():
        c.execute("UPDATE Project SET RemovedUtc = NULL, LastOpenedUtc = ? WHERE Path = ?", (when, path))
    else:
        c.execute("INSERT INTO Project (ProjectFolderId, Name, Path, Kind, LastOpenedUtc) VALUES (NULL, ?, ?, 'Folder', ?)", (name, path, when))
c.commit()

# a WIN record left by an earlier run goes before this machine syncs, so the sync never takes its roles
if os.path.exists(store):
    with open(store) as f:
        records = json.load(f)
    records[account] = [r for r in records.get(account, []) if r.get("DeviceId") != "seed-win-desk"]
    with open(store, "w") as f:
        json.dump(records, f, indent=2)
print("all-projects: six projects")
PY

# This machine is "up to date" only once a sync has recorded the hash of its own roles, which only Chatur itself
# can work out: so the seed presses Sync now, signed in as test user 1, in a headless browser. The address and
# password are read from tests/verify/test-settings.json inside the script and never printed.
REPO="$ROOT" BASE="$BASE" node - <<'JS'
const { chromium } = require(process.env.REPO + '/node_modules/@playwright/test');
const fs = require('fs');
(async () => {
  const owner = JSON.parse(fs.readFileSync(process.env.REPO + '/tests/verify/test-settings.json', 'utf8')).testUsers.Owner;
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage({ baseURL: process.env.BASE });
    for (let attempt = 1; attempt <= 3 && /sign-in/.test(page.url() || '/sign-in'); attempt++) {
      await page.goto('/sign-in', { waitUntil: 'networkidle' });
      if (!/sign-in/.test(page.url())) break;
      await page.waitForTimeout(1500 * attempt);
      await page.getByTestId('input-email').fill(owner.email);
      await page.getByTestId('input-password').fill(owner.password);
      await page.locator('[data-testid=signin-form] button[type=submit]').first().click();
      await page.waitForURL((u) => !/sign-in/.test(u.toString()), { timeout: 45000 }).catch(() => {});
    }
    await page.goto('/all-projects', { waitUntil: 'networkidle' });
    await page.getByTestId('sync-now').click({ timeout: 30000 });
    await page.getByTestId('sync-state-this').filter({ hasText: /up to date/i }).waitFor({ timeout: 30000 });
    console.log('all-projects: this machine synced');
  } catch (e) {
    console.log('all-projects: could not sync this machine: ' + String(e).split('\n')[0].slice(0, 160));
    process.exitCode = 1;
  } finally {
    await browser.close();
  }
})();
JS

# Only after this machine has synced: a second machine under the same account, two rules behind it. Written
# before the sync, its empty roles would have been taken by this machine's first sync.
python3 - "$DATA/role-sync-store.json" "$ACCOUNT" <<'PY'
import datetime, json, os, sys
store, account = sys.argv[1:3]
now = datetime.datetime.now(datetime.timezone.utc)
records = {}
if os.path.exists(store):
    with open(store) as f:
        records = json.load(f)
mine = [r for r in records.get(account, []) if r.get("DeviceId") != "seed-win-desk"]
stamp = (now - datetime.timedelta(days=2)).strftime("%Y-%m-%dT%H:%M:%SZ")
mine.append({"DeviceId": "seed-win-desk", "DeviceName": "WIN", "SyncedUtc": stamp, "ChangedUtc": stamp,
             "ContentHash": "seed-win-older", "Agents": 4, "Rules": 1, "Json": "{\"roles\":[],\"rules\":[]}"})
records[account] = mine
with open(store, "w") as f:
    json.dump(records, f, indent=2)
print("all-projects: a WIN machine two rules behind")
PY
