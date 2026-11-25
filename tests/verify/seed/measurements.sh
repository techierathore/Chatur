#!/usr/bin/env bash
# Seed for Measurements (docs/mockups/measurements.html), run by tf-verify-screens.sh before it opens /measurements,
# with TF_BASE set to the running app's address (TechieFlow TF-002).
#
# The screen reads the open project's own docs/metrics files. A verify copy may have no project open (the specs
# remove the projects they open), and then the screen rightly says "No project selected" and none of its regions are
# drawn. This seed opens the screens' shared seed project (_seedlib.open_project) and, when its docs/metrics holds no
# run yet, writes a small set of records so every region has something to show: three runs on two models (one with no
# cost, one with no tokens), two gate rows, and two misses (one fixed). The sample figures of the mockup are marked
# data-tf-sample there; the browser test tests/verify/measurements.spec.ts checks the figures themselves.
# Idempotent: a docs/metrics/runs.jsonl that already holds a run is left as it is.
# It only ever writes to a database and a project folder under tests/.artifacts/.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
BASE="${TF_BASE:?TF_BASE is not set: run this from tf-verify-screens.sh or set it to the app address}"
PORT="$(sed -E 's#.*:([0-9]+)/?.*#\1#' <<<"$BASE")"
DB="$ROOT/tests/.artifacts/verify/run-$PORT/harness-data/chatur.db"
# Each account has its own database (BRD-210): seed the file of whoever signed in last.
POINTER="$(dirname "$DB")/accounts/current.txt"
if [ -s "$POINTER" ]; then DB="$(dirname "$DB")/accounts/$(tr -d '\r\n' < "$POINTER")/chatur.db"; fi
[[ -f "$DB" ]] || { echo "no database for the app at $BASE (looked for $DB)"; exit 1; }
case "$DB" in
  "$ROOT/tests/.artifacts/"*) ;;
  *) echo "refusing to write to $DB: the seed only writes under tests/.artifacts/"; exit 1 ;;
esac

python3 - "$DB" "$ROOT" "$(dirname "${BASH_SOURCE[0]}")" <<'PY'
import datetime, json, os, sqlite3, sys
sys.path.insert(0, sys.argv[3])
import _seedlib
db, root = sys.argv[1], sys.argv[2]
c = sqlite3.connect(db, timeout=20)
project_id, path = _seedlib.open_project(c, root)
real = os.path.realpath(path)
if not (real + "/").startswith(os.path.realpath(os.path.join(root, "tests", ".artifacts")) + "/"):
    print(f"refusing to write for {path}: the seed only writes for projects under tests/.artifacts/"); sys.exit(1)

folder = os.path.join(real, "docs", "metrics")
os.makedirs(folder, exist_ok=True)
runs = os.path.join(folder, "runs.jsonl")
if os.path.exists(runs) and any('"cmd"' in line for line in open(runs, encoding="utf-8")):
    print(f"project {project_id}: docs/metrics already holds runs; left as it is")
    sys.exit(0)

def at(days):
    d = datetime.datetime.now(datetime.timezone.utc) - datetime.timedelta(days=days)
    return d.strftime("%Y-%m-%dT%H:%M:%SZ")

def write(name, records):
    with open(os.path.join(folder, name), "a", encoding="utf-8") as f:
        for r in records:
            f.write(json.dumps(r) + "\n")

write("runs.jsonl", [
    {"kind": "run", "ts": at(0), "cmd": "build-phase", "model": "claude-sonnet-4-5", "tokens_in": 1200, "tokens_out": 5400,
     "tokens_cache_read": 80000, "tokens_cache_write": 3000, "cost_usd": 0.62, "duration_s": 1260, "build_result": "pass", "reqs_touched": ["REQ-UI-001"]},
    {"kind": "run", "ts": at(2), "cmd": "verify-phase", "model": "claude-opus-4-1", "tokens_in": 600, "tokens_out": 2100,
     "tokens_cache_read": 20000, "tokens_cache_write": 900, "duration_s": 480, "build_result": "pass", "reqs_touched": ["REQ-UI-001", "REQ-FN-002"]},
    {"kind": "run", "ts": at(9), "cmd": "fix-issues", "model": "gpt-5-mini", "tokens_scope": "none", "build_result": "fail", "reqs_touched": ["REQ-FN-002"]},
])
write("gates.jsonl", [
    {"kind": "gate", "ts": at(2), "app": "SeedOpenApp", "req_id": "REQ-UI-001", "verdict": "Verified", "attempt": 1},
    {"kind": "gate", "ts": at(2), "app": "SeedOpenApp", "req_id": "REQ-FN-002", "verdict": "Failed", "attempt": 1},
])
write("misses.jsonl", [
    {"kind": "miss", "ts": at(2), "miss_id": "MISS-Seed-1", "req_id": "REQ-FN-002", "found_by": "gate", "found_gate": "render",
     "miss_class": "wrong-behaviour", "severity": "major", "what": "The list lost its sort after paging"},
    {"kind": "miss", "ts": at(1), "miss_id": "MISS-Seed-2", "req_id": "REQ-UI-001", "found_by": "owner", "miss_class": "ui", "severity": "minor",
     "what": "A label was cut off"},
    {"kind": "miss-fix", "ts": at(0), "miss_id": "MISS-Seed-2", "fix_attempt": 1, "verdict_after": "Verified"},
])
print(f"project {project_id}: docs/metrics seeded with 3 runs, 2 gate rows and 2 misses")
PY
