#!/usr/bin/env bash
# Seed for the Measurements screen (docs/mockups/settings-measurements.html), run by tf-verify-screens.sh
# before it opens /settings/measurements, with TF_BASE set to the running app's address (TechieFlow TF-002).
#
# The mockup draws one refused write on the Misses stream (misses-refused 1, in the warning colour), none on
# Sessions (sessions-refused 0, neutral), and the "One write failed" note in the warning colour
# (write-failed). This seed makes that state real for the open project, the way the app records and detects it
# (MeasurementActions.RefuseAsync / StreamsAsync):
#   - the project's RefusedWrite rows are exactly one, on misses.jsonl, the reason a failed append gives, dated
#     now. A stream's last write has failed when its newest RefusedWrite is later than the newest record in its
#     file, so misses.jsonl then reads "failed" and the note shows. Any refusal left on Sessions by the
#     REQ-UI-035 spec (it makes sessions.jsonl a directory) is cleared for this project, because the mockup
#     draws none there;
#   - docs/metrics holds its five files, as the app creates them (an existing file is never touched).
# Idempotent: every run leaves exactly one refusal on misses.jsonl (re-dated), nothing else changed.
#
# The open project is read from the running copy's own database (Setting SelectedProjectId, then that
# Project's Path). The seed only ever writes to a project folder or database under tests/.artifacts/, and
# refuses anything else.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
BASE="${TF_BASE:?TF_BASE is not set: run this from tf-verify-screens.sh or set it to the app address}"
PORT="$(sed -E 's#.*:([0-9]+)/?.*#\1#' <<<"$BASE")"
DB="$ROOT/tests/.artifacts/verify/run-$PORT/harness-data/chatur.db"
# Each account has its own database (BRD-210): seed the file of whoever signed in last, named by
# harness-data/accounts/current.txt, or the install's own before anyone has signed in.
POINTER="$(dirname "$DB")/accounts/current.txt"
if [ -s "$POINTER" ]; then DB="$(dirname "$DB")/accounts/$(tr -d '\r\n' < "$POINTER")/chatur.db"; fi
[[ -f "$DB" ]] || { echo "no database for the app at $BASE (looked for $DB)"; exit 1; }
case "$DB" in
  "$ROOT/tests/.artifacts/"*) ;;
  *) echo "refusing to write to $DB: the seed only writes under tests/.artifacts/"; exit 1 ;;
esac

python3 - "$DB" "$ROOT" "$(dirname "${BASH_SOURCE[0]}")" <<'PY'
import datetime, os, sqlite3, sys
sys.path.insert(0, sys.argv[3])
db, root = sys.argv[1], sys.argv[2]
c = sqlite3.connect(db, timeout=20)
row = c.execute("SELECT p.ProjectId, p.Path FROM Setting s JOIN Project p ON p.ProjectId = CAST(s.Value AS INTEGER) "
                "WHERE s.Key = 'SelectedProjectId'").fetchone()
if row is None or not row[1] or not os.path.isdir(row[1]):
    # Nothing open (the Workbench's menu walk ends on File > Close folder, 20 Nov 2026): open the screens' shared
    # seed project, as the other seeds do, rather than failing.
    import _seedlib
    _seedlib.open_project(c, root)
    c.commit()
    row = c.execute("SELECT p.ProjectId, p.Path FROM Setting s JOIN Project p ON p.ProjectId = CAST(s.Value AS INTEGER) "
                    "WHERE s.Key = 'SelectedProjectId'").fetchone()
project_id, path = row
real = os.path.realpath(path)
if not (real + "/").startswith(os.path.realpath(os.path.join(root, "tests", ".artifacts")) + "/"):
    print(f"refusing to write for {path}: the seed only writes for projects under tests/.artifacts/"); sys.exit(1)

folder = os.path.join(real, "docs", "metrics")
os.makedirs(folder, exist_ok=True)
for name in ("runs", "gates", "misses", "sessions", "commits"):
    f = os.path.join(folder, name + ".jsonl")
    if not os.path.exists(f):
        open(f, "w").close()

now = datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f0Z")
c.execute("DELETE FROM RefusedWrite WHERE ProjectId = ?", (project_id,))
c.execute("INSERT INTO RefusedWrite (ProjectId, StreamName, Reason, CreatedUtc) VALUES (?,?,?,?)",
          (project_id, "misses.jsonl", "Write failed: No space left on device", now))
c.commit()
print(f"project {project_id}: one refused write on misses.jsonl, dated {now}")

# The Sister projects card (REQ-UI-086) draws two sisters with their run counts. Two fixture folders under
# tests/.artifacts/ each hold a docs/metrics/runs.jsonl, and are named as sisters unless already named.
sisters_root = os.path.join(root, "tests", ".artifacts", "verify", "fixtures", "sisters")
for name, runs in (("Sevak", 4), ("TechieBlog", 3)):
    sister = os.path.join(sisters_root, name)
    metrics = os.path.join(sister, "docs", "metrics")
    os.makedirs(metrics, exist_ok=True)
    with open(os.path.join(metrics, "runs.jsonl"), "w", encoding="utf-8") as f:
        for i in range(runs):
            f.write('{"v":"1","ts":"2026-11-0%dT10:00:00Z","app":"%s","cmd":"build-phase","reqs":["REQ-FN-00%d"],"tokens_out":2000,"duration_s":300}\n' % (i + 1, name, i + 1))
    if not c.execute("SELECT 1 FROM SisterProject WHERE Path = ?", (sister,)).fetchone():
        c.execute("INSERT INTO SisterProject (Path, AddedUtc) VALUES (?, ?)", (sister, now))
c.commit()
print("sister projects: Sevak (4 runs) and TechieBlog (3 runs) named")
PY
