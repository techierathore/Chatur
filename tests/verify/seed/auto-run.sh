#!/usr/bin/env bash
# Seed for the End-to-end run screen (docs/mockups/auto-run.html), run by tf-verify-screens.sh before it
# opens /auto, with TF_BASE set to the running app's address (TechieFlow TF-002).
#
# The mockup draws a run going: the "Running now" panel (agent, process, step, requirement, Pause, Stop),
# a chain of eight with four finished, the fifth running and one retried, and the usage-limit and retry
# notes. A fresh verify copy has no run, so this seed writes one for the open project, the same way
# tests/verify/auto-run.spec.ts seedGoingRun does: paused on process 5 (build-phase), so nothing carries
# it on by itself while the screen is measured. It only writes under tests/.artifacts/, and replaces only the
# runs it wrote before (Brief starts "[seed:auto]"); the queue and report seeds' rows are never touched (see
# _seedlib.py for how the three coexist in any order). With no project open it opens a fixture project itself.
# The run started 5 h 17 m ago (the mockup says 1 h 42 m) so the report seed's run is the newest-started one.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
SEED_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BASE="${TF_BASE:?TF_BASE is not set: run this from tf-verify-screens.sh or set it to the app address}"
PORT="$(sed -E 's#.*:([0-9]+)/?.*#\1#' <<<"$BASE")"
DB="$ROOT/tests/.artifacts/verify/run-$PORT/harness-data/chatur.db"
[[ -f "$DB" ]] || { echo "no database for the app at $BASE (looked for $DB)"; exit 1; }
case "$DB" in
  "$ROOT/tests/.artifacts/"*) ;;
  *) echo "refusing to write to $DB: the seed only writes under tests/.artifacts/"; exit 1 ;;
esac

python3 - "$DB" "$ROOT" "$SEED_DIR" <<'PY'
import sys
sys.path.insert(0, sys.argv[3])
import _seedlib as L
c = L.connect(sys.argv[1], sys.argv[2])
project, _path = L.open_project(c, sys.argv[2])
now = L.now_utc()
iso, mins = L.iso, L.mins
# A run that is going or paused would be picked up instead; end any the app holds that no seed wrote.
L.clear_own(c, L.MARK_AUTO)
L.stop_foreign_going(c, project)
start = now - mins(L.START_GOING_MIN)
cur = c.execute("INSERT INTO AutoRun (ProjectId, Brief, State, CurrentProcess, StartedUtc) VALUES (?, ?, 'Paused', 5, ?)",
                (project, L.MARK_AUTO + " Finish the coverage panel: the file list, the per-file bar and the filter. Verify before you stop.", iso(start)))
run_id = cur.lastrowid
chain = ["day1-greenfield", "split-brd", "mockups", "deploy-checklist", "build-phase", "verify-phase", "fix-issues", "refresh-status"]
for pos, code in enumerate(chain, start=1):
    state = "Finished" if pos < 5 else "Running" if pos == 5 else "ToCome"
    c.execute("INSERT INTO AutoRunProcess (AutoRunId, Position, ProcessCode, State, ProcessRunId, Retries) VALUES (?, ?, ?, ?, NULL, ?)",
              (run_id, pos, code, state, 1 if pos == 4 else 0))
wait_at = start + mins(120)
events = [
    (iso(start + mins(60)), "Retry", 4, "deploy-checklist", "Step 4, deploy-checklist, stalled once and was retried.", None),
    (iso(wait_at), "LimitWait", 5, "build-phase", "The model reported a usage limit; waiting.", iso(wait_at + mins(22))),
    (iso(wait_at + mins(22)), "LimitResumed", 5, "build-phase", "The limit lifted; the run carried on by itself.", None),
    (iso(start + mins(160)), "ProcessStarted", 5, "build-phase", "Started the Flow master for build-phase.", None),
]
for at, kind, pos, code, msg, until in events:
    c.execute("INSERT INTO AutoRunEvent (AutoRunId, AtUtc, Kind, ProcessPosition, ProcessCode, Message, UntilUtc) VALUES (?, ?, ?, ?, ?, ?, ?)",
              (run_id, at, kind, pos, code, msg, until))
c.commit()
print(f"project {project}: seeded end-to-end run {run_id}, paused on process 5 (build-phase)")
PY
