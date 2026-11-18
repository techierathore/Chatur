#!/usr/bin/env bash
# Seed for the Agent workspace screen (mockups/agent-workspace.html): the verifier's open work is drawn as a
# table, so the open project needs requirements the verifier owns (NFR rows, Board/RequirementAgents.cs) that
# are not Verified. Opens the same screen-seed project as board.sh and writes docs/Seed-P3-Checklist.md with two
# open NFR rows when it is not there yet. Only writes under tests/.artifacts/. Added 2026-11-18 (full verify).
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

python3 - "$DB" "$ROOT" "$(dirname "${BASH_SOURCE[0]}")" <<'PY2'
import os, sys
sys.path.insert(0, sys.argv[3])
import _seedlib as s

db, root = sys.argv[1], sys.argv[2]
c = s.connect(db, root)
pid, path = s.open_project(c, root)
docs = os.path.join(path, "docs")
os.makedirs(docs, exist_ok=True)
p = os.path.join(docs, "Seed-P3-Checklist.md")
rows = [("REQ-NFR-105", "The board loads within a second", "In Progress", 50),
        ("REQ-NFR-106", "A run survives a restart of the app", "Implemented", 75)]
if not os.path.exists(p):
    lines = ["# Seed — Phase 3 checklist (screen-check seed)", "", "## Requirements Status", "",
             "| ID | Requirement | Status | % | Remarks | Details |", "|----|-------------|--------|---|---------|---------|"]
    lines += [f"| {rid} | {name} | {status} | {pct}% | [seed] | [view](#d-{rid.lower()}) |" for rid, name, status, pct in rows]
    lines += ["", "## Page: Home (`/`)", ""]
    for rid, name, _, _ in rows:
        lines += [f'<a id="d-{rid.lower()}"></a>', f"- **{rid}** — {name}. *BRD:* BRD-1", "  - *Acceptance:* When it is used, then it works."]
    with open(p, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
    print(f"project {pid} ({path}) open; wrote docs/Seed-P3-Checklist.md")
else:
    print(f"project {pid} ({path}) open; docs/Seed-P3-Checklist.md was already there")
PY2
