#!/usr/bin/env bash
# Seed for the Run report screen (docs/mockups/run-report.html), run by tf-verify-screens.sh before it opens
# /auto/report, with TF_BASE set to the running app's address (TechieFlow TF-002).
#
# The mockup draws the newest run of the open project: a finished end-to-end run, eight requirements with their
# verdicts, four misses (whose gap, which requirement, fixed or not), a cost row per model with a Total row, and
# the note that the time spent waiting on a usage limit cost nothing. This seed writes that run where
# RunReportActions reads each figure (an AutoRun of Chatur's own, key auto-{id}):
#   - the run: an AutoRun (Complete) on the open project with its eight AutoRunProcess rows, each on a finished
#     ProcessRun, a usage-limit wait of 22 minutes in its AutoRunEvent record and two retries of step 4. It
#     started 3 h 37 m ago and ended a minute ago, so it is the newest and its working time is 3 h 14 m;
#   - the requirements: ProcessRunVerdict rows on the verify-phase ProcessRun, in the mockup's order, with the
#     verdict words Verified, Verified, Verified, Needs re-verify (shown as Partial), Verified, Needs re-verify,
#     Blocked, Verified; their titles come from docs/Seed-Checklist.md, a checklist this seed writes into the
#     project folder (the eight mockup ids and titles, status to match);
#   - the misses: four records in the project's docs/metrics/misses.jsonl, stamped inside the run, with the
#     owners the mockup draws (the owner's brief, TrBlazeUI, Chatur, TechieRag) and a miss-fix record for the
#     first and third (fixed yes, no, yes, no);
#   - the cost: five records in docs/metrics/runs.jsonl, one per model, started inside the run, with the tokens
#     and seconds the mockup draws, so the rows add up to the Total row, the tokens tile and the time tile.
# Where the mockup draws a figure the data cannot carry, the real page differs: the Calls column (no record
# counts calls) and the agent of REQ-RAG-008 / REQ-UI-017 (the app names the agent from the id's type).
# The screen opens the run that STARTED last on the open project (RunReportActions orders by StartedUtc), so the
# End-to-end run and Run queue seeds start their runs on the open project earlier than this one (5 h 17 m ago,
# against 3 h 37 m here) and the three seeds can run in any order. This seed removes only its own rows (Brief
# starts "[seed:report]"), never another seed's; with no project open it opens a fixture project itself.
# Every record this seed writes carries "seed": true and is replaced on the next run. The project folder and the
# database are only written when they are under tests/.artifacts/; anything else is refused. Idempotent.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
BASE="${TF_BASE:?TF_BASE is not set: run this from tf-verify-screens.sh or set it to the app address}"
PORT="$(sed -E 's#.*:([0-9]+)/?.*#\1#' <<<"$BASE")"
DB="$ROOT/tests/.artifacts/verify/run-$PORT/harness-data/chatur.db"
[[ -f "$DB" ]] || { echo "no database for the app at $BASE (looked for $DB)"; exit 1; }
case "$DB" in
  "$ROOT/tests/.artifacts/"*) ;;
  *) echo "refusing to write to $DB: the seed only writes under tests/.artifacts/"; exit 1 ;;
esac

SEED_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
python3 - "$DB" "$ROOT" "$SEED_DIR" <<'PY'
import datetime, json, os, sqlite3, sys
db, root = sys.argv[1], sys.argv[2]
sys.path.insert(0, sys.argv[3])
import _seedlib as L
c = L.connect(db, root)
project, path = L.open_project(c, root)
if not path or not os.path.isdir(path):
    print("the app has no project open"); sys.exit(1)
real = os.path.realpath(path)
if not (real + "/").startswith(os.path.realpath(os.path.join(root, "tests", ".artifacts")) + "/"):
    print(f"refusing to write for {path}: the seed only writes for projects under tests/.artifacts/"); sys.exit(1)

now = datetime.datetime.now(datetime.timezone.utc)
mins = lambda m: datetime.timedelta(minutes=m)
iso = lambda d: d.strftime("%Y-%m-%dT%H:%M:%S.%f0Z")
iso_s = lambda d: d.strftime("%Y-%m-%dT%H:%M:%SZ")
start = now - mins(217)          # 3 h 37 m ago
end = now - mins(1)              # 3 h 36 m later, less the 22 m wait = 3 h 14 m of work

# ---- the database: replace this seed's earlier run, and only that --------------------------------------------
L.clear_own(c, L.MARK_REPORT, with_process_runs=True)
# A run still going on this project that no seed wrote is ended, so the screen is not about it.
L.stop_foreign_going(c, project)
# The screen opens the newest-STARTED run. Runs a test left on this project (no seed marker) that started after
# the report run are moved back, whole, to before it (start and end shifted by the same amount), so the report
# run is the one opened. Only the verify copy's database is touched, as everywhere in this seed.
newer = c.execute("SELECT AutoRunId, StartedUtc, EndedUtc FROM AutoRun WHERE ProjectId = ? AND StartedUtc > ? "
                  "AND Brief NOT LIKE '[seed:%'", (project, iso(start))).fetchall()
for run_id, started, ended in newer:
    s0 = datetime.datetime.fromisoformat(started.replace("Z", "+00:00")[:26] + "+00:00" if "+" not in started else started)
    shift = s0 - (start - mins(120))
    moved_end = None
    if ended:
        e0 = datetime.datetime.fromisoformat(ended.replace("Z", "+00:00")[:26] + "+00:00" if "+" not in ended else ended)
        moved_end = iso(e0 - shift)
    c.execute("UPDATE AutoRun SET StartedUtc = ?, EndedUtc = ? WHERE AutoRunId = ?", (iso(s0 - shift), moved_end, run_id))
if newer:
    print(f"moved {len(newer)} test-made run(s) of this project to before the report run")

chain = ["day1-greenfield", "split-brd", "mockups", "deploy-checklist", "build-phase", "verify-phase", "fix-issues", "refresh-status"]
cur = c.execute("INSERT INTO AutoRun (ProjectId, Brief, State, CurrentProcess, StartedUtc, EndedUtc) VALUES (?,?,?,?,?,?)",
                (project, L.MARK_REPORT + " Finish the coverage panel: the file list, the per-file bar and the filter. Verify before you stop.",
                 "Complete", len(chain), iso(start), iso(end)))
auto_id = cur.lastrowid
span = (end - start) / len(chain)
verify_run = None
for pos, code in enumerate(chain, start=1):
    p_start = start + span * (pos - 1)
    pr = c.execute("INSERT INTO ProcessRun (ProjectId, ProcessCode, State, CurrentStep, StartedUtc, EndedUtc) VALUES (?,?,'Finished',1,?,?)",
                   (project, code, iso(p_start), iso(p_start + span))).lastrowid
    c.execute("INSERT INTO AutoRunProcess (AutoRunId, Position, ProcessCode, State, ProcessRunId, Retries) VALUES (?,?,?,'Finished',?,?)",
              (auto_id, pos, code, pr, 2 if pos == 4 else 0))
    if code == "verify-phase":
        verify_run = pr

# Record of the run: starts, the 22-minute usage-limit wait, the retries of step 4.
events = [
    (start + mins(1), "Started", None, None, "The run started from the brief.", None),
    (start + mins(40), "Retry", 4, "deploy-checklist", "Step 4 of deploy-checklist stalled. Trying again (try 1 of 3).", None),
    (start + mins(48), "Retry", 4, "deploy-checklist", "Step 4 of deploy-checklist stalled. Trying again (try 2 of 3).", None),
    (start + mins(100), "LimitWait", 5, "build-phase", "A model reported a usage limit. The run waits 22 minutes and carries on by itself on the same step.", start + mins(122)),
    (start + mins(122), "LimitResumed", 5, "build-phase", "The wait is over. The run carried on by itself, on the same step.", None),
    (end, "Completed", None, None, "Marked complete after your acceptance test; no requirement was open.", None),
]
for at, kind, pos, code, msg, until in events:
    c.execute("INSERT INTO AutoRunEvent (AutoRunId, AtUtc, Kind, ProcessPosition, ProcessCode, Message, UntilUtc) VALUES (?,?,?,?,?,?,?)",
              (auto_id, iso(at), kind, pos, code, msg, iso(until) if until else None))

# ---- the eight requirements (the mockup's order, titles and verdict words) -----------------------------------
reqs = [  # id, title, verdict written, minutes after the start, checklist status, percent
    ("REQ-UI-014", "Coverage panel file list", "Verified", 41, "Verified", 100),
    ("REQ-UI-015", "Per-file coverage bar", "Verified", 66, "Verified", 100),
    ("REQ-UI-016", "Empty state when a project has no run yet", "Verified", 82, "Verified", 100),
    ("REQ-FN-021", "The filter keeps its choice between runs", "Needs re-verify", 118, "Implemented", 75),
    ("REQ-FN-022", "Coverage read straight from the test run", "Verified", 137, "Verified", 100),
    ("REQ-RAG-008", "Explain an uncovered line in plain words", "Needs re-verify", 155, "Implemented", 75),
    ("REQ-NFR-004", "The panel opens within 400 ms on 4,000 files", "Blocked", 172, "Blocked", 25),
    ("REQ-UI-017", "Filter chips match the rest of the app", "Verified", 189, "Verified", 100),
]
for rid, title, verdict, at, _status, pct in reqs:
    c.execute("INSERT INTO ProcessRunVerdict (RunId, RequirementId, ChecklistFile, Status, Percent, Remark, WrittenUtc) VALUES (?,?,?,?,?,?,?)",
              (verify_run, rid, "docs/Seed-Checklist.md", verdict, pct, "[seed]", iso(start + mins(at))))
c.commit()

# ---- the project folder: the checklist that names the requirements ------------------------------------------
os.makedirs(os.path.join(real, "docs", "metrics"), exist_ok=True)
lines = ["# Seed — Checklist (screen-check seed, replaced on every run)", "", "## Requirements Status", "",
         "| ID | Requirement | Status | % | Remarks | Details |", "|----|-------------|--------|---|---------|---------|"]
for rid, title, _verdict, _at, status, pct in reqs:
    lines.append(f"| {rid} | {title} | {status} | {pct}% | [seed] | [view](#d-{rid.lower()}) |")
with open(os.path.join(real, "docs", "Seed-Checklist.md"), "w", encoding="utf-8") as f:
    f.write("\n".join(lines) + "\n")

def rewrite(name, add):
    """Drops this seed's earlier records from a metrics stream, and any record a test left inside this run's
    time window (the report reads misses and costs by time, so they would join the report), then appends
    the new ones. The project folder is under tests/.artifacts/, checked above."""
    file = os.path.join(real, "docs", "metrics", name)
    keep = []
    lo, hi = iso_s(start), iso_s(end)
    if os.path.exists(file):
        for line in open(file, encoding="utf-8"):
            if not line.strip():
                continue
            try:
                rec = json.loads(line)
                if rec.get("seed") is True:
                    continue
                when = str(rec.get("ts") or rec.get("started") or "")[:19] + "Z"
                if len(when) == 20 and lo <= when <= hi:
                    continue
            except ValueError:
                pass
            keep.append(line.rstrip("\n"))
    with open(file, "w", encoding="utf-8") as f:
        f.write("".join(l + "\n" for l in keep + [json.dumps(r) for r in add]))

# ---- the misses: four, in the mockup's order -----------------------------------------------------------------
misses = [
    ("SEED-M1", "REQ-FN-021", 72, "The brief never said which projects the filter spans", True,
     {"miss_class": "unspecified-gap", "artifact": "brd", "found_gate": "build"}),
    ("SEED-M2", "REQ-UI-017", 120, "The chip control has no disabled state", False,
     {"miss_class": "missed-requirement", "artifact": "src", "found_by": "library-feedback", "req_class": "UI", "found_gate": "build"}),
    ("SEED-M3", "REQ-FN-022", 150, "The run retried step 4 twice before reporting the stall", True,
     {"miss_class": "wrong-behaviour", "artifact": "src", "found_gate": "verify"}),
    ("SEED-M4", "REQ-RAG-008", 160, "An empty explanation came back for a file with no hits", False,
     {"miss_class": "missed-requirement", "artifact": "src", "found_by": "library-feedback", "req_class": "RAG", "found_gate": "verify"}),
]
recs = []
for mid, rid, at, what, fixed, extra in misses:
    recs.append({"seed": True, "kind": "miss", "miss_id": mid, "ts": iso_s(start + mins(at)), "req_id": rid, "what": what, **extra})
    if fixed:
        recs.append({"seed": True, "kind": "miss-fix", "miss_id": mid, "ts": iso_s(start + mins(at + 20)), "verdict_after": "Verified"})
rewrite("misses.jsonl", recs)

# ---- the cost: one record per model, started inside the run ----------------------------------------------------
models = [  # model, tier, tokens in, tokens out, minutes of work
    ("Claude Opus 5", "deep", 412000, 96400, 82),
    ("Claude Sonnet 5", "everyday", 388000, 74200, 58),
    ("Claude Haiku 4.5", "quick", 96000, 18600, 21),
    ("GPT-5", "2nd opinion", 74000, 12800, 14),
    ("local Qwen3-30B", "on this Mac", 58000, 9800, 19),
]
costs = [{"seed": True, "kind": "run", "cmd": "verify-phase", "started": iso_s(start + mins(30 + 20 * i)),
          "ended": iso_s(start + mins(30 + 20 * i) + mins(m)), "model": model, "tier": tier,
          "tokens_in": tin, "tokens_out": tout, "duration_s": m * 60}
         for i, (model, tier, tin, tout, m) in enumerate(models)]
rewrite("runs.jsonl", costs)
for name in ("gates", "sessions", "commits"):
    f = os.path.join(real, "docs", "metrics", name + ".jsonl")
    if not os.path.exists(f):
        open(f, "w").close()

print(f"project {project}: seeded finished run auto-{auto_id} (8 requirements, 4 misses, 5 models, 3 h 14 m) and docs/Seed-Checklist.md")
PY
