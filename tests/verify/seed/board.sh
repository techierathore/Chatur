#!/usr/bin/env bash
# Seed for the Requirements board (docs/mockups/board.html), run by tf-verify-screens.sh before it opens /board,
# with TF_BASE set to the running app's address (TechieFlow TF-002).
#
# The mockup draws the board of a project with a phase 1 and a phase 2 checklist: the filter row (phase, status,
# agent), the counts and the requirement rows. A fresh verify copy has only the web harness's default fixture
# open, which has no checklist, so the board draws its empty state and none of those controls. This seed opens
# the screens' shared seed project (tests/.artifacts/verify/fixtures/screen-seed/SeedOpenApp, _seedlib.open_project)
# and gives it docs/Seed-Checklist.md and docs/Seed-P2-Checklist.md when they are missing, one row per status the
# board counts. The mockup's own rows and phase choices are sample data (data-tf-sample), so the rows here are
# not the mockup's; they only have to exist.
# Idempotent: an existing checklist is left as it is (the Run report seed rewrites Seed-Checklist.md with its own
# rows, which serve the board as well). Only a project folder and a database under tests/.artifacts/ are written.
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
import os, sys
sys.path.insert(0, sys.argv[3])
import _seedlib as s

db, root = sys.argv[1], sys.argv[2]
c = s.connect(db, root)
pid, path = s.open_project(c, root)
docs = os.path.join(path, "docs")
os.makedirs(docs, exist_ok=True)

def checklist(title, rows):
    head = [f"# {title}", "", "## Requirements Status", "",
            "| ID | Requirement | Status | % | Remarks | Details |", "|----|-------------|--------|---|---------|---------|"]
    body = [f"| {rid} | {name} | {status} | {pct}% | [seed] | [view](#d-{rid.lower()}) |" for rid, name, status, pct in rows]
    detail = ["", "## Page: Home (`/`)", ""]
    for rid, name, _, _ in rows:
        detail += [f'<a id="d-{rid.lower()}"></a>', f"- **{rid}** — {name}. *BRD:* BRD-1", f"  - *Acceptance:* When it is used, then it works."]
    return "\n".join(head + body + detail) + "\n"

wrote = []
p1 = os.path.join(docs, "Seed-Checklist.md")
if not os.path.exists(p1):
    with open(p1, "w", encoding="utf-8") as f:
        f.write(checklist("Seed — Checklist (screen-check seed)", [
            ("REQ-UI-001", "Sign in", "Verified", 100), ("REQ-FN-001", "Keep the session", "Verified", 100)]))
    wrote.append("Seed-Checklist.md")
p2 = os.path.join(docs, "Seed-P2-Checklist.md")
if not os.path.exists(p2):
    with open(p2, "w", encoding="utf-8") as f:
        f.write(checklist("Seed — Phase 2 checklist (screen-check seed)", [
            ("REQ-UI-101", "The board filters by phase", "Verified", 100),
            ("REQ-UI-102", "The board remembers the last filter", "In Progress", 50),
            ("REQ-FN-103", "A run resumes at its step", "Not Started", 0),
            ("REQ-FN-104", "A refused write is reported", "Blocked", 25)]))
    wrote.append("Seed-P2-Checklist.md")
print(f"project {pid} ({path}) open; " + (f"wrote docs/{', docs/'.join(wrote)}" if wrote else "its checklists were already there"))
PY
