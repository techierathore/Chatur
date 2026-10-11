#!/usr/bin/env bash
# Seed for the Run queue screen (docs/mockups/run-queue.html), run by tf-verify-screens.sh before it opens
# /auto/queue, with TF_BASE set to the running app's address (TechieFlow TF-002).
#
# The mockup draws five rows: 1 running, 2 waiting, 3 paused, 4 waiting, and one finished run kept in the
# history below them ("1 running · 3 waiting"). A fresh verify copy has an empty queue, so this seed writes
# those rows the way the app keeps them (RunQueueActions): a RunQueueItem is only Queued or Started; where a
# started row stands is read from its AutoRun (Running or WaitingForLimit = running, Paused = paused, any
# other state = finished). So:
#   - row 1 "running": a Started row on an AutoRun waiting out a usage limit for the next six hours. The queue
#     reads that as "running", and nothing carries it on while the screen is measured (the same going state
#     tests/verify/run-queue.spec.ts seeds). A genuinely Running AutoRun is NOT used: opening /auto would
#     launch it and start a real process run;
#   - rows 2 and 4 "waiting": Queued rows with no run. They stay waiting because row 1 is going (the queue
#     starts the next waiting row only when none is running);
#   - row 3 "paused": a Started row on a Paused AutoRun (stopped inside process 3);
#   - the finished row: a Started row on a Complete AutoRun, older than the others, so it is listed last.
# Rows 1 and 4 belong to the open project (a fixture project is opened when none is); rows 2, 3 and the finished
# one to two private, hidden projects this seed makes, so they never show in the open project's run report.
# Only writes the verify copy's database under tests/.artifacts/, and refuses anything else. Idempotent: it
# removes what an earlier run wrote (brief starts "[seed:queue]") and marks every queue row no seed wrote Removed,
# so the queue holds exactly these five rows. It never touches the End-to-end run or Run report seeds' rows.
# Row 1's run is also a going run of the open project, so it is given a LOWER AutoRunId than the End-to-end run
# seed's run (the one /auto must show; see _seedlib.raise_auto). Row 1 started 5 h 17 m ago (the mockup says
# 1 h 42 m) so the report seed's run stays the newest-started run of the open project.
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
open_project, _path = L.open_project(c, sys.argv[2])
qa = L.hidden_project(c, sys.argv[2], "SeedQueueA")
qb = L.hidden_project(c, sys.argv[2], "SeedQueueB")
other = lambda i: (qa, qb)[i % 2]

now = L.now_utc()
iso, mins = L.iso, L.mins

# What an earlier run of this seed wrote.
L.clear_own(c, L.MARK_QUEUE)
c.execute("DELETE FROM RunQueueItem WHERE Brief LIKE '[seed:queue]%'")
# A queue row no seed wrote, or a run going on a project used that no seed wrote, would change what the screen shows.
c.execute("UPDATE RunQueueItem SET Status = 'Removed', RemovedUtc = ? WHERE Status <> 'Removed' AND Brief NOT LIKE '[seed:%'", (iso(now),))
for p in (open_project, qa, qb):
    L.stop_foreign_going(c, p)

chain = ["day1-greenfield", "split-brd", "mockups", "deploy-checklist", "build-phase", "verify-phase", "fix-issues", "refresh-status"]

def make_run(project, brief, state, current, started, ended=None, resume=None):
    cur = c.execute("INSERT INTO AutoRun (ProjectId, Brief, State, CurrentProcess, StartedUtc, EndedUtc, ResumeAtUtc) VALUES (?,?,?,?,?,?,?)",
                    (project, brief, state, current, iso(started), iso(ended) if ended else None, iso(resume) if resume else None))
    run_id = cur.lastrowid
    for pos, code in enumerate(chain, start=1):
        if state == "Complete":
            pstate = "Finished"
        else:
            pstate = "Finished" if pos < current else "Running" if pos == current else "ToCome"
        c.execute("INSERT INTO AutoRunProcess (AutoRunId, Position, ProcessCode, State, ProcessRunId, Retries) VALUES (?,?,?,?,NULL,0)",
                  (run_id, pos, code, pstate))
    return run_id

def queue(project, brief, status, run_id, queued):
    c.execute("INSERT INTO RunQueueItem (ProjectId, Brief, Status, AutoRunId, QueuedUtc) VALUES (?,?,?,?,?)",
              (project, brief, status, run_id, iso(queued)))

M = L.MARK_QUEUE
# The finished run (queued first, so it is the oldest row and is listed in the history).
done = make_run(other(2), M + " Nightly verify of what is open", "Complete", 8, now - mins(500), now - mins(500) + mins(194))
queue(other(2), M + " Nightly verify of what is open", "Started", done, now - mins(510))
# Row 1, running: waiting out a usage limit for six more hours. It is the open project's.
going = make_run(open_project, M + " Coverage panel, then verify", "WaitingForLimit", 5, now - mins(L.START_GOING_MIN), resume=now + mins(360))
queue(open_project, M + " Coverage panel, then verify", "Started", going, now - mins(L.START_GOING_MIN + 8))
# Row 2, waiting.
queue(other(0), M + " Chart screen from the P3 brief", "Queued", None, now - mins(100))
# Row 3, paused, 26 minutes in.
paused = make_run(other(1), M + " Rewrite the entry editor", "Paused", 3, now - mins(26))
queue(other(1), M + " Rewrite the entry editor", "Started", paused, now - mins(90))
# Row 4, waiting.
queue(open_project, M + " Settings: providers and routing", "Queued", None, now - mins(80))
# The End-to-end run seed's run, when it is there, must stay the run going on the open project.
L.raise_auto(c, open_project)
c.commit()
print(f"seeded the run queue: row 1 running (project {open_project}), 2 waiting, 3 paused, 4 waiting, plus 1 finished in the history")
PY
